using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Mod settings. Every settings-window string (labels, tooltips, section headers,
// the reset button, the SettingsCategory mod-list name) is routed through
// .Translate() against 1.6/Languages/English/Keyed/UMW_UI.xml, mirroring the
// companion mods' localizable convention. The window body renders inside a scroll
// view that only shows a scrollbar once the content would overflow vertically;
// settings are grouped under GameFont.Medium section headers, with a pinned
// "Reset to defaults" button below the scroll view.
//
// To add a setting:
//  1. declare a public field here (with its default as the initializer),
//  2. persist it in ExposeData with Scribe_Values.Look
//     (pass the same default so an unset value loads correctly),
//  3. restore it in ResetToDefaults,
//  4. add its label/description keys to UMW_UI.xml,
//  5. surface it in DoWindowContents under a section header.
// A setting that only means something with a DLC present wraps its row in the matching
// ModsConfig.<Dlc>Active check (see allowUltratechTraits) — hidden, not disabled, and the stored
// value is never touched, so it survives a session with that DLC unloaded. A per-def setting needs no
// such check at all when its rows are generated from the DefDatabase, since a MayRequire-gated def is
// absent without its DLC (see the per-weapon rows and UniqueWeaponDefs).
// A collection-valued setting scribes with Scribe_Collections rather than Scribe_Values, and must
// re-create itself when loading finds nothing stored (Scribe_Collections leaves the field null on an
// absent or empty entry); see disabledWeapons.
// The scroll view measures its own content each frame, so new rows need no
// layout bookkeeping — they just push the scrollbar in once they don't fit.
public class UniqueMeleeWeaponsSettings : ModSettings
{
    // --- Settings fields ---------------------------------------------------

    // Per-weapon opt-out from every generation and reward pool (see
    // Patches/ThingSetMakerUtility_CanGenerate_Patch.cs for how it is enforced).
    //
    // Stores the defNames the player switched OFF rather than the ones left on, so the empty default
    // means "all enabled" and a weapon that appears later — a new one in a future version, or the
    // Royalty pair once that DLC is enabled — is on without a settings migration. Keyed by defName
    // rather than by ThingDef so the stored value survives a session with the def absent.
    public HashSet<string> disabledWeapons = new HashSet<string>();

    // Drop Woody stuffs from the random material roll when one of our unique
    // weapons is generated (see Patches/GenStuff_ExcludeWoodStuff_Patch.cs).
    public bool excludeWoodStuff = true;

    // Let the three Royalty-tech traits roll (see Patches/CompUniqueWeapon_UltratechTraits_Patch.cs).
    // Only meaningful — and only shown — with Royalty active, since without it the defs don't exist.
    public bool allowUltratechTraits = true;

    // Selection weight of the warband opportunity-site quest. This is the real
    // default; the XML rootSelectionWeight is overwritten by ApplyWarbandQuestWeight
    // at startup, so the two only need to agree for documentation's sake.
    public const float WarbandQuestWeightDefault = 0.6f;
    public float warbandQuestWeight = WarbandQuestWeightDefault;

    // Earthshake (Piledriver's ability). Both are def-field overrides applied by
    // ApplyAbilityTuning, so the XML holds only the shipped default and these are
    // the real ones. Stored in the units the slider shows rather than in ticks, so
    // the label needs no conversion and the snap grid is exact.
    public const float EarthshakeCooldownHoursDefault = 12f;
    public float earthshakeCooldownHours = EarthshakeCooldownHoursDefault;

    public const float EarthshakeRadiusDefault = 3.9f;
    public float earthshakeRadius = EarthshakeRadiusDefault;

    // Rallying Cry (Storied's ability) and the UMW_Rallied buff it grants. Same
    // def-field-override pattern; the cooldown is in days because it is an heirloom
    // moment measured against Earthshake's hours.
    public const float RallyingCryCooldownDaysDefault = 3f;
    public float rallyingCryCooldownDays = RallyingCryCooldownDaysDefault;

    public const float RallyingCryRadiusDefault = 9.9f;
    public float rallyingCryRadius = RallyingCryRadiusDefault;

    public const float RalliedDurationHoursDefault = 2f;
    public float ralliedDurationHours = RalliedDurationHoursDefault;

    // --- Transient UI state (not persisted) -------------------------------
    // Scroll offset and last-measured content height for the settings list.
    // These are presentation state, so they are deliberately NOT scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref disabledWeapons, "disabledWeapons", LookMode.Value);
        // Scribe_Collections nulls the field when the stored entry is absent or empty (which is the
        // default state — nothing disabled), so restore the empty set rather than carrying a null.
        disabledWeapons ??= new HashSet<string>();
        Scribe_Values.Look(ref excludeWoodStuff, "excludeWoodStuff", true);
        Scribe_Values.Look(ref allowUltratechTraits, "allowUltratechTraits", true);
        Scribe_Values.Look(ref warbandQuestWeight, "warbandQuestWeight", WarbandQuestWeightDefault);
        Scribe_Values.Look(ref earthshakeCooldownHours, "earthshakeCooldownHours", EarthshakeCooldownHoursDefault);
        Scribe_Values.Look(ref earthshakeRadius, "earthshakeRadius", EarthshakeRadiusDefault);
        Scribe_Values.Look(ref rallyingCryCooldownDays, "rallyingCryCooldownDays", RallyingCryCooldownDaysDefault);
        Scribe_Values.Look(ref rallyingCryRadius, "rallyingCryRadius", RallyingCryRadiusDefault);
        Scribe_Values.Look(ref ralliedDurationHours, "ralliedDurationHours", RalliedDurationHoursDefault);
    }

    // Restores every setting to its shipped default. Called by the
    // "Reset to defaults" button. Keep this in sync with the field
    // initializers above (and the Scribe_Values.Look defaults).
    public void ResetToDefaults()
    {
        disabledWeapons.Clear();
        excludeWoodStuff = true;
        allowUltratechTraits = true;
        warbandQuestWeight = WarbandQuestWeightDefault;
        earthshakeCooldownHours = EarthshakeCooldownHoursDefault;
        earthshakeRadius = EarthshakeRadiusDefault;
        rallyingCryCooldownDays = RallyingCryCooldownDaysDefault;
        rallyingCryRadius = RallyingCryRadiusDefault;
        ralliedDurationHours = RalliedDurationHoursDefault;
    }

    // True when the player has switched this weapon off. The Count check comes first because the
    // pool patch asks this about every candidate ThingDef of every roll, and the common case is an
    // empty set.
    public bool IsWeaponDisabled(ThingDef def)
    {
        return disabledWeapons.Count > 0 && def != null && disabledWeapons.Contains(def.defName);
    }

    // Whether any of our weapons can still be rolled at all. The warband quest's whole reward is one of
    // them, so it stops being offered when the answer is no (QuestNode_Root_Warband.TestRunInt).
    public bool AnyWeaponEnabled => UniqueWeaponDefs.All.Any(d => !IsWeaponDisabled(d));

    // Refreshes the one place a weapon's pool eligibility is CACHED rather than asked live, so toggling a
    // weapon mid-session needs no restart. ThingSetMakerUtility.allGeneratableItems is filled once at
    // play-data load by calling CanGenerate on every def — our patch is therefore already reflected in it
    // at startup, and this only matters for a later change. Its consumers are base-gen's item scatterers
    // (SymbolResolver_FillWithThings / _SingleThing pick a random weapon, medicine or drug from it) and
    // QuestNode_TradeRequest_GetRequestedThing. Reset() does nothing but refill that list (and
    // ThingSetMaker_Meteorite's mineables, the same way), so re-running it is idempotent and safe
    // outside a game.
    //
    // Every other pool path calls CanGenerate live through ThingSetMakerUtility.GetAllowedThingDefs and
    // needs no invalidation. (Decompile-verified 2026-07-26, RimWorld 1.6.)
    public void ApplyWeaponAvailability()
    {
        ThingSetMakerUtility.Reset();
    }

    // Writes the configured weight onto the live quest def. rootSelectionWeight is
    // read fresh from the def on every opportunity-site roll, so a def-field write
    // is all an override takes. Called after defs load (UMW_Startup) and whenever
    // the settings window closes (UniqueMeleeWeaponsMod.WriteSettings).
    public void ApplyWarbandQuestWeight()
    {
        if (UMW_QuestDefOf.UMW_OpportunitySite_Warband != null)
        {
            UMW_QuestDefOf.UMW_OpportunitySite_Warband.rootSelectionWeight = warbandQuestWeight;
        }
    }

    // Writes the configured ability tuning onto the live defs. Called after defs load
    // (UMW_Startup) and whenever the settings window closes
    // (UniqueMeleeWeaponsMod.WriteSettings), same as ApplyWarbandQuestWeight.
    //
    // Every field written here is read fresh at use, so no restart is needed:
    // AbilityDef.cooldownTicksRange is sampled per cast (Ability.StartCooldown takes
    // .RandomInRange), and Verb.EffectiveRange resolves verbProps.AdjustedRange live
    // rather than caching (both decompile-verified 2026-07-25, RimWorld 1.6).
    public void ApplyAbilityTuning()
    {
        SetCooldown(UMW_DefOf.UMW_Earthshake, earthshakeCooldownHours * GenDate.TicksPerHour);
        SetRadius(UMW_DefOf.UMW_Earthshake, earthshakeRadius);
        ScaleAreaFleck(UMW_DefOf.UMW_Earthshake, earthshakeRadius / EarthshakeRadiusDefault);

        SetCooldown(UMW_DefOf.UMW_RallyingCry, rallyingCryCooldownDays * GenDate.TicksPerDay);
        SetRadius(UMW_DefOf.UMW_RallyingCry, rallyingCryRadius);
        SetHediffDuration(UMW_DefOf.UMW_Rallied, ralliedDurationHours * GenDate.TicksPerHour);
    }

    private static void SetCooldown(AbilityDef def, float ticks)
    {
        if (def == null)
        {
            return;
        }
        int rounded = Mathf.RoundToInt(ticks);
        def.cooldownTicksRange = new IntRange(rounded, rounded);
    }

    // An ability's radius lives in two places that must agree, per CLAUDE.md: verbProperties.range
    // drives the hover ring (VerbProperties.DrawRadiusRing reads verb.EffectiveRange and never a comp
    // field), and the effect comp's own radius drives what actually happens. Writing only one of them
    // would leave a preview that lies about the burst, so this owns both.
    private static void SetRadius(AbilityDef def, float radius)
    {
        if (def == null)
        {
            return;
        }
        if (def.verbProperties != null)
        {
            def.verbProperties.range = radius;
        }
        for (int i = 0; i < def.comps.Count; i++)
        {
            switch (def.comps[i])
            {
                case CompProperties_AbilityGroundShockwave shockwave:
                    shockwave.explosionRadius = radius;
                    break;

                case CompProperties_AbilityRallyAllies rally:
                    rally.radius = radius;
                    break;
            }
        }
    }

    // Resize an ability's fleck along with its radius, for a fleck that depicts the AREA of the effect:
    // otherwise a resized burst keeps a fixed-size shimmer sitting over it. Opt-in per ability rather
    // than folded into SetRadius, because a fleck is not necessarily an area indicator — Rallying Cry's
    // lightshaft is a beam over the wielder, and scaling THAT to a 12.9-cell rally would put a column of
    // light over one pawn. Only Earthshake's overhead ripple qualifies.
    //
    // Deliberately approximate: FleckDef.growthRate adds a component that this factor does not scale
    // (FleckStatic grows linearScale additively), so at large radii the ripple lands a little inside the
    // effect edge rather than a little outside it. It tracks, which is the point.
    private static void ScaleAreaFleck(AbilityDef def, float factor)
    {
        if (def == null)
        {
            return;
        }
        for (int i = 0; i < def.comps.Count; i++)
        {
            if (def.comps[i] is CompProperties_AbilityFleckOnTarget fleck)
            {
                fleck.scale = factor;
            }
        }
    }

    // Hediff duration is an IntRange on the disappear comp's props, read at HediffComp_Disappears
    // .CompPostMake via .RandomInRange — so a props write governs every hediff added from then on, with
    // no restart and without touching instances already running on a pawn.
    private static void SetHediffDuration(HediffDef def, float ticks)
    {
        if (def?.comps == null)
        {
            return;
        }
        int rounded = Mathf.RoundToInt(ticks);
        for (int i = 0; i < def.comps.Count; i++)
        {
            if (def.comps[i] is HediffCompProperties_Disappears disappears)
            {
                disappears.disappearsAfterTicks = new IntRange(rounded, rounded);
            }
        }
    }

    public void DoWindowContents(Rect inRect)
    {
        const float buttonHeight = 30f;
        const float buttonGap = 10f;
        const float buttonWidth = 200f;
        const float scrollBarWidth = 16f;

        // Reserve the bottom strip for the pinned reset button; the scroll
        // view gets everything above it.
        Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
        Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);

        // The inner (content) rect is as wide as the view minus the scrollbar
        // gutter, and as tall as the content OR the view — whichever is larger.
        // When content fits, inner == view height, so no scrollbar shows; once
        // content exceeds the view, the inner grows and the scrollbar appears.
        // contentHeight is 0 on the first frame (so no scroll) and is measured
        // from the listing below for every frame after.
        float innerWidth = viewRect.width - scrollBarWidth;
        Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(contentHeight, viewRect.height));

        Widgets.BeginScrollView(viewRect, ref scrollPosition, innerRect);

        Listing_Standard listing = new Listing_Standard();
        // Begin with a tall scratch rect (99999f) so the listing never clamps
        // its own height; we read the real height back via CurHeight afterwards.
        listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
        GameFont prevFont = Text.Font;

        listing.Gap();

        // --- Weapons ------------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsWeapons".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        WeaponRows(listing);

        listing.Gap(18f);

        // --- Generation ---------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsGeneration".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        listing.CheckboxLabeled(
            "UMW_ExcludeWoodStuff".Translate(),
            ref excludeWoodStuff,
            "UMW_ExcludeWoodStuffDesc".Translate());

        // Royalty-only row: the three traits it governs are MayRequire-gated on Royalty, so without
        // the DLC there is nothing to toggle and the row would only confuse. Hidden rather than
        // disabled, and the stored value is left alone so it survives a run without Royalty loaded.
        if (ModsConfig.RoyaltyActive)
        {
            listing.CheckboxLabeled(
                "UMW_AllowUltratechTraits".Translate(),
                ref allowUltratechTraits,
                "UMW_AllowUltratechTraitsDesc".Translate());
        }

        listing.Gap(18f);

        // --- Abilities ----------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsAbilities".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        earthshakeCooldownHours = SliderRow(
            listing, "UMW_EarthshakeCooldown", "UMW_EarthshakeCooldownDesc",
            earthshakeCooldownHours, EarthshakeCooldownHoursDefault,
            min: 0f, max: 24f, step: 1f, format: "0");

        earthshakeRadius = SliderRow(
            listing, "UMW_EarthshakeRadius", "UMW_EarthshakeRadiusDesc",
            earthshakeRadius, EarthshakeRadiusDefault,
            min: 1.9f, max: 12.9f, step: 1f, format: "0.0");

        listing.Gap(6f);

        rallyingCryCooldownDays = SliderRow(
            listing, "UMW_RallyingCryCooldown", "UMW_RallyingCryCooldownDesc",
            rallyingCryCooldownDays, RallyingCryCooldownDaysDefault,
            min: 1f, max: 15f, step: 0.5f, format: "0.#");

        rallyingCryRadius = SliderRow(
            listing, "UMW_RallyingCryRadius", "UMW_RallyingCryRadiusDesc",
            rallyingCryRadius, RallyingCryRadiusDefault,
            min: 1.9f, max: 12.9f, step: 1f, format: "0.0");

        ralliedDurationHours = SliderRow(
            listing, "UMW_RalliedDuration", "UMW_RalliedDurationDesc",
            ralliedDurationHours, RalliedDurationHoursDefault,
            min: 1f, max: 24f, step: 1f, format: "0");

        listing.Gap(18f);

        // --- Quests -------------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsQuests".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        // Annotated at 1.0, which is AncientMercenaries' own rootSelectionWeight: our quest shares the
        // opportunity-site giver pool with it, so that mark is the reference point for "as often as the
        // vanilla one". The name comes from that quest's site-part def (see UMW_QuestDefOf.BanditGang)
        // so it is the same localized string the world map shows, not a second copy of it here.
        warbandQuestWeight = SliderRow(
            listing, "UMW_WarbandQuestWeight", "UMW_WarbandQuestWeightDesc",
            warbandQuestWeight, WarbandQuestWeightDefault,
            min: 0f, max: 2f, step: 0.05f, format: "0.00",
            annotateAt: 1f, annotationLabel: UMW_QuestDefOf.BanditGang?.label);

        // Measure the content height for next frame's scroll calculation,
        // restore the font, then close the listing and the scroll view.
        Text.Font = prevFont;
        contentHeight = listing.CurHeight;
        listing.End();
        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonRect, "UMW_ResetToDefaults".Translate()))
        {
            ResetToDefaults();
        }
    }

    // One checkbox per weapon we own, toggling it in or out of every generation and reward pool.
    //
    // The list is generated from the DefDatabase (UniqueWeaponDefs.All), not hand-written, so the
    // Royalty-gated axe and warhammer rows appear only when that DLC is active — their defs are
    // MayRequire-gated and simply do not exist otherwise — and a weapon added in a future version gets
    // its row for free. The label is the weapon's own def label, so it is already localized.
    //
    // The checkbox reads as "enabled" while the stored set holds the DISABLED ones, so the ref bool is a
    // local that is written back only on an actual change; that keeps the set free of entries for
    // weapons the player never touched.
    private void WeaponRows(Listing_Standard listing)
    {
        foreach (ThingDef weapon in UniqueWeaponDefs.All)
        {
            bool enabled = !IsWeaponDisabled(weapon);
            bool wasEnabled = enabled;
            listing.CheckboxLabeled(
                weapon.LabelCap,
                ref enabled,
                "UMW_WeaponEnabledDesc".Translate(weapon.label));
            if (enabled == wasEnabled)
            {
                continue;
            }
            if (enabled)
            {
                disabledWeapons.Remove(weapon.defName);
            }
            else
            {
                disabledWeapons.Add(weapon.defName);
            }
        }
    }

    // One labelled slider row in the house style: "Label: value", an inline "(default)" suffix while it
    // sits at the shipped value, the description as a hover tooltip, and the returned value snapped to
    // `step` measured from `min` (so a 1.9-to-12.9 radius lands on 1.9, 2.9, ... and never between).
    //
    // `annotateAt` + `annotationLabel` optionally mark another value as a named reference point
    // ("(same as ancient mercenaries)"), for a setting whose number means nothing on its own.
    //
    // Suffixes follow the companion mod's convention (UWU_Mod.DoSettingsWindowContents): each is an
    // independent " (word)" fragment tested on its own and appended in order, with NO precedence between
    // them — a value that is both the default and a reference point shows both, rather than one silently
    // suppressing the other. Keep it that way when adding a suffix: no else-chaining.
    //
    // The default check is Mathf.Approximately rather than ==, because snapping off a non-zero `min`
    // does not reproduce the default's exact float: Round((3.9f - 1.9f)/1f) * 1f + 1.9f is
    // 3.8999999761, while the 3.9f literal is 3.9000000954. An exact compare would silently never show
    // the suffix on those rows. The residue is far below anything the game can act on.
    private static float SliderRow(Listing_Standard listing, string labelKey, string descKey,
        float value, float defaultValue, float min, float max, float step, string format,
        float? annotateAt = null, string annotationLabel = null)
    {
        string label = labelKey.Translate(value.ToString(format));
        if (Mathf.Approximately(value, defaultValue))
        {
            label += "UMW_DefaultSuffix".Translate();
        }
        if (annotateAt.HasValue && annotationLabel != null && Mathf.Approximately(value, annotateAt.Value))
        {
            label += "UMW_SameAsSuffix".Translate(annotationLabel);
        }
        listing.Label(label, tooltip: descKey.Translate());
        return Mathf.Round((listing.Slider(value, min, max) - min) / step) * step + min;
    }
}

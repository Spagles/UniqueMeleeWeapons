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
// The scroll view measures its own content each frame, so new rows need no
// layout bookkeeping — they just push the scrollbar in once they don't fit.
public class UniqueMeleeWeaponsSettings : ModSettings
{
    // --- Settings fields ---------------------------------------------------

    // Drop Woody stuffs from the random material roll when one of our unique
    // weapons is generated (see Patches/GenStuff_ExcludeWoodStuff_Patch.cs).
    public bool excludeWoodStuff = true;

    // Selection weight of the warband opportunity-site quest. This is the real
    // default; the XML rootSelectionWeight is overwritten by ApplyWarbandQuestWeight
    // at startup, so the two only need to agree for documentation's sake.
    public const float WarbandQuestWeightDefault = 0.6f;
    public float warbandQuestWeight = WarbandQuestWeightDefault;

    // --- Transient UI state (not persisted) -------------------------------
    // Scroll offset and last-measured content height for the settings list.
    // These are presentation state, so they are deliberately NOT scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref excludeWoodStuff, "excludeWoodStuff", true);
        Scribe_Values.Look(ref warbandQuestWeight, "warbandQuestWeight", WarbandQuestWeightDefault);
    }

    // Restores every setting to its shipped default. Called by the
    // "Reset to defaults" button. Keep this in sync with the field
    // initializers above (and the Scribe_Values.Look defaults).
    public void ResetToDefaults()
    {
        excludeWoodStuff = true;
        warbandQuestWeight = WarbandQuestWeightDefault;
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

        // --- Generation ---------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsGeneration".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        listing.CheckboxLabeled(
            "UMW_ExcludeWoodStuff".Translate(),
            ref excludeWoodStuff,
            "UMW_ExcludeWoodStuffDesc".Translate());

        listing.Gap(18f);

        // --- Quests -------------------------------------------------------
        Text.Font = GameFont.Medium;
        listing.Label("UMW_SettingsQuests".Translate());
        Text.Font = GameFont.Small;
        listing.Gap(6f);

        // Inline "(default)" suffix when the slider sits at the shipped value.
        // The value snaps to a 0.05 grid below, so the float compare is exact.
        string weightLabel = "UMW_WarbandQuestWeight".Translate(warbandQuestWeight.ToString("0.00"));
        if (warbandQuestWeight == WarbandQuestWeightDefault)
        {
            weightLabel += "UMW_DefaultSuffix".Translate();
        }
        listing.Label(weightLabel, tooltip: "UMW_WarbandQuestWeightDesc".Translate());
        warbandQuestWeight = Mathf.Round(listing.Slider(warbandQuestWeight, 0f, 2f) * 20f) / 20f;

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
}

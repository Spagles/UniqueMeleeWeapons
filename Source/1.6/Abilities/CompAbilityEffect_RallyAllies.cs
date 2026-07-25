using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Backs UMW_RallyingCry, the active half of the Storied trait (Melee). Core's DLC-free
// CompAbilityEffect_* roster has no ally-AoE-buff shape — every stock effect either targets a single
// pawn/cell or is hostile-only (verified per spec) — hence this custom comp. Faction-SYMMETRIC by
// design (user directive 2026-07-24, superseding the spec's player-faction-only target set): the cry
// rallies the CASTER's faction, whoever that is, so a hostile wielder rallies its own raid. That is
// also why the payload is a timed hediff (pain dampening) rather than the original mood memory —
// stat effects are real for NPCs, mood is not.
public class CompProperties_AbilityRallyAllies : CompProperties_AbilityEffect
{
    // Cells around the caster scanned for ralliable allies (an EMPPulse-style AoE footprint —
    // cf. CompProperties_AbilityExplosion.explosionRadius).
    public float radius = 9.9f;

    // Timed hediff granted to every pawn the cry reaches (UMW_Rallied).
    public HediffDef hediffDef;

    public CompProperties_AbilityRallyAllies()
    {
        compClass = typeof(CompAbilityEffect_RallyAllies);
    }
}

// On cast: every humanlike of the caster's faction (animals and mechs take no heart from a speech)
// spawned on the caster's map, within radius and with line of sight to the caster, gains hediffDef.
// The caster is within radius of their own position, so they rally too. A factionless caster rallies
// no one.
//
// Also emits the cry's speech bubble. That has to live here rather than on the def: the only XML route
// to a mote at cast time is CompProperties_AbilityMoteOnTarget, which reaches Mote_Speech via
// MoteMaker.MakeAttachedOverlay and so never calls MoteBubble.SetupMoteBubble — you get the bubble
// background with no symbol inside it. MakeSpeechBubble does both (decompile-verified 2026-07-25).
// The audible half of the cry needs no code at all: it rides the inherited soundMale/soundFemale that
// CompAbilityEffect.Apply plays gender-aware, wired on the def to our own one-shot copies of Core's
// throne-speech recording. See Defs/SoundDefs/RallyingCryShout_Male.xml.
// The def's remaining cast visuals (the sunbeam fleck, the raised-weapon pose) are pure XML; see
// Defs/AbilityDefs/RallyingCry.xml.
[StaticConstructorOnStartup]
public class CompAbilityEffect_RallyAllies : CompAbilityEffect
{
    // Core's generic "speaking" symbol — the texture Core's own SpeechBase AbilityDef and
    // JobDriver_GiveSpeech (the closest vanilla analog to this: a pawn deliberately addressing a room,
    // not a two-pawn social interaction) both use. Core-owned, so no MayRequire is involved.
    // [StaticConstructorOnStartup] keeps the ContentFinder call on the main thread after content load.
    private static readonly Texture2D SpeechSymbol =
        ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Speech");

    private new CompProperties_AbilityRallyAllies Props => (CompProperties_AbilityRallyAllies)props;

    // The AI half of the "must be able to speak" gate. The def carries the vanilla
    // CompProperties_AbilityRequiresCapacity (Talking), but that comp overrides only GizmoDisabled,
    // and Ability.AICanTargetNow gates on comp CanCast instead - so on its own it would leave a mute
    // AI wielder still shouting, and this ability is faction-symmetric by design. Both halves are
    // needed: CanCast alone would disable the player's gizmo with a blank reason, because
    // Ability.GizmoDisabled surfaces only AcceptanceReport.Reason for a comp-vetoed cast.
    public override bool CanCast =>
        parent.pawn?.health?.capacities?.CapableOf(PawnCapacityDefOf.Talking) ?? false;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = parent.pawn;
        Map map = caster?.Map;
        if (map == null || caster.Faction == null || Props.hediffDef == null)
        {
            return;
        }

        // After the guards: a cast that rallies no one shouldn't mime the cry either. base.Apply is called
        // HERE rather than at the top of the method (where the stock comps put it) for the same reason —
        // it is what plays the comp's soundMale/soundFemale, so the shout has to be gated exactly as the
        // bubble is. Nothing else in CompAbilityEffect.Apply is affected by the move: this comp leaves
        // screenShakeIntensity, goodwillImpact, clamorType and message unset.
        base.Apply(target, dest);
        MoteMaker.MakeSpeechBubble(caster, SpeechSymbol);

        List<Pawn> allies = map.mapPawns.SpawnedPawnsInFaction(caster.Faction);
        for (int i = 0; i < allies.Count; i++)
        {
            Pawn ally = allies[i];
            if (!ally.RaceProps.Humanlike)
            {
                continue;
            }
            if (!ally.Position.InHorDistOf(caster.Position, Props.radius))
            {
                continue;
            }
            if (!GenSight.LineOfSight(caster.Position, ally.Position, map))
            {
                continue;
            }
            ally.health?.AddHediff(Props.hediffDef);
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
// spawned on the caster's map, within radius and with line of sight to the caster, gains hediffDef and
// answers with a brief speech-lines fleck. The caster is within radius of their own position, so they
// rally too (but answer nothing — they are the one being answered, and carry the cry's own speech bubble
// instead). A factionless caster rallies no one.
//
// Also emits the cry's speech bubble. That has to live here rather than on the def: the only XML route
// to a mote at cast time is CompProperties_AbilityMoteOnTarget, which reaches Mote_Speech via
// MoteMaker.MakeAttachedOverlay and so never calls MoteBubble.SetupMoteBubble — you get the bubble
// background with no symbol inside it. MakeSpeechBubble does both (decompile-verified 2026-07-25).
// The audible half of the cry reads its two SoundDefs off the inherited soundMale/soundFemale props
// (wired on the def to our own one-shot copies of Core's throne-speech recording — see
// Defs/SoundDefs/RallyingCryShout_Male.xml) but plays them itself rather than letting
// CompAbilityEffect.Apply do it, so the caster's own voice pitch can ride along. See PlayShout.
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

    // Vanilla's own offsets for the answering speech-lines fleck, lifted from Core's `Speech`
    // RitualVisualEffectDef: beside the head when the pawn is side-on, above it when facing away, and
    // centred when facing the camera — that def sets east/west/north and leaves southRotationOffset at
    // zero, which is the fallthrough here.
    private static Vector3 SpeechLinesOffset(Rot4 rotation)
    {
        if (rotation == Rot4.East)
        {
            return new Vector3(0.5f, 0f, 0.2f);
        }
        if (rotation == Rot4.West)
        {
            return new Vector3(-0.5f, 0f, 0.2f);
        }
        if (rotation == Rot4.North)
        {
            return new Vector3(0f, 0f, 0.5f);
        }
        return Vector3.zero;
    }

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

        // After the guards: a cast that rallies no one shouldn't mime the cry either.
        //
        // base.Apply is deliberately NOT called. For this comp its only live contribution was playing
        // Props.soundMale/soundFemale, which PlayShout now does with the caster's own voice pitch — and
        // everything else it does is inert here: screenShakeIntensity, goodwillImpact, clamorType and
        // message are all left unset. Skipping it matches CompAbilityEffect_GroundShockwave, which skips
        // it for the same reason. FOOTGUN: setting any of those four props fields later would silently do
        // nothing until this call is restored.
        PlayShout(caster, map);
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

            // Each ally answers with Core's ritual speech-lines fleck. Deliberately NOT a speech bubble:
            // the caster's full bubble should stay the loud thing on screen, and this ability can reach
            // 20+ pawns at its widest radius, where that many bubbles would be a wall. SpeechLines is
            // drawSize 0.7 and lives 0.25s (fadeIn 0.03 + solid 0.2 + fadeOut 0.02), so a crowd answering
            // reads as a brief collective murmur. It is also what vanilla itself uses to show a pawn
            // speaking outside a bubble — Core's `Speech` RitualVisualEffectDef spawns this over a ritual
            // organizer on an interval.
            //
            // Skipped for the caster, who is in this list (they are within radius of their own position
            // and rally themselves) and already carries the cry's bubble — otherwise one pawn shows both.
            //
            // All answers land on the same tick rather than rippling outward from the caster: there is no
            // delay hook here, and answering as one is the read we want. The fleck's 0.25s life also makes
            // its fixed spawn position a non-issue, even though a fleck does not follow a moving pawn
            // (FleckMaker.AttachedOverlay only snapshots DrawPos; only Motes track their parent).
            if (ally != caster)
            {
                FleckCreationData lines = FleckMaker.GetDataAttachedOverlay(
                    ally, UMW_DefOf.SpeechLines, SpeechLinesOffset(ally.Rotation));
                // Mirrors the ritual comp, which passes pawn.Rotation.AsAngle. If the lines read oddly on
                // pawns facing away mid-combat, dropping this line is the first thing to try: vanilla only
                // ever spawns this fleck on a seated organizer, so the angle is untested in the open.
                lines.rotation = ally.Rotation.AsAngle;
                map.flecks.CreateFleck(lines);
            }
        }
    }

    // The shout. Played here rather than by CompAbilityEffect.Apply so it can carry the caster's own
    // voice, which that method cannot: it plays the sound through a bare PlayOneShot with no pitch hook.
    // SoundInfo.pitchFactor does have one, and Pawn_StoryTracker.VoicePitchFactor is a 0.85-1.15
    // multiplier seeded on the pawn's thingIDNumber — so it is stable for the life of that pawn, differs
    // between pawns, and is INDEPENDENT of gender (which separately selects the recording, via the two
    // soundMale/soundFemale defs). One particular colonist's cry therefore always sounds like them.
    // This is exactly what vanilla speeches do: JobDriver_GiveSpeech feeds the same value to
    // PlaySustainerOrSound (all decompile-verified 2026-07-25, RimWorld 1.6).
    //
    // A def-level SoundDef.pitchRange would NOT achieve this — that re-rolls on every play, so the same
    // pawn would sound like a different person each cry, which is worse than no variation at all.
    //
    // The gender switch mirrors CompAbilityEffect.Apply's exactly, including Gender.None falling through
    // to the unset Props.sound, i.e. silence. See Defs/AbilityDefs/RallyingCry.xml for why that no-op is
    // the right one.
    private void PlayShout(Pawn caster, Map map)
    {
        SoundDef shout = caster.gender switch
        {
            Gender.Male => Props.soundMale ?? Props.sound,
            Gender.Female => Props.soundFemale ?? Props.sound,
            _ => Props.sound,
        };
        if (shout == null)
        {
            return;
        }

        SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, map));
        info.pitchFactor = caster.story?.VoicePitchFactor ?? 1f;
        shout.PlayOneShot(info);
    }
}

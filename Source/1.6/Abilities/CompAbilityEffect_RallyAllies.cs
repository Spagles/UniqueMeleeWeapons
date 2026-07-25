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

// On cast, in three widening circles:
//   • WAKES every sleeping pawn in radius, friend or foe, animals included — no faction or line-of-sight
//     test, because a shout is a noise and noise does not care who is listening or where the walls are.
//   • RALLIES every humanlike in radius that is NOT hostile to the caster and can see them: hediffDef
//     plus a brief speech-lines fleck answering back. Deliberately across faction lines rather than the
//     caster's own faction only (corrected 2026-07-25) — an allied or neutral bystander watching someone
//     invoke a storied weapon's history takes heart from it just as a squadmate does, and gating on
//     faction identity made allied pawns fighting beside you inexplicably immune. Animals and mechs take
//     no heart from a speech; prisoners are excluded on purpose (see the loop).
//   • Anyone who was ASLEEP gets neither — just the vanilla disturbed-sleep grudge for being woken.
// The caster is within radius of their own position, so they rally themselves (but answer nothing — they
// are the one being answered, and carry the cry's own speech bubble instead).
//
// A factionless caster still rallies no one, and that guard is now load-bearing for a subtler reason than
// before: GenHostility.HostileTo returns false whenever EITHER side has a null faction, so without the
// guard a factionless wielder would read every raider on the map as non-hostile and rally the people
// trying to kill them.
//
// Also emits the cry's speech bubble. That has to live here rather than on the def: the only XML route
// to a mote at cast time is CompProperties_AbilityMoteOnTarget, which reaches Mote_Speech via
// MoteMaker.MakeAttachedOverlay and so never calls MoteBubble.SetupMoteBubble — you get the bubble
// background with no symbol inside it. MakeSpeechBubble does both (decompile-verified 2026-07-25).
// Also drives Core's SpeechLines fleck on both sides of the cry: blinking over the caster for the length
// of the raised-weapon pose (CompTick), and once over each pawn that answers. See those two members.
//
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

    // Where a RESPONDER's speech-lines fleck sits, and which way it points. Deliberately NOT vanilla's
    // per-facing offsets from Core's `Speech` RitualVisualEffectDef (which put the lines beside the head
    // when side-on and centred when facing the camera, i.e. down around the mouth): the fiction here is a
    // whole squad shouting UPWARD in unison, so the lines are held clear above every head regardless of
    // which way the pawn faces, and the angle is flipped 180 degrees from vanilla's so they read as
    // rising rather than falling. 0.5 is vanilla's own north-facing lift, which clears the head.
    private static readonly Vector3 ResponderLinesOffset = new Vector3(0f, 0f, 0.5f);

    // Core's SpeechLines is authored for a ritual: 0.25s all told (fadeIn 0.03 + solid 0.2 + fadeOut
    // 0.02), respawned on an interval so it reads as a blink. That cadence is right for the CASTER, who
    // keeps talking for the whole cry, but far too brief for a RESPONDER, who gets exactly one and needs
    // it legible. FleckStatic.SolidTime honours a per-instance solidTimeOverride, so the responder's is
    // stretched to a full second WITHOUT cloning Core's def or disturbing the ritual that shares it:
    // 0.95 solid plus the def's own 0.05 of fade.
    private const float ResponderSolidTime = 0.95f;

    // Vanilla's own respawn cadence for this fleck, from Core's `Speech` RitualVisualEffectDef
    // (spawnIntervalTicks 45). Left at the def's own 0.25s life, so the caster reads as a quarter-second
    // on, half a second off — a blink, matching the Royalty speech look rather than a solid glow.
    private const int CasterLinesIntervalTicks = 45;

    // Ticks of blinking left over the caster. Purely cosmetic, so deliberately NOT scribed: 0 is the inert
    // value a load lands on, which just means a save/load part-way through a cry stops the blink early.
    private int cryTicksLeft;

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

        // Start the caster's own speech lines blinking, and land the first one now so it coincides with the
        // shout rather than 45 ticks after it. Length is read off the def rather than hardcoded so it
        // tracks defaultCooldownTime whenever that is retuned. Apply runs at the END of warmup —
        // Verb.TryCastNextBurstShot casts and only THEN sets Stance_Cooldown — so the raised-weapon pose
        // still to come is exactly defaultCooldownTime, which is the window we want to fill.
        cryTicksLeft = (parent.def.verbProperties?.defaultCooldownTime ?? 0f).SecondsToTicks();
        SpawnCasterLines(caster, map);

        // Every spawned pawn on the map, distance-filtered — the same shape vanilla's closest analog uses
        // (CompAbilityEffect_Neuroquake sweeps AllPawnsSpawned rather than a GenRadial query). Walked
        // backwards because waking a pawn ends its current job, and a live list is safer traversed in
        // reverse. AllPawnsSpawned is an incrementally-maintained cache, so this allocates nothing.
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = pawns.Count - 1; i >= 0; i--)
        {
            Pawn pawn = pawns[i];
            if (!pawn.Position.InHorDistOf(caster.Position, Props.radius))
            {
                continue;
            }

            // WAKING comes first, and is the one effect with no faction test and no line-of-sight test: a
            // shout carries by sound, through walls and to friend and foe alike. Animals included — a yell
            // wakes the sleeping muffalo too.
            //
            // !Awake() alone is not "asleep": RestUtility.Awake() returns false BOTH for a pawn in the
            // LayDown job and for one whose Consciousness has crashed, so an anaesthetised or comatose pawn
            // reads identically to a sleeper. CanBeAwake separates them, and Deathresting is its own
            // Biotech state — neither should count as merely sleeping, and neither should collect a
            // disturbed-sleep grudge for it. RestUtility.WakeUp is safe regardless (it no-ops on a Downed
            // pawn internally), but the distinction still matters for the branch below.
            bool wasAsleep = !pawn.Awake() && pawn.health.capacities.CanBeAwake && !pawn.Deathresting;
            if (wasAsleep)
            {
                RestUtility.WakeUp(pawn);
            }

            // Everything past here is the rally proper, which reaches humanlikes that are not hostile to
            // the caster — across faction lines, so allied and neutral pawns take heart too, not just the
            // caster's own. Prisoners are excluded despite reading as non-hostile: GenHostility treats a
            // quiet captive as friendly, but a captive is not a comrade, and pain-dampening plus stagger
            // resistance on the people in your cells is a prison break waiting to happen.
            if (!pawn.RaceProps.Humanlike || pawn.IsPrisoner || pawn.HostileTo(caster))
            {
                continue;
            }

            // A sleeper gets no buff and no answering shout, just a grudge about being woken. Vanilla has
            // no single helper for this: walking past a sleeping pawn (ClamorDefOf.Movement) applies this
            // very thought and never wakes anyone, while every wake path never applies a thought, and
            // Pawn.CheckForDisturbedSleep — which does hold both halves — is private. So this is the same
            // two explicit calls Neuroquake makes for its own wake-and-thought sweep, which is as
            // idiomatic as it gets. Note vanilla's private version additionally limits the thought to
            // player-faction pawns and rate-limits it with an unreadable private tick field; the ThoughtDef's
            // own stackLimit of 3 covers spam for a 3-day-cooldown ability, and the null-conditional covers
            // every pawn that has no mood need to hurt.
            if (wasAsleep)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.SleepDisturbed);
                continue;
            }

            // The cry has to be SEEN to rally: it is the weapon's history being invoked, not just a noise.
            // This is why RallyingCry.xml sets drawHighlightWithLineOfSight, unlike Earthshake.
            if (!GenSight.LineOfSight(caster.Position, pawn.Position, map))
            {
                continue;
            }

            pawn.health?.AddHediff(Props.hediffDef);

            // Each rallied pawn answers with Core's ritual speech-lines fleck. Deliberately NOT a speech
            // bubble: the caster's full bubble should stay the loud thing on screen, and this ability can
            // reach 20+ pawns at its widest radius, where that many bubbles would be a wall. SpeechLines is
            // drawSize 0.7 and lives 0.25s (fadeIn 0.03 + solid 0.2 + fadeOut 0.02), so a crowd answering
            // reads as a brief collective murmur. It is also what vanilla itself uses to show a pawn
            // speaking outside a bubble — Core's `Speech` RitualVisualEffectDef spawns this over a ritual
            // organizer on an interval.
            //
            // Skipped for the caster, who reaches this loop too (they are within radius of their own
            // position and rally themselves) and already carries the cry's bubble — otherwise one pawn
            // would show both.
            //
            // All answers land on the same tick rather than rippling outward from the caster: there is no
            // delay hook here, and answering as one is the read we want. The fleck's 0.25s life also makes
            // its fixed spawn position a non-issue, even though a fleck does not follow a moving pawn
            // (FleckMaker only snapshots DrawPos; only Motes track their parent).
            if (pawn != caster)
            {
                SpawnSpeechLines(
                    pawn, map, ResponderLinesOffset, pawn.Rotation.AsAngle + 180f, ResponderSolidTime);
            }
        }
    }

    // Keeps the caster's speech lines blinking for the rest of the raised-weapon pose, the way Core's
    // `Speech` RitualVisualEffectDef keeps them going over a ritual organizer. Ability comps really are
    // ticked for a weapon-trait ability: Pawn.Tick (not TickInterval, so this is genuinely per-tick) calls
    // Pawn_AbilityTracker.AbilitiesTick over AllAbilitiesForReading, which folds in the equipped primary's
    // CompEquippableAbility.AbilityForReading, and Ability.AbilityTick calls CompTick on each comp
    // (decompile-verified 2026-07-25, RimWorld 1.6). Consequence worth knowing: the blink stops early if
    // the weapon stops being the pawn's primary mid-cry, which is the correct outcome anyway.
    public override void CompTick()
    {
        base.CompTick();
        if (cryTicksLeft <= 0)
        {
            return;
        }

        cryTicksLeft--;
        if (cryTicksLeft <= 0 || cryTicksLeft % CasterLinesIntervalTicks != 0)
        {
            return;
        }

        Pawn caster = parent.pawn;
        Map map = caster?.Map;
        if (map == null || caster.Dead)
        {
            cryTicksLeft = 0;
            return;
        }
        SpawnCasterLines(caster, map);
    }

    // Vanilla's south-facing case verbatim: centred on the pawn (Core's `Speech` def leaves
    // southRotationOffset at zero) at Rot4.South.AsAngle. Fixed rather than following the caster's real
    // facing — unlike the responders above, this is the speech-giving pose itself, and the Royalty ritual
    // look is the one being mimicked.
    private static void SpawnCasterLines(Pawn caster, Map map)
    {
        SpawnSpeechLines(caster, map, Vector3.zero, Rot4.South.AsAngle, solidTimeOverride: -1f);
    }

    // solidTimeOverride of -1 means "use the def's own solidTime", which is how FleckStatic.SolidTime
    // reads the sentinel.
    private static void SpawnSpeechLines(
        Pawn pawn, Map map, Vector3 offset, float angle, float solidTimeOverride)
    {
        FleckCreationData lines = FleckMaker.GetDataAttachedOverlay(
            pawn, UMW_DefOf.SpeechLines, offset, scale: 1f, solidTimeOverride: solidTimeOverride);
        lines.rotation = angle;
        map.flecks.CreateFleck(lines);
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

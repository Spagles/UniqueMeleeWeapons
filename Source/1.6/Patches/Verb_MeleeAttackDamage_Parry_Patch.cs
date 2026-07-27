using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Resolves a defender-side PARRY for unique weapons carrying a MeleeParryExtension trait
// (UMW_Quilloned): a chance to catch an incoming melee blow on the guard and negate it outright.
//
// WHERE THIS SITS IN VANILLA'S SWING (Verb_MeleeAttack.TryCastShot, decompile-verified 1.6):
// miss roll (attacker MeleeHitChance) → dodge roll (defender MeleeDodgeChance) → hit branch:
// create a "hit" battle-log entry, call ApplyMeleeDamageToTarget, then post-process the entry —
// REMOVE it when the result is stun-only with no damaged parts, or relabel it to the deflect
// pack when armor zeroed the damage. We prefix ApplyMeleeDamageToTarget, so a parry rolls only
// on blows that already beat both the hit and dodge rolls, and negates the WHOLE swing (main hit
// plus extraMeleeDamages) at its single choke point, pre-armor.
//
// THE SENTINEL RESULT. On a parry we skip the original and hand back a DamageResult with
// stunned=true and no parts: that is precisely the shape TryCastShot's cleanup branch deletes
// its own pre-created "hit" entry for, so no false hit line survives. We then log our own entry
// through the PUBLIC CreateCombatLog with the UMW_Combat_Parry rule pack (ignoring the maneuver
// argument — see that def's header for why the pack avoids maneuver-owned grammar symbols).
// TryCastShot still returns true and the hit sound still plays; both match vanilla's own
// armor-deflect outcome, whose audio layers the deflect ping OVER the impact sound — we trigger
// the same Deflect_Metal effecter, plus a "Parried" text mote in dodge's style.
//
// GATES mirror GetDodgeChance/IsTargetImmobile exactly (surprise attack, downed/non-standing,
// aiming or firing a ranged verb), so parry is never better-behaved than dodge; the wielder gate
// is CompUniqueWeapon on the defender's primary, so natural attacks and other weapons no-op.
// Our on-hit postfix (Verb_MeleeAttackDamage_OnHitTraits_Patch) sees the sentinel's
// wounded=false and correctly skips attacker trait procs on a parried swing.
//
// DRIFT WATCH: the sentinel relies on TryCastShot's "stunned && parts.NullOrEmpty() → remove
// entry" cleanup; if a vanilla update reshapes that branch, revisit this file with it.
[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
public static class Verb_MeleeAttackDamage_Parry_Patch
{
    // surpriseAttack is protected on Verse.Verb (set by TryStartCastOn); vanilla zeroes both the
    // miss and dodge rolls for surprise attacks and a parry must not outclass them.
    private static readonly AccessTools.FieldRef<Verb, bool> SurpriseAttackRef =
        AccessTools.FieldRefAccess<Verb, bool>("surpriseAttack");

    public static bool Prefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target,
        ref DamageWorker.DamageResult __result)
    {
        if (!(target.Thing is Pawn victim) || victim.Dead || !victim.Spawned)
        {
            return true;
        }
        // Vanilla's dodge gates, mirrored: no parry when surprised, downed or off their feet
        // (IsTargetImmobile), or busy with a non-melee (ranged) verb.
        if (SurpriseAttackRef(__instance)
            || victim.Downed
            || victim.GetPosture() != PawnPosture.Standing)
        {
            return true;
        }
        if (victim.stances?.curStance is Stance_Busy busy
            && busy.verb?.verbProps.IsMeleeAttack == false)
        {
            return true;
        }

        CompUniqueWeapon comp = victim.equipment?.Primary?.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return true;
        }
        // Highest chance wins rather than stacking: guard-family traits are exclusion-tagged to
        // one per weapon, so this only matters if a third-party trait adds a second extension.
        float parryChance = 0f;
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            var ext = traits[i].GetModExtension<MeleeParryExtension>();
            if (ext != null)
            {
                parryChance = Mathf.Max(parryChance, ext.parryChance);
            }
        }
        if (parryChance <= 0f || !Rand.Chance(parryChance))
        {
            return true;
        }

        // Parried. Sentinel result: stun-only + no parts makes TryCastShot delete its "hit" log
        // entry; wounded=false keeps every downstream consumer (slave suppression, our on-hit
        // trait procs) correctly inert.
        __result = new DamageWorker.DamageResult { stunned = true };
        __instance.CreateCombatLog(_ => UMW_DefOf.UMW_Combat_Parry, alwaysShow: false);

        // Vanilla armor-deflect presentation: the metal ping layered over the already-chosen hit
        // sound, plus a text mote in the dodge mote's style.
        TargetInfo victimInfo = new TargetInfo(victim.Position, victim.Map);
        Pawn attacker = __instance.CasterPawn;
        Effecter effecter = EffecterDefOf.Deflect_Metal.Spawn();
        effecter.Trigger(victimInfo, attacker != null ? (TargetInfo)attacker : victimInfo);
        effecter.Cleanup();
        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "UMW_TextMote_Parried".Translate(), 1.9f);
        return false;
    }
}

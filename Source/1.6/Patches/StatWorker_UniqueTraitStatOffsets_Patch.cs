using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Routes WeaponTraitDef.equippedStatOffsets to the wielder for OUR unique weapons, so a trait can
// modify a wielder stat with the weapon itself named in the stat breakdown — no equipped hediff.
//
// Why a patch is needed (full 1.6 decompile audit, 2026-07-27): equippedStatOffsets is consumed in
// exactly two places in the assembly — StatWorker.StatOffsetFromGear (value + the "Relevant gear"
// breakdown line, which routes through it via InfoTextLineFromGear) and the relevance filter
// StatWorker.GearHasCompsThatAffectStat — and both are hardcoded to TryGetComp<CompBladelinkWeapon>,
// so the field is silently inert on CompUniqueWeapon. Postfixing those same two methods with a
// mirrored CompUniqueWeapon scan is the whole feature: the value pipeline (GetValueUnfinalized sums
// StatOffsetFromGear for the primary weapon) and the display pipeline both flow through here, so the
// offset shows as "    <weapon's unique name>: -1.0" under "Relevant gear" for free.
//
// Semantics to keep straight when authoring offsets:
// - Offsets ONLY. Gear can never contribute a stat factor (there is no factor pipe); a wielder
//   stat *factor* still needs an equipped hediff (vanilla's WeaponTraitWorker applies
//   equippedHediffs via CompUniqueWeapon — currently unused by our defs, kept as the factor escape
//   hatch).
// - RAW pre-curve units, same as hediff/trait offsets: all offsets are summed in
//   StatWorker.GetValueUnfinalized and FinalizeValue applies the stat's postProcessCurve after.
//   StatDef.finalizeEquippedStatOffset only selects the DISPLAY style, never the math (exhaustive
//   decompile grep) — MeleeHitChance ships finalizeEquippedStatOffset=false, so the gear line
//   renders the raw offset ("-1.0"); a stat that leaves it true would display the raw value
//   percent-styled, so check the flag before putting a new stat through this pipe.
// - Scoped to UniqueWeaponDefs.IsOurs so another mod's CompUniqueWeapon items keep vanilla-inert
//   behaviour for a field they may have authored assuming inertness.
// - StatOffsetFromGear's trailing stat.parts transform (StatPart_Age on MeleeHitChance) is not
//   replayed for our contribution: those parts transform pawn requests and no-op on a gear
//   StatRequest, which is all they ever receive here.
public static class StatWorker_UniqueTraitStatOffsets
{
    // Summed trait offsets for stat on one of our unique weapons; 0 when gear isn't ours.
    internal static float TraitOffset(Thing gear, StatDef stat)
    {
        if (!UniqueWeaponDefs.IsOurs(gear.def))
        {
            return 0f;
        }
        CompUniqueWeapon comp = gear.TryGetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return 0f;
        }
        float val = 0f;
        List<WeaponTraitDef> traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            val += traits[i].equippedStatOffsets.GetStatOffsetFromList(stat);
        }
        return val;
    }
}

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear))]
public static class StatWorker_StatOffsetFromGear_Patch
{
    public static void Postfix(ref float __result, Thing gear, StatDef stat)
    {
        __result += StatWorker_UniqueTraitStatOffsets.TraitOffset(gear, stat);
    }
}

// Private static relevance filter: decides whether the primary weapon earns a "Relevant gear" line
// (RelevantGear/GetExplanationUnfinalized) when its ThingDef-level equippedStatOffsets don't
// already qualify it.
[HarmonyPatch(typeof(StatWorker), "GearHasCompsThatAffectStat")]
public static class StatWorker_GearHasCompsThatAffectStat_Patch
{
    public static void Postfix(ref bool __result, Thing gear, StatDef stat)
    {
        if (!__result)
        {
            __result = StatWorker_UniqueTraitStatOffsets.TraitOffset(gear, stat) != 0f;
        }
    }
}

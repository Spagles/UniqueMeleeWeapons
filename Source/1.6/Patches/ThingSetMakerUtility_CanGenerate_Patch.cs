using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Setting-gated (the per-weapon toggles, all on by default): keeps a weapon the player switched off out
// of every generation and reward pool.
//
// ThingSetMakerUtility.CanGenerate is the single choke point for that. Every ThingSetMaker — ours
// (UMW_Reward_UniqueWeapon, which the warband quest rolls from), the repointed vanilla unique-weapon
// consumers, and the tag-based count makers behind ancient crates, fishing and map-gen loot — reaches its
// candidate set through ThingSetMakerUtility.GetAllowedThingDefs, whose final Where clause calls
// CanGenerate on each def. Filtering here therefore also keeps each maker's count/value/mass
// pre-estimates consistent with what it can actually pick, and a maker that ends up with nothing left
// reports it cannot generate rather than producing a broken set.
//
// This is the pool gate ONLY. It deliberately does not remove the def from the DefDatabase or from
// trade/storage/debug: a disabled weapon still exists, so a save that already contains one keeps it
// working, and re-enabling is a settings flip rather than a migration.
//
// The one place eligibility is cached instead of asked live (ThingSetMakerUtility.allGeneratableItems,
// built once at play-data load) is refreshed by settings.ApplyWeaponAvailability when the window closes;
// see the comment there for why nothing else needs invalidating.
//
// Null-conditional on Settings because CanGenerate runs during def loading, before GetSettings has
// necessarily returned — no settings yet means nothing is disabled yet.
[HarmonyPatch(typeof(ThingSetMakerUtility), nameof(ThingSetMakerUtility.CanGenerate))]
public static class ThingSetMakerUtility_CanGenerate_Patch
{
    public static void Postfix(ThingDef thingDef, ref bool __result)
    {
        if (__result && UniqueMeleeWeaponsMod.Settings?.IsWeaponDisabled(thingDef) == true)
        {
            __result = false;
        }
    }
}

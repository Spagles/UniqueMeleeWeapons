using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons.Patches;

// Setting-gated (excludeWoodStuff, default on): drops Woody-category stuffs from the
// material roll when one of OUR unique weapons is generated, so a hard-won reward
// never lands as a wooden longsword.
//
// GenStuff.AllowedStuffsFor is the single choke point every generation path funnels
// through: the market-value makers (our UMW_Reward_UniqueWeapon pool — also the
// warband quest's — and the repointed vanilla unique-reward consumers) reach it via
// ThingSetMakerByTotalStatUtility → GenStuff.TryRandomStuffFor, and the tag-based
// count makers (ancient crates, fishing, map-gen loot) via
// ThingSetMakerUtility.TryGetRandomThingWhichCanWeighNoMoreThan. Filtering here also
// keeps those makers' mass/value pre-estimates consistent with the actual pick.
//
// Our weapons cannot be crafted or built (recipeMaker is nulled), so the filter can
// never block a player action, and the def gate (thingSetMakerTags) keeps every other
// AllowedStuffsFor consumer — construction/bill material pickers etc. — untouched.
// If filtering would leave no material at all, the unfiltered list is returned:
// never break generation.
[HarmonyPatch(typeof(GenStuff), nameof(GenStuff.AllowedStuffsFor))]
public static class GenStuff_ExcludeWoodStuff_Patch
{
    public static IEnumerable<ThingDef> Postfix(IEnumerable<ThingDef> stuffs, BuildableDef td)
    {
        if (UniqueMeleeWeaponsMod.Settings?.excludeWoodStuff != true || !IsOurUniqueWeapon(td))
        {
            return stuffs;
        }
        List<ThingDef> all = stuffs.ToList();
        List<ThingDef> kept = all.FindAll(s => !IsWoody(s));
        return kept.Count > 0 ? kept : all;
    }

    private static bool IsOurUniqueWeapon(BuildableDef td)
    {
        return td is ThingDef def
            && def.thingSetMakerTags?.Contains(ThingSetMaker_UMWUnique.OurTag) == true;
    }

    // Category-based (not defName == WoodLog) so modded woods are excluded too.
    private static bool IsWoody(ThingDef stuff)
    {
        return stuff.stuffProps?.categories?.Contains(StuffCategoryDefOf.Woody) == true;
    }
}

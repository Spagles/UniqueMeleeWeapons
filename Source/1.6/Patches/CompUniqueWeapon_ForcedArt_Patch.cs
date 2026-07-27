using HarmonyLib;
using RimWorld;

namespace UniqueMeleeWeapons.Patches;

// When a ForcedArtExtension trait lands on a weapon that already exists in the world — UWU's
// customization bench, dev tools, any mod — ensure the weapon has art, colony-tale preferred
// (ForcedArtUtility.EnsureArt). Vanilla CompUniqueWeapon.AddTrait is the one mutation point they
// all funnel through (UWU's WeaponModificationUtility.AddTrait delegates to it), so patching here
// needs no cross-mod reference and covers callers that don't exist yet.
//
// The SpawnedOrAnyParentSpawned guard is load-bearing, not an optimization: InitializeTraits ALSO
// adds its generation roll through AddTrait (decompile-verified), before PostPostMake reaches
// SetQuality -> InitializeArt. A quest reward generated mid-game runs with ProgramState.Playing,
// so without the guard EnsureArt would attach a colony tale to an outsider weapon — and the title
// it sets would make the vanilla InitializeArt that follows no-op (it early-outs on a non-empty
// title), permanently sticking the colony's deeds onto a reward that was never near the colony.
// During ThingMaker.MakeThing the thing has no map and no holder; at UWU's bench it is spawned.
// The guard is also the semantics: a colony tale only when the colony does the inscribing.
[HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.AddTrait))]
public static class CompUniqueWeapon_ForcedArt_Patch
{
    public static void Postfix(CompUniqueWeapon __instance, WeaponTraitDef traitDef)
    {
        if (traitDef.HasModExtension<ForcedArtExtension>()
            && __instance.parent.SpawnedOrAnyParentSpawned)
        {
            ForcedArtUtility.EnsureArt(__instance.parent);
        }
    }
}

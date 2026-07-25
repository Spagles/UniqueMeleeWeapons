using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Drop-in replacement for Odyssey's ThingSetMaker_UniqueWeapon, swapped in (via XPath, see
// Patches/RepointUniqueWeaponPool.xml) on the two vanilla consumers that roll a random unique weapon
// by market value: Reward_UniqueWeapon (the AncientMercenaries quest reward) and MapGen_OrbitalItemStash.
// The stock class is wrong for us on two counts:
//
//   1. It calls ThingMaker.MakeThing(def) with no stuff. Our melee uniques ARE stuffable, so that
//      logs a red "madeFromStuff but stuff=null" error every roll and forces them to plain steel.
//   2. It rolls ANY def with CompUniqueWeapon, so our melee weapons dilute Odyssey's ranged-unique pool.
//
// Subclassing ThingSetMaker_MarketValue fixes (1) for free: it builds via ThingStuffPairWithQuality,
// which passes a stuff (no error) AND preserves the comp's Super quality (it has an explicit
// CompUniqueWeapon branch). We fix (2) by overriding the single candidate hook to keep vanilla's
// open-ended comp criterion MINUS our weapons. Keeping it COMP-based (not tag-based) is deliberate:
// third-party mods' unique weapons stay in the pool even if they never carry the UniqueWeapon tag
// (vanilla never required it) — only ours are removed. All three of MarketValue's selection paths
// (CanGenerateSub / Generate / debug) route through AllowedThingDefs, so this one override controls
// the whole pool without reimplementing any value/stuff/quality logic.
//
// LIMITATION (accepted): this only bites where the maker def is repointed to this class (the two known
// consumers). A future mod that uses the raw ThingSetMaker_UniqueWeapon class in a NEW def would still
// hit problem (1) on our weapons (non-fatal: logs + forces steel). Covering it would require patching
// the stock class itself (fragile — its candidate filter is an inlined lambda); revisit only if it occurs.
public class ThingSetMaker_UMWUnique : ThingSetMaker_MarketValue
{
    // "Ours" is the UMW_UniqueMelee marker tag; UniqueWeaponDefs owns that constant and the test.
    protected override IEnumerable<ThingDef> AllowedThingDefs(ThingSetMakerParams parms)
    {
        return DefDatabase<ThingDef>.AllDefs
            .Where(d => d.HasComp<CompUniqueWeapon>() && !UniqueWeaponDefs.IsOurs(d));
    }
}

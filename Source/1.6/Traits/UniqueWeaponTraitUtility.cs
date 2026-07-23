using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Shared "does the pawn's primary weapon carry this trait" scan, factored out of
// ThoughtWorker_BloodStainedWeapon so ThoughtWorker_StoriedWeapon (and any future trait-gated
// ThoughtWorker) doesn't repeat it. Trivial by design — no stage routing, no nullification
// handling; those stay the concern of the caller.
public static class UniqueWeaponTraitUtility
{
    // True if p's primary equipped weapon is a unique weapon whose rolled traits include trait.
    public static bool PrimaryWeaponHasTrait(Pawn p, WeaponTraitDef trait)
    {
        ThingWithComps weapon = p.equipment?.Primary;
        if (weapon == null)
        {
            return false;
        }

        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        return comp?.TraitsListForReading.Contains(trait) == true;
    }
}

using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Situational mood for carrying a UMW_Storied unique weapon as primary equipment. Unlike
// ThoughtWorker_BloodStainedWeapon, there's no stage routing and no nullification complexity — a
// weapon with a recorded lineage is simply a source of quiet pride, full stop. The trivial consumer
// of UniqueWeaponTraitUtility.PrimaryWeaponHasTrait.
public class ThoughtWorker_StoriedWeapon : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return UniqueWeaponTraitUtility.PrimaryWeaponHasTrait(p, UMW_DefOf.UMW_Storied)
            ? ThoughtState.ActiveAtStage(0)
            : ThoughtState.Inactive;
    }
}

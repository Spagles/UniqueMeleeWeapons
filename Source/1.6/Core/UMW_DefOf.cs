using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Static handles for this mod's own defs that C# needs to reference by identity. Populated by
// RimWorld at startup (fields match defName). Only add defs the code actually looks up.
[DefOf]
public static class UMW_DefOf
{
    // The blood-stained Melee trait — looked up by ThoughtWorker_BloodStainedWeapon
    // to decide whether the wielder's primary weapon carries it.
    public static WeaponTraitDef UMW_BloodStained;

    // The storied Melee trait — looked up by ThoughtWorker_StoriedWeapon
    // to decide whether the wielder's primary weapon carries it.
    public static WeaponTraitDef UMW_Storied;

    // Extra unique-name grammar built on the weapon's material — injected into the
    // naming request by NameGenerator_StuffAdjective_Patch, never referenced from XML.
    public static RulePackDef UMW_NamerStuffAdjectives;

    static UMW_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_DefOf));
    }
}

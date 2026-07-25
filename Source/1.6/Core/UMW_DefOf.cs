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

    // Piledriver's active ability. Looked up by UniqueMeleeWeaponsSettings.ApplyAbilityTuning, which
    // writes the configured cooldown and radius onto the live def; the XML holds only the shipped
    // default. Nothing else in C# references it.
    public static AbilityDef UMW_Earthshake;

    // Storied's active ability and the timed buff it grants. Same reason as UMW_Earthshake:
    // ApplyAbilityTuning writes the configured cooldown, radius and buff duration onto the live defs.
    public static AbilityDef UMW_RallyingCry;
    public static HediffDef UMW_Rallied;

    // Core's ritual speech-lines fleck, spawned over each ally a rallying cry reaches
    // (CompAbilityEffect_RallyAllies). Vanilla's own, referenced by identity rather than copied — unlike
    // the shout SoundDefs, a FleckDef needs no cloning to be usable. Core-owned, so no MayRequire; it is
    // absent from vanilla's FleckDefOf, which is why the handle lives here.
    public static FleckDef SpeechLines;

    static UMW_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_DefOf));
    }
}

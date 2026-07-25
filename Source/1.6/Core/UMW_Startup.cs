using Verse;

namespace UniqueMeleeWeapons;

// Runs once on the main thread after all defs are loaded, translations injected and DefOf fields
// injected — the earliest point where settings that override def fields can be
// applied. (The Mod constructor is too early: it runs while mod assemblies are
// still loading, before any def exists.)
//
// Re-runs on a play-data reload, which is how a mid-session language change reaches
// TraitEffectSummary: the DefDatabase is rebuilt and the effect lines are re-derived in the new
// language.
[StaticConstructorOnStartup]
public static class UMW_Startup
{
    static UMW_Startup()
    {
        UniqueMeleeWeaponsMod.Settings.ApplyWarbandQuestWeight();
        UniqueMeleeWeaponsMod.Settings.ApplyAbilityTuning();
        TraitEffectSummary.AttachToTraits();
    }
}

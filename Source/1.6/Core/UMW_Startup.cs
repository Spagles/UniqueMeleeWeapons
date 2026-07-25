using Verse;

namespace UniqueMeleeWeapons;

// Runs once on the main thread after all defs are loaded and DefOf fields are
// injected — the earliest point where settings that override def fields can be
// applied. (The Mod constructor is too early: it runs while mod assemblies are
// still loading, before any def exists.)
[StaticConstructorOnStartup]
public static class UMW_Startup
{
    static UMW_Startup()
    {
        UniqueMeleeWeaponsMod.Settings.ApplyWarbandQuestWeight();
        UniqueMeleeWeaponsMod.Settings.ApplyAbilityTuning();
    }
}

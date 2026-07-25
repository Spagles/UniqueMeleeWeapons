using HarmonyLib;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Mod entry point. Wires up settings and applies all Harmony patches at startup.
// Add patch classes under the UniqueMeleeWeapons.Patches namespace; PatchAll
// discovers them automatically via their [HarmonyPatch] attributes.
public class UniqueMeleeWeaponsMod : Mod
{
    public static UniqueMeleeWeaponsSettings Settings { get; private set; }

    public UniqueMeleeWeaponsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<UniqueMeleeWeaponsSettings>();
        var harmony = new Harmony("shunter.uniquemeleeweapons");
        harmony.PatchAll();
        Log.Message($"[Unique Melee Weapons] Initialized with {harmony.GetPatchedMethods().EnumerableCount()} patches.");
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
    }

    // Called when the settings window closes. Settings that override def fields
    // re-apply here so a change takes effect without a restart.
    public override void WriteSettings()
    {
        base.WriteSettings();
        Settings.ApplyWarbandQuestWeight();
        Settings.ApplyAbilityTuning();
    }

    public override string SettingsCategory() => "UMW_SettingsCategory".Translate();
}

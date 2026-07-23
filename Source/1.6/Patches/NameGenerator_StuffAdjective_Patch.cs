using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace UniqueMeleeWeapons.Patches;

// Mixes the weapon's material into unique-name generation ("The Golden Reaper",
// "Plasteel Fang") — unique names hide the stuff an ordinary weapon's label shows.
//
// CompUniqueWeapon.PostPostMake builds a GrammarRequest (weapon_type / color /
// trait_adjective rules + Odyssey's NamerUniqueWeapon rulepack) and resolves it
// through this GenerateName overload with root keyword "r_weapon_name". The comp is
// sealed around that request, but its rule/include lists are plain shared references,
// so a prefix can extend the request without touching vanilla code. Gates:
//   - UniqueMeleeWeapon.StuffBeingNamed is only set while one of OUR weapons runs
//     PostPostMake (vanilla's ranged uniques never set it), and
//   - rootKeyword must be "r_weapon_name", so unrelated name requests that happen
//     to resolve inside that window (e.g. a CompArt title) are untouched.
// The injected stuff_adjective rule prefers the stuff's stuffAdjective ("wooden",
// "golden") and falls back to its label ("plasteel", "jade" — English stuff labels
// read fine adjectivally); UMW_NamerStuffAdjectives supplies the extra patterns
// that weave it into NamerUniqueWeapon's grammar.
[HarmonyPatch(typeof(NameGenerator), nameof(NameGenerator.GenerateName),
    typeof(GrammarRequest), typeof(Predicate<string>), typeof(bool), typeof(string), typeof(string))]
public static class NameGenerator_StuffAdjective_Patch
{
    private const string WeaponNameRoot = "r_weapon_name";

    public static void Prefix(ref GrammarRequest request, string rootKeyword)
    {
        ThingDef stuff = UniqueMeleeWeapon.StuffBeingNamed;
        if (stuff == null || rootKeyword != WeaponNameRoot)
        {
            return;
        }
        string adjective = stuff.stuffProps?.stuffAdjective;
        if (adjective.NullOrEmpty())
        {
            adjective = stuff.label;
        }
        request.Rules.Add(new Rule_String("stuff_adjective", adjective));
        request.Includes.Add(UMW_DefOf.UMW_NamerStuffAdjectives);
    }
}

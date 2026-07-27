using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// The working half of ForcedArtExtension (see there for the case-by-case design): the trait scan
// shared by the two ForcedArt patches, and the colony-tale art initializer the AddTrait one fires.
public static class ForcedArtUtility
{
    // CompArt scribes its tale in a private field (taleRef) and exposes no setter — only Title has
    // one. UWU's art-transfer code reflects the same field, so the name is already a de-facto
    // shared dependency on vanilla; if a vanilla update renames it, EnsureArt degrades to
    // InitializeArt(Colony) below rather than losing the art guarantee.
    private static readonly FieldInfo TaleRefField = AccessTools.Field(typeof(CompArt), "taleRef");

    // True if weapon's rolled traits include one carrying ForcedArtExtension. Plain loop: the
    // CanShowArt postfix runs this on every sub-Excellent art thing a recreation scan touches.
    public static bool HasForcedArtTrait(ThingWithComps weapon)
    {
        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return false;
        }
        List<WeaponTraitDef> traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].HasModExtension<ForcedArtExtension>())
            {
                return true;
            }
        }
        return false;
    }

    // Gives weapon an active art inscription carrying a colony tale, best-effort. No-op when there
    // is no CompArt or art is already Active — a weapon that brought art along keeps it untouched.
    //
    // The tale pick mirrors TaleManager.GetRandomTaleReferenceForArt's selection (usableForArt,
    // weighted by InterestLevel, Notify_NewlyUsed bookkeeping) minus its two tale-less exits: the
    // unconditional one for Outsider context and the 25% roll even in Colony context. Tale-less is
    // kept only as the fallback when the colony genuinely has no usable tales yet — the sentinel is
    // non-null, so the art is Active (title + abstract description) either way.
    public static void EnsureArt(ThingWithComps weapon)
    {
        CompArt art = weapon.GetComp<CompArt>();
        if (art?.Active != false) // no comp, or already has art
        {
            return;
        }
        if (TaleRefField == null)
        {
            // Reflection broken by a vanilla rename: vanilla init still yields art (the CanShowArt
            // postfix lifts the quality bar), just at vanilla's tale odds instead of ours.
            Log.Warning("[Unique Melee Weapons] CompArt.taleRef field not found; falling back to vanilla art initialization.");
            art.InitializeArt(ArtGenerationContext.Colony);
            return;
        }
        TaleReference taleRef = PickColonyTale();
        TaleRefField.SetValue(art, taleRef);
        // Mirrors CompArt.GenerateTitle (protected virtual), which is exactly this expression.
        art.Title = GenText.CapitalizeAsTitle(taleRef.GenerateText(TextGenerationPurpose.ArtName, art.Props.nameMaker));
    }

    private static TaleReference PickColonyTale()
    {
        // EnsureArt's callers only run in a live game (the AddTrait patch requires a spawned
        // weapon), but stay safe if a future caller doesn't.
        if (Current.ProgramState != ProgramState.Playing)
        {
            return TaleReference.Taleless;
        }
        if (!Find.TaleManager.AllTalesListForReading
                .Where(t => t.def.usableForArt)
                .TryRandomElementByWeight(t => t.InterestLevel, out Tale tale))
        {
            return TaleReference.Taleless;
        }
        tale.Notify_NewlyUsed();
        return new TaleReference(tale);
    }
}

using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Def handles the warband quest's root node looks up by identity. Our own defs (UMW_*) plus the two
// vanilla defs we reference directly. AbandonedColonyTribal is Odyssey-gated; the mod requires Odyssey,
// but MayRequire keeps DefOf init from erroring in the degenerate case it is somehow absent.
[DefOf]
public static class UMW_QuestDefOf
{
    // Our melee-only reward pool (Defs/ThingSetMakerDefs/UMW_Reward_UniqueWeapon.xml).
    public static ThingSetMakerDef UMW_Reward_UniqueWeapon;

    // Our warband quest — its rootSelectionWeight is overwritten by the
    // warband-commonality mod setting (at startup and on settings close).
    public static QuestScriptDef UMW_OpportunitySite_Warband;

    // The temporary hidden faction the warband belongs to (Defs/FactionDefs/Warband.xml).
    public static FactionDef UMW_Warband;

    // The camp site part (Defs/SitePartDefs/WarbandCamp.xml).
    public static SitePartDef UMW_WarbandCamp;

    // Vanilla tribal melee chief, reused as the warband leader (stripped + given the rolled unique).
    public static PawnKindDef Tribal_ChiefMelee;

    // Odyssey tile mutator: themes the site as a ruined, abandoned, pawn-less tribal settlement and
    // stores its footprint in the "SettlementRect" map var.
    public static TileMutatorDef AbandonedColonyTribal;

    static UMW_QuestDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(UMW_QuestDefOf));
    }
}

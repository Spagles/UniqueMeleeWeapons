using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Situational mood for carrying a UMW_BloodStained unique weapon as primary equipment. Backs the
// single two-stage thought UMW_BloodStainedWeapon: stage 0 is the -2 penalty, stage 1 the +3
// bloodlust relish. The worker reports the situation (primary weapon carries the trait) and routes
// exactly ONE personality fork — Bloodlust → stage 1 — the vanilla stage-routing idiom
// (ThoughtWorker_Pretty/Ugly route stages by beauty; Anomaly's UnnaturalDarkness routes between
// opposite-sign stages). Bloodlust must be routed here rather than listed in nullifyingTraits,
// because ThoughtUtility.ThoughtNullified zeroes the whole DEF, relish stage included.
//
// Every other personality exemption stays declarative on the def, which the pipeline honors for
// situational thoughts (verified by decompile): the hardened exemptions (Psychopath / VTE
// Desensitized / World-weary, and the Biotech hemogenic gene) ride nullifyingTraits/nullifyingGenes —
// Thought.MoodOffset returns 0 when ThoughtUtility.ThoughtNullified is true. Nullified situational
// thoughts still render as a grey "0" row (only MEMORY thoughts are dropped at MoodOffset()==0, in
// ThoughtHandler.GetAllMoodThoughts), which is vanilla-standard — a psychopath sees the same for
// ColonistLeftUnburied. That grey-0 row is also why the buff can't be a separate requiredTraits def:
// a bloodlust pawn would see it beside the nullified penalty row as a duplicate moodlet.
public class ThoughtWorker_BloodStainedWeapon : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        ThingWithComps weapon = p.equipment?.Primary;
        if (weapon == null)
        {
            return ThoughtState.Inactive;
        }

        CompUniqueWeapon comp = weapon.GetComp<CompUniqueWeapon>();
        if (comp?.TraitsListForReading.Contains(UMW_DefOf.UMW_BloodStained) != true)
        {
            return ThoughtState.Inactive;
        }

        bool relishes = p.story?.traits?.HasTrait(TraitDefOf.Bloodlust) == true;
        return ThoughtState.ActiveAtStage(relishes ? 1 : 0);
    }
}

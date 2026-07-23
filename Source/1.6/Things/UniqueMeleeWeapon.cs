using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Thing class for stuffable unique melee weapons. It combines two independent
// recolours in a single texture via the CutoutComplex shader:
//   - Colour one (DrawColor, mask red) — the unique accent
//     colour. Supplied unchanged by vanilla CompUniqueWeapon.ForceColor() (a randomly
//     picked weapon ColorDef, or a trait's forcedColor such as gold/jade).
//   - Colour two (DrawColorTwo, mask green) — the stuff/material
//     tint. Vanilla leaves this at the def's colorTwo (white); we redirect it to the stuff
//     colour so the blade tints like any ordinary smithed weapon.
// The mask drives the two colours per pixel, so one diffuse renders both the material tint and
// the unique accent at once — the trick the whole mod relies on. The mask must contain no
// black over the weapon silhouette: black means "not painted" and would show the raw,
// un-tinted diffuse (a black blade would ignore its material entirely).
public class UniqueMeleeWeapon : ThingWithComps
{
    // The stuff of the weapon currently running PostPostMake (trait roll + naming
    // inside CompUniqueWeapon), exposed for NameGenerator_StuffAdjective_Patch to
    // inject material adjectives into the name grammar. Thing generation is
    // single-threaded, so one static slot (cleared in finally) is safe.
    public static ThingDef StuffBeingNamed { get; private set; }

    public override void PostPostMake()
    {
        StuffBeingNamed = Stuff;
        try
        {
            base.PostPostMake();
        }
        finally
        {
            StuffBeingNamed = null;
        }
    }

    // The unique name hides the material that an ordinary weapon's label shows
    // ("plasteel longsword" → "The Grim Reaper"), so surface it in the inspect
    // pane instead. Reuses the info card's own "Stuff" stat label (Stat_Stuff_Name)
    // — matching the term the stats card shows players — so it's already translated.
    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (Stuff != null)
        {
            string line = "Stat_Stuff_Name".Translate() + ": " + Stuff.label;
            text = text.NullOrEmpty() ? line : text + "\n" + line;
        }
        return text;
    }

    // Colour one (the red-masked accent) is left to the base implementation, which returns
    // the first comp's ForceColor() — i.e. CompUniqueWeapon's unique colour.
    // Colour two (the green-masked body) defaults to the stuff/material tint, but a trait may
    // override it via ForcedColorTwoExtension — the colour-two analogue of vanilla's
    // colour-one forcedColor. A forced body colour replaces the material tint
    // (there is no third mask channel); first such trait wins.
    public override Color DrawColorTwo
    {
        get
        {
            ColorDef forced = ForcedBodyColor();
            if (forced != null)
            {
                return forced.color;
            }
            if (Stuff != null)
            {
                return def.GetColorForStuff(Stuff);
            }
            return base.DrawColorTwo;
        }
    }

    // The body (colour two) override from the first equipped trait carrying a
    // ForcedColorTwoExtension, or null for the default material tint.
    private ColorDef ForcedBodyColor()
    {
        var comp = this.TryGetComp<CompUniqueWeapon>();
        if (comp == null)
        {
            return null;
        }
        var traits = comp.TraitsListForReading;
        for (int i = 0; i < traits.Count; i++)
        {
            ColorDef color = traits[i].GetModExtension<ForcedColorTwoExtension>()?.color;
            if (color != null)
            {
                return color;
            }
        }
        return null;
    }
}

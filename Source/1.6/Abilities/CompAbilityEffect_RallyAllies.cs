using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Backs UMW_RallyingCry, the active half of the Storied trait (Melee). Core's DLC-free
// CompAbilityEffect_* roster has no ally-AoE-mood shape — every stock effect either targets a single
// pawn/cell or is hostile-only (verified per spec) — hence this custom comp. The spec locks the
// target set to player-faction humanlikes only: a rallying cry from a storied heirloom weapon should
// never buff a raid or an animal.
public class CompProperties_AbilityRallyAllies : CompProperties_AbilityEffect
{
    // Cells around the caster scanned for ralliable allies (an EMPPulse-style AoE footprint —
    // cf. CompProperties_AbilityExplosion.explosionRadius).
    public float radius = 9.9f;

    // Memory thought granted to every pawn the cry reaches (UMW_Rallied).
    public ThoughtDef thoughtDef;

    public CompProperties_AbilityRallyAllies()
    {
        compClass = typeof(CompAbilityEffect_RallyAllies);
    }
}

// On cast: every player-faction humanlike (colonists + slaves — both carry Faction == Faction.OfPlayer,
// prisoners/guests don't) spawned on the caster's map, within radius and with line of sight to the
// caster, gains thoughtDef as a memory. The caster is within radius of their own position, so they
// rally too.
public class CompAbilityEffect_RallyAllies : CompAbilityEffect
{
    private new CompProperties_AbilityRallyAllies Props => (CompProperties_AbilityRallyAllies)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Pawn caster = parent.pawn;
        Map map = caster?.Map;
        if (map == null || Props.thoughtDef == null)
        {
            return;
        }

        List<Pawn> allies = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        for (int i = 0; i < allies.Count; i++)
        {
            Pawn ally = allies[i];
            if (!ally.RaceProps.Humanlike)
            {
                continue;
            }
            if (!ally.Position.InHorDistOf(caster.Position, Props.radius))
            {
                continue;
            }
            if (!GenSight.LineOfSight(caster.Position, ally.Position, map))
            {
                continue;
            }
            ally.needs?.mood?.thoughts?.memories?.TryGainMemory(Props.thoughtDef);
        }
    }
}

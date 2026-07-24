using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UniqueMeleeWeapons;

// Backs UMW_RallyingCry, the active half of the Storied trait (Melee). Core's DLC-free
// CompAbilityEffect_* roster has no ally-AoE-buff shape — every stock effect either targets a single
// pawn/cell or is hostile-only (verified per spec) — hence this custom comp. Faction-SYMMETRIC by
// design (user directive 2026-07-24, superseding the spec's player-faction-only target set): the cry
// rallies the CASTER's faction, whoever that is, so a hostile wielder rallies its own raid. That is
// also why the payload is a timed hediff (pain dampening) rather than the original mood memory —
// stat effects are real for NPCs, mood is not.
public class CompProperties_AbilityRallyAllies : CompProperties_AbilityEffect
{
    // Cells around the caster scanned for ralliable allies (an EMPPulse-style AoE footprint —
    // cf. CompProperties_AbilityExplosion.explosionRadius).
    public float radius = 9.9f;

    // Timed hediff granted to every pawn the cry reaches (UMW_Rallied).
    public HediffDef hediffDef;

    public CompProperties_AbilityRallyAllies()
    {
        compClass = typeof(CompAbilityEffect_RallyAllies);
    }
}

// On cast: every humanlike of the caster's faction (animals and mechs take no heart from a speech)
// spawned on the caster's map, within radius and with line of sight to the caster, gains hediffDef.
// The caster is within radius of their own position, so they rally too. A factionless caster rallies
// no one.
public class CompAbilityEffect_RallyAllies : CompAbilityEffect
{
    private new CompProperties_AbilityRallyAllies Props => (CompProperties_AbilityRallyAllies)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Pawn caster = parent.pawn;
        Map map = caster?.Map;
        if (map == null || caster.Faction == null || Props.hediffDef == null)
        {
            return;
        }

        List<Pawn> allies = map.mapPawns.SpawnedPawnsInFaction(caster.Faction);
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
            ally.health?.AddHediff(Props.hediffDef);
        }
    }
}

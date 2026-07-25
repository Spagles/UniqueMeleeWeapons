using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UniqueMeleeWeapons;

// Backs UMW_Earthshake (Defs/AbilityDefs/Earthshake.xml). Vanilla's CompProperties_AbilityExplosion is
// the right shape for a self-centred kinetic burst and we keep every one of its fields, but three of
// the arguments it forwards to GenExplosion.DoExplosion are HARDCODED in
// CompAbilityEffect_Explosion.Apply with no XML hook (decompile-verified 2026-07-25, RimWorld 1.6):
//
//   ignoredThings:  always null    -> the caster is stunned by their own slam
//   overrideCells:  always null    -> cells come from DamageWorker.ExplosionCellsToHit, which filters
//                                    on GenSight.LineOfSight(center, cell, skipFirstCell: true), so a
//                                    wall stops the shock
//   propagationSpeed: taken from damageDef.expolosionPropagationSpeed (sic, vanilla's typo), not from
//                                    the comp -> tuning the roll would mean cloning Core's Stun DamageDef
//
// So this is CompProperties_AbilityExplosion plus exactly those three knobs. Subclassing rather than
// reimplementing keeps every other explosion field (spawn things, gas, fire chance, screen shake, VFX)
// working as vanilla documents it, and inherits any non-Apply behaviour a future RimWorld adds to the
// stock comp.
public class CompProperties_AbilityGroundShockwave : CompProperties_AbilityExplosion
{
    // Cells/second the shock travels outward. Explosion.GetCellAffectTick is
    // startTick + (int)(distance * 1.5 / propagationSpeed), so LOWER is slower and more readable; the
    // outermost cell of a radius-R burst lands at ceil(R_maxCell * 1.5 / speed) ticks. -1 keeps
    // vanilla's behaviour of reading damageDef.expolosionPropagationSpeed (which is 1 for anything that
    // doesn't override it, i.e. ~1.5 ticks per cell - technically progressive, too fast to see).
    // Sentinel matches the -1 idiom CompProperties_AbilityExplosion already uses for damageAmount and
    // armorPenetration.
    public float propagationSpeed = -1f;

    // Spare the wielder from their own blast. Implemented as GenExplosion's ignoredThings, NOT as its
    // excludeRadius: excludeRadius would skip the centre CELL outright, losing the dust puff at the
    // wielder's feet (where the slam visibly happens) and sparing anyone standing on top of them.
    // ignoredThings skips TakeDamage on the listed things only - the cell is still affected, so the
    // per-cell fleck, spawns and fire chance all still fire there.
    public bool excludeCaster = true;

    // Carry the shock through the ground rather than the air, which is two changes to vanilla's cell
    // selection and they only make sense together (see GroundConnectedCells):
    //   • a wall does NOT stop it. Vanilla's DamageWorker.ExplosionCellsToHit filters every cell on
    //     GenSight.LineOfSight from the origin, so without this the burst was smaller than the
    //     line-of-sight-free ring drawn for it. NOTE dropping that filter is a real power increase, not
    //     just cosmetic.
    //   • a GAP does. The set is flood-filled outward from the caster, so the shock cannot cross cells
    //     with no ground to carry it - Odyssey's Space terrain, i.e. the void between sections of an
    //     orbital platform or a gravship exterior.
    // Fed to GenExplosion as overrideCells, which Explosion.StartExplosion uses verbatim when non-empty;
    // everything downstream (the distance sort, the progressive Tick, the per-cell fleck, ignoredThings)
    // is identical either way. Default false so the comp behaves exactly like vanilla unless opted in.
    public bool travelsThroughGround;

    public CompProperties_AbilityGroundShockwave()
    {
        compClass = typeof(CompAbilityEffect_GroundShockwave);
    }
}

public class CompAbilityEffect_GroundShockwave : CompAbilityEffect_Explosion
{
    private new CompProperties_AbilityGroundShockwave Props => (CompProperties_AbilityGroundShockwave)props;

    // Read by Command_Ability_GroundShockwave so the gizmo preview draws from the same numbers and the
    // same cell set the burst actually uses, rather than a second copy of them that can drift.
    public float Radius => Props.explosionRadius;
    public bool TravelsThroughGround => Props.travelsThroughGround;

    // A faithful copy of CompAbilityEffect_Explosion.Apply with the three arguments above substituted.
    // Every argument is passed by name: the vanilla call is 36 positional arguments deep, and naming them
    // keeps this readable and keeps it compiling if a future RimWorld inserts a parameter.
    //
    // Deliberately does NOT call base.Apply. That would run CompAbilityEffect_Explosion.Apply and set off
    // a second, vanilla-configured explosion. Skipping the grandparent CompAbilityEffect.Apply too is
    // what the stock explosion comp does, so its Props.sound/message/clamorType/screenShakeIntensity are
    // inert on any explosion comp - the explosion carries its own soundExplode and screenShakeFactor
    // instead. Matching that keeps this comp a drop-in for the vanilla one.
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        // target/dest are unread, exactly as in vanilla: this is a self-centred burst, and for a
        // targetRequired=False ability Command_Ability.ProcessInput passes the caster as the target anyway.
        Pawn caster = parent.pawn;
        Map map = caster?.MapHeld;
        if (map == null)
        {
            // Vanilla would hand the null straight to GenExplosion and log "Tried to do explosion in a
            // null map." Returning quietly is the only behavioural difference from stock, and it can only
            // be reached off-map, where the gizmo isn't drawn.
            return;
        }

        int damAmount = Props.damageAmount;
        float armorPenetration = Props.armorPenetration;
        if (damAmount == -1)
        {
            damAmount = Props.damageDef.defaultDamage;
        }
        if (Mathf.Approximately(armorPenetration, -1f))
        {
            armorPenetration = Props.damageDef.defaultArmorPenetration;
        }

        GenExplosion.DoExplosion(
            center: caster.Position,
            map: map,
            radius: Props.explosionRadius,
            damType: Props.damageDef,
            instigator: caster,
            damAmount: damAmount,
            armorPenetration: armorPenetration,
            explosionSound: Props.soundExplode,
            weapon: parent.verb?.EquipmentSource?.def,
            projectile: null,
            intendedTarget: null,
            postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
            postExplosionSpawnChance: Props.postExplosionSpawnChance,
            postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount,
            postExplosionGasType: Props.postExplosionGasType,
            postExplosionGasRadiusOverride: null,
            postExplosionGasAmount: 255,
            applyDamageToExplosionCellsNeighbors: Props.applyDamageToExplosionCellsNeighbors,
            preExplosionSpawnThingDef: Props.preExplosionSpawnThingDef,
            preExplosionSpawnChance: Props.preExplosionSpawnChance,
            preExplosionSpawnThingCount: Props.preExplosionSpawnThingCount,
            chanceToStartFire: Props.explosionChanceToStartFire,
            damageFalloff: Props.explosionDamageFalloff,
            direction: null,
            ignoredThings: Props.excludeCaster ? new List<Thing> { caster } : null,
            affectedAngle: null,
            doVisualEffects: Props.doExplosionVFX,
            propagationSpeed: Props.propagationSpeed > 0f
                ? Props.propagationSpeed
                : Props.damageDef.expolosionPropagationSpeed,
            excludeRadius: 0f,
            doSoundEffects: true,
            postExplosionSpawnThingDefWater: Props.postExplosionSpawnThingDefWater,
            screenShakeFactor: Props.screenShakeFactor,
            flammabilityChanceCurve: null,
            overrideCells: Props.travelsThroughGround
                ? GroundConnectedCellList(caster.Position, map, Props.explosionRadius)
                : null,
            postExplosionSpawnSingleThingDef: Props.postExplosionSpawnSingleThingDef,
            preExplosionSpawnSingleThingDef: Props.preExplosionSpawnSingleThingDef);
    }

    // Per-cast wrapper: GenExplosion keeps the list it is handed (Explosion.overrideCells holds the
    // reference for the explosion's lifetime), so this must not be the shared scratch set below.
    // One allocation per cast of a 12-hour-cooldown ability is not worth optimising away.
    private static List<IntVec3> GroundConnectedCellList(IntVec3 center, Map map, float radius)
    {
        HashSet<IntVec3> cells = new HashSet<IntVec3>();
        GroundConnectedCells(center, map, radius, cells);
        return new List<IntVec3>(cells);
    }

    // Does this cell have ground to carry the shock? Odyssey's `Space` terrain is the ONLY terrain in
    // the game that sets exposesToVacuum, so this is the one field that covers both motivating cases -
    // the void between orbital-platform sections and a gravship exterior - and it is false for every
    // terrain on an ordinary map, which is what makes GroundConnectedCells a no-op off space maps.
    //
    // Deliberately NOT TerrainDef.IsSubstructure, which looks like the obvious test and is wrong: only
    // gravship decking carries the Substructure tag, so OrbitalPlatform and MechanoidPlatform - solid
    // floor a player walks on - would read as gaps. GridsUtility.GetTerrain never returns null (it falls
    // back to Soil), so no guard is needed here.
    public static bool ConductsShock(IntVec3 cell, Map map)
    {
        return !cell.GetTerrain(map).exposesToVacuum;
    }

    // Fills `into` with the cells the shock reaches: the euclidean disc of `radius`, flood-filled
    // outward from `center` through cells that conduct. Two properties worth preserving if this is ever
    // touched:
    //   • On a map with no vacuum terrain the result is cell-for-cell IDENTICAL to the plain radial disc,
    //     because the candidate set is built from the same GenRadial walk vanilla uses and the fill then
    //     admits all of it. So this changes nothing outside space maps.
    //   • The fill is CARDINAL-only, matching Verse.FloodFiller and GasGrid diffusion, so the shock
    //     cannot squeeze diagonally between two void cells that touch only at a corner.
    // Hand-rolled rather than map.floodFiller because that is a shared per-map singleton with a
    // reentrancy guard that logs an error on nesting, and it has no radius bound - a pure connectivity
    // fill would follow an open deck well past `radius`. Scratch collections are static and cleared on
    // entry: both callers are main-thread and neither re-enters, which is the same pattern vanilla uses
    // for DamageWorker.openCells and GenDraw.ringDrawCells.
    public static void GroundConnectedCells(IntVec3 center, Map map, float radius, HashSet<IntVec3> into)
    {
        into.Clear();
        if (map == null)
        {
            return;
        }

        candidates.Clear();
        int numCells = GenRadial.NumCellsInRadius(radius);
        for (int i = 0; i < numCells; i++)
        {
            IntVec3 cell = center + GenRadial.RadialPattern[i];
            if (cell.InBounds(map))
            {
                candidates.Add(cell);
            }
        }

        // The origin is always struck, conducting or not: that is where the weapon hits, so the slam,
        // its sound and its dust belong there even in the degenerate case of a caster over open space.
        frontier.Clear();
        into.Add(center);
        frontier.Enqueue(center);
        while (frontier.Count > 0)
        {
            IntVec3 cell = frontier.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                IntVec3 neighbour = cell + GenAdj.CardinalDirections[i];
                if (into.Contains(neighbour) || !candidates.Contains(neighbour) || !ConductsShock(neighbour, map))
                {
                    continue;
                }
                into.Add(neighbour);
                frontier.Enqueue(neighbour);
            }
        }
    }

    private static readonly HashSet<IntVec3> candidates = new HashSet<IntVec3>();
    private static readonly Queue<IntVec3> frontier = new Queue<IntVec3>();
}

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

    // Feed GenExplosion a pre-built radial cell set instead of letting the DamageWorker build one, which
    // is the only way to drop vanilla's line-of-sight filter (Explosion.StartExplosion uses overrideCells
    // verbatim when non-empty, and everything downstream - the distance sort, the progressive Tick, the
    // per-cell fleck, ignoredThings - is identical either way). For a shock that travels through the
    // ground rather than the air. NOTE this is a real power increase, not just cosmetic: the burst now
    // reaches through walls. Default false so the comp behaves exactly like vanilla unless opted in.
    public bool ignoreLineOfSight;

    public CompProperties_AbilityGroundShockwave()
    {
        compClass = typeof(CompAbilityEffect_GroundShockwave);
    }
}

public class CompAbilityEffect_GroundShockwave : CompAbilityEffect_Explosion
{
    private new CompProperties_AbilityGroundShockwave Props => (CompProperties_AbilityGroundShockwave)props;

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
            overrideCells: Props.ignoreLineOfSight
                ? RadialCells(caster.Position, map, Props.explosionRadius)
                : null,
            postExplosionSpawnSingleThingDef: Props.postExplosionSpawnSingleThingDef,
            preExplosionSpawnSingleThingDef: Props.preExplosionSpawnSingleThingDef);
    }

    // Every in-bounds cell within radius, unfiltered. This is the same GenRadial walk
    // DamageWorker.ExplosionCellsToHit opens with (a circle: GenRadial.RadialPattern is sorted by true
    // euclidean distance and NumCellsInRadius admits distance <= radius), minus its line-of-sight test
    // and minus its adjWallCells pass - the latter is unnecessary here because taking the whole radial
    // set already includes the wall cells that pass would have added back.
    private static List<IntVec3> RadialCells(IntVec3 center, Map map, float radius)
    {
        int numCells = GenRadial.NumCellsInRadius(radius);
        List<IntVec3> cells = new List<IntVec3>(numCells);
        for (int i = 0; i < numCells; i++)
        {
            IntVec3 cell = center + GenRadial.RadialPattern[i];
            if (cell.InBounds(map))
            {
                cells.Add(cell);
            }
        }
        return cells;
    }
}

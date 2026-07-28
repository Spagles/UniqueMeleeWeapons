using Verse;

namespace UniqueMeleeWeapons;

// Defender-side parry carried by a weapon trait (UMW_Quilloned): while the weapon is wielded,
// each incoming melee blow that would land has parryChance to be caught on the guard and negated
// outright — a first-class combat outcome with its own battle-log line (UMW_Combat_Parry), the
// vanilla metal-deflect effecter and a "Parried" text mote, resolved by
// Patches/Verb_MeleeAttackDamage_Parry_Patch.cs (gates, sequencing and the log mechanics live
// in that file's header).
//
// WHY A MECHANIC AND NOT A STAT. Deliberately NOT an equipped hediff offsetting MeleeDodgeChance
// (+3 raw): that vehicle is bladelink's idiom — a psychic bond writing a condition onto the pawn —
// and reads wrong for plain steel quillons; it also surfaces in the health tab as a pseudo-
// condition. A parry roll IS the physical feature: the blow is caught on the weapon, visibly and
// audibly, and nothing is ever applied to the pawn. No vanilla precedent binds us here either —
// Odyssey unique traits never touch the wielder at all.
//
// The chance is FLAT — deliberately independent of melee skill, where every dodge-stat source
// decays through MeleeDodgeChance's postProcessCurve. Hardware does the work: a conscript and a
// master benefit alike, which is the point of bolting a guard onto a sword.
public class MeleeParryExtension : DefModExtension
{
    // Chance (0-1) to negate an incoming melee blow that passed the attacker's hit roll and the
    // defender's dodge roll. 0.10 replaces the old +3 raw dodge hediff: net avoidance for a
    // mid-skill wielder ~15% (6% dodge + 10% of the rest) vs ~12% under the hediff, and a real
    // improvement for masters, who got ~nothing from a raw dodge offset. Sized so the trait's
    // MV +50 keeps its QuickReload (ranged-tempo utility) parity anchor.
    public float parryChance = 0.1f;
}

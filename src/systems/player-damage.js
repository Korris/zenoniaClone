// Shared helpers for hero-sourced damage: attack buffs (Battle Cry) and crit rolls (Crit Eye).
// Used by combat-system.js (combo/radial hits) and skill-cast-system.js (skill damage) so both
// paths apply buffs/crits consistently. Pure -> unit-testable.
import { computeDamage } from './damage-formula.js';

/** Current attack power including temporary buffs (e.g. Battle Cry). */
export function heroAttack(player) {
  return player.atk * (player.buffs?.atkMult || 1);
}

/**
 * Rolls a crit (Crit Eye passive) and returns the final damage for one hero-sourced hit.
 * @param {object} player
 * @param {number} targetDef defender's defense
 * @param {number} mult skill/combo multiplier (NOT including the crit multiplier)
 * @param {() => number} rand injectable RNG for deterministic tests
 */
export function rollPlayerDamage(player, targetDef, mult, rand = Math.random) {
  const crit = rand() < (player.passiveMods?.critChance || 0);
  const damage = computeDamage(heroAttack(player), targetDef, mult * (crit ? 1.6 : 1), rand);
  return { damage, crit };
}

export const CRIT_COLOR = '#fff44d';

// Extras that ride on the Assassin's basic combo hits: Poison Blade (poisons the foes the swing damaged)
// and Shadow Clone (echoes the hit around the clone). Called by player-controller after performComboHit;
// a no-op for every other class (no statuses / no clone).
import { performRadialHit } from './combat-system.js';
import { applyPoison, poisonDamage } from './monster-status.js';
import { heroAttack } from './player-damage.js';

/**
 * @param {object} world
 * @param {object[]} landed monsters the swing damaged (performComboHit's return value)
 * @param {number} step combo step (the clone repeats the same multiplier)
 */
export function applyComboRiders(world, landed, step) {
  const { player } = world;
  const blade = player.poisonBlade;
  if (blade && blade.charges > 0) {
    for (const monster of landed) {
      if (monster.dead || blade.charges <= 0) continue;
      applyPoison(monster, poisonDamage(heroAttack(player), blade.pct, player), blade.poisonMs);
      blade.charges -= 1;
    }
    if (blade.charges <= 0) player.poisonBlade = null;
  }
  if (!landed.length) return;
  const clone = world.effects.find((e) => e.kind === 'shadowClone' && !e.dead);
  if (!clone) return;
  const mult = player.combo.multiplierFor(step, player.level) * clone.mult * (1 + (player.passiveMods?.comboMult || 0));
  performRadialHit(world, clone.radius, mult, 30, { x: clone.x, y: clone.y - 6 });
  world.addEffect({ x: clone.x, y: clone.y - 8, spriteKey: 'slash', lifeMs: 180, scale: 1.2, tint: '#b57cff' });
}

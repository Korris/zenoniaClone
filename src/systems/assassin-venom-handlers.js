// Cast handlers for the Assassin's Venom effect kinds: poisonBlade, venomCloud, crippleStrike, deathMark.
// (Merged by assassin-skill-handlers.js.) A handler returning `false` = no target, tryCastSkill refunds.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { getSkillValue } from '../data/skill-definitions.js';
import { boxesOverlap } from './damage-formula.js';
import { attackBoxFor } from './combat-system.js';
import { heroAttack } from './player-damage.js';
import { applyPoisonBlade } from './player-status.js';
import { applyPoison, applySlow, applyMark, poisonDamage } from './monster-status.js';
import { monstersInRange, strikeMonster } from './assassin-strike-helpers.js';
import { VenomZone } from '../entities/venom-zone.js';

/** Poison Blade only arms a buff, deals no damage. */
export const ASSASSIN_VENOM_SUPPORT_KINDS = Object.freeze(['poisonBlade']);

const say = (world, def) => world.addText(world.player.x, world.player.y - 26, def.effect.label, def.effect.textColor, { lifeMs: 600 });

function castPoisonBlade(world, level, mult, def) {
  const { player } = world;
  applyPoisonBlade(player, getSkillValue(def.id, 'poisonPct', level), getSkillValue(def.id, 'charges', level), def.effect.durationMs, def.effect.poisonMs);
  world.addEffect({ x: player.x, y: player.y - 10, spriteKey: 'hitSpark', lifeMs: 320, scale: 2.2, follow: player, offsetY: -10, tint: '#7dff5a' });
  say(world, def);
  return true;
}

/** Cloud ahead of the hero: every tick it re-poisons the monsters standing in it (poison itself ticks once a second). */
function castVenomCloud(world, level, mult, def) {
  const { player } = world, e = def.effect;
  const v = DIRECTION_VECTORS[player.dir], radius = getSkillValue(def.id, 'radius', level);
  const dmg = poisonDamage(heroAttack(player), getSkillValue(def.id, 'poisonPct', level), player);
  world.effects.push(new VenomZone({
    x: player.x + v.x * e.start, y: player.y - 2 + v.y * e.start, radius, durationMs: e.durationMs, tickMs: e.tickMs,
    onTick: (zone) => {
      if (player.dead) return;
      for (const m of world.monsters) {
        if (!m.dead && Math.hypot(m.x - zone.x, (m.y - zone.y) / 0.72) <= zone.radius) applyPoison(m, dmg, e.poisonMs);
      }
    },
  }));
  say(world, def);
  return true;
}

/** Short stab on everything in front: damage + heavy slow. */
function castCrippleStrike(world, level, mult, def) {
  const { reach, width, knockback } = def.effect;
  const box = attackBoxFor(world.player, reach, width);
  const slow = getSkillValue(def.id, 'slowMult', level), slowMs = getSkillValue(def.id, 'slowMs', level);
  for (const monster of world.monsters) {
    if (monster.dead || !boxesOverlap(box, monster.box)) continue;
    strikeMonster(world, monster, mult, { knockback });
    if (!monster.dead) applySlow(monster, slow, slowMs);
  }
  world.hitStopMs = 40;
  say(world, def);
  return true;
}

/** Marks the nearest foe in range; see monster-status-tick.js for the detonation. */
function castDeathMark(world, level, mult, def) {
  const target = monstersInRange(world, getSkillValue(def.id, 'range', level))[0];
  if (!target) return false;
  applyMark(target, {
    durationMs: getSkillValue(def.id, 'markMs', level), storePct: getSkillValue(def.id, 'storePct', level),
    base: heroAttack(world.player) * mult,
  });
  world.addEffect({ x: target.x, y: target.y - 8, spriteKey: 'hitSpark', lifeMs: 360, scale: 2.4, tint: '#b57cff' });
  say(world, def);
  return true;
}

export const ASSASSIN_VENOM_HANDLERS = Object.freeze({
  poisonBlade: castPoisonBlade, venomCloud: castVenomCloud, crippleStrike: castCrippleStrike, deathMark: castDeathMark,
});

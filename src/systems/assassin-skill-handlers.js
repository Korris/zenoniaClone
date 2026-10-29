// Cast handlers for the Assassin's Shadow + Agility effect kinds (merged into CAST_HANDLERS in
// skill-cast-system.js): shadowStep, backstab, smokeBomb, phantomAssault, shadowClone. Venom kinds are in
// assassin-venom-handlers.js. Adrenaline reuses the shared 'haste' kind, Blade Dance the shared 'radial'.
// A handler returning `false` means "nothing to do" (e.g. no target): tryCastSkill then refunds MP + cooldown.
import { getSkillValue } from '../data/skill-definitions.js';
import { circleOverlapsBox, boxesOverlap } from './damage-formula.js';
import { attackBoxFor } from './combat-system.js';
import { applyStealth } from './player-status.js';
import { applySlow, applyBlind } from './monster-status.js';
import { monstersInRange, blinkBehind, strikeMonster, shadowPuff } from './assassin-strike-helpers.js';
import { ASSASSIN_VENOM_HANDLERS, ASSASSIN_VENOM_SUPPORT_KINDS } from './assassin-venom-handlers.js';
import { SkillPulse } from '../entities/skill-pulse.js';
import { ShadowClone } from '../entities/shadow-clone.js';

/** Effect kinds that deal no damage when cast, so they must not interrupt the combo chain. */
export const ASSASSIN_SUPPORT_KINDS = Object.freeze(['smokeBomb', 'shadowClone', ...ASSASSIN_VENOM_SUPPORT_KINDS]);

const say = (world, def) => world.addText(world.player.x, world.player.y - 26, def.effect.label, def.effect.textColor, { lifeMs: 600 });

/** Blink behind the nearest foe within `range` and stab it. */
function castShadowStep(world, level, mult, def) {
  const { player } = world, e = def.effect;
  const target = monstersInRange(world, getSkillValue(def.id, 'range', level))[0];
  if (!target) return false;
  blinkBehind(world, target, e.behindOffset);
  player.invulnTimer = Math.max(player.invulnTimer, e.invulnMs);
  strikeMonster(world, target, mult, e);
  world.camera.shake(1.5, 90);
  world.hitStopMs = 40;
  say(world, def);
  return true;
}

/** Short-reach stab in front: big crit chance, extra damage on foes whose back is turned. */
function castBackstab(world, level, mult, def) {
  const { reach, width, knockback } = def.effect;
  const box = attackBoxFor(world.player, reach, width);
  const opts = { knockback, critBonus: getSkillValue(def.id, 'critBonus', level), behindMult: getSkillValue(def.id, 'behindMult', level) };
  let hit = 0;
  for (const monster of world.monsters) {
    if (monster.dead || !boxesOverlap(box, monster.box)) continue;
    strikeMonster(world, monster, mult, opts);
    hit++;
  }
  if (hit) world.hitStopMs = 40;
  say(world, def);
  return true;
}

/** Smoke cloud: foes inside are slowed + blinded; the hero turns stealthy (extra dodge). */
function castSmokeBomb(world, level, mult, def) {
  const { player } = world;
  const radius = getSkillValue(def.id, 'radius', level), slow = getSkillValue(def.id, 'slowMult', level);
  for (const monster of world.monsters) {
    if (monster.dead || !circleOverlapsBox(player.x, player.y - 6, radius, monster.box)) continue;
    applySlow(monster, slow, def.effect.debuffMs);
    applyBlind(monster, def.effect.debuffMs);
  }
  applyStealth(player, getSkillValue(def.id, 'evasion', level), getSkillValue(def.id, 'stealthMs', level));
  shadowPuff(world, player.x, player.y, (2 * radius) / 30);
  say(world, def);
  return true;
}

/** Blinks through up to `targets` foes (nearest first), one stab per 130ms, invulnerable throughout. */
function castPhantomAssault(world, level, mult, def) {
  const { player } = world, e = def.effect;
  const targets = monstersInRange(world, getSkillValue(def.id, 'range', level)).slice(0, getSkillValue(def.id, 'targets', level));
  if (!targets.length) return false;
  player.invulnTimer = Math.max(player.invulnTimer, targets.length * e.intervalMs + 250);
  let i = 0;
  const strike = () => {
    const target = targets[i++];
    if (player.dead || !target || target.dead) return;
    blinkBehind(world, target, e.behindOffset);
    strikeMonster(world, target, mult, e);
    world.camera.shake(1.5, 80);
  };
  strike();
  if (targets.length > 1) world.skillPulses.push(new SkillPulse({ remaining: targets.length - 1, intervalMs: e.intervalMs, onPulse: strike }));
  say(world, def);
  return true;
}

/** Summons the clone (replacing any previous one); it echoes basic hits, see assassin-combo-riders.js. */
function castShadowClone(world, level, mult, def) {
  for (const old of world.effects) if (old.kind === 'shadowClone') old.dead = true;
  world.effects.push(new ShadowClone({
    owner: world.player, durationMs: getSkillValue(def.id, 'durationMs', level),
    mult: getSkillValue(def.id, 'cloneMult', level), radius: def.effect.mirrorRadius,
  }));
  shadowPuff(world, world.player.x - 14, world.player.y + 2);
  say(world, def);
  return true;
}

export const ASSASSIN_CAST_HANDLERS = Object.freeze({
  shadowStep: castShadowStep, backstab: castBackstab, smokeBomb: castSmokeBomb,
  phantomAssault: castPhantomAssault, shadowClone: castShadowClone, ...ASSASSIN_VENOM_HANDLERS,
});

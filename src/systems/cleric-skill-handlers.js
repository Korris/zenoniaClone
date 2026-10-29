// Cast handlers for the Cleric's own effect kinds (merged into CAST_HANDLERS in skill-cast-system.js):
//   holyBlast (Divine Judgment, Heavenly Ray), healZone (Healing Circle), purify, shield (Sacred Shield),
//   haste (Haste Hymn), angelsDescent. Heal / buff / projectile / strike reuse the shared handlers.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { getSkillValue } from '../data/skill-definitions.js';
import { performRadialHit } from './combat-system.js';
import { applyShield, applyHaste } from './player-status.js';
import { SkillPulse } from '../entities/skill-pulse.js';
import { HealingZone } from '../entities/healing-zone.js';
import { HOLY_FX_TINT } from '../data/cleric-fx-config.js';

/** Effect kinds that never interrupt the combo chain (they deal no damage). */
export const SUPPORT_EFFECT_KINDS = Object.freeze(['heal', 'buff', 'healZone', 'purify', 'shield', 'haste']);

const burst = (world, x, y, def, radius) => world.addEffect({
  x, y: y - 2, spriteKey: def.effect.fx || 'arcaneBurst', rotation: Math.random() * Math.PI * 2,
  lifeMs: 320, scale: (2 * radius) / (def.effect.fxDiameter || 40), spin: 8, tint: def.effect.tint,
});
const say = (world, text, color) => world.addText(world.player.x, world.player.y - 26, text, color, { lifeMs: 700 });
const sparkle = (world) => {
  const { player } = world;
  world.addEffect({ x: player.x, y: player.y - 10, spriteKey: 'hitSpark', lifeMs: 320, scale: 2.2, follow: player, offsetY: -10, tint: HOLY_FX_TINT });
};

/** Holy damage at points ahead of the hero (fixed at cast time): a delayed pillar (1 pulse) or a sweeping beam. */
function castHolyBlast(world, level, mult, def) {
  const { player } = world, e = def.effect;
  const v = DIRECTION_VECTORS[player.dir], radius = getSkillValue(def.id, 'radius', level);
  const origin = { x: player.x, y: player.y - 6 };
  const strike = (i) => {
    if (player.dead) return;
    const d = e.start + e.advance * i, center = { x: origin.x + v.x * d, y: origin.y + v.y * d };
    performRadialHit(world, radius, mult, e.knockback, center);
    burst(world, center.x, center.y, def, radius);
    world.camera.shake(1.5, 90);
  };
  if (e.pulses === 1) { // telegraph at the target, then the pillar lands
    burst(world, origin.x + v.x * e.start, origin.y + v.y * e.start, def, radius * 0.5);
    world.skillPulses.push(new SkillPulse({ remaining: 1, intervalMs: e.delayMs, onPulse: () => strike(0) }));
  } else {
    let i = 0;
    strike(i++);
    world.skillPulses.push(new SkillPulse({ remaining: e.pulses - 1, intervalMs: e.intervalMs, onPulse: () => strike(i++) }));
  }
  world.addText(player.x, player.y - 26, e.label, e.textColor, { lifeMs: 500 });
}

/** Healing Circle: a ground zone that heals the hero a % of max HP each tick while inside it. */
function castHealZone(world, level, mult, def) {
  const { player } = world;
  const percent = getSkillValue(def.id, 'healPercent', level);
  world.effects.push(new HealingZone({
    x: player.x, y: player.y - 2, radius: getSkillValue(def.id, 'radius', level),
    durationMs: def.effect.durationMs, tickMs: def.effect.tickMs,
    onTick: (zone) => {
      if (player.dead || Math.hypot(player.x - zone.x, (player.y - zone.y) / 0.72) > zone.radius) return;
      const amount = Math.round((player.maxHp * percent) / 100);
      player.heal(amount);
      world.addText(player.x, player.y - 24, `+${amount}`, def.effect.textColor, { lifeMs: 600 });
    },
  }));
  world.addText(player.x, player.y - 26, def.effect.label, def.effect.textColor, { lifeMs: 500 });
}

/** Purify: cleanse knockback/hit-stun, small heal, brief invulnerability. */
function castPurify(world, level, mult, def) {
  const { player } = world;
  const amount = Math.round((player.maxHp * getSkillValue(def.id, 'healPercent', level)) / 100);
  player.heal(amount);
  player.knockback = { vx: 0, vy: 0, timer: 0 };
  player.hitFlashTimer = 0;
  player.invulnTimer = Math.max(player.invulnTimer, getSkillValue(def.id, 'invulnMs', level));
  sparkle(world);
  say(world, `${def.effect.label} +${amount} HP`, def.effect.textColor);
}

function castShield(world, level, mult, def) {
  const { player } = world;
  applyShield(player, (player.maxHp * getSkillValue(def.id, 'shieldPercent', level)) / 100, def.effect.durationMs);
  sparkle(world);
  say(world, `${def.effect.label} (${player.shield.amount})`, def.effect.textColor);
}

function castHaste(world, level, mult, def) {
  applyHaste(world.player, getSkillValue(def.id, 'hasteMult', level), def.effect.durationMs);
  sparkle(world);
  say(world, def.effect.label, def.effect.textColor);
}

/** Angel's Descent: holy blast around the hero, then ATK buff + haste + shield together. */
function castAngelsDescent(world, level, mult, def) {
  const { player } = world, e = def.effect;
  const radius = getSkillValue(def.id, 'radius', level);
  performRadialHit(world, radius, mult, e.knockback);
  burst(world, player.x, player.y - 8, def, radius);
  player.buffs = { atkMult: getSkillValue(def.id, 'atkMult', level), timer: e.durationMs };
  applyHaste(player, getSkillValue(def.id, 'hasteMult', level), e.durationMs);
  applyShield(player, (player.maxHp * getSkillValue(def.id, 'shieldPercent', level)) / 100, e.durationMs);
  world.camera.shake(3, 200);
  sparkle(world);
  say(world, e.label, e.textColor);
}

export const CLERIC_CAST_HANDLERS = Object.freeze({
  holyBlast: castHolyBlast, healZone: castHealZone, purify: castPurify,
  shield: castShield, haste: castHaste, angelsDescent: castAngelsDescent,
});

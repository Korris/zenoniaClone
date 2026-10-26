// Executes active skills. Data-driven by `effect.kind` (see src/data/skill-definitions.js):
// 'projectile', 'radial', 'strike', 'dash', 'heal', 'buff'. One handler per kind, shared by
// every skill that uses it, so adding a new active skill rarely needs new code here.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { SKILL_DEFINITIONS, getSkillValue } from '../data/skill-definitions.js';
import { canCastSkill } from './skill-system.js';
import { performRadialHit, performSkillStrike, damageMonster } from './combat-system.js';
import { heroAttack, rollPlayerDamage, CRIT_COLOR } from './player-damage.js';
import { boxesOverlap } from './damage-formula.js';
import { Projectile } from '../entities/projectile.js';
import { SkillPulse } from '../entities/skill-pulse.js';

const DASH_SPEED = 420; // px/s
const DIR_ANGLE = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 };

/** Attempts to cast; returns {ok, reason}. Spends MP and starts cooldown on success. */
export function tryCastSkill(world, skillId) {
  const { player } = world;
  if (player.dead || player.dash) return { ok: false, reason: 'Busy' };
  const check = canCastSkill(player, skillId, player.cooldowns);
  if (!check.ok) return check;

  player.mp -= check.cost;
  player.cooldowns[skillId] = getSkillValue(skillId, 'cooldownMs', check.level);
  const def = SKILL_DEFINITIONS[skillId];
  const mult = getSkillValue(skillId, 'damageMult', check.level);
  // Non-damage skills (heal/buff) don't interrupt an in-progress combo chain.
  if (def.effect.kind !== 'heal' && def.effect.kind !== 'buff') player.combo.reset();

  CAST_HANDLERS[def.effect.kind](world, check.level, mult, def);
  return { ok: true, reason: '' };
}

function castProjectile(world, level, mult, def) {
  const { player } = world;
  const v = DIRECTION_VECTORS[player.dir];
  const { speed, spriteKey, w, h, scale, pierceFromLevel, label, textColor, explodeFx } = def.effect;
  const range = getSkillValue(def.id, 'range', level);
  world.projectiles.push(new Projectile({
    x: player.x + v.x * 10, y: player.y - 6 + v.y * 10, vx: v.x * speed, vy: v.y * speed,
    owner: 'player', damage: heroAttack(player) * mult, spriteKey, scale,
    lifeMs: (range / speed) * 1000, w, h, pierce: level >= pierceFromLevel,
    // Fireball-style skills burst on impact (projectile-hit-system.js); 0 for plain projectiles.
    explodeRadius: getSkillValue(def.id, 'explodeRadius', level), explodeFx, knockback: 110,
  }));
  world.addText(player.x, player.y - 26, label, textColor, { lifeMs: 500 });
}

function castRadial(world, level, mult, def) {
  const { player } = world;
  const { knockback, pulses = 1, pulseIntervalMs, label, textColor, fx = 'arc', fxDiameter } = def.effect;
  const radius = getSkillValue(def.id, 'radius', level);
  // fx: burst sprite. The sword arc keeps its old radius/30 sizing; HD bursts give their diameter in
  // world units (fxDiameter) so the art spans exactly the hit radius.
  const fxScale = fxDiameter ? (2 * radius) / fxDiameter : radius / 30;
  const pulse = () => {
    performRadialHit(world, radius, mult, knockback);
    world.addEffect({ x: player.x, y: player.y - 8, spriteKey: fx, rotation: Math.random() * Math.PI * 2, lifeMs: 320, scale: fxScale, spin: 16, follow: player, offsetY: -8 });
  };
  pulse();
  if (pulses > 1) world.skillPulses.push(new SkillPulse({ remaining: pulses - 1, intervalMs: pulseIntervalMs, onPulse: pulse }));
  world.camera.shake(pulses > 1 ? 3 : 2, 160);
  world.addText(player.x, player.y - 26, label, textColor, { lifeMs: 500 });
}

function castStrike(world, level, mult, def) {
  const { player } = world;
  const { reach, width, knockback, label, textColor } = def.effect;
  performSkillStrike(world, reach, width, mult, knockback);
  const v = DIRECTION_VECTORS[player.dir];
  world.addEffect({ x: player.x + v.x * 14, y: player.y - 7 + v.y * 14, spriteKey: 'arc', rotation: DIR_ANGLE[player.dir], lifeMs: 260, scale: 1.3 });
  world.hitStopMs = 40;
  world.addText(player.x, player.y - 26, label, textColor, { lifeMs: 500 });
}

function castDash(world, level, mult, def) {
  const { player } = world;
  const v = DIRECTION_VECTORS[player.dir];
  const distance = getSkillValue(def.id, 'dashDistance', level);
  player.dash = { dirX: v.x, dirY: v.y, speed: DASH_SPEED, remaining: distance, damageMult: mult, hitIds: new Set() };
  world.addText(player.x, player.y - 26, def.effect.label, def.effect.textColor, { lifeMs: 500 });
}

function castHeal(world, level, mult, def) {
  const { player } = world;
  const percent = getSkillValue(def.id, 'healPercent', level);
  const amount = Math.round((player.maxHp * percent) / 100);
  player.heal(amount);
  world.addEffect({ x: player.x, y: player.y - 10, spriteKey: 'hitSpark', lifeMs: 320, scale: 2.2, follow: player, offsetY: -10 });
  world.addText(player.x, player.y - 26, `+${amount} HP`, def.effect.textColor, { lifeMs: 700 });
}

function castBuff(world, level, mult, def) {
  const { player } = world;
  const atkMult = getSkillValue(def.id, 'atkMult', level);
  player.buffs = { atkMult, timer: def.effect.durationMs };
  world.addEffect({ x: player.x, y: player.y - 10, spriteKey: 'hitSpark', lifeMs: 320, scale: 2.2, follow: player, offsetY: -10 });
  world.addText(player.x, player.y - 26, def.effect.label, def.effect.textColor, { lifeMs: 700 });
}

const CAST_HANDLERS = Object.freeze({
  projectile: castProjectile, radial: castRadial, strike: castStrike, dash: castDash, heal: castHeal, buff: castBuff,
});

/**
 * Distance covered `t` ms into an eased dash ({distance, durationMs, exitSpeed}): a fast burst that
 * decelerates (ease-out) and finishes moving at exactly `exitSpeed` px/s, so handing over to normal
 * walking (or to the next chained dash) has no speed jump.
 */
export function easedDashTravel(dash, t) {
  const T = dash.durationMs;
  const u = Math.min(1, t / T);
  const linear = (dash.exitSpeed * Math.min(t, T)) / 1000;
  const burst = dash.distance - (dash.exitSpeed * T) / 1000;
  return linear + burst * (1 - (1 - u) * (1 - u));
}

/** Moves the hero along the dash and damages everything crossed. */
export function updateDash(world, dt) {
  const { player, tilemap } = world;
  const dash = player.dash;
  if (!dash) return;
  let stepDist;
  if (dash.durationMs) { // eased (free double-tap) dash
    dash.elapsed = Math.min(dash.durationMs, (dash.elapsed || 0) + dt);
    // Snap float residue (e.g. 12 x 16.67ms = 199.99) to the end: otherwise one extra near-zero
    // dash frame runs before walking resumes, which reads as a hitch.
    if (dash.durationMs - dash.elapsed < 1) dash.elapsed = dash.durationMs;
    const travel = easedDashTravel(dash, dash.elapsed);
    stepDist = travel - (dash.travelled || 0);
    dash.travelled = travel;
  } else { // constant-speed skill dash
    stepDist = Math.min(dash.remaining, (dash.speed * dt) / 1000);
    dash.remaining -= stepDist;
  }
  const box = player.box;
  const moved = tilemap.moveBox(box, dash.dirX * stepDist, dash.dirY * stepDist);
  player.x = moved.x + box.w / 2;
  player.y = moved.y + box.h;

  const strikeBox = { x: player.x - 12, y: player.y - 18, w: 24, h: 20 };
  for (const monster of dash.damageMult > 0 ? world.monsters : []) { // free (double-tap) dash deals no damage
    if (monster.dead || dash.hitIds.has(monster.id) || !boxesOverlap(strikeBox, monster.box)) continue;
    dash.hitIds.add(monster.id);
    const { damage, crit } = rollPlayerDamage(player, monster.def.def, dash.damageMult);
    damageMonster(world, monster, damage, dash.dirX * 80, dash.dirY * 80, crit ? CRIT_COLOR : '#ffe680');
    world.addEffect({ x: monster.x, y: monster.y - 7, spriteKey: 'slash', rotation: DIR_ANGLE[player.dir], lifeMs: 160, scale: 0.6 });
  }

  const finished = dash.durationMs ? dash.elapsed >= dash.durationMs : dash.remaining <= 0;
  if (finished || moved.blockedX || moved.blockedY) player.dash = null;
}

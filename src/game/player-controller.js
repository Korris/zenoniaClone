// Translates input into hero actions each frame: movement with collision, facing, combo
// attacks, skill casts, knockback, regen and timers.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { HP_REGEN_PER_SECOND, MP_REGEN_PER_SECOND } from '../data/hero-stats-config.js';
import { SKILL_HOTKEY_ACTIONS } from '../data/skill-definitions.js';
import { tickCooldowns } from '../systems/skill-system.js';
import { performComboHit } from '../systems/combat-system.js';
import { fireComboBolt } from '../systems/projectile-hit-system.js';
import { tryCastSkill, updateDash } from '../systems/skill-cast-system.js';

export function facingFromVector(x, y, current) {
  if (x === 0 && y === 0) return current;
  if (Math.abs(x) > Math.abs(y)) return x > 0 ? 'right' : 'left';
  return y > 0 ? 'down' : 'up';
}

function moveWithCollision(world, dx, dy) {
  const { player, tilemap } = world;
  const box = player.box;
  const moved = tilemap.moveBox(box, dx, dy);
  player.x = moved.x + box.w / 2;
  player.y = moved.y + box.h;
}

const DASH_TRAIL_EVERY_MS = 45;
const DOUBLE_TAP_MS = 260; // two taps of the same direction within this window = free dash
// Zenonia-style quick step: ~3 tiles in 200 ms, bursting then easing out to walking speed. The
// cooldown (from dash start) leaves only a short gap, so double-taps can be chained, and a
// double-tap made mid-dash / mid-cooldown is buffered for `bufferMs` instead of being dropped.
const FREE_DASH = { distance: 48, durationMs: 200, cooldownMs: 260, bufferMs: 250 };
const MOVE_ACTIONS = { moveUp: 'up', moveDown: 'down', moveLeft: 'left', moveRight: 'right' };
const DIR_ANGLE = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 };

/** Leaves a motion-trail effect behind the dashing hero every few ms. */
function spawnDashTrail(world, dt) {
  const { player } = world;
  player.dash.trailTimer = (player.dash.trailTimer || 0) + dt;
  if (player.dash.trailTimer < DASH_TRAIL_EVERY_MS) return;
  player.dash.trailTimer = 0;
  world.addEffect({ x: player.x - player.dash.dirX * 10, y: player.y - 8, spriteKey: 'dashTrail', rotation: DIR_ANGLE[player.dir], lifeMs: 220, scale: 0.9 });
}

/** Direction double-tapped this frame (same direction pressed twice within DOUBLE_TAP_MS), or null. */
function detectDoubleTap(player, input, now) {
  for (const [action, dir] of Object.entries(MOVE_ACTIONS)) {
    if (!input.wasPressed(action)) continue;
    const tap = player.lastTap;
    if (tap && tap.dir === dir && now - tap.time < DOUBLE_TAP_MS) {
      player.lastTap = null;
      return dir;
    }
    player.lastTap = { dir, time: now };
  }
  return null;
}

/**
 * Double-tapping a direction key starts a short, damage-free dash (i-frames, trail, no MP). Taps are
 * read every frame -- also while dashing -- and a double-tap that can't fire yet is buffered.
 * A dash may cancel a combo's recovery (not its windup/active swing), as in Zenonia.
 */
function handleDoubleTapDash(world, input) {
  const { player } = world;
  const now = world.elapsedMs;
  const dir = detectDoubleTap(player, input, now);
  if (dir) player.queuedDash = { dir, expiresAt: now + FREE_DASH.bufferMs };
  const queued = player.queuedDash;
  if (!queued) return;
  if (now > queued.expiresAt) {
    player.queuedDash = null;
    return;
  }
  const { combo } = player;
  const comboBusy = combo.isAttacking && combo.state !== 'recover';
  if (player.dash || player.freeDashCooldown > 0 || player.knockback.timer > 0 || comboBusy) return;
  if (combo.isAttacking) combo.reset();
  const v = DIRECTION_VECTORS[queued.dir];
  player.dir = queued.dir;
  player.dash = {
    dirX: v.x, dirY: v.y, distance: FREE_DASH.distance, durationMs: FREE_DASH.durationMs,
    exitSpeed: player.speed, damageMult: 0, hitIds: new Set(),
  };
  player.freeDashCooldown = FREE_DASH.cooldownMs;
  player.queuedDash = null;
}

/** During windup + active the hero slides forward `stepPx` for the current combo hit (momentum). */
function lungeForward(world, dt) {
  const { player } = world;
  const { combo } = player;
  const hit = combo.currentHit;
  if (!hit || combo.state === 'recover' || !hit.stepPx) return;
  const v = DIRECTION_VECTORS[player.dir];
  const perMs = hit.stepPx / (hit.windupMs + hit.activeMs);
  moveWithCollision(world, v.x * perMs * dt, v.y * perMs * dt);
}

function applyKnockback(world, dt) {
  const kb = world.player.knockback;
  if (kb.timer <= 0) return false;
  kb.timer -= dt;
  moveWithCollision(world, (kb.vx * dt) / 1000, (kb.vy * dt) / 1000);
  return true;
}

/** K/L/I cast whatever skill is currently bound to that hotbar slot (if any). */
function handleSkillInput(world, input) {
  const { player } = world;
  for (const [action, slot] of Object.entries(SKILL_HOTKEY_ACTIONS)) {
    if (!input.wasPressed(action)) continue;
    const skillId = player.hotbar[slot];
    if (!skillId) continue;
    const result = tryCastSkill(world, skillId);
    if (!result.ok && result.reason !== 'Busy') {
      world.addText(player.x, player.y - 26, result.reason, '#ff9a7a', { lifeMs: 600 });
    }
  }
}

function tickTimers(player, dt) {
  tickCooldowns(player.cooldowns, dt);
  if (player.invulnTimer > 0) player.invulnTimer -= dt;
  if (player.hitFlashTimer > 0) player.hitFlashTimer -= dt;
  player.tickBuffs(dt);
  const mods = player.passiveMods;
  player.heal((HP_REGEN_PER_SECOND * mods.regenMult * dt) / 1000);
  player.restoreMp((MP_REGEN_PER_SECOND * mods.mpRegenMult * dt) / 1000);
}

/**
 * @param {import('./world-state.js').WorldState} world
 * @param {import('../core/input-manager.js').InputManager} input
 * @param {number} dt ms
 */
export function updatePlayer(world, input, dt, banner) {
  const { player } = world;
  if (player.dead) {
    player.deathTimer += dt;
    return;
  }
  tickTimers(player, dt);
  player.idleTimer += dt;
  if (player.freeDashCooldown > 0) player.freeDashCooldown -= dt;
  handleDoubleTapDash(world, input); // every frame, so taps made mid-dash still count

  if (player.dash) {
    spawnDashTrail(world, dt); // before updateDash, which may end the dash this frame
    updateDash(world, dt);
    player.moving = true;
    player.animTimer += dt;
    return;
  }

  if (applyKnockback(world, dt)) {
    player.moving = false;
    return;
  }

  handleSkillInput(world, input);
  if (player.dash) return; // skill dash started this frame

  // Combo: process events generated by the attack state machine
  if (input.wasPressed('attack')) player.combo.pressAttack();
  for (const event of player.combo.update(dt)) {
    if (event.type !== 'hit') continue;
    if (player.attackStyle === 'ranged') fireComboBolt(world, event.step); // wizard bolts / mechanic bullets
    else performComboHit(world, event.step); // warrior: sword arc
  }

  if (player.combo.isAttacking) {
    // Allow turning during windup so quick direction changes still feel responsive
    const v = input.getMoveVector();
    if (player.combo.state === 'windup') player.dir = facingFromVector(v.x, v.y, player.dir);
    lungeForward(world, dt);
    player.moving = false;
    return;
  }

  const move = input.getMoveVector();
  player.moving = move.x !== 0 || move.y !== 0;
  if (player.moving) {
    player.dir = facingFromVector(move.x, move.y, player.dir);
    const distance = (player.speed * dt) / 1000;
    moveWithCollision(world, move.x * distance, move.y * distance);
    player.animTimer += dt;
  } else {
    player.animTimer = 0;
  }
}

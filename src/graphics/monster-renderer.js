// Draws monsters from 4-direction sheets (16px pixel pack, or the HD painted walk-cycle sheets of
// Frost Hollow where loaded) and the boss from idle/walk/hit strips.
import { drawShadow } from './player-renderer.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';
import { HD_MONSTER, HD_MONSTER_ATTACK } from './asset-atlas-hd-monsters.js';
import { COMBAT_FEEL } from '../data/combat-feel-config.js';

export const RIM_GROW = 0.12;
const FRAME_MS = 180;
export const BOSS_FRAME_MS = 140;
// Real rear-view frames are available; walking upward must never show the front face.
export const HD_MONSTER_FACING = Object.freeze({ down: 'down', up: 'up', left: 'left', right: 'right' });
export const HD_MONSTER_UNITS = 24; // 192px frame / pixelsPerUnit 8, matching the HD hero.
/** Impact squash: widest/flattest at the moment of the hit, relaxing as the flash timer runs out. */
export function hitSquash(monster) {
  const k = Math.max(0, Math.min(1, (monster.hitFlashTimer || 0) / COMBAT_FEEL.hitReaction.flashMs));
  const a = COMBAT_FEEL.hitReaction.squashAmount * k;
  return { sx: 1 + a, sy: 1 - a };
}
export const launchHeight = monster => monster.airborneMs > 0 ? Math.sin(Math.PI * (1 - monster.airborneMs / 1500)) * 13 : 0;

function drawTelegraph(ctx, monster) {
  // Ranged/boss attackers would cover the screen, so the ring is capped to the melee reach
  const ringRadius = Math.min(monster.def.attackRange, 16) + 4;
  const pulse = 0.35 + 0.25 * Math.sin(monster.animTimer / 50);
  ctx.fillStyle = `rgba(255,60,60,${pulse})`;
  ctx.beginPath();
  ctx.ellipse(monster.x, monster.y, ringRadius, ringRadius * 0.5, 0, 0, Math.PI * 2);
  ctx.fill();
}

function drawBoss(ctx, monster, registry) {
  const { boss } = registry;
  const moving = monster.ai.state === 'chase' || monster.ai.state === 'return';
  const strip = monster.hitFlashTimer > 0 ? boss.hit : moving ? boss.walk : boss.idle;
  const frame = strip[Math.floor(monster.animTimer / BOSS_FRAME_MS) % strip.length];
  const drawX = Math.round(monster.x - boss.width / 2);
  const drawY = Math.round(monster.y - boss.height + 6);
  drawShadow(ctx, monster.x, monster.y, 16, 5);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  ctx.save();
  if (monster.dir === 'right') {
    ctx.translate(drawX + boss.width, 0);
    ctx.scale(-1, 1);
    ctx.drawImage(frame, 0, drawY);
  } else {
    ctx.drawImage(frame, drawX, drawY);
  }
  ctx.restore();
}

function drawMiniHpBar(ctx, monster, topY) {
  const w = 14;
  const x = Math.round(monster.x - w / 2);
  const y = topY - 4;
  ctx.fillStyle = '#1b1b2a';
  ctx.fillRect(x - 1, y - 1, w + 2, 3);
  ctx.fillStyle = '#3a3a4a';
  ctx.fillRect(x, y, w, 1);
  ctx.fillStyle = '#e03c3c';
  ctx.fillRect(x, y, Math.max(1, Math.round((w * monster.hp) / monster.maxHp)), 1);
}

/** Wind-up "!" and the mini HP bar, hung just above `topY` (the top of the drawn body). */
function drawMarkers(ctx, monster, topY) {
  if (monster.isTelegraphing) {
    ctx.fillStyle = '#ff3b3b';
    ctx.fillRect(Math.round(monster.x) - 1, topY - 7, 2, 4);
    ctx.fillRect(Math.round(monster.x) - 1, topY - 2, 2, 1);
  }
  if (monster.hp < monster.maxHp) drawMiniHpBar(ctx, monster, topY);
}

/** Idle/cooldown hold frame 0; every other state loops the 4-frame cycle. */
export function pickFrame(frames, monster) {
  const idle = monster.ai.state === 'idle' || monster.ai.state === 'cooldown';
  return idle ? frames[0] : frames[Math.floor(monster.animTimer / FRAME_MS) % frames.length];
}

/** The strike begins exactly when the AI deals damage, then recovers without looping. */
export function attackFrameIndex(monster) {
  const { state, timer } = monster.ai;
  if (state === 'windup') {
    const windup = monster.def?.attackWindupMs || 350;
    return timer > windup / 2 ? 0 : 1;
  }
  if (state === 'cooldown') {
    const elapsed = monster.def?.attackCooldownMs - timer;
    if (elapsed >= 0 && elapsed < HD_MONSTER_ATTACK.recoveryMs) {
      return 2 + Math.floor(elapsed / (HD_MONSTER_ATTACK.recoveryMs / 2));
    }
  }
  return -1;
}

/** HD painted sheet: drawn smoothed at a fixed 24-unit box, markers hang over the painted body. */
function drawMonsterHd(ctx, monster, hd) {
  const renderScale = monster.def?.renderScale || 1;
  const facing = HD_MONSTER_FACING[monster.dir] || 'down';
  const attackIndex = attackFrameIndex(monster);
  const attacking = monster.hitFlashTimer <= 0 && attackIndex >= 0 && hd.attack?.[facing];
  const size = (attacking ? HD_MONSTER_ATTACK.frame / HD_MONSTER.pixelsPerUnit : HD_MONSTER_UNITS) * renderScale;
  const feetY = attacking ? HD_MONSTER_ATTACK.feetY / HD_MONSTER.pixelsPerUnit * renderScale : size;
  const frame = monster.hitFlashTimer > 0 ? hd.hitFlash[facing]
    : attacking ? hd.attack[facing][attackIndex] : pickFrame(hd.frames[facing], monster);
  const drawX = Math.round(monster.x - size / 2);
  const lift = launchHeight(monster);
  const drawY = Math.round(monster.y - feetY - lift);
  drawShadow(ctx, monster.x, monster.y, 6 * renderScale, 2.5);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  const { sx, sy } = hitSquash(monster);
  const dx = drawX - (size * (sx - 1)) / 2, dy = drawY + size * (1 - sy), w = size * sx, h = size * sy;
  if (monster.hitFlashTimer > 0 && hd.hitRim?.[facing]) { // orange rim: same silhouette, +12%, behind the white flash
    const grow = RIM_GROW;
    drawSmoothImage(ctx, hd.hitRim[facing], dx - (w * grow) / 2, dy - h * grow, w * (1 + grow), h * (1 + grow));
  }
  // Squash keeps the feet planted: width grows around the centre, height shrinks toward the ground.
  drawSmoothImage(ctx, frame, dx, dy, w, h);
  ctx.imageSmoothingEnabled = false;
  drawMarkers(ctx, monster, Math.round(monster.y - hd.bodyUnits * renderScale - lift));
}

/**
 * @param {CanvasRenderingContext2D} ctx world-space context
 * @param {import('../entities/monster.js').Monster} monster
 * @param {object} registry sprite registry
 */
export function drawMonster(ctx, monster, registry) {
  const hd = registry.monstersHd?.[monster.type];
  if (hd) return drawMonsterHd(ctx, monster, hd);
  if (monster.isBoss && !monster.def.localBoss) return drawBoss(ctx, monster, registry);
  const sprite = registry.monsters[monster.type] || registry.monsters[monster.def.sprite];
  if (!sprite) return;
  const frames = sprite.frames[monster.dir] || sprite.frames.down;
  const frame = pickFrame(frames, monster);
  const drawX = Math.round(monster.x - sprite.width / 2);
  const drawY = Math.round(monster.y - sprite.height - launchHeight(monster));

  drawShadow(ctx, monster.x, monster.y, 5, 2);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  const { sx, sy } = hitSquash(monster);
  const w = sprite.width * sx, h = sprite.height * sy;
  if (monster.hitFlashTimer > 0 && sprite.hitRim?.[monster.dir]) {
    ctx.drawImage(sprite.hitRim[monster.dir], Math.round(drawX - (w * (1 + RIM_GROW) - sprite.width) / 2),
      Math.round(drawY + sprite.height - h * (1 + RIM_GROW)), Math.round(w * (1 + RIM_GROW)), Math.round(h * (1 + RIM_GROW)));
  }
  ctx.drawImage(monster.hitFlashTimer > 0 ? sprite.hitFlash[monster.dir] || sprite.hitFlash.down : frame,
    Math.round(drawX - (w - sprite.width) / 2), Math.round(drawY + sprite.height - h), Math.round(w), Math.round(h));
  drawMarkers(ctx, monster, drawY);
}

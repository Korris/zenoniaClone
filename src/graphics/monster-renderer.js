// Draws monsters from 4-direction sheets (16px pixel pack, or the HD painted walk-cycle sheets of
// Frost Hollow where loaded) and the boss from idle/walk/hit strips.
import { drawShadow } from './player-renderer.js';

const FRAME_MS = 180;
const BOSS_FRAME_MS = 140;
// HD sheets paint no rear pose (asset-atlas-hd-monsters.js), so 'up' shows the front row.
const HD_MONSTER_FACING = Object.freeze({ down: 'down', up: 'down', left: 'left', right: 'right' });
const HD_MONSTER_UNITS = 24; // 96px frame / pixelsPerUnit 4 -- the same world-space box as the HD hero

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
function pickFrame(frames, monster) {
  const idle = monster.ai.state === 'idle' || monster.ai.state === 'cooldown';
  return idle ? frames[0] : frames[Math.floor(monster.animTimer / FRAME_MS) % frames.length];
}

/** HD painted sheet: drawn smoothed at a fixed 24-unit box, markers hang over the painted body. */
function drawMonsterHd(ctx, monster, hd) {
  const facing = HD_MONSTER_FACING[monster.dir] || 'down';
  const frame = monster.hitFlashTimer > 0 ? hd.hitFlash[facing] : pickFrame(hd.frames[facing], monster);
  const drawX = Math.round(monster.x - HD_MONSTER_UNITS / 2);
  const drawY = Math.round(monster.y - HD_MONSTER_UNITS);
  drawShadow(ctx, monster.x, monster.y, 6, 2.5);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  ctx.imageSmoothingEnabled = true;
  ctx.drawImage(frame, drawX, drawY, HD_MONSTER_UNITS, HD_MONSTER_UNITS);
  ctx.imageSmoothingEnabled = false;
  drawMarkers(ctx, monster, Math.round(monster.y - hd.bodyUnits));
}

/**
 * @param {CanvasRenderingContext2D} ctx world-space context
 * @param {import('../entities/monster.js').Monster} monster
 * @param {object} registry sprite registry
 */
export function drawMonster(ctx, monster, registry) {
  if (monster.isBoss) return drawBoss(ctx, monster, registry);
  const hd = registry.monstersHd?.[monster.type];
  if (hd) return drawMonsterHd(ctx, monster, hd);
  const sprite = registry.monsters[monster.type];
  if (!sprite) return;
  const frames = sprite.frames[monster.dir] || sprite.frames.down;
  const frame = pickFrame(frames, monster);
  const drawX = Math.round(monster.x - sprite.width / 2);
  const drawY = Math.round(monster.y - sprite.height);

  drawShadow(ctx, monster.x, monster.y, 5, 2);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  ctx.drawImage(monster.hitFlashTimer > 0 ? sprite.hitFlash[monster.dir] || sprite.hitFlash.down : frame, drawX, drawY);
  drawMarkers(ctx, monster, drawY);
}

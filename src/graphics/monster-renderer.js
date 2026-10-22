// Draws monsters from 4-direction sheets and the boss from idle/walk/hit strips.
import { drawShadow } from './player-renderer.js';

const FRAME_MS = 180;
const BOSS_FRAME_MS = 140;

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

/**
 * @param {CanvasRenderingContext2D} ctx world-space context
 * @param {import('../entities/monster.js').Monster} monster
 * @param {object} registry sprite registry
 */
export function drawMonster(ctx, monster, registry) {
  if (monster.isBoss) return drawBoss(ctx, monster, registry);
  const sprite = registry.monsters[monster.type];
  if (!sprite) return;
  const frames = sprite.frames[monster.dir] || sprite.frames.down;
  const idle = monster.ai.state === 'idle' || monster.ai.state === 'cooldown';
  const frame = idle ? frames[0] : frames[Math.floor(monster.animTimer / FRAME_MS) % frames.length];
  const drawX = Math.round(monster.x - sprite.width / 2);
  const drawY = Math.round(monster.y - sprite.height);

  drawShadow(ctx, monster.x, monster.y, 5, 2);
  if (monster.isTelegraphing) drawTelegraph(ctx, monster);
  ctx.drawImage(monster.hitFlashTimer > 0 ? sprite.hitFlash[monster.dir] || sprite.hitFlash.down : frame, drawX, drawY);

  if (monster.isTelegraphing) {
    ctx.fillStyle = '#ff3b3b';
    ctx.fillRect(Math.round(monster.x) - 1, drawY - 7, 2, 4);
    ctx.fillRect(Math.round(monster.x) - 1, drawY - 2, 2, 1);
  }
  if (monster.hp < monster.maxHp) drawMiniHpBar(ctx, monster, drawY);
}

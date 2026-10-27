// Draws the hero from the custom 24x24 animation set: death / dash / combo attacks / hurt /
// walk / idle, chosen from game state. NPCs share the shadow + characterFrame helpers.
import { HERO_FRAME_SIZE } from './sprite-registry.js';
import { createTintedSprite } from './pixel-sprite-factory.js';

const WALK_FRAME_MS = 100; // pack NPC walk (4 frames)
// One full hero step cycle (left step + right step). Spread over however many frames the class's walk
// sheet has, so the 4-key-pose walks and older 6-frame walks move at the same pace.
const HERO_WALK_CYCLE_MS = 560;
const IDLE_FRAME_MS = 200;
const DASH_FRAME_MS = 55;
const DEATH_FRAME_MS = 120;
const HIT_TINT_COLOR = 'rgba(255,70,70,0.75)';
const tintedFrameCache = new WeakMap(); // frame canvas -> red-tinted copy, built lazily

function tintedFrame(frame) {
  let tinted = tintedFrameCache.get(frame);
  if (!tinted) {
    tinted = createTintedSprite(frame, HIT_TINT_COLOR);
    tintedFrameCache.set(frame, tinted);
  }
  return tinted;
}

export function drawShadow(ctx, x, y, radiusX = 5, radiusY = 2) {
  ctx.fillStyle = 'rgba(0,0,0,0.3)';
  ctx.beginPath();
  ctx.ellipse(x, y, radiusX, radiusY, 0, 0, Math.PI * 2);
  ctx.fill();
}

/** Frame for a 16x16 NPC/pack character set: { idle, walk[], attack }. */
export function characterFrame(set, dir, { attacking = false, moving = false, animTimer = 0 } = {}) {
  const facing = set[dir] || set.down;
  if (attacking) return facing.attack;
  if (!moving) return facing.idle;
  return facing.walk[Math.floor(animTimer / WALK_FRAME_MS) % facing.walk.length];
}

const cycle = (frames, timer, ms) => frames[Math.floor(timer / ms) % frames.length];
const byProgress = (frames, progress) => frames[Math.min(frames.length - 1, Math.floor(progress * frames.length))];

/** Pick the hero frame from the custom animation set for the current state. */
function heroFrame(set, player) {
  const anims = set[player.dir] || set.down;
  if (player.dead) return byProgress(anims.death, Math.min(1, player.deathTimer / (DEATH_FRAME_MS * anims.death.length)));
  if (player.dash) return cycle(anims.dash, player.animTimer, DASH_FRAME_MS);
  if (player.combo.isAttacking) {
    const finisher = player.combo.step === player.combo.config.hits.length;
    const animationStep = finisher ? 3 : ((player.combo.step - 1) % 2) + 1;
    const attack = anims[`attack${animationStep}`] || anims.attack1;
    return byProgress(attack, player.combo.phaseProgress);
  }
  if (player.hitFlashTimer > 0) return anims.hurt[player.hitFlashTimer > 60 ? 0 : 1];
  if (player.moving) return cycle(anims.walk, player.animTimer, HERO_WALK_CYCLE_MS / anims.walk.length);
  return cycle(anims.idle, player.idleTimer || 0, IDLE_FRAME_MS);
}

/**
 * @param {CanvasRenderingContext2D} ctx world-space context
 * @param {import('../entities/player.js').Player} player
 * @param {object} registry sprite registry
 */
export function drawPlayer(ctx, player, registry) {
  // Blink while invulnerable after a hit (not while dying)
  if (!player.dead && player.invulnTimer > 0 && Math.floor(player.invulnTimer / 80) % 2 === 0 && player.hitFlashTimer <= 0) return;

  const frame = heroFrame(registry.heroes?.[player.classId] || registry.hero, player);
  // Dest size is always HERO_FRAME_SIZE world units -- HD frames (96px @ 4px/unit) and pixel
  // frames (24px @ 1px/unit) both render at the same world-space footprint, just crisper for HD.
  const size = HERO_FRAME_SIZE;
  const drawX = Math.round(player.x - size / 2);
  const drawY = Math.round(player.y - size);
  const smooth = (frame.pixelsPerUnit || 1) > 1;

  drawShadow(ctx, player.x, player.y, 6, 2.5);
  ctx.imageSmoothingEnabled = smooth;
  if (player.dash) {
    ctx.globalAlpha = 0.35;
    ctx.drawImage(frame, drawX - player.dash.dirX * 6, drawY - player.dash.dirY * 6, size, size);
    ctx.globalAlpha = 0.2;
    ctx.drawImage(frame, drawX - player.dash.dirX * 12, drawY - player.dash.dirY * 12, size, size);
    ctx.globalAlpha = 1;
  }
  ctx.drawImage(player.hitFlashTimer > 0 && !player.dead ? tintedFrame(frame) : frame, drawX, drawY, size, size);
  ctx.imageSmoothingEnabled = false; // restore the pixel-art default for whatever draws next in world space
}

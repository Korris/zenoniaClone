// Draws drops, projectiles, frame-animated FX strips, particles (world space) and floating text (screen space).
import { PIXEL_SCALE } from '../core/constants.js';
import { drawShadow } from './player-renderer.js';
import { drawText } from '../ui/pixel-text.js';
import { RARITIES } from '../data/item-definitions.js';

/** FX entries are either a single canvas or an array of frames (strip). */
function pickFrame(entry, progress) {
  if (!Array.isArray(entry)) return entry;
  return entry[Math.min(entry.length - 1, Math.floor(progress * entry.length))];
}

function drawRotated(ctx, sprite, x, y, rotation, scale = 1, alpha = 1) {
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.translate(Math.round(x), Math.round(y));
  ctx.rotate(rotation);
  ctx.scale(scale, scale);
  ctx.drawImage(sprite, -sprite.width / 2, -sprite.height / 2);
  ctx.restore();
}

/** Equipment ground drops: weapon sprites at 1x, armor icons at 0.6x, with a rarity glow pulse on rare+. */
function drawEquipmentDrop(ctx, drop, registry) {
  const item = drop.item;
  const sprite = registry.items[item.templateId];
  if (!sprite) return;
  const scale = item.slot === 'weapon' ? 1 : 0.6;
  const w = sprite.width * scale;
  const h = sprite.height * scale;
  if (item.rarity === 'rare' || item.rarity === 'epic') {
    const pulse = 0.35 + 0.35 * (0.5 + 0.5 * Math.sin(drop.age / 150));
    ctx.save();
    ctx.globalAlpha = pulse;
    ctx.fillStyle = RARITIES[item.rarity].color;
    ctx.beginPath();
    ctx.ellipse(drop.x, drop.y + drop.bobOffset - 1, w * 0.8, h * 0.4, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(sprite, Math.round(drop.x - w / 2), Math.round(drop.y - h + drop.bobOffset - 2), w, h);
}

export function drawDrops(ctx, drops, registry) {
  for (const drop of drops) {
    if (drop.lifeMs < 3000 && Math.floor(drop.lifeMs / 120) % 2 === 0) continue; // blink before vanish
    drawShadow(ctx, drop.x, drop.y, 3, 1.5);
    if (drop.kind === 'equipment') { drawEquipmentDrop(ctx, drop, registry); continue; }
    const sprite = registry.effects[drop.spriteKey];
    if (!sprite) continue;
    ctx.drawImage(sprite, Math.round(drop.x - sprite.width / 2), Math.round(drop.y - sprite.height + drop.bobOffset - 2));
  }
}

export function drawProjectiles(ctx, projectiles, registry) {
  for (const p of projectiles) {
    const entry = registry.effects[p.spriteKey];
    if (!entry) continue;
    const sprite = pickFrame(entry, 0.4);
    drawRotated(ctx, sprite, p.x, p.y - 6, p.rotation, p.owner === 'player' ? 0.7 : 0.4);
  }
}

export function drawEffects(ctx, effects, registry) {
  for (const e of effects) {
    const entry = registry.effects[e.spriteKey];
    if (!entry) continue;
    drawRotated(ctx, pickFrame(entry, e.progress), e.x, e.y, e.rotation, e.scale, e.alpha);
  }
}

export function drawParticles(ctx, particles) {
  ctx.fillStyle = '#ffe680';
  for (const p of particles) ctx.fillRect(Math.round(p.x), Math.round(p.y), 2, 2);
}

/** Floating text is drawn in SCREEN space so the font stays sharp. Call after resetting transform. */
export function drawFloatingTexts(ctx, texts, camera) {
  for (const t of texts) {
    const sx = (t.x - camera.renderX) * PIXEL_SCALE;
    const sy = (t.y - camera.renderY) * PIXEL_SCALE;
    ctx.globalAlpha = t.alpha;
    drawText(ctx, t.text, sx, sy, { size: t.big ? 22 : 14, color: t.color, align: 'center' });
    ctx.globalAlpha = 1;
  }
}

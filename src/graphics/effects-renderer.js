// Draws drops, projectiles, frame-animated FX strips, particles (world space) and floating text (screen space).
import { PIXEL_SCALE } from '../core/constants.js';
import { drawShadow } from './player-renderer.js';
import { drawText } from '../ui/pixel-text.js';
import { drawPotion } from './potion-renderer.js';
import { itemSprite, weaponIconIndex, groundGearSprite } from './weapon-icons.js';
import { equipmentTier } from '../data/item-definitions.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';
import { drawIceStorm } from './ice-storm-renderer.js';
import { drawUltimate } from './ultimate-renderer.js';
import { drawShadowClone } from './shadow-clone-renderer.js';
import { tintedSprite } from './tinted-sprite-cache.js';
import { drawDamageNumber } from './damage-number-renderer.js';

export const DROP_SIZE = 6.6;

/** FX entries are either a single canvas or an array of frames (strip). */
export function pickFrame(entry, progress) {
  if (!Array.isArray(entry)) return entry;
  return entry[Math.min(entry.length - 1, Math.floor(progress * entry.length))];
}

/** Draw centred and rotated; `scale` is in world units, so an HD sprite (pixelsPerUnit > 1) is shrunk
 * accordingly and drawn smoothed instead of pixel-crisp. */
function drawRotated(ctx, sprite, x, y, rotation, scale = 1, alpha = 1, flipY = false, additive = 0) {
  const ppu = sprite.pixelsPerUnit || 1;
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.imageSmoothingEnabled = ppu > 1;
  ctx.translate(Math.round(x), Math.round(y));
  ctx.rotate(rotation);
  ctx.scale(scale / ppu, (flipY ? -1 : 1) * scale / ppu);
  if (ppu > 1) drawSmoothImage(ctx, sprite, -sprite.width / 2, -sprite.height / 2);
  else ctx.drawImage(sprite, -sprite.width / 2, -sprite.height / 2);
  if (additive > 0) { // second pass adds light on the opaque pixels only (no blur, no halo)
    ctx.globalCompositeOperation = 'lighter';
    ctx.globalAlpha = alpha * additive;
    if (ppu > 1) drawSmoothImage(ctx, sprite, -sprite.width / 2, -sprite.height / 2);
    else ctx.drawImage(sprite, -sprite.width / 2, -sprite.height / 2);
  }
  ctx.restore();
}

/** Ground art for an equipment drop: `sprite` sets the footprint, `art` (ground variant when present) is what is drawn. */
export function equipmentDropArt(drop, registry, classId) {
  const item = drop.item;
  const sprite = itemSprite(registry, item, classId, true);
  if (!sprite) return null;
  const scale = DROP_SIZE / Math.max(sprite.width, sprite.height);
  const art = groundGearSprite(registry, item, classId) || (item.slot === 'weapon'
    ? registry.groundWeaponIcons?.[weaponIconIndex(item.itemLevel, item.classId || classId)]
    : registry.groundArmorIcons?.[item.slot]?.[equipmentTier(item.itemLevel)] || registry.groundItems?.[item.templateId]) || sprite;
  return { art, w: sprite.width * scale, h: sprite.height * scale };
}

/** Ground loot preserves the exact artwork used by the inventory. */
function drawEquipmentDrop(ctx, drop, registry, classId) {
  const found = equipmentDropArt(drop, registry, classId);
  if (!found) return;
  const { art, w, h } = found;
  ctx.save();
  // Snap to physical canvas pixels to prevent subpixel shimmer while the camera moves.
  const transform = ctx.getTransform?.();
  const sx = transform?.a || PIXEL_SCALE, sy = transform?.d || PIXEL_SCALE;
  const tx = transform?.e || 0, ty = transform?.f || 0;
  const x = (Math.round((drop.x - w / 2) * sx + tx) - tx) / sx;
  const y = (Math.round((drop.y + drop.bobOffset - 2 - h) * sy + ty) - ty) / sy;
  drawSmoothImage(ctx, art, x, y, w, h);
  ctx.restore();
}

/** Smooth beveled gold coin, using the same size as dropped potions. */
export function drawGoldCoin(ctx, x, y, size = DROP_SIZE) {
  ctx.save();
  ctx.translate(x, y);
  ctx.scale(size / 48, size / 48);
  const metal = ctx.createLinearGradient(-18, -22, 18, 22);
  metal.addColorStop(0, '#fff8ce');
  metal.addColorStop(0.3, '#ffdc72');
  metal.addColorStop(0.65, '#d99520');
  metal.addColorStop(1, '#88500e');
  ctx.beginPath(); ctx.ellipse(0, 0, 19, 22, 0, 0, Math.PI * 2);
  ctx.fillStyle = metal; ctx.fill();
  ctx.strokeStyle = '#694017'; ctx.lineWidth = 2; ctx.stroke();
  ctx.beginPath(); ctx.ellipse(0, 0, 14, 17, 0, 0, Math.PI * 2);
  ctx.strokeStyle = '#fff0a0'; ctx.lineWidth = 2; ctx.stroke();
  ctx.beginPath(); ctx.moveTo(0, -11); ctx.lineTo(7, 0);
  ctx.lineTo(0, 11); ctx.lineTo(-7, 0); ctx.closePath();
  ctx.fillStyle = '#ad6d16'; ctx.fill();
  ctx.strokeStyle = '#ffe6a0'; ctx.lineWidth = 1; ctx.stroke();
  ctx.beginPath(); ctx.ellipse(-7, -12, 3, 5, 0.6, 0, Math.PI * 2);
  ctx.fillStyle = '#fffbe7'; ctx.fill();
  ctx.restore();
}

export function drawDrops(ctx, drops, registry, classId = 'warrior') {
  for (const drop of drops) {
    ctx.save();
    try {
      drawShadow(ctx, drop.x, drop.y, 3, 1.5);
      if (drop.kind === 'coin') {
        drawGoldCoin(ctx, drop.x, drop.y + drop.bobOffset - DROP_SIZE / 2);
        continue;
      }
      if (drop.kind === 'hpPotion' || drop.kind === 'mpPotion' || drop.kind === 'upgradeStone') {
        drawPotion(ctx, drop.kind, drop.x, drop.y + drop.bobOffset - DROP_SIZE / 2, DROP_SIZE);
        continue;
      }
      if (drop.kind === 'equipment') { drawEquipmentDrop(ctx, drop, registry, classId); continue; }
      const sprite = registry.effects[drop.spriteKey];
      if (!sprite) continue;
      ctx.drawImage(sprite, Math.round(drop.x - sprite.width / 2), Math.round(drop.y - sprite.height + drop.bobOffset - 2));
    } finally {
      ctx.restore();
    }
  }
}

export function drawProjectiles(ctx, projectiles, registry) {
  for (const p of projectiles) {
    const entry = registry.effects[p.spriteKey];
    if (!entry) continue;
    const sprite = pickFrame(entry, 0.4);
    drawRotated(ctx, p.tint ? tintedSprite(sprite, p.tint) : sprite, p.x, p.y - 6, p.rotation, p.scale);
  }
}

export function drawEffects(ctx, effects, registry) {
  for (const e of effects) {
    if (e.kind === 'earthBreak' || e.kind === 'rocketBarrage') { drawUltimate(ctx, e, registry.effects[e.kind]); continue; }
    if (e.kind === 'iceStorm') { drawIceStorm(ctx, e, registry.effects.iceStorm); continue; }
    if (e.kind === 'healingZone' || e.kind === 'venomZone') continue; // drawn on the ground layer (world-renderer.js)
    if (e.kind === 'shadowClone') { drawShadowClone(ctx, e, registry); continue; }
    const entry = registry.effects[e.spriteKey];
    if (!entry) continue;
    const frame = pickFrame(entry, e.progress);
    drawRotated(ctx, e.tint ? tintedSprite(frame, e.tint) : frame, e.x, e.y, e.rotation, e.scale, e.alpha, e.flipY, e.additive);
  }
}

export function drawParticles(ctx, particles, registry) {
  const sprite = registry?.effects.hitParticle;
  ctx.save(); ctx.imageSmoothingEnabled = true;
  for (const p of particles) {
    const alpha = Math.min(1, Math.max(0, p.lifeMs / 120));
    if (sprite) {
      const size = 3.5;
      ctx.globalAlpha = alpha; drawSmoothImage(ctx, sprite, p.x - size / 2, p.y - size / 2, size, size);
    } else {
      ctx.fillStyle = '#ffe680'; ctx.beginPath(); ctx.arc(p.x, p.y, 0.8, 0, Math.PI * 2); ctx.fill();
    }
  }
  ctx.restore();
}

export function drawFloatingTexts(ctx, texts, camera) {
  for (const t of texts) {
    const sx = (t.x - camera.renderX) * PIXEL_SCALE;
    const sy = (t.y - camera.renderY) * PIXEL_SCALE;
    if (t.damageStyle) {
      drawDamageNumber(ctx, t, sx, sy);
      continue;
    }
    ctx.globalAlpha = t.alpha;
    drawText(ctx, t.text, sx, sy, { size: t.big ? 22 : 14, color: t.color, align: 'center' });
    ctx.globalAlpha = 1;
  }
}

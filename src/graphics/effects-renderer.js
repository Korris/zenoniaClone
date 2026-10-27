// Draws drops, projectiles, frame-animated FX strips, particles (world space) and floating text (screen space).
import { PIXEL_SCALE } from '../core/constants.js';
import { drawShadow } from './player-renderer.js';
import { drawText } from '../ui/pixel-text.js';
import { drawPotion } from './potion-renderer.js';
import { itemSprite, weaponIconIndex } from './weapon-icons.js';
import { equipmentTier } from '../data/item-definitions.js';
import { drawIceStorm } from './ice-storm-renderer.js';

const DROP_SIZE = 6.6;

/** FX entries are either a single canvas or an array of frames (strip). */
function pickFrame(entry, progress) {
  if (!Array.isArray(entry)) return entry;
  return entry[Math.min(entry.length - 1, Math.floor(progress * entry.length))];
}

/** Draw centred and rotated; `scale` is in world units, so an HD sprite (pixelsPerUnit > 1) is shrunk
 * accordingly and drawn smoothed instead of pixel-crisp. */
function drawRotated(ctx, sprite, x, y, rotation, scale = 1, alpha = 1) {
  const ppu = sprite.pixelsPerUnit || 1;
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.imageSmoothingEnabled = ppu > 1;
  ctx.translate(Math.round(x), Math.round(y));
  ctx.rotate(rotation);
  ctx.scale(scale / ppu, scale / ppu);
  ctx.drawImage(sprite, -sprite.width / 2, -sprite.height / 2);
  ctx.restore();
}

/** Ground loot preserves the exact artwork used by the inventory. */
function drawEquipmentDrop(ctx, drop, registry, classId) {
  const item = drop.item;
  const sprite = itemSprite(registry, item, classId, true);
  if (!sprite) return;
  const scale = DROP_SIZE / Math.max(sprite.width, sprite.height);
  const w = sprite.width * scale, h = sprite.height * scale;
  const groundSprite = item.slot === 'weapon'
    ? registry.groundWeaponIcons?.[weaponIconIndex(item.itemLevel, item.classId || classId)]
    : registry.groundArmorIcons?.[item.slot]?.[equipmentTier(item.itemLevel)] || registry.groundItems?.[item.templateId];
  ctx.save();
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  // Snap to physical canvas pixels to prevent subpixel shimmer while the camera moves.
  const transform = ctx.getTransform?.();
  const sx = transform?.a || PIXEL_SCALE, sy = transform?.d || PIXEL_SCALE;
  const tx = transform?.e || 0, ty = transform?.f || 0;
  const x = (Math.round((drop.x - w / 2) * sx + tx) - tx) / sx;
  const y = (Math.round((drop.y + drop.bobOffset - 2 - h) * sy + ty) - ty) / sy;
  ctx.drawImage(groundSprite || sprite, x, y, w, h);
  ctx.restore();
}

/** Smooth beveled gold coin, using the same size as dropped potions. */
function drawGoldCoin(ctx, x, y, size = DROP_SIZE) {
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
      if (drop.kind === 'hpPotion' || drop.kind === 'mpPotion') {
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
    drawRotated(ctx, sprite, p.x, p.y - 6, p.rotation, p.scale);
  }
}

export function drawEffects(ctx, effects, registry) {
  for (const e of effects) {
    if (e.kind === 'iceStorm') { drawIceStorm(ctx, e, registry.effects.iceStorm); continue; }
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
function drawDamageNumber(ctx, t, sx, sy) {
  const age = t.maxLifeMs - t.lifeMs;
  // Brief impact pop, then settle while the number rises and fades.
  const pop = age < 90 ? 0.8 + age / 90 * 0.4 : 1 + 0.2 * Math.max(0, 1 - (age - 90) / 140);
  const size = t.critical ? 40 : 32;
  ctx.save();
  ctx.globalAlpha = t.alpha;
  ctx.translate(sx, sy);
  ctx.scale(pop, pop);
  ctx.font = `italic 900 ${size}px "Arial Black", "Segoe UI", sans-serif`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.lineJoin = 'round';
  const gradient = ctx.createLinearGradient(0, -size / 2, 0, size / 2);
  const colors = t.damageStyle === 'incoming'
    ? ['#e5faff', '#65d4ff', '#1685ef', '#1546ad']
    : t.critical ? ['#fffbd6', '#ffec62', '#ffad25', '#e35716']
      : ['#fff1bd', '#ffc15a', '#fa5335', '#bf192d'];
  colors.forEach((color, i) => gradient.addColorStop(i / 3, color));
  ctx.strokeStyle = '#070f1e';
  ctx.lineWidth = 5;
  ctx.shadowColor = '#000a'; ctx.shadowBlur = 3; ctx.shadowOffsetY = 3;
  ctx.strokeText(t.text, 0, 0);
  ctx.shadowBlur = 0; ctx.shadowOffsetY = 0;
  ctx.fillStyle = gradient;
  ctx.fillText(t.text, 0, 0);
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

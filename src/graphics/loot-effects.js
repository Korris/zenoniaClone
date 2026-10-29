import { cropSprite } from './sprite-cell.js';
import { createTintedSprite } from './pixel-sprite-factory.js';
import { itemSprite } from './weapon-icons.js';
import { drawPotion } from './potion-renderer.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';

const COLORS = { common: '#eff6ff', uncommon: '#8fff8f', rare: '#76b7ff', epic: '#d888ff', gold: '#ffd16b' };
const STRENGTH = { common: 0.65, uncommon: 0.8, rare: 1, epic: 1, gold: 1 };
export const lootRarity = drop => drop.kind === 'equipment' ? drop.item.rarity : drop.kind === 'coin' ? 'gold' : 'common';

/** Two six-frame rows: idle beam and one-shot collection burst, both anchored at (0.5,0.8). */
export function buildLootEffects(sheet) {
  if (!sheet) return null;
  const rows = [0, 1].map(row => Array.from({ length: 6 }, (_, col) => {
    const x = Math.round(col * sheet.width / 6), y = Math.round(row * sheet.height / 2);
    return cropSprite(sheet, x, y, Math.round((col + 1) * sheet.width / 6) - x,
      Math.round((row + 1) * sheet.height / 2) - y);
  }));
  return Object.fromEntries(Object.entries(COLORS).map(([rarity, color]) => [rarity, {
    idle: rows[0].map(frame => createTintedSprite(frame, color)),
    pickup: rows[1].map(frame => createTintedSprite(frame, color)),
  }]));
}

function drawLight(ctx, frame, x, y, size, alpha) {
  if (!frame) return;
  ctx.save();
  ctx.globalAlpha *= alpha;
  ctx.globalCompositeOperation = 'lighter';
  drawSmoothImage(ctx, frame, x - size / 2, y - size * 0.8, size, size);
  // A second light pass keeps the narrow core legible over the bright daytime ground.
  ctx.globalAlpha *= 0.55;
  drawSmoothImage(ctx, frame, x - size / 2, y - size * 0.8, size, size);
  ctx.restore();
}

export function drawLootGroundFx(ctx, drop, registry) {
  const rarity = lootRarity(drop);
  const frames = registry.lootEffects?.[rarity]?.idle;
  if (!frames?.length) return;
  const frame = frames[Math.floor((drop.age || 0) / 150) % frames.length];
  drawLight(ctx, frame, drop.x, drop.y - 1, 24, STRENGTH[rarity] ?? 0.4);
}

/** Upper rays are emissive overlays; foreground grass must not hide the loot marker. */
export function drawLootBeams(ctx, drops, registry) {
  for (const drop of drops) {
    if (drop.dead) continue;
    ctx.save();
    ctx.beginPath(); ctx.rect(drop.x - 12, drop.y - 25, 24, 17); ctx.clip();
    drawLootGroundFx(ctx, drop, registry);
    // Keep a fine luminous core: the sheet supplies the halo and stars, while this
    // subpixel ray stays sharp when the HD light texture is drawn at game scale.
    if (registry.lootEffects) {
      const rarity = lootRarity(drop), color = COLORS[rarity] || COLORS.common;
      const top = drop.y - 24, base = drop.y - 4;
      const light = ctx.createLinearGradient(drop.x, top, drop.x, base);
      light.addColorStop(0, `${color}00`);
      light.addColorStop(0.2, `${color}44`);
      light.addColorStop(1, color);
      ctx.globalCompositeOperation = 'lighter';
      ctx.globalAlpha *= STRENGTH[rarity] ?? 0.65;
      ctx.fillStyle = light;
      ctx.beginPath(); ctx.moveTo(drop.x, top); ctx.lineTo(drop.x - 0.45, base);
      ctx.lineTo(drop.x + 0.45, base); ctx.closePath(); ctx.fill();
    }
    ctx.restore();
  }
}

export function drawLootPickupEffects(ctx, effects, registry, classId) {
  for (const effect of effects) {
    if (effect.dead) continue;
    const p = effect.progress;
    const rarity = lootRarity(effect.drop);
    const frames = registry.lootEffects?.[rarity]?.pickup;
    const t = Math.min(1, Math.max(0, (p - 0.12) / 0.55));
    const travel = 1 - (1 - t) ** 3;
    const x = effect.x + (effect.player.x - effect.x) * travel;
    const y = effect.y - 2 + (effect.player.y - 8 - (effect.y - 2)) * travel;
    if (frames?.length) drawLight(ctx, frames[Math.min(frames.length - 1, Math.floor(p * frames.length))],
      x, y, 26, 0.85 * (1 - p * 0.5));
    if (t >= 1) continue;
    // Use the very same chibi item art while it is absorbed, not a replacement icon.
    const drop = effect.drop;
    const size = 6.6 * (1 - t * 0.65);
    ctx.save(); ctx.globalAlpha *= 1 - t;
    if (drop.kind === 'equipment') {
      const sprite = itemSprite(registry, drop.item, classId, true);
      if (sprite) {
        const scale = size / Math.max(sprite.width, sprite.height);
        drawSmoothImage(ctx, sprite, x - sprite.width * scale / 2, y - sprite.height * scale, sprite.width * scale, sprite.height * scale);
      }
    } else if (drop.kind === 'hpPotion' || drop.kind === 'mpPotion' || drop.kind === 'upgradeStone') {
      drawPotion(ctx, drop.kind, x, y - size / 2, size);
    } else if (drop.kind === 'coin') {
      ctx.beginPath(); ctx.ellipse(x, y - size / 2, size * 0.38, size * 0.45, 0, 0, Math.PI * 2);
      ctx.fillStyle = '#ffd16b'; ctx.fill();
    }
    ctx.restore();
  }
}

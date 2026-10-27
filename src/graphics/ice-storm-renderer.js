import { cropSprite } from './sprite-cell.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';
import { ICE_STORM_CAST_MS, ICE_STORM_FALL_MS } from '../entities/ice-storm.js';
const IMPACT_VISUAL_SCALE = 0.75;

export function buildIceStormFrames(image) {
  if (!image) return null;
  return Object.fromEntries(['cast', 'meteor', 'impact', 'fade'].map((name, row) => [name,
    Array.from({ length: 6 }, (_, col) => cropSprite(image, col * 192, row * 192, 192, 192, false, 8))]));
}
function stamp(ctx, frames, progress, x, y, size, alpha = 1, anchor = 0.84) {
  if (!frames || progress < 0 || progress >= 1) return;
  const sprite = frames[Math.min(5, Math.floor(progress * 6))];
  ctx.globalAlpha = alpha;
  drawSmoothImage(ctx, sprite, x - size / 2, y - size * anchor, size, size);
}
export function drawIceStorm(ctx, storm, frames, ground = false) {
  if (!frames) return;
  ctx.save();
  if (ground) {
    stamp(ctx, frames.cast, storm.age / ICE_STORM_CAST_MS, storm.casterX, storm.casterY, 42, 1, 0.8);
  } else {
    for (const m of storm.meteors) {
      const falling = (storm.age - (m.impactMs - ICE_STORM_FALL_MS)) / ICE_STORM_FALL_MS;
      if (falling >= 0 && falling < 1) stamp(ctx, frames.meteor, falling,
        m.x - (1 - falling) * 32, m.y - (1 - falling) * 82, 58, 1, 0.92);
      const impact = (storm.age - m.impactMs) / 440;
      stamp(ctx, frames.impact, impact, m.x, m.y, m.size * IMPACT_VISUAL_SCALE);
      const fading = (storm.age - m.impactMs - 360) / 490;
      stamp(ctx, frames.fade, fading, m.x, m.y, m.size * 1.15 * IMPACT_VISUAL_SCALE, Math.max(0, 1 - fading), 0.5);
    }
  }
  ctx.restore();
}

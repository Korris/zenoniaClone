// Live glow for ENHANCED held gear in the PixiJS renderer (replaces the baked outline rings of
// held-weapon-glow-baker.js, which stay as the Canvas2D look and as the F8 "effects off" look). One pixi-filters
// GlowFilter per gear slot, coloured with the overlay manifest's class/tier `glow`, strength by enhance band
// (1 = +7 faint ... 4 = +13..15 strong) with a slow shine pulse on the top bands.
import { COMBAT_FEEL } from '../../data/combat-feel-config.js';

const hexToInt = (css) => parseInt(String(css).replace('#', '').slice(0, 6), 16);

export function createGearGlow(filters) {
  return filters ? new filters.GlowFilter({ distance: 4, outerStrength: 1, innerStrength: 0, color: 0xffffff, alpha: 1, quality: 0.25 }) : null;
}

/**
 * Update `filter` for an enhance band 1..4.
 * @param {number} scale device px per world unit (the filter works in screen pixels)
 * @param {boolean} bloomOn bloom toggle (F7): off => a thin rim only, so A/B shows what bloom adds
 */
export function tuneGearGlow(filter, band, color, scale, bloomOn, nowMs) {
  const cfg = COMBAT_FEEL.pixi.gearGlow;
  const b = Math.min(4, Math.max(1, band)) - 1;
  const pulse = 1 + cfg.pulse[b] * Math.sin((nowMs / cfg.pulseMs) * Math.PI * 2);
  const k = bloomOn ? 1 : 0.45;
  filter.distance = cfg.distance[b] * (scale / 4) * k;
  filter.outerStrength = cfg.outer[b] * pulse * k;
  filter.innerStrength = cfg.inner[b] * pulse;
  filter.alpha = cfg.alpha;
  const c = hexToInt(color || '#ffffff');
  if (filter.color !== c) filter.color = c;
}

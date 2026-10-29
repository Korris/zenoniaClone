// Weapon enhancement tuning (+0..+15). Pure data: edit here to rebalance without save migration.
export const MAX_ENHANCE = 15;
/** First level whose weapon art/glow switches from the base look to the enhanced look. */
export const ENHANCED_ART_MIN_LEVEL = 7;

/** Per-level % of base ATK by band: +1..6 5%, +7..9 8%, +10..12 12%, +13..15 18%. */
const PER_LEVEL_PCT = (level) => (level <= 6 ? 0.05 : level <= 9 ? 0.08 : level <= 12 ? 0.12 : 0.18);

/** Cumulative fraction of base ATK granted at each level (index = level). */
export const ENHANCE_ATK_PCT = Object.freeze(
  Array.from({ length: MAX_ENHANCE + 1 }, (_, level) => {
    let total = 0;
    for (let i = 1; i <= level; i += 1) total += PER_LEVEL_PCT(i);
    return Math.round(total * 1000) / 1000;
  }),
);

/** Visual bands (band index 1..4 = +7 / +9 / +11 / +13 and up). */
export const ENHANCE_VISUAL_BANDS = Object.freeze([
  Object.freeze({ band: 1, min: 7 }),
  Object.freeze({ band: 2, min: 9 }),
  Object.freeze({ band: 3, min: 11 }),
  Object.freeze({ band: 4, min: 13 }),
]);

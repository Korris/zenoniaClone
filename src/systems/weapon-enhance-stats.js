// Pure helpers deriving enhanced weapon values. Base `item.stats` is never mutated.
import { MAX_ENHANCE, ENHANCE_ATK_PCT, ENHANCE_VISUAL_BANDS } from '../data/weapon-enhance-config.js';
import { ITEM_TEMPLATES } from '../data/item-definitions.js';

/** Weapons and offhands share the enhance system. */
export function isEnhanceable(item) {
  return Boolean(item) && (item.slot === 'weapon' || item.slot === 'offhand');
}

/** Stat the enhance bonus scales: atk for weapons, the offhand template's `enhanceStat` otherwise. */
export function enhanceStatKey(item) {
  return item?.slot === 'offhand' ? ITEM_TEMPLATES[item.templateId]?.enhanceStat || 'atk' : 'atk';
}

/** Enhance level 0..MAX_ENHANCE; non-enhanceable items and garbage values count as 0. */
export function enhanceLevel(item) {
  if (!isEnhanceable(item) || !Number.isInteger(item.enhance)) return 0;
  return Math.max(0, Math.min(MAX_ENHANCE, item.enhance));
}

/** Offhand items get half of the weapon % table (shared success table / visual bands). */
export const OFFHAND_ENHANCE_SCALE = 0.5;
export function enhanceScale(item) {
  return item?.slot === 'offhand' ? OFFHAND_ENHANCE_SCALE : 1;
}

/**
 * Bonus for `level`: % of the base primary stat. The minimum per level scales with the base
 * (full +1/level only for base >= 8) so weak items can't gain more than their own base stat.
 * @param {number} pctScale multiplier on the % table (offhand = 0.5)
 */
export function enhanceAtkBonus(baseAtk, level, pctScale = 1) {
  const lvl = Math.max(0, Math.min(MAX_ENHANCE, Math.floor(level) || 0));
  if (lvl === 0) return 0;
  const base = baseAtk || 0;
  const floor = Math.ceil(lvl * Math.min(1, base / 8));
  return Math.max(floor, Math.round(base * ENHANCE_ATK_PCT[lvl] * pctScale));
}

/** Stats including the enhance bonus (new object; the item is untouched). */
export function effectiveItemStats(item) {
  const stats = { ...item.stats };
  const level = enhanceLevel(item);
  if (level > 0) {
    const key = enhanceStatKey(item);
    stats[key] = (stats[key] || 0) + enhanceAtkBonus(stats[key] || 0, level, enhanceScale(item));
  }
  return stats;
}

/** "Fine Sword +7"; +0 shows the plain name. Presentation only, never stored. */
export function displayItemName(item) {
  const level = enhanceLevel(item);
  return level > 0 ? `${item.name} +${level}` : item.name;
}

/** 0 = no enhanced look, 1..4 for the +7 / +9 / +11 / +13 thresholds. */
export function enhanceVisualBand(item) {
  const level = enhanceLevel(item);
  let band = 0;
  for (const b of ENHANCE_VISUAL_BANDS) if (level >= b.min) band = b.band;
  return band;
}

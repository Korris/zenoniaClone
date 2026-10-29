// Enhance UI logic for the Inventory panel: pure preview view-model, drawing and the click action.
import { bagText as drawText, bagButton } from './bag-widgets.js';
import { MAX_ENHANCE } from '../data/weapon-enhance-config.js';
import { enhanceLevel, enhanceAtkBonus, displayItemName, isEnhanceable, enhanceStatKey, enhanceScale } from '../systems/weapon-enhance-stats.js';
import { STONE_UPGRADE_METHOD } from '../systems/weapon-upgrade-method-stone.js';
import { attemptEnhance } from '../systems/weapon-enhance-system.js';

const STAT_LABELS = { atk: 'ATK', def: 'DEF', maxHp: 'Max HP', maxMp: 'Max MP' };
const REASON_TEXT = { 'not-weapon': 'Weapons only', 'max-level': 'Max +15', 'no-cost': 'Need Upgrade Stone', 'not-owned': 'Item not owned' };

/** Pure view-model for the detail panel / button state of `item` (null item => disabled). */
export function enhancePreview(player, item, method = STONE_UPGRADE_METHOD) {
  const owned = player.materials?.upgradeStone || 0;
  const base = { can: false, reason: '', from: 0, to: 0, chance: 0, atkFrom: 0, atkTo: 0, statLabel: 'ATK', costLabel: method.costLabel(0), owned };
  if (!isEnhanceable(item)) return { ...base, reason: REASON_TEXT['not-weapon'] };
  const key = enhanceStatKey(item), from = enhanceLevel(item), baseAtk = item.stats[key] || 0, scale = enhanceScale(item);
  base.statLabel = STAT_LABELS[key] || key;
  const atkFrom = baseAtk + enhanceAtkBonus(baseAtk, from, scale);
  if (from >= MAX_ENHANCE) return { ...base, from, to: from, atkFrom, atkTo: atkFrom, reason: REASON_TEXT['max-level'] };
  const to = from + 1;
  const view = { ...base, from, to, chance: method.successChance(from), atkFrom, atkTo: baseAtk + enhanceAtkBonus(baseAtk, to, scale), costLabel: method.costLabel(from) };
  return method.canPay(player, from) ? { ...view, can: true } : { ...view, reason: REASON_TEXT['no-cost'] };
}

/** Detail lines (replaces the set blurb for weapons) at x, y; lines are 21px apart. */
export function drawEnhancePreview(ctx, preview, x, y) {
  if (preview.to === preview.from) { drawText(ctx, `Enhance ${preview.reason || ''}`, x, y, { size: 13, color: '#f2deae' }); return; }
  const lines = [
    [`Enhance  +${preview.from} -> +${preview.to}   Success ${Math.round(preview.chance * 100)}%`, '#f2deae'],
    [`${preview.statLabel} ${preview.atkFrom} -> ${preview.atkTo}`, '#8fff8f'],
    [`Cost: ${preview.costLabel} (have ${preview.owned})`, preview.can ? '#c7e2f2' : '#ff8f8f'],
  ];
  lines.forEach(([text, color], i) => drawText(ctx, text, x, y + i * 21, { size: 13, color }));
}

/** Gold button when usable, 45% alpha otherwise (click still explains why). */
export function drawEnhanceButton(ctx, preview, bounds) {
  ctx.save();
  if (!preview.can) ctx.globalAlpha = 0.45;
  bagButton(ctx, 'Enhance', bounds.x, bounds.y, bounds.w, bounds.h, 'gold');
  ctx.restore();
}

/** Runs one attempt and flashes the outcome on the panel. Returns the attemptEnhance result. */
export function runEnhance(panel, player, item) {
  const result = attemptEnhance(player, item, { roll: panel.enhanceRoll });
  if (!result.ok) panel.flash(REASON_TEXT[result.reason] || 'Cannot enhance');
  else if (result.success) panel.flash(`Success! ${displayItemName(item)}`);
  else panel.flash(`Failed... stone lost (+${result.level} kept)`);
  return result;
}

// Pure layout + formatting helpers for the inventory panel (mirrors skill-tree-layout.js).
// No DOM/canvas calls here; inventory-panel.js does the actual drawing.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { EQUIPMENT_SLOTS } from '../data/item-definitions.js';

export const PANEL_W = 640;
export const PANEL_H = 568;
export const PANEL_X = CANVAS_WIDTH - PANEL_W - 16;
export const PANEL_Y = (CANVAS_HEIGHT - PANEL_H) / 2 - 2;
export const SLOT_SIZE = 82;
export const EQUIP_SLOT_SIZE = 60;
export const GRID_COLS = 3;
export const GRID_ROWS = 4;
export const INFO_BOX_Y = 154;
export const INFO_BOX_H = 426;
export const PORTRAIT = { x: 510, y: 342 };
export const EQUIP_SLOT_ORDER = [...EQUIPMENT_SLOTS];
export const BAG_ACTIONS = Object.freeze({
  primary: { x: 612, y: 528, w: 140, h: 32 },
  secondary: { x: 766, y: 528, w: 146, h: 32 },
  sort: { x: 332, y: 528, w: 76, h: 32 },
  page: { x: 420, y: 528, w: 76, h: 32 },
  unequip: { x: 748, y: 528, w: 160, h: 32 },
});
export function equipSlotPosition(index) {
  const left = [0, 1, 2, 3].includes(index);
  return { x: left ? 360 : 658, y: 202 + (left ? index : 0) * 84 };
}
export function gridSlotPosition(index) {
  index %= GRID_COLS * GRID_ROWS;
  return { x: 322 + (index % GRID_COLS) * 90 + SLOT_SIZE / 2, y: 164 + Math.floor(index / GRID_COLS) * 90 + SLOT_SIZE / 2 };
}

export function slotLabel(slot) {
  return slot.charAt(0).toUpperCase() + slot.slice(1);
}

const STAT_LABELS = { atk: 'ATK', def: 'DEF', maxHp: 'Max HP', speedMult: 'Speed', critChance: 'Crit' };
const PERCENT_STATS = new Set(['speedMult', 'critChance']);

export function formatStatValue(stat, value) {
  if (PERCENT_STATS.has(stat)) return `${Math.round(value * 1000) / 10}%`;
  return `${Math.round(value)}`;
}

/** Absolute stat lines for the selected item, e.g. ["ATK 7", "Crit 4%"]. */
export function describeItemStats(item) {
  return Object.entries(item.stats).map(([stat, value]) => `${STAT_LABELS[stat] || stat} ${formatStatValue(stat, value)}`);
}

/** Per-stat deltas vs. the currently equipped item in the same slot (or none, if nothing equipped). */
export function compareToEquipped(item, equipped) {
  return Object.entries(item.stats).map(([stat, value]) => {
    const prev = equipped?.stats?.[stat] || 0;
    const delta = value - prev;
    const sign = delta > 0 ? '+' : delta < 0 ? '-' : '';
    const magnitude = formatStatValue(stat, Math.abs(delta));
    const color = delta > 0 ? '#8fff8f' : delta < 0 ? '#ff7070' : '#cfcfe0';
    return { text: `${STAT_LABELS[stat] || stat} ${sign}${magnitude}`, color };
  });
}

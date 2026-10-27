// Pure layout + formatting helpers for the inventory panel (mirrors skill-tree-layout.js).
// No DOM/canvas calls here; inventory-panel.js does the actual drawing.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { EQUIPMENT_SLOTS } from '../data/item-definitions.js';

export const PANEL_W = 860;
export const PANEL_H = 568;
export const PANEL_X = (CANVAS_WIDTH - PANEL_W) / 2;
export const PANEL_Y = (CANVAS_HEIGHT - PANEL_H) / 2;
export const SLOT_SIZE = 64;
export const EQUIP_SLOT_SIZE = 44;
export const GRID_COLS = 5;
export const GRID_ROWS = 4;
export const INFO_BOX_Y = PANEL_Y + 440;
export const INFO_BOX_H = 94;

const LEFT_X = PANEL_X + 75;
const EQUIP_TOP = PANEL_Y + 176;
const EQUIP_GAP = 60;
const GRID_LEFT = PANEL_X + 210;
const GRID_TOP = PANEL_Y + 154;
const GRID_GAP = 10;

export const PORTRAIT = { x: LEFT_X, y: PANEL_Y + 154 };
export const EQUIP_SLOT_ORDER = [...EQUIPMENT_SLOTS]; // ['weapon','helmet','armor','gloves','boots']

export function equipSlotPosition(index) {
  return { x: LEFT_X, y: EQUIP_TOP + index * EQUIP_GAP };
}

export function gridSlotPosition(index) {
  const col = index % GRID_COLS;
  const row = Math.floor(index / GRID_COLS);
  return {
    x: GRID_LEFT + col * (SLOT_SIZE + GRID_GAP) + SLOT_SIZE / 2,
    y: GRID_TOP + row * (SLOT_SIZE + GRID_GAP) + SLOT_SIZE / 2,
  };
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

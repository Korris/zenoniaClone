// Bag + equipment panel (mirrors skill-tree-panel.js). Keyboard: Arrows move selection across
// the equipment column and the 5x4 bag grid, Enter equips/unequips, X drops the selected bag
// item, B/Esc/Tab close. Mouse: click selects, double-click equips/unequips.
import { drawText, drawPanel, drawOverlay, drawScaledSprite } from './pixel-text.js';
import { RARITIES } from '../data/item-definitions.js';
import { equipItem, unequipItem, removeFromInventory, INVENTORY_SIZE } from '../systems/equipment-system.js';
import {
  PANEL_X, PANEL_Y, PANEL_W, PANEL_H, SLOT_SIZE, EQUIP_SLOT_SIZE, GRID_COLS, GRID_ROWS, INFO_BOX_Y, INFO_BOX_H,
  EQUIP_SLOT_ORDER, PORTRAIT, equipSlotPosition, gridSlotPosition, slotLabel, describeItemStats, compareToEquipped,
} from './inventory-layout.js';

const DOUBLE_CLICK_MS = 350;
const EMPTY_BORDER = '#3a3a4c';
const LOCKED_BORDER = '#555566';
const SLOT_ABBR = { weapon: 'WPN', helmet: 'HELM', armor: 'ARMR', gloves: 'GLV', boots: 'BOOT' };

export class InventoryPanel {
  constructor() {
    this.selection = { area: 'bag', index: 0 };
    this.message = '';
    this.messageTimer = 0;
    this.lastClick = null; // { area, index, time }
  }

  /** @returns {{close:boolean}} */
  handleInput(input, player) {
    const result = { close: false };
    if (input.wasPressed('inventory') || input.wasPressed('pause') || input.wasPressed('skillPanel')) {
      result.close = true;
      return result;
    }
    this.navigate(input);
    for (const click of input.takeClicks()) {
      const hit = this.hitTest(click.x, click.y);
      if (!hit) continue;
      const now = performance.now();
      const isDouble = this.lastClick && this.lastClick.area === hit.area && this.lastClick.index === hit.index
        && now - this.lastClick.time < DOUBLE_CLICK_MS;
      this.selection = hit;
      this.lastClick = { ...hit, time: now };
      if (isDouble) this.activate(player);
    }
    if (input.wasPressed('confirm')) this.activate(player);
    if (input.wasPressed('dropItem')) this.dropSelected(player);
    this.messageTimer -= 16;
    return result;
  }

  navigate(input) {
    const { area, index } = this.selection;
    if (area === 'equipment') {
      if (input.wasPressed('moveUp')) this.selection.index = (index + EQUIP_SLOT_ORDER.length - 1) % EQUIP_SLOT_ORDER.length;
      if (input.wasPressed('moveDown')) this.selection.index = (index + 1) % EQUIP_SLOT_ORDER.length;
      if (input.wasPressed('moveRight')) this.selection = { area: 'bag', index: Math.min(index, GRID_ROWS - 1) * GRID_COLS };
      return;
    }
    let col = index % GRID_COLS;
    let row = Math.floor(index / GRID_COLS);
    if (input.wasPressed('moveLeft')) {
      if (col === 0) { this.selection = { area: 'equipment', index: Math.min(row, EQUIP_SLOT_ORDER.length - 1) }; return; }
      col -= 1;
    }
    if (input.wasPressed('moveRight')) col = Math.min(GRID_COLS - 1, col + 1);
    if (input.wasPressed('moveUp')) row = (row + GRID_ROWS - 1) % GRID_ROWS;
    if (input.wasPressed('moveDown')) row = (row + 1) % GRID_ROWS;
    this.selection = { area: 'bag', index: row * GRID_COLS + col };
  }

  hitTest(x, y) {
    const equipHalf = EQUIP_SLOT_SIZE / 2;
    for (let i = 0; i < EQUIP_SLOT_ORDER.length; i++) {
      const pos = equipSlotPosition(i);
      if (Math.abs(x - pos.x) <= equipHalf && Math.abs(y - pos.y) <= equipHalf) return { area: 'equipment', index: i };
    }
    const half = SLOT_SIZE / 2;
    for (let i = 0; i < INVENTORY_SIZE; i++) {
      const pos = gridSlotPosition(i);
      if (Math.abs(x - pos.x) <= half && Math.abs(y - pos.y) <= half) return { area: 'bag', index: i };
    }
    return null;
  }

  selectedItem(player) {
    const { area, index } = this.selection;
    return area === 'bag' ? player.inventory[index] || null : player.equipment[EQUIP_SLOT_ORDER[index]];
  }

  activate(player) {
    const { area, index } = this.selection;
    if (area === 'bag') {
      const item = player.inventory[index];
      if (!item) return;
      const outcome = equipItem(player, item);
      if (outcome.ok) player.recomputeStats();
      this.flash(outcome.ok ? `Equipped ${item.name}` : outcome.reason);
    } else {
      const outcome = unequipItem(player, EQUIP_SLOT_ORDER[index]);
      if (outcome.ok) player.recomputeStats();
      this.flash(outcome.ok ? 'Unequipped' : outcome.reason);
    }
  }

  dropSelected(player) {
    if (this.selection.area !== 'bag') return;
    const item = player.inventory[this.selection.index];
    if (!item) return;
    removeFromInventory(player, item.uid);
    this.flash(`Dropped ${item.name}`);
  }

  flash(text) {
    this.message = text;
    this.messageTimer = 1500;
  }

  render(ctx, player, registry) {
    drawOverlay(ctx, 0.65);
    drawPanel(ctx, PANEL_X, PANEL_Y, PANEL_W, PANEL_H);
    drawText(ctx, 'INVENTORY', PANEL_X + 16, PANEL_Y + 12, { size: 16, color: '#ffe680' });
    drawText(ctx, `${player.inventory.length}/${INVENTORY_SIZE}`, PANEL_X + PANEL_W - 16, PANEL_Y + 14, { size: 14, align: 'right', color: '#cfcfe0' });
    this.drawPortrait(ctx, registry, player);
    EQUIP_SLOT_ORDER.forEach((slot, i) => this.drawEquipSlot(ctx, player, registry, slot, i));
    for (let i = 0; i < INVENTORY_SIZE; i++) this.drawGridSlot(ctx, player, registry, i);
    this.drawInfoBox(ctx, player);
  }

  drawPortrait(ctx, registry, player) {
    const { x, y } = PORTRAIT;
    drawPanel(ctx, x - 40, y - 40, 80, 80, { fill: '#1b1630', border: '#d9b45a' });
    const face = registry.faces.heroByClass?.[player.classId] || registry.faces.hero;
    ctx.imageSmoothingEnabled = Boolean(registry.faces.heroIsHd); // crisp upscale for the HD faceset only
    ctx.drawImage(face, x - face.width, y - face.height, face.width * 2, face.height * 2);
    ctx.imageSmoothingEnabled = false;
  }

  drawEquipSlot(ctx, player, registry, slot, index) {
    const { x, y } = equipSlotPosition(index);
    const half = EQUIP_SLOT_SIZE / 2;
    const item = player.equipment[slot];
    const selected = this.selection.area === 'equipment' && this.selection.index === index;
    const border = selected ? '#ffffff' : item ? RARITIES[item.rarity].color : LOCKED_BORDER;
    drawPanel(ctx, x - half, y - half, EQUIP_SLOT_SIZE, EQUIP_SLOT_SIZE, { fill: 'rgba(30,26,54,0.92)', border });
    if (item) this.drawItemIcon(ctx, registry, item, x, y - 4, 0.8);
    drawText(ctx, SLOT_ABBR[slot], x, y + half - 12, { size: 9, align: 'center', color: '#9a9ab0' });
  }

  drawGridSlot(ctx, player, registry, index) {
    const { x, y } = gridSlotPosition(index);
    const half = SLOT_SIZE / 2;
    const item = player.inventory[index];
    const selected = this.selection.area === 'bag' && this.selection.index === index;
    const border = selected ? '#ffffff' : item ? RARITIES[item.rarity].color : EMPTY_BORDER;
    drawPanel(ctx, x - half, y - half, SLOT_SIZE, SLOT_SIZE, { fill: 'rgba(20,18,36,0.9)', border });
    if (item) this.drawItemIcon(ctx, registry, item, x, y);
  }

  /** Weapons render small (base scale 3), gear icons are 24x24 (base scale 2); `factor` shrinks both for the smaller equipment-column slots. */
  drawItemIcon(ctx, registry, item, cx, cy, factor = 1) {
    const sprite = registry.items[item.templateId];
    if (!sprite) return;
    const scale = (item.slot === 'weapon' ? 3 : 2) * factor;
    drawScaledSprite(ctx, sprite, cx - (sprite.width * scale) / 2, cy - (sprite.height * scale) / 2, scale);
  }

  drawInfoBox(ctx, player) {
    drawPanel(ctx, PANEL_X + 16, INFO_BOX_Y, PANEL_W - 32, INFO_BOX_H, { fill: 'rgba(8,8,18,0.9)' });
    const item = this.selectedItem(player);
    if (!item) {
      drawText(ctx, 'Empty', PANEL_X + 28, INFO_BOX_Y + 8, { size: 14, color: '#888' });
    } else {
      drawText(ctx, item.name, PANEL_X + 28, INFO_BOX_Y + 8, { size: 15, color: RARITIES[item.rarity].color });
      drawText(ctx, `${slotLabel(item.slot)}   Lv.${item.itemLevel}`, PANEL_X + 28, INFO_BOX_Y + 27, { size: 12, color: '#b8b8cc' });
      drawText(ctx, describeItemStats(item).join('   '), PANEL_X + 28, INFO_BOX_Y + 44, { size: 12, color: '#8fc3ff' });
      if (this.selection.area === 'bag') {
        const equipped = player.equipment[item.slot];
        this.drawDeltaLine(ctx, PANEL_X + 28, INFO_BOX_Y + 61, compareToEquipped(item, equipped));
      }
    }
    const footer = this.messageTimer > 0 ? this.message : 'Arrows select   Enter equip/unequip   X drop   B/Esc/Tab close';
    drawText(ctx, footer, PANEL_X + PANEL_W / 2, INFO_BOX_Y + INFO_BOX_H + 10, { size: 12, align: 'center', color: this.messageTimer > 0 ? '#ffe680' : '#9a9ab0' });
  }

  /** Draws "vs. equipped" stat deltas with per-stat green/red color (drawText only supports one color). */
  drawDeltaLine(ctx, x, y, deltas) {
    ctx.font = 'bold 12px "Courier New", Courier, monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'top';
    let cx = x;
    for (const d of deltas) {
      ctx.fillStyle = 'rgba(0,0,0,0.8)';
      ctx.fillText(d.text, cx + 2, y + 2);
      ctx.fillStyle = d.color;
      ctx.fillText(d.text, cx, y);
      cx += ctx.measureText(`${d.text}   `).width;
    }
  }
}

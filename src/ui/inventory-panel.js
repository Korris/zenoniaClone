// Bag + equipment panel (mirrors skill-tree-panel.js). Keyboard: Arrows move selection across
// the equipment column and the 5x4 bag grid, Enter equips/unequips, X drops the selected bag
// item, B/Esc close   Tab skills. Mouse: click selects, double-click equips/unequips.
import { drawText, drawPanel } from './pixel-text.js';
import { drawCharacterMenu } from './character-menu.js';
import { RARITIES } from '../data/item-definitions.js';
import { drawPotion } from '../graphics/potion-renderer.js';
import { itemSprite } from '../graphics/weapon-icons.js';
import { usePotion } from '../systems/loot-system.js';
import { equipItem, unequipItem, removeFromInventory, INVENTORY_SIZE } from '../systems/equipment-system.js';
import {
  PANEL_X, PANEL_Y, SLOT_SIZE, EQUIP_SLOT_SIZE, GRID_COLS, GRID_ROWS,
  EQUIP_SLOT_ORDER, equipSlotPosition, gridSlotPosition, slotLabel, describeItemStats, compareToEquipped,
} from './inventory-layout.js';

const DOUBLE_CLICK_MS = 350;
const EMPTY_BORDER = '#3a3a4c';
const LOCKED_BORDER = '#555566';

export class InventoryPanel {
  constructor() {
    this.mode = 'Inventory';
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
    for (const kind of ['hpPotion', 'mpPotion']) {
      if (input.wasPressed(kind)) this.flash(usePotion(player, kind) ? 'Restored 30%' : 'No potion or resource full');
    }
    for (const click of input.takeClicks()) {
      if (click.y >= 514 && click.y <= 550 && click.x >= 660 && click.x <= 888) {
        if (click.x < 779) this.activate(player); else this.dropSelected(player);
        continue;
      }
      const potion = this.mode === 'Inventory' && click.y >= 495 && click.y <= 548
        ? [0, 1].find(i => click.x >= 265 + i * 110 && click.x <= 365 + i * 110) : undefined;
      if (potion !== undefined && potion !== false) {
        this.flash(usePotion(player, potion === 0 ? 'hpPotion' : 'mpPotion') ? 'Restored 30%' : 'No potion or resource full');
        continue;
      }
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
      if (col === 0 && this.mode === 'Equipment') { this.selection = { area: 'equipment', index: Math.min(row, EQUIP_SLOT_ORDER.length - 1) }; return; }
      col = Math.max(0, col - 1);
    }
    if (input.wasPressed('moveRight')) col = Math.min(GRID_COLS - 1, col + 1);
    if (input.wasPressed('moveUp')) row = (row + GRID_ROWS - 1) % GRID_ROWS;
    if (input.wasPressed('moveDown')) row = (row + 1) % GRID_ROWS;
    this.selection = { area: 'bag', index: row * GRID_COLS + col };
  }

  hitTest(x, y) {
    const equipHalf = this.mode === 'Equipment' ? EQUIP_SLOT_SIZE / 2 : -1;
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
    drawCharacterMenu(ctx, player, this.mode);
    drawPanel(ctx, 70, 149, 176, 409, { fill: '#111e2dee', border: '#35495d' });
    drawPanel(ctx, 254, 149, 388, 409, { fill: '#0d1928dd', border: '#35495d' });
    drawText(ctx, this.mode === 'Equipment' ? 'YOUR LOADOUT' : 'HERO', 158, 161, { size: 11, color: '#9db3c9', align: 'center', shadow: false });
    drawText(ctx, `Items (${player.inventory.length} / ${INVENTORY_SIZE})`, 270, 163, { size: 13, color: '#bccbda', shadow: false });
    if (this.mode === 'Equipment') {
      EQUIP_SLOT_ORDER.forEach((slot, i) => this.drawEquipSlot(ctx, player, registry, slot, i));
      drawText(ctx, 'Select gear to unequip', 158, 535, { size: 11, align: 'center', color: '#819bb3' });
    } else this.drawHero(ctx, registry, player);
    for (let i = 0; i < INVENTORY_SIZE; i++) this.drawGridSlot(ctx, player, registry, i);
    if (this.mode === 'Inventory') {
      ['hpPotion', 'mpPotion'].forEach((kind, i) => {
        const x = 265 + i * 110;
        drawPanel(ctx, x, 495, 100, 53, { fill: '#162b3d', border: i ? '#3a6788' : '#794455' });
        drawPotion(ctx, kind, x + 17, 520, 14);
        drawText(ctx, `${i ? 'MP' : 'HP'}  x${player.potions?.[kind] ?? 0}`, x + 34, 506, { size: 12, color: i ? '#9bddff' : '#ffb8c8' });
        drawText(ctx, `[${i + 1}] Restore 30%`, x + 34, 525, { size: 9, color: '#a1b6c7', bold: false });
      });
    }
    this.drawInfoBox(ctx, player, registry);
    drawText(ctx, this.messageTimer > 0 ? this.message : 'B  Close     Enter  Equip / Unequip     X  Discard     1 / 2  Potion', 480, 578, { size: 12, align: 'center', color: this.messageTimer > 0 ? '#ebd195' : '#93a8bc', shadow: false });
  }

  drawHero(ctx, registry, player) {
    const face = registry.faces.heroByClass?.[player.classId] || registry.faces.hero;
    ctx.save();
    ctx.beginPath(); ctx.arc(158, 237, 51, 0, Math.PI * 2);
    ctx.fillStyle = '#22394b'; ctx.fill(); ctx.strokeStyle = '#a3bac9'; ctx.lineWidth = 2; ctx.stroke(); ctx.clip();
    ctx.imageSmoothingEnabled = true; ctx.drawImage(face, 107, 186, 102, 102); ctx.restore();
    const frame = (registry.heroes?.[player.classId] || registry.hero)?.down?.idle?.[0];
    if (frame) {
      ctx.save(); ctx.imageSmoothingEnabled = true;
      ctx.drawImage(frame, 83, 290, 150, 150); ctx.restore();
    }
    const stats = [['ATK', player.atk], ['DEF', player.def], ['CRIT', `${(player.passiveMods.critChance * 100).toFixed(1)}%`]];
    stats.forEach(([name, value], i) => {
      const y = 461 + i * 29;
      ctx.fillStyle = '#304151'; ctx.fillRect(88, y - 9, 140, 1);
      drawText(ctx, name, 90, y, { size: 12, color: '#adbfce', bold: false });
      drawText(ctx, `${value}`, 226, y, { size: 13, align: 'right', color: '#efd594' });
    });
  }

  drawEquipSlot(ctx, player, registry, slot, index) {
    const { x, y } = equipSlotPosition(index);
    const half = EQUIP_SLOT_SIZE / 2;
    const item = player.equipment[slot];
    const selected = this.selection.area === 'equipment' && this.selection.index === index;
    const border = selected ? '#8edfff' : item ? RARITIES[item.rarity].color : LOCKED_BORDER;
    drawPanel(ctx, x - half, y - half, EQUIP_SLOT_SIZE, EQUIP_SLOT_SIZE, { fill: '#192b3e', border });
    if (item) this.drawItemIcon(ctx, registry, item, x, y - 4, 0.8, player.classId);
    drawText(ctx, slotLabel(slot), x + half + 10, y - 6, { size: 12, color: '#b1c5d6' });
  }

  drawGridSlot(ctx, player, registry, index) {
    const { x, y } = gridSlotPosition(index);
    const half = SLOT_SIZE / 2;
    const item = player.inventory[index];
    const selected = this.selection.area === 'bag' && this.selection.index === index;
    const border = selected ? '#8edfff' : item ? RARITIES[item.rarity].color : EMPTY_BORDER;
    drawPanel(ctx, x - half, y - half, SLOT_SIZE, SLOT_SIZE, { fill: '#122235', border });
    if (item) this.drawItemIcon(ctx, registry, item, x, y, 1, player.classId);
  }

  drawItemIcon(ctx, registry, item, cx, cy, factor = 1, classId = 'warrior') {
    const sprite = itemSprite(registry, item, classId, true);
    if (!sprite) return;
    const scale = Math.min(44 / sprite.width, 44 / sprite.height) * factor;
    ctx.save(); ctx.imageSmoothingEnabled = true;
    ctx.drawImage(sprite, cx - sprite.width * scale / 2, cy - sprite.height * scale / 2, sprite.width * scale, sprite.height * scale);
    ctx.restore();
  }

  drawInfoBox(ctx, player, registry) {
    drawPanel(ctx, 652, 149, 238, 409, { fill: '#101e2fee', border: '#4b5a6a' });
    const item = this.selectedItem(player);
    if (!item) {
      drawText(ctx, 'ITEM DETAILS', 771, 175, { size: 11, align: 'center', color: '#829bb1' });
      drawText(ctx, 'Select an item', 771, 325, { size: 18, align: 'center', color: '#d3e1ec' });
      drawText(ctx, 'View its stats and equipment bonuses.', 771, 354, { size: 11, align: 'center', color: '#829bb1', bold: false });
      return;
    }
    const glow = ctx.createRadialGradient(771, 242, 0, 771, 242, 85);
    glow.addColorStop(0, '#38608266'); glow.addColorStop(1, '#16283a00');
    ctx.fillStyle = glow; ctx.fillRect(669, 161, 204, 162);
    this.drawItemIcon(ctx, registry, item, 771, 242, 2.7, player.classId);
    ctx.save(); ctx.beginPath(); ctx.rect(666, 318, 209, 27); ctx.clip();
    drawText(ctx, item.name, 669, 322, { size: 18, shadow: false }); ctx.restore();
    drawText(ctx, `${item.rarity.charAt(0).toUpperCase() + item.rarity.slice(1)}  /  Level ${item.itemLevel}`, 669, 352, { size: 12, color: RARITIES[item.rarity].color });
    drawText(ctx, slotLabel(item.slot), 669, 375, { size: 11, color: '#819db4', bold: false });
    describeItemStats(item).forEach((line, i) => drawText(ctx, line, 669, 402 + i * 23, { size: 14, color: '#d3e4ef' }));
    if (this.selection.area === 'bag') {
      const deltas = compareToEquipped(item, player.equipment[item.slot]);
      deltas.slice(0, 2).forEach((d, i) => drawText(ctx, `${d.text} vs equipped`, 669, 470 + i * 17, { size: 11, color: d.color }));
    }
    const button = ctx.createLinearGradient(0, 514, 0, 550);
    button.addColorStop(0, '#287e9f'); button.addColorStop(1, '#164663');
    drawPanel(ctx, 666, 514, 110, 36, { fill: button, border: '#8bdfff' });
    drawText(ctx, this.selection.area === 'equipment' ? 'Unequip' : 'Equip', 721, 524, { size: 13, align: 'center' });
    if (this.selection.area === 'bag') {
      drawPanel(ctx, 783, 514, 94, 36, { fill: '#223044', border: '#65778a' });
      drawText(ctx, 'Discard', 830, 524, { size: 13, align: 'center', color: '#bbcbd9' });
    }
  }
}

// Equipment shows the loadout; Inventory holds both supplies and unequipped gear.
import { drawCharacterMenu } from './character-menu.js';
import { bagText as drawText, bagPanel, bagButton, bagSlot, bagWrappedText, bagPortraitBackdrop } from './bag-widgets.js';
import { RARITIES, OFFHAND_BY_CLASS } from '../data/item-definitions.js';
import { getClassDefinition } from '../data/class-definitions.js';
import { itemSprite } from '../graphics/weapon-icons.js';
import { usePotion } from '../systems/loot-system.js';
import { displayItemName, isEnhanceable } from '../systems/weapon-enhance-stats.js';
import { enhancePreview, drawEnhancePreview, drawEnhanceButton, runEnhance } from './inventory-enhance-actions.js';
import { supplyCount, drawSupplyIcon, drawSupplyDetails } from './inventory-supply-cells.js';
import { equipItem, unequipItem, removeFromInventory, INVENTORY_SIZE } from '../systems/equipment-system.js';
import { SLOT_SIZE, EQUIP_SLOT_SIZE, GRID_COLS, GRID_ROWS, BAG_ACTIONS, EQUIP_SLOT_ORDER,
  equipSlotPosition, gridSlotPosition, slotLabel, describeItemStats, compareToEquipped } from './inventory-layout.js';

const PAGE_SIZE = GRID_COLS * GRID_ROWS;
const inside = (p, r) => p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h;
const drawAction = (ctx, text, bounds, tone) => bagButton(ctx, text, bounds.x, bounds.y, bounds.w, bounds.h, tone);

export class InventoryPanel {
  constructor() {
    this.mode = 'Equipment';
    this.selection = { area: 'equipment', index: 0 };
    this.page = 0;
    this.supplyOrder = ['coin', 'hpPotion', 'mpPotion', 'upgradeStone'];
    this.enhanceRoll = Math.random; // overridable by checks/tests for deterministic outcomes
    this.message = ''; this.messageTimer = 0; this.lastClick = null;
  }
  handleInput(input, player) {
    const result = { close: false };
    if (input.wasPressed('inventory') || input.wasPressed('pause') || input.wasPressed('skillPanel')) return { close: true };
    this.navigate(input);
    for (const kind of ['hpPotion', 'mpPotion']) if (input.wasPressed(kind)) this.flash(usePotion(player, kind) ? 'Restored 30%' : 'No potion or resource full');
    for (const click of input.takeClicks()) {
      if (this.mode === 'Equipment' && inside(click, BAG_ACTIONS.unequip)) { this.activate(player); continue; }
      if (this.mode === 'Equipment' && inside(click, BAG_ACTIONS.loadoutEnhance) && isEnhanceable(this.selectedItem(player))) { runEnhance(this, player, this.selectedItem(player)); continue; }
      if (this.mode === 'Inventory') {
        if (inside(click, BAG_ACTIONS.primary)) { this.activate(player); continue; }
        if (inside(click, BAG_ACTIONS.secondary) && this.selection.area === 'bag' && this.selectedItem(player)) { runEnhance(this, player, this.selectedItem(player)); continue; }
        if (inside(click, BAG_ACTIONS.page)) { this.page = (this.page + 1) % this.pageCount; this.selection = this.cellSelection(this.page * PAGE_SIZE); continue; }
        if (inside(click, BAG_ACTIONS.sort)) {
          player.inventory.sort((a, b) => EQUIP_SLOT_ORDER.indexOf(a.slot) - EQUIP_SLOT_ORDER.indexOf(b.slot) || b.itemLevel - a.itemLevel);
          this.supplyOrder.sort((a, b) => this.supplyCount(player, b) - this.supplyCount(player, a));
          this.page = 0; this.selection = this.cellSelection(0); this.lastClick = null; continue;
        }
      }
      const hit = this.hitTest(click.x, click.y);
      if (!hit) continue;
      const now = performance.now();
      const double = this.lastClick && this.lastClick.area === hit.area && this.lastClick.index === hit.index && now - this.lastClick.time < 350;
      this.selection = hit; this.lastClick = { ...hit, time: now };
      if (double) this.activate(player);
    }
    if (input.wasPressed('confirm')) this.activate(player);
    if (input.wasPressed('dropItem')) this.dropSelected(player);
    this.messageTimer -= 16;
    return result;
  }
  navigate(input) {
    if (this.mode === 'Equipment') {
      if (this.selection.area !== 'equipment') this.selection = { area: 'equipment', index: 0 };
      if (input.wasPressed('moveUp')) this.selection.index = (this.selection.index + EQUIP_SLOT_ORDER.length - 1) % EQUIP_SLOT_ORDER.length;
      if (input.wasPressed('moveDown')) this.selection.index = (this.selection.index + 1) % EQUIP_SLOT_ORDER.length;
      return;
    }
    let i = this.selection.area === 'bag' ? this.selection.index + this.supplyOrder.length : this.selection.index;
    if (input.wasPressed('moveLeft')) i = Math.max(0, i - 1);
    if (input.wasPressed('moveRight')) i = Math.min(this.cellCount - 1, i + 1);
    if (input.wasPressed('moveUp')) i = Math.max(0, i - GRID_COLS);
    if (input.wasPressed('moveDown')) i = Math.min(this.cellCount - 1, i + GRID_COLS);
    this.selection = this.cellSelection(i); this.page = Math.floor(i / PAGE_SIZE);
  }
  get cellCount() { return this.supplyOrder.length + INVENTORY_SIZE; }
  get pageCount() { return Math.ceil(this.cellCount / PAGE_SIZE); }
  cellSelection(index) { return index < this.supplyOrder.length ? { area: 'supply', index } : { area: 'bag', index: index - this.supplyOrder.length }; }
  hitTest(x, y) {
    if (this.mode === 'Equipment') {
      for (let i = 0; i < EQUIP_SLOT_ORDER.length; i++) { const p = equipSlotPosition(i); if (Math.abs(x - p.x) <= 30 && Math.abs(y - p.y) <= 30) return { area: 'equipment', index: i }; }
    } else {
      for (let i = 0; i < PAGE_SIZE; i++) {
        const p = gridSlotPosition(i), index = this.page * PAGE_SIZE + i;
        if (index < this.cellCount && Math.abs(x - p.x) <= SLOT_SIZE / 2 && Math.abs(y - p.y) <= SLOT_SIZE / 2) return this.cellSelection(index);
      }
    }
    return null;
  }
  selectedItem(player) {
    return this.selection.area === 'bag' ? player.inventory[this.selection.index] || null
      : this.selection.area === 'equipment' ? player.equipment[EQUIP_SLOT_ORDER[this.selection.index]] : null;
  }
  activate(player) {
    if (this.selection.area === 'supply') {
      const kind = this.supplyOrder[this.selection.index];
      if (kind === 'hpPotion' || kind === 'mpPotion') this.flash(usePotion(player, kind) ? 'Restored 30%' : 'No potion or resource full');
      return;
    }
    const item = this.selectedItem(player); if (!item) return;
    const outcome = this.selection.area === 'bag' ? equipItem(player, item) : unequipItem(player, EQUIP_SLOT_ORDER[this.selection.index]);
    if (outcome.ok) player.recomputeStats();
    this.flash(outcome.ok ? this.selection.area === 'bag' ? `Equipped ${item.name}` : 'Unequipped' : outcome.reason);
  }
  dropSelected(player) {
    if (this.mode !== 'Inventory' || this.selection.area !== 'bag') return;
    const item = this.selectedItem(player); if (!item) return;
    removeFromInventory(player, item.uid); this.flash(`Discarded ${item.name}`);
  }
  flash(text) { this.message = text; this.messageTimer = 1500; }
  supplyCount(player, kind) { return supplyCount(player, kind); }
  render(ctx, player, registry) {
    drawCharacterMenu(ctx, player, this.mode, registry);
    if (this.mode === 'Inventory') this.drawEquipmentBag(ctx, player, registry);
    else this.drawLoadout(ctx, player, registry);
    if (this.messageTimer > 0) {
      ctx.save(); ctx.beginPath(); ctx.rect(612, 500, 300, 20); ctx.clip();
      drawText(ctx, this.message, 616, 503, { size: 11, color: '#f0d39a' }); ctx.restore();
    }
  }
  drawItemIcon(ctx, registry, item, cx, cy, factor = 1, classId = 'warrior') {
    const sprite = itemSprite(registry, item, classId, true); if (!sprite) return;
    const scale = Math.min(54 / sprite.width, 54 / sprite.height) * factor;
    ctx.save(); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high';
    ctx.drawImage(sprite, cx - sprite.width * scale / 2, cy - sprite.height * scale / 2, sprite.width * scale, sprite.height * scale); ctx.restore();
  }
  drawLoadout(ctx, player, registry) {
    bagPanel(ctx, 320, 154, 376, 414); bagPanel(ctx, 702, 154, 226, 414);
    bagPortraitBackdrop(ctx, 400, 186, 218, 306);
    EQUIP_SLOT_ORDER.forEach((slot, i) => {
      const p = equipSlotPosition(i), item = player.equipment[slot];
      bagSlot(ctx, p.x - 30, p.y - 30, EQUIP_SLOT_SIZE, { selected: this.selection.area === 'equipment' && this.selection.index === i, color: item ? RARITIES[item.rarity].color : '#8398a9' });
      if (item) this.drawItemIcon(ctx, registry, item, p.x, p.y, 0.85, player.classId);
      else {
        ctx.save(); ctx.globalAlpha = 0.22; ctx.filter = 'grayscale(1)';
        this.drawItemIcon(ctx, registry, { slot, itemLevel: 1, templateId: slot === 'offhand' ? OFFHAND_BY_CLASS[player.classId]?.type : undefined }, p.x, p.y, 0.72, player.classId); ctx.restore();
      }
    });
    for (let i = 2; i < 4; i++) bagSlot(ctx, 628, 172 + i * 84, 60, { locked: true, color: '#4e667d' });
    const frame = (registry.heroes?.[player.classId] || registry.hero)?.down.idle[0];
    if (frame) { ctx.save(); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high'; ctx.drawImage(frame, 399, 240, 216, 216); ctx.restore(); }
    drawText(ctx, `${getClassDefinition(player.classId).name}  Lv. ${player.level}`, 508, 490, { size: 12, align: 'center', color: '#b0cadb' });
    drawText(ctx, '+ Status', 716, 165, { size: 17 });
    const stats = [['HP', player.maxHp], ['MP', player.maxMp], ['ATK', player.atk], ['DEF', player.def],
      ['Magic ATK', player.classId === 'wizard' || player.classId === 'cleric' ? player.atk : 0], ['Magic DEF', player.def], ['CRI Rate', `${(player.passiveMods.critChance * 100).toFixed(1)}%`],
      ['Move Speed', `${Math.round(player.speed / getClassDefinition(player.classId).baseStats.speed * 100)}%`]];
    stats.forEach(([label, value], i) => {
      const y = 204 + i * 33;
      ctx.fillStyle = i % 2 ? '#0b192c33' : '#45648319'; ctx.fillRect(714, y - 6, 200, 31);
      ctx.fillStyle = '#91acc322'; ctx.fillRect(714, y - 8, 200, 1);
      drawText(ctx, label, 716, y, { size: 13, color: '#bbdaed', bold: false });
      drawText(ctx, `${value}`, 914, y, { size: 14, align: 'right' });
    });
    const held = this.selectedItem(player);
    if (held) drawAction(ctx, 'Unequip', BAG_ACTIONS.unequip, 'blue');
    if (isEnhanceable(held)) {
      const preview = enhancePreview(player, held);
      drawText(ctx, preview.to === preview.from ? preview.reason : `+${preview.from} -> +${preview.to}  ${Math.round(preview.chance * 100)}%  Stone x${preview.owned}`, 332, 538, { size: 12, color: '#f0d39a' });
      drawEnhanceButton(ctx, preview, BAG_ACTIONS.loadoutEnhance);
    }
  }
  drawEquipmentBag(ctx, player, registry) {
    bagPanel(ctx, 320, 154, 278, 414); bagPanel(ctx, 600, 154, 328, 414);
    for (let i = 0; i < PAGE_SIZE; i++) {
      const index = this.page * PAGE_SIZE + i, p = gridSlotPosition(i), entry = this.cellSelection(index);
      const item = entry.area === 'bag' ? player.inventory[entry.index] : null;
      bagSlot(ctx, p.x - SLOT_SIZE / 2, p.y - SLOT_SIZE / 2, SLOT_SIZE, { selected: this.selection.area === entry.area && this.selection.index === entry.index, color: item ? RARITIES[item.rarity].color : '#607b91', locked: index >= this.cellCount });
      if (entry.area === 'supply') drawSupplyIcon(ctx, player, this.supplyOrder[entry.index], p.x, p.y);
      if (item) this.drawItemIcon(ctx, registry, item, p.x, p.y, 1.12, player.classId);
    }
    drawAction(ctx, 'Sort', BAG_ACTIONS.sort); drawAction(ctx, `${this.page + 1} / ${this.pageCount} >`, BAG_ACTIONS.page);
    drawText(ctx, `${player.inventory.length}/${INVENTORY_SIZE}`, 581, 538, { size: 13, align: 'right', color: '#bfd1dc' });
    const item = this.selectedItem(player);
    if (this.selection.area === 'supply') { drawSupplyDetails(ctx, player, this.supplyOrder[this.selection.index]); return; }
    if (!item) { drawText(ctx, 'Select equipment', 764, 300, { size: 18, align: 'center', color: '#b4d0e0' }); return; }
    bagSlot(ctx, 615, 169, 76, { color: RARITIES[item.rarity].color });
    this.drawItemIcon(ctx, registry, item, 653, 207, 1.15, player.classId);
    bagWrappedText(ctx, displayItemName(item), 704, 170, 210, { size: 17, color: '#f2deae', lineHeight: 21, maxLines: 2 });
    drawText(ctx, slotLabel(item.slot), 704, 219, { size: 12, color: '#c7e2f2' });
    drawText(ctx, `Lv. ${item.itemLevel}`, 704, 239, { size: 13 });
    const comparisons = compareToEquipped(item, player.equipment[item.slot]);
    describeItemStats(item).forEach((line, i) => {
      const y = 285 + i * 29; ctx.fillStyle = '#6f9ab355'; ctx.fillRect(616, y - 9, 295, 1);
      drawText(ctx, line, 616, y, { size: 14, color: '#c2e3f4', bold: false });
      if (this.selection.area === 'bag') drawText(ctx, comparisons[i].text.split(' ').slice(1).join(' '), 910, y, { size: 12, align: 'right', color: comparisons[i].color });
    });
    const preview = isEnhanceable(item) ? enhancePreview(player, item) : null;
    if (preview) drawEnhancePreview(ctx, preview, 616, 403);
    else bagWrappedText(ctx, `${RARITIES[item.rarity].prefix || 'Normal'} ${slotLabel(item.slot).toLowerCase()} from the level ${item.itemLevel} equipment set.`, 616, 403, 288, { size: 13, lineHeight: 21 });
    drawAction(ctx, 'Equip', BAG_ACTIONS.primary, 'blue');
    drawEnhanceButton(ctx, preview || enhancePreview(player, null), BAG_ACTIONS.secondary);
  }
}

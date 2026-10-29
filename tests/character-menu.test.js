import { test } from 'node:test';
import assert from 'node:assert/strict';
import { InventoryPanel } from '../src/ui/inventory-panel.js';
import { menuHit } from '../src/ui/character-menu.js';
import { Player } from '../src/entities/player.js';
import { generateItem } from '../src/systems/equipment-system.js';
import { gridSlotPosition, equipSlotPosition, BAG_ACTIONS } from '../src/ui/inventory-layout.js';
const clickInput = (x, y) => ({ wasPressed: () => false, takeClicks: () => [{ x, y }] });
test('Bag tabs, close and paged cells match rendered positions', () => {
  assert.equal(menuHit(410, 118), 'Equipment');
  assert.equal(menuHit(620, 118), 'Inventory');
  assert.equal(menuHit(820, 118), 'Skills');
  assert.equal(menuHit(910, 64), 'close');
  const panel = new InventoryPanel();
  for (let i = 0; i < 6; i++) { // weapon, helmet, armor, gloves, boots, offhand
    const p = equipSlotPosition(i);
    assert.deepEqual(panel.hitTest(p.x, p.y), { area: 'equipment', index: i });
  }
  panel.mode = 'Inventory';
  for (let i = 0; i < 24; i++) {
    panel.page = Math.floor(i / 12);
    const p = gridSlotPosition(i);
    assert.deepEqual(panel.hitTest(p.x, p.y), i < 4 ? { area: 'supply', index: i } : { area: 'bag', index: i - 4 });
  }
});
test('Inventory equips and discards gear; Equipment unequips without a Detail view', () => {
  const p = new Player(), panel = new InventoryPanel();
  const item = generateItem(() => 0.5, 1, 'weapon', 'common');
  p.inventory.push(item);
  panel.mode = 'Inventory'; panel.selection = { area: 'bag', index: 0 };
  const button = BAG_ACTIONS.primary;
  panel.handleInput(clickInput(button.x + 10, button.y + 10), p);
  assert.equal(p.equipment.weapon.uid, item.uid);
  panel.mode = 'Equipment'; panel.selection = { area: 'equipment', index: 0 };
  panel.handleInput(clickInput(800, 550), p);
  assert.equal(p.equipment.weapon, null);
  panel.mode = 'Inventory'; panel.selection = { area: 'bag', index: 0 };
  panel.handleInput({ wasPressed: key => key === 'dropItem', takeClicks: () => [] }, p);
  assert.equal(p.inventory.length, 0);
});
test('selecting supply does not consume it; Use consumes exactly one potion', () => {
  const p = new Player(), panel = new InventoryPanel();
  p.hp = 1; p.potions.hpPotion = 2; panel.mode = 'Inventory';
  const pos = gridSlotPosition(1);
  panel.handleInput(clickInput(pos.x, pos.y), p);
  assert.equal(p.potions.hpPotion, 2);
  panel.handleInput(clickInput(650, 550), p);
  assert.equal(p.potions.hpPotion, 1);
  assert.equal(p.hp, 1 + Math.round(p.maxHp * 0.3));
  assert.deepEqual(panel.selection, { area: 'supply', index: 1 });
});

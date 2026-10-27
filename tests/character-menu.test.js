import { test } from 'node:test';
import assert from 'node:assert/strict';
import { InventoryPanel } from '../src/ui/inventory-panel.js';
import { menuHit } from '../src/ui/character-menu.js';
import { Player } from '../src/entities/player.js';
import { generateItem } from '../src/systems/equipment-system.js';
import { gridSlotPosition, equipSlotPosition } from '../src/ui/inventory-layout.js';

const clickInput = (x, y) => ({ wasPressed: () => false, takeClicks: () => [{ x, y }] });
test('menu tabs, close, bag cells and equipment cells match their rendered positions', () => {
  assert.equal(menuHit(150, 118), 'Inventory');
  assert.equal(menuHit(316, 118), 'Skills');
  assert.equal(menuHit(480, 118), 'Equipment');
  assert.equal(menuHit(873, 70), 'close');
  const panel = new InventoryPanel();
  for (let i = 0; i < 20; i++) {
    const p = gridSlotPosition(i);
    assert.deepEqual(panel.hitTest(p.x, p.y), { area: 'bag', index: i });
    assert.ok(p.x + 32 < 642 && p.y + 32 < 495);
  }
  const pos = equipSlotPosition(0);
  assert.equal(panel.hitTest(pos.x, pos.y), null);
  panel.mode = 'Equipment';
  assert.deepEqual(panel.hitTest(pos.x, pos.y), { area: 'equipment', index: 0 });
});

test('menu action buttons equip, unequip and discard the selected item', () => {
  const p = new Player(), panel = new InventoryPanel();
  const item = generateItem(() => 0.5, 3, 'weapon', 'rare');
  p.inventory.push(item);
  panel.handleInput(clickInput(721, 532), p);
  assert.equal(p.equipment.weapon.uid, item.uid);
  panel.mode = 'Equipment'; panel.selection = { area: 'equipment', index: 0 };
  panel.handleInput(clickInput(721, 532), p);
  assert.equal(p.equipment.weapon, null);
  assert.equal(p.inventory.length, 1);
  panel.mode = 'Inventory'; panel.selection = { area: 'bag', index: 0 };
  panel.handleInput(clickInput(830, 532), p);
  assert.equal(p.inventory.length, 0);
});

test('supply clicks consume exactly one potion without affecting item selection', () => {
  const p = new Player(), panel = new InventoryPanel();
  p.hp = 1; p.potions.hpPotion = 2;
  panel.handleInput(clickInput(300, 520), p);
  assert.equal(p.potions.hpPotion, 1);
  assert.equal(p.hp, 1 + Math.round(p.maxHp * 0.3));
  assert.deepEqual(panel.selection, { area: 'bag', index: 0 });
});

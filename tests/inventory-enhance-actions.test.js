import { test } from 'node:test';
import assert from 'node:assert/strict';
import { enhancePreview, runEnhance } from '../src/ui/inventory-enhance-actions.js';
import { InventoryPanel } from '../src/ui/inventory-panel.js';
import { Player } from '../src/entities/player.js';

const weapon = (enhance = 0) => ({ uid: 'w1', templateId: 'sword', slot: 'weapon', name: 'Fine Sword', rarity: 'common', itemLevel: 1, stats: { atk: 40 }, enhance });

test('preview: armor, no stones, max and normal values', () => {
  const player = new Player();
  assert.equal(enhancePreview(player, { slot: 'armor', stats: { def: 3 } }).reason, 'Weapons only');
  assert.equal(enhancePreview(player, weapon(2)).reason, 'Need Upgrade Stone');
  assert.equal(enhancePreview(player, weapon(2)).can, false);
  player.materials.upgradeStone = 4;
  const p = enhancePreview(player, weapon(6));
  assert.deepEqual([p.can, p.from, p.to, p.chance, p.owned], [true, 6, 7, 0.7, 4]);
  assert.deepEqual([p.atkFrom, p.atkTo], [52, 55]);
  assert.equal(p.costLabel, '1 Upgrade Stone');
  const max = enhancePreview(player, weapon(15));
  assert.deepEqual([max.can, max.reason], [false, 'Max +15']);
});

test('runEnhance flashes success / failure and updates the item', () => {
  const panel = new InventoryPanel();
  const player = new Player();
  const item = weapon(7);
  player.inventory.push(item);
  player.materials.upgradeStone = 2;
  panel.enhanceRoll = () => 0;
  runEnhance(panel, player, item);
  assert.equal(item.enhance, 8);
  assert.equal(panel.message, 'Success! Fine Sword +8');
  panel.enhanceRoll = () => 0.999;
  runEnhance(panel, player, item);
  assert.equal(item.enhance, 8);
  assert.equal(panel.message, 'Failed... stone lost (+8 kept)');
  runEnhance(panel, player, item);
  assert.equal(panel.message, 'Need Upgrade Stone');
});

test('equipped weapon enhance raises player atk through the panel action', () => {
  const panel = new InventoryPanel();
  const player = new Player();
  const item = weapon(0);
  player.equipment.weapon = item;
  player.materials.upgradeStone = 1;
  player.recomputeStats();
  const before = player.atk;
  panel.enhanceRoll = () => 0;
  runEnhance(panel, player, item);
  assert.ok(player.atk > before);
});

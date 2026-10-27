import test from 'node:test';
import assert from 'node:assert/strict';
import { weaponIconIndex, itemSprite } from '../src/graphics/weapon-icons.js';
import { generateItem, sanitizeItem } from '../src/systems/equipment-system.js';

test('all classes share artwork within level pairs and advance at decade boundaries', () => {
  for (const [classId, offset] of [['warrior', 0], ['wizard', 10], ['mechanic', 20]]) {
    for (let tier = 0; tier < 10; tier++) {
      const level = tier === 0 ? 1 : tier * 10;
      assert.equal(weaponIconIndex(level, classId), offset + tier);
      assert.equal(weaponIconIndex(tier * 10 + 5, classId), offset + tier);
    }
  }
  assert.equal(weaponIconIndex(100), 9);
  assert.equal(weaponIconIndex(undefined, 'unknown'), 0);
});

test('weapon class and high-level artwork survive save sanitization', () => {
  for (const classId of ['warrior', 'wizard', 'mechanic']) {
    const item = generateItem(() => 0, 95, 'weapon', 'common', classId);
    const saved = sanitizeItem(JSON.parse(JSON.stringify(item)));
    assert.equal(saved.classId, classId);
    assert.equal(saved.itemLevel, 95);
  }
});

test('inventory and drops resolve the same weapon, with legacy and missing-atlas fallbacks', () => {
  const registry = { weaponIcons: Array.from({ length: 30 }, (_, i) => ({ index: i })), items: { sword: {} }, inventoryItems: { sword: {} } };
  const item = { slot: 'weapon', templateId: 'sword', itemLevel: 25, classId: 'wizard' };
  assert.equal(itemSprite(registry, item), registry.weaponIcons[12]);
  assert.equal(itemSprite(registry, item, 'mechanic', true), registry.weaponIcons[12]);
  delete item.classId;
  assert.equal(itemSprite(registry, item, 'mechanic', true), registry.weaponIcons[22]);
  delete registry.weaponIcons;
  assert.equal(itemSprite(registry, item), registry.items.sword);
  assert.equal(itemSprite(registry, item, 'warrior', true), registry.inventoryItems.sword);
});

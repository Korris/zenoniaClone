import test from 'node:test';
import assert from 'node:assert/strict';
import { equipmentTier, ARMOR_TIER_NAMES } from '../src/data/item-definitions.js';
import { itemSprite } from '../src/graphics/weapon-icons.js';
import { generateItem, sanitizeItem } from '../src/systems/equipment-system.js';

test('all four armor slots use ten shared level-pair tiers, like weapons', () => {
  for (const slot of ['helmet', 'armor', 'gloves', 'boots']) {
    const icons = Array.from({ length: 10 }, (_, tier) => ({ tier, slot }));
    const registry = { armorIcons: { [slot]: icons }, items: {} };
    for (let tier = 0; tier < 10; tier++) {
      const lower = tier ? tier * 10 : 1;
      const upper = tier * 10 + 5;
      const item = generateItem(() => 0, lower, slot, 'common');
      const paired = generateItem(() => 0.99, upper, slot, 'rare');
      assert.equal(itemSprite(registry, item, 'warrior', true), icons[tier]);
      assert.equal(itemSprite(registry, paired, 'wizard', false), icons[tier]);
      assert.equal(item.name, ARMOR_TIER_NAMES[slot][tier]);
      const saved = sanitizeItem(JSON.parse(JSON.stringify(paired)));
      assert.equal(saved.itemLevel, upper);
      assert.equal(itemSprite(registry, saved, 'mechanic', true), icons[tier]);
    }
  }
  assert.equal(equipmentTier(100), 9);
  assert.equal(equipmentTier(undefined), 0);
});

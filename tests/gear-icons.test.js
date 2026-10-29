import test from 'node:test';
import assert from 'node:assert/strict';
import { gearIconRef, GEAR_ICON_SPECS } from '../src/graphics/gear-icons.js';
import { itemSprite, groundGearSprite } from '../src/graphics/weapon-icons.js';

const registry = {
  gearIcons: { warrior: { weapon: { 0: 'sword0', 4: 'sword4' }, offhand: { 0: 'shield0', 4: 'shield4' } },
    mechanic: { weapon: { 0: 'gun0' }, offhand: { 0: 'robot0' } } },
  groundGearIcons: { warrior: { weapon: { 0: 'gSword0', 4: 'gSword4' }, offhand: { 0: 'gShield0', 4: 'gShield4' } } },
  weaponIcons: Array.from({ length: 30 }, (_, i) => `painted${i}`),
  armorIcons: {}, inventoryItems: {}, items: { shield: 'placeholderShield' },
};
const sword = (itemLevel, classId) => ({ slot: 'weapon', itemLevel, classId, templateId: 'sword' });
const shield = (itemLevel) => ({ slot: 'offhand', itemLevel, templateId: 'shield' });

test('one GEAR_ICON_SPECS table drives warrior, mechanic and assassin icons', () => {
  assert.deepEqual(Object.keys(GEAR_ICON_SPECS), ['warrior', 'mechanic', 'assassin']);
  for (const spec of Object.values(GEAR_ICON_SPECS)) assert.deepEqual(Object.keys(spec), ['weapon', 'offhand']);
});

test('warrior sword and shield resolve to the cut overlay icon of their tier', () => {
  assert.deepEqual(gearIconRef(sword(5, 'warrior'), 'warrior'), { classId: 'warrior', slot: 'weapon', tier: 0 });
  assert.deepEqual(gearIconRef(shield(45), 'warrior'), { classId: 'warrior', slot: 'offhand', tier: 4 });
  assert.equal(itemSprite(registry, sword(5, 'warrior'), 'warrior', true), 'sword0');
  assert.equal(itemSprite(registry, shield(45), 'warrior', true), 'shield4');
  assert.equal(groundGearSprite(registry, shield(5), 'warrior'), 'gShield0');
  assert.equal(itemSprite(registry, { slot: 'weapon', itemLevel: 5, classId: 'mechanic' }, 'warrior', true), 'gun0', 'owner class comes from the item');
});

test('classes without a spec keep their painted icons', () => {
  assert.equal(gearIconRef(sword(5, 'wizard'), 'warrior'), null);
  assert.equal(gearIconRef({ slot: 'offhand', templateId: 'spellbook', itemLevel: 5 }, 'wizard'), null);
  assert.equal(itemSprite(registry, sword(5, 'wizard'), 'wizard', true), 'painted10');
  assert.ok(!groundGearSprite(registry, sword(5, 'mechanic'), 'mechanic'), 'ground icon of an unbuilt class => caller fallback');
});

test('missing overlay art (class not loaded) falls back to the painted icon', () => {
  const bare = { ...registry, gearIcons: { warrior: { weapon: [], offhand: [] } } };
  assert.equal(itemSprite(bare, sword(5, 'warrior'), 'warrior', true), 'painted0');
  const none = { ...registry, gearIcons: {} };
  assert.equal(itemSprite(none, { slot: 'weapon', itemLevel: 5, classId: 'mechanic' }, 'warrior', true), 'painted20');
});

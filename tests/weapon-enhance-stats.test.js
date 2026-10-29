import { test } from 'node:test';
import assert from 'node:assert/strict';
import { enhanceLevel, enhanceAtkBonus, effectiveItemStats, displayItemName, enhanceVisualBand } from '../src/systems/weapon-enhance-stats.js';
import { generateItem, sanitizeItem, computeEquipmentModifiers } from '../src/systems/equipment-system.js';
import { describeItemStats, compareToEquipped } from '../src/ui/inventory-layout.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { Player } from '../src/entities/player.js';

const weapon = (enhance, atk = 40) => ({ uid: 'w1', templateId: 'sword', slot: 'weapon', name: 'Fine Sword', rarity: 'common', itemLevel: 1, stats: { atk }, enhance });

test('atk bonus table: zero at +0, monotonic, floor of +1 per level, exact values for base 40', () => {
  assert.equal(enhanceAtkBonus(40, 0), 0);
  let prev = 0;
  for (let l = 1; l <= 15; l += 1) {
    const b = enhanceAtkBonus(40, l);
    assert.ok(b > prev && b >= l, `level ${l}`);
    prev = b;
  }
  assert.equal(enhanceAtkBonus(40, 7), 15); // 38%
  assert.equal(enhanceAtkBonus(40, 15), 58); // 144%
  assert.equal(enhanceAtkBonus(2, 5), 2); // floor scales with base: ceil(5 * 2/8)
  assert.equal(enhanceAtkBonus(8, 5), 5); // base >= 8 keeps the full +1/level floor
  assert.equal(enhanceAtkBonus(4, 1), 1);
  assert.equal(enhanceAtkBonus(40, 7, 0.5), 8); // offhand: half the % table (38% -> 19% of 40)
});

test('sanitizeItem clamps enhance and ignores garbage / armor', () => {
  const base = (e) => ({ ...weapon(e), stats: { atk: 4 } });
  assert.equal(sanitizeItem(base(undefined)).enhance, 0);
  assert.equal(sanitizeItem(base(99)).enhance, 15);
  assert.equal(sanitizeItem(base(-3)).enhance, 0);
  assert.equal(sanitizeItem(base('7')).enhance, 0);
  assert.equal(sanitizeItem(base(7)).enhance, 7);
  const armor = sanitizeItem({ uid: 'a', templateId: 'cloth', slot: 'armor', name: 'x', rarity: 'common', itemLevel: 1, stats: { def: 1 }, enhance: 9 });
  if (armor) assert.equal('enhance' in armor, false);
});

test('generateItem sets enhance 0 on weapons only', () => {
  const w = generateItem(() => 0.5, 1, 'weapon');
  assert.equal(w.enhance, 0);
  assert.equal('enhance' in generateItem(() => 0.5, 1, 'armor'), false);
});

test('effective stats do not mutate base and feed equipment modifiers', () => {
  const item = weapon(10);
  assert.equal(effectiveItemStats(item).atk, 40 + enhanceAtkBonus(40, 10));
  assert.equal(item.stats.atk, 40);
  const player = new Player();
  player.equipment.weapon = item;
  assert.equal(computeEquipmentModifiers(player).atkAdd, 40 + enhanceAtkBonus(40, 10));
});

test('stat lines and compare use effective values', () => {
  assert.deepEqual(describeItemStats(weapon(7)), ['ATK 55 (+15)']);
  assert.deepEqual(describeItemStats(weapon(0)), ['ATK 40']);
  assert.equal(compareToEquipped(weapon(7), weapon(0))[0].text, 'ATK +15');
});

test('enhance survives save round-trip; names and bands', () => {
  const player = new Player();
  player.equipment.weapon = sanitizeItem(weapon(8, 5));
  const loaded = validateSave(JSON.parse(JSON.stringify(serializeSave({ player, mapId: 'darkCave', x: 1, y: 1, bossDefeated: false }))));
  assert.equal(loaded.player.equipment.weapon.enhance, 8);
  assert.equal(displayItemName(weapon(0)), 'Fine Sword');
  assert.equal(displayItemName(weapon(7)), 'Fine Sword +7');
  assert.deepEqual([0, 6, 7, 9, 11, 13, 15].map((l) => enhanceVisualBand(weapon(l))), [0, 0, 1, 2, 3, 4, 4]);
  assert.equal(enhanceLevel(weapon(3.5)), 0);
});

import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  generateItem, rollRarity, rollEquipmentDrop, computeEquipmentModifiers, equipItem, unequipItem,
  addToInventory, removeFromInventory, isUpgrade, sanitizeItem, INVENTORY_SIZE,
} from '../src/systems/equipment-system.js';
import { Player } from '../src/entities/player.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';

/** Deterministic fake RNG: replays a fixed sequence of [0,1) values. */
function fakeRand(sequence) {
  let i = 0;
  return () => sequence[i++ % sequence.length];
}

test('generateItem is deterministic given the same rand sequence', () => {
  const seq = [0.1, 0.05, 0.9, 0.3];
  const a = generateItem(fakeRand(seq), 5, 'weapon');
  const b = generateItem(fakeRand(seq), 5, 'weapon');
  assert.deepEqual(a, b);
  assert.equal(a.slot, 'weapon');
  assert.equal(a.itemLevel, 5);
});

test('generateItem only picks templates unlocked at the given itemLevel', () => {
  // itemLevel 1: only the two tier-1 weapons (sword/sword2, minItemLevel 1) can be picked.
  for (let i = 0; i < 20; i++) {
    const item = generateItem(fakeRand([i / 20, 0.5, 0.1]), 1, 'weapon');
    assert.ok(['sword', 'sword2'].includes(item.templateId), item.templateId);
  }
});

test('rollRarity respects cumulative weight boundaries (common/uncommon/rare/epic)', () => {
  assert.equal(rollRarity(fakeRand([0.5])), 'common'); // < 0.70
  assert.equal(rollRarity(fakeRand([0.75])), 'uncommon'); // 0.70-0.90
  assert.equal(rollRarity(fakeRand([0.95])), 'rare'); // 0.90-0.98
  assert.equal(rollRarity(fakeRand([0.99])), 'epic'); // 0.98-1.0
});

test('generateItem scales stats by rarity multiplier and item level', () => {
  // gloves at itemLevel 1 only has one candidate template (clothGloves, baseStats {atk:1, critChance:0.02})
  const common = generateItem(fakeRand([0, 0.1]), 1, 'gloves');
  const epic = generateItem(fakeRand([0, 0.1]), 1, 'gloves', 'epic');
  assert.equal(common.rarity, 'common');
  assert.equal(common.stats.atk, 1); // round(1 * 1.0 * 1)
  assert.equal(epic.rarity, 'epic');
  assert.equal(epic.stats.atk, 2); // round(1 * 2.1 * 1)
  assert.equal(epic.name, 'Legendary Cloth Gloves');
});

test('rollEquipmentDrop: normal monsters respect the 12% drop chance, bosses always drop rare/epic', () => {
  const slime = MONSTER_DEFINITIONS.slime;
  assert.equal(rollEquipmentDrop(slime, fakeRand([0.5])), null); // 0.5 >= 0.12 -> no drop
  const dropped = rollEquipmentDrop(slime, fakeRand([0.01, 0, 0.1, 0.1]));
  assert.ok(dropped);
  assert.equal(dropped.itemLevel, slime.itemLevel);

  const boss = MONSTER_DEFINITIONS.golemBoss;
  for (let i = 0; i < 10; i++) {
    const item = rollEquipmentDrop(boss, fakeRand([i / 10, 0.3, i / 10]));
    assert.ok(item);
    assert.ok(['rare', 'epic'].includes(item.rarity), item.rarity);
    assert.equal(item.itemLevel, boss.itemLevel);
  }
});

test('addToInventory / removeFromInventory respect capacity and uid lookup', () => {
  const player = new Player();
  for (let i = 0; i < INVENTORY_SIZE; i++) {
    assert.equal(addToInventory(player, { uid: `i${i}`, slot: 'weapon', stats: {} }), true);
  }
  assert.equal(player.inventory.length, INVENTORY_SIZE);
  assert.equal(addToInventory(player, { uid: 'overflow', slot: 'weapon', stats: {} }), false);
  assert.equal(removeFromInventory(player, 'i0'), true);
  assert.equal(removeFromInventory(player, 'does-not-exist'), false);
  assert.equal(player.inventory.length, INVENTORY_SIZE - 1);
});

test('equipItem swaps the previous item back into the bag and unequipItem reverses it', () => {
  const player = new Player();
  const sword = generateItem(fakeRand([0, 0.1]), 1, 'weapon');
  const axe = generateItem(fakeRand([0.9, 0.1]), 6, 'weapon');
  addToInventory(player, sword);
  addToInventory(player, axe);

  assert.equal(equipItem(player, sword).ok, true);
  assert.equal(player.equipment.weapon.uid, sword.uid);
  assert.equal(player.inventory.length, 1); // axe still in bag, sword left the bag

  assert.equal(equipItem(player, axe).ok, true);
  assert.equal(player.equipment.weapon.uid, axe.uid);
  assert.equal(player.inventory.length, 1); // sword swapped back into the bag
  assert.equal(player.inventory[0].uid, sword.uid);

  assert.equal(unequipItem(player, 'weapon').ok, true);
  assert.equal(player.equipment.weapon, null);
  assert.equal(player.inventory.length, 2);
});

test('equipItem refuses when the bag would overflow', () => {
  const player = new Player();
  const equipped = generateItem(fakeRand([0, 0.1]), 1, 'weapon');
  player.equipment.weapon = equipped; // pre-equip without ever putting it in the bag
  for (let i = 0; i < INVENTORY_SIZE; i++) addToInventory(player, { uid: `i${i}`, slot: 'weapon', stats: {} });
  const fresh = generateItem(fakeRand([0.9, 0.1]), 6, 'weapon');
  const outcome = equipItem(player, fresh);
  assert.equal(outcome.ok, false);
  assert.equal(outcome.reason, 'Inventory full');
  assert.equal(player.equipment.weapon.uid, equipped.uid); // unchanged
});

test('computeEquipmentModifiers aggregates stats across every equipped slot', () => {
  const player = new Player();
  player.equipment.weapon = { stats: { atk: 5 } };
  player.equipment.gloves = { stats: { atk: 2, critChance: 0.05 } };
  player.equipment.boots = { stats: { def: 3, speedMult: 0.08 } };
  const mods = computeEquipmentModifiers(player);
  assert.equal(mods.atkAdd, 7);
  assert.equal(mods.defAdd, 3);
  assert.equal(mods.hpAdd, 0);
  assert.equal(Math.round(mods.speedMult * 100), 108);
  assert.equal(mods.critChance, 0.05);
});

test('player.recomputeStats folds equipment into atk/def/maxHp/speed/crit', () => {
  const player = new Player();
  const baseAtk = player.atk;
  const baseDef = player.def;
  const baseMaxHp = player.maxHp;
  player.equipment.weapon = { stats: { atk: 10 } };
  player.equipment.helmet = { stats: { def: 3, maxHp: 12 } };
  player.equipment.gloves = { stats: { critChance: 0.06 } };
  player.recomputeStats();
  assert.equal(player.atk, baseAtk + 10);
  assert.equal(player.def, baseDef + 3);
  assert.equal(player.maxHp, baseMaxHp + 12);
  assert.ok(player.passiveMods.critChance >= 0.06);
});

test('isUpgrade compares total stat value against the currently equipped item', () => {
  const weak = { stats: { atk: 4 } };
  const strong = { stats: { atk: 10 } };
  assert.equal(isUpgrade(strong, weak), true);
  assert.equal(isUpgrade(weak, strong), false);
  assert.equal(isUpgrade(weak, null), true);
});

test('sanitizeItem rejects unknown templates/slots/rarities and clamps stats', () => {
  assert.equal(sanitizeItem(null), null);
  assert.equal(sanitizeItem({ templateId: 'doesNotExist' }), null);
  assert.equal(sanitizeItem({ templateId: 'sword', slot: 'helmet', rarity: 'common' }), null); // slot mismatch
  assert.equal(sanitizeItem({ templateId: 'sword', slot: 'weapon', rarity: 'nope' }), null);

  const sanitized = sanitizeItem({
    uid: 'u1', templateId: 'sword', slot: 'weapon', rarity: 'rare', itemLevel: 999, name: 'Haxed Sword',
    stats: { atk: 99999, junk: 1 },
  });
  assert.equal(sanitized.itemLevel, 30); // clamped to MAX_LEVEL
  assert.equal(sanitized.stats.atk, 999); // clamped
  assert.equal(sanitized.stats.junk, undefined); // not a template stat, dropped
});

test('player save round-trip preserves inventory and equipment through sanitizeItem', async () => {
  const { serializeSave, validateSave } = await import('../src/save/save-serializer.js');
  const player = new Player();
  const weapon = generateItem(fakeRand([0, 0.1]), 3, 'weapon');
  const bagItem = generateItem(fakeRand([0.9, 0.2]), 5, 'boots');
  player.equipment.weapon = weapon;
  addToInventory(player, bagItem);

  const raw = serializeSave({ player, mapId: 'verdantMeadow', x: 10, y: 20, bossDefeated: false });
  const loaded = validateSave(JSON.parse(JSON.stringify(raw)));

  assert.equal(loaded.player.equipment.weapon.templateId, weapon.templateId);
  assert.equal(loaded.player.equipment.weapon.stats.atk, weapon.stats.atk);
  assert.equal(loaded.player.inventory.length, 1);
  assert.equal(loaded.player.inventory[0].templateId, bagItem.templateId);
  assert.equal(loaded.player.equipment.helmet, null);
});

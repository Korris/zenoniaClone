import { test } from 'node:test';
import assert from 'node:assert/strict';
import { attemptEnhance } from '../src/systems/weapon-enhance-system.js';
import { STONE_UPGRADE_METHOD, SUCCESS_BY_TARGET } from '../src/systems/weapon-upgrade-method-stone.js';
import { Player } from '../src/entities/player.js';

const makeWeapon = (enhance = 0) => ({ uid: 'w1', templateId: 'sword', slot: 'weapon', name: 'Sword', rarity: 'common', itemLevel: 1, stats: { atk: 40 }, enhance });
function setup(enhance, stones = 5) {
  const player = new Player();
  const weapon = makeWeapon(enhance);
  player.inventory.push(weapon);
  player.materials.upgradeStone = stones;
  return { player, weapon };
}

test('success raises level and spends one stone', () => {
  const { player, weapon } = setup(0);
  const r = attemptEnhance(player, weapon, { roll: () => 0 });
  assert.deepEqual([r.ok, r.success, r.level], [true, true, 1]);
  assert.equal(weapon.enhance, 1);
  assert.equal(player.materials.upgradeStone, 4);
});

test('failure at +4 target (90%) consumes the stone and keeps the level', () => {
  const { player, weapon } = setup(3);
  const r = attemptEnhance(player, weapon, { roll: () => 0.999 });
  assert.deepEqual([r.ok, r.success, r.level], [true, false, 3]);
  assert.equal(weapon.enhance, 3);
  assert.equal(player.materials.upgradeStone, 4);
});

test('rejections leave state untouched', () => {
  const none = setup(2, 0);
  assert.equal(attemptEnhance(none.player, none.weapon, { roll: () => 0 }).reason, 'no-cost');
  assert.equal(none.weapon.enhance, 2);
  const max = setup(15);
  assert.equal(attemptEnhance(max.player, max.weapon, { roll: () => 0 }).reason, 'max-level');
  assert.equal(max.player.materials.upgradeStone, 5);
  const armor = setup(0);
  armor.weapon.slot = 'armor';
  assert.equal(attemptEnhance(armor.player, armor.weapon).reason, 'not-weapon');
  const stranger = setup(0);
  assert.equal(attemptEnhance(stranger.player, makeWeapon(), { roll: () => 0 }).reason, 'not-owned');
  assert.equal(stranger.player.materials.upgradeStone, 5);
});

test('equipped weapon success recomputes player atk', () => {
  const player = new Player();
  const weapon = makeWeapon(0);
  player.equipment.weapon = weapon;
  player.materials.upgradeStone = 1;
  player.recomputeStats();
  const before = player.atk;
  attemptEnhance(player, weapon, { roll: () => 0 });
  assert.ok(player.atk > before);
});

test('success table is non-increasing, in (0,1], and fits the approved values', () => {
  const table = SUCCESS_BY_TARGET.slice(1);
  assert.equal(table.length, 15);
  table.forEach((p, i) => { assert.ok(p > 0 && p <= 1); if (i) assert.ok(p <= table[i - 1]); });
  assert.equal(STONE_UPGRADE_METHOD.successChance(0), 1);
  assert.equal(STONE_UPGRADE_METHOD.successChance(3), 0.9);
  assert.equal(STONE_UPGRADE_METHOD.successChance(14), 0.2);
});

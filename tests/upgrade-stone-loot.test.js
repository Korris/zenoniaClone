import { test } from 'node:test';
import assert from 'node:assert/strict';
import { rollDrops, applyPickup, spawnDrops } from '../src/systems/loot-system.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { Player } from '../src/entities/player.js';

const seq = (values) => { let i = 0; return () => values[i++ % values.length]; };

test('normal monsters drop a stone at <5% roll, not at >=5%', () => {
  const def = MONSTER_DEFINITIONS.slime;
  assert.equal(def.drops.upgradeStone, 0.05);
  // gold, hp, mp rolls high (no drops), stone roll last.
  assert.ok(rollDrops(def, seq([0.5, 0.99, 0.99, 0.04])).some((d) => d.kind === 'upgradeStone'));
  assert.ok(!rollDrops(def, seq([0.5, 0.99, 0.99, 0.05])).some((d) => d.kind === 'upgradeStone'));
});

test('earlier drop rolls keep their sequence (stone roll is last)', () => {
  const drops = rollDrops(MONSTER_DEFINITIONS.slime, seq([0, 0.01, 0.01, 0.99]));
  assert.deepEqual(drops.map((d) => d.kind), ['coin', 'hpPotion', 'mpPotion']);
});

test('bosses always drop 2 stones', () => {
  for (const id of ['golemBoss', 'frostfangKing']) {
    const stone = rollDrops(MONSTER_DEFINITIONS[id], () => 0.999).find((d) => d.kind === 'upgradeStone');
    assert.equal(stone.value, 2, id);
  }
  assert.equal(spawnDrops(MONSTER_DEFINITIONS.golemBoss, 0, 0, () => 0.999).some((d) => d.kind === 'upgradeStone'), true);
});

test('pickup adds stones, capped at 9999', () => {
  const player = new Player();
  const label = applyPickup(player, { kind: 'upgradeStone', value: 2 });
  assert.equal(player.materials.upgradeStone, 2);
  assert.equal(label.text, '+2 Upgrade Stones');
  assert.equal(applyPickup(new Player(), { kind: 'upgradeStone', value: 1 }).text, '+1 Upgrade Stone');
  player.materials.upgradeStone = 9999;
  applyPickup(player, { kind: 'upgradeStone', value: 2 });
  assert.equal(player.materials.upgradeStone, 9999);
});

test('materials survive save round-trip; garbage and legacy saves load as 0', () => {
  const player = new Player();
  player.materials.upgradeStone = 12;
  const raw = JSON.parse(JSON.stringify(serializeSave({ player, mapId: 'darkCave', x: 1, y: 1, bossDefeated: false })));
  assert.equal(validateSave(raw).player.materials.upgradeStone, 12);
  raw.player.materials.upgradeStone = -5;
  assert.equal(validateSave(raw).player.materials.upgradeStone, 0);
  raw.player.materials.upgradeStone = 1e9;
  assert.equal(validateSave(raw).player.materials.upgradeStone, 9999);
  raw.player.materials = 'junk';
  assert.equal(validateSave(raw).player.materials.upgradeStone, 0);
  delete raw.player.materials;
  const loaded = validateSave(raw);
  const fresh = new Player();
  fresh.applySaveData(loaded.player);
  assert.equal(fresh.materials.upgradeStone, 0);
});

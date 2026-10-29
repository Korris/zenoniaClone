import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createStarterItem, equipStarterGear } from '../src/systems/starter-gear.js';
import { OFFHAND_BY_CLASS } from '../src/data/item-definitions.js';
import { generateItem } from '../src/systems/equipment-system.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { Player } from '../src/entities/player.js';
import { itemSprite } from '../src/graphics/weapon-icons.js';

const WEAPON_NAMES = { warrior: 'Sword', wizard: 'Staff', mechanic: 'Gun', assassin: 'Dagger', cleric: 'Holy Staff' };

test('every class starts with a common +0 level-1 weapon and its own offhand', () => {
  for (const classId of Object.keys(OFFHAND_BY_CLASS)) {
    const p = new Player('warrior');
    p.classId = classId; // assassin is not playable yet; exercise the table directly
    assert.equal(equipStarterGear(p), true);
    const { weapon, offhand } = p.equipment;
    assert.equal(weapon.name, WEAPON_NAMES[classId]);
    assert.equal(offhand.templateId, OFFHAND_BY_CLASS[classId].type);
    for (const [slot, it] of [['weapon', weapon], ['offhand', offhand]]) {
      assert.deepEqual([it.slot, it.uid, it.itemLevel, it.rarity, it.enhance], [slot, `starter-${slot}`, 1, 'common', 0]);
      assert.ok(it.classId);
    }
  }
});

test('starter gear raises stats and does not touch equipped or bagged items', () => {
  const p = new Player('warrior'), atk = p.atk, def = p.def;
  equipStarterGear(p);
  assert.ok(p.atk > atk && p.def > def);

  const q = new Player('wizard');
  const mine = generateItem(() => 0.5, 20, 'weapon', 'epic', 'wizard');
  q.equipment.weapon = mine;
  q.inventory.push(createStarterItem('offhand', 'wizard'));
  assert.equal(equipStarterGear(q), false);
  assert.equal(q.equipment.weapon, mine);
  assert.equal(q.equipment.offhand, null);
  assert.equal(q.inventory.length, 1);
});

test('old save with empty slots gets starters; equipped items stay unchanged', () => {
  const empty = new Player('mechanic');
  const raw = JSON.parse(JSON.stringify(serializeSave({ player: empty, mapId: 'verdantMeadow', x: 1, y: 2, bossDefeated: false })));
  const loaded = new Player('mechanic');
  loaded.applySaveData(validateSave(raw).player);
  equipStarterGear(loaded);
  assert.equal(loaded.equipment.weapon.uid, 'starter-weapon');
  assert.equal(loaded.equipment.offhand.templateId, 'robot');

  const geared = new Player('warrior');
  geared.equipment.weapon = generateItem(() => 0.5, 30, 'weapon', 'rare', 'warrior');
  geared.equipment.offhand = generateItem(() => 0.5, 30, 'offhand', 'rare', 'warrior');
  const uids = [geared.equipment.weapon.uid, geared.equipment.offhand.uid];
  const raw2 = JSON.parse(JSON.stringify(serializeSave({ player: geared, mapId: 'verdantMeadow', x: 1, y: 2, bossDefeated: false })));
  const again = new Player('warrior');
  again.applySaveData(validateSave(raw2).player);
  assert.equal(equipStarterGear(again), false);
  assert.deepEqual([again.equipment.weapon.uid, again.equipment.offhand.uid], uids);
});

test('empty-slot visual fallback still resolves without starter items', () => {
  const sprite = { w: 1 };
  const registry = { weaponIcons: Array(30).fill(sprite), items: {} };
  assert.equal(itemSprite(registry, { slot: 'weapon', itemLevel: 1, classId: 'wizard' }), sprite);
});

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { generateItem, rollEquipmentDrop, computeEquipmentModifiers, equipItem, sanitizeItem } from '../src/systems/equipment-system.js';
import { attemptEnhance } from '../src/systems/weapon-enhance-system.js';
import { effectiveItemStats, enhanceVisualBand } from '../src/systems/weapon-enhance-stats.js';
import { enhancePreview } from '../src/ui/inventory-enhance-actions.js';
import { EQUIPMENT_SLOTS, OFFHAND_BY_CLASS } from '../src/data/item-definitions.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { Player } from '../src/entities/player.js';
import { equipSlotPosition } from '../src/ui/inventory-layout.js';

const seq = (values) => { let i = 0; return () => values[i++ % values.length]; };
const OFFHAND_STATS = {
  shield: ['def', 'maxHp'], dagger: ['atk', 'critChance'], robot: ['atk'], spirit: ['maxHp', 'maxMp'], spellbook: ['atk', 'maxMp'],
};

test('offhand is a slot and has a distinct layout position', () => {
  assert.ok(EQUIPMENT_SLOTS.includes('offhand'));
  const positions = EQUIPMENT_SLOTS.map((_, i) => `${equipSlotPosition(i).x},${equipSlotPosition(i).y}`);
  assert.equal(new Set(positions).size, EQUIPMENT_SLOTS.length);
});

test('every class rolls its own offhand type, name and stat keys', () => {
  for (const [classId, { type, name }] of Object.entries(OFFHAND_BY_CLASS)) {
    const item = generateItem(seq([0.3]), 5, 'offhand', 'common', classId);
    assert.equal(item.slot, 'offhand');
    assert.equal(item.templateId, type);
    assert.equal(item.name, name);
    assert.equal(item.classId, classId);
    assert.equal(item.enhance, 0);
    assert.deepEqual(Object.keys(item.stats).sort(), [...OFFHAND_STATS[type]].sort());
  }
  assert.equal(generateItem(seq([0.3]), 1, 'offhand', 'rare', 'unknown').templateId, 'shield'); // fallback class
  assert.equal(generateItem(seq([0.3]), 1, 'offhand', 'rare', 'wizard').name, 'Superior Spellbook');
});

test('offhand stats are modest next to the same-level weapon', () => {
  const weapon = generateItem(seq([0.3]), 10, 'weapon', 'common', 'warrior');
  const book = generateItem(seq([0.3]), 10, 'offhand', 'common', 'mechanic');
  assert.ok(book.stats.atk < weapon.stats.atk && book.stats.atk >= weapon.stats.atk * 0.3);
});

test('stat aggregation covers def/hp/mp/crit/atk from the offhand', () => {
  const p = new Player('wizard');
  const base = { atk: p.atk, maxMp: p.maxMp };
  p.equipment.offhand = generateItem(seq([0.3]), 1, 'offhand', 'common', 'wizard');
  assert.equal(computeEquipmentModifiers(p).mpAdd, p.equipment.offhand.stats.maxMp);
  p.recomputeStats();
  assert.equal(p.atk, base.atk + p.equipment.offhand.stats.atk);
  assert.equal(p.maxMp, base.maxMp + p.equipment.offhand.stats.maxMp);

  const w = new Player('warrior'), defBefore = w.def, hpBefore = w.maxHp;
  w.equipment.offhand = generateItem(seq([0.3]), 1, 'offhand', 'common', 'warrior');
  w.recomputeStats();
  assert.equal(w.def, defBefore + w.equipment.offhand.stats.def);
  assert.equal(w.maxHp, hpBefore + w.equipment.offhand.stats.maxHp);

  const d = generateItem(seq([0.3]), 1, 'offhand', 'common', 'assassin');
  assert.equal(computeEquipmentModifiers({ equipment: { offhand: d } }).critChance, d.stats.critChance);
});

test('equipItem puts offhand in its own slot and keeps the weapon', () => {
  const p = new Player(), weapon = generateItem(seq([0.3]), 1, 'weapon'), off = generateItem(seq([0.3]), 1, 'offhand');
  p.inventory.push(weapon, off);
  assert.equal(equipItem(p, weapon).ok, true);
  assert.equal(equipItem(p, off).ok, true);
  assert.equal(p.equipment.weapon.uid, weapon.uid);
  assert.equal(p.equipment.offhand.uid, off.uid);
});

test('offhand enhances like a weapon on its enhanceStat', () => {
  const p = new Player(), shield = generateItem(seq([0.3]), 1, 'offhand', 'common', 'warrior');
  p.equipment.offhand = shield; p.materials.upgradeStone = 3;
  p.recomputeStats();
  const defBefore = p.def;
  const r = attemptEnhance(p, shield, { roll: () => 0 });
  assert.deepEqual([r.ok, r.success, r.level], [true, true, 1]);
  assert.equal(p.materials.upgradeStone, 2);
  assert.ok(effectiveItemStats(shield).def > shield.stats.def);
  assert.equal(effectiveItemStats(shield).maxHp, shield.stats.maxHp);
  assert.equal(p.def, defBefore + 1); // equipped -> recomputed
  const view = enhancePreview(p, shield);
  assert.deepEqual([view.can, view.statLabel, view.from, view.to], [true, 'DEF', 1, 2]);
  shield.enhance = 13;
  assert.equal(enhanceVisualBand(shield), 4);
  const spirit = generateItem(seq([0.3]), 1, 'offhand', 'common', 'cleric');
  spirit.enhance = 5;
  assert.ok(effectiveItemStats(spirit).maxHp > spirit.stats.maxHp);
});

test('drops roll class gear for a random class (tradeable) with injected rand', () => {
  const def = { itemLevel: 5 };
  // rand order: drop gate (<0.12), drop class 2/5 -> mechanic, slot 5/6 -> offhand, template, rarity 0.99 (epic), uid
  const item = rollEquipmentDrop(def, seq([0, 2.5 / 5, 5.5 / 6, 0, 0.99, 0.5]));
  assert.equal(item.slot, 'offhand');
  assert.equal(item.templateId, 'robot');
  assert.equal(item.classId, 'mechanic');
  assert.equal(item.rarity, 'epic');
  const wizardGear = rollEquipmentDrop(def, seq([0, 1.5 / 5, 5.5 / 6, 0, 0.5, 0.5]));
  assert.equal(wizardGear.classId, 'wizard');
  assert.equal(rollEquipmentDrop(def, seq([0.5])), null);
});

test('class gear only equips on its own class; armor fits every class', () => {
  const player = new Player();
  player.classId = 'warrior';
  const staff = generateItem(seq([0.3]), 1, 'weapon', 'common', 'wizard');
  player.inventory.push(staff);
  assert.equal(equipItem(player, staff).ok, false);
  assert.ok(player.inventory.includes(staff));
  const sword = generateItem(seq([0.3]), 1, 'weapon', 'common', 'warrior');
  player.inventory.push(sword);
  assert.equal(equipItem(player, sword).ok, true);
  const boots = generateItem(seq([0.3]), 1, 'boots', 'common', 'wizard');
  player.inventory.push(boots);
  assert.equal(equipItem(player, boots).ok, true);
});

test('save round-trip keeps offhand + enhance; old saves without offhand load', () => {
  const p = new Player('wizard');
  p.equipment.offhand = generateItem(seq([0.3]), 12, 'offhand', 'rare', 'wizard');
  p.equipment.offhand.enhance = 9;
  const raw = JSON.parse(JSON.stringify(serializeSave({ player: p, mapId: 'verdantMeadow', x: 1, y: 2, bossDefeated: false })));
  const loaded = validateSave(raw);
  assert.equal(loaded.player.equipment.offhand.templateId, 'spellbook');
  assert.equal(loaded.player.equipment.offhand.enhance, 9);
  assert.equal(loaded.player.equipment.offhand.classId, 'wizard');

  delete raw.player.equipment.offhand; // pre-offhand save
  const old = validateSave(raw);
  assert.ok(old);
  assert.equal(old.player.equipment.offhand, null);
  const restored = new Player('wizard');
  restored.applySaveData({ ...old.player, equipment: { weapon: null } });
  assert.equal(restored.equipment.offhand, null);
});

test('sanitize rejects slot mismatch and clamps offhand enhance', () => {
  assert.equal(sanitizeItem({ templateId: 'shield', slot: 'weapon', rarity: 'common' }), null);
  const item = sanitizeItem({ templateId: 'dagger', slot: 'offhand', rarity: 'common', enhance: 99, classId: 'hacker', stats: { atk: 2, def: 50 } });
  assert.equal(item.enhance, 15);
  assert.equal(item.classId, 'assassin'); // class comes from the dagger template, never the raw tag
  assert.equal(item.stats.def, undefined);
});

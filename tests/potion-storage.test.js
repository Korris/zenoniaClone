import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { applyPickup, usePotion } from '../src/systems/loot-system.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';

test('potions stack on pickup without restoring HP or MP; use restores 30% and consumes one', () => {
  const p = new Player();
  p.hp = 1; p.mp = 0;
  for (const kind of ['hpPotion', 'mpPotion']) {
    applyPickup(p, { kind, value: 0.3 });
    applyPickup(p, { kind, value: 0.3 });
    assert.equal(p.potions[kind], 2);
  }
  assert.equal(p.hp, 1); assert.equal(p.mp, 0);
  assert.equal(usePotion(p, 'hpPotion'), true);
  assert.equal(p.hp, 1 + Math.round(p.maxHp * 0.3));
  assert.equal(usePotion(p, 'mpPotion'), true);
  assert.equal(p.mp, Math.round(p.maxMp * 0.3));
  assert.deepEqual(p.potions, { hpPotion: 1, mpPotion: 1 });
  p.hp = p.maxHp; p.mp = p.maxMp;
  assert.equal(usePotion(p, 'hpPotion'), false);
  assert.equal(usePotion(p, 'mpPotion'), false);
  p.hp = 1; p.dead = true;
  assert.equal(usePotion(p, 'hpPotion'), false);
  assert.equal(usePotion(p, 'invalid'), false);
  assert.deepEqual(p.potions, { hpPotion: 1, mpPotion: 1 });
});

test('stored potions survive saving and old or invalid potion counts are sanitized', () => {
  const p = new Player(); p.potions = { hpPotion: 12, mpPotion: 8 };
  const raw = serializeSave({ player: p, mapId: 'oakhavenVillage', x: 0, y: 0 });
  const restored = new Player(); restored.applySaveData(validateSave(raw).player);
  assert.deepEqual(restored.potions, p.potions);
  delete raw.player.potions;
  assert.deepEqual(validateSave(raw).player.potions, { hpPotion: 0, mpPotion: 0 });
  raw.player.potions = { hpPotion: -4, mpPotion: '999' };
  assert.deepEqual(validateSave(raw).player.potions, { hpPotion: 0, mpPotion: 0 });
});

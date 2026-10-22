import { test } from 'node:test';
import assert from 'node:assert/strict';
import { serializeSave, validateSave, SAVE_VERSION } from '../src/save/save-serializer.js';
import { Player } from '../src/entities/player.js';

test('serialize -> validate round-trips player progress', () => {
  const player = new Player();
  player.gainExp(60);
  player.skills.vitality = 1;
  player.gold = 42;
  const raw = serializeSave({ player, mapId: 'darkCave', x: 100.4, y: 200.6, bossDefeated: false });
  assert.equal(raw.version, SAVE_VERSION);
  const loaded = validateSave(JSON.parse(JSON.stringify(raw)));
  assert.equal(loaded.player.level, 2);
  assert.equal(loaded.player.gold, 42);
  assert.equal(loaded.player.skills.vitality, 1);
  assert.equal(loaded.mapId, 'darkCave');
  assert.deepEqual([loaded.x, loaded.y], [100, 201]);
});

test('validateSave rejects garbage and wrong versions', () => {
  assert.equal(validateSave(null), null);
  assert.equal(validateSave('nope'), null);
  assert.equal(validateSave({ version: 99, player: { level: 1, exp: 0, skillPoints: 0 }, mapId: 'x' }), null);
  assert.equal(validateSave({ version: SAVE_VERSION, player: { level: 'high' }, mapId: 'x' }), null);
});

test('validateSave clamps out-of-range values and drops unknown skills', () => {
  const loaded = validateSave({
    version: SAVE_VERSION,
    player: { level: 999, exp: -5, skillPoints: 2.7, skills: { vitality: 50, hack: 3 }, gold: -10 },
    mapId: 'verdantMeadow',
    x: 'bad',
  });
  assert.equal(loaded.player.level, 30);
  assert.equal(loaded.player.exp, 0);
  assert.equal(loaded.player.skillPoints, 2);
  assert.equal(loaded.player.skills.vitality, 5);
  assert.equal(loaded.player.skills.hack, undefined);
  assert.equal(loaded.player.gold, 0);
  assert.equal(loaded.x, null);
});

test('player.applySaveData restores stats and clamps hp to new max', () => {
  const player = new Player();
  player.applySaveData({ level: 3, exp: 5, skillPoints: 0, skills: { vitality: 1 }, gold: 7, hp: 9999, mp: 3 });
  assert.equal(player.level, 3);
  assert.equal(player.hp, player.maxHp);
  assert.equal(player.mp, 3);
  assert.ok(player.maxHp > 124); // vitality bonus applied on top of level 3 base (124)
});

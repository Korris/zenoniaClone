import { test } from 'node:test';
import assert from 'node:assert/strict';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';
import { Monster } from '../src/entities/monster.js';
import { Player } from '../src/entities/player.js';
import { WorldState } from '../src/game/world-state.js';
import { FROST_HOLLOW_MAP } from '../src/world/maps/frost-hollow-map.js';
import { Tilemap } from '../src/world/tilemap.js';
import { MonsterSpawner } from '../src/world/monster-spawner.js';
import { updateMonsterAi } from '../src/systems/monster-ai-system.js';
import { damageMonster } from '../src/systems/combat-system.js';
import { rollEquipmentDrop } from '../src/systems/equipment-system.js';

test('Frostfang boss has 10x Ice Goblin HP and 3x attack, at a clear north-west spawn', () => {
  const normal = MONSTER_DEFINITIONS.iceGoblin, def = MONSTER_DEFINITIONS.frostfangKing;
  assert.equal(def.hp, normal.hp * 10);
  assert.equal(def.atk, normal.atk * 3);
  assert.ok(['rare', 'epic'].includes(rollEquipmentDrop(def, () => 0.9).rarity));
  const map = new Tilemap(FROST_HOLLOW_MAP.rows);
  const spawn = FROST_HOLLOW_MAP.boss;
  assert.ok(spawn.tx < map.width / 2 && spawn.ty < map.height / 2);
  for (const completed of [false, true]) {
    const bosses = new MonsterSpawner(FROST_HOLLOW_MAP, map).spawnInitial(completed).filter(m => m.isBoss);
    assert.equal(bosses.length, 1);
    const b = bosses[0].box;
    assert.equal(map.isRectBlocked(b.x, b.y, b.w, b.h), false);
  }
});

test('Frostfang waits at its post, chases nearby players, and death never wins the game', () => {
  const boss = new Monster('frostfangKing', 100, 100);
  updateMonsterAi(boss, { x: 500, y: 500 }, 5000);
  assert.equal(boss.ai.state, 'idle');
  updateMonsterAi(boss, { x: 140, y: 100 }, 16);
  assert.equal(boss.ai.state, 'chase');
  const w = new WorldState(new Player('warrior'), {});
  w.monsters = [boss];
  damageMonster(w, boss, boss.maxHp);
  assert.equal(boss.dead, true);
  assert.equal(w.bossDefeated, false);
  assert.ok(w.events.some(e => e.type === 'monsterKilled' && e.monsterType === boss.type));
  assert.equal(w.events.some(e => e.type === 'bossDefeated'), false);
});

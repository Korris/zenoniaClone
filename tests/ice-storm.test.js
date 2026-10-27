import { test } from 'node:test';
import assert from 'node:assert/strict';
import { IceStorm } from '../src/entities/ice-storm.js';
import { Player } from '../src/entities/player.js';
import { WorldState } from '../src/game/world-state.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { canUpgradeSkill, upgradeSkill } from '../src/systems/skill-system.js';
import { SKILL_DEFINITIONS } from '../src/data/skill-definitions.js';

test('Ice Storm delays damage until impact and delivers exactly four waves even across a long frame', () => {
  const waves = [];
  const storm = new IceStorm({ x: 10, y: 20, radius: 40, onWave: i => waves.push(i) });
  storm.update(649); assert.deepEqual(waves, []);
  storm.update(1); assert.deepEqual(waves, [0]);
  storm.update(3000); assert.deepEqual(waves, [0, 1, 2, 3]); assert.equal(storm.dead, true);
  storm.update(2000); assert.equal(waves.length, 4);
  assert.equal(storm.meteors.length, 12);
  assert.ok(storm.meteors.every(m => Math.hypot(m.x - 10, m.y - 20) <= 40));
});
test('casting spends MP once, keeps save skill id, locks the storm area and respects cooldown', () => {
  const player = new Player('wizard'); player.skills.blizzard = 1; player.dir = 'right';
  const world = new WorldState(player, { shake() {} });
  assert.equal(tryCastSkill(world, 'blizzard').ok, true);
  assert.equal(player.mp, player.maxMp - 16);
  const effect = world.effects[0]; assert.equal(effect.kind, 'iceStorm');
  const targetX = effect.x; player.x += 100; effect.update(450);
  assert.equal(effect.x, targetX);
  assert.equal(tryCastSkill(world, 'blizzard').ok, false);
  assert.equal(world.effects.length, 1);
});
test('preview points can learn the entire wizard tree at level one while normal level gates still apply', () => {
  const player = new Player('wizard'); player.skillTestMode = true; player.skillPoints = 75;
  const branches = new Set(['fire', 'frost', 'arcane']);
  for (const def of Object.values(SKILL_DEFINITIONS).filter(d => branches.has(d.branch))) {
    for (let i = 0; i < def.maxLevel; i++) assert.equal(upgradeSkill(player, def.id).ok, true, def.id);
  }
  assert.equal(player.skillPoints, 0); assert.equal(player.skills.blizzard, 5); assert.equal(player.level, 1);
  const normal = new Player('wizard');
  assert.equal(canUpgradeSkill(normal, 'iceShard').ok, false);
});

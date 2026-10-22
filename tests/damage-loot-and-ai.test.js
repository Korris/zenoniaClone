import { test } from 'node:test';
import assert from 'node:assert/strict';
import { computeDamage, boxesOverlap, circleOverlapsBox, entityBox } from '../src/systems/damage-formula.js';
import { rollDrops } from '../src/systems/loot-system.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';
import { updateMonsterAi, createAiState } from '../src/systems/monster-ai-system.js';

test('computeDamage applies multiplier, variance and defense with a floor of 1', () => {
  assert.equal(computeDamage(20, 4, 1, () => 0.5), 18); // 20*1*1.0 - 2
  assert.equal(computeDamage(20, 4, 1, () => 0), 16); // 0.9 variance
  assert.equal(computeDamage(20, 4, 2, () => 1), 42); // 1.1 variance, mult 2
  assert.equal(computeDamage(1, 100, 1, () => 0.5), 1);
});

test('box and circle overlap helpers', () => {
  assert.equal(boxesOverlap({ x: 0, y: 0, w: 10, h: 10 }, { x: 5, y: 5, w: 10, h: 10 }), true);
  assert.equal(boxesOverlap({ x: 0, y: 0, w: 10, h: 10 }, { x: 10, y: 0, w: 10, h: 10 }), false);
  assert.equal(circleOverlapsBox(0, 0, 5, { x: 3, y: 3, w: 10, h: 10 }), true);
  assert.equal(circleOverlapsBox(0, 0, 2, { x: 3, y: 3, w: 10, h: 10 }), false);
  assert.deepEqual(entityBox({ x: 10, y: 20 }, { w: 4, h: 6 }), { x: 8, y: 14, w: 4, h: 6 });
});

test('rollDrops always gives gold in range and potions by probability', () => {
  const slime = MONSTER_DEFINITIONS.slime;
  const none = rollDrops(slime, () => 0.99); // high roll -> max gold, no potions
  assert.deepEqual(none, [{ kind: 'coin', value: 3 }]);
  const all = rollDrops(slime, () => 0); // low roll -> min gold + both potions
  assert.equal(all[0].value, 1);
  assert.ok(all.some((d) => d.kind === 'hpPotion'));
  assert.ok(all.some((d) => d.kind === 'mpPotion'));
});

test('monster AI chases when player is in aggro range and winds up in attack range', () => {
  const def = MONSTER_DEFINITIONS.goblin;
  const monster = { x: 0, y: 0, def, ai: createAiState(0, 0) };
  updateMonsterAi(monster, { x: 50, y: 0 }, 16, () => 0.5);
  assert.equal(monster.ai.state, 'chase');
  const intent = updateMonsterAi(monster, { x: 50, y: 0 }, 16, () => 0.5);
  assert.ok(intent.moveX > 0.99);
  updateMonsterAi(monster, { x: 5, y: 0 }, 16, () => 0.5);
  assert.equal(monster.ai.state, 'windup');
  const strike = updateMonsterAi(monster, { x: 5, y: 0 }, def.attackWindupMs + 1, () => 0.5);
  assert.equal(strike.attack, true);
  assert.equal(monster.ai.state, 'cooldown');
});

test('monster AI returns home when leashed too far', () => {
  const def = MONSTER_DEFINITIONS.slime;
  const monster = { x: 500, y: 0, def, ai: createAiState(0, 0) };
  monster.ai.state = 'chase';
  const intent = updateMonsterAi(monster, { x: 520, y: 0 }, 16, () => 0.5);
  assert.equal(monster.ai.state, 'return');
  assert.equal(intent.moveX, 0); // state switch frame has no movement
  const next = updateMonsterAi(monster, { x: 900, y: 0 }, 16, () => 0.5);
  assert.ok(next.moveX < 0);
});

test('idle monster starts wandering when its timer expires', () => {
  const def = MONSTER_DEFINITIONS.slime;
  const monster = { x: 0, y: 0, def, ai: createAiState(0, 0) };
  updateMonsterAi(monster, { x: 999, y: 999 }, 1000, () => 0.25);
  assert.equal(monster.ai.state, 'wander');
  const intent = updateMonsterAi(monster, { x: 999, y: 999 }, 16, () => 0.25);
  assert.equal(intent.speedFactor, 0.5);
});

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { expToNext, applyExpGain, applyDeathPenalty, statsForLevel, expProgressRatio } from '../src/systems/leveling-system.js';
import { MAX_LEVEL } from '../src/data/hero-stats-config.js';

test('expToNext follows floor(50 * level^1.5)', () => {
  assert.equal(expToNext(1), 50);
  assert.equal(expToNext(2), 141);
  assert.equal(expToNext(4), 400);
  assert.equal(expToNext(10), 1581);
});

test('applyExpGain levels up once and keeps overflow', () => {
  const result = applyExpGain({ level: 1, exp: 0 }, 60);
  assert.equal(result.level, 2);
  assert.equal(result.exp, 10);
  assert.equal(result.levelsGained, 1);
  assert.equal(result.skillPointsGained, 1);
});

test('applyExpGain can gain several levels at once', () => {
  const result = applyExpGain({ level: 1, exp: 0 }, 200); // 50 + 141 = 191 -> level 3 with 9 left
  assert.equal(result.level, 3);
  assert.equal(result.exp, 9);
  assert.equal(result.levelsGained, 2);
});

test('applyExpGain caps at MAX_LEVEL', () => {
  const result = applyExpGain({ level: MAX_LEVEL, exp: 0 }, 1e9);
  assert.equal(result.level, MAX_LEVEL);
  assert.ok(result.exp < expToNext(MAX_LEVEL));
});

test('negative exp gain is ignored', () => {
  const result = applyExpGain({ level: 2, exp: 30 }, -500);
  assert.deepEqual([result.level, result.exp], [2, 30]);
});

test('death penalty removes 10% of exp and never a level', () => {
  const result = applyDeathPenalty({ level: 5, exp: 100 });
  assert.equal(result.level, 5);
  assert.equal(result.exp, 90);
  assert.equal(result.lost, 10);
  const zero = applyDeathPenalty({ level: 5, exp: 0 });
  assert.equal(zero.exp, 0);
});

test('stats scale with level', () => {
  const lv1 = statsForLevel(1);
  const lv5 = statsForLevel(5);
  assert.equal(lv1.maxHp, 100);
  assert.equal(lv5.maxHp, 148);
  assert.equal(lv5.atk, 22);
  assert.ok(lv5.def > lv1.def);
});

test('expProgressRatio is clamped to [0,1]', () => {
  assert.equal(expProgressRatio({ level: 1, exp: 25 }), 0.5);
  assert.equal(expProgressRatio({ level: 1, exp: 999 }), 1);
});

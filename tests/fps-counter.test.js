import test from 'node:test';
import assert from 'node:assert/strict';
import { FpsCounter } from '../src/ui/fps-counter.js';

test('fps counter averages presented frames and reports the worst frame per window', () => {
  const fps = new FpsCounter();
  let t = 1000;
  for (let i = 0; i < 30; i++) { fps.tick(t); t += i === 10 ? 50 : 1000 / 60; }
  fps.tick(t);
  assert.ok(fps.fps >= 50 && fps.fps <= 60, `fps ${fps.fps}`);
  assert.equal(fps.shownWorstMs, 50);
  assert.equal(fps.color(), '#7dff9a');
});

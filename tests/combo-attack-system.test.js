import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ComboController } from '../src/systems/combo-attack-system.js';
import { COMBO_CONFIG_BY_CLASS } from '../src/data/hero-stats-config.js';

test('every class has three normal attacks followed by the fourth-hit finisher', () => {
  for (const [classId, config] of Object.entries(COMBO_CONFIG_BY_CLASS)) {
    const combo = new ComboController(config);
    assert.equal(config.hits.length, 4, classId);
    for (let step = 1; step <= 4; step++) {
      combo.pressAttack();
      assert.equal(combo.step, step);
      const hit = config.hits[step - 1];
      const events = combo.update(hit.windupMs + hit.activeMs + hit.recoverMs);
      assert.deepEqual(events, [{ type: 'hit', step }, { type: 'end', step }]);
      if (step < 4) {
        assert.equal(combo.multiplierFor(step, 10), hit.mult);
        if (hit.bolt) assert.ok(!hit.bolt.explodeRadius);
      } else {
        assert.ok(combo.multiplierFor(step, 10) > hit.mult);
        assert.equal(combo.lastStep, 0);
        if (hit.bolt) assert.ok(hit.bolt.explodeRadius > 0);
      }
    }
  }
});

const CONFIG = {
  chainWindowMs: 400,
  hits: [
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.0 },
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.15 },
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.25 },
    { windupMs: 90, activeMs: 120, recoverMs: 220, mult: 1.8 },
  ],
  finisherMultPerLevel: 0.05,
};

function run(combo, ms, step = 10) {
  const events = [];
  for (let t = 0; t < ms; t += step) events.push(...combo.update(step));
  return events;
}

test('first press starts hit 1 and emits a hit event when active', () => {
  const combo = new ComboController(CONFIG);
  assert.equal(combo.pressAttack(), true);
  assert.equal(combo.state, 'windup');
  const events = run(combo, 60);
  assert.deepEqual(events[0], { type: 'hit', step: 1 });
  assert.equal(combo.state, 'active');
});

test('a buffered press chains into hit 2 after recovery', () => {
  const combo = new ComboController(CONFIG);
  combo.pressAttack();
  run(combo, 100);
  assert.equal(combo.pressAttack(), false); // buffered during swing
  const events = run(combo, 200);
  assert.ok(events.some((e) => e.type === 'end' && e.step === 1));
  assert.ok(events.some((e) => e.type === 'hit' && e.step === 2));
});

test('pressing within the chain window continues the combo', () => {
  const combo = new ComboController(CONFIG);
  combo.pressAttack();
  run(combo, 260); // hit 1 fully done (250ms)
  assert.equal(combo.state, 'idle');
  assert.equal(combo.lastStep, 1);
  run(combo, 200); // still inside 400ms window
  combo.pressAttack();
  assert.equal(combo.step, 2);
});

test('pressing after the chain window resets to hit 1', () => {
  const combo = new ComboController(CONFIG);
  combo.pressAttack();
  run(combo, 260);
  run(combo, 450); // window expired
  assert.equal(combo.lastStep, 0);
  combo.pressAttack();
  assert.equal(combo.step, 1);
});

test('finisher (hit 4) resets the chain and ignores buffered presses', () => {
  const combo = new ComboController(CONFIG);
  combo.pressAttack();
  run(combo, 260);
  combo.pressAttack();
  run(combo, 260);
  combo.pressAttack();
  assert.equal(combo.step, 3);
  run(combo, 260);
  combo.pressAttack();
  assert.equal(combo.step, 4);
  combo.pressAttack(); // nothing past hit 4
  assert.equal(combo.queued, false);
  run(combo, 450);
  assert.equal(combo.state, 'idle');
  assert.equal(combo.lastStep, 0);
  assert.equal(combo.chainTimer, 0);
});

test('finisher multiplier scales with hero level', () => {
  const combo = new ComboController(CONFIG);
  assert.equal(combo.multiplierFor(1, 10), 1.0);
  assert.equal(combo.multiplierFor(4, 1), 1.8);
  assert.ok(Math.abs(combo.multiplierFor(4, 11) - 2.3) < 1e-9);
});

test('reset returns to a clean idle state', () => {
  const combo = new ComboController(CONFIG);
  combo.pressAttack();
  run(combo, 100);
  combo.reset();
  assert.equal(combo.state, 'idle');
  assert.equal(combo.step, 0);
  assert.equal(combo.displayCount, 0);
});

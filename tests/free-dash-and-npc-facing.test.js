// Free (double-tap) dash easing and NPC talk facing.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { easedDashTravel } from '../src/systems/skill-cast-system.js';
import { Npc } from '../src/entities/npc.js';
import { facingFromVector } from '../src/game/player-controller.js';

test('hero faces sideways on all diagonals and vertically only for straight movement', () => {
  for (const y of [-1, 1]) {
    assert.equal(facingFromVector(-1, y, 'down', true), 'left');
    assert.equal(facingFromVector(1, y, 'down', true), 'right');
  }
  assert.equal(facingFromVector(0, -1, 'left', true), 'up');
  assert.equal(facingFromVector(0, 1, 'right', true), 'down');
  assert.equal(facingFromVector(0, 0, 'left', true), 'left');
  assert.equal(facingFromVector(0.1, 1, 'left'), 'down');
});

const DASH = { distance: 48, durationMs: 200, exitSpeed: 80 };
const speedAt = (t) => (easedDashTravel(DASH, t + 0.5) - easedDashTravel(DASH, t - 0.5)) * 1000; // px/s

test('eased dash covers its full distance, never backtracks, and stops at the end', () => {
  assert.equal(easedDashTravel(DASH, 0), 0);
  assert.ok(Math.abs(easedDashTravel(DASH, DASH.durationMs) - DASH.distance) < 1e-9);
  assert.equal(easedDashTravel(DASH, 999), easedDashTravel(DASH, DASH.durationMs));
  for (let t = 1; t <= DASH.durationMs; t++) assert.ok(easedDashTravel(DASH, t) > easedDashTravel(DASH, t - 1));
});

test('eased dash bursts fast and decelerates to walking speed (no jump when walking resumes)', () => {
  assert.ok(speedAt(1) > 4 * DASH.exitSpeed, `start ${speedAt(1)}`);
  assert.ok(speedAt(100) < speedAt(10));
  assert.ok(Math.abs(speedAt(DASH.durationMs - 0.5) - DASH.exitSpeed) < 5, `end ${speedAt(DASH.durationMs - 0.5)}`);
});

test('NPC turns only left/right toward a talking hero; above/below gets the front pose', () => {
  const npc = new Npc('hunterLena', 100, 100); // resting pose is 'left'
  npc.faceTowards(130, 102);
  assert.equal(npc.dir, 'right');
  npc.faceTowards(70, 98);
  assert.equal(npc.dir, 'left');
  npc.faceTowards(101, 130); // hero below
  assert.equal(npc.dir, 'down');
  npc.faceTowards(99, 70); // hero above: never shows its back
  assert.equal(npc.dir, 'down');
  npc.resetFacing();
  assert.equal(npc.dir, 'left');
});

import test from 'node:test';
import assert from 'node:assert/strict';
import { Camera } from '../src/core/camera.js';
import { captureInterpolationSnapshot, renderInterpolated } from '../src/game/render-interpolation.js';

test('camera render offset snaps to screen pixels, not whole world pixels', () => {
  const camera = new Camera();
  camera.x = 10.3; camera.y = 20.6;
  assert.equal(camera.renderX, 10.25);
  assert.equal(camera.renderY, 20.5);
});

test('render blends positions between logic steps and restores them afterwards', () => {
  const player = { x: 0, y: 0 };
  const monster = { x: 50, y: 50 };
  const camera = { x: 0, y: 0 };
  const world = { player, camera, monsters: [monster], texts: [{ text: 'no position yet' }] };
  captureInterpolationSnapshot(world);
  player.x = 2; camera.x = 2; monster.y = 54;
  let seen;
  renderInterpolated(world, 0.25, () => { seen = { px: player.x, cx: camera.x, my: monster.y }; });
  assert.deepEqual(seen, { px: 0.5, cx: 0.5, my: 51 });
  assert.deepEqual([player.x, camera.x, monster.y], [2, 2, 54]);
});

test('teleports and newly spawned things draw at their real position', () => {
  const player = { x: 0, y: 0 };
  const world = { player, monsters: [] };
  captureInterpolationSnapshot(world);
  player.x = 400; // map change
  const spawned = { x: 10, y: 10 };
  world.monsters.push(spawned);
  renderInterpolated(world, 0.5, () => assert.deepEqual([player.x, spawned.x], [400, 10]));
});

test('positions are restored even if drawing throws', () => {
  const player = { x: 0, y: 0 };
  const world = { player };
  captureInterpolationSnapshot(world);
  player.x = 4;
  assert.throws(() => renderInterpolated(world, 0.5, () => { throw new Error('boom'); }));
  assert.equal(player.x, 4);
});

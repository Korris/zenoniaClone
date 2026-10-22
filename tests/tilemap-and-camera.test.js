import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Tilemap } from '../src/world/tilemap.js';
import { Camera } from '../src/core/camera.js';
import { VIEW_WIDTH, VIEW_HEIGHT, TILE_SIZE } from '../src/core/constants.js';
import { MAP_REGISTRY } from '../src/world/map-registry.js';

const rows = [
  'CCCC',
  'CggC',
  'CgwC',
  'CCCC',
];

test('tilemap reports solidity and out-of-bounds as solid', () => {
  const tm = new Tilemap(rows);
  assert.equal(tm.width, 4);
  assert.equal(tm.isSolidTile(1, 1), false);
  assert.equal(tm.isSolidTile(0, 0), true);
  assert.equal(tm.isSolidTile(2, 2), true); // water
  assert.equal(tm.isSolidTile(-1, 0), true);
  assert.equal(tm.isSolidTile(99, 99), true);
});

test('moveBox stops at walls and slides along the free axis', () => {
  const tm = new Tilemap(rows);
  const box = { x: 18, y: 18, w: 8, h: 8 }; // inside tile (1,1)
  const moved = tm.moveBox(box, 30, 0); // cliff at x=48
  assert.equal(moved.x, 40);
  assert.equal(moved.blockedX, true);
  const diagonal = tm.moveBox(box, 30, 4);
  assert.equal(diagonal.x, 40);
  assert.equal(diagonal.y, 22);
  assert.equal(diagonal.blockedY, false);
});

test('findWalkableNear returns the tile itself when free, else a neighbour', () => {
  const tm = new Tilemap(rows);
  assert.deepEqual(tm.findWalkableNear(1, 1), { tx: 1, ty: 1 });
  const near = tm.findWalkableNear(0, 0, 2);
  assert.ok(near && !tm.isSolidTile(near.tx, near.ty));
});

test('all registered maps have rectangular rows and walkable starts', () => {
  for (const def of Object.values(MAP_REGISTRY)) {
    const tm = new Tilemap(def.rows);
    assert.ok(def.rows.every((r) => r.length === tm.width), `${def.id} ragged`);
    assert.equal(tm.isSolidTile(def.playerStart.tx, def.playerStart.ty), false, `${def.id} start solid`);
    for (const p of def.portals) assert.equal(tm.charAt(p.tx, p.ty), 'P', `${def.id} portal missing`);
  }
});

test('camera centers on target and clamps to map bounds', () => {
  const cam = new Camera();
  cam.setMapSize(40, 30);
  cam.follow(0, 0);
  assert.deepEqual([cam.x, cam.y], [0, 0]);
  cam.follow(40 * TILE_SIZE, 30 * TILE_SIZE);
  assert.equal(cam.x, 40 * TILE_SIZE - VIEW_WIDTH);
  assert.equal(cam.y, 30 * TILE_SIZE - VIEW_HEIGHT);
  cam.follow(320, 240);
  assert.equal(cam.x, 320 - VIEW_WIDTH / 2);
});

test('camera centers maps smaller than the view', () => {
  const cam = new Camera();
  cam.setMapSize(10, 5);
  cam.follow(0, 0);
  assert.equal(cam.x, (10 * TILE_SIZE - VIEW_WIDTH) / 2);
});

test('camera shake decays to zero offset', () => {
  const cam = new Camera();
  cam.shake(4, 50);
  cam.update(16, () => 1);
  assert.equal(cam.shakeOffsetX, 4);
  cam.update(100, () => 1);
  cam.update(16, () => 1);
  assert.equal(cam.shakeOffsetX, 0);
});

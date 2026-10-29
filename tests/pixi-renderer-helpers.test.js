import test from 'node:test';
import assert from 'node:assert/strict';
import { resolveRendererMode, parseRendererMode, otherRendererMode, searchWithRenderer, readRendererMode, switchRendererMode, RENDERER_STORAGE_KEY } from '../src/graphics/pixi/renderer-mode.js';
import { depthKey, snapToDevice } from '../src/graphics/pixi/depth-key.js';
import { RetainedSet, NodePool } from '../src/graphics/pixi/retained-set.js';
import { monsterSpec } from '../src/graphics/pixi/pixi-monster-spec.js';

const fakeWindow = (search = '', stored = null) => {
  const store = new Map(stored ? [[RENDERER_STORAGE_KEY, stored]] : []);
  return { location: { search }, localStorage: { getItem: (k) => store.get(k) ?? null, setItem: (k, v) => store.set(k, v) }, store };
};

test('renderer mode: default canvas, query beats storage, junk ignored', () => {
  assert.equal(resolveRendererMode('', null), 'canvas');
  assert.equal(resolveRendererMode('?renderer=pixi', null), 'pixi');
  assert.equal(resolveRendererMode('?renderer=PIXI', 'canvas'), 'pixi');
  assert.equal(resolveRendererMode('', 'pixi'), 'pixi');
  assert.equal(resolveRendererMode('?renderer=webgpu', 'pixi'), 'pixi');
  assert.equal(parseRendererMode(null), null);
  assert.equal(otherRendererMode('pixi'), 'canvas');
  assert.equal(searchWithRenderer('?a=1', 'pixi'), '?a=1&renderer=pixi');
});

test('renderer mode persists the explicit choice and F9 flips it', () => {
  const win = fakeWindow('?renderer=pixi');
  assert.equal(readRendererMode(win), 'pixi');
  assert.equal(win.store.get(RENDERER_STORAGE_KEY), 'pixi');
  assert.equal(switchRendererMode(win), 'canvas');
  assert.match(win.location.search, /renderer=canvas/);
  assert.equal(readRendererMode({ location: { search: '' }, localStorage: win.localStorage }), 'canvas');
  assert.equal(readRendererMode({ location: { search: '' }, localStorage: null }), 'canvas'); // storage blocked
});

test('depth keys reproduce the canvas y-sort including ties', () => {
  assert.ok(depthKey('player', 50) > depthKey('monster', 50));
  assert.ok(depthKey('monster', 50) > depthKey('npc', 50));
  assert.ok(depthKey('npc', 50) > depthKey('prop', 50));
  assert.ok(depthKey('prop', 51) > depthKey('player', 50));
  assert.equal(snapToDevice(1.13, 4), 1.25);
});

test('RetainedSet creates once per key, releases unseen keys, pool recycles nodes', () => {
  const pool = new NodePool(() => ({ id: Math.random() }), (n) => { n.hidden = true; });
  const set = new RetainedSet(() => { const n = pool.take(); n.hidden = false; return n; }, (n) => pool.put(n));
  const a = {}, b = {};
  const na = set.get(a), nb = set.get(b);
  assert.equal(set.get(a), na);
  assert.equal(set.endFrame(), 0);
  set.get(a);
  assert.equal(set.endFrame(), 1); // b not drawn this frame
  assert.equal(nb.hidden, true);
  const c = {};
  assert.equal(set.get(c), nb); // recycled, not rebuilt
  assert.equal(pool.created, 2);
  set.endFrame();
  set.clear();
  assert.equal(set.size, 0);
});

const sprite = { width: 16, height: 16, frames: { down: ['f0', 'f1', 'f2', 'f3'] }, hitFlash: { down: 'flash' }, hitRim: { down: 'rim' } };
const monster = (over = {}) => ({ type: 'slime', dir: 'down', x: 100, y: 80, hitFlashTimer: 0, animTimer: 0, airborneMs: 0, ai: { state: 'idle', timer: 0 },
  def: { sprite: 'slime' }, isBoss: false, ...over });

test('monsterSpec: pixel monster rect, flash swaps frame and adds rim, squash keeps feet planted', () => {
  const registry = { monsters: { slime: sprite } };
  const calm = monsterSpec(monster(), registry);
  assert.equal(calm.frame, 'f0');
  assert.deepEqual(calm.rect, { x: 92, y: 64, w: 16, h: 16 });
  assert.equal(calm.rim, null);
  const hit = monsterSpec(monster({ hitFlashTimer: 110 }), registry);
  assert.equal(hit.frame, 'flash');
  assert.equal(hit.rim.frame, 'rim');
  assert.ok(hit.rect.w > 16 && hit.rect.h < 16);
  assert.ok(Math.abs(hit.rect.y + hit.rect.h - 80) < 1e-9); // bottom edge stays at the feet
});

test('monsterSpec: boss mirrors facing right, unknown monster has no spec', () => {
  const strip = ['b0', 'b1'];
  const registry = { monsters: {}, boss: { idle: strip, walk: strip, hit: strip, width: 32, height: 32 } };
  const boss = monsterSpec(monster({ type: 'golem', isBoss: true, dir: 'right', def: {} }), registry);
  assert.equal(boss.flipX, true);
  assert.equal(boss.markers, false);
  assert.equal(monsterSpec(monster({ type: 'ghost' }), { monsters: {} }), null);
});

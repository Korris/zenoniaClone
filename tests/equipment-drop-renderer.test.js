import test from 'node:test';
import assert from 'node:assert/strict';
import { drawDrops } from '../src/graphics/effects-renderer.js';
import { itemSprite } from '../src/graphics/weapon-icons.js';

test('loot opacity pulses smoothly and never hides, including near expiration', () => {
  const stack = [], alphas = [];
  const ctx = {
    globalAlpha: 1,
    save() { stack.push(this.globalAlpha); }, restore() { this.globalAlpha = stack.pop(); },
    beginPath() {}, ellipse() {}, fill() {}, drawImage() { alphas.push(this.globalAlpha); },
  };
  const registry = { items: { sword: { width: 24, height: 24 } } };
  const drop = { kind: 'equipment', item: { slot: 'weapon', templateId: 'sword', itemLevel: 1, rarity: 'common' }, x: 0, y: 0, bobOffset: 0 };
  for (const [age, lifeMs] of [[0, 20000], [225, 19775], [450, 19550], [675, 19325], [900, 19100], [18000, 2000], [18100, 1900]]) {
    drawDrops(ctx, [{ ...drop, age, lifeMs }], registry);
    assert.equal(ctx.globalAlpha, 1, 'restore opacity for the rest of the world');
  }
  assert.equal(alphas.length, 7);
  assert.ok(alphas.every(alpha => alpha >= 0.63 && alpha <= 1));
  assert.ok(Math.abs(alphas[0] - 1) < 0.001);
  assert.ok(Math.abs(alphas[2] - 0.64) < 0.001);
  assert.ok(Math.abs(alphas[4] - 1) < 0.001);
});

test('ground items use the exact inventory artwork at 10% larger size', () => {
  const registry = {
    items: { leatherCap: { width: 24, height: 24 } },
    inventoryItems: { leatherCap: { width: 320, height: 320 } },
    weaponIcons: Array.from({ length: 30 }, () => ({ width: 200, height: 180 })),
  };
  const calls = [];
  const ctx = {
    save() {}, restore() {}, beginPath() {}, ellipse() {}, fill() {},
    drawImage(...args) { calls.push(args); },
  };
  for (const classId of ['warrior', 'wizard', 'mechanic']) {
    for (const item of [
      { slot: 'helmet', templateId: 'leatherCap', itemLevel: 5 },
      { slot: 'weapon', templateId: 'sword', itemLevel: 5 },
      { slot: 'weapon', templateId: 'sword', itemLevel: 95, classId: 'wizard' },
    ]) {
      drawDrops(ctx, [{ kind: 'equipment', item: { rarity: 'common', ...item }, x: 100, y: 100, bobOffset: 0, lifeMs: 20000 }], registry, classId);
      const [sprite, , , w, h] = calls.at(-1);
      assert.equal(sprite, itemSprite(registry, item, classId, true));
      assert.ok(Math.abs(Math.max(w, h) - 6 * 1.1) < 1e-10);
      assert.ok(Math.abs(w / h - sprite.width / sprite.height) < 1e-10);
    }
  }
});

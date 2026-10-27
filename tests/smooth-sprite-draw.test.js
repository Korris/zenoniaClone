import test from 'node:test';
import assert from 'node:assert/strict';

// Minimal OffscreenCanvas double: records the size each pre-shrunk level is built at.
globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {} }; }
};
const { pickSpriteLevel, drawSmoothImage } = await import('../src/graphics/smooth-sprite-draw.js');

test('picks the smallest pre-shrunk level that still covers the on-screen size', () => {
  const src = { width: 384, height: 384 };
  assert.equal(pickSpriteLevel(src, 400, 400).level, src); // upscaling: full size
  assert.equal(pickSpriteLevel(src, 200, 200).level, src); // 384 -> 200 is under 2x
  const { level, factor } = pickSpriteLevel(src, 150, 150);
  assert.deepEqual([level.width, factor], [192, 0.5]);
  assert.equal(pickSpriteLevel(src, 40, 40).level.width, 48);
  assert.equal(pickSpriteLevel(src, 1, 1).level.width, 12); // stops near MIN_LEVEL_SIZE
  assert.equal(pickSpriteLevel(src, 150, 150).level, level, 'levels are cached per source');
});

test('draws with cheap filtering at the size the device actually shows', () => {
  const src = { width: 192, height: 192 };
  const calls = [];
  const ctx = { getTransform: () => ({ a: 5, b: 0, c: 0, d: 5 }), drawImage: (...a) => calls.push(a) };
  drawSmoothImage(ctx, src, 0, 0, 24, 24); // 24 units * 5 = 120 device px
  assert.equal(ctx.imageSmoothingQuality, 'low');
  assert.equal(calls[0][0].width, 192); // 192 -> 120 is under 2x, no shrink needed
  drawSmoothImage(ctx, src, 0, 0, 8, 8); // 40 device px
  assert.equal(calls[1][0].width, 48);
});

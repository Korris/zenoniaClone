import { test } from 'node:test';
import assert from 'node:assert/strict';
import { SpriteAnimation } from '../src/graphics/sprite-animation.js';

test('looping animation cycles through frames repeatedly', () => {
  const frames = ['frame0', 'frame1', 'frame2'];
  const anim = new SpriteAnimation(frames, 100, true);

  assert.equal(anim.current, 'frame0');
  assert.equal(anim.finished, false);

  anim.update(100);
  assert.equal(anim.current, 'frame1');

  anim.update(100);
  assert.equal(anim.current, 'frame2');

  anim.update(100);
  assert.equal(anim.current, 'frame0'); // wraps back
  assert.equal(anim.finished, false);
});

test('non-looping animation stops at last frame and sets finished', () => {
  const frames = ['frame0', 'frame1', 'frame2'];
  const anim = new SpriteAnimation(frames, 100, false);

  assert.equal(anim.finished, false);

  anim.update(100);
  assert.equal(anim.current, 'frame1');
  assert.equal(anim.finished, false);

  anim.update(100);
  assert.equal(anim.current, 'frame2');
  assert.equal(anim.finished, false);

  anim.update(100);
  assert.equal(anim.current, 'frame2'); // stays on last frame
  assert.equal(anim.finished, true);
});

test('finished animation ignores further updates', () => {
  const frames = ['frame0', 'frame1'];
  const anim = new SpriteAnimation(frames, 50, false);

  anim.update(50);
  anim.update(50);
  assert.equal(anim.finished, true);
  assert.equal(anim.index, 1);

  anim.update(100);
  assert.equal(anim.index, 1); // no change
  assert.equal(anim.current, 'frame1');
});

test('reset clears elapsed, index, and finished state', () => {
  const frames = ['frame0', 'frame1', 'frame2'];
  const anim = new SpriteAnimation(frames, 100, false);

  anim.update(100);
  anim.update(100);
  anim.update(100);
  assert.equal(anim.finished, true);
  assert.equal(anim.index, 2);

  anim.reset();
  assert.equal(anim.index, 0);
  assert.equal(anim.elapsed, 0);
  assert.equal(anim.finished, false);
  assert.equal(anim.current, 'frame0');
});

test('partial frame advances track elapsed time correctly', () => {
  const frames = ['frame0', 'frame1', 'frame2'];
  const anim = new SpriteAnimation(frames, 100, true);

  anim.update(60);
  assert.equal(anim.index, 0);
  assert.equal(anim.elapsed, 60);

  anim.update(30);
  assert.equal(anim.index, 0);
  assert.equal(anim.elapsed, 90);

  anim.update(20);
  assert.equal(anim.index, 1);
  assert.equal(anim.elapsed, 10);
});

test('multiple frames advanced in single update', () => {
  const frames = ['frame0', 'frame1', 'frame2', 'frame3'];
  const anim = new SpriteAnimation(frames, 50, true);

  anim.update(180);
  assert.equal(anim.index, 3);
  assert.equal(anim.elapsed, 30);
});

test('single frame animation loops correctly', () => {
  const frames = ['only'];
  const anim = new SpriteAnimation(frames, 100, true);

  assert.equal(anim.current, 'only');
  anim.update(100);
  assert.equal(anim.index, 0);
  assert.equal(anim.current, 'only');
  assert.equal(anim.finished, false);
});

test('single frame non-looping animation finishes immediately after one frame duration', () => {
  const frames = ['only'];
  const anim = new SpriteAnimation(frames, 100, false);

  anim.update(100);
  assert.equal(anim.index, 0);
  assert.equal(anim.finished, true);
  assert.equal(anim.current, 'only');
});

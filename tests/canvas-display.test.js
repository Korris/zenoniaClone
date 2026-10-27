import test from 'node:test';
import assert from 'node:assert/strict';
import { configureCanvasDisplay, logicalPointerPosition, setScreenTransform, setWorldTransform } from '../src/core/canvas-display.js';

test('HiDPI buffer tracks display density while logical layout and pointer coordinates stay fixed', () => {
  const canvas = { style: {}, width: 960, height: 640,
    getBoundingClientRect: () => ({ left: 10, top: 20, width: 768, height: 512 }) };
  configureCanvasDisplay(canvas, 768, 512, 2);
  assert.equal(canvas.width, 1536);
  assert.equal(canvas.height, 1024);
  assert.equal(canvas.style.width, '768px');
  assert.deepEqual(logicalPointerPosition(canvas, 394, 276), { x: 480, y: 320 });
  configureCanvasDisplay(canvas, 1920, 1080, 1.5);
  assert.equal(canvas.width, 1440);
  assert.equal(canvas.height, 960);
  assert.equal(canvas.style.width, '960px');
});

test('screen/world transforms use physical buffer scale and align camera to device pixels', () => {
  const calls = [];
  const ctx = { canvas: { width: 1920, height: 1280 }, setTransform(...args) { calls.push(args); } };
  setScreenTransform(ctx);
  assert.deepEqual(calls.pop(), [2, 0, 0, 2, 0, 0]);
  setWorldTransform(ctx, { renderX: 10.1, renderY: 20.2 });
  assert.deepEqual(calls.pop(), [8, 0, 0, 8, -81, -162]);
});

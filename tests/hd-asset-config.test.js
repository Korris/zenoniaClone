import { test } from 'node:test';
import assert from 'node:assert/strict';
import { unitsFor, HD_HERO, HD_HERO_ANIMATIONS, HD_HERO_SIDE_ROWS, HERO_FRAME } from '../src/graphics/asset-atlas.js';

test('unitsFor converts native pixel width to world units using pixelsPerUnit', () => {
  assert.equal(unitsFor({ width: 96, pixelsPerUnit: 4 }), 24);
  assert.equal(unitsFor({ width: 24, pixelsPerUnit: 1 }), 24);
  assert.equal(unitsFor({ width: 24 }), 24); // defaults to 1 px/unit (pixel-art sprites don't set it)
});

test('HD_HERO frame matches the pixel hero frame scaled by pixelsPerUnit', () => {
  assert.equal(HD_HERO.pixelsPerUnit, 8);
  assert.equal(HD_HERO.frame, HERO_FRAME * HD_HERO.pixelsPerUnit);
  assert.equal(unitsFor({ width: HD_HERO.frame, pixelsPerUnit: HD_HERO.pixelsPerUnit }), HERO_FRAME);
});

test('HD_HERO_ANIMATIONS covers the same 8 animations as the pixel hero set', () => {
  const expected = ['idle', 'walk', 'attack1', 'attack2', 'attack3', 'dash', 'hurt', 'death'];
  assert.deepEqual(Object.keys(HD_HERO_ANIMATIONS).sort(), expected.sort());
  for (const key of Object.values(HD_HERO_ANIMATIONS)) assert.equal(typeof key, 'string');
});

test('HD_HERO.animations is the same object as the exported HD_HERO_ANIMATIONS table', () => {
  assert.equal(HD_HERO.animations, HD_HERO_ANIMATIONS);
});

test('every HD hero animation declares which sheet rows face left / right', () => {
  for (const anim of Object.keys(HD_HERO_ANIMATIONS)) {
    const side = HD_HERO_SIDE_ROWS[anim];
    assert.ok(side, `${anim}: missing HD_HERO_SIDE_ROWS entry`);
    assert.ok([2, 3].includes(side.right), `${anim}: right must be row 2 or 3`);
    assert.ok([2, 3, 'mirror'].includes(side.left) && side.left !== side.right, `${anim}: bad left row`);
  }
});

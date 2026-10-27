// Pure config checks for the HD monster pipeline (no DOM): every HD monster type is a defined
// monster with a pixel fallback sheet, its sheet key resolves to a file in ASSET_FILES, and the
// normalized PNG has exactly the frame grid the registry slices (cols*96 x rows*96) -- read via the
// PNG IHDR chunk like hd-npcs-config.test.js.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { ASSET_FILES, MONSTER_SHEETS } from '../src/graphics/asset-atlas.js';
import { HD_MONSTER, HD_MONSTER_ATTACK, HD_MONSTERS, monsterSheetKey } from '../src/graphics/asset-atlas-hd-monsters.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';
import { drawMonster } from '../src/graphics/monster-renderer.js';

const ASSET_ROOT = 'assets/';
const pngSize = (path) => { const buf = readFileSync(path); return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) }; };

test('HD monsters select actual rear frames when chasing upward, including hit flash', () => {
  assert.deepEqual(HD_MONSTER.rows, ['down', 'up', 'left', 'right']);
  const images = [];
  const ctx = { beginPath() {}, ellipse() {}, fill() {}, drawImage(frame) { images.push(frame); } };
  const up = {}, down = {}, flash = {};
  const registry = { monstersHd: { spectralWolf: { frames: { up: [up], down: [down] }, hitFlash: { up: flash }, bodyUnits: 19.5 } } };
  const monster = { type: 'spectralWolf', dir: 'up', x: 0, y: 0, animTimer: 0, ai: { state: 'chase' }, hp: 10, maxHp: 10, hitFlashTimer: 0 };
  drawMonster(ctx, monster, registry);
  assert.equal(images.pop(), up);
  monster.hitFlashTimer = 50;
  drawMonster(ctx, monster, registry);
  assert.equal(images.pop(), flash);
  monster.dir = 'down'; monster.hitFlashTimer = 0;
  drawMonster(ctx, monster, registry);
  assert.equal(images.pop(), down);
});

test('every HD monster type is a defined monster with a pixel-art fallback sheet', () => {
  for (const type of Object.keys(HD_MONSTERS)) {
    assert.ok(MONSTER_DEFINITIONS[type], `HD_MONSTERS has unknown monster "${type}"`);
    assert.ok(MONSTER_SHEETS[type], `${type}: no pixel fallback in MONSTER_SHEETS`);
  }
});

test('every non-boss monster definition names a sheet in MONSTER_SHEETS', () => {
  for (const [type, def] of Object.entries(MONSTER_DEFINITIONS)) {
    if (def.behavior === 'boss') continue;
    assert.ok(MONSTER_SHEETS[type], `${type} missing from MONSTER_SHEETS`);
  }
});

test('HD monster sheet files exist with the sliced grid size and a sane body height', () => {
  const { frame, cols, rows } = HD_MONSTER;
  for (const [type, { bodyHeight }] of Object.entries(HD_MONSTERS)) {
    const key = monsterSheetKey(type);
    assert.ok(ASSET_FILES[key], `${type}: "${key}" missing from ASSET_FILES`);
    const path = ASSET_ROOT + ASSET_FILES[key];
    assert.ok(existsSync(path), `${path} missing -- run npm run hd:monsters`);
    assert.deepEqual(pngSize(path), { width: cols * frame, height: rows.length * frame }, `${path} has the wrong size`);
    assert.ok(bodyHeight > 0 && bodyHeight <= frame, `${type}: bodyHeight ${bodyHeight} outside the frame`);
  }
});

test('every Frost Hollow monster has a four-direction attack sheet with room for extended strikes', () => {
  for (const type of Object.keys(HD_MONSTERS)) {
    const path = ASSET_ROOT + ASSET_FILES[monsterSheetKey(type) + 'Attack'];
    assert.ok(existsSync(path), `${type}: missing attack sheet`);
    assert.deepEqual(pngSize(path), { width: HD_MONSTER_ATTACK.frame * 4, height: HD_MONSTER_ATTACK.frame * 4 });
  }
});

test('directional attacks anticipate during windup, strike at damage time, recover once and return to idle', () => {
  for (const type of Object.keys(HD_MONSTERS)) for (const dir of HD_MONSTER.rows) {
    const def = MONSTER_DEFINITIONS[type], rendered = [];
    const ctx = { beginPath() {}, ellipse() {}, fill() {}, fillRect() {}, drawImage(...args) { rendered.push(args); } };
    const idle = {}, frames = [{}, {}, {}, {}];
    const hd = { frames: { [dir]: [idle] }, attack: { [dir]: frames }, hitFlash: {}, bodyUnits: 19 };
    const m = { type, def, dir, x: 100, y: 100, animTimer: 9999, hitFlashTimer: 0, hp: 10, maxHp: 10, ai: {} };
    const draw = (state, timer, expected) => {
      m.ai = { state, timer };
      drawMonster(ctx, m, { monstersHd: { [type]: hd } });
      const [frame, x, y, w, h] = rendered.at(-1);
      assert.equal(frame, expected, `${type}/${dir}/${state}/${timer}`);
      if (frames.includes(expected)) {
        const scale = def.renderScale || 1;
        assert.equal(w, 32 * scale);
        assert.equal(h, w);
        assert.ok(Math.abs(y + 28 * scale - 100) <= 0.5, 'Feet must stay anchored during extended attack poses');
        assert.ok(Math.abs(x + w / 2 - 100) <= 0.5);
      }
    };
    draw('windup', def.attackWindupMs, frames[0]);
    draw('windup', def.attackWindupMs / 2, frames[1]);
    draw('cooldown', def.attackCooldownMs, frames[2]);
    draw('cooldown', def.attackCooldownMs - 120, frames[3]);
    draw('cooldown', def.attackCooldownMs - 240, idle);
    draw('idle', 600, idle);
    hd.attack = {}; // Missing artwork falls back to the existing animation.
    draw('cooldown', def.attackCooldownMs, idle);
  }
});

// Pure config checks for the HD monster pipeline (no DOM): every HD monster type is a defined
// monster with a pixel fallback sheet, its sheet key resolves to a file in ASSET_FILES, and the
// normalized PNG has exactly the frame grid the registry slices (cols*96 x rows*96) -- read via the
// PNG IHDR chunk like hd-npcs-config.test.js.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { ASSET_FILES, MONSTER_SHEETS } from '../src/graphics/asset-atlas.js';
import { HD_MONSTER, HD_MONSTERS, monsterSheetKey } from '../src/graphics/asset-atlas-hd-monsters.js';
import { MONSTER_DEFINITIONS } from '../src/data/monster-definitions.js';

const ASSET_ROOT = 'assets/';
const pngSize = (path) => { const buf = readFileSync(path); return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) }; };

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

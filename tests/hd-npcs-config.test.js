// Pure config checks for the HD NPC pipeline (no DOM): every HD NPC id is a real NPC definition,
// its sheet/portrait keys resolve to files in ASSET_FILES, and the normalized PNGs have exactly the
// frame grid the registry slices (cols*96 x rows*96) and a 152x152 portrait -- read via the PNG
// IHDR chunk like hd-props-config.test.js.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { ASSET_FILES, NPC_SHEETS } from '../src/graphics/asset-atlas.js';
import { HD_NPC, HD_NPC_IDS, sheetKey, faceKey } from '../src/graphics/asset-atlas-hd-npcs.js';
import { NPC_DEFINITIONS } from '../src/data/npc-definitions.js';
import { MAP_REGISTRY } from '../src/world/map-registry.js';

const ASSET_ROOT = 'assets/';
const pngSize = (path) => { const buf = readFileSync(path); return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) }; };

test('every HD NPC id is a defined NPC', () => {
  for (const id of HD_NPC_IDS) assert.ok(NPC_DEFINITIONS[id], `HD_NPC_IDS has unknown npc "${id}"`);
});

test('every NPC definition names a pixel-art sheet in NPC_SHEETS (drawNpc has no other fallback)', () => {
  for (const def of Object.values(NPC_DEFINITIONS)) assert.ok(NPC_SHEETS[def.sprite], `${def.id}: sprite "${def.sprite}" is not in NPC_SHEETS`);
});

test('HD NPC sheet + portrait files exist with the sliced grid size', () => {
  const { frame, cols, rows } = HD_NPC;
  for (const id of HD_NPC_IDS) {
    for (const [key, expected] of [[sheetKey(id), { width: cols * frame, height: rows.length * frame }], [faceKey(id), { width: 152, height: 152 }]]) {
      assert.ok(ASSET_FILES[key], `${id}: "${key}" missing from ASSET_FILES`);
      const path = ASSET_ROOT + ASSET_FILES[key];
      assert.ok(existsSync(path), `${path} missing -- run npm run hd:npcs`);
      assert.deepEqual(pngSize(path), expected, `${path} has the wrong size`);
    }
  }
});

test('all 10 Oakhaven villagers stand in Oakhaven Village, each exactly once', () => {
  const placed = MAP_REGISTRY.oakhavenVillage.npcs.map((n) => n.id);
  for (const id of HD_NPC_IDS) assert.equal(placed.filter((p) => p === id).length, 1, `${id} placed ${placed.filter((p) => p === id).length} times`);
});

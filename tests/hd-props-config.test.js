// Pure config checks for the HD prop pipeline (no DOM): every prop type actually used on a map has
// either HD art or the pixel-art fallback (never silently missing both), and every HD_PROPS entry
// resolves to a normalized PNG sized exactly (def.width*4) x (def.height*4) -- read via the PNG
// IHDR chunk, no image-decoding dependency needed (same approach as hd-tiles-config.test.js).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { HD_PROPS, HD_PROP_PIXELS_PER_UNIT, PROP_RECTS, ASSET_FILES } from '../src/graphics/asset-atlas.js';
import { PROP_DEFINITIONS } from '../src/world/prop-definitions.js';
import { MAP_REGISTRY } from '../src/world/map-registry.js';

const ASSET_ROOT = 'assets/';

/** Width/height from a PNG's IHDR chunk (bytes 16-23) -- avoids a decode dependency in tests. */
function pngSize(path) {
  const buf = readFileSync(path);
  return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) };
}

function propTypesUsedByMaps() {
  const types = new Set();
  for (const mapDef of Object.values(MAP_REGISTRY)) {
    for (const p of mapDef.props || []) types.add(p.type);
  }
  return types;
}

test('every map prop type has an HD_PROPS entry or a pixel-art fallback (PROP_RECTS)', () => {
  for (const type of propTypesUsedByMaps()) {
    assert.ok(HD_PROPS[type] || PROP_RECTS[type], `"${type}" is used on a map but has neither an HD_PROPS entry nor a PROP_RECTS fallback`);
  }
});

test('every HD_PROPS image key resolves to a file in ASSET_FILES', () => {
  for (const [name, imageKey] of Object.entries(HD_PROPS)) {
    assert.ok(ASSET_FILES[imageKey], `${name}: HD_PROPS entry "${imageKey}" is not a key in ASSET_FILES`);
  }
});

test('every HD_PROPS normalized PNG is exactly (def.width*4) x (def.height*4)', () => {
  for (const [name, imageKey] of Object.entries(HD_PROPS)) {
    const path = ASSET_ROOT + ASSET_FILES[imageKey];
    assert.ok(existsSync(path), `${imageKey} (${path}) missing -- run npm run hd:props`);
    const def = PROP_DEFINITIONS[name];
    assert.ok(def, `HD_PROPS has an entry for "${name}" but PROP_DEFINITIONS does not`);
    const size = pngSize(path);
    assert.equal(size.width, def.width * HD_PROP_PIXELS_PER_UNIT, `${name}: ${imageKey} width ${size.width} != def.width*${HD_PROP_PIXELS_PER_UNIT} (${def.width * HD_PROP_PIXELS_PER_UNIT})`);
    assert.equal(size.height, def.height * HD_PROP_PIXELS_PER_UNIT, `${name}: ${imageKey} height ${size.height} != def.height*${HD_PROP_PIXELS_PER_UNIT} (${def.height * HD_PROP_PIXELS_PER_UNIT})`);
  }
});

test('every HD_PROPS key is a known prop type (PROP_DEFINITIONS)', () => {
  for (const name of Object.keys(HD_PROPS)) {
    assert.ok(PROP_DEFINITIONS[name], `HD_PROPS has an entry for unknown prop type "${name}"`);
  }
});

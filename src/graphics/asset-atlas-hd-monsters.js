// HD hand-painted monster walk-cycle sheets (Frost Hollow) -- split out of asset-atlas.js like
// asset-atlas-hd-npcs.js. Built by tools/build-hd-monsters-v2.py from assets/hd/monsters/raw/ into
// assets/hd/monsters/normalized-hd/. Same resolution model as the HD hero/NPCs: 192px frames = 24 world
// units * pixelsPerUnit 8. Keyed by monster type (src/data/monster-definitions.js): sprite-registry.js
// builds `monstersHd[type]` (hd-monster-sprite-builder.js) and monster-renderer.js prefers it over the
// 16px pixel sheet for that type, falling back per type when a file is missing (same boot-resilience
// as hero/tiles/props/NPCs -- every HD type still names a pixel sheet in MONSTER_SHEETS).
const HD_MONSTER_NORM_DIR = 'hd/monsters/normalized-hd/';

// type -> painted body height of the front frame in source px.
// The renderer hangs the mini HP bar / telegraph marker just above the painted body rather than at
// the top of the (mostly empty) 192px frame.
export const HD_MONSTERS = Object.freeze({
  frostSlime: { bodyHeight: 104 },
  iceGoblin: { bodyHeight: 152 },
  spectralWolf: { bodyHeight: 156 },
});

// Sheet layout contract: four walk frames per row, with an actual rear pose for UP.
export const HD_MONSTER = Object.freeze({ frame: 192, pixelsPerUnit: 8, cols: 4, rows: ['down', 'up', 'left', 'right'] });

/** Spread into ASSET_FILES: `hdMonster<Type>` -> normalized sheet. */
export const HD_MONSTER_ASSET_FILES = Object.freeze(Object.fromEntries(
  Object.keys(HD_MONSTERS).map((type) => [monsterSheetKey(type), `${HD_MONSTER_NORM_DIR}${type}.png`]),
));

export function monsterSheetKey(type) {
  return `hdMonster${type[0].toUpperCase()}${type.slice(1)}`;
}

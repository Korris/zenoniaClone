// HD CLERIC class sprites. Same contract as the mechanic (asset-atlas-hd-mechanic.js): sheets built by
// `python tools/build-hd-render.py cleric` land in assets/hd/cleric/normalized-hd/<anim>.png at 192px per
// frame, 4 facing rows (front / back / screen-left / screen-right), portrait in normalized/hero_face.png.
//
// The real art is in place (tools/build-cleric-sheets.py): armed sheets = the baked cleric set. Both side rows are painted
// (row 2 faces screen-left, row 3 screen-right), so CLERIC_MIRRORED stays empty. Set the flag to false to request no HD
// sheets (the class then draws the shared base hero).
export const CLERIC_ART_READY = true;

const HD_CLERIC_NORM_DIR = 'hd/cleric/normalized-hd/';

export const HD_CLERIC_ANIMATIONS = Object.freeze({
  idle: 'hdClericIdle', walk: 'hdClericWalk', attack1: 'hdClericAttack1', attack2: 'hdClericAttack2',
  attack3: 'hdClericAttack3', dash: 'hdClericDash', hurt: 'hdClericHurt', death: 'hdClericDeath',
});

const CLERIC_MIRRORED = new Set(); // anims whose left row is the mirrored right row (verify on import)
export const HD_CLERIC_SIDE_ROWS = Object.freeze(Object.fromEntries(
  Object.keys(HD_CLERIC_ANIMATIONS).map((a) => [a, CLERIC_MIRRORED.has(a) ? { right: 3, left: 'mirror' } : { right: 3, left: 2 }]),
));

export const HD_CLERIC_FACE_KEY = 'hdClericFace';

// Spread into asset-atlas.js's ASSET_FILES. Empty while the art is not ready, so the loader does not
// request files that do not exist yet.
export const HD_CLERIC_ASSET_FILES = Object.freeze(CLERIC_ART_READY ? {
  ...Object.fromEntries(Object.entries(HD_CLERIC_ANIMATIONS).map(([anim, key]) => [key, `${HD_CLERIC_NORM_DIR}${anim}.png`])),
  [HD_CLERIC_FACE_KEY]: 'hd/cleric/normalized/hero_face.png',
} : {});

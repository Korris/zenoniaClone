// HD ASSASSIN class sprites. Same contract as the cleric (asset-atlas-hd-cleric.js): sheets built by
// `python tools/build-assassin-sheets.py` land in assets/hd/assassin/normalized-hd/<anim>.png at 192px per
// frame, 4 facing rows (front / back / screen-left / screen-right), portrait in normalized/hero_face.png.
//
// The real art is in place: idle / attack1-3 are the armed (two daggers) sheets, walk / dash / hurt are the weaponless
// body (no armed sheets exist; the held-gear overlay always draws the daggers there), death is weaponless. Both side
// rows are painted (row 2 faces screen-left, row 3 screen-right), so ASSASSIN_MIRRORED stays empty. Set the flag to
// false to load the old-hero placeholder sheets instead.
export const ASSASSIN_ART_READY = true;

const REAL_DIR = 'hd/assassin/';
const PLACEHOLDER_DIR = 'hd/hero/'; // old hero: black ponytail, resembles the roster assassin
const DIR = ASSASSIN_ART_READY ? REAL_DIR : PLACEHOLDER_DIR;

export const HD_ASSASSIN_ANIMATIONS = Object.freeze({
  idle: 'hdAssassinIdle', walk: 'hdAssassinWalk', attack1: 'hdAssassinAttack1', attack2: 'hdAssassinAttack2',
  attack3: 'hdAssassinAttack3', dash: 'hdAssassinDash', hurt: 'hdAssassinHurt', death: 'hdAssassinDeath',
});

const ASSASSIN_MIRRORED = new Set(); // anims whose left row is the mirrored right row (verify on import)
export const HD_ASSASSIN_SIDE_ROWS = Object.freeze(Object.fromEntries(
  Object.keys(HD_ASSASSIN_ANIMATIONS).map((a) => [a, ASSASSIN_MIRRORED.has(a) ? { right: 3, left: 'mirror' } : { right: 3, left: 2 }]),
));

export const HD_ASSASSIN_FACE_KEY = 'hdAssassinFace';

// Spread into asset-atlas.js's ASSET_FILES. Always populated: real sheets when ready, otherwise the old hero
// sheets that exist today (so the placeholder costs no 404s).
export const HD_ASSASSIN_ASSET_FILES = Object.freeze({
  ...Object.fromEntries(Object.entries(HD_ASSASSIN_ANIMATIONS).map(([anim, key]) => [key, `${DIR}normalized-hd/${anim}.png`])),
  [HD_ASSASSIN_FACE_KEY]: `${DIR}normalized/hero_face.png`,
});

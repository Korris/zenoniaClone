// HD hand-painted WARRIOR sheets (the original hero set). pixelsPerUnit = source px per world unit (8 here vs. 1 for
// the pixel-art pack) so the renderer draws an HD frame at the SAME world-space size as the pixel frame, just crisper.
// Built by tools/build-hd-render.py into normalized-hd/ (192px frames). Missing files are dropped by asset-loader and
// sprite-registry falls back to the 24px pixel set per animation. Split out of asset-atlas.js.
const HD_HERO_DIR = 'hd/warrior-v2/'; // new red-haired warrior (the old black-haired set stays in assets/hd/hero/, unused)
const HD_HERO_NORM_DIR = HD_HERO_DIR + 'normalized-hd/';

export const HD_HERO_ANIMATIONS = Object.freeze({
  idle: 'hdHeroIdle', walk: 'hdHeroWalk', attack1: 'hdHeroAttack1', attack2: 'hdHeroAttack2',
  attack3: 'hdHeroAttack3', dash: 'hdHeroDash', hurt: 'hdHeroHurt', death: 'hdHeroDeath',
});
export const HD_HERO = Object.freeze({ pixelsPerUnit: 8, frame: 192, animations: HD_HERO_ANIMATIONS });
export const HD_WARRIOR_FACE_KEY = 'hdHeroFace';

// Which HD sheet row faces screen-left / screen-right. The v2 Warrior was painted with every sheet in the requested
// order -- row 2 faces screen-left, row 3 screen-right -- verified by the red headband tails, which always trail
// behind him (see assets/hd/hero/v1 for the old blond set, whose side rows were inconsistent).
const HERO_SIDE = Object.freeze({ right: 3, left: 2 });
export const HD_HERO_SIDE_ROWS = Object.freeze(Object.fromEntries(Object.keys(HD_HERO_ANIMATIONS).map((a) => [a, HERO_SIDE])));

/** All warrior sheets + portrait (class asset set). */
export const HD_WARRIOR_ASSET_FILES = Object.freeze({
  ...Object.fromEntries(Object.entries(HD_HERO_ANIMATIONS).map(([anim, key]) => [key, `${HD_HERO_NORM_DIR}${anim}.png`])),
  [HD_WARRIOR_FACE_KEY]: HD_HERO_DIR + 'normalized/hero_face.png',
});

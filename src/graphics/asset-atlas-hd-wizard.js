// HD hand-painted WIZARD class sprites (ChatGPT sheets in assets/hd/wizard/*.png, normalized by
// `python tools/build-hd-render.py wizard` into normalized-hd/ at 192px per frame,
// same pixelsPerUnit as the warrior's HD set -- see HD_HERO in asset-atlas.js). Split out of
// asset-atlas.js to keep that file small. Missing sheets fall back to the warrior's frames for that
// animation (sprite-registry.js), so the class stays playable while art is still being made.
const HD_WIZARD_NORM_DIR = 'hd/wizard/normalized-hd/';

export const HD_WIZARD_ANIMATIONS = Object.freeze({
  idle: 'hdWizardIdle', walk: 'hdWizardWalk', attack1: 'hdWizardAttack1', attack2: 'hdWizardAttack2',
  attack3: 'hdWizardAttack3', dash: 'hdWizardDash', hurt: 'hdWizardHurt', death: 'hdWizardDeath',
});

// The wizard sheets were requested as rows front / back / screen-LEFT / screen-RIGHT. Most came
// back that way (row 2 = left, row 3 = right); exceptions verified on import are listed below --
// attack1/attack2 thrust to screen-right in BOTH side rows, so their left is the right row mirrored.
const SIDE = Object.freeze({ right: 3, left: 2 });
const MIRROR = Object.freeze({ right: 3, left: 'mirror' });
const SIDE_EXCEPTIONS = { attack1: MIRROR, attack2: MIRROR };
export const HD_WIZARD_SIDE_ROWS = Object.freeze(Object.fromEntries(
  Object.keys(HD_WIZARD_ANIMATIONS).map((a) => [a, SIDE_EXCEPTIONS[a] || SIDE]),
));

export const HD_WIZARD_FACE_KEY = 'hdWizardFace';

// HD spell effects (tools/build-wizard-fx.py -> assets/hd/wizard/fx/, 4 px per world unit).
// effect name (registry.effects key, used as a projectile/effect spriteKey) -> image key.
// Projectiles are painted flying to screen-right; the renderer rotates them to the cast direction.
export const HD_WIZARD_FX = Object.freeze({
  magicBolt: 'hdFxMagicBolt', fireball: 'hdFxFireball', iceShard: 'hdFxIceShard',
  fireBurst: 'hdFxFireBurst', frostNova: 'hdFxFrostNova', arcaneBurst: 'hdFxArcaneBurst',
});
export const HD_FX_PIXELS_PER_UNIT = 4;

// Spread into asset-atlas.js's ASSET_FILES.
export const HD_WIZARD_ASSET_FILES = Object.freeze({
  ...Object.fromEntries(Object.entries(HD_WIZARD_ANIMATIONS).map(([anim, key]) => [key, `${HD_WIZARD_NORM_DIR}${anim}.png`])),
  [HD_WIZARD_FACE_KEY]: 'hd/wizard/normalized/hero_face.png',
  ...Object.fromEntries(Object.entries(HD_WIZARD_FX).map(([name, key]) => [key, `hd/wizard/fx/${name}.png`])),
});

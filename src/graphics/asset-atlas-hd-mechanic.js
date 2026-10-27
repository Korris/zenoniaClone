// HD hand-painted MECHANIC class sprites (ChatGPT sheets in assets/hd/mechanic/*.png, normalized by
// `python tools/build-hd-hero.py assets/hd/mechanic assets/hd/mechanic/normalized` into 96px frames,
// same contract as the wizard -- see asset-atlas-hd-wizard.js). Missing sheets fall back to the
// warrior's frames for that animation (sprite-registry.js), so the class stays playable meanwhile.
const HD_MECHANIC_NORM_DIR = 'hd/mechanic/normalized/';

export const HD_MECHANIC_ANIMATIONS = Object.freeze({
  idle: 'hdMechanicIdle', walk: 'hdMechanicWalk', attack1: 'hdMechanicAttack1', attack2: 'hdMechanicAttack2',
  attack3: 'hdMechanicAttack3', dash: 'hdMechanicDash', hurt: 'hdMechanicHurt', death: 'hdMechanicDeath',
});

// Sheets were requested as rows front / back / screen-LEFT / screen-RIGHT. Sheets whose two side
// rows both came back facing screen-right are listed in MIRRORED (left = right row mirrored);
// filled in after checking each imported sheet's contact sheet.
const SIDE = Object.freeze({ right: 3, left: 2 });
const MIRROR = Object.freeze({ right: 3, left: 'mirror' });
const MIRRORED = new Set(['attack3', 'dash', 'hurt', 'death']); // verified on import (2026-10-27)
export const HD_MECHANIC_SIDE_ROWS = Object.freeze(Object.fromEntries(
  Object.keys(HD_MECHANIC_ANIMATIONS).map((a) => [a, MIRRORED.has(a) ? MIRROR : SIDE]),
));

export const HD_MECHANIC_FACE_KEY = 'hdMechanicFace';

// HD gun effects (tools/build-mechanic-fx.py -> assets/hd/mechanic/fx/, 4 px per world unit).
// effect name (registry.effects key, used as a projectile/effect spriteKey) -> image key.
// Projectiles are painted flying to screen-right; the renderer rotates them to the shot direction.
export const HD_MECHANIC_FX = Object.freeze({
  bullet: 'hdFxBullet', plasmaShot: 'hdFxPlasmaShot', rocket: 'hdFxRocket',
  muzzleFlash: 'hdFxMuzzleFlash', explosion: 'hdFxExplosion', shockPulse: 'hdFxShockPulse',
});

// Spread into asset-atlas.js's ASSET_FILES.
export const HD_MECHANIC_ASSET_FILES = Object.freeze({
  ...Object.fromEntries(Object.entries(HD_MECHANIC_ANIMATIONS).map(([anim, key]) => [key, `${HD_MECHANIC_NORM_DIR}${anim === 'walk' ? 'walk-v2' : anim}.png`])),
  [HD_MECHANIC_FACE_KEY]: `${HD_MECHANIC_NORM_DIR}hero_face.png`,
  ...Object.fromEntries(Object.entries(HD_MECHANIC_FX).map(([name, key]) => [key, `hd/mechanic/fx/${name}.png`])),
});

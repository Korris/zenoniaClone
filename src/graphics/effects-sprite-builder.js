// Builds registry.effects: the shared set at boot (strips, pickups, markers, painted slashes) and the class-owned
// spell / ultimate FX when that class's assets finish loading (wizard + mechanic projectiles, Ice Storm, Earth Break, Rocket Barrage).
import { createSprite } from './pixel-sprite-factory.js';
import { cropSprite } from './sprite-cell.js';
import { FX_STRIPS } from './asset-atlas.js';
import { HD_WIZARD_FX, HD_FX_PIXELS_PER_UNIT } from './asset-atlas-hd-wizard.js';
import { HD_MECHANIC_FX } from './asset-atlas-hd-mechanic.js';
import { EFFECT_PALETTE, MARKER_ROWS } from './sprites/effect-sprite-data.js';
import { buildStrip } from './character-sprite-builders.js';
import { buildIceStormFrames } from './ice-storm-renderer.js';
import { buildUltimateFrames } from './ultimate-renderer.js';
import { buildPaintedCombatEffects } from './painted-combat-assets.js';

export function buildSharedEffects(images) {
  const effects = {};
  for (const [name, strip] of Object.entries(FX_STRIPS)) effects[name] = buildStrip(images[strip.image], strip.w, strip.h);
  effects.coin = images.coin;
  effects.potionHp = images.lifePot;
  effects.potionMp = images.milkPot;
  effects.markers = {};
  for (const [id, rows] of Object.entries(MARKER_ROWS)) effects.markers[id] = createSprite(rows, { palette: EFFECT_PALETTE });
  Object.assign(effects, buildPaintedCombatEffects(images));
  return effects;
}

/** FX owned by whichever class sheets are in `images` (absent art => key omitted; renderers tolerate that). */
export function buildClassEffects(images) {
  const effects = {};
  // HD spell FX: one canvas each, tagged with pixelsPerUnit so drawRotated() sizes it in world units.
  for (const [name, key] of Object.entries({ ...HD_WIZARD_FX, ...HD_MECHANIC_FX })) {
    const img = images[key];
    if (img) effects[name] = cropSprite(img, 0, 0, img.width, img.height, false, HD_FX_PIXELS_PER_UNIT);
  }
  const ultimates = {
    iceStorm: buildIceStormFrames(images.iceStorm),
    earthBreak: buildUltimateFrames(images, 'earthBreak'),
    rocketBarrage: buildUltimateFrames(images.rocketBarrage, 'rocketBarrage'),
  };
  for (const [name, frames] of Object.entries(ultimates)) if (frames) effects[name] = frames;
  return effects;
}

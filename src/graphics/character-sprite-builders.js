// Slicers that turn loaded character / monster / strip images into canvases (hero sheets, NPCs, monsters, FX strips).
// Split out of sprite-registry.js; pure functions of the loaded `images` map, so missing sheets just yield fewer frames.
import { createTintedSprite, HIT_FLASH_COLOR, HIT_RIM_COLOR } from './pixel-sprite-factory.js';
import { cropSprite, cell } from './sprite-cell.js';
import {
  CHARACTER_DIRECTIONS, HERO_ANIMATIONS, HERO_FRAME, HERO_SHEET_ROW, HD_HERO, HD_HERO_ANIMATIONS, HD_HERO_SIDE_ROWS,
} from './asset-atlas.js';
import { HD_NPC, HD_NPC_IDS, sheetKey, faceKey } from './asset-atlas-hd-npcs.js';

export const CHARACTER_SIZE = 16; // pack NPCs/monsters
export const HERO_FRAME_SIZE = HERO_FRAME; // custom hero sheets

/** 64x112 character sheet -> { down/up/left/right: { idle, walk[4], attack } } */
export function buildCharacter(img) {
  const set = {};
  CHARACTER_DIRECTIONS.forEach((dir, col) => {
    const walk = [0, 1, 2, 3].map((row) => cell(img, col, row));
    set[dir] = { idle: walk[0], walk, attack: cell(img, col, 4) };
  });
  return set;
}

/** Custom hero sheets -> { dir: { idle[], walk[], attack1[], ..., death[] } } (rows = directions,
 * remapped through HERO_SHEET_ROW -- see its comment in asset-atlas.js for why left/right aren't
 * read straight off CHARACTER_DIRECTIONS' index like every other sheet in this file). */
export function buildHeroPixel(images) {
  const set = {};
  CHARACTER_DIRECTIONS.forEach((dir) => {
    const row = HERO_SHEET_ROW[dir];
    set[dir] = {};
    for (const [anim, imageKey] of Object.entries(HERO_ANIMATIONS)) {
      const img = images[imageKey];
      const frames = [];
      for (let col = 0; col < Math.floor(img.width / HERO_FRAME); col++) {
        frames.push(cropSprite(img, col * HERO_FRAME, row * HERO_FRAME, HERO_FRAME, HERO_FRAME));
      }
      set[dir][anim] = frames;
    }
  });
  return set;
}

/** HD class sheets (hand-painted, 192px frames, pixelsPerUnit=8) -> { dir: { anim[] } }, but only
 * for animations whose normalized sheet actually loaded (asset-loader.js drops missing files).
 * `animations` maps anim -> image key, `sideRows` maps anim -> which rows face left/right. */
export function buildHeroHd(images, animations = HD_HERO_ANIMATIONS, sideRows = HD_HERO_SIDE_ROWS) {
  const set = {};
  CHARACTER_DIRECTIONS.forEach((dir) => { set[dir] = {}; });
  const { frame, pixelsPerUnit } = HD_HERO;
  for (const [anim, imageKey] of Object.entries(animations)) {
    const img = images[imageKey];
    if (!img) continue; // not generated yet -- the caller falls back to another set for this anim
    const cols = Math.floor(img.width / frame);
    const side = sideRows[anim];
    CHARACTER_DIRECTIONS.forEach((dir) => {
      // down/up are rows 0/1 everywhere; side rows come from the per-sheet table ('mirror' = flip
      // the right-facing row for sheets that never painted a left-facing one).
      const mirror = dir === 'left' && side.left === 'mirror';
      const row = { down: 0, up: 1 }[dir] ?? (mirror ? side.right : side[dir]);
      const frames = [];
      for (let col = 0; col < cols; col++) frames.push(cropSprite(img, col * frame, row * frame, frame, frame, mirror, pixelsPerUnit));
      set[dir][anim] = frames;
    });
  }
  return set;
}

/** Merges the HD set over the pixel-art set per animation: HD wins where its sheet loaded,
 * otherwise the 24px pixel frames are used (e.g. only idle/walk are HD so far, attacks are still pixel). */
export function buildHero(images) {
  const pixel = buildHeroPixel(images);
  const hd = buildHeroHd(images);
  const merged = {};
  CHARACTER_DIRECTIONS.forEach((dir) => { merged[dir] = { ...pixel[dir], ...hd[dir] }; });
  return merged;
}

/** HD NPC idle sheets (192px frames, pixelsPerUnit 8; rows down/left/right, no rear pose) ->
 * npcsHd[id] = { down|left|right: { idle[] } }, only for ids whose normalized sheet loaded. The
 * matching 152px portrait lands in `faces[id]` (stamped with pixelsPerUnit so the dialog box
 * knows to smooth-fit it instead of 3x pixel-scaling a 38px faceset). */
export function buildNpcsHd(images, faces) {
  const npcsHd = {};
  const { frame, pixelsPerUnit, cols, rows } = HD_NPC;
  for (const id of HD_NPC_IDS) {
    const img = images[sheetKey(id)];
    if (!img) continue; // not generated yet -- drawNpc() falls back to the pixel-art set for this id
    const set = {};
    rows.forEach((dir, row) => {
      const idle = [];
      for (let col = 0; col < cols; col++) idle.push(cropSprite(img, col * frame, row * frame, frame, frame, false, pixelsPerUnit));
      set[dir] = { idle };
    });
    npcsHd[id] = set;
    const face = images[faceKey(id)];
    if (face) faces[id] = cropSprite(face, 0, 0, face.width, face.height, false, pixelsPerUnit);
  }
  return npcsHd;
}

/** 64x64 monster sheet -> { frames: {dir: [4]}, hitFlash: {dir: canvas} } */
export function buildMonster(img) {
  const frames = {};
  const hitFlash = {};
  const hitRim = {};
  CHARACTER_DIRECTIONS.forEach((dir, col) => {
    frames[dir] = [0, 1, 2, 3].map((row) => cell(img, col, row));
    hitFlash[dir] = createTintedSprite(frames[dir][0], HIT_FLASH_COLOR);
    hitRim[dir] = createTintedSprite(frames[dir][0], HIT_RIM_COLOR);
  });
  return { frames, hitFlash, hitRim, width: CHARACTER_SIZE, height: CHARACTER_SIZE };
}

/** Horizontal strip -> frames; the count is derived from the image width so data never lies. */
export function buildStrip(img, w, h) {
  const frames = [];
  for (let i = 0; i < Math.floor(img.width / w); i++) frames.push(cropSprite(img, i * w, 0, w, h));
  return frames;
}

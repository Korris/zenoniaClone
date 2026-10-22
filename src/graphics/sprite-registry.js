// Slices the loaded Ninja Adventure images into canvases, once at boot.
// Shape: { hero, npcs, monsters, boss, tiles, flowers, props, effects, icons, faces, ui }
import { createSprite, createTintedSprite } from './pixel-sprite-factory.js';
import { cropSprite, cell } from './sprite-cell.js';
import {
  CHARACTER_DIRECTIONS, FLOWER_OVERLAYS, MONSTER_SHEETS, BOSS_STRIPS, NPC_SHEETS, FX_STRIPS,
  HERO_ANIMATIONS, HERO_FRAME, HERO_SHEET_ROW, HD_HERO, HD_HERO_ANIMATIONS,
} from './asset-atlas.js';
import { EFFECT_PALETTE, MARKER_ROWS } from './sprites/effect-sprite-data.js';
import { buildTiles } from './tile-sprite-builder.js';
import { buildProps } from './prop-sprite-builder.js';
import { HD_NPC, HD_NPC_IDS, sheetKey, faceKey } from './asset-atlas-hd-npcs.js';
import { SKILL_DEFINITIONS } from '../data/skill-definitions.js';
import { ITEM_TEMPLATES } from '../data/item-definitions.js';

export const CHARACTER_SIZE = 16; // pack NPCs/monsters
export const HERO_FRAME_SIZE = HERO_FRAME; // custom hero sheets
const HIT_FLASH_COLOR = 'rgba(255,255,255,0.85)';

/** 64x112 character sheet -> { down/up/left/right: { idle, walk[4], attack } } */
function buildCharacter(img) {
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
function buildHeroPixel(images) {
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

/** HD hero sheets (hand-painted, 96px frames, pixelsPerUnit=4) -> { dir: { anim[] } }, but only
 * for animations whose normalized sheet actually loaded (asset-loader.js drops missing files). */
function buildHeroHd(images) {
  const set = {};
  CHARACTER_DIRECTIONS.forEach((dir) => { set[dir] = {}; });
  const { frame, pixelsPerUnit } = HD_HERO;
  for (const [anim, imageKey] of Object.entries(HD_HERO_ANIMATIONS)) {
    const img = images[imageKey];
    if (!img) continue; // not generated yet -- buildHero() falls back to the pixel set for this anim
    const cols = Math.floor(img.width / frame);
    CHARACTER_DIRECTIONS.forEach((dir) => {
      const row = HERO_SHEET_ROW[dir];
      const frames = [];
      for (let col = 0; col < cols; col++) frames.push(cropSprite(img, col * frame, row * frame, frame, frame, false, pixelsPerUnit));
      set[dir][anim] = frames;
    });
  }
  return set;
}

/** Merges the HD set over the pixel-art set per animation: HD wins where its sheet loaded,
 * otherwise the 24px pixel frames are used (e.g. only idle/walk are HD so far, attacks are still pixel). */
function buildHero(images) {
  const pixel = buildHeroPixel(images);
  const hd = buildHeroHd(images);
  const merged = {};
  CHARACTER_DIRECTIONS.forEach((dir) => { merged[dir] = { ...pixel[dir], ...hd[dir] }; });
  return merged;
}

/** HD NPC idle sheets (96px frames, pixelsPerUnit 4; rows down/left/right, no rear pose) ->
 * npcsHd[id] = { down|left|right: { idle[] } }, only for ids whose normalized sheet loaded. The
 * matching 152px portrait lands in `faces[id]` (stamped with pixelsPerUnit so the dialog box
 * knows to smooth-fit it instead of 3x pixel-scaling a 38px faceset). */
function buildNpcsHd(images, faces) {
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
function buildMonster(img) {
  const frames = {};
  const hitFlash = {};
  CHARACTER_DIRECTIONS.forEach((dir, col) => {
    frames[dir] = [0, 1, 2, 3].map((row) => cell(img, col, row));
    hitFlash[dir] = createTintedSprite(frames[dir][0], HIT_FLASH_COLOR);
  });
  return { frames, hitFlash, width: CHARACTER_SIZE, height: CHARACTER_SIZE };
}

/** Horizontal strip -> frames; the count is derived from the image width so data never lies. */
function buildStrip(img, w, h) {
  const frames = [];
  for (let i = 0; i < Math.floor(img.width / w); i++) frames.push(cropSprite(img, i * w, 0, w, h));
  return frames;
}

function buildEffects(images) {
  const effects = {};
  for (const [name, strip] of Object.entries(FX_STRIPS)) effects[name] = buildStrip(images[strip.image], strip.w, strip.h);
  effects.coin = images.coin;
  effects.potionHp = images.lifePot;
  effects.potionMp = images.milkPot;
  effects.markers = {};
  for (const [id, rows] of Object.entries(MARKER_ROWS)) effects.markers[id] = createSprite(rows, { palette: EFFECT_PALETTE });
  return effects;
}

export function buildSpriteRegistry(images) {
  const npcs = {};
  // HUD portrait: prefer the HD hero face if that sheet exists; heroIsHd flags the HUD renderer
  // to enable image smoothing only for this draw (everything else stays crisp pixel-art).
  const faces = { hero: images.hdHeroFace || images.heroFace, heroIsHd: Boolean(images.hdHeroFace), boss: images.bossFace };
  for (const [key, imageKey] of Object.entries(NPC_SHEETS)) {
    npcs[key] = buildCharacter(images[imageKey]);
    faces[key] = images[`${imageKey}Face`];
  }
  const monsters = {};
  for (const [type, imageKey] of Object.entries(MONSTER_SHEETS)) monsters[type] = buildMonster(images[imageKey]);
  const { frameW, frameH } = BOSS_STRIPS;
  const bossIdle = buildStrip(images[BOSS_STRIPS.idle], frameW, frameH);
  // Skill icons are already-sized 24x24 PNGs from the Ninja Adventure icon pack -- used directly.
  const icons = {};
  for (const [id, def] of Object.entries(SKILL_DEFINITIONS)) icons[id] = images[def.icon];
  // Equipment sprites (weapons used as-is, gear icons 24x24) keyed by templateId.
  const items = {};
  for (const [id, def] of Object.entries(ITEM_TEMPLATES)) items[id] = images[def.sprite];

  const npcsHd = buildNpcsHd(images, faces); // also fills faces[id] with HD portraits

  return {
    hero: buildHero(images),
    heroFrameUnits: HERO_FRAME_SIZE, // world units a hero frame covers, regardless of source resolution
    npcs,
    npcsHd, // keyed by NPC id (HD, hero-scale); `npcs` stays keyed by pixel sprite name (fallback)
    monsters,
    boss: {
      idle: bossIdle, walk: buildStrip(images[BOSS_STRIPS.walk], frameW, frameH), hit: buildStrip(images[BOSS_STRIPS.hit], frameW, frameH),
      width: frameW, height: frameH, hitFlash: createTintedSprite(bossIdle[0], HIT_FLASH_COLOR),
    },
    ...buildTiles(images), // { tiles, tilesHd }
    flowers: FLOWER_OVERLAYS.map(([image, cx, cy]) => cell(images[image], cx, cy)), // pixel-art flower overlay (non-HD fallback)
    props: buildProps(images),
    effects: buildEffects(images),
    icons,
    items,
    faces,
    ui: { dialogBox: images.dialogBox, facesetBox: images.facesetBox },
  };
}

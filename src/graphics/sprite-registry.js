// Slices the loaded Ninja Adventure images into canvases, once at boot.
// Shape: { hero, npcs, monsters, boss, tiles, flowers, props, effects, icons, faces, ui }
import { createSprite, createTintedSprite, HIT_FLASH_COLOR } from './pixel-sprite-factory.js';
import { buildMonstersHd } from './hd-monster-sprite-builder.js';
import { cropSprite, cell } from './sprite-cell.js';
import {
  CHARACTER_DIRECTIONS, FLOWER_OVERLAYS, MONSTER_SHEETS, BOSS_STRIPS, NPC_SHEETS, FX_STRIPS,
  HERO_ANIMATIONS, HERO_FRAME, HERO_SHEET_ROW, HD_HERO, HD_HERO_ANIMATIONS, HD_HERO_SIDE_ROWS, BACKDROP_FILES,
} from './asset-atlas.js';
import {
  HD_WIZARD_ANIMATIONS, HD_WIZARD_SIDE_ROWS, HD_WIZARD_FACE_KEY, HD_WIZARD_FX, HD_FX_PIXELS_PER_UNIT,
} from './asset-atlas-hd-wizard.js';
import { EFFECT_PALETTE, MARKER_ROWS } from './sprites/effect-sprite-data.js';
import { buildTiles } from './tile-sprite-builder.js';
import { buildProps } from './prop-sprite-builder.js';
import { HD_NPC, HD_NPC_IDS, sheetKey, faceKey } from './asset-atlas-hd-npcs.js';
import { HD_MECHANIC_ANIMATIONS, HD_MECHANIC_SIDE_ROWS, HD_MECHANIC_FACE_KEY, HD_MECHANIC_FX } from './asset-atlas-hd-mechanic.js';
import { SKILL_DEFINITIONS } from '../data/skill-definitions.js';
import { ITEM_TEMPLATES } from '../data/item-definitions.js';
import { buildWeaponIcons } from './weapon-icons.js';
import { buildGroundSprite } from './ground-sprite-builder.js';

export const CHARACTER_SIZE = 16; // pack NPCs/monsters
export const HERO_FRAME_SIZE = HERO_FRAME; // custom hero sheets

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

/** HD class sheets (hand-painted, 96px frames, pixelsPerUnit=4) -> { dir: { anim[] } }, but only
 * for animations whose normalized sheet actually loaded (asset-loader.js drops missing files).
 * `animations` maps anim -> image key, `sideRows` maps anim -> which rows face left/right. */
function buildHeroHd(images, animations = HD_HERO_ANIMATIONS, sideRows = HD_HERO_SIDE_ROWS) {
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
  // HD spell FX: one canvas each, tagged with pixelsPerUnit so drawRotated() sizes it in world units.
  for (const [name, key] of Object.entries({ ...HD_WIZARD_FX, ...HD_MECHANIC_FX })) {
    const img = images[key];
    if (img) effects[name] = cropSprite(img, 0, 0, img.width, img.height, false, HD_FX_PIXELS_PER_UNIT);
  }
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
  // Per-class portrait (HUD + inventory); a class without its own face yet borrows the warrior's.
  faces.heroByClass = {
    warrior: faces.hero, wizard: images[HD_WIZARD_FACE_KEY] || faces.hero, mechanic: images[HD_MECHANIC_FACE_KEY] || faces.hero,
  };
  for (const [key, imageKey] of Object.entries(NPC_SHEETS)) {
    npcs[key] = buildCharacter(images[imageKey]);
    faces[key] = images[`${imageKey}Face`];
  }
  const monsters = {};
  for (const [type, imageKey] of Object.entries(MONSTER_SHEETS)) monsters[type] = buildMonster(images[imageKey]);
  const monstersHd = buildMonstersHd(images); // HD painted sheets (Frost Hollow), per type where loaded
  const { frameW, frameH } = BOSS_STRIPS;
  const bossIdle = buildStrip(images[BOSS_STRIPS.idle], frameW, frameH);
  // Skill icons are already-sized 24x24 PNGs from the Ninja Adventure icon pack -- used directly.
  const icons = {};
  for (const [id, def] of Object.entries(SKILL_DEFINITIONS)) icons[id] = images[def.icon];
  // Equipment sprites (weapons used as-is, gear icons 24x24) keyed by templateId.
  const items = {};
  for (const [id, def] of Object.entries(ITEM_TEMPLATES)) items[id] = images[def.sprite];
  // Painted 4x4 equipment atlas shared by the bag and ground drops.
  const inventoryItems = {};
  const weaponIcons = images.weaponIcons ? buildWeaponIcons(images.weaponIcons) : [];
  const painted = images.inventoryIcons;
  if (painted) {
    const weapons = ['sword', 'sword2', 'rapier', 'club', 'katana', 'axe', 'lance', 'hammer', 'bigSword'];
    const gear = { helmet: 9, armor: 10, gloves: 11, boots: 12 };
    const cw = painted.width / 4, ch = painted.height / 4;
    for (const [id, def] of Object.entries(ITEM_TEMPLATES)) {
      const index = def.slot === 'weapon' ? weapons.indexOf(id) : gear[def.slot];
      if (index === undefined || index < 0) continue;
      inventoryItems[id] = cropSprite(painted, (index % 4) * cw, Math.floor(index / 4) * ch, cw, ch);
    }
  }

  const npcsHd = buildNpcsHd(images, faces); // also fills faces[id] with HD portraits

  const warrior = buildHero(images);
  // Wizard: its HD sheets over the warrior set, so an animation not painted yet still shows something.
  const wizardHd = buildHeroHd(images, HD_WIZARD_ANIMATIONS, HD_WIZARD_SIDE_ROWS);
  const wizard = Object.fromEntries(CHARACTER_DIRECTIONS.map((dir) => [dir, { ...warrior[dir], ...wizardHd[dir] }]));
  const mechanicHd = buildHeroHd(images, HD_MECHANIC_ANIMATIONS, HD_MECHANIC_SIDE_ROWS); // same fallback as the wizard
  const mechanic = Object.fromEntries(CHARACTER_DIRECTIONS.map((dir) => [dir, { ...warrior[dir], ...mechanicHd[dir] }]));

  return {
    hero: warrior, // title screen / fallback
    heroes: { warrior, wizard, mechanic }, // keyed by class id (class-definitions.js)
    heroFrameUnits: HERO_FRAME_SIZE, // world units a hero frame covers, regardless of source resolution
    npcs,
    npcsHd, // keyed by NPC id (HD, hero-scale); `npcs` stays keyed by pixel sprite name (fallback)
    monsters,
    monstersHd,
    boss: {
      idle: bossIdle, walk: buildStrip(images[BOSS_STRIPS.walk], frameW, frameH), hit: buildStrip(images[BOSS_STRIPS.hit], frameW, frameH),
      width: frameW, height: frameH, hitFlash: createTintedSprite(bossIdle[0], HIT_FLASH_COLOR),
    },
    ...buildTiles(images), // { tiles, tilesHd }
    backdrops: Object.fromEntries(Object.keys(BACKDROP_FILES).filter((k) => images[k]).map((k) => [k, images[k]])),
    flowers: FLOWER_OVERLAYS.map(([image, cx, cy]) => cell(images[image], cx, cy)), // pixel-art flower overlay (non-HD fallback)
    props: buildProps(images),
    effects: buildEffects(images),
    icons,
    items,
    groundItems: Object.fromEntries(Object.entries(inventoryItems).map(([id, sprite]) => [id, buildGroundSprite(sprite)])),
    groundWeaponIcons: weaponIcons.map(sprite => buildGroundSprite(sprite)),
    inventoryItems,
    weaponIcons,
    faces,
    ui: { dialogBox: images.dialogBox, facesetBox: images.facesetBox },
  };
}

// Slices the SHARED loaded images into canvases, once at boot (class-owned sprites are added later per class by
// class-sprite-builder.js `registerClassSprites`, after the class pick).
// Shape: { hero, heroes{}, heldGear{}, companions{}, gearIcons{}, npcs, monsters, boss, tiles, props, effects, icons, faces, ui, ... }
import { createTintedSprite, HIT_FLASH_COLOR } from './pixel-sprite-factory.js';
import { buildMonstersHd } from './hd-monster-sprite-builder.js';
import { buildPaintedSkillIcons } from './painted-combat-assets.js';
import { cropSprite, cell } from './sprite-cell.js';
import { NPC_SHEETS, MONSTER_SHEETS, BOSS_STRIPS, FLOWER_OVERLAYS, BACKDROP_FILES } from './asset-atlas.js';
import { HD_CLASS_SHEETS } from './asset-atlas-class-files.js';
import { buildTiles } from './tile-sprite-builder.js';
import { buildProps } from './prop-sprite-builder.js';
import { buildCharacter, buildHero, buildNpcsHd, buildMonster, buildStrip, CHARACTER_SIZE, HERO_FRAME_SIZE } from './character-sprite-builders.js';
import { buildSharedEffects } from './effects-sprite-builder.js';
import { SKILL_DEFINITIONS } from '../data/skill-definitions.js';
import { ITEM_TEMPLATES } from '../data/item-definitions.js';
import { buildWeaponIcons } from './weapon-icons.js';
import { buildGroundSprite } from './ground-sprite-builder.js';
import { buildArmorIcons } from './equipment-icons.js';

export { CHARACTER_SIZE, HERO_FRAME_SIZE };

/** Painted 4x4 equipment atlas (bag + ground drops): templateId -> cropped cell. */
function buildInventoryItems(painted) {
  const inventoryItems = {};
  if (!painted) return inventoryItems;
  const weapons = ['sword', 'sword2', 'rapier', 'club', 'katana', 'axe', 'lance', 'hammer', 'bigSword'];
  const gear = { helmet: 9, armor: 10, gloves: 11, boots: 12 };
  const cw = painted.width / 4, ch = painted.height / 4;
  for (const [id, def] of Object.entries(ITEM_TEMPLATES)) {
    const index = def.slot === 'weapon' ? weapons.indexOf(id) : gear[def.slot];
    if (index === undefined || index < 0) continue;
    inventoryItems[id] = cropSprite(painted, (index % 4) * cw, Math.floor(index / 4) * ch, cw, ch);
  }
  return inventoryItems;
}

/** Portraits: the HUD face (HD when its sheet loaded), the boss, NPC faces and one preview portrait per class. */
function buildFaces(images) {
  // heroIsHd flags the HUD renderer to enable image smoothing only for this draw (everything else stays crisp pixel-art).
  const faces = { hero: images.hdHeroFace || images.heroFace, heroIsHd: Boolean(images.hdHeroFace), boss: images.bossFace };
  faces.heroByClass = Object.fromEntries(Object.entries(HD_CLASS_SHEETS).map(([id, { faceKey }]) => [id, images[faceKey] || faces.hero]));
  return faces;
}

export function buildSpriteRegistry(images) {
  const faces = buildFaces(images);
  const npcs = {};
  for (const [key, imageKey] of Object.entries(NPC_SHEETS)) {
    npcs[key] = buildCharacter(images[imageKey]);
    faces[key] = images[`${imageKey}Face`];
  }
  const monsters = {};
  for (const [type, imageKey] of Object.entries(MONSTER_SHEETS)) monsters[type] = buildMonster(images[imageKey]);
  const { frameW, frameH } = BOSS_STRIPS;
  const bossIdle = buildStrip(images[BOSS_STRIPS.idle], frameW, frameH);
  // Skill icons are already-sized 24x24 PNGs from the Ninja Adventure icon pack -- used directly; class rosters join per class.
  const icons = {};
  for (const [id, def] of Object.entries(SKILL_DEFINITIONS)) icons[id] = images[def.icon];
  Object.assign(icons, buildPaintedSkillIcons(images));
  const items = {};
  for (const [id, def] of Object.entries(ITEM_TEMPLATES)) items[id] = images[def.sprite];
  const inventoryItems = buildInventoryItems(images.inventoryIcons);
  const weaponIcons = images.weaponIcons ? buildWeaponIcons(images.weaponIcons) : [];
  const armorIcons = Object.fromEntries(['helmet', 'armor', 'gloves', 'boots'].map((slot) => [slot, buildArmorIcons(images[`${slot}Icons`])]));
  const npcsHd = buildNpcsHd(images, faces); // also fills faces[id] with HD portraits
  const hero = buildHero(images); // pixel set + whichever HD sheets are shared (the title shows the warrior idle); classes lay theirs over it

  return {
    hero, // title screen / base layer every class set is laid over
    heroes: {}, // keyed by class id (class-definitions.js), filled by registerClassSprites
    heldGear: {}, // paper-doll held gear per class (null => baked frames)
    companions: {}, // offhand companion art per class: cleric spirit, mechanic robot (absent => not drawn)
    gearIcons: {}, groundGearIcons: {}, // cut class gear icons: [classId][slot][tier] (gear-icons.js)
    loadedClasses: new Set(),
    heroFrameUnits: HERO_FRAME_SIZE, // world units a hero frame covers, regardless of source resolution
    npcs,
    npcsHd, // keyed by NPC id (HD, hero-scale); `npcs` stays keyed by pixel sprite name (fallback)
    monsters,
    monstersHd: buildMonstersHd(images), // HD painted sheets (Frost Hollow), per type where loaded
    boss: {
      idle: bossIdle, walk: buildStrip(images[BOSS_STRIPS.walk], frameW, frameH), hit: buildStrip(images[BOSS_STRIPS.hit], frameW, frameH),
      width: frameW, height: frameH, hitFlash: createTintedSprite(bossIdle[0], HIT_FLASH_COLOR),
    },
    ...buildTiles(images), // { tiles, tilesHd }
    backdrops: Object.fromEntries(Object.keys(BACKDROP_FILES).filter((k) => images[k]).map((k) => [k, images[k]])),
    flowers: FLOWER_OVERLAYS.map(([image, cx, cy]) => cell(images[image], cx, cy)), // pixel-art flower overlay (non-HD fallback)
    props: buildProps(images),
    effects: buildSharedEffects(images),
    icons,
    items,
    groundItems: Object.fromEntries(Object.entries(inventoryItems).map(([id, sprite]) => [id, buildGroundSprite(sprite)])),
    groundWeaponIcons: weaponIcons.map((sprite) => buildGroundSprite(sprite)),
    armorIcons,
    groundArmorIcons: Object.fromEntries(Object.entries(armorIcons).map(([slot, list]) => [slot, list.map((sprite) => buildGroundSprite(sprite))])),
    inventoryItems,
    weaponIcons,
    faces,
    ui: { dialogBox: images.dialogBox, facesetBox: images.facesetBox, bagFrame: images.bagFrame },
  };
}

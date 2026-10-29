// Where every sprite lives inside the Ninja Adventure (CC0) image files. Pure data.
// Cell coordinates are in 16px tiles: [cx, cy, w, h]. Character sheets are 64x112:
// columns = facing (down, up, left, right), rows 0-3 = walk frames, row 4 = attack.
// Monster sheets are 64x64: columns = facing, rows = 4 animation frames.
import { HD_PROP_ASSET_FILES } from './asset-atlas-hd-props.js';
import { HD_NPC_ASSET_FILES } from './asset-atlas-hd-npcs.js';
import { HD_MONSTER_ASSET_FILES } from './asset-atlas-hd-monsters.js';
import { WIZARD_ICON_FILES } from '../data/skill-definitions-wizard.js';
import { MECHANIC_ICON_FILES } from '../data/skill-definitions-mechanic.js';
import { ALL_CLASS_ASSET_FILES, CLASS_PREVIEW_FILES, TITLE_HERO_FILES } from './asset-atlas-class-files.js';

export { HD_PROPS, HD_PROP_PIXELS_PER_UNIT } from './asset-atlas-hd-props.js';
export { GROUND_TILES, FLOWER_OVERLAYS, HD_TILE_CELL, HD_TILE_PIXELS_PER_UNIT, HD_TILES, PROP_RECTS } from './asset-atlas-tiles.js';
export { HD_HERO, HD_HERO_ANIMATIONS, HD_HERO_SIDE_ROWS } from './asset-atlas-hd-warrior.js';
export { classAssetFiles } from './asset-atlas-class-files.js';

const PACK = 'ninja-adventure/';
const HERO = 'custom/hero/';
const HD_TILE_NORM_DIR = 'hd/tiles/normalized/';

// Whole-map painted ground layers (4 px per world unit, map-sized), keyed by the image key a map
// definition names in `backdrop`. The tile rows still drive collision and the minimap; the renderer
// draws the backdrop instead of the tiles (tilemap-renderer.js). Built by
// tools/build-oakhaven-village-art.py (village) and tools/build-frost-hollow-art.py (battle map) from
// ChatGPT paintings.
export const BACKDROP_FILES = Object.freeze({
  hdOakhavenGround: 'hd/map/oakhaven-v6/ground.webp',
  hdFrostHollowGround: 'hd/map/FrostHollow/ground.webp',
});

// Files every session needs (fetched before the title screen). Class-owned art -- HD hero sheets, held gear,
// companions, painted skill rosters, ultimate FX -- lives in asset-atlas-class-files.js and loads after the class pick.
export const SHARED_ASSET_FILES = Object.freeze({
  inventoryIcons: 'custom/inventory-icons-chibi.png',
  weaponIcons: 'custom/weapon-icons-chibi.png',
  helmetIcons: 'custom/helmet-icons-chibi.png',
  armorIcons: 'custom/armor-icons-chibi.png',
  glovesIcons: 'custom/gloves-icons-chibi.png',
  bootsIcons: 'custom/boots-icons-chibi.png',
  bagFrame: 'custom/bag-frame-v2.png',
  paintedCombatFx: 'custom/combat-fx-painted.png',
  warriorSlashFx: 'custom/warrior-slashes-chibi.png',
  paintedUtility: 'custom/utility-icons-painted.png',
  field: PACK + 'tilesets/TilesetField.png', water: PACK + 'tilesets/TilesetWater.png', relief: PACK + 'tilesets/TilesetRelief.png',
  floor: PACK + 'tilesets/TilesetFloor.png', nature: PACK + 'tilesets/TilesetNature.png', desert: PACK + 'tilesets/TilesetDesert.png',
  house: PACK + 'tilesets/TilesetHouse.png', dungeon: PACK + 'tilesets/TilesetDungeon.png', wall: PACK + 'tilesets/TilesetWallSimple.png',
  interior: PACK + 'tilesets/TilesetInteriorFloor.png',
  hero: PACK + 'characters/Hero.png', heroFace: PACK + 'characters/Hero-face.png',
  OldMan: PACK + 'characters/OldMan.png', OldManFace: PACK + 'characters/OldMan-face.png',
  Hunter: PACK + 'characters/Hunter.png', HunterFace: PACK + 'characters/Hunter-face.png',
  Villager4: PACK + 'characters/Villager4.png', Villager4Face: PACK + 'characters/Villager4-face.png',
  Caveman: PACK + 'characters/Caveman.png', CavemanFace: PACK + 'characters/Caveman-face.png',
  Sultan: PACK + 'characters/Sultan.png', SultanFace: PACK + 'characters/Sultan-face.png',
  Knight: PACK + 'characters/Knight.png', KnightFace: PACK + 'characters/Knight-face.png',
  Slime2: PACK + 'monsters/Slime2.png', BlueBat: PACK + 'monsters/BlueBat.png', KappaGreen: PACK + 'monsters/KappaGreen.png',
  Flam: PACK + 'monsters/Flam.png', Cyclope: PACK + 'monsters/Cyclope.png', Skull: PACK + 'monsters/Skull.png', SkullBlue: PACK + 'monsters/SkullBlue.png',
  bossIdle: PACK + 'boss/DemonCyclop-idle.png', bossWalk: PACK + 'boss/DemonCyclop-walk.png', bossHit: PACK + 'boss/DemonCyclop-hit.png',
  bossFace: PACK + 'boss/DemonCyclop-face.png',
  fxSlash: PACK + 'fx/slash.png', fxArc: PACK + 'fx/arc.png', fxCircular: PACK + 'fx/circular.png', fxFlame: PACK + 'fx/flame.png', fxSpark: PACK + 'fx/spark.png',
  coin: PACK + 'items/GoldCoin.png', lifePot: PACK + 'items/LifePot.png', milkPot: PACK + 'items/MilkPot.png',
  dialogBox: PACK + 'ui/DialogBoxFaceset.png', facesetBox: PACK + 'ui/FacesetBox.png',
  // custom hero set generated from the concept sheet (see assets/custom/hero/README.md)
  heroIdle: HERO + 'idle.png', heroWalk: HERO + 'walk.png', heroAttack1: HERO + 'attack1.png',
  heroAttack2: HERO + 'attack2.png', heroAttack3: HERO + 'attack3.png', heroDash: HERO + 'dash.png',
  heroHurt: HERO + 'hurt.png', heroDeath: HERO + 'death.png',
  fxSlashSmall: HERO + 'slash_small.png', fxSlashBig: HERO + 'slash_big.png', fxDashTrail: HERO + 'dash_trail.png',
  fxHitSpark: HERO + 'hit_spark.png',
  ...WIZARD_ICON_FILES, // wizard skill-tree icons
  ...MECHANIC_ICON_FILES, // mechanic skill-tree icons
  ...CLASS_PREVIEW_FILES, // portraits for the hero-select cards (the class sheets load after the pick)
  ...TITLE_HERO_FILES,
  // HD hand-painted ground tiles (Zenonia 4 style), see docs/system-architecture.md "HD Assets".
  // Built by tools/build-hd-tiles.py from assets/hd/tiles/*.png into assets/hd/tiles/normalized/.
  // Same boot-resilience rule as the HD hero set: missing files just log a 404 warning and
  // sprite-registry.js falls back to the pixel-art cut from GROUND_TILES per tile name.
  hdGrass: HD_TILE_NORM_DIR + 'grass.png', hdPath: HD_TILE_NORM_DIR + 'path.png',
  hdWater: HD_TILE_NORM_DIR + 'water.png', hdFloors: HD_TILE_NORM_DIR + 'floors.png',
  hdWalls: HD_TILE_NORM_DIR + 'walls.png', hdPortal: HD_TILE_NORM_DIR + 'portal.png',
  hdPlaza: HD_TILE_NORM_DIR + 'plaza.png', // seamless 2x2 stone macro drawn through the path block's silhouette
  ...BACKDROP_FILES,
  // HD hand-painted props (trees, rocks, houses, fences...): see asset-atlas-hd-props.js.
  ...HD_PROP_ASSET_FILES,
  // HD hand-painted NPC idle sheets + portraits (Oakhaven villagers): see asset-atlas-hd-npcs.js.
  ...HD_NPC_ASSET_FILES,
  // HD hand-painted monster walk-cycle sheets (Frost Hollow): see asset-atlas-hd-monsters.js.
  ...HD_MONSTER_ASSET_FILES,
  // Skill tree icons (24x24, used as-is -- no slicing needed). Keys match `icon` fields in
  // src/data/skill-definitions.js.
  iconCut: PACK + 'icons/Spell/Cut.png', iconAttackUpgrade: PACK + 'icons/Spell/AttackUpgrade.png',
  iconExplosion: PACK + 'icons/Spell/Explosion.png', iconRockSpike: PACK + 'icons/Spell/RockSpike.png',
  iconBookWind: PACK + 'icons/Spell/BookWind.png', iconUpgrade: PACK + 'icons/Spell/Upgrade.png',
  iconDefenseUpgrade: PACK + 'icons/Spell/DefenseUpgrade.png', iconCamouflage: PACK + 'icons/Spell/Camouflage.png',
  iconInteract: PACK + 'icons/Job & Action/Interact.png', iconHeal: PACK + 'icons/Spell/Heal.png',
  iconOrbLight: PACK + 'icons/Spell/OrbLight.png', iconPotion: PACK + 'icons/Job & Action/Potion.png',
  iconSing: PACK + 'icons/Job & Action/Sing.png', iconOrbFire: PACK + 'icons/Spell/OrbFire.png',
  iconVision: PACK + 'icons/Spell/Vision.png',
  // Equipment: weapon sprites (used as-is on the ground/inventory, no slicing needed) and the
  // 4 gear-slot icons (helmet/armor/gloves/boots -- gloves reuse the "Guard" icon). Keys match
  // `sprite` fields in src/data/item-definitions.js.
  itemSword: PACK + 'items/weapons/Sword.png', itemSword2: PACK + 'items/weapons/Sword2.png',
  itemKatana: PACK + 'items/weapons/Katana.png', itemBigSword: PACK + 'items/weapons/BigSword.png',
  itemAxe: PACK + 'items/weapons/Axe.png', itemRapier: PACK + 'items/weapons/Rapier.png',
  itemLance: PACK + 'items/weapons/Lance.png', itemHammer: PACK + 'items/weapons/Hammer.png',
  itemClub: PACK + 'items/weapons/Club.png',
  iconHelmet: PACK + 'icons/Items & Weapon/Helmet.png', iconArmor: PACK + 'icons/Items & Weapon/Armor.png',
  iconGuard: PACK + 'icons/Items & Weapon/Guard.png', iconBoot: PACK + 'icons/Items & Weapon/Boot.png',
});

/** Everything, shared + every class (tests and tooling; the game loads these in two stages). */
export const ASSET_FILES = Object.freeze({ ...SHARED_ASSET_FILES, ...ALL_CLASS_ASSET_FILES });

export const CHARACTER_DIRECTIONS = Object.freeze(['down', 'up', 'left', 'right']);

// Monster type -> 16px pixel sheet image. Boss uses the dedicated strips below. Frost Hollow's HD
// types (asset-atlas-hd-monsters.js) list a pack look-alike here as the fallback when their painted
// sheet is missing.
export const MONSTER_SHEETS = Object.freeze({
  slime: 'Slime2', bat: 'BlueBat', goblin: 'KappaGreen', fireSlime: 'Flam', goblinWarrior: 'Cyclope',
  skeleton: 'Skull', skeletonKnight: 'SkullBlue',
  frostSlime: 'Slime2', iceGoblin: 'KappaGreen', spectralWolf: 'BlueBat',
  frostfangKing: 'Cyclope',
});
// Custom hero: 24x24 frames, rows = CHARACTER_DIRECTIONS, columns = frames
export const HERO_ANIMATIONS = Object.freeze({
  idle: 'heroIdle', walk: 'heroWalk', attack1: 'heroAttack1', attack2: 'heroAttack2', attack3: 'heroAttack3',
  dash: 'heroDash', hurt: 'heroHurt', death: 'heroDeath',
});
export const HERO_FRAME = 24;

// The custom pixel hero sheets (assets/custom/hero/*.png, built from the concept art) were authored
// with the 'left' and 'right' rows physically swapped relative to CHARACTER_DIRECTIONS' order (row 2
// is drawn facing screen-right, row 3 facing screen-left). Rather than re-paint committed art or
// special-case CHARACTER_DIRECTIONS (shared with NPCs/monsters, which are NOT affected -- they use
// a different, column-based sheet layout from the CC0 pack), sprite-registry.js's pixel hero builder
// reads this map when picking a sheet row. HD sheets differ per sheet: see HD_HERO_SIDE_ROWS below.
export const HERO_SHEET_ROW = Object.freeze({ down: 0, up: 1, left: 3, right: 2 });

/** World units spanned by a sprite, given its native resolution (px per world unit). Pixel-art
 * sprites default to pixelsPerUnit=1 (1 source px = 1 world unit); HD sprites set it higher. */
export function unitsFor(sprite) {
  return sprite.width / (sprite.pixelsPerUnit || 1);
}

export const BOSS_STRIPS = Object.freeze({ frameW: 50, frameH: 50, idle: 'bossIdle', walk: 'bossWalk', hit: 'bossHit' });

// NPC sprite key -> image key (NPC definitions use the `sprite` field)
export const NPC_SHEETS = Object.freeze({
  elder: 'OldMan', hunter: 'Hunter', girl: 'Villager4', miner: 'Caveman', nomad: 'Sultan', knight: 'Knight',
});

// Horizontal FX strips: frame size (frame count is derived from the image width)
export const FX_STRIPS = Object.freeze({
  slash: { image: 'fxSlashSmall', w: 32, h: 32 },
  arc: { image: 'fxSlashBig', w: 48, h: 48 },
  dashTrail: { image: 'fxDashTrail', w: 32, h: 32 },
  hitSpark: { image: 'fxHitSpark', w: 16, h: 16 },
  circular: { image: 'fxCircular', w: 54, h: 55 },
  flame: { image: 'fxFlame', w: 25, h: 30 },
  spark: { image: 'fxSpark', w: 27, h: 35 },
});

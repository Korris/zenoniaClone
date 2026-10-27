// Where every sprite lives inside the Ninja Adventure (CC0) image files. Pure data.
// Cell coordinates are in 16px tiles: [cx, cy, w, h]. Character sheets are 64x112:
// columns = facing (down, up, left, right), rows 0-3 = walk frames, row 4 = attack.
// Monster sheets are 64x64: columns = facing, rows = 4 animation frames.
import { HD_PROP_ASSET_FILES, HD_PROPS, HD_PROP_PIXELS_PER_UNIT } from './asset-atlas-hd-props.js';
import { HD_NPC_ASSET_FILES } from './asset-atlas-hd-npcs.js';
import { HD_WIZARD_ASSET_FILES } from './asset-atlas-hd-wizard.js';
import { HD_MONSTER_ASSET_FILES } from './asset-atlas-hd-monsters.js';
import { WIZARD_ICON_FILES } from '../data/skill-definitions-wizard.js';
import { HD_MECHANIC_ASSET_FILES } from './asset-atlas-hd-mechanic.js';
import { MECHANIC_ICON_FILES } from '../data/skill-definitions-mechanic.js';

export { HD_PROPS, HD_PROP_PIXELS_PER_UNIT };

const PACK = 'ninja-adventure/';
const HERO = 'custom/hero/';
const HD_HERO_DIR = 'hd/hero/';
const HD_HERO_NORM_DIR = HD_HERO_DIR + 'normalized-hd/';
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

export const ASSET_FILES = Object.freeze({
  inventoryIcons: 'custom/inventory-icons-chibi.png',
  weaponIcons: 'custom/weapon-icons-chibi.png',
  helmetIcons: 'custom/helmet-icons-chibi.png',
  armorIcons: 'custom/armor-icons-chibi.png',
  glovesIcons: 'custom/gloves-icons-chibi.png',
  bootsIcons: 'custom/boots-icons-chibi.png',
  bagFrame: 'custom/bag-frame-v2.png',
  iceStorm: 'hd/wizard/fx/ice-storm.png',
  ...Object.fromEntries(['charge', 'sweep', 'slam', 'fissures'].flatMap(stage =>
    Array.from({ length: 6 }, (_, i) => [`earthBreak_${stage}_${i + 1}`, `custom/earth-break-frames/${stage}-${i + 1}.png`]))),
  rocketBarrage: 'custom/rocket-barrage-fx.png',
  ultimateIcons: 'custom/ultimate-icons-chibi.png',
  paintedSkillswarrior: 'custom/skill-icons-warrior-chibi.png',
  paintedSkillswizard: 'custom/skill-icons-wizard-chibi.png',
  paintedSkillsmechanic: 'custom/skill-icons-mechanic-chibi.png',
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
  // HD hand-painted hero set (Zenonia 4 style), see docs/system-architecture.md "HD assets".
  // Built by tools/build-hd-render.py from original artwork into normalized-hd/ (192px frames).
  // Only list keys for sheets that actually exist -- asset-loader.js tolerates a load failure fine
  // (Promise.allSettled), but a 404 for a file we know isn't there is just noise. If a sheet is
  // ever removed/renamed, drop its key here too; sprite-registry.js falls back to the 24px pixel
  // set for any animation whose HD key is absent or fails to load.
  hdHeroIdle: HD_HERO_NORM_DIR + 'idle.png', hdHeroWalk: HD_HERO_NORM_DIR + 'walk.png',
  hdHeroAttack1: HD_HERO_NORM_DIR + 'attack1.png', hdHeroAttack2: HD_HERO_NORM_DIR + 'attack2.png',
  hdHeroAttack3: HD_HERO_NORM_DIR + 'attack3.png', hdHeroDash: HD_HERO_NORM_DIR + 'dash.png',
  hdHeroHurt: HD_HERO_NORM_DIR + 'hurt.png', hdHeroDeath: HD_HERO_NORM_DIR + 'death.png',
  hdHeroFace: HD_HERO_DIR + 'normalized/hero_face.png',
  ...HD_WIZARD_ASSET_FILES, // wizard class HD set, see asset-atlas-hd-wizard.js
  ...WIZARD_ICON_FILES, // wizard skill-tree icons
  ...HD_MECHANIC_ASSET_FILES, // mechanic class HD set + gun FX, see asset-atlas-hd-mechanic.js
  ...MECHANIC_ICON_FILES, // mechanic skill-tree icons
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

export const CHARACTER_DIRECTIONS = Object.freeze(['down', 'up', 'left', 'right']);

// Ground tiles: single cells or 3x3 auto-tile blocks (block = [image, cx, cy] of the top-left cell).
export const GROUND_TILES = Object.freeze({
  grass: { image: 'field', variants: [[3, 3], [4, 3], [3, 4], [4, 4]] },
  darkGrass: { image: 'field', variants: [[3, 6], [4, 6], [3, 7], [4, 7]] },
  sand: { image: 'field', variants: [[3, 0], [4, 0], [3, 1], [4, 1]] },
  dirtBlock: { image: 'field', block: [0, 0] },
  plazaStone: { image: 'field', block: [0, 0] }, // pixel fallback only: the plaza reads as a dirt block
  waterBlock: { image: 'water', block: [0, 6] },
  bridge: { image: 'water', variants: [[5, 13]] },
  cliffTop: { image: 'relief', variants: [[5, 5]] },
  cliffWall: { image: 'relief', variants: [[5, 6]] },
  cliffBottom: { image: 'relief', variants: [[5, 7]] },
  caveWallTop: { image: 'relief', variants: [[5, 0]] },
  caveWall: { image: 'relief', variants: [[5, 1]] },
  caveFloor: { image: 'interior', variants: [[14, 13], [15, 13], [14, 14], [15, 14]] },
  castleFloor: { image: 'interior', variants: [[13, 8], [12, 8], [13, 9], [12, 9]] },
  castleWall: { image: 'wall', variants: [[0, 8]] },
  lava: { image: 'floor', variants: [[16, 22]] },
});

// Decorations drawn over grass for the flower tile ('q')
export const FLOWER_OVERLAYS = Object.freeze([['nature', 0, 11], ['nature', 3, 11], ['nature', 7, 11], ['nature', 2, 10]]);

// HD hand-painted ground tiles: same shape as GROUND_TILES (variants / 3x3 block) but cells are
// 64px (pixelsPerUnit 4) on the normalized sheets built by tools/build-hd-tiles.py. Keyed by the
// SAME sprite names as GROUND_TILES where there's a 1:1 equivalent, so tile-sprite-builder.js can
// merge HD over pixel per-name; `flowers` and `waterAnim` have no pixel-art equivalent (the pixel
// pack overlays flowers on grass and has no animated water variant).
// Seamless extras (see tile-block-renderer.js / tile-variant-picker.js):
// - `macro: [cols, rows]` -- the variants are quadrants of ONE seamless texture; the renderer picks
//   variant (ty % rows) * cols + (tx % cols) so neighbouring tiles continue each other's texture.
// - `center` -- macro variants for a block's open center cells (replaces the single 3x3 center).
// - `inner` -- 4 grass wedge masks [TL, TR, BL, BR] drawn over concave (inner) corners.
// - `fill` -- a macro material drawn THROUGH the block cells' alpha (stone through the dirt
//   silhouette): the block cells then only act as transition masks.
// - `shapes` -- pieces the 3x3 block lacks, keyed by which sides are OPEN (same family):
//   'TB'/'LR' 1-wide pipes, 'T'/'B'/'L'/'R' dead-end caps, '' a lone island.
export const HD_TILE_CELL = 64;
const PATH_SHAPES = Object.freeze({ TB: [0, 4], LR: [1, 4], T: [2, 4], B: [3, 4], L: [4, 4], R: [0, 5], '': [1, 5] });
const PATH_INNER = Object.freeze([[4, 0], [4, 1], [4, 2], [4, 3]]); // px per cell in the normalized sheets = 16 world units * pixelsPerUnit
export const HD_TILE_PIXELS_PER_UNIT = 4;
export const HD_TILES = Object.freeze({
  grass: { image: 'hdGrass', variants: [[0, 0], [1, 0], [2, 0], [3, 0]], macro: [2, 2] },
  // Same grass quadrants with painted flowers on top (kind 'flowers' draws these directly in HD).
  flowers: { image: 'hdGrass', variants: [[0, 1], [1, 1], [2, 1], [3, 1]], macro: [2, 2] },
  dirtBlock: {
    image: 'hdPath', block: [0, 0], inner: PATH_INNER, shapes: PATH_SHAPES,
    center: { variants: [[0, 3], [1, 3], [2, 3], [3, 3]], macro: [2, 2] },
  },
  waterBlock: { image: 'hdWater', block: [0, 0] },
  // Open-water animation: the block's own center cell + the 2 extra variants on row3 (col0/col1).
  waterAnim: { image: 'hdWater', variants: [[1, 1], [0, 3], [1, 3]] },
  bridge: { image: 'hdWater', variants: [[2, 3]] },
  sand: { image: 'hdFloors', variants: [[0, 0], [1, 0]], macro: [2, 1] },
  castleFloor: { image: 'hdFloors', variants: [[2, 0], [3, 0]], macro: [2, 1] },
  // Oakhaven plaza: stone-on-grass auto-tile. Uses the path block's cells purely as alpha masks and
  // draws the seamless stone macro (plaza.png) through them, sharing the dirt path's inner corners.
  plazaStone: {
    image: 'hdPath', block: [0, 0], inner: PATH_INNER, shapes: PATH_SHAPES,
    fill: { image: 'hdPlaza', variants: [[0, 0], [1, 0], [0, 1], [1, 1]], macro: [2, 2] },
  },
  caveFloor: { image: 'hdFloors', variants: [[0, 1], [1, 1]], macro: [2, 1] },
  lava: { image: 'hdFloors', variants: [[2, 1], [3, 1]], macro: [2, 1] },
  cliffTop: { image: 'hdWalls', variants: [[0, 0]] },
  cliffWall: { image: 'hdWalls', variants: [[1, 0]] },
  cliffBottom: { image: 'hdWalls', variants: [[2, 0]] },
  castleWall: { image: 'hdWalls', variants: [[3, 0]] },
  caveWall: { image: 'hdWalls', variants: [[4, 0]] },
  caveWallTop: { image: 'hdWalls', variants: [[4, 0]] }, // no distinct cave-top art; reuse caveWall
  portal: { image: 'hdPortal', variants: [[0, 0], [1, 0], [2, 0]] },
});

// Props: [image, cx, cy, w, h] in cells. Footprints live in world/prop-definitions.js.
export const PROP_RECTS = Object.freeze({
  pine: ['nature', 0, 2, 2, 3], pineDark: ['nature', 2, 2, 2, 3], roundTree: ['nature', 4, 2, 3, 3],
  smallTree: ['nature', 0, 0, 2, 2], deadTree: ['nature', 4, 0, 2, 2], bigTree: ['nature', 3, 18, 3, 3],
  bush: ['nature', 0, 10, 1, 1], bush2: ['nature', 1, 10, 1, 1], stones: ['nature', 15, 18, 1, 1],
  rockBrown: ['nature', 12, 5, 3, 3], rockGrey: ['nature', 16, 5, 3, 3],
  rockSmallGrey: ['nature', 16, 8, 2, 2], rockSmallBrown: ['nature', 13, 8, 2, 2], stump: ['nature', 0, 8, 2, 2],
  palm: ['desert', 10, 4, 2, 3], cactus: ['desert', 14, 10, 1, 2], bones: ['desert', 18, 11, 2, 1],
  house: ['house', 0, 0, 4, 3], house2: ['house', 4, 0, 4, 3], house3: ['house', 8, 0, 4, 3], elderHouse: ['house', 8, 0, 4, 3],
  fence: ['house', 11, 10, 1, 1], signpost: ['house', 4, 3, 1, 1], pillar: ['house', 0, 19, 1, 3],
  torch: ['dungeon', 3, 2, 1, 2], crystal: ['dungeon', 4, 2, 1, 2],
});

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

// HD hero set: hand-painted (Zenonia 4 style) replacement for the pixel-art animations above.
// pixelsPerUnit = source px per world unit (4, vs. 1 for the pixel-art pack) so the renderer can
// draw an HD frame at the SAME world-space size (24 units) as the pixel frame, just crisper.
export const HD_HERO_ANIMATIONS = Object.freeze({
  idle: 'hdHeroIdle', walk: 'hdHeroWalk', attack1: 'hdHeroAttack1', attack2: 'hdHeroAttack2',
  attack3: 'hdHeroAttack3', dash: 'hdHeroDash', hurt: 'hdHeroHurt', death: 'hdHeroDeath',
});
export const HD_HERO = Object.freeze({ pixelsPerUnit: 8, frame: 192, animations: HD_HERO_ANIMATIONS });

// Which HD sheet row faces screen-left / screen-right. The v2 Warrior (black ponytail, 2026-10-27)
// was painted with every sheet in the requested order -- row 2 faces screen-left, row 3 screen-right
// -- verified by the red headband tails, which always trail behind him (see assets/hd/hero/v1 for the
// old blond set, whose side rows were inconsistent and needed per-sheet mirroring).
const HERO_SIDE = Object.freeze({ right: 3, left: 2 });
export const HD_HERO_SIDE_ROWS = Object.freeze({
  idle: HERO_SIDE, walk: HERO_SIDE, dash: HERO_SIDE, hurt: HERO_SIDE,
  attack1: HERO_SIDE, attack2: HERO_SIDE, attack3: HERO_SIDE, death: HERO_SIDE,
});

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

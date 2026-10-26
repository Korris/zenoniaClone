// HD hand-painted props (trees, rocks, houses, fences...) -- split out of asset-atlas.js to keep
// that file under the 200-line guideline. Same pixelsPerUnit model as HD_TILES/HD_HERO: each prop
// type maps to one normalized PNG (tools/build-hd-props.py, assets/hd/props/normalized/), already
// cropped/fit to that type's exact world-size x4 canvas. Keyed by the SAME name as PROP_RECTS
// (asset-atlas.js) so graphics/prop-sprite-builder.js can merge HD over pixel-art per prop type
// (HD wins where its file loaded; missing an entry here just means that type stays pixel-art).
const HD_PROP_NORM_DIR = 'hd/props/normalized/';

export const HD_PROP_PIXELS_PER_UNIT = 4;

// Oakhaven Village (v6) props, cut from ChatGPT sheets by tools/build-oakhaven-village-art.py. The
// normalized file is named after the prop type, so both tables below are derived from this list.
const OAKHAVEN_PROP_TYPES = [
  'elderHouse', 'cottageRed', 'hunterLodge', 'healerCottage', 'carpenterWorkshop',
  'well', 'lanternPost', 'marketStall', 'scarecrow', 'woodpile', 'campfire', 'barrel', 'crate', 'rowboat',
  'villageFence', 'villageOak', 'villagePine', 'villageBush', 'villageFlowerBush', 'villageSignpost',
];
const oakhavenKey = (type) => `hdProp${type[0].toUpperCase()}${type.slice(1)}`;

// Spread into asset-atlas.js's ASSET_FILES. Same boot-resilience rule as the other HD sets:
// missing files just log a 404 warning and prop-sprite-builder.js falls back to PROP_RECTS.
export const HD_PROP_ASSET_FILES = Object.freeze({
  hdPropPine: HD_PROP_NORM_DIR + 'pine.png', hdPropPineDark: HD_PROP_NORM_DIR + 'pineDark.png',
  hdPropRoundTree: HD_PROP_NORM_DIR + 'roundTree.png', hdPropBigTree: HD_PROP_NORM_DIR + 'bigTree.png',
  hdPropSmallTree: HD_PROP_NORM_DIR + 'smallTree.png', hdPropDeadTree: HD_PROP_NORM_DIR + 'deadTree.png',
  hdPropBush: HD_PROP_NORM_DIR + 'bush.png', hdPropBush2: HD_PROP_NORM_DIR + 'bush2.png',
  hdPropStones: HD_PROP_NORM_DIR + 'stones.png',
  hdPropRockBrown: HD_PROP_NORM_DIR + 'rockBrown.png', hdPropRockGrey: HD_PROP_NORM_DIR + 'rockGrey.png',
  hdPropRockSmallGrey: HD_PROP_NORM_DIR + 'rockSmallGrey.png', hdPropRockSmallBrown: HD_PROP_NORM_DIR + 'rockSmallBrown.png',
  hdPropStump: HD_PROP_NORM_DIR + 'stump.png',
  hdPropPalm: HD_PROP_NORM_DIR + 'palm.png', hdPropCactus: HD_PROP_NORM_DIR + 'cactus.png',
  hdPropBones: HD_PROP_NORM_DIR + 'bones.png',
  hdPropHouse: HD_PROP_NORM_DIR + 'house.png', hdPropHouse2: HD_PROP_NORM_DIR + 'house2.png',
  hdPropHouse3: HD_PROP_NORM_DIR + 'house3.png',
  hdPropFence: HD_PROP_NORM_DIR + 'fence.png', hdPropSignpost: HD_PROP_NORM_DIR + 'signpost.png',
  hdPropPillar: HD_PROP_NORM_DIR + 'pillar.png', hdPropTorch: HD_PROP_NORM_DIR + 'torch.png',
  hdPropCrystal: HD_PROP_NORM_DIR + 'crystal.png',
  ...Object.fromEntries(OAKHAVEN_PROP_TYPES.map((t) => [oakhavenKey(t), `${HD_PROP_NORM_DIR}${t}.png`])),
});

export const HD_PROPS = Object.freeze({
  pine: 'hdPropPine', pineDark: 'hdPropPineDark', roundTree: 'hdPropRoundTree', bigTree: 'hdPropBigTree',
  smallTree: 'hdPropSmallTree', deadTree: 'hdPropDeadTree',
  bush: 'hdPropBush', bush2: 'hdPropBush2', stones: 'hdPropStones',
  rockBrown: 'hdPropRockBrown', rockGrey: 'hdPropRockGrey',
  rockSmallGrey: 'hdPropRockSmallGrey', rockSmallBrown: 'hdPropRockSmallBrown', stump: 'hdPropStump',
  palm: 'hdPropPalm', cactus: 'hdPropCactus', bones: 'hdPropBones',
  house: 'hdPropHouse', house2: 'hdPropHouse2', house3: 'hdPropHouse3',
  fence: 'hdPropFence', signpost: 'hdPropSignpost', pillar: 'hdPropPillar',
  torch: 'hdPropTorch', crystal: 'hdPropCrystal',
  ...Object.fromEntries(OAKHAVEN_PROP_TYPES.map((t) => [t, oakhavenKey(t)])),
});

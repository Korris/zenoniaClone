// Monster stat blocks. `speed` is px/s; ranges are in px; `behavior` picks the AI attack style.
// Drops are probabilities per kill; gold is an inclusive [min, max] range.
const base = (overrides) => ({
  aggroRange: 70,
  leashRange: 150,
  attackRange: 12,
  attackWindupMs: 350,
  attackCooldownMs: 1200,
  behavior: 'melee',
  hitbox: { w: 12, h: 8 },
  drops: { hpPotion: 0.15, mpPotion: 0.1, upgradeStone: 0.05 },
  gold: [1, 3],
  ...overrides,
});

export const MONSTER_DEFINITIONS = Object.freeze({
  slime: base({ name: 'Slime', sprite: 'slime', hp: 30, atk: 6, def: 1, speed: 30, exp: 12, itemLevel: 1 }),
  bat: base({ name: 'Bat', sprite: 'bat', hp: 22, atk: 8, def: 0, speed: 60, exp: 15, aggroRange: 90, attackCooldownMs: 900, itemLevel: 1 }),
  goblin: base({ name: 'Goblin', sprite: 'goblin', hp: 60, atk: 12, def: 3, speed: 42, exp: 30, gold: [2, 6], hitbox: { w: 12, h: 10 }, itemLevel: 3 }),
  skeleton: base({
    name: 'Skeleton', sprite: 'skeleton', hp: 80, atk: 15, def: 5, speed: 34, exp: 45, gold: [3, 7],
    behavior: 'ranged', attackRange: 80, attackWindupMs: 500, attackCooldownMs: 1800, aggroRange: 100,
    projectileSpeed: 90, hitbox: { w: 12, h: 10 }, itemLevel: 4,
  }),
  fireSlime: base({ name: 'Fire Slime', sprite: 'fireSlime', hp: 110, atk: 20, def: 6, speed: 36, exp: 70, gold: [4, 9], itemLevel: 6 }),
  goblinWarrior: base({
    name: 'Goblin Warrior', sprite: 'goblinWarrior', hp: 150, atk: 26, def: 9, speed: 46, exp: 95, gold: [6, 12],
    attackRange: 14, attackCooldownMs: 1000, hitbox: { w: 12, h: 10 }, itemLevel: 8,
  }),
  skeletonKnight: base({
    name: 'Skeleton Knight', sprite: 'skeletonKnight', hp: 220, atk: 34, def: 14, speed: 38, exp: 140, gold: [8, 16],
    attackRange: 14, attackWindupMs: 450, attackCooldownMs: 1300, aggroRange: 90, hitbox: { w: 12, h: 10 }, itemLevel: 11,
  }),
  // Frost Hollow (Lv 1-3 zone between the village and the meadow). HD painted sheets, see
  // src/graphics/asset-atlas-hd-monsters.js; `sprite` names the pixel fallback in MONSTER_SHEETS.
  frostSlime: base({ name: 'Frost Slime', sprite: 'frostSlime', hp: 36, atk: 7, def: 2, speed: 30, exp: 14, itemLevel: 1 }),
  iceGoblin: base({
    name: 'Ice Goblin', sprite: 'iceGoblin', hp: 58, atk: 11, def: 3, speed: 44, exp: 28, gold: [2, 5],
    hitbox: { w: 12, h: 10 }, itemLevel: 2,
  }),
  spectralWolf: base({
    name: 'Spectral Wolf', sprite: 'spectralWolf', hp: 48, atk: 10, def: 1, speed: 62, exp: 26, gold: [1, 4],
    aggroRange: 95, attackCooldownMs: 900, hitbox: { w: 14, h: 8 }, itemLevel: 2,
  }),
  golemBoss: base({
    name: 'Demon Cyclops', sprite: 'golemBoss', hp: 1500, atk: 45, def: 18, speed: 28, exp: 1200, gold: [80, 120],
    behavior: 'boss', aggroRange: 130, leashRange: 9999, attackRange: 24, attackWindupMs: 600, attackCooldownMs: 1600,
    projectileSpeed: 70, hitbox: { w: 22, h: 14 }, drops: { hpPotion: 1, mpPotion: 1, upgradeStone: 1, upgradeStoneCount: 2 }, itemLevel: 14,
  }),
  frostfangKing: base({
    name: 'Frostfang King', sprite: 'iceGoblin', hp: 580, atk: 33, def: 3, speed: 36,
    exp: 280, gold: [20, 50], itemLevel: 3, isBoss: true, localBoss: true,
    stationaryUntilAggro: true, renderScale: 1.65, aggroRange: 85, leashRange: 120,
    attackRange: 20, attackWindupMs: 650, attackCooldownMs: 1500,
    hitbox: { w: 22, h: 14 }, drops: { hpPotion: 1, mpPotion: 1, upgradeStone: 1, upgradeStoneCount: 2 },
  }),
});

export const BOSS_TYPE = 'golemBoss';

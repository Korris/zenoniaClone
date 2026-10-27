// Wizard skill tree: 3 branches (fire / frost / arcane) x 5 nodes, same shape and schema as the
// warrior tree in skill-definitions.js (merged into SKILL_DEFINITIONS there). Effects reuse the
// existing cast kinds; wizard extras: projectile `explodeRadius` (per level) + `explodeFx` burst on
// impact, and radial `fx`/`fxDiameter` pick an HD burst sprite instead of the warrior's sword arc.
export const WIZARD_SKILL_DEFINITIONS = Object.freeze({
  // ---- FIRE ----
  fireball: {
    id: 'fireball', name: 'Fireball', type: 'active', branch: 'fire', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [7, 8, 9, 10, 11], cooldownMs: [2200, 2100, 2000, 1900, 1800],
    damageMult: [1.5, 1.8, 2.1, 2.5, 3.0], range: [110, 120, 130, 140, 150], explodeRadius: [20, 22, 24, 26, 30],
    icon: 'iconFireball', description: 'Hurl a fireball that explodes on impact.',
    effect: { kind: 'projectile', speed: 170, spriteKey: 'fireball', w: 12, h: 12, scale: 1.0, pierceFromLevel: 99, explodeFx: 'fireBurst', label: 'Fireball!', textColor: '#ff9a4d' },
  },
  pyromancy: {
    id: 'pyromancy', name: 'Pyromancy', type: 'passive', branch: 'fire', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconBookFire', description: 'Passive: boosts magic bolt (combo) damage.',
    passive: { comboMult: [0.06, 0.12, 0.18, 0.24, 0.30] },
  },
  flameBurst: {
    id: 'flameBurst', name: 'Flame Burst', type: 'active', branch: 'fire', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'fireball', level: 2 }],
    mpCost: [11, 12, 13, 14, 16], cooldownMs: [5000, 4800, 4600, 4400, 4000],
    damageMult: [1.3, 1.6, 1.9, 2.3, 2.7], radius: [30, 32, 34, 36, 40],
    icon: 'iconExplosion', description: 'Erupt in flames, scorching every enemy around you.',
    effect: { kind: 'radial', fx: 'fireBurst', fxDiameter: 40, knockback: 130, label: 'Flame Burst!', textColor: '#ff9a4d' },
  },
  searingMind: {
    id: 'searingMind', name: 'Searing Mind', type: 'passive', branch: 'fire', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'pyromancy', level: 2 }],
    icon: 'iconOrbFire', description: 'Passive: chance to land a critical spell (x1.6 damage).',
    passive: { critChance: [0.03, 0.06, 0.09, 0.12, 0.15] },
  },
  meteorRain: {
    id: 'meteorRain', name: 'Meteor Rain', type: 'active', branch: 'fire', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'flameBurst', level: 3 }, { skill: 'searingMind', level: 1 }],
    mpCost: [18, 20, 22, 24, 27], cooldownMs: [9000, 8600, 8200, 7800, 7200],
    damageMult: [1.5, 1.8, 2.1, 2.5, 2.9], radius: [38, 40, 42, 44, 48],
    icon: 'iconBookRock', description: 'Call down three waves of meteors around you.',
    effect: { kind: 'radial', fx: 'fireBurst', fxDiameter: 40, knockback: 150, pulses: 3, pulseIntervalMs: 260, label: 'Meteor Rain!', textColor: '#ff7a3a' },
  },

  // ---- FROST ----
  iceShard: {
    id: 'iceShard', name: 'Ice Shard', type: 'active', branch: 'frost', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [5, 6, 6, 7, 8], cooldownMs: [1400, 1350, 1300, 1250, 1150],
    damageMult: [1.0, 1.2, 1.4, 1.6, 1.8], range: [140, 150, 160, 170, 180],
    icon: 'iconBookIce', description: 'A fast shard of ice that pierces every foe in line.',
    effect: { kind: 'projectile', speed: 260, spriteKey: 'iceShard', w: 10, h: 10, scale: 0.9, pierceFromLevel: 1, label: 'Ice Shard!', textColor: '#9fe0ff' },
  },
  frostArmor: {
    id: 'frostArmor', name: 'Frost Armor', type: 'passive', branch: 'frost', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconOrbWater', description: 'Passive: a coat of ice raises defense.',
    passive: { defAdd: [2, 4, 6, 8, 10] },
  },
  frostNova: {
    id: 'frostNova', name: 'Frost Nova', type: 'active', branch: 'frost', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'iceShard', level: 2 }],
    mpCost: [10, 11, 12, 13, 15], cooldownMs: [6000, 5700, 5400, 5100, 4600],
    damageMult: [1.0, 1.25, 1.5, 1.75, 2.0], radius: [32, 34, 36, 38, 42],
    icon: 'iconMist', description: 'A ring of frost that blasts nearby enemies away.',
    effect: { kind: 'radial', fx: 'frostNova', fxDiameter: 40, knockback: 190, label: 'Frost Nova!', textColor: '#bfe9ff' },
  },
  glacialHeart: {
    id: 'glacialHeart', name: 'Glacial Heart', type: 'passive', branch: 'frost', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'frostArmor', level: 1 }],
    icon: 'iconUpgrade', description: 'Passive: increases maximum HP.',
    passive: { hpMult: [0.08, 0.16, 0.24, 0.32, 0.40] },
  },
  blizzard: {
    id: 'blizzard', name: 'Ice Storm', type: 'active', branch: 'frost', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'frostNova', level: 3 }, { skill: 'glacialHeart', level: 2 }],
    mpCost: [16, 18, 20, 22, 25], cooldownMs: [8500, 8100, 7700, 7300, 6800],
    damageMult: [1.0, 1.2, 1.4, 1.65, 1.9], radius: [40, 42, 44, 46, 50],
    icon: 'iconWaterCanon', description: 'Summons falling ice meteors ahead, striking the area four times.',
    effect: { kind: 'iceStorm', knockback: 90, textColor: '#bfe9ff' },
  },

  // ---- ARCANE ----
  manaFlow: {
    id: 'manaFlow', name: 'Mana Flow', type: 'passive', branch: 'arcane', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconOrbLight', description: 'Passive: increases max MP and MP regeneration.',
    passive: { mpMult: [0.10, 0.20, 0.30, 0.40, 0.50], mpRegenMult: [0.25, 0.50, 0.75, 1.00, 1.25] },
  },
  mendingLight: {
    id: 'mendingLight', name: 'Mending Light', type: 'active', branch: 'arcane', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 14], requires: [],
    mpCost: [12, 13, 14, 16, 18], cooldownMs: [12000, 12000, 12000, 12000, 12000],
    healPercent: [18, 23, 28, 33, 40],
    icon: 'iconHeal', description: 'Weave arcane light to heal yourself.',
    effect: { kind: 'heal', label: 'Mending Light!', textColor: '#8fff8f' },
  },
  blink: {
    id: 'blink', name: 'Blink', type: 'active', branch: 'arcane', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'manaFlow', level: 1 }],
    mpCost: [6, 6, 7, 7, 8], cooldownMs: [3500, 3300, 3100, 2900, 2500],
    damageMult: [0.6, 0.75, 0.9, 1.05, 1.2], dashDistance: [60, 66, 72, 78, 86],
    icon: 'iconPermutation', description: 'Flash forward in a streak of arcane light.',
    effect: { kind: 'dash', label: 'Blink!', textColor: '#d8a8ff' },
  },
  arcaneEmpower: {
    id: 'arcaneEmpower', name: 'Arcane Empower', type: 'active', branch: 'arcane', tier: 2,
    maxLevel: 5, requiredLevel: [6, 8, 11, 14, 18], requires: [{ skill: 'manaFlow', level: 2 }],
    mpCost: [14, 15, 16, 17, 19], cooldownMs: [20000, 19000, 18000, 17000, 15000],
    atkMult: [1.2, 1.25, 1.3, 1.35, 1.45],
    icon: 'iconBookLight', description: 'Overcharge your magic, boosting attack power for a time.',
    effect: { kind: 'buff', durationMs: 8000, label: 'Arcane Empower!', textColor: '#d8a8ff' },
  },
  arcaneMastery: {
    id: 'arcaneMastery', name: 'Arcane Mastery', type: 'passive', branch: 'arcane', tier: 3,
    maxLevel: 5, requiredLevel: [8, 11, 14, 17, 20],
    requires: [{ skill: 'blink', level: 2 }, { skill: 'arcaneEmpower', level: 1 }],
    icon: 'iconMagicWeapon', description: 'Passive: stronger bolts and more frequent critical spells.',
    passive: { comboMult: [0.05, 0.10, 0.15, 0.20, 0.25], critChance: [0.02, 0.04, 0.06, 0.08, 0.10] },
  },
});

// Display order per branch: tier 1 pair, tier 2 pair, tier 3 node (same convention as SKILL_ORDER).
export const WIZARD_SKILL_ORDER = Object.freeze([
  'fireball', 'pyromancy', 'flameBurst', 'searingMind', 'meteorRain',
  'iceShard', 'frostArmor', 'frostNova', 'glacialHeart', 'blizzard',
  'manaFlow', 'mendingLight', 'blink', 'arcaneEmpower', 'arcaneMastery',
]);

// New spell icons used above (registered in ASSET_FILES via asset-atlas.js).
export const WIZARD_ICON_FILES = Object.freeze({
  iconFireball: 'ninja-adventure/icons/Spell/Fireball.png',
  iconBookFire: 'ninja-adventure/icons/Spell/BookFire.png',
  iconBookIce: 'ninja-adventure/icons/Spell/BookIce.png',
  iconBookRock: 'ninja-adventure/icons/Spell/BookRock.png',
  iconBookLight: 'ninja-adventure/icons/Spell/BookLight.png',
  iconOrbWater: 'ninja-adventure/icons/Spell/OrbWater.png',
  iconMist: 'ninja-adventure/icons/Spell/Mist.png',
  iconWaterCanon: 'ninja-adventure/icons/Spell/WaterCanon.png',
  iconPermutation: 'ninja-adventure/icons/Spell/Permutation.png',
  iconMagicWeapon: 'ninja-adventure/icons/Spell/MagicWeapon.png',
});

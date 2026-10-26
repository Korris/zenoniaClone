// Mechanic skill tree: 3 branches (gunnery / explosives / engineering) x 5 nodes, same shape and
// schema as the warrior and wizard trees (merged into SKILL_DEFINITIONS in skill-definitions.js).
// Every active reuses an existing cast kind (projectile / radial / heal / dash / buff), so the
// Mechanic needs no new cast code -- only its own HD FX (asset-atlas-hd-mechanic.js).
export const MECHANIC_SKILL_DEFINITIONS = Object.freeze({
  // ---- GUNNERY ----
  piercingShot: {
    id: 'piercingShot', name: 'Piercing Shot', type: 'active', branch: 'gunnery', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [5, 6, 6, 7, 8], cooldownMs: [1500, 1400, 1300, 1200, 1100],
    damageMult: [1.2, 1.4, 1.6, 1.9, 2.2], range: [150, 160, 170, 180, 190],
    icon: 'iconKunai', description: 'A charged plasma round that pierces every foe in line.',
    effect: { kind: 'projectile', speed: 320, spriteKey: 'plasmaShot', w: 10, h: 8, scale: 1.0, pierceFromLevel: 1, label: 'Piercing Shot!', textColor: '#7fe8ff' },
  },
  marksmanship: {
    id: 'marksmanship', name: 'Marksmanship', type: 'passive', branch: 'gunnery', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconVision', description: 'Passive: boosts gun combo damage.',
    passive: { comboMult: [0.06, 0.12, 0.18, 0.24, 0.30] },
  },
  gatlingSpin: {
    id: 'gatlingSpin', name: 'Gatling Spin', type: 'active', branch: 'gunnery', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'piercingShot', level: 2 }],
    mpCost: [11, 12, 13, 14, 16], cooldownMs: [6000, 5700, 5400, 5100, 4600],
    damageMult: [0.7, 0.85, 1.0, 1.15, 1.3], radius: [30, 32, 34, 36, 40],
    icon: 'iconShuriken', description: 'Spin and spray bullets in every direction, three times.',
    effect: { kind: 'radial', fx: 'shockPulse', fxDiameter: 40, knockback: 70, pulses: 3, pulseIntervalMs: 200, label: 'Gatling Spin!', textColor: '#ffe27a' },
  },
  steadyAim: {
    id: 'steadyAim', name: 'Steady Aim', type: 'passive', branch: 'gunnery', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'marksmanship', level: 2 }],
    icon: 'iconLuckUpgrade', description: 'Passive: chance to land a critical shot (x1.6 damage).',
    passive: { critChance: [0.04, 0.08, 0.12, 0.16, 0.20] },
  },
  railgun: {
    id: 'railgun', name: 'Railgun', type: 'active', branch: 'gunnery', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'gatlingSpin', level: 3 }, { skill: 'steadyAim', level: 1 }],
    mpCost: [16, 18, 20, 22, 25], cooldownMs: [7000, 6600, 6200, 5800, 5200],
    damageMult: [2.6, 3.0, 3.4, 3.9, 4.5], range: [200, 210, 220, 230, 240],
    icon: 'iconMagicWeapon', description: 'A devastating rail slug that tears through a whole line of enemies.',
    effect: { kind: 'projectile', speed: 460, spriteKey: 'plasmaShot', w: 14, h: 10, scale: 1.8, pierceFromLevel: 1, label: 'Railgun!', textColor: '#7fe8ff' },
  },

  // ---- EXPLOSIVES ----
  rocketShot: {
    id: 'rocketShot', name: 'Rocket Shot', type: 'active', branch: 'explosives', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [7, 8, 9, 10, 11], cooldownMs: [2400, 2300, 2200, 2100, 2000],
    damageMult: [1.5, 1.8, 2.1, 2.5, 3.0], range: [120, 130, 140, 150, 160], explodeRadius: [22, 24, 26, 28, 32],
    icon: 'iconExplosion', description: 'Fire a mini rocket that explodes on impact.',
    effect: { kind: 'projectile', speed: 190, spriteKey: 'rocket', w: 12, h: 10, scale: 1.0, pierceFromLevel: 99, explodeFx: 'explosion', label: 'Rocket!', textColor: '#ff9a4d' },
  },
  blastPlating: {
    id: 'blastPlating', name: 'Blast Plating', type: 'passive', branch: 'explosives', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconDefenseUpgrade', description: 'Passive: riveted armor plates raise defense.',
    passive: { defAdd: [2, 4, 6, 8, 10] },
  },
  grenadeBurst: {
    id: 'grenadeBurst', name: 'Grenade Burst', type: 'active', branch: 'explosives', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'rocketShot', level: 2 }],
    mpCost: [11, 12, 13, 14, 16], cooldownMs: [5200, 5000, 4800, 4600, 4200],
    damageMult: [1.4, 1.7, 2.0, 2.4, 2.8], radius: [30, 32, 34, 36, 40],
    icon: 'iconFireball', description: 'Drop a grenade at your feet that blasts every enemy around you.',
    effect: { kind: 'radial', fx: 'explosion', fxDiameter: 40, knockback: 150, label: 'Grenade!', textColor: '#ff9a4d' },
  },
  demolitionExpert: {
    id: 'demolitionExpert', name: 'Demolition Expert', type: 'passive', branch: 'explosives', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'blastPlating', level: 1 }],
    icon: 'iconAttackUpgrade', description: 'Passive: heavier charges, stronger gun combo.',
    passive: { comboMult: [0.05, 0.10, 0.15, 0.20, 0.25] },
  },
  carpetBomb: {
    id: 'carpetBomb', name: 'Carpet Bomb', type: 'active', branch: 'explosives', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'grenadeBurst', level: 3 }, { skill: 'demolitionExpert', level: 1 }],
    mpCost: [18, 20, 22, 24, 27], cooldownMs: [9000, 8600, 8200, 7800, 7200],
    damageMult: [1.4, 1.7, 2.0, 2.4, 2.8], radius: [38, 40, 42, 44, 48],
    icon: 'iconBookFire', description: 'Call in three waves of bombs around you.',
    effect: { kind: 'radial', fx: 'explosion', fxDiameter: 40, knockback: 140, pulses: 3, pulseIntervalMs: 260, label: 'Carpet Bomb!', textColor: '#ff7a3a' },
  },

  // ---- ENGINEERING ----
  repairKit: {
    id: 'repairKit', name: 'Repair Kit', type: 'active', branch: 'engineering', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 14], requires: [],
    mpCost: [12, 13, 14, 16, 18], cooldownMs: [12000, 12000, 12000, 12000, 12000],
    healPercent: [18, 23, 28, 33, 40],
    icon: 'iconRepair', description: 'Patch yourself up with a quick repair kit.',
    effect: { kind: 'heal', label: 'Repair Kit!', textColor: '#8fff8f' },
  },
  alloyFrame: {
    id: 'alloyFrame', name: 'Alloy Frame', type: 'passive', branch: 'engineering', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconArmor', description: 'Passive: a reinforced frame increases maximum HP.',
    passive: { hpMult: [0.08, 0.16, 0.24, 0.32, 0.40] },
  },
  jetBoots: {
    id: 'jetBoots', name: 'Jet Boots', type: 'active', branch: 'engineering', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'alloyFrame', level: 1 }],
    mpCost: [6, 6, 7, 7, 8], cooldownMs: [3500, 3300, 3100, 2900, 2500],
    damageMult: [0.7, 0.85, 1.0, 1.15, 1.3], dashDistance: [60, 66, 72, 78, 86],
    icon: 'iconBoot', description: 'Rocket forward on jet boots, ramming anything in the way.',
    effect: { kind: 'dash', label: 'Jet Boots!', textColor: '#ffd27a' },
  },
  overclock: {
    id: 'overclock', name: 'Overclock', type: 'active', branch: 'engineering', tier: 2,
    maxLevel: 5, requiredLevel: [6, 8, 11, 14, 18], requires: [{ skill: 'repairKit', level: 1 }],
    mpCost: [14, 15, 16, 17, 19], cooldownMs: [20000, 19000, 18000, 17000, 15000],
    atkMult: [1.2, 1.25, 1.3, 1.35, 1.45],
    icon: 'iconUpgrade', description: 'Overclock your gear, boosting attack power for a time.',
    effect: { kind: 'buff', durationMs: 8000, label: 'Overclock!', textColor: '#ffd27a' },
  },
  masterEngineer: {
    id: 'masterEngineer', name: 'Master Engineer', type: 'passive', branch: 'engineering', tier: 3,
    maxLevel: 5, requiredLevel: [8, 11, 14, 17, 20],
    requires: [{ skill: 'jetBoots', level: 2 }, { skill: 'overclock', level: 1 }],
    icon: 'iconPermutation', description: 'Passive: more energy (MP and MP regen) and harder-hitting shots.',
    passive: { mpMult: [0.10, 0.20, 0.30, 0.40, 0.50], mpRegenMult: [0.25, 0.50, 0.75, 1.00, 1.25], comboMult: [0.04, 0.08, 0.12, 0.16, 0.20] },
  },
});

// Display order per branch: tier 1 pair, tier 2 pair, tier 3 node (same convention as SKILL_ORDER).
export const MECHANIC_SKILL_ORDER = Object.freeze([
  'piercingShot', 'marksmanship', 'gatlingSpin', 'steadyAim', 'railgun',
  'rocketShot', 'blastPlating', 'grenadeBurst', 'demolitionExpert', 'carpetBomb',
  'repairKit', 'alloyFrame', 'jetBoots', 'overclock', 'masterEngineer',
]);

// Icons used above that no other tree registers yet (spread into ASSET_FILES via asset-atlas.js).
const ICON_DIR = 'ninja-adventure/icons/';
export const MECHANIC_ICON_FILES = Object.freeze({
  iconKunai: ICON_DIR + 'Items & Weapon/Kunai.png',
  iconShuriken: ICON_DIR + 'Items & Weapon/Shuriken.png',
  iconLuckUpgrade: ICON_DIR + 'Spell/LuckUpgrade.png',
  iconRepair: ICON_DIR + 'Job & Action/Repair.png',
});

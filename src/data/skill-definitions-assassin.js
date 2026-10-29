// Assassin skill tree: 3 branches (shadow / venom / agility) x 5 nodes, same schema as the other class
// trees (merged into SKILL_DEFINITIONS in skill-definitions.js). Shadow = blink + crit burst, Venom =
// poison DoTs and debuffs, Agility = dodge, speed and a mirror clone.
// ORDER MATTERS: ASSASSIN_SKILL_ORDER mirrors the rows/columns of the painted icon sheet
// (assets/custom/skill-icons-assassin-roster.png: 3 rows = branches, 5 columns = tier1 pair, tier2 pair, tier3).
// Effect kinds live in systems/assassin-skill-handlers.js (shadowStep, backstab, smokeBomb, phantomAssault,
// shadowClone) and systems/assassin-venom-handlers.js (poisonBlade, venomCloud, crippleStrike, deathMark).
// Adrenaline reuses the shared 'haste' kind, Blade Dance the shared multi-pulse 'radial' kind.
// Passive stats read elsewhere: critDamage (player-damage.js), evasion + attackSpeed (player-status.js),
// poisonMult (monster-status.js).
export const ASSASSIN_SKILL_DEFINITIONS = Object.freeze({
  // ---- SHADOW ----
  shadowStep: {
    id: 'shadowStep', name: 'Shadow Step', type: 'active', branch: 'shadow', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [8, 8, 9, 10, 11], cooldownMs: [4500, 4200, 3900, 3600, 3200],
    damageMult: [1.6, 1.9, 2.2, 2.6, 3.0], range: [70, 80, 90, 100, 110],
    icon: 'iconCamouflage', description: 'Vanish and reappear behind the nearest foe, stabbing it in the back.',
    effect: { kind: 'shadowStep', behindOffset: 12, invulnMs: 300, knockback: 40, critBonus: 0.1, behindMult: 1.25, label: 'Shadow Step!', textColor: '#d9a8ff' },
  },
  backstab: {
    id: 'backstab', name: 'Backstab', type: 'active', branch: 'shadow', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 15], requires: [],
    mpCost: [7, 8, 8, 9, 10], cooldownMs: [3000, 2900, 2800, 2600, 2400],
    damageMult: [2.2, 2.6, 3.0, 3.5, 4.0], behindMult: [1.5, 1.6, 1.7, 1.8, 2.0], critBonus: [0.25, 0.3, 0.35, 0.4, 0.5],
    icon: 'iconCut', description: 'A deadly precise stab with a high critical chance. Hits much harder from behind.',
    effect: { kind: 'backstab', reach: 24, width: 20, knockback: 40, label: 'Backstab!', textColor: '#d9a8ff' },
  },
  shadowMastery: {
    id: 'shadowMastery', name: 'Shadow Mastery', type: 'passive', branch: 'shadow', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'backstab', level: 1 }],
    icon: 'iconVision', description: 'Passive: critical hits deal more damage and land more often.',
    passive: { critDamage: [0.15, 0.3, 0.45, 0.6, 0.8], critChance: [0.01, 0.02, 0.03, 0.04, 0.05] },
  },
  smokeBomb: {
    id: 'smokeBomb', name: 'Smoke Bomb', type: 'active', branch: 'shadow', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'shadowStep', level: 2 }],
    mpCost: [12, 13, 14, 15, 17], cooldownMs: [14000, 13500, 13000, 12000, 11000],
    radius: [34, 36, 38, 40, 44], slowMult: [0.6, 0.55, 0.5, 0.45, 0.4], stealthMs: [2500, 3000, 3500, 4000, 4500], evasion: [0.25, 0.3, 0.35, 0.4, 0.5],
    icon: 'iconExplosion', description: 'Burst a smoke cloud: nearby foes are blinded and slowed while you slip into stealth and dodge more.',
    effect: { kind: 'smokeBomb', debuffMs: 3000, label: 'Smoke Bomb!', textColor: '#c9b6ff' },
  },
  phantomAssault: {
    id: 'phantomAssault', name: 'Phantom Assault', type: 'active', branch: 'shadow', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'smokeBomb', level: 3 }, { skill: 'shadowMastery', level: 1 }],
    mpCost: [22, 25, 28, 32, 36], cooldownMs: [16000, 15500, 15000, 14000, 13000],
    damageMult: [1.4, 1.6, 1.8, 2.0, 2.3], targets: [3, 4, 5, 6, 7], range: [90, 95, 100, 105, 110],
    icon: 'iconBookWind', description: 'Blink through several foes in a flash, slashing each one from behind. You cannot be hit meanwhile.',
    effect: { kind: 'phantomAssault', behindOffset: 12, intervalMs: 130, knockback: 50, critBonus: 0.15, behindMult: 1.25, label: 'Phantom Assault!', textColor: '#e0b8ff' },
  },

  // ---- VENOM ----
  poisonBlade: {
    id: 'poisonBlade', name: 'Poison Blade', type: 'active', branch: 'venom', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [8, 9, 10, 11, 12], cooldownMs: [12000, 11500, 11000, 10000, 9000],
    poisonPct: [0.3, 0.38, 0.46, 0.55, 0.65], charges: [5, 6, 7, 8, 10],
    icon: 'iconPotion', description: 'Coat your daggers: your next basic hits poison the foe (damage every second).',
    effect: { kind: 'poisonBlade', durationMs: 10000, poisonMs: 5000, label: 'Poison Blade!', textColor: '#9dff7a' },
  },
  toxicMastery: {
    id: 'toxicMastery', name: 'Toxic Mastery', type: 'passive', branch: 'venom', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconUpgrade', description: 'Passive: poison deals more damage.',
    passive: { poisonMult: [0.2, 0.4, 0.6, 0.8, 1.0] },
  },
  venomCloud: {
    id: 'venomCloud', name: 'Venom Cloud', type: 'active', branch: 'venom', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'poisonBlade', level: 2 }],
    mpCost: [14, 15, 16, 18, 20], cooldownMs: [14000, 13500, 13000, 12000, 11000],
    radius: [28, 31, 34, 37, 40], poisonPct: [0.4, 0.5, 0.6, 0.7, 0.85],
    icon: 'iconOrbWater', description: 'Throw a cloud of venom ahead: foes standing in it are poisoned.',
    effect: { kind: 'venomCloud', start: 34, durationMs: 6000, tickMs: 500, poisonMs: 1500, label: 'Venom Cloud!', textColor: '#9dff7a' },
  },
  crippleStrike: {
    id: 'crippleStrike', name: 'Cripple Strike', type: 'active', branch: 'venom', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'toxicMastery', level: 1 }],
    mpCost: [9, 10, 11, 12, 13], cooldownMs: [4500, 4300, 4100, 3800, 3400],
    damageMult: [1.8, 2.1, 2.5, 2.9, 3.4], slowMult: [0.7, 0.6, 0.5, 0.4, 0.3], slowMs: [2500, 3000, 3500, 4000, 4500],
    icon: 'iconRockSpike', description: 'Slash the foe\'s legs: damage and a heavy slow.',
    effect: { kind: 'crippleStrike', reach: 24, width: 22, knockback: 20, label: 'Cripple Strike!', textColor: '#9fe0ff' },
  },
  deathMark: {
    id: 'deathMark', name: 'Death Mark', type: 'active', branch: 'venom', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'venomCloud', level: 3 }, { skill: 'crippleStrike', level: 1 }],
    mpCost: [20, 23, 26, 29, 33], cooldownMs: [18000, 17500, 17000, 16000, 15000],
    damageMult: [3.0, 3.6, 4.2, 5.0, 6.0], storePct: [0.3, 0.4, 0.5, 0.6, 0.8], markMs: [4000, 4500, 5000, 5500, 6000], range: [80, 85, 90, 95, 100],
    icon: 'iconMagicWeapon', description: 'Mark the nearest foe. When the mark expires it detonates for big damage plus a share of the damage it took.',
    effect: { kind: 'deathMark', label: 'Death Mark!', textColor: '#e08cff' },
  },

  // ---- AGILITY ----
  evasion: {
    id: 'evasion', name: 'Evasion', type: 'passive', branch: 'agility', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconDefenseUpgrade', description: 'Passive: chance to dodge ordinary enemy attacks (MISS).',
    passive: { evasion: [0.04, 0.08, 0.12, 0.16, 0.2] },
  },
  quickReflexes: {
    id: 'quickReflexes', name: 'Quick Reflexes', type: 'passive', branch: 'agility', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconInteract', description: 'Passive: faster basic-attack speed and movement.',
    passive: { attackSpeed: [0.06, 0.12, 0.18, 0.24, 0.3], speedMult: [0.02, 0.04, 0.06, 0.08, 0.1] },
  },
  bladeDance: {
    id: 'bladeDance', name: 'Blade Dance', type: 'active', branch: 'agility', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'evasion', level: 2 }],
    mpCost: [11, 12, 13, 14, 16], cooldownMs: [5500, 5300, 5100, 4800, 4400],
    damageMult: [0.9, 1.05, 1.2, 1.4, 1.6], radius: [26, 28, 30, 32, 36],
    icon: 'iconCut', description: 'Spin in a whirl of daggers, slicing everything around you three times.',
    effect: { kind: 'radial', knockback: 50, pulses: 3, pulseIntervalMs: 130, tint: '#b57cff', label: 'Blade Dance!', textColor: '#d9a8ff' },
  },
  adrenaline: {
    id: 'adrenaline', name: 'Adrenaline', type: 'active', branch: 'agility', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'quickReflexes', level: 1 }],
    mpCost: [10, 11, 12, 13, 15], cooldownMs: [20000, 19000, 18000, 17000, 15000],
    hasteMult: [1.2, 1.28, 1.36, 1.44, 1.55],
    icon: 'iconSing', description: 'A rush of adrenaline: much faster movement and attacks for a short time.',
    effect: { kind: 'haste', durationMs: 7000, label: 'Adrenaline!', textColor: '#ff9ad0' },
  },
  shadowClone: {
    id: 'shadowClone', name: 'Shadow Clone', type: 'active', branch: 'agility', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'bladeDance', level: 3 }, { skill: 'adrenaline', level: 2 }],
    mpCost: [24, 27, 30, 34, 38], cooldownMs: [30000, 29000, 28000, 26000, 24000],
    cloneMult: [0.5, 0.6, 0.7, 0.8, 1.0], durationMs: [5000, 6000, 7000, 8000, 9000],
    icon: 'iconCamouflage', description: 'Summon a shadow clone beside you. Every basic hit you land is echoed by the clone for a few seconds.',
    effect: { kind: 'shadowClone', mirrorRadius: 22, label: 'Shadow Clone!', textColor: '#c9b6ff' },
  },
});

// Display order per branch: tier 1 pair, tier 2 pair, tier 3 node (= icon sheet row order).
export const ASSASSIN_SKILL_ORDER = Object.freeze([
  'shadowStep', 'backstab', 'shadowMastery', 'smokeBomb', 'phantomAssault',
  'poisonBlade', 'toxicMastery', 'venomCloud', 'crippleStrike', 'deathMark',
  'evasion', 'quickReflexes', 'bladeDance', 'adrenaline', 'shadowClone',
]);

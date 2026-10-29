// Skill tree data: per class, 3 branches x 5 nodes each (tiers 1-3), 15 nodes per class.
// Per-level arrays are indexed by (skillLevel - 1). `requiredLevel[i]` = hero level needed to
// buy skill level i+1. `requires` lists prerequisite nodes {skill, level} that must already be
// learned to that level before this node can be upgraded at all.
// Actives carry `effect` (kind + render/behavior params consumed by skill-cast-system.js).
// Passives carry `passive`: a map of stat name -> per-level array of bonus values, summed by
// computePassiveModifiers() in skill-system.js (see that file for the full stat list).
// Warrior tree; the wizard tree lives in skill-definitions-wizard.js. Both merge into SKILL_DEFINITIONS
// (ids are unique across classes; a skill's class is the class owning its branch, class-definitions.js).
import { WIZARD_SKILL_DEFINITIONS, WIZARD_SKILL_ORDER } from './skill-definitions-wizard.js';
import { MECHANIC_SKILL_DEFINITIONS, MECHANIC_SKILL_ORDER } from './skill-definitions-mechanic.js';
import { CLERIC_SKILL_DEFINITIONS, CLERIC_SKILL_ORDER } from './skill-definitions-cleric.js';
import { ASSASSIN_SKILL_DEFINITIONS, ASSASSIN_SKILL_ORDER } from './skill-definitions-assassin.js';

const WARRIOR_SKILL_DEFINITIONS = {
  // ---- SWORD ----
  slashWave: {
    id: 'slashWave', name: 'Slash Wave', type: 'active', branch: 'sword', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [6, 7, 8, 9, 10], cooldownMs: [2500, 2400, 2300, 2200, 2000],
    damageMult: [1.4, 1.7, 2.0, 2.4, 2.9], range: [90, 100, 110, 120, 130],
    icon: 'iconCut', description: 'Fire a crescent wave of energy forward.',
    effect: { kind: 'projectile', speed: 160, spriteKey: 'arc', w: 14, h: 14, pierceFromLevel: 3, label: 'Slash Wave!', textColor: '#e0a0ff' },
  },
  powerStrike: {
    id: 'powerStrike', name: 'Power Strike', type: 'passive', branch: 'sword', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconAttackUpgrade', description: 'Passive: boosts combo attack damage.',
    passive: { comboMult: [0.05, 0.10, 0.15, 0.20, 0.25] },
  },
  whirlwind: {
    id: 'whirlwind', name: 'Whirlwind', type: 'active', branch: 'sword', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'slashWave', level: 2 }],
    mpCost: [10, 11, 12, 13, 15], cooldownMs: [5000, 4800, 4600, 4400, 4000],
    damageMult: [1.2, 1.5, 1.8, 2.2, 2.6], radius: [28, 30, 32, 34, 38],
    icon: 'iconExplosion', description: 'Spin and hit every enemy around you.',
    effect: { kind: 'radial', knockback: 120, label: 'Whirlwind!', textColor: '#bfe9ff' },
  },
  heavyBlow: {
    id: 'heavyBlow', name: 'Heavy Blow', type: 'active', branch: 'sword', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'powerStrike', level: 2 }],
    mpCost: [9, 10, 11, 12, 14], cooldownMs: [3500, 3300, 3100, 2900, 2600],
    damageMult: [2.2, 2.6, 3.0, 3.5, 4.0],
    icon: 'iconRockSpike', description: 'A crushing strike that knocks enemies back.',
    effect: { kind: 'strike', reach: 20, width: 24, knockback: 160, label: 'Heavy Blow!', textColor: '#ffb13b' },
  },
  bladeStorm: {
    id: 'bladeStorm', name: 'Earth Break', type: 'active', branch: 'sword', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'whirlwind', level: 3 }, { skill: 'heavyBlow', level: 1 }],
    mpCost: [25, 27, 29, 32, 35], cooldownMs: [8000, 8000, 8000, 8000, 8000],
    damageMult: [3.2, 3.5, 3.8, 4.1, 4.4], radius: [42, 48, 54, 60, 66], range: [32, 34, 36, 38, 40],
    icon: 'iconBookWind', description: 'Charge, sweep and slam a giant sword of energy. Launches foes for 1.5s and leaves damaging fissures.',
    effect: { kind: 'earthBreak', textColor: '#ffb13b' },
  },

  // ---- BODY ----
  vitality: {
    id: 'vitality', name: 'Vitality', type: 'passive', branch: 'body', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconUpgrade', description: 'Passive: increases maximum HP.',
    passive: { hpMult: [0.10, 0.20, 0.30, 0.40, 0.55] },
  },
  ironSkin: {
    id: 'ironSkin', name: 'Iron Skin', type: 'passive', branch: 'body', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconDefenseUpgrade', description: 'Passive: increases defense.',
    passive: { defAdd: [2, 4, 6, 8, 10] },
  },
  dashStrike: {
    id: 'dashStrike', name: 'Dash Strike', type: 'active', branch: 'body', tier: 2,
    maxLevel: 5, requiredLevel: [6, 8, 11, 14, 18], requires: [{ skill: 'vitality', level: 1 }],
    mpCost: [8, 9, 10, 11, 12], cooldownMs: [4000, 3800, 3600, 3400, 3000],
    damageMult: [1.6, 1.9, 2.3, 2.7, 3.2], dashDistance: [56, 60, 64, 70, 76],
    icon: 'iconCamouflage', description: 'Dash forward, cutting through enemies in your path.',
    effect: { kind: 'dash', label: 'Dash Strike!', textColor: '#ffe680' },
  },
  swiftFeet: {
    id: 'swiftFeet', name: 'Swift Feet', type: 'passive', branch: 'body', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'ironSkin', level: 1 }],
    icon: 'iconInteract', description: 'Passive: increases movement speed.',
    passive: { speedMult: [0.04, 0.08, 0.12, 0.16, 0.20] },
  },
  secondWind: {
    id: 'secondWind', name: 'Second Wind', type: 'passive', branch: 'body', tier: 3,
    maxLevel: 5, requiredLevel: [9, 12, 15, 18, 21],
    requires: [{ skill: 'vitality', level: 3 }, { skill: 'swiftFeet', level: 2 }],
    icon: 'iconHeal', description: 'Passive: boosts HP and MP regeneration.',
    passive: { regenMult: [0.5, 1.0, 1.5, 2.0, 2.5] },
  },

  // ---- SPIRIT ----
  focus: {
    id: 'focus', name: 'Focus', type: 'passive', branch: 'spirit', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconOrbLight', description: 'Passive: increases max MP and MP regeneration.',
    passive: { mpRegenMult: [0.25, 0.50, 0.75, 1.00, 1.25], mpMult: [0.05, 0.10, 0.15, 0.20, 0.25] },
  },
  healingLight: {
    id: 'healingLight', name: 'Healing Light', type: 'active', branch: 'spirit', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 14], requires: [],
    mpCost: [12, 13, 14, 16, 18], cooldownMs: [12000, 12000, 12000, 12000, 12000],
    healPercent: [20, 25, 30, 35, 45],
    icon: 'iconPotion', description: 'Channel light to heal yourself.',
    effect: { kind: 'heal', label: 'Healing Light!', textColor: '#8fff8f' },
  },
  battleCry: {
    id: 'battleCry', name: 'Battle Cry', type: 'active', branch: 'spirit', tier: 2,
    maxLevel: 5, requiredLevel: [6, 8, 11, 14, 18], requires: [{ skill: 'focus', level: 2 }],
    mpCost: [14, 15, 16, 17, 19], cooldownMs: [20000, 19000, 18000, 17000, 15000],
    atkMult: [1.2, 1.25, 1.3, 1.35, 1.45],
    icon: 'iconSing', description: 'Rally your spirit, boosting attack power for a time.',
    effect: { kind: 'buff', durationMs: 8000, label: 'Battle Cry!', textColor: '#ffe680' },
  },
  spiritBolt: {
    id: 'spiritBolt', name: 'Spirit Bolt', type: 'active', branch: 'spirit', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'focus', level: 1 }],
    mpCost: [5, 6, 6, 7, 8], cooldownMs: [1200, 1200, 1100, 1100, 1000],
    damageMult: [0.9, 1.1, 1.3, 1.45, 1.6], range: [140, 150, 160, 170, 180],
    icon: 'iconOrbFire', description: 'A fast bolt of spirit energy that pierces foes.',
    effect: { kind: 'projectile', speed: 260, spriteKey: 'flame', w: 10, h: 10, pierceFromLevel: 1, label: 'Spirit Bolt!', textColor: '#9fe0ff' },
  },
  critEye: {
    id: 'critEye', name: 'Crit Eye', type: 'passive', branch: 'spirit', tier: 3,
    maxLevel: 5, requiredLevel: [8, 11, 14, 17, 20], requires: [{ skill: 'spiritBolt', level: 2 }],
    icon: 'iconVision', description: 'Passive: chance to land a critical strike (x1.6 damage).',
    passive: { critChance: [0.04, 0.08, 0.12, 0.16, 0.20] },
  },
};

export const SKILL_DEFINITIONS = Object.freeze({ ...WARRIOR_SKILL_DEFINITIONS, ...WIZARD_SKILL_DEFINITIONS, ...MECHANIC_SKILL_DEFINITIONS, ...CLERIC_SKILL_DEFINITIONS, ...ASSASSIN_SKILL_DEFINITIONS });

// Display/grouping order: tier 1 pair, tier 2 pair, tier 3 node, per branch (warrior, wizard, mechanic, cleric, assassin).
// Branches per class: CLASS_DEFINITIONS[classId].branches.
export const SKILL_ORDER = Object.freeze([
  'slashWave', 'powerStrike', 'whirlwind', 'heavyBlow', 'bladeStorm',
  'vitality', 'ironSkin', 'dashStrike', 'swiftFeet', 'secondWind',
  'focus', 'healingLight', 'battleCry', 'spiritBolt', 'critEye',
  ...WIZARD_SKILL_ORDER,
  ...MECHANIC_SKILL_ORDER,
  ...CLERIC_SKILL_ORDER,
  ...ASSASSIN_SKILL_ORDER,
]);

// Input action -> hotbar slot index (hotbar skills are bound dynamically, see skill-system.js).
export const SKILL_HOTKEY_ACTIONS = Object.freeze({ skill1: 0, skill2: 1, skill3: 2 });
// Display labels for the 3 hotbar slots, matching ACTION_KEYS in input-manager.js.
export const HOTBAR_SLOT_KEYS = Object.freeze(['K', 'L', 'I']);

/** Read a per-level value for a skill at the given skill level (1-based). */
export function getSkillValue(skillId, field, skillLevel) {
  const def = SKILL_DEFINITIONS[skillId];
  if (!def || skillLevel < 1) return 0;
  const arr = def[field];
  if (!Array.isArray(arr)) return 0;
  return arr[Math.min(skillLevel, arr.length) - 1];
}

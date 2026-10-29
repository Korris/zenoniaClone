// Cleric skill tree: 3 branches (holy / restoration / blessing) x 5 nodes, same schema as the other
// class trees (merged into SKILL_DEFINITIONS in skill-definitions.js). Holy = ranged smiting,
// Restoration = healing + survival, Blessing = buffs and shields.
// ORDER MATTERS: CLERIC_SKILL_ORDER mirrors the rows/columns of the painted icon sheet
// (assets/custom/skill-icons-cleric-roster.png: 3 rows = branches, 5 columns = tier1 pair, tier2 pair, tier3).
// Everything is self-targeted (single-player): "allies" means the hero. New effect kinds live in
// systems/cleric-skill-handlers.js: holyBlast, healZone, purify, shield, haste, angelsDescent.
// Resurrection Grace is a PASSIVE: a lethal hit instead leaves the hero at `surviveHpPct`% max HP
// with 1.5s invulnerability, then it recharges for `graceCooldownMs` (see systems/player-status.js).
import { HOLY_FX_TINT } from './cleric-fx-config.js';

export const CLERIC_SKILL_DEFINITIONS = Object.freeze({
  // ---- HOLY ----
  holyBolt: {
    id: 'holyBolt', name: 'Holy Bolt', type: 'active', branch: 'holy', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [6, 7, 7, 8, 9], cooldownMs: [1800, 1700, 1600, 1500, 1400],
    damageMult: [1.3, 1.6, 1.9, 2.2, 2.6], range: [120, 130, 140, 150, 160], explodeRadius: [14, 16, 18, 20, 24],
    icon: 'iconOrbLight', description: 'Hurl an orb of holy light that bursts on impact.',
    effect: { kind: 'projectile', speed: 200, spriteKey: 'magicBolt', w: 10, h: 10, scale: 0.9, pierceFromLevel: 99, explodeFx: 'arcaneBurst', tint: HOLY_FX_TINT, label: 'Holy Bolt!', textColor: '#fff0a0' },
  },
  smite: {
    id: 'smite', name: 'Smite', type: 'active', branch: 'holy', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 15], requires: [],
    mpCost: [8, 9, 10, 11, 12], cooldownMs: [3000, 2900, 2800, 2600, 2400],
    damageMult: [2.0, 2.4, 2.8, 3.3, 3.8],
    icon: 'iconCut', description: 'A focused blow of light that smites the foe in front of you.',
    effect: { kind: 'strike', reach: 36, width: 12, knockback: 110, fx: 'arcaneBurst', tint: HOLY_FX_TINT, fxScale: 0.7, label: 'Smite!', textColor: '#fff0a0' },
  },
  divineJudgment: {
    id: 'divineJudgment', name: 'Divine Judgment', type: 'active', branch: 'holy', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'holyBolt', level: 2 }],
    mpCost: [14, 15, 16, 18, 20], cooldownMs: [6500, 6200, 5900, 5600, 5000],
    damageMult: [2.0, 2.4, 2.8, 3.1, 3.5], radius: [30, 32, 34, 36, 40],
    icon: 'iconBookLight', description: 'Call a pillar of judgment down ahead of you, striking the area.',
    effect: { kind: 'holyBlast', start: 40, advance: 0, pulses: 1, delayMs: 350, intervalMs: 0, knockback: 120, fx: 'arcaneBurst', tint: HOLY_FX_TINT, fxDiameter: 40, label: 'Judgment!', textColor: '#fff0a0' },
  },
  consecrate: {
    id: 'consecrate', name: 'Consecrate', type: 'passive', branch: 'holy', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'smite', level: 1 }],
    icon: 'iconAttackUpgrade', description: 'Passive: holy light empowers your combo and sharpens critical hits.',
    passive: { comboMult: [0.06, 0.12, 0.18, 0.24, 0.30], critChance: [0.01, 0.02, 0.03, 0.04, 0.05] },
  },
  heavenlyRay: {
    id: 'heavenlyRay', name: 'Heavenly Ray', type: 'active', branch: 'holy', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'divineJudgment', level: 3 }, { skill: 'consecrate', level: 1 }],
    mpCost: [24, 27, 30, 33, 37], cooldownMs: [11000, 10500, 10000, 9500, 9000],
    damageMult: [1.0, 1.15, 1.3, 1.5, 1.7], radius: [18, 19, 20, 21, 22],
    icon: 'iconMagicWeapon', description: 'A beam of heavenly light sweeps forward in five rapid strikes.',
    effect: { kind: 'holyBlast', start: 18, advance: 18, pulses: 5, delayMs: 0, intervalMs: 130, knockback: 60, fx: 'arcaneBurst', tint: HOLY_FX_TINT, fxDiameter: 40, label: 'Heavenly Ray!', textColor: '#ffe680' },
  },

  // ---- RESTORATION ----
  heal: {
    id: 'heal', name: 'Heal', type: 'active', branch: 'restoration', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [10, 11, 12, 13, 15], cooldownMs: [9000, 8800, 8500, 8200, 7500],
    healPercent: [25, 31, 38, 45, 55],
    icon: 'iconHeal', description: 'Mend your wounds with holy light.',
    effect: { kind: 'heal', label: 'Heal!', textColor: '#8fff8f' },
  },
  regeneration: {
    id: 'regeneration', name: 'Regeneration', type: 'passive', branch: 'restoration', tier: 1,
    maxLevel: 5, requiredLevel: [1, 3, 6, 9, 13], requires: [],
    icon: 'iconUpgrade', description: 'Passive: faster HP and MP regeneration.',
    passive: { regenMult: [0.6, 1.2, 1.8, 2.4, 3.0], mpRegenMult: [0.1, 0.2, 0.3, 0.4, 0.5] },
  },
  healingCircle: {
    id: 'healingCircle', name: 'Healing Circle', type: 'active', branch: 'restoration', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'heal', level: 2 }],
    mpCost: [16, 18, 20, 22, 24], cooldownMs: [16000, 15500, 15000, 14000, 13000],
    healPercent: [4, 5, 6, 7, 8], radius: [34, 36, 38, 40, 44],
    icon: 'iconPotion', description: 'Lay a circle of light that heals you every second while you stand in it.',
    effect: { kind: 'healZone', durationMs: 6000, tickMs: 1000, label: 'Healing Circle!', textColor: '#8fff8f' },
  },
  purify: {
    id: 'purify', name: 'Purify', type: 'active', branch: 'restoration', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'regeneration', level: 1 }],
    mpCost: [8, 8, 9, 9, 10], cooldownMs: [10000, 9500, 9000, 8500, 8000],
    healPercent: [10, 13, 16, 19, 22], invulnMs: [500, 600, 700, 800, 900],
    icon: 'iconOrbWater', description: 'Cleanse hit-stun and knockback, heal a little and shrug off hits briefly.',
    effect: { kind: 'purify', label: 'Purify!', textColor: '#b8ffe0' },
  },
  resurrectionGrace: {
    id: 'resurrectionGrace', name: 'Resurrection Grace', type: 'passive', branch: 'restoration', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'healingCircle', level: 2 }, { skill: 'purify', level: 1 }],
    icon: 'iconHeal', description: 'Passive: a lethal blow instead leaves you alive with a burst of HP. Recharges slowly.',
    passive: { surviveHpPct: [20, 30, 40, 50, 60], graceCooldownMs: [120000, 110000, 100000, 90000, 80000] },
  },

  // ---- BLESSING ----
  blessingOfMight: {
    id: 'blessingOfMight', name: 'Blessing of Might', type: 'active', branch: 'blessing', tier: 1,
    maxLevel: 5, requiredLevel: [2, 4, 7, 10, 14], requires: [],
    mpCost: [12, 13, 14, 15, 17], cooldownMs: [22000, 21000, 20000, 19000, 17000],
    atkMult: [1.2, 1.25, 1.3, 1.35, 1.45],
    icon: 'iconSing', description: 'Bless yourself with might, boosting attack power for a time.',
    effect: { kind: 'buff', durationMs: 12000, label: 'Blessing of Might!', textColor: '#ffe680' },
  },
  sacredShield: {
    id: 'sacredShield', name: 'Sacred Shield', type: 'active', branch: 'blessing', tier: 1,
    maxLevel: 5, requiredLevel: [3, 5, 8, 11, 15], requires: [],
    mpCost: [12, 13, 14, 15, 17], cooldownMs: [18000, 17500, 17000, 16000, 14000],
    shieldPercent: [20, 26, 32, 38, 45],
    icon: 'iconGuard', description: 'A holy barrier absorbs damage up to a share of your max HP.',
    effect: { kind: 'shield', durationMs: 10000, label: 'Sacred Shield!', textColor: '#fff0a0' },
  },
  hasteHymn: {
    id: 'hasteHymn', name: 'Haste Hymn', type: 'active', branch: 'blessing', tier: 2,
    maxLevel: 5, requiredLevel: [4, 6, 9, 12, 16], requires: [{ skill: 'blessingOfMight', level: 2 }],
    mpCost: [10, 11, 12, 13, 15], cooldownMs: [24000, 23000, 22000, 21000, 19000],
    hasteMult: [1.15, 1.2, 1.25, 1.3, 1.4],
    icon: 'iconInteract', description: 'Sing a hymn that quickens your movement and attacks.',
    effect: { kind: 'haste', durationMs: 8000, label: 'Haste Hymn!', textColor: '#b8ffe0' },
  },
  spiritBond: {
    id: 'spiritBond', name: 'Spirit Bond', type: 'passive', branch: 'blessing', tier: 2,
    maxLevel: 5, requiredLevel: [5, 7, 10, 13, 17], requires: [{ skill: 'sacredShield', level: 1 }],
    icon: 'iconDefenseUpgrade', description: 'Passive: your spirit companion raises max HP, defense and MP regeneration.',
    passive: { hpMult: [0.04, 0.08, 0.12, 0.16, 0.20], defAdd: [1, 2, 3, 4, 5], mpRegenMult: [0.2, 0.4, 0.6, 0.8, 1.0] },
  },
  angelsDescent: {
    id: 'angelsDescent', name: "Angel's Descent", type: 'active', branch: 'blessing', tier: 3,
    maxLevel: 5, requiredLevel: [10, 13, 16, 19, 22],
    requires: [{ skill: 'hasteHymn', level: 3 }, { skill: 'spiritBond', level: 2 }],
    mpCost: [30, 34, 38, 42, 46], cooldownMs: [45000, 44000, 42000, 40000, 38000],
    damageMult: [2.5, 3.0, 3.5, 4.0, 4.6], radius: [44, 48, 52, 56, 62],
    atkMult: [1.25, 1.3, 1.35, 1.4, 1.5], hasteMult: [1.1, 1.12, 1.15, 1.18, 1.2], shieldPercent: [25, 30, 35, 40, 50],
    icon: 'iconBookLight', description: 'An angel descends: a holy blast around you, then attack, speed and shield blessings.',
    effect: { kind: 'angelsDescent', durationMs: 12000, knockback: 150, fx: 'arcaneBurst', tint: HOLY_FX_TINT, fxDiameter: 40, label: "Angel's Descent!", textColor: '#ffe680' },
  },
});

// Display order per branch: tier 1 pair, tier 2 pair, tier 3 node (= icon sheet row order).
export const CLERIC_SKILL_ORDER = Object.freeze([
  'holyBolt', 'smite', 'divineJudgment', 'consecrate', 'heavenlyRay',
  'heal', 'regeneration', 'healingCircle', 'purify', 'resurrectionGrace',
  'blessingOfMight', 'sacredShield', 'hasteHymn', 'spiritBond', 'angelsDescent',
]);

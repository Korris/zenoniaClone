// Hero tuning numbers shared by every class. Speeds are world px per second. Per-class base
// stats and growth live in class-definitions.js.
import { HOLY_FX_TINT } from './cleric-fx-config.js';

export const HERO_HITBOX = Object.freeze({ w: 10, h: 8 });
export const HP_REGEN_PER_SECOND = 0.6;
export const MP_REGEN_PER_SECOND = 1.5;
export const INVULNERABLE_AFTER_HIT_MS = 500;
export const DEATH_EXP_PENALTY = 0.1; // lose 10% of current-level EXP, never a level
export const SKILL_POINTS_PER_LEVEL = 1;
export const MAX_LEVEL = 30;

// Four-hit combo chain. Durations in ms; `mult` scales ATK; `reach` is hitbox depth in px;
// `stepPx` is how far the hero lunges forward during windup+active (gives the combo momentum).
export const COMBO_CONFIG = Object.freeze({
  chainWindowMs: 400,
  hits: [
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.0, reach: 30, width: 28, knockback: 40, stepPx: 6, fxScale: 1.45 },
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.15, reach: 32, width: 30, knockback: 50, stepPx: 8, fxScale: 1.55 },
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.25, reach: 34, width: 32, knockback: 55, stepPx: 8, fxScale: 1.65 },
    { windupMs: 90, activeMs: 120, recoverMs: 220, mult: 1.8, reach: 42, width: 40, knockback: 110, stepPx: 16, fxScale: 1.15 },
  ],
  finisherMultPerLevel: 0.05,
});

// Wizard's ranged combo (J): same 4-step chain, but each step fires a magic projectile instead of a
// melee box and the caster stands still (stepPx 0). `bolt` = projectile params: speed px/s, range px,
// HD FX spriteKey + scale, collision size, and for the finisher an explosion radius + burst FX.
// Per-hit multipliers sit a bit below the warrior's because the wizard hits from safety.
export const WIZARD_COMBO_CONFIG = Object.freeze({
  chainWindowMs: 450,
  hits: [
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 0.9, knockback: 30, stepPx: 0,
      bolt: { speed: 230, range: 120, spriteKey: 'magicBolt', scale: 0.8, w: 10, h: 10 } },
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 1.0, knockback: 35, stepPx: 0,
      bolt: { speed: 230, range: 120, spriteKey: 'magicBolt', scale: 0.8, w: 10, h: 10 } },
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 1.1, knockback: 40, stepPx: 0,
      bolt: { speed: 230, range: 120, spriteKey: 'magicBolt', scale: 0.8, w: 10, h: 10 } },
    { windupMs: 130, activeMs: 80, recoverMs: 260, mult: 1.6, knockback: 90, stepPx: 0,
      bolt: { speed: 180, range: 110, spriteKey: 'magicBolt', scale: 1.3, w: 14, h: 14, explodeRadius: 26, explodeFx: 'arcaneBurst' } },
  ],
  finisherMultPerLevel: 0.05,
});

// Mechanic's gun combo (J): three quick pistol shots, then a charged hand-cannon blast that explodes.
// Faster, longer-ranged bullets than the wizard's bolts; slightly lower per-shot multipliers.
// `flashFx` = muzzle flash drawn at the gun barrel when the shot fires.
export const MECHANIC_COMBO_CONFIG = Object.freeze({
  chainWindowMs: 420,
  hits: [
    { windupMs: 50, activeMs: 50, recoverMs: 110, mult: 0.85, knockback: 25, stepPx: 0,
      bolt: { speed: 340, range: 140, spriteKey: 'bullet', scale: 1, w: 8, h: 8, flashFx: 'muzzleFlash' } },
    { windupMs: 50, activeMs: 50, recoverMs: 110, mult: 0.95, knockback: 30, stepPx: 0,
      bolt: { speed: 340, range: 140, spriteKey: 'bullet', scale: 1, w: 8, h: 8, flashFx: 'muzzleFlash' } },
    { windupMs: 50, activeMs: 50, recoverMs: 110, mult: 1.05, knockback: 35, stepPx: 0,
      bolt: { speed: 340, range: 140, spriteKey: 'bullet', scale: 1, w: 8, h: 8, flashFx: 'muzzleFlash' } },
    { windupMs: 140, activeMs: 80, recoverMs: 260, mult: 1.6, knockback: 100, stepPx: 0,
      bolt: { speed: 240, range: 130, spriteKey: 'plasmaShot', scale: 1.2, w: 12, h: 12, explodeRadius: 24, explodeFx: 'explosion', flashFx: 'muzzleFlash' } },
  ],
  finisherMultPerLevel: 0.05,
});

// Cleric's holy combo (J): three quick light orbs, then a heavier orb that bursts (4-step chain like the wizard).
// Reuses the wizard bolt art until cleric FX exist; slightly gentler multipliers (support class).
const CLERIC_BOLT = { speed: 220, range: 120, spriteKey: 'magicBolt', scale: 0.8, w: 10, h: 10, tint: HOLY_FX_TINT };
export const CLERIC_COMBO_CONFIG = Object.freeze({
  chainWindowMs: 450,
  hits: [
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 0.85, knockback: 30, stepPx: 0, bolt: CLERIC_BOLT },
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 1.0, knockback: 35, stepPx: 0, bolt: CLERIC_BOLT },
    { windupMs: 70, activeMs: 60, recoverMs: 130, mult: 1.1, knockback: 40, stepPx: 0, bolt: CLERIC_BOLT },
    { windupMs: 130, activeMs: 80, recoverMs: 250, mult: 1.6, knockback: 80, stepPx: 0,
      bolt: { speed: 180, range: 110, spriteKey: 'magicBolt', scale: 1.3, w: 14, h: 14, explodeRadius: 26, explodeFx: 'arcaneBurst', tint: HOLY_FX_TINT } },
  ],
  finisherMultPerLevel: 0.05,
});

// Assassin's dual-dagger combo (J): three quick slashes with short reach, then a double-stab finisher
// (`stabs: 2` -> two hits 90ms apart sharing the finisher multiplier). `tint` recolours the warrior slash FX purple.
const DAGGER_TINT = '#b57cff';
export const ASSASSIN_COMBO_CONFIG = Object.freeze({
  chainWindowMs: 380,
  hits: [
    { windupMs: 30, activeMs: 60, recoverMs: 70, mult: 0.75, reach: 22, width: 24, knockback: 25, stepPx: 5, fxScale: 1.1, tint: DAGGER_TINT },
    { windupMs: 30, activeMs: 60, recoverMs: 70, mult: 0.85, reach: 24, width: 26, knockback: 30, stepPx: 6, fxScale: 1.2, tint: DAGGER_TINT },
    { windupMs: 30, activeMs: 60, recoverMs: 80, mult: 0.95, reach: 26, width: 28, knockback: 35, stepPx: 6, fxScale: 1.25, tint: DAGGER_TINT },
    { windupMs: 60, activeMs: 100, recoverMs: 150, mult: 1.6, reach: 30, width: 30, knockback: 70, stepPx: 10, fxScale: 1.0, tint: DAGGER_TINT, stabs: 2 },
  ],
  finisherMultPerLevel: 0.05,
});

// Basic-attack combo per class (Player picks it by classId; unknown classes use the warrior's).
export const COMBO_CONFIG_BY_CLASS = Object.freeze({
  warrior: COMBO_CONFIG, wizard: WIZARD_COMBO_CONFIG, mechanic: MECHANIC_COMBO_CONFIG, cleric: CLERIC_COMBO_CONFIG, assassin: ASSASSIN_COMBO_CONFIG,
});

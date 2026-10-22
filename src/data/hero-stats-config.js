// Hero tuning numbers in one place. Speeds are world px per second.
export const HERO_BASE_STATS = Object.freeze({
  maxHp: 100,
  maxMp: 40,
  atk: 12,
  def: 4,
  speed: 80,
});

export const HERO_GROWTH_PER_LEVEL = Object.freeze({
  maxHp: 12,
  maxMp: 5,
  atk: 2.5,
  def: 1,
});

export const HERO_HITBOX = Object.freeze({ w: 10, h: 8 });
export const HP_REGEN_PER_SECOND = 0.6;
export const MP_REGEN_PER_SECOND = 1.5;
export const INVULNERABLE_AFTER_HIT_MS = 500;
export const DEATH_EXP_PENALTY = 0.1; // lose 10% of current-level EXP, never a level
export const SKILL_POINTS_PER_LEVEL = 1;
export const MAX_LEVEL = 30;

// Three-hit combo chain. Durations in ms; `mult` scales ATK; `reach` is hitbox depth in px;
// `stepPx` is how far the hero lunges forward during windup+active (gives the combo momentum).
export const COMBO_CONFIG = Object.freeze({
  chainWindowMs: 400,
  hits: [
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.0, reach: 18, width: 20, knockback: 40, stepPx: 6 },
    { windupMs: 50, activeMs: 90, recoverMs: 110, mult: 1.15, reach: 18, width: 20, knockback: 50, stepPx: 8 },
    { windupMs: 90, activeMs: 120, recoverMs: 220, mult: 1.8, reach: 24, width: 28, knockback: 110, stepPx: 16 },
  ],
  finisherMultPerLevel: 0.05,
});

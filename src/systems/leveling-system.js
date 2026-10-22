// EXP curve, level-ups and stat scaling. Pure functions -> unit-testable with node --test.
import {
  HERO_BASE_STATS, HERO_GROWTH_PER_LEVEL, DEATH_EXP_PENALTY, SKILL_POINTS_PER_LEVEL, MAX_LEVEL,
} from '../data/hero-stats-config.js';

/** EXP required to go from `level` to `level + 1`. */
export function expToNext(level) {
  return Math.floor(50 * Math.pow(level, 1.5));
}

/** Base stats (before skill passives) for a hero level. */
export function statsForLevel(level) {
  const n = Math.max(0, level - 1);
  return {
    maxHp: Math.round(HERO_BASE_STATS.maxHp + HERO_GROWTH_PER_LEVEL.maxHp * n),
    maxMp: Math.round(HERO_BASE_STATS.maxMp + HERO_GROWTH_PER_LEVEL.maxMp * n),
    atk: Math.round(HERO_BASE_STATS.atk + HERO_GROWTH_PER_LEVEL.atk * n),
    def: Math.round(HERO_BASE_STATS.def + HERO_GROWTH_PER_LEVEL.def * n),
    speed: HERO_BASE_STATS.speed,
  };
}

/**
 * Add EXP and resolve any level-ups (can gain several levels at once).
 * @param {{level:number, exp:number}} progress current level + exp into that level
 * @param {number} amount EXP to add
 * @returns {{level:number, exp:number, levelsGained:number, skillPointsGained:number}}
 */
export function applyExpGain(progress, amount) {
  let { level, exp } = progress;
  let levelsGained = 0;
  exp += Math.max(0, amount);
  while (level < MAX_LEVEL && exp >= expToNext(level)) {
    exp -= expToNext(level);
    level++;
    levelsGained++;
  }
  if (level >= MAX_LEVEL) exp = Math.min(exp, expToNext(level) - 1);
  return { level, exp, levelsGained, skillPointsGained: levelsGained * SKILL_POINTS_PER_LEVEL };
}

/** Death penalty: lose a fraction of EXP earned toward the next level, never a level. */
export function applyDeathPenalty(progress) {
  const lost = Math.floor(progress.exp * DEATH_EXP_PENALTY);
  return { level: progress.level, exp: progress.exp - lost, lost };
}

/** Fraction [0,1] of progress toward the next level, for the EXP bar. */
export function expProgressRatio(progress) {
  return Math.max(0, Math.min(1, progress.exp / expToNext(progress.level)));
}

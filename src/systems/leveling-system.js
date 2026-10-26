// EXP curve, level-ups and stat scaling. Pure functions -> unit-testable with node --test.
import { DEATH_EXP_PENALTY, SKILL_POINTS_PER_LEVEL, MAX_LEVEL } from '../data/hero-stats-config.js';
import { getClassDefinition, DEFAULT_CLASS_ID } from '../data/class-definitions.js';

/** EXP required to go from `level` to `level + 1`. */
export function expToNext(level) {
  return Math.floor(50 * Math.pow(level, 1.5));
}

/** Base stats (before skill passives) for a hero level of the given class. */
export function statsForLevel(level, classId = DEFAULT_CLASS_ID) {
  const { baseStats, growth } = getClassDefinition(classId);
  const n = Math.max(0, level - 1);
  return {
    maxHp: Math.round(baseStats.maxHp + growth.maxHp * n),
    maxMp: Math.round(baseStats.maxMp + growth.maxMp * n),
    atk: Math.round(baseStats.atk + growth.atk * n),
    def: Math.round(baseStats.def + growth.def * n),
    speed: baseStats.speed,
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

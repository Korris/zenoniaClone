// Temporary enhance method: 1 Upgrade Stone per attempt, success chance by target level.
// The ONLY file (besides stone loot/UI supply cells) that knows about stones; swap this object
// to change how weapons are upgraded. Contract: id, successChance, canPay, pay, costLabel.
import { MAX_ENHANCE } from '../data/weapon-enhance-config.js';

/** Chance to reach target level (index = target level; +1..+3 are guaranteed). */
export const SUCCESS_BY_TARGET = Object.freeze([
  0, 1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2,
]);

export const STONE_UPGRADE_METHOD = Object.freeze({
  id: 'upgradeStone',
  /** Chance that an attempt at `level` yields level + 1. */
  successChance(level) {
    return SUCCESS_BY_TARGET[Math.max(1, Math.min(MAX_ENHANCE, level + 1))];
  },
  canPay(player, level) {
    return level < MAX_ENHANCE && (player.materials?.upgradeStone ?? 0) >= 1;
  },
  /** Consumes the cost (stone is spent on failure too). */
  pay(player) {
    player.materials.upgradeStone -= 1;
  },
  costLabel() {
    return '1 Upgrade Stone';
  },
});

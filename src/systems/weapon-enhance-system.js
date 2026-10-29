// Applies one enhance attempt. Knows nothing about stones: cost/chance come from the injected method.
import { MAX_ENHANCE } from '../data/weapon-enhance-config.js';
import { enhanceLevel, isEnhanceable } from './weapon-enhance-stats.js';
import { STONE_UPGRADE_METHOD } from './weapon-upgrade-method-stone.js';

/**
 * Attempts level -> level + 1. Failure keeps the level but the cost is still paid.
 * @param {{method?: object, roll?: () => number}} opts roll is injectable for deterministic tests
 * @returns {{ok:boolean, success:boolean, level:number, reason:string}} ok = an attempt was made
 */
export function attemptEnhance(player, item, { method = STONE_UPGRADE_METHOD, roll = Math.random } = {}) {
  const level = enhanceLevel(item);
  const fail = (reason) => ({ ok: false, success: false, level, reason });
  if (!isEnhanceable(item)) return fail('not-weapon');
  const equipped = player.equipment?.[item.slot] === item;
  if (!equipped && !player.inventory.includes(item)) return fail('not-owned');
  if (level >= MAX_ENHANCE) return fail('max-level');
  if (!method.canPay(player, level)) return fail('no-cost');
  method.pay(player, level);
  const success = roll() < method.successChance(level);
  if (success) {
    item.enhance = level + 1;
    if (equipped) player.recomputeStats();
  }
  return { ok: true, success, level: success ? level + 1 : level, reason: '' };
}

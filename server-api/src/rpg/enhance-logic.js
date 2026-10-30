import { ENHANCEABLE_SLOTS, MAX_ENHANCE, OFFHAND_SCALE, STONE_COST, SUCCESS_BY_TARGET } from './rpg-constants.js';

// Port of Assets/Scripts/Rpg/Enhance.cs (EnhanceConfig / EnhanceStats / StoneUpgradeMethod / EnhanceSystem). Same numbers, same order of checks.

const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v);
const round = (v) => Math.floor(v + 0.5); // RpgMath.Round

/** Cumulative fraction of the base stat per level (index = level): +1..6 10%, +7..9 20%, +10..12 35%, +13..15 60% per level. */
export const ATK_PCT = (() => {
  const t = new Array(MAX_ENHANCE + 1).fill(0);
  let total = 0;
  for (let level = 1; level <= MAX_ENHANCE; level++) {
    total += level <= 6 ? 0.1 : level <= 9 ? 0.2 : level <= 12 ? 0.35 : 0.6;
    t[level] = Math.floor(total * 1000 + 0.5) / 1000;
  }
  return t;
})();

export const isEnhanceable = (item) => !!item && ENHANCEABLE_SLOTS.includes(item.slot);
export const enhanceLevel = (item) => (isEnhanceable(item) ? clamp(item.enhance ?? 0, 0, MAX_ENHANCE) : 0);

/** Chance that the attempt from `level` to level+1 succeeds. */
export const successChance = (level) => SUCCESS_BY_TARGET[clamp(level + 1, 1, MAX_ENHANCE)];

/** Bonus stat for `level`: % of base, at least +1 per level (scaled down for bases under 4, and by pctScale for offhands). */
export function enhanceBonus(baseValue, level, pctScale = 1) {
  const lvl = clamp(level, 0, MAX_ENHANCE);
  if (lvl === 0) return 0;
  const floor = Math.ceil(lvl * Math.min(1, baseValue / 4) * pctScale);
  return Math.max(floor, round(baseValue * ATK_PCT[lvl] * pctScale));
}
export const offhandScale = (item) => (item.slot === 'offhand' ? OFFHAND_SCALE : 1);

/**
 * One enhance attempt on a plain SaveData-shaped state (NOT mutated; a modified clone is returned in `state`).
 * `rand` is an injected () => [0,1) so tests are deterministic. ok = an attempt was made (stone spent).
 * reason: not-owned | not-weapon | max-level | no-cost (same strings as C#).
 */
export function attemptEnhance(state, itemUid, rand) {
  const next = structuredClone(state);
  const equipped = next.equipment.find((i) => i.uid === itemUid);
  const item = equipped ?? next.inventory.find((i) => i.uid === itemUid);
  if (!item) return { ok: false, success: false, level: 0, reason: 'not-owned', state };
  const level = enhanceLevel(item);
  const fail = (reason) => ({ ok: false, success: false, level, reason, state });
  if (!isEnhanceable(item)) return fail('not-weapon');
  if (level >= MAX_ENHANCE) return fail('max-level');
  if (next.upgradeStone < STONE_COST) return fail('no-cost');
  next.upgradeStone -= STONE_COST;
  const success = rand() < successChance(level);
  if (success) item.enhance = level + 1;
  return { ok: true, success, level: success ? level + 1 : level, reason: '', state: next, slot: item.slot, equipped: !!equipped };
}

/**
 * Anti-tamper for snapshots sent by the zone server: only this API raises enhance levels, so a saved item may never carry a
 * higher level than the stored one, and an item the API has never seen must be +0. Returns the offending uid or null.
 */
export function findEnhanceTamper(oldState, newState) {
  const known = new Map([...oldState.inventory, ...oldState.equipment].map((i) => [i.uid, i.enhance ?? 0]));
  for (const i of [...newState.inventory, ...newState.equipment]) {
    if ((i.enhance ?? 0) > (known.get(i.uid) ?? 0)) return i.uid;
  }
  return null;
}

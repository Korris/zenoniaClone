// Timed debuffs on monsters (Assassin): slow, blind, poison (damage over time) and Death Mark.
// State only -- lives in `monster.status` (created lazily): { slow:{factor,timer}, blind:{timer},
// poison:{dmg,timer,tick}, mark:{timer,base,stored,storePct} }. Ticking/damage is in monster-status-tick.js
// (kept apart so combat-system.js can import recordMarkDamage without an import cycle).
export const POISON_TICK_MS = 1000;

export const statusOf = (monster) => (monster.status ||= {});

/** Slow: movement speed x `factor` (bosses only feel half of it). A stronger slow replaces a weaker one. */
export function applySlow(monster, factor, durationMs) {
  const f = monster.isBoss ? 1 - (1 - factor) / 2 : factor;
  const s = statusOf(monster);
  s.slow = { factor: s.slow && s.slow.timer > 0 ? Math.min(s.slow.factor, f) : f, timer: Math.max(durationMs, s.slow?.timer || 0) };
}

/** Blind: the monster's attacks whiff (its windup/cooldown still run) until the timer ends. */
export function applyBlind(monster, durationMs) {
  const s = statusOf(monster);
  s.blind = { timer: Math.max(durationMs, s.blind?.timer || 0) };
}

/** Poison: `dmg` true damage every POISON_TICK_MS for `durationMs` (re-applying refreshes, never resets the tick). */
export function applyPoison(monster, dmg, durationMs) {
  const s = statusOf(monster);
  const live = s.poison && s.poison.timer > 0 ? s.poison : null;
  s.poison = { dmg: Math.max(dmg, live?.dmg || 0), timer: Math.max(durationMs, live?.timer || 0), tick: live ? live.tick : POISON_TICK_MS };
}

/** Death Mark: after `durationMs` it detonates for `base` + `storePct` of all damage taken meanwhile. */
export function applyMark(monster, { durationMs, base, storePct }) {
  statusOf(monster).mark = { timer: durationMs, base, storePct, stored: 0 };
}

/** Called for every point of damage a monster takes; a marked monster banks a share of it. */
export function recordMarkDamage(monster, damage) {
  const mark = monster.status?.mark;
  if (mark) mark.stored += damage * mark.storePct;
}

/** Speed multiplier from slow (1 = normal). */
export function slowFactor(monster) {
  const slow = monster.status?.slow;
  return slow && slow.timer > 0 ? slow.factor : 1;
}

export function isBlinded(monster) {
  return (monster.status?.blind?.timer || 0) > 0;
}

/** Poison damage per tick from the hero's attack: pct of ATK, boosted by Toxic Mastery (poisonMult). */
export function poisonDamage(attack, pct, player) {
  return Math.max(1, Math.round(attack * pct * (1 + (player.passiveMods?.poisonMult || 0))));
}

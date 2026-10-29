// Timed hero statuses: Cleric's Sacred Shield (damage absorb), Haste (move + attack speed, also the
// Assassin's Adrenaline) and Resurrection Grace (survive a lethal hit, then recharge); Assassin's
// Stealth (extra dodge) and Poison Blade (next hits poison). State lives on the player:
//   player.shield = { amount, timer } | null     player.haste = { mult, timer } | null
//   player.stealth = { evasion, timer } | null   player.poisonBlade = { pct, charges, timer, poisonMs } | null
//   player.graceCooldown (ms)                    Resurrection Grace is a passive (surviveHpPct, graceCooldownMs)
// Pure helpers -> unit-testable; Player.takeDamage / tickBuffs and player-controller call them.
export const GRACE_INVULN_MS = 1500;

/** Grants an absorb shield worth `amount` HP for `durationMs` (replaces any current shield). */
export function applyShield(player, amount, durationMs) {
  player.shield = { amount: Math.max(0, Math.round(amount)), timer: durationMs };
}

/** Soaks up to the shield's remaining amount; returns how much damage was absorbed. */
export function absorbWithShield(player, damage) {
  const shield = player.shield;
  if (!shield || shield.amount <= 0 || damage <= 0) return 0;
  const absorbed = Math.min(shield.amount, damage);
  shield.amount -= absorbed;
  if (shield.amount <= 0) player.shield = null;
  return absorbed;
}

/** Haste: multiplies movement speed and combo speed while active (a new haste replaces the old one). */
export function applyHaste(player, mult, durationMs) {
  player.haste = { mult, timer: durationMs };
}

/** Current speed factor (1 = normal). */
export function hasteFactor(player) {
  return player.haste && player.haste.timer > 0 ? player.haste.mult : 1;
}

/** Combo-update speed factor: Haste/Adrenaline times the Quick Reflexes passive (attackSpeed). */
export function attackSpeedFactor(player) {
  return hasteFactor(player) * (1 + (player.passiveMods?.attackSpeed || 0));
}

/** Stealth (Smoke Bomb): extra dodge chance for a while. */
export function applyStealth(player, evasion, durationMs) {
  player.stealth = { evasion, timer: durationMs };
}

/** Total chance to dodge an ordinary hit: Evasion passive + stealth (read by hit-chance.js as `evasion`). */
export function evasionOf(player) {
  return (player.passiveMods?.evasion || 0) + (player.stealth?.evasion || 0);
}

/** Poison Blade: the next `charges` basic hits within `durationMs` poison the foe for `poisonMs`. */
export function applyPoisonBlade(player, pct, charges, durationMs, poisonMs) {
  player.poisonBlade = { pct, charges, timer: durationMs, poisonMs };
}

/** Counts shield/haste/stealth/poison-blade/grace timers down; expired statuses are cleared. */
export function tickStatuses(player, dt) {
  if (player.shield) {
    player.shield.timer -= dt;
    if (player.shield.timer <= 0) player.shield = null;
  }
  if (player.haste) {
    player.haste.timer -= dt;
    if (player.haste.timer <= 0) player.haste = null;
  }
  for (const key of ['stealth', 'poisonBlade']) {
    if (!player[key]) continue;
    player[key].timer -= dt;
    if (player[key].timer <= 0) player[key] = null;
  }
  if (player.graceCooldown > 0) player.graceCooldown = Math.max(0, player.graceCooldown - dt);
}

/**
 * Called when a hit would kill the hero. With Resurrection Grace learned and off cooldown the hero
 * stays alive at surviveHpPct% max HP (at least 1) with brief invulnerability. Returns true if saved.
 */
export function tryResurrectionGrace(player) {
  const pct = player.passiveMods?.surviveHpPct || 0;
  if (pct <= 0 || player.graceCooldown > 0) return false;
  player.hp = Math.max(1, Math.round((player.maxHp * pct) / 100));
  player.graceCooldown = player.passiveMods.graceCooldownMs || 0;
  player.invulnTimer = GRACE_INVULN_MS;
  player.graceFlash = true; // read once by combat-system.hitPlayer to show feedback
  return true;
}

/** Clears every transient status (used on revive). */
export function clearStatuses(player) {
  player.shield = null;
  player.haste = null;
  player.stealth = null;
  player.poisonBlade = null;
  player.graceCooldown = 0;
}

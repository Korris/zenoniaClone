// Both sides can miss. Accuracy/evasion are fractions; defaults give a 5% miss chance.
export const BASE_HIT_CHANCE = 0.95;
export function rollHit(attacker, defender, rand = Math.random) {
  const accuracy = attacker?.accuracy ?? attacker?.def?.accuracy ?? BASE_HIT_CHANCE;
  const evasion = defender?.evasion ?? defender?.def?.evasion ?? 0;
  return rand() < Math.max(0.05, Math.min(0.99, accuracy - evasion));
}

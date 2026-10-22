// Shared damage math for hero and monsters. Pure -> unit-testable.

/**
 * damage = max(1, round(atk * mult * variance(0.9..1.1) - def * 0.5))
 * @param {number} atk attacker's attack
 * @param {number} def defender's defense
 * @param {number} mult skill/combo multiplier
 * @param {() => number} rand injectable RNG for deterministic tests
 */
export function computeDamage(atk, def, mult = 1, rand = Math.random) {
  const variance = 0.9 + rand() * 0.2;
  return Math.max(1, Math.round(atk * mult * variance - def * 0.5));
}

/** Axis-aligned box overlap test. Boxes are {x, y, w, h} with top-left origin. */
export function boxesOverlap(a, b) {
  return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
}

/** Circle (cx, cy, r) vs box overlap, used by Whirlwind. */
export function circleOverlapsBox(cx, cy, r, box) {
  const nearestX = Math.max(box.x, Math.min(cx, box.x + box.w));
  const nearestY = Math.max(box.y, Math.min(cy, box.y + box.h));
  const dx = cx - nearestX;
  const dy = cy - nearestY;
  return dx * dx + dy * dy <= r * r;
}

/** Collision box for an entity positioned by center-x / feet-y. */
export function entityBox(entity, hitbox) {
  return { x: entity.x - hitbox.w / 2, y: entity.y - hitbox.h, w: hitbox.w, h: hitbox.h };
}

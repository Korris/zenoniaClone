// Offhand companion (cleric spirit, mechanic robot): pure follow / bob / visibility rules (render-only entity, no gameplay effect).
// The companion hovers beside the hero, on the side the hero's back is turned to, and eases toward that spot.
// Offsets are in world units (a hero frame is 24 units); y is the companion's GROUND point, the sprite floats COMPANION_HOVER above it.
export const COMPANION_OFFSETS = Object.freeze({
  down: { x: 11, y: -3 }, // behind the hero's screen-right shoulder (drawn first)
  up: { x: -11, y: 3 }, // hero faces away: companion drifts in front on the left
  left: { x: 10, y: 0 },
  right: { x: -10, y: 0 },
});
export const COMPANION_HOVER = 11;
export const COMPANION_FOLLOW_MS = 160; // easing time constant
/** Classes whose offhand item shows as a floating companion. */
export const COMPANION_CLASSES = Object.freeze(['cleric', 'mechanic']);
const SNAP_DISTANCE = 90; // after a teleport / map change the companion snaps instead of flying across the map
const MAX_STEP_MS = 100; // a long frame (tab switch) must not overshoot
const BOB_PERIOD_MS = 1500;

/** Where the companion wants to be (ground point) for the hero's current facing. */
export function companionTarget(player) {
  const o = COMPANION_OFFSETS[player.dir] || COMPANION_OFFSETS.down;
  return { x: player.x + o.x, y: player.y + o.y };
}

export function createCompanionState(player) {
  const t = companionTarget(player);
  return { x: t.x, y: t.y, ageMs: 0 };
}

/** Advance the follow by dtMs (frame-rate independent exponential ease). Mutates and returns `state`. */
export function stepCompanion(state, player, dtMs) {
  const t = companionTarget(player);
  const dt = Math.max(0, Math.min(MAX_STEP_MS, dtMs));
  state.ageMs += dt;
  if (Math.hypot(t.x - state.x, t.y - state.y) > SNAP_DISTANCE) {
    state.x = t.x;
    state.y = t.y;
    return state;
  }
  const k = 1 - Math.exp(-dt / COMPANION_FOLLOW_MS);
  state.x += (t.x - state.x) * k;
  state.y += (t.y - state.y) * k;
  return state;
}

/** Vertical bob in world units (negative = up) added to the hover height; amplitude is smaller for the animated float frames. */
export function companionBob(ageMs, amplitude = 1.3) {
  return Math.sin((ageMs / BOB_PERIOD_MS) * Math.PI * 2) * amplitude;
}

/** Shown for a living companion class with an item in the offhand slot. */
export function companionVisible(player) {
  return COMPANION_CLASSES.includes(player.classId) && Boolean(player.equipment?.offhand) && !player.dead;
}

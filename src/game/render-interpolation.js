// Render interpolation for the fixed-step loop: logic runs at 60Hz, but the browser may present
// frames late (screen recording, busy GPU) or more often (120/144Hz monitors). Drawing the raw
// latest state then makes motion jump by 0 or 2 steps per frame. Instead, every logic step
// remembers where each moving thing was, and the renderer draws the blend
// prev + (current - prev) * alpha, where alpha = leftover accumulator / step (0..1).
// Positions are swapped in only for the duration of the draw call and always restored.

// A move larger than this in one 16ms step is a teleport (map change, respawn), not motion.
const TELEPORT_DISTANCE = 32;
const LIST_KEYS = ['npcs', 'monsters', 'projectiles', 'drops', 'effects', 'particles', 'texts'];

function* movingThings(world) {
  if (world.player) yield world.player;
  for (const key of LIST_KEYS) {
    const list = world[key];
    if (Array.isArray(list)) yield* list;
  }
}

function remember(thing) {
  if (typeof thing.x !== 'number' || typeof thing.y !== 'number') return;
  thing.interpPrevX = thing.x;
  thing.interpPrevY = thing.y;
}

/** Call once BEFORE every fixed logic step. */
export function captureInterpolationSnapshot(world) {
  if (!world) return;
  for (const thing of movingThings(world)) remember(thing);
  if (world.camera) remember(world.camera);
}

function blend(thing, alpha, saved) {
  if (thing.interpPrevX === undefined || typeof thing.x !== 'number') return;
  const dx = thing.x - thing.interpPrevX;
  const dy = thing.y - thing.interpPrevY;
  if ((dx === 0 && dy === 0) || Math.abs(dx) > TELEPORT_DISTANCE || Math.abs(dy) > TELEPORT_DISTANCE) return;
  saved.push(thing, thing.x, thing.y);
  thing.x = thing.interpPrevX + dx * alpha;
  thing.y = thing.interpPrevY + dy * alpha;
}

/**
 * Draw `drawFn` with every moving thing (and the camera) placed at its interpolated position.
 * @param {object|null} world
 * @param {number} alpha 0..1 progress into the next logic step
 * @param {() => void} drawFn
 */
export function renderInterpolated(world, alpha, drawFn) {
  if (!world || !(alpha > 0 && alpha < 1)) { drawFn(); return; }
  const saved = [];
  for (const thing of movingThings(world)) blend(thing, alpha, saved);
  if (world.camera) blend(world.camera, alpha, saved);
  try {
    drawFn();
  } finally {
    for (let i = 0; i < saved.length; i += 3) {
      saved[i].x = saved[i + 1];
      saved[i].y = saved[i + 2];
    }
  }
}

// Draw order for the PixiJS actor layer. The Canvas2D renderer y-sorts a plain array (stable sort, kinds pushed
// props -> npcs -> monsters -> hero -> companion); zIndex = y plus a tiny per-kind rank reproduces exactly that
// ordering, including ties, without re-sorting by hand.

export const DEPTH_RANK = Object.freeze({ prop: 0, npc: 1, monster: 2, player: 3, companion: 4, clone: 3 });
const RANK_STEP = 0.001;

/** zIndex for an actor standing at ground y. Higher draws in front. */
export function depthKey(kind, y) {
  return y + (DEPTH_RANK[kind] ?? 0) * RANK_STEP;
}

/** World units -> device-pixel-aligned world units (what the 2D renderer does with Math.round on the transform). */
export function snapToDevice(value, scale) {
  return Math.round(value * scale) / scale;
}

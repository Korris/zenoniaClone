// Picks which frame of a ground-tile variant set to draw at tile (tx, ty).
// Frames with a `macro: [cols, rows]` tag (set by tile-sprite-builder.js from HD_TILES) are the
// quadrants of ONE seamless texture, so the pick is by position -- neighbouring tiles continue
// each other's texture and the repeat period is cols x rows tiles instead of a 1-tile lattice.
// Untagged sets (pixel-art pack) keep the old pseudo-random scatter.
export function pickVariant(frames, tx, ty) {
  const macro = frames.macro;
  if (macro) return frames[(ty % macro[1]) * macro[0] + (tx % macro[0])];
  return frames[(tx * 7 + ty * 13) % frames.length];
}

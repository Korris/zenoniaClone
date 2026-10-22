// Single shared palette: each character in a pixel-map string maps to a CSS color.
// '.' is transparent. Keep this the only place colors are defined (DRY).
export const PALETTE = Object.freeze({
  '.': null,
  // outlines / neutrals
  K: '#1b1b2a', W: '#ffffff', V: '#bfe9ff', Y: '#ffe680',
  // hero
  S: '#f6c9a2', s: '#d9a070', H: '#5a3a22', h: '#8a5a34', E: '#22223a',
  R: '#d63a3a', r: '#9b2222', A: '#3b6fd6', a: '#2a4ea0', L: '#7fb0ff',
  G: '#f2c14e', M: '#c9d3e0', m: '#7f8ca0', P: '#4a3b6b', p: '#2e2545',
  B: '#6b4b2a', b: '#4a3218',
  // monsters
  '1': '#5fd35f', '2': '#2f9a2f', '3': '#b9ffb9',
  '4': '#6a4ca3', '5': '#45307a',
  '6': '#8fbf5a', '7': '#5a8530',
  '8': '#e8e8e8', '9': '#a8a8a8',
  F: '#ff7a2a', f: '#ffd23a',
  O: '#7a7a8a', o: '#50505e', X: '#c43a3a', x: '#7a1f1f',
  // tiles
  g: '#4f9a3a', q: '#5fae44', d: '#a57a4a', c: '#8a6238',
  w: '#3a6fd0', v: '#5a8ff0', n: '#2b2b38', N: '#383848',
  t: '#2e6b2a', u: '#3f8c36', T: '#5a3a22',
  y: '#e8d28a', z: '#d4bb6e', i: '#6e6e7e', j: '#5c5c6c',
  k: '#3c3c4c', l: '#8c8c9c', e: '#b43aff', Q: '#e0a0ff',
});

// Global constants shared across the game. World units are "world pixels";
// the canvas is scaled by PIXEL_SCALE so 1 world px = 4 screen px (16px chibi sprites read well).
export const CANVAS_WIDTH = 960;
export const CANVAS_HEIGHT = 640;
export const PIXEL_SCALE = 4;
export const VIEW_WIDTH = CANVAS_WIDTH / PIXEL_SCALE;   // 240 world px
export const VIEW_HEIGHT = CANVAS_HEIGHT / PIXEL_SCALE; // 160 world px
export const TILE_SIZE = 16;

// Game state identifiers used by the state machine in game-state.js
export const GAME_STATES = Object.freeze({
  TITLE: 'title',
  PLAYING: 'playing',
  PAUSED: 'paused',
  SKILLS: 'skills',
  INVENTORY: 'inventory',
  DIALOG: 'dialog',
  QUEST_LOG: 'questLog',
  DEAD: 'dead',
  WIN: 'win',
});

// Facing directions (also used to pick sprite rows)
export const DIRECTIONS = Object.freeze({
  DOWN: 'down',
  UP: 'up',
  LEFT: 'left',
  RIGHT: 'right',
});

// Unit vectors per direction, handy for attacks/dashes/projectiles
export const DIRECTION_VECTORS = Object.freeze({
  down: { x: 0, y: 1 },
  up: { x: 0, y: -1 },
  left: { x: -1, y: 0 },
  right: { x: 1, y: 0 },
});

export const SAVE_KEY = 'zenonia-save-v1';

// Development overlays (the floating "Hero" label over the player). Off for normal play; flip to
// true -- or set `window.__zenoniaDebug = true` in devtools -- to get them back while debugging.
export const SHOW_DEBUG_LABELS = false;

// Visual effect pixel maps: big shaded sword, slash arcs, pickups, hero portrait, quest markers,
// skill icons. Each map carries its own palette.
export const EFFECT_PALETTE = Object.freeze({
  '.': null, o: '#2a2030', w: '#ffffff', c: '#cfe3ff', C: '#8fb4e0', g: '#eab62f', G: '#b07f14',
  k: '#6a4324', V: '#bfe9ff', Y: '#ffe680', Q: '#e0a0ff', e: '#b43aff', R: '#d63a3a', r: '#9b2222',
  A: '#3b6fd6', s: '#f9d6b4', S: '#e4ad85', h: '#f5cf47', H: '#d49a22', L: '#fff3b0', E: '#2e4fa8',
  m: '#c4625c', b: '#3d74e0', n: '#1b1b2a', y: '#ffd23a', p: '#9b59ff',
});

// Big two-handed blade pointing UP (8x21); renderer rotates per direction.
export const SWORD_ROWS = [
  '...oo...', '..owwo..', '.owwwco.', '.owwwco.', '.owwwco.', '.owwwco.', '.owwwco.', '.owwwco.',
  '.owwwco.', '.owwwco.', '.owwwco.', '.owwCco.', '.oCCCCo.', 'oggggggo', 'oGggggGo', '...okko.',
  '...okko.', '...okko.', '...oggo.', '....oo..', '........',
];

// Slash arc facing RIGHT (16x16); renderer rotates for other directions.
export const SLASH_ARC_ROWS = [
  '..........oo....', '..........oVo...', '...........oVo..', '...........oVwo.', '............oVo.',
  '............oVwo', '............owVo', '............oVwo', '............owVo', '............oVwo',
  '............oVo.', '...........oVwo.', '...........oVo..', '..........oVo...', '..........oo....',
  '................',
];

export const POTION_ROWS = ['..oo..', '..oo..', '.owwo.', 'oRRRRo', 'oRRRRo', 'oRRRRo', '.oRRo.', '..oo..'];
export const COIN_ROWS = ['.oooo.', 'ogyygo', 'ogyggo', 'oggygo', 'oggggo', '.oooo.'];
export const SPARK_ROWS = ['.Y.', 'YwY', '.Y.'];

// 24x24 hero face for the HUD portrait.
export const PORTRAIT_ROWS = [
  '.......ooo..oo..........', '.....oohhhoohhoo........', '....ohhhhhhhhhhho.......', '...ohLhhhhhhhhhhho......',
  '..ohhLhhhhhhhhhhhho.....', '..ohhhhhhhhhhhhhhHo.....', '..ohhhhhhhhhhhhhHHo.....', '..ohHhhssssssssshHo.....',
  '..oHhssssssssssssHo.....', '..oHssssEwssssEwsSo.....', '..oHssssEEssssEEsSo.....', '...osssssssssssssSo.....',
  '...oSssssssmmsssSSo.....', '....oSsssssssssSSo......', '.....ooSSSSSSSSoo.......', '.......ooccccoo.........',
  '......obbbccbbbbo.......', '.....obbbbbbbbbbbo......', '.....obbbbggbbbbbo......', '....obbbbbbbbbbbbbo.....',
  '....obbbbbbbbbbbbbo.....', '....obbbbbbbbbbbbbo.....', '....obbbbbbbbbbbbbo.....', '....oooooooooooooo......',
];

// Quest markers drawn above NPCs: '!' = quest available, '?' = ready to turn in.
export const MARKER_ROWS = {
  available: ['.oo.', 'oyyo', 'oyyo', 'oyyo', 'oyyo', '.oo.', '....', '.oo.', 'oyyo', '.oo.'],
  turnIn: ['.oooo.', 'oyyyyo', 'oyooyo', '.oooyo', '..oyo.', '..oyo.', '..oo..', '..oo..', '..oyo.', '..oo..'],
  talk: ['oooooo', 'owwwwo', 'owwwwo', 'owwwwo', 'oooooo', '.oo...', 'oo....', '......', '......', '......'],
};

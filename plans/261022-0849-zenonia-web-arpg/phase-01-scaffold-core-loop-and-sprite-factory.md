# Phase 01 - Scaffold, Core Loop, Input, Camera, Sprite Factory

## Context
- Plan: [plan.md](plan.md). Next: [phase-02](phase-02-tilemap-maps-portals-collision.md)

## Overview
Priority P1 | Status pending | 4h
Bootable page: canvas 960x640 scaled to window, fixed-timestep loop, keyboard input, camera, pixel sprite factory + palette.

## Requirements
- `index.html` loads `src/main.js` as `type="module"`; single `<canvas id="game" width=960 height=640>`.
- CSS: canvas centered, letterboxed, `image-rendering: pixelated`, scale preserves 3:2 aspect.
- `ctx.imageSmoothingEnabled = false` after every resize.
- Constants: `TILE=16`, `SCALE=3`, `TILE_PX=48`, `VIEW_W=960`, `VIEW_H=640`.

## Related Files (create)
- `index.html`, `styles.css`, `README.md` (run instructions)
- `package.json`: `{"name":"zenonia","type":"module","scripts":{"start":"npx serve .","test":"node --test tests/"}}`
- `src/main.js`, `src/config/game-constants.js`
- `src/core/game-loop.js`, `input-manager.js`, `camera.js`, `game-state.js`, `scene-manager.js`
- `src/graphics/palette.js`, `pixel-sprite-factory.js`, `sprite-animation.js`

## Key Signatures
```js
// game-loop.js - fixed 1/60 step, accumulator, clamp frame dt to 0.25s
export function startGameLoop({ update(dt), render() }) -> { stop() }

// input-manager.js - keydown/keyup on window, preventDefault for game keys
export function createInputManager(target = window) -> {
  isDown(action): bool,       // held
  wasPressed(action): bool,   // edge this frame
  endFrame(): void            // clear pressed set; call at end of each update step
}
// actions: up,down,left,right,attack,skill1,skill2,skill3,skillPanel,pause,confirm
// keymap: WASD/arrows, J/Space=attack, K/L/I=skill1..3, Tab=skillPanel, Esc=pause, Enter=confirm

// camera.js (pure)
export function createCamera(viewW, viewH) -> { x, y, follow(targetX, targetY, worldW, worldH) }
// world coords = screen px (tile*48). Clamp x in [0, worldW-viewW]; if world smaller, center.

// game-state.js - single mutable state object
export function createGameState() -> {
  scene: 'title'|'playing'|'paused'|'skillPanel'|'dead'|'win',
  player: null, monsters: [], projectiles: [], drops: [], damageNumbers: [], effects: [],
  map: null, camera, time: 0, banner: null, killedBoss: false, shake: 0, hitStop: 0
}

// palette.js
export const PALETTE = { '.': null, 'k': '#1a1020', 'w': '#ffffff', 's': '#f5c99b', ... }
// ~24 single-char keys: outline, skin, skin-shade, hair x2, armor x3, eyes, greens x3,
// browns x3, blues x2, greys x3, reds x2, yellow, purple, orange

// pixel-sprite-factory.js
export function buildSprite(rows: string[], palette = PALETTE, scale = SCALE) -> HTMLCanvasElement
export function buildFlippedSprite(canvas) -> HTMLCanvasElement   // mirror: left from right
export function buildFrames(framesRows: string[][]) -> HTMLCanvasElement[]
// cache results by key in a Map; built once at boot

// sprite-animation.js (pure)
export function createAnimation(frameCount, fps, loop = true) -> { frame, t, done, update(dt), reset() }
```

## Implementation Steps
1. index.html/styles.css/package.json/README. Resize: scale = min(innerW/960, innerH/640), set CSS size.
2. game-constants.js with tunables (TILE, SCALE, COMBO_WINDOW=0.4, INVULN_SEC=0.6, etc).
3. game-loop.js with accumulator; call input.endFrame() after each update step.
4. input-manager.js with key->action map; ignore `e.repeat` for pressed set.
5. camera.js pure.
6. palette + sprite factory; verify by drawing a test 16x16 sprite.
7. scene-manager.js: `updateScene(state, input, dt)` / `renderScene(ctx, state)` switch on `state.scene` (stubs).
8. main.js: get ctx, create state + input, start loop; title scene "Press Enter".

## Todo
- [ ] index.html, styles.css, package.json, README.md
- [ ] game-constants.js
- [ ] game-loop.js, input-manager.js, camera.js, game-state.js, scene-manager.js
- [ ] palette.js, pixel-sprite-factory.js, sprite-animation.js
- [ ] main.js boots title screen

## Success Criteria
- `npx serve .` -> crisp scaled test sprite, no console errors, correct resize.
- Input edges/holds work (temporary debug log).

## Risks
- ES modules over file:// fail -> README states http-only.
- Tab steals focus -> preventDefault on Tab.

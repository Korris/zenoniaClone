# Phase 02 - Tilemap, 4 Maps, Map Manager, Portals, Collision

## Context
- Prev: [phase-01](phase-01-scaffold-core-loop-and-sprite-factory.md). Next: [phase-03](phase-03-player-combo-hit-detection.md)

## Overview
Priority P1 | Status pending | 5h
Code-drawn tiles, 4 ASCII maps, map load/transition via portals with level gate, AABB-vs-tile collision.

## Requirements
- Tile chars: `.` grass, `,` dirt, `~` water(solid), `T` tree(solid), `R` rock(solid), `:` sand, `_` stone floor, `#` wall(solid), `P` portal (2-frame anim, walkable).
- Maps 40x30 up to 60x40 tiles. Tile draw culled to camera viewport.
- Portal: player hitbox overlaps portal rect -> if `player.level >= minLevel` load target map, else banner "Requires Lv X" (throttle 1.5s).

## Data Shapes
```js
// world/maps/verdant-meadow-map.js
export default {
  id: 'verdant-meadow', name: 'Verdant Meadow',
  tiles: ['########', '#..T...#', ...],      // equal-length strings
  playerStart: { tx: 3, ty: 3 },
  portals: [{ tx: 38, ty: 15, w: 1, h: 2, toMap: 'dark-cave', toTx: 2, toTy: 10, minLevel: 3 }],
  spawnZones: [{ tx: 10, ty: 5, w: 10, h: 8, monsterIds: ['slime', 'bat'], levelRange: [1, 3], max: 6, respawnSec: 8 }],
  boss: null   // ruined-castle: { tx, ty, monsterId: 'stone-golem' }
}
// graphics/sprites/tile-sprite-data.js
export const TILE_SPRITES = { grass: [/*16 strings*/], dirt, water, tree, rock, sand, stone, wall, portal: [[f0], [f1]] }
// world/tile-char-info.js (pure, shared by tilemap + renderer)
export const TILE_CHAR_INFO = { '.': { sprite: 'grass', solid: false }, '~': { sprite: 'water', solid: true }, ... }
```
Gates: meadow->cave Lv3, cave->desert Lv7, desert->castle Lv12. Each map has back-portal (minLevel 1).
Monster level bands: meadow 1-4, cave 4-8, desert 8-12, castle 12-16 + boss.

## Key Signatures
```js
// world/tilemap.js (pure) - out-of-bounds counts as solid
export function createTilemap(tileRows, charInfo = TILE_CHAR_INFO) -> { width, height, getChar(tx, ty), isSolid(tx, ty) }

// systems/collision-system.js (pure). entity hitbox {x, y, w, h} in world px (top-left)
export function moveWithCollision(entity, dx, dy, tilemap, tilePx = 48) -> { hitX: bool, hitY: bool }
// resolve X then Y; check tiles overlapped by hitbox; snap to tile edge on hit
export function rectsOverlap(a, b) -> bool

// world/portal.js (pure)
export function findTouchedPortal(hitbox, portals, tilePx = 48) -> portal | null

// world/map-manager.js
export const MAP_REGISTRY = { 'verdant-meadow', 'dark-cave', 'scorched-desert', 'ruined-castle' } // -> defs
export function loadMap(state, mapId, spawnTx?, spawnTy?) -> void
// builds tilemap, sets state.map = { def, tilemap, worldW, worldH, spawnerState },
// clears monsters/projectiles/drops, places player, init spawner, set banner = map name

// graphics/tilemap-renderer.js
export function drawTilemap(ctx, map, camera, time) -> void   // culled to view
```

## Related Files (create)
- `src/graphics/sprites/tile-sprite-data.js`, `src/graphics/tilemap-renderer.js`
- `src/world/tile-char-info.js`, `tilemap.js`, `map-manager.js`, `portal.js`
- `src/world/maps/verdant-meadow-map.js`, `dark-cave-map.js`, `scorched-desert-map.js`, `ruined-castle-map.js`
- `src/systems/collision-system.js`

## Implementation Steps
1. Draw 9 tile pixel maps (16x16). Grass variant via `(tx*7 + ty*13) % 3` tint optional.
2. tilemap + collision (pure). Chibi hitbox: 10x8 px at feet (30x24 world px).
3. Author 4 ASCII maps: meadow open fields + pond; cave walls/stone corridors; desert sand + rocks; castle walls + stone floor + boss hall.
4. map-manager + registry; portal check inside playing update.
5. Debug: move a rectangle with WASD, camera follows, walk through all portals.

## Todo
- [ ] tile sprites + tile-char-info
- [ ] tilemap.js, collision-system.js, portal.js
- [ ] 4 map files (<=40 rows each, fits <200 lines)
- [ ] map-manager.js, tilemap-renderer.js
- [ ] debug walk through all maps

## Success Criteria
- Walk all 4 maps, blocked by solids, no corner clipping, gate message when underleveled.

## Risks
- Tunneling on dash -> substep moves so each step < hitbox size.
- Portal re-trigger on arrival -> place target tile next to (not on) return portal.

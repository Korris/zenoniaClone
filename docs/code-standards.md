# Code Standards & Conventions

## File & Module Organization

- **ES modules** (`import`/`export`) — bundler-free, runs in browser via HTTP.
- **kebab-case filenames** (e.g., `combo-attack-system.js`) — self-documenting for Grep/Glob tools.
- **<200 lines per file** — encourages single responsibility, eases code review.
- **One class/export per file** (unless tightly paired utilities).

## Code Style

- **No external dependencies** — vanilla JS + HTML5 Canvas only.
- **const/let** — no var; freeze immutable config with `Object.freeze()`.
- **Pure functions where possible** — especially in `src/systems/` and `src/data/`.
- **JSDoc comments** for complex functions (parameters, return type, side effects).
- **Descriptive variable names** over brevity; avoid single-letter vars outside loops.

## Architecture Principles

| Layer | What Goes Here | Rules |
|-------|---|---|
| **core/** | Framework-like utilities | No game state; no DOM. |
| **graphics/** | Rendering only | Read game state, draw to ctx, don't mutate state. |
| **systems/** | Game logic (pure) | Input: state object. Output: mutation instructions or return values. No DOM. |
| **data/** | Config & tuning | Immutable, frozen objects. Global constants. |
| **game/** | Orchestration | Glue systems together; manage state transitions. |
| **ui/** | Menus & HUD | Render only; use pixel-text.js for consistency. |
| **entities/** | Game objects | Data + pure methods. Rendering in graphics/ only. |
| **world/** | World structure | Tilemap, maps, spawners. Mostly pure. |
| **save/** | Persistence | Serialization & validation (no localStorage in save-serializer.js). |

## Testability (node --test)

**Pure modules** (testable in Node without browser):
- `src/systems/*.js`
- `src/data/*.js`
- `src/world/tilemap.js`
- `src/save/save-serializer.js`

**Impure modules** (require browser):
- `src/graphics/*`
- `src/game/game-controller.js`
- `src/game/game-session.js`
- `src/main.js`

Tests: `npm test` runs `node --test tests/*.test.js`. Avoid mocking; test real state transforms.

## Naming Conventions

| Category | Convention | Example |
|----------|-----------|---------|
| **Classes** | PascalCase | `ComboController`, `Player`, `Tilemap` |
| **Functions & methods** | camelCase | `updateWorld()`, `applyExpGain()`, `isSolidTile()` |
| **Constants** | UPPER_SNAKE_CASE | `TILE_SIZE`, `COMBO_CONFIG`, `SKILL_DEFINITIONS` |
| **Private methods** | prefix with `#` (or convention `_`) | `#updateAttack()` or `_resolveCollision()` |
| **Booleans** | prefix with `is`, `has`, `can`, `should` | `isAttacking`, `hasAggro`, `canChain` |

## Data Shapes & Immutability

- **Config objects** → freeze with `Object.freeze()` to prevent mutations:
  ```js
  export const HERO_BASE_STATS = Object.freeze({ maxHp: 100, atk: 12, ... });
  ```
- **Runtime state** → mutable (entities, world state, etc.).
- **Save/Load** → validate with `validateSave()` before restoring; never trust client JSON.

## Common Patterns

### Adding a New Monster Type

1. **Asset:** Create 64×64 PNG in `assets/ninja-adventure/monsters/` (columns=facing down/up/left/right, rows=walk/attack frames).
2. **Register:** Add to ASSET_FILES + MONSTER_SHEETS in asset-atlas.js.
3. **Stats:** Define in monster-definitions.js: name, HP, ATK, DEF, speed, EXP, gold, aggroRange.
4. **Spawn:** Add spawner to a map with x, y, monsterType, maxCount, respawnDelayMs.

### Adding a New Prop

1. **Add sprite to asset pack** in `assets/ninja-adventure/tilesets/` (or appropriate folder): Create PNG with correct dimensions (w×h 16px cells).

2. **Register in asset atlas** in `src/graphics/asset-atlas.js` (PROP_RECTS):
   ```js
   PROP_RECTS: { ..., myProp: ['nature', cx, cy, w, h] } // w, h in cells
   ```

3. **Define metadata** in `src/world/prop-definitions.js`:
   ```js
   myProp: def(w, h, fw, fh, solid)
   // w, h = canvas size in 16px cells; fw, fh = footprint in tiles; solid = blocks movement
   ```

4. **Place in maps** in `src/world/maps/*.js`:
   ```js
   props: [{ type: 'myProp', tx, ty }, ...]
   ```

### Adding a New Map

1. **Create map file** `src/world/maps/my-map.js`:
   ```js
   export const MY_MAP = {
     id: 'my-map', name: 'My Map', recommendedLevel: 5,
     tilemap: { rows: ['ggqqgg', 'gg..gg', ...] },
     portals: [{ x: 160, y: 320, destMapId: 'other-map', destX: 80, destY: 80 }],
     spawners: [{ x: 200, y: 200, monsterType: 'slime', maxCount: 5, respawnDelayMs: 3000 }],
     props: [{ type: 'pine', tx: 50, ty: 60 }, ...]
   };
   ```

2. **Register** in `src/world/map-registry.js`:
   ```js
   import { MY_MAP } from './maps/my-map.js';
   export const MAP_REGISTRY = Object.freeze({
     ...existing..., [MY_MAP.id]: MY_MAP
   });
   ```

3. **Wire portals** from other maps to point here.

### Adding a New Skill

1. **Define** in `src/data/skill-definitions.js` (pick a branch/tier, reuse an existing `effect.kind`
   when possible -- `skill-cast-system.js` has one generic handler per kind):
   ```js
   mySkill: {
     id: 'mySkill', name: 'My Skill', type: 'active', branch: 'sword', tier: 2,
     maxLevel: 5, requiredLevel: [3, 5, 8, 11, 15], requires: [{ skill: 'slashWave', level: 2 }],
     mpCost: [10, 11, 12, 13, 15], cooldownMs: [3000, 2800, ...], damageMult: [1.5, 1.8, ...],
     range: [100, 110, ...], icon: 'iconCut', description: 'Does X.',
     effect: { kind: 'projectile', speed: 160, spriteKey: 'arc', w: 14, h: 14, pierceFromLevel: 3, label: 'My Skill!', textColor: '#fff' },
   }
   ```
2. **Add to `SKILL_ORDER`** in the same file (order drives the tree's tier1/tier1/tier2/tier2/tier3 layout per branch).
3. **New icon**: add the 24x24 PNG key (e.g. `assets/ninja-adventure/icons/...`) to `ASSET_FILES` in
   `src/graphics/asset-atlas.js`; `sprite-registry.js` wires `registry.icons[id]` automatically from `def.icon`.
4. **New effect kind** (only if none of `projectile/radial/strike/dash/heal/buff` fit): add a handler
   function + entry in `CAST_HANDLERS` in `src/systems/skill-cast-system.js`.
5. **Add tests** in `tests/skill-tree.test.js` / `tests/skill-system.test.js` to verify damage scaling,
   prerequisite gating and cooldowns.

### Adding a New Equipment Item

1. **Weapon**: add a `weapon(id, name, spriteKey, baseAtk, minItemLevel)` entry to `WEAPON_TEMPLATES` in
   `src/data/item-definitions.js`. `minItemLevel` gates which monsters can drop it (see `itemLevel` on
   `MonsterDefinition` in `monster-definitions.js`) -- keep weapons roughly ordered by `baseAtk` so
   stronger gear shows up later.
2. **Armor** (helmet/armor/gloves/boots): add a `gear(slot, id, name, spriteKey, minItemLevel, baseStats)`
   entry to `ARMOR_TEMPLATES`. helmet/armor use `{def, maxHp}` (armor's should be bigger), gloves use
   `{atk, critChance}`, boots use `{def, speedMult}`. Reuse the slot's existing icon unless you add a new one.
3. **Sprite**: if it's a new image, add the path to `ASSET_FILES` in `src/graphics/asset-atlas.js` (weapon
   PNGs from `assets/ninja-adventure/items/weapons/`, gear icons from `assets/ninja-adventure/icons/Items & Weapon/`)
   -- `sprite-registry.js` wires `registry.items[templateId]` automatically from the template's `sprite` field.
4. No systems/UI code changes needed: `equipment-system.js`'s `generateItem`/`rollEquipmentDrop` and
   `inventory-panel.js` are fully data-driven off `ITEM_TEMPLATES`/`TEMPLATES_BY_SLOT`.
5. **Add tests** in `tests/equipment-system.test.js` if the new item changes rarity/level-gating behavior.

### Adding Content (NPCs / Quests)

**NPC:** (1) Add character PNG to `assets/ninja-adventure/characters/` (64×112px: columns=facing, rows 0-3=walk, row 4=attack) + face PNG. (2) Register in asset-atlas.js (ASSET_FILES + NPC_SHEETS). (3) Define in `npc-definitions.js`: `{id, name, sprite, dir, lines, quests}`. (4) Wire quests list. (5) Auto-interaction via `npc-interaction.js` (E key).

**Quest:** (1) Define in `quest-definitions.js`: `{id, title, giver, requiredLevel, prerequisite, objective, rewards, dialog}`. (2) Add to NPC's quests list. (3) State machine in `quest-system.js` handles accept/track/turn-in. (4) Test via `quest-system.test.js`.

**Licensing:** All sprites from CC0 Ninja Adventure asset pack (NPC/monsters/boss). Custom hero sprites in `assets/custom/hero/` are AI-generated from concept. Do not add copyrighted art without explicit license.

### Regenerating Custom Hero Sprites
Update concept PNG → `npm run sprites:build` → validate → commit `assets/custom/hero/`. Spec: 24×24px, 8 animations per row (down/up/left/right), 32-color palette, feet pivot bottom.

### Adding an HD Hero Sheet (hand-painted, Zenonia 4 style)

1. **Drop the raw sheet** in `assets/hd/hero/<anim>.png` -- 4 rows (DOWN/UP/LEFT/RIGHT) x N columns
   (frames in time order); see `FRAME_COUNTS` in `tools/build-hd-hero.py` for the expected frame
   count per animation (idle 4, walk 6, attack1-2 4, attack3 6, dash 3, hurt 2, death 5). A
   standalone portrait goes to `assets/hd/hero/hero_face.png` (any resolution, square-ish). Real
   alpha or an opaque painted checkerboard are both fine -- the build script detects which.
2. **Build:** `npm run hd:build` (= `python tools/build-hd-hero.py`). Writes 96×96 frame sheets (or
   the 152×152 face) to `assets/hd/hero/normalized/`, regenerates `contact-sheet.png` + `README.md`
   + `scale.json`, and validates (exact sizes, RGBA, no empty frames, feet near the bottom row).
   Read the contact sheet to sanity-check the cutout before wiring it in.
3. **Register in `ASSET_FILES`** (`src/graphics/asset-atlas.js`): add `hdHero<Anim>: HD_HERO_NORM_DIR
   + '<anim>.png'` (or `hdHeroFace` for the portrait). Only list keys for sheets that actually exist
   -- `loadAllImages()` tolerates a missing file fine, but a 404 for one we know isn't there yet is
   just console noise.
4. No other code changes: `sprite-registry.js`'s `buildHero()` picks up any `HD_HERO_ANIMATIONS`
   entry whose image loaded and uses it in place of the 24px pixel frames for that animation;
   `player-renderer.js`/`menu-screens.js`/`hud-renderer.js`/`inventory-panel.js` already draw at an
   explicit world-unit size with smoothing toggled from the frame's `pixelsPerUnit`.
5. **Add tests** in `tests/hd-asset-config.test.js` if you change the `HD_HERO`/`unitsFor` contract.

See `docs/system-architecture.md` → "HD Assets" for the resolution-independence model
(`pixelsPerUnit`) and why boot stays resilient to a not-yet-generated sheet.

### Adding an HD Ground Tile (hand-painted, Zenonia 4 style)

1. **Drop the raw sheet** in `assets/hd/tiles/<name>.png` -- a flat, evenly-spaced grid (no exact
   pixel alignment needed; cells are split evenly by their known `(cols, rows)`). Only `portal.png`
   needs real alpha (transparent background); the rest are opaque painted textures. A painted
   panel border around each cell is fine -- the build crops inside it (`margin`).
2. **Register the grid:** add `'<name>.png': (cols, rows, margin)` to `GRIDS` plus a `build_<name>`
   function in `tools/build-hd-tiles.py` (plus a short `CELL_NOTES` entry documenting what each
   cell is). Use the helpers in `tools/hd_tile_texture_utils.py`: `heal_seams` on the axes a cell
   repeats along (every cell must tile without a visible edge), `macro_from_cells` for a set of
   variants that should read as one continuous texture, `material_alpha` for a transition cell
   that should let the base grass show through.
3. **Build:** `npm run hd:tiles` (= `python tools/build-hd-tiles.py`). Writes a 64px-per-cell sheet
   to `assets/hd/tiles/normalized/<name>.png`, regenerates `contact-sheet.png` + `README.md`. Read
   the contact sheet to confirm the cut.
4. **Register in `ASSET_FILES`** (`src/graphics/asset-atlas.js`): add `hd<Name>: HD_TILE_NORM_DIR +
   '<name>.png'` -- only list keys for sheets that actually exist, same 404-noise reasoning as the
   HD hero set.
5. **Add cells to `HD_TILES`** (same file): `{ image: 'hd<Name>', variants: [[cx, cy], ...] }` for
   independent tiles, or `{ image: 'hd<Name>', block: [cx, cy] }` for a 3x3 auto-tile block (top-left
   cell) -- same shape as the pixel-art `GROUND_TILES`. Reuse an existing key (e.g. `grass`,
   `dirtBlock`) when the HD sheet replaces a pixel-art tile 1:1; use a new key (like `flowers`/
   `waterAnim`) only for an HD-only concept with no pixel-art equivalent. Tag a variant set built
   as one seamless texture with `macro: [cols, rows]` (cols*rows variants, quadrant order); a
   block may add `center`/`inner`/`shapes`/`fill` (see the `HD_TILES` comment and
   `docs/system-architecture.md` → "HD ground tiles").
6. No other code changes: `tile-sprite-builder.js`'s `buildTiles()` merges `HD_TILES` over the
   pixel-art cut per tile name (HD wins where its sheet loaded); `tilemap-renderer.js` already draws
   every tile at an explicit `TILE_SIZE` destination size with smoothing toggled from
   `registry.tilesHd`.
7. **Add tests** in `tests/hd-tiles-config.test.js` if you change the `HD_TILES` shape or add a name
   that `tile-char-info.js` doesn't yet reference.

See `docs/system-architecture.md` → "HD Assets" → "HD ground tiles" for the merge-by-name model and
the portal-backdrop fix (real alpha vs. the per-frame black canvas clear).

### Adding an HD Prop (hand-painted, Zenonia 4 style)

1. **Drop the raw sheet** in `assets/hd/props/<name>.png` -- objects can be placed irregularly (not
   an exact grid); real alpha (a painted glow/halo haze is fine, it gets thresholded away).
2. **Register it:** add `'<name>.png': { mode: 'auto'|'whole'|'manual', outputs: [...] }` to `SHEETS`
   in `tools/build-hd-props.py` (`'auto'` splits by empty-column gaps -- the common case; `'whole'`
   is for a sheet with exactly one object reused by 2+ prop types; `'manual'` takes explicit column
   `splits` for the rare case where two objects touch by a few stray px). Add each new prop type's
   target world size to `PROP_TARGETS` (must match `width`/`height` in `prop-definitions.js`).
3. **Build:** `npm run hd:props` (= `python tools/build-hd-props.py`). Writes one PNG per prop type
   to `assets/hd/props/normalized/` (bottom-center fit into that type's world-size x4 canvas),
   regenerates `contact-sheet.png` + `README.md`. Read the contact sheet to confirm every object is
   clean (no halo/bleed) and sits on the bottom edge.
4. **Register in `asset-atlas-hd-props.js`**: add `hdProp<Name>: HD_PROP_NORM_DIR + '<name>.png'` to
   `HD_PROP_ASSET_FILES` and `<propType>: 'hdProp<Name>'` to `HD_PROPS` -- only list keys for files
   that actually exist, same 404-noise reasoning as the other HD sets.
5. No other code changes: `prop-sprite-builder.js`'s `buildProps()` merges `HD_PROPS` over the
   pixel-art `PROP_RECTS` cut per prop type (HD wins where its file loaded); `prop-renderer.js`
   already draws every prop at an explicit `(def.width, def.height)` world-unit dest size with
   smoothing toggled from the picked canvas's `pixelsPerUnit`.
6. If the HD art needs a taller box than the existing pixel-art `def` (e.g. a tall painted post
   squashed into a flat 16x16 footprint box), bump the prop's `height` in `prop-definitions.js`
   (never its `footprint` -- that would change map collision/placement) -- same convention already
   used by `pillar`/`torch`/`crystal`/`fence`/`signpost` (visually taller than their 1-tile ground
   footprint).
7. **Add tests** in `tests/hd-props-config.test.js` if you change the `HD_PROPS` shape or add a prop
   type that isn't yet in `PROP_DEFINITIONS`.

See `docs/system-architecture.md` → "HD Assets" → "HD props" for the segmentation model (gap
detection vs. manual split) and why fence/signpost got a height bump.

## Coordinate System Notes

- **World pixels** are the canonical unit (matches tile grid scale).
- Entity position `(x, y)` = center-x, feet-y (bottom of sprite).
- Hitbox `{w, h}` centered on entity position.
- On-screen pixels = world px × `PIXEL_SCALE` (4): 240×160 world px → 960×640 screen px.
- Camera offset applied in `world-renderer.js` only; logic uses world px throughout.

## Comment Clarity

- Comment **why**, not what. Example: "Axis-separated AABB sweep prevents corner-clipping."
- For complex algorithms (collision, AI state machine), explain approach.
- Mark **HACK**, **TODO**, **BUG** if present.

## Performance Considerations

- **Sprite caching** — all sprites built once at boot in sprite-registry.js.
- **Canvas re-renders** — tilemap pre-rendered to offscreen canvas; re-render only on map change.
- **Collision** — no broad-phase; iterate monsters/drops (small counts).
- **Animation** — frame index cached per entity; update once per tick.
- **Fixed timestep** — logic at 60Hz, rendering at monitor refresh rate (decoupled).

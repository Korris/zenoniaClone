# Code Review: Art Overhaul + Quest System (pass 2)

Date: 2026-10-22 | Scope: 33 new/rewritten files listed in task (graphics, world/maps, quest, NPC, UI, save, input) | Tests: `node --test tests/*.test.js` 53/53 pass.

Verification method: read every file in scope + their direct dependents (world-update, world-state, combat-system death path, monster-spawner, menu-screens, skill-panel, player, camera). Prop-coverage claims checked numerically by importing the real map modules in Node (scratchpad script, actual seeds).

**Score: 7.5 / 10** — architecture is clean (pure quest system, controller/session split, data-driven maps), input routing is correct, no state leaks found between PLAYING/DIALOG/QUEST_LOG. Deductions: three confirmed visual bugs in the generated meadow (portal + two NPCs hidden behind trees) and one quest soft-lock.

---

## Critical

None.

## High

### H1. Pines drawn over the meadow portal and two NPCs (confirmed with real seeds)
`src/world/map-builder.js:67-77` (`footprintFree`) + `src/world/maps/verdant-meadow-map.js:29-30`
`footprintFree` only checks the 1-row footprint; a 48px pine canvas covers the 2 tiles above its base. Numerically verified placements:
- `pine@(36,17)` canvas covers portal tile `(37,15)` — tree y=288 > hero y≈254, so the hero on the portal is hidden and the portal glyph is obscured (this is the `forestBorder` concern: `placePortal` reserves only `ty-1..ty+1`, not the rows below).
- `pine@(7,12)` covers `elderRowan (8,10)`; `pine@(10,24)` covers `hunterLena (10,22)` — NPCs (and their `!` markers) permanently behind foliage.
Why: props placed by `scatterProps`/`forestBorder` after `reserve()` of only the actor tile.
Minimal fix (map-builder.js) — reserve the "shadow" below actor spots and portals so tall props cannot sit there:
```js
/** Reserve a spot plus the 2 rows below it so tall props cannot draw over it. */
export function reserveActorSpot(grid, tx, ty, w = 1) { reserve(grid, tx - 1, ty, w + 2, 3); }
// placePortal(): replace reserve(grid, tx - 1, ty - 1, 3, 3) with reserve(grid, tx - 1, ty - 1, 3, 5);
```
Then in verdant-meadow-map.js use `reserveActorSpot(grid, 8, 10, 2)`, `reserveActorSpot(grid, 14, 11)`, `reserveActorSpot(grid, 10, 22)`. Add a unit test that iterates `MAP_REGISTRY` props with `height > 16` and asserts none overlaps an NPC/portal/start tile above its base row (the scratchpad check is ~15 lines).

### H2. "The Ancient Golem" quest becomes permanently uncompletable if the boss dies before accept
`src/data/quest-definitions.js:79-81`, `src/world/monster-spawner.js:30`, `src/game/quest-flow.js:32-33`
`theAncientGolem` is a kill quest gated on `knightsHonor` (6 skeleton-knight kills). A level-12 player who reaches the throne room first kills the Golem → `world.bossDefeated = true` → spawner never respawns it → quest offered later can never progress. Minimal fix in `onDialogOutcomeForQuests`:
```js
if (outcome.type === 'accepted') {
  const { objective } = def;
  if (objective.type === 'kill' && world.bossDefeated && world.mapDef.boss?.type === objective.monster) {
    session.quests.active[def.id].progress = objective.count; // boss already slain
  }
  session.banner.show('Quest accepted', def.title, 1800, '#ffe680');
}
```

## Medium

### M1. Save sanitizer keeps a quest in both `active` and `completed`
`src/systems/quest-system.js:14-17`
Corrupt/hand-edited save with same id in both lists → `questStatus` says `completed` so no NPC ever offers turn-in, but the zombie `active` entry shows "DONE" in tracker/log forever. Fix: fill `completed` first, then `if (QUEST_DEFINITIONS[id] && !state.completed.includes(id)) state.active[id] = ...`.

### M2. `computeNpcMarkers` recomputed every frame though quest state only changes on events
`src/game/game-controller.js:71`, `src/game/npc-interaction.js:24-31`
Per frame: object literal + per-NPC `statuses` array + `includes` scans. Cheap today (3 NPCs) but it is pure event-driven state. Fix: drop line 71; call `this.npcMarkers = computeNpcMarkers(...)` at the end of `handleEvent` (kills/levelUp/portal) and after `enterMap` in title/respawn paths (already done in `updateDialog`).

### M3. Map-name banner immediately overwritten by quest banner on portal travel
`src/game/game-session.js:49` then `src/game/quest-flow.js:23`
`enterMap` shows "Dark Cave" for 2400ms, `onMapReachedForQuests` replaces it the same frame. Fix: in `onMapReachedForQuests` use floating text (`world.addText`) like the kill path, or queue banner after a delay. Low effort, noticeable UX.

### M4. `continueFromSave`/`respawn` can silently complete a reach quest
`src/game/game-session.js:41,81`
`enterMap` returns touched ids; both callers discard them → quest flips to ready with no feedback. Acceptable, but at least `respawn()` should route through `onMapReachedForQuests`. One-liner in controller `updateDead`: `onMapReachedForQuests(session, session.respawn())` (respawn returns `lost`; change it to return `{ lost, reached }` or ignore).

## Low

- `src/game/game-controller.js:72` — manual `playerDied` synth duplicates `combat-system.js:128` event push. Keep one (DRY).
- `src/game/game-controller.js:62,68` — `nearbyNpc` computed before `updateWorld`; after a portal the stale NPC from the previous map is name-tagged for one frame. Set `this.nearbyNpc = null` in the `portal` branch of `handleEvent`.
- `src/ui/minimap-renderer.js:17-18,55` — module-level cache keyed by `mapId` is correct today (map defs are built once at import, deterministic). Not a bug for New Game. Prefer caching on `world.mapDef` identity (`WeakMap`) so it can never drift if maps become dynamic; also `/ 16` on line 44-45 should be `TILE_SIZE`.
- `src/world/tilemap.js:30,37` — string keys `` `${x},${y}` `` allocated per 1px sweep step × 4 tiles × every mover. Use `ty * this.width + tx` numeric key in a `Set<number>`.
- `src/graphics/world-renderer.js:28-33` — per-frame `layer` wrapper objects + closure. Fine at this scale; if profiled hot, keep a reusable array and sort by `e.y` directly (give props/npcs a `kind` field).
- `src/ui/dialog-box.js:56-59` — Esc on a `turnIn` payload still turns the quest in (payload has no choice). Harmless but surprising; document or skip turn-in when `choice === null && pause`.
- `src/ui/dialog-box.js:51-54` + `input-manager.js:15` — `confirm` includes J/Space, so an attack-mashing player skips all pages and auto-selects `Accept` (index 0). Consider defaulting `selected` to the last choice (Decline) for offers.
- `src/ui/quest-log-panel.js:16` — Tab closes the log but does not open Skills (user expectation: switch). Return `{ close:true, openSkills:true }` or drop `skillPanel` from the close set.
- Dead code / YAGNI: `quest-log-panel.js:48,56` unused `active` param + `void active`; `prop-art-generator.js:10` identity `prop()` wrapper; `prop-art-generator.js:117,121` `shade` computed then `void shade`; `quest-definitions.js:90` `QUEST_ORDER` unused; `effect-sprite-data.js:43` `talk` marker unused; `hud-renderer.js:134` `HUD_BAR_HEIGHT` unused.
- `src/graphics/tile-art-generator.js:177-182` — cliff "grass lip" is green on the desert map border (sand). Cosmetic; pass a lip colour per map or skip lip when the above tile is `s`.
- `src/save/save-serializer.js:55` + `game-session.js:40` — quests sanitized twice (validateSave then continueFromSave). Harmless; drop the second.

---

## Verified OK (no action)

- **Input double-trigger**: `updatePlaying` returns right after opening the dialog, `endFrame()` clears `pressedCodes`, so the opening E/Enter never advances the dialog. Same for title→PLAYING (Enter), dialog close→PLAYING, pause/skills/quest-log toggles. `e.repeat` ignored, so holding E does not re-fire. Fixed-step loop (`game-loop.js:37-39`) calls `update` → `endFrame` per sub-step, so no replay across sub-steps.
- **Quest edge cases**: `acceptQuest`/`turnInQuest` re-validate status at close time; level-up inside `turnInQuest` recomputes stats + saves; prerequisite + level gates in `questStatus`; unknown quest ids dropped by `sanitizeQuestState`; kill counting includes respawns (intended); `monster.type` matches objective keys incl. `golemBoss`.
- **Collision**: prop footprints (`buildProps`) and NPC 1x1 footprints go into `blockedByProps`, honoured by `moveBox`, `findWalkableNear`, `placePlayerAtWorld`, and `spawnInZone`. No NPC sits on a solid tile; no spawn zone is majority-solid; spawn targets are adjacent to, not on, portal tiles (no bounce-back loop).
- **Save**: quests serialized shallow-copied → JSON; dead hero never saved; death → respawn → save path intact.

## Recommended actions (priority order)
1. H1 — reserve rows below NPC/portal spots; add the map-invariant unit test.
2. H2 — auto-progress boss kill quest when `bossDefeated` already true.
3. M1, M2, M3 — small, isolated fixes.
4. Low items opportunistically; dead-code removals are safe deletes.

## Unresolved questions
- Is bats spawning in Hunter Lena's tile neighbourhood (zone `{6,20,14,7}` contains `(10,22)`) intentional flavour or should NPC spots be excluded from spawn areas?
- Should Esc on a turn-in dialog really complete the quest, or only close it?

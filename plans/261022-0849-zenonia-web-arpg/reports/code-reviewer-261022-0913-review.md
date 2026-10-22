# Code Review - Zenonia Web ARPG (vanilla JS + Canvas)

Date: 2026-10-22 | Scope: `src/**` (58 files, 3832 LOC), `index.html`, `styles.css`, `package.json` | Tests: 48/48 pass (`node --test`)

## Overall Assessment

Clean, well-layered codebase. Systems are pure and testable, entities are thin, state machine is explicit, every file < 200 lines. Input edge-triggering is handled correctly: `endFrame()` runs after every update and every state transition `return`s before further processing, so Tab/Enter/J do not double-trigger across screens (verified title->play, pause->play, skills->play, dead->play, win->play). Map transitions clear all per-map lists, dash/knockback are reset on placement, effects that `follow` the hero die with the map. No Critical issues. Three High items are real gameplay bugs that the headless run would not surface.

**Score: 7.5 / 10**

---

## Critical

None.

## High

### H1. Hit-stop swallows attack/skill presses (combo buffer bypassed)
- `src/game/world-update.js:79-84` returns before `updatePlayer()` while `hitStopMs > 0`; `GameController.update` then calls `input.endFrame()` (`game-controller.js:35`) so `wasPressed('attack')` is lost.
- Every landed hit sets 25 ms (finisher 60 ms) of hit-stop (`combat-system.js:70,72`) = 2-4 update steps per hit where mashing is ignored. `ComboController.queued` exists exactly for this but is never reached. Also drops K/L/I casts.
- Fix (minimal):
  ```js
  if (world.hitStopMs > 0) {
    world.hitStopMs -= dt;
    if (input.wasPressed('attack')) world.player.combo.pressAttack(); // buffer into combo queue
    ...
  }
  ```
  (Skill presses can be buffered similarly or accepted as dropped.)

### H2. Mouse clicks never drained during PLAYING -> stale clicks fire menu options
- `src/core/input-manager.js:52-57` queues every `mousedown`; only `MenuScreen.handleInput` / `SkillPanel.handleInput` call `takeClicks()`. Nothing drains in PLAYING, so clicks accumulate (unbounded, minor) and are all replayed on the first frame a menu opens.
- Repro: click canvas twice at y ~ 360 while playing, press Esc -> pause menu opens and instantly selects+confirms row 1 ("Save Game") or row 2 ("Quit to Title") via `menu-screens.js:22-27`.
- Fix: `endFrame() { this.pressedCodes.clear(); this.clicks.length = 0; }` (`input-manager.js:89-91`).

### H3. `moveBox` blocked flags use float equality -> spurious blocks
- `src/world/tilemap.js:62-65`: `blockedX = x !== box.x + dx` compares a 1 px-stepped sum against a single addition. With `dt = 1000/60`, dash step is `7.000000000000001`; measured 1 spurious block per ~2300 dash frames on open ground (script in scratchpad). Consumers: `skill-cast-system.js:88` (dash ends early), `world-update.js:38` (wander reset).
- Fix: have `sweepAxis` report the collision instead of inferring it:
  ```js
  sweepAxis(...) { ... if (blocked) return { pos, blocked: true }; ... return { pos, blocked: false }; }
  ```
  or compare with `Math.abs(x - (box.x + dx)) > 1e-6`.

## Medium

### M1. Any exception in update/render kills the loop silently
- `src/core/game-loop.js:36-41`: a throw inside `handlers.update/render` prevents the next `requestAnimationFrame`; screen freezes, only console shows why. `main.js:27-32` only guards boot.
- Fix: wrap body in `try { ... } catch (err) { this.stop(); console.error(err); /* show overlay */ }`.

### M2. Save accepts hp <= 0; autosave can run on the death frame
- `src/save/save-serializer.js:46-47` passes any finite `hp` through; `player.js:113` clamps only the upper bound. `game-session.js:77-84` autosave runs in `updatePlaying` *before* events are handled (`game-controller.js:57-58`), so a save with `hp: 0` is possible, and tampered saves with negative hp load fine.
- Result: "Continue" yields a hero with hp <= 0, `dead=false`; `Player.takeDamage` computes `dealt = min(hp, amount) <= 0`, `hitPlayer` (`combat-system.js:119`) returns early so no `playerDied` event; only the safety net at `game-controller.js:60` rescues it.
- Fix: `hp: isNum(p.hp) && p.hp > 0 ? p.hp : undefined` (same for mp >= 0) and skip autosave when `player.dead`.

### M3. One-frame camera pop after every map load / respawn
- `map-manager.js:26,48` snap to `(player.x, player.y)`; `world-update.js:97` follows `(player.x, player.y - 8)`. First frame after load jumps 8 world px (24 screen px).
- Fix: snap with the same `-8`, or move the offset into `Camera.follow`.

### M4. Portal tile test differs between load placement and runtime detection
- `map-manager.js:42` checks tile at `floor(y / 16)`; `findPortalUnderPlayer` (`:55`) uses `floor((y - 4) / 16)`. A save made with feet 1-3 px into the tile below a portal passes the 'P' guard, then triggers the portal on the first frame after Continue (possible instant travel/"Portal sealed" on load).
- Fix: one helper `portalTileUnder(player)` used by both.

### M5. Plan TODO lists not updated
- 29 `- [ ]` items across `phase-01..06` remain unchecked although the work is done (and file names drifted: `game-state.js`, `scene-manager.js`, `collision-system.js`, `hit-detection.js`, `skill-cast-effects.js` never existed). Mark complete / reconcile names so the plan reflects reality.

## Low

- **L1 DRY:** `game-controller.js:105` re-implements `DEATH_EXP_PENALTY` math (`Math.floor(exp * 0.1)`); use `applyDeathPenalty({level, exp}).lost`.
- **L2 DRY:** `DIR_ANGLE` duplicated (`combat-system.js:7`, `skill-cast-system.js:10`); "moveBox -> write back x/y" pattern repeated 3x (`player-controller.js:15-21`, `world-update.js:33-36`, `skill-cast-system.js:74-76`) -> one `applyMove(entity, tilemap, dx, dy)` helper.
- **L3 YAGNI / dead code:** `input-manager.js:26,33-39` `keyToActions` never read; `graphics/sprite-animation.js` only imported by tests; `leveling-system.js:49 expProgressRatio`, `local-storage-save.js:28 clearSave`, `menu-screens.js:12 setOptions`, `tilemap.js:14-19,35 pixelWidth/pixelHeight/charAtWorld`, `monster-definitions.js:41 BOSS_TYPE`, `player.js:33 hitbox` getter unused. `monster.hitBySwing` (`monster.js:25`, `combat-system.js:62-63`, `world-update.js:15`) can never dedupe anything: `swingId` is fresh per swing and each monster is visited once per swing -> delete the Set and its per-frame cleanup loop.
- **L4** `combat-system.js:93` reads `monster.attackCount` before init (`NaN % 3`); works by accident. Init `attackCount = 0` in `Monster` ctor.
- **L5** `monster-renderer.js:34` ternary has identical branches; hit flash ignores `facingLeft` and is built from `frames[0]` only (`sprite-registry.js:47`).
- **L6** Hardcoded `-= 16` instead of `dt`: `game-controller.js:103`, `skill-panel.js:49`.
- **L7** `player-controller.js:66-69` knockback early-return freezes `combo.update`, so a swing pauses 100 ms mid-animation then resumes; consider `combo.reset()` on hit.
- **L8** `world-update.js:78` comment says effects stay alive during hit-stop; only texts/particles are ticked.
- **L9** `game-session.js:16-18` builds a throwaway `Player`/`WorldState` that is always replaced.
- **L10** `index.html:2` `lang="vi"` on English content; `constants.js:10` comment references nonexistent `game-state.js`.
- **L11** "New Game" with an existing save overwrites it at the first autosave/level-up without confirmation.
- **L12** `WorldState.removeDead` allocates 6 arrays per step - fine at this scale; only revisit if entity counts grow.

## Edge Cases Checked (no issue found)
- Empty monster list: all loops are plain `for..of`; `livingBoss` returns null; HUD guards it.
- Map transition mid-attack: combo state persists harmlessly; dash/knockback cleared in `placePlayerAtTile`; projectiles/effects/texts cleared.
- Respawn while whirlwind effects follow hero: world frozen in DEAD, cleared by `enterMap` on respawn.
- Portal ping-pong: all 6 portal targets land on a non-'P' tile adjacent to the return portal.
- Spawn zones do not overlap any `playerStart`.
- Press-state leaks: none - every transition `return`s and `endFrame()` clears per update; `wasPressed` survives zero-update rAF frames on high-Hz displays.
- Memory: `tintedFrameCache` WeakMap keyed by static registry canvases; `hitIds`/`hitBySwing` bounded; only the click queue (H2) grows.
- Performance: per-frame hot paths (1 px sweep, filter+sort of <=30 actors, 6 filters) are trivial at this scale.

## Positive Observations
- Pure systems (`combo`, `leveling`, `skill`, `ai`, `damage`, `loot`, `tilemap`, `camera`, `save-serializer`) with injectable RNG - good test surface.
- Fixed 60 Hz step with frame clamp; render decoupled.
- Save validation clamps level/skills/gold and falls back to the first map on unknown ids.
- Clear comments explaining *why* (Y-sort, hit-stop, pixel rounding, mirrored sprites).

## Recommended Actions (priority order)
1. H1 buffer attack press during hit-stop (3 lines).
2. H2 clear `clicks` in `endFrame()` (1 line).
3. H3 return `blocked` from `sweepAxis` (small refactor).
4. M2 reject `hp <= 0` in `validateSave`; skip autosave when dead.
5. M1 guard the loop body; M3 unify camera offset; M4 unify portal tile helper.
6. L3 delete dead code (`hitBySwing`, `keyToActions`, unused exports/module).
7. Add a DOM-free smoke test for `updateWorld` + `GameController` with a stub input (both are already pure enough).
8. Reconcile plan phase checklists (M5).

## Metrics
- Tests: 48 pass / 0 fail (tester report says 40; suite has grown).
- Lint/type: no tooling configured (plain ES modules); no syntax errors found.
- Largest file: `sprite-registry.js` 104 LOC; all < 200.

## Unresolved Questions
- Is dropping K/L/I casts during hit-stop acceptable, or should they be buffered like attack?
- Should "New Game" over an existing save require confirmation?

# Phase 05 - Skills, Skill Panel, HUD, Pause, Save/Load, Death, Boss Win

## Context
- Prev: [phase-04](phase-04-monsters-ai-drops-leveling.md). Next: [phase-06](phase-06-tests-and-polish.md)

## Overview
Priority P1 | Status pending | 6h
3 active skills + 1 passive, upgrade rules, skill panel, full HUD, banners, pause menu, localStorage save/load, death/respawn, boss win screen.

## Skill Definition Shape (data/skill-definitions.js)
```js
export const SKILL_DEFS = {
  'slash-wave': { id: 'slash-wave', name: 'Slash Wave', type: 'active', slot: 1, icon: 'icon-slash-wave',
    maxLevel: 5, reqHeroLevel: [1, 4, 7, 10, 13],          // hero level needed to reach skill lv 1..5
    mpCost: [8, 9, 10, 11, 12], cooldown: [2.5, 2.2, 2.0, 1.7, 1.5], mult: [1.5, 1.8, 2.1, 2.4, 2.8],
    params: { speed: 420, range: 360, pierce: [0, 0, 1, 1, 2] }, desc: 'Fires a sword wave forward.' },
  'whirlwind': { ..., slot: 2, reqHeroLevel: [4, 7, 10, 13, 16], mpCost: [15, 18, 21, 24, 27],
    cooldown: [6, 5.5, 5, 4.5, 4], mult: [0.8, 1.0, 1.2, 1.4, 1.6],          // per tick, 3 ticks over 0.6s
    params: { radius: [90, 100, 110, 120, 130], ticks: 3, duration: 0.6 } },
  'dash-strike': { ..., slot: 3, reqHeroLevel: [6, 9, 12, 15, 18], mpCost: [12, 12, 13, 13, 14],
    cooldown: [5, 4.5, 4, 3.5, 3], mult: [2.0, 2.4, 2.8, 3.2, 3.6],
    params: { distance: [160, 180, 200, 220, 240], duration: 0.15 } },        // invuln while dashing
  'vitality': { ..., type: 'passive', slot: null, reqHeroLevel: [3, 6, 9, 12, 15],
    params: { maxHpPct: [0.08, 0.16, 0.24, 0.32, 0.40] } },
}
export const SKILL_SLOT_ORDER = ['slash-wave', 'whirlwind', 'dash-strike']   // K, L, I
```
Arrays indexed by `skillLevel - 1`. Player starts Lv1 with 1 SP, all skills lv0.

## Skill System (pure, systems/skill-system.js)
```js
export function canUpgradeSkill(skillState, def, heroLevel, skillPoints) -> { ok, reason }
// reasons: 'max-level' | 'no-points' | 'hero-level' (needs def.reqHeroLevel[skillState.level])
export function upgradeSkill(skills, skillId, heroLevel, skillPoints, defs = SKILL_DEFS) -> { skills, skillPoints, ok, reason }  // immutable
export function canCastSkill(skillState, def, mp) -> { ok, reason }   // 'not-learned' | 'cooldown' | 'no-mp' | 'passive'
export function tickCooldowns(skills, dt) -> void
export function getVitalityPct(skills) -> number
```
Cast effects live in `systems/skill-cast-effects.js` (DOM-free but state-mutating):
`castSlashWave(state, player, lv)` spawns projectile; `castWhirlwind` adds timed AoE effect hitting monsters in radius per tick (dedupe per tick); `castDashStrike` sets `player.dashing = { dirX, dirY, remaining, hitIds }`, substep movement via collision, damages monsters overlapped once.

## UI
- `ui/hud-renderer.js`: top-left HP (red) / MP (blue) / EXP (yellow) bars with numbers, `Lv N`, gold; bottom-right 3 skill icons (16x16 pixel icons, x3) with key label, dark vertical cooldown overlay + seconds, greyed if not learned or no MP; combo counter `N HITS` right-center when hitStreak >= 2 (scale pop on increment).
- `ui/message-banner.js`: `showBanner(state, text, sec = 2.5)`; draws center-top, fade in/out. Used for map name, level up, portal gate, saved.
- `ui/skill-panel.js`: scene 'skillPanel' (world frozen). Up/Down select 4 skills, J/Enter upgrade, Tab/Esc close. Shows name, lv x/5, desc, next-level req, cost/cd, SP available, red reason text on fail.
- `ui/pause-menu.js`: Esc -> scene 'paused'. Options: Resume, Save Game, New Game (confirm with second press). 
- `ui/title-and-end-screens.js`: title (Continue if save exists / New Game), dead ("You fell... Enter to respawn"), win ("Golem defeated! Lv, time, gold" -> Enter back to playing).

## Save/Load
```js
// save/save-serializer.js (pure)
export function buildSavePayload(state) -> { version: 1, savedAt, mapId, x, y, level, exp, skillPoints, gold,
  hp, mp, skills: { [id]: level }, killedBoss }
export function validateSavePayload(obj) -> payload | null   // type-check every field, clamp level 1..30, unknown skills dropped
// save/local-storage-save.js
const KEY = 'zenonia-save-v1'
export function saveGame(state) / loadGame() -> payload|null / hasSave() / clearSave()   // try/catch JSON + quota errors
```
Autosave: on map change, level up, and every 30s while playing. Load: `createPlayer(payload)` + `loadMap(state, payload.mapId)` then set x,y.

## Death / Respawn / Win
- hp <= 0 -> scene 'dead', player death anim (flash). Enter -> applyDeathPenalty, full HP/MP, loadMap(current, playerStart), autosave.
- Boss dies -> `killedBoss = true`, 1.5s delay -> scene 'win'. Boss does not respawn once killed (spawner checks flag).

## Related Files (create)
- `src/data/skill-definitions.js`, `src/systems/skill-system.js`, `src/systems/skill-cast-effects.js`
- `src/graphics/sprites/skill-icon-sprite-data.js` (+ slash-wave projectile & whirlwind ring frames in effect-sprite-data.js)
- `src/ui/hud-renderer.js`, `skill-panel.js`, `message-banner.js`, `pause-menu.js`, `title-and-end-screens.js`
- `src/save/save-serializer.js`, `local-storage-save.js`
- Modify: `scene-manager.js`, `player.js` (skill input, vitality in computeStats), `main.js`

## Implementation Steps
1. skill-definitions + skill-system (pure).
2. Cast effects + projectile reuse; wire K/L/I in updatePlayer.
3. Skill panel scene; recompute stats after Vitality upgrade (keep hp ratio).
4. HUD, banner, combo counter.
5. Save serializer + storage; title Continue/New Game; autosave timer.
6. Pause menu, death/respawn, boss win.

## Todo
- [ ] skill-definitions.js, skill-system.js, skill-cast-effects.js, icons
- [ ] skill-panel.js, hud-renderer.js, message-banner.js
- [ ] pause-menu.js, title-and-end-screens.js
- [ ] save-serializer.js, local-storage-save.js, autosave
- [ ] death/respawn, boss win flow

## Success Criteria
- Upgrade blocked with clear reason when level/SP insufficient; skills respect MP + cooldown.
- Reload page -> Continue restores map, position, level, skills.
- Full run meadow -> castle -> boss kill -> win screen.

## Security / Robustness
- Never trust localStorage: validate payload, fallback to new game on any error.

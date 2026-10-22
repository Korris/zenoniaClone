# Phase 08: Quest System, NPCs, Dialog

**Status:** Complete (2026-10-22)
**Priority:** High (user request: "làm cả hệ thống quest")

## Requirements
- NPCs on maps with `!` (available) / `?` (turn-in) markers; talk with E; "[E] Talk" prompt + name tag.
- Quests: kill N monsters or reach a map; level gates + prerequisite chains; rewards EXP/gold.
- Dialog box: speaker, typewriter text, pages, Accept/Decline.
- Quest log (Q) + top-right tracker; progress floating text; completion banners.
- Persisted in save (`quests: {active, completed}`), sanitized on load.

## Data shapes
- NPC: `{id, name, sprite, dir, lines[], quests[]}` (`data/npc-definitions.js`, 6 NPCs)
- Quest: `{id, title, giver, requiredLevel, prerequisite, description, objective, rewards, dialog{offer,progress,complete}}` (`data/quest-definitions.js`, 9 quests)
- State: `{active: {questId: {progress}}, completed: []}`

## Files
- New: `systems/quest-system.js` (pure), `entities/npc.js`, `game/npc-interaction.js`, `game/quest-flow.js`, `ui/dialog-box.js`, `ui/quest-log-panel.js`, `ui/quest-tracker-hud.js`, `tests/quest-system.test.js`
- Modified: `game/game-controller.js` (DIALOG / QUEST_LOG states), `game/game-session.js` (quest state, map-enter hook), `save/save-serializer.js`, `core/input-manager.js` (interact, questLog), `core/constants.js`, `systems/combat-system.js` (monsterKilled event), `world/map-manager.js` (NPC spawn), `maps/*.js` (npcs)

## Todo
- [x] pure quest state machine + 5 tests
- [x] NPC entity + placement + solid tile
- [x] dialog box + accept/decline flow
- [x] markers, tracker, quest log
- [x] save/load quests
- [x] browser e2e: accept -> kill 5 slimes -> `?` -> turn in -> rewards -> saved

## Success criteria
- 53/53 tests pass; e2e loop verified headless; no console errors.

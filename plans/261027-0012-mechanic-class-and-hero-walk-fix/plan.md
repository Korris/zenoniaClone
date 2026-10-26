# Mechanic class + hero walk fix + Warrior black-hair redesign

Date: 2026-10-27. ChatGPT Plus chat "Zenonia Art Style Summary" (https://chatgpt.com/c/6adf7726-24c8-83ec-8802-2f636b0fc47b), Chrome "Browser 1" (Tan Pham).

## User asks
1. New playable class MECHANIC with a gun, like Zenonia 5's Mechanic.
2. Walk looks like "same arm + same leg" for Warrior and Wizard -> redraw walks as 4-key-pose cycles (L step / pass / R step / pass, opposite arm swing).
3. Warrior hair too similar to Mechanic -> black hair, different style (approved: black ponytail + red headband).

## Code (done, 143 tests pass)
- class-definitions.js: mechanic (gunnery/explosives/engineering, ranged, styleLabel per class)
- hero-stats-config.js: MECHANIC_COMBO_CONFIG (2 bullets + plasma blast finisher), COMBO_CONFIG_BY_CLASS
- skill-definitions-mechanic.js (15 skills, existing cast kinds), asset-atlas-hd-mechanic.js, tools/build-mechanic-fx.py
- sprite-registry heroes.mechanic, face, mechanic FX; class-select 3 cards; projectile-hit flashFx
- player-renderer: walk speed = HERO_WALK_CYCLE_MS / frames; build-hd-hero.py reads <src>/frame-counts.json

## Art status
Warrior v2: ALL DONE, swapped in (old set in assets/hd/hero/v1)
Mechanic: ALL DONE (8 sheets, face, fx). Mirrored left rows: attack3, dash, hurt, death
Wizard: 4-col walk DONE (old walk in assets/hd/wizard/v1)
Status: complete. 147 tests, class check + frost hollow check clean.
Capture recipe: scratchpad capture-notes.md + stitch-any.py; memory chatgpt-image-capture-via-chrome.

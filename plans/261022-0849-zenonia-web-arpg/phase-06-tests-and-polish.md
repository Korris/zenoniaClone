# Phase 06 - Unit Tests + Polish

## Context
- Prev: [phase-05](phase-05-skills-hud-save-death-win.md). Plan: [plan.md](plan.md)

## Overview
Priority P2 | Status pending | 3h
`node --test` suites for pure logic; game-feel polish; balance pass; 200-line audit; README.

## Requirements
- Tests import modules directly (`import { expToNext } from '../src/systems/leveling-system.js'`). No deps, no DOM.
- Deterministic RNG in tests: `const rng = () => 0.5`.
- `package.json` `"type": "module"` so tests use ESM.

## Test Files (create)
- `tests/leveling-system.test.js`
  - expToNext(1)=50, (2)=141, (4)=400, (10)=1581
  - addExp single level, multi-level jump, SP += levelsGained, cap at MAX_LEVEL
  - computeStats growth + vitality pct; applyDeathPenalty floors at 0, no de-level
- `tests/combo-attack-system.test.js`
  - press from idle -> swingStart step1; advance windup -> hit event
  - press within 0.4s after hit -> step2; then step3 finisher
  - no press for >0.4s after hit -> reset event, step 0
  - press after finisher does not start step 4 (restarts at step1 after reset)
  - mashing queues only one next step
- `tests/skill-system.test.js`
  - upgrade fails 'no-points', 'hero-level', 'max-level'; success decrements SP, immutable input
  - canCastSkill: not-learned, cooldown, no-mp, passive; tickCooldowns floors at 0
  - getVitalityPct matches defs
- `tests/collision-and-camera.test.js`
  - moveWithCollision stops at wall, slides along wall on diagonal, out-of-bounds solid
  - camera clamps to bounds, centers when world < view
- `tests/save-serializer.test.js`
  - round-trip buildSavePayload -> validateSavePayload; rejects bad version/types; drops unknown skills
- `tests/monster-ai-system.test.js` (light): idle->chase in aggro radius, windup in range, return beyond leash

## Polish Items
- Hit-stop 50ms on hit, 90ms on finisher (`state.hitStop` pauses world update).
- Screen shake on finisher/boss slam (`state.shake` decays, offset camera render only).
- White flash on damaged monster (draw with `globalCompositeOperation` via pre-built white silhouette sprite).
- Shadow ellipse under entities; y-sort.
- Monster windup telegraph: red "!" or flashing.
- Balance pass: Lv3 reachable in ~3 min in meadow, Lv12 before castle, boss ~60-90s fight at Lv14.

## Implementation Steps
1. Write test files; run `node --test tests/`; fix logic bugs found.
2. Audit line counts: `Get-ChildItem src -Recurse -Filter *.js | % { "{0} {1}" -f (Get-Content $_.FullName).Count, $_.FullName } | sort -desc`; split any >200.
3. Polish items above.
4. Full playthrough; tune numbers in data files only.
5. README: run instructions, controls, project structure.

## Todo
- [ ] 6 test files, all passing
- [ ] line-count audit, splits done
- [ ] hit-stop, shake, flash, shadows, telegraph
- [ ] balance pass + full playthrough
- [ ] README

## Success Criteria
- `node --test tests/` green; zero console errors through full playthrough; every src file <200 lines.

## Risks
- Logic modules accidentally importing DOM code -> tests fail at import; keep sprite/render imports out of systems/data.
- Balance drift -> all numbers centralized in data/*.js and game-constants.js.

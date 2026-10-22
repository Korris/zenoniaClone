# Phase 03 - Player, Hero Sprites, Combo, Hit Detection, Effects

## Context
- Prev: [phase-02](phase-02-tilemap-maps-portals-collision.md). Next: [phase-04](phase-04-monsters-ai-drops-leveling.md)

## Overview
Priority P1 | Status pending | 6h
Chibi hero with 4-dir walk + 3-step attack anims, pure combo state machine (400ms window), attack hitboxes, damage formula, slash/spark effects, floating damage numbers.

## Hero Sprite Spec
- 16x20 px, head ~12 rows (60%), body 8 rows. Big eyes, spiky hair, small sword.
- Frames: `down|up|right` x (`walk` 4 frames, frame0 = idle) + (`attack` 3 frames = combo step 1/2/3). Left = flipped right.
- `HERO_FRAMES = { down: { walk: [rows x4], attack: [rows x3] }, up: {...}, right: {...} }`
- Slash arc effect drawn on top (effect-sprite-data), rotated by facing.

## Data Shapes
```js
// data/hero-stats-config.js
export const HERO_BASE = { maxHp: 100, maxMp: 40, atk: 10, def: 4, spd: 150 }   // spd = world px/s
export const HERO_GROWTH = { maxHp: 12, maxMp: 5, atk: 3, def: 1.5 }             // per level, floor at use
export const MAX_LEVEL = 30
export const COMBO_STEPS = [   // seconds; knockback in world px
  { windup: 0.05, active: 0.08, recovery: 0.12, mult: 1.0, knockback: 40,  reach: 44, width: 40 },
  { windup: 0.05, active: 0.08, recovery: 0.12, mult: 1.2, knockback: 50,  reach: 44, width: 44 },
  { windup: 0.10, active: 0.10, recovery: 0.30, mult: 1.8, knockback: 160, reach: 56, width: 60, finisher: true },
]
export const COMBO_WINDOW = 0.4
export const FINISHER_LEVEL_BONUS = 0.05   // finisher mult += 0.05 * heroLevel

// entities/player.js
player = { x, y, w: 30, h: 24, facing: 'down'|'up'|'left'|'right', vx, vy,
  level, exp, skillPoints, gold, hp, mp, stats: {maxHp,maxMp,atk,def,spd},
  skills: { [skillId]: { level, cooldown } },
  combo: ComboState, invuln: 0, hitStreak: 0, streakTimer: 0, anim, dashing: null }
```

## Combo State Machine (pure, systems/combo-attack-system.js)
```js
export function createComboState() -> { step: 0, phase: 'idle', timer: 0, window: 0, queued: false, swingId: 0 }
// phases: idle -> windup -> active -> recovery -> (next step | waiting) ; waiting -> idle on window timeout
export function updateCombo(combo, dt, attackPressed, steps = COMBO_STEPS, comboWindow = COMBO_WINDOW) -> events[]
// events: { type: 'swingStart', step } | { type: 'hit', step, swingId } | { type: 'reset' }
```
Rules:
1. idle + press -> step=1, windup, swingId++, emit swingStart.
2. windup ends -> active, emit `hit` (hit lands), `window = comboWindow` starts counting.
3. press while active/recovery/waiting AND window > 0 AND step < 3 -> queued = true.
4. recovery ends: queued -> next step windup (swingId++); else phase 'waiting'.
5. waiting: press within window -> next step; window <= 0 -> reset to idle (step 0), emit reset.
6. After step 3 recovery -> reset (no chaining past finisher).
Player cannot move during windup/active; half speed in recovery.

## Hit Detection & Damage (pure)
```js
// systems/hit-detection.js
export function getAttackHitbox(player, stepDef) -> { x, y, w, h }   // rect in front of player by facing
export function findHitTargets(hitbox, targets, swingId) -> target[] // skips dead and target.lastSwingId === swingId
// systems/damage-formula.js
export function computeDamage(atk, mult, def, rng = Math.random) -> int
// = max(1, round(atk * mult * (0.9 + 0.2*rng()) - def * 0.5))
export function applyKnockback(target, fromX, fromY, force) -> void  // sets target.kbX/kbY, decays 0.15s
```

## Related Files (create)
- `src/data/hero-stats-config.js`
- `src/graphics/sprites/hero-sprite-data.js`, `effect-sprite-data.js` (slash arc 3f, hit spark 3f)
- `src/entities/player.js` (create/update), `player-renderer.js` (draw), `damage-number.js`, `visual-effect.js`
- `src/systems/combo-attack-system.js`, `hit-detection.js`, `damage-formula.js`

## Key Signatures
```js
export function createPlayer(saveData?) -> player
export function updatePlayer(player, input, state, dt) -> void   // move (collision), combo, hits, invuln
export function damagePlayer(player, rawAtk, state) -> void      // respects invuln (0.6s), spawns red number
export function drawPlayer(ctx, player, camera) -> void          // blink when invuln
export function spawnDamageNumber(state, x, y, value, color) -> void   // life 0.8s, vy -40, fade
export function spawnEffect(state, frames, x, y, opts) -> void         // {rotation, fps, flipX}
```

## Implementation Steps
1. hero-stats-config + combo state machine (pure) first; write quick test (phase 06 expands).
2. Hero pixel maps: 3 dirs x 7 frames; keep in one file (<200 lines; use helper to share head rows across frames).
3. player.js movement: normalize diagonal, facing from last input dir, moveWithCollision.
4. Wire combo events: on `hit` -> hitbox -> computeDamage -> monster.takeDamage (phase 04 stub with dummy target) -> knockback, spark, number, hitStreak++ (streakTimer 1.5s).
5. Finisher: mult += FINISHER_LEVEL_BONUS * level; bigger arc effect.
6. Y-sort render list: player + monsters + drops by feet y.

## Todo
- [ ] hero-stats-config.js, combo-attack-system.js, hit-detection.js, damage-formula.js
- [ ] hero-sprite-data.js, effect-sprite-data.js
- [ ] player.js, player-renderer.js, damage-number.js, visual-effect.js
- [ ] dummy target to verify combo in-game

## Success Criteria
- Tapping J 3x in rhythm plays hit1->hit2->hit3; pause >400ms after a hit resets to hit1.
- Each swing damages a target max once; finisher knocks back far.

## Risks
- Hero file size: share head rows via helper `composeFrame(head, body)`.
- Input mashing: queue only one press (bool), not a counter.

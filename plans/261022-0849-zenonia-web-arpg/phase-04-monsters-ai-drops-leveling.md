# Phase 04 - Monsters, AI, Spawner, Drops, Leveling

## Context
- Prev: [phase-03](phase-03-player-combo-hit-detection.md). Next: [phase-05](phase-05-skills-hud-save-death-win.md)

## Overview
Priority P1 | Status pending | 6h
Data-driven monsters with pure AI state machine, zone spawner with respawn, EXP/gold/potion drops with auto pickup, pure leveling system.

## Data Shapes
```js
// data/monster-definitions.js
export const MONSTER_DEFS = {
  slime: { id: 'slime', name: 'Slime', sprite: 'slime', paletteSwap: null, w: 30, h: 21,
    hp: 30, atk: 6, def: 1, spd: 60, aggroRadius: 200, leashRadius: 400,
    attackRange: 36, windup: 0.45, attackCooldown: 1.2, attackType: 'melee',  // 'melee'|'ranged'|'boss'
    projectile: null,          // ranged: { speed: 220, sprite: 'bone', mult: 1 }
    exp: 8, gold: [1, 3], drops: [{ item: 'hp-potion', chance: 0.12 }, { item: 'mp-potion', chance: 0.08 }],
    perLevel: { hp: 0.15, atk: 0.12, def: 0.1, exp: 0.2 },   // multiplier growth per level above 1
    isBoss: false },
  // bat (fast, low hp), goblin, skeleton (ranged bone throw), fire-slime (slime + paletteSwap {g:'r'}),
  // goblin-warrior, skeleton-knight, stone-golem (boss: hp 1500, slam radius 110, ring of 8 rock projectiles every 3rd attack)
}
// map spawnZones (phase 02) add levelRange: [min, max]
```
Monster instance:
```js
{ def, level, x, y, w, h, hp, maxHp, atk, def, homeX, homeY, ai: { state, timer, wanderDir, cooldown },
  kbX, kbY, kbTimer, flash, lastSwingId, dead, facing, anim }
```

## Monster AI (pure, systems/monster-ai-system.js)
States: `idle -> wander -> idle`, `chase`, `windup`, `attack`, `recover`, `return`, `hurt`, `dead`.
```js
export function updateMonsterAI(monster, player, dt, rng = Math.random) -> { moveX, moveY, attack: null | { type, x, y } }
```
Rules: idle 1-2s -> wander random dir 1s at 50% spd. dist(player) < aggroRadius -> chase. dist < attackRange -> windup (telegraph: flash/shake) -> attack event -> recover (attackCooldown). dist(home) > leashRadius -> return (heal to full on arrival, ignore player). Taking damage -> hurt 0.25s (no move except knockback) then chase. Caller applies move with collision and resolves attack event (melee: rect overlap check vs player; ranged: spawn projectile; boss: slam AoE + ring).

## Leveling (pure, systems/leveling-system.js)
```js
export function expToNext(level) -> Math.floor(50 * Math.pow(level, 1.5))
export function computeStats(level, base = HERO_BASE, growth = HERO_GROWTH, vitalityPct = 0) -> stats
// maxHp = floor((base + growth*(lv-1)) * (1 + vitalityPct)), others floor(base + growth*(lv-1))
export function addExp(progress: {level, exp, skillPoints}, amount, maxLevel = 30) -> { level, exp, skillPoints, levelsGained }
// loops multi-level ups; +1 SP per level; at max level exp capped at 0
export function applyDeathPenalty(progress, pct = 0.1) -> progress   // exp -= floor(expToNext*pct), min 0, never de-level
```
On levelsGained > 0: recompute stats, full heal HP/MP, banner "LEVEL UP!", golden effect.

## Spawner & Drops
```js
// world/monster-spawner.js
export function initSpawner(mapDef) -> spawnerState   // { zones: [{ zone, alive: Set, timer }] }, fill each zone to max
export function updateSpawner(spawnerState, state, dt, rng) -> void  // respawn after respawnSec; >4 tiles from player; non-solid tile
export function createMonster(defId, level, x, y) -> monster          // entities/monster.js
// entities/item-drop.js
export function rollDrops(def, x, y, rng) -> drop[]   // drop: { type: 'hp-potion'|'mp-potion'|'gold', value, x, y, life: 20 }
export function updateDrops(state, dt) -> void        // pickup radius 32px; potion = 30% max; gold adds value
// entities/projectile.js (shared with skills later)
{ x, y, vx, vy, w, h, owner: 'player'|'monster', damage, pierce, life, sprite, hitIds: Set }
export function updateProjectiles(state, dt) -> void  // dies on solid tile or life<=0
```
On monster death: addExp(def.exp * scaled), gold drop, potion roll, death puff effect, remove after 0.4s fade. Boss death sets `state.killedBoss = true`.

## Related Files (create)
- `src/data/monster-definitions.js`
- `src/graphics/sprites/monster-sprite-data.js` (slime 2f, bat 2f, goblin 2f, skeleton 2f, skeleton-knight 2f; 16x16 chibi), `boss-sprite-data.js` (golem 32x32, 2f)
- `src/entities/monster.js`, `monster-renderer.js`, `item-drop.js`, `projectile.js`
- `src/systems/monster-ai-system.js`, `leveling-system.js`
- `src/world/monster-spawner.js`

## Implementation Steps
1. leveling-system.js (pure) + monster-definitions.
2. Monster sprites; palette swap support in sprite factory (`buildSprite(rows, {...PALETTE, ...swapMap})`).
3. monster.js: createMonster (scaled stats), takeDamage(monster, dmg, fromX, fromY, force, swingId).
4. AI state machine; integrate with collision + player damage.
5. Projectiles (bone throw, boss ring); spawner; drops + pickup.
6. Kill -> EXP/level-up flow end-to-end in Meadow.

## Todo
- [ ] leveling-system.js, monster-definitions.js
- [ ] monster + boss sprite data, palette swap
- [ ] monster.js, monster-renderer.js, monster-ai-system.js
- [ ] projectile.js, item-drop.js, monster-spawner.js
- [ ] boss slam + ring attack

## Success Criteria
- Monsters wander, chase, telegraph, attack, leash home. Killing grants EXP; level-up at 50 EXP (Lv1->2).
- Potions/gold auto-pickup; zones respawn.

## Risks
- AI jitter at range edge -> hysteresis (exit chase at aggroRadius*1.2).
- Perf with many monsters -> cap ~25 per map; distance check squared.

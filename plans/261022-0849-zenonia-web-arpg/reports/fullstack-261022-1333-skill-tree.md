# Zenonia-style skill tree implementation

## Summary
15-node skill tree (3 branches sword/body/spirit x 3 tiers, 5 levels each, prereq-gated), 3-slot
hotbar (K/L/I), data-driven skill casting, passive stat aggregation, crit/combo/buff combat hooks,
new tree UI with real icons. 80/80 unit tests pass, browser smoke test `errors []`.

## Files changed
- `src/data/skill-definitions.js` (rewrite) - 15 nodes, `requires`, `effect`/`passive` shapes, `SKILL_BRANCHES`, `HOTBAR_SLOT_KEYS`; kept `SKILL_DEFINITIONS/SKILL_ORDER/SKILL_HOTKEY_ACTIONS/getSkillValue` exports (semantics of `SKILL_HOTKEY_ACTIONS` changed: action -> hotbar slot index, not skillId).
- `src/systems/skill-system.js` - prerequisite check in `canUpgradeSkill` (reason "Requires X Lv N"), `computePassiveModifiers()`, `bindSkillToSlot()`, hotbar auto-bind in `upgradeSkill()`. Removed `vitalityHpMultiplier` (superseded).
- `src/systems/player-damage.js` (new) - `heroAttack()` (Battle Cry buff), `rollPlayerDamage()` (Crit Eye roll, x1.6), shared by combat-system + skill-cast-system.
- `src/systems/combat-system.js` - `performComboHit` applies `comboMult` (Power Strike); all player-sourced hits (combo/radial/projectile/dash) roll crit via `rollPlayerDamage`; new `performSkillStrike()` for Heavy Blow.
- `src/systems/skill-cast-system.js` (rewrite) - fully data-driven dispatcher keyed by `effect.kind` (projectile/radial/strike/dash/heal/buff), one handler per kind shared by every skill using it (122 lines, no split needed).
- `src/entities/skill-pulse.js` (new) - generic repeating timer entity for multi-hit skills (Blade Storm).
- `src/entities/player.js` - `hotbar` (3 slots), `buffs {atkMult, timer}`, `tickBuffs()`, `passiveMods` cached by `recomputeStats()` (hpMult/mpMult/defAdd/speedMult), save/load hotbar.
- `src/game/player-controller.js` - K/L/I cast `player.hotbar[slot]`; regen scaled by `regenMult`/`mpRegenMult`; ticks buffs.
- `src/game/world-state.js`, `src/game/world-update.js` - added `world.skillPulses` list (ticked/cleaned like effects/particles).
- `src/save/save-serializer.js` - sanitizes `hotbar` (only known, learned active skill ids survive load).
- `src/graphics/asset-atlas.js` - 15 icon entries (24x24 PNGs from `assets/ninja-adventure/icons/`).
- `src/graphics/asset-loader.js` - `encodeURI()` on image src (icon paths contain spaces/`&`).
- `src/graphics/sprite-registry.js` - `icons[id] = images[def.icon]` directly (no slicing needed); dropped `SKILL_ICON_ROWS` import.
- `src/graphics/sprites/effect-sprite-data.js` - removed unused `SKILL_ICON_ROWS` (icons are now real PNGs).
- `src/ui/skill-tree-panel.js` (new, replaces `src/ui/skill-panel.js`) - 3-column x 3-tier grid, prerequisite connectors, node states (locked/available/learned/maxed), K/L/I hotbar bind, mouse click-to-select/click-again-to-upgrade, info box. 155 lines.
- `src/ui/skill-tree-layout.js` (new) - pure layout (node positions, nav index) + formatting helpers, kept separate to stay under 200 lines and stay unit-testable.
- `src/ui/hud-renderer.js` - hotbar slots read `player.hotbar` + `HOTBAR_SLOT_KEYS` instead of the old fixed skill map.
- `src/game/game-controller.js` - wires `SkillTreePanel` instead of `SkillPanel`.
- `tests/skill-system.test.js` (rewrite) - prerequisite gating, hotbar auto-bind/bindSkillToSlot, `computePassiveModifiers` (single + multi-stat + multi-skill).
- `tests/skill-tree.test.js` (new) - data sanity (15 nodes, 5/branch, prereqs reference real skills, icons registered in `ASSET_FILES`, effect.kind validity, passive array lengths), prerequisite gating (Blade Storm dual prereq), passive aggregation, hotbar cast integration.
- `tools/browser-check.mjs` - extended: opens tree (Tab) -> `06-skill-tree.png`, upgrades selected node (Enter) -> `07-skill-tree-upgraded.png`, closes (Tab), casts hotbar slot 0 (K) -> `08-hotbar-cast.png`; renumbered death screenshot to `09-death.png` to stay chronological.
- `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/code-standards.md`, `docs/project-roadmap.md`, `README.md` - updated to describe the new tree/hotbar/data shapes.
- Deleted `src/ui/skill-panel.js`.

## How it works
- **Branches/tiers**: `SKILL_DEFINITIONS[id].branch` (sword/body/spirit) + `.tier` (1-3). `SKILL_ORDER` lists tier1 pair, tier2 pair, tier3 single per branch -- this order drives the tree's visual sub-column layout in `skill-tree-layout.js`.
- **Prerequisites**: `requires: [{skill, level}]`. `canUpgradeSkill` checks them before the hero-level gate.
- **Passives**: `passive: {statName: [v1..v5]}` (bonus values, not absolutes). `computePassiveModifiers` sums every learned passive into one object; `player.recomputeStats()` caches it as `player.passiveMods` and applies hpMult/mpMult/defAdd/speedMult to stats.
- **Actives**: `effect: {kind, ...}`. `skill-cast-system.js` has exactly one handler per kind (`projectile/radial/strike/dash/heal/buff`); adding a new skill that reuses a kind needs zero new cast-system code.
- **Hotbar**: `player.hotbar = [null,null,null]`. Learning an active auto-binds the first empty slot; `bindSkillToSlot`/K-L-I in the tree panel rebind. `SKILL_HOTKEY_ACTIONS` now maps action name -> slot index (0/1/2), not skillId.
- **Combat hooks**: `heroAttack()` applies Battle Cry's `atkMult`; `rollPlayerDamage()` rolls Crit Eye's `critChance` (x1.6, yellow `#fff44d` text) for every player-sourced hit (combo, radial, strike, dash, and on-impact for player projectiles); combo hits additionally get `(1 + comboMult)` from Power Strike.
- **Multi-hit skills** (Blade Storm): first pulse fires immediately, remaining pulses scheduled via `SkillPulse` (generic timer entity) pushed to `world.skillPulses`, ticked in `world-update.js` alongside effects/particles/texts.

## Tests
- `node --test tests/*.test.js` -> 80/80 pass.
- `node tools/browser-check.mjs tools/screenshots` -> `errors []` (ran 4x for stability, consistent `mp` deduction / hotbar bind confirmed except one cold-start flake where Chrome launch overhead shifted timing -- re-ran clean).
- `node --check` on every `src/**/*.js` file: clean.
- `wc -l` on every `src/**/*.js` file: all < 200 (largest: `combat-system.js` 174, `game-controller.js` 165, `player-controller.js` 158, `skill-tree-panel.js` 155).
- Screenshots reviewed visually: `06-skill-tree.png` (fresh tree, headers no longer overlap "Skill Points" after a layout fix), `07-skill-tree-upgraded.png` (post-upgrade state, info box updates, bound slot shown), `08-hotbar-cast.png` (HUD hotbar icon + cooldown overlay + MP cost, in-world "Slash Wave!" text + hit).

## Usage
- Open tree: Tab/T. Arrows: Left/Right branch, Up/Down node. Enter: upgrade. K/L/I: bind selected active skill to that hotbar slot. Mouse: click to select, click again to upgrade.
- Cast: K/L/I in normal play casts whatever is bound to that slot (empty slot = no-op).
- New skill checklist is in `docs/code-standards.md` -> "Adding a New Skill".

## Unresolved / judgment calls
- Removed `vitalityHpMultiplier` export entirely rather than keeping a back-compat wrapper (task said "keep working OR replace its uses") -- cleaner since `computePassiveModifiers` generalizes it; only `player.js` and the old test imported it, both updated.
- Numeric balance (mpCost/cooldownMs/damageMult for the 8 new skills, tier-1 passive `requiredLevel` arrays) is my design within the spec's constraints ("Keep current numbers for slashWave/whirlwind/dashStrike/vitality") -- not separately play-tested for balance, only for correctness/wiring.
- Did not implement inventory/equipment per instructions (later task).

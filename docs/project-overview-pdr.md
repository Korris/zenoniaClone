# Zenonia Web ARPG — Project Overview & PDR

## What Is It?

A browser-based 2D top-down action RPG in the style of Zenonia: chibi characters, pixel art, real-time hack-and-slash combat with a 3-hit combo system, character progression via leveling and skill trees, and 4 interconnected maps with portal transitions.

**Tech Stack:** Vanilla HTML5 Canvas + ES modules, no frameworks or npm dependencies. Fixed 60Hz timestep logic; rendering once per animation frame. Runs in any modern browser via HTTP server (ES modules require http:// not file://). **Art Source:** CC0 "Ninja Adventure" asset pack by pixel-boy (NPCs/monsters/boss) + custom AI-generated hero sprites (24×24px, 8 animations, 32-color palette).

## Core Features

| Feature | Details |
|---------|---------|
| **3-Hit Combo** | Timed button presses chain attacks. 3rd hit is a powerful finisher that scales with hero level. Knockback on each hit. |
| **Combat** | Attack range hitbox; melee & ranged monsters; projectiles; hit-stop on impact. |
| **Leveling & EXP** | Kill monsters → gain EXP → level up. Max level 30. Stat growth formula. Death incurs 10% EXP penalty. |
| **Skill Tree** | 4 skills (Slash Wave, Whirlwind, Dash Strike, Vitality). Each has 5 levels; requires hero level gates. Active skills cost MP; passives boost HP. |
| **NPCs & Quests** | 6 NPCs across 4 maps. 9 quests with kill/reach objectives, prerequisites, level gates, gold/EXP rewards. Quest log (Q), tracker HUD, dialog system. |
| **4 Maps** | Verdant Meadow (start) → Dark Cave → Scorched Desert → Ruined Castle (boss). Portal travel gated by level recommendations. |
| **Boss** | Ancient Golem in Ruined Castle. 1500 HP, projectile attacks. Victory screen after defeat. |
| **Save/Load** | localStorage auto-save. Player level/EXP/skills/position/map state persisted. Manual save on pause menu. |
| **Resources** | HP (damage taken, potions restore). MP (spent on skills). HP/MP regenerate slowly out of combat. |

## Controls

| Action | Control | Context |
|--------|---------|---------|
| **Move** | WASD | While playing |
| **Attack** | Space or J | Tap repeatedly for combo |
| **Interact** | E or Enter | Talk to NPC, accept/turn in quests |
| **Skill 1** | K | Slash Wave (active) |
| **Skill 2** | L | Whirlwind (active) |
| **Skill 3** | I | Dash Strike (active) |
| **Quest Log** | Q | View active quests & tracker |
| **Skills Panel** | TAB | Open skill tree / upgrades |
| **Pause** | P | Pause menu (save/resume/quit) |
| **Menu Nav** | Arrow keys / Enter | Title & pause screens |

## Non-Goals

- **No backend:** All state in browser (localStorage only).
- **No audio:** Silent experience.
- **No mobile/touch:** Designed for keyboard + mouse.
- **No equipment system:** Stats from level + skills only.
- **No PvP:** Single-player campaign only.

## Success Criteria (v0.1 & v0.2)

- Play from Verdant Meadow → Ruined Castle boss kill.
- 3-hit combo works with 400ms chain window.
- Level up grants 1 skill point per level.
- All 4 skills upgradable to level 5.
- 6 NPCs with idle dialogue; 9 quests with prerequisites and level gates.
- Quest log (Q) shows active/completed quests; HUD tracker shows progress.
- Procedural art: terrain auto-tiling, 11 multi-tile props, hero/NPC sprite palette remaps.
- Save/load restores hero position, stats, quest state, and map state.
- Boss defeat shows victory screen; hero can continue playing.
- Collision, camera follow, sprite animation all visually solid.

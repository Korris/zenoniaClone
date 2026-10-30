# Phase 10 - Cleric skills (Heal redone)

## Context links
- Recipe: phase 07. Data `SkillTable.Cleric.cs`; preview `skill-vfx-preview/cleric.png`; `HeroCompanion.cs` (spirit stays passive); `World/ZoneHeroes.cs`

## Overview
Priority P2 | after 07 | first ally targeting (heal lowest ally, party buffs/shields), ground heal Zone, damage+heal combo.

## Key insights
- Allies = other heroes in the zone within radius until the MMO party system exists (MMO plan phase 06); later filter by party. PvP maps (MMO phase 07) must exclude enemies.
- Heal amount = % of caster ATK (design), not max HP like the old 38% heal; old heal refused casts at full HP -> new Heal always castable (allies) but refused if no one in range is hurt (no cost).
- Shields from Guardian Spirit and Mana Shield share `HeroBuffs` absorb (take the larger, no stacking).
- Purification "cleanse": heroes have no debuffs yet -> damage only (YAGNI), keep the flag in data.
- Spirit companion can play a short "cast" bob on cleric skills (visual only, optional).

## Requirements / skills
Heal, Spirit Blessing, Holy Ray, Divine Light, Sanctuary (8 s zone), Guardian Spirit (shield), Purification, Divine Ascension.

## Related code files
Modify: `SkillTable.Cleric.cs`; `Skills/SkillSupport.cs` (ally pick, heal numbers green via `DamageNumber`), `Skills/HeroBuffs.cs` (shield bubble visual).
Online: heals change remote heroes' HP -> already replicated by snapshots (HpPct) and SelfState; add heal number for viewers via SkillCast `Other` = healed amount of the main target.
Assets: `Sprites/skills/cleric/**`, `Data/skills/cleric-*.json`, `Audio/sfx/skills/cleric/**`.

## Implementation steps
Wire art/sfx -> record/review -> party test with `--party`-style debug heroes / 2 online clients (heal, blessing, shield reach the other hero) -> tune heal numbers vs monster DPS.

## Todo
- [ ] wire art/sfx  - [ ] recordings approved  - [ ] ally tests offline + online  - [ ] tuning

## Success criteria
Approved recordings; ally heals/buffs/shields verified on a second hero online; heal never exceeds max HP; shield absorb visible.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Sanctuary heal-over-time makes solo play trivial | M x M | balance phase, heal % of ATK not of max HP |
| Buffs on remote heroes invisible to them | M x L | SelfState carries buff mask; HUD buff icons (small) |

## Security
Server chooses ally targets; heal capped at max HP; no cross-zone effects.

## Next steps
Phase 11.

## Unresolved questions
1. Should cleric ally skills affect non-party players before the party system exists? Default: yes (all heroes in radius).

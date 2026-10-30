# Phase 09 - Assassin skills (Shadow Step redone)

## Context links
- Recipe: phase 07. Data `SkillTable.Assassin.cs`; preview `skill-vfx-preview/assassin.png`
- Old Shadow Step logic (removed in phase 04, git history `HeroController.ClassSkills.cs`) for the blink-behind + no-room fallback rules

## Overview
Priority P2 | after 07 | first Blink + NearestEnemy targeting, multi-dash, Summon (clone), Mark with charges, Poison.

## Key insights
- Shadow Step vs boss (design note): land at the nearest free rear/side spot OUTSIDE the boss collision box; try behind, then left/right side, then front; refuse (no cost) if none.
- Death Mark = monster Mark status with 3 charges consumed in `SkillHitHooks` by ANY hero's hit (party-friendly) - or only the caster's? (question).
- Shadow Clone reuses `SkillMinion` with the hero's own sheet tinted purple at 60% alpha (no new art beyond the shimmer loop).

## Requirements / skills
Shadow Strike, Shadow Step (blink + 260%, +30% from behind), Blade Dance, Phantom Rush (3 dashes), Venom Daggers (poison 10 s), Shadow Clone (8 s, 50%), Death Mark (+50% next 3 hits), Nightfall Execution (8 x125% r3).

## Related code files
Modify: `SkillTable.Assassin.cs`; `Skills/SkillTargeting.cs` (boss-side landing search), `Skills/SkillMinion.cs` (clone visual mode).
Assets: `Sprites/skills/assassin/**`, `Data/skills/assassin-*.json`, `Audio/sfx/skills/assassin/**`.

## Implementation steps
Wire art/sfx -> record/review -> boss landing tests (smoke: blink onto Frostfang King from 4 sides, never inside box) -> online check -> tune.

## Todo
- [ ] wire art/sfx  - [ ] recordings approved  - [ ] boss blink tests  - [ ] online check  - [ ] tuning

## Success criteria
Approved recordings; blink never lands in solids or inside a monster box; poison/mark visible on clients (status code).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Burst too high with crit + mark + behind bonus | H x M | multiplicative stack capped (x2.5 total) in `SkillDamage`; balance phase |

## Security
Server picks the blink target; client cannot choose it.

## Next steps
Phase 10.

## Unresolved questions
1. Death Mark charges: consumed by any hero's hits or only the assassin's? Default: only the caster's.

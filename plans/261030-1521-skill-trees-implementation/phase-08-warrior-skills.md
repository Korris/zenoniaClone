# Phase 08 - Warrior skills

## Context links
- Recipe: phase 07 steps 1-4. Data `Rpg/Skills/SkillTable.Warrior.cs`; preview `skill-vfx-preview/warrior.png`; legacy `EarthBreakUltimate.cs` (kept as is)
- Combo/melee art pipeline untouched (`ComboSystem.cs`, melee sheets)

## Overview
Priority P2 | after 07 | first use of Motion (Dash, Leap), attached Zone (Blade Cyclone) and self Buff with attack speed (Berserker).

## Key insights
- Charging Strike / Inferno Crash move the hero -> non-predicted (client waits for server SkillCast, reconciles with MoveLeft). Verify feel at 100 ms RTT.
- Berserker attack speed scales combo dt in HeroController (phase 04 hook) - verify the 4-hit combo + sword ribbon still sync with the faster swing.
- Earth Break keeps its 2.5 s timeline; only MP/CD/mult come from the table.

## Requirements / skills
Power Slash, Whirlwind, Earth Break (legacy), Flame Cleave (+Burn), Charging Strike (dash line), Blade Cyclone (moving zone), Inferno Crash (leap + burn), Berserker Awakening (+25% ATK, +15% atk speed 12 s, red aura).

## Related code files
Modify: `SkillTable.Warrior.cs`; possibly `Skills/SkillMotion.cs` (leap arc visual), `Skills/HeroBuffs.cs` (aura hook) - lane-exclusive while this phase runs.
Assets: `Sprites/skills/warrior/**`, `Data/skills/warrior-*.json`, `Audio/sfx/skills/warrior/**`.

## Implementation steps
1. Wire art/sfx; 2. record 8 skills, review; 3. online check incl. dash/leap rubber-band at 100/200 ms RTT (`Tools/net-lag-matrix.ps1`); 4. tune.

## Todo
- [ ] wire art/sfx  - [ ] recordings approved  - [ ] online motion check  - [ ] tuning

## Success criteria
User approves recordings; no hero ends inside solids after dash/leap (smoke test casts both against cliffs); FrostSmokeTest green.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Leap lands in river/solid | M x H | landing point = last free point along the path (sweep), same as Blink check |
| Rubber-band on dash online | M x M | short cast lock, MoveLeft in OwnerSim; fall back to "predict motion" flag if felt bad |

## Security
Motion distance from SkillDef only; server clamps.

## Next steps
Phase 09.

## Unresolved questions
None beyond phase 01/04.

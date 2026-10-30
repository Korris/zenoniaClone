# Phase 07 - PvP maps / arena

## Context links
- P1 attribution (`TakeHit` src), `SkillDamage.cs`, `ComboProjectile.cs`, `HeroController.TakeDamage`; P2 prediction; P4 MapDef/zones

## Overview
Priority P3, pending, 12h. PvP flag per map (`MapDef.Pvp`): a small arena zone reachable by portal from Oakhaven; heroes damage heroes, no EXP loss/drops on PvP death, kill scoreboard.

## Key insights
- Damage loops only scan monsters; add `IDamageable` query (Monster, HeroController) enabled only when `zone.Map.Pvp` (co-op maps untouched).
- Fairness: rewind hero positions to attacker view time (<= 200 ms) for hero targets.
- Gear gaps (+15) -> optional normalized stats on arena maps.

## Requirements
- `MapDef.Pvp`, `MapDef.NormalizeStats`; party members never damage each other; events `PvpKill{killer, victim}`; scoreboard per zone session.

## Related code files
Create: `Scripts/IDamageable.cs`, `Scripts/Net/Server/HeroPositionHistory.cs`, `Scripts/World/ArenaMap.cs`, `Scripts/Net/Client/PvpScoreboardGui.cs`
Modify: `HeroController.Combat.cs`, `ComboProjectile.cs`, `SkillDamage.cs`, `HeroController.ClassSkills.cs` (Shadow Step targets), `Net/Protocol/NetEventTypes.cs`, `MapDef.cs`

## Implementation steps
1. IDamageable + PvP filter.
2. Position history + rewind.
3. Arena map + portal + respawn points + scoreboard.
4. Bot duel harness (dotnet bots with PvP brain) + balance pass.

## Todo
- [ ] IDamageable
- [ ] Rewind
- [ ] Arena map + scoreboard
- [ ] Harness + balance

## Success criteria
- 1000 bot duels, 0 exceptions; co-op maps: friendly fire impossible (test).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Co-op regression | M x H | flag default off, smoke test |
| High-ping advantage | M x M | rewind cap 200 ms |
| Balance (heal, blink crit) | H x M | PvP multipliers |

## Security
Server-only hit resolution; input rate caps.

## Next steps
Guild vs guild events later (YAGNI).

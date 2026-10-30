# Phase 11 - Mechanic skills (first skills ever) + Overcharge passive

## Context links
- Recipe: phase 07. Data `SkillTable.Mechanic.cs`; preview `skill-vfx-preview/mechanic.png`; `HeroCompanion.cs` (robot stays passive), `ClassPalette.MechanicBlue` (C toggles orange/blue)

## Overview
Priority P2 | after 07 | first homing projectiles, multi-target Mark, Summon drone, Overcharge passive. Mechanic had no L skill: magazine/legacy code paths with `SkillName == null` disappear here.

## Key insights
- Robot Assault: the passive companion stays; the skill spawns a SEPARATE attack drone (`SkillMinion`) for 8 s (preview panel 6 shows a second, bigger robot). Option: temporarily hide the passive robot while the drone is out (question).
- Preview colours mix orange (Power Shot, Rockets, Punch, Orbital) and blue (Plasma Barrage, Robot, EMP) - keep per-skill art colours, ignore the C toggle for skills (C stays basic-attack only).
- Overcharge: energy 0-100, server-side in `HeroSkills`; +4 per basic-attack hit landed, +2 per skill hit (via `SkillHitHooks` from `Monster.TakeHit`); at 100 the next skill deals +20% and resets. Replicated to owner in `SelfStateMsg.Energy` (phase 04 field); HUD shows a thin energy bar under MP for mechanics.

## Requirements / skills
Power Shot (pierce), Homing Rockets (3 homing), Plasma Barrage (6-bolt cone), Target Lock (mark 5, +15%), Rocket Punch (pierce wide + knockback), Robot Assault (drone 8 s), EMP Burst (stun), Orbital Cannon (6 strikes).

## Related code files
Modify: `SkillTable.Mechanic.cs`; `Skills/SkillBolt.cs` (homing turn rate, retarget on death), `Skills/HeroSkills.cs` (energy), `SkillHitHooks.cs` (energy gain), `RpgHud.cs` (energy bar), `Skills/SkillMinion.cs` (drone mode).
Assets: `Sprites/skills/mechanic/**`, `Data/skills/mechanic-*.json`, `Audio/sfx/skills/mechanic/**`.

## Implementation steps
Wire art/sfx -> homing tests (target dies mid-flight -> retarget nearest within r6 else fly straight and expire) -> Overcharge HUD + rules -> record/review -> online check -> tune.

## Todo
- [ ] wire art/sfx  - [ ] homing + retarget  - [ ] Overcharge (server + HUD)  - [ ] recordings approved  - [ ] online check  - [ ] tuning

## Success criteria
Approved recordings; Overcharge bonus applied exactly once per full bar (dotnet test on the pure energy rule in `Rpg/Skills/OverchargeRules.cs`).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Homing visuals on clients chase different targets than server | M x L | clients home onto the nearest proxy; numbers from server events |
| Barrage 6 bolts x many players = many SkillBolts on server | L x M | bolts are cheap structs-in-MonoBehaviour; cap per hero |

## Security
Energy is server state only; client value is display.

## Next steps
Phase 12.

## Unresolved questions
1. Overcharge energy gain values (default +4 basic hit, +2 skill hit) - design only says "100 energy".
2. Hide the passive robot while Robot Assault drone is active?

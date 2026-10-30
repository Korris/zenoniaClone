# Phase 03 - Hero touch points (the ONLY phase editing netcode-owned files)

## Context links
- Gate: skill plan `plans/261030-1521-skill-trees-implementation/phase-04-hero-netcode-integration.md` merged + online netcode agent merged.
- Line refs from 2026-10-30 (re-verify after rebase): `HeroController.Combat.cs:39-40` (melee crit roll x1.6), `:92-95` (`TakeDamage`,
  p04 adds `dmg = skills.Buffs.Absorb(dmg)`), `HeroController.SimState.cs` (p04: `castTotal` from SkillDef lock).

## Overview
P2 | after 02 | 3 surgical edits, ~6 lines total, single commit `feat: second-weapon hero hooks`.

## Touch points (exhaustive)
| File | Change | ~Lines |
|---|---|---|
| `HeroController.Combat.cs` melee loop | `bool crit = Random.value < CritChance + CritBonus; ... mult * (crit ? 1.6f : 1f)` -> `HeroCrit.Roll(this, CritChance + CritBonus, false, out float cm)`; `mult * cm` | 2 |
| `HeroController.Combat.cs` TakeDamage | p04 line `dmg = skills.Buffs.Absorb(dmg)` -> `dmg = SecondWeaponHooks.Incoming(this, dmg)` (DR then Absorb) | 1 |
| `HeroController.SimState.cs` | p04 `castTotal = def.LockMs` -> `SecondWeaponHooks.LockMs(rpg, def)` so replay uses the reduced lock | 1 |
Never touched: `ComboSystem.cs`, `ComboProjectile*.cs`, `Net/**`, `HeroController.Net.cs`, `.Predicted.cs`, `.Skill.cs`, `.ClassSkills.cs`.

## Key insights
- `Net/**` needs nothing: `InvOp.Enhance` takes any enhanceable uid; `ServerInventoryActions.LookKey` already includes offhand uid+enhance;
  `NetEventBus.Appearance` packs offhand enhance in `EnhanceBand >> 4`; RpgState JSON mirrors full equipment to the owner.
- Mechanic/wizard/cleric ranged combo crits (`ComboProjectile`) keep x1.6: crit damage is an assassin stat and assassin is melee.

## Requirements
Compile-identical behaviour for +0 offhand (HeroCrit at CritDmg 0 = x1.6; DR 0 = identity).

## Implementation steps
1. Rebase; grep the 3 sites; update this table if moved.
2. Apply edits; Unity compile; `dotnet test Tests/dotnet/ZenoniaNet.Tests` (no protocol change expected -> no Version bump).
3. `Tools/net-local-test.ps1` host + 1 client, `--bot fight`: corrections/min within baseline.

## Todo
- [ ] rebase + verify sites
- [ ] 3 edits + compile
- [ ] net tests + local host/client run

## Success criteria
Diff touches exactly the 3 sites; NetStats corrections/min <= p04 baseline +10%.

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Netcode agent reshaped Combat/SimState | M x M | gate + step 1; logic lives in Skills/ so only call sites move |
| p04 did not add castTotal in SimState | M x L | then put LockMs in HeroSkills only (phase 02) and skip row 3 |

## Security
No new inputs; server authoritative.

## Rollback
Revert this one commit; phase 02 hooks become unused (still compile).

## Next steps
04 combos.

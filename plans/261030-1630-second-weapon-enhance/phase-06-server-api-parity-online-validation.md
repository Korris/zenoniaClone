# Phase 06 - server-api parity + online validation

## Context links
- `server-api/src/rpg/enhance-logic.js` (attemptEnhance, findEnhanceTamper, enhanceBonus/offhandScale), `src/rpg/rpg-constants.js`
  (`ENHANCEABLE_SLOTS` incl. offhand, `OFFHAND_SCALE = 0.5`), `src/rpg/item-validation.js` (newCharacterState: EMPTY equipment),
  `src/routes/internal-routes.js:37` (`/internal/characters/:id/enhance`, idempotent reqId, ledger), tests `test/enhance-*.test.js`, `test/save-ops.test.js`
- Online plan phase 03 (`plans/261030-1150-online-mmo-world/phase-03-account-character-api-persistence.md`): `ServerInventoryActions` Enhance -> API (not wired yet)
- `World/PlayerRpg.cs:91-95` (InvOp.Enhance -> EnhanceSystem.Attempt today)

## Overview
P2 | after 01 | No new endpoint, no schema change. Mirror the one constant, add offhand tests, flag the starter-uid gap for online p03.

## Key insights
- Roll rules identical for offhand -> `attemptEnhance` already works on `starter-offhand` or any offhand uid.
- Server never computes combat stats -> no JS mirror of `SecondWeapon` table (YAGNI). Only `OFFHAND_SCALE` parity (0) for the parity test.
- Gap: API-created characters have empty equipment; zone `SaveSerializer.Restore` adds `starter-weapon`/`starter-offhand` locally. Until a
  snapshot is saved, API enhance on those uids returns `not-owned`. Same issue exists for the main weapon today. Fix belongs to online p03:
  zone flushes a save when `RpgDirty` before forwarding Enhance to the API (save-then-enhance). Starter is +0 so tamper check passes.

## Requirements
- `OFFHAND_SCALE = 0` + comment pointing at `Enhance.cs`; `enhanceBonus(x, lvl, 0) === 0`.
- vitest: offhand enhance success/fail/max-level/no-cost, reqId replay, tamper on offhand enhance raise via save.

## Architecture / files
Modify `server-api/src/rpg/rpg-constants.js`, `server-api/test/enhance-logic.test.js`, `server-api/test/enhance-api.test.js`
(offhand cases using the existing `geared()` helper with an offhand item `o1`).
Note for online p03 owner (no edit here): `Net/Server/ServerInventoryActions.cs` save-then-enhance.

## Implementation steps
1. Constant + parity tests (`enhanceBonus(3,2,0)=0`).
2. API tests for offhand (6 cases).
3. `npm test` (vitest + mongo memory server) green.
4. Online run: `Tools/net-local-test.ps1` host + 2 clients, client enhances offhand via bag: RpgState reply updates both equip slot "+N",
   other client sees glow band change (Appearance), combos fire once.

## Todo
- [ ] OFFHAND_SCALE parity
- [ ] offhand API tests
- [ ] npm test green
- [ ] host + 2 clients run

## Success criteria
`npm test` green; replayed reqId on offhand never double-applies; online run log shows 1 Enhance InvAction -> 1 RpgState.

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Online p03 wires API enhance without save-then-enhance | M x M | listed in unresolved + message to online agent |
| Old JS tests expect 0.5 offhand bonus | H x L | update the single assertion (enhance-logic.test.js:29) |

## Security
Unchanged: CSPRNG roll, ledger, idempotency, tamper detection covers offhand (all items).

## Rollback
Revert constant + tests.

## Next steps
07.

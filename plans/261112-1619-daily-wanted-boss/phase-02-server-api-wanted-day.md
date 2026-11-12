# Phase 02: server-api, persist and clamp `wantedDay`

## Context
- `server-api/src/rpg/item-validation.js` `stateSchema`: zod `z.object` **strips unknown keys** ("Listed so the strip below keeps it", see `quests`, `defeatedBossMaps`). An unlisted `wantedDay` would be silently dropped on every save, and every relog would allow a new claim.
- `services/character-service.js` `save()` runs `guard(oldState, newState)` and then writes `newState`. `commitTrade` runs the guard on each side and `moveTradeGoods` clones `side.state` (other fields kept). `mutate` (ops/enhance) works on the stored state, so the field is kept.
- `routes/internal-routes.js` `noEnhanceTamper` is the guard used by save and trade.

## Overview
Priority P1 for deploy order: this phase must be live before the zone build of phase 03. It is small and backwards compatible.

## Key insight: why there is no new endpoint
I considered a dedicated `/internal/characters/:id/wanted-claim` through `mutate` (reqId, ledger, top-level doc field) and rejected it. It needs a new queue job kind, a route, a C# HTTP method and replay handling, and the client would still need the flag somewhere. It also adds no security: the zone already writes gold/stones in every save without any gold guard. Keeping the flag in the state gives atomicity with the reward for free. A ledger/audit entry can come later if the economy needs auditing (YAGNI).

## Requirements
- `stateSchema`: `wantedDay: z.number().int().min(0).max(1_000_000).default(0)`, with a comment that mirrors `SaveData.wantedDay` and `WantedBoss.DayKey`.
- Clamp: the stored day never goes backwards. `keepWantedDay(oldState, newState)` sets `newState.wantedDay = max(old ?? 0, new ?? 0)`. It never rejects, because a rejected save spools the whole character.
- Old documents without the field read as 0 (zod default on the next save). No migration script.

## Architecture
Zone save JSON -> `saveSchema` parse (keeps wantedDay) -> guard: `noEnhanceTamper` + `keepWantedDay` -> `$set state`. Redeem -> `characters.full(doc)` -> zone gets `state.wantedDay` back.

## Related code files
- Modify: `server-api/src/rpg/item-validation.js` (+2 lines).
- Create: `server-api/src/rpg/wanted-day.js` (~10 lines, `keepWantedDay`).
- Modify: `server-api/src/routes/internal-routes.js`: compose the guard as `stateGuard = (o, n) => { noEnhanceTamper(o, n); keepWantedDay(o, n); }` for save and trade.
- Create: `server-api/test/wanted-day.test.js`.
- Modify: `server-api/src/db/indexes.js` header comment only (document the field). No index.

## Implementation steps
1. Add the schema field and `wanted-day.js`.
2. Wire the composed guard into `/save` and `/trades/commit`.
3. Tests (use `helpers.js` `makeCtx().online()`):
   - A save with `wantedDay: 20405` is stored; a redeem after `endSession` returns 20405.
   - A save with a lower value (or the field missing) keeps 20405.
   - A new character has 0.
   - A trade commit keeps each side's `wantedDay`.
   - ops/enhance keep it.
4. `cd server-api && npm test` (vitest + mongodb-memory-server).

## Todo
- [ ] schema field
- [ ] wanted-day.js + guard composition
- [ ] wanted-day.test.js, whole vitest suite green

## Success criteria
- `npm test` is green: the 118 existing tests plus the new ones.
- `GET /characters/:id` (owner) shows `state.wantedDay` after a zone save.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Zone deployed before the API (field stripped, double claims) | M x H | Deploy-order note in plan.md and `docs/deploy.md`. Phase 05 checks the persisted value through the real API. |
| A guard that mutates input surprises a later reader | L x L | Document it in the guard. It only touches one field. |
| Pending undeployed server-api work (character-flow, `docs/pending-tasks.md`) ships together | M x M | Deploy both together, after running vitest. Write this in pending-tasks. |

## Rollback
Remove the guard and the field **only after** the zone feature is reverted. Otherwise old states keep the value and new saves would strip it.

## Security
Service-key routes only; no public route changes. The value is an int between 0 and 1e6.

## Deploy note
Rebuild and restart only the `server-api` container (`docs/deploy.md` flow). No DB migration. Keep the existing SERVICE_KEY.

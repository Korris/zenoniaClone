# Phase 03 - Account/character API, persistence, server-side inventory/enhance/loot

## Context links
- `Assets/Scripts/Rpg/*` (PlayerState, Enhance `StoneUpgradeMethod.SuccessByTarget`, LootTable, ItemFactory, Leveling, SaveSerializer, RpgRandom)
- P1 `PlayerRpg`, `ServerInventoryActions`, `DevLoginStub`, Hello.token

## Overview
Priority P1, pending, 36h. New `server-api/` (Node 22 LTS, Fastify, mongodb driver, zod, jsonwebtoken, argon2) = RO login + character server.
Accounts (JWT), characters (class, map, position, stats, inventory, equipment, enhance), zone tickets, one-session lock, server-side enhance,
periodic/event saves from zone servers. Offline keeps local SaveStore.

## Key insights
- Numbers must match C#: Editor exporter writes `server-api/src/rpg-data/*.json` + golden vectors (seeded RpgRandom) -> JS parity tests (DRY).
- Enhance in API (economy-critical, transactional, ledgered). Field loot: rolled by trusted zone server in C# (per-kill REST would add latency/load) - see plan Q9; grant persisted via save.
- Character doc = `SaveData` schema + `map`, `x`, `y`, `version`, `onlineSession`.

## Requirements
Public: `POST /auth/register`, `POST /auth/login` -> {access 15 min, refresh 30 d}, `POST /auth/refresh`, `POST /auth/logout`,
`GET /characters`, `POST /characters {name, class}` (name unique), `DELETE /characters/:id`, `POST /characters/:id/enter` -> {zoneHost, zonePort, ticket(60 s, single use)}.
Internal (service key): `POST /internal/tickets/redeem {ticket}` -> character + sessionId (sets lock), `POST /internal/characters/:id/save {sessionId, version, state, map, x, y}`,
`POST /internal/characters/:id/enhance {sessionId, itemUid, reqId}`, `POST /internal/sessions/:id/end`, `GET /internal/zones` (registry, P4 fills).
Saves: every 60 s, logout, zone change, level-up, enhance (API itself), rare drop (rarity >= epic).

## Architecture
```
client -> /auth/login -> /characters -> /enter -> ticket ; client -> zone UDP Hello{ticket} -> zone -> /internal/tickets/redeem
zone PlayerRpg <- character state ; zone -> /save (optimistic version; rejected if sessionId stale)
Mongo: users{email uniq, passHash, banned, createdAt} characters{userId, name uniq, class, version, map, x, y, state, session{id, zone, at}}
       ledger{_id reqId, characterId, type, before, after, at} (enhance idempotency + audit)
```

## Related code files
Create: `server-api/package.json`, `src/app.js`, `src/routes/auth-routes.js`, `src/routes/character-routes.js`, `src/routes/internal-routes.js`,
`src/rpg/seeded-random.js`, `src/rpg/enhance-logic.js`, `src/rpg/item-validation.js`, `src/rpg-data/*.json`, `src/db/mongo-client.js`, `src/db/indexes.js`,
`src/auth/jwt-service.js`, `src/auth/ticket-service.js`, `test/*.test.js`, `.env.example`;
`Assets/Editor/RpgDataExporter.cs`; `Assets/Scripts/Net/Server/ApiClient.cs`, `CharacterSaveScheduler.cs`; `Assets/Scripts/Net/Client/LoginScreenGui.cs`, `AuthStore.cs`, `CharacterSelectGui.cs`.
Modify: `Net/Server/NetPlayer.cs`, `ZoneServer.cs` (ticket auth, AlreadyOnline), `ServerInventoryActions.cs` (Enhance -> API), `PlayerRpg.cs`, `Net/Client/ConnectScreenGui.cs` (login flow), remove `DevLoginStub.cs` from non-dev builds.
Ownership: P3 owns `server-api/**`, `NetPlayer*.cs`, `ApiClient.cs`, `PlayerRpg.cs`, `ServerInventoryActions.cs`, login/char-select GUIs.

## Implementation steps
1. Exporter + golden vectors; JSON checked in.
2. API skeleton, indexes, auth (argon2id, rate limit), characters CRUD.
3. Tickets + session lock + redeem.
4. Save endpoint with optimistic version + session check; enhance endpoint with ledger + parity tests.
5. Zone server: ApiClient (async, retry w/ backoff, never blocks tick), redeem on Hello, load PlayerRpg, spawn at saved map/x/y (fallback map spawn if blocked), CharacterSaveScheduler.
6. Graceful shutdown: save all, end sessions. Crash: session lock expires after 90 s without save heartbeat.
7. Client: login/register, token store + refresh, character select/create, enter -> connect.

## Todo
- [ ] Exporter + vectors
- [ ] Auth + characters
- [ ] Tickets + session lock
- [ ] Save + enhance (ledger)
- [ ] Zone integration + scheduler
- [ ] Shutdown/crash handling
- [ ] Client login/char select

## Success criteria
- `npm test` green incl. 100 % enhance parity; replayed enhance reqId never double-applies.
- Logging in twice -> second rejected AlreadyOnline (or first kicked; Q). Kill zone process -> relogin after lock expiry, progress loss <= 60 s.
- Offline saves untouched.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| C#/JS drift | M x H | exporter + vectors in both test suites |
| API outage | M x H | zone keeps playing, queues saves, refuses new logins; alert |
| Dupe via save race (two sessions) | L x H | session lock + version check |
| Mongo write load (200 players x 60 s) | L x M | ~3.3 writes/s/zone, fine; batch if needed |

## Security
argon2id, rate limits on auth, JWT secret/env, short single-use tickets, service key for internal routes (mTLS later), zod validation, least-privilege Mongo user, no secrets in repo.

## Next steps
P4 zone registry + handoff; P6 trade uses ledger pattern.

## Decision 2026-10-30: registered accounts only (no guest login)
- Every player registers before playing: `POST /auth/register` {username, email, password} (bcrypt hash, unique username + email, email verification optional at first), `POST /auth/login` -> JWT access token + refresh token stored on the device, `POST /auth/refresh`, `POST /auth/logout`.
- Password reset by email; login rate limit + lockout after repeated failures; registrations rate-limited per IP.
- Social login (Google / Apple / Facebook) is a later option; if any is offered on iOS, Sign in with Apple is required too.
- The client remembers the session (refresh token), so players only type the password on a new device.

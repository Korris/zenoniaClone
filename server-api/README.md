# server-api

Account / character API for the Zenonia online world (phase 3). Node 20.12+ (22 LTS in Docker), ESM JavaScript, Fastify, MongoDB driver, zod, bcryptjs, jsonwebtoken.

Registered accounts only (no guest login): username + email + password, JWT access token (15 min) + rotating opaque refresh token (30 d).

## Run

```bash
cd server-api
npm install
cp .env.example .env        # then edit JWT_SECRET / SERVICE_KEY. NEVER commit .env
npm start                   # needs MongoDB at MONGO_URI
npm test                    # in-memory MongoDB (mongodb-memory-server), no Docker needed
```

Docker (API + Mongo): `docker compose up --build` (dev-only default secrets; override via env / untracked `.env`). API on http://localhost:3000, `GET /health`.
Tests against a real Mongo instead: `TEST_MONGO_URI=mongodb://localhost:27017 npm test` (each test file uses its own throw-away database).

## Environment

See `.env.example`. Required: `MONGO_URI`, `JWT_SECRET` (>= 32 chars), `SERVICE_KEY` (>= 16 chars). The process refuses to start on invalid config.
`ZONE_ID/ZONE_HOST/ZONE_PORT` = the address handed to clients by `/characters/:id/enter` (phase 4 replaces it with a zone registry).
Behind a reverse proxy set `TRUST_PROXY=true`, otherwise IP rate limits see the proxy address.

## Conventions

- Errors: `{ "error": { "code": "snake_case", "message": "...", "details"?: [...] } }`. 400 validation_error (details = per-field), 401 unauthenticated, 403 bad service key, 404 not_found (also "not yours"), 409 conflict, 422 rule violation, 429 rate_limited (+ `Retry-After`).
- Public routes: `Authorization: Bearer <accessToken>`. Internal routes: `x-service-key: <SERVICE_KEY>` (zone servers only; keep the API's internal routes off the public internet / behind the firewall).
- Character `state` is exactly Unity `SaveData` (`Assets/Scripts/Rpg/SaveSerializer.cs`) with `Item` / `ItemStats` field names, so `JsonUtility` reads it as-is. `rev` (optimistic version, starts 0, +1 per write) is separate from `state.version` (save format, 1).

## Public endpoints

| Method + path | Body | Result |
|---|---|---|
| `POST /auth/register` | `{username 3-20 [A-Za-z0-9_], email, password 8-72 bytes}` | 201 `{user, accessToken, refreshToken, expiresIn}` (409 `username_taken` / `email_taken`) |
| `POST /auth/login` | `{login (username or email), password}` | 200 same shape; 401 `invalid_credentials`; 429 lockout / IP limit |
| `POST /auth/refresh` | `{refreshToken}` | 200 new pair (old token revoked; reuse of a revoked token revokes the whole family) |
| `POST /auth/logout` | `{refreshToken}` | `{ok}` |
| `POST /auth/password-reset/request` | `{email}` | 202 always. STUB: the reset link is logged, not mailed |
| `POST /auth/password-reset/confirm` | `{token, password}` | `{ok}`; signs out every device |
| `GET /characters` | | `{characters:[{id,name,classId,level,mapId,rev,online,createdAt}]}` |
| `POST /characters` | `{name 3-16 [A-Za-z0-9_], classId}` | 201 `{character}`; one per class (warrior, wizard, mechanic, cleric, assassin) = max 5; names unique case-insensitively |
| `GET /characters/:id` | | `{character:{..., state}}` |
| `DELETE /characters/:id` | | `{ok}`; 409 `character_online` |
| `POST /characters/:id/enter` | | `{ticket, zoneId, zoneHost, zonePort, expiresIn:60}`; 409 `already_online` |

## Internal endpoints (zone server)

| Endpoint | Purpose |
|---|---|
| `POST /internal/tickets/redeem {ticket}` | Single-use. Takes the one-session lock. -> `{sessionId, character:{id,name,classId,rev,state,...}}`. 401 `invalid_ticket`, 409 `already_online` |
| `POST /internal/characters/:id/save {sessionId, rev, state}` | Full snapshot. -> `{rev, savedAt}`. Needs the lock + current rev, refreshes the heartbeat. 409 `session_mismatch` / `rev_conflict`(details.currentRev), 422 `enhance_tamper` / `class_mismatch` |
| `POST /internal/characters/:id/ops {sessionId, rev, reqId, ops[]}` | Atomic, idempotent batch: `addItem`, `removeItem`, `equip`, `unequip`, `adjust {gold,hpPotion,mpPotion,upgradeStone deltas}`. -> `{ok, rev, state, replayed}`; 422 `inventory_full`, `wrong_class`, `insufficient_funds`, ... |
| `POST /internal/characters/:id/enhance {sessionId, rev, reqId, itemUid}` | One attempt rolled by the API. -> `{ok, success, level, reason, upgradeStone, rev, state, replayed}`. `ok:false` = refused (`not-owned`, `not-weapon`, `max-level`, `no-cost`), nothing changes |
| `POST /internal/sessions/:sessionId/end {characterId}` | Release the lock after the final save. Idempotent -> `{released}` |
| `GET /internal/zones` | Registry stub |

### Rules worth knowing

- **One session per character.** `redeem` takes the lock; a second `enter`/`redeem` gets 409 `already_online` (the first session is NOT kicked). The lock is refreshed by every save/ops/enhance and expires 90 s after the last one (crashed zone). A session that lost the lock can never write again (`session_mismatch`).
- **Enhance** uses the C# numbers (`Enhance.cs`): success by target level `[+1..+3: 100 %, 90, 85, 80, 70, 65, 60, 50, 45, 40, 30, 25, 20 %]` (+15 max), cost 1 Upgrade Stone (also spent on failure), ATK curve 10/20/35/60 % per level with a +1/level floor (`src/rpg/enhance-logic.js` ports it, incl. `enhanceBonus`). The roll is `rand() < chance` with a CSPRNG; tests inject the RNG. Idempotent per `reqId` (stored atomically with the result in the character document, last 30 ids), audited in the `ledger` collection.
- **Only the API raises enhance levels.** A saved snapshot may not carry a higher `enhance` than stored; unseen items must be +0 (`enhance_tamper`). Granted items (`ops.addItem`) must be +0.
- **Loot** is rolled by the zone server in C#; persist it with `ops`/`save`.
- Writes carry `rev`. After enhance/ops the zone must adopt the returned `rev` (and `state`) before its next save; flush pending changes with `save` BEFORE calling enhance/ops so the stored state is current.

## Rate limits (in-memory, per process)

Login/refresh/reset: `LOGIN_IP_MAX` per `LOGIN_IP_WINDOW_SEC` per IP. Account lockout: `LOGIN_MAX_FAILURES` wrong passwords -> locked `LOGIN_LOCKOUT_SEC` (stored in Mongo, survives restarts). Register: `REGISTER_IP_MAX` per `REGISTER_IP_WINDOW_SEC` per IP. Move the IP limiters to Redis/Mongo before running several API instances.

## Layout

```
src/app.js            build Fastify app (injectable db/cfg/now/rand/mailer)   src/server.js  entry
src/routes/*          auth / character / internal routes                      src/schemas.js zod request schemas
src/services/*        character persistence + session lock
src/auth/*            jwt, bcrypt, refresh tokens, tickets, rate limiter, guards
src/rpg/*             constants, item/state validation, enhance logic, inventory ops (mirror Assets/Scripts/Rpg)
test/*                vitest + mongodb-memory-server
```

Not covered yet (see plan): item template catalog export for stricter item validation, email delivery, email verification, social login, metrics.

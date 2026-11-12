# Phase 06 (DEFERRED, not approved yet): server-api multi-character + persisted look

Do not start without user approval.

## Context
`server-api/src/db/indexes.js` (unique `{userId:1, classId:1}`), `services/character-service.js` (`create`, `summary`, `class_taken`), `schemas.js` (`createCharacterSchema`), `config.js`, `test/characters.test.js` (line 37 expects `class_taken`, line 43 expects 6 classes).

## Requirements
- Drop the per-class unique index: in `ensureIndexes`, `dropIndex('userId_1_classId_1')` ignoring `IndexNotFound` (code 27) / `NamespaceNotFound` (26); add non-unique `{userId:1}` for list/count. Idempotent at every boot; old zones and clients unaffected (they never rely on it).
- Slot cap: `MAX_CHARACTERS` env, default 6 (one per class today, so no existing account exceeds it; mockup pages of 4). Create: `countDocuments({userId}) >= cap` -> 409 `slots_full`. Accounts above cap keep their heroes, only create is refused. Race of two parallel creates can exceed by 1: accepted (or guard with a per-user counter later; YAGNI).
- `createCharacterSchema`: `hairStyle` int 0..15 default 0, `hairDye` int -1..15 default -1 (preset index; see plan.md). Stored top-level `look: {hairStyle, hairDye}` NOT inside `state` (state is SaveData validated by `stateSchema` and replaced by zone saves).
- `summary` adds `hairStyle`, `hairDye` (missing -> 0 / -1). Ticket redeem response (zone side) carries `look`.
- Keep `class_taken` mapping for old indexes until dropped (harmless).

## Tests (vitest)
Two heroes same class OK; cap -> `slots_full`; look round trip in list/create/redeem; invalid look -> 400; old doc without look -> defaults; index drop idempotent (run ensureIndexes twice).

## Client follow-up
`ApiDtos` (`CreateCharacterRequest` + `CharacterSummary` fields), `ApiClient.CreateCharacterAsync(name, cls, style, dye)`, list uses server look, PlayerPrefs only as fallback (then delete `CharacterLookPrefs`), remove greyed-out classes, map `slots_full`.

## Risks
Live DB index drop: dropping a unique index cannot violate data; do it at boot, log it. Rollback: re-create index fails if an account already has 2 of a class -> rollback = leave index dropped and re-add the per-class check in code.
Deploy order: server-api first (new fields optional), then client.

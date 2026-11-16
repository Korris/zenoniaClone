# Phase 01: AccountState + lưu (offline, server-api, zone)

## Context
- `server-api/src/services/character-service.js` (mutate, recentOps, ledger), `routes/internal-routes.js`, `rpg/item-validation.js` (stateSchema), `db/indexes.js`.
- `Assets/Scripts/Net/Server/ZonePersistence.cs`, `ZoneApiHttp.cs`, `Net/Protocol/CharacterSaveQueue.cs`, `Rpg/SaveSerializer.cs`, `Rpg/SaveStore.cs`.

## Overview
Priority cao (02, 03 cần). Thêm tầng dữ liệu theo tài khoản; nhân vật giữ lựa chọn đang dùng.

## Account JSON (C# `AccountState` = JsonUtility, JS zod `accountStateSchema`) — bản gacha PET (16/11)
```json
{ "version": 1,
  "pets": [{ "id": "spirit", "stars": 1, "shards": 0 }],
  "tickets": 0, "urShards": 0,
  "pitySR": 0, "pitySSR": 0, "pityUR": 0, "totalPulls": 0,
  "history": [{ "at": 0, "id": "imp", "dup": false, "shards": 0, "pitySR": 0, "pitySSR": 0, "pityUR": 0 }],
  "starterGiven": false, "compensated": ["spirit"], "levelReward": 0,
  "rewardDay": 0, "dayWanted": false,
  "rewardWeek": 0, "weekWantedTickets": 0, "weekBossKills": 0 }
```
Giới hạn: pets <= 16 (PET_IDS, stars 1..3, shards >= 0, không trùng); tickets / urShards / totalPulls 0..MAX_INT; pitySR 0..9, pitySSR 0..29, pityUR 0..59; history <= 200; compensated PET_IDS; levelReward 0..60; weekWantedTickets 0..4; weekBossKills 0..999.

Character state (SaveData v4) thêm `petId` ('' = không). Item pet cũ vẫn được schema nhận (save cũ), zone chuyển đổi.

## server-api
- Collection `accounts` {_id: userId, rev, state, recentOps[{reqId, rev}] (giữ 30), createdAt}.
- `/internal/tickets/redeem` trả thêm `account: { rev, state }` (tạo mặc định nếu chưa có).
- `POST /internal/characters/:id/account/get` {sessionId} -> {rev, state}.
- `POST /internal/characters/:id/account/write` {sessionId, rev, reqId, state}: session phải giữ khoá nhân vật; reqId đã thấy -> {rev, replayed: true}; rev lệch -> 409 rev_conflict {currentRev}; guard chống lùi -> 422 account_tamper; ghi ledger {type:'account', userId, characterId, reqId, before/after: tickets, urShards, pitySR, pitySSR, pityUR, totalPulls}. Trả {rev}.
- Guard: pet cũ còn đủ và sao không giảm; compensated là tập cha; starterGiven không true->false; totalPulls, levelReward không giảm.
- Test vitest: tạo mặc định ở redeem, ghi/đọc, replay reqId, 409, 422 từng luật guard, schema v4 giữ 4 trường mới.

## Zone / client (C#)
- `Rpg/Account/AccountState.cs` (data + Clone + Sanitize), `AccountStore.cs` (file offline, ngoài test project).
- `Net/Server/ZoneAccountQueue.cs`: 1 hàng đợi / tài khoản, job = (reqId, Func<AccountState, ActionResult>, callback). Lỗi mạng: gửi lại cùng reqId + cùng state. 409: `account/get`, tính lại 1 lần.
- `AccountService` (static, World): `Run(hero, reqId, mutate, done)` chọn backend: offline file / zone API / RAM.
- RpgState thêm `AccountJson` (protocol 20). Client mirror gắn vào `PlayerState.Account`.

## Todo
- [ ] C# AccountState + Sanitize + Clone
- [ ] AccountStore offline + AccountService
- [ ] server-api schema, service, routes, indexes, tests
- [ ] ZoneApiHttp account calls + ZoneAccountQueue + redeem đọc account
- [ ] RpgState AccountJson

## Rủi ro
- 2 nhân vật cùng tài khoản online ở 2 zone: rev + tải lại xử lý; cache zone kia cũ tới lần ghi sau (chỉ ảnh hưởng hiển thị).
- Crash giữa vàng (nhân vật) và ghi tài khoản: trừ vàng chỉ khi tài khoản ghi xong; tệ nhất được 1 lần nâng / mua không mất vàng nếu crash trước save nhân vật (log ledger).

## Bảo mật
Chỉ zone (service key) ghi; client chỉ gửi InvOp. RNG ở zone (`RandomNumberGenerator`).

# Phase 02: Wire v35 + server-api zod cardHelp

## Context Links
- `Assets/Scripts/Net/Protocol/DuelMessages.cs`, `NetOpcodes.cs` (Version 34), `Assets/Scripts/Net/Client/DuelWire.cs`.
- `server-api/src/rpg/item-validation.js` dòng `cardHelp`, `server-api/test/card-state-schema.test.js`.
- Phase 01 (DuelView.Npc / ScriptStep, HelpMask 3).

## Overview
Priority P2. Done (code + tests, 24/11). Thêm op / cờ cho ván NPC, bump Version, nới zod.

## Key Insights
- `DuelStateMsg` flags byte còn bit 16 / 32 trống. Version 34 chưa phát hành nhưng bản test `C:/temp/player-cards-p8` đang dùng 34 -> bump 35 để exe cũ bị từ chối rõ ràng.
- Quest id là chuỗi tự do trong zod (`/^[A-Za-z0-9._-]+$/`) -> `first-card-duel` qua được, không đổi API cho quest.
- Chỉ đổi API: `cardHelp` max 1 -> 3.

## Requirements
- `DuelOp.PlayNpc = 11` (A = 0 Tutorial, 1 Practice), `DuelOp.SkipScript = 12` (thêm cuối enum).
- `DuelFail` thêm cuối: `NoQuest` (chưa nhận / đã xong quest khi xin Tutorial, chưa xong khi xin Practice), `TooFar` (xa Orin), `ScriptMove` (nước lệch kịch bản).
- `DuelStateMsg`: flag bit 16 = Npc, thêm byte `ScriptStep` cuối gói.
- `NetProtocol.Version = 35` + ghi chú.
- zod `cardHelp: max(3)`.

## Architecture
Client -> `DuelRequestMsg{Op=PlayNpc, A}` / `{Op=SkipScript}` / `Place` / `Forfeit` (dùng lại). Server -> `DuelStateMsg` (Npc, ScriptStep). `DuelWire.ToMsg / ToView` copy 2 field. Gợi ý không lên wire (client tự tính).

## Related Code Files
- Sửa: `Assets/Scripts/Net/Protocol/DuelMessages.cs`, `NetOpcodes.cs`, `Assets/Scripts/Net/Client/DuelWire.cs`, `server-api/src/rpg/item-validation.js`, `server-api/test/card-state-schema.test.js`, `Tests/dotnet/ZenoniaNet.Tests/DuelMessageRoundTripTests.cs`, `MessageRoundTripTests.cs` (nếu assert Version).
- Ownership: `Net/Protocol/Duel*.cs`, `NetOpcodes.cs`, `Net/Client/DuelWire.cs`, `server-api/**`, `Tests/dotnet/ZenoniaNet.Tests/Duel*` + round-trip.

## Implementation Steps
1. Enum + fail codes (append-only).
2. `DuelStateMsg` Write / Read: bit 16 Npc, byte ScriptStep sau trường cuối hiện tại.
3. `DuelWire` copy `Npc`, `ScriptStep`.
4. Version 35, ghi chú đầu chuỗi comment: "35 (unreleased): DuelOp PlayNpc / SkipScript (Orin tutorial + practice), DuelStateMsg Npc flag + ScriptStep, DuelFail NoQuest / TooFar / ScriptMove; SaveData cardHelp bit 2 (PvP risk confirmed)".
5. zod `cardHelp` max 3; vitest: 2 và 3 qua, 4 bị từ chối; state có quest `first-card-duel` state 3 qua.
6. ZenoniaNet: round-trip DuelStateMsg Npc + ScriptStep; DuelRequestMsg PlayNpc A=1.
7. Chạy `dotnet test Tests/dotnet/ZenoniaNet.Tests`, `cd server-api && npx vitest run`.

## Todo List
- [x] DuelOp / DuelFail append
- [x] DuelStateMsg Npc + ScriptStep
- [x] DuelWire
- [x] Version 35
- [x] zod cardHelp max 3 + vitest
- [x] ZenoniaNet round-trip

## Success Criteria
ZenoniaNet 557 + mới pass (2 FrostHollow fail sẵn có), vitest 264 + mới pass.

## Risk Assessment
Đổi thứ tự enum làm lệch PvP: chỉ append. Quên bump -> exe p8 cũ đọc sai gói: Version 35 chặn.

## Security Considerations
Server không nhận gì mới từ client ngoài op + A (0/1); giá trị A khác bị từ chối ở phase 03.

## Next Steps
Phase 03 dùng op; phase 04 dùng `DuelView.Npc/ScriptStep`.

## Làm khác kế hoạch (24/11)
- `DuelStateMsg.MaxScriptStep = 5` (gói bị từ chối nếu ScriptStep lớn hơn); cờ bit 16 = Npc nên `flags > 31` mới bị từ chối.
- Sửa 2 test PvP cũ (`DuelPvpMessageTests`): cờ bất hợp lệ giờ là bit 32, op không tồn tại giờ là 13.
- Không có test cho `DuelWire` (ZenoniaNet.Tests không biên dịch Rpg / Net/Client); chỉ test round-trip gói. ZenoniaNet 557 -> 559 pass (+2 FrostHollow fail sẵn có). vitest 264 -> 265.

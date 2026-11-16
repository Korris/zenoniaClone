# Phase 10: Wanted Boss - luật chung, tin trạng thái từ server, protocol 22, test

## Context Links
- Mockup: `chatgpt/wanted-board.png`, `wanted-notifications.png`
- Code luật chung (asmdef `Rpg`, server + test dùng): `Rpg/WantedBoss.cs` (Rotation 3 boss, DayKey, Today, NextResetUtcMs, RewardText), `Rpg/WantedSkills/WantedSkillTable.cs`, `WantedSkillDef.cs` (Shape, SafeArc, Enrage), `Rpg/PlayerState.Wanted.cs` (`WantedDay`)
- Server / zone: `World/WantedBossSpawner.cs`, `World/Zone.cs` (`WantedTimer`), `World/BossRespawnTimer.cs`, `World/WantedBossCredit.cs`, `Net/Server/ZoneServer.cs` (một tiến trình chứa mọi zone: `host.Zones`)
- Protocol: `Net/Protocol/NetOpcodes.cs` (`NetOp` server->client 10..29, `NetProtocol.Version = 21`), `NetMessageWriter.cs` / `NetMessageReader.cs`
- Client: `WantedBossTracker.cs` (so `WantedDay` để biết đã trả thưởng), `WantedBossBoard.cs` (`WantedBoardData`)
- server-api: `server-api/src/rpg/wanted-day.js` (`keepWantedDay`), test `server-api/test/wanted-day.test.js`
- Test: `Tests/dotnet/ZenoniaSkills.Tests/WantedBossTests.cs`, `WantedSkillTests.cs`; `Tests/dotnet/ZenoniaNet.Tests/MessageRoundTripTests.cs`, `MapGraphTests.cs`

## Overview
- Ưu tiên: P1 (chặn phase 11). Effort: M. Status: code + test xong 16/11 (CHƯA commit, CHƯA build server / client, CHƯA e2e trong game). Bắt đầu ngay, song song với UI (không đụng file UI).
- Bổ sung dữ liệu + một tin mạng để client biết boss **đang sống / chờ hồi sinh (giờ hồi)** ở mọi map; thêm hàm luật cho lịch, gợi ý né, đường đi.

## Key Insights
- Client hiện chỉ thấy boss khi cùng zone (proxy). Trạng thái sống / hồi sinh nằm ở `Zone.WantedTimer` phía server -> **cần tin mới** `NetOp.WantedStatus` (server->client, reliable, kênh 1): `bossType`, `alive`, `respawnAtUtcMs`. Gửi: lúc vào game, khi boss chết, khi boss hồi, khi qua ngày. Một tiến trình chứa mọi zone nên gửi cho mọi người chơi đang online dễ.
- Đã trả thưởng hay chưa: giữ cơ chế `WantedDay` trong RpgState (không đổi). Không thêm bước nhận nhiệm vụ. Không đếm người đánh (bỏ).
- Lịch hôm qua / hôm nay / ngày mai: `WantedBoss.For(DayKey ± 1)` - thuần, không cần mạng. Gợi ý level = `BossLevel` (hiện "My Lv").
- "Cách né" mỗi chiêu: thêm `Hint` (câu ngắn tiếng Anh như mockup) vào `WantedSkillDef` / bảng, sơ đồ chọn theo `Shape` + `OnPlayers` + `SafeArcCount` (không cần dữ liệu thêm).
- Dẫn đường: map đích + ô đứng có sẵn (`MapId`, `SpawnTx/Ty`). Khác map: BFS trên `MapRegistry.All` + `PortalsFor(...)` để ra cổng kế tiếp (hàm thuần, test được như `MapGraphTests`). Cổng có khoá boss (`PortalsFor(gateOpen)`) -> dùng trạng thái cổng của hero.
- server-api: **không đổi** (claim vẫn là `wantedDay`, `keepWantedDay` giữ nguyên). Nếu phase 12 duyệt personal loot thì xét lại ở đó.
- Đổi bảng skill / luật dùng chung -> phải build lại server (`Tools/build-linux-server.ps1`) theo CLAUDE.md.

## Requirements
- `WantedBoss.Schedule(utcMs)` -> 3 def (hôm qua, hôm nay, ngày mai), tôn trọng `Override`.
- `WantedSkillDef.Hint` cho mọi chiêu của 3 boss; test: không chiêu nào thiếu Hint.
- `WantedRoute.NextPortal(fromMapId, toMapId, gateOpen)` -> cổng kế tiếp hoặc null (cùng map / không có đường).
- Tin `WantedStatus` + codec + version 22; client lưu vào `WantedBossTracker.Status` (thuần dữ liệu), phase 11 đọc.
- Client cũ (21) gặp server 22: bị từ chối bằng `RejectReason.Version` (cơ chế có sẵn) - chấp nhận vì 21 chưa phát hành.

## Architecture
```
Zone(wanted) ──chết / hồi / qua ngày──> ZoneServer.BroadcastWantedStatus() ──NetOp.WantedStatus──> mọi client
Hello/Welcome xong ──> gửi WantedStatus cho người vừa vào
Client: NetClient nhận ──> WantedBossTracker.Status {bossType, alive, respawnAtUtcMs, receivedAt}
Luật thuần (Rpg): WantedBoss.Schedule, WantedSkillDef.Hint, WantedRoute.NextPortal (đặt cạnh Maps nếu cần MapRegistry)
```

## Related Code Files
- Sửa: `Rpg/WantedBoss.cs` (Schedule), `Rpg/WantedSkills/WantedSkillDef.cs` + `WantedSkillTable.cs` (Hint), `Net/Protocol/NetOpcodes.cs` (`NetOp.WantedStatus`, Version 22 + ghi chú), `NetMessageWriter.cs` / `NetMessageReader.cs` (hoặc file message mới), `Net/Server/ZoneServer.cs` (gửi), `World/Zone.cs` hoặc `WantedBossSpawner.cs` (sự kiện chết / hồi), client nhận tin (file NetClient tương ứng: grep `NetOp.PartyRoster` để thấy chỗ phân phối), `WantedBossTracker.cs` (Status)
- Tạo: `Net/Protocol/WantedStatusMessage.cs`, `Maps/WantedRoute.cs` (hoặc nơi MapRegistry sống), test mới
- Test sửa / tạo: `ZenoniaSkills.Tests/WantedBossTests.cs` (Schedule), `WantedSkillTests.cs` (Hint đủ), `ZenoniaNet.Tests/MessageRoundTripTests.cs` (WantedStatus), `ZenoniaNet.Tests/WantedRouteTests.cs` (mới: từ mỗi map có đường tới 3 map boss)
- Sở hữu độc quyền: các file trên; phase 11 không sửa chúng, chỉ đọc.

## Implementation Steps
1. Luật thuần: `Schedule`, `Hint`, `WantedRoute` + test (viết test trước).
2. Tin `WantedStatus` + round-trip test; bump Version 22, ghi chú dòng version.
3. Server: tính `respawnAtUtcMs` từ `WantedTimer` còn lại; phát khi chết / hồi / qua ngày; gửi riêng lúc vào game.
4. Client: nhận -> `WantedBossTracker.Status`; log debug.
5. `dotnet test Tests/dotnet/ZenoniaSkills.Tests` và `dotnet test Tests/dotnet/ZenoniaNet.Tests`: xanh hết.
6. Build lại server Linux (`Tools/build-linux-server.ps1`) + client từ bản sao; e2e: `DebugCapture.NetWanted` (`--wanted-shots`, `--wanted-respawn-check`, `--wanted-respawn-sec` ngắn) kiểm tra client thấy alive=false rồi true đúng giờ.
7. `server-api`: chạy `npm test` (vitest) để chắc không vỡ gì (không sửa code).

## Todo List
- [x] Schedule + Hint + WantedRoute + test
- [x] WantedStatus message + round-trip test + Version 22
- [x] Server phát / gửi lúc vào game (`Net/Server/WantedStatusService.cs`; chưa chạy trên zone server thật)
- [x] Client lưu Status (`WantedBossTracker.Status`; chưa chạy trong game)
- [x] 2 bộ dotnet test (358 + 422) + server-api npm test (149)
- [ ] Build server + client bản sao, e2e hồi sinh (`Tools/wanted-boss-test.ps1`: bot đã có 3 dòng PASS / FAIL mới về status)

## Kết quả 16/11 (lệch so với kế hoạch ở trên)
- Tin gửi thời gian TƯƠNG ĐỐI (`RespawnInMs`, `ResetInMs`), không gửi `respawnAtUtcMs`: theo mục Risk (lệch đồng hồ). Thêm `DayKey` của server để "đã nhận hôm nay" không phụ thuộc đồng hồ máy.
- Layout tin (opcode 25): `BossType` string (<= 32), `DayKey` int, `Alive` byte 0/1, `RespawnInMs` uint, `ResetInMs` uint. Không có số người đánh, không có toạ độ (map + ô lấy từ `WantedBoss.Find(BossType)`).
- Server không gắn sự kiện vào `Zone.cs` / `WantedBossSpawner.cs`: `WantedStatusService` nhìn zone của boss mỗi tick, phát khi `WantedStatusRules.IsNews` (đổi boss / đổi ngày / chết / hồi). Zone ngủ không chạy đồng hồ hồi sinh nên trừ thời gian ngủ (`ZoneIdleClock.SleptMs`, thêm mới).
- `WantedRoute`: `NextMap` + `NextPortal(from, to, gateOpen theo từng map, ô hero)` (chọn cửa gần hero khi map có nhiều cửa sang cùng map kế, vd 2 lối ra của Den); vẫn có dạng `NextPortal(from, to, bool)`.
- Phase 11 đọc: `WantedBossTracker.Status / Today / TodayKey / Schedule() / RespawnLeftMs / ResetLeftMs / ClaimedToday / StatusChanged / BountyPaid`, `WantedSkillDef.Hint`, `WantedRoute`.

## Success Criteria
- Client ở map khác vẫn biết boss sống / giờ hồi (log + e2e); sau qua ngày đổi đúng boss mới.
- Mọi test xanh; không đổi schema save / server-api.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Lệch đồng hồ client / server làm đếm ngược sai | Trung bình x Thấp | Gửi `respawnInMs` thay vì giờ tuyệt đối; client cộng với lúc nhận |
| Quên build lại server -> client 22 bị từ chối | Trung bình x Cao | Bước 6 bắt buộc; ghi vào `docs/pending-tasks.md` |
| Revert một phía | Thấp x Cao | Rollback = revert commit phase 10 cả client + server cùng lúc |

## Security Considerations
- Tin chỉ đọc (server -> client); client không gửi gì mới, không ảnh hưởng chuyện trả thưởng (server quyết qua `WantedBossCredit`).

## Next Steps
- Phase 11 dùng `Status`, `Schedule`, `Hint`, `WantedRoute`.

## Câu hỏi mở
- Boss thắng lúc qua ngày đang đánh dở: giữ như hiện tại (boss cũ bị gỡ, không thưởng) - có muốn báo trước 5 phút không? Đề xuất: thêm 1 toast "Wanted changes in 5:00" (client tự tính, không cần mạng).

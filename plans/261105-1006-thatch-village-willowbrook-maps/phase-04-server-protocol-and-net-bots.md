# Phase 04: Server, protocol, bot test mạng

## Context
- Zone server: `Assets/Scripts/World/ZoneHost.cs` tạo 1 zone cho mỗi `MapRegistry.All`, spawn quái nếu `Hostile`; chuyển map server-side ở `Assets/Scripts/Net/Server/ZoneTransferService.cs` (đọc `PortalsFor`). Không có danh sách map cứng -> không sửa code server.
- `server-api` (node): `src/rpg/item-validation.js` chỉ kiểm `mapId` dạng `^[A-Za-z0-9_-]+$`, tối đa 32 -> không sửa. Nhân vật mới vẫn bắt đầu ở `oakhavenVillage`.
- `Assets/Scripts/Net/Protocol/NetOpcodes.cs:13`: `Version = 13`.
- Bot: `Assets/Scripts/DebugCapture.NetNav.cs` `EnablePortalBot()` chọn `portals[slot % portals.Length]` theo tên bot; `DebugCapture.NetPersist.cs` `WalkOutOfTown()` đi tới khi `Hostile`, rồi kiểm save = `frostHollow` (dòng 268, 305); `Tools/net-persist-test.ps1:375` chờ "PASS walked from oakhavenVillage".

## Overview
Không code server mới; tăng protocol version; sửa bot để test mạng không đi lạc sang tuyến mới.

## Key insights
- Oakhaven có 3 cổng sau phase 2 (nam + cặp đông). Bot slot ngẫu nhiên theo tên có thể đi đông -> Thatchwick (an toàn, vòng lặp tiếp) -> Willowbrook (hostile) -> save `willowbrookMeadows` -> FAIL "the last save was flushed". Lỗi phụ thuộc tên bot nên chập chờn.
- Client cũ không có dữ liệu map mới nhưng server vẫn chuyển hero qua cổng đông -> client `SetCurrent` lỗi. Tăng version để server từ chối client cũ (`RejectReason.Version`).

## Requirements
- `NetProtocol.Version` 13 -> 14 (comment ngắn: thêm map thatchVillage / willowbrookMeadows).
- `EnablePortalBot(string preferTarget = null)`: khi có `preferTarget` và map hiện tại có cổng tới đó thì luôn chọn cổng đó; không có thì giữ cách chọn theo slot. `WalkOutOfTown` gọi `EnablePortalBot(FrostHollowLayout.MapId)`. Bot `--net-bot portal` (`DebugCapture.Net.cs:48`) giữ nguyên (đi khắp nơi là đúng ý).

## Related code files
- Sửa: `Assets/Scripts/Net/Protocol/NetOpcodes.cs`, `Assets/Scripts/DebugCapture.NetNav.cs`, `Assets/Scripts/DebugCapture.NetPersist.cs`.
- Không sửa: `ZoneHost.cs`, `ZoneTransferService.cs`, `server-api/**`, `Tools/net-persist-test.ps1`.

## Implementation steps
1. Tăng version.
2. Thêm tham số `preferTarget` vào `EnablePortalBot`, chọn goal: duyệt `portals`, lấy cổng đầu tiên có `TargetMapId == preferTarget`, nếu không có thì `portals[slot % Length]`.
3. `WalkOutOfTown`: truyền `FrostHollowLayout.MapId`.
4. Build lại server khi phát hành: `Tools/build-linux-server.ps1` / `Tools/package-server.ps1` (theo `docs/deploy.md`); client và server phải lên cùng lúc vì version mới.

## Todo
- [x] Version 14
- [x] Bot ưu tiên cổng Frost Hollow
- [x] Ghi chú deploy (server + client cùng lúc)

## Success criteria
- `dotnet test Tests/dotnet/ZenoniaNet.Tests` xanh (`ProtocolVersionWasBumpedForTheNewWireFormat` vẫn >= 4).
- `Tools/net-persist-test.ps1` PASS ở phase 6 (nếu có server-api local chạy được).

## Risks
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Quên deploy server cùng client | TB x Cao | ghi vào `docs/pending-tasks.md` + deploy.md checklist |
| Test mạng khác cũng dùng `EnablePortalBot` và giả định Oakhaven 1 cổng | Thấp x TB | grep `EnablePortalBot` trước khi sửa (hiện chỉ Net.cs:48 và NetPersist) |

## Security
Version bump chặn client cũ; không đổi auth / dữ liệu save.

## Next
Phase 06 chạy test mạng.

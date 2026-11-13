# World Map 2.0, bước 1

Thiết kế: `docs/world-map-2-design.md`. Người dùng 13/11/2026: "bỏ luôn nhiệm vụ mở đường, làm bước 1 đi". Không có điều kiện nào để qua cổng.

## Phạm vi bước 1 (chỉ 9 map cũ, chưa có map mới)

1. Nối lại map + đổi cấp quái + gỡ boss chặn cổng + client online xuất phát ở Oakhaven (agent A).
2. Hệ thống bán trang bị + người bán thuốc / trang bị ở cả 3 làng (agent B).
3. Oakhaven mới từ bản vẽ đã duyệt (agent C).
4. Chợ Thatchwick: bước này chỉ bố trí quảng trường + NPC môi giới (thoại) + người bán; sạp bán của người chơi là việc riêng sau (trade trực tiếp đã có).

## Tuyến tạm (cho tới khi có map mới)

```
Willowbrook (1–10) ─tây─ OAKHAVEN ─đông─ THATCHWICK ─đông─ Frost Hollow (11–15) ─ Frostfang Den (16–20, boss Glacier Warden 20) ─ SUNSPIRE ─ Amber Dunes (21–30) ─ Emberfen Marsh (31–35) ─ Sunken Reliquary (36–40, boss Mirefang Hydra 40)
```

- Oakhaven nam (đường ra mép dưới bản vẽ mới) → Willowbrook (cổng tây cũ của Willowbrook); Willowbrook tây → Oakhaven.
- Oakhaven đông → Thatchwick tây (như cũ). Thatchwick đông (trước sang Willowbrook) → Frost Hollow, tới chỗ cổng cũ (2,21) của Frost Hollow; cổng đó → Thatchwick.
- Frostfang Den tạm đứng chỗ Snowcrest Pass (16–20) và luôn mở sang Sunspire.

## NPC (id cố định, B định nghĩa, A + C đặt lên map)

| Làng | Thuốc | Trang bị (tier bán) | Khác |
|---|---|---|---|
| Oakhaven | healerNina, merchantDale (có sẵn) | `smithBram` (mới) T0–T1 | giữ elderRowan, guardRook (bảng truy nã) |
| Thatchwick | `herbalistWren` (mới) | `smithHolt` (mới) T2–T3 | `brokerPell` (mới, thoại chợ) |
| Sunspire | apothecaryNara (có sẵn) | blacksmithKhaled (có sẵn, thành người bán) T4–T5 | |

## Sở hữu file

- A: `Assets/Scripts/Maps/*` TRỪ `OakhavenVillageMap.cs`; `MapSpawnTable.cs`, `MonsterDef.cs`, `Rpg/LootTable.cs`, `RpgSession.cs` (gate), `Net/Server/*` (start map, toast gate), test dotnet liên quan. Đặt MapNpc cho Thatchwick (+ Sunspire nếu cần).
- B: `NpcDefs.cs`, `Rpg/PotionCatalog.cs`, hệ thống shop trang bị mới (catalog thuần C#, InvOp mua, xử lý server, cửa sổ UI), `TownNpc.cs`, test dotnet liên quan.
- C: `Maps/OakhavenVillageMap.cs`, ảnh `Assets/ArtLibrary/village/new-oakhaven/`, `Sprites/village/tiles/`, `Tools/build-map-backdrop.py` (nếu cần sửa).

Build: chỉ một agent build cùng lúc (khoá `C:\temp\zu-copy.lock`), lỗi biên dịch ở file của agent khác thì chờ rồi build lại.

## Trạng thái

- [x] A (xong, dotnet test 2 bộ qua, build 0 lỗi, đã xem trong game — xem mục ghi chú)  - [x] B (code xong, dotnet test qua; `ServerHeroFactory.cs` không còn gọi `GatePassedOn` nữa, A đã build qua được)  - [x] C (build qua 0 lỗi lúc build, đã xem trong game, xem mục ghi chú)  - [ ] chạy thử chung, ảnh trong game, cập nhật docs

### A — ghi chú bàn giao
- Nối cổng: Willowbrook tây (0,23)/(0,24) → Oakhaven (13,20)/(14,20) [theo tile C báo]; Oakhaven nam (13,21)/(14,21) → Willowbrook (2,23) [C làm]; Oakhaven đông ↔ Thatchwick giữ nguyên; Thatchwick đông (35,11)/(35,12) → Frost Hollow (4,21); Frost Hollow (2,21) → Thatchwick (33,11). File: `WillowbrookMeadowsMap.cs`, `ThatchVillageMap.cs`, `FrostHollowLayout.cs`.
- Gỡ chặn cổng Frostfang Den → Sunspire: `FrostfangDenMap.cs` gộp `BackPortals`/`AllPortals` thành 1 danh sách luôn trả về; xoá `MapSpawnTable.HasBossGate`/`GatePassedOn`; bỏ nhánh gate trong `RpgSession.cs`, toast "The way forward is open" trong `NetPlayer.cs`, 2 chỗ `GatePassedOn` trong `ServerHeroFactory.cs`. Vẫn giữ `defeatedBossMaps`/`bossDefeated` cho phần thưởng. Đã xem trong game: bot đánh Glacier Warden đứng cạnh cổng Sunspire mà cổng vẫn sáng (ảnh `C:\temp\step1-shots\den-sunspire-gate_1.png`).
- Đổi cấp quái (bảng đầy đủ trong báo cáo cuối); file `MonsterDef.cs`, `Rpg/LootTable.cs`. `MonsterDef.Wanted()` đổi sang nhận số liệu cũ đóng băng (không lấy từ boss map sống nữa) để Rimeclaw/Glacial/Hydra Queen giữ nguyên số — đã xác nhận qua bot: Rimeclaw Tyrant HP 10440/10440 ở hệ số test x0.3 = đúng 34800 đầy đủ (580×30×2), không đổi.
- Client online xuất phát Oakhaven: `GameSession.Online.cs` (2 chỗ `FrostHollowLayout.MapId` → `OakhavenVillageMap.MapId`), `World/ZoneHost.cs` (`CreateDedicated` mặc định Oakhaven khi không có `--map`). server-api không cần đổi (`ZONE_ID` chỉ là nhãn routing, không phải map mặc định). Đã xác nhận qua log zone thật: `heroes begin on oakhavenVillage`.
- Thêm NPC Thatchwick: `herbalistWren` (13,9), `smithHolt` (22,9), `brokerPell` (18,13) quanh giếng quảng trường, không chắn đường — đã xem trong game (`thatch-npcs_1.png`).
- Test dotnet sửa theo: `MapGraphTests.cs`, `FrostHollowLargeMapTests.cs`, `WillowbrookLargeMapTests.cs`, `LevelingTests.cs` (frostSlime đổi cấp nên tách hằng số 14 ra khỏi `LootTable.For`). Cả 2 bộ test qua (297 + 335).

### C — ghi chú bàn giao
- File đổi: `Assets/Scripts/Maps/OakhavenVillageMap.cs` (viết lại hoàn toàn, giống Thatchwick: một ảnh nền vẽ sẵn, không còn `MapProp`/`PropList`), `Assets/Resources/Sprites/village/tiles/oakhaven-ground.png` (thay bằng bản vẽ mới đã duyệt, 2304x1536, qua `Tools/build-map-backdrop.py backdrop`). Không sửa `Tools/build-map-backdrop.py` (dùng được luôn). File phân loại ô + ảnh kiểm: `plans/261113-0106-world-map-step1/layout-oakhaven/` (rows.txt, grid/check images).
- Cổng nam (row 21, 2 ô `(13,21)` `(14,21)`) → `willowbrookMeadows` tile `(2,23)` cho cả hai (hàng 22-23 chặn cứng dưới HUD, giống quy ước Oakhaven cũ). **A cần sửa**: `WillowbrookMeadowsMap.cs` đang đặt tạm `OakhavenVillageMap.MapId, 16, 19` (placeholder của A) cho cả 2 cổng `(0,23)`/`(0,24)` — xin đổi thành `(13,20)` và `(14,20)` (một hàng phía bắc cổng nam mới của Oakhaven, 2 ô đó đang đi được).
- Cổng đông giữ nguyên như cũ: `(35,10)`/`(35,11)` → `thatchVillage` `(2,8)`/`(2,9)`; cầu bắc qua suối ra sát mép bản đồ nên không cần đổi gì — đã kiểm tra Thatchwick's `(0,8)`/`(0,9)` → Oakhaven `(33,10)`/`(33,11)` vẫn đi được trên bản đồ mới (nằm trên cầu/đường).
- NPC đặt lại: `elderRowan` (16,7), `guardRook` (20,7) hai bên cửa Guild Hall; `healerNina` (5,7) trước tiệm thuốc tây; `merchantDale` (9,11) cạnh sạp trái cây; `smithBram` (27,9) trong lò rèn đông (chờ B thêm NpcDefs nếu chưa có — build vẫn qua vì NpcDefs tra theo id lúc runtime, không lỗi biên dịch); thêm `villagerMia` (22,11), `childTim` (12,12) cho có người ở quảng trường. Bỏ villagerHunter/farmer/fisherman/carpenter cũ (không còn nhà phù hợp trong bản vẽ mới).
- PlayerStartTile mới (20,12), giữa quảng trường cạnh giếng (giếng chặn ở (17-18,10-11)).
- Đã build `Tools/build-from-copy.ps1` 0 lỗi (khoá/mở `C:\temp\zu-copy.lock` đúng quy trình). Đã chụp và XEM trong game (offline, `--map oakhavenVillage`): toàn cảnh, `--solids` toàn cảnh, cận cảnh khu tây-nam (farm + cottage), cận cảnh lò rèn, và tp thẳng vào từng ô cổng để xác nhận chuyển map — cổng nam qua `Willowbrook Meadows` (hạ đúng cạnh cổng tây của Willowbrook, đi được), cổng đông qua `Thatchwick Village` (hạ đúng cạnh sạp, đi được). Ảnh lưu `plans/261113-0106-world-map-step1/shots/` (chưa copy sang `docs/reference/`, sẽ làm nếu cần so sánh sau).
- Chưa xem: hero online thật sự xuất phát ở Oakhaven (phần của A), và tương tác hội thoại/mua hàng của `smithBram`/NPC mới (phần của B) trong map mới — mới xem được NPC đứng đúng chỗ, chưa bấm thử hội thoại.

### B — ghi chú bàn giao
- File mới: `Assets/Scripts/Rpg/GearCatalog.cs` (GearShops + GearCatalog), `Assets/Scripts/Rpg/PlayerState.Gear.cs` (BuyGear), `Assets/Scripts/GearShopWindow.cs`.
- Đụng ngoài danh sách sở hữu (nhỏ, cần thiết để nối hệ thống, không đụng phần map/gate của A): `Assets/Scripts/Net/Server/ServerInventoryActions.cs` (thêm nhánh GearBuy vào ShopRefusal + RequestSave), `Assets/Scripts/Net/Protocol/NetOpcodes.cs` + `NetMessageReader.cs` (thêm `InvOp.GearBuy`, bump `NetProtocol.Version` 17→18), `Assets/Scripts/World/PlayerRpg.cs`, `Assets/Scripts/WindowsHost.cs`, `Assets/Scripts/UiWindows.cs`, `Assets/Scripts/DebugCapture.cs` (mở rộng `--open-shop`).
- Chưa build/chụp ảnh được: lỗi biên dịch hiện tại nằm ở file của A, không phải của B.

## Kiểm chung (13/11 sáng, coordinator)

- Build chung 0 lỗi, dotnet 2 bộ qua. Ảnh: `docs/reference/gear-shop-window-ingame.png`, `new-oakhaven-ingame-overview.png`, `thatchwick-new-npcs-ingame.png`.
- Sửa: cửa sổ shop trang bị tràn màn hình (12 món) -> thẻ chọn tier, mỗi lần 6 món, mở sẵn tier cao nhất hero mặc được.
- Còn: NPC mới ở Thatchwick mượn hình NPC khác (Elder Sahir, lính giống Guard Rook, Nara) -> nhờ ChatGPT vẽ riêng; iceGoblin / spectralWolf dùng chung Frost Hollow (11-15) và Frostfang Den (16-20) nên bãi tây-nam Frost Hollow có quái Lv16-18; chưa đi bộ thử Oakhaven <-> Willowbrook; chưa mua thử online; chợ người chơi (sạp) chưa làm.

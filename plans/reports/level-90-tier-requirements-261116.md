# Cấp tối đa 90 + cấp mặc theo tier (16/11/2026)

Người dùng: "chỉnh lại level max lên tới 90, và các đồ giờ sẽ theo cấp: T1 cần lvl 10, T2 cần level 20, T3 cần 30 cho đến hết T9". CHƯA commit.

## Đã đổi

| File | Đổi |
|---|---|
| `Assets/Scripts/Rpg/Leveling.cs` | `MaxLevel` 60 -> 90. Công thức EXP giữ nguyên |
| `Assets/Scripts/Rpg/EquipLevel.cs` | `ByTier` = 1,10,20…90; áp cho MỌI ô trang bị (vũ khí, tay phụ, mũ, giáp, găng, giày), pet không; thêm `UnequipAboveLevel` (save cũ) |
| `Assets/Scripts/Rpg/SaveSerializer.cs` | 1 dòng trong `Restore`: gọi `EquipLevel.UnequipAboveLevel(p)` trước `EquipStarterGear` |
| `Assets/Scripts/Rpg/GearCatalog.cs` | Thợ rèn: Thatchwick T1–T2 (trước T2–T3), Sunspire T2–T3 (trước T4–T5); Oakhaven giữ T0–T1 |
| `Assets/Scripts/DebugCapture.cs` | `--worn-tier`/`--wear-tiers` mặc thẳng (bỏ qua cấp), vì các tool chụp art dùng `--level 60` với T9 |
| `Assets/Scripts/Rpg/Skills/SkillRespec.cs`, `DevStartConfig.cs` | Chỉ sửa ghi chú |
| `server-api/src/rpg/rpg-constants.js` | `MAX_LEVEL` 90, `EQUIP_LEVEL_BY_TIER` mới, `requiredEquipLevel` áp cho mọi ô trừ pet |
| `server-api/scripts/dev-boost-character.mjs` | Dùng `MAX_LEVEL` thay số 60 |
| Test | `EquipLevelTests.cs`, `LevelingTests.cs`, `GearShopTests.cs`, `AccountPetsTests.cs` (1 dòng, file của việc pet), `server-api/test/equip-level.test.js`, `save-ops.test.js` |
| Docs | `docs/world-map-2-design.md` (luật đồ mới), `docs/pending-tasks.md` |

Các chỗ còn lại đã dùng hằng `Leveling.MaxLevel` nên tự lên 90: thanh EXP HUD/web, `NetLaunchArgs --dev-level`, clamp trong SaveSerializer, `AccountState.levelReward`, server-api zod `level`.

## Bảng EXP (`ExpToNext(L) = 5 * floor(50 * L^1.5)`, không đổi công thức)

| Lv | EXP lên cấp | | Lv | EXP lên cấp |
|---|---|---|---|---|
| 1 | 250 | | 60 | 116 185 |
| 10 | 7 905 | | 61 | 119 105 |
| 20 | 22 360 | | 70 | 146 415 |
| 30 | 41 075 | | 80 | 178 885 |
| 50 | 88 385 | | 89 | 209 905 |

Mỗi bậc sau Lv60 tăng dưới 3%, không nhảy vọt. Tổng EXP từ Lv1 lên 60: 2,73 triệu; lên 90: 7,58 triệu (khoảng 2,8 lần). Tổng vẫn nằm trong int.

## Cấp mặc

| Tier | T0 | T1 | T2 | T3 | T4 | T5 | T6 | T7 | T8 | T9 |
|---|---|---|---|---|---|---|---|---|---|---|
| Trước | 1 | 5 | 10 | 15 | 20 | 30 | 35 | 40 | 50 | 60 (chỉ vũ khí/tay phụ) |
| Giờ | 1 | 10 | 20 | 30 | 40 | 50 | 60 | 70 | 80 | 90 (mọi ô trừ pet) |

Tier = itemLevel / 10. Cường hoá không làm tăng cấp mặc. Thông báo khi mặc không đủ cấp vẫn như cũ ("Requires level X", client + zone server + server-api). Ô đồ trong túi có số đỏ "LvX" ở góc, bảng chi tiết hiện "Req. Lv X" màu đỏ; giờ hiện cả với giáp/mũ/găng/giày. UI game viết tiếng Anh nên giữ "Req. Lv X", không đổi sang "Cần Lv X".

## Save cũ

Khi load (`SaveSerializer.Restore`, chạy ở cả offline lẫn zone server nên client và server ra cùng kết quả): món đang mặc mà không đủ cấp thì cất vào túi, ô vũ khí/tay phụ trống thì đeo lại đồ khởi đầu. Không mất đồ: túi đầy thì món đó vẫn được mặc. Khi đang chơi vẫn chỉ kiểm cấp lúc mặc (tháo thì luôn được). Ví dụ: nhân vật Lv25 đang cầm vũ khí T4 (trước cần 20, giờ cần 40) sẽ thấy nó nằm trong túi và tay cầm kiếm khởi đầu.

## Kiểm tra

- Cây làm việc chính: `ZenoniaSkills.Tests` 327/327, `ZenoniaNet.Tests` 404/404, `server-api` vitest 141/141.
- Unity không biên dịch được cây chính vì code UI pet/gacha đang viết dở (`PetWindow`, `PetGachaWindow`, `PetArt`…). Vì vậy đã build từ worktree sạch `D:/temp/zu-src-level` (HEAD 4c527c36 + đúng patch này). Trên worktree đó: Skills 326/326, Net 404/404.
- Exe: `D:/temp/player-level90/Zenonia.exe` (bản sao `D:/temp/zu-copy-level`), build thành công, 0 lỗi.
- Ảnh đã tự mở xem:
  - `D:/temp/cap-level90/t3-lv12_1.png`: Warrior Lv12, chọn kiếm T3 "Balrog". Bảng chi tiết hiện "Req. Lv 30" màu đỏ; các ô trong túi hiện Lv20 … Lv90 màu đỏ; kiếm T1 không có số (Lv12 ≥ 10).
  - `D:/temp/cap-level90/lv90_1.png`: HUD "Lv. 90", "Warrior Lv. 90", HP 1178, kiếm T9 hiện "Req. Lv 90" màu trắng.
- Chưa xem trong game: thanh EXP khi đã đạt Lv90, load một save cũ thật, online, cửa hàng thợ rèn mới.

## Cần build lại server

Zone server (C#: `Leveling`, `EquipLevel`, `GearShops`, `SaveSerializer`) và deploy lại `server-api` (`MAX_LEVEL`, `EQUIP_LEVEL_BY_TIER`). Nếu server-api còn `MAX_LEVEL` 60 thì sẽ từ chối save Lv61+.

## Quyết định cần người dùng xác nhận

1. Cấp mặc áp cho cả giáp/mũ/găng/giày (đọc "các đồ" là mọi trang bị), trước đây chỉ vũ khí/tay phụ. Muốn chỉ vũ khí thì sửa `EquipLevel.Applies` và `requiredEquipLevel`.
2. Thợ rèn: Thatchwick T1–T2, Sunspire T2–T3, để mỗi làng bán đồ người chơi vùng đó mặc được. Để như cũ thì Sunspire (vùng Lv21–30) chỉ bán đồ cần Lv40/50.
3. Đồ rơi giữ theo itemLevel = cấp quái, nên rơi ra luôn mặc được ở cấp vùng. Nội dung hiện có cao nhất Lv40, nên T5–T9 chỉ có từ dev. Thiết kế World Map 2 (vùng 5–6, Lv41–60, rơi T7–T9) giờ lệch; Lv61–90 chưa có vùng. Chưa thêm nội dung.
4. Skill: 8 skill × 10 rank = 80 SP, nên từ Lv80 là max hết, 10 SP còn thừa. Chưa đổi.
5. Vé pet theo mốc cấp (`AccountRewards.OnLevel`, việc pet) tự tính thêm mốc 70/80/90 (+6 vé). Ghi chú "10…60" trong file đó chưa sửa vì là file của việc pet.
6. Save cũ: tự cất đồ vượt cấp vào túi khi load (như trên). Muốn giữ nguyên đồ đang mặc thì bỏ 1 dòng trong `SaveSerializer.Restore`.
7. Tools `net-*-test.ps1 --dev-level 60` giữ nguyên (chỉ là số test).

# Hệ thống mũ thời trang (headgear) + 2 việc sau bước B (19/11/2026)

Branch `worktree-agent-a7372becdbac6f407` (gốc `unity` 89fe05e7). Exe test: `D:\temp\player-headgear\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-headgear`, Succeeded, 0 lỗi).

## Đã làm
1. **Món mũ + ô riêng.** `Slots.Headgear = "headgear"`, ô thứ 8 (index 7; ô `helmet` vẫn là dây chuyền, không đụng). Mũ là `Item` template `hat-<key>`, không chỉ số, không yêu cầu cấp, mọi class đội được, không cường hoá được, đổi / bán / vứt như đồ khác. Đeo / tháo qua `InvOp.Equip` / `Unequip` sẵn có (server kiểm). `PlayerState.RecomputeStats` ghi `Appearance.equippedHeadgearId` = id mũ đang đội (đeo, tháo, load save) nên `NetAppearance.Headgear` tự đúng; client không bao giờ gửi id mũ. Server: `LookKey` có thêm id mũ -> đổi mũ là phát `AppearanceChanged` cho người xung quanh.
2. **Dữ liệu.** Luật (id, nguồn, tỉ lệ, giá, class) trong `Assets/Scripts/Rpg/HeadgearCatalog.cs` (engine-free, dùng chung server/offline/test). `Assets/Resources/Data/hero-layered/headgear.json` chép lại các trường đó (test `HeadgearJsonMirrorsTheCatalog` bắt hai bên khớp: sửa thì sửa cả hai) và thêm bố cục art client đọc: `sheet` = `Sprites/hero-layered/headgear/<key>`, `icon` = `headgear-<key>`, `cellW 160, cellH 128, ax 80, ay 112, refW 88, drop 0.3` (giá trị của mũ test; tool cắt art sẽ ghi lại).
3. **Art chưa có -> không vẽ gì bằng code.** `HeadgearArt.cs`: icon = `Sprites/icons/headgear-<key>.png` -> ô hướng xuống của sheet mũ -> ô hướng xuống của mũ bộ test có sẵn (`Sprites/hero-layered/test/headgear/1`, mũ phù thuỷ xanh) -> icon dây chuyền. Lớp đầu: `HeroLayeredSet.Headgear(id)` dùng mục của bộ, không có thì mục trong headgear.json khi sheet đã tồn tại, không thì đầu trần.
4. **Nguồn mũ (server quyết, lưu cùng save):**
   - Quái rơi (đường loot cũ, `LootTable.RollAll`, rút sau cùng nên các lần rút cũ giữ thứ tự): Horn Rabbit tai thỏ 4 %, Frost Slime mũ slime 3 %, Snow Fox tai cáo 2,5 %, Ice Imp sừng quỷ 2 %, Mire Toad mũ ếch 1,5 %, Thorn Sprite vòng hoa 3 %. Chủ đồ như loot thường (người gây nhiều sát thương nhất, 3 s).
   - Boss truy nã: `KillRewards` sau `WantedBossCredit.Pay`, MỖI người góp sức rút riêng, mũ rơi dưới boss thuộc người đó (online): Rimeclaw vương miện sói bạc 6 %, Glacial Colossus vương miện pha lê 6 %, Mire Hydra Queen vương miện rắn 6 %; thêm mũ phù thuỷ tối thượng 1 % từ mọi boss truy nã. Mỗi lần giết boss (không giới hạn theo ngày; boss có hồi sinh).
   - Shop: cả 3 thợ rèn làng (Bram Oakhaven, Holt Thatchwick, Khaled Sunspire) thêm tab "Hats": mũ rơm 150 G, băng đô vải 120 G, mũ len quả bông 200 G, mũ phi công da 280 G. Online gửi `InvOp.GearBuy` với Slot = 128 + id mũ; `GearShops.Refusal` kiểm người bán, map, tầm với.
   - Quest tân thủ "First Hunt" (Elder Thomas, Oakhaven, đứng đầu danh sách nên ông mời trước): nhận quest rồi giết 5 quái bất kỳ (`MonsterType = "*"`), nộp -> 30 G + mũ class: Warrior mũ sắt nhỏ, Wizard mũ phù thuỷ nhọn, Mechanic kính đồng, Assassin băng đô ninja đỏ, Cleric khăn tu nữ trắng (Fighter: băng đô vải). Túi đầy thì không cho nộp (quest giữ nguyên). Mũ vào túi, người chơi tự đội. Sửa luôn lỗi cũ: thưởng 0 đá vẫn trả 1 đá.
5. **UI.** Túi đồ (skin RO Soft): ô "Hat" ở cuối hàng dưới (sau Pet); nút Stats dời lên góc trên-phải khung chân dung. Bấm ô mũ lọc túi theo mũ (`BagFilter.Headgear`). Chi tiết mũ: "Cosmetic hat: changes your look only. / No stats. Any class, any level. / nguồn". Cửa sổ nhận quest có ô thưởng mũ; sổ quest + chat ghi tên mũ. Armory (giao diện mới, đang tắt) có thêm bệ thứ 8 cho khỏi lỗi mảng, chưa canh đẹp.
6. **Việc sau bước B:** (a) `PlayerIndicatorRing` + tên xanh hiện cả offline (offline không có tên tài khoản nên hiện tên class); tên ẩn khi có cửa sổ modal mở (trước chỉ ẩn khi trade; thấy tên đè lên tab shop lúc chụp). (b) `palettes.json`: "earth brown" `#A9744A` -> "jade teal" `#2F9A8E`, vẫn 6 màu, cùng index 4 (nhân vật đã chọn index 4 sẽ thành ngọc lam). Thêm test: không màu áo nào gần màu da thuộc thắt lưng.

## Mạng / server
- `NetProtocol.Version` 24 (chưa phát hành): ô 7 trong RpgState / Unequip, loot `ItemDefId` ô 7 mang id mũ ở 7 bit cấp, GearBuy 128 + id. **Phải build lại zone server cùng client.**
- server-api: `SLOTS` thêm `headgear`, `HEADGEAR_KEYS/COUNT/TEMPLATE_IDS`, schema: mũ chỉ ở ô headgear, không chỉ số / class / cường hoá, `appearance.equippedHeadgearId` 0..19, không yêu cầu cấp. **Phải deploy server-api mới** trước khi người chơi có mũ, nếu không save có mũ bị từ chối. Không tăng `SaveVersion` (bản build cũ đọc save mới sẽ bỏ mất mũ; bản cũ chưa phát hành). Không deploy gì trong việc này.

## Kiểm tra
- `ZenoniaSkills.Tests` 403/403 (mới: `HeadgearTests` 19, `NewbieQuestTests` 9, palette 1; sửa `QuestTests` dùng `crystal-survey` + hero đã xong First Hunt, `SaveRoundTripKeepsTheLook` đội mũ thật). `ZenoniaNet.Tests` 422/422 (Version 24). server-api vitest 168/168 (mới `test/headgear-schema.test.js`).
- Cờ chụp (`DebugCapture.Headgear.cs`): `--give-hats 1,6,10`, `--wear-hat N`, `--drop-hat-from TYPE` (loot thật với mọi lần rút trúng), `--newbie-quest offer|ready|done`, `--open-shop smithBram --shop-tab hats`, `--select-worn headgear`.

## Ảnh (đã xem từng ảnh) `docs/reference/headgear-ingame/`
- `bag-hat-slot-worn-wizard-hat-and-hat-filter.png`: Wizard đội mũ id 2, ô Hat sáng ở góc dưới-phải, túi lọc "Hat" 4 mũ khung màu theo độ hiếm, chi tiết đúng 3 dòng. Mọi mũ dùng icon tạm (mũ test) vì chưa có art.
- `newbie-quest-offer-hat-reward.png`: First Hunt, "Defeat any monster 0/5", thưởng 30 G + ô "Hat". (Ô đá và thuốc hiện rỗng chữ như cũ.)
- `newbie-quest-hand-in-elder.png`: hội thoại nộp của Elder Thomas sau 5 lần giết.
- `newbie-quest-gift-hat-in-bag-mechanic.png`: sau khi nộp, túi có 1 mũ (kính đồng, icon tạm), ô Hat trống.
- `blacksmith-bram-hats-tab.png`: tab Hats (pill bạc hà) với 4 mũ, giá, "No req.".
- `horn-rabbit-loot-with-bunny-ears-forced-roll.png`: loot Horn Rabbit có mũ trên đất (cạnh vàng, thuốc, đá, kiếm).
- `glacial-colossus-hats-forced-roll.png`: 2 mũ (vương miện pha lê + mũ tối thượng) dưới đất.
- `offline-ring-green-name-frost-hollow.png`, `offline-ring-green-name-village.png`: offline, vòng bạc hà dưới chân + tên class xanh lá.
- Chưa xem: mũ trên đầu nhân vật (chưa class nào vẽ nhiều lớp, chưa có art mũ), online thật (mua / rơi / đổi mũ thấy ở người khác), host mode, tỉ lệ rơi thật trong game (chỉ test thống kê dotnet).

## Câu hỏi còn mở
- Fighter không có mũ khởi đầu riêng: đang tặng băng đô vải (mũ shop). Có muốn mũ riêng (id 20)?
- Quest tân thủ phải nhận ở Elder trước mới đếm; có muốn tự kích hoạt từ đầu để đếm luôn 5 con đầu tiên?
- Mũ boss truy nã rơi mỗi lần giết (boss hồi sinh) chứ không chỉ lần nhận thưởng ngày; tỉ lệ 6 % / 1 % có ổn?
- Offline tên hiện là tên class (không có tên nhân vật); có muốn thay bằng gì khác?
- Đổi index 4 thành ngọc lam: nhân vật đã chọn nâu đất sẽ đổi màu (chưa phát hành nên coi như không sao).

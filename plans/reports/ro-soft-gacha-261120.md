# Báo cáo: cửa sổ Gacha RO Soft Fantasy theo mockup 06-gacha (20/11/2026)

Exe: `D:\temp\player-gacha\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-gacha`, 0 lỗi). Mockup: `docs/reference/ui-style-ro-soft-fantasy/06-gacha.png`.
Chat ChatGPT (Tay, đã kiểm "Tân Korris" / "Tay Plus"): https://chatgpt.com/c/6aff3c7b-d898-83ec-9563-8f66ba49a6d1

## Diễn biến
1. ChatGPT vẽ sheet 22 mảnh (huy hiệu paw, ?, xu gacha, pha lê tím, chồng xu, !, ruy băng, huy hiệu UR, 5 pill độ hiếm, mũi tên, mảnh ghép, thanh track / vàng / tím, thẻ pet, 2 icon) và ảnh sân khấu (bệ + quầng sáng, nền phẳng -> alpha).
2. Dựng `RoGachaWindow` theo toạ độ đo trên mockup (hàm X / Y / D đổi px mockup -> canvas), dùng lại `bag-*`, `btn-*`, `wanted-*`, `InventoryRoBoxes` / `InventoryRoWidgets`.
3. Nghiệm thu ChatGPT 3 vòng: 8.1 -> 8.7 -> 9.0/10, vòng 3 "không còn khác biệt mức LỚN". Ảnh `docs/reference/ro-soft-gacha/compare-round1..3.png`. Sửa giữa các vòng: nền xanh băng, viền panel nhạt hơn, boss + sân khấu to / cao hơn, huy hiệu UR to hơn, nút Pull / Buy bo tròn hơn, chấm trang to hơn, thẻ pet tô màu theo độ hiếm, hàng pet cuối căn giữa, thanh dày hơn, tiêu đề chữ trắng viền xanh, hàng tỉ lệ có nền capsule.

## Mockup -> chức năng thật
| Mockup | Game |
|---|---|
| Xu gacha / pha lê + "+" | `Account.tickets` / `crystals`; "+" xu mở popup Buy, "+" pha lê báo nguồn (chơi mới có) |
| ? / ! | Popup luật: tỉ lệ + `PetGachaText.Rules()` (số của server) |
| Lumi + bóng thoại | Bust Lumi có sẵn (`Sprites/ui/paper/gacha/lumi-bust`), lời thoại đổi theo kết quả |
| Pity SSR n/20, UR n/80 | `pitySSR / SsrPity` (20), `pityUR / UrPity` (**50**, số thật) |
| Đổi mảnh UR n/120 | `urShards / UrExchangeCost`, Exchange = `InvOp.GachaExchange` (xám khi thiếu, "Owned" khi đã có) |
| Mua Xu Gacha | Popup: Buy 1 / Buy 10 / "Buy the missing N" (150 crystal / xu, `InvOp.GachaBuyCoins`) |
| Sự kiện vĩnh viễn | "Permanent event" (gacha pet không có hạn) |
| Pet nổi bật + carousel | Mặc định Frostfang King (đích pity / đổi mảnh); carousel 8 pet, 5 / trang, mũi tên + chấm; chạm = xem trước trên bệ |
| Quay x1 / x10 | `InvOp.GachaPull` thật, khoá khi đang chờ server, thiếu xu -> popup Buy báo thiếu bao nhiêu |
| Kết quả | Thẻ RO: x1 pet trên bệ + NEW / mảnh; x10 lật từng thẻ (Skip), SSR+ có viền vàng, tổng; Pull again / OK (J / Space), pity còn lại |
| Tỉ lệ rơi | N 12 / R 24 / SR 36 / SSR 6,25 / UR 2,5 % + **dòng shared shards 19,25 %** (tổng 100 %) từ `PetGacha.Rates` |
| Lịch sử quay | `Account.history`, mới nhất trước, 5 / trang |
| Danh sách pet | 8 pet gacha thật (mockup 10), thẻ màu theo độ hiếm, chạm = xem trước |

`--ui-style classic` vẫn mở `PetGachaWindow` cũ (đã chụp); `--ui-new gacha` vẫn quầy Lumi giấy.

## File
- Code mới: `RoGachaWindow.cs` (+ `.Left`, `.Centre`, `.Right`, `.Result`, `.Popups`, `.Testing`). Sửa: `PetGachaWindow.cs` (Open / Close / IsOpen / ShowTab / TestPull chuyển sang bản RO), `DebugCapture.UiGacha.cs` (`--gacha-press`, `--gacha-click`, `--gacha-click-test`), `RoSoftSprites.Borders.cs` (sinh lại).
- Art: `Assets/ArtLibrary/ui/ro-soft-gacha/src/` -> `Resources/Sprites/ui/ro-soft/gacha-*` bằng `Tools/ui-ro-soft-sheets/ro-gacha-a-kit.json`, `ro-gacha-b-green-pill.json`; `gacha-stage.png` tách alpha từ nền phẳng.
- Ảnh: `docs/reference/ro-soft-gacha/` (after-*, compare-round1..3, zoom-seam-check, classic-style-old-window, art-contact).
- Sổ lỗi: `docs/sprite-rules.md` (nền magenta ăn màu tím), `docs/ui-layout-rules.md` (thứ tự vẽ, toast đè nút kết quả).

## Kiểm tra
- Build 0 lỗi. Đã tự xem: màn chính, kết quả x1 (UR qua pity), x10 (UR ở lượt pity 50), tab lịch sử, popup Buy (thiếu 7 xu), popup luật, bản classic, phóng 4x pill / thanh / nút (không vạch, không khe).
- Click 15/15 nút qua EventSystem, raycast ở tâm nút trúng đúng nút (không bị che).
- dotnet test: ZenoniaSkills 452/452, ZenoniaNet 455/455.
- CHƯA xem: online thật với zone server, màn 4:3 / 21:9 / điện thoại, nói chuyện với Lumi trong làng (cùng đường `UiScreens.OpenForNpc` -> `PetGachaWindow.Open`, chưa chụp), nút Exchange khi đủ 120 mảnh mà chưa có King (tài khoản test đã có King).

## Câu hỏi chưa giải quyết
1. Mockup pity UR 80, game 50: giữ 50 (số server)?
2. Carousel "featured": game không có banner xoay vòng nên carousel chỉ để xem trước pet. Có muốn banner theo mùa (cần server + dữ liệu)?
3. Lumi đang dùng bust Adventure Paper (áo nâu, mũ kính); mockup vẽ Lumi khác (mũ trắng, áo navy). Có vẽ lại Lumi theo RO Soft không?
4. Thẻ pet chưa đánh dấu pet đã có / số sao. Có cần?

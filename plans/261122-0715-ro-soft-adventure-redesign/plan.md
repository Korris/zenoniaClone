# Đổi toàn bộ hình ảnh sang "RO Soft Adventure" (hướng RO3)

Nhánh `ro-soft-adventure` (tách từ `ro-fantasy-heroes` 22/11, commit thẳng, không push khi chưa được bảo).
Người dùng 22/11: "tạo nhánh mới từ nhánh này, đổi design tất cả qua menu, nhân vật qua kiểu RO Soft Adventure"; lý do: "tui thấy Adventure này có vẻ giống style Ragnarok 3 hơn kiểu Fantasy". **Sao Bắc Đẩu = cảm giác RO3**; Adventure là cách diễn giải.

Luật style: khối "RO Soft Adventure" đầu `docs/sprite-rules.md` + `docs/ui-layout-rules.md`. Style guide đầy đủ + palette hex: `docs/reference/ro-soft-adventure-redesign/style-guide-chatgpt.md`. Chat ChatGPT (Tay): https://chatgpt.com/c/6b0243dc-91b8-83ec-afe6-bec308abaf1d

## Phát hiện chính

- Bộ mẫu Adventure (`docs/reference/ui-style-ro-soft-adventure/01..14`) dùng **CÙNG bố cục** với bộ RO Soft Fantasy đang chạy (`so-sanh-fantasy-vs-adventure/`): đổi là đổi DA (sprite + màu + font), code bố cục giữ gần hết.
- Mọi sprite UI RO Soft nạp qua MỘT hằng `RoSoftSprites.Folder = "Sprites/ui/ro-soft/"` (544 file, 272 mảnh) và mọi màu qua `RoSoftTheme` => thêm thư mục `Sprites/ui/ro-adventure/` cùng tên mảnh, nạp ưu tiên Adventure rồi rơi về ro-soft (mảnh chưa vẽ vẫn chạy), + bộ token Adventure. Cờ `--ui-skin fantasy|adventure` để so A/B.
- Bộ "Adventure Paper" cũ (`Sprites/ui/paper/`, 426 file, `--ui new`) là gỗ dày + nút bão hoà + Baloo kiểu cartoon, KHÁC Adventure mới (giấy ngà, viền nâu mảnh, chữ có chân): chỉ tái dùng ý tưởng / vài texture giấy, không dùng thẳng.
- Thử đổi tông bằng tool (giảm bão hoà, ấm, tối nhẹ): `ro-soft-adventure-redesign/warrior-idle-regrade-tool-test.png` => ấm hơn nhưng vẫn là thiết kế Fantasy (vàng bóng, miệng cười to). Tool chỉ dùng tạm cho walk / attack khi chờ vẽ lại; idle + chân dung phải ChatGPT vẽ lại.
- Test ChatGPT: Warrior idle xuống Adventure đạt (cùng tỷ lệ chibi, thép mờ + da + đồng, mặt nghiêm nhẹ): `warrior-idle-fantasy-vs-adventure.png` (+ `-game-size.png` ở cỡ game 200 px vẫn đọc rõ). Túi đồ: `bag-adventure-chatgpt-1.png`, so sánh `bag-fantasy-vs-adventure.jpg`.
- **Warrior V2 làm xong 22/11 (người dùng duyệt "ok duyệt V2, làm luôn cả 3, làm cho warrior chém trước, khoan design tất cả"):** idle 5 hướng + đi + chém nặng 8 hướng x 8 frame Adventure 2,65 đầu, lực đòn engine `WarriorSlashFeel` (frame va chạm đúng 120 ms, hit-stop 35 ms trong chu kỳ, lunge ~15 px, rung 1,8 px, loé quái 65 ms), ChatGPT chấm lực 5,6 -> 8,6. Không đổi bảng skill / server. Camera giữ `HeroPx` 69 (Warrior hiện ~76 px). Ảnh `docs/reference/warrior-v2-adventure/`. Chi tiết `docs/pending-tasks.md` 22/11 10:15. Class khác chờ (người dùng: "khoan design tất cả").
- **Tỷ lệ nhân vật (ĐỀ XUẤT 22/11, đã DUYỆT, xem dòng trên):** người dùng thấy nhân vật "lùn lùn", đòn đánh "không lực". ChatGPT: V2 = **2,65 đầu, đầu 37,7 % chiều cao** (hiện 2,33-2,44 / 41-43 %; RO3, Ragnarok M / Origin ước 2,5-2,9 / 34-40 %), giữ cỡ đầu, thân +6-8 %, chân +12-16 %, vai 1,2-1,25 lần đầu, giới hạn ≤ 2,75 đầu / đầu ≥ 36 %; hero ~76 px. Đòn đánh: vẽ lại f1/f3/f4/f5 theo chuỗi chân-hông-vai-kiếm + lunge / hit-stop / rung / flash khớp impact. Chi tiết + ảnh test: `ro-soft-adventure-redesign/proportions-and-attack-force-chatgpt.md`, `warrior-proportions-current-vs-v2-game-size.png`. Nếu duyệt: phase 05 đổi chiều cao thân (anchor đầu / tay / tóc, `build-ro-fantasy-hero-game-assets.py`), sửa số "đầu 37-42 %" trong `docs/sprite-rules.md`; đổi mốc sát thương thì build lại server.

## Phase

| # | Phase | Trạng thái | Công (phiên agent ~2-4 h) |
|---|---|---|---|
| 01 | [Nền style: luật, token, font, skin switch](phase-01-style-foundation-tokens-fonts-skin-switch.md) | Luật + style guide XONG; code chưa | 1 |
| 02 | [UI kit Adventure (sheet ChatGPT -> 9-slice)](phase-02-adventure-ui-kit-sheets.md) | Chưa | 3-4 |
| 03 | [Menu từng màn (31 màn)](phase-03-menus-reskin-per-screen.md) | Chưa | 6-8 |
| 04 | [HUD mobile + PC hotbar + chữ thế giới](phase-04-hud-pc-hotbar-world-text.md) | Chưa | 2-3 |
| 05 | [Nhân vật 6 class](phase-05-heroes-six-classes-redraw.md) | Warrior V2 (2,65 đầu) idle / đi / chém 8 hướng XONG 22/11, chờ người dùng thử; 4 class còn lại chưa | 10-14 |
| 06 | [Icon item / trang bị / skill](phase-06-item-gear-skill-icons.md) | Chưa | 3-4 |
| 07 | [Map, quái, FX: đối chiếu + build](phase-07-maps-monsters-fx-alignment-build.md) | Chưa | 2 (+ map tuỳ quyết định) |

Tổng ~27-36 phiên. Thứ tự: 01 -> 02 -> (03 túi đồ làm mẫu, người dùng duyệt) -> 05 Warrior đủ bộ (duyệt) -> 03/04 còn lại song song 05 các class khác -> 06 -> 07.

## Rủi ro

- ChatGPT lệch tông giữa các lần vẽ (về pastel hoặc quá tối): mọi prompt dán khối luật + gửi kèm `adv-warrior-idle-down-chatgpt-1.png` + `02-bag.png`; chấm checklist 10 điểm.
- Vẽ lại nhân vật phá anchor / nắm tay / tóc nhuộm đã cài (sổ lỗi sprite-rules: đổi tay, to đầu, vũ khí đè tay): giữ ô 320x256, feetY 244, bodyPx 200, chạy lại `build-ro-fantasy-hero-game-assets.py` `fists` / `grips` / `check`.
- Font mới (Noto Serif + Be Vietnam Pro) đổi độ rộng chữ => tràn nút; đo lại theo `docs/ui-layout-rules.md` mục 2.
- Ổ D: còn ~2,7 GB: build chỉ từ `D:\temp\zu-copy-*` có sẵn, xoá exe `D:\temp\player-*` cũ trước.
- 2 test Frost Hollow đang lỗi từ nhánh cũ (không liên quan, sửa riêng).

## Câu hỏi mở

Xem cuối `phase-05` và `phase-07` (map giữ nguyên hay ấm lên; Fighter có đưa vào hệ nhân vật nhiều lớp; tiếng Việt hay tiếng Anh cho UI; giữ cờ chuyển Fantasy hay xoá hẳn).

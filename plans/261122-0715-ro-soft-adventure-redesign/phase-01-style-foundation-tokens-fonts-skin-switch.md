# Phase 01: Nền style (luật, token, font, công tắc skin)

Ưu tiên: P0. Trạng thái: luật + style guide XONG 22/11; code chưa.

## Bối cảnh
- `docs/reference/ro-soft-adventure-redesign/style-guide-chatgpt.md` (palette, khung, chữ, icon, nhân vật, cấm, checklist).
- `Assets/Scripts/RoSoftTheme.cs` (8 màu Fantasy + màu suy ra + font Nunito qua `PaperTheme`), `RoSoftSprites.cs` (`Folder`), `RoSoftSkin.cs`.

## Việc
1. [x] Khối luật Adventure đầu `docs/sprite-rules.md` (thay điều 0 Fantasy trên nhánh này) và `docs/ui-layout-rules.md`.
2. [ ] `RoSoftTheme`: thêm bộ token Adventure (Paper #F4EBDD, PaperLight #FFF8ED, PaperDark #DFD0BA, WoodDark #75523C, Ink #514137, Text #342F2B, TextSoft #776C60, Bronze #B88A4B, Teal #328C81, Gold #C8923D, Red #B85D50, Blue #527E9C, rarity, HP/MP/EXP). Giữ TÊN thuộc tính cũ (Ink, Mint, Sky, Coral, Gold...) trỏ sang màu Adventure tương ứng (Mint->Teal, Sky->Blue, Coral->Red, CloudWhite->Paper) để mọi màn đổi màu không cần sửa từng file; `OnLight` / `FillOnLight` map sang nâu thay navy / sky.
3. [ ] Skin switch: `RoSoftSkinMode { Fantasy, Adventure }` mặc định Adventure trên nhánh này, cờ `--ui-skin fantasy` để chụp A/B. `RoSoftSprites` nạp `Sprites/ui/ro-adventure/<mảnh>` trước, thiếu thì `ro-soft/`.
4. [ ] Font: Noto Serif (SemiBold/Bold, tiêu đề) + Be Vietnam Pro (Regular/SemiBold/ExtraBold) từ Google Fonts (OFL), vào `Assets/Resources/Fonts/`; `RoSoftTheme.Title/Body/Bold/Number`. Kiểm dấu tiếng Việt. Damage number giữ font sprite hiện có (đổi sau nếu cần).
5. [ ] Compile qua bản sao (`Tools/build-from-copy.ps1`, bản sao `D:\temp\zu-copy-*`), không build vào project đang mở.

## Thành công
Mở bất kỳ màn RO nào: nền giấy kem, chữ nâu đen, nút teal / vàng / đỏ, không còn xanh trời / tím pastel, dù sprite chưa vẽ lại.

## Rủi ro
Màu sprite cũ (viền xanh) lệch với token nâu trong giai đoạn chuyển: chấp nhận tạm, phase 02 thay sprite.

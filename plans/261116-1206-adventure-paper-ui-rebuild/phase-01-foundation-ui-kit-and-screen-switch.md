# Phase 01: Nền tảng (UI kit Adventure Paper, font, công tắc từng màn, hook chụp ảnh)

## Context Links
- `docs/reference/ui-redesign-261116/chatgpt/style-board-adventure-paper.png` (màu, khung, nút, tab, ô, thanh, badge)
- Code hiện có: `Assets/Scripts/UiKit.cs` (canvas 960x640, Rect/Label/Bar), `UiWinKit.cs` (sliced/plain/button), `UiArt.cs` (load `Sprites/ui/*.png` + bảng border 9-slice), `BagWidgets.cs` (font Crimson Text), `ChatArt.cs` (Inter cho tiếng Việt trên WebGL), `UiWindows.cs` (danh sách modal)
- Phase 02 (sheet K1-K4 cấp art cho kit)

## Overview
- Ưu tiên: P1, chặn mọi phase UI. Effort: M. Status: done 16/11 (chưa commit, chưa code review).
- Thêm một lớp kit mới dùng chung, không đổi `UiKit` cũ (cửa sổ khác như Settings, Quest, Shop vẫn dùng). Thêm bộ định tuyến mở cửa sổ có công tắc cũ / mới theo từng màn.

## Key Insights
- Toàn bộ UI dựng bằng code, không prefab: giữ nguyên cách này (KISS, đồng bộ codebase). Không thêm TextMeshPro (đổi toàn bộ Text là việc riêng, YAGNI).
- `UiArt.Get` tạo Sprite từ Texture2D lúc chạy với border truyền tay: kit mới dùng cùng cơ chế, border khai báo trong một bảng.
- 1 đơn vị canvas = 1,69 px ở 1080p, 2,25 px ở 1440p. Art gốc nên ~2,5x đơn vị canvas. Đo trên mockup (1672x941): đơn vị canvas ≈ 0,68 x px mockup, nên px art gốc ≈ 1,7 x px mockup.
- Bề rộng canvas theo tỉ lệ màn: 4:3 = 853, 16:9 = 1138, 20:9 = 1422, 21:9 (3440x1440) = 1529 đơn vị. Màn "nơi chốn" phải co giãn theo dải này.
- Mở cửa sổ đang rải ở `RpgSession.cs` (B/T/P/G), `TouchControls.Menu.cs`, `InventoryPanel.cs` (tab Skill), `PetWindow.cs` (nút Gacha), `TownNpc.cs` (Lumi), `WindowsHost.cs`, `DebugCapture*.cs`.

## Requirements
- Chức năng: kit có khung 9-slice (giấy, giấy ghim, gỗ, bảng tối), nút 3 màu (vàng chính, xanh phụ, đỏ nguy hiểm) x thường / nhấn + xám không dùng được, tab (ngang da, dọc ván gỗ), ô vật phẩm + thẻ vật phẩm theo 5 độ hiếm + chọn + khoá, badge (số, "!", chấm đỏ, ruy băng chữ), thanh (khung + fill tô màu), chip tài nguyên, sao, mũi tên trang, chấm trang, tiêu đề bảng gỗ, ruy băng tiêu đề.
- Token màu từ style board: nâu `#4A2E1F`, giấy `#F6E6C8`, kem, đỏ, xanh, lục, navy, vàng, tím (lấy mã đúng từ ảnh bằng tool khi cắt K1).
- Font: tiêu đề Baloo 2 ExtraBold, nội dung Nunito (Google Fonts, SIL OFL, kèm license), phải có dấu tiếng Việt; WebGL chat giữ Inter.
- Công tắc màn: `UiScreens` có cờ cho Bag, Skill, Pet, Gacha, Chat, Hud, Boss, Wanted. Mặc định = cũ cho tới khi người dùng duyệt màn đó; cờ chạy `--ui new|old` và `--ui-new bag,skill` để chụp A/B.
- Phi chức năng: mỗi file < 200 dòng; không cấp phát mỗi frame (theo mẫu RpgHud: chỉ dựng lại chữ khi giá trị đổi).

## Architecture
```
UiScreens (router) --Open/Toggle(Bag|Skill|Pet|Gacha|WantedBoard)--> view mới (cờ bật) | cửa sổ cũ (cờ tắt)
PaperKit (dựng widget) -> PaperArt (load + border + cache) -> Resources/Sprites/ui/paper/**.png
PaperTheme (token màu, cỡ chữ, font) ; PaperLayout (khung an toàn 16:9 giữa màn, safe area, co theo 4:3, nền phủ kín 21:9)
UiWindows.RegisterModal(Func<bool>) : màn mới tự đăng ký, không ai phải sửa UiWindows.cs nữa
DebugCapture.UiRedesign.cs : partial void hook cho từng màn (phase sau tự cài phần thân trong file riêng)
```
Luồng dữ liệu: input (phím / chạm / cờ chụp) -> `UiScreens` -> view -> đọc `PlayerState` / `AccountState` (đã có) -> hành động qua lớp actions có sẵn (`SkillTreeActions`, `PetActions`, logic thùng đồ tách ở phase 05) -> offline lưu file / online gửi server -> view vẽ lại khi dấu vân tay trạng thái đổi.

## Related Code Files
- Tạo (để phẳng trong `Assets/Scripts/` như `UiKit.cs`, tránh rơi vào asmdef `Rpg/` hay `Net/`): `PaperTheme.cs`, `PaperArt.cs`, `PaperKit.Frames.cs`, `PaperKit.Buttons.cs`, `PaperKit.Slots.cs`, `PaperKit.Bars.cs`, `PaperKit.Badges.cs`, `PaperLayout.cs`, `Assets/Scripts/UiScreens.cs`, `Assets/Scripts/DebugCapture.UiRedesign.cs`
- Tạo font: `Assets/Resources/Fonts/Baloo2/Baloo2-ExtraBold.ttf`, `Assets/Resources/Fonts/Nunito/Nunito-{Regular,Bold}.ttf` + `OFL.txt`
- Sửa (chỉ phase này): `UiWindows.cs` (RegisterModal), `RpgSession.cs`, `TouchControls.Menu.cs` (chỉ đổi lời gọi sang UiScreens), `InventoryPanel.cs` (tab Skill gọi UiScreens), `PetWindow.cs` (nút Gacha), `TownNpc.cs`, `WindowsHost.cs`, `DebugCapture.cs` (gọi hook + đọc `--ui`)
- Không xoá gì.

## Implementation Steps
1. Tải Baloo 2 + Nunito từ Google Fonts, kiểm tra glyph tiếng Việt (ă â đ ê ô ơ ư + 5 dấu) bằng ảnh thử; nếu Baloo 2 thiếu thì dùng Nunito Black cho tiêu đề.
2. `PaperTheme`: token màu, cỡ chữ (tiêu đề 22 / mục 15 / nội dung 13 / số trên thanh 11 đơn vị), viền chữ (Outline nâu đậm trên nền sáng, bóng đen trên nền game).
3. `PaperArt`: như `UiArt.Get` nhưng thư mục `Sprites/ui/paper/`, bảng border 9-slice theo tên mảnh, cache.
4. `PaperKit.*`: hàm dựng từng widget (khung, nút màu + trạng thái, tab, ô + thẻ độ hiếm, badge, thanh có khung + fill trắng tô màu, chip tài nguyên). Trước khi có art: fallback màu phẳng để code chạy được, log cảnh báo thiếu sprite (đã có mẫu ở UiArt).
5. `PaperLayout`: gốc màn toàn cảnh = nền phủ kín (cover, cắt mép) + khung nội dung 16:9 đặt giữa, co theo chiều rộng khi màn hẹp hơn 16:9, giới hạn trong `Screen.safeArea`.
6. `UiScreens`: Open/Close/Toggle/IsOpen mỗi màn; đọc cờ; đổi mọi lời gọi cũ sang đây (grep `InventoryPanel.Toggle|Open`, `SkillTreeWindow.Open`, `PetWindow.Open|Toggle`, `PetGachaWindow.Open|Toggle`).
7. `UiWindows.RegisterModal` + giữ danh sách cũ.
8. `DebugCapture.UiRedesign.cs`: khai báo `partial void DebugUiHud(); DebugUiBag(); ...` và gọi chúng từ `DebugCapture.cs` sau bước spawn (cạnh `DebugPetWindows()`), thêm cờ `--ui`, `--ui-new`, `--kit-sheet` (mở một màn mẫu bày mọi widget kit để chụp).
9. Build từ bản sao (`Tools/build-from-copy.ps1`), chụp `--kit-sheet` ở 1920x1080, 2400x1080, 1600x1200, 3440x1440 (tham số player `-screen-width/-screen-height -screen-fullscreen 0`), tự xem ảnh.

## Todo List
- [x] Font + license + ảnh thử dấu tiếng Việt (bản static cắt từ font biến thiên Google Fonts bằng fontTools; đủ glyph Việt)
- [x] PaperTheme, PaperArt (+ `PaperArt.Pieces.cs` tên mảnh, `PaperArt.Borders.cs` sinh từ json), PaperKit.* , PaperLayout
- [x] UiScreens + đổi mọi lời gọi mở cửa sổ (trừ `TradeWindow.cs`, `DebugCapture.Pets.cs`, `DebugCapture.Skills.cs`: xem Ghi chú)
- [x] UiWindows.RegisterModal (+ CloseRegistered, RegisteredOpen)
- [x] Hook DebugCapture + cờ `--ui`, `--ui-new`, `--kit-sheet`, thêm `--ui-open`, `--ui-keys`
- [x] Build bản sao, chụp 16:9 / 20:9 / 4:3 / 21:9, xem ảnh (`docs/reference/ui-redesign-261116/phase-01-kit/`)
- [ ] `dotnet test` cả 2 bộ: không chạy, phase này không sửa file nào hai bộ test biên dịch (Rpg/, Net/Protocol, Maps/)

## Ghi chú sau khi làm (16/11)
- Tên mảnh art lấy theo tool cắt của làn Art, gom ở `PaperArt.Pieces.cs` (`PaperPiece`). Border: chạy `python Tools/export-ui-paper-borders-to-csharp.py` sau mỗi lần cắt mảnh 9-slice mới (json là nguồn, file C# là bản sinh).
- Mảnh chưa có art, đang vẽ fallback phẳng: `kit/chip-resource` (chip tài nguyên), `kit/slot-item-selected-frame` (viền chọn của ô vuông).
- Thẻ vật phẩm theo art đã cắt là 88 x 112u (không phải 88 x 122u). Thanh: rãnh fill chỉ ~46% chiều cao khung, thanh có chữ nên cao >= 26u.
- Phase sau bật màn: `UiScreens.Register(UiScreen.X, open, close, isOpen)` từ `[RuntimeInitializeOnLoadMethod]` trong file view; chụp bằng `--ui-new x`; khi người dùng duyệt thì đổi mặc định trong `UiScreens.useNew`. NPC mở màn: `UiScreens.BindNpc(npcId, UiScreen.X)`. Cờ chụp riêng: thân `partial void DebugUi<X>()` trong `DebugCapture.Ui<X>.cs`.
- Việc để lại cho phase sau: (1) `InputAdapter.Gated` chặn phím B khi có modal, nên B không đóng được Thùng đồ mới (Esc đóng được): phase 05 thêm ngoại lệ như phím T. (2) `TradeWindow.cs:259` đóng thẳng InventoryPanel / SkillTreeWindow, chưa đóng màn mới: thêm `UiWindows.CloseRegistered()` khi làm phase 05. (3) Huy chương skill (`kit/skill-ring-*`), dropdown, icon tiền đã cắt nhưng kit chưa có widget: phase 05 / 06 thêm khi dùng.
- Màn hình máy build cao 1080 px: cửa sổ 1600x1200 và 3440x1440 bị Windows cắt còn 1080 dòng, nên tỉ lệ 4:3 và 21:9 chụp ở 1440x1080 và 2580x1080.

## Success Criteria
- Với cờ mặc định, mọi cửa sổ cũ mở / đóng y như trước (chụp lại `ingame/*.png` cùng cờ, so bằng mắt).
- `--kit-sheet` hiện đủ widget, không vỡ 9-slice ở 4 tỉ lệ; chữ Việt có dấu đúng.
- Không file mới nào > 200 dòng; Editor.log lần biên dịch mới nhất không lỗi.

## Risk Assessment
| Rủi ro | Khả năng x Tác động | Giảm thiểu |
|---|---|---|
| Đổi lời gọi mở cửa sổ làm hỏng phím B/T/P/G | Trung bình x Cao | Chỉ đổi lời gọi, không đổi logic; test tay + chụp mỗi phím |
| Font thiếu glyph Việt / WebGL nặng thêm | Thấp x Trung bình | Thử glyph trước; WebGL giữ Inter cho chat |
| Sprite.Create lúc chạy không gom atlas, nhiều draw call | Trung bình x Trung bình | Đo bằng `--perf`; chỉ khi vượt ngưỡng mới gộp mảnh nhỏ thành 1 sheet + bảng rect (YAGNI tới lúc đo) |

## Security Considerations
- Không có. Font OFL kèm license; không tải tài nguyên lúc chạy.

## Next Steps
- Mở khoá 03, 05, 07, 08 (khi có art). Ghi token + quy tắc kit vào `docs/sprite-rules.md` mục UI (phase 13).

## Câu hỏi mở
- Không.

# Phase 13: Dọn dẹp - bỏ view cũ, art cũ, docs, rà mọi tỉ lệ màn

## Context Links
- Tất cả phase 03-11; `docs/sprite-rules.md` (Sổ lỗi), `docs/pending-tasks.md`, `CLAUDE.md` (asset đúng chỗ, `git mv` kèm `.meta`, `Resources.Load` theo chuỗi)

## Overview
- Ưu tiên: P2. Effort: S. Status: pending. Chạy theo từng màn ngay khi màn đó được người dùng duyệt (không đợi hết).

## Key Insights
- Công tắc `UiScreens` chỉ là giàn giáo: màn đã duyệt thì bật mặc định, rồi xoá view cũ + cờ của màn đó (không giữ 2 bản lâu dài, theo quy tắc "không tạo file enhanced").
- Art cũ còn người dùng: `win-*` (hộp thoại NPC, Quest, Shop, Trade, Settings, Party chưa làm lại) -> giữ. `hud-*`, `UiSprites` (đĩa vẽ code), `bag-frame.png`, `bag-icon-v3.png`, Crimson Text: xoá khi grep không còn ai gọi.
- Cửa sổ chưa nằm trong phạm vi (Settings, Quest log, Shop, Trade, Party, hộp thoại NPC, màn chọn hero) sẽ còn style cũ -> ghi thành việc tiếp theo, không làm ở đây (YAGNI).

## Requirements
- Mỗi màn đã duyệt: xoá view cũ, xoá nhánh trong `UiScreens`, xoá art / font không còn tham chiếu (grep chuỗi `Resources.Load` và tên file trong `Assets/` + `Tools/`).
- Rà cuối 4 tỉ lệ (1920x1080, 2400x1080, 1600x1200, 3440x1440) + 1 lần WebGL (nếu có build web): HUD, mọi màn, Wanted online.
- Docs: thêm mục "UI Adventure Paper" vào `docs/sprite-rules.md` (token, cỡ art, quy tắc chia sheet theo màu nền, tool cắt) + các dòng Sổ lỗi phát sinh; cập nhật `docs/pending-tasks.md`.

## Architecture
- Không có thành phần mới.

## Related Code Files
- Xoá (khi hết tham chiếu): `InventoryPanel*.cs` (view), `BagWidgets.cs`, `SkillTreeWindow.cs`, `SkillTreeNodeView.cs`, `SkillTreeDetailPane.cs`, `SkillTreeLinks.cs`, `PetWindow*.cs` (view), `PetGachaWindow*.cs`, `UiSprites.cs`, art `Sprites/ui/hud-*.png`, `bag-frame.png`, `bag-icon-v3.png`, `Fonts/CrimsonText/*`, `win-chat-input.png`, `win-send.png` (mỗi file kiểm grep trước)
- Sửa: `UiScreens.cs`, `docs/sprite-rules.md`, `docs/pending-tasks.md`

## Implementation Steps
1. Màn được duyệt -> bật mặc định -> build + chụp lại -> xoá view cũ + cờ -> build lại (Editor.log lần mới nhất không lỗi) -> `dotnet test` 2 bộ.
2. Grep từng art / font cũ; xoá kèm `.meta` bằng `git rm`.
3. Rà 4 tỉ lệ + WebGL; tự xem mọi ảnh; liệt kê phần chưa xem trong game thật.
4. Cập nhật docs.

## Todo List
- [ ] Bag cũ xoá
- [ ] Skill cũ xoá
- [ ] Pet + Gacha cũ xoá
- [ ] Chat / HUD / boss bar IMGUI cũ xoá
- [ ] Art + font cũ hết tham chiếu xoá
- [ ] Rà 4 tỉ lệ + WebGL
- [ ] Docs

## Success Criteria
- Không còn công tắc cũ / mới; không file nào > 200 dòng trong phần mới; không sprite "missing" trong log khi mở mọi màn.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Xoá art còn được `Resources.Load` bằng chuỗi ghép | Trung bình x Trung bình | Grep cả phần tên (ví dụ `"hud-"`), chạy mọi cờ chụp sau khi xoá, xem log missing sprite |

## Security Considerations
- Không có.

## Next Steps
- Việc sau (ngoài phạm vi): làm lại Settings / Quest / Shop / Trade / Party / hộp thoại NPC theo Adventure Paper; gamepad dùng bố cục PC + đổi nhãn phím.

## Câu hỏi mở
- Không.

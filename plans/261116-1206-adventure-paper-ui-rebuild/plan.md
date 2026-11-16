---
title: "Dựng lại UI theo mockup Adventure Paper (mỗi màn là một nơi chốn)"
description: "UI kit Adventure Paper, HUD mới, thanh máu boss, Armory Carousel, Training Hall, Pet Camp, quầy Lumi, chat bảng gỗ, Wanted Boss đầy đủ."
status: pending
priority: P1
effort: ~46 agent-day (S=1, M=3, L=6)
branch: unity
tags: [ui, ux, hud, wanted-boss, chatgpt-art, unity]
created: 2026-11-16
---

# Dựng lại UI Adventure Paper

Nguồn: mục "16/11: Thiết kế lại UI" trong `docs/pending-tasks.md`; mockup `docs/reference/ui-redesign-261116/chatgpt/*.png` (bỏ 2 thư mục con; `boss-hp-bar.png` đã vẽ lại bỏ Hunters, `wanted-fight-full-mobile.png` đang chờ bản mới); ảnh hiện tại `.../ingame/`. Quyết định người dùng 16/11 12:06: làm theo mọi mockup, không hiện số người đánh boss, không bước nhận truy nã, làm tính năng Wanted mới, nhặt đồ chờ duyệt.
Hiện trạng: UI dựng bằng code uGUI (`UiKit`, `UiWinKit`, `UiArt`, canvas tham chiếu 960x640 match height), riêng boss bar + debug HUD là IMGUI (`Hud.cs`). Không prefab, không TextMeshPro, font legacy `Text`.

## Thứ tự và lý do
Kit trước (mọi màn dùng chung). Sau đó **HUD trạng thái + thanh boss làm màn thử kit** (nhỏ, thấy suốt lúc chơi, lộ lỗi 9-slice / font / tỉ lệ màn rẻ nhất), rồi **Thùng đồ, Skill** (người dùng ưu tiên nhất). Server Wanted chạy song song từ đầu (không đụng file UI).

| # | Phase | Effort | Status | Phụ thuộc |
|---|-------|--------|--------|-----------|
| 01 | [Nền tảng: UI kit, font, công tắc từng màn, hook chụp ảnh](phase-01-foundation-ui-kit-and-screen-switch.md) | M | done 16/11 (chờ review + commit) | - |
| 02 | [Art ChatGPT: danh sách sheet, mockup trạng thái thiếu, tool cắt](phase-02-chatgpt-art-sheets-and-cut-tool.md) | L (chạy suốt) | pending | - |
| 03 | [HUD trạng thái người chơi, toast, thanh máu boss](phase-03-hud-player-status-toasts-boss-bars.md) | M | code xong 16/11, build + ảnh OK (chờ review + commit, công tắc tắt) | 01, sheet K*, H1, B1 |
| 04 | [HUD nút vào (Bag/Skill/More), cảm ứng, hotbar PC](phase-04-hud-entry-buttons-touch-controls-pc-hotbar.md) | M | code xong 16/11, build + ảnh OK (chờ review + commit, công tắc tắt) | 01, 03 (neo layout), H2, H3 |
| 05 | [Thùng đồ Armory Carousel](phase-05-bag-armory-carousel.md) | L | code xong 16/11, công tắc tắt (chờ duyệt; chưa commit / review / online) | 01, A1 + mockup trạng thái thiếu |
| 06 | [Skill Training Hall](phase-06-skill-training-hall.md) | M | code xong 16/11, build + ảnh OK, công tắc tắt (chờ duyệt; chưa commit / review / online) | 01, S1, 05 (kệ thẻ / bệ dùng chung) |
| 07 | [Chat bảng gỗ](phase-07-chat-notice-board.md) | M | code xong 16/11 sau công tắc (chờ art C1 / H2, duyệt, review, commit) | 01, C1 |
| 08 | [Pet Camp (Team + 3 tab chờ mockup)](phase-08-pet-camp.md) | L | code xong 16/11 sau công tắc, 4 tab theo mockup, build + ảnh OK (chờ duyệt, review, commit, online) | 01, P1 + 3 mockup tab |
| 09 | [Gacha quầy Lumi](phase-09-pet-gacha-lumi-counter.md) | M | code xong 16/11, build + ảnh OK, công tắc tắt (chờ duyệt; chưa commit / review / online) | 08 (nhãn độ hiếm), G1 + mockup kết quả |
| 10 | [Wanted: luật chung, tin trạng thái server, protocol 22, test](phase-10-wanted-shared-rules-server-status-protocol.md) | M | code + test xong (chưa commit, chưa build / e2e) | - |
| 11 | [Wanted UI: bảng, thẻ HUD, thông báo, dấu minimap, dẫn đường](phase-11-wanted-board-hud-card-notifications.md) | L | code xong 16/11, ảnh OK, 1 lần online thật (chờ review + commit, công tắc tắt; chưa đổi icon trạng thái trên đầu) | 03, 04, 10, W1 |
| 12 | [Nhặt đồ (giữ chỗ, chờ ChatGPT + người dùng)](phase-12-loot-pickup-placeholder.md) | S? | blocked | luật nhặt đồ |
| 13 | [Dọn dẹp: bỏ view cũ, art cũ, docs, rà mọi tỉ lệ màn](phase-13-cleanup-old-views-docs-final-pass.md) | S | pending | 03-11 đã duyệt |

## Làn song song (sở hữu file nghiêm ngặt, chi tiết trong từng phase)
- Làn Art: 02 (một người, một tài khoản ChatGPT tại một thời điểm = nút cổ chai).
- Làn A: 03 -> 04 -> 11. Làn B: 05 -> 06. Làn C: 07, rồi 08 -> 09. Làn D: 10 (bắt đầu ngay).
- File dùng chung chỉ phase 01 sửa: `UiWindows.cs`, `DebugCapture.cs`, `RpgSession.cs`, `WindowsHost.cs`, `TownNpc.cs` (định tuyến mở cửa sổ). Mỗi phase thêm cờ chụp trong file `DebugCapture.Ui<Màn>.cs` riêng (partial hook).
- File hai phase cùng sửa thì chạy nối tiếp: `NotificationToast.cs` (03 rồi 11), `WantedBossTracker.cs` (10 rồi 11).
- Phase 08 / 09 chờ Pet V2 (agent pet-gacha, chưa commit) để tránh xung đột `PetWindow*.cs`, `PetConfig.cs`.

## Rủi ro chính
- Tỉ lệ màn: canvas 960x640 match height -> rộng 853u (4:3) tới 1529u (21:9). Màn toàn cảnh = nền phủ kín + khung nội dung 16:9 giữa, co khi hẹp; HUD neo góc trong safe area. Chụp 4 cỡ mỗi phase.
- Hiệu năng nền toàn cảnh: 1 ảnh/màn, load khi mở, unload khi đóng, nén theo nền tảng; game đã tạm dừng khi mở cửa sổ. Đo `--perf` trước khi tối ưu thêm.
- Độ dễ đọc: chữ code vẽ (không vẽ chữ trong art), viền chữ theo nền, cỡ tối thiểu 11u.
- Lượt vẽ ChatGPT là nút cổ chai; code dùng fallback màu phẳng để không bị chặn.
- Mỗi file code < 200 dòng, tách theo partial / mối quan tâm (đặt tên trong từng phase).

## Giả định cần người dùng xác nhận
1. Thùng đồ có nút chuyển sang dạng lưới để xem đủ 50 ô (kệ thẻ chỉ thấy ~10).
2. Skill có nút riêng trên HUD (thẻ treo), Gacha nằm trong menu More (bỏ tab Skill trong Bag, nút Gacha trong Pet vẫn giữ làm lối tắt).
3. Nút cảm ứng giữ vị trí hiện tại, chỉ đổi hình; đai skill vòng cung (theo `hud-touch-controls.png`, không theo ảnh tổng vẽ ngang).
4. PC dùng huy hiệu khiên như `hud-player-status.png` (bản gọn, không EXP); EXP trên PC chỉ ở dưới hotbar; chat PC ở góc dưới trái.
5. Thanh trên cùng thùng đồ hiện tài nguyên game có thật (vàng, đá cường hoá, bình HP/MP), không có "gem".
6. Bệ Pet trong thùng đồ hiện pet Combat, chạm mở Pet Camp.

## Rollback
Mỗi màn mới có công tắc riêng (`UiScreens`, phase 01) + chạy song song view cũ tới khi người dùng duyệt; mỗi phase một commit riêng, revert độc lập. Phase 10 đổi protocol: revert cả client + server cùng lúc.

## Câu hỏi mở: xem cuối mỗi phase và báo cáo tổng.

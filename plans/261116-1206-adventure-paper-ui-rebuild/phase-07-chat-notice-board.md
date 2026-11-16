# Phase 07: Chat "bảng gỗ"

## Context Links
- Mockup: `chatgpt/chat-final-mockup-notice-board.png` (+ bản nhỏ PC / mobile xin ở phase 02)
- Hiện tại: `ingame/chat-{web,mobile}-{open,collapsed,typing}.png`
- Code: `ChatWindow.cs` (167, layout theo HudLayout, safe area, Enter / Esc / Tab), `ChatPanelView.cs`, `ChatCollapsedView.cs` (3 dòng mờ dần + nút + badge), `ChatArt.cs` (font Inter WebGL), `ChatTextFormat.cs`, `ChatHub.cs`, `WebChatInput.cs` (ô nhập HTML trên WebGL), `PartyPlayerMenu.cs` (menu chạm tên)
- Phase 01, 02 (C1), 04 (`HudAnchors`)

## Overview
- Ưu tiên: P2. Effort: M. Status: code xong 16/11 sau công tắc `--ui-new chat` (mặc định tắt), dùng art kit + fallback phẳng; chờ sheet C1 / H2, người dùng duyệt, review, commit.
- Thu gọn: tin nổi (tối đa 4 dòng, màu kênh) + nút chat gỗ có badge. Mở: bảng gỗ, ruy băng kênh dọc (All / World / Map / Party / Whisper), chip lọc kênh, dòng có avatar, chạm tên ra menu (Whisper / Invite to Party / View Info), chọn kênh gửi cạnh ô nhập, Send, "đang gõ".

## Key Insights
- Kênh, badge chưa đọc, Enter / Esc / Tab, ô nhập HTML WebGL, safe area: đã có, giữ nguyên.
- Game CHƯA có: kênh "All" gộp + chip bật tắt từng kênh (logic lọc nhỏ phía client); avatar người gửi (tin chat không mang class -> **không làm avatar** trừ khi người dùng muốn, vì phải đổi wire); "Mira is typing..." (cần tin mạng mới -> **bỏ**, YAGNI); menu chạm tên: dùng lại `PartyPlayerMenu` (đã có Whisper / Invite / View info?) - kiểm tra trước khi làm.
- Mở trên PC che thanh skill, trên mobile còn to (ChatGPT tự lưu ý): dùng bản nhỏ theo mockup bổ sung; trên PC đặt góc dưới trái (giả định 4), chiều cao không vượt mép trên hotbar.

## Requirements
- Thu gọn: tin nổi hiện 7 s rồi mờ (giữ), nhãn kênh màu theo mockup (World vàng, Party tím, Whisper hồng, Map lục); nút chat theo `HudAnchors`.
- Mở: ruy băng kênh chọn kênh xem; chip lọc khi ở All; ô nhập giấy + dropdown kênh gửi + Send vàng; X đỏ đóng.
- Không che hotbar (PC) và nút đánh (mobile); bàn phím mobile lên thì ẩn điều khiển (giữ).

## Architecture
```
ChatHub (tin) ──> ChatWindow (điều phối, giữ) ──> ChatCollapsedView / ChatPanelView (đổi hình bằng PaperKit + C1)
ChatChannelFilter (mới, thuần): All + bật / tắt kênh -> danh sách dòng hiển thị
chạm tên ──> PartyPlayerMenu (có sẵn)
```

## Related Code Files
- Tạo: `ChatChannelFilter.cs`, `ChatPanelView.Ribbons.cs` (ruy băng kênh + chip), `DebugCapture.UiChat.cs`
- Sửa: `ChatWindow.cs`, `ChatPanelView.cs`, `ChatCollapsedView.cs`, `ChatArt.cs`
- Không sửa: `ChatHub.cs`, `WebChatInput.cs` (trừ vị trí ô HTML nếu khung đổi: thì sửa, ghi lại)
- Xoá (phase 13): art `win-chat-input.png`, `win-send.png` nếu hết dùng

## Implementation Steps
1. Nhận mockup bản nhỏ.
2. `ChatChannelFilter` + kênh All.
3. Đổi hình ChatCollapsedView (tin nổi, nút gỗ, badge K4).
4. Đổi hình ChatPanelView (bảng gỗ 9-slice, ruy băng, chip, ô nhập, dropdown kênh, Send).
5. Nối chạm tên -> PartyPlayerMenu.
6. Cờ chụp: dùng `--chat-demo`, `--open-chat`, `--close-chat`, `--chat-focus`, `--hud web|mobile`; mới `--chat-channel all|world|party`, `--chat-name-menu`.
7. Build bản sao, chụp 2 HUD x 3 trạng thái x 4 tỉ lệ; thử gõ tiếng Việt (Telex) trên bản WebGL nếu có build web; tự xem.

## Todo List
- [ ] Mockup bản nhỏ (làn Art chưa xin; cỡ bảng đang lấy theo vùng chat cũ, xem Ghi chú)
- [x] ChatChannelFilter + All
- [x] Thu gọn + nút badge (`PaperChatFeedView`; nút gỗ đang là fallback phẳng, chờ `hud/btn-chat-wood`)
- [x] Bảng mở + ruy băng + chip + ô nhập + dropdown kênh gửi + Send (`PaperChatPanelView*`; ruy băng, ô nhập là fallback phẳng, chờ C1)
- [x] Chạm dòng của người chơi khác -> `PartyPlayerMenu.OpenFor` (Invite / Trade). Chưa thấy chạy: chat chưa có đường mạng nên chưa có dòng của người khác
- [x] Cờ chụp (`DebugCapture.UiChat.cs`), ảnh 16:9 / 20:9 / 4:3, tiếng Việt, tự xem (`docs/reference/ui-redesign-261116/phase-07-chat/`)
- [ ] Bản WebGL (ô nhập HTML, Telex): chưa build, chưa thử

## Ghi chú sau khi làm (16/11)
- Hai giao diện dùng chung `ChatWindow` qua `ChatViewContracts.cs` (`ChatPanel`, `ChatFeed`): cũ = `ChatPanelView` + `ChatCollapsedView` (không đổi hình), mới = `PaperChatPanelView` (+ `.Ribbons`, `.Lines`, `.SendMenu`, `.Testing`) + `PaperChatFeedView`. `ChatWindow` đọc `UiScreens.UseNew(UiScreen.Chat)` mỗi frame và dựng lại view khi công tắc đổi, vì `--ui-new` được đọc SAU khi `WindowsHost` dựng chat (Bootstrap gọi `DebugCapture.AttachIfRequested` cuối cùng): phase 03 / 04 dùng `UseNew(Hud)` lúc dựng sẽ gặp đúng chuyện này.
- Chat hiện KHÔNG có đường mạng: chỉ có `LocalChatTransport` (dòng mình gõ dội lại), không có opcode chat trong `Net/`. Nên "gửi / nhận 4 kênh" = gửi cục bộ; Whisper chưa có người nhận. Menu chạm tên của mockup (Whisper / Invite to Party / View Info) vì vậy chỉ nối tới hộp có sẵn (Invite / Trade) khi người gửi đang ở cùng zone; mục Whisper và View Info chưa làm (chưa có tính năng nền).
- Cỡ: PC 340 x 232u ở góc dưới trái (hotbar đã tự tránh x = 346u, `TouchControls.WebHotbar`); mobile 440 x 276u dưới bảng trạng thái (đúng vùng chat cũ, không che joystick / bình / nút đánh). Khi phase 04 có `HudAnchors` thì đổi 2 chỗ neo trong `ChatWindow.Build` (nút chat) và `PaperChatPanelView.Build` (bảng).
- Mobile: bảng nằm dưới `RpgHud.BlockHeight` (HUD cũ 126u, HUD mới cao hơn) và tự thấp lại để đáy không quá 418u (mép trên joystick); đã chụp với `--ui-new chat,hud`.
- Build: ổ C: chỉ còn ~1,5 GB nên player thứ ba (3,1 GB) không chép nổi `resources.assets.resS` ("not enough space on the disk"); script vẫn biên dịch sạch trong bản sao. Ảnh chụp bằng `C:/temp/ui-p07-work/p07.ps1`: file nhỏ + data của `player-ui-p03`, DLL script của chính lần build đó, hard link tạm tới file 3 GB chỉ tồn tại trong lúc giữ khoá `zu-copy.lock`. Cần dọn ổ đĩa để có exe riêng của phase 07.
- Kênh xem (ruy băng) và kênh gửi (dropdown) tách nhau: chọn ruy băng một kênh thì gửi luôn kênh đó; ở All thì kênh gửi giữ nguyên. Tab lúc đang gõ = kênh gửi kế tiếp. Dòng System (quest, Wanted) hiện ở All và World. Chip chỉ hiện khi xem All; chip "All" = bật lại cả 4 kênh.
- Danh sách cuộn được (40 dòng của `ChatHub`), dòng dài tự xuống hàng; tự bám dòng mới nhất trừ khi người chơi đã cuộn lên.
- Tên mảnh chờ art trong `PaperArt.Pieces.cs`: `hud/btn-chat-wood`, `chat/ribbon-channel-white` (một ruy băng trắng, code tô màu theo kênh), `chat/icon-{all,world,map,party,whisper}`, `chat/input-paper`, `chat/title-quill`. Đang dùng art thật của kit: khung giấy viền gỗ, ruy băng đỏ "Chat", X đỏ, chip lọc, dropdown giấy, nút vàng, badge đỏ.
- Bỏ so với mockup: avatar, "đang gõ", nút mặt cười (game không có emoji), dropdown tối màu (kit chỉ có dropdown giấy), dấu tích xanh trên chip (dùng chip vàng / kem của kit).
- WebGL: ô `<input>` HTML (`ChatInput.jslib`) nền tối chữ trắng sẽ đè lên ô giấy lúc gõ; vị trí lấy từ `panel.field` nên tự khớp khung mới. Chưa build WebGL để xem.

## Success Criteria
- Gửi / nhận 4 kênh như cũ; All gộp đúng thứ tự thời gian; chip lọc đúng.
- Bảng mở không che hotbar PC ở 1920x1080, không che nút đánh mobile ở 20:9.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Ô nhập HTML WebGL lệch khung mới | Trung bình x Trung bình | Tính lại rect từ khung mới, chụp bản web |
| Chữ Việt mất dấu do font Baloo / Nunito | Thấp x Cao | Chat giữ Inter trên WebGL; desktop thử dấu |

## Security Considerations
- Không đổi lọc / giới hạn nội dung chat phía server. Chữ người chơi hiển thị không bật rich text (kiểm `ChatTextFormat` vẫn thoát thẻ).

## Next Steps
- Phase 13 dọn art chat cũ.

## Câu hỏi mở
- Có cần avatar người gửi không (phải thêm class vào tin chat, đổi protocol)? Đề xuất: không.
- Bỏ "đang gõ..."? Đề xuất: bỏ.

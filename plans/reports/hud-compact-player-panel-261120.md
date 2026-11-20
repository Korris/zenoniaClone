# HUD RO Soft: khung máu người chơi gọn + thanh Wanted boss không bị đè (20/11/2026)

Yêu cầu: "chắc thiết kế ống máu của người chơi ở góc trái nhỏ lại, hiện tại nó đè lên ống máu của wanted boss luôn rồi"; thêm: "bỏ hiện cái popup ở giữa today wanted đi".

## Nguyên nhân
- Khung trạng thái: chân dung 92 + bảng 256x96, mép phải x 322 đơn vị (canvas cao 640).
- Thanh boss: rộng 320 + chân dung 70 bên trái, luôn căn giữa màn hình, top 92.
- 1280x853 (rộng 960 đv): chân dung boss từ x 246 -> nằm dưới bảng. 4:3 (853 đv): che hết chân dung + tên. 16:9 vừa sát.

## Thiết kế (ChatGPT, Tan Pham)
Chat: https://chatgpt.com/c/6b001b80-4db0-83ec-b021-e76c99ef1fec . Mockup: `docs/reference/hud-compact-player-panel/chatgpt-mockup-compact-status-boss-bar.png`.
- Khung trạng thái (đv): chân dung 64, bảng 218x69, pill Lv 65x21 (chữ 13), nhãn HP/MP/EXP 12, thanh 154x14 cách 20 (số 11), ô buff 20. Đáy 116 (cũ ~152).
- Thanh boss: chân dung 52, cột 242: pill tên 22 (chữ 14), HP 18 (12), cast 18 (12), cách 4 / 5. Gợi ý né: phải 200x64 hoặc dưới 242x54 (chữ 12).
- Vị trí: dưới hàng menu (top 92), căn giữa màn hình nhưng kẹp vào dải trống [mép phải bảng + 8, mép trái cột minimap - 8]; gợi ý bên phải khi dải ≥ 510, không thì dưới thanh cast.
- Không cần art mới.
- Lưu ý: text ChatGPT và bảng số trong ảnh lệch nhau (ảnh ghi px 720p: bảng 176x84, thanh boss rộng 420); dùng bảng số chữ (đơn vị 640) vì khớp hệ toạ độ game.

## Code
- `Assets/Scripts/RoHud.cs`: hằng số khung gọn + `StatusRight`.
- `Assets/Scripts/RoHudStatus.cs`: bố cục gọn, pill Lv bo tròn đúng ở cao 21.
- `Assets/Scripts/RoHudBossBar.cs`: bố cục gọn, đặt nhóm theo dải trống mỗi khung hình, gợi ý đổi cỡ phải/dưới, `FitName` (tên tự giảm 14 -> 11 khi ENRAGED chiếm cuối hàng).
- `Assets/Scripts/RoHudMenu.cs`: dùng `StatusRight` (hàng menu ở 4:3 không còn bị thu nhỏ nhiều).
- `Assets/Scripts/WantedNotices.cs`: bỏ bật notice Login ("Today's Wanted" giữa trên, 5 s khi vào game). Thẻ WANTED dưới minimap (`RoHudWanted`) và bảng Truy Nã là phần khác, giữ nguyên. Cờ debug `--wanted-notice login` vẫn hiện được bản này.
- `Tools/wanted-boss-crowd-playtest.ps1`: thêm `-ScreenW/-ScreenH`.

## Kiểm
- Build từ bản sao: 0 lỗi, `C:/temp/player-hud/Zenonia.exe`.
- Chụp trận Wanted thật (8 bot, OutDir `C:\temp\hud-crowd`, cổng 7810/3210/27210, không đụng phiên của người dùng): Hydra Queen 1280x853, 1024x768 (cast + gợi ý dưới), 1560x720; Rimeclaw 1280x853, 1280x720 (gợi ý phải); Hydra ENRAGED 1280x853. Không chỗ nào đè. Solo (không boss) 1280x853, 1024x768 ổn.
- Log client: trước có `wanted notice: Login`, sau chỉ còn `Warning`.
- Phóng 3x khung trạng thái + thanh boss: chữ không chạm viền, pill bo tròn đều.
- `dotnet test` ZenoniaSkills 474 pass, ZenoniaNet 455 pass.
- Lỗi gặp khi làm (đã sửa, ghi Sổ lỗi `docs/ui-layout-rules.md`): thẻ ENRAGED đè đuôi pill tên dài (`bug-enraged-tag-over-name-pill.jpg`).

## Ảnh
`docs/reference/hud-compact-player-panel/`: `before-*`, `after-*`, `compare-before-after-mockup-4x3.jpg`, `zoom3x-*`, `zoom1x-boss-bar-enraged.png`.

## Chưa xem
- Điện thoại thật (safe area / tai thỏ), PC layout `--hud web`, WebGL.
- Tổ đội (RoHudParty dời lên theo đáy mới), hàng buff nhiều ô, cột More mở trong trận boss.
- Khác mockup: chân dung boss vẫn có ngôi sao của vòng + vương miện (như trước); gợi ý bên phải ở 16:9 nằm sát dưới chữ hàng menu (~6 đv).

## Câu hỏi
- Có muốn gợi ý né ở 16:9 luôn nằm dưới thanh (như 4:3) để cách xa hàng menu hơn không?

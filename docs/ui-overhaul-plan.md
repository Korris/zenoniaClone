# Làm lại toàn bộ UI + menu theo Adventure Paper (ChatGPT, 17/11/2026)

Người dùng: "các menu của chúng ta đã đổi qua style khác rồi ko còn navy và vàng ... chụp ít ảnh trong game cho nó xem" và "sẵn đây tôi muốn chỉnh lại toàn bộ UI + menu chụp tất cả các menu đang có trong game luôn đi". Chat ChatGPT (Tay): https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde.

Trạng thái: đề xuất + mockup, CHƯA code. Thay cho phần "navy + vàng" của `docs/social-interaction-ux.md` (giữ phần UX: Player Card, lời mời chung, Nearby, Pending, Social Settings).

## Ảnh

`docs/reference/ui-all-menus-261117/` (xem README.md trong đó; ảnh chụp ở `1-hien-tai/`, mockup ở `2-mockup-moi/`, so sánh ở `3-so-sanh-cu-moi/`):
- `01..21-*.png`: chụp từng màn trong game, 1920x1080, `--ui new` (bản build `D:/temp/player-261117-menu`, script chụp ở scratchpad `capture-all-menus.ps1`: mỗi màn một lần chạy với cờ DebugCapture, `--shots 5`).
- `22..29-*.png`: party / trade ở giao diện mới, 960x640 (`Tools/net-party-test.ps1 -Ui` và `Tools/net-trade-test.ps1 -Ui` thêm `--ui new` cho client).
- `sheet-A-rebuilt-adventure-paper.jpg`, `sheet-B-old-style-windows.jpg`, `sheet-C-old-style-party-trade.jpg`: 3 bảng ghép đã gửi ChatGPT.
- `chatgpt-paper-player-card-duel-request.png`, `chatgpt-paper-trade-window.png`: 2 mockup Adventure Paper của ChatGPT.

Đã là Adventure Paper: HUD mobile, menu More, chat gọn, thanh boss, thẻ Wanted, Armory Carousel, Training Hall, Pet Camp, Gacha Lumi, Wanted Board, danh sách party trên HUD.
Còn kiểu cũ (navy): Settings, Quest log, shop thuốc, shop đồ, hộp thoại NPC, chữ Level Up, chọn class / Change hero (nền đẹp, panel dưới kiểu cũ), menu Invite/Trade, bảng Party đang mời, bảng nhận lời mời, Trade Request, bàn phím vàng, cửa sổ Trade, xem đồ khi trade.
Chưa chụp: HUD bản PC (`--hud web` không hiện thanh nút nào: cần xem riêng), màn đăng nhập / danh sách nhân vật / tạo nhân vật (cần server-api).

## Nhận xét bộ mới

Tốt: bản sắc riêng (giấy kem, gỗ, cờ / ruy băng, nút bóng, Baloo), HUD gọn, các màn lớn cùng một "không khí", màu dễ phân vai, đã có UI kit.

Chưa đồng bộ:

| Vấn đề | Nhận xét |
|---|---|
| Thanh trên cùng | Màn thì đầy, màn thì rời rạc; chưa thống nhất chỗ đặt vàng / gem / nút đóng |
| Nút đóng | Chỗ X đỏ tròn, chỗ nút nhỏ khác kiểu |
| Cỡ chữ | Tiêu đề, nhãn phụ, mô tả item chưa cùng thang |
| Khoảng cách | Wanted rất thoáng, Pet Camp / Trade cũ hơi dày |
| Khung con | Ô item, ô thông tin, tab chưa cùng bán kính / độ dày viền |
| Màu nút | Ý nghĩa màu chưa nhất quán giữa các màn |

## Style guide ngắn

| Thành phần | Quy tắc |
|---|---|
| Khung 1 – Full window | Màn lớn: Armory, Training Hall, Pet Camp, Gacha, Trade. Giấy kem, viền gỗ, ruy băng tiêu đề, X đỏ góc phải |
| Khung 2 – Medium dialog | Shop, Settings, Quest log, bảng xã hội, lời mời. Cỡ vừa, header ruy băng, 1 vùng nội dung |
| Khung 3 – Small toast / pending | Thông báo ngắn, đang mời, xác nhận đã gửi |
| Nút vàng | Hành động chính (Accept, Use, Confirm) |
| Nút xanh | Phụ / trung tính (Details, Inspect, Whisper, Back) |
| Nút đỏ | Cancel / Decline / Delete / Leave / Close trade |
| Nút tím | Đặc biệt / premium / gacha |
| Tiêu đề | Ruy băng hoặc bảng gỗ nhỏ, Baloo lớn đậm, canh giữa |
| Tab | Một kiểu tab giấy / gỗ, tab đang chọn sáng hơn |
| Ô item | Một khung gỗ vuông bo nhẹ, viền rõ, số lượng góc dưới phải |
| Thanh tài nguyên | Luôn ở thanh trên: vàng → premium → vật liệu đặc biệt |
| Toast | Thanh giấy nhỏ, 1 dòng chính + icon |
| Hộp xác nhận | 1 câu hỏi + vàng xác nhận / đỏ huỷ |

## Từng màn cũ

| Màn | Thành | Bố cục | Giữ / bỏ |
|---|---|---|---|
| Settings | Medium dialog | Trái: nhóm cài đặt; phải: công tắc / thanh trượt; "Change Hero" riêng ở cuối | Giữ, thêm nhóm Social / Audio / Display |
| Quest log | Medium / large | Danh sách trái, chi tiết phải, Track / Abandon | Giữ, sau này lọc Main / Side / Daily |
| Shop thuốc | Medium shop | Danh sách trái, chi tiết phải, Buy có số lượng | Giữ, chung khuôn với shop đồ |
| Shop đồ | Medium / large shop | Tab theo tier / ô, danh sách trái, xem trước phải | Giữ |
| Hộp thoại NPC | Thanh hội thoại giấy | Chân dung NPC trái, chữ dưới, tên trên ruy băng nhỏ | Giữ, khung giống Wanted |
| Level Up | Banner mới | "LEVEL UP!" dạng ruy băng / giấy sáng, có thể thêm chỉ số tăng | Giữ |
| Chọn class | Full screen | Giữ nền, đổi panel thông tin / preview sang kiểu mới | Giữ |
| Change hero | Gộp vào Chọn class (chế độ Switch Hero) | | Bỏ màn riêng |
| Menu Invite/Trade | Player Card | | Làm sớm |
| Party đang mời | Pending chung | Party / Duel / Trade dùng chung | |
| Nhận lời mời party / Trade Request | Bảng lời mời chung | Đổi icon + chữ theo loại | |
| Bàn phím vàng | Gộp vào cửa sổ Trade | Ô vàng nhập thẳng + nút −/+ | Bỏ pad riêng |
| Cửa sổ Trade | Full window Trade mới | | |
| Xem đồ khi trade | Tooltip Adventure Paper | | |

## Phần xã hội (mockup `2-mockup-moi/11-player-card-duel-request.png`)

- Player Card "Thông Tin Người Chơi": ruy băng đỏ + X đỏ, avatar tròn viền vàng, tên, class, Lv, "Party 2/5"; nút Mời Party (vàng), Thách Đấu / Giao Dịch / Xem Thông Tin / Nhắn Riêng (xanh), Chặn (đỏ). Đặt bên phải giữa, không bám đầu nhân vật.
- Lời Mời Thách Đấu: ruy băng đỏ có kiếm chéo, 2 avatar "A VS B" kèm class, đếm 15 s, 3 dòng luật (Duel 1v1 khu vực riêng; chuẩn hoá chỉ số • không mất EXP / vật phẩm; 120 giây), Chấp nhận (vàng) / Từ chối (xanh). Party và Trade dùng cùng khung.
- Nearby: medium panel, avatar + tên + Lv + class + party, lọc cùng map / ẩn người bị chặn, chọn 1 người mở Player Card.
- Pending: góc trên phải gần minimap, loại + tên + đếm ngược + Huỷ, 1 yêu cầu gửi đi một lúc.

## Cửa sổ Trade mới (mockup `2-mockup-moi/12-trade-window.png`)

Full window: ruy băng "Trade" có biểu tượng bắt tay, X đỏ. 3 cột giấy: "Your Offer" (ruy băng xanh) và "Opponent's Offer" (ruy băng đỏ) mỗi cột có avatar + tên + Lv + class, lưới 3×3 ô (ô trống có dấu +), ô "Vàng" với nút −/+, thanh trạng thái khoá ("Đã khoá (sẵn sàng)" xanh lá / "Chưa khoá" nâu); mũi tên đổi chiều giữa 2 cột; "Túi Đồ (50/50)" có tab lọc (Tất cả, Vũ khí, Áo, Phụ kiện, Vật phẩm, Khác) và lưới 5 cột. Dưới: ô lưu ý (i), Mở khoá Offer (xanh), Xác nhận Trade (vàng), Huỷ (đỏ).

## Thứ tự làm (theo tần suất gặp)

| # | Hạng mục | Vì sao |
|---|---|---|
| 1 | Player Card + bảng lời mời chung + Pending chung | Gặp liên tục, sửa Party / Duel / Trade một lần |
| 2 | Cửa sổ Trade mới | Lệch style rõ nhất |
| 3 | Settings + Quest log | Hay mở, lệch mạnh |
| 4 | Shop thuốc + shop đồ | Một khuôn shop |
| 5 | Hộp thoại NPC + Level Up | Polish |
| 6 | Nearby + Social Settings | Khi đông người |
| 7 | Chọn class / Change hero | Ít gặp |

## Lưu ý kỹ thuật khi chụp (17/11)

- Bài test `net-party-test -Ui` / `net-trade-test -Ui` mặc định chạy giao diện CŨ: thêm `--ui new` cho client mới thấy HUD mới.
- Một lần chạy test đã bấm phím ẩn nút cảm ứng (`GameKey.TouchToggle`) và lưu `zenonia.touchControls = 0` vào PlayerPrefs của bản build (registry `HKCU\Software\DefaultCompany\zenoniaUnity`, dùng chung cho mọi bản build trên máy): mọi lần chạy sau mất joystick + nút. Đã đặt lại 1. Editor dùng khoá riêng, không bị.

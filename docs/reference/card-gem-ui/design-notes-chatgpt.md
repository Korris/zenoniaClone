# Card + ngọc + đấu card: UI/UX do ChatGPT thiết kế (23/11/2026)

- Chat (tài khoản Tay "Tân Korris"): https://chatgpt.com/c/6b031e60-ffa0-83ec-903d-9449063ac0e0
- Yêu cầu người dùng: "design card menu và danh sách card để sưu tầm, design luôn chức năng chuyển hoá card thành ngọc, chức năng tháo ngọc, khảm ngọc vào item, ui ux theo style ro fantasy" + "design cả ui ux đấu card với nhau nữa".
- ChatGPT tự quyết flow, luật, số màn, bố cục (người dùng dặn không áp bố cục cho nó). Gửi kèm style mẫu: `ui-style-ro-soft-fantasy/01-style-board.png`, `03-bag.png`, `ro-soft-menu-ingame/tui-do.jpg`.
- Chỉ là mockup, chưa có code game.

## Ảnh

| File | Màn |
|---|---|
| `01-card-menu.png` | Monster Cards (kho card): menu trái My Cards / Album / Convert / Socket / Remove Gem / Card Duel; lưới card + lọc N/R/SR/SSR, tìm, sort; badge xN, "In Deck", "On Market"; chi tiết card: 4 chỉ số hướng đấu bài, hiệu ứng Power Gem / Guard Gem; nút Add to Deck / Sell / Convert |
| `02-card-album.png` | Monster Album: tiến độ 24/40 + rương mốc 10/20/30/40, Claim Reward; tab vùng All / Meadow / Frost / Marsh / Ruins / Boss; card chưa có = bóng đen "?"; chi tiết nguồn + trạng thái Collected |
| `03-card-to-gem.png` | Convert to Soul Gem: chọn card → chọn Power / Guard → kết quả ngọc; hiệu ứng, phí (1 card + 500 G), nút Convert. Nhãn "Conversion Result" do mình vá chữ (ChatGPT vẽ 3 lần đều sai "Converstion") |
| `04-gem-socket.png` | Gem Socketing: chọn Weapon / Armor / Accessory, trang bị, 2 ô ngọc, kho ngọc lọc Power / Guard, Gem Info, so chỉ số trước / sau, phí 0 G, cảnh báo khoá giao dịch, nút Socket Gem |
| `05-gem-remove.png` | Remove Gem: chọn ô ngọc, xem trước chỉ số sau tháo, ngọc về túi (vẫn khoá giao dịch, 100 %), phí 3.000 G (SR), Cancel / Remove Gem |
| `06a-card-duel-deck.png` | Card Duel lobby: PvE (NPC) / Ranked (PvP) / Friend Duel / Room Match; đối thủ NPC + thưởng thắng lần đầu; deck 8/8, Edit Deck; thông tin card 4 chỉ số hướng; Start Duel |
| `06b-card-duel-battle.png` | Bàn đấu 4×4: 2 người chơi trên cùng (8/8), "Turn 7/16 – Your Move", luật bên trái, card đang chọn + battle log bên phải, tay bài dưới, Place Card / Cancel |

## Luật ChatGPT đề xuất (V1)

**Card và ngọc**
- Độ hiếm 4 bậc N / R / SR / SSR; boss có vương miện (không phải bậc thứ 5). Ngọc giữ độ hiếm của card.
- Card rơi từ quái; Album ghi nhận lần đầu sở hữu, card đã bán / chuyển hoá vẫn tính. Card trùng giữ nhiều bản: đấu bài, bán chợ hoặc chuyển hoá.
- Chuyển hoá: 1 card + Gold → 1 Soul Gem, chọn Power (công) hoặc Guard (thủ). Phí N 500 / R 2.000 / SR 8.000 / SSR 25.000 G, 100 %, mất card.
- Ô ngọc: vũ khí 2, giáp 2, phụ kiện 1; không tăng theo tier. Khảm miễn phí, 100 %; ngọc khảm lần đầu bị khoá giao dịch vĩnh viễn.
- Tháo: N 200 / R 800 / SR 3.000 / SSR 10.000 G, 100 %, giữ ngọc (vẫn khoá giao dịch), không hỏng trang bị.
- Album 40 loại: mốc 10 = 5.000 G, 20 = danh hiệu Card Hunter, 30 = khung chân dung, 40 = hiệu ứng Monster Master. Không cộng chỉ số vĩnh viễn.
- Card / ngọc chưa khảm bán được ở chợ; 1 bản card không được cùng lúc ở deck đang đấu, chợ và chuyển hoá.

**Đấu card (Monster Card Duel, lấy ý Tetra Master FF9 / Triple Triad FF8, người dùng tự chọn 4×4 trong chat)**
- Bàn 4×4, mỗi bên 8 card, deck tối đa 1 boss + 2 SR/SSR.
- Card có 4 số hướng (trên / phải / dưới / trái, 1-9). Đặt lần lượt; cạnh mình lớn hơn cạnh đối diện của card địch kề bên thì lật card đó. Không ngẫu nhiên, không combo ở V1.
- Đầy 16 ô: chiếm 9+ thắng, 8-8 hoà. Người đi trước ngẫu nhiên, 20 s / lượt, hết giờ server đặt ngẫu nhiên.
- Chế độ: NPC (luyện, thưởng lần đầu + nhiệm vụ ngày), PvP thách đấu trực tiếp; Ranked để V2. PvP: thắng 300 G, thua 100, hoà 150, tối đa 5 trận có thưởng / ngày. Không cược mất card.
- Server quản lý deck, lượt, lật, đồng hồ, thưởng; card đang trong trận bị khoá bán / chuyển hoá.

## Điểm yếu thấy khi xem ảnh

- Tông là RO Soft **Fantasy** (theo lời người dùng), trong khi nhánh `ro-soft-adventure` đã đổi UI sang RO Soft **Adventure** (giấy kem / gỗ nâu): làm thật thì phải đổi da.
- Tên quái / địa danh lấy từ RO (Poring, Fabre, Lunatic, Prontera Field ở màn 1), không phải quái của game mình (màn 6a ghi đúng Willowbrook Meadows).
- Màn 1-2 có menu trái chuyển màn; màn 3 không có menu đó; màn 4-5 dùng menu trái là loại trang bị. Tiêu đề màn 6a nằm ngoài khung cửa sổ, các màn khác nằm trong.
- Màn 4 có "Lv. 3 / Lv. 2" cạnh ngọc dù V1 không nâng cấp ngọc; ô 2 đã có ngọc nhưng vẫn hiện như đang khảm.
- Màn 6b: card đang chọn (Horn Rabbit) không có trong tay bài; bộ đếm 8/8 không giảm khi đã đặt 3 lá.
- Màn 1 ghi Guard Gem của Poring là "giảm sát thương từ Poring": hiệu ứng ngọc còn chung chung, chưa có bảng hiệu ứng theo từng quái.

## Câu hỏi mở cho người dùng

1. Giữ style Fantasy như ảnh hay vẽ lại sang Adventure cho khớp nhánh hiện tại?
2. Đồng ý bộ phí / mốc album / thưởng PvP của ChatGPT không (chỉ là số đề xuất)?
3. Ngọc khảm khoá giao dịch vĩnh viễn: được không?
4. Đấu card có Ranked (V2) và cược card không?
5. Card Duel mở ở đâu: NPC trong làng hay nút trên HUD?

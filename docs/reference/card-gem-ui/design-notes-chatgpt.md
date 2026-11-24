# Card + ngọc + đấu card: UI/UX do ChatGPT thiết kế (23/11/2026)

- Chat (tài khoản Tay "Tân Korris"): https://chatgpt.com/c/6b031e60-ffa0-83ec-903d-9449063ac0e0
- Yêu cầu người dùng: "design card menu và danh sách card để sưu tầm, design luôn chức năng chuyển hoá card thành ngọc, chức năng tháo ngọc, khảm ngọc vào item, ui ux theo style ro fantasy" + "design cả ui ux đấu card với nhau nữa".
- ChatGPT tự quyết flow, luật, số màn, bố cục (người dùng dặn không áp bố cục cho nó). Gửi kèm style mẫu: `ui-style-ro-soft-fantasy/01-style-board.png`, `03-bag.png`, `ro-soft-menu-ingame/tui-do.jpg`.
- Chỉ là mockup, chưa có code game.

## Ảnh

Menu trái chung (ChatGPT chốt 23/11, sau khi người dùng bảo bỏ "Card Growth"): **My Cards / Monster Album / Convert to Soul Gem / Gem Socketing / Gem Removal / Card Duel**, giống hệt ở màn 1-5. Màn 6a / 6b là giao diện đấu riêng, không dùng menu này.

| File | Màn |
|---|---|
| `01-card-menu.png` | (vòng 4) My Cards: lọc độ hiếm / vùng + ATK / DEF / HP / Utility, "Card Collection 12/40", lưới card có 4 số hướng + xN, nhãn On Market / In Deck; chi tiết: Element, Source, Owned, Duel Stats 4 hướng, Card Effect, mô tả; nút Add to Deck / Sell / View in Album |
| `02-card-album.png` | (vòng 4) Monster Album: tiến độ 24/40, thanh mốc 10/20/30/40 + Claim, tab 7 vùng của game, quái chưa có = bóng "?", chi tiết quái + Duel Stats + Card Effect, Add to Deck / Sell |
| `03-card-to-gem.png` | (vòng 4) Convert to Soul Gem: lưới card có số hướng, chọn Power Gem (ATK) / Guard Gem (DEF), phí 1 card + 500 G (card N) → 1 ngọc, nút Convert |
| `04-gem-socket.png` | (vòng 4) Gem Socketing: danh sách trang bị Equip / Bag, mỗi món 2 ô, ô Gem Info + Gem Option riêng, kho ngọc lọc All / ATK / DEF / HP / Utility, ngọc là viên ngọc mang hình quái, nút Socket Gem |
| `05-gem-remove.png` | (vòng 4) Gem Removal: cùng khung màn 4, ngọc chọn (Meadow Slime Gem N, ATK +6), phí 200 G (N, đúng luật), ô đỏ "Trade Locked" (ngọc tháo vẫn khoá giao dịch), nút Remove Gem |
| `06a-card-duel-deck.png` | (vòng 4) Card Duel deck: lọc vùng / độ hiếm, My Deck 8/8 (Boss 1/1, SR+ 2/2), Deck Rules, Deck Power, Play NPC / Challenge Player / Save Deck / Ready. Dùng menu trái chung (không còn lobby PvE / Ranked / Room) |
| `06b-card-duel-battle.png` | (vòng 4) Bàn đấu 4×4 A-D × 1-4, tỉ số Player / NPC, Turn 7/16, battle log, NPC "Frost Scout Nia", card chọn + Directional Stats, tay bài 5 lá, Place Card |

Góp ý người dùng 23/11:
- Màn khảm (nguyên văn): "cái màn hình khảm ngọc ko cần hiển thị current với after vì nó ko + thẳng vào vũ khí trực tiếp mà chỉ là option thêm thôi nó sẽ hiện ở 1 ô riêng chứ". ChatGPT bỏ bảng Current / After, hiệu ứng ngọc nằm ở ô "Gem Info → Option"; màn tháo đổi theo.
- (nguyên văn) "card growth là cái gì ta có vẻ là thừa kêu nó sửa đi": bỏ mục Card Growth (V1 không nâng cấp card), đồng bộ menu trái, sửa 3 ô → 2 ô, phí tháo 5.000 → 3.000 G. Đã vẽ lại màn 1, 2, 3, 4, 5. Bản cũ nằm trong các commit f1340e40, 44b4168a.

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
- ~~Chế độ: NPC (luyện, thưởng lần đầu + nhiệm vụ ngày), PvP thách đấu trực tiếp; Ranked để V2. PvP: thắng 300 G, thua 100, hoà 150, tối đa 5 trận có thưởng / ngày. Không cược mất card.~~ **THAY THẾ 23/11 chiều (mục "Luật đấu card v2" cuối phần Đã chốt): không còn đấu NPC, không còn thưởng Gold PvP; mọi trận PvP thường thắng lấy 1 card.**
- Server quản lý deck, lượt, lật, đồng hồ, thưởng; card đang trong trận bị khoá bán / chuyển hoá.

Vòng 4 (23/11, người dùng): trả lại phần bị mất (Add to Deck / Sell, mốc album + Claim, tổng 40, số hướng trên card, dòng khoá giao dịch), giữ lọc ATK / DEF / HP / Utility + menu trái chung, đổi hết quái / vùng RO sang quái / map của game (`our-monsters-contact-sheet.png`). Đã vẽ lại cả 7 màn.

## Điểm yếu thấy khi xem ảnh (sau vòng 4)

- Màn 1 và 6a/6b dùng menu trái nền xanh lá, màn 2-5 dùng nút vàng: chưa đồng bộ hẳn.
- Số 4 hướng của cùng một quái lệch giữa các màn (VD Meadow Slime 6/3/5/4 ở màn 1 và 6a, 6/5/5/4 ở màn 2-3); Element Meadow Slime ghi Nature (màn 1) và Earth (màn 2-5). Làm thật thì lấy số từ bảng dữ liệu, không lấy từ ảnh.
- Màn 6b: log ghi Mire Toad bị lật nhưng trên bàn vẫn xanh (tỉ số 4-2 sai, đúng ra 3-3); tay bài có 4 SR/SSR và không khớp deck 6a. Đã nhờ sửa 1 lần nhưng bản sửa tệ hơn (tỉ số 7-8, tay 8 lá) nên giữ bản này; logic ván lấy từ luật, không từ ảnh.
- Thú cưng góc trái tiêu đề vẫn giống Poring có cánh (logo), chưa đổi.

## Điểm yếu vòng trước (đã sửa ở vòng 4)

- Tông là RO Soft **Fantasy** (theo lời người dùng), trong khi nhánh `ro-soft-adventure` đã đổi UI sang RO Soft **Adventure** (giấy kem / gỗ nâu): làm thật thì phải đổi da.
- Tên quái vẫn có Poring (của RO); địa danh đã đổi sang Willowbrook Meadows.
- Lưới card ở màn 1-3 không còn 4 số hướng trên mặt card như bản đầu (chỉ hiện ở ô chi tiết); 06a / 06b vẫn có số trên card.
- Màn 1: Poring thành SR (màn 3 vẫn N); không còn nút Add to Deck / Sell (chỉ Convert + Lock Card).
- Màn 2: bỏ thanh mốc thưởng + Claim, tổng thành 60 loại (luật 40); card Poring SR có Option "+5% Max HP".
- Màn 5 không còn dòng "ngọc vẫn khoá giao dịch".
- Lọc ngọc ATK / DEF / HP / Utility thay cho Power / Guard.
- Màn 6b: card đang chọn (Horn Rabbit) không có trong tay bài; bộ đếm 8/8 không giảm khi đã đặt 3 lá.

## Đã chốt (23/11 11:20)

Người dùng: giữ style RO Soft **Fantasy**; duyệt bộ phí chuyển hoá / tháo, mốc album, thưởng PvP ở trên; đấu card CÓ Ranked và CÓ cược card. Câu 3 và 5 để ChatGPT quyết:

**Khoá giao dịch ngọc (ChatGPT):** giữ khoá vĩnh viễn sau lần khảm đầu, kể cả khi tháo; card chưa chuyển hoá và ngọc chưa khảm bán tự do; không có vật phẩm mở khoá (kể cả nạp). Lý do: giữ nhu cầu săn card, ngọc không lưu thông vô hạn, khó chuyển ngọc giữa tài khoản để bán ngoài game. Khoá không tự chặn bot / RMT: cần thêm giới hạn đăng bán cho tài khoản mới, log chuyển hoá / khảm / tháo, phát hiện giao dịch bất thường.

**Chỗ mở đấu card (ChatGPT):** kết hợp. NPC Card Master ở Willowbrook: hướng dẫn, ~~đấu NPC,~~ luật, nhận thưởng album (đấu NPC bỏ 23/11 chiều). Menu tính năng "Card Duel": deck, lịch sử, thách bạn, Ranked ở mọi map an toàn. Không thêm nút thường trực trên HUD. Không mở trận khi đang đánh, ở Wanted Boss hay trong dungeon.

**Ranked (ChatGPT):** mùa 30 ngày, reset một phần điểm; 5 bậc Bronze → Silver → Gold → Platinum → Diamond; cần xong tutorial, Lv20+, deck 8 lá hợp lệ; ghép theo MMR ẩn ±150, sau 30 s nới dần, không ghép bạn bè; thắng +20..30, thua −15..25, hoà 0, bỏ trận = thua; ~~thưởng Gold như PvP (300 / 100 / 150, tối đa 5 trận / ngày)~~ (PvP không còn thưởng Gold, 23/11 chiều), cuối mùa thưởng danh hiệu, khung avatar, hiệu ứng card, đồ trang trí (không tăng sức mạnh); trận dàn xếp không tính điểm, theo dõi cặp đấu lặp / cố tình thua.

**~~Cược card (Card Wager, ChatGPT)~~ ĐÃ THAY THẾ bằng "Luật đấu card v2" ngay dưới (cược 1 card N/R chọn trước, nút Wager của người mời, Lock / Confirm, thưởng Gold PvP đều bỏ).** Bản cũ: chỉ PvP thường, KHÔNG trong Ranked. Mỗi bên cược đúng 1 card, chỉ N hoặc R, hai lá cùng độ hiếm; không cược Gold / ngọc / pet / SR / SSR / boss / card khoá / card đang trên chợ; card cược không cần nằm trong deck. Hai bên xem card, Lock rồi Confirm, đổi card thì huỷ xác nhận cả hai. Thắng lấy cả 2, thua mất, hoà trả lại. Server giữ 2 card (escrow) trước trận, chuyển chủ bằng transaction nguyên tử. Rớt mạng có 60 s nối lại rồi xử thua. Chống chuyển đồ: Lv30+, tài khoản ≥ 7 ngày, tối đa 3 trận cược / ngày, log mọi lần chuyển, cảnh báo cặp cố tình thua; có công tắc tắt tính năng. ChatGPT lưu ý kiểm tra luật pháp từng thị trường về cược vật phẩm có giá trị.

### Luật đấu card v2 (người dùng chốt cuối 23/11, thay mọi luật cược / thưởng / NPC ở trên)

1. **Không còn đấu NPC thưởng / cược** (bỏ thưởng thắng lần đầu / nhiệm vụ ngày 500 G, nút Play NPC trên trang deck). **Ngoại lệ DUY NHẤT (người dùng chốt 24/11, thay điều này): Card Master Orin (Oakhaven) cho đúng 2 loại ván: (a) ván hướng dẫn của quest "The First Card Duel" bằng deck 8 lá N MƯỢN (kịch bản 3 bài học rồi chơi tự do), (b) đấu tập sau khi quest xong. Cả hai KHÔNG cược, KHÔNG mất / nhận card, KHÔNG Gold; thưởng quest (300 G + hornRabbit / thornBoar / forestWolf, 1 lần / nhân vật) chỉ trả khi trả quest cho Orin (TurnIn), không phải lúc đấu.** Ván Orin chạy được cả offline (`LocalDuelHost`, chỉ ván Orin); PvP / Ranked vẫn **chỉ online**, offline trang Card Duel hiện "disabled" kèm lý do. Server huỷ ván Orin (quest giữ nguyên, chơi lại được) khi chết / sang map có quái / rớt mạng / 10 phút không thao tác. Đường test dev server vẫn giữ (`--dev-cards`, `--net-bot duel`, `Tools/net-duel-test.ps1`; quest Orin: `--net-bot orin`, `Tools/net-orin-test.ps1`).
2. **Mọi trận PvP thường: thắng lấy 1 card (kiểu FF8 "One").** Người thắng chọn BẤT KỲ 1 card trong deck 8 lá người thua đã dùng ở trận đó (kể cả SR / SSR / boss / card đang Lock). **Hoà: không ai mất gì.** Hộp mời ghi rõ "Winner takes 1 card from the loser's deck"; trang deck cảnh báo card trong deck (kể cả card Lock) có thể mất. Không còn thưởng Gold PvP.
3. **Escrow cả deck 8 lá của HAI bên** ở server-api trước ván (một transaction Mongo). Kết thúc: người thắng nhận lại 8 lá của mình + lá đã chọn, người thua nhận lại 7 lá, hoà trả đủ; idempotent theo matchId; quá hạn hoàn như cũ; ghi ledger.
4. **Zone:** thắng xong người thắng thấy màn chọn với 8 lá của người thua (ngửa), 30 s, hết giờ server chọn ngẫu nhiên; người thua thấy "waiting for opponent to pick", sau đó lá nào bị lấy. Rớt mạng: 60 s nối lại rồi xử thua, người thắng vẫn chọn.
5. **Chống lạm dụng:** Lv30+, tài khoản ≥ 7 ngày, không tự đấu chính tài khoản mình, cùng 1 cặp tối đa 2 trận / ngày, tối đa 10 trận PvP / tài khoản / ngày (API chốt, zone kiểm sớm để tắt nút). Công tắc khẩn `CARD_WAGER_ENABLED`: tắt thì PvP vẫn chơi nhưng không lấy card.
6. **Nhạc kết quả:** thắng `duel-victory-cozy-win-sparkle` (người dùng chốt, thay `spark`), thua `duel-defeat-try-again-tomorrow`, hoà dùng bản nhẹ `duel-defeat-try-again-gently` (quyết định của dev); nhạc nền map hạ xuống trong lúc phát rồi trở lại; bảng thay bản ở `DuelMusic.cs` (bản thay: `duel-victory-heros-prize`, `duel-defeat-try-again-gently`).
7. **Ranked (phase 07, sau): KHÔNG lấy card.**

Ngoài ra ChatGPT vẽ thêm 1 bản 6b "logic fixed" (tỉ số 3-3 đúng, nhưng nó tự nhận mỹ thuật đơn giản hơn), chưa tải về.

## Asset thật (phase 02, 23/11 12:30)

- Chat mới cùng tài khoản Tay: https://chatgpt.com/c/6b03c3de-7f2c-83ec-b6f2-0b27b06a2617 . Mỗi tin gửi kèm mockup liên quan (+ `ui-style-ro-soft-fantasy/01-style-board.png` ở tin đầu), danh sách mảnh theo hàng, nền phẳng #FF00FF hoặc #00FF00 (theo màu của sheet), không chữ, cách nhau >= 24-48 px; để ChatGPT tự quyết cách vẽ.
- Chân dung quái: mỗi sheet 8 con, gửi ảnh ghép frame idle trong game phóng nearest có đánh số (`Sprites/monsters/<id>.png` ô 0,0; 4 quái hang dùng sheet gốc nhân `FallbackTint`) + `our-monsters-contact-sheet.png` làm mẫu phong cách.
- Ảnh gốc: `Assets/ArtLibrary/ui/cards/<a-frame|b-icons|c-card|d-portraits|e-gems|f-board|g-ranked|h-npc>/`; config cắt `Tools/ui-paper-sheets/cards-*.json`; contact sheet + ảnh so `docs/reference/card-gem-ui/art-contact/`.

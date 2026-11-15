# Wizard bộ D: thiết kế trang bị 10 bậc T0–T9 (14/11/2026)

Bước thiết kế, chưa cài game, chưa sửa code, chưa commit.
ChatGPT tài khoản Tay, chat mới: https://chatgpt.com/c/6af870fd-e614-83ec-b7a5-d59f2b382980 ("Create Wizard Equipment Board").
Ảnh gửi kèm (lượt đầu): `outfit-d-t0/src/concept-d-x2.png`, `custom/hero-select/wizard-outfit-d-hero-select-chatgpt-1.png`, `docs/reference/hero-idle-down-5-classes.png`. Prompt có WORLD ART STYLE LOCK.

## Bảng (thư mục `Assets/ArtLibrary/classes/wizard/outfits/outfit-d-tiers-design/`)

| File | Nội dung | Đánh giá |
|---|---|---|
| `wizard-d-tiers-outfit-board-chatgpt-2.png` | **Bảng chọn**: 10 bậc toàn thân 2x5, nền #00FF00 | T1–T9 khác hình khối rõ, bậc sau to hơn |
| `wizard-d-tiers-outfit-board-chatgpt-1.png` | Lượt 1, bỏ | T1–T4 gần giống y T0 |
| `wizard-d-tiers-staff-board-chatgpt-3b.png` | **Bảng trượng chọn** (đầu trượng to, cán dài) | T0→T9 cao dần ~1,7x |
| `wizard-d-tiers-staff-board-chatgpt-3a.png` | Trượng phương án phụ (cán rất mảnh, đầu nhỏ) | để người dùng so |
| `wizard-d-tiers-staff-book-board-chatgpt-1.png` | Trượng + sách; **dùng hàng SÁCH** | trượng ngắn như quyền trượng, bỏ |
| `wizard-d-tiers-staff-board-chatgpt-2.png` | Trượng lượt 2, bỏ | chép lại lượt 1, vẫn ngắn |
| `prompt-1..5-*.txt` | Prompt nguyên văn từng lần + lý do vẽ lại | |

## Chủ đề từng bậc (theo bảng chọn)

- T0 Frost Apprentice: bộ D (cổ đứng xanh, vai vàng ngọc xanh, đá thoi ngực, dây chéo, thắt lưng nâu, áo trắng xẻ vạt, vạt bông tuyết). Trượng gỗ vàng xoắn + chùm pha lê; sách navy bông tuyết góc vàng.
- T1 Frost Adept: vai to hơn viền bạc, vòng tay bạc. Trượng thêm 2 pha lê; sách góc bạc + ngọc.
- T2 Rime Scholar: cổ trắng cao cứng, tấm vai trắng, vạt có tấm pha lê. Trượng vòng vàng quanh pha lê; sách khoá pha lê.
- T3 Glacier Mage: giáp vai pha lê băng to, vạt bông tuyết lớn. Trượng chùm pha lê lớn + chạc bạc; sách viền bạc nhọn.
- T4 Frost Warden: giáp ngực + váy giáp vàng, vai vàng 2 lớp, giày giáp. Trượng lồng vàng; sách bìa kim loại vàng.
- T5 Blizzard Sorcerer: xanh hoàng gia + trắng, vai gai băng, vạt có vân rune. Trượng xoắn mảnh băng; sách viền băng.
- T6 Spellblade Knight: giáp tấm trắng bạc, găng giáp, đùi giáp. Trượng đầu lưỡi pha lê; sách bìa kim loại bạc.
- T7 Aurora Paladin: bạch kim + vàng, vai vây pha lê, lõi ngực sáng. Trượng vòng hào quang vàng; sách bìa vàng chéo pha lê.
- T8 Celestial Archmage: vai lớn nhiều lớp, vương miện pha lê, mảnh băng bay, tấm áo choàng cứng góc cạnh. Trượng vương miện pha lê + mảnh bay; sách khung vàng bạc ngọc lớn.
- T9 Ice Dragon Archmage: giáp vảy rồng băng, vai đầu rồng, cánh pha lê băng cứng. Trượng rồng băng cuộn quanh pha lê lớn; sách bìa vảy rồng + mắt rồng.

## Đã kiểm

- Tóc/mặt giống nhau 10 bậc, đầu ~45% chiều cao người như concept D (ảnh chuẩn 5 class ~40–45%): không to đầu hơn T0.
- Không lông xù, không vải rủ, không xích/ruy băng. Chiều cao bóng (cả trượng) T0 358 → T9 432 px.

## Còn lệch

- T0 trên bảng gần D nhưng không y hệt: vạt áo trắng cắt răng cưa, trượng ít xoắn và chùm pha lê nhỏ hơn D; trượng T0 bảng trượng cũng mảnh hơn D. Khi làm sprite nên lấy T0 từ concept D gốc, không từ bảng.
- Vạt áo T2–T9 nhiều mũi nhọn pha lê (cứng, gọn, nhưng nhiều chi tiết nhỏ ở cỡ sprite).
- Vòng hào quang lặp ở trượng T2 và T7.
- Bảng trượng/sách vẽ theo bảng toàn thân nhưng không khớp 100% (vd. T4 lồng vàng đơn giản hơn).
- Chữ T0–T9 vẫn to hơn yêu cầu "nhỏ".
- Bậc thấp (T1–T3) vẫn cùng tông xanh-trắng-vàng; chênh lệch chủ yếu ở hình khối, không ở màu.
- Chưa xem ở cỡ game (~160 px), chưa so cạnh Warrior/Mechanic trong game.

## Câu hỏi cho người dùng

1. Duyệt bảng `outfit-board-chatgpt-2` làm hướng T0–T9? Bậc nào muốn đổi?
2. Trượng: chọn 3b (đầu to) hay 3a (cán mảnh)?
3. Có muốn mỗi bậc thêm màu nhấn riêng (vd. bạc, tím băng, vàng hồng) để phân biệt hơn, hay giữ nguyên xanh-trắng-vàng?
4. Bỏ hào quang ở T2 cho khỏi trùng T7?

## Vòng 2 (22:05): mỗi bậc một màu riêng (người dùng: "hiện tại đang bị trùng màu")

Cùng chat, gửi kèm bảng -2 (mẫu hình khối) và -3b (mẫu dáng trượng). Prompt `prompt-6..9-*.txt`.

| File | Nội dung | Đánh giá |
|---|---|---|
| `wizard-d-tiers-outfit-board-chatgpt-4.png` | **Bảng toàn thân chọn** | 10 màu khác hẳn nhau, giữ hình khối -2 |
| `wizard-d-tiers-outfit-board-chatgpt-3.png` | Lượt màu 1, bỏ | T5 chàm + xanh gần giống T0, có 3 bậc cùng tông xanh |
| `wizard-d-tiers-staff-board-chatgpt-4.png` | **Bảng trượng chọn** (dáng -3b, đổi màu, T2 đã bỏ vòng hào quang) | đạt |
| `wizard-d-tiers-book-board-chatgpt-1.png` | **Bảng sách chọn** 10 bậc theo màu mới | đạt, to dần nhẹ |

Màu: T0 xanh băng + trắng + vàng (bộ D) · T1 xanh ô liu + da nâu + đồng · T2 trắng + bạc + xanh nhạt · T3 xanh ngọc lam đậm + bạc · T4 vàng kim + trắng ngà · T5 đen than + kim loại xám + rune xanh điện · T6 bạc thép + đỏ rượu · T7 bạch kim + vàng hồng · T8 xanh đêm + nhiều vàng sao · T9 xanh băng + tím pha lê (rồng băng).

Còn lệch (vòng 2):
- T1 có mảng xanh lá (pha lê trượng, lá trên sách) trùng nền #00FF00: khi tách nền để làm sprite T1 phải xin nền #FF00FF (quy tắc 1).
- Trượng trên bảng toàn thân -4 chưa đổi màu hết (T5, T8 vẫn vàng/xanh); lấy màu trượng theo bảng trượng -4.
- Lồng vàng T4 trên bảng trượng thành vương miện vàng, không rõ là lồng.
- Đã xem ảnh, chưa xem ở cỡ game.

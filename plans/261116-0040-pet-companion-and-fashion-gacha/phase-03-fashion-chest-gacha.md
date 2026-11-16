# Phase 03: Gacha pet (thay Rương Thời Trang, người dùng 16/11)

## Luật (`Rpg/Account/PetGacha.cs`, engine-free)
- 1 vé / lượt, x1 hoặc x10 (từng lượt, 1 lần ghi tài khoản).
- Độ hiếm N 45 / R 30 / SR 17 / SSR 6 / UR 2 %, pet chia đều trong độ hiếm.
- Pity theo tài khoản: `pityUR >= 59` -> UR; else `pitySSR >= 29` -> SSR+ (SSR : UR = 6 : 2); else `pitySR >= 9` -> SR+ (17 : 6 : 2); else tỉ lệ thường. Ra SR+ -> pitySR = 0; SSR+ -> pitySSR = 0 (và pitySR); UR -> cả ba = 0; còn lại +1.
- Mới -> pet 1★. Trùng -> +10 mảnh loài. Lượt không UR -> +1 Mảnh UR. 120 Mảnh UR -> Mini Dragon nếu chưa có.
- Lịch sử 200: at, id, dup, shards, pity sau lượt.
- RNG: zone `RpgRandom.Secure`; offline System.Random.

## Vé (`Rpg/Account/AccountRewards.cs`)
Tân thủ 10 (1 lần); Wanted lần đầu/ngày/tài khoản 1 (<= 4 / tuần); boss map hợp lệ (>= 1 % MaxHP, không tính sát thương pet) lần thứ 5 trong tuần 1; mốc Lv 10..60 lần đầu trên tài khoản 2 / mốc.

## UI `PetGachaWindow` (phím G)
Vé, Mảnh UR, pity còn x lượt tới SR+ / SSR+ / UR, nút Quay x1 / x10, kết quả (pet, độ hiếm, mới / +mảnh). Bảng tỉ lệ: 5 độ hiếm + % từng pet + luật pity / mảnh. Lịch sử 200. Nút đổi 120 Mảnh UR.

## Net
InvOp `GachaPull` (Slot 1 / 10), `GachaExchange` (đổi UR). Trả lời RpgState (AccountJson + Text tóm tắt). Không có cosmetic, không đổi NetAppearance ngoài PetId.

## Todo
- [ ] PetGacha + test (thống kê tỉ lệ, 3 mốc pity, mảnh, Mảnh UR, x10, lịch sử)
- [ ] AccountRewards + hook Wanted / boss map / lên cấp + test
- [ ] Window

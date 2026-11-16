# Phase 09: Gacha "quầy của Lumi"

## Context Links
- Mockup: `chatgpt/pet-gacha-final-mockup.png` (+ kết quả x1, x10, thiếu coin, xác nhận đổi Shards xin ở phase 02)
- Hiện tại: `ingame/pet-gacha.png`, `pet-gacha-rates.png`, `pet-gacha-history.png`
- Code: `PetGachaWindow.cs` (155: Pull, bảo hiểm, shards, kết quả gần nhất), `PetGachaWindow.Tabs.cs` (Rates, History 200 / 16 mỗi trang), `Rpg/Account/PetGacha.cs`, `TownNpc.cs` (Lumi mở gacha), `DebugCapture.Pets.cs` (`--open-gacha`, `--gacha-pull`)
- Phase 01, 02 (G1), 08 (nhãn độ hiếm, công tắc)

## Overview
- Ưu tiên: P2. Effort: M. Status: code xong 16/11, công tắc Gacha vẫn TẮT (chờ người dùng duyệt; chưa commit, chưa code review, chưa chạy online).
- Quầy chợ: Lumi + bong bóng thoại, đài trứng, cờ Featured UR, 2 thanh bảo hiểm (SSR+ x/20, UR x/50), Shards x/120 -> Frostfang King, Buy 1 / 10 coin bằng crystal, Pull x1 / x10, bảng Rates + cuộn History (Newer / Older) hiện sẵn bên phải.

## Key Insights
- Mọi số đã có thật và đúng mockup (tỉ lệ, 150 crystal = 1 coin, bảo hiểm, shard, lịch sử 200). Chỉ làm lại view.
- Mockup gộp Rates + History bên phải và có cờ Rates / History: cờ chỉ cuộn / làm nổi bảng tương ứng (không còn tab ẩn hiện) - giữ đơn giản.
- **Chưa có mockup**: trứng nở x1, lưới kết quả x10 (hiện game ghi kết quả gần nhất dạng chữ), thiếu coin, xác nhận đổi Shards -> chờ ảnh rồi mới code phần kết quả. Hiệu ứng nở trứng: chỉ làm theo mockup (khung + thứ tự hiện), không tự nghĩ hoạt ảnh phức tạp.
- Server tung kết quả online; client đọc từ lịch sử tài khoản (giữ cách hiện tại để offline / online giống nhau).

## Requirements
- Thanh trên: Gacha Coin, Pet Crystal, quy đổi, Pet Camp (mở PetCampWindow), Close.
- Pull x1 / x10 tắt khi thiếu coin (nút xám K2 + gợi ý Buy); Buy 1 / 10 coin; đổi Shards có xác nhận.
- Kết quả: x1 hiện 1 pet / shard; x10 lưới 10 thẻ theo độ hiếm, nút OK.
- Phím G bật tắt, Esc đóng; nói chuyện với Lumi (E) mở như cũ.

## Architecture
```
AccountState.Gacha (pity, shards, history) ──> LumiCounterWindow
  ├─ .Altar (trứng, Featured, bảo hiểm, shards, nút)   ├─ .RatesBoard   ├─ .HistoryScroll
  └─ .ResultPanel (x1 / x10, sau khi có mockup)
Pull / Buy / Exchange ──> PetActions.Send(InvOp.GachaPull | GachaExchange | ...) (có sẵn)
```

## Related Code Files
- Tạo: `LumiCounterWindow.cs`, `LumiCounterWindow.Altar.cs`, `LumiCounterWindow.RatesHistory.cs`, `LumiCounterWindow.ResultPanel.cs`, `DebugCapture.UiGacha.cs`
- Sửa: không file chung (định tuyến đã ở phase 01)
- Không sửa: `Rpg/Account/PetGacha.cs` (luật + tỉ lệ dùng chung server)
- Xoá (phase 13): `PetGachaWindow*.cs`

## Implementation Steps
1. Nền G1, Lumi + bong bóng, đài trứng, Featured.
2. Thanh bảo hiểm, shards + đổi, Buy, Pull (trạng thái tắt).
3. Rates (lấy từ `RateTable()` dữ liệu, không chữ cứng) + History 5 dòng / trang như mockup (dữ liệu vẫn 200).
4. Nhận mockup kết quả -> ResultPanel x1 / x10.
5. Cờ chụp: dùng `--open-gacha [rates|history]`, `--gacha-pull 1|10`, `--pet-tickets`, `--pity`, `--ur-shards`, `--shared-shards`; mới `--gacha-confirm exchange`.
6. Build bản sao, chụp trước / sau pull x1, x10, thiếu coin; online 1 lần (vai `pet-gacha`). Tự xem.

## Todo List
- [x] Đài trứng, Featured, Lumi
- [x] Bảo hiểm, shards, Buy, Pull
- [x] Rates + History
- [x] Mockup kết quả -> ResultPanel (x1, x10, thiếu coin, xác nhận đổi Shards)
- [x] Cờ chụp, ảnh 16:9 / 20:9 / 4:3, tự xem (`docs/reference/ui-redesign-261116/phase-09-gacha/`)
- [ ] Online (vai `pet-gacha`): CHƯA chạy
- [x] `dotnet test` 2 bộ (358 + 422 xanh)

## Ghi chú sau khi làm (16/11)
- File thật: `LumiCounterWindow.cs` + `.TopBar`, `.Altar`, `.Rates`, `.History`, `.ResultPanel`, `.Dialogs`, `.Testing`; widget `PaperGachaButton.cs`, `PaperGachaCard.cs`; `PetGachaText.cs` (bảng tỉ lệ + luật, dùng chung 2 giao diện: tách khỏi `PetGachaWindow.Tabs.cs`, chuỗi giữ nguyên); `DebugCapture.UiGacha.cs`.
- Sửa nhỏ ngoài danh sách: `PetGachaWindow.cs` / `.Tabs.cs` (gọi `PetGachaText`), `DebugCapture.Pets.cs` (2 dòng: `--open-gacha` mở qua `UiScreens`, màn mới tự đọc cờ), `PaperArt.Pieces.cs` (khối `Gacha*`; nhãn độ hiếm và icon ví dùng lại hằng của Pet Camp).
- Bật màn: `--ui-new gacha`; duyệt xong thì đổi mặc định trong `UiScreens.useNew`. Lumi đã gắn sẵn với màn Gacha trong `UiScreens`.
- Cờ chụp mới: `--gacha-history N`, `--gacha-history-page N`, `--gacha-confirm exchange`, `--gacha-press rates|history|exchange|nocoins1|nocoins10|ok|cancel`, `--gacha-reveal`. Trên màn mới `--open-gacha rates` = tờ luật, `--gacha-pull` = bấm nút Pull.
- Khoá bấm 2 lần: nút Pull khoá từ lúc gửi tới khi tài khoản có kết quả (hoặc 6 giây), thêm ở view mới; `PetActions` không đổi.
- Chờ art (đang vẽ phẳng): `gacha/result-plate-laurel` (bảng tên / "Joined your collection" / tổng x10), `gacha/btn-slate` (Cancel, Skip), `kit/chip-resource`.
- Khác mockup: số UR shard viết trên mặt nút vàng của plaque (art không có ô số); không khung gỗ quanh 2 nút Buy; crystal không có nút "+" (không mua được); History thêm ngày và cột pity (thông tin của tab cũ); cờ Rates mở tờ luật, cờ History về trang mới nhất; viền thẻ x10 theo trạng thái (mới / SSR+ / shard / trùng); lời Lumi cho kết quả trùng / shard / x10 là chữ tự đặt (mockup chỉ có câu pet mới).
- Còn lại: 4:3 thấy mép cắt ngang hông Lumi khi nút Buy ẩn (kết quả); toast cũ của PetActions hiện chồng lên vùng nút kết quả ~2 giây.

## Success Criteria
- Pull / Buy / Exchange đúng như cũ offline + online; số tỉ lệ khớp `PetGacha` (test hiện có vẫn xanh).
- Kết quả x10 hiện đủ 10 thẻ, đúng độ hiếm từ lịch sử.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Hiện tỉ lệ sai so với luật (pháp lý / niềm tin) | Thấp x Cao | Đọc số từ bảng luật, không gõ tay; so ảnh với bảng |
| Bấm Pull 2 lần khi mạng chậm | Trung bình x Trung bình | Khoá nút tới khi trả lời về (đã có ở PetActions? kiểm tra, thiếu thì thêm) |

## Security Considerations
- Kết quả online do server tung; client không tự tính kết quả.

## Next Steps
- Phase 13 xoá PetGachaWindow cũ.

## Câu hỏi mở
- Không.

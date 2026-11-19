# Báo cáo: cửa sổ Pet RO Soft Fantasy theo mockup 05-pet (20/11/2026)

Exe: `D:\temp\player-pet\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-pet`, 0 lỗi). Mockup: `docs/reference/ui-style-ro-soft-fantasy/05-pet.png`.
Chat ChatGPT (Tay, `/api/auth/session` = "Tân Korris", Plus): https://chatgpt.com/c/6aff3c4c-7704-83ec-936f-bcd384d07653 (2 sheet mảnh + 3 vòng nghiệm thu).

## Kết quả
- Cửa sổ Pet mới `PetRoWindow` là mặc định dưới `--ui-style ro`; `--ui-style classic` = `PetWindow` cũ; `--ui-new pet` = Pet Camp (Adventure Paper). Định tuyến ở `UiScreens.Open/Close/IsOpen(Pet)`.
- ChatGPT chấm: vòng 1 8.3 -> vòng 2 9.0 -> vòng 3 **9.3/10**, "đã đạt mức nghiệm thu hình thức, không còn lỗi LỚN hoặc VỪA". Ảnh `docs/reference/ro-soft-pet/compare-round1..3.png`.
  - Vòng 1 sửa: pet to hơn 12-18 %, viền hộp trong nhạt (alpha 0,8), chữ chỉ số / kỹ năng / bonus to hơn, nút / tab / menu cao hơn, bệ + halo to hơn, pill header cao hơn.
  - Vòng 2 sửa: Slime / Rabbit to hơn, halo vàng sau pet Combat mạnh hơn, vòng xanh nhạt sau pet bên phải, chữ phụ dưới nút 13, hình pet trong card to hơn, cột chỉ số giãn dòng.
  - Còn góp ý nhỏ vòng 3 (không làm): nền header thiếu chút lavender, nút menu chưa chọn hơi phẳng, chữ menu hơi mảnh.
- Phóng 4x pill header / ribbon / rarity / bonus / thanh MAX / tab: không có vạch ở hai đầu, không khe.

## Ánh xạ mockup -> hệ pet thật
| Mockup | Game |
|---|---|
| Header: vàng, gem, crystal tím, đùi gà, bóng n/3 | Gold; Pet Crystals (gem xanh như túi đồ); xu gacha (`tickets`, icon xu chân mèo có sẵn); Pet Food; bóng thường / bóng hiếm. Nút +: nguồn vàng / crystal / food (thông báo), xu -> mở Gacha, bóng -> trang Capture |
| Menu trái 4 mục | Team / Collection / Growth / Capture (logic như Pet Camp) |
| 3 ô Battle 100 % / Support 50 % x2 | Combat + Support 1-2 thật; 2 ô **Stat 25 %** là 2 chip cạnh tiêu đề. Khoá theo Lv (5/20/30/35/45). Nút đổi = đưa pet đang chọn vào ô (pet bị thay sang ô cũ, luật Pet Camp). Chạm pet = chọn |
| Tổng lực chiến đội | **Team Level** = tổng level pet trong đội (game không có lực chiến pet) |
| Bonus đội hình | `PetTeam.Bonus` thật (tối đa 3 mục trên hàng, ⓘ hiện đủ + Bond đang bật) |
| Danh sách (10/100), All, Độ hiếm, sắp xếp, trang | Pet sở hữu (n/10 loài), lọc All / In team / Not in team, độ hiếm N..UR / Legacy, mũi tên đảo thứ tự độ hiếm, 5x2 / trang + chấm trang |
| Thẻ pet phải | Độ hiếm, tên, Lv / 60 + thanh, sao, hệ (theo loại skill), vai trò (Melee / Ranged / Guard). **Đổi tên, khoá: mờ, chỉ báo "chưa có"** |
| ATK / DEF / HP / Tốc độ / Crit / Crit DMG | Pet không có các chỉ số đó: thay bằng số thật ở level/sao hiện tại: passive, đòn pet (% đòn chủ, +0,5 %/lv), skill, nhịp đánh 1,5 s, hồi chiêu 20 s, tầm; số xanh = sao / level kế tiếp thêm |
| 3 kỹ năng + ô khoá | Đánh thường, skill, nội tại, **Bond** (khoá khi chưa có cặp trong đội, Legacy "No Bond") |
| Tab Kỹ năng / Duyên | Skills = 4 mục bằng chữ; Bond = 4 Bond, Bond của pet đang chọn tô vàng, ACTIVE / inactive |
| Lên cấp / Lên sao / Đặt vào đội | `InvOp.PetLevelUp` / `PetStarUp` / `PetEquip` qua `PetActions.Send` (online server quyết); đã trong đội thì nút thành Remove. Dòng phụ: chi phí food, mảnh + vàng, ô đang ở |
| Growth / Collection / Capture | Reset level / sao, đổi mảnh chung 10 / tất cả; lưới 10 loài (chưa có = mờ) + cách lấy + cấp sưu tập; số bóng, cách bắt, tỉ lệ, luật boss |

## Bấm thử (EventSystem, `--pet-ro-clicktest`)
62 nút, tất cả raycast trúng chính nút (không bị che), click chạy: help, 4 nút +, Stat chip x2, 3 ô + 3 nút đổi, bonus ⓘ, 2 dropdown + sort, 10 card, rename / lock (báo chưa có), 3 tab, 4 icon kỹ năng, Level up / Star up / Set to team (Remove), 10 card Collection, 6 nút Growth (Convert báo "No shared shards"), Close / xu (đóng + mở Gacha). Thử thật offline: Level up Thorn Boar 12 -> 13, food 3 -> 0; Star up thiếu mảnh -> toast "Needs 10 Thorn Boar shards".

## File
- Code mới: `PetRoWindow.cs`, `.Team.cs`, `.List.cs`, `.Detail.cs`, `.DetailTabs.cs`, `.Pages.cs`, `.Testing.cs`, `PetRoWidgets.cs`. Sửa: `UiScreens.cs`, `DebugCapture.Pets.cs` (`--pet-ro`, `--pet-ro-clicktest`), `RoSoftSprites.Borders.cs` (sinh lại).
- Art: `Assets/ArtLibrary/ui/ro-soft-pet/src/` (2 sheet) -> 51 mảnh `Sprites/ui/ro-soft/pet-*` (`Tools/ui-ro-soft-sheets/ro6-pet-pieces.json`, `ro7-pet-icons.json`); `pet-glow`, `pet-dot` là texture Python (quầng mềm, chấm trang). Dùng lại: bag-window/section/pill/slot/close, btn-*, icon xu / food / bóng của kit paper.
- Ảnh: `docs/reference/ro-soft-pet/` (compare-round1-3, ingame-*.jpg, art-contact/).

## Kiểm tra
- dotnet: ZenoniaSkills 452/452, ZenoniaNet 455/455.
- Đã tự xem: Team (Lv60 đủ đội), Collection 4/10 loài, Growth, Capture, tab Skills / Bond, Lv22 ô khoá, sau khi bấm Level up / Star up.
- CHƯA xem: online thật với zone server, màn 4:3 / 21:9 / điện thoại, trang 2 của danh sách (chỉ có 10 loài), popup dropdown mở trên ảnh (chỉ thấy qua log click), phím P bật/tắt và Esc thật (đi qua `UiWindows.RegisterModal` như Pet Camp).

## Sự cố
- Ổ D đầy giữa lúc build (3 agent cùng sao chép 16 GB): xoá `Library/PlayerDataCache` và `Assets/ArtLibrary` trong bản sao CỦA MÌNH, xoá cache sau mỗi build. Không đụng thư mục của agent khác. Còn ~8 GB.
- Thư mục scratchpad dùng chung với agent gacha (file `compare.py` bị ghi đè): đổi tên file của mình `pet-*`.

## Câu hỏi chưa giải quyết
1. Đổi tên pet và khoá pet: cần trường mới trong `AccountState` + server-api + InvOp. Có làm không? (đang để nút mờ)
2. Đội 5 ô thật vs mockup 3 ô: giữ 2 ô Stat dạng chip, hay muốn bố cục 5 bệ?
3. "Tổng lực chiến đội": có muốn công thức lực chiến pet chính thức thay cho tổng level?
4. Ô kỹ năng thứ 4 đang là Bond. Có muốn kỹ năng pet thứ 2-3 thật (mở theo sao) như mockup không? (cần thiết kế + server)
5. Pill bóng hiện "thường / hiếm" (12 / 3); mockup ghi n/3. Có muốn hiển thị khác (ví dụ lượt mua còn lại hôm nay / 5)?

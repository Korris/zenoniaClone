# Art nhân vật nhiều lớp, đợt 3: mũ rời + vũ khí (19/11/2026)

Tài khoản Tay ("Tân Korris", Plus), chat `6afe1ebe-b668-83ec-8233-c010a9354ef0`. Tay không hết lượt nên không dùng Tan Pham. Có 1 lần A/B (vũ khí).

## Ảnh đã nhận (`Assets/ArtLibrary/hero-layered/src/`)
| # | File | Ghi chú |
|---|---|---|
| 1 | headgear-iron-helmet--wizard-hat-chatgpt-2 | vẽ lại 1 lần: mũ sắt rộng 1,40 lần sọ, có miếng che má. Bản -2 rộng 1,09-1,12 |
| 2 | headgear-copper-goggles--ninja-headband-chatgpt-2 | vẽ lại 1 lần: ở góc phải và chéo lên-phải có nét mép dây phía xa vắt ngang chỗ cái đầu, góc lên lòi kính trên đỉnh |
| 3 | headgear-nun-veil--straw-hat-chatgpt-1 | đạt ngay lần 1. Mặt sau khăn trùm đặc, mặt trước chừa chỗ khuôn mặt. Mũ rơm rộng 1,4 (có vành) |
| 4 | headgear-cloth-headband--pompom-beanie-chatgpt-1 | đạt. Băng đô xanh gần như là băng đô ninja đổi màu |
| 5 | headgear-aviator-cap--bunny-ears-chatgpt-1 | đạt. Tai gập đúng bên khi nhìn từ sau |
| 6 | headgear-slime-cap--fox-ears-chatgpt-3 | vẽ lại 2 lần: slime rộng 1,75, rồi 1,35, rồi 1,25-1,30. Ở góc chéo lên-phải còn một chấm mắt ở mép, tool cắt cần xoá. Nền `#FF00FF` |
| 7 | headgear-devil-horns--frog-hat-chatgpt-2 | vẽ lại 1 lần: mũ ếch rộng 1,40. Bản -2 rộng 0,99-1,19. Nền `#FF00FF` |
| 8 | headgear-wildflower-wreath--silver-wolf-crown-chatgpt-2 | vẽ lại 1 lần: vòng hoa rộng 1,45. Bản -2 rộng 1,08-1,21. Nền `#FF00FF` |
| 9 | headgear-ice-crystal-tiara--serpent-crown-chatgpt-1 | đạt (vương miện được rộng 1,1-1,4) |
| 10 | headgear-ultimate-wizard-hat--none-chatgpt-2 | vẽ lại 1 lần: ở góc phải, mặt dưới vành lẫn xanh lá của nền (nửa trong suốt). Ảnh 1 hàng, nên `b` = `none` |
| W | weapons-5-classes-chatgpt-1 | A/B, chọn ảnh 1. Ảnh 2 có viền xanh lá quanh pha lê gậy phép |

Bản bị loại không giữ trong repo.

Commit (chưa push): e30c53c2, deed54ab, b0aa5cb8, b0245e3a, e1c931b1, 1b9f3a95, 9bd67f7e, cùng commit vũ khí, Sổ lỗi và báo cáo.

## Kiểm tra
- **Cách đo:** script nháp tách từng món theo nền, lấy tỉ lệ = khoảng cách món 1 tới món 5 chia cho khoảng cách đầu 1 tới đầu 5 ở ảnh mẫu. Sọ mẫu rộng 222 px.
- **Mũ không vành:** rộng 0,99-1,30 lần sọ. Vương miện, mũ có vành, tai, sừng thì rộng hơn, đúng ngoại lệ.
- **Cỡ ảnh:** gửi kèm ảnh mẫu thì ảnh ra 2172x724, không kèm thì 1536x1024. 5 góc vẫn đúng thứ tự và vị trí tương đối.
- **Mặt sau** của mọi mũ đều đặc, không thấy lòng mũ. Không ảnh nào có người hay đầu.
- **Mép dưới:** trong mỗi hàng lệch dưới 20 px. Phần lệch là do vành cong, không phải mũ trôi.
- **Màu nền:** các món có xanh lá (slime, ếch, vòng hoa, cùng hàng tai cáo và sừng) vẽ trên nền `#FF00FF`. Ở phần lõi của các món nền `#00FF00` không có pixel xanh lá, trừ lỗ nền nằm trong hình như lòng vòng của gậy.
- **Vũ khí:** 8 món đều trọn trong ô của mình (lưới 4x2), không chạm nhau, dựng đứng, súng chĩa nòng lên, sách để gáy bên trái.
- Chưa cắt sheet, chưa ghép lên đầu, chưa xem trong game: chưa có tool cắt mũ / vũ khí.

## Lỗi còn thấy
- **Vũ khí sai tỉ lệ cỡ:** cả 2 bản A/B vẽ các món gần cùng cỡ (sách to bằng khiên, dao gần bằng kiếm). Tool cắt phải co từng món theo số trong spec.
- **Slime:** góc chéo lên-phải còn chấm mắt ở mép.
- **Băng đô vải xanh** trùng kiểu với băng đô ninja, chỉ khác màu.
- **Nét mép xa còn sót:** vương miện pha lê (góc phải) và vương miện rắn (góc chéo lên-phải) còn thấy một đoạn nét mép xa của vòng. Đoạn này nhỏ, cần xem khi ghép.
- **Đè tóc:** khăn tu nữ và mũ phi công trùm kín đầu, sẽ đè lên tóc. Cần quy tắc ẩn tóc khi đội (ngoài phạm vi).
- `__grab` sửa lại để chỉ lấy ảnh nằm sau ảnh "User attachment" cuối. Có 1 lần JS treo quá 45 s (CDP timeout); chạy lại thì bình thường, không gửi trùng.

## Số lượt vẽ
18 prompt: mũ 10 + 7 vẽ lại, vũ khí 1 (A/B). Tất cả trên Tay.

## Câu hỏi
- Băng đô vải xanh có cần vẽ khác hẳn băng đô ninja (ví dụ khăn quấn hoặc nơ) không?
- Mũ trùm kín (khăn tu nữ, mũ phi công, mũ len, mũ ếch, slime) có ẩn lớp tóc khi đội không?
- Vũ khí: co theo spec trong tool hay vẽ lại kèm khung cỡ cho từng món?

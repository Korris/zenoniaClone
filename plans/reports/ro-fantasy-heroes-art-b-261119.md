# RO Fantasy heroes, art đợt B (19/11/2026)

- Tài khoản **Tan Pham** (Plus), chat đợt A: https://chatgpt.com/c/6afe7669-88e4-83ec-8049-f8c7ea9ceaaf. Hàm gửi đọc `/api/auth/session` trước mỗi lần gửi, lần nào cũng là "Tan Pham". Không dùng Tay. Không hết lượt.
- **Đổi phạm vi giữa chừng** (người dùng, khoảng 12:25): mỗi class chỉ 1 đòn đánh, kiểu game khoá mục tiêu. Mỗi class có 3 ảnh đi + 3 ảnh đánh, tổng 30 ảnh. Đòn của Warrior lấy từ ảnh "đánh 1" (chém ngang) đã vẽ, đổi tên thành `attack`. Ảnh "đánh 2" của Warrior đã bỏ.
- Commit trên branch `ro-fantasy-heroes`, chưa push:
  - 1c5016ee Warrior + ảnh mẫu đứng theo hướng của 5 class;
  - 7df46caa Wizard; fb95f731 Mechanic; 9658713e Assassin; da152257 Cleric.

## Ảnh đã nhận (30)

`Assets/ArtLibrary/ro-fantasy-heroes/src/body-<class>-<walk|attack>-<a>--<b>-chatgpt-N.png`. N = 2 là ảnh vẽ lại. Bản bị loại đã xoá.

| Class | Đi xuống / chéo xuống | Đi phải / chéo lên | Đi lên | Đánh xuống / chéo xuống | Đánh phải / chéo lên | Đánh lên |
|---|---|---|---|---|---|---|
| Warrior (chém ngang) | -2 | -1 | -1 | -2 | -1 | -1 |
| Wizard (chĩa gậy) | -1 | -1 | -1 | -2 | -1 | -2 |
| Mechanic (bắn súng tay) | -1 | -2 | -1 | -1 | -2 | -1 |
| Assassin (chém lướt hai dao) | -1 | -1 | -1 | -2 | -1 | -1 |
| Cleric (chĩa gậy, tay trái giữ khiên) | -1 | -2 | -1 | -1 | -2 | -1 |

Đã mở xem cả 30 ảnh. Ảnh nào cũng:
- đúng hướng, đủ 4 frame, đầu trọc, tay nắm rỗng;
- đồ và màu đúng như ảnh đứng, tông sáng ấm (Assassin vẫn đồ đen tím như ảnh đứng);
- nền `#00FF00`, không chữ, chân cùng một đường đáy;
- đúng 1536x1024, không hình nào chạm mép ô hay hình bên cạnh.

Cỡ đầu đo bằng bề ngang đầu (cả viền, tai) / chiều cao người, so với ảnh đứng cùng hướng. Phần lớn lệch trong khoảng ±4 %.

Ảnh mẫu đứng theo hướng, phóng x2: `ref/body-<class>-idle-<a>-<b>.png`.

## Ảnh vẽ lại

| Ảnh | Lỗi | Kết quả |
|---|---|---|
| Warrior đi xuống | đầu -8 đến -10 % | +2 đến +4 % |
| Warrior đánh xuống | đầu -6 đến -10 % | 0 % |
| Wizard đánh xuống | đầu -7 đến -8 % | 0 đến +4 % |
| Wizard đánh lên | ảnh 1774x887; đầu +5 %; nắm tay sát sọ | đúng 1536x1024; tay rời sọ; nhưng đầu -6 % (nhận bản này) |
| Mechanic đi phải | hàng chéo lên: đầu -5 đến -7 % | 0 %; hàng phải còn -4 đến -6 % |
| Mechanic đánh phải | đầu -4 đến -7 % | +1 đến +4 % |
| Assassin đánh xuống | vệt sáng chuyển động quanh nắm tay; đầu hàng trên +5 % | hết vệt; hàng chéo còn -3 đến -6 % |
| Cleric đi phải | đầu -5 đến -8 % | +1 đến +5 % |
| Cleric đánh phải (2 lần) | đầu -5 đến -7 % | lần 1: hàng phải +5/+6 %, hàng chéo +2 đến +4 %; lần 2: hàng phải -9 %. Nhận bản lần 1 |

## Lỗi còn

- **Cỡ đầu lệch 5-6 %, chưa sửa** (giới hạn 2 lần vẽ lại, sửa tiếp chỉ đổi qua đổi lại):
  - Cleric đánh phải, hàng phải: đầu **to** hơn +5/+6 %.
  - Đầu **nhỏ** hơn -5/-6 %:
    - Wizard đánh lên;
    - Mechanic đi phải (hàng phải);
    - Mechanic đánh xuống (hàng chéo);
    - Assassin đánh xuống (hàng chéo).
  - Có thể co/giãn đầu bằng tool khi cắt sheet.
- **Đánh hướng lên:** tay đánh duỗi sang ngang (phải), không duỗi ra xa người xem. Có ở cả 5 class.
- **Đánh hướng chéo lên-phải** (Warrior, Wizard): frame 1 người hơi xoay ra trước, thấy khoá thắt lưng.
- **Vết nhỏ:**
  - Cleric đi lên: vệt tối ngả xanh ở mép vạt áo giữa hai chân.
  - Assassin đi phải: một chấm nâu xanh nhỏ cạnh thân ở frame 1.
  - Tool tách nền có bước lọc chấm xanh (Sổ lỗi "vào game: thân") nên có thể tự xử lý, nhưng cần xem lại sau khi cắt.
- **Cỡ người không đều giữa các ảnh:** người cao 410-600 px tuỳ ảnh, ảnh hướng lên to nhất. Tool cắt phải co theo từng ảnh.
- Chưa xem trong game: chưa cắt sheet, chưa có code.

## Lượt vẽ

- 43 ảnh trên Tan Pham, cộng 1 lần bị từ chối không ra ảnh (Assassin đi phải, viết lại prompt thì được).
- 43 ảnh gồm:
  - 30 ảnh nhận;
  - 10 ảnh vẽ hỏng, đã thay bằng bản vẽ lại;
  - 3 ảnh theo phạm vi cũ, bỏ: "đánh 2" của Warrior (2 bản hướng xuống, 1 bản hướng phải).
- Không có A/B. Không hết lượt.

## Ghi chú cách làm (Tan Pham)

- Gửi ở 1 tab. Mỗi ảnh tải bằng 1 tab mới mở lại URL chat rồi đóng (Chrome chặn tải lần hai trong cùng tab).
- Cách chờ vẽ xong:
  - đã gửi được hơn 30 s;
  - không còn nút Stop;
  - cuối trang có chữ "Đã hoàn tất phản hồi";
  - `src` của ảnh "Ảnh được tạo" cuối trang khác ảnh trước khi gửi, và giữ nguyên qua 2 lần đọc.
- Ref của ô file input đổi sau một lúc (`ref_428` thành `ref_2239`): lỗi thì gọi `find` lại.
- Script đo (`seg.py`, `q.py`, `mkref.py`) để trong scratchpad, không đưa vào repo.

## Câu hỏi

1. Có cần sửa 5 ảnh lệch đầu 5-6 % bằng tool không (co/giãn đầu khi cắt), hay vẽ lại?
2. Đòn đánh hướng lên đang duỗi tay sang ngang. Có cần vẽ lại cho tay đưa ra xa người xem (gần như khuất sau lưng) không?

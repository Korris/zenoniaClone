# Fighter xương rig (cutout rig): bản thử 15/11/2026, ĐÃ BỎ

**Kết luận:** người dùng bỏ rig, nhận xét "nhìn như con rối giấy". Fighter giữ sprite frame-by-frame như 5 class khác. Chưa commit.

## Đã thử những gì (3 vòng trong một ngày)
1. **Hướng xuống: idle, walk, jab.**
   - ChatGPT (chat Tay https://chatgpt.com/c/6af926a3-ddc8-83ec-a9d0-b6347d8a7ab9) vẽ 13 mảnh rời.
   - Tool cắt mảnh, dựng cây xương; keyframe JSON nội suy mượt.
   - Code chạy lúc runtime bằng SpriteRenderer thường, không dùng package.
2. **Vòng 2: cho rig đánh được.**
   - Walk xuống làm lại vì bị chê "đi như thây ma".
   - Jab có lực hơn; thêm cross, hook, uppercut và Dash Punch.
   - Thêm hướng phải (trái là phải lật) bằng parts sheet nghiêng thứ hai.
   - Exe chọn được Fighter và đánh quái thật.
3. **Vòng 3: hướng ngang bị chê "ốm nhách".**
   - Vẽ lại parts sheet nghiêng v2 có gửi kèm vóc của sheet làm mẫu, rồi phóng ngang từng mảnh.
   - Đo lại idle hướng phải: diện tích hình từ 6776 lên 8363 px (sheet: 9606 px).

Ảnh lưu so sánh (Warrior | Fighter sheet | rig, hướng xuống và phải): `plans/reports/fighter-bone-rig-dropped-vs-sheet-archive.png`.

## Vì sao không đạt (lý do)
- **Nhìn ra mảnh giấy xoay quanh đinh ghim.**
  - Mỗi mảnh là một hình phẳng vẽ sẵn, chỉ xoay, dời, co giãn. Thân, vai, tay không đổi hình theo tư thế như frame vẽ tay: không vặn vai, không thấy cơ căng, nếp quần không đổi.
  - Ở cỡ chibi, đầu to và găng to, các khớp xoay lộ rất rõ.
- **Hướng nhìn thẳng (xuống) là điểm yếu tự nhiên của cutout.**
  - Đi về phía camera hay đấm vào camera chỉ giả được bằng co giãn và phóng to găng.
  - Đọc được là cú đấm nhưng không có chiều sâu.
- **Mỗi hướng cần một bộ mảnh riêng, khó giữ vóc người giống sheet.**
  - ChatGPT không giữ đúng tỉ lệ giữa các lần vẽ: lần 1 mảnh khảnh; lần 2 vẫn phải phóng ngang bằng tool.
  - Kéo dãn mảnh lại làm art lệch khỏi nét gốc.
- **Hai kiểu vẽ lẫn nhau.** Art rig sáng hơn, nét mềm hơn (lọc khi xoay), găng to hơn. Đứng cạnh Warrior frame-by-frame trông như hai game khác nhau.

## Bài học
- Phong cách chibi vẽ tay chi tiết của game này hợp frame-by-frame. Cutout chỉ hợp nhân vật thiết kế riêng cho cutout (mảng phẳng, khớp giấu sau áo hoặc giáp, góc nhìn nghiêng).
- **Nên cho người dùng xem sớm** một gif 2–3 giây trong game ngay sau vòng 1, trước khi làm đòn và hướng thứ hai. Cảm giác "rối giấy" thấy ngay từ walk.
- Số dung lượng đẹp (rig 2 hướng 266 KB / ~1,3 MB RAM, so với sheet Fighter 8,1 MB / ~48 MB) không bù được cảm giác. Nếu cần tiết kiệm RAM thì nén texture hoặc nạp sheet theo nhu cầu sẽ hiệu quả hơn.
- Kỹ thuật (ghi lại nếu sau này dùng cho quái hay vật có khớp):
  - Không scale lệch một Transform cha đã xoay, vì làm méo (shear) mảnh con.
  - Hình ghép mẫu của ChatGPT hay giấu tay sau găng, nên đừng dò khớp tay trên hình ghép.
  - Parts sheet phải gửi kèm frame sheet cùng hướng làm mẫu vóc, và đo bề ngang so với sheet trước khi cài.
  - Chuyển clip cần cross-fade ngắn.
- Phát hiện phụ có ích: Fighter không có sheet `attack3` nên lúc cast skill bị **vô hình**. Lỗi này có từ trước rig, và đã sửa (giữ lại).

## Đã gỡ / đã giữ
- Gỡ khỏi game:
  - Code: `HeroRig.cs`, `HeroRigSkeleton.cs`, `HeroRigAnimator.cs`, `HeroRigData.cs`, `DebugCapture.RigCompare.cs`, các nhánh rig trong `HeroRenderer.cs` / `DebugCapture.cs`, phím F12.
  - Dữ liệu: `Resources/{Sprites,Data}/classes/fighter/rig/`.
  - Tool: `Tools/build-fighter-rig-parts.py`, `build-fighter-rig-clips.py`, `fighter_rig_right_layout.py`, `fighter_rig_side_clips.py`.
  - Ảnh và gif rig trong `docs/reference/` và `plans/reports/`.
- Giữ (chưa commit):
  - Sửa cast skill: `HeroController.CastPose` dùng `ComboAnims[1]` (`melee2`) khi hướng đó không có `attack3`; thêm `HeroRenderer.HasAnim` (chỉ kiểm có sheet).
  - Cờ test `--hero-select-test --hero-select-start <Class>` để chạy thử màn chọn với class bất kỳ.
  - Art nguồn lưu trữ: `Assets/ArtLibrary/classes/fighter/rig/` (2 parts sheet hướng xuống / nghiêng v1, v2, ảnh mẫu `ref/`).
- Kiểm sau khi gỡ:
  - Build từ `C:/temp/zu-copy-rig` thành công, 0 lỗi; exe `C:/temp/player-fighter-rig/Zenonia.exe`.
  - Chụp Fighter đánh combo bằng sheet rồi cast Dash Punch: thấy hình ở tư thế `melee2` (`plans/reports/fighter-sheet-combo-and-skill-cast-visible-ingame.png`).
  - `dotnet test`: ZenoniaSkills 313/313, ZenoniaNet 404/404 đạt.

## Câu hỏi mở
- Fighter có cần một sheet `attack3` riêng (tư thế cast) thay cho mượn `melee2` không?

# Wizard: dáng pháp sư chiến đấu, 09/11/2026

Người dùng yêu cầu Wizard ngầu hơn Warrior/Mechanic thay vì dáng hiền, tay áo loe và thân bè. Đổi cả 10 bộ idle/down: vai góc cạnh, tay áo ôm với giáp cẳng tay, eo gọn, hai vạt tách rõ để thấy chân, tay hạ ngoài hông. Gậy nghiêng ra ngoài 10 độ, sách theo nắm tay thấp hơn. Tóc trắng/mắt xanh giữ lại; mặt mặc định hướng xuống đổi sang bình tĩnh nghiêm, không cười.

Nguồn vẽ bằng imagegen: `Assets/ArtLibrary/classes/wizard/outfits/stance-v4/t0-body.png`, `tier-sheet.png`, hai prompt trong cùng thư mục. Tool `Tools/build-wizard-stance-v4.py split` tách mảng alpha theo từng nhân vật, không cắt theo ô cố định vì vạt áo vượt vạch. T1–T9 xuất thành `tN-body.png`; T0 dùng ảnh vẽ riêng chất lượng cao. Alpha đục 245–254 của sheet được chuẩn hóa thành255, alpha mép giữ nguyên.

`write` dùng lại bộ xuất sheet/anchor đã kiểm của v3, khớp cằm107–đế189 và đầu83×78 tại(57,29), vẫn cao160px. Độ rộng thân được tính theo tỷ lệ tóc nguồn với đầu83px, bỏ WIDTH tăng tùy ý theo bậc làm người bè. Dò tay trong dải hông, bỏ ngọc/kim loại phía trên dễ nhầm da. Xuất đủ 4 frame thở, bare/headless/hand, anchor đầu và trang bị theo bộ. Kiểu tóc0 hướng xuống lấy biểu cảm mới; bốn kiểu tóc còn lại giữ nguyên.

Chạy `python Tools/build-wizard-stance-v4.py write`, rồi `verify`. Không chạy lại v3/write nếu muốn giữ dáng này. Bản sao trước cài ở `stance-v4/before-install/`. Chỉ thay idle/down; trang phục đi/đánh và các hướng khác vẫn là art cũ.

Bản chạy: `Build/wizard-reference/player-battle-mage/Zenonia.exe`, build từ bản sao `Build/wizard-reference/project`; không đổi dev-start của người dùng. Kiểm bằng `Tools/check-wizard-height.ps1 -Exe ... -Output Build/wizard-reference/captures-battle-mage`.

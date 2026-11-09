# Wizard: bảng trang phục hiện tại

## Vẽ lại từng bộ từ mẫu phóng4x và thêm5kiểu tóc

Nguồn thân hiện hành là `Assets/ArtLibrary/classes/wizard/outfits/reference-20261109-exact/src/tN-body.png`, 10ảnh imagegen riêng theo đúng mảnh thiết kế từng bậc phóng4x ở `ref/`. Prompt từng bộ và từng kiểu tóc lưu trong `prompts.json`. Không dùng lại thân tách cũ `reference-20261108/tN-body.png`: các bản đó diễn giải sai chi tiết trang phục.

Đối chiếu: T0 khóa sao/buckle ngọc xanh và họa tiết sao trên cổ/vạt; T1 dây chéo, hai cuộn bản đồ, la bàn hông, viền tua; T2 giáp pha lê hồng trên vải đỏ rượu; T3 ngọc trai và vỏ sò; T4 dây thừng/armillary ở eo và vạt trắng/cam; T5 nhiều đai/xích và viền trăng khuyết; T6 tua vai sĩ quan/dây bện/tua eo/mây; T7 vai gỗ/lá/hoa nhỏ/khóa đỏ; T8 vai tinh thể/dải quỹ đạo/chòm sao; T9 vai cánh rồng/vảy đen/lông bên hông/rồng đỏ trên vạt trắng.

Tóc: giữ Northwind ở index0; index1–5 là5kiểu mới, thay4kiểu cũ và thêm index5. Mỗi kiểu có down/up/right; trái lật từ phải theo runtime. Chạy `python Tools/install-wizard-exact-hair.py` rồi `python Tools/install-wizard-current-reference.py`. Kiểm tự động khoảng cách mắt hướng xuống lệch dưới5% so với đầu mặc định, vị trí cổ và khung ảnh hợp lệ. Runtime tự đếm6kiểu; phím H đổi tóc trong bản test.

Nguồn tóc là `src/hairN-turnaround.png`: undercut bạc/mái cyan; xanh navy buộc thấp; lavender rẽ giữa; đen điểm teal vuốt ra sau; trắng tết nửa đầu. Bảng xem3hướng ở `Build/wizard-current-reference/new-hairstyles-turnaround.png`; lưới6kiểu ×10bộ ở `hairstyles.png`.

Bộ dò tay hiện tìm cả các mảng da ngón tay bị nét mực chia nhỏ, ưu tiên nắm tay đủ lớn thay vì ngọc trai hoặc viền vàng gần màu da. Tọa độ và lớp tay được xuất lại sau mỗi lần vẽ lại thân.

Áp dụng lại bảng người dùng gửi `9670d96f-6102-4692-8a05-79ebebdb31ad.png` vào Resources chính, thay dáng battle-mage v4 ở idle/down. Gậy/sách tách ở `Assets/ArtLibrary/classes/wizard/outfits/reference-20261108/tN-{staff,book}.png`; thân đã thay bằng bản vẽ lại ở trên. Không dùng bộ Astral V7/V8.

Chạy `python Tools/install-wizard-current-reference.py` từ bất kỳ thư mục nào. Tool sao lưu file trước cài vào `Build/wizard-current-reference/before-install`, xuất báo cáo và ảnh ghép tại đó. Không chạy lại các installer so sánh copy-only để cài bộ này.

- Thân scale đồng đều hai trục, dùng mốc Northwind đã sửa cằm94/đế189 thay cho cằm107/đế189 (áo cũ bị thu nhỏ vào đoạn82px). Phần thân có95px nên rộng/cao hơn khoảng16%, không ép chiều ngang theo WIDTH.
- Tóc mặc định lấy trực tiếp từ Northwind V7 T0: tóc trắng, mái xanh cyan, mắt xanh. Texture đầu đăng ký cổ107 để giữ đúng anchor khi đi/đánh; riêng idle/down dùng scale đồng đều65/78 và offset căn cổ94. Cả5kiểu tóc dùng phép căn đó, không kéo méo mặt.
- Chỉ thay hàng0 của 30sheet bare/headless/hand idle; đủ4frame thở mỗi bộ. Các hàng hướng khác và anchor của động tác khác được kiểm tra giữ nguyên.
- Trượng cao160,169,…,241px tại scale0,447. Thu riêng bề ngang cán quanh trục, không thu đầu trượng/đuôi trang trí. Cán tại grip thực5,8–6,3px, nhỏ hơn nắm tay từng bộ; base/enhanced/idle-stance và icon được xuất đồng bộ, grip đo sau khi thu cán.
- Sách tăng chiều rộng40→53,5px. Anchor mỗi bộ đo từ bàn tay đã vẽ, lớp tay đè lên trang bị. Icon áo/gậy/sách đồng bộ.

Kiểm chứng: `verification.json` ghi chiều dài trượng tăng nghiêm ngặt qua10bậc, bề ngang cán và nắm tay, 30sheet giữ nguyên các hướng khác, anchor trang bị ngoài idle/down không đổi. `all-tiers.png`, `breathing.png`, `hairstyles.png` ghép cùng hệ số và đường chân, đúng thứ tự lớp body/head/gear/hand.

Đã bỏ phép ép ngang `kx=WIDTH[t]/...` khỏi exporter cũ `build-wizard-stance-v3.py`, thay bằng `kx=ky`. Runtime không có phép bóp ngang áo riêng; `Lean` chỉ co dọc rất nhẹ khi di chuyển. Lỗi áo nhỏ lần này nằm ở mốc export cằm107. Báo cáo kiểm scaleX/scaleY sai khác dưới1,2% do làm tròn pixel.

Bản test: `Build/Wizard-Current-Reference/Zenonia.exe`. Bảng trang phục chỉ có tư thế đứng nhìn xuống; đi/đánh và các hướng khác vẫn dùng sheet hiện có. Art trượng/sách dùng chung nên các động tác đó cũng nhận trang bị mới cùng kích thước.

# Wizard: vai, cổ và choàng rộng hơn

Bản so sánh riêng theo người dùng duyệt: vẽ lại trang phục có độ dày ở vai/cổ và choàng mở sau lưng/hông, giữ nhận diện từng bậc, eo và tỉ lệ người. Không kéo ngang sprite, không cài vào Resources chính. Nguồn chỉnh từng bộ T0–T9 ở `Assets/ArtLibrary/classes/wizard/outfits/grandeur-20261109/src/`, prompt trong `prompts.json`; nguồn đối chiếu nguyên bản `reference-20261109-exact/src/`.

Chạy `Tools/build-from-copy.ps1 -CopyDir C:/temp/zu-copy -OutExe C:/Project/zenoniaUnity/Build/wizard-grandeur/player/Zenonia.exe -WizardGrandeur`. Switch sync bản hiện tại vào copy rồi gọi exporter với `--target`, `--body-source`, `--output` riêng. Exporter kiểm hash Resources Wizard chính trước/sau để không ghi đè bản đang dùng.

Phạm vi: idle/down,10bộ ×4frame thở ×6kiểu tóc hiện có. Cằm94/đế189, thân scale đều hai trục. Giữ art/cỡ gậy và sách, cán mảnh, các hướng và anchor động tác khác. Kiểm30sheet không đổi các hướng khác, grip theo nắm tay, trượng tăng160→241px. Lớp đầu được tách theo các mảng tóc không liền nhau và bỏ viền màu tóc còn sót; giữ các phần vai nằm ngoài vùng đầu.

File chạy trong `Build/wizard-grandeur/`: `Play-Wizard-Grandeur.cmd`, `Play-Current.cmd`, `Play-Northwind.cmd`. Cả ba cùng lv60/T9/zoom42/cửa sổ1280×720. E/Q đổi bậc trang bị; H đổi tóc. Bản Wizard hiện tại và Northwind vẫn là hai EXE đã build trước đó.

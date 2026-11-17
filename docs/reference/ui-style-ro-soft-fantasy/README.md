# Style thử nghiệm "RO Soft Fantasy" (17/11/2026)

Người dùng: "thử kêu nó design 1 style hoàn toàn mới cho các menu đang có thử xem cái nào mà nhìn clean như ragnarok midgard senki". Chat ChatGPT (Tay): https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde. Trạng thái: chỉ là mockup để so với Adventure Paper (`docs/reference/ui-all-menus-261117/`), CHƯA chọn, chưa code.

ChatGPT nói Midgard Senki không phải màn nào cũng sáng và tối giản, nên lấy Ragnarok Origin làm mẫu chính cho menu, còn Midgard Senki làm mẫu cho cách xếp HUD và icon.

## Quy tắc

- Màu: Cloud White `#F7F8F5`, Mint Mist `#EAF4F2`, Sky Mist `#E4EDF7`, Ink Blue `#32566B` (chữ), Mint Primary `#48B6A5` (nút chính), Sky Secondary `#78B7F0` (nút phụ), Danger Coral `#E46C77`, Reward Gold `#F4C66A`.
- Font: Nunito Sans (nội dung tiếng Việt), Nunito ExtraBold (tiêu đề); có thể giữ Baloo cho chữ vui như LEVEL UP.
- Khung lớn trắng ngà 96 % bo 20 px, bóng nhẹ, header gọn, tab dọc bên trái; khung vừa trắng 94 % viền xanh xám 1 px bo 16 px; khung nhỏ / tooltip / toast dạng kính mờ 88-92 % bo 12 px.
- Nút khối màu phẳng, ít bóng 3D. Tab dạng viên thuốc. Ô item vuông bo 10 px viền mảnh, giữ màu độ hiếm và icon hiện có. Thanh tài nguyên dạng viên thuốc trắng mờ.
- HUD: nhóm nhỏ trên nền tối mờ cho dễ đọc trên map.

## So với Adventure Paper (theo ChatGPT)

| | Adventure Paper | RO Soft Fantasy |
|---|---|---|
| Cá tính fantasy | Rất mạnh, thủ công, ấm | Nhẹ, hiện đại |
| Độ thoáng | Trung bình, nhiều trang trí | Cao |
| Mobile | Khung gỗ chiếm nhiều chỗ | Dùng diện tích tốt hơn |
| Art UI | Nhiều khung, ruy băng, viền vẽ riêng | Ít asset trang trí |
| Unity | Nhiều sprite + 9-slice | Chủ yếu Image, 9-slice, TMP, màu, layout |
| Công chuyển | Đã làm phần lớn | Cao lúc đầu, thay toàn bộ |

ChatGPT khuyên nếu chọn thì thử trước trên Quest log, Shop, Trade; ba màn đó dễ đọc và thoáng hơn ở 960×640 thì mới đổi cả game.

## Ảnh

- `01-style-board.png` … `13-class-select-level-up.png`: style board, HUD, túi đồ, skill, pet, gacha, bảng Truy Nã, cửa hàng, cài đặt, nhiệm vụ + NPC, xã hội (Player Card, lời mời thách đấu, đang gửi, người chơi gần đây), Trade, chọn nhân vật + Level Up.
- `tong-hop-ro-soft-fantasy.jpg`: 13 ảnh trên một tấm.
- `so-sanh-3-style/`: mỗi màn 3 cột: hiện tại trong game | Adventure Paper | RO Soft Fantasy.

## 3 Wanted Boss vẽ lại (18/11/2026)

Người dùng: "lấy hình 3 con wanted boss và kêu nó vẽ thử lại với style RO soft fantasy". Gửi 3 ảnh concept (`Assets/ArtLibrary/custom/wanted-boss/ref/concept-*-2x.png`), chỉ nói vẽ lại theo RO Soft Fantasy, phần còn lại ChatGPT tự quyết. Kết quả: `wanted-boss/3-wanted-boss-ro-soft-fantasy.png` (3 con trên nền lâu đài sáng, có bảng tên), so sánh `wanted-boss/so-sanh-cu-tren-moi-duoi.jpg`. Chỉ là concept, chưa phải sprite sheet trong game.
- Người dùng thích kiểu boss trong mockup Bảng Truy Nã (`07-wanted.png`, chép thêm thành `wanted-boss/user-pick-wanted-board-boss-style.png`) hơn boss hiện tại. Lần vẽ 1 (`3-...`) và lần 2 do người dùng tự hỏi ChatGPT (`4-wanted-boss-chatgpt-own-style.png`) đều vẫn giống concept cũ. Lần 3 gửi lại ảnh bảng và ghi rõ "theo đúng kiểu trong ảnh, không dựa vào concept cũ": `wanted-boss/5-wanted-boss-board-style.png` = ĐÚNG kiểu người dùng muốn (Rimeclaw Tyrant sói trắng đội vương miện + áo choàng xanh, Glacial Colossus golem đá xám pha lê xanh viền vàng, Mire Hydra Queen nữ hoàng tóc tím đuôi rắn tím + 3 đầu rắn). Chưa làm sprite sheet.
- 40 người đánh Mire Hydra Queen (18/11/2026): ảnh thật `wanted-boss/ingame-40-players-vs-mire-hydra-queen(-b).png` (chụp bằng `Tools/wanted-boss-crowd-playtest.ps1 -Boss mireHydraQueen`, 39 bot + 1 cửa sổ), ChatGPT vẽ lại theo RO Soft Fantasy (tự quyết phần còn lại): `wanted-boss/6-40-players-vs-mire-hydra-queen-ro-soft-fantasy.png`. Boss trong bản vẽ lại tự theo kiểu nữ hoàng rắn của mockup Bảng Truy Nã.
- Hỏi ChatGPT (18/11) để có khung hình như ảnh vẽ lại 40 người có phải thu nhỏ nhân vật không. Trả lời: làm cả hai: heroScale 1.15 → 1.0 VÀ camera cao 160 → 192 (12 ô, nhân vật còn ~72 % so với bây giờ), cho zoom gần lại 160, thử 208 cho raid; không đổi sprite / map / góc nhìn; không đổi hitbox, tầm đánh; nếu đông vẫn rối thì giảm VFX và số sát thương của đồng đội trước khi thu nhỏ thêm; 224 quá nhỏ cho điện thoại. "Không phải nhân vật vẽ quá to, mà là x1.15 cộng camera chỉ 10 ô dọc làm chiến trường chật." Thử: `D:/temp/player-261117-menu/dev-start-camera-192.json` / `-208.json` (đổi tên thành `dev-start.json` cạnh exe; khi đặt cameraViewHeight thì cuộn chuột không zoom).

## Hỏi ChatGPT: Paper hay RO dễ nhìn hơn (18/11/2026)

Gửi 3 ảnh `so-sanh-3-style/` (túi đồ, shop, trade). ChatGPT chọn RO Soft Fantasy cho toàn bộ UI: chữ đậm trên nền sáng dễ đọc, màu nút phân vai rõ, phân vùng bằng khoảng trắng thay vì gỗ / ruy băng, đỡ chiếm chỗ trên điện thoại. Túi đồ thắng rõ nhất (thấy thẳng lưới 50 ô + trang bị + so sánh, không phải lật carousel), shop và trade dễ đọc hơn. Adventure Paper: cá tính mạnh hơn, item nổi đẹp trên nền tối, đã có nhiều asset; RO dễ giống game mobile khác.
Sửa khi làm RO: nền trắng ngà #F7F8F5 đục 95-98 % (đừng trắng lạnh), chữ và vùng chạm to hơn mockup (tối thiểu ~44×44 px vật lý), giữ icon class, chân dung NPC, màu độ hiếm, biểu tượng boss và họa tiết fantasy nhỏ.
Trận 40-80 người: HUD chiến đấu KHÔNG dùng panel trắng trong suốt; dùng biến thể nền xanh đen mờ 85-95 %, chữ trắng, thanh HP / MP màu nổi. Menu dùng bản sáng.
Kết luận: 80 % ngôn ngữ RO Soft Fantasy + 20 % chi tiết fantasy riêng, không chép nguyên Ragnarok; bố cục gọn quan trọng ngang phong cách.

## Hỏi ChatGPT: có nên vẽ lại nhân vật cho hợp RO Soft Fantasy (18/11/2026)

Gửi ảnh màn chọn class + ảnh đánh quái hiện tại. ChatGPT: KHÔNG vẽ lại toàn bộ; giữ ~85-90 % thiết kế. Tỷ lệ chibi đã gần RO, giữ nguyên body, đầu, 6 tóc + nhuộm, T0-T9, vũ khí, animation. Lệch ở: mắt sắc / mặt dữ (làm mềm nhẹ), nhiều nét phụ trong giáp (giữ viền ngoài, bớt nét trong), màu đỏ / đen / vàng / tím quá gắt (giảm 10-15 %, giữ màu class), đổ bóng tương phản mạnh (cel shading mềm 2-3 cấp), glow vũ khí thường trực (bớt). Hướng tên "Soft Action Chibi".
Thử rẻ: 1 Warrior T0 hướng xuống, 3 bản cạnh nhau: A hiện tại / B chỉ chỉnh màu + tương phản / C vẽ lại Soft Action Chibi; thu về cỡ chơi thật trên map tuyết, rừng, làng; C không đẹp hơn B rõ thì chọn B.
Ưu tiên: 1 UI RO Soft Fantasy, 2 camera + cỡ nhân vật, 3 VFX / số sát thương trong trận 40-80 người, 4 thử Warrior Soft Action Chibi, 5 class khác chỉ khi thử thật sự tốt hơn. "RO Soft Fantasy là phong cách giao diện, không bắt nhân vật thành pastel; nhân vật màu đậm trên UI sáng còn tạo điểm nhấn."

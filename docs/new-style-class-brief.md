# Brief: chuyển một class sang style mới

Làm theo đúng cách đã chốt với Warrior. Người dùng duyệt ngày 2026-11-01.

## Phạm vi
- Làm: idle, walk, hurt, các động tác đánh thường của class, tư thế skill riêng của hero trong `ExtraAnims`.
- Làm cả vũ khí và vũ khí bổ trợ theo style mới (xem mục dưới). Làm sprite trước, rồi vũ khí, rồi đồ bổ trợ.
- Không làm: `dash` (game dùng sprite đi kèm dư ảnh), hiệu ứng skill trong `fx/`, `skills/` (đang tốt, không đổi một file nào).
- Không đụng `idle` hướng xuống (hàng 0): đã xong, vẽ liền vũ khí, cơ chế `fallback`.

## Cách vẽ từng loại
- **Hình mẫu bắt buộc tải kèm mỗi lần vẽ:** `docs/reference/hero-idle-down-5-classes.png`, cùng ảnh thân của class ở `Assets/ArtLibrary/custom/idle-down-set/<class>.png`.
- **Attack và tư thế skill:** bám sprite cũ, vì đường chém và hiệu ứng khớp theo nó. Tải hàng `body/bare/<anim>.png` cũ, yêu cầu đúng tư thế, thứ tự frame, vị trí tay, chỉ đổi nét. Giữ số frame, giữ góc vũ khí từng frame, giữ thời gian.
- **Idle và walk hướng trái, phải:** thiết kế mới theo hình mẫu, không tải sprite cũ, người dùng không thích bản cũ. Giữ số frame.
- **Walk xuống, walk lên, idle lên, hurt:** vẽ theo hàng cũ.
- **Hướng ngang phải nhìn ngang thật:** mặt, ngực, chân quay về hướng đó, không nhìn vào camera. Hướng lên thấy gáy. Kiểm tra từng frame, sai thì vẽ lại.
- **Prompt ngắn.** Không vũ khí, nắm tay rõ, nền `#00FF00` phẳng (`#FF00FF` nếu art có xanh lá, như Cleric), không bóng, không hiệu ứng, không chạm mép. Cleric không có cánh: sau này cánh là item riêng. Sprite trong game không vẽ companion vì game vẽ riêng; ảnh giới thiệu và ảnh mẫu thì có.
- **Idle thở:** một frame vẽ, uốn thở nhỏ bằng script (cách trong `Tools/build-warrior-move.py`), không vẽ 4 lần, nếu không sẽ giật.

## Vũ khí và đồ bổ trợ
- **Mọi lần nhờ ChatGPT vẽ vũ khí đều tải kèm ảnh màn chọn tướng của class đó (`Assets/ArtLibrary/custom/hero-select/<class>.png`) để style bám sát ảnh này (người dùng dặn).** Không dùng `mechanic-gauntlet-old.png`, `cleric-wings-old.png`: đó là bản cũ.
- Theo cách đã làm với kiếm và khiên Warrior: mẫu chuẩn cắt hoặc đo từ vũ khí trong `docs/reference/hero-idle-down-5-classes.png` (ảnh thân `idle-down-set/<class>.png`), rồi mọi tier cùng hình bóng, giữ màu và họa tiết riêng của từng tier từ art cũ. Xem `Tools/build-warrior-sword.py`, `Tools/build-warrior-shield.py`.
- Làm cả 10 tier, mỗi tier bản thường và cường hoá. Bản cường hoá cùng hình bóng, mạnh hơn rõ, không thò ra ngoài viền. Sao lưu art cũ trước khi ghi đè. Cập nhật `baseGrip`, `enhancedGrip` của class trong file `held-*.json`, đo từ art; `gripX, gripY` đúng giữa cán. Giữ độ dài trên màn hình như cũ.
- Làm theo thứ tự tier từ thấp lên, tier đầu kiểm tra kỹ trên nhân vật rồi mới làm tiếp. Hết lượt vẽ thì dừng và báo tier nào xong.
- Cập nhật icon vũ khí và đồ bổ trợ của class trong `Sprites/icons` (xem lệnh `icons` trong `Tools/build-warrior-sword.py`), kiểm tra `git diff --stat` chỉ đổi icon class mình.
- Mechanic chỉ cầm một khẩu súng, tay kia là bàn tay thường để trống, trụ bắn đứng riêng. Không có găng cơ khí, không có súng thứ hai, dù hình mẫu idle hướng xuống đang vẽ như vậy.
- Đồ bổ trợ từng class: Assassin là dao tay trái (chung art dao, kiểm tra `Parts`); Mechanic là trụ bắn đứng dưới đất (mẫu `Assets/ArtLibrary/classes/mechanic/turret-src/turret-sample.png`, nòng chĩa trái, điểm cầm ở giữa đế, không dùng `companion/float`), Cleric là linh thú (`companion/art`, `companion/float`: giữ định dạng và số frame, không có trong `Parts`); Wizard không có art bổ trợ cầm tay trong game (sách trong hình mẫu chỉ thuộc idle hướng xuống), đừng tự thêm code, báo lại. Cánh không thuộc Cleric.

## Cỡ và vị trí
- Cao 164 px từ đỉnh đầu tới đế trong ô 192 px, đế đúng hàng cũ của class. Cùng scale trong một hàng. Căn theo chân và đầu, đo không ước lượng.
- **Đầu và mặt phải cùng cỡ với idle hướng xuống của class.** Đo từng hàng, lệch thì sửa (xem `HEAD_GROW`, `grow_head` trong `Tools/build-warrior-move.py`).
- **Vũ khí giữ đúng cỡ cũ của class:** không đổi `s`, `DrawScale`. Người dùng từng bắt trả lại khi kiếm Warrior bị thu nhỏ.
- **Vũ khí nằm trong tay:** điểm cầm là giữa cán. Với góc vũ khí cũ, chỉ đổi `x, y` theo nắm tay mới. Xem phóng to từng hàng; quy tắc không đè lên tay. Lớp `body/hand` chỉ vừa nắm tay.
- **Không đổi tay khi quay trái.** Hàng trái lật từ hàng phải sẽ đổi tay: tay cầm vũ khí phải đúng dáng tay cầm, không được là tay giơ lên kiểu tay kia. Nếu sai thì vẽ riêng hàng trái, và bỏ anim đó khỏi `MirrorLeft`.

## Tài khoản, máy và file
- ChatGPT qua Claude in Chrome: nạp `mcp__claude-in-chrome__*` bằng một ToolSearch. Browser deviceId `7fd3e286-b745-4da0-b1ff-cecfb5c78fc7` (`select_browser`, người dùng chọn trong chat). Tài khoản Tan Pham (Plus), kiểm tra tên qua `/api/auth/session` trước khi gửi. Không dùng "xaochai", "Tieu Duy Phuong". Không nhập mật khẩu, không đổi tài khoản.
- Tab riêng, chat mới, chỉ lấy ảnh từ tab của mình, mở từng ảnh tải về để xác nhận, đóng tab khi xong. Ô nhập trên trang chat mới dùng chung nháp giữa các tab: chụp màn hình xem đúng chữ rồi mới gửi. Trình duyệt có thể chặn tải nhiều lần: báo lại nếu kẹt.
- Tải ảnh: fetch `img.src` thành blob, bấm `<a download>`, về `C:\Users\korri\Downloads\`, đặt tiền tố tên class. `file_upload` chỉ nhận đường dẫn trong repo.
- Giữ độ thống nhất giữa các tài khoản: một bộ đồ chỉ vẽ trên một tài khoản; trên tài khoản Tay dùng Temporary Chat nếu chế độ đó cho tạo ảnh, để bộ nhớ không kéo art cũ vào; vẽ thử một hàng, so cạnh hàng đã có rồi mới vẽ tiếp.
- Hết lượt vẽ thì dừng và báo rõ phần xong, phần chưa; phần chưa làm giữ bản cũ.
- Python: `C:\Users\korri\AppData\Local\Temp\claude\D--zenoniaClone\fe6a3c77-b3df-4eba-936f-3b0510d923d2\scratchpad\venv\Scripts\python.exe`.
- Tool chạy lại được: `Tools/build-<class>-new-style.py`; nguồn và bản sao lưu: `Assets/ArtLibrary/classes/<class>/new-style/`. Mẫu tham khảo: `Tools/build-warrior-move.py`, `Tools/build-warrior-attack.py`, `Tools/build-warrior-skill-poses.py`, `Tools/compose-hero-frame.py`.
- Chỉ sửa file của class mình. Trong `HeroClassDef.cs` chỉ sửa dòng class mình (`HandAnims`, `MirrorLeft`, `BareAnims`), đọc và ghi ngay vì agent khác cũng sửa. Ghi lỗi mới vào "Sổ lỗi" của `docs/sprite-rules.md`: đọc lại ngay trước khi sửa, chỉ thêm dòng của mình.
- Không commit, không build, không chạy Unity trên project này.

## Tự kiểm tra và báo cáo
Ghép đúng cách game vẽ bằng `Tools/compose-hero-frame.py`, xem từng hàng phóng to 2-3 lần cạnh idle hướng xuống, soi nắm tay trên cán, tạo GIF. Lưu ở scratchpad `<class>-new\`. Báo cáo tiếng Việt: file đổi, đường dẫn ảnh xem trước, số ảnh đã vẽ, phần chưa làm, phần dưới chuẩn, phần chưa kiểm tra.

# Mechanic: 10 tier súng lục và pháo vẽ lại theo sheet người dùng (2026-11-03 tối)

## Làm gì
- Nguồn: `Assets/ArtLibrary/classes/mechanic/new-style/refs/ref-user-10-guns-and-cannons.png` (10 hàng = 10 tier theo thứ tự scrap..dragon, 5 cột: súng Normal, súng Enhanced, pháo ngang, pháo đầu nòng về người xem, pháo đuôi về người xem).
- ChatGPT, chat "Tạo warrior 2D" https://chatgpt.com/c/6ae8db29-6900-83ec-a73a-0d1f6d148fa4 (sidebar hiện "Tay / Plus"; brief nói "Tân Korris", cùng tài khoản Tay, deviceId cb671945). Sheet đã nằm sẵn trong composer, gửi cùng prompt đầu. 20 ảnh: 10 súng (1024x1536, trên Normal dưới Enhanced) + 9 pháo (1536x1024, 3 góc) + 1 pháo brass bị bác. Không hết hạn mức.
- Cài bằng `Tools/build-mechanic-pistol-tiers.py install <10 tier>` + `icons`, `Tools/build-mechanic-cannon-tiers.py install <9 tier>` + `icons`.

## Tier
- Súng: đủ 10/10 (scrap, brass, copper, plasma, ember, frost, volt, gatling, star, dragon). Scrap nay cũng lấy cả 2 bản từ ảnh ChatGPT (sheet hàng 1 là khẩu vẽ lại), `pistol-base.png` mẫu giữ nguyên làm file dự phòng/kích thước.
- Pháo: 9/9 (scrap giữ art mẫu theo tool). Không có bản cường hoá (code chưa vẽ).

## Tool sửa
- `build-mechanic-pistol-tiers.py`: `fit` co đều theo chiều dài mẫu, góc dưới trái (trước ép khung 258x238 không giữ tỉ lệ); bản cường hoá co cùng hệ số bản thường và lùi vào nửa chênh quầng, grip lấy của bản thường; `handle_centre` dò cán từ hàng đáy lên (dragon bị bắt nhầm hàm rồng); icon đóng khung theo art từng tier; bỏ đặc cách scrap = master.
- `build-mechanic-cannon-tiers.py`: `fit` co đều, tâm vào tâm mẫu, canvas nở đối xứng khi dày hơn (pivot tâm nên không lệch); ảnh diff trong `check` đệm mẫu; icon đóng khung theo art từng tier.

## Đã xem bằng mắt
- 20 ảnh tải về (thumbnail 1/3): đúng thiết kế từng hàng sheet, đúng bố cục.
- Ảnh ghép `sheet`: súng 10 tier x 2 bản trên hero (idle phải, attack2 trái, attack3 phải, idle xuống), icon 10 súng; pháo 10 tier trên hero 4 hướng + attack4 trái/phải, icon 10 pháo. Súng nằm trong tay, nòng ra ngoài; pháo đúng chiều trên lưng.
- Chưa xem trong game.

## File đổi (chưa commit)
- Art gốc: `dual-src/tiers/*-pair-chatgpt.png` (10), `dual-src/tiers/cannon/*-chatgpt.png` (9), `grips.json`; bản cũ ở `old-20261103-v1/` (2 thư mục mới), `cannon/rejected/brass-master-tinted-20261103.png`.
- Game: `weapons/pistol/<tier>-base|enhanced.png` (20), `weapons/cannon/<tier>/{front,back,side}.png` (27), `Sprites/icons/weapon-mechanic-0..9`, `offhand-mechanic-1..9`, `Data/classes/mechanic/held-mechanic-dual.json` (mục `weapon`).
- Tool: 2 file trên. Docs: `docs/pending-tasks.md` (mục Mechanic 7), `docs/sprite-rules.md` (6 dòng Sổ lỗi).

## Chưa giải quyết
- Điểm bắn đạn `tip` trong `build-mechanic-dual.py` vẫn theo khẩu mẫu; súng mới dẹt hơn nên đạn có thể lệch vài đơn vị, chỉ thấy khi chơi.
- Thư mục `old-20261103-v1/` chưa có `.meta` (Unity sẽ tự tạo khi mở).
- Sidebar ChatGPT hiện "Tay" chứ không phải "Tân Korris": nếu người dùng đổi tên thì ghi lại bảng tài khoản trong `pending-tasks.md`.

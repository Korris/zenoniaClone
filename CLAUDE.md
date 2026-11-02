# zenoniaClone

RPG 2D kiểu Zenonia: client Unity 6000.3.9f1 (branch `unity`), online qua zone server C# và `server-api` (node).

Trả lời người dùng bằng tiếng Việt.

## Sprite và VFX

**Trước mọi việc về sprite nhân vật, vũ khí hay hiệu ứng, kể cả khi giao cho agent: đọc hết `docs/sprite-weapon-rules.md` rồi mới làm.** Khi giao agent, ghi rõ yêu cầu này trong brief.

- Mỗi lỗi sprite hoặc VFX mới phải ghi ngay vào mục "Sổ lỗi" của file đó, kèm nguyên nhân và cách sửa.
- Những lỗi đã gây mất thời gian nhất:
  - Vũ khí đè lên tay, sai hướng, đổi tay khi quay trái, đổi cỡ giữa lúc cầm và lúc đánh.
  - Art ChatGPT bị vẽ cứng ô caro hoặc quá trong suốt nên trông nhạt.
  - Thời gian trong `*Fx.cs` không khớp `SkillTable`.
- Sửa xong phải tự xem ảnh: ghép đúng cách game vẽ (sheet `body/bare/` + overlay), xem từng frame. Báo cáo "xong" của agent hay chỉ số không đủ. Chỉ nói đã sửa khi đã thấy, và nói rõ phần chưa xem trong game.

## Asset để đúng chỗ, không thả lung tung

Asset của class nào nằm trong folder class đó (warrior, wizard, mechanic, assassin, cleric):

```
Assets/Resources/Sprites/classes/<class>/
  body/        armed, bare, hand (sheet nhân vật)
  weapons/     sword, shield, staff, gun, dagger
  companion/   art, float (robot, spirit)
  fx/          <tên-skill>, combat, attack (hình hiệu ứng)
  skills/      <skill>/ (frame và icon của skill)
  skill-icons/ <index>.png
Assets/Resources/Data/classes/<class>/   held-*.json, vfx-attack*.json, skills/<skill>.json
Assets/ArtLibrary/classes/<class>/       ảnh gốc: anim, frames, overlays, skill-vfx, vfx, skill-icons-src
```

- Dùng chung mới để ngoài `classes/`: `Sprites/{monsters,village,tiles,ui,icons,fx}`, `Data/dev-start.json`.
- Âm thanh: `Assets/Resources/Audio/sfx/<class>`, `Audio/sfx/skills/<class>`.
- Ảnh tham khảo: `docs/reference/`, đặt tên có nghĩa. Không để ảnh ở thư mục gốc repo.
- `Resources.Load` dùng đường dẫn chuỗi: chuyển file phải sửa cả code và `Tools/`, và chuyển kèm `.meta` bằng `git mv`.

## Art trên ChatGPT

Dùng tài khoản Tay (Plus), viết tiếp trong các chat skill ở Recents. Không dùng tài khoản "xaochai" hay "Tieu Duy Phuong".
Khi Tay hết lượt vẽ thì dùng tài khoản Tan Pham (Plus) (người dùng cho phép ngày 2026-11-01), mở chat mới và kiểm tra đúng tên tài khoản trước khi gửi.

Vẽ sprite mới cho class nào cũng tải kèm hình mẫu `docs/reference/hero-idle-down-5-classes.png`.

## Unity

Người dùng thường mở Unity trên chính project này:
- Không build hay chạy batch vào project đang mở; build từ bản sao (`Tools/build-from-copy.ps1`).
- Unity có thể biên dịch giữa hai lần lưu file nên báo lỗi thoáng qua: đọc lần biên dịch mới nhất trong `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
- Người dùng có thể tự đổi file trong lúc test, như `dev-start.json`; đừng tự đổi lại khi chưa hỏi.

## Kiểm tra

```
dotnet test Tests/dotnet/ZenoniaSkills.Tests
dotnet test Tests/dotnet/ZenoniaNet.Tests
```

Các test này không chạy phần hình ảnh. Bảng skill dùng chung với server: đổi số liệu skill thì phải build lại server.

## Trước khi phát hành

`Assets/Resources/Data/dev-start.json` đang bật cheat để test (`noCooldowns`, `noMpCost`, level, vàng). Phím F10 (`DevBuffKey`) là phím test tạm.

## Tiếp tục việc cũ

Người dùng hay đổi máy: lịch sử, việc dở và nội dung đang bàn phải ghi vào file trong git (`docs/pending-tasks.md`), không ghi vào memory.

`docs/pending-tasks.md` ghi việc đang dở và chat ChatGPT của từng việc: đọc trước. `docs/session-handoff.md` ghi việc dở, nhưng có thể đã cũ: đối chiếu với `git log` trước khi làm theo.

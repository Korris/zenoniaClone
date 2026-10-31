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
- Sửa xong phải tự xem ảnh: ghép đúng cách game vẽ (sheet `bare/` + overlay), xem từng frame. Báo cáo "xong" của agent hay chỉ số không đủ. Chỉ nói đã sửa khi đã thấy, và nói rõ phần chưa xem trong game.

## Art trên ChatGPT

Dùng tài khoản Tay (Plus), viết tiếp trong các chat skill ở Recents. Không dùng tài khoản "xaochai" hay "Tieu Duy Phuong".

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

`docs/session-handoff.md` ghi việc dở, nhưng có thể đã cũ: đối chiếu với `git log` trước khi làm theo.

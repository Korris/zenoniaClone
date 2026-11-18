# HUD 3D + 4 skill Warrior cho prototype 3D (18/11/2026)

Status: DONE_WITH_CONCERNS

## Đã làm
- HUD V2 theo ChatGPT (phong cách RO3 / Midgard Senki, khung bạc): góc trên trái chân dung + Lv + HP/MP; giữa đáy 4 ô skill có phím, giá MP, quét hồi chiêu + số giây, ô thiếu MP (xám + số đỏ), loé khi nhấn / khi hồi xong.
- 4 skill (phím 1-4), số liệu + màu + timeline theo ChatGPT: Power Slash (xanh băng, 18 MP, 4 s), Whirlwind (xanh lá, 28 MP, 8 s), Charging Strike (tím, 40 MP, 10 s), Inferno Crash (cam lửa, 80 MP, 18 s).
- HP giảm khi slime đánh (qua Hud3D.Pop hurt), HP hồi sau 4 s không bị đánh, về 0 thì hồi đầy sau 2 s; MP hồi 4/s.
- Cờ chụp: `--skills 5:1,7:3` (giây unscaled : phím), `--mp 60`, `--mpregen 0`.

## File
Mới: Scripts/SkillBar3D.cs, CooldownSweep.cs, SkillFx3D.cs, ArcRibbon3D.cs, WarriorSkills3D.cs, WarriorSkillMoves3D.cs, WarriorSkillDefs3D.cs, HeroStats3D.cs; Editor/ProtoSkillLook.cs.
Sửa: HeroController3D.cs (hook Casting/SkillStarted/SkillPose/Anim/Defeated), Hud3D.cs (Pop hurt -> HP; ô tên map ra giữa trên; hộp hướng dẫn hẹp 6 dòng, thêm "1 2 3 4: skill"), ProtoBuilder.cs (2 dòng), setup-proto3d.ps1 (thư mục Resources/ProtoHud + chép parts).
Art: docs/reference/3d-warrior-prototype/hud-3d-chatgpt/ (mockup v1 -rejected, mockup v2, parts sheet, icon sheet, spec text, cut-hud3d-parts.py, parts/).

## Kiểm tra
- Build riêng C:\temp\hud\proto3d-hud -> C:\temp\hud\proto3d-build (không đụng C:\temp\proto3d-build).
- Ảnh: docs/reference/3d-warrior-prototype/screens/v7-*.jpg; frame gốc C:\temp\proto3d-hud-shots\{a,b,c,d,e}_*.png.
- FPS 125-144 (1600x900).
- Chưa thấy trong ảnh: loé nhấn 0,12 s, loé cyan hồi xong, chữ HP về 0 + hồi sinh, phím bấm tay (chỉ test bằng --skills).

## Câu hỏi
- Hiệu ứng skill vẽ bằng code theo mô tả ChatGPT (chưa có sprite/flipbook ChatGPT riêng). Có cần xin ChatGPT bảng frame hiệu ứng?
- Chân dung ChatGPT là mặt anime tóc đỏ (không phải mẫu Tripo 3D).

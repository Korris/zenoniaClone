# Số sát thương vẽ lại theo ChatGPT — 15/11/2026

ChatGPT (tài khoản Tay): https://chatgpt.com/c/6af98129-e4e8-83ec-88ef-2c765b7f4edf. Spec V1 + V3: `docs/reference/damage-numbers-chatgpt-spec.md`.

## Vòng 1 (V1)
Glyph "Fantasy Arcade Chibi" (sheet A trắng/xám tô 8 kiểu màu, sheet B crit màu sẵn), bảng màu + chuyển động từng loại.

## Vòng 2 (V3 "Comic Impact", người dùng: "chưa đủ wow")
- Gửi ảnh đánh thật V1 + ảnh so trước/sau. Tin đầu có gợi ý của mình (ra spec V2); theo yêu cầu người dùng đã bảo ChatGPT bỏ qua gợi ý, tự đề xuất -> V3 + mockup + sheet FX.
- ChatGPT chốt: số là một phần hiệu ứng trúng đòn; crit 46 px bật 0,5→1,65→1,0, rung, giữ đọc tới 310 ms, nền nổ comic + banner CRITICAL đỏ + hạt; finisher 32 px có vệt chém + vòng; thường 24 px gọn; người khác trắng xanh viền navy; chống chồng bằng khung bao màn hình (giữa → trái → phải → tầng trên), crit ưu tiên, EXP cạnh hero, hiện trễ. Giữ glyph A/B.
- Làm khác spec: burst to hơn (170 px / 1,9x bề ngang số, 125 px bị số che); FX 1 hình/món, chuyển động bằng scale/xoay; không gộp hit cùng nguồn; không đổi hit-stop (game đã có 45/80 ms).

## File
- Code: `Assets/Scripts/DamageNumber.cs`, `DamageNumber.Layout.cs` (mới), `DamageNumber.Extras.cs`, `DamageNumberKind.cs`, `DamageNumberFont.cs`, `DebugCapture.DamageNumbers.cs`, `DebugCapture.cs` (1 dòng), `FeelConfig.cs` (bỏ hằng Pop*). Mọi file < 200 dòng, API `Spawn` không đổi.
- Tool: `Tools/build-damage-number-font.py`, `Tools/build-damage-number-fx.py`.
- Art: `Assets/ArtLibrary/ui/damage-font/` (sheet ChatGPT, mockup, ảnh gửi `ref/`), `Assets/Resources/Sprites/ui/damage-font/`, `Assets/Resources/Data/ui/damage-font-glyphs.json`.
- Docs: `docs/sprite-rules.md` (Sổ lỗi +2), `docs/pending-tasks.md`, `docs/reference/damage-numbers-redesign-ingame.png` (trước / V1 / V2).

## Kiểm tra
- `ZenoniaSkills.Tests` 322/322, `ZenoniaNet.Tests` 404/404.
- Exe `C:/temp/player-dmg-numbers-v2/Zenonia.exe`. Đã xem trong game: thường, kết combo (vòng + vệt chém), crit thật 64!/65!/98! (burst + banner, crit sau né crit trước), hero bị đánh (đỏ), heal/thuốc/EXP/người khác qua demo.

## Chưa xem / còn lại
- Heal Cleric thật, online, màn khác 720p, "Shadow Step!".
- Nhiều quái dồn: số thường đôi khi vẫn chạm nhau; số bị crit đẩy ra nhảy ngang tức thì.
- File mới chưa có `.meta`.

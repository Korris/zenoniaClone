# RO Soft hero 8 hướng — báo cáo (19/11/2026)

Branch `worktree-agent-a5678ba0bd607db95` (đã merge `unity` bb3f09ad: attack.png Warrior). Exe test: `D:\temp\player-ro-hero\Zenonia.exe`.

## Đã đổi
- `Assets/Scripts/HeroRenderer.Soft.cs` (mới): `InitSoft` đọc `Data/classes/<class>/soft-sheet.json` + `body/soft/idle|walk[|attack].png`; bật khi đủ idle + walk + json và style bật. PPU = bodyPx/20,5, pivot (0,5; (cellH-feetY)/cellH). `ShowSoft`: một SpriteRenderer thân, ẩn parts/head/hand; giữ bóng, lean, lift, FigureScale, dash trail (copy sprite + lossyScale), order thân = base+3, tint đóng băng.
  - Map anim: idle/hurt/death -> idle; walk/dash -> walk; mọi anim khác (attackN, melee*, combo, cast, skill pose, spin) -> attack theo progress. Thiếu attack.png -> walk frame 2 (FrameCount = 1).
  - Idle thở bằng code: scale Y thân ±0,75 % (1,5 % đỉnh-đỉnh), chu kỳ 1,6 s, lệch pha ngẫu nhiên mỗi hero.
  - Hurt: idle + tint đỏ nhạt (trước đây không có tint hurt). Death: idle mờ dần qua 8 frame ảo (~0,96 s), bóng mờ theo.
  - `FrameCount`/`HasAnim` trả số frame soft (mọi tên anim), nên timing ByProgress/walk cycle giữ đúng.
- `Assets/Scripts/HeroRenderer.cs`: 4 dòng hook (InitSoft cuối Init, nhánh FrameCount, nhánh Show, góc lean cho hướng chéo x0,7).
- `Assets/Scripts/Dir8Util.cs` (mới): thứ tự hàng sheet, vector->8 hướng, khớp diagonal với facing 4 hướng.
- `Assets/Scripts/HeroController.Dir8.cs` (mới) + 1 dòng trong `HeroController.Animation.cs`: `dir8` chỉ để vẽ.
  - Hero mình / AI: từ `ReadMove()` khi đi, từ `dashDir` khi lướt. Proxy: từ chuyển động nội suy khi Moving/Dash (gom ≥1,5 unit rồi mới đọc hướng để lọc rung snapshot). Đứng yên giữ hướng cũ; facing đổi lúc đứng yên -> về hướng chính; bất cứ lúc nào dir8 không khớp facing (một trong hai trục của hướng chéo) -> về hướng chính. Đánh: giữ hướng chéo nếu một trục của nó là facing đánh.
  - Sim, hitbox, RangedAimRules, protocol, server: không đổi.
- `Assets/Scripts/DebugCapture.Face8.cs` (mới) + 1 dòng hook: `--face8 <hướng>`, `--bot walk8 <hướng>`.
- `Assets/Editor/FrostSmokeTest.cs`: các check paper doll chạy với classic (ép `SoftStyleEnabled=false`), thêm `CheckSoftStyle` (mọi anim x 4 facing x 9 giá trị Dir8 đều có sprite). Chưa chạy smoke test này (cần Unity batch trên bản sao).
- `docs/sprite-rules.md`: thêm dòng RO Soft vào bảng "Cách hero được vẽ".

## Bật/tắt
- Mặc định: soft cho class có sheet (hiện chỉ Warrior). Class khác tự bật khi có `body/soft/idle.png`, `walk.png` + `soft-sheet.json`; `attack.png` tự nhận khi có.
- Bản cũ để so: thêm `--hero-style classic` vào lệnh chạy exe.

## Đã xem trong game (exe trên, Frost Hollow, zoom 30, 960x640)
Ảnh: `docs/reference/ro-soft-hero-ingame/`, contact sheet `warrior-ro-soft-8dir-ingame-contact-sheet.png`.
- Đứng 8 hướng (`warrior-soft-stand-<hướng>.png`): đúng hàng từng hướng, chéo xuống-phải/trái thấy mặt 3/4, chéo lên thấy lưng 3/4.
- Đi 8 hướng (`warrior-soft-walk-<hướng>-6-shots.png`, 6 ảnh cách 0,1 s): chân đổi frame, hướng chéo khớp phím giữ. Walk left: sau 1,2 s hero đi qua cổng tây nên màn hình mờ dần (chuyển map), 3 ảnh đầu rõ.
- Đánh (`warrior-soft-attack-down-right-6-shots.png`, `-left-`): dùng frame attack.png thật (giơ kiếm, chém), hướng chéo xuống-phải giữ khi đánh.
- So cỡ (`warrior-classic-vs-soft-stand-down-right.png`): chiều cao đỉnh tóc-đế gần như bằng nhau (~248 vs ~245 px trên ảnh chụp). Chân bản soft thấp hơn bản cũ ~0,7 unit vì sheet cũ chân ở hàng 186/192 (lơ lửng trên gốc), soft chân đúng ở gốc/bóng.
- Túi đồ (`warrior-soft-bag-portrait.png`): hiện idle xuống soft, không lỗi. Màn chọn nhân vật dùng video/art vẽ sẵn, không bị ảnh hưởng; chạy `--hero-select-test` không có exception trong Player.log.
- Chưa xem: online (proxy 2 client), dash, hurt, death, skill pose, Blade Cyclone, freeze tint trên soft.

## Test/build
- `dotnet test ZenoniaSkills.Tests`: 361/361 pass. `ZenoniaNet.Tests`: 422/422 pass (SDK 10).
- Build từ bản sao `D:\temp\zu-copy-hero`: Succeeded, 0 errors.

## Hạn chế đã biết
- Vũ khí/khiên/giáp vẽ liền: đổi đồ, tier, cường hoá (glow HeldEnhanceFx) không hiện trên thân soft; kiểu tóc/nhuộm không đổi được trên path này.
- Chém chéo nhưng hitbox vẫn hướng chính (đúng plan v1).
- Bảng nòng/đầu gậy (HeroFx, RangedAimRules) đo theo art cũ: lệch khi class bắn xa có soft.
- ShadowCloneFx (Assassin) vẫn đọc sheet cũ: khi Assassin có soft, bóng sẽ khác hình.
- Hurt tint và death fade là tạm, chưa có anim vẽ.
- FrostSmokeTest chưa chạy (Unity batch).

## Câu hỏi chưa rõ
- Tint đỏ khi bị đánh trên path soft có giữ không, hay muốn hiệu ứng khác?
- Chân soft đặt đúng gốc (thấp hơn bản cũ ~0,7 unit): giữ như vậy hay nâng cho giống bản cũ?
- Có cần hiện đồ đang mặc (ít nhất glow cường hoá) trên path soft không?

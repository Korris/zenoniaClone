# Camera đám đông đánh boss: zoom xa + vòng bot + hướng nhìn 8 hướng (19/11/2026)

Branch `ro-fantasy-heroes`. Mục tiêu: `docs/reference/ro-soft-fantasy-5-class-redesign/40-players-new-classes-vs-mire-hydra-queen.png`.
Trước: `docs/reference/mire-hydra-queen-v2/crowd-39-god-bots-*.png`. Sau: `crowd-cam-*.png`, so cạnh nhau `crowd-cam-vs-target.png`.
ChatGPT so sánh camera (Tay): https://chatgpt.com/c/6aff02e1-5da4-83ec-a58c-dd81801ab93c

## Đã đổi

1. **Camera rộng 1,35x ở mọi map** (người dùng: zoom xa luôn; hệ số theo ChatGPT cho 20-40 người).
   - Một hằng: `GameConfig.WorldZoomOut = 1.35` → `ViewHeightUnits` 160 → 216.
   - `CameraFollow.FitToMap`: view không bao giờ rộng hơn map, kể cả khi cửa sổ hẹp hoặc map nhỏ (Thornheart 24x16), nên không lộ khoảng đen ngoài map.
   - Gần boss truy nã còn sống (dưới 140 đơn vị, mờ dần tới 260): camera lệch nửa đường về giữa thân boss, để thấy cả boss lẫn vòng người. Hero luôn nằm trong 60 % giữa màn.
   - Lăn chuột vẫn chỉ phóng gần, tối đa 0,6x. PlayerPrefs đổi sang khoá `zenonia.zoom.v2`, để mức phóng gần đã lưu từ trước không che mất view mới.
   - `dev-start.json cameraViewHeight` nay cho phép tới 320.
2. **Hướng nhìn 8 hướng khi đánh**, chỉ phần hình (`HeroController.Dir8Target.cs`).
   - Hero đang đánh hoặc niệm tại chỗ quay Dir8 về giữa hit box của quái gần nhất, nếu hướng đó khớp hướng 4 của sim và quái trong 150 đơn vị.
   - Áp dụng cho hero của mình, hero AI và hero người khác (proxy đọc pose từ snapshot). Đánh xong vẫn giữ hướng chéo đó tới khi đổi hướng.
   - Sim, hitbox và dữ liệu mạng vẫn 4 hướng.
3. **Thứ tự vẽ và tên.** Hero vốn đã y-sort đúng: phía nam boss vẽ trước, phía bắc vẽ sau. Mới thêm: hero khuất sau thân boss truy nã (bắc chân boss, đầu nằm trong sprite boss) không còn tên, để tên không chồng lên boss (`Monster.HidesHeroAt`, `NameTags`).
4. **Bot đứng vòng** (`Tests/dotnet/ZenoniaSwarm/Program.Formation.cs`, `--formation`, mặc định bật trong `Tools/wanted-boss-crowd-playtest.ps1`).
   - Ba vòng elip quanh hit box boss: melee (Warrior, Assassin) cách 24 đơn vị; Wizard, Cleric cách 48; Mechanic cách 66.
   - Bot chia đều 360° trong vòng của mình. Ô vướng cột thì trượt dọc vòng.
   - Bị đẩy ra quá 14 đơn vị thì bot đi về chỗ. Đứng đúng chỗ thì quay về boss bằng một lần bấm hướng kèm đánh, nên không bước đi.
   - Script nhận thêm `-OutDir`, các cổng riêng và `-Formation`, chạy được song song với một trận khác.
5. **Zone server có đổi:** `InterestSet` SeeRadius/DropRadius 2/3 → 3/4 ô (64 đơn vị một ô). Lý do: view 384x216 ở 16:9 thì bán bề ngang là 192 đơn vị, giữ 2/3 thì người và quái hiện ra ngay trong mép màn. Phải build lại zone server. Snapshot tối đa khoảng 2x số ô.

## Kiểm tra

- `dotnet test` ZenoniaNet 455/455, ZenoniaSkills 442/442. Đã sửa test hysteresis interest theo 3/4.
- Build `D:/temp/player-hydra-crowdcam/Zenonia.exe` dựng từ `D:/temp/zu-copy-hydra`. Ổ D còn khoảng 200 GB.
- Trận 39 bot (`-GodHero`, cổng 7801/3201/27201, `C:/temp/wanted-crowdcam`):
  - 39/39 bot ở gần boss suốt trận.
  - Máu boss giảm khoảng 1 % mỗi 30 s, bằng trận cũ, nên bot đứng vòng vẫn đánh trúng.
- Ảnh đã tự xem:
  - `crowd-cam-ring-start.png`: thấy cả boss, tường sân, vòng người.
  - `crowd-cam-ring-2min.png`.
  - `crowd-cam-ring-2min-facing-zoom.png`: hero bên phải nhìn trái hoặc chéo trái, hero phía nam quay lưng nhìn lên, hero bên trái nhìn phải.
  - `crowd-cam-vs-target.png`.
  - `crowd-cam-town-oakhaven.png`, `crowd-cam-farm-willowbrook.png` (đang đánh, số sát thương và tên vẫn đọc được).
  - `crowd-cam-small-map-thornheart.png`: map vừa khung, không lộ khoảng đen.
- Cửa sổ trận mới để chạy cho người dùng (pid trong `C:/temp/wanted-crowdcam/pids.txt`, dừng: `-Stop -OutDir C:\temp\wanted-crowdcam -MongoPort 27201`).

## Chưa làm / chưa xem

- Vòng ngoài chỉ cách khoảng 4-5 ô, không phải 5-8 ô như ảnh mục tiêu: tầm đạn đánh thường là 60 (Wizard, Cleric) và 82 (Mechanic). Đứng xa hơn sẽ không trúng.
- Chưa làm 2 gợi ý của ChatGPT: FX của người khác mờ 40-60 %, và dấu cho hero bị boss che.
- Chưa xem: cửa sổ 21:9 (có thể vẫn hiện ra ở mép), bản web/mobile, hướng nhìn khi đánh quái thường online có người thật, và HUD ở cửa sổ nhỏ hơn 1280.
- Trận của người dùng ở `D:/temp/player-hydra-god2` (pid trong `C:/temp/wanted-40/pids.txt`) đã tắt khi tôi kiểm tra sau lần build thứ 2. Lệnh `-Stop` của tôi chỉ đọc pid file và cổng mongo của tôi (27201), nên không rõ trận đó tắt vì đâu.

## Câu hỏi

- Zoom 1,35x ở mọi nơi hay chỉ khi đánh boss? ChatGPT khuyên chỉ khi đánh boss; hằng `WorldZoomOut` đổi một chỗ là xong.
- Có tăng tầm đánh thường / cho bot dùng skill để vòng ngoài rộng như ảnh mục tiêu không?

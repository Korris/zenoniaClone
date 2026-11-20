# Bản thử 2.5D sân Hydra (Sunken Reliquary) - 20/11

Cờ `--view25d` (chỉ bản test, chỉ map `sunkenReliquary`). Mức 1: giữ map 2D + sprite; camera nghiêng 38°, FOV 22°, có phối cảnh; nhân vật, quái, pet, đồ rơi, prop cao dựng đứng (billboard) quanh chân; bóng nắng đổ về đông nam; sương xa + đốm sáng bay. Chỉ phần hình, server không đổi.

## Đã làm
- Merge `wip/proto-25d` (98a2106a) vào `ro-fantasy-heroes`, không conflict; vẫn giữ thay đổi crit 55b802dc (`DamageNumber*.cs` chỉ đổi điểm neo sang `View25D.Label`).
- Sửa lỗi mất tóc ở 2.5D (6684d14a): camera phối cảnh xếp sprite theo khoảng cách nên đầu hói che mất tóc -> `transparencySortMode = Orthographic`. Đã ghi vào Sổ lỗi `docs/sprite-rules.md`.
- Build `C:/temp/player-25d/Zenonia.exe` (build từ bản sao, 0 lỗi).

## Đã xem (ảnh trong game)
- Một mình gần Mirefang Hydra: `docs/reference/proto-25d/hydra-arena-solo-2d-vs-25d-hair-fixed.jpg`. Tóc đúng; cột đá dựng đứng có bóng; nền co lại theo góc nghiêng.
- Trận 39 bot + 1 cửa sổ đánh Mire Hydra Queen (`Tools/wanted-boss-crowd-playtest.ps1 -Boss mireHydraQueen -GodHero -ExtraArgs --fps-log,--view25d`): `docs/reference/proto-25d/hydra-queen-crowd-39-bots-2d-vs-25d.jpg`; ảnh thô ở `C:/temp/cap-25d/crowd/`. Queen, nhân vật, tên, thanh máu, vòng cảnh báo vẫn đọc được; ở 2.5D thấy được tường xa của sân.

## FPS (máy này, cửa sổ 1280x853, tắt vsync, 40 hero, lấy trung bình mỗi 5 s, bỏ 10 s đầu)

| Lần | 2D | 2.5D | Chênh |
|---|---|---|---|
| 1 | ~3,3 ms (~300 fps) | ~4,5 ms (~220 fps) | +1,2 ms |
| 2 | ~2,5 ms (~400 fps) | ~4,1 ms (~245 fps) | +1,6 ms |

- Khung chậm nhất lúc chạy ổn định: 2D ≤ 15 ms, 2.5D ≤ 18 ms. Gai 115-165 ms có ở cả hai chế độ.
- Lưu ý: màn hình Windows bị khoá trong lúc đo, nên chỉ nên so tương đối giữa hai chế độ. Chưa đo trên điện thoại / WebGL.
- Phần tốn thêm nhiều khả năng là bóng nắng: `View25DDropShadow` chép mọi sprite của từng nhân vật mỗi khung hình. Nếu cần tiết kiệm: giảm cập nhật xuống 2-3 khung một lần, hoặc chỉ bật bóng cho hero mình + boss.

## Lỗi thấy được nhưng không do 2.5D
- Khi đông người, số sát thương xếp ngang dính thành một dãy ("160157308"). Lỗi này có cả ở 2D.

## Ước tính làm cho 14 map (thô)
- Phần chung, khoảng 1-2 ngày: bỏ giới hạn `Active` chỉ cho Sunken (thay bằng danh sách map), màu sương theo biome, công tắc trong Settings, đo FPS điện thoại / WebGL, giảm tải bóng đổ.
- Map phẳng (đồng cỏ, làng, sa mạc rộng), khoảng 2-4 giờ mỗi map: rà prop nào dựng đứng, prop nào nằm (luật `MapScenery` hiện dựa vào `GroundOrder + 4`), rà mép camera, chụp so sánh.
- Map nhiều vách / bậc đá vẽ sẵn trong ảnh nền (Frost Hollow, Frostfang Den, Scorpion Gorge, Thornheart...): vách vẽ phẳng sẽ bị nghiêng, trông dẹt. Muốn đẹp thì phải tách vách thành sprite đứng (vẽ thêm bằng ChatGPT), mỗi map 1 ngày trở lên. Đây là phần tốn nhất, chưa thử.

## Câu hỏi chưa giải quyết
- Giữ góc 38° / FOV 22° hay thử góc khác (đã có ảnh 30/38/45 ở `camera-angles-tilt30-38-45-fov30.jpg`)?
- Có làm tiếp cho map khác không? Nếu có thì map nào trước?
- Bóng nắng cho cả 40 người hay chỉ hero mình + boss (tiết kiệm FPS)?

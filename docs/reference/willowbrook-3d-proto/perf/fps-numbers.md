# FPS map 3D Willowbrook: trước / sau (22/11/2026)

Máy: AMD Radeon(TM) Graphics tích hợp (D3D11), 1280x720 cửa sổ, `--fps-log` (tắt vsync), warrior, 70 quái, 1 hero. Exe `D:\temp\player-map3d-fast\Zenonia.exe`.

**LƯU Ý:** suốt buổi đo luôn có một cửa sổ game khác mở trên cùng GPU (`player-map3d` / `player-eb3d --map3d` của người dùng / agent Earth Break), nên số tuyệt đối chỉ khoảng một nửa số máy rảnh (cùng điều kiện: 2.5D đo 84-117 FPS thay vì ~200; bản 3D cũ 27,7 thay vì 58). So sánh giữa các mức là đúng; muốn số tuyệt đối thì đo lại khi chỉ mở 1 game.

## Trước (bản cũ, mọi thứ như High)

| Đo | FPS |
|---|---|
| Agent trước, máy rảnh: 3D đủ / không bóng / không lá / không rừng viền | 58 / 156 / 116 / 81 |
| Hôm nay, có game khác mở: 3D cổng tây (3,23) | 27,7 |
| Hôm nay, có game khác mở: 2.5D cùng chỗ | 84,5 |
| Cùng chỗ ở 640x360 (3D) | 81 (gấp 3: nghẽn ở tô pixel, không phải CPU / draw call) |

## Sau (bản mới, có game khác mở; trung vị 2 lần chạy xen kẽ, số trong ngoặc = lần ít bị tranh GPU hơn)

| Chỗ (tile, yaw) | High | Medium (mặc định) | Low |
|---|---|---|---|
| Cổng tây (3,23), 0 | 44 | 67 (lần 2: 95) | 102 |
| Cầu (27,21), 0 | 42 | 80 (91) | 107 |
| Ao (38,36), 300 | 36 | 67 (81) | 97 |
| Đông (60,12), 90 | 42 | 79 (94) | 111 |
| 2.5D cổng tây | 99 (MSAA 2x) | 118 (157) (MSAA tắt) | |

Ước lượng máy rảnh (x ~2 như bản cũ 27,7 -> 58): High ~80, Medium ~130-180, Low ~200.

## Phân tích (A/B cùng chỗ, có game khác mở)

- Bóng realtime: Medium thử 1 cascade + chỉ prop lớn đổ bóng vẫn chỉ 33 so với 56 không bóng: chi phí nằm ở depth pre-pass + screen-space collect + caster lá / thân. Không chỉnh thông số nào đạt mục tiêu -> Medium chuyển sang bóng nướng 1 lần (`Map3DBakedShadow`): 4 mẫu / pixel, gần như không tốn (Medium có bóng nướng = Medium tắt bóng, trong sai số).
- Lá: cỡ 6 ms / khung khi không bóng. Depth pre-pass lá (`LEAFDEPTH` + ZTest Equal): +7 % (34,5 so với 32). Lá không đổ bóng bỏ pass ShadowCaster: không đổi đo được.
- Cỏ: tắt hết cỏ không đổi FPS (cỏ rẻ); vẫn giảm theo mức cho máy yếu.
- Độ phân giải bóng nướng 2048 so với 4096: như nhau -> giữ 4096 (sắc hơn).
- Hình học (log `Map3D batch`): lá không đổ bóng 437 renderer / 481k đỉnh, lá đổ bóng 67 / 82k, thân / đá / gỗ 165 / ~104k.

## 40 người

`Tools/wanted-boss-crowd-playtest.ps1` chỉ chạy trên map boss 2.5D (Frost Hollow / Sunken Reliquary), không có map 3D; bot `Tests/dotnet/ZenoniaSwarm` là net10, máy này chỉ có SDK 9 nên không chạy được. CHƯA có số 40 người trên map 3D.

## Ảnh

- `bridge-yaw0-{high,medium,low}.jpg`, `west-gate-yaw0-{high,medium,low}.jpg`: cùng chỗ, `--no-hud`. Bản trước (High cũ) cùng chỗ cầu: `../props-3d/bridge-yaw0-pitch52.jpg`.
- `bridge-crop-high-top-medium-bottom.jpg`: phóng to, trên High realtime, dưới Medium nướng (bóng đá, biển, rừng viền khớp; biển chỉ đường nhạt hơn chút).
- `settings-graphics-row.jpg`: hàng Graphics trong Settings. `live-switch-low-medium-high.jpg`: bấm nút đổi ngay trong game.

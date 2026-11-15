# Thử nghiệm Fighter xương rig (cutout rig)

Ngày 2026-11-15. Trạng thái: ĐÃ BỎ (15/11, người dùng: nhìn như con rối giấy). Code + dữ liệu rig đã gỡ khỏi game; art nguồn giữ ở `Assets/ArtLibrary/classes/fighter/rig/`. Giữ: sửa Fighter vô hình khi cast skill. Không commit.

Mục tiêu: Fighter idle + walk hướng xuống bằng mảnh rời + xương xoay theo keyframe, để người dùng so cảm giác và dung lượng với frame-by-frame (5 class khác giữ nguyên).

## Cách làm
- Art: ChatGPT (tài khoản Tay "Tân Korris") vẽ 1 parts sheet Fighter hướng xuống, nền `#00FF00`, 13 mảnh + hình ghép mẫu. Chat: https://chatgpt.com/c/6af926a3-ddc8-83ec-a9d0-b6347d8a7ab9. Nguồn `Assets/ArtLibrary/classes/fighter/rig/`.
- Tool `Tools/build-fighter-rig-parts.py`: tách nền, cắt mảnh, ghi PNG mảnh vào `Resources/Sprites/classes/fighter/rig/` (một atlas), pivot + điểm khớp vào `Resources/Data/classes/fighter/rig/skeleton.json`; ảnh kiểm tư thế + từng frame.
- Keyframe: `Resources/Data/classes/fighter/rig/{idle,walk}-down.json` (góc + dời từng xương, thời gian chuẩn hoá 0..1), nội suy mượt (cosine), lặp.
- Code runtime, không package: `HeroRig.cs` (dựng cây Transform + SpriteRenderer), `HeroRigAnimator.cs` (đọc keyframe, nội suy), `HeroRigData.cs` (JSON). `HeroRenderer.Show` gọi rig khi class là Fighter và có clip cho anim/hướng đó; còn lại đường sheet cũ.
- Kiểm: `--record-rig-compare` trong DebugCapture: Fighter rig cạnh Warrior, idle rồi walk tại chỗ, lưu frame PNG.
- Build: `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player-fighter-rig\Zenonia.exe`.

## Phase
| # | Việc | Trạng thái |
|---|---|---|
| 1 | Parts sheet ChatGPT | xong |
| 2 | Tool cắt mảnh + skeleton.json | xong |
| 3 | Code rig runtime + nối HeroRenderer | xong |
| 4 | Keyframe idle/walk/melee1 down | xong |
| 5 | Build, chụp so Warrior, đo dung lượng | xong |
| 6 | dotnet test, báo cáo, pending-tasks | xong |
| 7 | Vòng 2: walk xuống tự nhiên, jab có lực, melee2-4 + Dash Punch xuống | xong |
| 8 | Hướng phải (parts sheet nghiêng, trái = lật), idle/walk/4 đòn/Dash Punch | xong |
| 9 | Exe đánh được, copy build riêng `C:/temp/zu-copy-rig`, chụp đánh quái thật | xong |
| 10 | Vòng 3: hướng ngang to vóc (parts sheet v2 + phóng ngang) | xong |
| 11 | Người dùng bỏ rig: gỡ code / dữ liệu / tool, giữ sửa cast | xong |

Báo cáo: `plans/reports/fighter-bone-rig-prototype-261115.md`.

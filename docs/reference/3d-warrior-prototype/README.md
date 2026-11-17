# Bản thử 3D top-down: Warrior RO (18/11/2026)

Người dùng: "tui muốn vẽ sprite mới warrior và làm thành game 3d luôn 3d top down"; chọn **3D thật, thử Warrior trước**, thiết kế = Warrior trong bộ 5 class RO (`docs/reference/ro-soft-fantasy-5-class-redesign/5-class-lineup-v1-chatgpt.png`).

Máy này: không Blender, không GPU NVIDIA (torch CPU), Unity 6000.3.9f1 (+ 6000.0.47f1) → không tạo model AI tại máy; dùng dịch vụ web image-to-3D (Meshy / Tripo, có auto-rig + animation) bằng tài khoản người dùng.

- `warrior-ro-turnaround-v1-chatgpt.png`: ChatGPT (Tay, chat https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde) vẽ bảng turnaround T0: trước / nghiêng phải / sau / nghiêng trái, A-pose không vũ khí, kiếm + khiên riêng, biểu cảm, chi tiết, bảng màu, tỉ lệ ~3 đầu.
- `view-*.png` (1024², nền trắng, đã giữ khối lớn nhất): `front`, `side-right`, `back`, `side-left`, `a-pose`, `sword`, `shield`: đầu vào multi-view cho image-to-3D. `views-contact.jpg`: xem nhanh.

Lời khuyên ChatGPT khi tạo model: dùng 3 hình trước / nghiêng / sau, giữ tỉ lệ đầu và chân; A-pose tay cách thân 30-40°, bàn tay không chạm áo choàng; kiếm / khiên làm model riêng, gắn vào xương bàn tay trong Unity; áo choàng mesh riêng (bone hoặc vật lý); kiểm tra ở camera top-down 3/4 cỡ thật, ưu tiên silhouette + vũ khí; làm xong Warrior T0 (rig + idle / walk / attack trong Unity) rồi mới nhân cho 4 class còn lại.

Kế hoạch: (1) turnaround (xong) → (2) model + rig + idle / walk / attack trên Meshy / Tripo (cần tài khoản người dùng) → (3) scene thử riêng trong Unity: mặt đất = ảnh map Frost Hollow, camera nghiêng, Warrior đi WASD + đánh; build từ bản sao (`Tools/build-from-copy.ps1`), không đụng game chính.

## Tripo (18/11 01:40)

Người dùng đăng nhập Tripo Studio (studio.tripo3d.ai, 200 credit free). Tạo model từ `view-a-pose.png` (1 ảnh, multi-view cần gói trả phí): 55 credit, model đẹp, đúng thiết kế (task `de0b0736-b8b1-4ae0-abbc-816236135e19`, trong Assets của tài khoản). Còn 145 credit. Auto Rig humanoid: 20 credit (chưa bấm).
Export (GLB 4k) bị chặn: hiện bảng nâng cấp. Giá (trả năm, -60 %): Starter 52.000 đ/tháng (750 credit/tháng, ~50 model, 623.996 đ/năm); Pro 210.400 đ/tháng (3000 credit). Không tải được file nào. Chưa mua gì (người dùng tự quyết + tự thanh toán).
- 01:52 người dùng tự export được: `knight character 3d model.glb` (67 MB) → chép ra `D:/temp/3d-warrior/warrior-ro-t0-tripo-raw.glb` (ngoài git vì nặng). Bên trong: 1 mesh, KHÔNG có xương, KHÔNG có animation, ~1,01 triệu đỉnh / 1,94 triệu tam giác, texture basecolor + roughness/metal + normal, cao 0,98 m. Quá nặng cho game đông người (cần ~5-15 nghìn tam giác / nhân vật): phải Retopo / giảm lưới + Auto Rig trên Tripo rồi export lại (FBX có xương + động tác).

## Bản thử 3D trong Unity (18/11 02:00-02:30, người dùng đi ngủ: "mày làm đi nha ... tao muốn cái game sẽ giống như Ragnarok 3")

Người dùng nâng Tripo lên Pro (3145 credit, tự trả). Trên Tripo: Retopo Triangle 10 000 (40 credit) → 9 513 mặt / 13 779 đỉnh; Auto Rig Humanoid, skeleton Mixamo (20 credit); áp 8 động tác preset (idle, standing_relax, walk, run, slash, chop, hit_to_body_01, defeat; Tripo chỉ giữ danh sách động tác đã áp, mỗi lần áp thêm thì danh sách export tăng); Export FBX preset Mixamo, giữ skeleton, "Animation stay in Place" bật, 4k. File: `D:/temp/3d-warrior/set8/` (FBX 56 MB + 5 texture), bản 4 động tác `set1/`, bản GLB thô `warrior-ro-t0-tripo-raw.glb` (ngoài git vì nặng).

Project thử riêng `D:/temp/proto3d` (Unity 6000.3.9f1, Built-in RP, Input cũ; KHÔNG đụng project game). Mã nguồn chép vào repo: `Prototypes/3d-topdown-warrior/` (Scripts: `HeroController3D`, `CameraFollow3D`, `CrowdBot`, `BossBillboard`, `Hud3D`; Editor: `ProtoBuilder.Build` nhập FBX Generic + vòng lặp idle/walk/run, tạo Animator + material, nền = ảnh Sunken Reliquary 36x24 ô với 1 ô = 0,8 m (hero cao 1 m ≈ 1,25 ô như game 2D), boss Mire Hydra Queen = sprite 2D billboard cao 6 m ở ô (30,14), 20 Warrior bot vây đánh, camera nghiêng 50°, build `D:/temp/proto3d-build/Warrior3D.exe`).
Chạy lại: `Unity.exe -batchmode -projectPath D:\temp\proto3d -executeMethod ProtoBuilder.Build -quit` (chép FBX vào `Assets/Warrior/warrior.fbx`, texture `.fbm` vào `Assets/Warrior/`, ảnh nền + sheet boss vào `Assets/Env/`).
Điều khiển: WASD đi, Shift chạy, J / Space / chuột trái chém (slash / chop xen kẽ), Q / E xoay camera 45°, cuộn chuột zoom, H bị đánh, K gục.

Kết quả (02:30): build OK, chạy được: `D:/temp/proto3d-build/Warrior3D.exe`. Ảnh: `screens/v1-crowd-around-boss.png` (camera xa, 20 Warrior quanh boss), `screens/v2-hero-attacks-boss-closeup.jpg` (camera 7,5 m, hero đi tới chém). Clip từ Tripo: slash 6,58 s, chop 6,58 s (phát x2,5), defeat_02 8,46 s, hit 1,29 s, idle 15,3 s, walk 2,33 s, run 1,25 s, standing_relax 17,6 s.
Còn yếu / việc tiếp: boss là sprite 2D phóng to (mờ khi gần) → cần model 3D (Tripo từ `ui-style-ro-soft-fantasy/wanted-boss/5-wanted-boss-board-style.png`); nền là ảnh map 2D phẳng (đá, cột vẽ sẵn không có khối) → map 3D thật hoặc đặt prop 3D lên; clip chém Tripo chậm và dài, nên tạo thêm bằng "AI Animation" / Text to Motion (combo 4 nhát ngắn); cả đám đông chung 1 model (chỉ tint màu); chưa có kiếm / khiên (model riêng gắn xương tay); chưa nối server / gameplay thật.

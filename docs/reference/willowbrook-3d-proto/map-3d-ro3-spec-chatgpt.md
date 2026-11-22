# Spec map 3D kiểu RO3 cho Willowbrook (ChatGPT, 22/11/2026)

Chat (tài khoản Tay "Tân Korris"): https://chatgpt.com/c/6b029c3a-4a48-83ec-affb-a9e9bf71b449 . Gửi kèm: ảnh Willowbrook 2.5D (`../willowbrook-25d/camera-loop/round-3-hud-edges.jpg`), ảnh thử mesh 3D bị chê nhựa (`../willowbrook-25d/camera-loop/round-1-mesh-ab.jpg`), Midgard Senki (`../ro3-skill-style/midgard-senki-forest-map-reference.png`).
Dưới đây là bản tóm tắt các con số ChatGPT chốt ("Tech-art spec V1"); những chỗ code làm khác ghi ở cuối.

## Phán quyết

- Duyệt làm prototype 3D cho MỘT map, chưa duyệt chuyển cả game. Hướng: môi trường 3D vẽ tay + nhân vật 2D chibi, không low-poly toon trơn, không PBR.
- Điểm ước lượng: 2.5D hiện tại 7.6/10, mesh 3D cũ 4.3/10, mục tiêu prototype 8.0/10. Dưới 7: không nhân rộng; 7-7.9: có tiềm năng nhưng chưa đáng thay 2.5D; 8-8.5: làm một map 3D hoàn chỉnh nếu đủ hiệu năng.
- Hai điều kiện loại thẳng: hero bị che quá thường xuyên; dưới 30 FPS trên mobile đích lúc 40 người giao tranh.

## Terrain

- Mesh từ lưới, đỉnh cách 0,5 m. Đường đi, cầu, bãi quái ở đúng 0 m; đất mở 0-0,18 m; mép rừng không đi được +0,3 đến +1,15 m; đồi hậu cảnh +1,2 đến +2,2 m; đáy suối -0,45 đến -0,7 m. Bờ cao hơn 0,6 m thì dùng mesh vách riêng.
- Splat 4 lớp: cỏ chính #789647 (58-65 %), cỏ rừng #536E40 (15-20 %), đất đường #CBA16B (12-17 %), đá dốc #898676 (5-8 %). Mép đường mềm 0,35-0,55 m, nhiễu mép ±0,12-0,2 m.
- 128-256 texel/m (texture 1024 lặp mỗi 4-6 m). Chống lặp: xoay UV theo ô macro, noise màu 10-14 m ±4-6 %, decal.

## Cây (quyết định 50 % cảm giác RO3)

- Thân mesh + leaf card vẽ tay (quad alpha-cutout, mỗi card một cụm 5-12 lá) + spherical normal + diffuse mềm. Không khối đa diện trơn, không billboard cả cây.
- Sồi: cao 5,8-7,2 m, tán 4,2-5,8 m, thân gốc 0,55-0,85 m, 5-8 cành, 7-10 cụm tán, 8-12 card mỗi cụm (65-110 card), 450-850 tam giác. Card: 40 % theo mặt tán, 40 % chéo nhau, 20 % hướng về góc nhìn mặc định. Dưới tán tối hơn 12-20 %, lõi tối hơn 18-25 %.
- Spherical normal: N = normalize((v - tâm cụm) / bán kính), trộn 0,8-0,9 với normal hình học (sồi 0,85, bạch dương 0,8, thông 0,55-0,7, liễu 0,7), tính theo từng cụm.
- Ánh sáng lá: wrap = (N.L + 0,35) / 1,35, ramp mềm 3 mức 0,72 / 0,92 / 1,08; subsurface +8-14 %, rim +5-8 %, không specular; lệch màu từng cụm ±4 % hue, ±6 % độ sáng.
- Gió: thân 0-0,025 m 0,15 Hz, cành 0,035-0,075 m, lá 0,015-0,04 m 0,7-1,1 Hz, liễu 0,05-0,11 m.
- Thông 5,5-7 m, tán 2,5-3,5 m, 7-10 tầng cành bằng card lá kim; bạch dương 5,5-6,8 m, tán thưa 15-25 % hở; liễu 5-6,5 m, dải lá rủ 0,8-1,8 m.

## Đá

- Icosphere scale không đều, noise 8-14 % + 2-4 %, vài mặt phẳng lớn (không thành củ khoai), seed theo id. Texture vẽ tay; rêu theo normal hướng lên 0,55-0,85, 15-35 % diện tích; AO đỉnh 0,72-0,88. Vách cao dùng triplanar.

## Ánh sáng, bóng, fog, post

- Directional: cao 48°, từ trên-trái khung mặc định, #FFF0CF, 0,9, bóng mềm, strength 0,52, cố định theo thế giới (xoay camera bóng không đổi hướng).
- Ambient trilight: trời #DDE8D2, ngang #AEBDA2, đất #819274, ~0,65. Không cộng ambient hai lần.
- Bóng: cây / phế tích / cầu dùng shadow map; hero / quái dùng blob (rộng 0,55-0,75 m, 23-32 %); bụi, đá nhỏ dùng AO giả. Bóng tán có lỗ sáng.
- Fog linear #D1DEC5, gần như 0 trên hero, 5-18 % ở cây xa. Post: bloom 0,08-0,14, saturation +3-6, contrast +2-5, vignette 0,06-0,09, không DOF / motion blur.

## Sprite hero / quái trong cảnh 3D

- Giữ 80-85 % màu vẽ, ánh sáng cảnh 15-20 %, tối tối đa 15-25 % dưới tán. Billboard đứng thẳng, chỉ quay yaw; frame 8 hướng theo góc tương đối với camera.
- Bắt buộc làm mờ lá che hero khi xoay 360°: dither screen-space 4x4 (còn 30-45 % độ phủ khi che nhiều), chỉ các cụm lá thật sự che.

## Camera

- Yaw 360°, pitch 48-55° (mặc định 52°), FOV ~35°; khoá theo khung hình: hero 65-75 px ở 720p, nhìn trước 5-7 m, cây gần không che quá 30 % màn hình.

## Texture (16 tấm trong spec, đã vẽ 6 tấm cho prototype)

| Tấm | File nguồn (`Assets/ArtLibrary/maps/willowbrook-meadows/map3d/`) | Runtime (`Assets/Resources/Map3D/`) |
|---|---|---|
| T05 cụm lá sồi 4x4 trên #FF00FF | `map3d-leaf-oak-atlas-chatgpt-1.png` | `leaf-oak.png` (sồi, bạch dương, liễu, bụi; tint trong shader) |
| T06 cành lá kim 4x4 | `map3d-leaf-pine-atlas-chatgpt-1.png` | `leaf-pine.png` |
| T09 vỏ cây 4 dải | `map3d-bark-atlas-chatgpt-1.png` | `bark.png` |
| T10/T11 đá liền mạch | `map3d-rock-chatgpt-1.png` | `rock.png` |
| sân lát đá phế tích | `map3d-paving-chatgpt-1.png` | `ground-paving.png` |
| T01/T03 cỏ, đất | dùng lại swatch 2.5D `forest-25d/*-grass/dirt-chatgpt-1.png` | `ground-grass.png`, `ground-dirt.png` |

Chưa vẽ: atlas liễu riêng, bạch dương riêng, decal mặt đất, cầu gỗ / phế tích 3D, nước (đang làm bằng shader).

## Ngân sách hiệu năng (40 người)

- PC 1080p 60 FPS, mobile 720p 30 FPS. Tam giác thấy được: PC 112-187k, mobile 54-114k. Draw call PC 180-350.
- LOD cây: LOD0 0-14 m, LOD1 14-28 m (bớt 50-60 % card), LOD2 28-48 m (2 mặt chéo). Alpha overdraw và bóng đắt hơn số tam giác; giảm bóng / LOD lá trước khi giảm FX skill.

## Code làm khác spec (quyết định khi làm)

- Pitch 52° như spec nhưng FOV 32° (không phải 35°), hero vẫn 69 px.
- Bóng strength 0,6 (spec 0,52) vì bóng trên cỏ nhạt khó thấy; 1 cascade 4096 (bóng chiếm ~60 % thời gian khung hình với 2 cascade); rừng viền ngoài map (trừ 6 m đầu) không đổ bóng.
- Đá đều dùng triplanar (đá sinh bằng code không có UV đáng vẽ).
- Đáy suối / ao hạ theo kênh nước của nền 2.5D, không theo ô rows (rows bậc thang theo ô).

# Willowbrook Meadows thành rừng 2.5D (21/11/2026)

Người dùng: map Willowbrook làm lại thành map 2.5D giống sân Hydra Queen, có bóng cây, trông như khu rừng RO3 / ảnh Midgard (`docs/reference/ro3-skill-style/midgard-senki-forest-map-reference.png`), đất trên đường đơn giản hơn, hiện tại quá rối. Thêm: "chưa thấy nó giống 1 khu rừng", "tương lai tất cả map sẽ thành 2.5d". Đã đọc `docs/map-design-rules.md`, `plans/reports/proto-25d-hydra-261120.md`, mục 2.5D / bóng đổ trong `docs/pending-tasks.md`.

ChatGPT (Tay, "Tân Korris"): https://chatgpt.com/c/6b00f11a-1148-83ec-bd9e-0d255d3f0211 . Định hướng art đầy đủ: `docs/reference/willowbrook-25d/willowbrook-forest-art-direction-chatgpt.md`. Ảnh gốc ChatGPT: `Assets/ArtLibrary/maps/willowbrook-meadows/forest-25d/`.

## Quyết định

| Phần | Cách làm |
|---|---|
| Bố cục | Giữ rows, đường, suối, cầu, ao, cổng tây / nam, phế tích, chòi. Bãi quái giữ tâm, rect lớn thu 1 ô mỗi bên để rừng ôm sát bãi. |
| Nền | KHÔNG vẽ lại cả map: ChatGPT vẽ 2 mẫu texture (cỏ, đất) liền mép; tool ghép theo mặt nạ lấy từ nền cũ (đường = điểm ảnh màu đất, nới rộng ~2 ô, mép răng cưa bậc 8 px; suối / ao / phế tích / ruộng giữ ảnh cũ, mép mờ); nền rừng tối dần theo mặt nạ dải cây. Đường luôn đúng ô. |
| Cây | 7 loại vẽ 1 tấm (sồi x2, sồi cổ thụ, thông, cây non, bạch dương, liễu) + tấm đồ nhỏ (bụi, đá, gốc, khúc gỗ, sậy) vẽ đè PNG `wbx*` cũ. Toàn bộ cây cũ bỏ, 138 cây mới do tool đặt theo luật (xa đường / bãi / nước / cổng, hàng dày trên viền chặn, liễu cạnh nước, thông + bạch dương quanh phế tích). Cây chỉ chặn ô gốc. |
| 2.5D | Không còn chỉ là bản thử: `View25D.Maps` (danh sách id) bật 2.5D cho mọi người, cả bản phát hành; `--view25d off` để so 2D; Hydra vẫn chỉ với `--view25d`. Camera 38° / FOV 22° như Hydra. |
| Bóng | Map 2.5D dùng FakeSun (bóng mềm dưới chân + bóng cây nướng sẵn, mask trần 0,40), bỏ bóng bóng-in-hình (silhouette) của bản thử Hydra. Mask tính trên hình thang đất camera nhìn thấy. Màu bóng Willowbrook #304C3B. |
| Cây che hero | Cây cao trước hero mình mờ còn 32 % (0,18 s), hiện lại 0,3 s (`View25DPropFade`). |

## Lượng art ChatGPT: 5 ảnh

1 concept 2.5D, 1 texture cỏ, 1 texture đất, 1 tấm 7 cây, 1 tấm 12 đồ nhỏ. Không bị hết lượt.

## Tool (dùng lại cho map sau)

1. `Tools/place-map-25d-forest.py <map>`: đặt cây, ghi `<Map>ForestProps.cs` + mặt nạ rừng.
2. `Tools/build-map-25d-ground.py <map> --preview`: ghép nền từ texture + mặt nạ, ghi chunk nền (bản cũ lưu một lần `<map>-before-ground.png`).
3. `Tools/cut-magenta-prop-sheet.py <sheet> <tên...> [--scale]`: cắt tấm prop nền hồng, căn gốc cây giữa đáy, in dòng PropDef.
4. `Tools/preview-<map>-composite.py --minimap`: ảnh ghép + ảnh ô chặn + minimap.
5. Thêm id map vào `View25D.Maps`, chọn màu bóng trong `FakeSun.Tint`.

## Map tiếp theo dùng lại thế nào

- Map phẳng (đồng cỏ, sa mạc, làng): thêm cấu hình vào MAPS của 2 tool (rows, bảng prop, lane, khối spawn, rect giữ ảnh cũ), xin ChatGPT 2 texture + tấm cây / đồ theo biome (gửi kèm concept Willowbrook để cùng nét), chạy 1-5.
- Map có vách / bậc vẽ trong nền (Frost Hollow, Scorpion Gorge...): vách phẳng sẽ nghiêng, phải tách vách thành sprite đứng; chưa làm, tốn nhất (xem báo cáo Hydra).

## Trạng thái

- [x] Định hướng art + concept (ChatGPT)
- [x] Texture, cây, đồ nhỏ; cắt bằng tool
- [x] Nền mới, 138 cây, rect bãi, ô chặn rà trên ảnh tô đỏ
- [x] 2.5D theo danh sách map, FakeSun trên camera phối cảnh, mờ cây che hero
- [x] dotnet test 2 project
- [ ] Người dùng xem trong game (2.5D, bóng, độ dày rừng)
- [ ] Build lại zone server (ô chặn + rect bãi đổi)
- [ ] Còn art cũ: đá cạnh suối / vách cũ (`wbRock*`, `wbCliff*`), chòi, rào, khúc gỗ `wbLog`, cầu

## Rủi ro

- Rows / prop / rect bãi đổi: client + server phải phát hành cùng lúc.
- 2.5D trên điện thoại / WebGL chưa đo FPS.

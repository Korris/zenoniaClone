# Phase 07: Map, quái, FX skill: đối chiếu + build

Ưu tiên: P2. Trạng thái: chưa.

## Map
- Map 2.5D mới (Willowbrook, Frost Hollow, Frostfang Den, Rootburrow) đã đi theo RO3 / Midgard Senki (`docs/reference/willowbrook-25d/*ro3*`): HỢP hướng Adventure-RO3, không vẽ lại. Có thể ấm nhẹ ánh sáng toàn cảnh (FakeSun, màu sương) cho khớp UI ấm: chỉ chỉnh profile, không vẽ lại tile.
- Map 2D cũ: giữ, chờ quyết định map thường 2D / 2.5D (pending 22/11).

## Quái
Quái RO Soft (`monster-redesign-bible.md`) dễ thương, sáng: chấp nhận; nếu đứng cạnh hero Adventure bị "nhựa" thì đổi tông tool tạm (giảm bão hoà ~10 %). Không thiết kế lại đợt này.

## FX skill: GIỮ "RO trailer style" (không xung đột lớn)
- FX trong suốt vàng chanh / champagne / ngà (Warrior), lam (Wizard)... là ÁNH SÁNG, không phải vật liệu UI: style guide cấm đổi màu VFX cho khớp giấy. Giữ toàn bộ khối luật FX đầu `sprite-rules.md`.
- Điểm cần soát: vật thể skill vẽ sprite (kiếm Earth Break thép-ngà, trụ súng Mechanic) đổi tông như vũ khí hero; vòng buff quanh thân không dùng xanh glow kính.
- Xung đột nhỏ: luật RO3 skill "pastel" (Cleric giữ màu RO3 Soft Fantasy) vẫn giữ: Adventure chỉ là UI + nhân vật.

## Build + bàn giao
- Build exe từ bản sao `D:\temp\zu-copy-*` (`Tools/build-from-copy.ps1`), xoá exe `D:\temp\player-*` cũ trước (ổ D còn ~2,7 GB).
- `dotnet test` chỉ khi đổi logic dùng chung (đổi da không đụng). 2 test Frost Hollow lỗi sẵn từ nhánh gốc: sửa riêng.
- Cập nhật `docs/pending-tasks.md` mỗi lượt.

## Câu hỏi mở
- Ấm ánh sáng map 2.5D cho khớp UI hay giữ nguyên?
- Giữ cờ chuyển `--ui-skin fantasy` lâu dài hay xoá skin Fantasy khi Adventure xong?
- UI tiếng Anh (như game hiện tại) hay tiếng Việt (như mockup Adventure)?

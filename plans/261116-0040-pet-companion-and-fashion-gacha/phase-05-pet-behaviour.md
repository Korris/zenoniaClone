# Phase 05: Pet xuất chiến thật (Lần 8, câu 1) — DONE

- `HeroPet.cs`: máy trạng thái FOLLOW → CHASE → ATTACK; nghỉ 1,5 ô sau chủ lệch 0,6; MoveTowards 110 % / 160 % / teleport > 7 ô; đi theo `FrostHollowMap.Move` (trượt theo tường, kẹt thì hiện ở đích).
- `HeroPet.Sheets.cs`: pet quái dùng `Sprites/monsters/<type>.png` (walk 192) + `-attack.png` (256); scale = HeightUnits / BodyHeightUnits; Walk 8 FPS, Attack 4 frame / 400 ms; Legacy: ảnh tĩnh nhấp 2 px.
- `HeroController.Pet.cs` (authority): mục tiêu nhớ 4 s / 6 ô; đánh thường 2 s 10 %, skill 20 s; thời điểm trúng = thời gian chạy (khoảng cách chủ→quái trừ tầm, tốc 160 %) + 300 ms; `NetEventBus.PetSkill` lúc bắt đầu chu kỳ (bit 7 = đánh thường); `Monster.TakePetHit(dmg, owner, skill)`.
- Số sát thương: `HitFlags.Pet` (xanh ngọc) / `PetSkill` (vàng nhạt), nhỏ, không sparks, không rung; ẩn theo "Other players' pets".
- FX: `PetSkillFx.Strike` khi pet bắt đầu vung (xa: đạn; cận: nổ ở frame 3).

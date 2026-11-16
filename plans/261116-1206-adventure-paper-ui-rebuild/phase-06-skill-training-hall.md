# Phase 06: Skill "Training Hall"

## Context Links
- Mockup: `chatgpt/skill-final-mockup-training-hall.png` (+ mockup thiếu: gán, Respec, Advanced, max rank, thiếu SP)
- Hiện tại: `ingame/skill-warrior-*.png`, `skill-wizard-skill-selected.png`
- Code: `SkillTreeWindow.cs` (180), `SkillTreeNodeView.cs`, `SkillTreeDetailPane.cs`, `SkillTreeLinks.cs` (đường nối điều kiện), `SkillTreeActions.cs` (`Reason`, `PrereqText`, Learn / Assign / Respec offline + online), `DebugCapture.Skills.cs` (`--skill-ui "t=step"`)
- Phase 01, 02 (S1, mockup thiếu), 05 (style thẻ / giấy ghim)

## Overview
- Ưu tiên: P1. Effort: M. Status: code xong 16/11, build + ảnh OK, công tắc Skill vẫn TẮT (chờ người dùng duyệt; chưa commit, chưa code review, chưa chạy online).
- Võ đường: 8 skill là tượng trên 2 tầng kệ (Basic / Advanced), nhãn điều kiện nổi, cuộn chi tiết + Rank Up / Assign to Magazine, đai Skill Magazine Dash + ô 1-8.

## Key Insights
- Logic đã tách sẵn (`ISkillTreeActions`, `SkillTreeActions.Reason/PrereqText`): chỉ làm lại view. Window vẽ lại khi dấu vân tay trạng thái đổi: giữ cơ chế.
- Dữ liệu có: SP còn / tổng, rank / max, MP, cooldown, mô tả, giá trị rank sau (Damage 124 % -> 136 %), điều kiện, Respec (giá thật trong `SkillRespec`), 8 ô magazine + Dash, 3 trang HUD.
- Điều kiện mở khoá thật **không phải chuỗi thẳng**: nhãn "Needs X Rank N" hiện trên tượng đang chọn / tượng khoá, mũi tên tới các tượng bị chặn; bỏ đường nối chéo (ChatGPT chê đè tên).
- Mockup có tranh minh hoạ skill (Whirlwind quanh hero) mà game không có: dùng icon skill cỡ lớn trong cuộn; nếu muốn tranh thì phải xin ChatGPT riêng (YAGNI, hỏi người dùng).
- Đai magazine hiện đủ Dash + 8 ô, đánh dấu ranh giới 3 trang HUD (Dash,1,2 | 3,4,5 | 6,7,8) thay cho "Page 1/3" đổi trang.
- 6 class dùng chung một nền võ đường; hero bên trái là class hiện tại (dùng lại cách vẽ hero xem trước của phase 05).

## Requirements
- Tượng 4 trạng thái: đã học (tích lục), đang chọn (khung vàng), học được (chấm than), khoá (ổ khoá + bệ xám); số rank "3/10".
- Cuộn chi tiết: icon, tên, nhãn Basic / Advanced, Lv / Rank, mô tả, MP, Cooldown, Next Rank (giá trị trước -> sau), dòng điều kiện đạt / chưa đạt, Rank Up (x SP), Assign to Magazine.
- Gán: bấm Assign -> ô đai nhấp nháy -> chạm ô; hoặc kéo tượng xuống ô (dùng lại Drag của phase 05 qua PaperKit, không chép code).
- Thanh trên: class + Lv, SP, vàng, Respec (giá thật, xác nhận), Inventory (mở Bag), Close. Không hiện "gem".
- Phím T bật tắt, Esc đóng, mũi tên chọn tượng (thêm nếu chưa có).

## Architecture
```
PlayerState.Skills/Loadout/SkillPointsFree + SkillTable ──> TrainingHallWindow (view)
  ├─ .Shelves (8 tượng, 2 tầng, nhãn điều kiện)   ├─ .DetailScroll (cuộn giấy, Rank Up / Assign)
  └─ .MagazineBelt (Dash + 8 ô, chế độ gán)
hành động ──> ISkillTreeActions (có sẵn) ──> offline state+save | online request
UiScreens.Skill chọn TrainingHallWindow | SkillTreeWindow
```

## Related Code Files
- Tạo: `TrainingHallWindow.cs`, `TrainingHallWindow.Shelves.cs`, `TrainingHallWindow.DetailScroll.cs`, `TrainingHallWindow.MagazineBelt.cs`, `DebugCapture.UiSkill.cs`
- Sửa: `DebugCapture.Skills.cs` (bước `open` mở qua UiScreens) - chỉ phase này sửa file đó
- Không sửa: `SkillTreeActions.cs` (trừ khi thiếu hàm đọc; nếu cần thêm hàm thì thêm, không đổi hành vi)
- Xoá (phase 13): `SkillTreeWindow.cs`, `SkillTreeNodeView.cs`, `SkillTreeDetailPane.cs`, `SkillTreeLinks.cs`

## Implementation Steps
1. Nhận mockup thiếu (phase 02).
2. Khung + nền S1 + thanh trên.
3. Kệ + tượng + nhãn điều kiện (từ `PrereqText`).
4. Cuộn chi tiết + Rank Up / Assign + xác nhận Respec.
5. Đai magazine + chế độ gán + kéo thả.
6. Phím.
7. Cờ chụp: dùng `--skill-ui` (open, learn:i, slot:n, respec) + `--class`, `--level`; mới bước `select:i`, `assign-mode`, `confirm-respec`.
8. Build bản sao, chụp 6 class x (học một phần, max, sau respec, đang gán) ở 1920x1080 + 1600x1200 + 3440x1440; online 1 lần (`--net-bot` + `DebugCapture.NetSkills`). Tự xem.

## Todo List
- [x] Mockup thiếu (`skill-states-assign-respec-advanced-max-rank-no-sp.png`) + art S1 đã cắt (`Sprites/ui/paper/skill/`)
- [x] Khung, nền, thanh trên
- [x] Tượng 4 trạng thái + nhãn điều kiện (theo `SkillDef.Prereqs` thật)
- [x] Cuộn chi tiết, Rank Up, Assign, Respec xác nhận
- [x] Đai magazine + gán + kéo thả
- [x] Phím (T bật tắt, Esc đóng, mũi tên chọn tượng; lúc gán: trái / phải chọn ô, J / Space đặt)
- [x] Cờ chụp, ảnh 5 class, 16:9 / 20:9 / 4:3, tự xem (`docs/reference/ui-redesign-261116/phase-06-skill/`)
- [ ] Online (`--net-bot`): CHƯA chạy (đường gửi không đổi: vẫn `SkillTreeActions.Current`)
- [ ] Test tay kéo thả bằng chuột thật: CHƯA (mới chạy qua bước `drag` / `move` / `unslot`, cùng handler)
- [x] `dotnet test` 2 bộ: qua (358 + 422); phase này không sửa file nào hai bộ test biên dịch

## Ghi chú sau khi làm (16/11)
- File thật: `TrainingHallWindow.cs` + `.TopBar`, `.Shelves`, `.DetailScroll`, `.MagazineBelt`, `.Respec`, `.Input`, `.Drag`, `.Capture`; widget `PaperSkillStatue.cs` (tượng + `PaperSkillParts`), `PaperSkillBeltSlot.cs` (ô đai); `DebugCapture.UiSkill.cs`. Sửa: `DebugCapture.Skills.cs` (bước `open` / `close` qua UiScreens, mọi bước chạy trên giao diện đang bật), `PaperArt.Pieces.cs` (thêm khối `Hall*`), `PaperKit.Slots.cs` (PaperCard: tên 2 dòng không đè hàng sao / dòng nhỏ) + `ArmoryWindow.Shelf.cs` (bỏ 2 dòng nhích tay tương ứng).
- Bật màn: `--ui-new skill`; duyệt xong thì đổi mặc định trong `UiScreens.useNew`. Công tắc tắt = `SkillTreeWindow` cũ y nguyên.
- Dữ liệu skill KHÔNG chia Basic (4) / Advanced (4) như mockup: `SkillDef.Tier` có 3 bậc theo level mở (Basic < 20, Advanced 20-39, Ultimate >= 40) = 3 / 3 / 2 skill. Nên: 3 ván gỗ bên trái ghi số thật, bấm = chọn tượng đầu của bậc, ván của bậc đang chọn sáng; cả 8 tượng luôn nằm trên kệ theo thứ tự cây (0-3 tầng trên, 4-7 tầng dưới, như lưới cũ).
- Điều kiện thật (rẽ nhánh, mỗi skill đòi rank khác nhau): chỉ hiện cho tượng đang chọn = biển gỗ "Needs + tên skill + Rank N" (lục = đạt, đỏ = chưa; tầng trên biển nằm phía trên, tầng dưới biển nằm phía dưới để không che tượng khác) + viên "Rank N" trên chính tượng điều kiện. Art `condition-sign-arrows` có 2 mũi tên cố định: chỉ dùng phần biển (mũi tên bị che), dùng `arrow-down-yellow` chỉ vào tượng đang chọn.
- Khoá theo level: viên ở chân tượng ghi "Lv. N" thay cho "0/10".
- Đai: đủ Dash + 8 ô, 3 nhóm = 3 trang HUD ("Page 1-3"), không có nút đổi trang. Chạm ô có skill = chọn skill đó. Thêm so với cửa sổ cũ: kéo tượng xuống ô = gán; kéo ô sang ô khác = đổi chỗ; kéo ô ra ngoài đai = bỏ gán (`Assign(slot, -1)`, hành động có sẵn nhưng cửa sổ cũ chưa có nút).
- Respec: hỏi lại bằng bảng "Reset all skills?" (trả SP = tổng rank, giá = `SkillRespec.Cost(level)`, đỏ khi thiếu vàng) thay cho "bấm lần hai".
- SP trên thanh trên: "còn / tổng đã có" (level + điểm thưởng dev), cửa sổ cũ ghi "còn / level".
- Cờ chụp: `--hall-steps "learn:0:5,select:1,assign"` (chạy ngay các bước `--skill-ui`), `--hall-fresh`, `--hall-no-dev` (tắt allSkills + điểm thưởng của dev-start trong lần chạy đó), `--hall-gold N`; bước mới của `--skill-ui`: `assign-mode`, `confirm-respec`, `tier:T`, `drag:I:K`, `move:K:J`, `unslot:K`, `drag-demo:I:K`.
- Khác mockup: không có tranh minh hoạ skill, chip gem, nút "+", "Page 1/3" + mũi tên; ô đai không ghi tên skill (chỉ icon + số ô); nút Cancel lúc gán là chính nút Assign trên cuộn; ở 20:9 / 4:3 hero không đứng đúng giữa thảm (nền phủ kín bị cắt khác khung 16:9).
- Chưa dùng trong art S1: `magazine-slot-*` đều dùng; `icon-tab-*` dùng cho Basic / Advanced, Ultimate chưa có icon riêng (đang dùng sao của kit).

## Success Criteria
- Học, tăng rank, gán, respec chạy như cũ offline + online; điều kiện hiển thị đúng với `SkillTreeActions.Reason`.
- Ảnh khớp bố cục mockup ở 6 class; tên skill dài (Berserker Awakening) không tràn.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Tên / mô tả dài tràn tượng hoặc cuộn | Trung bình x Trung bình | Chữ 2 dòng + co cỡ tối thiểu; chụp đủ 6 class |
| Nhãn điều kiện che tượng khác | Thấp x Thấp | Chỉ hiện cho tượng đang chọn |

## Security Considerations
- Server vẫn kiểm học / gán / respec online (không đổi).

## Next Steps
- Phase 13 xoá SkillTree view cũ.

## Câu hỏi mở
- 2 ván "Basic (4) / Advanced (4)" bên trái: chỉ là nhãn / cuộn tới tầng, hay ẩn bớt một tầng? Đề xuất: nhãn + bấm thì chọn tượng đầu tầng đó.
- Có cần tranh minh hoạ cho từng skill (mockup có) không? Đề xuất: chưa, dùng icon lớn.

# Phase 06: Chuỗi quest tân thủ ở làng đầu (Elder stone + Orin card)

## Context Links
- `Assets/Scripts/Rpg/QuestLog.cs` (`QuestDef`: `RewardStones`, `Requires`, `AutoStart`, `RewardCards`, `CardDuel`, `NeedsCard`; `QuestCatalog.All`), `Assets/Scripts/Rpg/CardDuelQuest.cs`, `Rpg/PlayerState.Quests.cs`, `Rpg/Enhance.cs` (đá `upgradeStone`)
- `docs/pending-tasks.md` mục 24/11 ĐANG BÀN; plan đã code `plans/261124-0317-card-tutorial-quest-npc-duel/`; chat Tay https://chatgpt.com/c/6b03f69b-a4f8-83ec-af99-547d92463f83

## Overview
P1 cho phần Elder; phần Orin CHỜ người dùng chốt (không code trước). Tái dùng `first-hunt` và `first-card-duel`, không tạo hệ quest mới.

## Chuỗi đề xuất (khớp góp ý ChatGPT trong pending-tasks)
1. **First Hunt (Elder Thomas, giữ id `first-hunt`)**: AutoStart, đánh 5 quái camp Đồng Cỏ Ba Lá (`MonsterType = "meadowSlime"` hoặc giữ AnyMonster), thưởng `RewardStones` (số theo production sheet, vd 3-5) + 30 G (+ mũ khởi đầu nếu người dùng giữ). KHÔNG vũ khí / off-hand. Dòng thoại nhắc "dùng đá ở thợ rèn / Enhance".
2. **Orin Q1 (MỚI, chờ chốt)**: `Requires = "first-hunt"` (Orin chỉ có dấu sau hướng dẫn chiến đấu), đánh 5 quái camp đầu -> `RewardCards` 5 lá Common Taurus. Append cuối `QuestCatalog.All`.
3. **Orin Q2 = `first-card-duel` (đã code)**: đổi gate từ `NeedsCard` sang `Requires` Q1; ván hướng dẫn deck mượn giữ nguyên. Thắng chọn 1 lá trong 3-5 lá Common/Uncommon của Orin, thua đấu lại miễn phí (sau 2 lần thua bật gợi ý): phần "chọn 1 lá" là UI + server mới -> chỉ làm khi người dùng chốt.
Điểm nối camp: Q1 + First Hunt đếm kill quái camp 1 (Clover Slime) -> phase 03 phải giữ camp 1 đủ quái gần cổng tây (≥ 2 cụm, hồi ≤ 6,5 s) cho nhiều người mới cùng lúc.

## Implementation Steps
1. Sửa `first-hunt` (stone, text, MonsterType) - làm ngay sau phase 03.
2. Chờ người dùng chốt Orin; sau đó: Q1 QuestDef mới, đổi gate `first-card-duel`, (nếu duyệt) UI chọn lá + server cấp lá, kiểm chống farm alt (lá thưởng chỉ Common yếu, 1 lần / nhân vật).
3. Quest đồng bộ server: `QuestCatalog` dùng chung, build lại server; thêm test quest (gate Requires, thưởng stone, AutoStart đếm meadowSlime).
4. Cập nhật `pending-tasks.md` mục 24/11.

## Todo
- [ ] first-hunt thưởng stone
- [ ] (chờ chốt) Orin Q1
- [ ] (chờ chốt) gate + chọn lá Q2
- [ ] Test quest, offline + online (`Tools/net-orin-test.ps1`)

## Success Criteria
Nhân vật mới: đánh 5 Clover Slime -> Elder trả stone, ép được 1 lần ở Enhance; Orin chỉ hiện dấu sau First Hunt (khi phần Orin được chốt).

## Risk
- Đổi MonsterType của AutoStart: save đang đếm AnyMonster vẫn giữ progress; chấp nhận.
- Game dùng ? = có quest, ! = trả quest (ngược quy ước ChatGPT): giữ quy ước game.
- Đụng code card tutorial đang chờ người dùng thử: chỉ sửa sau khi chốt.

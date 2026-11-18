# Quest tân thủ "First Hunt" tự kích hoạt (19/11/2026)

Branch `worktree-agent-a385bc9ed637c19ce` (gốc `unity` 60d3b56a). Exe test: `D:\temp\player-quest\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-quest`, Succeeded, 0 lỗi).

## Đã làm
- `QuestDef` thêm `AutoStart` và `ReadyNotice`. First Hunt: `AutoStart = true`, `ReadyNotice = "First Hunt complete! Visit Elder Thomas in Oakhaven for a reward."`.
- `QuestLog`: lúc tạo (game mới) và sau mỗi `Load` (save cũ, RpgState của client), quest AutoStart nào chưa có mục thì thêm mục Active, 0 kill. Save đã nhận (Active / Ready / Done) giữ nguyên trạng thái và số kill. Mục mới lưu cùng nhân vật như quest thường (server-api đã nhận `quests` state 1, không phải sửa).
- Đếm kill vẫn đi đường cũ, server quyết: `Zone` -> `KillRewards.Resolve` -> `PlayerRpg.QuestKill` -> `QuestLog.CountKill`. Online thì zone server đếm và lưu, client chỉ đọc RpgState.
- `QuestTracker`: khi đủ 5 kill, chat hệ thống dùng `ReadyNotice`, toast "Objective Complete / Visit Elder Thomas for a reward". Các kill 1..4 hiện toast "First Hunt / Defeat any monster N / 5" như trước.
- Nộp ở Elder giữ nguyên (30 G + mũ class, túi đầy thì không cho nộp). `AcceptQuest("first-hunt")` giờ luôn trả "Quest already taken" vì quest đã chạy sẵn.
- Cờ chụp `--newbie-quest ready|done` bỏ bước nhận quest.

## Thay đổi hành vi cần biết
- Elder không mời First Hunt nữa (không còn trạng thái Available). Trong lúc First Hunt đang chạy, nói chuyện với Elder sẽ ra lời mời quest "Ancient Crystal Survey" (dấu "?"). Đủ 5 kill thì Elder hiện "!" và cuộc nói chuyện là màn nộp.
- Cửa sổ mời quest (có ô thưởng mũ) không còn hiện cho First Hunt. Sổ quest vẫn liệt kê First Hunt ở mục đang làm, có phần thưởng.
- Save cũ chưa từng nhận First Hunt (kể cả save đã xong Crystal Survey) sẽ bắt đầu đếm từ 0 ở lần load tới.

## Kiểm tra
- `ZenoniaSkills.Tests` 408/408. `NewbieQuestTests` mới / sửa: `ANewCharacterHasItRunningAlready`, `TheFirstKillsCountWithoutTalkingToAnyone`, `KillsArePersistedWithTheCharacter` (đi qua JSON), `AnOldSaveThatNeverTookItStartsIt`, `AnOldSaveThatTookItKeepsItsState` (Active 2 / Ready 5 / Done 5). Các test nộp và test túi đầy giờ không gọi Accept. `QuestTests.Hero()` bỏ Accept.
- `ZenoniaNet.Tests` 422/422. Không sửa server-api nên không chạy vitest.
- Không đổi `NetProtocol.Version`. Zone server dùng chung `QuestLog.cs`, nên phải build lại server cùng client.

## Ảnh `docs/reference/headgear-ingame/` (đã xem từng ảnh)
- `auto-first-hunt-progress-3of5-fresh-warrior.png`: Warrior mới Lv 1, Willowbrook Meadows, `--bot fight` giết quái thật, chưa nói chuyện với ai. Toast "First Hunt / Defeat any monster 3 / 5". Chat đã có dòng hoàn thành vì toast xếp hàng còn chat hiện ngay.
- `auto-first-hunt-complete-notice-fresh-warrior.png`: cùng lượt chơi: toast "Objective Complete / Visit Elder Thomas for a reward" và chat "[System] First Hunt complete! Visit Elder Thomas in Oakhaven for a reward."
- `auto-first-hunt-hand-in-elder.png`: Oakhaven, Elder Thomas: "Well done! You fight like a true adventurer." 5 kill ở ảnh này giả qua `--newbie-quest ready` (gọi `PlayerRpg.QuestKill`, cùng hàm `KillRewards` gọi), không phải đánh thật. Toast "3 / 5" còn sót trong hàng đợi.
- `auto-first-hunt-gift-hat-in-bag-warrior.png`: sau khi nộp, túi lọc "Hat" có 1 mũ (icon tạm, chưa có art mũ sắt nhỏ). Vàng 100030 = dev-start + 30.
- Chưa xem: online thật (zone server đếm, lưu, load lại), đi bộ từ bãi quái về Elder trong một lượt chơi, nộp khi túi đầy trong game (chỉ có test dotnet).

## Câu hỏi còn mở
- Trong lúc First Hunt chạy, Elder mời Crystal Survey. Có muốn ông nhắc First Hunt trước (hiện ActiveLines kèm "N / 5")?
- Chat ghi cứng "in Oakhaven" trong `ReadyNotice`. Nếu sau này dời Elder sang map khác thì phải sửa chuỗi này.

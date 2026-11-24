# Phase 04: UI (Orin, bàn hướng dẫn, gợi ý / bỏ qua, xác nhận rủi ro PvP)

**BẮT BUỘC: implementer đọc HẾT `docs/ui-layout-rules.md` trước khi làm** (padding, gap icon-chữ, căn giữa dọc, bo góc; icon / khung / nút chỉ dùng asset ChatGPT đã cắt, KHÔNG tự vẽ bằng code). Chụp trong game so mockup trước khi báo xong. Không đụng map (Orin đã có ô (16,21)); nếu phải đụng map thì đọc `docs/map-design-rules.md`.

## Context Links
- `Assets/Scripts/CardMasterTalk.cs`, `NpcDefs.cs` (cardMaster), `TownNpc.cs` (Talk dòng 89-97), `ElderNpc.cs` (Talk / marker), `UiScreens.cs` (OpenForNpc), `QuestOfferWindow`, `NpcDialogueBox`.
- `CardTutorial.cs`, `DuelClient.cs`, `CardDuelBattleView*.cs`, `CardDuelDeckPage.cs`, `CardDuelLobbyWindow.cs`, `PartyPlayerMenu.cs`, `CardConfirmBox`, `CardDuelPickPanel.cs` (thumbnail card dùng lại), `CardWindow.Highlight`.
- Ảnh cũ: `docs/reference/card-gem-ui/phase-05-shots/` (bàn đấu NPC trước đây), `phase-08-shots/`.

## Overview
Priority P2. Done (code + chụp offline; chưa thử online / người dùng chưa test cảm giác). Toàn bộ là client view: gửi op, vẽ `DuelView`. Không quyết kết quả.

## Key Insights
- Orin hiện `QuestGiver=false`, Talk -> `UiScreens.OpenForNpc` -> `CardMasterTalk`. `ElderNpc.Talk` dùng portrait elder cứng -> Orin cần nhánh riêng trong `CardMasterTalk` (portrait Orin) nhưng vẫn bật `QuestGiver=true` để có "?" / "!" trên đầu + minimap.
- Battle view hiện ẩn / xám bàn khi `SecondsLeft < MinPlaceSeconds`: ván NPC không đồng hồ -> phải bỏ qua đồng hồ khi `View.Npc`.
- Highlight vòng sáng của `CardWindow.Highlight` / CardTutorial có sẵn asset -> dùng lại cho ô bàn + lá trên tay.
- `DuelClient.Apply` tự mở CardWindow trang Duel: dùng luôn cho ván NPC.

## Requirements
1. **Orin** (`CardMasterTalk.Talk(npcId)`), theo trạng thái quest `first-card-duel`:
   - Available: OfferLines -> `QuestOfferWindow` (Accept) -> sau Accept hiện "Ready for your first duel?" (CardConfirmBox: "Duel" / "Later").
   - Active: ActiveLines -> CardConfirmBox "Duel now" -> `DuelClient.PlayNpc(0)`.
   - ReadyToTurnIn: ReadyLines (giải thích PvP thắng lấy 1 lá, hoà không mất, Lv30, Ranked không lấy lá, deck mượn trả lại) -> `QuestTracker.TurnIn` -> toast thưởng (`RewardText`: "+300 G, Horn Rabbit, Thorn Boar, Forest Wolf").
   - Done: 5 trang cũ (sửa câu "No duels") -> CardConfirmBox 2 nút "Practice Duel" (`PlayNpc(1)`) / "Open Cards" (như cũ).
   - `TownNpc.Talk`: cardMaster đi nhánh `CardMasterTalk` TRƯỚC `ElderNpc.Talk`.
2. **Bàn hướng dẫn** (`CardDuelBattleView.Tutorial.cs` mới, partial):
   - `View.Npc`: ẩn đồng hồ lượt, ẩn mọi phần stakes / Ranked, tên đối thủ "Card Master Orin".
   - `ScriptStep > 0`: panel bài học (khung + chữ giống CardTutorial: tiêu đề + 1-2 câu từ `DuelTutorialScript`), vòng sáng trên lá phải chọn và ô phải đặt; chỉ cho bấm đúng lá / ô (server vẫn kiểm). Sau nước Orin kịch bản (lật Horn Rabbit) panel giải thích 1 dòng.
   - Nút **Hint** (luôn có trong ván NPC, sau kịch bản): `DuelNpcAi.Best(view.Board(), view.Hand, 0, careful:true)` -> vòng sáng lá + ô 1 nước.
   - Nút **Skip tutorial** (khi ScriptStep > 0): `DuelClient.SkipScript()`.
   - Kết thúc: banner dùng chung (Win / Draw / Lose + nhạc `DuelMusic`); Tutorial thêm dòng "Return the borrowed deck to Orin for your reward" + nút "Talk to Orin" (đóng bàn, gọi `CardMasterTalk` nhánh Ready); Practice: "Practice only, nothing won or lost".
3. **CardTutorial**: thêm bước 4 "First Card Duel": "Card Master Orin by Oakhaven's south gate lends you a deck for a first practice duel." (không mũi tên).
4. **Xác nhận rủi ro PvP lần đầu** (`CardDuelPvpRiskConfirm.cs` mới): trước khi gửi Challenge (lobby, PartyPlayerMenu) và trước Accept lời mời, nếu bit `HelpPvpRisk` chưa bật: hộp có câu "Winner takes 1 card from the loser's deck. These 8 cards can be taken:" + 8 thumbnail deck hiện tại (dùng lại widget thumbnail của `CardDuelPickPanel`), nút "I understand" (gửi `InvOp.CardHelp` Slot 2 rồi làm tiếp) / "Cancel". Ranked không hỏi.
5. `DuelClient`: `PlayNpc(int mode)`, `SkipScript()`; `Online` không còn chặn op NPC (định tuyến offline đã làm ở phase 03). Trang deck offline giữ "disabled" cho PvP; thêm câu "Practice with Card Master Orin works offline too".

## Architecture
View thuần: `DuelView` (Npc, ScriptStep, Moves, Hand) -> vẽ. Gợi ý là hàm thuần trên view (phase 01). Lesson text lấy từ `DuelTutorialScript` (một nguồn, DRY).

## Related Code Files
- Tạo: `Assets/Scripts/CardDuelBattleView.Tutorial.cs`, `Assets/Scripts/CardDuelPvpRiskConfirm.cs`.
- Sửa: `CardMasterTalk.cs`, `NpcDefs.cs` (QuestGiver true, câu thoại), `TownNpc.cs` (thứ tự nhánh), `CardTutorial.cs`, `DuelClient.cs` (trừ dòng định tuyến offline của phase 03), `CardDuelBattleView.cs` / `.Build.cs` (bỏ đồng hồ khi Npc, móc partial), `CardDuelDeckPage.cs` (câu offline), `CardDuelLobbyWindow.cs` + `PartyPlayerMenu.cs` (gọi xác nhận), `DebugCapture.Duel.cs` (cờ chụp `--duel-npc-tutorial N`, `--talk-npc cardMaster --quest-state X`).
- Ownership: các file client trên; KHÔNG sửa `Net/**`, `Rpg/**`.

## Implementation Steps
1. Đọc `docs/ui-layout-rules.md` + Sổ lỗi UI.
2. Orin: nhánh quest trong `CardMasterTalk`, `NpcDefs` QuestGiver + lời thoại, `TownNpc` thứ tự.
3. `DuelClient` PlayNpc / SkipScript.
4. Battle view: Npc không đồng hồ, partial Tutorial (panel, vòng sáng, chặn bấm sai, Hint, Skip, banner cuối).
5. CardTutorial bước 4.
6. PvP risk confirm ở 3 chỗ (Challenge lobby, Challenge từ menu người chơi, Accept).
7. DebugCapture cờ chụp; build từ bản sao `Tools/build-from-copy.ps1`; chụp: Orin 4 trạng thái, 3 bước kịch bản, Hint, Skip, banner Tutorial / Practice, hộp xác nhận PvP, offline. So `ui-layout-rules`; lỗi UI mới ghi Sổ lỗi.

## Todo List
- [x] Orin nhánh quest + Practice menu
- [x] DuelClient op mới
- [x] Battle view NPC (không đồng hồ, ẩn stakes)
- [x] Tutorial panel + highlight + chặn bấm sai
- [x] Hint + Skip
- [x] Banner cuối Tutorial / Practice + "Talk to Orin"
- [x] CardTutorial bước 4
- [x] PvP risk confirm (3 chỗ)
- [x] Ảnh chụp từ bản sao, so rules

## Success Criteria
Chụp đủ màn, không chữ tràn / lệch tâm; ván tutorial chơi trọn từ client bản sao (offline + dev server). Người dùng tự test cảm giác trong game (memory: FX/UI người dùng test).

## Risk Assessment
- Battle view đã 233 dòng: mọi thứ mới vào partial `.Tutorial.cs`.
- Hộp xác nhận chen giữa luồng Accept có đồng hồ lời mời: hỏi trước khi bấm Accept, hết giờ thì lời mời tự huỷ như cũ.
- Chữ panel dài: giữ 1-2 câu, kiểm tràn khung.

## Security Considerations
Client chỉ gợi ý / highlight; server từ chối nước lệch kịch bản (`ScriptMove`). Bit xác nhận chỉ là UX, không ảnh hưởng luật.

## Next Steps
Phase 05.

## Làm khác kế hoạch (24/11)
- `DuelClient` đã xong từ phase 03 (không sửa thêm). Câu hỏi của Orin ("Ready for your first duel?", "Practice Duel / Open Cards") dùng `CardConfirmBox` của cửa sổ Card nên cửa sổ mở trang My Cards phía sau (không phải trang Duel: offline trang đó bị tấm "offline" che). `CardConfirmBox.Ask` thêm tham số nhãn + hành động nút thứ 2 (`cancelLabel`, `onCancel`: "Later" đóng cửa sổ).
- `CardMasterTalk.Open()` (không tham số) = "Talk to Orin" ở bảng kết quả; `TownNpc` gọi `UiScreens.OpenForNpc` cho cardMaster TRƯỚC `ElderNpc.Talk` (Orin `QuestGiver = true` chỉ để có dấu ?/! + minimap). Chưa có card nào thì quest bị khoá: Orin nói 5 trang cũ rồi mở My Cards.
- Bảng bài học nằm đúng chỗ khung Battle Log (cột trái), Hint nằm cạnh pill "Your Turn" (chỗ vòng đồng hồ bị ẩn); lá + ô bài học tự chọn, lá / ô khác bị chặn và báo `ScriptMove`. Lá Orin chơi theo kịch bản hiện 1 dòng "Orin's last move" ở bài sau. Tutorial hay practice suy từ trạng thái quest (Done = practice).
- Xác nhận PvP: cửa sổ riêng trên thế giới (`CardDuelPvpRiskConfirm`, không phụ thuộc cửa sổ Card), hỏi ở Challenge (trang Duel + menu người chơi) và Accept lời mời CÓ cược; Ranked không gọi. `DuelClient.Challenge/Accept` giữ nguyên (net bot gọi thẳng).
- Thêm: slot "3 cards" ở `QuestOfferWindow`; chữ "Leave the duel with Orin? It counts as a loss." ở `CardWindow`; cờ chụp `--quest-state`, `--quest-offer`, `--orin-ask`, `--duel-npc tutorial:N|free|hint|end|end-talk`, `--duel-risk` (`DebugCapture.DuelNpc.cs`); `CardDuelBattleView.cs` 242 dòng (đã 233 trước đó), phần mới ở `.Tutorial.cs` 173 dòng.
- Chưa làm: thử online với dev server; chụp panel "Orin's move" (chỉ ~1 s); bấm thật nút Accept / Duel / Skip / Hint / "I understand" bằng chuột; tiếng (chưa có sfx card).
- Ảnh: `docs/reference/card-gem-ui/card-tutorial-shots/` (01..13). Test: ZenoniaSkills 722, ZenoniaNet 559 + 2 FrostHollow fail sẵn có. Biên dịch từ bản sao 0 lỗi.

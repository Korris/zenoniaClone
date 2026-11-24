# Phase 03 (P1): Card tutorial V4 với Card Master Orin

## Context Links
- BIBLE part2 V Q03B + Q04 (5 quái -> 5 lá, đấu 4x4, 2 lượt dạy, thắng mới chọn 1 lá, thua / hoà đấu lại), part3 X.2-X.5 (12 card Taurus, deck Orin 8 Common, ≥ 3 lá chưa nhận, chống khai thác).
- Đã code (CHƯA commit): `plans/261124-0317-card-tutorial-quest-npc-duel/` (phase 01-04 done): `Rpg/CardDuelQuest.cs`, `Rpg/Cards/DuelTutorialScript.cs`, `DuelNpcSession.cs`, `DuelNpcAi.cs`, `LocalDuelHost.cs`, `Net/Server/DuelService.Npc.cs`, `CardMasterTalk.cs`, `CardDuelBattleView.Tutorial.cs`, `CardDuelPvpRiskConfirm.cs`, test `DuelNpc*Tests`, `CardDuelQuestTests`, `Tools/net-orin-test.ps1`.
- Card: `Rpg/Cards/CardTableData.cs`, `CardTable.cs` (`Count = 40`, `Regions`), `AlbumRules.cs` (mốc 10/20/30/40), `server-api/src/rpg/card-ids.json` (sinh bằng `ZENONIA_WRITE_CARD_IDS=1 dotnet test ... --filter CardIdsExportTests`).
- UI: **đọc hết `docs/ui-layout-rules.md`** cho màn chọn lá thưởng + nhãn CAPTURE.

## Overview
P1. Giữ 90 % code đã làm; đổi dữ liệu + 3 luật. Điểm nối với code cũ ghi ở bảng dưới.

## Điểm nối / việc phải đổi
| Đang có (261124-0317) | V4 | Việc |
|---|---|---|
| Quest mở khi có card đầu (`NeedsCard`) | Mở khi xong `tau-meadow-trial` của Rowan | `Requires = tau-meadow-trial`, bỏ `NeedsCard` khỏi quest này |
| Không có quest "đánh 5 quái" của Orin | `tau-orin-first-deck`: 5 quái bất kỳ -> 5 Common cố định | Quest mới (`MonsterType "*"`, Count 5, `RewardCards` 5 lá) trong `TaurusQuests.cs` |
| Deck mượn cố định 8 lá card CŨ | 5 lá vừa nhận + 3 lá Orin cho mượn | Sửa dữ liệu `DuelTutorialScript` (vẫn deck cố định, không đụng `CardCollection`, nên không thu hồi / không nhân bài) |
| Kịch bản 3 bài học (đặt / chiếm / phòng thủ) | 2 lượt dạy rồi AI dễ | Giữ 3 bài (đủ ý BIBLE: nhìn cạnh, chiếm, đặt cạnh không luôn thắng); viết lại ô / lá theo card mới; bài chiếm = Horn Rabbit cạnh phải ăn cạnh trái yếu của Orin |
| Hết ván (thắng / hoà / thua / bỏ cuộc) = xong quest | Chỉ THẮNG mới xong; thua / hoà đấu lại | Mục tiêu `CardDuelWin`; thua / hoà / bỏ cuộc: quest giữ Active, banner "Try again" |
| Thưởng 300 G + 3 lá cũ | Chọn 1 lá trong deck Orin (8 Common, ≥ 3 lá chưa nhận: Bramble Boar, Rootling, Amber Sporeling) | `QuestDef.RewardPickCards` (string[]); `InvOp.QuestTurnIn` thêm byte lựa chọn (zone kiểm id thuộc danh sách); màn "Choose Your Reward Card" |
| Lật bài hiện có | Lượt dạy đầu: hiện 2 số cạnh nhau 0,3-0,4 s rồi lật + "CAPTURE!" | Thêm vào `CardDuelBattleView.Tutorial.cs` nếu chưa có |
| Hộp xác nhận PvP (bit `cardHelp` 2) | Giữ | Không đổi |
| `CardTutorial` 4 bước (lụm card đầu) | 3 bước My Cards / chọn Slime / Deck + Album gắn vào quest | Kích hoạt khi nhận 5 lá từ Orin thay vì lúc lụm card đầu |

## Làm khác kế hoạch (24/11, đã làm xong)
- Không commit "card tutorial cũ" lần nữa: đã commit ở `38cf603e` (bước 1 bỏ). Quest Orin KHÔNG chờ Bram: `Requires = tau-meadow-trial` đúng BIBLE (cường hóa không bắt buộc cho Monster Card), khác chữ trong brief "sau quest slime / enhance".
- Chỉ số 12 card theo hình BIBLE nhưng đúng luật bảng (1 cạnh mạnh DUY NHẤT, tổng N 14-18 / R 18-22 / SR 22-26): nháp trong phase này (Slime 4/3/4/3, Sporeling 3/4/4/4) bị hoà cạnh nên đổi: Slime 5/3/4/3, Rabbit 3/7/2/3, Frog 6/3/3/3, Mushroom 3/3/6/3, Boar 6/5/3/3, Mantis 3/6/3/4, Rootling 4/3/5/4, Sporeling 3/4/3/6, Thorn Sprite 4/7/3/6, Ancient Barkling 3/5/8/4, Rune Bark Guardian 5/4/8/5, Elder Root Guardian (SR boss) 6/5/9/6. Elder Root Guardian là SR nhưng `IsBoss` = true (khung boss, rơi 3 %): test cũ "boss = SSR" chỉ còn áp cho 40 lá cũ.
- `CardTable.Count` = 52 (mọi dòng, index trong save), thêm `LiveCount` = 12, `Regions` = 2 vùng Taurus, `LegacyRegions` = 10 vùng cũ, `IsLive`. `CardCollection.AlbumCount` chỉ đếm lá Taurus (40 lá cũ ẩn: Album, mốc, tiến độ). Mốc Album 3 / 6 / 9 / 12 (giữ 4 cờ, bit `albumClaimed` không đổi); thưởng vàng mốc đầu vẫn 5,000 G (`AlbumRules.Gold10`), quá lớn cho Lv 1: xem câu hỏi.
- Card chưa có quái (05 / 06): `CardIdsCheck` bỏ qua id `tau*` thiếu MonsterDef; test `EveryCardHasALootTable` miễn trừ `tau*` (TODO gỡ ở 05 / 06). Ảnh card: `GemUiArt.Portrait` rơi về ô đầu sheet quái `taurus/<id>/<id>` (TEMP, gỡ khi có `card-portraits/tau*.png`).
- Quest: `tau-orin-first-deck` (5 quái bất kỳ -> 5 lá, `RewardCards`), `first-card-duel` chuyển từ `Legacy` sang `TaurusQuests.All` (`Requires` first-deck, bỏ hẳn `NeedsCard` + `QuestLog.HasCards`, không còn 300 G). Không có exp / vàng cho Orin (BIBLE không nêu).
- Thắng mới xong: `QuestCardDuel` chỉ gọi khi `TakeResult() == Win` (zone `DuelService.Npc` + `LocalDuelHost`). Thua / hoà / bỏ cuộc / rớt giữa ván: quest giữ Active, banner "Try again! Nothing was lost" + nút "Try again" (mở lại hội thoại Orin, câu hỏi Duel). ĐỔI quyết định cũ của người dùng "bỏ cuộc = xong quest" (V4 buộc thắng): cần người dùng xác nhận.
- Deck: người chơi = 5 lá nhận + 3 mượn (`DuelTutorialScript.Borrowed`: Bramble Boar, Rootling, Amber Sporeling), Orin = đúng 8 lá đó và cũng là danh sách chọn thưởng (`RewardPickCards` = `OrinDeck`). Deck dựng từ script, không từ `CardCollection` -> lá mượn không vào Collection / Album / PvP / giao dịch (test). Kịch bản 5 bước giữ (3 bài học) nhưng đổi lá + số.
- AI dễ: Orin ở chế độ Tutorial, sau kịch bản, 35 % nước đi là nước ngẫu nhiên hợp lệ (`DuelNpcSession.EasyMistakePct`), seed ngẫu nhiên mỗi ván (`Tutorial(now, seed)`). Đo 400 ván người đi theo kịch bản + Hint: thắng 80 %, hoà 16 %, thua 4 % (test giữ thắng >= 70 %). Chưa làm "sau 2 trận thua bật gợi ý": nút Hint luôn có sẵn ở lượt tự do (BIBLE phase 4 chỉ nói "có thể cho gợi ý").
- Chọn thưởng: dùng lại `InvOp.QuestTurnIn` với `Slot` = chỉ số (KHÔNG thêm byte / opcode, wire vẫn 36). `PlayerState.TurnInQuest(id, pick)` kiểm chỉ số với `QuestDef.RewardPickCards` trước khi đóng quest; chỉ số sai không đóng gì. Màn mới `CardRewardPickPanel.cs` (modal: 8 lá + tên + 4 số + xác nhận), mở từ cuối ReadyLines của Orin (`CardMasterTalk`); `QuestOfferWindow` thêm ô "Pick 1".
- UI lật: `CardDuelBattleView.Capture.cs` (lần ăn bài ĐẦU của ván Orin: "7 > 3" giữa 2 lá 0,35 s trước khi lật, "CAPTURE!" 0,7 s sau). Chữ thuần, không popup.
- `CardTutorial` (4 bước): chỉ bật sau khi `tau-orin-first-deck` Done (`OfferAfterDeck` từ `QuestTracker`), bước 4 đổi chữ theo Willowbrook.
- E2E: `Tools/net-orin-test.ps1` + bot `DebugCapture.NetOrin.cs` viết lại cho luồng V4; `server-api/scripts/dev-seed.mjs` thêm `--quest id:state[:progress]`, `--card a:1,b:1` và nâng `version` lên 7 khi seed (nhân vật API mặc định version 1 bị zone đọc thành nhân vật mới). Kết quả: 69 PASS / 0 FAIL (full, drop, forfeit, chưa xong trial).
- Ảnh: `docs/reference/world-design/greybox-shots/phase03-*` (offer deck, offer duel, reward pick + chọn 1, lật "7 > 3" + CAPTURE!, album Taurus, thắng, không thắng).

## Requirements
- 12 card Taurus APPEND cuối `CardTableData` (`tauCloverSlime` ... `tauElderRootGuardian`, rarity theo part3 X.2: 8 N, 3 R, 1 SR), region `tauCloverMeadows` / `tauAncientRootShrine`; `CardTable.Count` 40 -> 52; `Regions` live chỉ Taurus (40 lá cũ ẩn khỏi Album). Card id = id quái (quái thêm ở 05/06; card có trước không sao vì chỉ rơi khi quái tồn tại).
- Chỉ số: giữ HÌNH của BIBLE (cạnh mạnh), nâng lên thang N hiện có (tổng 14-18). Nháp: Slime 4/3/4/3, Rabbit 3/7/2/3, Frog 6/3/3/3, Mushroom 3/3/6/3, Mantis 3/6/3/4, Boar 5/5/3/3, Rootling 4/3/5/4, Sporeling 3/4/4/4; R / SR theo thang 18-22 / 22-26. Test kịch bản xác nhận mask lật.
- Chống khai thác: thưởng 1 lần (quest Done), chỉ zone trao, lựa chọn sai id bị từ chối, TurnIn lặp bị từ chối, rớt mạng giữa ván chơi lại không mất / không nhân.
- Album mốc: 10/20/30/40 -> theo 12 lá Taurus (đề xuất 4/8/12), cập nhật test.

## Related Code Files
- Sửa: `Rpg/CardDuelQuest.cs`, `Rpg/TaurusQuests.cs`, `Rpg/QuestLog.cs` (`CardDuelWin`, `RewardPickCards`), `Rpg/PlayerState.Quests.cs` (TurnIn có lựa chọn), `Rpg/Cards/DuelTutorialScript.cs`, `DuelNpcSession.cs` (kết quả thắng), `CardTableData.cs`, `CardTable.cs`, `AlbumRules.cs`, `Net/Protocol/NetOpcodes.cs` + reader / writer InvOp, `Net/Server/ServerInventoryActions.cs`, `Net/Server/DuelService.Npc.cs`, `LocalDuelHost.cs`, `CardMasterTalk.cs`, `CardDuelBattleView.Tutorial.cs`, `CardTutorial.cs`, `QuestOfferWindow.cs` (màn chọn lá hoặc file mới `CardRewardPickPanel.cs`), `server-api/src/rpg/card-ids.json`, test `CardDuelQuestTests`, `DuelTutorialScriptTests`, `DuelNpcSessionTests`, `CardTableTests`, `MessageRoundTripTests`, `server-api/test/card-state-schema.test.js`, `Tools/net-orin-test.ps1`.
- Tạo: `CardRewardPickPanel.cs` (nếu `QuestOfferWindow` > 200 dòng).

## Implementation Steps
1. Commit phần card tutorial đang dở (người dùng đồng ý) để diff V4 sạch.
2. 12 card + Count + Regions + Album; sinh lại card-ids.json.
3. Quest `tau-orin-first-deck`; sửa `first-card-duel` (Requires, CardDuelWin, RewardPickCards).
4. Viết lại `DuelTutorialScript` + test mask từng bước.
5. Wire: QuestTurnIn + choice; zone + offline kiểm id.
6. UI: màn chọn lá (8 lá, tên + 4 số, xác nhận), 2 số so sánh + CAPTURE!, banner thua "Try again". Chụp so mockup (art khung lá từ ChatGPT, không vẽ code).
7. `CardTutorial` gắn vào lúc nhận 5 lá.
8. 3 bộ test + `Tools/net-orin-test.ps1` sửa kịch bản (thắng -> chọn -> 1 lá; thua -> Active; TurnIn 2 lần bị từ chối).

## Todo List
- [x] Commit card tutorial cũ (đã có `38cf603e`)
- [x] 12 card Taurus, Count 52, Regions, Album, card-ids.json
- [x] Quest first-deck + first-card-duel V4
- [x] Kịch bản mới + test
- [x] QuestTurnIn có lựa chọn (wire + zone + offline)
- [x] UI chọn lá, CAPTURE!, Try again
- [x] net-orin-test pass (69 / 0)

## Success Criteria
Online: xong Slime -> Orin có dấu -> 5 kill -> nhận đúng 5 lá -> hướng dẫn My Cards -> đấu: thua đấu lại không mất bài; thắng -> chọn 1 trong 8 -> nhận đúng 1 lá 1 lần (API xác nhận). Lá mượn không xuất hiện trong Collection. Test xanh.

## Risk Assessment
- Thưởng phải thắng: người mới kẹt -> AI dễ + Hint có sẵn; nếu playtest thấy khó, giảm AI (chọn ngẫu nhiên giữa nước tốt nhất và kém).
- Người dùng từng chốt "bỏ cuộc = xong quest": V4 đổi thành phải thắng -> HỎI lại.
- Card art 12 lá chưa có tới phase 04-06: dùng khung lá + tên tạm, không hiện ảnh quái cũ.

## Security Considerations
Lựa chọn lá do client gửi index, zone tra lại danh sách `RewardPickCards`; không tin card id từ client.

## Câu hỏi cho người dùng
1. V4 buộc THẮNG Orin mới xong quest (thay "bỏ cuộc = xong quest" bạn chốt trước): đồng ý?
2. Mốc Album đầu (3 lá) vẫn trả 5,000 G: giảm còn bao nhiêu cho nhân vật Lv 1-10?
3. "Sau 2 trận thua bật gợi ý": Hint luôn có sẵn ở lượt tự do, có cần bật riêng sau 2 thua không?

## Next Steps
Phase 04 vẽ art 5 lá tân thủ (từ concept quái); phase 07 deploy kèm card-ids.json mới.

## Bổ sung ChatGPT 24/11 11:00 (`docs/reference/world-design/taurus-album-rewards-orin-hint-chatgpt.txt`) — VIỆC TIẾP (03b)
- Album Taurus mốc 3/6/9/12: 150 G + 2 đá / 300 G + 3 đá / 500 G + 5 đá / 1.000 G + 5 đá + danh hiệu "Taurus Card Collector" (game chưa có hệ danh hiệu: lưu cờ / TODO). Tính số loài từng đăng ký, mỗi mốc 1 lần / nhân vật. Card Field Boss nằm NGOÀI 12 lá Album.
- Orin's Guidance: sau 2 trận THUA LIÊN TIẾP Orin đề nghị "Nhận hướng dẫn / Tự thử lại"; nhận = mỗi lượt tự hiện gợi ý (gọi hàm Hint có sẵn, tự highlight), thua tiếp vẫn dùng được; không auto-thắng, không đổi luật / deck.
- Quyết định V4 "phải thắng Orin" giữ (ChatGPT chốt).

### 03b: đã làm (24/11)
- Album: `AlbumRules.Gold/Stones` = 150+2 / 300+3 / 500+5 / 1000+5, bit mốc 4 (`CollectorTitle`) = cờ danh hiệu "Taurus Card Collector" (TODO hiển thị khi có hệ danh hiệu). Từ chối (bit chưa set) nếu vàng / đá chạm trần. `AlbumCount` chỉ đếm 12 lá `IsLive` => Card Field Boss (khi thêm) phải để `IsLive` = false / ngoài 12. Bỏ `Gold10`, bỏ 3 cờ cosmetic cũ.
- Guidance: `OrinGuidance` (Rpg/Cards), đếm chuỗi không-thắng ở client (`DuelClient.Apply`, 1 lần / ván) trong `CardCollection.OrinLosses` (KHÔNG lưu, theo phiên). `CardMasterTalk.AskDuel` hỏi trước câu "Ready?" khi >= 2. Nhận = bit `HelpGuidance` (4) qua `InvOp.CardHelp` có sẵn (zone + offline lưu; schema `cardHelp` max 7; không đổi wire). Có cờ => `CardDuelBattleView.UpdateLesson` gọi `AskHint` mỗi lượt tự do (chữ "Orin's guidance: ..."). Từ chối => chuỗi về 0, hỏi lại sau 2 lần không thắng nữa.
- Ảnh: `greybox-shots/phase03b-*` (offer, lượt tự gợi ý, thanh mốc Album).

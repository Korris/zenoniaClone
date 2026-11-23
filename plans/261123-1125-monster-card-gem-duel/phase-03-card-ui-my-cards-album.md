# Phase 03: UI My Cards + Monster Album

## Context Links
- Mockup: `docs/reference/card-gem-ui/01-card-menu.png`, `02-card-album.png`
- Luật UI: `docs/ui-layout-rules.md` (BẮT BUỘC đọc hết trước khi làm), asset: phase 02
- Mẫu cửa sổ RO Soft có sẵn: `Assets/Scripts/PetRoWindow*.cs`, `PetRoWidgets.cs`, `UiScreens.cs` (Register / BindNpc), `RoHudMenu.cs` (cột More)
- Dữ liệu: phase 01 (`CardTable`, `CardCollection`)

## Overview
P1, pending. Cửa sổ Card có menu trái chung 6 mục (My Cards / Monster Album / Convert to Soul Gem / Gem Socketing / Gem Removal / Card Duel); phase này làm 2 trang đầu, 4 mục kia hiện nhưng mở trang "sắp có" cho tới phase 04/05. Thêm nút Claim mốc album, bán card cho NPC (câu hỏi 2 plan.md).

## Key Insights
- Đã có hệ mở cửa sổ duy nhất `UiScreens`: thêm `UiScreen.Card`, cửa sổ tự Register; mở từ cột More của `RoHudMenu` (không thêm nút thường trực HUD, theo "Đã chốt").
- Số hướng / element / option hiện từ `CardTable`, không chép từ mockup (mockup lệch số giữa các màn).
- Card ít trường (đếm theo id) nên lưới lấy từ `CardTable.All` lọc theo đã sở hữu: rẻ, không cần pool lớn.

## Requirements
- My Cards: lọc độ hiếm (All/N/R/SR/SSR), vùng, category (ATK/DEF/HP/Utility); "Card Collection x/40"; ô card có chân dung, khung độ hiếm, 4 số hướng trên mặt card, xN, tag In Deck; chi tiết: Element, Source (vùng + quái), Owned, Duel Stats 4 hướng, Card Effect (option Power/Guard), mô tả; nút Add to Deck (phase 05 bật), Sell (bán NPC, xác nhận), View in Album.
- Album: tiến độ x/40, thanh mốc 10/20/30/40 + Claim, tab vùng, quái chưa có = chân dung tint tối + dấu "?", chi tiết quái + Duel Stats + Card Effect.
- Toast nhận card (event phase 01) dùng khung toast phase 02.
- Constraints: chỉ dựng bằng asset phase 02; padding / gap / 9-slice / căn giữa theo `ui-layout-rules.md` mục 1-2; chụp trong game so mockup trước khi báo xong.

## Architecture
```
Assets/Scripts/CardWindow.cs            vỏ cửa sổ, menu trái, đổi trang, Register UiScreen.Card
Assets/Scripts/CardWindow.MyCards.cs    lưới + lọc + chi tiết
Assets/Scripts/CardWindow.Album.cs      tab vùng, mốc, Claim
Assets/Scripts/CardWidgets.cs           CardFace (khung+chân dung+4 số+xN+tag), RarityBadge, MilestoneBar
Assets/Scripts/CardArt.cs               Resources.Load đường dẫn asset card / chân dung, cache
Assets/Scripts/CardDropToast.cs         toast nhận card
```
Luồng: mở cửa sổ -> đọc `PlayerRpg.State.Cards` (client mirror `ClientRpgMirror` online) -> vẽ. Claim / Sell -> `InvOp.AlbumClaim` (Slot = mốc 0-3) / `InvOp.CardSell` (ItemUid = cardId, Slot = số lượng) -> server `ServerInventoryActions` -> `PlayerRpg.ApplyInvAction` -> `AlbumRules.Claim` / `CardOps.Sell` -> `RpgState` -> cửa sổ vẽ lại.

## Related Code Files
Tạo: các file ở Architecture; `Assets/Scripts/Rpg/Cards/CardOps.cs` (Sell, giá theo rarity N 50 / R 200 / SR 800 / SSR 2.500 G, đề xuất), `Tests/dotnet/ZenoniaSkills.Tests/CardSellAlbumClaimTests.cs`.
Sửa: `UiScreens.cs` (enum Card), `RoHudMenu.cs` (mục Cards trong More), `NetOpcodes.cs` (`InvOp.AlbumClaim = 30`, `CardSell = 31`, `InvOpInfo.Last`), `Assets/Scripts/World/PlayerRpg.cs` (switch), `Net/Protocol/TradeBook.cs` (`BlocksInventoryOp` chặn 2 op mới khi đang trade).

## Implementation Steps
1. Đọc hết `docs/ui-layout-rules.md`; kiểm asset phase 02 đủ cho 01/02.
2. `CardArt` + `CardWidgets.CardFace` (prefab dựng bằng code từ sprite có sẵn, LayoutGroup, hằng số padding).
3. Vỏ `CardWindow` + menu trái 6 mục; mục chưa làm hiện trang trống "Coming soon".
4. Trang My Cards, rồi Album.
5. Op `AlbumClaim`, `CardSell` + logic thuần + test (claim 2 lần bị từ chối; bán khi lá đang trong deck bị từ chối, phase 05 nối).
6. Toast nhận card.
7. Chụp 1280x720 + mobile, so mockup, đo padding (mục 2 ui-layout-rules).

## Todo List
- [ ] CardArt / CardWidgets   - [ ] CardWindow + menu trái   - [ ] My Cards   - [ ] Album + Claim
- [ ] InvOp AlbumClaim / CardSell + test   - [ ] Toast   - [ ] Ảnh so mockup

## Success Criteria
- Mở từ HUD More -> Cards; Esc đóng; hero đứng yên khi mở (modal như cửa sổ khác).
- Đếm x/40 đúng album; Claim mốc 10 cộng đúng 5.000 G một lần (test + online).
- Ảnh trong game so mockup: không chữ chạm viền, lệch padding <= 2 px.
- Grep không có khung / nút vẽ bằng code.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Thiếu asset khi dựng | TB x TB | xin thêm ChatGPT, không vẽ tạm bằng code |
| Mirror client lệch server | Thấp x TB | mọi op trả `RpgState` đầy đủ (mẫu sẵn) |
| Trang nặng trên mobile | Thấp x Thấp | 40 ô, cache sprite |

Rollback: bỏ mục Cards khỏi `RoHudMenu`; dữ liệu không ảnh hưởng.

## Security Considerations
Claim / Sell chỉ ở server, kiểm mốc đạt + chưa nhận, số lượng <= sở hữu - đang khoá. Không tin số Gold từ client.

## Next Steps
Phase 04 (3 trang ngọc), phase 05 (Add to Deck, trang Card Duel). Cần build lại zone (InvOp mới, Version bump).

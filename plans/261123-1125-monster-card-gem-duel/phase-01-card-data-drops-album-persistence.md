# Phase 01: Dữ liệu card, rơi card, Album, lưu

## Context Links
- Spec: `docs/reference/card-gem-ui/design-notes-chatgpt.md`
- Quái: `Assets/Scripts/MonsterDef.cs` (47 quái, `MonsterDef.All`), vùng: `Assets/Scripts/MapSpawnTable.cs`
- Rơi: `Assets/Scripts/World/KillRewards.cs`, `Assets/Scripts/Rpg/LootTable.cs`
- Lưu: `Assets/Scripts/Rpg/SaveSerializer.cs` (`SaveVersion = 5`), `SaveStore.Migration.cs`, `server-api/src/rpg/item-validation.js` (`stateSchema`), `server-api/src/rpg/rpg-constants.js`

## Overview
P1, pending. Bảng 40 card thuần C#, rơi card khi giết quái (zone quyết), Album ghi lần đầu sở hữu + 4 mốc Claim, lưu trong state nhân vật client + API.

## Key Insights
- `MonsterDef` nằm ngoài asm Rpg, nên bảng card chỉ tham chiếu id chuỗi (`meadowSlime`...), có test map id <-> `MonsterDef` ở phía Unity (debug check lúc khởi động) và test thuần trong `ZenoniaSkills.Tests` với danh sách id cứng.
- Rơi theo mẫu Pet Food (`AccountFoodDrop`): cộng thẳng cho mỗi người góp sát thương, KHÔNG thả xuống đất -> không đổi `NetLootCodec`.
- Card V1 không có level / thuộc tính riêng -> lưu đếm theo id (`cards: [{id, n}]`), không uid từng lá. Ngọc tương tự (`gems: [{id, kind, locked, n}]`).
- API strip key lạ (`stateSchema`): mọi field mới PHẢI khai báo trong zod, nếu không relog là mất.
- `MonsterDef` hiện có 35 quái thường + 8 boss map + 4 Wanted.

## Requirements
- 40 card: id, monsterId, name, region, rarity N/R/SR/SSR, isBoss (vương miện), element, 4 số hướng (trên/phải/dưới/trái 1-9), category (ATK/DEF/HP/Utility), powerOption + guardOption (stat + giá trị theo rarity), mô tả 1 dòng.
- Tổng 4 số theo rarity: N 14-18, R 18-22, SR 22-26, SSR 26-30 (đề xuất, chỉnh ở bảng).
- Option ngọc đề xuất: ATK N6/R12/SR20/SSR32; DEF N4/R8/SR14/SSR22; HP N40/R80/SR140/SSR220; Utility: crit N1/R2/SR3/SSR5 % hoặc maxMp N15/R30/SR50/SSR80.
- Rơi: mỗi người góp >= 1 % máu (như `AccountRewards.IsValidBossShare`) roll riêng; tỉ lệ theo rarity N 1 % / R 0,5 % / SR 0,2 % / boss 3 %; Wanted Boss chỉ khi là boss của ngày.
- Album: bit đã sở hữu (không mất khi bán / đổi / cược), mốc 10 = 5.000 G, 20 = danh hiệu Card Hunter, 30 = khung chân dung, 40 = Monster Master (chỉ cờ, xem câu hỏi 9 plan.md). Claim 1 lần / mốc.
- Giới hạn 999 / id card, 999 / loại ngọc.

## Architecture
```
Assets/Scripts/Rpg/Cards/
  CardDef.cs            struct dữ liệu + enum CardRarity, CardCategory, GemKind
  CardTable.cs          40 dòng, Get(id), All, Index(id), Regions
  CardDrops.cs          Roll(monsterId, isBoss, rand) -> cardId | null
  CardCollection.cs     đếm card/ngọc, album bitmask, claim mốc; ToSave/FromSave
  AlbumRules.cs         mốc 10/20/30/40, Claim(...) -> ActionResult
Assets/Scripts/Rpg/PlayerState.Cards.cs   PlayerState.Cards (CardCollection), AddCard(id)
```
Save (`SaveData`, JsonUtility): `cards:[{id,n}]`, `gems:[{id,kind,locked,n}]`, `album:[int idx]`, `albumClaimed:int`, `cardDeck:[string]` (phase 05 dùng), `cardDaily:{day,npcWins,pvpRewards,wagers}`, `cardNpcFirstWins:int`, `cardRank:{...}` (phase 07). Item thêm `sockets:[string]` ("meadowSlime:p"), phase 04 dùng. Thêm hết ở phase này để chỉ bump `SaveVersion` 6 một lần.

Luồng: `KillRewards.Resolve` -> với mỗi Winner hợp lệ `CardDrops.Roll` -> `Winners[i].Owner.CardDrop(id)` (PlayerRpg) -> `State.AddCard` -> `NetEventBus` toast "Card: Meadow Slime" -> `RpgDirty`; card SR+ thì `RequestSave` ngay.

## Related Code Files
Tạo: `Assets/Scripts/Rpg/Cards/{CardDef,CardTable,CardDrops,CardCollection,AlbumRules}.cs`, `Assets/Scripts/Rpg/PlayerState.Cards.cs`, `Tests/dotnet/ZenoniaSkills.Tests/{CardTableTests,CardDropTests,CardCollectionTests,CardIdsExportTests}.cs`, `server-api/src/rpg/card-ids.json`, `server-api/test/card-state-schema.test.js`.
Sửa: `Assets/Scripts/Rpg/Item.cs` (sockets), `SaveSerializer.cs`, `SaveStore.Migration.cs`, `Assets/Scripts/World/KillRewards.cs`, `Assets/Scripts/World/PlayerRpg.cs` (CardDrop), `Assets/Scripts/Net/NetEventBus.cs` + `Net/Protocol/NetEventTypes.cs` (event CardDrop), `NetOpcodes.cs` (Version 27), `server-api/src/rpg/item-validation.js`, `rpg-constants.js`, `server-api/src/db/indexes.js` (chỉ comment state mới).

## Implementation Steps
1. Lập bảng 40 card (xem câu hỏi 3): rà `MapSpawnTable.cs` để gán region đúng; rarity theo itemLevel quái (<=10 N, 11-20 R, 21+ SR), boss SSR. Đặt số hướng theo tổng rarity, mỗi card có 1 hướng mạnh rõ ràng.
2. Viết `CardDef`, `CardTable` (thuần, không Unity), `CardDrops`, `CardCollection`, `AlbumRules`.
3. `PlayerState.Cards.cs` + nối `SaveSerializer` (Build/Restore), `SaveVersion` 6, migration offline: save cũ = rỗng.
4. `KillRewards.Resolve`: thêm roll card cho mỗi Winner (sau `PartyKillShare.Pay`). Bỏ qua `WasCaptured`.
5. Event CardDrop (`NetEventType`), client hiện toast; offline đi cùng đường.
6. server-api: zod cho mọi field mới (default rỗng), `sockets` trong `itemSchema` (max 2, regex `^[a-zA-Z]+:[pg]$`), id card phải thuộc `card-ids.json`.
7. `CardIdsExportTests`: so `CardTable` với `server-api/src/rpg/card-ids.json` (id + rarity + isBoss); lệch thì fail kèm lệnh ghi lại.
8. Build lại zone server (bản sao) + deploy server-api (`docs/deploy.md`).

## Todo List
- [ ] Bảng 40 card + region
- [ ] CardDef / CardTable / CardDrops / CardCollection / AlbumRules
- [ ] PlayerState.Cards + SaveSerializer v6 + migration
- [ ] KillRewards roll + event toast
- [ ] zod schema + card-ids.json + vitest
- [ ] dotnet tests (bảng, rơi, đếm, album, export)
- [ ] Build zone + deploy API

## Success Criteria
- 40 id duy nhất, mỗi id có `MonsterDef` thật, 4 số 1-9, tổng đúng dải rarity (test).
- 10.000 lần roll với RNG cố định cho tỉ lệ trong ±10 % mục tiêu (test).
- Save -> load (C#) và save -> API -> load giữ nguyên cards/gems/album/sockets (test + vitest).
- Save cũ v5 load được, card rỗng; save v6 qua API không mất field.
- Online: giết Meadow Slime đủ lần thấy toast, relog vẫn còn card.

## Risk Assessment
| Rủi ro | Khả năng x Tác động | Giảm thiểu |
|---|---|---|
| Field mới bị zod strip -> mất card khi relog | TB x Cao | vitest round-trip mọi field |
| JSON vượt 60 KB | Thấp x Cao | lưu đếm, max 40 dòng card + 80 dòng ngọc |
| Lệch id C# / node | TB x TB | CardIdsExportTests |
| Rơi quá nhiều/ít | Cao x Thấp | hằng số trong `CardDrops`, chỉnh không cần migration |

Rollback: tắt roll trong `KillRewards` bằng hằng số `CardDrops.Enabled`; field state giữ lại (vô hại).

## Security Considerations
Rơi chỉ ở zone; client không có op thêm card. API chỉ nhận state từ zone (service key). Log rơi SR+ ra server log.

## Next Steps
Phase 03 (UI), 04 (ngọc), 05 (deck dùng `cardDeck`).

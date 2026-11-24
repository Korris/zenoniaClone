# Phase 05: Data Card / Pet / loot + portrait

## Context Links
- `Assets/Scripts/Rpg/Cards/CardTableData.cs`, `CardTable.cs` (PetDef), `Rpg/PetConfig.cs`, `Rpg/LootTable.cs`, `server-api/src/rpg/card-ids.json`, `server-api/src/rpg/item-validation.js`, `server-api/test/card-state-schema.test.js`
- `docs/sprite-rules.md` mục "Pet"; chat asset card Tay https://chatgpt.com/c/6b03c3de-7f2c-83ec-b6f2-0b27b06a2617 ; chat pet "Create Pet Sprite Sheet" (Tay, Recents)

## Overview
P2. Card Taurus "cân bằng, dễ dùng"; Card và Pet cùng nhận diện hình với quái nhưng độc lập (bible III.3).

## Implementation Steps
1. Card: đổi tên + mô tả meadowSlime / thornBoar / brambleTreant / thornheartTreant; thêm 3 dòng MỚI (mossMushroom, waterSlime, reedFrog) N, region `willowbrookMeadows`, 4 cạnh theo production sheet (tổng trong dải N, 1 cạnh mạnh nhất). Append cuối danh sách nếu thứ tự có ý nghĩa (kiểm `CardCollection` lưu theo id hay index). Cập nhật số "40 lá" ở README / album.
2. server-api: thêm 3 id vào `card-ids.json`, chạy test server-api.
3. Loot: `LootTable` thêm `MonsterLootDef` 3 quái (EXP 14L, gold L..2L+1); restat thornBoar theo Lv mới.
4. Portrait card: 8 ảnh (5 vẽ lại theo thiết kế mới, 3 mới) trong chat asset card, cắt vào `Resources/Sprites/monsters/card-portraits/<id>.png`; màu đúng màu trong game (Sổ lỗi 23/11 portrait tím).
5. Pet: Horn Rabbit, Thorn Boar (đổi tên "Bramble Boar") vẽ lại art tĩnh `Sprites/pets/<ArtId>/art.png` + icon theo sheet mới; Clover Slime pet chỉ nếu người dùng duyệt (câu hỏi 5). Không làm bắt pet ngoài đồng (YAGNI, game dùng gacha).

## Todo
- [ ] CardTableData + card-ids.json
- [ ] LootTable
- [ ] 8 portrait
- [ ] Pet art 2 (+1)
- [ ] dotnet test + server-api test

## Success Criteria
Quái mới rơi card, album hiện đủ, portrait cùng màu sheet trong game; `CardCollectionTests`, `card-state-schema.test.js` qua.

## Risk
- Save cũ có card id cũ: id giữ nguyên nên an toàn.
- Card tutorial deck / `RewardCards` dùng thornBoar, hornRabbit: đổi tên hiển thị không ảnh hưởng.

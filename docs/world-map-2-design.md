# World Map 2.0 / 2.1 (thiết kế)

> **2.1 (13/11/2026) thay các chỗ khác bên dưới:** xem mục "World Map 2.1" ở cuối file: Snowcrest Pass → Sunbreak Foothills, bảng cỡ map + số quái + channel chốt.

Ngày 13/11/2026. Người dùng nhờ ChatGPT thiết kế lại hệ thống map, làng và bãi farm thành một tuyến hoàn chỉnh Lv1–60, quái + rơi đồ T0–T9, làng nào cũng có người bán, có một làng làm chợ giao dịch giữa người chơi. Chat ChatGPT (tài khoản Tay): https://chatgpt.com/c/6af5faea-f11c-83ec-9e09-3c13537b61a2 . Câu trả lời gốc đầy đủ: `docs/reference/world-map-2-chatgpt-answer.txt`. Ảnh 9 map hiện tại: `docs/reference/world-maps-current-contact-sheet.png`.

## Hiện trạng trước khi đổi

- Liên kết: Oakhaven → (nam) Frost Hollow Lv1–9 → Frostfang Den (Lv2, boss Glacier Warden Lv20) → Sunspire Oasis (làng) → Amber Dunes Lv21–26 → Emberfen Marsh Lv28–34 → Sunken Reliquary (Lv34, boss Mirefang Hydra Lv35). Oakhaven → (đông) Thatchwick (làng) → Willowbrook Meadows Lv1–6.
- Lỗi: 2 map băng liền nhau mà nhảy Lv9 → Lv20; 2 bãi khởi đầu cùng Lv1–6; không có gì trên Lv35; chỉ Oakhaven (healerNina, merchantDale) và Sunspire (apothecaryNara) bán thuốc; không làng nào bán trang bị; chưa có chợ.
- Luật đồ: cấp mặc theo tier `EquipLevel.ByTier` = T0 1, T1 5, T2 10, T3 15, T4 20, T5 30, T6 35, T7 40, T8 50, T9 60; quái rơi đồ theo `ItemLevel` của nó (tier = itemLevel / 10). Cấp tối đa 60.

## 6 vùng, mỗi vùng 10 cấp

| Vùng | Cấp | Làng (mở) | Map farm 1 | Map farm 2 | Dungeon (boss) | Boss map |
|---|---|---|---|---|---|---|
| 1 Verdant Frontier | 1–10 | Oakhaven mới (Lv1, bán T0–T2) | Willowbrook Meadows (cũ) 1–5: meadow slime, horn rabbit, thorn boar | Bramblewood Forest (mới) 6–10: mantis, bramble treant, forest wolf, thorn sprite | Rootburrow (mới), Thornheart Treant Lv10 | Thornfang Alpha Lv10 |
| 2 Frostbound Highlands | 11–20 | Thatchwick (Lv10, bán T2–T4, CHỢ CHÍNH) | Frost Hollow (cũ, đổi cấp) 11–15: frost slime, snow fox, ice imp, frost bear | Snowcrest Pass (mới) 16–20: ice wraith, frostbite yeti, crystal golem, snow harpy | Frostfang Den (cũ), Glacier Warden Lv20 | Crystal Golem King Lv20 |
| 3 Sunscorch Desert | 21–30 | Sunspire Oasis (Lv20, bán T4–T5) | Amber Dunes (cũ) 21–25 | Scorpion Gorge (mới) 26–30: giant scorpion, sand wraith, rock basilisk, dune golem | Sunspire Vault (mới), Sandglass Djinn Lv30 | Dune Devourer Lv30 |
| 4 Blighted Wetlands | 31–40 | Mirewatch (mới, Lv30, bán T5–T7) | Emberfen Marsh (cũ, đổi cấp) 31–35 | Ruined Causeway (mới) 36–40: mire cultist, ancient guardian, bog wraith, ruin serpent | Sunken Reliquary (cũ), Mirefang Hydra Lv40 | Rotwood Colossus Lv40 |
| 5 Ashen Frontier | 41–50 | Ashforge (mới, Lv40, bán T7–T8) | Ashfall Plains (mới) 41–45: ash wolf, cinder slime, fire imp, charred treant | Obsidian Crater (mới) 46–50: magma golem, lava serpent, infernal knight, ember drake | Molten Crucible (mới), Cinder Titan Lv50 | Magma Behemoth Lv50 |
| 6 Astral Highlands | 51–60 | Starfall Citadel (mới, Lv50, bán T8–T9) | Crystal Highlands (mới) 51–55: crystal wolf, arcane sprite, prism golem, sky harpy | Starfall Sanctuary (mới) 56–60: astral wraith, void knight, fallen seraph, star golem | Astral Spire (mới), Void Seraph Lv60 | Astral Guardian Lv60 |

- Gear farm theo map (ChatGPT): 1–5 T0–T1, 6–10 T1–T2, 11–15 T2–T3, 16–20 T3–T4, 21–25 T4, 26–30 T4–T5, 31–35 T5–T6, 36–40 T6–T7, 41–45 T7, 46–50 T7–T8, 51–55 T8, 56–60 T8–T9. T9 chủ yếu từ boss / endgame Lv60, không cho farm tràn lan ở Lv56.
- Lưu ý của ChatGPT: itemLevel của món đồ là thuộc tính riêng, không nhất thiết bằng cấp quái (vd quái Lv30 có thể rơi T5 itemLevel 50–59). Cần chốt lại với `LootTable` khi làm (hiện itemLevel gắn theo quái).

**Cỡ map** (người dùng chốt 13/11/2026): map farm và dungeon làm TO như Willowbrook Meadows, 72x48 ô (ảnh nền chỉ đất + prop riêng, mở rộng bằng outpaint, theo `docs/map-design-rules.md`). Làng giữ 36x24, một ảnh liền như Thatchwick.

## Làng

Làng nào cũng có đủ (dùng chung UI và code, mỗi làng chỉ khác sprite NPC, bảng hàng, chỗ đứng): bán thuốc HP/MP theo vùng, bán trang bị cơ bản theo tier của vùng, thợ rèn (cường hoá / nâng cấp), thủ kho (kho chung tài khoản), bảng nhiệm vụ + Wanted, NPC dịch chuyển tới các làng đã tới.

**Chợ chính: Thatchwick.** Lý do: làng cấp thấp nhưng không phải chỗ tân thủ vừa vào, nằm giữa 2 nhánh khởi đầu và đường lên vùng cao; quảng trường đất rộng, ít vật cản, sửa được mà không vẽ lại nền. Quảng trường giữa = Player Market Square, 12–16 chỗ mở sạp; phía tây Trade Broker + Auction Board (làm sau); phía đông kho / ngân hàng; phía bắc NPC dịch chuyển; không có quái, không PvP. Giai đoạn đầu chỉ cần trade trực tiếp (đã có) + sạp đơn giản, chưa cần đấu giá toàn server. Sau Lv10 nhiệm vụ dẫn người chơi tới Thatchwick gặp NPC thương mại.

## Wanted boss (giữ 3 con)

Rimeclaw Tyrant Lv10 ở Frost Hollow; Glacial Colossus Lv25 ở Frostfang Den; Mire Hydra Queen Lv40 ở Sunken Reliquary. ChatGPT đề xuất bảng Wanted ở mọi làng đưa người chơi tới arena đã mở (đang là boss đứng trên map chung, xem `docs/boss-hunter-pending.md`).

## Sơ đồ nối (rút gọn)

```
OAKHAVEN ──đông── THATCHWICK (chợ)
   │nam               │bắc
Willowbrook 1–5    Frost Hollow 11–15
   │nam               │bắc
Bramblewood 6–10   Snowcrest Pass 16–20 ──bắc── Frostfang Den (dungeon)
   └─ Rootburrow      │đông
                   SUNSPIRE → Amber Dunes 21–25 → Scorpion Gorge 26–30 (+ Sunspire Vault)
                      → MIREWATCH → Emberfen Marsh 31–35 → Ruined Causeway 36–40 (+ Sunken Reliquary)
                      → ASHFORGE → Ashfall Plains 41–45 → Obsidian Crater 46–50 (+ Molten Crucible)
                      → STARFALL CITADEL → Crystal Highlands 51–55 → Starfall Sanctuary 56–60 (+ Astral Spire) → endgame
```

**Không có boss chặn cổng** (người dùng chốt 13/11/2026): mọi cổng nối map luôn mở, không phải giết boss map hay boss dungeon mới qua được map / vùng sau. Dungeon và boss là nội dung phụ để farm đồ. Cấp quái tự giới hạn người chơi đi quá sớm. Cũng KHÔNG có "nhiệm vụ mở đường" (người dùng bỏ 13/11): không có điều kiện nào để qua cổng; mốc "mở khoá" của làng chỉ là cấp gợi ý.

Code hiện tại còn một chỗ chặn kiểu này: cổng Frostfang Den → Sunspire chỉ mở sau khi giết Glacier Warden (`MapSpawnTable.HasBossGate`, `FrostfangDenMap.Portals`, `RpgSession.BossDefeatedOn`, toast "The way forward is open" ở `NetPlayer`). Phải gỡ khi làm bước 1.

## Khối lượng

Giữ cả 9 map (3 làng, 4 farm, 2 dungeon). Phải làm mới: 3 làng (Mirewatch, Ashforge, Starfall Citadel) + 8 map farm + 4 dungeon, cộng quái mới cho các map đó và Oakhaven mới (đã có bản vẽ `docs/reference/new-oakhaven-village-concept-1.png`).

## Câu hỏi còn mở

- Thứ tự làm (đề xuất): (1) đổi cấp + nối lại 9 map cũ, Oakhaven mới, NPC bán thuốc + trang bị ở cả 3 làng cũ, chợ Thatchwick; (2) vùng 1–2 cho đủ Lv1–20 (Bramblewood, Snowcrest Pass, Rootburrow); (3) các vùng sau.
- Willowbrook và Frost Hollow đổi cấp thì quái hiện có (đang Lv1–9) phải nâng chỉ số + itemLevel; Frost Hollow là map khởi đầu của client online (`GameSession.Online`), phải đổi chỗ xuất phát sang Oakhaven.
- Wanted boss: giữ đứng trên map chung hay đổi sang arena riêng như ChatGPT đề xuất.

## World Map 2.1 (13/11/2026, người dùng chốt)

Người dùng: "không nên design tới 3 map băng, giống trùng lặp; map to 72 hay village to thì cứ theo ChatGPT design, đây là game online nhiều người chơi". Câu trả lời gốc: `docs/reference/world2-region2-and-map-sizes-chatgpt-answer.txt`.

- **Snowcrest Pass bỏ, thay bằng Sunbreak Foothills (Lv16–20):** cao nguyên chân núi khô, đồng cỏ vàng olive, đá xám nâu, thông thưa, suối tuyết tan, mỏm đá chuyển dần sang sa thạch (chuyển tiếp tuyết → sa mạc). Màu: cỏ #8B966B, đất #B6A285, đá #827A71, bóng #48575A, nắng #C4B27E. Quái: Highland Ibex Lv16 (dê núi húc), Dustscale Lizard Lv17 (thằn lằn đá lao nhanh), Cliff Harpy Lv18 (harpy nâu xám bắn lông), Stoneback Guardian Lv19–20 (quái đá đập đất); boss map Ridgehorn Behemoth Lv20 (dê núi khổng lồ bọc đá, húc, đá rơi). Crystal Golem King giữ làm elite hiếm / boss sự kiện ở Frost Hollow; Snow Harpy giữ ở Frost Hollow hoặc Frostfang Den. Frostfang Den là dungeon nhánh, không còn 3 map băng liền.
- Vùng khác không trùng biome, nhưng khi vẽ Scorpion Gorge, Ruined Causeway, Starfall Sanctuary phải khác rõ hình khối / vật liệu / màu với map trước.

| Map | Loại | Cấp | Cỡ | Quái sống / channel | Hồi quái | Người / channel |
|---|---|---|---|---|---|---|
| Oakhaven | làng | 1+ | 36x24 | 0 | — | 40 |
| Willowbrook Meadows | farm | 1–5 | 72x48 | 90 | 8–12 s | 24 |
| Bramblewood Forest | farm + Thornfang Alpha | 6–10 | 36x24 | 30 + boss | 6–9 s | 10 |
| Rootburrow | dungeon | 10 | 36x24 | 16 | không hồi | 5 |
| Thornheart Chamber | phòng boss | 10 | 24x18 | 1 | theo lượt | 5 |
| Thatchwick | chợ | 10+ | 48x32 | 0 | — | 50 |
| Frost Hollow | farm | 11–15 | 72x48 | 90 | 8–12 s | 24 |
| Sunbreak Foothills | farm + Ridgehorn Behemoth | 16–20 | 48x32 | 50 + boss | 7–10 s | 16 |
| Frostfang Den | dungeon | 20 | 36x24 | 18 | không hồi | 5 |
| Glacier Warden Chamber | phòng boss | 20 | 24x18 | 1 | theo lượt | 5 |
| Sunspire Oasis | làng | 20+ | 36x24 | 0 | — | 40 |
| Amber Dunes | farm | 21–25 | 36x24 | 30 | 6–9 s | 10 |
| Scorpion Gorge | farm + Dune Devourer | 26–30 | 48x32 | 50 + boss | 7–10 s | 16 |
| Sunspire Vault | dungeon | 30 | 36x24 | 18 | không hồi | 5 |
| Sandglass Djinn Chamber | phòng boss | 30 | 24x18 | 1 | theo lượt | 5 |
| Mirewatch | làng | 30+ | 36x24 | 0 | — | 40 |
| Emberfen Marsh | farm | 31–35 | 36x24 | 30 | 6–9 s | 10 |
| Ruined Causeway | farm + Rotwood Colossus | 36–40 | 48x32 | 50 + boss | 7–10 s | 16 |
| Sunken Reliquary | dungeon | 40 | 36x24 | 18 | không hồi | 5 |
| Mirefang Hydra Chamber | phòng boss | 40 | 24x18 | 1 | theo lượt | 5 |
| Ashforge | làng | 40+ | 36x24 | 0 | — | 40 |
| Ashfall Plains | farm | 41–45 | 48x32 | 50 | 7–10 s | 16 |
| Obsidian Crater | farm + Magma Behemoth | 46–50 | 48x32 | 50 + boss | 7–10 s | 16 |
| Molten Crucible | dungeon | 50 | 36x24 | 18 | không hồi | 5 |
| Cinder Titan Chamber | phòng boss | 50 | 24x18 | 1 | theo lượt | 5 |
| Starfall Citadel | làng | 50+ | 36x24 | 0 | — | 40 |
| Crystal Highlands | farm | 51–55 | 72x48 | 90 | 8–12 s | 24 |
| Starfall Sanctuary | farm + Astral Guardian | 56–60 | 48x32 | 50 + boss | 7–10 s | 16 |
| Astral Spire | dungeon cuối | 60 | 48x32 | 24 | không hồi | 5 |
| Void Seraph Chamber | phòng boss cuối | 60 | 30x20 | 1 | theo lượt | 5 |
| 3 đấu trường truy nã (Rimeclaw 10, Glacial 25, Hydra Queen 40) | instance | | 24x18 | 1 | theo lượt | 1–5 |

- 3 map 72x48: Willowbrook (đông tân thủ nhất, đã có), Frost Hollow (đã có), Crystal Highlands (farm cuối game, party). Map 36x24 đã vẽ lại (Amber Dunes, Emberfen, Sunken Reliquary) và Bramblewood giữ 36x24, không kéo giãn; bù sức chứa bằng channel.
- Channel: farm 36x24 mềm 8 / cứng 10 người, 48x32 12 / 16, 72x48 20 / 24; làng 40, Thatchwick 50; dungeon và phòng boss là instance riêng theo party. Server tự mở channel khi đông, ưu tiên ghép party, đóng channel vắng; chat, guild, trade, dữ liệu nhân vật dùng chung, chỉ quái + combat tách theo channel.
- Hồi quái trong bảng là gốc, server giảm còn 5–6 s khi đông. Boss map ra theo timer riêng 10–15 phút, không tính vào quái thường; chặn đổi channel liên tục để ăn boss (loot / quyền nhận kiểm ở server). Dungeon: quái không hồi trong một lượt, boss có lại khi vào lượt mới.
- Chưa có trong code: hệ thống channel, dungeon / phòng boss instance, đấu trường truy nã instance (boss truy nã hiện đứng trên map chung).

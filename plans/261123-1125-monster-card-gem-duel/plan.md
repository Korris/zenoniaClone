---
title: "Monster Card: rơi card + Album, Soul Gem (chuyển hoá / khảm / tháo), Card Duel 4x4 (NPC / PvP / Ranked / cược)"
description: "Hệ card quái V1 theo spec design-notes-chatgpt.md, server quyết hết, luật chung C# có unit test"
status: pending
priority: P2
effort: 70h
branch: ro-soft-adventure
tags: [card, gem, duel, pvp, ranked, server, ui]
created: 2026-11-23
---

# Monster Card + Soul Gem + Card Duel (V1)

Spec: `docs/reference/card-gem-ui/design-notes-chatgpt.md` (mục "Đã chốt" thắng mọi chỗ khác). Mockup: `docs/reference/card-gem-ui/01..06b-*.png` (chỉ bố cục; số liệu lấy từ bảng dữ liệu, không từ ảnh).

## Kiến trúc chốt
- Luật thuần C# ở `Assets/Scripts/Rpg/Cards/` (bảng card, phí, deck, bàn 4x4, NPC AI, Ranked, cược): client offline, zone server, `ZenoniaSkills.Tests` dùng chung.
- Zone server quyết mọi thứ (rơi, đổi, khảm, lượt, đồng hồ, thưởng); client chỉ gửi yêu cầu (`InvOp` mới + `NetOp.DuelRequest`), nhận `RpgState` / `DuelState`.
- Lưu trong state nhân vật (Mongo `characters.state`, zod `stateSchema`): đếm card / ngọc theo id (card V1 không có thuộc tính riêng: KHÔNG dùng uid từng lá, tránh phình JSON 60 KB). Ngọc đã khảm nằm trong `item.sockets`.
- Luật v2 (23/11 chiều): KHÔNG đấu NPC, KHÔNG thưởng Gold PvP; mọi trận PvP thường thắng lấy 1 card của deck người thua (hoà không mất). Ranked: không lấy card.
- Cược card: escrow thật ở server-api (cả deck 8 lá của hai bên) (collection `card_wagers`, transaction 2 nhân vật như `commitTrade`).
- Mọi icon, khung, nút, nền, chân dung quái, ngọc, badge do ChatGPT vẽ rồi cắt nền trong suốt (phase 02); phase UI 03-08 chỉ dựng bằng các asset đó, KHÔNG vẽ bằng code / script / shape.
- DB: Mongo không có migration script; field mới có default trong zod + `SaveVersion` 6. Mới: collection `card_wagers` (phase 06).

## Phase
| # | Phase | Effort | Phụ thuộc | Build lại server | Trạng thái |
|---|---|---|---|---|---|
| 01 | [Dữ liệu card, rơi card, Album, lưu](phase-01-card-data-drops-album-persistence.md) | 7h | - | Zone + API | code xong 23/11 + sửa review (chưa deploy) |
| 02 | [Xin asset ChatGPT (card, ngọc, nút, bàn đấu)](phase-02-chatgpt-asset-request.md) | 10h | - (song song 01) | Không | done (chờ duyệt) |
| 03 | [UI My Cards + Monster Album](phase-03-card-ui-my-cards-album.md) | 8h | 01, 02 | Zone (InvOp mới) | code + UI xong 23/11 (chưa deploy, chưa kiểm online) |
| 04 | [Chuyển hoá, khảm, tháo ngọc + chỉ số](phase-04-soul-gem-convert-socket-remove.md) | 9h | 03 | Zone + API | code + UI xong 23/11, nối CardWindow xong + sửa review 23/11 (chưa deploy, chưa kiểm online) |
| 05 | [Duel core + UI deck/bàn (đấu NPC đã BỎ 23/11 chiều)](phase-05-duel-core-npc-duel.md) | 13h | 03 | Zone | code + UI xong 23/11; NPC + thưởng NPC xoá ở v2 (chưa deploy) |
| 06 | [PvP thách đấu, "thắng lấy 1 card", escrow cả deck (v2)](phase-06-pvp-duel-card-wager.md) | 10h | 05 | Zone + API | **v2 xong 23/11 chiều** (API 250 vitest, 2 client dev server win / lose / draw / drop / rejoin / nopick; CHƯA deploy, chưa thử API + Mongo thật) |
| 07 | [Ranked mùa 30 ngày](phase-07-ranked-season.md) | 8h | 06 | Zone (+ API: zod cardRank 2 field) | **xong code + UI 23/11 chiều** (2 client dev server ranked / ranked3; CHƯA deploy, chưa thử API thật) |
| 08 | [NPC Card Master, lối vào, hoàn thiện, docs](phase-08-card-master-npc-entry-polish.md) | 5h | 05 (07 cho nút Ranked) | Zone | pending |

Thứ tự phát hành được: 01+03 (sưu tầm) -> 04 (ngọc) -> 05 (đấu NPC) -> 06 -> 07. 08 làm sau 05, nút Ranked bật sau 07.

## Luồng dữ liệu
Quái chết -> `KillRewards.Resolve` -> `CardDrops.Roll` mỗi người góp sát thương -> `PlayerState.AddCard` (+Album) -> event toast + `RpgState` -> save định kỳ -> API zod.
Đổi / khảm / tháo: client `InvOp` -> `ServerInventoryActions` -> `PlayerRpg.ApplyInvAction` -> `CardOps` (thuần) -> `RpgState` + `RequestSave`.
Đấu: `DuelRequest` -> `DuelService` (zone, tick đồng hồ) -> `DuelMatch` (thuần) -> `DuelState` tới 2 bên; xong ván -> thưởng / Ranked / settle cược (API) -> save.

## Rủi ro lớn
1. Cược card mất đồ khi rớt mạng / zone sập: escrow ở API + hoàn khi quá hạn (phase 06).
2. JSON save vượt `MaxSaveJsonLength` 60000: lưu đếm, giới hạn 999 / id.
3. Lệch bảng card C# và node: test dotnet so `server-api/src/rpg/card-ids.json`.
4. Đổi `NetProtocol.Version` nhiều lần: gộp mỗi phase một lần bump, client cũ bị từ chối.
5. Art ChatGPT 40 chân dung + ngọc tốn lượt: chia đợt, dùng tài khoản theo CLAUDE.md.

## Thứ tự deploy (bắt buộc)
Mọi phase có cột "Zone + API": **deploy server-api TRƯỚC, zone SAU** (`docs/deploy.md`, mục Cập nhật). Zone mới gửi save mà API cũ từ chối thì tiến độ chỉ còn trong file kept. Zone log lỗi khi API từ chối 3 save trong 10 phút (`SaveRejectWatch`).

## Kiểm tra chung
`dotnet test Tests/dotnet/ZenoniaSkills.Tests`, `dotnet test Tests/dotnet/ZenoniaNet.Tests`, `cd server-api && npx vitest run`. Build từ bản sao `Tools/build-from-copy.ps1`, không build vào project đang mở.

## Câu hỏi chưa giải quyết
1. Card Master "ở Willowbrook": Willowbrook Meadows là map săn (`Hostile`), luật cấm đấu ở map có quái. Đặt ở Oakhaven cạnh cổng nam sang Willowbrook (mặc định) hay làm góc an toàn trong Willowbrook?
2. Không có chợ trong game: V1 "Sell" = bán cho NPC theo giá cố định (mặc định), nhãn "On Market" ẩn. Có cần chợ thật / cho card vào cửa sổ Trade P2P không?
3. Bộ 40 card: mặc định 35 quái thường + 5 boss (Thornfang Alpha, Frostfang King, Mirefang Hydra, Ambercap Warden, Rimeclaw Tyrant). Tab vùng: mockup 7 vùng, game còn Sunbreak Foothills + Scorpion Gorge (8 quái) -> 9 tab hay gộp?
4. Ô ngọc: game không có slot "phụ kiện" riêng. Mặc định vũ khí 2, giáp (armor) 2, dây chuyền (`helmet`) 1, các slot khác 0. Đúng không?
5. Album, card, ngọc theo nhân vật (mặc định, đơn giản) hay theo tài khoản (như pet)?
6. Tỉ lệ rơi (đề xuất N 1 %, R 0,5 %, SR 0,2 %, boss SSR 3 %) và số option ngọc (bảng ở phase 01) cần duyệt.
7. Deck "Boss 1/1, SR+ 2/2": boss có tính vào 2 lá SR+ không? Mặc định: không.
8. Tay bài 5 lá rút từ deck 8 (theo mockup 06b) hay hiện cả 8? Mặc định 5 + rút bù.
9. Thưởng cosmetic (danh hiệu Card Hunter, khung chân dung, hiệu ứng Monster Master, thưởng mùa Ranked): game chưa có hệ khung / hiệu ứng. V1 chỉ lưu cờ + hiện danh hiệu dưới tên?
10. ~~Style~~ ĐÃ CHỐT 23/11: người dùng giữ RO Soft Fantasy cho bộ card.
11. ~~Ai bật cược~~ THAY THẾ 23/11 chiều: không còn lựa chọn cược; mọi trận PvP thường thắng lấy 1 card. `CARD_WAGER_ENABLED` là công tắc khẩn cấp (tắt = PvP vẫn chơi, không lấy card), mặc định BẬT.

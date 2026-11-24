---
title: "Taurus: vùng khởi đầu theo thiết kế ChatGPT (camp, quái mới, quest tân thủ)"
description: "Đưa Oakhaven + Willowbrook + Bramblewood + Rootburrow thành Tinh Vực Taurus: camp theo địa hình, 3 quái mới, đổi tên/vẽ mới 5 quái, chuỗi quest tân thủ stone + card."
status: cancelled
priority: P2
effort: 34h
branch: ro-soft-adventure
tags: [taurus, map, monsters, camps, chatgpt-art, cards, pets, quests]
created: 2026-11-24
---

# Taurus region: camps + monsters + quest tân thủ

> **SUPERSEDED 24/11/2026.** Người dùng chốt design lại TOÀN BỘ thế giới theo ChatGPT (TAURUS PRODUCTION BIBLE V4.0), KHÔNG giữ map / quái / tên / art cũ. Plan này giữ map cũ nên lỗi thời. Thay bằng `plans/261124-0930-taurus-v4-new-world/plan.md`. Không làm theo file nào trong thư mục này.

Nguồn: `docs/reference/world-design/asteria-world-monster-bible-chatgpt.txt` (mục Taurus), `asteria-villages-vs-hunting-maps-chatgpt.txt`, `asteria-one-big-map-camps-chatgpt.txt` (mô hình CHỐT: 1 khu dân cư + map săn lớn chia camp + 1 dungeon), `docs/monster-redesign-bible.md`, `docs/pending-tasks.md` mục 24/11.

**Bắt buộc trước mọi phase có art / map: đọc hết `docs/sprite-rules.md`, `docs/map-design-rules.md`** (ghi trong brief agent). Art do ChatGPT vẽ (Tay, hết lượt thì Tan Pham), không tự vẽ bằng code.

## Hướng KISS (đề xuất, chốt ở phase 01)
- GIỮ map + id + tên: Oakhaven Village (làng), Willowbrook Meadows (map săn chính, camp 1-4), Bramblewood Forest (phần sâu: Rừng Gai + Gốc Cây Cổ + cửa hang), Rootburrow (= Ancient Root Shrine), Thornheart Chamber (sảnh boss).
- GIỮ id quái, chỉ đổi tên hiển thị + vẽ THIẾT KẾ MỚI: meadowSlime -> Clover Slime, thornBoar -> Bramble Boar (elite, chuyển sang Bramblewood), brambleTreant -> Root Guardian, thornheartTreant -> Elder Root Guardian. hornRabbit giữ tên, vẽ mới.
- MỚI (append `MonsterDef.All`): mossMushroom, waterSlime, reedFrog.
- Quái thừa (mantis, wolf, sprite, Thornfang Alpha, quái Rootburrow, Ambercap Warden) giữ nguyên: quest Rootburrow, card, deck tutorial đang dùng.

## Phase
| # | Phase | Effort | Status | Phụ thuộc |
|---|---|---|---|---|
| 01 | [Chốt Production Sheet với ChatGPT](phase-01-taurus-production-sheet-chatgpt.md) | 3h | pending | - |
| 02 | [Art quái (concept, sheet đi/đánh)](phase-02-taurus-monster-art-chatgpt.md) | 9h | pending | 01 |
| 03 | [Camp / spawn / leash / MonsterDef](phase-03-camps-spawn-leash-monsterdef.md) | 5h | pending | 01 (art TẠM dùng FallbackSheet) |
| 04 | [Props / địa hình camp](phase-04-camp-props-terrain.md) | 7h | pending | 01, 03 |
| 05 | [Data Card / Pet / loot + portrait](phase-05-card-pet-loot-data.md) | 4h | pending | 01, 02 |
| 06 | [Chuỗi quest tân thủ (Elder stone + Orin card)](phase-06-newbie-quest-chain.md) | 3h (+ phần Orin chờ chốt) | pending | 03; Orin chờ người dùng |
| 07 | [Test + build + phát hành cùng server](phase-07-test-build-release.md) | 3h | pending | 02-06 |

## Phụ thuộc chính
- Wire: `MonsterDef.All` append only; quái mới / đổi QuestCatalog -> bump `NetProtocol.Version` (đang 35 chưa phát hành, có thể gộp), build lại zone server (`Tools/build-linux-server.ps1`), server-api `card-ids.json` cùng lúc.
- Willowbrook có 2 bản hình: 2.5D (phát hành) + 3D (mặc định bản test, `Map3D.Maps`): prop mới phải chạy được cả hai.
- Không build / batch vào project đang mở: `Tools/build-from-copy.ps1`.
- Card tutorial Orin (`plans/261124-0317-card-tutorial-quest-npc-duel/`) đã code: phase 06 chỉ nối, phần Orin kiểu mới chờ người dùng chốt.

## Câu hỏi mở
Xem cuối `phase-01-taurus-production-sheet-chatgpt.md`.

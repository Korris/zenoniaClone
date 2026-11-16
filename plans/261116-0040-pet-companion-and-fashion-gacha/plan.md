---
title: "Pet Companion (Thú Đồng Hành) + Gacha Pet V1"
description: "8 pet theo tài khoản (1 nội tại + 1 skill tự dùng 20 s, 1-3 sao bằng mảnh + vàng), gacha pet quay ở server (45/30/17/6/2, pity 10/30/60), vé kiếm trong game."
status: done (V2: hệ RO đầy đủ, docs/pet-system-design.md; protocol 21)
priority: P2
branch: unity
tags: [pet, gacha, account, zone-server, server-api, ui]
created: 2026-11-16
---

# Pet Companion + Gacha Pet V1

Nguồn chốt: `docs/reference/gacha-discussion-chatgpt.md` Lần 5 + **Lần 6** ("CHỐT V1 — GACHA PET", 16/11). Chat Tay https://chatgpt.com/c/6af992f9-0fe0-83ec-92b7-59dad8abf6f2.
Người dùng 16/11: BỎ Rương Thời Trang / cosmetic, gacha là gacha PET. Không tiền nạp. (Thư mục giữ tên cũ `...fashion-gacha` cho khỏi gãy link.)

## Quyết định chính
- **Dữ liệu theo tài khoản** `AccountState`: pet {id, sao, mảnh loài}, vé, Mảnh UR, pity SR/SSR/UR, tổng lượt, lịch sử 200, cờ quà tân thủ, cờ bù pet cũ, mốc cấp đã thưởng, bộ đếm ngày/tuần. Nhân vật chỉ lưu `petId` (SaveData v4).
- **Lưu**: online = server-api `accounts` (1 doc / userId, `rev`, ghi nguyên state + `reqId`, ledger). Zone giữ cache, mọi thay đổi qua hàng đợi nối tiếp `ZoneAccountQueue` (tính trên bản sao -> POST -> thành công mới áp; 409 tải lại tính lại 1 lần). Offline = file `zenonia-account.json`. Dev host không API = RAM.
- **Luật một chỗ** (C# engine-free `Assets/Scripts/Rpg/Account/`, `PetConfig.cs`): pet, sao, gacha, vé, migration. Zone chạy luật + RNG an toàn. JS kiểm giới hạn + chống lùi + lưu.
- **10 pet = quái / boss của game (Lần 7)**: gacha N Frost Slime, Horn Rabbit; R Ice Goblin, Thorn Boar; SR Spectral Wolf, Marsh Stalker; SSR Crystal Golem; UR Frostfang King; Legacy ngoài gacha Spirit, Imp. Mọi pet tự đánh mỗi 3 s (10 %). Nội tại +1 điểm % / sao. Skill cd 20 s, chỉ đánh mục tiêu chủ vừa đánh trúng trong 2 s, tầm 6 ô; sát thương pet không tính vào damage share (loot / EXP / boss hợp lệ).
- **Gacha**: 45/30/17/6/2, chia đều; pity 10 SR+ / 30 SSR+ / 60 UR (mốc cao nhất thắng); x1 / x10. Trùng = 10 mảnh loài; sao 1->2 10 mảnh + 5000 vàng, 2->3 20 mảnh + 10000; lượt không UR +1 Mảnh UR, 120 đổi Frostfang King.
- **Vé**: tân thủ 10; Wanted lần đầu/ngày/tài khoản 1 (<= 4/tuần); boss map hợp lệ lần thứ 5 trong tuần 1; mốc Lv 10..60 lần đầu trên tài khoản 2. Tuần reset 00:00 thứ Hai UTC+7; ngày = `WantedBoss.DayKey`.
- **Không qua gacha**: Spirit Lv 5; Imp 3000 vàng (1 lần); Frostfang King 120 Mảnh UR.
- **Pet cũ**: item pet -> mở 1★ + 10 mảnh loài + 3000 vàng / loài, 1 lần / tài khoản.
- **Hình**: 8 pet quái vẽ ChatGPT từ sprite quái gốc (`Assets/ArtLibrary/pets/pet-monsters-chatgpt-v1.png`), FX skill ChatGPT (`pet-fx-chatgpt-v1.png`). NPC Lumi (Thatchwick) mở gacha; Settings ẩn pet người khác; mốc vé Lv 10..90.
- `NetProtocol.Version` 20: RpgState kèm AccountJson, InvOp mới, `NetEventType.PetSkill`.

## V2 (16/11, người dùng: học toàn bộ hệ RO) — spec `docs/pet-system-design.md`
| # | Phase | Files chính | Trạng thái |
|---|---|---|---|
| 05 | [Pet xuất chiến thật: Walk/Attack sheet quái, FOLLOW→CHASE→ATTACK, đánh 2 s, số màu riêng](phase-05-pet-behaviour.md) | `HeroPet*.cs`, `HeroController.Pet.cs`, `PetSkillFx.cs`, `Monster.PetHit.cs` | done |
| 06 | [Đội hình 1+2+2, Bond, trần bonus; level + Thức Ăn Pet; sao 1–5; reset](phase-06-team-level-stars.md) | `Rpg/PetConfig*.cs`, `Rpg/Account/PetTeam.cs`, `PetGrowth.cs`, `PlayerState.Companion.cs` | done |
| 07 | [Gacha RO: Pet Crystal, 12/24/36/6,25/2,5 + mảnh chung, pity 20/50, migrate pity cũ](phase-07-gacha-ro.md) | `Rpg/Account/PetGacha.cs`, `AccountRewards.cs`, `AccountState.cs` | done |
| 08 | [Bắt pet: bóng thường/hiếm, quái sống <= 50 % HP, xác boss 30/45 s, sổ pet 5 cấp](phase-08-capture-collection.md) | `Rpg/Account/PetCapture.cs`, `World/PetCaptureService.cs`, `Monster.cs`, InvOp 19–25 | done (xác boss chưa xem trong game) |
| 09 | [UI 1 cửa sổ 5 tab + nút bắt trên quái; server-api schema; test; e2e; ảnh; ChatGPT duyệt](phase-09-ui-server-verify.md) | `PetWindow*.cs`, `server-api/*`, `Tests/*`, `Tools/net-persist-test.ps1` | done |

## V1 (đã xong trước đó)
## Phases
| # | Phase | Files chính | Trạng thái |
|---|---|---|---|
| 01 | [AccountState + lưu (offline, server-api, zone)](phase-01-account-state-and-persistence.md) | `Rpg/Account/*`, `server-api/src/*`, `Net/Server/ZoneAccount*.cs` | done |
| 02 | [Pet Companion: 8 pet, sao, skill, migration, UI](phase-02-pet-companion-phase1.md) | `Rpg/PetConfig.cs`, `HeroController.Pet.cs`, `PetSkillFx.cs`, `PetWindow.cs` | done (ảnh tạm) |
| 03 | [Gacha pet: luật, vé, UI rương + tỉ lệ + lịch sử](phase-03-fashion-chest-gacha.md) | `Rpg/Account/PetGacha.cs`, `AccountRewards.cs`, `PetGachaWindow*.cs` | done |
| 04 | [Test, build bản sao, chụp, docs](phase-04-tests-build-verify-docs.md) | `Tests/dotnet/*`, `server-api/test/*`, `Tools/net-persist-test.ps1`, docs | done |

## Phụ thuộc / deploy
01 trước 02, 03. **Deploy server-api trước** (schema v4 + accounts), rồi zone + client cùng build (protocol 20).

## Rollback
Revert sau khi đã migrate = mất pet đã chuyển; phải viết ngược (không làm).

## Câu hỏi còn mở (mặc định trong ngoặc)
1. Bắt quái ngoài map như Ragnarok [V2].
2. Mở gacha ở đâu [phím `G` + nút; NPC sau].
3. Ẩn pet người khác [chưa làm V1].

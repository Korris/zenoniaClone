# Báo cáo: Pet V2 = hệ pet RO Pocket Saga thu gọn (16/11/2026)

Plan: `plans/261116-0040-pet-companion-and-fashion-gacha/` (phase 05–09). Spec chốt: `docs/pet-system-design.md`. ChatGPT: `docs/reference/gacha-discussion-chatgpt.md` Lần 8 (spec) + Lần 9 (duyệt), chat Tay https://chatgpt.com/c/6af992f9-0fe0-83ec-92b7-59dad8abf6f2.

## Diễn biến
1. V1 (sáng 16/11): pet theo tài khoản, 10 pet quái, gacha vé. Người dùng chơi thử: "pet lướt qua lướt lại, không đánh gì" → Lần 8 câu 1: pet đi bằng sheet quái, lao tới đánh, số sát thương riêng.
2. Mở rộng: "học TOÀN BỘ hệ pet + gacha + bắt bóng giống y chang Ragnarok Midgard Senki" → Lần 8 câu 2 (ChatGPT tra Ludens / 4Gamers / LDPlayer / MuMu / Bahamut) → spec V2 → làm hết → Lần 9 duyệt.

## Spec chốt (V2)
- Đội 1 xuất chiến + 2 hỗ trợ skill (50 %) + 2 thuộc tính (25 % nội tại), mở Lv 5/20/30/35/45; 4 Bond +2 %; trần +10 % dmg / +12 % HP / +10 % speed / −10 % dmg nhận.
- Pet xuất chiến: theo sau 1,5 ô, đuổi 160 %, đánh thường 2 s (10 % đòn), skill 20 s, sát thương × (1 + 0,5 %/level); không HP, không aggro, không tính share.
- Level 1–60 bằng Thức Ăn Pet (2 + L/10; quái 8 %, boss 3, Wanted 10/ngày), không vượt hero. Sao 1–5: 10/20/30/40 mảnh loài + 5k/10k/20k/30k vàng, nội tại ×1,0…1,4. Mảnh chung → mảnh loài 1:1. Reset level / sao hoàn 100 % theo sổ đã tiêu.
- Gacha: Pet Crystal (kiếm trong game) 150 = 1 Gacha Coin = 1 lượt; N 12 / R 24 / SR 36 / SSR 6,25 / UR 2,5 + 19,25 % = 5 mảnh chung; trùng 10 mảnh; pity SSR+ 20 (71,43 : 28,57) / UR 50; +1 Mảnh UR mỗi lượt không UR, 120 = Frostfang King. Thưởng: tân thủ 10 coin, Wanted 150 crystal (≤ 4/tuần), boss lần 5/tuần 150, mốc Lv 10..90 300.
- Bắt: Bóng thường 500 G (Dale / Nara, 5/ngày) + 10 % boss; Bóng Hiếm mỗi 28 Wanted (100 %). Quái sống: vừa đánh 10 s, HP ≤ 50 %, 4 ô; N 40/60, R 25/40, SR 12/20, SSR 4/8 %. Xác boss 30 s (SSR 5 / UR 1 %), Wanted 45 s (3 / 0,5 %, chưa có PetDef), 1 lần/tài khoản/lần chết, 1 pet boss/ngày. Sổ 5 cấp (1/3/5/7/10 loài) chặn N-R / SR / SSR / UR / danh hiệu.

## Đã làm (code)
- Luật engine-free: `Rpg/PetRules.cs`, `Rpg/Account/{PetTeam,PetGrowth,PetGacha,PetCapture,AccountRewards,AccountPets,AccountState(v2)}.cs`, `PlayerState.Companion.cs` (Team[5], PetBonus), `SaveSerializer` v5 `petTeam`.
- World / zone: `HeroController.Pet.cs` (mark 4 s, đánh 2 s, skill 20 s, 2 ô hỗ trợ bắn từ chủ), `HeroPet(.Sheets).cs` (sheet quái walk / attack, máy trạng thái), `PetSkillFx.cs`, `Monster(.PetHit).cs` (Captured, CaptureLocked, WasCaptured, boss bond), `World/PetCaptureService.cs` (xác boss, Request), `World/PlayerRpg.Account.cs` (mọi InvOp tài khoản), `KillRewards.cs` (Thức Ăn, bóng, xác boss).
- Net: protocol 21, InvOp 19–25 (PetLevelUp, PetResetLevel, PetResetStars, PetConvertShards, GachaBuyCoins, BallBuy, PetCapture), `NetEventType.PetCapturable`; `ServerInventoryActions` route Capture.
- UI: `PetWindow.cs` + `.Team.cs` / `.Growth.cs` / `.Capture.cs` (1 cửa sổ 4 tab + nút Gacha), `PetGachaWindow(.Tabs).cs` V2, `CaptureMarker.cs` (2 nút bóng trên nhắc Talk; Bóng Hiếm bấm 2 lần), hàng "Pet Ball" trong `PotionShopWindow`, icon `Sprites/icons/pet-ball*.png`, `pet-shard.png`.
- server-api (agent con): `account-validation.js` v2 + `migrateAccountState`, character v5 `petTeam`, README, test mới.
- Cờ chụp: `--open-pets team|collection|growth|capture`, `--pet-select`, `--pet-team`, `--pet-level`, `--pet-crystals`, `--pet-food`, `--pet-balls`, `--rare-balls`, `--shared-shards`.

## Kiểm
- dotnet ZenoniaSkills **339/339**, ZenoniaNet **405/405** (protocol 21, PetCapturable round trip). server-api **149/149**.
- e2e `Tools/net-persist-test.ps1 -PetsOnly` trên exe cuối: **51/51 PASS** (mua Imp + Pet Ball, giữ qua relog + restart zone, x10 ở server với mảnh chung + Mảnh UR + pity, từ chối mua coin không crystal, migrate pet cũ, người khác thấy / ẩn pet). Seed nhân vật Lv 5 (`dev-seed.mjs --level`) vì ô xuất chiến mở ở Lv 5.
- Exe: `D:\temp\player-pet-gacha\Zenonia.exe` (bản sao `D:\temp\zu-copy-pet`).

## Ảnh đã tự xem (`D:\temp\cap-pet2\`)
- `win-team2_1.png`, `win-collection_1.png`, `win-growth_1.png`, `win-capture2_1.png`, `gacha-pull10_1.png` (4 ô mảnh chung), `gacha-rates_1.png`, `gacha-history_1.png`, `shop-ball_1.png`.
- `marker2-sheet.png`: nút "Pet Ball x3 / Rare Ball x1" hiện từ khung 12 khi slime ≤ 50 % HP. `wolf2-sheet.png` / `wolf2-zoom.png`: Spectral Wolf đứng cạnh hero, số xanh ngọc. V1.5: `D:\temp\cap-pet\walk2-wolf-sheet.png`, `fight3-wolf-sheet.png` (đi + lao cắn).
- Gửi ChatGPT: `review-ui.png`, `review-world.png`.

## ChatGPT Lần 9 + 10
- Lần 9: UI 8/10, combat 8,5/10, "85–90 % bản thu gọn". Phải sửa 4 (bấm nhầm Bóng Hiếm, chữ sổ pet, test reset kép, server tự tính) → đã sửa / đã có. Nên sửa: chữ to hơn, nhãn ô (đã), tên pet trên đầu, lọc History, đo kinh tế UR (chưa). Bỏ qua: 1+2+2, không talent, 4 Bond, pet bất tử.
- Lần 10: **"OK phát hành bản playtest"**, không cần thêm tính năng pet. Trước khi mở: 2 smoke test online (gacha x10 gửi lại reqId không trừ / thưởng hai lần; 2–5 người bắt cùng xác boss) — CHƯA chạy.

## 06:40 người dùng: "sao chưa thấy nó tự đánh" (Golem, Lv10, offline)
- Tái hiện 25 khung / 10 s (`D:	emp\cap-pet3efore-{crystalGolem,spectralWolf,frostfangKing}-sheet.png`): cả 3 pet đứng im sau lưng hero suốt trận, chỉ hiện số "1" trắng.
- Nguyên nhân thật (log `--pet-log`): (1) chu kỳ có chạy ("pet cycle ... dmg=1", "pet attack state Chase dist 38") nhưng pet KHÔNG BAO GIỜ tới quái: đuổi bằng MoveTowards + `FrostHollowMap.Move`, hero đứng mép bậc đá, quái dưới bậc, pet trượt dọc vách ~10 đv/s (không có "pet swings"); (2) đòn thường 10 % tính TRƯỚC giáp (Atk×0,1 − Def/2) = 1–2, số pet vẽ trắng (tint không áp cho glyph atlas), nhỏ, cùng cột với số hero; (3) hero hạ slime < 2 s nên chu kỳ 2 s chỉ kịp 1 cú. Không phải lỗi Golem kiểu khiên (Golem vẫn có đòn thường Strike).
- Sửa (ChatGPT Lần 11 chốt số): đòn thường = max(1, 25 % đòn SAU giáp) (`PetConfig.BasicPct` 0,25), cú đầu ngay, rồi mỗi 1,5 s (`BasicAttackMs` 1500); pet đứng bám cạnh quái giữa các cú (`HeroPet` Guard, `petGuarding` → cú sau chỉ chờ 300 ms); watchdog 250 ms không tiến → hiện ra cạnh quái; `DamageNumberKind.Pet` teal / vàng 26 px lệch phải 22 px (`DamageNumber*.cs`, `Monster.Visuals.cs`).
- Sau sửa: `after-*-sheet.png` (cùng chỗ bậc đá: Golem đi xuống, đứng cạnh slime, số 8 teal mỗi 1,5 s), `open-*.gif` + `open-*-sheet.png` (bãi 11,6 cạnh King: Golem / Wolf / King bám bầy slime đánh 8–9 liên tục, kể cả khi hero bị đẩy ra xa). Test 339 / 405 / 149 xanh, exe build lại cùng đường dẫn.

## Chưa làm / chưa xem
- GIF ở bãi 11,6 bị bầy slime + King vây nên hero bot bị đẩy khỏi khung; chưa chụp được GIF "sạch" 1 hero + 1 pet + 1 quái ở chỗ trống (chỗ spawn mặc định pet nấp sau khung chat ở màn 3440×1440).
- Ném bóng thật + xác boss trong game (chỉ test luật + nút hiện). 2 smoke test online ChatGPT yêu cầu (reqId replay x10, nhiều người bắt 1 boss). Tên pet trên đầu, lọc History. 3 Wanted Boss chưa có PetDef / sprite pet. Đo kinh tế UR khi playtest.

## Phát hành
Deploy server-api trước (schema account v2 + character v5), rồi zone + client cùng build (protocol 21). Tài khoản v1 tự migrate khi đọc.

## Câu hỏi còn mở
- Có cần tên pet trên đầu / lọc History theo ChatGPT "nên sửa" không.

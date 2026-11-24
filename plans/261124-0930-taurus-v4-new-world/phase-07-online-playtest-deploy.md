# Phase 07 (P5): Playtest online + deploy

## Context Links
- BIBLE part3 XIV (performance budget, đông người C01, nhóm đánh boss), XVIII "Performance & Release".
- `docs/deploy.md` (mục "Cập nhật": server-api TRƯỚC, zone SAU; mục Monster Card 23/11), `Tools/package-server.ps1`, `Tools/build-linux-server.ps1`, `Tools/build-from-copy.ps1`, `Tools/play-online*.ps1`, `Tools/net-*-test.ps1`, `Tests/dotnet/ZenoniaSwarm` (`Program.Map.cs`), `docs/optimization-notes.md`.

## Overview
P1 trước phát hành. Có thể deploy sớm một lần sau phase 03 (vòng tân thủ + Orin) nếu người dùng muốn thử thật, rồi lần 2 sau phase 06.

## Requirements
- Test tự động: ZenoniaSkills, ZenoniaNet (trừ 2 FrostHollow fail sẵn có, kiểm vẫn chỉ 2), vitest, `Tools/net-orin-test.ps1`, thêm `Tools/net-taurus-newbie-test.ps1` (bot: tạo nhân vật -> Rowan -> 5 Slime -> Bram -> Orin -> đấu -> chọn lá; tài khoản + Mongo thật localhost).
- Tải: ZenoniaSwarm 20 và 40 bot cùng đứng C01 -> quest Slime không nghẽn (đo thời gian 5 kill trung bình), FPS client PC (`--fps-log`) ≥ 55 PC / 30 mobile mục tiêu; zone tick ổn định. Nhóm 4 bot đánh boss.
- TTK + thời gian boss 5 class (Warrior, Wizard, Cleric mốc; Mechanic, Assassin kiểm không lệch).
- Exploit: TurnIn lặp, reconnect giữa duel / sau chọn lá, kéo quái Lv10 về cổng làng (leash), chết giữa boss.
- `Assets/Resources/Data/dev-start.json` cheat (noCooldowns, noMpCost, level, vàng) TẮT trước bản phát hành; F10 `DevBuffKey` tắt.

## Deploy (thứ tự bắt buộc, hỏi người dùng đồng ý trước khi chạm production)
1. Backup Mongo (`deploy/backup`).
2. server-api: zod `mapId` default mới, `card-ids.json` 52 lá, `PET_IDS` nếu phase 08 đã thêm pet, `newCharacterState` map mới; `npx vitest run`; deploy, `curl /health`.
3. Xoá nhân vật cũ (người dùng đã chốt bỏ save): script một lần `server-api/scripts/reset-characters.mjs` (xoá collection nhân vật, giữ / xoá tài khoản theo câu trả lời phase 01 Q2), chạy SAU backup, TRƯỚC zone mới.
4. Zone: build Linux (`Tools/build-linux-server.ps1`) wire 36, `--map` start = `tauWillowbrookVillage` (kiểm cấu hình pm2 / docker zone), khởi động.
5. Client: build từ bản sao, phát hành cùng lúc (client 35 bị từ chối do version).
6. Theo dõi log zone dòng `the API refused N saves` 30 phút đầu.

## Related Code Files
- Tạo: `Tools/net-taurus-newbie-test.ps1`, `server-api/scripts/reset-characters.mjs` (+ test nhỏ vitest nếu có logic lọc), cấu hình swarm map Taurus trong `Tests/dotnet/ZenoniaSwarm/Program.Map.cs`.
- Sửa: `docs/deploy.md` (mục Taurus V4: reset nhân vật, thứ tự), cấu hình zone `--map` trong `deploy/zone` / pm2.

## Todo List
- [ ] 3 bộ test + net-orin + net-taurus-newbie
- [ ] Swarm 20 / 40 bot C01, nhóm boss
- [ ] TTK 5 class, thời gian boss
- [ ] Exploit checklist
- [ ] Tắt cheat dev-start
- [ ] Deploy theo thứ tự (khi người dùng đồng ý)

## Success Criteria
Mọi test xanh; mục Performance & Release của part3 XVIII đạt; production chạy, nhân vật mới bắt đầu ở Willowbrook Village, không có save bị API từ chối.

## Risk Assessment
- Đảo thứ tự deploy = mất tiến độ (docs/deploy.md): làm đúng 1-6.
- Xoá nhầm dữ liệu: backup trước, script in số bản ghi + hỏi xác nhận.
- Map 96x64 nặng client mobile: dynamic resolution có sẵn; nếu thiếu FPS, giảm prop / cỏ ở Low.

## Security Considerations
Script reset chỉ chạy bằng tay với biến môi trường xác nhận; không đưa vào API route. Không commit bí mật / `.env`.

## Next Steps
Phase 08 khoá nội dung.

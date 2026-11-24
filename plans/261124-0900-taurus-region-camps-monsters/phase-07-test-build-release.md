# Phase 07: Test + build + phát hành cùng server

## Context Links
- `CLAUDE.md` (Kiểm tra, Unity), `docs/map-design-rules.md` "Kiểm tra trước khi báo xong", `Tools/build-from-copy.ps1`, `Tools/build-linux-server.ps1`, `Tools/net-local-test.ps1`

## Overview
P1. Không build / batch vào project đang mở. Bảng quái, map, quest, card dùng chung client + zone server + server-api: phát hành cùng lúc.

## Steps
1. `dotnet test Tests/dotnet/ZenoniaSkills.Tests` và `Tests/dotnet/ZenoniaNet.Tests` (spawn rect, MonsterDef index, card, quest, round-trip message).
2. server-api test (`card-state-schema`, item-validation).
3. Build client từ bản sao; đọc lần biên dịch mới nhất trong `%LOCALAPPDATA%\Unity\Editor\Editor.log` nếu lỗi.
4. Chụp: `--map willowbrook-meadows --tp x,y --solids --shots` mỗi camp, cổng, cầu, cổng sang Bramblewood; Bramblewood Rừng Gai + Gốc Cây Cổ + cửa hang; `--dummy-types` 8 quái; `--map3d` 4 yaw; 2.5D (`--map2d`).
5. Online: zone server + 2 client (`Tools/net-local-test.ps1`): quái mới spawn / đánh / rơi card, tên mới hiện, quest stone.
6. Tự xem ảnh, báo rõ phần chưa xem (online thật, mobile). Người dùng tự test FX / UI trong game (memory testing scope).
7. Docs: `pending-tasks.md`, Sổ lỗi sprite / map nếu có lỗi mới, README số lá card.

## Todo
- [ ] dotnet test x2 + server-api test
- [ ] Build bản sao, chụp camp / quái
- [ ] Online local
- [ ] Docs

## Success Criteria
Test qua, ảnh ô chặn sạch, từng camp chụp trong game, quái mới chạy online; dev-start.json không bị tự đổi.

## Risk
- Quên build lại server -> client mới gặp index quái lạ: bump `NetProtocol.Version`.
- `dev-start.json` cheat còn bật: không đụng, ghi nhắc trước phát hành.

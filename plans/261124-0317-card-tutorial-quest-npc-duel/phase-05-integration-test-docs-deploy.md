# Phase 05: Kiểm thử tích hợp, docs, deploy

## Context Links
- `docs/deploy.md`, `Tools/build-from-copy.ps1`, `Tools/net-duel-test.ps1`, `docs/pending-tasks.md`, `docs/reference/card-gem-ui/design-notes-chatgpt.md`, `plans/261123-1125-monster-card-gem-duel/plan.md`.

## Overview
Priority P2. Done 24/11 (trừ deploy, chờ người dùng). Online thử bằng `Tools/net-orin-test.ps1` (mongod + API + zone thật, 71 PASS). Kiểm toàn bộ, cập nhật docs, chuẩn bị deploy chung đợt V1 card.

## Key Insights
- V1 card chưa deploy: gộp 1 đợt. API trước (zod cardHelp max 3), zone sau (v35).
- Dev server không lưu: kiểm "nhận 1 lần qua relog" cần API + Mongo thật (hoặc offline save file).

## Requirements
- 3 bộ test xanh; kịch bản thủ công online (dev server) + offline; docs cập nhật.

## Related Code Files
- Sửa: `docs/pending-tasks.md`, `docs/reference/card-gem-ui/design-notes-chatgpt.md` (Luật v2 điều 1: "trừ ván hướng dẫn quest + đấu tập Orin: không thưởng, không cược, không mất bài"), `plans/261123-1125-monster-card-gem-duel/plan.md` (ghi chú luật), `Tools/net-duel-test.ps1` (tuỳ chọn `-Scenario tutorial`), `docs/project-changelog.md` nếu có.
- Ownership: docs + Tools.

## Implementation Steps
1. `dotnet test Tests/dotnet/ZenoniaSkills.Tests`, `dotnet test Tests/dotnet/ZenoniaNet.Tests`, `cd server-api && npx vitest run` (cần .NET 10 SDK; máy chỉ SDK 9 thì dùng `C:\temp\dotnet10`).
2. Build bản sao -> player mới (vd `C:/temp/player-card-tutorial/Zenonia.exe`).
3. Online dev server: nhận quest -> ván tutorial (đúng 3 bước, nước sai bị chặn, Skip, Hint) -> thua / hoà / thắng / Forfeit -> TurnIn +300 G +3 lá; TurnIn lại bị từ chối; rớt mạng giữa ván -> quest Active, chơi lại; Practice sau quest không đổi Gold / card; PvP lần đầu hiện hộp xác nhận, lần 2 không.
4. Offline: cùng luồng, thoát game vào lại quest giữ Done, không nhận lại.
5. Docs + pending-tasks (ghi số test mới, đường dẫn player, ảnh `docs/reference/card-gem-ui/tutorial-shots/`).
6. Deploy (CHỜ người dùng đồng ý, là production): server-api -> zone.

## Todo List
- [x] 3 bộ test xanh
- [x] Build bản sao + chụp ảnh
- [x] Kịch bản online dev server
- [x] Kịch bản offline
- [x] Docs (design-notes, pending-tasks, plan V1)
- [ ] Deploy API -> zone (khi người dùng cho phép)

## Success Criteria
Không regress PvP / Ranked (net-duel-test win / lose / draw / ranked vẫn đúng); quest chỉ nhận 1 lần mỗi nhân vật kể cả relog.

## Risk Assessment
- Zone mới trước API cũ: save có cardHelp=3 bị API cũ từ chối -> đúng thứ tự API trước.
- Exe test cũ (v34) bị từ chối: thông báo người dùng dùng player mới.

## Security Considerations
Deploy là hành động production: hỏi người dùng trước. Không commit `.env` / key.

## Next Steps
Sau V1 deploy: thử online thật theo danh sách việc dở trong `docs/pending-tasks.md`.

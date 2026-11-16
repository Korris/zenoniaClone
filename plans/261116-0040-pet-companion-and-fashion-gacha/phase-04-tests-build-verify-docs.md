# Phase 04: Test, build bản sao, chụp, docs

## Test
- `~/.dotnet/dotnet.exe test Tests/dotnet/ZenoniaSkills.Tests` (luật pet, sao, migration, gacha, thưởng) và `Tests/dotnet/ZenoniaNet.Tests` (round-trip RpgState AccountJson, NetAppearance mới, InvOp mới, event PetSkill).
- `npm test` trong `server-api`.

## Build
`Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy-pet -OutExe C:\temp\player-pet-gacha\Zenonia.exe`. Không build vào D:\zenoniaClone.

## Chụp (DebugCapture)
- Pet đi theo + khiên Spirit + cầu lửa Imp (offline capture).
- Rương: quay x10, pity, bảng tỉ lệ, lịch sử.
- Cosmetic trên nhân vật: màu tóc, khung tên, vòng chân.
- Online local (nếu được): 2 client thấy cosmetic + pet của nhau.

## Docs
`docs/pending-tasks.md`, `docs/reference/gacha-discussion-chatgpt.md` (Lần 6), README (phím P/G), báo cáo `plans/reports/pet-gacha-261116.md`.

# Phase 09: UI 1 cửa sổ 4 tab + Gacha, server-api, test, e2e, ảnh, ChatGPT duyệt — DONE (trừ mục ghi CHƯA)

- `PetWindow.cs` (+ `.Team.cs`, `.Growth.cs`, `.Capture.cs`): ví (vàng / coin / crystal / Thức Ăn / bóng), lưới 10 loài, tab Team / Collection / Growth / Capture, nút Gacha [G]. Cờ chụp `--open-pets team|collection|growth|capture --pet-select id --pet-team a,b,c,d,e --pet-level --pet-crystals --pet-food --pet-balls --rare-balls --shared-shards`.
- Protocol 21: InvOp 19–25 (PetLevelUp, PetResetLevel, PetResetStars, PetConvertShards, GachaBuyCoins, BallBuy, PetCapture), `PetCapturable`; test `MessageRoundTripTests` cập nhật.
- server-api V2 (agent con): schema v2, migrate v1, character v5 `petTeam`, README, 149 test.
- e2e `Tools/net-persist-test.ps1` mục pet: mua bóng (gold 6500, balls 1), x10 coin + mảnh chung + Mảnh UR, từ chối mua coin không crystal. (`DebugCapture.NetPets.cs`)
- Ảnh `D:/temp/cap-pet2/`: `win-*_1.png`, `gacha-*_1.png`, `shop-ball_1.png`, `marker2-sheet.png`, `wolf2-sheet.png`.
- ChatGPT duyệt: xem `docs/reference/gacha-discussion-chatgpt.md` Lần 9 (+10).

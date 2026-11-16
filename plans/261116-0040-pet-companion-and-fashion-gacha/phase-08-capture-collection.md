# Phase 08: Bắt pet bằng bóng, xác boss, sổ sưu tập — DONE (xác boss chưa xem trong game)

Spec: `docs/pet-system-design.md` mục 6–7. `Rpg/Account/PetCapture.cs`, `World/PetCaptureService.cs`, `Monster.cs` (`Captured`, `CaptureLocked`, `WasCaptured`), `CaptureMarker.cs`; test `PetTeamGrowthCaptureTests.cs`.

- Bóng thường 500 G ở Dale / Nara (`InvOp.BallBuy`, 5/ngày/tài khoản, hàng "Pet Ball" trong `PotionShopWindow`), 10 % từ boss map; bóng hiếm mỗi 28 lượt Wanted hợp lệ (bắt chắc).
- Quái sống: hero vừa đánh trong 10 s (`HeroController.RecentlyHit`), HP ≤ 50 % (≤ 25 % tỉ lệ cao hơn), 4 ô, không phải boss; N 40/60, R 25/40, SR 12/20, SSR 4/8 %; bắt được thì quái biến mất không rơi đồ / EXP (`KillRewards` bỏ qua `WasCaptured`).
- Xác boss: `KillRewards` → `PetCaptureService.OpenCorpse` 30 s (Wanted 45 s), ai có share hợp lệ (≥ 1 %) ném 1 lần; SSR 5 / UR 1 % (Wanted 3 / 0,5 %); 1 pet boss/ngày; `NetEventType.PetCapturable` báo client.
- Sổ: cấp 1..5 theo số loài 1/3/5/7/10, cấp chặn độ hiếm bắt được (N-R / SR / SSR / UR / danh hiệu).
- Client: `CaptureMarker` 2 nút trên nhắc Talk ("Pet Ball xN", "Rare Ball xN") khi có mục tiêu; `InvOp.PetCapture` (slot 1 = hiếm, uid = NetId) → `ServerInventoryActions.Capture` / offline `PetCaptureService.Request`.
- Ảnh: `D:/temp/cap-pet2/marker2-sheet.png` (nút hiện khi slime ≤ 50 %). CHƯA xem: ném bóng thật, xác boss trong game (có test luật).

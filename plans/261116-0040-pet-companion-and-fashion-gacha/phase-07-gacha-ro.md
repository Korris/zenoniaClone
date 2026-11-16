# Phase 07: Gacha kiểu RO Pocket Saga — DONE

Spec: `docs/pet-system-design.md` mục 4–5. `Rpg/Account/PetGacha.cs`, `AccountRewards.cs`, `AccountState.cs` v2; test `PetGachaTests.cs`.

- Tiền: Pet Crystal (kiếm trong game) → `GachaBuyCoins` 150 = 1 Gacha Coin (`tickets`); 1 lượt = 1 coin; x1 / x10.
- Tỉ lệ N 12 / R 24 / SR 36 / SSR 6,25 / UR 2,5 + 19,25 % = 5 mảnh chung (`history.id == ""`); trùng = 10 mảnh loài; lượt không UR +1 Mảnh UR, 120 = Frostfang King (`GachaExchange`).
- Pity theo tài khoản: SSR+ ở lượt 20 (SSR 71,43 : UR 28,57), UR ở 50; SSR+ reset pitySSR, UR reset cả hai. Tài khoản v1 (10/30/60) giữ bộ đếm, kẹp.
- Thưởng: tân thủ 10 coin; Wanted lần đầu/ngày 150 crystal (≤ 4/tuần) + 10 Thức Ăn + tiến độ bóng hiếm; boss map 3 Thức Ăn, lần 5/tuần 150 crystal, 10 % bóng; mốc Lv 10..90 300 crystal; quái 8 % Thức Ăn (`KillRewards.cs`).
- UI `PetGachaWindow(.Tabs).cs`: coin + crystal + nút mua, pity 20/50, ô kết quả mảnh (icon `pet-shard`), bảng tỉ lệ từ `PetRules`, lịch sử 200.
- server-api: `account-validation.js` v2 + `migrateAccountState` (weekWantedTickets→weekWantedClaims, kẹp pity), chống lùi level/sao.

# Phase 06: Đội hình 1+2+2, Bond, level + Thức Ăn, sao 1–5, reset — DONE

Spec: `docs/pet-system-design.md` mục 2–3. Luật engine-free trong `Assets/Scripts/Rpg/`, test `Tests/dotnet/ZenoniaSkills.Tests/PetTeamGrowthCaptureTests.cs`.

- `PetRules.cs`: `PetSlot` (Combat, Support1-2, Stat1-2), mở ở Lv 5/20/30/35/45; `SlotPassiveShare` 1/0/0/0,25/0,25; `SupportSkillShare` 0,5; trần `Cap(kind)` (+10 % dmg, +12 % HP, +10 % speed, −10 % dmg taken); 4 Bond +2 % (`Bonds`); `EncodeSlot/DecodeSlot` cho InvOp.PetEquip (slot*16 + index, 15 = bỏ).
- `Rpg/Account/PetTeam.cs`: `Refusal` (chưa sở hữu, chưa đủ cấp, trùng loài), `Bonus(team, account)` → `PetBonus` (cộng dồn, Bond, trần), `ActiveBonds`.
- `Rpg/Account/PetGrowth.cs`: `LevelUp` (Thức Ăn `2 + L/10`, trần 60 và không vượt hero), `StarUp` (10/20/30/40 mảnh + 5k/10k/20k/30k vàng, nội tại ×1,0…1,4), `Convert` (mảnh chung → mảnh loài), `ResetLevel` / `ResetStars` (trả hết theo sổ `foodSpent/shardsSpent/goldSpent`).
- `PlayerState.Companion.cs`: `Team[5]`, `PetBonus` tính lại trong `RecomputeStats`; `SaveSerializer` v5 `petTeam`.
- `HeroController.Pet.cs`: 2 ô hỗ trợ bắn skill 50 % từ chủ, không có thân; sát thương pet × `LevelDamageMul`.
- Phím / UI: `PetWindow.Team.cs` (5 ô, bonus, Bond), `PetWindow.Growth.cs`.

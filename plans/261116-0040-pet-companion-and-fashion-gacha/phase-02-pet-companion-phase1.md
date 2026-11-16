# Phase 02: Pet Companion (8 pet, sao, skill, migration, UI)

## Context
`Rpg/PetConfig.cs`, `Rpg/PlayerState*.cs`, `HeroPet.cs`, `HeroController.Combat.cs`, `Monster.cs` (TakeHit), `Skills/HeroBuffs.cs` (Shield), `Rpg/Skills/StatusRules.cs` (slow), `PotionShopWindow.cs`, `InventoryPanel.*`, `Net/Protocol/*`, `World/PlayerRpg.cs`.

## Bảng pet (ChatGPT Lần 6; nội tại +1 điểm % / sao)
| Id | Độ hiếm | Nội tại 1/2/3★ | Skill (cd 20 s) |
|---|---|---|---|
| spirit | N | MaxHP +5/6/7 % | chủ mất HP thật, HP <= 70 %: khiên 5 % MaxHP 5 s |
| imp | N | sát thương PvE +3/4/5 % | lửa 40 % |
| snowFox | R | tốc chạy +3/4/5 % (vẫn dưới trần tốc) | băng 45 % + chậm 20 % 2 s (boss miễn) |
| greenSlime | R | sát thương nhận từ quái -3/4/5 % | độc 45 % |
| forestFairy | SR | thuốc HP +5/6/7 % | chủ mất HP thật, HP <= 70 %: hồi 5 % MaxHP |
| thunderSprite | SR | tốc đánh +3/4/5 % | sét 55 % |
| phoenix | SSR | sát thương PvE +4/5/6 % | lửa 65 % |
| miniDragon | UR | sát thương PvE +5/6/7 % | lửa 75 % |
"%" skill = `CombatMath.Damage(Atk, pct, def)` (Atk không pet bonus, không combo / crit / on-hit). Đánh đúng quái chủ đánh trúng (trong 2 s, tầm 6 ô), bay ~0,25 s, quái chết trước thì thôi. `Monster.TakePetHit`: không ghi damage share, không gọi lại pet, không SkillHitHooks.

## Sở hữu / sao
- Spirit tự mở khi nhân vật bất kỳ đạt Lv 5; Imp 3000 vàng ở merchant (InvOp.PetBuy, 1 lần / tài khoản); gacha; Mini Dragon 120 Mảnh UR.
- Sao: 1->2 10 mảnh loài + 5000 vàng; 2->3 20 + 10000; 100 %.
- Migration: item pet cũ -> mở 1★ + 10 mảnh + 3000 vàng / loài (1 lần / tài khoản, `compensated`), con đang mặc -> petId; xoá item sau khi tài khoản ghi xong.

## Net / UI
- InvOp `PetEquip` (Slot index, 255 = cất), `PetStarUp` (Slot index), `PetBuy` (mở khoá). `NetEventType.PetSkill` (Id hero, Other quái hoặc 0, Step loại skill) -> FX có sẵn.
- `PetWindow` (phím P / ô Pet ở loadout): 8 ô, khoá / sở hữu, sao, mảnh, nội tại + skill, Mang / Cất / Nâng sao.
- Ảnh 6 pet mới: tạm nhuộm ảnh Spirit / Imp (HeroPet tint), ghi pending.

## Todo
- [ ] PetConfig 8 pet + PlayerState pet (PetId, Account, nội tại) + test
- [ ] AccountPets: unlock, buy, star up, migrate + test
- [ ] Runtime skill + TakePetHit + event + FX
- [ ] Bỏ độ bền (SelfState, UI)
- [ ] PetWindow + shop + loadout
- [ ] Cập nhật PetTests cũ theo luật mới

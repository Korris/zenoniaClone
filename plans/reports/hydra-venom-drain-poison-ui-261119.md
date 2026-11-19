# Mire Hydra Queen: Venom Drain + icon trúng độc + hiệu ứng độc trên người + sửa boss quay mặt (19/11/2026)

Branch worktree `worktree-agent-ab0c5779b333945d9` (từ `ro-fantasy-heroes` e3903bf7, đã merge thêm HUD RO e8dd9b84). Exe: `D:\temp\player-hydra-venom\Zenonia.exe` (bản sao `D:\temp\zu-copy-venom`, 0 lỗi). Đã đọc `CLAUDE.md`, `docs/sprite-rules.md` (quy tắc art ChatGPT, VFX, Sổ lỗi mục độc / HUD / shader), `docs/ui-layout-rules.md`, `docs/wanted-boss-skills-design.md`, báo cáo Hydra v2.

## 1. ChatGPT (Tay = "Tân Korris", Plus; kiểm tra `/api/auth/session` trước khi gửi)

Chat: https://chatgpt.com/c/6aff091a-624c-83ec-b0f2-af0bb9e55cb4 (1 câu hỏi chữ + 1 lượt vẽ icon).

Câu trả lời (tóm tắt):
- **Venom Drain V1**: vòng 6 ô quanh boss, chỉ trúng người đang độc + còn trong vòng lúc đòn rơi; cảnh báo 2 000 ms; 0,8x; xoá độc; boss hồi 50 % sát thương thật, trần 2 % máu tối đa / lần; hồi 22 s; giữ khoảng nghỉ chung 3 s; không có ai độc trong vòng thì không dùng, không mất hồi chiêu. Câu né: "Leave the circle to avoid poison drain!" (đã viết lại thành 2 câu theo luật bảng: "The queen drains poisoned hunters. Leave the circle."). Số chưa cân bằng.
- **Icon**: đầu lâu trên giọt độc xanh olive, viền nâu; HUD mình 24 px có số giây góc dưới phải; party 16 px + số giây; người ngoài party không hiện; hết độc tắt ngay.
- **Hiệu ứng trên người: CÓ**, phân cấp: mình tint olive 20 % không nhấp nháy + 2 bọt / s; party tint 12 % + 1 bọt / s; người khác tint 8 %, không hạt; bị rút: chớp xanh 120 ms + tia hút 350 ms. Không khói đầu lâu, không nhấp nháy theo chu kỳ, không ParticleSystem riêng cho 40 người; tia hút tối đa 8, ưu tiên mình và party.
- Icon vẽ: `Assets/ArtLibrary/ui/status/poison-status-icon-chatgpt-1.png` (nền #FF00FF, có câu tông RO Soft Fantasy, kèm 2 ảnh tông mẫu), tách nền → `Assets/Resources/Sprites/ui/status/poison.png` (128 px). Đã xem trên nền tối / sáng ở 64 / 24 / 16 px: không viền hồng. 1 lượt vẽ, nhận lần đầu.

## 2. Code

| Phần | File |
|---|---|
| Skill Id 4 trong bảng dùng chung | `Rpg/WantedSkills/WantedSkillTable.cs`, `WantedSkillDef.cs` (`DrainPoisoned`, `DrainHealShare`, `DrainHealCap`), `WantedSkillClock.cs` (8 ô hồi chiêu) |
| Luật rút / hồi / giữ độc (engine-free, có test) | `Rpg/WantedSkills/WantedVenomDrain.cs`, `MonsterStatus.DotMsLeft / HoldDot / ClearDot` |
| Server | `Monster.WantedSkills.cs` (chỉ bắt đầu khi có người độc trong vòng; giữ độc tới sau đòn; đòn rơi: rút người độc còn trong vòng, hồi boss), `HeroController.WantedStatus.cs` (`TakeWantedSkillHit` trả máu thật mất, xoá độc; `HoldWantedPoison`; `PoisonMsLeft`) |
| Tick độc không cho bất tử | `HeroController.Combat.cs` `TakeDamage(dmg, dot)` (lỗi cũ: hero trúng độc né gần như mọi đòn boss) |
| Mạng | `NetEventType.HeroStatus = 22` (Id hero, kind + cờ Drained, ms còn lại), gửi khi áp / làm mới / giữ / xoá độc; client tự đếm theo đồng hồ server. **Protocol 24 → 25.** |
| FX Venom Drain | `MireHydraLightFx.Drain.cs`: vòng cảnh báo shader như các skill khác + tia `LightBeamScroll` tím/rêu từ hero độc về thân boss, bọt chảy theo tia, đòn rơi tia phình sáng rồi tắt ở 350 ms, cột hồi máu xanh + bọt phun trên boss (650 ms). Thời gian ở `WantedSkillLightTiming` (`TetherAlpha / TetherWidth / HealFlash`), cảnh báo đầy đúng `TelegraphMs`, có test |
| Icon HUD | `HudBuffRow` ô độc đầu hàng (HUD gọn và `RoHudStatus.StatusIcons` của HUD RO đều dùng nó); party: `PoisonStatusIcon` gắn vào `RoHudParty.StatusIconsOf(i)` (HUD RO), ô buff 0 ở `PartyHudList` (HUD gọn) |
| Hiệu ứng trên người | `HeroController.WantedPoisonFx.cs` (theo đúng phân cấp ChatGPT, 1 ParticleSystem chung), `HeroRenderer.WantedFreeze.cs` + `HeroLayeredView.MultiplyTint` (tint giờ ăn cả hero RO Fantasy nhiều lớp, sửa luôn tint đóng băng) |
| Icon độc trên đầu (cũ) | bỏ phần độc, giữ phần chậm (3 dấu hiệu cho 1 trạng thái là thừa) |
| Boss quay mặt | `Rpg/MonsterFacingRule.cs` + `Monster.Ai.cs` (xem mục 4) |
| Hook test | `Monster.FacingLog.cs`: env `ZENONIA_BOSS_FACING_LOG=1` (log mỗi 10 s + log Venom Drain), `ZENONIA_BOSS_FACING_LEGACY=1` (luật cũ để đo), `ZENONIA_PREFER_DRAIN=1` (boss ưu tiên Venom Drain khi có người độc, chỉ để chụp). Showcase chụp 5 skill + 6 ảnh khi trúng độc; joiner party chụp khi mình / đồng đội trúng độc |

Bảng Wanted (Boss Skills & How to Dodge) vẫn 4 dòng: Venom Drain chưa hiện trên bảng.

## 3. Chơi thử (đã xem ảnh)

Server log (`[venom-drain]`), bot không bất tử: mỗi lần rút 1-8 người, ví dụ "8 poisoned inside, took 1794 hp, boss healed 897"; trần 2 % (13 920 máu ở hp x20) chưa lần nào chạm. Trước khi sửa luật giữ độc: 4 lần chơi thử, mọi lần "0 poisoned inside".

Ảnh `docs/reference/mire-hydra-queen-v2/`:
- `venom-drain-telegraph-crowd40.png`: 40 người, vòng cảnh báo 6 ô (viền đỏ hồng), thanh niệm "Venom Drain 1.2s".
- `venom-drain-telegraph-ro-hud.png`, `venom-drain-strike-heal-flash.png`, `venom-drain-series.png` (HUD RO, 6 bot): tia xanh rêu từ hero độc về boss lúc cảnh báo, phình sáng ở +0,1 s, cột hồi máu xanh trên thân boss, tắt ở +0,3 s.
- `venom-poison-hud-compact-crowd40.png`: icon độc "4s" dưới thanh máu (HUD gọn, 40 người).
- `venom-poison-party-ro-hud.png` (HUD RO, `-Party`, bot không bất tử): đồng đội c727b14 trúng độc có icon giọt độc + số giây bên phải tên ở hàng party; icon độc của mình dưới bảng trạng thái.
- `venom-poison-hud-ro-and-onbody.png`: icon độc dưới bảng trạng thái HUD RO + wizard trúng độc: bọt xanh nhỏ bay lên, tint olive rất nhẹ (đúng mức 20 % ChatGPT, khó thấy trên đồ nhiều màu).

## 4. Boss quay mặt liên tục (người dùng báo)

Kiểm 3 nghi vấn:
1. Telegraph / Execute không giữ mục tiêu → mỗi tick chọn lại hero gần nhất: **đúng** (70-126 lần đổi mục tiêu / 10 s).
2. Chọn hướng theo trục trội, vector nhỏ / chéo 45° lật mỗi tick: **đúng**.
3. Chase ↔ Return: **không thấy** trong sân (returns = 0 suốt các lần đo).

Sửa: hướng có vùng chết 4 đv + biên 18° (mọi quái), boss truy nã giữ hướng 400 ms (quạt / đường thẳng vẫn quay ngay khi khoá hướng skill), giữ mục tiêu qua Telegraph / Execute, boss đang đuổi người ngoài tầm thì đánh người trong tầm.

| 39 bot + 1 hero, ~60-190 s | đổi hướng / s | đổi mục tiêu / 10 s | Return |
|---|---|---|---|
| Trước (`LEGACY`) | 3,49-5,48 (7 cửa sổ 10 s) | 71-126 | 0 |
| Sau | 0,40-1,00 (19 cửa sổ) | 0-5 | 0 |

## 5. Kiểm tra

- `dotnet test` ZenoniaSkills 452 (thêm `WantedVenomDrainTests` 6, `MonsterFacingRuleTests` 4), ZenoniaNet 455 (round-trip `HeroStatus`, pin protocol 25): đạt.
- **Phải build lại zone server** (bảng skill, luật server, protocol 25) và phát client mới cùng lúc. server-api không đổi.

## 6. Chưa xem trong game

- Icon độc trên hàng party của HUD gọn (`PartyHudList`, `--ui new`): chưa chụp (đã chụp ở HUD RO).
- Số giây của icon độc trên hàng party HUD RO rất nhỏ (chữ ~10 px), chồng nhẹ lên icon.
- Chớp xanh 120 ms trên người bị rút: chưa thấy rõ (đám hiệu ứng che).
- Tia hút từ chính hero của mình (các lần chụp hero mình không độc đúng lúc đó; thấy tia từ bot).

## Câu hỏi

1. Tint độc 20 % theo ChatGPT gần như không thấy trên hero RO Fantasy: giữ hay tăng (ví dụ 35 %)?
2. Có thêm dòng Venom Drain vào bảng Wanted (5 dòng) không?
3. Thanh máu boss hiện "65 535 / ..." và ENRAGED sai (HpLeft ushort trong MonsterHit, lỗi có từ trước): sửa riêng?
4. Icon độc trong HUD RO rất nhỏ (cỡ `RoHudStatus.IconSize`): agent HUD có muốn tăng cỡ ô?

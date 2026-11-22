# Phase 04: HUD mobile + PC hotbar + chữ thế giới

Ưu tiên: P1. Trạng thái: chưa. Đọc hết `docs/ui-layout-rules.md` (nhiều mục sổ lỗi HUD 19-21/11).

## Phạm vi
| Phần | Code | Mẫu | Ghi chú |
|---|---|---|---|
| Khung trạng thái (chân dung tròn, HP/MP/EXP, Lv, buff) | `RoHudStatus`, `RoHudPortraits` | 01-hud-boss-fight | vòng chân dung đồng, thanh theo token HP/MP/EXP; giữ luật mask = khung vòng |
| Minimap tròn + tên map | `Minimap.RoRound.cs`, `RoHudMapPanel` | 01 | vòng đồng mảnh |
| Hàng menu trên phải | `RoHudMenu` | 01 | icon nâu-đồng, chữ kem viền nâu |
| Nút đánh / skill / auto / bình máu, joystick | `RoHudControls`, `RoHudSkillSlot`, `TouchControls` | 01 | HUD cấp 3: ít hoa văn; vòng ô ≤ 2,5 px, icon ≥ 88 % ô (luật 21/11) |
| Hotbar PC (1-8, Q, E, V, Shift, Enter) | `RoHudHotbar`, `RoHudSkillSlot.Hotbar` | 01 + `ro3-pc-hud/` | khay giấy tối 78 % hoặc nâu đen ấm |
| Chat thanh + panel | `RoHudChatBar`, `RoHudChatPanel` | 01 đáy trái | nền nâu đen ấm 85-90 % (không xanh đen) |
| Thanh boss, thẻ Wanted | `RoHudBossBar`, `RoHudWanted*` | 01 | HP boss đỏ trầm (cảnh báo boss vẫn đỏ theo luật FX) |
| Party list | `RoHudParty` | 01 | |
| Chữ thế giới: tên người chơi, nhãn quái, số dam | `NameTags*`, `Monster.RoFootLabel`, `RoTextOutline`, `DamageNumber*` | style guide B3 | viền mực navy #141A29 -> nâu đen #2A211B; font Be Vietnam Pro; số dam giữ màu chức năng (heal xanh, crit vàng) |

## Việc
1. Sheet ChatGPT HUD (H1 trạng thái + minimap + menu icon, H2 nút / hotbar / chat) theo kit phase 02, cùng tên mảnh `hud-*`, `hotbar-*`.
2. `RoSoftTheme.Night*` (tấm HUD tối) -> nâu đen ấm (#2B231D 88 %), viền #6B5444.
3. Kiểm 4:3, 1280x853, 16:9, 19.5:9 (luật boss bar kẹp dải trống).

## Thành công
Chụp 01-hud cạnh mockup cùng cỡ; HUD đọc rõ trên tuyết, cỏ, hang tối.

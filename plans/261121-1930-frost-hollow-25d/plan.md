# Frost Hollow + Frostfang Den thành 2.5D, tuyết giá trị trung bình (21/11/2026)

Người dùng: "sao không design lại map tuyết luôn là được mà" (thay vì làm FX skill đục trên tuyết), rồi "làm luôn Frostfang Den 2.5D đi". Đã đọc `CLAUDE.md`, `docs/map-design-rules.md` (Map 2.5D 1-21), đầu `docs/sprite-rules.md` (RO trailer style), `docs/reference/ro-trailer-skill-style/prompt-templates.md`, `docs/reference/warrior-fx-ro-trailer/after-snow-all.jpg` vs `after-grass-all.jpg`.

ChatGPT (Tay, "Tân Korris"): https://chatgpt.com/c/6b018f05-1e84-83ec-bd2a-6e9ddc093520 . Art direction tóm tắt: `docs/reference/frost-hollow-25d/snow-25d-art-direction-chatgpt.md`. Ảnh gốc: `Assets/ArtLibrary/maps/frost-hollow/snow-25d/`, `Assets/ArtLibrary/maps/frostfang-den/ice-25d/`.

## Quyết định

| Phần | Cách làm |
|---|---|
| Lý do | Tuyết cũ L ~0,85-0,95 ăn mất thân quạt / vòng vàng trong suốt. Đổi map, KHÔNG tăng alpha FX. |
| Bố cục | Giữ nguyên rows, cổng, bãi quái, bảng prop tay (OldSection / Frame / Scatter; Den PropList). Prop mới chỉ đứng trên ô rows đã chặn hoặc đi xuyên: tập ô chặn KHÔNG đổi (tool preview kiểm), không cần build lại server cho va chạm. |
| Nền Frost Hollow | `Tools/build-map-25d-snow-ground.py frost-hollow`: 1 atlas 2x2 ChatGPT (tuyết #8999AF / mảng sáng #B2BED0 / tuyết bóng #6F7F98 / đường nén #848D9B); đường theo file lanes; khối đá ô `C` #6E7D93 sâu dần #56657B + AO chân vách; sông băng #417F9D lòng #315B7A viền #425B73; hồ băng #638CA5 + sương + nứt; cầu / bậc giữ pixel cũ (x0,78). Y tuyết đi được: trung vị ~0,28, p90 ~0,38. |
| Vách | Vách vẽ phẳng bỏ, thay bằng prop đứng `fhCliff*` (8 mảnh 1-3 ô) xếp LIỀN trên mọi ô `C` có tuyết ngay phía nam (`place-map-25d-dungeon-props.py frost-hollow`, `runs`), mặt vách tối dần xuống chân (x0,92 -> x0,70); cửa hang băng `fhIceCave` (67,17); thông / đá thưa trên khối cao nguyên. |
| Prop | Vẽ lại cho 58°, giữ tên: `fhPine*`, `fhDeadTree*`, `fhBoulder`, `fhRock*`, `fhStump`, `fhCrystalSmall/Mid`, `fhRune*`; mới `fhRockSlab`, `fhLogSnow`, `fhBushSnow`. |
| Ánh sáng FH | Profile `View25DProfile.SnowField`: nắng #FFF0D7 0,25, bóng #667B8E, fog #8799AE max 0,16, vignette 0,07, tuyết rơi thưa (`Snowfall` 28, `View25DAmbientFx.TickSnow`), rừng viền thông / đá. |
| Den | Nền `build-map-25d-dungeon-ground.py frostfang-den` (atlas: sàn #53697D, mảng #72899E, khối đá #334B66, đấu trường #66788E); vách băng đứng `fdIceWall*` / `fdIcicleWall` / `fdIcePillar` liền theo hàng; pha lê `fdCrystalTall` / `fdCrystalTeal` / `fdCrystalWall` / `fdCrystalShards` = `DungeonLights`; 4 đèn viền đấu trường cạnh 4 cột góc (`DungeonLights.Extra`); profile `IceDungeon` (ambient #344F69 x 0,30). Prop cũ `icePillar`, `frozenStalagmite`, `frostCrystal`, `frozenBones` vẽ lại, giữ ô. |
| 2.5D | Thêm `frostHollow`, `frostfangDen` vào `View25D.Maps`. |

## Lượng art ChatGPT: 7 ảnh
1 atlas nền FH, 1 tấm vách FH, 1 tấm cây, 1 tấm đồ thấp (+ cửa hang), 1 atlas nền Den, 1 tấm vách / pha lê Den, 1 tấm đồ Den.

## Trạng thái
- [x] Art direction + 7 ảnh ChatGPT; nền, vách, prop, profile, đèn
- [x] Ảnh ghép + ô chặn (không PROBLEM, tập ô chặn không đổi), build `D:/temp/player-snow25d/Zenonia.exe`
- [x] Chụp 3 skill Warrior + 5 điểm FH + 3 điểm Den, ChatGPT chấm (xem pending-tasks)
- [ ] Người dùng xem trong game
- [ ] `dotnet test` (máy này chỉ có SDK 9)

## Rủi ro / để sau
- Bậc thang vẫn là pixel vẽ phẳng cũ (chưa có sprite bậc cho 58°).
- Vách là các mảnh rời liền nhau, góc lồi / lõm chưa có mảnh riêng.
- Bảng prop tay cũ (lều, lửa trại, tháp đổ, rào) chưa vẽ lại cho 58°.

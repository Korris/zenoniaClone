# Phase 04 (P2): Art style lock (dựa trên concept được duyệt)

## Context Links
- **Brief agent bắt buộc: đọc hết `docs/sprite-rules.md` (khối RO SOFT ADVENTURE đầu file, "Quy tắc art lấy từ ChatGPT", "Quy tắc thiết kế quái", Sổ lỗi) và `docs/map-design-rules.md` (Map 2.5D điều 1-18).**
- BIBLE part1 III (palette, độ sáng theo khu, mật độ trang trí), part3 XV (sheet walk 4x4 ô 192 / attack 4x4 ô 256, nền #FF00FF, không bóng, pivot chân, left = flip right), XVI (props).
- Concept: agent khác đang nhờ ChatGPT vẽ Willowbrook, Clover Meadows, Clover Slime, Horn Rabbit, Moss Mushroom. **Phase này chỉ bắt đầu khi người dùng duyệt concept**; không vẽ lại concept.
- Tool: `Tools/build-map-25d-ground.py` (+ `border`), `Tools/place-map-25d-forest.py`, `Tools/cut-magenta-prop-sheet.py`, `Tools/build-meadow-monsters.py` (walk 768 ô 192, attack 1024 ô 256, hàng xuống / lên / trái / phải), `StaticPropShadows.Classify` (tên prop).

## Overview
P1 cho chất lượng. Đưa đúng 2 map + 3 quái + 6 NPC + 5 lá tân thủ vào Unity, khoá phong cách trước khi làm 8 quái còn lại (BIBLE: "chỉ sau khi đạt chất lượng mới sản xuất các quái còn lại").

## Key Insights
- Thứ tự hàng sheet của engine (xuống / lên / trái / phải) khác BIBLE (front / right / back / left-flip): giữ engine, prompt ChatGPT theo engine; quái đối xứng nên hàng trái = lật phải trong tool.
- Map 2.5D: nền CHỈ đất phẳng (2 texture ChatGPT + mặt nạ từ rows), mọi vật đứng là prop riêng; không vẽ một ảnh liền cả map.
- Ảnh concept dùng làm hướng, không cắt từ concept làm nền (concept là ảnh phối cảnh).
- Quái: gửi concept được duyệt + số đo cỡ (H theo part3 XV.3: Slime 0,6H, Rabbit 0,7H, Mushroom 0,8H; hero 69 px ở 720p), KHÔNG gửi frame quái cũ.

## Requirements
- Willowbrook Village: texture cỏ / đường đất + prop: cây sồi quảng trường, nhà dân, Hội Thám Hiểm, tiệm rèn, quầy chợ, giếng, cối xay gió, cổng làng, rào, cầu nhỏ, thùng / sọt / đèn / biển, hoa, bụi (part3 XVI.1).
- Clover Meadows khu Z1 (+ texture đủ cả map): cỏ chính / sáng, đường đất, rào gỗ, bụi, đá nhỏ, cây sồi vừa; rừng viền.
- 3 quái: walk + attack sheet (idle = walk frame 0 nếu engine không có idle riêng), projectile bào tử Moss Mushroom (Particle / texture nhỏ, không sprite frame theo luật VFX).
- 6 NPC: idle sheet + face, cùng tông nhân vật V2 (gửi kèm master hero).
- 5 card art (Slime, Rabbit, Frog, Mushroom, Mantis) + 3 lá Orin (Boar, Rootling, Sporeling) nếu concept có, không thì để phase 05-06.
- Nghiệm thu part3 XV.7: đọc được ở cỡ game, flip không lỗi, không magenta lẫn, không bóng vẽ sẵn, tách màu khỏi nền, attack khớp timing damage.

## Related Code Files
- Tạo: `Assets/ArtLibrary/maps/tau-willowbrook-village/`, `.../tau-clover-meadows/` (ảnh gốc), `Assets/Resources/Sprites/village/tiles/tau-*-ground*`, `Resources/Sprites/monsters/tauCloverSlime*.png`, `tauHornRabbit*`, `tauMossMushroom*`, `Resources/Sprites/village/npcs/tau*`, `Maps/TauWillowbrookVillageProps.cs`, `Maps/TauCloverMeadowsProps.cs`, `docs/reference/taurus-v4-art/` (prompt, ảnh chấm).
- Sửa: 2 map class (Props, GroundTexture, chunk), `NpcDefs.cs` (bỏ `DefArt` tạm), `MonsterDef.cs` (bỏ FallbackSheet), `CardTableData.cs` (art key nếu có), `View25D/View25DProfile.cs` nếu cần profile Taurus.

## Implementation Steps
1. Đọc concept được duyệt + 2 rules doc; viết prompt từng mục, lưu `docs/reference/taurus-v4-art/prompts.md`.
2. Nền 2.5D 2 map -> rows giữ nguyên từ greybox (chỉ chỉnh nếu bố cục concept khác; khi đổi rows phải build lại zone).
3. Prop sheets -> cắt -> đặt (`place-map-25d-forest.py` cho cây; prop làng tay) -> preview in `PROBLEM`.
4. 3 quái: concept -> sheet -> `build-meadow-monsters.py` -> xem từng frame trên nền cỏ sáng + rừng tối.
5. NPC 6 + card art.
6. Build bản sao, chụp: làng 4 góc + quảng trường, cổng, C01 đông quái, `--cam-yaw ±28 --cam-pitch 45` 4 mép; tự xem ảnh, chấm checklist style guide ≥ 9/10.
7. Ghi lỗi mới vào Sổ lỗi `sprite-rules.md` / `map-design-rules.md`.

## Todo List
- [ ] Chờ người dùng duyệt concept
- [ ] Prompt + nền 2 map
- [ ] Prop làng + Z1
- [ ] 3 quái sheet + projectile
- [ ] 6 NPC + 5-8 card art
- [ ] Chụp + tự xem + người dùng xem

## Success Criteria
Người dùng duyệt ảnh trong game (không chỉ ảnh ChatGPT). Checklist part3 XV.7 đạt. Không còn art cũ trong 2 map live.

## Risk Assessment
- ChatGPT vẽ to đầu / lệch tỉ lệ giữa hướng -> điều 8 sprite-rules, gửi số đo.
- Nền caro / trong suốt -> luôn nền #FF00FF (art xanh lá) theo điều 1.
- Hết lượt vẽ Tay -> Tan Pham (kiểm đúng tên tài khoản).

## Security Considerations
Không có.

## Next Steps
Phase 05 dùng cùng prompt / pipeline cho 6 quái còn lại + 6 khu còn lại.

## ChatGPT art review 24/11 10:30 (`docs/reference/world-design/taurus-art-review-vs-global-style-chatgpt.txt`)
- Không vẽ lại: concept làng (93/100), Clover Meadows (90/100), 3 quái đầu + sprite sheet (khoá làm chuẩn tân thủ).
- Khi dựng map thật: làng giảm 25-35% chi tiết nhỏ ở chỗ đông người (bảng quest, khu Card), cây sồi giữa giữ làm landmark nhưng có occlusion/dither khi che nhân vật; Clover Meadows giảm 30-40% đá/hoa ở bãi combat, hàng rào chỉ giữ gần đường, mở rộng đường ở chỗ giao, khu Ancient Roots thêm rễ + phiến đá cổ, bớt hoa. Không làm tối map.
- Cute/Threat từng quái: Slime 10/1, Rabbit 10/2, Frog 9/1, Mushroom 10/2, Boar 6/5, Mantis 6/5, Rootling 7/4, Thorn Sprite 7/4, Barkling 5/6, Brambleback Matriarch 4/6, Elder Root Guardian 3/7.
- Sửa concept trước khi vẽ sheet: Boar (mềm gai), Rootling + Thorn Sprite (bớt cau mày), Ancient Barkling (thấp vương miện, nhỏ tay, bớt giáp).

## Môi trường làm khác kế hoạch (24/11 ~13:40, agent môi trường)
- Thiết kế: chat thiết kế chốt "TAURUS P2 — ENVIRONMENT PRODUCTION BRIEF V1.0" (9 material T-MAT01..09, 5 tấm làng WV-SHEET-01..05, 7 tấm đồng CM-SHEET-01..07), nguyên văn `docs/reference/taurus-v4-art/taurus-environment-production-brief-chatgpt.txt`; chat sản xuất vẽ theo nguyên văn (13 ảnh, `Assets/ArtLibrary/maps/taurus/{ground,props}/`, cắt ở `props/cut/`).
- Nền: KHÔNG dùng `build-map-25d-ground.py` (nó lấy mặt nạ từ ảnh vẽ cũ); tool mới `Tools/build-tau-25d-ground.py` ghép theo ký tự rows: lớp họ cỏ (cỏ chính / sáng / rợp / đất rừng / đất cổ) trộn rộng ~3 ô, lớp đường / sân lát / nước / bờ ẩm mép 0,35 m; chunk 2x2 (làng 1408x1024) và 4x4 (đồng 1536x1024), `-border`, minimap.
- Prop: `Tools/place-tau-25d-props.py <village|meadows> [--minimap]` + dữ liệu `Tools/tau_25d_props_layout.py` (cỡ, vị trí làng viết tay theo khối greybox, đồng cỏ sinh theo seed: rừng trên ô `t`, rừng thưa trong khu bắc suối, decor phẳng ven đường / rìa camp / bờ suối, không trong rect camp); sinh `Maps/TauPropDefs.cs`, `TauWillowbrookVillageProps.cs`, `TauCloverMeadowsProps.cs`; in PROBLEM khi prop chặn đè đường / sân / cầu / camp / cổng / NPC hoặc ô chặn không có prop phủ.
- Code: 2 map class (chunk, Props, `BlockPropFootprints`), `TileMapBase.BlockPropFootprints`, `VillagePropDefs.TryGet` -> `TauPropDefs`, `MapScenery.LiesFlat` (cầu đi xuyên luôn phẳng), `View25DProfile.TaurusField` (rừng viền bằng cây Taurus), `MapPortalLightFx` (cột sáng giảm 30 % khi hero mình đứng trong cổng, mọi map), test `TaurusMapTests` (chunk / minimap / border, prop không chặn đường + có PNG); `build-tau-greybox-rows.py` thôi ghi đè nền.
- Collision đổi nhỏ (ngoài lối, đã test flood fill): gốc cây rừng thưa bắc suối, cột đèn / biển / bảng / giếng / giá vũ khí trong làng. Phát hành: build lại zone.
- Duyệt: vòng 1 91/100 (làng 92, đồng 90), sửa P1 cổng (tách 3 mảnh, vòm xếp sau hàng cổng), P2 khu Card, P3 rìa rừng (bản lật + cỡ khác, nhịp lệch); vòng 2: xem `taurus-environment-ingame-review-2-chatgpt.txt`.
- Còn: chuồng pet art dẹt hơn khối 7 hàng (đã thêm rào thấp phía bắc, 2 ô cạnh tây / đông vẫn hơi trống); rừng gai Bramble Thicket còn thưa (camp chiếm gần hết khu); biển "Coming soon" cổng Gemini chưa có; chưa xem online, chưa xem `--map3d`.

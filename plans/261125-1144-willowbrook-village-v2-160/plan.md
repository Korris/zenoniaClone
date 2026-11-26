# Willowbrook Village V2 (160x160) greybox

Status: dựng theo concept hồ trung tâm (25/11 ~13:00, người dùng bỏ chợ riêng); 7 tấm prop ChatGPT đã cắt + đặt; build + ảnh trong game xong. Phần "Layout" dưới là bản greybox đầu (đã thay): toạ độ hiện tại trong Tools/tau_village_greybox_layout.py. Branch `ro-soft-adventure`. Design: ChatGPT chat "Ragnarok Và Đạo Chúa" (Tay) https://chatgpt.com/c/6b03f69b-a4f8-83ec-af99-547d92463f83, answer of 25/11 11:34 (user: "làm 160x160 như chatgpt luôn, dựng greybox đi").

## Goal
Village becomes the online social hub: 160x160 tiles, 7 districts, player market with 48 shop slots, wide roads. Greybox = rows + NPC / portal / gate
coordinates + existing props moved to their new spots + ground / 3D splat rebuilt from the rows. Buildings without art yet (inn, houses, warehouse,
fountain, market awnings) stay open ground, labelled only on the layout image (no invisible walls).

## Layout (tiles, x right, y down; inclusive rects)
- Forest border 3 tiles; south wall rows 155-156 with the 4-wide gate x78-81 (portal row 156); HUD rows 157-159 solid.
- Grand square (62,62)-(97,97) plaza; oak trunk 79-81 x 68-70 on a grass island r5 round (80,69). Start (80,84).
- Guild: hall block (75,40)-(84,44), plaza 20x14 (70,45)-(89,61), Elder Rowan (73,46), Wanted board on the grass west of the plaza.
- Forge NE: block (120,30)-(126,34), apron (116,35)-(129,46), Bram (127,36); road (90,47)-(115,51) from the guild plaza, (121,47)-(125,61) down to the market.
- Market E: plaza (106,62)-(149,97), road (98,76)-(105,83) from the square; 48 slots `k` 2x2 in 3-wide cells x110-145, rows y64 / 72 / 80 / 88.
- Garden + lake NW (lake ellipse c(32,37)); gathering plaza W (30,74)-(47,85), road (48,76)-(61,83); paths x36-39 north to the lake, y52-55 east to the guild plaza.
- Stream rows 100-102 x3-58 into a pond c(60,101); bridge x36-37 on the path x36-37 south to the residential road.
- Residential SW: road (30,112)-(75,115), windmill block (26,120)-(29,123).
- Card & Pet SE: road (84,112)-(129,115), plaza (110,116)-(137,135): tent block (114,120)-(117,122) + Orin, pen ring (128,118)-(132,124) open west + Lumi, stalls + Mira.
- Main road (76,98)-(83,154); Finn (75,151); Clover arrival (79,153).

## Steps
1. `Tools/build-tau-greybox-rows.py` village() + labelled layout image; `TauWillowbrookVillageMap.cs` NPC / portal / start / arrival / chunks.
2. `Tools/tau_25d_props_layout.py` village_props (shared forest fill), `place-tau-25d-props.py` size; run until "problems: none".
3. `Tools/build-tau-25d-ground.py` (160x160, 40 px / tile, 5x5 chunks, `k` = dirt), border; `build-map-3d-splat.py` (`k` = dirt).
4. Tests (TaurusMapTests etc.), dotnet test x2; build from copy, shots (square, market, gate, guild, 3D + `--map2d`).
5. docs: pending-tasks, map-design-rules (size exception). Online: zone server must be rebuilt (rows changed).

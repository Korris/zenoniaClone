# Rootburrow thành dungeon 2.5D tối kiểu RO3 + boss mới Ambercap Warden (21/11/2026)

Người dùng: "hiện có map nào đang là dungeon không, làm cho nó có phong cách tối tối giống ragnarok 3 (https://www.youtube.com/watch?v=X87Xt--Plj8), làm 1 con boss trong dungeon luôn". Đã đọc `CLAUDE.md`, `docs/map-design-rules.md` (cả mục Map 2.5D 1-18), `docs/sprite-rules.md` (quy tắc đầu file, VFX, art ChatGPT, thiết kế quái, Sổ lỗi phần quái / boss), `docs/monster-redesign-bible.md` (A1-A7, B2 Forest / Rootburrow, Thornheart, mục 7-8), báo cáo Rimeclaw, memory.

ChatGPT (Tay, "Tân Korris"): https://chatgpt.com/c/6b01232b-687c-83ec-a5a3-a2575a5d4a3c . Định hướng: `docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md`.

## Chọn map: Rootburrow
| Ứng viên | Vì sao không / có |
|---|---|
| **Rootburrow** (36x24, Lv6-10) | CHỌN: hầm kín thật (vòng lặp hành lang quanh đảo vách, sảnh bắc / đông / tây, cổng vào hầm Thornheart), vách là gờ đá + rễ thấp nên thay bằng prop đứng dễ, chưa có boss riêng (boss nằm ở phòng Thornheart) nên thêm boss không đè ai. |
| Frostfang Den | vách băng cao vẽ liền trong nền (điều 8 Map 2.5D: phải tách vách thành sprite, chưa làm), đã có Glacier Warden. |
| Thornheart Chamber | chỉ là phòng boss 24x16 đã có Treant. |
| Sunken Reliquary | sân Hydra, không lấy. |

## Quyết định
| Phần | Cách làm |
|---|---|
| Phong cách | "Enchanted Root Catacomb": giữ rễ / nấm / hổ phách của Rootburrow, thêm ánh sáng cyan / tím và hình học phép của Irmo (RO3). Art giữ màu gốc RO Soft Fantasy, tối là do ÁNH SÁNG. |
| Profile | `View25DProfile.DarkDungeon` (camera V3 58° / FOV 27 / hero 69 px như Ro3Feel): ambient #53516F 0,34 (unit 0,48, boss 0,52), không nắng, bóng rất ngắn từ trên xuống màu tím tối, fog #5B506F từ 5 m sau hero tối đa 0,32, vignette #211C35 0,22, không đốm nắng / gió / lá, hạt bào tử hổ phách + wisp cyan. |
| Đèn | `DungeonLights`: đèn từ prop theo tên (rbLantern hổ phách 3,2 m, rbCyanCrystal cyan 3,8 m, rbGiantShroom bạc hà 2 m, rbAmberCrystal hổ phách dịu, rbSmallShrooms, rbRuneSeal tím) + 3 đèn boss tay (lõi hổ phách 4,2 m, 2 ấn cyan); 16 đèn gần tâm camera vào shader `Sprite25DLit` (nền + prop), CPU cho hero / quái / pet; quầng additive dưới đất; nhấp nháy theo spec. |
| Nền | `Tools/build-map-25d-dungeon-ground.py`: 2 texture ChatGPT (sàn đất nén, khối đất rễ) theo rows, mép không đều ~0,45 m, AO chân vách, khối đặc sâu ngả màu vực #262235; ao giữ ảnh cũ. Viền ngoài `border`. |
| Vách / prop | `Tools/place-map-25d-dungeon-props.py`: mọi prop chặn đứng trên ô đã chặn (rows KHÔNG đổi): vách bắc cao (rễ tường 3 ô, cột rễ, màn rễ), hai bên cột, phía nam lối chỉ đồ thấp nằm phẳng (đứng ở đó sẽ che hero), khối đặc có rễ / cột thưa, đồ trang trí đi xuyên trên sàn sát vách. 15 loại `rb*` (`VillagePropDefs`). |
| Boss | Ambercap Warden (`ambercapWarden`): nấm khổng lồ mũ chuông hổ phách, 3 đốm cyan, mắt hổ phách con ngươi đen, miệng W; Lv12, 1946 HP (x2 farm), Atk 76, Def 10, đứng yên, RenderScale 1,8, ô 384 / 512 như boss truy nã; `MonsterDef.SkillBoss` = dùng hệ skill boss truy nã nhưng là boss map (không Wanted). Ở sảnh bắc (19,4), hồi sinh 5 phút; 5 thornSprite sảnh bắc dời sang sảnh đông nam. |
| Skill | `WantedSkillTable.AmbercapWarden`: Amber Pulse (vòng 3 ô quanh boss, 1,3 s, 1,25x + Slow), Spore Fan (nón 90° 4,5 ô, 1,5 s, 1,4x + Poison), Rootline Burst (đường 7 x 1,5 ô về mục tiêu, 1,7 s, 1,65x + Knockback 1 ô), Bloom of the Hollow (cuồng nộ < 30 %: vòng 5,5 ô chừa 1 làn 90°, 2,3 s, 2,2x + Freeze 1,2 s). |
| FX skill | `AmbercapLightFx` + `AmbercapLightCasts`: KHÔNG sprite; cảnh báo bằng shader `WantedWarningDisc` / dải LightBlob, cast + impact bằng bộ Cleric (`ClericLightFx` timeline, `ClericLightLook.M` mask x palette, thêm `Turn`), 4 palette riêng, đỉnh sáng 120-180 ms, rung camera nhẹ chỉ ở chiêu cuồng nộ. |

## Lượng ảnh ChatGPT
1 concept boss, 4 sheet đi / đánh, 2 sheet niệm, 2 texture nền, 2 tấm prop = 11.

## Trạng thái
- [x] Khung hình clip (ngoài git `D:\temp\ro3-dungeon-frames`), hỏi ChatGPT, định hướng
- [x] Boss: concept + 6 sheet, cắt `Tools/build-wanted-boss-sheets.py write ambercapWarden --boss-only`
- [x] Code: profile, DungeonLights, shader, apron, boss def / skill / spawn / loot, FX
- [ ] Nền + prop (ảnh ChatGPT, cắt), ô chặn rà trên ảnh tô đỏ
- [ ] dotnet test 2 project, build từ bản sao, vài ảnh chụp, đánh thử boss `--god`
- [ ] Người dùng xem trong game
- [ ] Build lại zone server (MonsterDef, bảng skill, MapSpawnTable, prop map đổi)

## Rủi ro
- Server dùng chung MonsterDef / WantedSkillTable / MapSpawnTable: phải build lại zone server, phát hành cùng client.
- Shader 16 đèn x mọi sprite nền / prop: chưa đo FPS điện thoại.
- Ảnh nền mới nằm thẳng ở `Resources`; `Tools/bake-map-ro-soft-tone.py apply` sẽ ghi đè bằng bản gốc cũ trong `ro-soft-originals~`.

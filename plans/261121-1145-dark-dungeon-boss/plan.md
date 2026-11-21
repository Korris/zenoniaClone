# Rootburrow thành hang tím tối 2.5D kiểu RO3 (đi sâu dần) + boss map Ambercap Warden + Wanted boss Aetherglyph Owl (21/11/2026)

Người dùng: "hiện có map nào đang là dungeon không, làm cho nó có phong cách tối tối giống ragnarok 3 (https://www.youtube.com/watch?v=X87Xt--Plj8), làm 1 con boss trong dungeon luôn". Bổ sung trong lúc làm: "giống ở trong hang", "clip này (t=745s) hang động tối tối màu TÍM mà sao vẽ ra đất NÂU", skill không sprite nhưng phải hoành tráng, "con nấm này cute quá, cho làm boss map thôi, design thêm 1 con wanted boss khác ở đó", "map dungeon thì làm kiểu đi vào sâu, không dàn trải".

Đã đọc `CLAUDE.md`, `docs/map-design-rules.md` (cả Map 2.5D 1-18), `docs/sprite-rules.md` (quy tắc đầu, VFX, art ChatGPT, thiết kế quái, Sổ lỗi quái / boss), `docs/monster-redesign-bible.md` (A1-A7, B2, Thornheart, mục 7-8), báo cáo Rimeclaw, memory.

ChatGPT (Tay "Tân Korris"), một chat cho cả việc: https://chatgpt.com/c/6b01232b-687c-83ec-a5a3-a2575a5d4a3c . Định hướng + số: `docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md`.

## Chọn map: Rootburrow
Hầm kín thật (Lv6-10 dưới rừng Bramblewood, nối Thornheart Chamber), vách thấp nên đổi sang prop đứng dễ, chưa có boss riêng. Không chọn Frostfang Den (vách băng cao vẽ liền, điều 8 Map 2.5D chưa giải), Thornheart Chamber (phòng boss nhỏ đã có Treant), Sunken Reliquary (sân Hydra).

## Quyết định
| Phần | Cách làm |
|---|---|
| Bố cục V4 | Người dùng chơi V3: "chưa đủ độ sâu". BA TẦNG, mỗi tầng 72 x 48 (tối đa), đường chính zig-zag 3 ô + nhánh 2 ô tới hốc cụt (layout ChatGPT V4): 1F `rootburrow` (cổng đông từ Bramblewood, 5 hang Lv6-8, thang xuống tây bắc), 2F `rootburrow2` (6 hang Lv9-11, trại Crystal Camp), 3F `rootburrow3` (hang Lv12-13, hang boss nấm Lv14 giữa tầng, đấu trường Wanted 17 x 17 ở tây, vòm Thornheart). Cầu thang: prop `rbStairsDown` / `rbStairsUp` (vòm đá tím, ChatGPT) trên ô chặn ngay bắc cuối hầm, ô cổng là hàng sàn dưới nó. Rows `Tools/build-rootburrow-delve-rows.py`, lớp chung `RootburrowFloorMap`. Quái 2F-3F mới (`caveMantis` 10, `gloomWolf` 11, `crystalSprite` 12, `hollowTreant` 13) dùng sheet rừng tô tím (`FallbackSheet`) tới khi có art riêng. Đường chính 202 / 220 / 164 ô = ~40 / 44 / 33 s đi thuần (hero 80 đv/s = 5 ô/s), ~2 phút cả dungeon chưa tính đánh (V3: ~14 s). |
| Màu V2 | "Dark Violet Cave" theo khung 12:25 của clip: đá lát tím oải hương, khối đá tím tối, khe chàm, mạch cyan, pha lê tím / hồng, quả cầu rune cyan, rễ hoá đá ít. Bản V1 nâu (đất + rễ hổ phách) bị người dùng bác. |
| Profile | `View25DProfile.DarkDungeon1F/2F/3F`: camera V3 như Ro3Feel, KHÔNG nắng, ambient #414064 x 0,22 (unit 0,45, boss 0,48) x1,25 / x1 / x0,82 theo tầng, bóng ngắn tím tối, fog chàm #292544 tối đa 0,40, vignette #100E25 0,32, hạt cyan / tím, giọt nước rơi. |
| Đèn | `DungeonLights`: theo tên prop (rune orb cyan 4 m, obelisk cyan, pha lê tím 3,2 m, pha lê hồng 2,8 m, nấm, ổ pha lê, ấn rune) + đèn tay ở 2 hang boss; 16 đèn gần tâm camera vào `Sprite25DLit`, CPU cho unit, quầng additive, hero mình mang đèn mềm, đòn boss loé sáng (`Flash`). |
| Nền / prop | 2 texture tím (`Tools/build-map-25d-dungeon-ground.py`, 2 mảnh 1920x1536 + minimap + border), 15 prop `rb*` vẽ cho góc 58° (`Tools/cut-map-25d-dungeon-props.py`), đặt bằng `Tools/place-map-25d-dungeon-props.py` (prop chặn chỉ trên ô đã chặn; phía nam lối chỉ đồ thấp), kiểm `Tools/preview-map-25d-dungeon.py`. |
| Boss map | Ambercap Warden (`ambercapWarden`): nấm khổng lồ mũ chuông hổ phách 3 đốm cyan, Lv14 (V4), `MonsterDef.SkillBoss` (hệ skill boss truy nã nhưng là boss map), 3F ô (38,21), hồi 5 phút. Skill: Amber Pulse (vòng 3,5 ô), Spore Fan (nón 90° 5,5 ô, độc), Rootline Burst (đường 8 x 1,8 ô, đẩy lùi), Bloom of the Hollow (cuồng nộ, vòng 6,5 ô chừa 90°, đóng băng). |
| Wanted boss | Aetherglyph Owl (`aetherglyphOwl`): cú vua phù văn tím, vương miện rune cyan, giáp vai vàng, cánh pha lê; Lv20, `Wanted(...)` basis 2400/100/13/32, drawMul 1,4, đấu trường 3F ô (12,22), thứ 4 trong `WantedBoss.Rotation` (400 G, 4 đá, 3+3 bình Medium). Skill: Rune Starfall (5 vòng 2 ô lên 5 người, chậm), Astral Wingstorm (nón 110° 7 ô, đẩy lùi), Crystal Judgment (đường 10 x 1,8 ô, đóng băng), Eclipse of Runes (cuồng nộ, vòng 8 ô chừa 150°). |
| FX skill | Không sprite: cảnh báo shader `WantedWarningDisc` / dải LightBlob (`CaveBossLightFx`), cast + impact bằng bộ Cleric (`ClericLightFx`, `ClericLightLook.M`, thêm `Turn`): `AmbercapLightCasts`, `AetherglyphOwlLightCasts`. Hoành tráng: vòng rune lớn, cột sáng, gai pha lê, sóng xung kích đôi, loé sáng cả hang, rung camera nhẹ cho người đứng trong. |
| Offline | Cảnh báo / FX skill boss trước đây chỉ hiện online; nay offline tự vẽ (`WantedSkillTelegraphFx.ClockMs`). |

## Ảnh ChatGPT (Tay): ~24
Boss nấm 7 (concept + 6 sheet), nền + prop V1 nâu 4 (bỏ), nền + prop V2 tím 4, Wanted cú 7 (concept + 6 sheet).

## Trạng thái
- [x] Khung hình clip (ngoài git `D:\temp\ro3-dungeon-frames`), định hướng V1 -> V2 tím -> bố cục V3
- [x] Boss nấm: art + code + skill + FX; chạy offline: 4 skill có cảnh báo + FX, hero Lv40 `--god --bot fight-boss` hạ được boss
- [x] Hang tím V2 + bố cục V3 (rows, prop, nền, minimap), `PROBLEM` không có
- [x] Wanted cú: dữ liệu, skill, FX code, art (đang vẽ / cắt)
- [x] dotnet test 2 project
- [x] V4 ba tầng: rows, prop, nền, minimap, cầu thang, quái 2F-3F, bảng spawn; `PROBLEM` không có; đi qua cầu thang 1F -> 2F chạy; ảnh `docs/reference/rootburrow-dark-25d/ingame-v4-*.jpg`, `worldmap-v4-*.jpg`
- [x] V5 (21/11 18:25): 9 rương, 3 pha lê dịch chuyển, chuỗi quest Elara, giữ 72 x 48 (`docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md`, `Rpg/DungeonPoints.cs`)
- [ ] Người dùng xem trong game
- [ ] BUILD LẠI ZONE SERVER: MonsterDef (2 boss mới, thêm cuối `All`), bảng skill, `MapSpawnTable`, rows / prop / cổng Rootburrow + 2 map mới `rootburrow2` / `rootburrow3`, 4 quái mới, `WantedBoss.Rotation` (vòng ngày đổi từ 3 sang 4 boss), Bramblewood / Thornheart trỏ cổng mới

## Rủi ro
- Hero đã lưu ở Rootburrow toạ độ cũ: map đổi 36x24 -> 72x48; server đưa về cổng nếu chỗ cũ bị chặn (`ServerHeroFactory`), không có bản dịch toạ độ.
- Thêm boss vào `Rotation` đổi lịch boss mỗi ngày (ngày nay rơi vào boss khác).
- Shader 16 đèn chưa đo FPS điện thoại; nền 2 mảnh 1920 px (Android nhập tối đa 2048, đủ).
- Ảnh nền mới ghi thẳng `Resources`; `Tools/bake-map-ro-soft-tone.py apply` sẽ ghi đè bằng bản gốc cũ trong `ro-soft-originals~`.

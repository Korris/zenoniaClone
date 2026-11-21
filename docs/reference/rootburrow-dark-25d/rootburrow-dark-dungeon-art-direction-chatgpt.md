# Rootburrow thành dungeon 2.5D tối kiểu RO3: art direction (ChatGPT, 21/11/2026)

Chat (Tay, "Tân Korris"): https://chatgpt.com/c/6b01232b-687c-83ec-a5a3-a2575a5d4a3c
Ảnh gửi: 2 contact sheet clip RO3 "Irmo Dungeon (Normal)" https://www.youtube.com/watch?v=X87Xt--Plj8 (khung lấy mỗi 4 s, giữ ngoài git ở `D:\temp\ro3-dungeon-frames`), nền Rootburrow cũ, ảnh Willowbrook 2.5D V3.
Đây là bản tóm tắt số liệu của câu trả lời (tiêu đề ChatGPT: "ROOTBURROW — Dungeon 2.5D Dark Fantasy V1"). Số là đề xuất art direction, không đo từ RO3.

## Chủ đề: "Enchanted Root Catacomb" (hầm rễ hổ phách ma thuật)
Giữ rễ, nấm, đất, hổ phách của Rootburrow; thêm ánh sáng cyan / tím và vòng phép hình học của Irmo. Không biến thành hang pha lê. Môi trường tối hơn Willowbrook ~45-55 % độ sáng cảm nhận, nhân vật / boss vẫn sáng. Art giữ màu gốc (không vẽ tối sẵn): Final = Albedo x (Ambient + đèn điểm + bounce), rồi grade + fog.

## A. Ánh sáng
| Mục | Giá trị |
|---|---|
| Ambient | #53516F, 0,34 (sàn); nhân vật / quái tối thiểu 0,48; boss 0,52 |
| Màu gốc | sàn #A38A70, rễ #B68D61, vách tối #51475D, khe rễ #393449, mạch phép #69D5D9, hổ phách #F5BD67, tím phép #AC8DD9, điểm sáng #FFF1CB |
| Đèn | Amber Lantern #FFC47A 3,2 m x0,65 (1 / 9-12 m); Cyan Crystal #73DDE5 3,8 m x0,80 (1 / 12-16 m); Glow Mushroom #B5E7C2 2,0 m x0,35 (1 cụm / 7-10 m); Violet Rune #AE90E2 3,0 m x0,55 (góc rẽ); Boss Amber Core #FFD08C 4,2 m x0,95; Boss Cyan Seal #75E8E6 3,5 m x0,65 (2 bên boss) |
| Số đèn | 12 chính + 4 động trong khung nhìn, hysteresis 15 % |
| Nhấp nháy | hổ phách ±6 % 1,8-2,6 s; pha lê ±3 % 3 s; nấm ±8 % 2,2-3,5 s; đèn boss ±10 % |
| Quầng đất additive | bán kính 0,75-0,9 bán kính đèn, alpha tâm 0,12-0,22, mềm về 0 |
| Grade | saturation 0,88, contrast 1,10, shadow #393551, midtone #827B90 8 %, highlight #FFE9C6 6 %, bloom ngưỡng 1,1 cường độ 0,16-0,23 |
| Fog | #5B506F: 0 ở hero, 0,08 ở 5 m, 0,20 ở 11 m, tối đa 0,32; không phủ HUD / telegraph / số |
| Vignette | #211C35, 0,22, mềm 0,65 |
| Bóng | bỏ FakeSun ngoài trời: bóng chân #241D32 0,32-0,42; bóng prop rất ngắn 0,15-0,35 m alpha 0,2-0,3; AO giả chân vách 0,25-0,45 m alpha 0,2-0,35; boss 1,6 x 0,65 m alpha 0,35 |
| Hạt | bụi 2-5 px sống 3-6 s ở vùng sáng; bào tử hổ phách 3-5 / s quanh nấm lớn alpha 0,2-0,4; wisp cyan 1-2 / s quanh pha lê 0,1-0,25 m/s; sương đất 0,1-0,25 m alpha 0,06-0,12 ở góc tối; sảnh boss giảm 50-70 % khi đánh |

## B. Nền + prop
- Texture 1 Floor: đất nâu vàng nhạt, đá vụn, rễ chìm; 70 % ít chi tiết, 20 % vết đất sỏi, 10 % mạch hổ phách / cyan mảnh. Không vẽ sáng / bóng / vật cao.
- Texture 2 SolidBase: ngoài lối đi + đảo giữa, đất rễ tối hơn 12-18 % albedo, đá bị rễ ôm, nứt; không vẽ vách nhìn thẳng.
- Mép chuyển mask 0,35-0,55 m không đều + sỏi / rễ thấp che. Collision theo ô.
- Vách ở 58°: sprite đứng neo chân ở ranh ô chặn, cao 1,5-2,5 m, chia khúc 1-3 m chồng 0,15-0,25 m; che hero thì mờ 0,30 trong 150 ms.
- 16 prop: Root Wall Segment 3x2,2 (chặn), Root Pillar 1,2x2,5, Twisted Root Arch 3x3, Root Boulder 2x1,2, Stone Cluster 1,5x0,8, Amber Crystal Cluster 1,4x1,6, Cyan Rune Crystal 0,8x1,4, Giant Glow Mushroom 1,5x1,8, Small Glow Mushrooms 0,9x0,5 (đi xuyên), Amber Lantern Root 0,8x1,7, Fallen Root Log 2,5x0,45 (xuyên), Root Pebble Scatter (xuyên), Violet Fungus Cluster 1,2x0,7 (xuyên), Hanging Root Curtain 2x2,2, Rune Floor Seal (xuyên), Amber Egg Cluster 1,2x0,9. Lật / scale 0,85-1,15 / tint ±5 %.
- Viền ngoài map: vách rễ 2-3 m, rễ lớn chồng, đá tối, nấm lớn, khe vực #262235 có sương tím; rộng 3-5 m (bắc 5-6 m); rễ 1 / 5-7 m², đá 1 / 8-12 m², nấm 1 / 10-15 m², fade 2 ô.
- Sàn chiến đấu 65-75 % sạch để telegraph nổi.

## C. Boss mới: Ambercap Warden (chọn trong 3: Ambercap Warden / Lumen Burrow Beetle / Rootdrop Guardian)
- Nấm khổng lồ mũ chuông rộng (5-7 gờ, mép rủ mềm), thân tròn kem, 2 chân rễ ngắn, 2 tay rễ ngắn 3 ngón tròn. Mũ 55 % cao, thân 30 %, chân 15 %. Cao 1,8 hero, rộng 1,7 hero. Đối xứng trái / phải.
- Mặt trên thân: 2 mắt lớn con ngươi đen, lông mày cong, miệng nhỏ chữ W; trán dấu rune hổ phách hình thoi; 3 đốm sáng cyan trên mũ (trái-giữa-phải).
- Tính cách: hiền khi ngủ, dữ khi bảo vệ tổ. Cute 45 / Threat 55.
- Màu: mũ #B77B68, mép mũ #E7B287, thân #E8D4AD, chân rễ #987454, lõi hổ phách #F6C56D, rune cyan #87E5DF, viền #594A48, con ngươi #24212B. Không vẽ tím lên boss.
- Động tác: idle nhún 1,8 s, mũ co giãn 3 %, chớp mắt 3-5 s; đánh nghiêng trước 10°, hai tay đập xuống ~550 ms; niệm mũ nâng, 3 rune sáng dần ~900 ms; cuồng nộ mắt nheo, mũ mở rộng 8 %, hổ phách sáng vàng.
- Sảnh: boss giữa phía bắc cách tường ~1,2 m, trống ≥ 8 x 4 m trước mặt.

## D. Skill (ChatGPT; số đã chỉnh theo hệ thống game xem `plans/261121-1145-dark-dungeon-boss/plan.md`)
1. Amber Pulse: Circle tâm boss R 1,8 m, cảnh báo 1300, hồi 7000, 1,25x + Slow 25 % 2 s.
2. Spore Fan: Cone 90° dài 3,5 m về 1 người, 1500 / 10000, 1,40x + Poison 3 s.
3. Rootline Burst: Line dài 4,5 m rộng 0,75 m, OnPlayers tối đa 2, 1700 / 13000, 1,65x + Knockback 0,8 m.
4. Bloom of the Hollow (cuồng nộ < 30 %): Circle R 3,6 m chừa 90° an toàn cố định, 2300 / 22000, 2,2x + Freeze 1,2 s.
Telegraph: vùng hổ phách #E9A96E alpha 0,18-0,25, viền #FFE0A0 0,75; làn an toàn teal #5DE0C5 0,22 có ký hiệu.

## E. FX (RO3 Soft Fantasy V1: hình học 60 / glow 25 / hạt 15, đỉnh sáng 100-180 ms, vòng đất nằm phẳng)
1. Amber Pulse (#F3BA70 + #D98B76, lõi #FFF0C8): 0-500 vòng rune 12 ký hiệu hiện dưới boss; 500-1300 vòng ngoài quay 35°/s, 6 dấu thoi sáng dần; 1300-1450 sóng vòng mesh dày 0,18 m lan tới bán kính, đỉnh 150; 1450-1850 rune tan (dissolve), 8-12 mảnh sáng bay lên.
2. Spore Fan (#B7D995 + #E6C781, lõi #FFF2C9): 0-700 quạt 90° 5 nan rune cong; 700-1500 nan sáng từ trong ra; 1500-1650 5 dải cánh hoa quét ra, đỉnh 120; 1650-2100 12-18 bào tử tròn bay thấp tan. Không mây độc che nhân vật.
3. Rootline Burst (#C99A70 + #77DAD2, lõi #FFF0CF): 0-700 hai mép song song + ô rune; 700-1700 rễ sáng chạy dọc line; 1700-1830 3-4 trụ rễ thấp mesh nhô (≤ 0,45 m); 1830-2250 rút xuống, 6-10 mảnh đá sáng bắn thấp. Knockback ở trụ đầu.
4. Bloom of the Hollow (#8CDDD6 + #F4BC7C, lõi #FFF5D5): 0-600 2 vòng đồng tâm 16 rune; 600-1500 8 cánh hoa hình học mở, làn an toàn không có cánh; 1500-2300 vòng ngoài quay 45°, ấn hổ phách tâm tụ sáng; 2300-2480 7/8 cung bùng như cánh hoa sáng, đỉnh 180; 2480-3000 sáng rút về tâm, 16-24 hạt rune tan. Rung camera nhẹ 100 ms (chỉ chiêu này).

## V2 "Dark Violet Cave" (người dùng bác V1 nâu: "hang động tối tối màu TÍM", khung clip 12:25 gửi kèm)
- Màu gốc: sàn đá #77718F (viên sáng #928AA9, khe #46415F), khối đá ngoài lối #514C70, vách #625B80 (bắt sáng #9387AD, bóng #302C4D), pha lê tím #9567D4, hồng #D88BCB, rune #56D6F5 (lõi #B5F5FF), vực #17152F; sàn 75 % đá / 15 % nứt / 10 % mạch cyan mảnh. Rễ chỉ còn 15-20 %, thành rễ hoá đá #71617E.
- Ambient #414064 x 0,22; nhân vật / boss sàn 0,42-0,48. Đèn: Cyan Rune Orb #65DBFF 4 m 0,95 ±5 % 2,5 s; Purple Crystal #A277F0 3,2 m 0,75; Pink Crystal #ED91D6 2,8 m 0,65; Blue Fissure #538FF5 2,5 m 0,45; Glow Mushroom #A9E9DF 2 m 0,40; Boss Rune Seal #72E7FF 4,5 m 1,05. Quầng 0,12-0,22, 70-85 % bán kính. Bóng chân #19172F 0,35, không FakeSun.
- Fog #292544: 0 ở hero, 0,10 ở 5 m, 0,24 ở 10 m, tối đa 0,40; vignette #100E25 0,32; saturation 0,9, contrast 1,15, shadow #252443, highlight #A2C7EC. 70-75 % màn hình trong bóng.
- 15 prop: Violet Stone Wall A / B (3 x 2,6), Ancient Stone Pillar (1,3 x 2,9), Hanging Cave Roots (2 x 2,4), Giant Purple Crystal (1,8 x 2,1), Pink Crystal Cluster (1,4 x 1,6), Cyan Rune Obelisk (1 x 1,8), Floating Rune Orb (0,9 x 1,9), Violet Crystal Nest, Cave Stone Boulder, Small Stone Cluster, Tiny Glow Mushrooms, Cave Pebble Scatter, Fallen Stone Slab, Cyan Rune Floor Seal.

## V3 bố cục "đi sâu dần" (người dùng: "map dungeon thì làm kiểu map đi vào sâu ấy")
60 x 24, cổng ĐÔNG -> TÂY: cổng 4 x 5 -> hầm 2-3 m (Lv6) -> hang I 8 x 7 (Lv6-7) -> hầm uốn -> hang II 8 x 8 (Lv8-10) -> hầm sâu -> hang Ambercap 10 x 9 (boss Lv12) -> hầm cuối -> đấu trường Wanted 17 x 17 (Lv20, ~7 m²/người cho 40 người, 85 % sàn trống). 5-6 khúc quanh, 4 hốc phụ, vòm Thornheart ở tây bắc đấu trường. Ambient theo độ sâu: 0,30 (cổng) -> 0,26 -> 0,23 -> 0,21 -> 0,17 (hầm cuối) -> 0,18 (đấu trường).

## Wanted boss: Aetherglyph Owl (chọn trong Aetherglyph Owl / Amethyst Crown Stag / Celestial Cave Manta)
- Cú vua phù văn: thân tròn mũm mĩm lông #80769F, ngực bạc tím #D8D0EC, cánh 3 tầng lông phiến pha lê (#574D83, mép #A77DE5), 2 chùm lông tai, vương miện rune cyan #60DDF5, mắt vàng #F3D98E con ngươi đen to 2 chấm sáng, lông mày nghiêm, viền #39334F. Cute 35 / Threat 65, cao 2,5 hero, sải cánh 3,3 hero. Đối xứng.
- Skill (ChatGPT; số game chỉnh trong `WantedSkillTable.AetherglyphOwl`): Rune Starfall (vòng 1,6 m lên tối đa 5 người, 1600 / 9000, 1,35x + Slow 25 %), Astral Wingstorm (nón 110° 6 m, 1800 / 13000, 1,65x + đẩy 1,2 m), Crystal Judgment (đường 8 x 1 m, 2000 / 16000, 1,85x + Freeze 1 s), Eclipse of Runes (cuồng nộ: vòng 7 m chừa 150°, 2800 / 28000, 2,5x + Slow 40 % 3 s).
- FX quy mô raid: Starfall 5 vòng phép #75DDF5 + ấn lục giác + cột năng lượng tím #A77DE5 rơi; Wingstorm 6 cung rune tím + 2 cánh sáng cyan quét ra 240 ms; Judgment line rune + 5-7 gai pha lê nhô 0,8-1,2 m + tia trắng-cyan; Eclipse 3 vòng R 2,5 / 5 / 7 m, 12 phiến pha lê xoay, 10-14 cột rune cao 2 m ngoài làn an toàn. Trần 80-120 hạt / skill (cuồng nộ 180).

# Yêu cầu art kỹ năng Assassin gửi ChatGPT

Kiểm kê ngày 2026-10-31 trên working tree nhánh `unity`. Chưa gửi gì lên ChatGPT, chưa sửa code hay art.

Đường dẫn trong file này tính từ gốc repo. `fx/` nghĩa là `Assets/Resources/Sprites/fx/`, `boards/` nghĩa là `plans/261030-1521-skill-trees-implementation/assassin-boards/`.

## 1. Kết luận

- **Hiện không còn bộ art nào thiếu file.** Lúc bắt đầu kiểm kê, `fx/deathmark/shatter-1..6` chưa có. Sáu file này xuất hiện lúc 17:57 ngày 2026-10-31 trong khi đang kiểm kê, do một tiến trình khác tạo: chưa commit, không có raw trong repo, chưa rõ nguồn. Chúng có đủ 6 khung (420x500) nhưng khung 1-4 dính caro, nên vẫn cần làm lại (prompt A).
- **Lỗi lớn nhất là ô caro xám bị dính vào art:** 7 bộ khung có caro hoặc mảng trắng bán trong suốt nhìn thấy được, hai bộ trong đó treo trên quái tới 10 giây.
- **Trước khi gửi, kiểm tra lại `fx/deathmark/`,** vì repo đang được nhiều tác vụ sửa cùng lúc.
- **Nguyên nhân caro đã rõ:** ChatGPT đôi khi vẽ caro thật vào ảnh RGB thay vì trả alpha. Các raw RGBA còn lưu đều sạch, các raw RGB đều bị (xem mục 6).
- **Pose và sheet nhân vật không thiếu gì.** Không cần yêu cầu art nào cho hero (mục 3).
- **Mâu thuẫn tên tài khoản cần chốt:** yêu cầu lần này nói "Tay Plus", còn `phase-06` và `docs/session-handoff.md` ghi art kỹ năng làm trên "Tan Pham" (handoff có ghi "Tay" cũng là của user). Vì vậy các prompt dưới đây tự đủ ý, không dựa vào chat cũ.

## 2. Bảng thiếu và lỗi theo skill

`T/B/L/R n` là số pixel alpha > 128 nằm sát mép trên/dưới/trái/phải của khung, tức phần art bị cắt cụt. Caro được xác nhận bằng cách phóng to 100% và tách kênh alpha. Bộ lọc tự động chỉ bắt chắc được `beam` và `ring`, nên các dòng còn lại là đánh giá bằng mắt.

| Ưu tiên | Skill | Bộ khung | Lỗi | Bằng chứng | Prompt |
|---|---|---|---|---|---|
| P1 | Death Mark | `deathmark/shatter-1..6` | Mới có lúc 17:57 (trước đó thiếu, `DeathMarkFx.cs:63` nạp mà thư mục chỉ có 18 file). Caro rõ ở khung 1-4, không có raw | Phóng to vùng giữa các khung | A |
| P2 | Death Mark | `deathmark/ring-1..6` | Caro trong lòng vòng và trong khói. Hiện liên tục trên quái khi còn ấn | Lọc caro 1-6 ô mỗi khung, ô khoảng 8 px | B |
| P2 | Venom Daggers | `venomdaggers/skull-1..6` | Caro trong khói xanh, cụt đỉnh T4/T48/T27 ở khung 2-4. Hiện suốt thời gian độc | Phóng to `skull-3` | C |
| P2 | Nightfall | `nightfall/cross-1..6` | Caro và mép răng cưa, cụt T42 ở khung 2. Lõi trắng khung 4 bán trong suốt (alpha 170-252, 4779 px) nên giữa chữ X nhìn như đĩa xám ngay lúc đỉnh | Raw `nf-cross-raw.png` là RGB có caro vẽ sẵn | D |
| P2 | Death Mark | `deathmark/beam-1..6` | Caro trong quầng sáng, cụt B69 ở khung 2, mảnh lạc ở cuối khung 1 và 3 (x 754-836, 995-1028, 985-1020) | Lọc caro 14-33 ô ở khung 2-5, ô khoảng 8 px | E |
| P2 | Venom Daggers | `venomdaggers/burst-1..6` | Caro rõ ở khung 4-6, cụt T13 ở khung 3 | Phóng to `burst-5` | F |
| P2 | Shadow Clone | `shadowclone/dissolve-1..6` | Caro trong khói tím ở cả chuỗi | Contact sheet và phóng to `dissolve-3` | G |
| P3 | Nightfall | `nightfall/shadow-1..6` | Cụt L41/L30/L6 ở khung 2-4, R55 ở khung 5. Mảnh lạc ở khung 2, 4, 6 | Raw `nf-shadow-raw.png` là RGB có caro | H |
| P3 | Nightfall | `nightfall/spin-1..6` | Cụt R11 ở khung 3, T21 và R30 ở khung 6. Khung 4-6 lặp suốt lúc xoay nên lỗi khung 6 lộ nhiều lần | Đo mép | I |
| P3 | Blade Dance | `bladedance/ring-1..6` | Cụt T23, B3 ở khung 6 | Đo mép | J |
| P3 | Blade Dance | `bladedance/finish-1..6` | Cụt T14 ở khung 1 | Đo mép | K |
| P3 | Shadow Clone | `shadowclone/slash-1..6` | Cụt L17 ở khung 4, mảnh lạc x 499-524 | Đo mép | L |
| P3 | Shadow Strike | `shadowstrike/cross-1..6` | Vệt caro cục bộ cạnh lõi ở khung 3, chỉ hiện chừng 65 ms | Phóng to `cross-3`, raw là RGB có caro | M |
| P3 | Shadow Strike | `shadowstrike/slash2-1..6` | Vệt xám giữa cung trắng ở khung 3 | Phóng to `slash2-3`, raw là RGB có caro | N |

P3 là lỗi nhỏ, chỉ hiện trong vài chục ms. Nên xem trong game trước rồi mới tốn quota.

**Theo từng skill**

| Skill | Tình trạng |
|---|---|
| Shadow Strike | Không thiếu. Hai lỗi P3 |
| Shadow Step | Không thiếu, không có lỗi đáng yêu cầu lại |
| Blade Dance | Không thiếu. Hai lỗi P3 |
| Phantom Rush | Không thiếu, không có lỗi đáng yêu cầu lại |
| Venom Daggers | Không thiếu. Hai lỗi P2 |
| Shadow Clone | Không thiếu. Một lỗi P2, một lỗi P3 |
| Death Mark | Không còn thiếu file, nhưng `shatter` mới dính caro (P1). Hai lỗi P2 |
| Nightfall Execution | Không thiếu. Một lỗi P2, hai lỗi P3 |

**Sửa được bằng script, không cần ChatGPT**

- `nightfall/cast-6`: cột sáng bị cắt ngang đột ngột tại y=54 (dòng 52 có 0 px, dòng 54 có 61 px). Làm mờ dần khoảng 40 px trên đỉnh cột.
- `venomdaggers/dagger-6`: xóa mảnh tam giác trắng lạc ở x khoảng 26-35.
- Nếu không làm lại H và L: xóa mảnh lạc của `nightfall/shadow-2,4,6` và `shadowclone/slash-4`.

**Bỏ qua vì quá nhỏ:** `venomdaggers/cast-3,4,5` (B6/B2/B6), `deathmark/star` (sạch), `shadowstep/stab-3` (0 px alpha > 128 ở mép).

## 3. Những thứ đã kiểm tra và không có lỗi

- **Sheet của Shadow Clone:** `HeroClassDef.Assassin.SheetFolder` là `assassin`. Có đủ `Sprites/assassin/idle.png`, `walk.png` (768x768, 4x4) và `melee1-3.png` (1152x768, 6x4), khớp ô 192 px. Các sheet melee có dao vẽ sẵn nên bản sao tô tối vẫn cầm dao.
- **Pose:** `attack3`, `melee1-4`, `nightfall` đều có ở `assassin/` và `assassin-bare/`, đủ 4 hàng 6 khung, nên mọi chỉ số khung trong code đều hợp lệ.
- **`nightfall.png`:** bản armed và bare giống hệt nhau (cùng md5), không có dao vẽ sẵn. Không phải lỗi, vì dao được gắn qua anchor `nightfall` trong `Data/held-assassin-melee.json`.
- **Số khung:** mọi bộ đang có đều đủ 6 khung và cùng canvas trong mỗi bộ.
- **Comment "24 frames" ở `DeathMarkFx.cs:56`:** giờ đã đúng vì có `shatter` (4 x 6). Không cần sửa comment.

## 4. Khác biệt giữa spec và code

Các điểm này **không tạo yêu cầu art**, vì code đang bám theo các board trong `boards/`, còn `vfx-choreography-assassin.md` là spec cũ hơn. Cần user quyết định nếu muốn theo spec.

| Skill | Spec mô tả | Code hiện tại | Ghi chú |
|---|---|---|---|
| Blade Dance | "crossed-blade snap" khi kết thúc | Vòng chém tan thành tia và mảnh | Board panel 4 cũng là tia |
| Phantom Rush | Tia va chạm và vòng sóng ở nhát 1, 2 | `Cut()` chỉ có hạt, bụi, rung | Nếu muốn, tái dùng `burst` thu nhỏ bằng code |
| Phantom Rush | Nhát 3 "steel finisher" | `burst` hoàn toàn tím | Board cũng tím |
| Venom Daggers | Chém đôi hẹp, "no poison cloud" | Quạt 7 dao, nổ độc xanh, đầu lâu | Board vẽ đúng như code |
| Shadow Clone | Chỉ một xung nền nhỏ gọn | Khói bốc cao quanh hero và bản sao | Spec có 600 ms accent, code thêm cả vòng đời bản sao |
| Death Mark | Ấn nhỏ rồi biến mất, không duy trì | Sao và vòng giữ suốt thời gian ấn, vỡ khi hết charge | Board panel 3, 4 vẽ đúng như code |
| Nightfall | 7 nhát chém mảnh tích lũy, X nổ ở 840 ms | Vòng xoay, 6 bóng lao vào, X ở 800 ms | Nhát trúng cuối vẫn ở 840 ms trong `SkillTable`, lệch 40 ms với hình |
| Shadow Step | Blink 0, hit 80, lock 260 | Blink 180, hit 300, lock 480 | `SkillTable` đã ghi là bản làm lại |

## 5. Cách gửi

1. **Mỗi sheet một chat mới.** `plans/261029-1720-chatgpt-pending-art/plan.md` ghi chat dài dễ treo.
2. **Không đính kèm ảnh có caro.** Đừng đính kèm các khung đang lỗi hoặc raw RGB. Danh sách đính kèm dưới đây chỉ dùng khung sạch và board.
3. **Thứ tự:** A, rồi B đến G (7 ảnh). P3 chỉ làm khi còn quota. Plan cũ ghi quota ChatGPT Plus từng hết giữa chừng ngày 2026-10-29.
4. **Kiểm tra ngay khi nhận ảnh** theo mục 6. Nếu không đạt, gửi tin nhắn sửa ở mục 7 thay vì nhận về.

## 6. Tiêu chí nhận ảnh

Đo trên raw đang có trong `Assets/ArtLibrary/custom/`:

| Loại raw | File | Tỉ lệ pixel alpha gần 0 | Kết quả |
|---|---|---|---|
| RGBA | `nf-cast`, `nf-spin`, `pr-gather`, `pr-rush`, `st-*`, `ss-slash1` | 0.56 - 0.88 | Sạch |
| RGB | `nf-cross`, `nf-shadow`, `pr-burst`, `ss-cross`, `ss-slash2` | 0.00 | Có caro vẽ sẵn |

Một ảnh đạt khi đủ cả các điều kiện sau:

- Là PNG RGBA, có trên 30% pixel alpha gần 0.
- Không có caro khi đặt lên nền đặc và phóng to 100%, nhất là chỗ khói và quầng sáng bán trong suốt.
- Không nét nào chạm mép ô, và có đủ khoảng trống giữa các ô.
- Lõi trắng đục hoàn toàn (alpha gần 255).

Lưu ý về đường lui: nền đen và `--from-black` (alpha = max(R,G,B)) sẽ làm mất các mảnh đen tím là đặc trưng của bộ art này, nên chỉ dùng nếu chấp nhận mất chi tiết đó. Nền xanh lá `#00FF00` cho art tím là lựa chọn khác, nhưng repo chưa có công cụ tách nền theo màu.

## 7. Tin nhắn sửa dùng chung

Khi ảnh trả về có caro hoặc không có alpha:

```
This image has a checkerboard painted into its pixels and no real alpha channel, so it cannot be used. Output the same artwork again as a PNG with a real alpha channel: empty areas must be fully transparent pixels (alpha 0), and soft glow and smoke must be real partial alpha. Do not draw any checkerboard or grey squares anywhere, including inside glow and smoke. Keep the same frames, layout, sizes and positions.
```

Khi nét chạm mép hoặc tràn sang ô bên cạnh:

```
Some strokes touch the cell edges or run into the neighbouring cell. Redraw the same frames about 15% smaller around the same fixed center, so every stroke, shard and glow stays fully inside its own cell with clear empty transparent space around it. Same layout, transparent background, no text.
```

## 8. Prompt

Mọi prompt đều yêu cầu ảnh 1536x1024 chia 3 cột x 2 hàng (ô 512x512), trừ E là 2172x724 chia 2 cột x 3 hàng. Đọc khung theo thứ tự từ trái sang phải, từ trên xuống dưới.

### A. Death Mark `shatter` (P1, thay bộ đang dính caro)

Không đính kèm các file `shatter` hiện tại, vì chúng có caro.

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/death-mark.png` (panel 4 "Ấn vỡ")
- `Assets/Resources/Sprites/fx/deathmark/star-3.png`
- `Assets/Resources/Sprites/fx/shadowstep/stab-3.png`
- Tùy chọn, tham khảo hình khối: `Assets/ArtLibrary/custom/skill-vfx/assassin/death-mark.png` (hàng 3)

```
Use case: game-asset. ONE production VFX sprite sheet for the assassin skill DEATH MARK: the moment the mark SHATTERS on an enemy. Image 1 is the design board: copy the burst in panel 4, without the hero, the monster, the number or the UI. Image 2 is the star sigil that breaks apart: same four-pointed white-hot star, dark ring and black-violet shards. Image 3 shows the exact rendering to match: crisp hand-painted anime chibi RPG effect, thin white-hot strokes, saturated violet edges, sharp black-violet shards.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. The effect is centered exactly on the center of every cell, same center in all 6 frames. Keep everything inside the central 440 pixels of each cell with empty transparent space around it.
Frame 1: the four-pointed star sigil cracks, thin bright fracture lines, still compact.
Frame 2: white-hot flash, the star bursts, long thin rays start in an X pattern.
Frame 3: biggest frame: long sharp violet-white rays in all directions, opaque white core, black-violet crystal shards flying out.
Frame 4: rays thin out, the core shrinks, shards farther out, wisps of dark violet shadow.
Frame 5: only short rays, scattered shards and thin wisps.
Frame 6: a few small shards and sparks almost gone.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No purple backdrop or haze filling the cell. No hero, no monster, no ground, no text, numbers, gridlines or UI. Not pixel art. No green, no gold, no fire.
```

### B. Death Mark `ring` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/death-mark.png` (panel 3, vòng dưới chân)
- `Assets/Resources/Sprites/fx/deathmark/star-3.png`
- `Assets/Resources/Sprites/fx/shadowclone/rise-3.png`

```
Use case: game-asset. ONE production looping VFX sprite sheet: the seal of the assassin skill DEATH MARK, locked under an enemy's feet. Image 1 panel 3 shows the design: a glowing ground circle with star points and thin shadow flames rising from it, without the monster. Images 2 and 3 show the exact rendering to match: crisp hand-painted anime chibi RPG effect, thin white-hot lines, violet edge, black-violet wisps.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. In every cell the ground ellipse is centered horizontally with its center at 78% of the cell height (400 pixels from the top), same place in all 6 frames. The ellipse is flat, seen in 3/4 top-down view, about 380 pixels wide and 110 pixels tall, with a double thin white-violet line and four small four-pointed star sparks on it.
The inside of the ellipse stays almost EMPTY and transparent: a monster stands there and must remain visible. Thin dark violet flames and wisps rise only from the ring's edge, up to about 300 pixels above it, with a few small star sparks.
SEAMLESS LOOP: frames 1 to 3 the flames grow, frames 4 to 6 they sink, and frame 6 flows back into frame 1. The ellipse keeps its size and position, only its brightness pulses.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in soft wisps. Do NOT paint a checkerboard or grey squares anywhere. No filled disc, no purple backdrop. Keep everything inside each cell. No monster, hero, ground texture, text, gridlines or UI. Not pixel art.
```

### C. Venom Daggers `skull` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/venom-daggers.png` (panel 4)
- `Assets/Resources/Sprites/fx/venomdaggers/cast-5.png`
- `Assets/Resources/Sprites/fx/deathmark/star-3.png`

```
Use case: game-asset. ONE production looping VFX sprite sheet: the POISON SKULL that floats over a poisoned enemy in the assassin skill VENOM DAGGERS. Image 1 panel 4 shows the idea: a small glowing purple skull over the enemy. Image 2 shows the exact green rendering to match (crisp, thin bright acid-green strokes). Image 3 shows the exact violet rendering to match.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. In every cell a front-facing cute-menacing skull with white-pink glow and violet flame on top is centered horizontally with its center at 40% of the cell height, same place and same size in all 6 frames. Skull about 170 pixels wide. Below and around it a thin curl of acid-green poison smoke and 2 to 4 glossy green bubbles.
Keep all of it inside a central box of 360 pixels wide and 450 pixels tall. The violet flame on top must NOT touch the top of that box.
SEAMLESS LOOP: the flame flickers, the green smoke curls and the bubbles drift slowly, and frame 6 flows back into frame 1. It must stay readable at small size: strong simple shapes, not much fine noise.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in smoke. Do NOT paint a checkerboard or grey squares anywhere, especially inside the smoke. No monster, hero, text, gridlines or UI. Not pixel art.
```

### D. Nightfall `cross` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/nightfall-execution.png`
- `Assets/Resources/Sprites/fx/nightfall/spin-5.png`
- `Assets/Resources/Sprites/fx/shadowstep/stab-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the giant crossing X-SLASH finisher of the assassin ultimate NIGHTFALL EXECUTION. Image 1 is the design board: match its final giant purple X, without hero, monsters, numbers or UI. Images 2 and 3 show the exact rendering to match: crisp hand-painted anime chibi RPG effect, thin white-hot strokes, saturated violet edges, sharp black-violet crystal shards.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. The crossing point of the X is exactly at the center of every cell, same center in all 6 frames. Keep every stroke and shard inside the central 450 pixels of each cell, nothing touching the cell edges.
Frame 1: one long curved blade arc from upper left to lower right.
Frame 2: the second arc crosses it from upper right to lower left, forming the X.
Frame 3: biggest frame: a straight sharp X with a large white-hot center, rays and black-violet shards flying out.
Frame 4: still big, the center remains a solid OPAQUE white core slightly smaller, shards farther out.
Frame 5: the X breaks into curling violet shadow wisps with thin bright lines.
Frame 6: faint violet smoke in an X shape with a few shards.
Every white core must be fully opaque white, never translucent or grey. Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in glow and smoke with smooth edges. Do NOT paint a checkerboard or grey squares anywhere, especially inside glow and smoke. No hero, monster, ground, text, gridlines or UI. Not pixel art.
```

### E. Death Mark `beam` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/death-mark.png` (panel 2)
- `Assets/Resources/Sprites/fx/deathmark/star-3.png`
- `Assets/Resources/Sprites/fx/shadowstep/trail-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the thin SHADOW BEAM of the assassin skill DEATH MARK, shot from a dagger tip to a target. Image 1 panel 2 shows the design: a black-violet streak with a bright arrow-shaped head, without the hero or monster. Images 2 and 3 show the exact rendering to match: crisp hand-painted anime chibi RPG effect, thin white-hot core, violet edge, black wisps.
Canvas 2172x724 PNG with REAL alpha. EXACT grid 2 columns x 3 rows, 6 equal cells of 1086x241, read left to right, top to bottom. Clear empty transparent space between all cells. The beam points RIGHT and is perfectly horizontal, on the vertical center of each cell. In every frame the small star spark at its origin is at the same spot, 60 pixels from the left edge of the cell, with a radius under 45 pixels.
Frame 1: origin spark with a short beam, about 25% of the length.
Frame 2: beam at about 50%.
Frame 3: beam at about 75%.
Frame 4: full length: arrow tip 1065 pixels from the left edge of the cell, the brightest frame.
Frame 5: same full length, thinner, fading.
Frame 6: same full length, a very thin dim line breaking apart.
Keep the beam within 90 pixels above and below its center line, never touching the top or bottom of the cell. Nothing else in the cell: no stray fragments.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in the glow around the beam. Do NOT paint a checkerboard or grey squares anywhere, especially inside the glow. No hero, monster, ground, text, gridlines or UI. Not pixel art.
```

### F. Venom Daggers `burst` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/venom-daggers.png` (panel 3)
- `Assets/Resources/Sprites/fx/venomdaggers/cast-5.png`
- `Assets/Resources/Sprites/fx/venomdaggers/dagger-5.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the POISON BURST on an enemy hit by the assassin skill VENOM DAGGERS. Image 1 panel 3 shows the design: an acid-green splash on impact, without daggers, monsters, numbers or UI. Images 2 and 3 show the exact green rendering to match: crisp hand-painted anime chibi RPG effect, thin bright acid-green strokes, sharp droplets.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. The burst is centered exactly on the center of every cell, same center in all 6 frames. Keep every stroke and droplet inside the central 440 pixels of each cell, nothing touching the cell edges.
Frame 1: small white-green flash with thin spikes.
Frame 2: spikes grow, droplets fly out.
Frame 3: biggest frame: opaque white-yellow core, long sharp green rays, large droplets.
Frame 4: core gone, a broken ring of thick green venom splashes.
Frame 5: thin green wisps and droplets far out, mostly empty center.
Frame 6: a few droplets and very faint wisps.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in the wisps. Do NOT paint a checkerboard or grey squares anywhere, especially inside the green cloud. No filled backdrop, no monster, hero, ground, text, gridlines or UI. Not pixel art.
```

### G. Shadow Clone `dissolve` (P2)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/shadow-clone.png`
- `Assets/Resources/Sprites/assassin/idle.png` (hàng 1, dáng người)
- `Assets/Resources/Sprites/fx/shadowclone/rise-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the assassin's SHADOW CLONE dissolving into purple smoke at the end of its life. Image 1 is the design board. Image 2, first row, shows the chibi body shape: big hair, ponytail, small body, about two heads tall. Image 3 shows the exact smoke rendering to match: crisp hand-painted anime chibi RPG effect, black-violet wisps, thin white-violet lines.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. In every cell the figure is centered horizontally and its feet are at the same line, 472 pixels from the top of the cell. Keep everything inside a central box 400 pixels wide, with at least 12 pixels empty above and below.
Frame 1: full dark silhouette of the chibi assassin, black-violet with thin violet lines, wisps starting to peel off. It has no face details and no red.
Frame 2: silhouette still readable, more wisps, the lower body fraying.
Frame 3: head and shoulders still readable, the body becoming twisting smoke.
Frame 4: only an S-shaped column of dark violet smoke with a thin bright line.
Frame 5: thinner fading smoke column and a few small wisps.
Frame 6: only a few small wisps and sparks.
Background must be truly transparent: alpha 0 in all empty areas, real partial alpha in smoke. Do NOT paint a checkerboard or grey squares anywhere, especially inside the smoke. No ground, no text, gridlines or UI. Not pixel art.
```

### H. Nightfall `shadow` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/nightfall-execution.png`
- `Assets/Resources/Sprites/fx/nightfall/spin-5.png`

```
Use case: game-asset. ONE production VFX sprite sheet: a SHADOW FIGURE that rises, rushes RIGHT, slashes once and dissolves, for the assassin ultimate NIGHTFALL EXECUTION. Image 1 is the design board. Image 2 shows the exact rendering to match: crisp hand-painted effect, black-violet with thin white-violet lines.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. A hooded faceless figure with two curved blades, drawn fully inside the central 420 pixels of each cell, with its feet on the same line 466 pixels from the top. Its smoke tail and slash arc also stay inside those 420 pixels, nothing touching the cell edges.
Frame 1: the figure rises from a violet smoke pillar. Frames 2 and 3: leaning far forward, rushing right, a short smoke trail behind. Frame 4: blade raised. Frame 5: a big white-hot crescent slash on the right side, fully inside the central area. Frame 6: the figure collapses into smoke and shards.
Background must be truly transparent: alpha 0 in empty areas, real partial alpha in smoke. Do NOT paint a checkerboard or grey squares anywhere. No stray fragments, ground, text, gridlines or UI. Not pixel art.
```

### I. Nightfall `spin` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/nightfall-execution.png`
- `Assets/Resources/Sprites/fx/nightfall/spin-4.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the flat spinning SLASH RING around the hero in the assassin ultimate NIGHTFALL EXECUTION. Image 1 is the design board, image 2 shows the exact rendering to match: thin white-hot and magenta arcs with black-violet wisps and dark crystal shards.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. A flat ellipse in 3/4 top-down view, centered exactly at the center of each cell in all frames. Width at most 460 pixels, and everything including shards stays within 180 pixels above and below the center, so nothing touches the cell edges. The middle stays open and empty where the hero stands.
Frames 1 to 3: the ring builds from one arc to several. Frames 4 to 6: the thickest state, and these three must loop seamlessly with each other.
Background must be truly transparent: alpha 0 in empty areas, real partial alpha in wisps. Do NOT paint a checkerboard or grey squares anywhere. No hero, monster, ground, text, gridlines or UI. Not pixel art.
```

### J. Blade Dance `ring` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/blade-dance.png` (panel 1-3)
- `Assets/Resources/Sprites/fx/bladedance/ring-4.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the growing SLASH RINGS of the assassin skill BLADE DANCE. Image 1 panels 1 to 3 show the design, without hero, monsters, numbers or UI. Image 2 shows the exact rendering to match: thin white-hot arcs with a violet edge and small black shards.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. A flat ellipse in 3/4 top-down view, centered exactly at the center of each cell in all frames. Width at most 410 pixels, and everything including shards stays within 145 pixels above and below the center. The middle stays open where the hero stands.
Frames 1 to 5: each frame adds a brighter, wider ring, with frame 5 the widest and fullest. Frame 6: the rings loosen and shards fly outward, still within the same limits.
Background must be truly transparent: alpha 0 in empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No hero, monster, ground, text, gridlines or UI. Not pixel art.
```

### K. Blade Dance `finish` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/blade-dance.png` (panel 4)
- `Assets/Resources/Sprites/fx/bladedance/finish-2.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the slash ring of the assassin skill BLADE DANCE breaking into rays and dark shards. Image 1 panel 4 shows the design, without hero, monsters, numbers or UI. Image 2 shows the exact rendering to match: sharp white-violet rays and black-violet shards around an empty center.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. Everything is arranged on a flat ellipse in 3/4 top-down view, centered exactly at the center of each cell in all frames. Width at most 400 pixels, and everything stays within 145 pixels above and below the center. The middle stays open where the hero stands.
Frame 1: a bright thick ring just starting to tear, with shards. Frame 2: the ring is gone, sharp rays and shards all around. Frames 3 to 6: the rays get shorter and fewer until only a few faint specks remain.
Background must be truly transparent: alpha 0 in empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No hero, monster, ground, text, gridlines or UI. Not pixel art.
```

### L. Shadow Clone `slash` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/shadow-clone.png`
- `Assets/Resources/Sprites/fx/shadowclone/slash-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the single purple CRESCENT SLASH of the assassin's shadow clone. Image 1 is the design board, image 2 shows the exact shape and rendering to match: a white crescent with a thin violet edge, curving to the right, trailing small black shards to the lower left.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. Same crescent position in all frames, with the brightest point of the curve 395 pixels from the left and 235 pixels from the top of the cell. Keep everything at least 24 pixels from all cell edges, including the shards.
Frames 1 to 4: the crescent grows, with frame 4 the biggest. Frame 5: thinner. Frame 6: a very thin fading line with a few specks.
Background must be truly transparent: alpha 0 in empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No character, ground, text, gridlines or UI. Not pixel art.
```

### M. Shadow Strike `cross` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/shadow-strike.png`
- `Assets/Resources/Sprites/fx/shadowstrike/slash1-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the X formed by two crossing dagger arcs in the assassin skill SHADOW STRIKE. Image 1 is the design board, image 2 shows the exact rendering to match: white crescents with a thin magenta-violet edge and small black shards.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. The crossing point is exactly at the center of each cell in all frames. Keep everything within 200 pixels left, 170 pixels right and 160 pixels above and below that center.
Frame 1: two thin arcs just crossed. Frame 2: brighter and thicker. Frame 3: biggest frame, sharp X with an opaque white center, rays and shards. Frame 4: thinner X, shards drifting. Frame 5: broken thin lines. Frame 6: one faint streak and specks.
Every white core must be fully opaque. Background must be truly transparent: alpha 0 in empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No character, ground, text, gridlines or UI. Not pixel art.
```

### N. Shadow Strike `slash2` (P3)

Đính kèm:
- `plans/261030-1521-skill-trees-implementation/assassin-boards/shadow-strike.png`
- `Assets/Resources/Sprites/fx/shadowstrike/slash1-3.png`

```
Use case: game-asset. ONE production VFX sprite sheet: the LEFT dagger's reverse crescent slash in the assassin skill SHADOW STRIKE, sweeping up from the lower left and opening to the left. Image 1 is the design board, image 2 shows the exact rendering to match: a solid white crescent with a thin magenta-violet edge and small black shards on its inner side.
Canvas 1536x1024 PNG with REAL alpha. EXACT grid 3 columns x 2 rows, 6 equal square cells of 512x512, read left to right, top to bottom. In all frames the crescent is measured from a fixed point 60 pixels from the left and at the vertical center of the cell: everything stays between 20 and 370 pixels to the right of that point, and within 155 pixels above and below it.
Frames 1 to 3: the crescent grows, with frame 3 the biggest. Frames 4 and 5: thinner. Frame 6: faint specks.
The white crescent must be solid opaque white with no grey streaks. Background must be truly transparent: alpha 0 in empty areas, real partial alpha in glow. Do NOT paint a checkerboard or grey squares anywhere. No character, ground, text, gridlines or UI. Not pixel art.
```

## 9. Cắt khung và vị trí file

**Công cụ.** Repo chưa có script tạo các khung `fx/<skill>/<tên>-<n>.png`. Không có file nào trong `Tools/` nhắc tới các raw này. `Tools/slice-skill-vfx.py` chỉ xử lý lưới 3 hàng x 4 cột và ghi vào `Sprites/skills/`, nên không dùng được nguyên trạng. Cần viết một script nhỏ, có thể tái dùng `Tools/vfx_slice_common.py` (`clean_mask`, `trim_box`). Máy hiện tại: Python 3.14 hệ thống chưa có Pillow, numpy và cv2, trong khi `vfx_slice_common.py` cần cả ba.

**Các bước cho mỗi sheet**

1. Lưu ảnh gốc vào cột "Raw", không sửa.
2. Chia theo lưới, cắt tại đường trống gần lưới nhất (giống `grid_cuts` trong `slice-skill-vfx.py`).
3. Với mỗi bộ, tính **một** độ dịch chung để tâm của khung đỉnh rơi đúng pivot. Không canh riêng từng khung, nếu không hiệu ứng sẽ rung.
4. Xóa mảnh lạc bằng `clean_mask`, rồi cắt hoặc thêm nền trong suốt cho đúng canvas.
5. Ghi đè `<tên>-1.png` đến `<tên>-6.png` và giữ nguyên file `.meta` đang có.
6. Kiểm tra: đủ 6 khung, cùng canvas, số pixel alpha > 128 ở mép bằng 0 (trừ cạnh trái khung `beam` nếu spark rất sát), không còn caro khi phóng to.

**Canvas và pivot phải giữ đúng**, vì code tính kích thước theo pixel. Giữ nguyên hằng số trong code, không cần sửa C#.

| Prompt | Raw | Đích | Canvas | Từ ô đến khung | Pivot theo code |
|---|---|---|---|---|---|
| A | `Assets/ArtLibrary/custom/death-mark-frames/dm-shatter-raw.png` | `fx/deathmark/shatter-N.png` | 512x512 | Giữ nguyên | Tâm khung, 0.11 đơn vị/px |
| B | `.../death-mark-frames/dm-ring-raw.png` | `fx/deathmark/ring-N.png` | 512x512 | Giữ nguyên | x 0.5, tâm vòng cách đỉnh 400 px, 0.065 |
| C | `.../venom-daggers-frames/vd-skull-raw.png` | `fx/venomdaggers/skull-N.png` | 384x480 | Cắt giữa ô | Tâm khung, 0.04 |
| D | `Assets/ArtLibrary/custom/nightfall-frames/nf-cross-v2-raw.png` | `fx/nightfall/cross-N.png` | 384x384 | Thu nhỏ còn 75% | Tâm khung, 0.27 |
| E | `.../death-mark-frames/dm-beam-raw.png` | `fx/deathmark/beam-N.png` | 1086x240 | Bỏ 1 hàng dư | x 60 px, giữa chiều cao, 0.04, độ dài đầy đủ 1010 px |
| F | `.../venom-daggers-frames/vd-burst-raw.png` | `fx/venomdaggers/burst-N.png` | 480x480 | Cắt giữa ô | Tâm khung, 0.085 |
| G | `.../shadow-clone-frames/sc-dissolve-raw.png` | `fx/shadowclone/dissolve-N.png` | 448x540 | Cắt giữa còn rộng 448, thêm 28 px trong suốt phía trên | x 0.5, chân cách đỉnh 500 px (0.074 từ đáy), 0.05 |
| H | `.../nightfall-frames/nf-shadow-v2-raw.png` | `fx/nightfall/shadow-N.png` | 384x384 | Thu nhỏ còn 75% | x 0.5, chân cách đáy 34 px, 0.11, art lao sang phải |
| I | `.../nightfall-frames/nf-spin-v2-raw.png` | `fx/nightfall/spin-N.png` | 384x300 | Thu nhỏ 75%, cắt giữa cao 300 | Đường cắt y=150, 0.285 |
| J | `.../blade-dance-frames/bd-ring-raw.png` | `fx/bladedance/ring-N.png` | 512x320 | Cắt giữa cao 320 | Đường cắt y=160, 0.165 |
| K | `.../blade-dance-frames/bd-finish-raw.png` | `fx/bladedance/finish-N.png` | 512x320 | Cắt giữa cao 320 | Đường cắt y=160, 0.155 |
| L | `.../shadow-clone-frames/sc-slash-raw.png` | `fx/shadowclone/slash-N.png` | 600x512 | Thêm 44 px trong suốt mỗi bên trái, phải | x 0.733, y 0.5, 0.085 |
| M | `.../shadow-strike-frames/ss-cross-v2-raw.png` | `fx/shadowstrike/cross-N.png` | 556x334 | Đặt điểm chéo vào pivot | (326, 170) từ góc trên trái, 0.165 |
| N | `.../shadow-strike-frames/ss-slash2-v2-raw.png` | `fx/shadowstrike/slash2-N.png` | 556x331 | Đặt điểm gốc vào pivot | (180, 161) từ góc trên trái, 0.127 |

Ghi chú:

- Dùng tên `-v2-raw` để không ghi đè raw cũ. Thư mục `death-mark-frames`, `venom-daggers-frames`, `shadow-clone-frames`, `blade-dance-frames` chưa tồn tại và cần tạo mới. Bốn skill này hiện không có raw trong repo, nên không thể cắt lại từ nguồn.
- Canvas 512x512 của A là lựa chọn của file này, vì code chỉ cần pivot ở giữa khung và không phụ thuộc kích thước canvas. Với 0.11 đơn vị/px, phần vẽ 440 px rộng khoảng 48 đơn vị, tức khoảng 3 ô (code cho thấy 1 ô khoảng 16 đơn vị), gần bằng bộ 420x500 hiện tại (phần vẽ rộng nhất khoảng 390 px, cao 460 px).
- `L` đặt điểm sáng nhất của cung theo `slash-4` hiện tại (đo trên contact sheet nên chỉ gần đúng), cần so lại với pivot x 0.733 sau khi cắt.
- Nếu ChatGPT trả E ở 1536x1024 thay vì 2172x724, ô chỉ còn 768x341. Cần phóng to 1.41 lần nên sẽ mềm hơn, khi đó nên gửi lại.

**Sau khi thay art:** xem lại Death Mark trong game ở cả hai trường hợp vỡ (hết charge, quái chết) và trường hợp hết giờ (fade). `shatter` được vẽ ở `lastFeet + (0, lastTop * 0.5 + 4)` trong 420 ms, mỗi khung khoảng 70 ms.

## 10. Câu hỏi còn mở

1. Dùng tài khoản nào, "Tay Plus" hay "Tan Pham"? File này không phụ thuộc vào chat cũ nên cả hai đều dùng được.
2. Có làm P3 không, hay xem trong game trước?
3. Có muốn đổi code theo spec ở mục 4 không? Mặc định là giữ theo board.

<!-- Lập 21/11/2026 từ phân tích trailer https://www.youtube.com/watch?v=t-LNpJN5H54 cùng ChatGPT (Tay "Tân Korris") https://chatgpt.com/c/6b017962-84d4-83ec-8b76-80286bcd2cc4 . Phân tích đầy đủ: analysis-chatgpt.md cùng thư mục. -->

# Template prompt skill VFX "RO trailer style" (người dùng chốt 21/11)

Dùng file này mỗi khi giao việc skill / đòn đánh / FX boss cho agent, hoặc xin ChatGPT ảnh concept / chấm ảnh. Chép nguyên khối, điền `{…}`. Vẫn phải đọc `docs/sprite-rules.md` (đầu file) trước khi làm.

Cách dùng nhanh:
- Giao agent làm FX trong Unity: mục 1 (DNA) + mục 2 (timing) + Template A.
- Xin ChatGPT ảnh concept: Template B một dòng (công cụ gửi prompt gửi luôn khi gặp xuống dòng), kèm ảnh nhân vật idle + ảnh in-game hiện tại.
- Nhờ ChatGPT / agent chấm ảnh chụp trong game: Template C, kèm sheet khung trailer (ngoài git, `D:/temp/ro-trailer-frames`) hoặc mô tả mục 4-5.

## 1. RO STYLE DNA (dán đầu MỌI brief)

**VI:** RO STYLE DNA: Hiệu ứng skill kiểu Ragnarok cổ điển (theo trailer Midgard Survivors), hiện đại hoá theo RO3 Soft Fantasy: HOÀNH TRÁNG về kích thước nhưng NHẠT, TRONG SUỐT và DỄ ĐỌC. Quạt chém có thể đạt bán kính 4,5-6H, đại ma pháp trận đường kính 8-10H. Ưu tiên ánh sáng pastel, trắng ngà, vàng dịu, hình học rõ, vòng phép mảnh và impact mạnh nhưng ngắn. Thân alpha 0,18-0,40, Alpha Blend; Bright / Core dùng Additive có kiểm soát. Không dùng viền tối đỏ mận, plasma đục, lửa đỏ mặc định hoặc Bloom che nhân vật. Màu theo class và nguyên tố; đỏ cảnh báo dành riêng cho boss. Chỉ dùng Particle System, Mesh Trail, mesh thủ tục và shader, không sprite frame.

**EN:** RO STYLE DNA: Classic Ragnarok-inspired skill VFX (Midgard Survivors trailer), modernized for RO3 Soft Fantasy. Effects must look GRAND in scale yet PALE, TRANSLUCENT, and READABLE. Sword sectors may reach a 4.5-6H radius; major magic circles may span 8-10H in diameter. Favor soft pastel light, ivory, gentle gold, clear geometry, thin magic rings, and powerful but brief impacts. Use Alpha Blend for the main body (alpha 0.18-0.40) and restrained Additive highlights. Avoid dark burgundy outlines, opaque plasma, default red flames, and bloom that hides characters. Respect class and elemental palettes; reserve danger-red telegraphs for bosses. Implement using procedural geometry, Particle Systems, Mesh Trails, and shaders, without animated sprite frames.

## 2. TIMING AUTHORITY (bắt buộc trong mọi brief)

**VI:** `SkillTable` (dùng chung với server) là nguồn duy nhất cho thời gian gameplay. Đọc cast, mốc hit, `SkillHitSplit` từ dữ liệu có sẵn; không bịa, không thay, không chuẩn hoá các số này. Chỉ sắp lại pha FX lấy đà / bung / đỉnh sáng / tan quanh các mốc đó. Ví dụ Earth Break: kiếm cắm đất đúng 1450 ms. Số hit, thứ tự hit, logic dam, hợp đồng server giữ nguyên. Phần dư âm thuần hình được kéo dài sau hit cuối.

**EN:** TIMING AUTHORITY — MANDATORY: SkillTable shared with the server is the single source of truth for all gameplay timing. Read cast time, animation events, hit timestamps, and SkillHitSplit directly from the existing data. Never invent, replace, normalize, or modify these values. Remap only VFX anticipation, expansion, brightness peak, and dissolve around the authoritative timestamps. Example: Earth Break sword impact at 1450 ms must remain exactly at 1450 ms. All hit counts, hit ordering, damage logic, and server contracts must remain unchanged. Cosmetic VFX may continue after the final hit.

## 3. Quy tắc ánh sáng "RO Translucent Light" (thay quy tắc 4 bậc sáng viền tối cũ)

1. Outer: cùng họ màu, nhạt / ít bão hoà; alpha 0,08-0,20, Alpha Blend; KHÔNG viền tối đỏ mận / nâu đen.
2. Body: pastel sáng, alpha 0,18-0,40, Alpha Blend; ~65-80 % diện tích hình chính.
3. Bright: vàng nhạt / trắng ngà, alpha 0,45-0,75, Additive (hoặc Alpha trên nền rất sáng); ~15-25 %.
4. Core: trắng / ngà, alpha 0,75-0,95, Additive; 3-8 % diện tích, tối đa 10-12 % lúc impact; ở kiếm thì core là đường mảnh trên mép, không phải quả cầu.
5. Khoảng trống: 45-65 % khung bao FX là rỗng hoặc alpha rất thấp (thấy nền, quái, nhân vật xuyên qua).
6. Bloom chỉ ở Bright / Core lúc impact; không cho cả Body phát HDR liên tục; không Additive mọi lớp (nhiều người đứng gần sẽ thành mảng trắng).
7. Hoành tráng bằng KÍCH THƯỚC + hình học + nhịp, không bằng độ đục / độ đậm.

## 4. Palette (hex sRGB, base color trước HDR emission)

| Class | Outer | Body | Bright | Core | Dùng cho |
|---|---|---|---|---|---|
| Warrior | #B2A873 | #D5C378 | #F5EA7C | #FEFDF8 | Chém thường, Bash / quạt, xung lực, Knight impact (vàng chanh / champagne / ngà) |
| Warrior Steel (phụ) | #718B9D | #A5BCCD | #DEE9EF | #FFFFFF | Gió dash, luồng khí ngoài, khiên |
| Warrior Fire (chỉ skill lửa thật) | #B67C50 | #E0A35A | #F6D586 | #FFF9DE | Flame Cleave, Inferno Crash: vàng mật ong, lửa ≤ 20-30 % diện tích |
| Wizard | #527FA7 | #76ADCE | #A9D9EF | #F2FBFF | Arcane / băng, ma pháp trận lam; Fire / Wind / Earth đổi sang bảng nguyên tố |
| Wizard Arcane tím (phụ) | #665B9E | #987DE0 | #C9BAF2 | #FCF7FF | Gravity, phép không gian |
| Mechanic | #587A88 | #84B7C5 | #D5E8E9 | #FFF5D8 | Súng năng lượng, máy, pháo |
| Mechanic Explosion (phụ) | #95654A | #D8A35E | #F7DB98 | #FFFFFF | Chớp nòng, nổ đạn pháo |
| Assassin | #526B69 | #75B987 | #B4E1A0 | #F1FFE1 | Độc, dao găm, vệt độc |
| Assassin Shadow (phụ) | #554B79 | #8774B5 | #BBADE0 | #F4EEFF | Tàng hình, bóng |
| Assassin Sonic Hit (phụ) | #B96064 | #F37860 | #FFC1A0 | #FFF8ED | Chuỗi chém nhanh kiểu Sonic Blow, CHỈ vệt ngắn, không lên sàn |
| Cleric | #BBAE88 | #F3D16B | #FFF4C9 | #FFFFFF | Holy, blessing, thánh ấn; Heal #91DB76 / #B6E9B7 và Bảo vệ #9DDAEC GIỮ như RO3 Soft Fantasy |
| Fighter | #709D9C | #A4D6C9 | #F4E7AE | #FFFFFF | Nắm đấm, chấn khí, spirit (teal nhạt + champagne) |
| Fighter Aura (phụ) | #A08B60 | #D6BD80 | #F5E6AF | #FFFFFF | Đòn mạnh, bùng khí |
| Boss danger | #8D372F | #CB6F43 | #E6A36A | #FFE0A7 | Cảnh báo sàn, rune đỏ, sọc, vòng đánh số của boss; không dùng cho mọi quái |
| Boss Fire | #963D32 | #DA834F | #EAAB40 | #FFF1BF | Đòn lửa của boss |

Nguyên tố: Neutral #D5C378 / lõi #FEFDF8; Fire #D9874D / #FFF2B5; Water #5EACCF / #E7FBFF; Wind #80CBB3 / #F0FFF6; Earth #A89463 / #E4D5A1; Holy #E8CD86 / #FFFFFF; Shadow #554C83 / #BCABEA; Ghost #907FCE / #F2E7FF; Poison #76B65D / #DCFFC1.

**Màu CẤM cho FX người chơi trên mặt đất (dành cho boss):** #D04C34, #CB6F43, #C83832, #8D372F, và mọi vòng / sector đỏ trầm cùng hình kiểu cảnh báo. Player = đường mềm, xung lực, rune mảnh; Boss = vùng phẳng, biên liên tục, sọc / nhịp đập rõ.

## 5. Cỡ theo H (H = chiều cao nhân vật trên màn hình, không tính tên / thanh máu / vũ khí nhô)

| Hình | Cỡ (đo từ trailer) |
|---|---|
| Quạt Bash / Power Slash | bán kính 4,5-5,5H, góc 100-125° |
| Cung kiếm lớn | bán kính 2,5-4,5H |
| Vòng elip tại mục tiêu (Spiral Pierce) | ĐK 2-2,5H |
| Vòng xung lực / AoE | ĐK 4-8H |
| Cột sáng | cao 4,5-6H |
| Đại ma pháp trận (Storm Gust, Gravitation) | ĐK 8-10H |
| Sao impact | ĐK 0,8-1,8H (ultimate 1,5-2,5H) |
| Rune buff quanh chân | ĐK 2-4H |
| Buff / heal đơn mục tiêu | giữ 0,8-1,8H (RO3 Soft Fantasy) |

Trong game mình: thân quạt / vòng LẤP ĐẦY tầm gameplay thật (vòng ranh giới đúng tầm), không co nhỏ quanh người; nếu tầm thật nhỏ hơn cỡ trên thì lấy tầm thật + mép mờ ≤ 15 %, không vẽ quá tầm làm người chơi hiểu nhầm vùng trúng.

## 6. Template A: Skill VFX brief cho agent Unity

### A1. Tiếng Việt

```
[RO STYLE DNA — dán mục 1]
[TIMING AUTHORITY — dán mục 2]

SKILL VFX BRIEF — RO TRAILER STYLE
Repo: D:\zenoniaClone (nhánh ro-fantasy-heroes, commit thẳng, không nhánh / worktree). BẮT BUỘC đọc hết đầu docs/sprite-rules.md và docs/reference/ro-trailer-skill-style/prompt-templates.md trước khi làm.
Class: {Warrior / Wizard / Mechanic / Assassin / Cleric / Fighter / Boss {tên}}
Skill: {tên skill} | Vai trò: {đơn mục tiêu / AoE / lướt / buff / ultimate / cảnh báo boss} | Nguyên tố: {Neutral / Fire / …}
Mốc thời gian (đọc từ SkillTable / SkillHitSplit, KHÔNG đổi): cast {…} ms, hit {…} ms, tổng {…} ms.
Tầm gameplay thật: {… ô / bán kính …}.

1. Hình chủ đạo: {quạt / cung kiếm / vòng elip nằm đất / cột sáng / tia / ma pháp trận / sao impact}; hướng {theo hướng đánh / từ tâm / từ trên xuống / xoáy quanh người}; cỡ {…H} (bảng mục 5); góc {…°}; khoảng trống {45-65 %}; neo {chân / vũ khí / điểm trúng / vị trí đất}. Hình chính phải nhận ra khi tắt hết hạt trang trí.
2. Palette (mục 4): Outer {#…} a 0,08-0,20 Alpha | Body {#…} a 0,18-0,40 Alpha | Bright {#…} a 0,45-0,75 Add | Core {#…} a 0,75-0,95 Add (3-8 % diện tích). Không tự đẩy sang đỏ / cam cho "mạnh".
3. Lớp (3-5, ultimate tối đa 6-8): Đất {…} / Thân {…} / Vệt năng lượng (Mesh Trail) {…} / Lõi {…} / Impact {sao + loé + vòng} / Dư âm {số hạt, thời gian}.
4. Nhịp quanh mốc SkillTable: lấy đà {…} ms trước hit, bung {40-120 ms}, đỉnh sáng đúng hit, tan {150-400 ms}; multi-hit: mỗi hit một loé nhỏ, hit cuối loé lớn.
5. Render: thân Alpha Blend, Bright / Core Additive, Bloom chỉ lúc impact, distortion {0-2 px thường / 2-5 px ultimate / tắt cho người khác}, rung camera {chỉ khi chính mình đánh mạnh}, vòng đất nằm đúng mặt đất theo góc camera.
6. CẤM: màu đỏ cảnh báo boss trên sàn; lửa / tàn lửa cho skill không phải Fire; viền tối quanh năng lượng; khối đục; Additive mọi lớp; Bloom che nhân vật / số dam; tên skill trên đầu; sprite frame (vật thể đặc gọi ra như kiếm khổng lồ được dùng sprite ChatGPT vẽ).
7. LOD đám đông: mình 100 %, đồng đội 40-60 %, người xa 10-25 %; cảnh báo boss luôn ưu tiên.
8. Kiểm tra: build từ bản sao (Tools/build-from-copy.ps1, không build vào project đang mở), chụp `--class {class} --record-choreo {skill} --zoom 60` trên tuyết + cỏ + hang; xem từng khung; chấm bằng Template C (≥ 8,5/10); ghi lỗi mới vào Sổ lỗi docs/sprite-rules.md; ghi docs/pending-tasks.md.
Giao: code + ảnh trước / sau trong docs/reference/{thư mục}/ + điểm chấm; nói rõ phần CHƯA xem trong game.
```

### A2. English

```
[RO STYLE DNA — paste section 1]
[TIMING AUTHORITY — paste section 2]

SKILL VFX BRIEF — RO TRAILER STYLE
Class: {…} | Skill: {…} | Role: {single-target / AoE / dash / buff / ultimate / boss telegraph} | Element: {…}
Authoritative timing (SkillTable / SkillHitSplit, DO NOT change): cast {…} ms, hits {…} ms, total {…} ms. Real gameplay range: {…}.
1. Primary silhouette: {sector / slash arc / ground ellipse / light pillar / ray / magic circle / impact star}; direction {…}; size {…H}; angle {…°}; negative space 45-65 %; anchor {feet / weapon / hit point / ground}. Must read with decorative particles disabled.
2. Palette: Outer {#…} a0.08-0.20 Alpha | Body {#…} a0.18-0.40 Alpha | Bright {#…} a0.45-0.75 Add | Core {#…} a0.75-0.95 Add, 3-8 % area. Never shift toward red/orange to look stronger.
3. Layers (3-5; ultimate up to 6-8): Ground / Body / Energy trail (Mesh Trail) / Core / Impact (star + flash + ring) / Residue.
4. Rhythm around SkillTable marks: anticipation {…} ms before hit, expand 40-120 ms, brightness peak on the hit, dissolve 150-400 ms; each sub-hit a small flash, last hit the big one.
5. Rendering: body Alpha Blend, highlights Additive, bloom only at impact, distortion 0-2 px (ultimate 2-5 px, off for other players), camera shake only for own heavy hits, ground rings on the ground plane.
6. Forbidden: boss-danger red on the ground; flames/embers on non-Fire skills; dark outlines around energy; opaque blobs; all-Additive stacks; bloom hiding hero or damage numbers; skill name above the caster; sprite-frame VFX (summoned solid objects may be painted sprites).
7. Crowd LOD: self 100 %, party 40-60 %, others 10-25 %; boss telegraphs always win.
8. Verify with in-game captures on snow, grass and cave; score with Template C (>= 8.5/10); state what was not seen in game.
```

## 7. Template B: xin ChatGPT ảnh concept

### B1. Một dòng, tiếng Anh (dùng với công cụ gửi prompt: không xuống dòng)

```
Create a {SKILL} VFX concept for {CLASS}, classic Ragnarok / RO3 Soft Fantasy, chibi 3/4 top-down camera {ANGLE}°. GRAND scale yet PALE, TRANSLUCENT, READABLE. Main shape: {SHAPE}, radius/size {SIZE}H, direction {DIRECTION}. Colors Outer {OUTER}, Body {BODY}, Bright {BRIGHT}, Core {CORE}. Body alpha 0.18-0.40 Alpha Blend; thin Bright/Core Additive; core 3-8% area; 45-65% visual negative space. Soft pastel light, ivory highlights, clear geometry, brief impact, delicate ground FX. Show {PHASE} at {TIME}ms, matching SkillTable/SkillHitSplit without changing gameplay timing. Use procedural Particle System + Mesh Trail + shader, NO sprite frames. No opaque plasma, dark burgundy outlines, excessive bloom, default red flames or boss-warning red. Background {BACKGROUND}, hero height H={H}px, keep hero and target visible.
```

Ví dụ điền sẵn (Warrior Power Slash):

```
Create a Power Slash VFX concept for Warrior (sword + shield), classic Ragnarok / RO3 Soft Fantasy, chibi 3/4 top-down camera 58°. GRAND scale yet PALE, TRANSLUCENT, READABLE. Main shape: huge 115° pale-gold Bash sector, radius 5H, sweeping forward, with a thin ivory edge and compact impact star. Colors Outer #B2A873, Body #D5C378, Bright #F5EA7C, Core #FEFDF8. Body alpha 0.18-0.35 Alpha Blend; Bright/Core Additive; core 3-8% area; large transparent areas. Show the impact peak aligned to the existing SkillTable hit event, without changing timing. Procedural Particle System + Mesh Trail + shader, NO sprite frames. No red fire, opaque plasma, burgundy outline or excessive bloom. Pastel snowy ground, hero H=58px, keep hero and enemy visible.
```

Gửi kèm: `docs/reference/hero-idle-down-5-classes.png` (hoặc idle của class) + ảnh in-game hiện tại cùng góc camera. H trong game mình đo từ ảnh chụp, không lấy 58 px của trailer. Skill quan trọng (ultimate): xin 3 ảnh (lấy đà / đỉnh / tan), không chỉ khung đẹp nhất.

### B2. Bản đầy đủ tiếng Việt (gửi tay, được xuống dòng)

```
Vẽ mới hoàn toàn một CONCEPT VFX cho skill {TÊN SKILL} của class {CLASS} trong game RPG 2D chibi, phong cách RAGNAROK ONLINE CỔ ĐIỂN / MIDGARD SURVIVORS, hiện đại hoá nhẹ bằng ánh sáng và shader nhưng giữ ngôn ngữ hình ảnh RO.
Góc nhìn 3/4 top-down, camera nghiêng {…}°, nhân vật cao H = {…} px.
CHỦ THỂ: {nhân vật, vũ khí, tư thế}; giữ đúng thiết kế nhân vật trong ảnh mẫu.
HÌNH CHÍNH: {quạt vàng / cung kiếm / vòng xung lực / cột sáng / ma pháp trận / tia / sao impact}, là hình nổi nhất.
CỠ: {…H}, hướng {…}, góc quét {…°}, vị trí {trước mặt / quanh chân / tại mục tiêu / từ trên xuống}. HOÀNH TRÁNG về diện tích nhưng NHẠT và TRONG.
MÀU BẮT BUỘC: Outer {#…}, Body {#…}, Bright {#…}, Core {#…}. Phần lớn ánh sáng là {màu chủ đạo}; lõi {màu lõi} chỉ ở mép sắc và điểm trúng; vùng trong suốt lớn để thấy nhân vật và mặt đất.
NGÔN NGỮ RO: ánh sáng vàng ngà / pastel mềm, lớp năng lượng xuyên thấu, vòng đất mảnh, quạt sáng hình học rõ, impact nhanh, sao bung nhỏ, ít hạt lấp lánh; không viền đen quanh năng lượng.
LỚP: {số lớp}: {Đất / Thân / Mép năng lượng / Loé trúng / Dư âm}; không tự thêm lớp khác.
KHUNG MUỐN VẼ: {lấy đà / đỉnh / tan} tại T = {…} ms của skill dài {…} ms.
BỐ CỤC: {1 khung lớn / 3 khung lấy đà-đỉnh-tan}; nhiều khung thì cùng nhân vật, camera, map, cỡ, hướng.
NỀN: {tuyết sáng / cỏ pastel / hang tối}.
TUYỆT ĐỐI KHÔNG: lửa đỏ cho skill Neutral, plasma neon, mảng năng lượng đặc, quả cầu che nhân vật, vòng rune đỏ kiểu boss, vàng cam cháy cả màn hình, lens flare, dark fantasy, chữ trên ảnh.
Đây là concept để làm bằng Unity Particle System + Mesh Trail + Shader, KHÔNG phải sprite sheet.
```

### B3. Full English (manual)

```
Create a completely new VFX CONCEPT IMAGE for the skill {SKILL NAME} of the {CLASS} class in a 2D chibi action RPG. Art direction: CLASSIC RAGNAROK ONLINE / MIDGARD SURVIVORS, lightly modernized with polished lighting and shaders while preserving the Ragnarok visual language. Camera: 3/4 top-down, {…}° above the ground. Hero height H = {…} px.
SUBJECT: {character, weapon, pose}; preserve the exact character design from the reference.
PRIMARY SILHOUETTE: {pale-gold sector / slash arc / shockwave ring / light pillar / magic circle / ray / impact star}; the single most recognizable shape.
SCALE: {…H}; direction {…}; sweep {…°}; anchor {weapon / feet / target / ground}. GRAND in area yet PALE and TRANSLUCENT.
MANDATORY PALETTE: Outer {#…}, Body {#…}, Bright {#…}, Core {#…}. Most energy uses {primary}; the brightest core only on thin edges and impact points; generous negative space.
LAYERS: {n}: {Ground / Main shape / Energy edge / Impact / Residue}; no unrelated effects.
FRAME: {anticipation / impact peak / dissolve} at T = {…} ms of a {…} ms skill (timing from SkillTable).
COMPOSITION: {one frame / three-panel progression}; identical camera, character, environment, direction across panels.
ENVIRONMENT: {bright snow / pastel meadow / dark cave}.
STRICT NEGATIVES: no red flames for Neutral attacks, no neon plasma, no opaque energy blobs, no fire spheres hiding the hero, no boss-danger red rune circles, no full-screen orange bloom, no dark-fantasy grading, no lens flares, no text.
This is an art-direction concept for Unity Particle Systems, Mesh Trails and shaders, not a sprite sheet.
```

## 8. Template C: chấm ảnh in-game (ChatGPT hoặc agent)

Thang 100 (quy /10): A Màu + nhận diện class 20 | B Hình chủ đạo + hình học 20 | C Độ trong + blend 15 | D Cỡ theo H 10 | E Nhịp / impact 15 (chỉ khi có chuỗi khung) | F Nằm đất / 2.5D 10 | G Đọc được trong trận 10. Ngưỡng: ≥ 9,0 duyệt; 8,5-8,9 duyệt có sửa; 7,5-8,4 sửa; 6,0-7,4 sửa lớn; < 6 làm lại palette / hình.

### C1. Tiếng Việt

```
REVIEW CHECKLIST — RAGNAROK CLASSIC VFX. Chỉ trả lời bằng chữ, không tạo ảnh.
Skill: {…} | Class: {…} | Nguyên tố: {…} | Ảnh tham chiếu: {sheet trailer / giây} | Ảnh in-game: {file} | Lượt: {V…} | Đã sửa: {…}
Nhiệm vụ: so ảnh trong game với ngôn ngữ VFX RO cổ điển (khung Midgard Survivors). Không chấm "đẹp" độc lập, không thưởng thêm lửa / Bloom / lớp. Chỉ có ảnh tĩnh thì không được nói đã kiểm timing / FPS / shader: ghi N/A và quy đổi điểm trên phần kiểm được.
Chấm: A {0-20}, B {0-20}, C {0-15}, D {0-10}, E {0-15 hoặc N/A}, F {0-10}, G {0-10}.
Câu hỏi bắt buộc: 1) đúng màu class + nguyên tố? 2) quá đỏ / cam hoặc giống cảnh báo boss? 3) hình chính nhận ra trong 100 ms quanh impact? 4) quá nhiều lớp? 5) thân đủ trong để thấy nhân vật, quái, nền? 6) Bright / Core quá dày, cháy trắng? 7) cỡ đúng H (quạt 4,5-5,5H, ma pháp trận 8-10H…)? 8) vòng đất nằm đúng mặt đất? 9) impact đúng mốc hit? 10) đọc được khi nhiều người cùng đánh?
Trả lời: PHÁN QUYẾT {Đạt / Đạt có điều kiện / Chưa đạt}; ĐIỂM RO STYLE {x/10}; 3-5 lỗi còn lại theo ưu tiên {LỚN / VỪA / NHỎ — lỗi — cách sửa bằng hex, alpha, %, H, ms}; GIỮ NGUYÊN {…}; THÔNG SỐ VÒNG SAU {…}; MỤC TIÊU VÒNG SAU {…}. Không mở lại phần đã đạt, không liệt kê polish vô tận khi đã qua ngưỡng.
```

### C2. English (một dòng được)

```
REVIEW CHECKLIST — CLASSIC RAGNAROK VFX. Text-only, no images. Skill {…}, class {…}, element {…}, reference {trailer sheet / seconds}, in-game {file}, revision {V…}, changes {…}. Compare the in-game VFX with classic Ragnarok VFX language (Midgard Survivors frames); judge style accuracy and combat readability, not spectacle; do not reward more flames, bloom or layers; with stills only mark timing/FPS/shader as N/A and normalize. Score A class palette 0-20, B silhouette 0-20, C transparency/blend 0-15, D scale vs H 0-10, E timing/impact 0-15 or N/A, F ground/2.5D 0-10, G crowd readability 0-10. Check: palette vs class/element; too red/orange or boss-like; silhouette readable at impact; too many layers; body transparent enough; core overexposed; size vs H (sector 4.5-5.5H, big circles 8-10H); ground rings on ground plane; impact on hit time; readable with many players. Reply: VERDICT, RAGNAROK STYLE SCORE x/10, top 3-5 remaining issues (MAJOR/MEDIUM/MINOR, exact fix in hex/alpha/%/H/ms), KEEP UNCHANGED, NEXT REVISION VALUES, NEXT ROUND TARGET. Do not reopen approved items.
```

## 9. Ví dụ áp dụng: 8 skill Warrior theo hướng mới (ChatGPT vòng 2, chờ làm)

Hướng: "Knight of Light and Steel" (Swordman -> Knight -> Lord Knight), không phải Fire Berserker. 5/8 skill không lửa, 2/8 lửa thật (vàng mật ong, lửa ≤ 20-30 % diện tích), 1/8 có điểm nhấn san hô nhỏ. Thời gian vẫn theo `SkillTable`.

| Skill | Hình chủ đạo | Màu | Cỡ | Alpha thân |
|---|---|---|---|---|
| Power Slash | Quạt Bash vàng chanh ~115° + mép ngà mảnh + sao impact lớn tại mục tiêu; không vết cháy / lưỡi lửa / tàn lửa | Warrior | quạt R 4,5-5H | 0,18-0,35 |
| Whirlwind | 2 cung khí vàng / steel lớn, đứt đoạn, tâm rỗng, lệch nhau | Warrior + Steel | R 3,5-4,5H | 0,18-0,32 |
| Flame Cleave | Cung vàng mật ong lớn, viền lửa cam rất nhẹ (5-7 cụm < 200 ms), không sàn đỏ | Warrior Fire | R 3,5-4,5H | 0,22-0,40 |
| Charging Strike | Mũi xung lực steel / ngà dẫn đường, vệt sáng rộng theo đường lao, impact 1,5H + vòng lực mặt đất | Warrior + Steel | ngang 1,5-2H | 0,20-0,40 |
| Earth Break | "Golden Knight Judgment": kiếm ánh thép ngà rune vàng (giữ sprite kiếm vẽ, đổi tông), ấn vàng, nứt đất nâu lõi vàng ngắn, 3-5 cột sáng vàng / trắng thấp thay cột lửa, finisher = sao vàng trắng + vòng sóng; cắm đất đúng 1450 ms | Earth Break #887C5A / #C6B37D / #F3E1A8 / #FFFFFF | rune ĐK 7-9H, sóng R 4-5H | 0,16-0,32 |
| Blade Cyclone | 3 cung kiếm champagne xoay, nghiêng khác nhau, tâm thoáng, 3 đỉnh sáng theo hit | Warrior | R 4-5H | 0,16-0,30 |
| Inferno Crash | Vòng bùng vàng lửa lan thấp (Magnum Break cấp cao + đập đất), lõi nhỏ, không phủ đỏ kín sàn, không rune đỏ | Warrior Fire | sóng R 4,5-5,5H | 0,20-0,38 |
| Berserker | Vầng sáng vàng ngà + đường năng lượng vươn lên, 2 nhịp pulse; san hô #F37860 chỉ 2-3 vệt chéo ngắn; duy trì = vòng mỏng vàng quanh thân | Warrior + chấm san hô | aura cao 2,5-3,5H, pulse R 2-3H | 0,18-0,32 |

Giữ từ bản hiện tại: Mesh Trail theo hướng đánh, sao impact / sóng / hit-stop, rune + vòng đất thủ tục, Dissolve, cấu trúc event; kiếm vẽ Earth Break (chỉ đổi ánh sáng quanh nó). Bỏ: viền tối đỏ mận, thân đỏ đục, vết cháy, cột lửa, vòng đỏ cam đặc. Nên làm mẫu trước: Power Slash (chuẩn đòn vật lý), Blade Cyclone (chuẩn AoE, thử độ trong), Earth Break (chuẩn ultimate).

# Dọn asset cũ theo class (16/11/2026)

Phạm vi: `Assets/ArtLibrary/classes/`, `Assets/Resources/{Sprites,Data}/classes/`, `Assets/Resources/Audio/sfx/<class>`. CHƯA commit.
Khôi phục bất kỳ file nào: `git checkout 4c527c36 -- <đường dẫn>`.

## Cách xét

- Runtime: đọc mọi `Resources.Load` / `SpriteSheet.Load` (HeroClassDef, HeldGearData, VfxSprites, SkillVfx, SkillIcons, *Fx.cs, HeroHeadLayer*, ClassSfx...).
- Tool: quét `Tools/*.py|ps1|mjs`, xem từng biến `OLD` / `BACKUP` / `STAGE` là chỉ ghi (sao lưu một lần) hay có đọc lại (`Image.open`, `json.load`, `os.path.exists` rồi đọc, `glob`/`rglob`).
- Nhóm A chỉ gồm thư mục/file cũ, bị loại, sao lưu, mà không code runtime nào nạp và không tool nào đọc. Nếu tool có đọc một phần thư mục `old/`, chỉ xoá phần không đọc.

## A. Đã xoá (`git rm`, kèm `.meta`)

1360 file trong git (898 ảnh/json + 462 `.meta`), **352 MB**, toàn bộ trong `Assets/ArtLibrary/classes/` (không đụng Resources, nên game không đổi).

| Class | File | MB |
|---|---|---|
| assassin | 140 | 50.7 |
| cleric | 123 | 27.4 |
| fighter | 8 | 9.1 |
| mechanic | 322 | 104.2 |
| warrior | 427 | 86.9 |
| wizard | 340 | 74.3 |

Chi tiết (đường dẫn tính từ `ArtLibrary/classes/`):

- fighter: `rig/` (rig đã bỏ 15/11, tool đã xoá, chỉ còn `.pyc` cũ), `new-style/src/old-guard-20261104/`.
- assassin: `new-style/dagger-src/old-20261103/`, `old-20261103-v1/`; `new-style/old/{armed,armed-before-daggers,dagger,icons}` (giữ `old/bare`, `old/*.json`, `old/idle-down-row0`, `old/dagger-pre-tier-sets` vì tool còn đọc); `attack-board/old/` (sao lưu chỉ ghi).
- cleric: `companion-src/rejected/`, `shield-src/rejected/`, `attack-board/old/`, `new-style/idle-down-doll/old/`, `new-style/old/{armed,staff,icons,*.png}` (giữ `old/held-cleric*.json`).
- mechanic: `heads/rejected-v1..v3/`, `new-style/old/` (tool không đọc), `new-style/rejected-gauntlet/`, `new-style/src/rejected/`, `dual-src/hero-select-old/`, `dual-src/tiers/{old-20261103-v1,old-20261107-v2,old-icons}/`, `dual-src/tiers/cannon/{old-20261103-v1,old-20261107-v2,old-icons,rejected}/`, `outfits/design-ladder-v2/` (đã có v3).
- warrior: `new-style/attack/{ref-old,rejected}/`, 14 sheet png trong `new-style/attack/old-sheets/` (giữ `held-gear-melee-old.json`, `build-warrior-attack.py` đọc), `new-style/centered/old/`, `new-style/move/old/`, `new-style/move/src/{from-old-rejected,old-rows}/`, `outfits/design-ladder-v2/`, `shield-src/{old-mu-shield-20261103,old-mu-shield-20261107,old-v2-before-v3-20261107,mu-ref-v2/old-lightning-t0}/`, `sword-src/{old-20261103,old-mu-20261103,old-mu-sword-20261103,-20261107,-20261108,old-v2-before-v3-20261107,mu-ref-v2/old-lightning-t0,t9-dragon-v3/old-ingame}/`.
- wizard: `heads/rejected-v1/`, `heads/layered/old/`, `heads/layered/on-body/old-1/`, `new-style/old/{armed,staff,icons,staff-pre-tier-sets}` (giữ `old/bare`, `old/*.json`), `new-style/idle-down-doll/old/`, `new-style/staff-src/{old-20261103,rejected}/`, `outfits/design-ladder-v2/`, `outfits/gear-src/old-v1/`, `outfits/outfit-d-tiers/{old,old-gear}/` (sao lưu chỉ ghi), `attack-board/{body,northwind-combo-v2,outfit-d}/old/`, 2 file `attack-board/outfit-d/*-pose-changed-old.png` (tư thế bị loại), `walk-board/{northwind-walk,outfit-d-walk}/old/`.

Lưu ý: `docs/pending-tasks.md` (15/11) từng ghi "giữ `fighter/rig/` làm lưu trữ"; báo cáo + ảnh lưu trữ vẫn còn ở `plans/reports/fighter-bone-rig-*`. Muốn giữ lại: `git checkout 4c527c36 -- Assets/ArtLibrary/classes/fighter/rig`.

Kiểm tra sau xoá: quét lại tên file/thư mục đã xoá trong Tools + Scripts: không còn chỗ nào đọc (chỉ còn trùng tên với file khác thư mục, vd `mu-ref-v2/pair1-lightning-lance.png` bản đang dùng vẫn còn).

## B. Chỉ còn dùng bởi biến thể cũ / tool đã bị thay (ĐÃ XOÁ đợt 2, người dùng: "nhóm B xoá luôn")

Resources (game không nạp, nhưng còn trong bản build):

| Đường dẫn | Cỡ | Lý do |
|---|---|---|
| `Resources/Sprites/classes/mechanic/weapons/gun/` | 1.4 MB | Súng đơn trước bản 2 súng; Parts dùng `pistol/`. Đọc bởi `build-item-icons.py`, `build-mechanic-gun.py`, đường quay lại trong `build-mechanic-dual.py` |
| `Resources/Sprites/classes/mechanic/companion/` | 2.9 MB | Robot đã tắt (`CompanionFolder = null`) |
| `Resources/Data/classes/mechanic/held-mechanic.json`, `held-mechanic-melee.json` | 20 KB | `GearFile` là `held-mechanic-dual` |
| `Resources/Sprites/classes/cleric/companion/` | 2.8 MB | Spirit chuyển sang pet (`Sprites/pets/spirit`); `build-pet-art.py` còn đọc `.meta` của nó làm mẫu import |
| `Resources/Sprites/classes/wizard/fx/{attack,attack-v2,attack-v3}/` + `vfx-attack*.json` | 4.0 MB | VfxSprites chỉ dùng khi `attack-v4` thiếu; tool fx cũ đọc làm mẫu `.meta` |
| `Resources/Sprites/classes/mechanic/fx/{attack-blue,attack-orange,attack-v2}/` + json | 4.5 MB | Chỉ dùng khi `attack-v3` thiếu (cả `--mech-blue`) |
| `Resources/Sprites/classes/cleric/fx/attack/` + json | 1.9 MB | Chỉ dùng khi `attack-v2` thiếu; `build-cleric-attack-fx.py` đọc |

ArtLibrary (chỉ cờ `build-from-copy.ps1` cũ hoặc tool cũ đọc):

| Đường dẫn (`ArtLibrary/classes/`) | Cỡ | Dùng bởi |
|---|---|---|
| `wizard/outfits/northwind-v7`, `northwind-v8` | 7 MB | `-WizardNorthwind` (install-wizard-northwind-v7); v7 còn được install-wizard-current-reference đọc |
| `wizard/northwind-default-attacks`, `northwind-t9-combos`, `northwind-t9-sides`, `unified-northwind-model` | 41 MB | `-WizardNorthwindCombos`; `unified` còn dùng cho `-FrameHairstyles` |
| `wizard/outfits/astral-v7`, `astral-v8` | 7 MB | `-WizardAstral` |
| `wizard/outfits/reference-20261108` (gồm `before-install`) | 53 MB | `-WizardOriginal`, build-wizard-reference-down |
| `wizard/outfits/grandeur-20261109`, `reference-20261109-exact` | 49 MB | `-WizardGrandeur`, install-wizard-current-reference / exact-hair (bộ trước D) |
| `wizard/outfits/stance-v3`, `stance-v4` (gồm `before-install`) | 44 MB | build-wizard-stance-v3/v4, đã thay bằng bộ D |
| `wizard/outfits/set-t0..t9` | 66 MB | build-wizard-outfit-idle-down (bộ áo trước D) |
| `wizard/outfits/northwind-t0-gear`, `northwind-t0-idle-dirs` | 4 MB | tool Northwind, đã có bản outfit-d |
| `wizard/attack-board/northwind-body`, `northwind-combo-v2`, `walk-board/northwind-walk` | 63 MB | tool Northwind (attacks, combo-v2, walk); bảng combo-v2 còn là nguồn của `build-wizard-attack-fx-v4.py` |
| `wizard/attack-board/body` | 20 MB | build-wizard-attack-board-repaint (bộ trước Northwind) |
| `wizard/outfits/book-grip-question` | 9 MB | build-wizard-t9-book-grip-variants (chỉ còn `install 0`) |
| `mechanic/new-style/pre-dual` | 9 MB | đường quay lại súng đơn (build-mechanic-dual, -dual-attack) |
| `cleric/companion-src/backup` | 3 MB | build-cleric-spirit (companion cũ); GIỮ `companion-src/src` vì build-pet-art đọc |

Tổng B khoảng 17 MB Resources + 380 MB ArtLibrary. Kèm `Assets/ArtLibrary/avatar-hair-parts/` (13 MB, nằm ngoài `classes/`, chỉ phục vụ `-FrameHairstyles`).

### Đợt 2 (16/11): đã xoá nhóm B + gỡ code/tool

- `git rm` 1329 file (1034 ảnh/json + `.meta`), 410.6 MB; trong đó Resources 424 file, 17.3 MB (bản build nhẹ đi chừng đó). Giữ `outfit-d-tiers-design` và mọi thứ bộ D đang dùng.
- Xoá 40 tool chỉ phục vụ nhóm B: `install-wizard-{northwind-v7,astral-v7,original,current-reference,exact-hair,unified-model}.py`, `install-northwind-{default-attacks,t9-combos}.py`, `export-northwind-t9-{combos,sides}.py`, `compile-avatar-hair-frames.py`, `import-avatar-hair-parts.py`, `prepare-avatar-hair-art.py`, `check-avatar-frame-preview.ps1`, `review-northwind-{collars,down-heads}.py`, `compose-{astral-comparison,astral-v8,northwind-comparison,northwind-slim-check,wizard-grandeur-review,wizard-exact-review,wizard-battle-mage-check}.py`, `check-{wizard-height,northwind-t9-combos,wizard-northwind-model,wizard-exact-hair}.ps1`, `build-wizard-{stance-v3,stance-v4,reference-down,t9-book-grip-variants,northwind-attacks,northwind-idle-dirs,northwind-t0-gear,northwind-walk,combo-v2-attacks,attack-fx}.py`, `wizard-combo-v2-chatgpt-vs-ingame-sheet.py`, `build-mechanic-gun.py`, `build-mechanic-dual-attack.py`.
- `Tools/build-from-copy.ps1`: bỏ `-WizardNorthwind`, `-WizardNorthwindCombos`, `-WizardAstral`, `-WizardOriginal`, `-WizardGrandeur`, `-FrameHairstyles` và đoạn code chúng bật.
- Code game: `VfxSprites.IdFor`/`Preload` chỉ còn `cleric-v2`, `wizard-v4`, `mechanic-v3` (bỏ nhánh dự phòng); sửa comment `HeroClassDef` (bỏ đường quay lại súng đơn, robot), `MechanicCannonBlastFx`, `HeroFx`. `ClassPalette.MechanicBlue` / `--mech-blue` vẫn còn (đổi màu FX vẽ bằng code), chỉ không còn bộ sprite xanh.
- Tool còn giữ, sửa để không phụ thuộc file đã xoá:
  - `build-mechanic-dual.py` thành thư viện (arts, compose, cannon_anchor, weapon_manifest...) cho pistol-tiers / cannon-tiers / attack-down-repaint; bỏ backup/grid/preview/write/muzzles, bảng fist và đường quay lại (dựa trên `pre-dual`). `build-mechanic-pistol-tiers.py` lấy tên + glow từ `held-mechanic-dual.json`.
  - `build-cleric-spirit.py` thành thư viện cho `build-pet-art.py` (key, tách cặp, icon); bỏ install/float/icons/backup/preview/drafts. `build-pet-art.py` lấy `.meta` mẫu từ `pets/spirit` (đã chạy thử `spirit()` OK).
  - `build-mechanic-attack-fx-v2.py` thành thư viện (key_magenta, pivot, draw_fx) cho mechanic v3 + cleric; `build-mechanic-attack-fx-v3.py` ghi cứng cỡ v2 (`V2_SIZES`), `.meta` mẫu từ chính bộ v3, preview so với wizard v4; `build-cleric-attack-fx.py` `.meta` mẫu từ chính bộ v2, bỏ hàng so bộ cũ; `build-wizard-attack-fx-v3.py` (nền của v4) lấy `.meta` mẫu từ attack-v4, `build-wizard-attack-fx-v4.py` bỏ thay chuỗi cũ (đã chạy thử nạp OK).
  - `compose-hero-frame.py`: Mechanic dùng `held-mechanic-dual` (pháo + 2 súng, art theo anchor, tâm giữa như `GearOverlays.CenteredTier`); đã ghép thử idle xuống, khớp trong game.
  - `build-item-icons.py`: bỏ phần icon vũ khí/offhand (súng đơn, companion); icon đó do tool tier từng class ghi. `build-idle-down-set.py` Mechanic dùng `held-mechanic-dual`.
  - Ghi chú docstring: `build-wizard-attack-board-repaint.py` (giờ là thư viện của outfit-d), `build-wizard-outfit-idle-down.py` (thư viện của cleric), `build-mechanic-new-style.py` (không chạy write), outfit-d-*/hair-on-bald-body/wizard_combo_v2_poses/wizard_attack_fx_common/cannon-blast/tier-weapon-fit.
- Còn nhắc tên cũ (cố ý): `export-held-gear.mjs` (bộ xuất từ bản web, đã ghi "không chạy lại"), docstring lịch sử. `docs/wizard-*-comparison.md`, `wizard-original-restored.md`, `wizard-stance-v3.md`, `wizard-current-reference.md`, `avatar-frame-hairstyles.md`, `sprite-rules.md` còn mô tả các biến thể/cờ đã bỏ (chưa sửa docs).

## C. Không chắc (không tool nào đọc theo tên, nhưng có thể là ảnh gốc / mẫu)

| Đường dẫn | Cỡ | Ghi chú |
|---|---|---|
| `*/frames/<skill>` (assassin 59, cleric 17, warrior 44 MB), `*/overlays/*`, `warrior/frames-src`, `mechanic/frames-src`, `warrior/hero-src` | ~135 MB | ảnh gốc bản web; CLAUDE.md coi `frames`, `overlays` là ảnh gốc |
| `*/heads/dirs` (4–11 MB mỗi class) | ~30 MB | không tool nào đọc tên này |
| `*/new-style/ref(s)`, `*/gear-icons-src/ref`, `cleric/companion-src/ref`, `warrior/shield-src/ref`, `assassin/new-style/dagger-src/ref-user-crops` | ~65 MB | ảnh tham khảo gửi ChatGPT |
| `wizard/redesign/*-20261111`, `redesign/{gear-ladders,outfit-ladders,ref}`, `head-redesign-20261114/ref`, `attack-northwind-20261109`, `attack-board/northwind-all-tiers`, `northwind-t9-attack`, `outfits/northwind-wardrobe-connected` | ~13 MB | bản nháp/so sánh thiết kế |
| `wizard/outfits/outfit-d-tiers-design` | 21 MB | bảng thiết kế bộ D đang dùng: nên giữ |
| `*/outfits/design-ladder` (v1), `design-ladder-v3` | ~43 MB | v3 còn được tool outfit-idle-down đọc |
| `mechanic/outfits/weapons-v3`, `warrior/{shield,sword}-src/v3-ladder`, `sword-src/t9-dragon-v3`, `sword-src/mu-ref`, `warrior/outfits/t1-steel`, `warrior/heads/{look-v2,original-expressions}` | ~57 MB | nguồn vũ khí/áo/đầu, không tool hiện tại đọc theo tên (mu-ref dùng khi `SHEET_SET=mu-ref`) |

Giữ chắc (tool đang đọc, KHÔNG đưa vào A dù tên cũ): `*/body/hand/*.pre-head.png` (head layer đọc), `*/heads/rejected-hood` (head_layer_dir_styles), `mechanic/new-style/attack-down-repaint/old`, `mechanic/new-style/dual-src/cannon-back-lr/old`, `warrior/new-style/skill-poses/old`, `cleric/shield-src/old/icons`, `cleric/outfits/gear-src/old-pre-v3` (T0 acolyte), `assassin/new-style/old/{bare,idle-down-row0,dagger-pre-tier-sets}`, `wizard/outfits/outfit-d-t0/{old,staging}`, `wizard/outfits/outfit-d-tiers/staging` (có `tiers-measure.json`), `wizard/heads/layered/on-body/staging`.

## Kiểm tra

- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 404/404 qua.
- `dotnet test Tests/dotnet/ZenoniaSkills.Tests`: trên cây làm việc KHÔNG biên dịch được vì file đang viết dở của việc khác (`Assets/Scripts/Rpg/Account/*`, untracked, thiếu `PetConfig.MaxStars`...), không liên quan việc dọn asset. Chạy trên worktree sạch HEAD: 322/322 qua (test không đọc ArtLibrary).
- Build từ bản sao (Scripts/Resources của HEAD, không kèm `Rpg/Account` dở): `D:/temp/player-asset-cleanup/Zenonia.exe` OK. Ổ C còn ~1,4 GB nên lần đầu ra `C:/temp/...` hỏng (hết chỗ), đã ra ổ D.
- Việc xoá chỉ đụng `ArtLibrary` (không vào bản build, `build-from-copy` không chép), `git diff --cached -- Assets/Resources` rỗng: game không đổi. Vẫn chụp trong game cho chắc: 6 class idle/walk/combo hướng xuống + phải, Wizard tóc 1/3/4 + nhuộm + bộ T5/T9 (cả walk), Warrior/Mechanic/Assassin/Cleric bộ T7. Ảnh `D:/temp/cap-asset-cleanup/sheet-0.png`, `sheet-1.png`: không thấy sprite hồng/trắng/trống, vũ khí/tóc/áo đủ.
- Player.log: chỉ có "Missing texture" vốn có từ trước (Wizard `set-tN` chỉ có `idle`, Fighter không có `attack1-3`; các file này không có ở HEAD), không có lỗi null/exception.

### Kiểm tra đợt 2

- Grep lại Tools/Scripts/Editor/Tests: không còn code đọc file/tool đã xoá (chỉ còn nhắc trong docstring lịch sử, `export-held-gear.mjs`; `Tools/__pycache__` cũ chưa xoá được vì hook chặn, file bị gitignore).
- `dotnet test` trên cây làm việc (lúc này đã biên dịch được): ZenoniaSkills 327/327, ZenoniaNet 404/404.
- Build từ worktree sạch = HEAD + các sửa C# của việc này + xoá Resources nhóm B (không kèm code dở của agent pet/level-90), bản sao `D:/temp/zu-copy-all` (C:/temp/zu-copy đã bị xoá): `D:/temp/player-asset-cleanup-b/Zenonia.exe` OK.
- Chụp trong game (`D:/temp/cap-asset-cleanup-b/sheet-0.png`, `sheet-1.png`, đã tự mở xem): 6 class idle (kèm pet spirit) / walk / combo hướng phải; Wizard, Mechanic, Cleric combo hướng xuống; Wizard tóc 2 + nhuộm + T9, tóc 4 + T4; Mechanic T8 + pet imp; Cleric T6. VFX đúng bộ mới: Wizard băng v4, Mechanic vàng v3 (2 súng + pháo), Cleric xanh lá v2. Không sprite hồng/trắng/trống.
- Player.log: chỉ "Missing texture" vốn có (`set-tN` chỉ có idle, Fighter không có attack1-3), không có lỗi VFX/companion/gun, không null/exception.

### Đợt 3 (16/11): docs biến thể Wizard, HeroCompanion, --mech-blue (người dùng: "CÓ" cả hai)

- Xoá 10 docs: `docs/avatar-frame-hairstyles.md`, `northwind-down-head-fix.md`, `wizard-{astral-comparison,battle-mage-stance,current-reference,grandeur-comparison,northwind-comparison,original-restored,reference-down-20261108,stance-v3}.md`.
- `docs/sprite-rules.md`: 11 đoạn hướng dẫn đầu file (Northwind/Astral/Grandeur/stance v3-v4/mẫu 08/11) thay bằng 1 đoạn "Bài học từ các bản Wizard đã bỏ" (scale đều, giữ chiều cao, không uốn kéo tay, cán trượng < nắm tay, giữ anchor động tác khác, zoom/HUD, ảnh giao đủ đồ); bỏ 10 dòng bảng tool đã xoá (t9-book-grip, mechanic FX v2, wizard attack board 08/11, wizard FX v3, 5 dòng Northwind T0, wizard bộ đồ 07/11); bỏ nhắc `build-mechanic-gun.py`; sửa link `wizard-original-restored.md` và câu "đối diện HeroCompanion" ở mục Pet. GIỮ mọi dòng Sổ lỗi (kể cả dòng Northwind/Astral/linh thú).
- `docs/pending-tasks.md`: 9 mục lịch sử bản so sánh Wizard 08–09/11 thay bằng 1 dòng trỏ báo cáo này. `README.md`: bỏ `--mech-blue`, dòng `HeroCompanion.cs`.
- Gỡ `HeroCompanion.cs` (+ .meta); `GameSession` bỏ 3 lời gọi; `HeroClassDef` bỏ `CompanionFolder`, `CompanionFloatFolder`, `CompanionGrounded`, `CompanionScale`; `HeldGearData` bỏ `Companion`, `CompanionFloat`, `FloatLoop` và 2 khoá json (JsonUtility bỏ qua khoá thừa); `HeroRenderer` bỏ `CompanionTier/Enhanced/Band`, đổi `CompanionEmpty` thành `OffhandEmpty` (dùng ở `FrostSmokeTest`); `FrostSmokeTest` bỏ kiểm "companion created"; comment `HeroPet`.
- Gỡ cờ `--mech-blue` (`DebugCapture.cs`, 3 dòng). Phím C / `GameKey.FxColor` + `ClassPalette.MechanicBlue` (đổi màu FX vẽ bằng code, gửi qua `NetEventBus`) vẫn còn: nằm trong file agent khác đang sửa (`HeroController`, `NetEventBus`, `InputAdapter`), chưa đụng.
- Còn để nguyên: khoá `companion`/`companionFloat` trong `held-cleric.json`, `held-mechanic-dual.json` (game bỏ qua; tool cũ `build-cleric-new-style.py`, `build-cleric-shield.py` cắt chuỗi theo khoá này nên chưa xoá), tham số `enhancedArt` của `HeldEnhanceFx`.

### Kiểm tra đợt 3

- `dotnet test`: ZenoniaSkills 327/327, ZenoniaNet 404/404 (cây làm việc).
- Build từ worktree sạch (HEAD + sửa của việc này, không kèm code dở của agent khác), bản sao `D:/temp/zu-copy-all`: `D:/temp/player-asset-cleanup-c/Zenonia.exe`, 0 lỗi (Unity biên dịch cả Editor: `FrostSmokeTest` dùng `OffhandEmpty`).
- Chụp Mechanic / Cleric / Wizard (`D:/temp/cap-asset-cleanup-c/sheet.png`, đã tự mở xem): idle + pet spirit/imp, walk, combo phải + xuống, Wizard tóc 3 nhuộm T9, Mechanic T8, Cleric T6. 2 súng + pháo, VFX vàng / xanh lá / băng đúng bộ mới, không companion, không sprite hồng/trắng/trống.
- Player.log: chỉ "Missing texture" vốn có (`set-tN` chỉ có idle), không null/exception.

### Đợt 4 (16/11): phím C đổi màu FX Mechanic + khoá json companion (người dùng: làm)

- Gỡ `ClassPalette.MechanicBlue`, `GameKey.FxColor` + phím C trong bảng bind (`InputAdapter`, enum không đi qua mạng), dòng xử lý phím C ở `HeroController`, dòng HUD "[C] Mechanic FX" (`Hud.cs`); `HeroClassDef.ActivePalette` giờ luôn trả `Palette` (Mechanic chỉ còn vàng v3). `ClassPalette.Blue` giữ vì Ice Storm dùng.
- `NetEventBus.AppearanceOf`: `Flags = 0` + chú thích "reserved". GIỮ field `NetAppearance.Flags` (không ai đọc nhưng nằm trong layout wire của protocol 20, không nâng version); test round-trip `MessageRoundTripTests` giữ nguyên (vẫn qua).
- Xoá khoá `companion`, `companionFloat` trong `held-cleric.json` (-2254 byte) và `held-mechanic-dual.json` (-2232 byte); kiểm dump lossless trước khi ghi, giữ dạng 1 dòng minified. `build-cleric-new-style.py` (partition theo `"anchors":[`), `build-cleric-shield.py` (lookahead/chèn trước `"anchors":[`) đã sửa, nạp thử OK; `export-held-gear.mjs` chỉ sửa chú thích.
- README: bỏ dòng phím C, câu "C swaps mechanic-orange / mechanic-blue", mô tả Mechanic (vàng, 2 súng + pháo).

### Kiểm tra đợt 4

- `dotnet test` cây làm việc: ZenoniaSkills 327/327, ZenoniaNet 405/405 (agent khác thêm 1 test).
- Build từ worktree sạch (HEAD + patch các file chỉ mình sửa + sửa chuỗi trong `DebugCapture`/`HeroController`/`InputAdapter`/`NetEventBus`, không kèm code dở của agent khác), bản sao `D:/temp/zu-copy-all`: `D:/temp/player-asset-cleanup-d/Zenonia.exe`, 0 lỗi.
- Chụp Mechanic / Cleric combo hướng phải + xuống, idle T7 kèm pet (`D:/temp/cap-asset-cleanup-d/sheet.png`, đã tự mở xem): Mechanic FX chỉ còn vàng (chớp nòng, đạn to, nổ pháo, trúng), 2 súng + pháo; Cleric gậy + khiên, FX xanh lá v2; không companion, không sprite hồng/trắng/trống.
- Player.log: không có lỗi mới (chỉ "Missing texture" `set-tN` vốn có), không null/exception.

## Câu hỏi chưa giải quyết

- Nhóm C: giữ (người dùng chốt 16/11), không đụng.
- `NetAppearance.Flags` giờ luôn 0: bỏ hẳn field khi lần tới nâng protocol?

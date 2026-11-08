# Hiệu ứng đòn thường Wizard v2 (đĩa ấn phù quay)

Ngày 2026-11-05. Chưa build, chưa xem trong game. Không commit.

## Pipeline hiện tại (khác brief)

- Wizard KHÔNG dùng `fx/combat/magicBolt.png`, `arcaneBurst.png` (đó là dự phòng `BurstFx`/`ArcaneBoltRig`/`RuneFx` khi thiếu art). Đòn thường vẽ bộ tay `Sprites/classes/wizard/fx/attack/{cast,proj,impact}_0..3.png` + `Data/classes/wizard/vfx-attack.json` qua `VfxSprites.IdFor(BoltStyle.Arcane) = "wizard"`, `VfxAnim` (cast 4 frame 26 fps tại đầu gậy, không xoay; impact 4 frame 16 fps, không xoay), `ComboProjectile.Painted` (proj lặp 4 frame 16 fps, pivot tâm cầu, xoay theo hướng bay). 1 px nguồn = 0,058 đơn vị. Ba đòn đầu cùng art; đòn 4 chỉ x1,35 (`VfxAnim.FinisherScale`) + tàn lửa.
- Cleric có bộ riêng `cleric` (`BoltStyle.HolyArrow`), không chung với Wizard; `fx/combat/` không bị đụng.
- Không có `AtMs`/`SkillTable` cho combo; thời gian cast/impact giữ nguyên trong `VfxAnim`, không đổi số liệu `ComboConfigs`, `Rpg/`.

## Thiết kế mới

Đĩa ấn phù (vòng rune + lục tinh, lõi trắng) quay, 2 lưỡi trăng khuyết phía sau; cast = vòng ấn mở ra rồi tan thành 3 vành; hit = đĩa đập xuống, nứt, vỡ thành mảnh rune. Tím `#7A4DFF`/`#3A2EFF`/`#D9C8FF`, hợp `ClassPalette.Arcane`. Một thiết kế cho cả combo, đòn 4 to hơn như cũ.

## ChatGPT

- Tài khoản Tay ("Tân Korris", đã kiểm tra `/api/auth/session`), chat mới "Create VFX Sprite Sheet": https://chatgpt.com/c/6aec7342-4068-83ec-8844-ac7846da159a. 1 lượt vẽ, ảnh 1536x1024 đạt ngay (bố cục 3x4, nền `#00FF00` phẳng, không chữ).
- Gốc: `Assets/ArtLibrary/classes/wizard/vfx/attack-v2/wizard-attack-v2-sheet-1.png` (tải bằng fetch blob, trùng file Downloads).

## File thêm / sửa

- Thêm `Tools/build-wizard-attack-fx.py` (`cut`, `preview`) + `Tools/wizard_attack_fx_common.py` (key xanh theo bão hoà, chia ô theo chỉ số ô, tô đặc vùng bị bao quanh, viền tím, xoay lõi đĩa, resize premultiplied, nhân bản `.meta` với guid mới).
- Thêm `Assets/Resources/Sprites/classes/wizard/fx/attack-v2/` 12 PNG + `.meta` (import giống `fx/attack/`: không nén, 4096, guid mới, không trùng) + `attack-v2.meta`; `Assets/Resources/Data/classes/wizard/vfx-attack-v2.json` + `.meta`.
- Sửa `Assets/Scripts/VfxSprites.cs`: `IdFor(Arcane)` → `"wizard-v2"`, `Preload(Wizard)` → `Get("wizard-v2")`, comment. Chỉ Wizard đổi. Quay lại bản cũ: đổi hai chữ về `"wizard"`.
- Docs: 5 dòng Sổ lỗi trong `docs/sprite-rules.md`; 1 dòng trong `docs/pending-tasks.md` mục "Xem trong game, chưa ai xem".

## Xử lý art (ghi trong Sổ lỗi)

- Mặt trong đĩa ChatGPT tô xanh ngọc mờ ăn nền → tô đặc vùng bị viền bao quanh lên tím đậm (`SOLID`), pixel xanh ngọc kéo về tím.
- 4 frame đạn xoay không đều → dựng từ frame 0, lõi quay 15°/frame (4 frame = 60° = đối xứng lục tinh), gai và lưỡi đứng yên.
- Frame nét trắng mảnh mất trên nền tuyết → viền tím 4 px (`OUTLINE`).
- Cỡ: `ROW_SCALE` cast 0,9 (vòng đầy 15,4 đơn vị), proj 0,95 (đĩa 9,3, cả đuôi 15,4), impact 1,05 (16-17,5; đòn 4 ≈ 23).

## Xem trước

`C:/temp/cap-cannon/wizard-attack-fx-preview.png` (2624x1920): nền tối + nền sáng, mỗi nền 4 hàng: bộ cũ, bộ mới, đòn 4 x1,35, Ice Storm impact cỡ game; hero Wizard attack1 để so cỡ; 8 px/đơn vị (2x cửa sổ 640p), có lớp additive 0,22 như game. Đã xem từng frame ở cỡ thật trên cả hai nền.

## Chưa kiểm tra được

- Không chạy Unity/build: chưa thấy Unity biên dịch `VfxSprites.cs` (Editor.log không có dòng biên dịch mới), chưa thấy đĩa quay thật ở 16 fps, cast 15 đơn vị ở đầu gậy có che mặt hero không, hit đòn 4 cạnh Ice Storm trong game, bloom lên lõi trắng.
- Unity đang mở có thể đã tự sinh `.meta` trước khi tool ghi; tool giữ guid sẵn có nếu file đã tồn tại, nhưng nên kiểm tra Inspector: Compression None, Max 4096.

## Câu hỏi còn mở

- Cast có nên xoay theo hướng bắn không (hiện tròn, không xoay như cũ)?
- Giữ bộ cũ `fx/attack/` (id `wizard`, không còn ai dùng) hay xoá sau khi người dùng duyệt?
- Đuôi đạn ngắn hơn hẳn bản cũ: nếu người dùng muốn dài hơn phải vẽ lại hàng proj (prompt trail dài 2-3 đường kính đĩa).

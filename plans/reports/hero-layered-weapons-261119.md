# Nhân vật nhiều lớp cầm vũ khí + sửa mũ (19/11/2026)

Branch `worktree-agent-a4ca4a5ec0dcfa022` (gốc `unity` e33a83b4, đã merge `unity` 54843dd5: palettes RO Soft, màn tạo nhân vật). Exe: `D:\temp\player-weapons\Zenonia.exe` (bản sao `D:\temp\zu-copy-weapons`, Succeeded, 0 lỗi).

## Pipeline
```
python Tools/build-hero-layered-grips.py fists   # dò 2 nắm tay mọi frame -> ArtLibrary/hero-layered/sheets/<giới>/fists.json + ảnh đánh dấu
python Tools/build-hero-layered-grips.py write   # lớp nắm tay body/hand/<anim>.png + neo vũ khí 8 hướng vào Data/hero-layered/<class>/class.json
python Tools/build-hero-layered-grips.py check   # ảnh ghép như game + kiểm luật (vũ khí trước đè mặt, lưỡi chĩa vào trong)
python Tools/build-hero-layered-game-assets.py accessory   # tách kính Mechanic khỏi tóc (write cũng làm, và giữ grips)
```
- Dò nắm tay (`hero_layered_fists.py`): da nâu bão hoà, mở 4 px, bỏ ủng (dưới thắt lưng 30 px), khoá thắt lưng, mắt/tai. R/L: xuống/chéo xuống R = x nhỏ; lên/chéo lên R = x lớn; ngang R = mảng to (tay gần). Một mảng to = hai tay chụm. Tay khuất đoán ở hông, vẽ sau thân. 29 frame sửa tay trong `OVERRIDES` (đọc trên ảnh đánh dấu 2x).
- Tư thế (`hero_layered_grip_poses.py`): idle/đi kiếm 58°, dao 35° chĩa xuống ra ngoài (+ nửa độ vung tay); gậy dựng nghiêng 8° (ngang 20°), cầm ở 74 % chiều cao; súng chĩa xuống-trước 25°; khiên/sách dựng. Đánh 1/3: theo hướng vai -> nắm tay; đánh 2: tay phải chĩa theo hướng nhìn; frame 3 = tư thế nghỉ. Mũi chạm đất -> xoay ra ngang. Vũ khí trước đè vùng đầu+tóc -> xoay ra (gậy tối đa 18°) không được thì ra sau thân.
- 3 hướng trái: vũ khí chính ở nắm tay XA (lật hàng phải), vẽ sau thân trừ khi nắm tay nằm trên thân, art lật (`flip`), góc = góc tay phải lật; tay phụ ở nắm tay gần, trước thân.
- Renderer: `HeroLayeredView` dùng `flip`, `overHand` (khiên vẽ trên lớp nắm tay), lớp `L_HairAccessory`; vũ khí / tay phụ ẩn chỉ khi tay đó không cầm gì (`LayeredWeaponOn`, `offhandEmpty`).
- Mũ: `hidesHair` thêm mũ len, mũ ếch, mũ slime (headgear.json + `HeadgearCatalog`, test đối chiếu qua). Kính Mechanic: lớp `hair/mechanic/0-acc.png` (`accSheet`), ẩn khi đội mũ bất kỳ, hiện khi đầu trần.

## Ảnh (đã xem)
- Offline `docs/reference/hero-layered-weapons/`: `<class>-{idle,walk,attack1,attack2,attack3}.png` (8 hướng x mọi frame; idle 3 cột: đầu trần / mũ class / mũ rơm), `fists-<giới>-<anim>.png`. Đã xem kỹ: warrior idle/walk/attack1/attack3, wizard idle/walk/attack1, mechanic idle/attack2, assassin attack1, cleric attack3. Còn lại chỉ qua kiểm luật tự động (0 lỗi đè mặt, 0 lưỡi chĩa vào trong), chưa xem từng frame bằng mắt: warrior attack2, wizard attack2/3, mechanic walk/attack1/3, assassin idle/walk/attack2/3, cleric idle/walk/attack1/2.
- Trong game `docs/reference/hero-layered-weapons/ingame/`: đám đông 10 hero (2 mỗi class) idle 8 hướng, đi phải, đánh mọi hướng, đánh khi quay trái (Warrior, Mechanic là hero mình). Thấy: kiếm tay phải + khiên tay trái, quay trái kiếm sau thân; gậy/sách Wizard, gậy/khiên Cleric, 2 dao, 2 súng đều trong tay; kính Mechanic hiện khi đầu trần, mất khi đội mũ/vương miện.

## Frame còn sai / chưa ổn
- Gậy hướng ngang khi đi: đổi trước/sau thân giữa các frame (tay gần vung ra sau thì gậy ra sau, chỉ đầu gậy ló cạnh đầu).
- Đánh 3 frame 0 (giơ hai tay qua đầu): vũ khí dựng trên đầu, đè lên mũ cao (thấy trong game với vương miện/mũ sắt).
- Đánh 1 hướng xuống/chéo frame 0: kiếm/gậy dài vươn ra ngoài ô 320 px (không cắt trong game, chỉ ở ảnh ghép).
- Tóc Mechanic khi đội mũ: hướng chéo/ngang còn mảng sọ hở nhỏ dưới vành (ChatGPT không vẽ tóc dưới kính). Hết hẳn: vẽ lại tóc Mechanic không kính.
- Mũ len/ếch/slime ẩn tóc: chưa xem trong game (offline hiện đúng theo `hidesHair`).
- Hero của mình không tự đánh trong cảnh chụp; skill pose, lướt, bị đánh dùng anim attack2/walk/idle như bước D.

## Kiểm tra
`ZenoniaSkills.Tests` 408/408, `ZenoniaNet.Tests` 422/422 (SDK 10).

## Câu hỏi
- Đánh 3 frame giơ tay: có cho vũ khí ra sau thân để không đè mũ không?
- Vẽ lại tóc Mechanic không kính (cần ChatGPT)?

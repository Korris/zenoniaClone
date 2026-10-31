# Quy tắc sprite và VFX

**Bắt buộc đọc hết file này trước khi làm bất cứ việc gì về sprite hoặc VFX, kể cả khi giao cho agent.** Mỗi khi phát hiện lỗi sprite mới, ghi ngay vào mục "Sổ lỗi" và thêm quy tắc nếu chưa có.

Áp dụng mỗi khi vẽ lại, cắt lại, thêm sprite nhân vật hoặc vũ khí. Lập ngày 2026-10-31, sau các lỗi Warrior phát hiện khi chơi thử.

## Cách hero được vẽ

Thân `bare/` (không vũ khí) + ảnh vũ khí/khiên đặt theo anchor từng frame. Thứ tự: khiên hoặc kiếm phía sau, thân, khiên hoặc kiếm phía trước, nắm tay `bare-hand/`.

| Dữ liệu | File |
|---|---|
| Anchor từng frame (`x`, `y`, `angle`, `s`, `behind`, `hidden`, `fallback`) | `Assets/Resources/Data/held-gear-melee.json` |
| Art và điểm cầm của từng loại vũ khí (`baseGrip`, `enhancedGrip`) | `Assets/Resources/Data/held-gear.json` |
| Cắt sheet chém, tính anchor melee | `Tools/build-melee-sheets.py` |
| Nắm tay đè lên chuôi, anchor đứng yên/đi hai hướng ngang | `Tools/build-hand-overlay.py` |
| Anchor súng Mechanic (`held-mechanic.json`) và lớp nắm tay `mechanic-hand/` | `Tools/build-mechanic-gun.py` (`Tools/export-held-gear.mjs` sẽ ghi đè, không chạy lại cho Mechanic) |
| Danh sách động tác có lớp nắm tay của từng class | `HeroClassDef.HandFolder`, `HandAnims` |
| Code vẽ | `Assets/Scripts/HeroRenderer.cs` |

## Quy tắc tư thế cầm

1. **Khi cầm vũ khí, vũ khí không được đè lên tay (mọi class, mọi loại vũ khí).** Tay phải nắm qua chuôi: bàn tay vẽ trên vũ khí, chuôi nhô ra hai đầu nắm tay. Nếu vũ khí vẽ trước thân thì phải có lớp nắm tay đè lên (`bare-hand/`); nếu vũ khí ở tay phía xa thì vẽ sau thân. Kiểm tra trên sheet `bare/` mà game thật sự vẽ, vì tay trong sheet `hero/` có kiếm thường ở chỗ khác.
2. **Lưỡi vũ khí đúng chiều ở mọi frame:** tay nắm ở chuôi, mũi hướng ra ngoài.
3. **Một vũ khí chỉ có một cỡ.** Warrior dùng `s = 0.34` cho đứng yên, đi, chém và skill. Không lấy cỡ theo sheet vẽ sẵn, vì kiếm trong đó nhỏ hơn.
4. **Không được đổi tay khi quay trái/phải.** Warrior cầm kiếm tay phải, khiên tay trái:
   - Quay phải: kiếm ở tay gần camera, khiên ở tay xa.
   - Quay trái: khiên ở tay gần và vẽ trước thân; kiếm ở tay xa và vẽ sau thân.
   - Lật ảnh hướng phải sẽ đổi tay, nên phải đặt lại vũ khí riêng cho hướng trái.
5. **Cắt lại hoặc vẽ lại sheet `bare/` thì phải đọc lại vị trí nắm tay** (`FISTS`, `FAR_FISTS` trong `build-hand-overlay.py`, `fixes` trong `build-melee-sheets.py`), rồi chạy lại tool.
6. **Sửa xong phải xem từng frame** ở ảnh ghép giống cách game vẽ, rồi xem lại trong game. Không tin báo cáo "xong" hay chỉ số.
7. **Vũ khí đang cầm và vũ khí lúc đánh phải là cùng một khẩu/thanh:** cùng art, cùng cỡ, không méo (`sx` không bóp tỉ lệ), trừ khi không có sheet `bare/` cho động tác đó.
8. **Hạn chế `fallback`:** frame này dùng vũ khí vẽ sẵn, nên khác cỡ và không theo đồ. Chỉ dùng khi không còn cách khác và phải báo lại.

## Quy tắc art lấy từ ChatGPT

1. **Không xin nền trong suốt:** ChatGPT hay vẽ cứng ô caro vào ảnh. Xin nền một màu không có trong hiệu ứng: `#00FF00` cho art tím/đen, `#FF00FF` cho art xanh lá.
2. **Viết "vẽ mới hoàn toàn từ đầu, không dựa trên các ảnh trước".** Viết "vẽ lại" dễ khiến ChatGPT sửa từ ảnh cũ nên còn dính caro.
3. **Hình phải đặc và đục:** ảnh mờ (độ đục khoảng 0,35) trông nhạt hẳn so với hiệu ứng khác. Khói vẽ thành lọn liền khối, viền sắc.
4. **Giữ cỡ khung và pivot cũ,** các frame không được chạm mép ảnh, tâm hiệu ứng cùng vị trí ở mọi frame.
5. **Xem bản xem trước trên cả nền tối và nền sáng,** ở cỡ gần với game, cạnh một hiệu ứng khác của cùng class.

## Quy tắc VFX skill

**Hình và số liệu**
1. **Thời gian trong code hình phải khớp bảng skill.** `LandMs`, `CastToMs` trong `*Fx.cs` phải bằng `AtMs` trong `Rpg/Skills/SkillTable.<Class>.cs`. `atMs` mặc định là 0, nếu quên thì sát thương đánh trước hình.
2. **Tầm và kích thước lấy từ dữ liệu skill** (`e.Radius * ctx.Units`), không ghi cứng. Kích thước trên màn hình = pixel × `UnitsPerPx`; 1 ô là 16 đơn vị, hero cao khoảng 20 đơn vị.
3. **Hiệu ứng tự buff phải trong mờ, không được che hero** (Mana Shield, Heal, Sanctuary, Spirit Blessing, Guardian Spirit, Berserker). Vòng quanh người thì cắt đôi: nửa trên sau hero, nửa dưới trước hero.
4. **Hiệu ứng cường hoá không được che vũ khí.**
5. **Độ đậm phải đồng đều trong cùng class:** so bản xem trước cạnh hiệu ứng khác của class đó ở cỡ gần với game.

**Nối vào game**
6. **Skill mới phải nối đủ:** dispatch ở `SkillExecutor` hoặc `AssassinSkillArt`, preload, và kiểm tra `SkillChoreo.DrawsBody` để không vẽ trùng hiệu ứng cũ.
7. **Hình chỉ chạy ở nơi có `NetMode.RendersFx`,** sát thương và trạng thái chỉ chạy ở authority. Client không có `Target` của server: nó tự chọn mục tiêu gần nhất và lấy trạng thái quái từ snapshot.
8. **Bảng skill dùng chung với server.** Đổi tầm hay thời gian thì phải build lại server, nếu không hình và sát thương sẽ lệch khi chơi online.

**Lỗi code hay gặp**
9. **Tìm mục tiêu cho hình trước khi gây sát thương.** Quái bị giết ngay đã ở trạng thái dying, tìm sau sẽ không thấy.
10. **Đổi map phải dọn hình:** thêm class hình vào `ProxySpawner.ClearForTransfer`; lúc đó quái bị huỷ mà không chết, nên đừng phát hiệu ứng "quái chết" hay rung màn hình.
11. **Dictionary tĩnh có khoá Unity phải xoá được khi object đã huỷ:** dùng `(object)hero != null`, vì `hero == null` đúng với object đã huỷ và làm bỏ qua lệnh xoá.
12. **Rung màn hình, hit-stop, flash chỉ cho người chơi cục bộ** (`IsLocal`).
13. **Giãn thời gian có thể làm âm thanh kêu 2 lần,** vì hai lệnh không còn rơi vào cùng frame để bộ lọc 70 ms chặn.

## Sổ lỗi

| Ngày | Class | Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|---|---|
| 2026-10-31 | Warrior | Kiếm chĩa ngược, tay nắm giữa lưỡi kiếm | Script nhận sai đầu kiếm; bản sửa cũ đảo grip/tip theo danh sách mà không xem ảnh | Dò góc từ sheet có kiếm, đặt tay nắm theo nắm tay ở `bare/` |
| 2026-10-31 | Warrior | Kiếm lúc chém bé hơn lúc đứng yên | Lấy cỡ theo kiếm vẽ sẵn trong sheet | Cố định `s = 0.34` |
| 2026-10-31 | Warrior | Chuôi đè lên bàn tay khi đứng yên/đi ngang | Kiếm vẽ trước toàn bộ thân, chuôi lệch nắm tay | Lớp `bare-hand/` đè lên chuôi, dời chuôi vào tâm nắm tay |
| 2026-10-31 | Warrior | Quay trái thì khiên và kiếm đổi tay | Hướng trái là ảnh lật của hướng phải | Đặt riêng: khiên tay gần, kiếm tay xa vẽ sau thân. Combo chém chưa sửa |
| 2026-10-31 | Assassin | Bóng Shadow Clone không có động tác chém | Tìm quái sau khi gây sát thương nên nhát giết không có mục tiêu | Tìm quái trước khi gây sát thương |
| 2026-10-31 | Assassin | Dao độc trông nhạt | `cast`, `dagger` bị trong suốt, độ đục khoảng 0,35 | Vẽ lại hình đặc, lõi sáng |
| 2026-10-31 | Assassin | Khói và quầng còn vân caro | ChatGPT vẽ cứng caro vào ảnh RGB | Vẽ lại trên nền một màu rồi tách nền |
| 2026-10-31 | Assassin | Tay đầu tiên tách nền vẫn thấy caro in vào quầng | Trừ nền bằng một màu xám cố định | Lấy màu theo độ bão hoà, hoặc vẽ lại trên nền đơn sắc |
| 2026-10-31 | Mechanic | Súng lúc đứng yên/đi sai hướng; lúc bắn thành khẩu khác | Anchor sinh tự động: cỡ đổi liên tục (`s` 0,11–0,29), `sx` làm méo, nhiều frame `fallback` | Bảng từng frame trong `Tools/build-mechanic-gun.py`: `s = 0.23`, `sx = ±1`, không còn `fallback` |
| 2026-10-31 | Mechanic | Chuôi súng vẫn đè lên nắm tay dù đã có lớp nắm tay | Hình ellipse nhỏ (bán kính 9) không phủ hết nắm tay, thành viền chuôi quanh tay | Bán kính 12,5: lớp này chỉ lặp lại pixel của thân, chỗ `bare` trống thì chuôi vẫn lộ |
| 2026-10-31 | Mechanic | Sheet `mechanic-bare` `attack3` (và tay trước ở `attack1` hướng lên) không có bàn tay rõ, chỉ là khúc tay hình ống, còn vệt nòng súng | Sheet `bare` được xoá súng từ sheet có súng nên giữ lại hình khẩu pháo lớn | Chưa vẽ lại; đặt súng theo vị trí chuôi vẽ sẵn, nòng đè lên khúc ống. Muốn đẹp hơn phải vẽ lại sheet |
| 2026-10-31 | Mechanic | Tay cầm súng ở `mechanic-bare` khác sheet có súng: `walk` hướng trái và hướng lên, sheet có súng vung súng theo một tay ra sau rồi ra trước, còn ở `bare` cánh tay trơn (tay cầm súng) không vung như vậy | Hai sheet vẽ riêng | Anchor lấy theo nắm tay ở `bare`, súng luôn ở tay phải. Các frame này (`walk` trái 0, 1, 3; `walk` lên 0, 1, 3) không khớp hướng nòng với sheet có súng. Muốn khớp phải vẽ lại sheet |
| 2026-10-31 | Mọi class | Ở +15, sao hình bông hoa loé trắng phủ lên vũ khí, nhất là súng và dao | `HeldEnhanceFx` vẽ sao ngay trên chỗ sáng nhất của art, ở lớp trên cùng, cường độ 1,7 vượt ngưỡng bloom; 5 sao cỡ 5 đơn vị và không co theo vũ khí nhỏ | Sao đặt lệch sang cạnh lưỡi và vẽ sau art (dùng material của quầng, cường độ 1,2), độ đục tối đa 0,6, ít trắng hơn, co theo cỡ art; +15 còn 3 sao cỡ 4. Thứ tự vẽ và vị trí áp dụng cho cả +11 và +13 để +13 không sáng hơn +15 |

## Khi đổi vũ khí

Mỗi loại kiếm có điểm cầm riêng và anchor dùng chung, nên art mới phải đặt `gripX`, `gripY` đúng tâm chuôi.

**Những chỗ hình KHÔNG đổi theo đồ đang mặc:**

| Chỗ | Hiện trạng | Hậu quả |
|---|---|---|
| Frame `fallback` (`melee4` hướng phải/trái, frame 3) | Dùng frame vẽ sẵn cầm kiếm | Khoảnh khắc đó kiếm không theo đồ |
| Khiên lúc chém (`melee1-4`, `inferno`) | Anchor khiên ẩn, khiên vẽ sẵn trong sheet `bare/` | Đổi khiên hay tháo khiên thì lúc chém vẫn là khiên cũ |
| Bóng Shadow Clone | Vẽ từ sheet `assassin/` nhuộm tối | Không theo dao đang cầm; `walk.png` không có dao nên bóng mất dao khi đi |

**Chưa kiểm tra:**
- Mới xem kiếm `iron` loại thường. Các loại khác có `gripX` 47–69 và `gripY` 217–222, nên chuôi có thể lệch vài pixel; cần xem lại cả bản cường hoá vì nó dùng `enhancedGrip` riêng.
- Lớp nắm tay chỉ mới có cho Warrior (đứng yên và đi hướng trái/phải) và Mechanic. Đi tay không thì lớp này vẫn vẽ nhưng trùng với thân nên không lộ.

## Việc còn tồn

- Combo chém hướng trái (`melee1-4`) vẫn lật ảnh hướng phải nên đổi tay. Muốn đúng phải vẽ riêng 4 sheet hướng trái.
- Warrior: hướng lên/xuống và lúc chém chưa có lớp nắm tay.
- Mechanic: `attack2`, `death` không có sheet `bare` nên vẫn dùng súng vẽ sẵn, không theo đồ. `attack3`, `dash`, `hurt` hướng trái lật ảnh hướng phải nên súng đổi tay.
- Mechanic: súng vẽ sẵn chĩa thẳng vào màn hình (`attack1` và `attack3` hướng xuống, `attack3` hướng lên) không vẽ lại được bằng art nhìn ngang, nên đặt súng chĩa ngang.
- Wizard, Assassin, Cleric chưa soát theo các quy tắc trên.

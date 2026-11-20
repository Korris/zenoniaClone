# Quy tắc UI: nút, tab, pill (làm như HTML/CSS)

**Trước mọi việc làm hoặc sửa UI (HUD, túi đồ, menu), kể cả khi giao agent: đọc hết file này.** Ghi rõ yêu cầu này trong brief.

Người dùng, 2026-11-19, khi xem túi đồ RO Soft: các nút All, Weapon dính sát đáy, icon cách chữ quá xa, không giống `03-bag.png`; nút "Common" (pill độ hiếm) xấu. Yêu cầu: "làm như html ấy padding đầy đủ rồi border-radius".

## 1. Mỗi nút là một "box" như CSS

Mỗi nút, tab, pill, ô có đủ các thuộc tính sau và ghi thành hằng số, không đặt tay từng chỗ:

| Thuộc tính | Unity uGUI |
|---|---|
| `padding` (top, right, bottom, left) | `HorizontalLayoutGroup` / `VerticalLayoutGroup.padding`, hoặc inset của RectTransform con |
| `gap` giữa icon và chữ | `spacing` của LayoutGroup |
| `border-radius` | sprite 9-slice (`Sprite.border`) có góc bo; mọi cỡ nút dùng cùng bán kính |
| `border` (viền) + `background` | lớp viền + lớp nền trong sprite, hoặc 2 Image chồng |
| căn giữa | `childAlignment = MiddleCenter`; chữ `TextAlignmentOptions.Center` (cả dọc lẫn ngang) |
| `min-height` / `min-width` | `LayoutElement.minHeight/minWidth` |
| co theo nội dung | `ContentSizeFitter` (pill độ hiếm, nhãn) |

Số mặc định (đơn vị tham chiếu 1280x720, scale theo `CanvasScaler`):
- Tab có icon trên chữ (All, Weapon...): padding 8 trên / 8 dưới / 6 trái-phải, gap icon-chữ 2-4, icon 28-32, chữ 13-14. Khối icon+chữ căn giữa dọc trong tab, khoảng trống trên = dưới.
- Nút chữ (Equip, Enhance, Discard, Stats): cao ≥ 44, padding 10 / 18, gap icon-chữ 8, bo góc 12.
- Pill (độ hiếm, Lv, nhãn NPC): cao 22-26, padding 3 / 12, bo góc = nửa chiều cao (hình viên thuốc), co theo chữ.
- Ô đồ: vuông, bo góc 8, gap giữa ô 6, số lượng góc dưới phải cách viền 3.

## 2. Đo trước khi báo xong

- Chụp nút trong game, phóng to, so với mockup cùng cỡ. Khoảng cách trên và dưới nội dung phải bằng nhau (lệch ≤ 2 px); icon-chữ phải sát như mockup.
- Không có chữ hay icon nào chạm viền. Không có nút nào cao hơn hay thấp hơn nút cùng hàng.
- Chữ dài nhất (tiếng Anh) phải vừa: thử "Consumable", "Accessory", "Material".

## 3. Hình nút: ChatGPT hay code

- Ưu tiên: cắt khung nút/tab/pill từ ChatGPT thành sprite **9-slice** (góc bo nằm trong vùng border), rồi bố cục bằng LayoutGroup như mục 1. Không nướng chữ hay icon vào hình nút.
- Được phép làm nút bằng code nếu đạt chất lượng như HTML/CSS: bo góc mượt (khử răng cưa), viền 1-2 px màu nâu đậm mềm, nền gradient dọc nhẹ, bóng đổ mềm 2-3 px, trạng thái normal / hover / pressed / selected / disabled. Không được ra hình chữ nhật phẳng hay góc vuông răng cưa (lần trước người dùng chê "nút xấu quá" là vì thế).
- Màu theo tông RO Soft Fantasy (`docs/sprite-rules.md` điều 0) và lấy mẫu màu từ mockup (`docs/reference/ui-style-ro-soft-fantasy/`).

## Sổ lỗi

- 2026-11-19 túi đồ RO Soft: tab All/Weapon chữ dính đáy, icon cách chữ xa; pill "Common" xấu. Nguyên nhân: đặt vị trí tay, không có padding/gap/căn giữa dọc. Sửa: bố cục theo mục 1.
- 2026-11-19 túi đồ RO Soft (đã sửa): tab, nút, pill dựng lại bằng `InventoryRoBoxes.cs` (VerticalLayoutGroup / HorizontalLayoutGroup + ContentSizeFitter, hằng số padding/gap ở đầu file). Thêm 2 lỗi gặp khi làm: (1) icon tab cắt nguyên ô ChatGPT nên glyph chỉ chiếm ~50 % ô, nhìn bé và lệch: cắt sát glyph (`glyph_inset` của `Tools/cut-ui-paper-sheet.py` giờ tự cắt vuông sát glyph + 2 px); (2) pill vẽ từ art viên thuốc cao 104 px bị bo góc sai khi thu nhỏ: đặt `pixelsPerUnitMultiplier = chiều cao art / (2 x chiều cao pill)` để cả chiều cao art khớp pill, hai đầu luôn là nửa hình tròn; chữ dài ("Unequip", "Consumable") thì giảm cỡ chữ cho vừa, không để tràn sang nút bên.
- 2026-11-19 HUD RO Soft: "2 dấu gạch ở 2 đầu mấy cái nút" (pill tên map, "Offline", pill tên NPC). Hai nguyên nhân: (1) `flatten_edges` của `Tools/cut-ui-paper-sheet.py` làm phẳng cột ngoài cùng theo cả chiều cao ở mảnh 9-slice chỉ kéo ngang (border trên = dưới = 0), biến viền mờ alpha 1-30 thành vạch dọc 1 px ngoài hai đầu pill. Sửa tool: chỉ làm phẳng trục mà 9-slice thật sự kéo (`(b or t)` cho cột, `(l or r)` cho hàng); cắt lại mọi sheet `ro-*` (17 mảnh đổi: pill, thanh, `bag-label`; mảnh khác giống hệt). (2) Pill tên NPC vẽ IMGUI 3 khúc ở toạ độ lẻ, khe 1 px giữa khúc giữa và đầu phải lộ nền: làm tròn pixel và vẽ khúc giữa lấn 1 px vào hai đầu (`NameTags.RoSoft.cs`). Texture UI đã đúng: Clamp, không mipmap, viền trong suốt ≥ 3 px. Kiểm: phóng 4x ảnh chụp, không còn vạch.
- 2026-11-19 HUD RO Soft: chữ IMGUI (tên NPC, tên nhân vật) vẽ đè lên khung chat mở, thẻ bounty, banner Wanted vì IMGUI luôn trên uGUI. Sửa: `NameTags.Occlude(rt)` đăng ký các khung RO; thẻ tên nào chạm khung đang hiện thì bỏ qua.
- 2026-11-19 pill nhỏ (badge giây, nhãn phím) dùng `hud-lv-badge` cao 44 px: đặt `pixelsPerUnitMultiplier = 44 / chiều cao pill` để hai đầu tròn; badge tròn `hud-count-badge` không kéo dài được (preserveAspect) nên chữ "10s" tràn ra ngoài.
- 2026-11-20 cửa sổ Pet RO Soft (`PetRoWindow*.cs`): (1) nhãn + số chỉ số đè nhau ("Damage+6.5%", "Atk Spd3%") vì ô số đặt cố định từ x 80 còn nhãn dài hơn: cỡ chữ số tự giảm theo chỗ còn lại sau nhãn (`178 - 35 - nhãn - 8`); (2) mũi tên dropdown xoay 180 độ nhảy ra ngoài hộp vì pivot góc trên trái: đặt pivot giữa trước khi xoay; (3) `bag-arrow-down` là tam giác đỏ, không hợp dropdown: dùng `hud-chevron-up` tô màu mực, xoay; (4) hộp trắng lồng trong panel (`bag-pill-white`) viền xanh quá sắc so với mockup: alpha 0,8 (`SoftBox`).
- 2026-11-20 Gacha RO Soft: (1) dòng chữ "Permanent event" bị quầng sáng sân khấu che khi kéo sân khấu lên: Image uGUI vẽ theo thứ tự tạo, phần tử nằm trên phải tạo SAU (tạo pill thời gian sau `Stage`). (2) Toast tóm tắt lượt quay của game đè nút Pull again / OK: khi màn kết quả hiện, đưa cửa sổ lên trên toast (thẻ kết quả đã nói cùng nội dung), đóng kết quả thì trả toast lên trên (để thấy lời từ chối như "Needs 150 Pet Crystals"). (3) Nút bấm bằng code test: `RoGachaWindow.TestClick` raycast ở tâm nút, báo khi tap bị phần tử khác che.
- 2026-11-20 HUD RO Soft: "vòng tròn chứa avatar ... bị nhìn xuyên qua map" và "mini map cũng đang bị". Nguyên nhân: Mask tròn (`hud-portrait-mask`, `hud-minimap-mask`) đặt lùi vào trong khung (inset 6 / 9) trong khi đĩa của mask đã sẵn bằng lỗ của vòng (ChatGPT cắt cùng cỡ) nên hở một dải 5-6 px giữa đĩa và vòng, lộ map; minimap thêm lỗi theo nhân vật ra mép map thì nửa vòng là nền trống. Sửa: mask phủ đúng khung như ảnh vòng (cùng sprite, preserveAspect) để mép đĩa nằm dưới viền vòng (`RoHudPortraits.Frame`, `Minimap.RoRound.cs`); nền chân dung là gradient tròn trời nhạt đục (`RoundBackdrop.cs`); khung cắt bán thân tự vừa đầu (vai ở đáy, tóc chạm mép trên; mũ cao như mũ phù thuỷ, tai thỏ thì thu nhỏ để không bị cắt, `RoHudPortraitHeadTop.cs`); minimap dừng ở mép map để map luôn phủ kín vòng ở mọi mức zoom (chấm nhân vật lệch tâm). Thẻ tên IMGUI không vẽ đè chân dung / minimap (`NameTags.Occlude`). Kiểm: phóng 4x `docs/reference/ro-soft-hud/portrait-fill-*.png`, `minimap-fill-*.png`. Quy tắc: mask tròn trong vòng ChatGPT phải cùng khung với ảnh vòng, không inset.
- 2026-11-20 nhãn quái RO Soft: "sao bạn bỏ level, tên kế ống máu của quái vật rồi". Nguyên nhân: thanh máu + "Lv.N Tên" dưới chân (làm 17/11) chỉ có trong `Monster.PaperOverheadBar.cs`, chạy khi `UiScreens.UseNew(UiScreen.Boss)` (giao diện Adventure Paper); từ khi RO Soft thành mặc định (19/11) Paper tắt nên quái chỉ còn thanh đỏ trơn trên đầu lúc bị đánh, mất tên + level. Không commit nào xoá code: nhánh RO chưa bao giờ có nhãn. Sửa: `Monster.RoFootLabel.cs` (giao diện RO, quái thường; boss / Wanted vẫn chỉ ở thanh boss trên cùng): thanh máu `hud-name-hp-track` + `hud-bar-red`, tab `hud-lv-badge`, đều 9-slice viên thuốc (border = nửa cao art), "Lv.N" vàng #FDC04E + tên trắng Nunito Bold, padding tab 2,2 / 0,75 đơn vị thế giới, luôn hiện khi còn sống, dưới chân. Vẽ bằng sprite thế giới (trên quái / nhân vật, dưới FX), quái gần camera vẽ nhãn đè quái sau. `MonsterFootLabelCrowd.cs`: tab đè tab / thanh máu quái gần hơn thì ẩn tab (giữ thanh máu); nhãn chạm hàng HUD trên (chữ "Pet", "Quest" không có nền nên nhãn lộ qua) hoặc khung `NameTags.Occlude` thì ẩn hẳn. Đăng ký thêm vào `NameTags.Occlude`: thanh chat (đục 90 %), joystick (trong suốt), nút skill / Attack / Auto / Run / bình máu (chữ nút đè lên nhãn). Kiểm: phóng 4x `docs/reference/monster-label-ro/zoom4x-*.png`.

# Phase 02: Art ChatGPT (sheet cắt rời, mockup trạng thái thiếu, tool cắt)

## Context Links
- `CLAUDE.md` (tài khoản: Tay Plus, hết lượt thì Tan Pham; không dùng xaochai / Tieu Duy Phuong; thiết kế lấy từ ChatGPT)
- `docs/sprite-rules.md` mục "Quy tắc art lấy từ ChatGPT" (điều 1-7) + Sổ lỗi các dòng "UI Party", "UI Trade", "UI Chọn hero", "UI HUD"
- Chat đã chốt hướng: https://chatgpt.com/c/6afa7ea3-b060-83ec-86af-1fa45497cea5 (tài khoản Tay)
- Tool mẫu: `Tools/build-settings-ui-art.py` (key `#00FF00`, blob theo lưới), `Tools/build-character-flow-ui-parts.py` (key `#FF00FF`, blob đọc theo hàng, đặt tên theo bảng NAMES, chép `.meta`)

## Overview
- Ưu tiên: P1. Effort: L, chạy suốt dự án (nút cổ chai là lượt vẽ ChatGPT). Status: pending.
- Mỗi màn: (a) xin mockup trạng thái còn thiếu trước khi code, (b) xin sheet mảnh cắt rời trên nền một màu, (c) cắt bằng 1 tool chung, (d) xem trên nền tối + sáng ở cỡ game.

## Key Insights
- Quy tắc nền: không bao giờ xin nền trong suốt. `#FF00FF` cho sheet có xanh lá; `#00FF00` cho sheet có tím / hồng. **Một sheet không được có cả tím và xanh lá** -> chia sheet theo màu (ví dụ độ hiếm tím ở sheet nền xanh lá, độ hiếm lục ở sheet nền magenta).
- Ghi trong prompt: "vẽ mới hoàn toàn từ đầu", "hình đặc và đục", "các mảnh cách nhau >= 40 px, không chạm mép", "không vẽ chữ trong nút / ruy băng" (chữ do code vẽ để dịch và đổi số), "không vẽ bóng đổ ra nền". Gửi kèm mockup của màn đó làm ảnh mẫu style.
- Ảnh ChatGPT tối đa 1536x1024: nền toàn cảnh (2845x1600 mới nét ở 1440p) sẽ bị phóng; chấp nhận vì nền nằm sau nội dung, có thể làm mờ / tối nhẹ trong code. Mảnh nhỏ: vẽ to hơn cỡ đích rồi thu nhỏ.
- Fill thanh vẽ một lần màu trắng / xám có độ sáng, tô màu trong code (HP đỏ, MP xanh, EXP lục, SP xanh, boss đỏ, niệm chú tím, gần lên cấp vàng, max tím): DRY, khỏi vẽ 8 thanh.
- Chân dung boss, icon vật phẩm, icon skill, art pet, mặt hero: dùng art game có sẵn (`Sprites/monsters`, `LootSprites`, skill icons, `PetArt`, `ui/faces`), không xin mới.
- Lấy ảnh bằng tải xuống (`fetch(img.src)` + `<a download>`), không chụp màn hình (điều 7).

## Requirements
Đích: `Assets/ArtLibrary/ui/adventure-paper/<sheet>/` (ảnh gốc ChatGPT + `prompt.txt` + link chat), xuất `Assets/Resources/Sprites/ui/paper/<nhóm>/<mảnh>.png` (+ `.meta` chép từ `ui/win-btn-gold.png.meta` với guid mới, như tool character-flow). Cỡ ghi theo đơn vị canvas (u) / px gốc.

### Mockup trạng thái thiếu (xin TRƯỚC khi code màn đó, cùng chat đã chốt)
| Màn | Trạng thái cần vẽ thêm |
|---|---|
| Thùng đồ | chọn món đang mặc (Unequip + Enhance preview: +0 -> +1, % thành công, đá cần / có, nút Enhance); bảng Stats; dạng lưới 50 ô (giả định 1); chọn bình / đá (Use / Discard); xác nhận Discard; đang kéo thẻ lên bệ |
| Skill | chế độ gán (ô đai nhấp nháy chờ chọn); xác nhận Respec; tab Advanced; skill max rank; không đủ SP |
| Pet | tab Collection, Growth, Capture; ổ rơm trống; chọn pet để đặt vào ổ |
| Gacha | kết quả x1 (trứng nở), kết quả x10 (lưới 10), thiếu coin, xác nhận đổi Shards |
| Chat | PC mở bản nhỏ không che hotbar; mobile mở cỡ gọn |
| Wanted | áp phích trạng thái Respawning và Claimed; bảng truy nã khi offline / chưa có dữ liệu (nếu giữ offline) |
| Thanh boss + ảnh tổng | vẽ lại bỏ "Hunters" (đang làm, người dùng đã chốt) |

### Sheet mảnh cắt (nền, cỡ chính)
| Sheet | Nền | Mảnh (cỡ u / px gốc) | Ra `paper/` |
|---|---|---|---|
| K1 khung | `#FF00FF` | giấy khung gỗ 9-slice (góc 40u / 100px, ảnh 512x512); giấy ghim đinh (512x512); bảng gỗ lớn (512x512); bảng tối slate (256x256); thẻ giấy viền chấm (256x256); biển tiêu đề gỗ + nguyệt quế (300x70u / 760x180); ruy băng đỏ (240x50u / 600x128); dải mục da "Compare with Equipped" (9-slice ngang 512x80); nút đóng X đỏ tròn (40u / 100px) | `kit/` |
| K2 nút + tab | `#00FF00` | nút vàng / xanh / đỏ x thường + nhấn (9-slice ngang 512x128); nút xám không dùng được; tab da ngang active / inactive (300x80); ván menu dọc active vàng / inactive gỗ (400x110); mũi tên tròn trái / phải (100px); dropdown; chip lọc active / inactive; chấm trang vàng / tối; nút "+" nhỏ | `kit/` |
| K3a ô + thẻ (xám, xanh dương, vàng, lục) | `#FF00FF` | ô vật phẩm 4 độ hiếm (64u / 160px); thẻ vật phẩm đứng 4 độ hiếm (88x122u / 224x312); khung chọn phát sáng vàng; lớp khoá; ô trống; sao đầy / rỗng (32px); huy chương skill: đã học / đang chọn / chưa học / khoá (63u / 160px) | `kit/` |
| K3b ô + thẻ tím | `#00FF00` | ô + thẻ độ hiếm tím (Anh hùng); badge góc chấm than vàng, ổ khoá (badge tích lục nằm ở K3a) | `kit/` |
| K4 badge + thanh + tiền | `#FF00FF` (không tím) | badge tròn đỏ trống (số do code), badge "!", chấm đỏ; ruy băng nhãn trống đỏ / cam / tối / lục (New, Hot, Lv, Đã gán); khung thanh lớn + nhỏ có nắp vàng (9-slice ngang 420x46); fill trắng có khối (256x32); icon tim HP, giọt MP; icon vàng, đá cường hoá (đã có, dùng lại), Gacha Coin, Pet Crystal, Pet Food, bóng, bóng hiếm, Shard (48px) | `kit/` |
| H1 trạng thái | `#00FF00` | huy hiệu khiên + nguyệt quế ôm chân dung (100u / 256px) bản thường, bản viền đỏ máu thấp, lớp khiên xanh; thẻ Lv da; ô buff (24u / 64px); ruy băng "LEVEL UP!" + nguyệt quế (no text? ruy băng trống); dải toast giấy | `hud/` |
| H2 nút vào | `#00FF00` | thanh gỗ treo + thẻ Bag, thẻ Skill (thường / nhấn, 60x80u / 150x200); túi More đóng / mở (80u / 200px); 4 cánh quạt giấy + icon Pet, Gacha (tím: nên tách sang sheet này nền lục, đúng), Quests, Settings; nút chat gỗ tròn (thường / nhấn, 56u / 140px); nắp phím tối (B, T...: chữ code) | `hud/` |
| H3 cảm ứng + hotbar | `#FF00FF` | đế joystick gỗ đinh vàng (113u / 288px) + núm thép; nút đánh đỏ (92u / 256px) thường / nhấn phát sáng; khung ô skill (63u / 160px); đai da vòng cung 3 ô; khung bình (47u / 128px); đai da hotbar ngang 9-slice (1024x160); biển "Tab 1/3" có mũi tên lên / xuống | `hud/` |
| B1 boss | `#00FF00` | huy chương boss vàng (100u / 256px) + bản xám (DEFEATED); ruy băng WANTED; khung thanh boss lớn mũi nhọn 9-slice; vạch RAGE; viền lửa ENRAGED (lớp phủ) + nhãn ENRAGED lửa; khung thanh niệm chú tím + huy chương nhỏ; biển DEFEATED + nguyệt quế; khung thanh nhỏ trên đầu (thường / elite / boss) | `boss/` |
| A1 thùng đồ | `#FF00FF` | nền phòng vũ khí KHÔNG có UI (1536x1024); bệ tròn trung tâm; bệ trang bị thường / đang chọn (100x75u / 256x192); kệ gỗ băng chuyền 9-slice ngang; biển "Bag 50/50" túi; biển đe "Enhance Equipped"; nhãn bệ; icon lọc 10 loại (All, Weapon, Shield, Armor, Gloves, Boots, Necklace, Pet, Consumable, Material); icon chỉ số (ATK, DEF, M.ATK, M.DEF, CRIT, HP, MP, SPD); tooltip "Drag to Equip" trống | `bag/` |
| S1 skill | `#00FF00` | nền võ đường KHÔNG UI; kệ gỗ 2 tầng (9-slice ngang); bệ tượng vàng / bạc / đá (100x130u / 256x320); biển điều kiện có mũi tên; sách Skill Magazine; đai magazine 9-slice; ô số có thẻ số (trống "+"); ván Basic / Advanced; nút Respec tím | `skill/` |
| P1 pet | `#FF00FF` (tím tách P1b `#00FF00`) | nền trại pet KHÔNG UI; ổ rơm lớn (176x75u / 448x192) + nhỏ (320x136) thường / chọn; biển vai trò Combat / Support / Stat + ruy băng %; biển Team Bonus nguyệt quế; thẻ Bond thường / ACTIVE; biển "Owned Pets"; khung Level Up / Star Up. P1b: nhãn độ hiếm N, R, SR, SSR, UR (UR tím), nút Gacha tím | `pet/` |
| G1 gacha | `#00FF00` | nền quầy chợ lều KHÔNG UI; đài trứng (350x175u / 880x440); trứng sáng; cờ Featured trống; khung bảo hiểm SSR+ / UR; biển đổi Shards; nút Pull x1 xanh, Pull x10 tím, Buy coin vàng; cờ dọc Rates / History; Lumi bán thân (tóc hồng: KHÔNG dùng nền magenta) + bong bóng thoại | `gacha/` |
| C1 chat | `#FF00FF` | bảng gỗ chat 9-slice; ruy băng "Chat" + bút lông; ruy băng kênh dọc trắng (tô màu code) + icon All / World / Map / Party / Whisper; chip lọc kênh; ô nhập giấy; nút mặt cười; dropdown kênh; menu ngữ cảnh gỗ tối; viên "đang gõ" | `chat/` |
| W1 wanted | `#00FF00` | nền quảng trường + bảng gỗ lớn KHÔNG UI; áp phích WANTED giấy; biển ALIVE / Respawns; dấu "NOT CLAIMED YET" + "CLAIMED"; thẻ lịch + tag Yesterday / Today / Tomorrow; dải chiêu + 4 sơ đồ né (đường thẳng, hình quạt, nhiều vòng, vòng có hành lang an toàn); Guard Rook toàn thân; thẻ Wanted HUD treo (64x48u / 320x240); banner đăng nhập, banner hồi sinh xanh, banner vào map + mũi tên, ruy băng WARNING xích, bảng BOUNTY CLAIMED nguyệt quế, dải "Already claimed"; đầu lâu minimap đỏ / xám (24u / 64px) | `wanted/` |

## Architecture
Một tool chung, mỗi sheet một file cấu hình nhỏ (DRY, thay cho mỗi sheet một script):
```
Tools/cut-ui-paper-sheet.py <sheet-config.json> check OUT.png | write
  key (#00FF00 | #FF00FF) -> alpha + khử viền màu nền (hàm key_green / keyed() hiện có)
  -> blob (đóng lỗ, bỏ dải 6 px mép) -> đọc theo hàng trái -> phải -> tên theo NAMES
  -> cắt sát + lề 4 px -> thu về cỡ đích -> ghi png + .meta (guid mới) -> ảnh xem trước nền tối + sáng
Tools/ui-paper-sheets/<sheet>.json : nguồn, key, NAMES theo hàng, cỡ đích, border 9-slice, giữ lỗ bên trong (key_all) hay chỉ phần nối mép
```
Border 9-slice ghi cùng json và xuất sang bảng border của `PaperArt` (một nguồn sự thật).

## Related Code Files
- Tạo: `Tools/cut-ui-paper-sheet.py`, `Tools/ui-paper-sheets/*.json`
- Tạo art: `Assets/ArtLibrary/ui/adventure-paper/**`, `Assets/Resources/Sprites/ui/paper/**`
- Không sửa art cũ (`ui/hud-*`, `ui/win-*` còn dùng cho cửa sổ chưa làm lại; xoá ở phase 13 cái nào hết dùng).

## Implementation Steps
1. Kiểm tra đúng tài khoản Tay trên ChatGPT trước khi gửi; mở chat đã chốt.
2. Thứ tự xin: K1-K4 -> H1, B1 -> mockup thiếu của Thùng đồ + A1 -> Skill thiếu + S1 -> H2, H3 -> C1 -> Pet 3 tab + P1 -> Gacha kết quả + G1 -> W1.
3. Mỗi sheet: prompt theo mẫu ở Key Insights, đính kèm mockup màn đó + style board; tải ảnh về `ArtLibrary/ui/adventure-paper/<sheet>/`, đặt tên có số lượt (`-chatgpt-1.png`), lưu prompt + link chat.
4. Viết `cut-ui-paper-sheet.py` từ 2 tool mẫu; chạy `check` -> tự xem ảnh xem trước phóng to 2x trên nền tối + sáng, soi viền màu nền, lỗ thủng, caro.
5. `write`; ghi link chat từng sheet vào `docs/pending-tasks.md` (người điều phối ghi, không phải agent khác đang sửa mục đó).
6. Mỗi lỗi cắt / art mới: thêm dòng vào "Sổ lỗi" `docs/sprite-rules.md`.

## Todo List
- [ ] Mockup thiếu: Thùng đồ, Skill, Pet (3 tab), Gacha (x1, x10), Chat (bản nhỏ), Wanted (áp phích 2 trạng thái)
  - [x] 16/11 Thùng đồ: `bag-state-equipped-item-enhance-preview.png`, `bag-state-grid-50-slots-consumable-selected.png`, `bag-states-stats-panel-discard-confirm-drag-card.png`
  - [x] 16/11 Skill: `skill-states-assign-respec-advanced-max-rank-no-sp.png` (5 trạng thái trên 1 bảng)
  - [x] 16/11 `wanted-fight-full-mobile.png` bản bỏ Hunters đã tải
  - [x] 16/11 Chat `chat-compact-open-states-pc-mobile-typing.png`; Wanted `wanted-board-poster-states-respawning-claimed-reward-panel.png`; Pet `pet-camp-collection-growth-capture-tabs.png` + `pet-camp-team-states-empty-nest-pet-selected.png`; Gacha `pet-gacha-result-states-x1-x10-no-coins-exchange.png`
  - [ ] Wanted khi offline / chưa có dữ liệu (chưa xin: chưa chốt có giữ offline không)
- [x] Sheet K1, K2, K3a, K3b, K4 (16/11, 72 mảnh trong `paper/kit/`)
- [ ] Sheet H1, H2, H3, B1
  - [x] 16/11 H1 (9 mảnh `paper/hud/`), B1 (16 mảnh `paper/boss/`)
  - [x] 16/11 H2 (21 mảnh), H3 (14 mảnh) -> `paper/hud/`
- [ ] Sheet A1, S1, C1, P1, P1b, G1, W1
  - [x] 16/11 A1: nền + 11 mảnh + 18 icon (`paper/bag/`)
  - [x] 16/11 C1 (22 + 2 nút đổi cỡ `paper/chat/`), S1 (nền + 9 + 19 `paper/skill/`), W1 (nền + 18 + 22 `paper/wanted/`), P1 (nền + 20) + P1b (14) `paper/pet/`, bóng bắt pet mới thay `kit/icon-capture-ball` + `hud/icon-capture-ball-rare`
  - [x] 16/11 G1: nền + G1-A (8) + G1-B (17) -> `paper/gacha/`
- [x] Tool cắt chung + json từng sheet (`Tools/cut-ui-paper-sheet.py`, `Tools/ui-paper-sheets/*.json`, border 9-slice gom ở `Tools/ui-paper-sheets/paper-borders.json`; không ghi `.meta`, để Unity tự sinh)
- [x] Ảnh xem trước đã tự xem từng sheet đã cắt (lưu ở `docs/reference/ui-redesign-261116/build/art/<sheet>-contact.png`); từ C1 trở đi lưu ở `docs/reference/ui-redesign-261116/art-contact/`

## Success Criteria
- Mỗi mảnh: không viền màu nền, không caro, không lỗ, đúng cỡ đích; 9-slice kéo dãn không méo góc (thấy ở `--kit-sheet`).
- Mọi mảnh trong bảng trên có file ở `Sprites/ui/paper/**`, hoặc ghi rõ vì sao bỏ.

## Risk Assessment
| Rủi ro | Khả năng x Tác động | Giảm thiểu |
|---|---|---|
| Hết lượt vẽ, chờ nhiều ngày | Cao x Cao | Xin theo thứ tự ưu tiên; dùng Tan Pham khi Tay hết (chat mới, kiểm tra tên tài khoản); code dùng fallback màu phẳng để không bị chặn |
| Style lệch giữa các sheet | Trung bình x Cao | Cùng một chat, luôn kèm style board + mockup; so sheet mới cạnh mảnh cũ trước khi cắt |
| Màu nền ăn vào art (tím trên magenta, lục trên lục) | Trung bình x Trung bình | Quy tắc chia sheet theo màu; chọn chế độ key_all hay chỉ nền nối mép theo từng sheet |
| ChatGPT vẽ chữ vào nút | Cao x Thấp | Prompt cấm chữ; nếu vẫn có, xin lại (không tự xoá chữ bằng tay) |

## Security Considerations
- Chỉ dùng các tài khoản được phép. Không đưa thông tin cá nhân vào prompt.

## Next Steps
- Mỗi sheet xong mở khoá phase tương ứng. Danh sách này là tối thiểu: phase nào thấy thiếu mảnh thì xin bổ sung, không tự vẽ.

## Câu hỏi mở
- Có muốn gộp mảnh nhỏ thành atlas ngay không? Đề xuất: chưa, chỉ khi `--perf` cho thấy draw call cao.

# Dọn asset không dùng (20/11/2026)

Người dùng: "xoa nhung file assets đang ko dùng tới trong repo này". Làm thẳng trên `ro-fantasy-heroes`.
Commit xoá: `afbf6e19`. Khôi phục: `git checkout afbf6e19~1 -- <đường dẫn>` (vd `git checkout afbf6e19~1 -- Assets/ArtLibrary/hd/wizard`).

## Cách xét

- Không có scene / prefab: game nạp mọi thứ bằng `Resources.Load` (chuỗi), ProjectSettings không trỏ GUID asset nào. `ArtLibrary` chỉ tool đọc, `build-from-copy.ps1` không chép nó vào bản build.
- Quét mọi thư mục `ArtLibrary` (sâu 1-3) theo đường dẫn / `os.path.join("a","b")` / tên lá trong `Tools`, `Assets/Scripts`, `Assets/Editor`, `Tests`.
- `docs/reference`: mỗi file kiểm tên (và tên không đuôi) trong mọi file chữ của repo (docs, plans, code), cả mẫu wildcard (`*-solids-overlay.png`, `<map>-...`, `{a,b}`); chỉ xoá file không ở đâu nhắc và thuộc biến thể đã bỏ.
- `Resources`: mọi thư mục đều có code / data json trỏ tới (đường dẫn ghép từ class / anim / tier) và không còn file tên old / backup / rejected, nên KHÔNG xoá gì trong Resources.
- Giữ nguyên theo yêu cầu: art / data Warrior đánh, `hero-layered/ro-fantasy`, `warrior/fx/attack`, `View25D`, `dev-start.json`, Mire Hydra Queen v2, `monsters/ro-soft` (+ `old/`, `ref/`), map hiện tại, nhóm C của đợt 16/11.

## Đã xoá (956 file trong git kể cả `.meta`, 499,5 MB)

| Nhóm | File | MB | Lý do |
|---|---|---|---|
| `Assets/ArtLibrary/hd/` | 906 | 410,6 | Art bản web nhập 29-31/10 (`hero`, `wizard`, `mechanic`, `cleric`, `assassin`, `warrior-v2`, `monsters`, `npcs`, `props`, `tiles`, `style-anchor-hero-upload.png`, `walk-v2-prompt.txt`) và tranh map cũ trước bản RO Soft (`hd/map/{FrostHollow,Oakhaven,oakhaven-v6,Sunspire,amber-dunes,emberfen-marsh,frostfang-den,sunken-reliquary,sunspire-oasis,thatch-village,portal-light}`, `village_reference.png`, `frost-hollow/frost-hollow-ground-gates-removed-attempt.png`). Chỉ 5 tool nhắc `hd`, đều chỉ đọc `hd/map/frost-hollow/large` và `hd/map/willowbrook-meadows`. |
| `docs/reference/` | 46 | 78,1 | Không doc / báo cáo / code nào nhắc: ảnh các biến thể Wizard đã gỡ 16/11 (`wizard-astral-*`, `wizard-northwind-*` 10 file không ai nhắc, `wizard-battle-mage-*`, `wizard-reference-20261108-*`, `wizard-original-all-tiers`, `wizard-t9-book-grip-variants`, `wizard-ultimate-v8-*`, `wizard-height-fixed`, `wizard-hairstyles-nohood`), `hero-layered-farm/farm-0..11.png` (giữ `farm-hud.png`), `village-compare-*` (4). |
| Gốc repo | 4 | 10,7 | Ảnh ChatGPT thả ở gốc (`9670d96f-*.png`, `e941e24e-*.png`, `db978393-*.png`, `download.png`: bảng tier Wizard cũ, mẫu Mechanic cũ, mockup túi đồ cũ), không ai nhắc; CLAUDE.md cấm để ảnh ở gốc. |

Giữ trong `hd/map`: `frost-hollow/large/`, `frost-hollow-ground-original.png` (code `FrostHollowProps.cs` + `cut-frost-hollow-props.py` ghi toạ độ theo ảnh này), `frost-crop-A/B-edited-raw.png` (xem CHƯA CHẮC), `willowbrook-meadows/` (cả `large/`, `*-chatgpt.png`).

## Dung lượng

- Cây làm việc nhẹ đi ~500 MB. `ArtLibrary/hd` nằm trong `Assets` nên Unity có import: `Library/Artifacts` sẽ bớt thêm (chưa đo; Unity dọn artifact mồ côi khi refresh, chắc nhất là xoá `Library` để import lại).
- `.git` KHÔNG nhỏ đi (lịch sử vẫn giữ file); muốn giảm `.git` phải viết lại lịch sử (filter-repo) + force push, chưa làm.
- Bản build không đổi (không đụng Resources).

## Kiểm tra

- `dotnet test`: ZenoniaSkills 468/468, ZenoniaNet 455/455.
- Build từ bản sao `C:/temp/zu-copy`: `C:/temp/player-cleanup/Zenonia.exe`, 0 lỗi; build.log không có cảnh báo thiếu resource.
- Chụp offline (`--bot fight --dummies 3`, đã tự mở xem `C:/temp/cap-cleanup/sheet.jpg`): Warrior Willowbrook, Wizard Frost Hollow, Mechanic Amber Dunes, Assassin Oakhaven, Cleric Sunbreak. Map, quái, hero, HUD, minimap, vệt chém đủ; không sprite hồng / trống. Cleric Lv1 ở Sunbreak bị quái hạ (HP 0), không liên quan asset. Player.log không có missing / exception.

## CHƯA CHẮC (chưa xoá, chờ người dùng)

| Đường dẫn | MB | Ghi chú |
|---|---|---|
| `ArtLibrary/maps/ro-soft-originals~/` | 150 | Bản gốc trước khi bake tông RO; `bake-map-ro-soft-tone.py revert` và `build-sunken-reliquary-wide-arena.py` còn đọc. Unity bỏ qua thư mục `~` nên chỉ tốn ổ đĩa. Map đã vẽ lại 20/11 nên có thể không cần nữa. |
| `ArtLibrary/monsters/{meadow,frost,bramblewood,snowcrest,rootburrow,scorpion-gorge,sunbreak,sunspire-vault}` | ~135 | Sheet ChatGPT trước bản RO Soft; `build-meadow-monsters.py`, `cut-*` còn đọc một phần. 40 quái sẽ thiết kế lại theo bible, không dùng frame cũ làm mẫu. |
| `ArtLibrary/custom/hud`, `custom/weapon-overlays`, `custom/weapon-tier-icon-refs`, `custom/potions`, các `*-chibi.png` / `*-prompt.txt` ở gốc `custom/` | ~37 | Không tool nào đọc theo tên; có thể là ảnh gốc của sprite UI / icon cũ (giống nhóm C). |
| `ArtLibrary/be72963d-*.png` | 2,6 | Bảng trượng Wizard ChatGPT, không ai nhắc; có thể là nguồn trượng tier. |
| `ArtLibrary/anim/_pending`, `ArtLibrary/Sprites/idle.png`, `ninja-adventure/` | ~7 | Nhỏ; `ninja-adventure` kèm CREDITS (bản quyền). |
| `hd/map/frost-hollow/frost-crop-A/B-edited-raw.png` | 6 | Ảnh crop trung gian, không tool đọc. |
| `ArtLibrary/classes/warrior/outfits/set-t*/src/old` | ~32 | Không xét vì đang có agent vẽ lại Warrior. |
| `docs/reference` không ai nhắc còn lại: `cleric-*-board.png`, `chat-ui/`, `mechanic-sets-v3-t0-t9-*`, `warrior-*` (4), `wizard-outfit-d-*` (4), `world2-monsters-sunbreak.png`, `world2-map-bramblewood-forest-concept.png` | ~35 | Không ai nhắc theo tên nhưng là bộ đang dùng / Warrior / concept. |
| `plans/` | 708 | Ảnh báo cáo / visuals; ngoài phạm vi "assets". |

## Câu hỏi chưa giải quyết

- Xoá tiếp nhóm CHƯA CHẮC nào (nhất là `ro-soft-originals~` 150 MB và sheet quái cũ ~135 MB)?
- Có muốn viết lại lịch sử git để `.git` nhỏ thật không (phải force push, mọi máy clone lại)?

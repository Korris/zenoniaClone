# Phase 08: Pet Camp

## Context Links
- Mockup: `chatgpt/pet-final-mockup-pet-camp-team.png` (chỉ tab Team; Collection / Growth / Capture xin ở phase 02)
- Hiện tại: `ingame/pet-tab-{team,collection,growth,capture}.png`
- Code: `PetWindow.cs` (159: ví, 10 loài, tab, nút Gacha), `PetWindow.Team.cs`, `PetWindow.Growth.cs` (Collection + Growth), `PetWindow.Capture.cs`, `PetActions.cs`, `PetArt.cs`, `PetViewPrefs.cs`, `Rpg/Account/{AccountPets,PetTeam,PetGrowth,PetCapture}.cs`, `Rpg/PetRules.cs`, `Rpg/PetConfig.cs`, `docs/pet-system-design.md`
- Phase 01, 02 (P1, P1b, 3 mockup tab)

## Overview
- Ưu tiên: P2. Effort: L. Status: code xong 16/11 sau công tắc `--ui-new pet` (4 tab theo mockup, build + ảnh OK; chờ duyệt, review, commit, chạy online).
- Trại pet: 5 ổ rơm theo vai trò (Combat 100 %, Support 1-2 50 %, Stat 1-2 25 %), Team Bonus, 4 thẻ Bond (ACTIVE), bảng chi tiết pet (độ hiếm, sao, Lv, Passive, Skill, Growth, Level Up, Star Up), kệ pet sở hữu, menu ván gỗ Team / Collection / Growth / Capture / Gacha.

## Key Insights
- Dữ liệu tab Team đã có đủ (vai trò %, team bonus, bond + active, passive, skill pet, Lv / sao, chi phí level / star, ví tài khoản: vàng, Gacha Coin, Pet Crystal, Pet Food, bóng, bóng hiếm). Hành động qua `PetActions` (cùng luật offline / online).
- Collection / Growth / Capture: logic có, **chưa có mockup** -> không code view các tab này trước khi có ảnh; trong lúc chờ, tab đó mở view cũ (công tắc theo tab, không theo cả cửa sổ).
- Pet V2 (RO Pocket Saga thu gọn) đang làm dở, CHƯA commit, bởi agent pet-gacha: `PetWindow*.cs`, `PetConfig.cs` đang đổi (git status có `Rpg/PetConfig.cs`, `HeroController.Pet.cs`). Bắt đầu phase này khi phần đó đã commit, nếu không sẽ xung đột.
- Chọn pet trên kệ -> chạm ổ để đặt / đổi (luồng cũ "tap slot to take it out, another to move it"); thêm kéo thả bằng Drag dùng chung (PaperKit), không viết lại.

## Requirements
- Team: 5 ổ rơm (ổ giữa to), biển vai trò + %, pet đứng trên ổ (art `PetArt`), Lv + sao; Team Bonus 3 chỉ số; 4 thẻ Bond, thẻ đang có hiệu lực nổi + nhãn ACTIVE; bảng chi tiết bên phải; kệ "Owned Pets (n)" cuộn ngang + mũi tên.
- Thanh trên: ví 6 loại (chỉ loại có thật), Inventory (mở Bag), Close.
- Menu trái: Team / Collection / Growth / Capture (ván gỗ), Gacha (tím) mở quầy Lumi.
- Phím P bật tắt, Esc đóng.

## Architecture
```
AccountState (PetTeam, PetGrowth, AccountPets) ──> PetCampWindow
  ├─ .Nests (5 ổ + vai trò + bonus)   ├─ .Bonds   ├─ .DetailBoard   ├─ .OwnedShelf
  └─ .SideMenu (tab mới hoặc mở PetWindow cũ đúng tab khi tab chưa làm lại)
hành động ──> PetActions (có sẵn)
```

## Related Code Files
- Tạo: `PetCampWindow.cs`, `PetCampWindow.Nests.cs`, `PetCampWindow.Bonds.cs`, `PetCampWindow.DetailBoard.cs`, `PetCampWindow.OwnedShelf.cs`, sau khi có mockup: `PetCampWindow.Collection.cs`, `PetCampWindow.Growth.cs`, `PetCampWindow.Capture.cs`; `DebugCapture.UiPet.cs`
- Sửa: `DebugCapture.Pets.cs` (`--open-pets` qua UiScreens) - chỉ phase này
- Không sửa: `PetActions.cs`, `Rpg/Account/*`, `Rpg/PetRules.cs` (luật dùng chung server)
- Xoá (phase 13): `PetWindow*.cs` view cũ

## Implementation Steps
1. Chờ Pet V2 commit; đọc lại `docs/pet-system-design.md` + code mới.
2. Team: nền P1, ổ, biển vai trò, bonus, bond, bảng chi tiết, kệ.
3. Menu trái + công tắc theo tab.
4. Nhận 3 mockup tab -> làm Collection, Growth, Capture.
5. Cờ chụp: dùng `--open-pets [tab]`, `--pet-select`, `--give-pets`, `--pet-team`, `--pet-level`, `--pet-stars`, `--pet-food`, `--pet-crystals`, `--pet-balls`; mới `--pet-nest-demo empty` (ổ trống).
6. Build bản sao, chụp 4 tab x 1920x1080 / 1600x1200 / 3440x1440, online 1 lần (vai `pet-wear`, `pet-see`). Tự xem.

## Todo List
- [x] Chờ Pet V2 commit (3e10cca6)
- [x] Tab Team đầy đủ
- [x] Menu trái + công tắc (cả 4 tab đã có mockup nên công tắc theo cả màn, không cần theo tab)
- [x] Mockup + view Collection, Growth, Capture
- [x] Cờ chụp, ảnh, tự xem (16:9, 20:9, 4:3; công tắc tắt = cửa sổ cũ)
- [ ] Chạy online 1 lần (vai `pet-wear`, `pet-see`): CHƯA chạy
- [x] `dotnet test` 2 bộ (358 + 422 pass)

## Ghi chú sau khi làm (16/11)
- File thật: `PetCampWindow.cs` + `.TopBar` (biển, ví, menu ván), `.Nests`, `.Bonds`, `.DetailBoard`, `.OwnedShelf`, `.Actions` (chạm, phím, kéo thả), `.Collection`, `.Growth`, `.Capture`, `.Testing`; widget `PetCampNest.cs`, `PetCampCard.cs` (kèm `PetCampBadge`: nhãn độ hiếm P1b, phase 09 dùng lại được); `PetViewModel.cs` (phần dùng chung 2 giao diện: tên ô, dấu vân trạng thái, chữ bonus / luật / nguồn pet, `ToggleSlot`, `Convert`); `DebugCapture.UiPet.cs`.
- Sửa: `PetWindow.cs`, `PetWindow.Team.cs`, `PetWindow.Growth.cs` (gọi sang `PetViewModel`, hành vi cũ giữ nguyên), `DebugCapture.Pets.cs` (`--open-pets` đi qua `UiScreens`), `PaperArt.Pieces.cs` (khối tên mảnh `Pet*` + icon ví).
- Bật màn: `--ui-new pet`; duyệt xong đổi mặc định trong `UiScreens.useNew`. Phím P đóng do `PetCampWindow` tự đọc (`InputAdapter.WindowPressed`), vì cổng phím chặn P khi có modal; không sửa `InputAdapter` / `WindowsHost`.
- Cờ chụp mới: `--pet-nest-demo empty`, `--pet-own none|id,id`, `--pet-shards N`, `--pet-camp-select`, `--pet-camp-place`, `--pet-camp-nest N`, `--pet-camp-press ...`, `--pet-camp-drag N`, `--pet-camp-drag-hold N` (xem đầu `DebugCapture.UiPet.cs`).
- Nút mũi tên vòng cạnh Growth: làm thành "Place / Move / Cancel" (bắt đầu đặt pet đang xem vào ổ). Đặt pet đang ở ổ khác = chuyển ổ; ổ đích có pet thì hai con đổi chỗ (2-3 lệnh `PetEquip` nối tiếp qua `PetActions.Send`).
- Khác mockup: ví không có nút "+" (màn này không mua gì); ván menu không có icon (chưa cắt); tab Collection / Growth / Capture bố cục lại cho khung ngang 16:9 (mockup vẽ dọc); bước "How to capture" là chữ, không có 3 tranh minh hoạ; chip ví là fallback phẳng (`kit/chip-resource` chưa cắt); tên dài trên thẻ pet co nhỏ dưới cỡ 11u cho vừa dải giấy.
- Ảnh: `docs/reference/ui-redesign-261116/phase-08-pet/`. Exe: `C:/temp/player-ui-p08/Zenonia.exe`.

## Success Criteria
- Đặt / đổi / bỏ pet khỏi ổ, Level Up, Star Up, Growth chạy như cũ offline + online.
- Team Bonus và Bond ACTIVE hiển thị đúng số của `PetRules`.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Xung đột với Pet V2 chưa commit | Cao x Cao | Không bắt đầu trước khi commit; làm view ở file mới |
| Pet art to nhỏ khác nhau trên ổ | Trung bình x Thấp | Co theo chiều cao khung, căn chân lên mặt ổ |
| Ví 6 loại tràn thanh trên ở 4:3 | Trung bình x Thấp | Rút gọn số (12.3k), ẩn loại = 0 nếu thiếu chỗ |

## Security Considerations
- Gacha / growth / capture vẫn do server quyết online (không đổi luật).

## Next Steps
- Phase 09 (gacha) dùng nhãn độ hiếm P1b.

## Câu hỏi mở
- Nút "đổi" (mũi tên vòng) cạnh Growth trên bảng chi tiết là gì (đổi pet ở ổ này?) -> hỏi ChatGPT khi xin mockup tab.

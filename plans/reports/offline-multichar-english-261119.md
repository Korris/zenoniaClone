# Offline nhiều nhân vật cùng class, đặt tên offline, toàn bộ chữ tiếng Anh (19/11/2026)

Branch `worktree-agent-a1a1efdb0e97d5114` (gốc `ro-fantasy-heroes` ebdbd5b7 + cherry-pick nền 8f4c0fab). Exe: `D:\temp\player-offline\Zenonia.exe` (bản sao `D:\temp\zu-copy-offline`, Library từ `zu-copy-charselect`; Succeeded, 0 lỗi).

## 1. Save theo slot
- File mới: `zenonia-save-slot<N>.json` (N = 0..5) trong persistentDataPath. Class và tên nằm trong file (`SaveData.classId`, `SaveData.name` mới; tên trống = tên class).
- `SaveStore` không còn phụ thuộc Unity (`#if UNITY_5_3_OR_NEWER`), dotnet test chạy được qua `RootOverride`. API: `Write(d, slot)`, `Load(slot)`, `LoadAll()`, `FirstFreeSlot()`, `SlotOfClass()`, `NameProblem()`, `Delete(slot)`, `DisplayName()`.
- `RpgSession.Begin(class, slot, name)`: nhớ `CurrentSlot` / `CurrentName`, autosave vào đúng slot. Nếu slot đang chứa class khác thì không ghi đè, chơi không lưu.
- `GameSession.EnterSlot(slot)` (nút Enter), `CreateInSlot(slot, class, look, name)` (tạo mới). `Spawn(class)` cũ (phím dev F1..F6, host, capture) lấy save đầu tiên của class đó, không có thì lấy ô trống đầu.
- Màn chọn class offline không khoá class đã có save nữa (`taken = i => false`).
- id nhân vật offline trên màn slot là `slot<N>`. Nhân vật đang chơi được nhận theo slot, không theo class.

### Migration (`SaveStore.Migration.cs`)
- Chạy tự động mỗi lần đọc save (`Load` / `LoadAll` / `FirstFreeSlot`). Khi không còn file cũ, mỗi lần chỉ tốn 6 lần `File.Exists`.
- Với mỗi `zenonia-save-<class>.json`:
  1. Chép nguyên văn từng byte vào file slot. Không serialize lại, nên save cũ không có tên và hiện bằng tên class.
  2. Đổi tên file cũ thành `.migrated`, kể cả `zenonia-slot-<class>.txt`.
- Thứ tự xếp slot:
  - Trước tiên, save có `zenonia-slot-<class>.txt` trỏ tới ô hợp lệ và còn trống thì giữ ô đó.
  - Các save còn lại lấy ô trống đầu, theo thứ tự class.
- Nếu game tắt giữa lúc chép và lúc đổi tên: lần sau thấy một slot có nội dung y hệt thì chỉ đổi tên, không tạo bản trùng.
- Nếu đủ 6 slot: file cũ để nguyên, lần đọc sau khi có ô trống sẽ chuyển tiếp.

## 2. Đặt tên offline
- Bước ngoại hình offline nay có ô tên giống online, tiêu đề "CUSTOMIZE AND NAME", nút CREATE, nút xúc xắc chọn tên ngẫu nhiên chưa trùng.
- Quy tắc nằm chung ở `Rpg/HeroNameRules.cs`, giống schema server: trim, 3-16 ký tự `A-Za-z0-9_`. `AccountFlow.NameProblem` dùng lại hàm này.
- Tên không được trùng các nhân vật trên máy, không phân biệt hoa thường (giống `nameLower` của server). Save cũ chưa có tên được tính là tên class, ví dụ "Wizard".
- Host dev vẫn chỉ chọn ngoại hình (CONFIRM), không đặt tên.
- Tên offline hiện trên đầu nhân vật (`NameTags.OwnName`) và trong chat (`ChatHub.LocalName`).

## 3. Tiếng Anh
- Màn slot đã chuyển hết sang tiếng Anh: Select Character, Enter, + Create Character, Delete, Name / Class / Level / Last Map, Switch Account, Play Online, Play Offline, Back, Connect (dev), Playing, In game, Lv N, Loading ..., No characters yet...
- Thẻ xoá: Delete X?, Hold to delete, Hold N s more, Keep, Deleted X.
- Tên class lấy từ `HeroClassDef.Name`: Warrior, Wizard, Mechanic, Assassin, Cleric, Fighter.
- Đã grep chữ có dấu trong `Assets/Scripts` (bỏ qua comment). Còn lại hai chỗ, cố ý để nguyên vì không phải chữ hiển thị của game:
  - `PaperKitSheet.cs`: bảng mẫu kit dev, chỉ mở bằng `--kit-sheet`. Dòng "Nội dung 13: ắ ằ ẳ…" dùng để kiểm font tiếng Việt.
  - `DebugCapture.UiChat.cs`: dữ liệu chat giả trong capture, kiểm dấu trong chat.
- server-api: không có chuỗi tiếng Việt, không sửa.

## 4. Nền màn slot
- `CharacterFlowKit.Backdrop` nạp `character-select/slot-screen-background` (cherry-pick 8f4c0fab), cover-fit, giữ tỉ lệ. Nếu thiếu file thì dùng lại `hero-select/bg`.
- Nền này dùng chung cho màn slot và màn ngoại hình / tên.

## Kiểm tra
- dotnet (SDK 10): ZenoniaSkills 435/435 (thêm `OfflineSaveSlotsTests` 9 test: migration + backup, tự migrate khi đọc, ô nhớ bị chiếm, crash giữa chừng, đủ 6 ô, 2 Warrior cùng class, quy tắc tên + trùng, tên qua round trip, save tắt). ZenoniaNet 422/422.
- server-api: không đụng, không chạy vitest.
- `--character-select-test --save-root D:\temp\offline-cs-saves`: 27 PASS, 0 FAIL. Luồng: seed 2 save cũ + 1 slot, migrate, tạo Wizard thứ 2 "Merlin" ở ô 5 (từ chối tên trống, từ chối "wIZARD"), vào game, 2 Wizard trên màn slot, Enter Wizard cũ Lv 41, Enter Mechanic đã migrate Lv 23, xoá bằng giữ nút.
- `--appearance-test`: 33 PASS, 0 FAIL (thêm: phải đặt tên, tên 2 ký tự báo luật, chơi dưới tên "Rowan").
- `--hero-select-test`: 3 PASS (đã cập nhật để đặt tên).

## Ảnh (đã xem) `docs/reference/character-select-ro-fantasy/en-*.jpg`
- `en-01`: màn slot sau migrate.
- `en-02`: bước chọn class, Wizard không bị khoá.
- `en-03`, `en-04`: bước đặt tên (tên trùng / Merlin).
- `en-05`: Merlin trong game, tên xanh trên đầu.
- `en-06`, `en-07`: màn slot có 2 Wizard trên nền sáng mới.
- `en-08`: thẻ xoá tiếng Anh.
- Chưa xem:
  - Nền mới ở cỡ 16:9 (capture chạy ở 3440x1440).
  - Màn hẹp hoặc điện thoại.
  - Save thật của người dùng: migration mới chạy trên thư mục tạm, chưa chạy trên AppData thật.

## Ghi chú
- "Last Map" của nhân vật vừa tạo trong capture là Frost Hollow: capture ép map Frost Hollow, chơi thường sẽ là làng.
- Bản build cũ đọc định dạng mới sẽ không thấy save, vì file cũ đã thành `.migrated`. Muốn quay lại bản cũ thì đổi tên file `.migrated` về như trước.

## Câu hỏi
- Có muốn cho đổi tên nhân vật offline sau khi tạo (save cũ đang mang tên class) không?

# Màn chọn slot nhân vật RO Soft Fantasy (19/11/2026)

Branch `worktree-agent-a876c886ff67f1533` (gốc `ro-fantasy-heroes` 9658713e, chưa merge mockup e677399a: chỉ lấy file từ commit đó). Exe: `D:\temp\player-charselect\Zenonia.exe` (bản sao `D:\temp\zu-copy-charselect`, Library từ `zu-copy-rofantasy`; Succeeded, 0 lỗi).

## Vì sao trước đây "1 nhân vật 1 tài khoản"
- server-api đã cho 5 nhân vật / tài khoản từ 12/11 (527d07c2), client online cũng đã có danh sách. Nhưng:
  - exe mặc định chạy **offline**, mở thẳng màn chọn class; mỗi class 1 save, không có danh sách; online nằm sau nút bánh răng.
  - Danh sách online cũ kiểu tối, 4 bệ / trang, không có ô slot.
- Nếu server thật vẫn là bản trước 12/11 thì còn index unique (userId, classId): bản mới tự xoá index đó khi khởi động.

## Luồng mới
- **Online**: đăng nhập -> màn slot (kể cả tài khoản mới: 6 ô trống) -> ô có nhân vật: chọn, "Vào game"; ô trống (hoặc "+ Tạo nhân vật" = ô trống đầu) -> chọn class -> ngoại hình + tên -> về màn slot, nhân vật nằm đúng ô đã bấm.
- **Offline**: mở game -> cùng màn slot, liệt kê save trên máy (tối đa 1 / class = 6). Ô trống -> chọn class (class đã có save bị khoá) -> ngoại hình -> vào game. Settings > Đổi nhân vật trong game cũng mở màn này (nút "Quay lại"; nhân vật đang chơi: "Đang chơi", không vào lại / xoá được).
- Back: class step -> màn slot; ngoại hình -> class step (giữ class); góc trái "Đổi tài khoản" (online) / "Chơi online" (offline) / "Quay lại" (trong game); góc phải "Chơi offline" (+ "Kết nối (dev)").
- Xoá 2 bước: "Xoá" mở thẻ xác nhận, phải **giữ nút đỏ 2 s** ("Giữ thêm 1.0 s"); bấm nhanh hoặc "Giữ lại" không xoá.
- Host dev vẫn chọn class trực tiếp (không save, không màn slot).

## Giới hạn và slot
- `DEFAULT_MAX_CHARACTERS = 6` (`server-api/src/rpg/rpg-constants.js`, env `MAX_CHARACTERS` đổi được), client `AccountFlow.MaxCharacters = 6`.
- Nhân vật có trường `slot` (0..5), `POST /characters` nhận `slot` tuỳ chọn: ô bận / ngoài giới hạn = 409 `slot_taken`; không gửi = ô trống đầu. Index unique (userId, slot). Nhân vật cũ chưa có slot được gán ô trống theo thứ tự tạo ở lần list đầu. List trả theo slot.
- Quyền sở hữu: GET / enter / delete nhân vật người khác = 404 (đã có, thêm test). Offline: slot ghi file `zenonia-slot-<class>.txt` cạnh save (không đổi định dạng save).

## Code chính
- server-api: `character-service.js` (`slotted`, create với slot), `schemas.js`, `character-routes.js`, `indexes.js`, README.
- Client: `CharacterListScreen.cs` (chế độ online/offline, slot, tạo), `.View.cs` (hiển thị), `.Build.cs` (bố cục theo mockup), `.Delete.cs` (thẻ xác nhận), `HoldToConfirmButton.cs`, `OfflineCharacterRoster.cs`; `HeroSelectScreen` chỉ còn chế độ tạo (`OpenCreate`) + host; `AccountFlow.PendingSlot/FirstFreeSlot/DeleteHeld`; `ApiClient.CreateCharacterAsync(..., slot)`.
- Art: tấm mảnh `Assets/ArtLibrary/ui/character-select/slot-ui-kit-chatgpt-2.png` (lấy từ e677399a, cùng nội dung) cắt bằng `Tools/cut-ui-paper-sheet.py` + `Tools/ui-character-select-sheets/slot-ui-kit.json` -> `Assets/Resources/Sprites/ui/character-select/` (slot-card, -selected, -empty, pedestal, info-panel 9-slice, title-banner, name-label, có .meta). Contact: `docs/reference/character-select-ro-fantasy/art-contact/`. Thiếu mảnh nào thì code tự lùi về mảnh RO Soft gần nhất (`SlotArt`), không vẽ code.
- Capture: `--character-select-test --save-root DIR` (mới, offline, bấm nút thật + giữ nút xoá), `--login-shots` thêm: bấm ô trống, đủ 6, thứ 7 bị từ chối, xoá bằng giữ nút; `--appearance-test`, `--hero-select-test` đi qua màn slot.

## Kiểm tra
- server-api vitest 194/194 (`test/character-roster.test.js` mới: 6 nhân vật lẫn class, thứ 7 409, xoá rồi tạo lại, người khác 404, giới hạn theo tài khoản, MAX_CHARACTERS=2, slot chọn / mặc định / bận / ngoài giới hạn / sai kiểu / tái dùng / nhân vật cũ).
- dotnet: ZenoniaSkills 426/426, ZenoniaNet 422/422 (SDK 10).
- `--character-select-test`: 20 PASS 0 FAIL. `--appearance-test`: 30 PASS. `--hero-select-test`: 3 PASS.
- `Tools/character-flow-shots.ps1` (mongod + server-api cục bộ): 11 PASS; 2 FAIL "in the world" / "account menu" là do script không chạy zone (như trước).

## Ảnh (đã xem) `docs/reference/character-select-ro-fantasy/`
- `so-sanh-mockup-vs-ingame.jpg`: mockup | trong game.
- Offline `offline-01..11`: trống, 3 save, chọn + xoay, class step (class đã có save khoá "NEXT"), ngoại hình, vào game, đổi nhân vật trong game (Warrior ở ô 6 đúng ô đã bấm, "Đang chơi"), vào Mechanic Lv 23, thẻ xoá, đang giữ (1.0 s), sau khi xoá.
- Online `online-*`: tài khoản mới 6 ô trống, class step online, tạo có tên, nhân vật vào ô 5 đã bấm, đủ 6 (2 Warrior), thứ 7 bị từ chối, thẻ xoá, sau xoá ô trống giữ đúng vị trí, sau "Change character".
- Chưa xem: vào zone online thật (script không chạy zone), màn hẹp < 1138 px, điện thoại.

## Khác mockup / art UI còn thiếu (đang mượn mảnh gần nhất)
1. Nền: vẫn `hero-select/bg` (lâu đài đổ nát, tối hơn mockup sáng có hoa, cờ).
2. Nút có icon (mũi tên "Đổi tài khoản", "+", thùng rác "Xoá", kiếm chéo "Vào game") và nút "Vào game" viền vàng: đang dùng nút RO Soft trơn (`btn-sky`, `btn-coral`, `btn-mint`) + chữ.
3. Nhãn tên đỏ cho ô đang chọn: chỉ có bản xanh.
4. Icon dòng "Tên" (người): chưa có; Class / Cấp / Bản đồ dùng icon cũ character-flow (màu tối hơn mockup). Icon class nhỏ trên thẻ slot: chưa có.
5. Đường kẻ giữa các dòng khung thông tin: chưa có (không vẽ code).
6. Mascot poring trên dải tiêu đề: không có trong tấm mảnh.
7. Nút xoay `<` `>` và nền mờ thẻ xoá: mượn `btn-ivory`, `panel-glass`.
8. Màn chọn class (bước tạo) vẫn là màn hero select cũ tông tối.

## Deploy
- **Cần deploy server-api mới** (giới hạn 6, `slot`, index mới tạo khi khởi động). Client mới gửi `slot`; server cũ bỏ qua trường này (vẫn tạo được, chỉ không giữ ô). Không đổi protocol zone, không cần build lại zone.

## Câu hỏi chưa chốt
1. Offline vẫn 1 save / class (tối đa 6, không 2 nhân vật cùng class): có cần cho offline nhiều nhân vật cùng class (đổi định dạng save) không?
2. Offline tên nhân vật = tên class (save không có tên): có thêm bước đặt tên offline không?
3. Chữ trên màn slot là tiếng Việt theo mockup, các màn khác và thông báo server vẫn tiếng Anh: đổi cả game sang tiếng Việt không?
4. Tên class tiếng Việt cho Mechanic / Assassin / Fighter (đang đặt "Cơ khí sư", "Sát thủ", "Võ sĩ") có đúng ý không?

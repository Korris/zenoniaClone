# Zoom camera bằng con lăn chuột trên bản PC (2026-11-05 19:09)

## File đổi
- `Assets/Scripts/CameraWheelZoom.cs` (mới, 84 dòng): MonoBehaviour gắn lên camera. `Allowed()`, `Attach(cam)`, đọc/ghi PlayerPrefs, mượt hoá, chặn theo map.
- `Assets/Scripts/InputAdapter.cs`: thêm `MouseScrollDelta()` trong 3 nhánh `#if ENABLE_LEGACY_INPUT_MANAGER` (`Input.mouseScrollDelta.y`) / `#elif ENABLE_INPUT_SYSTEM` (`Mouse.current.scroll.ReadValue().y / 120f`) / `#else` (0). File 195 dòng.
- `Assets/Scripts/DevStartConfig.cs`: thêm `HasCameraViewHeight()` (dev-start.json đặt `cameraViewHeight` > 0).
- `Assets/Scripts/CameraFollow.cs`: `Configure()` nhân nửa chiều cao mặc định với `CameraWheelZoom.Current` (= 1 khi không gắn zoom) để đổi map vẫn giữ mức zoom.
- `Assets/Scripts/Bootstrap.cs`: gọi `CameraWheelZoom.Attach(cam)` ngay trước `follow.Configure()`.
- `docs/pending-tasks.md`: một dòng mới trong "Xem trong game, chưa ai xem".
- Chưa có `CameraWheelZoom.cs.meta`: Unity đang mở sẽ tự sinh; bản copy khi build cũng tự sinh.

## Cách gating
- Chỉ gắn component khi `#if UNITY_STANDALONE || UNITY_EDITOR`, `!HudLayout.IsWeb`, `!Application.isMobilePlatform`, không phải chạy capture/bot (`DebugCapture.Requested(args)`), không có `--zoom`, không có `cameraViewHeight` trong dev-start.json. Khi không gắn thì mọi thứ y như cũ (không đọc PlayerPrefs, `Current` = 1).
- Mỗi frame bỏ qua con lăn khi `GameSession.Hero == null`, `UiWindows.BlocksGameplay` (modal: dialogue, quest, skill tree, shop, trade, settings; hoặc đang gõ chat), `InventoryPanel.IsOpen`, `HeroChangeScreen.IsOpen`, `AccountScreenGui.MenuOpen`, hoặc chuột đang trên UI (`EventSystem.current.IsPointerOverGameObject()`, cùng cách `MouseAttack()` dùng). Trên bản Windows HUD mặc định là mobile nên lăn khi chuột đè lên joystick/nút cũng bị bỏ qua.
- Ghi `cam.orthographicSize` trong `Update`; `CameraFollow.LateUpdate` nhận làm `baseSize` khi rảnh, punch-in vẫn nhân lên trên đó, `Clamp` theo map dùng size mới.

## Số liệu
- Mặc định giữ nguyên: `DevStartConfig.CameraViewHeight() * 0.5` (= 80).
- Hệ số 0,6x..1,6x (half-height 48..128). Mỗi nấc nhân 0,92 (lăn lên, gần) hoặc chia 0,92 (lăn xuống, xa); delta lẻ (touchpad) dùng `Pow`.
- Mượt bằng `SmoothDamp` 0,15 s, `unscaledDeltaTime` (không bị hit-stop/pause làm đứng).
- Chặn zoom xa: hệ số tối đa = min(1,6; min(MapH/2, MapW/(2·aspect)) / 80), nhưng không nhỏ hơn 1 (mặc định luôn được phép, map nhỏ hơn mặc định thì CameraFollow vẫn căn giữa như cũ). Map 36x24 ô 16 = 576x384: MapH/2 = 192 > 128 nên trên map hiện tại đủ 1,6x; cạnh ngang 16:9 tới 1,6x·80·1,78 = 228 < 288 cũng đủ.
- PlayerPrefs key `zenonia.zoom` (float hệ số), ghi mỗi lần đổi nấc, bọc try/catch.

## Build
`BUILD RESULT: Succeeded errors 0 -> C:\temp\player-zoom\Zenonia.exe` (build từ `C:\temp\zu-copy`). Không chạy dotnet test (không động `Rpg/`, `Net/Protocol/`, `Maps/`).

## Chưa làm / câu hỏi
- Chưa thử trong game: chiều lăn, cảm giác 8 %/nấc, pref có nhớ sau khi tắt mở lại, chuyển map giữ zoom, punch-in của ultimate khi đang zoom.
- Khi chuột đè lên joystick/nút touch (HUD mobile trên Windows) thì con lăn bị bỏ qua: có muốn chỉ chặn ở cửa sổ có danh sách (chat, túi) thay vì mọi UI không?
- Có nên thêm phím reset về 1x (vd. nhấn nút giữa chuột) hay mục trong Settings? Chưa làm vì chưa yêu cầu.
- Minimap/NameTags/`DamageNumber` vẽ theo pixel hay theo world: nếu theo world thì khi zoom xa chữ nhỏ đi, chưa xem.

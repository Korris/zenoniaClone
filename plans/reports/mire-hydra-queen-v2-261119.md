# Mire Hydra Queen v2: sprite RO Soft, idle, FX shader, vẽ 2x (19/11/2026)

Branch worktree `worktree-agent-a9afcc0a0095deaa9` (từ `ro-fantasy-heroes` 0e4133a8). Exe: `D:\temp\player-hydra\Zenonia.exe` (bản sao `D:\temp\zu-copy-hydra`, 0 lỗi). Đã đọc `CLAUDE.md`, `docs/sprite-rules.md` (quy tắc + Sổ lỗi boss truy nã), 2 báo cáo FX ánh sáng, báo cáo map.

## 1. Sprite (ChatGPT Tan Pham)

Chat: https://chatgpt.com/c/6afeda8c-09c8-83ec-87a8-8f43b022a923. Mỗi lần gửi đều đọc `/api/auth/session` = "Tan Pham".

| Sheet | Nguồn `ArtLibrary/monsters/wanted/mire-hydra-queen-v2-*` | Kết quả |
|---|---|---|
| walk xuống/lên, trái/phải | `walk-*-chatgpt-1.png` | nhận lần đầu, đúng concept, đủ 3 đầu rắn |
| idle (MỚI) | `idle-*-chatgpt-1.png` | vẽ lại 1 lần mỗi ảnh; vẫn gần giống walk (đuôi đổi hình nhẹ), frame 3 nhô cao. Nhận (hết 2 lần) |
| attack | `attack-*-chatgpt-1.png` | nhận lần đầu: 3 đầu rắn lao cắn, hàng lên lao ra xa người xem |
| cast hàng 0-1 (phun độc, quật đuôi) | `cast-breath-tail-chatgpt-1.png` | **thiếu đầu rắn thứ 3** ở hầu hết frame. Cài TẠM |
| cast hàng 2-3 (cuồng nộ, gục) | chưa vẽ | tạm dùng lại hàng 0-1 (tool in cảnh báo) |

- Nền `#00FF00` (boss tím), không bóng, không hiệu ứng, có câu tông RO Soft Fantasy; tin đầu kèm crop concept phóng 2x, ảnh bố cục ô, 2 ảnh tông mẫu.
- **Lượt vẽ: 9 ảnh trên Tan Pham** (walk 2, idle 4 gồm 2 lần vẽ lại, attack 2, cast 1); lần thứ 10 (vẽ lại cast đủ 3 đầu) báo hết lượt tới 09:17 ngày 20/11.
- Chuyển sang Tay theo lệnh người dùng: **bị từ chối quyền** khi tải ảnh mẫu lên chat Tay → DỪNG phần art, không lách.
- Tool: `Tools/build-wanted-boss-sheets.py` thêm `key_green`, `TARGET_H_OF` (nữ hoàng 350 px), idle → `mireHydraQueen-idle.png`, cast thiếu dải thì dùng lại hàng 0-1, nguồn FX cũ giữ tên v1 (`FX_STEM`).
- Code: `Monster.Visuals.cs` nạp `<sheet>-idle.png` nếu có; đứng yên (Idle / Cooldown / Windup ngoài frame đánh) vẽ idle 260 ms/frame, chỉ khi di chuyển mới vẽ walk.
- Ảnh sheet ghép trên nền đá: `docs/reference/mire-hydra-queen-v2/sheet-*.png` (đã xem từng frame: chân cùng đường đáy, không chạm mép ô).

## 2. Vẽ 2x (người dùng: "to gấp 2 lần")

- `MonsterDef.Wanted(..., drawMul: 2f)` chỉ cho nữ hoàng (RenderScale 2,5 → 5). `--wanted-render-scale` vẫn nhân thêm, giữ hệ số riêng. Hitbox, tầm đánh, bán kính skill, số liệu server KHÔNG đổi.
- Đã thấy trong ảnh (`2x-*`): hero đứng trước (phía nam) vẽ đè lên đuôi, hero sau lưng làm boss mờ (cơ chế có sẵn); boss nằm gọn trong sân 19x15 theo chiều ngang.
- **Lệch cần người dùng quyết** (không tự sửa):
  - hero đứng sát (2-3 ô phía nam) thì boss cao quá nửa màn hình, đầu và vương miện ra khỏi khung trên / sau thanh boss;
  - vòng Tail Crush 4 ô gần như nằm dưới thân boss; hitbox (43 x 25) nhỏ hơn hẳn thân vẽ, tầm đánh 37 đv: đánh vào mép thân vẽ có thể trượt.

## 3. FX skill bằng shader / particle

| File | Vai trò |
|---|---|
| `Resources/Shaders/WantedWarningDisc.shader` | vòng cảnh báo: hình tròn / quạt đúng bán kính (quad 2R x 2R, không ép dẹt), lấp dần từ tâm, viền đỏ-hồng, ruột tím có đốm rêu trôi |
| `Scripts/MireHydraLightFx.cs` (+ `.Impacts.cs`) | 1 object / hình skill, chạy theo `ServerNowMs - AtMs`: cảnh báo, bọt độc 450 ms cuối, va chạm từng skill |
| `Scripts/MireHydraFxLook.cs` | material tím độc + xanh rêu từ `LightBeamScroll`, `LightBlob`, `LightParticle` |
| `Scripts/WantedFxParticles.cs` | ParticleSystem dựng bằng code, phát từng hạt bằng `Emit` |
| `Scripts/Rpg/WantedSkills/WantedSkillLightTiming.cs` | thời gian thuần C#, có test |
| `WantedSkillTelegraphFx.cs` | boss có FX mới thì bỏ sprite cảnh báo + tấm va chạm; vẫn giữ kiểm tra vùng nguy hiểm, thanh niệm, đoán đóng băng |

- Venom Breath: 3 luồng độc (tấm tím + lõi rêu) dài ra tới mép quạt đúng lúc trúng, bọt bay theo, ánh tím trên nền.
- Poison Bloom / Toxic Reign: cột độc đứng (y-sort), vòng rêu loang, bọt bắn lên; Toxic Reign có giọt độc rơi chạm đất đúng lúc trúng.
- Tail Crush: vòng sóng rêu chạy ra mép (260 ms), tia tím toả theo, bọt bắn ra ngoài.
- Thời gian: cảnh báo đầy đúng `AtMs` (= `TelegraphMs`), mọi mảnh tắt trước 1,0 s. Không đổi số liệu, không đổi luật server.
- `--boss-fx legacy` = FX sprite cũ. Thiếu shader / texture thì tự về FX cũ.

Đã thấy trong ảnh chụp:
- vòng cảnh báo tròn, lấp dần, khớp chỗ trúng; quạt Venom Breath quay đúng về hero;
- luồng độc / sóng / cột đọc rõ trên đá sáng, không loá trắng, hero vẫn thấy; tắt hết ở +850 ms;
- lần chụp 1 cột Poison Bloom gần như không thấy; lần 2 che kín hero; đã hạ còn 0,72 / lõi 0,6 (ảnh `2x-solo-poison-bloom-geyser-series.jpg` là bản giữa, lõi vẫn hơi dày; bản cuối chưa chụp lại).

Chưa thấy rõ: giọt độc rơi của Toxic Reign (trận đông, nhiều FX hero che); cuồng nộ chỉ thấy cột độc ở mép dưới.

## 4. Ảnh `docs/reference/mire-hydra-queen-v2/` (đã xem từng ảnh)

- `sheet-mireHydraQueen{,-idle,-attack,-cast}.png`: 4 sheet trên nền đá.
- `1x-solo-*.jpg`: trước khi vẽ 2x (1 hero): Venom Breath, Poison Bloom, Tail Crush, ảnh cảnh báo tròn.
- `2x-solo-venom-breath-series.jpg`, `2x-solo-poison-bloom-geyser-series.jpg`.
- `2x-crowd-*.jpg`: 19 bot + 1 hero: idle / đòn đánh, Poison Bloom, Tail Crush, Toxic Reign (cuồng nộ).
- Cờ chụp mới: `--persist-role wanted-showcase` (`DebugCapture.NetWantedShowcase.cs`), `Tools/wanted-boss-playtest.ps1 -Role -StartDx -StartDy -ExtraArgs`, `Tools/wanted-boss-crowd-playtest.ps1 -Role`. Swarm cần `DOTNET_ROOT=C:\Users\korri\.dotnet`.

## 5. Texture tạm (Python) cần ChatGPT vẽ lại

`venom-bubble` (mới, 64x64, bọt độc), cùng các texture dùng chung đã ghi ở 2 báo cáo trước (`noise-clouds`, `portal-veil`, `burst-ring`, `meteor-core`). Lệnh nhập: `python Tools/build-light-skill-textures.py import <tên> <ảnh>`.

## 6. Kiểm tra

- `dotnet test` (SDK 10): ZenoniaSkills 442 (thêm 5 test `WantedSkillLightTimingTests`), ZenoniaNet 455, đều đạt.
- **Không cần build lại zone server**: không đổi số liệu skill / map / protocol. Zone dùng `MonsterDef` nhưng chỉ đổi `RenderScale` (hình).

## 7. Chưa làm (bị chặn quyền)

- Yêu cầu thêm của người dùng: skill "Venom Drain" (rút máu bằng độc), ô debuff độc dưới thanh máu, hỏi ChatGPT có hiện độc trên nhân vật không. Khi sửa bảng skill / luật server, hệ thống từ chối quyền ("Destructive or Unwanted Actions": brief ban đầu cấm đổi số liệu / luật server); gửi câu hỏi chữ lên chat Tay cũng bị từ chối ("Unsanctioned Communication"). Không lách. Cần người dùng cho phép trực tiếp.
- Vẽ lại cast (đủ 3 đầu) + hàng cuồng nộ / gục: chờ Tan Pham có lượt (09:17 ngày 20/11) hoặc người dùng cho phép Tay.

## Lỗi mới (đã ghi Sổ lỗi `docs/sprite-rules.md`)

- Idle ChatGPT chép sheet đi.
- Cast thiếu đầu rắn.
- Boss map Mirefang Hydra đứng cùng ô (45,12) với nữ hoàng, che nhau, bot đánh nhầm.
- FX cột độc quá mờ rồi quá dày; vẽ 2x làm vòng skill nằm dưới thân.

## Câu hỏi

1. Vẽ 2x: có nhân bán kính skill / hitbox theo cỡ vẽ, hay lùi camera ở sân boss?
2. Dời boss map Mirefang Hydra hay boss truy nã khỏi ô (45,12)?
3. Cho phép làm Venom Drain + ô debuff (đổi luật server, phải build lại zone) và cho phép dùng Tay?
4. Idle: chấp nhận bản ChatGPT gần giống walk hay chuyển sang thở bằng code?

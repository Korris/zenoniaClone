# Rimeclaw Tyrant thành sói vua băng (20/11/2026)

Nhánh `ro-fantasy-heroes`, làm thẳng trên cây chính. Đã đọc `CLAUDE.md`, `README.md`, `docs/pending-tasks.md`, `docs/sprite-rules.md` (quy tắc đầu file, mục art ChatGPT 0-8, quy tắc thiết kế quái, Sổ lỗi phần quái / boss truy nã; các dòng Sổ lỗi riêng hero chỉ lướt), `docs/monster-redesign-bible.md` (quy tắc cứng, A4-A7, mục Wanted, ràng buộc kỹ thuật, mẫu prompt), `docs/boss-hunter-pending.md`, báo cáo Mire Hydra Queen v2.

## 1. Yêu cầu
- "vẽ wanted boss con sói đi" → chọn "Rimeclaw Tyrant thành sói". Mẫu chính (người dùng đổi giữa chừng): con đầu tiên của `docs/reference/ui-style-ro-soft-fantasy/wanted-boss/5-wanted-boss-board-style.png` (sói bốn chân, vương miện vàng đá xanh, áo choàng + giáp navy viền vàng, tinh thể băng lưng).
- Góp ý 1: "mất cái lòng đen" (mắt) → vẽ lại với mắt vàng hổ phách, con ngươi đen.
- Góp ý 2: "cho to như con hydra queen" → `drawMul: 2` + chiều cao thân 350 như nữ hoàng.
- Id `rimeclawTyrant`, skill, luật server, HP / sát thương / bán kính skill KHÔNG đổi.

## 2. Art (ChatGPT Tan Pham)
Chat: https://chatgpt.com/c/6b0003a9-2194-83ec-97c9-65878e4f9677 (Chrome "Browser 1" 75b5d807, `/api/auth/session` = "Tan Pham" trước mỗi lần gửi). "Browser 2" 743f7981 là **Tuan Tai** (cấm), "Browser 3" 46560879 là Tay (tên hiển thị "Tân Korris").

11 lượt vẽ:
| # | Sheet | Kết quả |
|---|---|---|
| 1 | đi xuống/lên (theo mockup bảng truy nã) | người sói hai chân: bỏ (sai mẫu) |
| 2 | đi xuống/lên (theo mẫu 5-wanted-boss) | đúng thiết kế, mắt phát sáng: bỏ |
| 3 | đi ngang | đúng, mắt phát sáng: bỏ |
| 4 | đánh xuống/lên (nháp) | mắt phát sáng: chỉ dùng làm mẫu tư thế |
| 5 | đi xuống/lên v3 | NHẬN (mắt đúng) |
| 6 | đi ngang v2 | NHẬN |
| 7 | đánh xuống/lên v2 | NHẬN |
| 8 | đánh ngang | NHẬN |
| 9 | cast đập + dậm | NHẬN |
| 10 | cast cuồng nộ + gục | NHẬN |
(1 lượt đánh bị dừng giữa chừng.) Nền #FF00FF, prompt có câu tông RO Soft Fantasy, ô / chân / hướng ghi bằng số, ảnh bố cục 2x4.

Nguồn: `Assets/ArtLibrary/monsters/wanted/rimeclaw-tyrant-*-chatgpt-1.png` (thay bản cua-sư tử, bản cũ còn trong git), ảnh gửi kèm `.../rimeclaw-tyrant-wolf-ref/`. Cắt: `py Tools/build-wanted-boss-sheets.py write rimeclawTyrant` (layout cũ: ô 384 / 512, chân 448, 4 hàng cast), không đổi code đọc sheet.

## 3. Code / tool
- `MonsterDef.RimeclawTyrant`: `drawMul: 2f` (như nữ hoàng; RenderScale 2,5 → 5). Chân (`BodyCollisionRules.FootRadius`) và thân cận chiến (`Monster.MeleeBody`) tự to theo chiều cao vẽ, chung client + server. **Zone server dùng MonsterDef: phải build lại zone khi deploy.**
- `Tools/build-wanted-boss-sheets.py`: `TARGET_H_OF["rimeclawTyrant"] = 350` (tool tự co 0,951 → 0,920 vì frame ngang rộng; thân trung vị 345 px so với nữ hoàng 348).
- `BossPortraits.cs`: khung mặt Tyrant (0,51; 0,48; 0,40) cho thanh boss / thẻ Wanted.

## 4. Kiểm tra
- Ảnh từng frame: `docs/reference/rimeclaw-tyrant-wolf/sheet-*.png`, mắt mọi frame thấy mặt `eyes-check-every-face-frame.png` (con ngươi đen đủ), trước/sau `before-after-walk-sheet.png`, so cỡ với nữ hoàng `size-vs-queen-offline-same-cell.png`.
- Trong game (bản 1x, trước khi to 2x): `ingame-idle-walk.jpg`, `ingame-attack-series.jpg`, `ingame-skills-series.jpg` (Frostclaw Sweep, Chain Slam, Stomp), `ingame-cast-closeup-stomp-slam.jpg`; chân dung trên thanh boss đúng mặt sói.
- Bản 2x: xem mục 5. Exe `C:	emp\player-wolf\Zenonia.exe`.
- `dotnet test`: ZenoniaSkills 474, ZenoniaNet 455, đạt.

## 5. Bản to 2x (như nữ hoàng)
- Đo trên sheet: thân trung vị Tyrant 345 px, nữ hoàng 348 px (cùng ô 384, cùng drawMul 2); sói rộng hơn (324 so với 276 px).
- Ảnh cùng zoom trong game: `ingame-size-wolf-vs-queen-same-zoom.jpg` (cao gần bằng nhau). Chuỗi 2x: `ingame-2x-idle-walk-attack.jpg`, `ingame-2x-skills-series.jpg` (đủ 4 skill), `ingame-2x-blizzard-enrage.jpg` (cuồng nộ: hàng gầm + hơi lạnh hiện đúng).
- Thấy: đỉnh vương miện bị thẻ "Today's Wanted" che khi boss ở nửa trên màn (như nữ hoàng); hero đứng sau thân thì boss mờ trắng (cơ chế có sẵn); tấm FX sprite cũ của Blizzard / Stomp rất to, che hero.

## Lỗi mới (Sổ lỗi `docs/sprite-rules.md`)
- Mắt mất con ngươi do prompt "mắt phát sáng"; quy tắc mắt thêm vào bible mục 8.5.

## Chưa thấy trong game
- Hàng gục (game không dùng).
- Chơi tay, đông người.

## Câu hỏi
1. Không có idle riêng (sheet đi là nhịp thở tại chỗ): cần vẽ idle như nữ hoàng không?
2. FX skill Rimeclaw vẫn tấm sprite cũ (băng xanh): chuyển sang particle / shader như nữ hoàng?
3. Thẻ "Today's Wanted" che đỉnh boss to 2x (cả nữ hoàng): dời thẻ hay lệch camera?

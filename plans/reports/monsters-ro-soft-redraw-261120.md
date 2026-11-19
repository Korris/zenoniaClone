# Quái RO Soft Fantasy vẽ lại (20/11/2026, 01:30-03:20)

Branch `worktree-agent-accdfe050fefa5331`, merge thẳng (fast-forward) vào `ro-fantasy-heroes`. Exe `D:\temp\player-mobs\Zenonia.exe` (bản sao `D:\temp\zu-copy-mobs`, 0 lỗi). Đã đọc `CLAUDE.md`, `docs/sprite-rules.md` (quy tắc + Sổ lỗi phần quái / ChatGPT), báo cáo Hydra v2, brief ChatGPT, `docs/pending-tasks.md`.

## Kết quả

- **XONG 27/40**: meadowSlime, hornRabbit, thornBoar, frostSlime, iceGoblin, snowFox, forestWolf, meadowMantis, thornSprite, brambleTreant, iceImp, frostBear, iceWraith, frostbiteYeti, crystalGolem, spectralWolf, highlandIbex, dustscaleLizard, cliffHarpy, stonebackGuardian, duneScarab, sandRaider, amberWisp, giantScorpion, sandWraith, rockBasilisk, duneGolem. Mỗi con: commit riêng, ảnh trước/sau xem từng frame, chụp trong game cạnh Warrior.
- **CHƯA 13**: bogSlime, mireToad, marshStalker, ruinWisp; boss map thornfangAlpha, thornheartTreant, frostfangKing, glacierWarden, ridgehornBehemoth, duneDevourer, mirefangHydra; boss truy nã rimeclawTyrant, glacialColossus. Lý do: cả 2 tài khoản hết lượt vẽ.
- Pet dùng chung sheet quái nên pet của các quái trên cũng đổi (chưa chụp màn pet).

## Cách làm

- Mỗi quái 2 ảnh ChatGPT ngang 1536x1024, 4 cột x 3 hàng (xuống, lên, phải); hàng trái = lật hàng phải. Gửi kèm frame cũ xếp sẵn theo bố cục (`ArtLibrary/monsters/ro-soft/ref/`). Tin đầu chat gửi 3 ảnh tông (hero mẫu, bảng style, crop đám đông đánh Hydra Queen). Mọi prompt có câu tông RO Soft Fantasy, nền #00FF00 (quái xanh dương / tím / nâu) hoặc #FF00FF (quái xanh lá), không bóng, không caro.
- Tool mới `Tools/build-ro-soft-monsters.py` (`ref`, `build`, `check`), dùng lại key / tách ô / đặt chân của tool meadow: tự nhận màu nền, giữ ô 192/256, chân, và **chiều cao thân của sheet cũ** (`ro-soft/heights.json`) nên cỡ trong game, thanh máu, hitbox không đổi. Thêm `HOVER` (quái bay nhấc khỏi đường chân) và `ATK_MATCH` (sheet đánh bị vẽ nhỏ hơn: tỉ lệ riêng khớp frame "về thế thủ").
- Code: chỉ thêm cờ chụp `--dummy-types` (`DebugCapture.cs`). Không đổi số liệu, không cần build lại zone server.

## Ảnh (`docs/reference/monsters-ro-soft-redraw/`)

- `<id>-before-after.png`: trên là sheet cũ, dưới là mới, đủ 4 hướng x 4 frame đi + đánh, hero cùng tỉ lệ, vạch chân đỏ.
- `ingame-batch1..6-*.jpg`: chụp trong game trên mặt hồ Frost Hollow (`--tp 40,30 --dummies 6 --dummy-types ...`). Cỡ quái so với hero giống bản cũ. Ice Imp bị quái khác che trong ảnh chụp.

## Tài khoản / lượt vẽ

- Tan Pham: hết lượt từ trước (tới 09:17 20/11), 0 ảnh. Chat thử: https://chatgpt.com/c/6aff40c8-c3bc-83ec-a656-39f3f1d0233f
- Tay (Tân Korris): **54 lượt**, hết lượt 03:15, có lại 21:32 20/11. Chat: https://chatgpt.com/c/6aff410c-b5c4-83ec-8e5a-00ce76667f4a. 2 lần A/B (Thorn Sprite đánh chọn ảnh 1, Frost Bear đánh chọn ảnh 2). Không phải vẽ lại lần nào.

## Lỗi (đã ghi Sổ lỗi `docs/sprite-rules.md`)

- Sheet đánh nhỏ hơn sheet đi 5-15 %: sửa bằng `ATK_MATCH` + câu prompt mới.
- Quái bay mất độ lơ lửng: `HOVER`.
- Còn để (không tốn lượt vẽ lại): Amber Wisp hàng đánh xuống bắn lửa sang phải; Sand Raider đi 1 dao, đánh 2 dao; vệt gió trắng ra hồng trên nền magenta (Rock Basilisk); vài sheet đánh mặt cau có hơn sheet đi (Yeti, Scarab).

## Kiểm tra

- `dotnet test`: ZenoniaSkills 452, ZenoniaNet 455, đều đạt.

## Câu hỏi

1. Độ "dễ thương": Forest Wolf, Rock Basilisk, Thorn Boar vẫn hơi dữ; có muốn vẽ lại mặt hiền hơn kiểu Poring không?
2. Boss truy nã Rimeclaw / Glacial: vẽ lại cả sheet `-cast` (4 tư thế skill) hay chỉ đi + đánh và giữ cast cũ?
3. Khi có lượt: dùng Tan Pham (09:17) cho 13 con còn lại được không, hay chờ Tay (21:32)?

# Phase 06: Kiểm tra bằng build bản sao + ảnh chụp

## Context
- Không build / batch vào project đang mở. Dùng `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player\Zenonia.exe` (mirror Scripts/Plugins/Editor/Resources, bỏ .meta; ảnh nền mới được `ResourceTextureImportSettings` import trong bản sao).
- Cờ chụp (`Assets/Scripts/DebugCapture.cs`): `--map <id>`, `--class <c>`, `--shots t1,t2`, `--shot-path`, `--tp tx,ty`, `--zoom`, `--solids` (bật lớp tô ô đặc), `--god`.
- Test mạng: `Tools/net-persist-test.ps1`, `Tools/net-local-test.ps1`.

## Overview
Tự xem ảnh, không tin báo cáo. Nói rõ phần chưa xem tận mắt trong game.

## Implementation steps
1. Build bản sao (thường, không `-Release`).
2. Chụp (mỗi lệnh `--shots 3,5 --shot-path C:\temp\shots\<tên>`), class warrior:
   - `--map thatch-village` (start quảng trường) và `--map thatch-village --solids`.
   - `--map thatch-village --tp <ô trước cửa nhà>` x2 nhà: so cỡ hero với cửa.
   - `--map thatch-village --tp 2,7` (cạnh cổng tây) và `--tp 33,11` (cạnh cổng đông): thấy đèn cổng.
   - `--map willowbrook-meadows` / `--solids` / `--tp 3,11` (cổng tây) / `--zoom` nhỏ để thấy cả map, đếm quái, không boss.
   - `--map village --tp 33,10`: cổng đông Oakhaven có đèn, đường liền mạch.
3. Đi bộ thật (bản build, offline): Oakhaven -> đông -> Thatchwick -> đông -> Willowbrook -> về -> về. Kiểm minimap đúng nền, nhạc đổi, đứng ở ô đến không nảy lại.
4. Lưu, thoát, mở lại ở Thatchwick và Willowbrook: vào lại đúng map.
5. Test mạng nếu môi trường có: `Tools/net-persist-test.ps1` PASS ("walked from oakhavenVillage", save = frostHollow).
6. Ghi kết quả + phần chưa xem vào `docs/pending-tasks.md` (dòng "Map mới cạnh Green Village"); không ghi vào memory.
7. Không đụng `Assets/Resources/Data/dev-start.json`.

## Todo
- [ ] Build bản sao
- [ ] Ảnh Thatchwick (thường, solids, cửa nhà, 2 cổng)
- [ ] Ảnh Willowbrook (thường, solids, cổng, toàn map)
- [ ] Ảnh cổng đông Oakhaven
- [ ] Đi bộ khứ hồi + save/load
- [ ] net-persist-test
- [ ] Cập nhật `docs/pending-tasks.md`

## Success criteria
- Đã xem từng ảnh: lớp `--solids` khớp bóng nhà/giếng/sạp/nước/cây; không đi lên mái; cửa nhà cao hơn hero và không quá to.
- Đi khứ hồi 4 lần chuyển map không lỗi trong log người chơi.
- Willowbrook: chỉ frostSlime/iceGoblin/spectralWolf, không boss, không quái cạnh cổng.

## Risks
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Bản sao cũ thiếu Library -> build lâu | TB x Thấp | chạy nền, chờ |
| Màn hình HUD che hàng 21-23 khác dự kiến trên tỉ lệ màn khác | Thấp x TB | chụp thêm 1 ảnh ở `--tp` hàng 20 |

## Rollback
Revert commit phase 2-4; backdrop và script không ảnh hưởng runtime.

## Câu hỏi còn mở
1. Tên hiển thị làng: "Thatchwick Village" được không (id `thatchVillage` giữ nguyên)?
2. "Lv 1-5": chấp nhận chỉ Lv1-2 (frostSlime, iceGoblin, spectralWolf), hay thêm MonsterDef Lv3-5 tô màu lại từ sprite cũ (việc riêng, thêm loot/exp)?
3. Willowbrook cấp độ trùng Frost Hollow (cũng Lv1-2): có cần chỉnh Frost Hollow lên không? (ngoài phạm vi)
4. Nhạc riêng cho 2 map: tạm dùng lại `oakhaven-village` / `frost-hollow`, có kế hoạch nhạc mới không?
5. Đường bắc/nam của Thatchwick và đường đông Willowbrook: chặn ở mép (đề xuất) hay để sẵn cổng cho map sau?
6. Có deploy server online ngay sau khi xong không (cần build lại zone server + client cùng version 14)?

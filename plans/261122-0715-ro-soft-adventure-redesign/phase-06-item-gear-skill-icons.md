# Phase 06: Icon item / trang bị / skill

Ưu tiên: P2. Trạng thái: chưa. Đọc hết `docs/sprite-rules.md`.

## Hiện có
- Icon trang bị 10 bậc theo class `{armor,gloves,boots}-<class>-N`, vũ khí `weapon-<class>-N`, `offhand-*`, dây chuyền (`Tools/build-class-gear-icons.py`, `build-*-tier-*.py`).
- Icon tiêu hao / nguyên liệu (`LootSprites`), icon skill RO Soft Fantasy 8 đĩa / class (`Tools/build-soft-fantasy-skill-icons.py`).

## Việc
1. Đổi tông tool tạm cho icon item (giảm bão hoà màu quá rực 10-20 %, viền nâu #59463A) để khớp ô giấy ngà ngay.
2. ChatGPT vẽ lại theo nhóm (2 x 5 / ảnh): vũ khí + giáp thép xám ấm / da / đồng, không phủ vàng bóng; tiêu hao; nguyên liệu.
3. Icon skill: GIỮ màu hệ (luật RO trailer / class), chỉ đổi viền đĩa sang đồng mảnh + nền ấm; vẽ lại bằng prompt cũ `skill-icons-src/prompt-soft-fantasy-icons.md` + khối Adventure.

## Thành công
Lưới túi đồ đọc rõ ở 40-48 px, viền hiếm không lấn vật phẩm (checklist mục 8).

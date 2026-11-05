# Phase 07: Cài sheet 3 quái băng

## Context
- Đọc trước `docs/sprite-weapon-rules.md` (tự xem từng frame, Sổ lỗi).
- Mẫu: `Tools/build-meadow-monsters.py` (291 dòng: key #FF00FF, cắt theo ô, một scale mỗi quái, baseline chung) commit `c60a43ce`; sheet ra `Assets/Resources/Sprites/monsters/<type>.png`, `<type>-attack.png`.
- MonsterDef 3 loại đã có từ P2 với `FallbackSheet` (`Monster.Visuals.cs:20-21`: có sheet thật dùng được thì tự bỏ fallback).

## Overview
Priority P2. Độc lập, chạy khi có art A9-11 (Yeti đang vẽ). Không chặn P6/P8 (fallback tô màu chạy được).

## Steps
1. `Tools/build-ice-monsters.py` (mới, < 100 dòng): import hàm từ `build-meadow-monsters.py` (importlib), chỉ khai báo bảng 3 quái (file nguồn `ArtLibrary/monsters/ice/<name>-{walk,attack}-chatgpt.png`, scale, baseline). Nếu cần sửa hàm chung -> sửa tại `build-meadow-monsters.py`, chạy lại quái đồng cỏ, so ảnh không đổi.
2. Chạy, xuất preview `plans/.../visuals/ice-<type>-preview.png` + so cỡ với frostSlime/iceGoblin/spectralWolf/glacierWarden (`meadow-scale-compare.png` là mẫu).
3. Chỉnh `bodyHeightPx`, `RenderScale` trong MonsterDef theo sheet thật; giữ hoặc xoá `FallbackSheet` (giữ cũng vô hại).
4. Chụp trong game từ bản sao (`--map frost-hollow --tp <cụm>`), xem walk/attack 4 hướng.

## Todo
- [ ] script  - [ ] 3 sheet + preview  - [ ] chỉnh MonsterDef  - [ ] chụp trong game  - [ ] lỗi mới -> Sổ lỗi

## Success criteria
Mỗi quái 4 hướng walk + attack, chân đúng baseline, không viền hồng; cỡ hợp lý cạnh quái cũ; trong game không còn dùng fallback.

## Risk
Viền magenta / ô caro (Sổ lỗi) - TB x Thấp: despill như prop sheet. Cỡ crystalGolem vượt ô 192 - TB x Thấp: RenderScale như brambleTreant.

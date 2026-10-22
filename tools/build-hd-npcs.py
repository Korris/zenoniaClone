"""Normalizes the hand-painted Oakhaven NPC sheets (assets/hd/map/Oakhaven/*.png) into HD idle
sheets + dialog portraits the engine can slice, reusing build-hd-hero.py's alpha/grid/resize helpers.

Raw sheets (as generated): one PNG per NPC, 4 columns x 3 rows. Columns are idle frames in time
order; rows are facings: 0 = front (DOWN), 1 = turned to screen-LEFT, 2 = turned to screen-RIGHT.
There is no rear (UP) pose -- the engine falls back to the front row for it (see prop-renderer.js).
Cells are not an exact grid (characters even touch across rows on some sheets), so grid lines are
snapped to the emptiest gaps the same way the hero script does. Real-alpha sheets get the haze
threshold; a sheet that comes back fully opaque with a painted checkerboard gets color-keyed
(both handled by build-hd-hero.py's normalize_alpha).

Output (assets/hd/npcs/normalized/):
- <npcId>.png        384x288 = 4 cols x 3 rows of 96x96 frames (24 world units * pixelsPerUnit 4,
                     same frame contract as the HD hero so NPCs stand at hero scale), feet on the
                     bottom row, body scaled so the front/first frame is TARGET_BODY_HEIGHT px tall.
- <npcId>_face.png   152x152 portrait (head + shoulders crop of the front/first frame) for dialog.
- contact-sheet.png, README.md

Usage: python tools/build-hd-npcs.py [src_dir] [out_dir]
"""
import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERO_SCRIPT = Path(__file__).with_name('build-hd-hero.py')
spec = importlib.util.spec_from_file_location('build_hd_hero', HERO_SCRIPT)
hero = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hero)
# Fail fast (before any output is written) if the hero script's helpers this file relies on get
# renamed -- extract a shared module instead if a third consumer ever appears.
_REQUIRED_HELPERS = ('normalize_alpha', 'grid_lines', 'tight_bbox', 'downscale', 'place_in_frame', 'FRAME', 'FACE_SIZE', 'TARGET_BODY_HEIGHT')
_missing = [name for name in _REQUIRED_HELPERS if not hasattr(hero, name)]
if _missing:
    sys.exit(f'build-hd-hero.py no longer exports {_missing}; update build-hd-npcs.py')

SRC_DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/hd/map/Oakhaven')
OUT_DIR = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/hd/npcs/normalized')

ROWS = ['down', 'left', 'right']   # raw sheet row order -> engine facing (no 'up' pose exists)
COLS = 4                           # idle frames per row
FRAME = hero.FRAME                 # 96 px = 24 world units * 4
FACE_SIZE = hero.FACE_SIZE         # 152, same as hero_face.png
TARGET_BODY_HEIGHT = hero.TARGET_BODY_HEIGHT  # 82 px -> NPCs stand at the hero's height
FACE_CROP_FRACTION = 0.5           # top half of the front frame's bbox = head + shoulders

# npcId (src/data/npc-definitions.js) -> raw file name, plus optional body-height override
# (the child is drawn shorter so it reads as a child next to the adults).
NPCS = {
    'elderRowan': 'Elder_Rowan.png', 'villagerMia': 'Mia.png', 'hunterLena': 'Hunter Lena.png',
    'merchantDale': 'Merchant Dale.png', 'fishermanRoy': 'Fisherman Roy.png', 'carpenterBrol': 'Carpenter Brol.png',
    'farmerGuri': 'Farmer Guri.png', 'guardRook': 'Guard Rook.png', 'childTim': 'Child Tim.png', 'healerNina': 'nina.png',
}
BODY_HEIGHT_OVERRIDE = {'childTim': 70}


def split_sheet(img):
    """3 rows x COLS cells, grid lines snapped per row band (same approach as the hero script)."""
    arr = hero.normalize_alpha(img)
    mask = arr[..., 3] > 0
    row_bounds = hero.grid_lines(mask, axis=0, count=len(ROWS))
    cells = []
    for r in range(len(ROWS)):
        y0, y1 = row_bounds[r], row_bounds[r + 1]
        col_bounds = hero.grid_lines(mask[y0:y1], axis=1, count=COLS)
        cells.append([arr[y0:y1, col_bounds[c]:col_bounds[c + 1]] for c in range(COLS)])
    return cells


def build_face(front_frame_arr):
    """Head + shoulders of the front/first frame, centered in a FACE_SIZE square (not bottom-pivoted)."""
    h = front_frame_arr.shape[0]
    head = front_frame_arr[: int(h * FACE_CROP_FRACTION)]
    cropped = Image.fromarray(head, 'RGBA')
    scale = min((FACE_SIZE - 4) / cropped.width, (FACE_SIZE - 4) / cropped.height)
    small = hero.downscale(cropped, scale)
    face = Image.new('RGBA', (FACE_SIZE, FACE_SIZE), (0, 0, 0, 0))
    face.paste(small, ((FACE_SIZE - small.width) // 2, (FACE_SIZE - small.height) // 2), small)
    return face


def build_npc(npc_id, file_name):
    path = SRC_DIR / file_name
    if not path.exists():
        print(f'WARN skipping {npc_id}: {path} not found')
        return None, None
    cells = split_sheet(Image.open(path).convert('RGBA'))
    first = hero.tight_bbox(cells[0][0])
    if first is None:
        raise RuntimeError(f'{file_name}: front frame 0 is empty')
    target = BODY_HEIGHT_OVERRIDE.get(npc_id, TARGET_BODY_HEIGHT)
    scale = target / float(first.shape[0])
    sheet = Image.new('RGBA', (COLS * FRAME, len(ROWS) * FRAME), (0, 0, 0, 0))
    for r in range(len(ROWS)):
        for c in range(COLS):
            bbox_arr = hero.tight_bbox(cells[r][c])
            if bbox_arr is None:
                print(f'WARN {file_name}: empty frame row {ROWS[r]} col {c}')
                continue
            frame = hero.place_in_frame(hero.downscale(Image.fromarray(bbox_arr, 'RGBA'), scale))
            sheet.paste(frame, (c * FRAME, r * FRAME))
    print(f'{npc_id}: {file_name} scale {scale:.4f} (front frame {first.shape[0]}px -> {target}px)')
    return sheet, build_face(first)


def validate(npc_id, sheet):
    arr = np.array(sheet)[..., 3]
    failures = []
    for r in range(len(ROWS)):
        for c in range(COLS):
            cell = arr[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME]
            if cell.sum() == 0:
                failures.append(f'{npc_id}: frame {ROWS[r]}/{c} empty')
            elif cell[-2:].sum() == 0:
                failures.append(f'{npc_id}: frame {ROWS[r]}/{c} feet not on the bottom row')
    return failures


def contact_sheet(sheets, faces, path):
    cell_w = COLS * FRAME + 8 + FACE_SIZE
    out = Image.new('RGBA', (cell_w, len(sheets) * (len(ROWS) * FRAME + 20)), (48, 48, 64, 255))
    d = ImageDraw.Draw(out)
    y = 0
    for npc_id, sheet in sheets.items():
        d.text((4, y + 2), f'{npc_id} {sheet.width}x{sheet.height}', fill=(255, 255, 0, 255))
        out.paste(sheet, (0, y + 16), sheet)
        out.paste(faces[npc_id], (COLS * FRAME + 8, y + 16), faces[npc_id])
        y += len(ROWS) * FRAME + 20
    out.save(path)


def write_readme(sheets):
    rows = '\n'.join(f'| {npc_id} | `{NPCS[npc_id]}` | {s.width}x{s.height} |' for npc_id, s in sheets.items())
    (OUT_DIR / 'README.md').write_text(f"""# HD NPC sheets (normalized)

Generated from the hand-painted sheets in `assets/hd/map/Oakhaven/*.png` by `tools/build-hd-npcs.py`
(`npm run hd:npcs`). Same contract as the HD hero: 96x96 frames = 24 world units x pixelsPerUnit 4,
feet on the bottom row, body ~{TARGET_BODY_HEIGHT}px tall (child {BODY_HEIGHT_OVERRIDE['childTim']}px).

| npcId | source | sheet |
|---|---|---|
{rows}

- Layout: 4 idle frames per row; rows `0 = DOWN (front)`, `1 = LEFT`, `2 = RIGHT`. No rear pose:
  the engine draws the front row when an NPC faces up.
- `<npcId>_face.png` ({FACE_SIZE}x{FACE_SIZE}): dialog portrait, head+shoulders of the front frame, centered.
- Wired through `src/graphics/asset-atlas-hd-npcs.js`; a missing file falls back to the pixel-art
  NPC set (`NPC_SHEETS`).
""", encoding='utf-8')


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sheets, faces, failures = {}, {}, []
    for npc_id, file_name in NPCS.items():
        sheet, face = build_npc(npc_id, file_name)
        if sheet is None:
            continue
        sheet.save(OUT_DIR / f'{npc_id}.png')
        face.save(OUT_DIR / f'{npc_id}_face.png')
        sheets[npc_id], faces[npc_id] = sheet, face
        failures += validate(npc_id, sheet)
    if not sheets:
        print('ERROR: no NPC sheets produced (nothing found in', SRC_DIR, ')')
        sys.exit(1)
    contact_sheet(sheets, faces, OUT_DIR / 'contact-sheet.png')
    write_readme(sheets)
    for msg in failures:
        print('FAIL', msg)
    print('RESULT:', 'OK' if not failures else f'{len(failures)} failure(s)')
    sys.exit(1 if failures else 0)


if __name__ == '__main__':
    main()

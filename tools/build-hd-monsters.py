"""Normalizes the hand-painted Frost Hollow monster sheets (assets/hd/monsters/raw/<type>.png) into
HD walk-cycle sheets the engine slices, reusing build-hd-hero.py's alpha/grid/resize helpers the
same way build-hd-npcs.py does.

Raw sheets (ChatGPT, alpha recovered from black/white captures): one PNG per monster type, 4 columns
x 3 rows. Columns are the 4 frames of a looping walk/hover cycle; rows are facings: 0 = front
(DOWN), 1 = turned to screen-LEFT, 2 = turned to screen-RIGHT. No rear (UP) pose -- the renderer
falls back to the front row for it (monster-renderer.js), like the HD NPCs.

Output (assets/hd/monsters/normalized/):
- <type>.png        384x288 = 4 cols x 3 rows of 96x96 frames (24 world units * pixelsPerUnit 4, the
                    HD hero/NPC frame contract), feet on the bottom row, body scaled so the front/first
                    frame is BODY_HEIGHT[type] px tall (monsters are a touch shorter than the 82px hero).
- contact-sheet.png, README.md

Usage: python tools/build-hd-monsters.py [src_dir] [out_dir]
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
_REQUIRED_HELPERS = ('normalize_alpha', 'grid_lines', 'tight_bbox', 'downscale', 'place_in_frame', 'FRAME')
_missing = [name for name in _REQUIRED_HELPERS if not hasattr(hero, name)]
if _missing:
    sys.exit(f'build-hd-hero.py no longer exports {_missing}; update build-hd-monsters.py')

SRC_DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/hd/monsters/raw')
OUT_DIR = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/hd/monsters/normalized')

ROWS = ['down', 'left', 'right']   # raw sheet row order -> engine facing (no 'up' pose exists)
COLS = 4                           # walk-cycle frames per row
FRAME = hero.FRAME                 # 96 px = 24 world units * 4

# monster type (src/data/monster-definitions.js) -> front-frame body height in px (hero is 82).
BODY_HEIGHT = {'frostSlime': 52, 'iceGoblin': 76, 'spectralWolf': 70}
# Sheets whose screen-LEFT row was painted facing right too (ChatGPT ignored the row order): the left
# row is rebuilt by mirroring the right row.
MIRROR_LEFT_FROM_RIGHT = {'spectralWolf'}


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


def build_monster(mtype):
    path = SRC_DIR / f'{mtype}.png'
    if not path.exists():
        print(f'WARN skipping {mtype}: {path} not found')
        return None
    cells = split_sheet(Image.open(path).convert('RGBA'))
    first = hero.tight_bbox(cells[0][0])
    if first is None:
        raise RuntimeError(f'{path}: front frame 0 is empty')
    scale = BODY_HEIGHT[mtype] / float(first.shape[0])
    sheet = Image.new('RGBA', (COLS * FRAME, len(ROWS) * FRAME), (0, 0, 0, 0))
    if mtype in MIRROR_LEFT_FROM_RIGHT:
        cells[ROWS.index('left')] = [c[:, ::-1] for c in cells[ROWS.index('right')]]
    for r in range(len(ROWS)):
        for c in range(COLS):
            bbox_arr = hero.tight_bbox(cells[r][c])
            if bbox_arr is None:
                print(f'WARN {path}: empty frame row {ROWS[r]} col {c}')
                continue
            frame = hero.place_in_frame(hero.downscale(Image.fromarray(bbox_arr, 'RGBA'), scale))
            sheet.paste(frame, (c * FRAME, r * FRAME))
    print(f'{mtype}: scale {scale:.4f} (front frame {first.shape[0]}px -> {BODY_HEIGHT[mtype]}px)')
    return sheet


def validate(mtype, sheet):
    arr = np.array(sheet)[..., 3]
    failures = []
    for r in range(len(ROWS)):
        for c in range(COLS):
            cell = arr[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME]
            if cell.sum() == 0:
                failures.append(f'{mtype}: frame {ROWS[r]}/{c} empty')
            elif cell[-2:].sum() == 0:
                failures.append(f'{mtype}: frame {ROWS[r]}/{c} feet not on the bottom row')
    return failures


def contact_sheet(sheets, path):
    out = Image.new('RGBA', (COLS * FRAME, len(sheets) * (len(ROWS) * FRAME + 20)), (48, 48, 64, 255))
    d = ImageDraw.Draw(out)
    y = 0
    for mtype, sheet in sheets.items():
        d.text((4, y + 2), f'{mtype} {sheet.width}x{sheet.height}', fill=(255, 255, 0, 255))
        out.paste(sheet, (0, y + 16), sheet)
        y += len(ROWS) * FRAME + 20
    out.save(path)


def write_readme(sheets):
    rows = '\n'.join(f'| {t} | {BODY_HEIGHT[t]}px | {s.width}x{s.height} |' for t, s in sheets.items())
    text = (
        '# HD monster sheets (normalized)\n\n'
        'Generated from the ChatGPT sheets in `assets/hd/monsters/raw/*.png` by `tools/build-hd-monsters.py`\n'
        '(`npm run hd:monsters`). Same frame contract as the HD hero/NPCs: 96x96 frames = 24 world units x\n'
        'pixelsPerUnit 4, feet on the bottom row. Layout: 4 columns = walk/hover cycle frames, rows = front\n'
        '(DOWN), LEFT, RIGHT (no rear pose: the renderer shows the front row for UP).\n\n'
        '| type | body height | sheet |\n|------|-------------|-------|\n' + rows + '\n\n'
        'Sliced by `src/graphics/sprite-registry.js` into `registry.monstersHd[type]` (config in\n'
        '`src/graphics/asset-atlas-hd-monsters.js`).\n'
    )
    (OUT_DIR / 'README.md').write_text(text, encoding='utf-8')


if __name__ == '__main__':
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sheets = {}
    failures = []
    for mtype in BODY_HEIGHT:
        sheet = build_monster(mtype)
        if sheet is None:
            continue
        sheets[mtype] = sheet
        sheet.save(OUT_DIR / f'{mtype}.png')
        failures += validate(mtype, sheet)
    contact_sheet(sheets, OUT_DIR / 'contact-sheet.png')
    write_readme(sheets)
    if failures:
        sys.exit('VALIDATION FAILED:\n  ' + '\n  '.join(failures))
    print(f'OK: {len(sheets)} monster sheets -> {OUT_DIR}')

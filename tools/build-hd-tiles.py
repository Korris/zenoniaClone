"""Normalizes raw HD ground-tile sheets (assets/hd/tiles/*.png) into seamless 64x64-cell sheets.

The raw ChatGPT sheets are illustrations of a tile grid: every cell carries a painted panel
border, adjacent cells are unrelated paintings and sibling variants differ in brightness, so
tiled 1:1 in-game they show a conspicuous square lattice. This build removes that at the
texture level (see tools/hd_tile_texture_utils.py for the individual steps):

- every cell is cropped with an inset margin (drops the baked border), LANCZOS-resized to
  64x64 = 16 world units * pixelsPerUnit (4) and seam-healed on the axes it repeats along;
- grass: 4 plain variants are brightness-matched and healed into ONE 2x2 seamless macro
  texture; the renderer picks the quadrant by tile position (HD_TILES `macro`), so the
  repeat period is 2x2 tiles with no visible cell edges. Flower variants are the SAME
  quadrants plus the painted flowers lifted off the raw flower cells, so a flower tile
  never introduces a texture seam against the plain grass around it;
- path / water auto-tile blocks become RGBA: grass pixels in the painted transition cells
  go transparent (hue mask) so the seamless base grass drawn underneath shows through
  instead of a second, non-matching painted grass. The path also gets a 2x2 center macro
  and 4 inner-corner grass wedges (concave corners) for the renderer;
- plaza.png: a 2x2 seamless stone macro cut from the castle floor cells, drawn through the
  path block's alpha silhouette (stone-on-grass transitions) by tilemap-renderer.js;
- portal.png keeps its real alpha (haze thresholded, premultiplied resize), as before.

Only files that exist in SRC_DIR are processed; missing ones are skipped with a warning.
Usage: python tools/build-hd-tiles.py [src_dir] [out_dir]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).parent))
from hd_tile_texture_utils import (  # noqa: E402
    composite, equalize_means, flips, flower_decals, heal_seams, inner_corner_masks, macro_from_cells,
    material_alpha, open_alpha, premultiplied_resize, split_cells, to_f32, to_img, with_alpha,
)

SRC_DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/hd/tiles')
OUT_DIR = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/hd/tiles/normalized')

CELL = 64              # output cell size (px) = 16 world units * pixelsPerUnit (4)
FEATHER = 11           # seam-healing strip half-width (px at CELL resolution)
ALPHA_THRESHOLD = 48   # portal.png only: alpha <= this -> fully transparent
SIZE = (CELL, CELL)

# filename -> (cols, rows, inset margin as a fraction of the cell: the painted panel border).
GRIDS = {
    'grass.png': (4, 2, 0.035),
    'path.png': (3, 3, 0.025),
    'water.png': (3, 4, 0.02),
    'floors.png': (4, 2, 0.03),
    'walls.png': (5, 1, 0.02),
    'portal.png': (3, 1, 0.0),
}

# Cell-map notes written into the generated README (kept next to GRIDS so docs can't drift).
CELL_NOTES = {
    'grass.png': [
        'row0: ONE seamless 2x2 grass macro, quadrants in macro order (x0y0, x1y0, x0y1, x1y1); the renderer',
        'picks col = tx % 2, row = ty % 2 so any 2x2 group of tiles is continuous',
        'row1: the same 4 quadrants with the painted flowers composited on top (flower tiles, same macro order)',
    ],
    'path.png': [
        'rows0-2 cols0-2: 3x3 dirt-on-grass auto-tile block, RGBA (grass = transparent, base grass shows through)',
        'row3 cols0-3: seamless 2x2 dirt macro for open road centers (macro order, picked by tile position)',
        'col4 rows0-3: inner-corner grass wedge masks TL, TR, BL, BR (alpha only) for concave road corners',
        'rows4-5: pieces the 3x3 block lacks, keyed by OPEN sides -- (0,4) TB vertical pipe, (1,4) LR horizontal',
        'pipe, (2,4) T cap, (3,4) B cap, (4,4) L cap, (0,5) R cap, (1,5) island (RGBA, synthesized from edge alphas)',
    ],
    'water.png': [
        'rows0-2: 3x3 water-on-grass auto-tile block (banks), RGBA (grass = transparent)',
        'row3: col0-1 = open-water animation variants (brightness-matched to the center), col2 = wooden bridge',
    ],
    'floors.png': ['row0: sand x2, castle stone floor x2 -- each pair is a seamless 2x1 macro', 'row1: cave rock floor x2, lava x2 (same)'],
    'walls.png': ['col0 cliff top (grass lip), col1 cliff rock face, col2 cliff bottom (shadow), col3 castle brick wall, col4 cave rock wall'],
    'portal.png': ['3 magic-circle animation frames (real alpha)'],
    'plaza.png': ['seamless 2x2 stone macro (castle floor cells + mirrored copies) for the Oakhaven plaza fill'],
}

# Macro quadrant order: index = (ty % 2) * 2 + (tx % 2) -> sheet cell positions in that order.
MACRO_ORDER = [(0, 0), (1, 0), (0, 1), (1, 1)]


def load(name):
    path = SRC_DIR / name
    if not path.exists():
        print(f'WARN skipping {name}: {path} not found')
        return None
    return Image.open(path).convert('RGBA')


def rgba_cell(cell, opaque=False):
    """Painted cell -> 64x64 RGBA float array; grass goes transparent unless `opaque`."""
    if opaque:
        return to_f32(cell.convert('RGB').resize(SIZE, Image.LANCZOS))
    return open_alpha(premultiplied_resize(with_alpha(cell.convert('RGB'), material_alpha(cell)), SIZE))


def paste(sheet, arr, col, row):
    sheet.paste(to_img(arr), (col * CELL, row * CELL))


def macro_quadrants(macro):
    return [macro[y * CELL:(y + 1) * CELL, x * CELL:(x + 1) * CELL] for x, y in MACRO_ORDER]


def build_grass(img, cols, rows, margin):
    cells = split_cells(img, cols, rows, margin)
    plain = equalize_means([to_f32(cells[(c, 0)].resize(SIZE, Image.LANCZOS)) for c in range(4)])
    quadrants = macro_quadrants(macro_from_cells(plain, 2, 2, CELL, FEATHER))
    sheet = Image.new('RGBA', (4 * CELL, 2 * CELL), (0, 0, 0, 0))
    for i, q in enumerate(quadrants):
        paste(sheet, q, i, 0)
        decals = flower_decals(cells[(i, 1)].resize(SIZE, Image.LANCZOS))
        paste(sheet, composite(q, decals), i, 1)
    return sheet


def heal_block(block):
    """Heal the 3x3 block cells on the axes they repeat along: center both, L/R up-down, T/B left-right."""
    for (c, r), arr in block.items():
        if (c, r) == (1, 1):
            block[(c, r)] = heal_seams(arr, CELL, FEATHER)
        elif c == 1:
            block[(c, r)] = heal_seams(arr, CELL, FEATHER, vertical=False)
        elif r == 1:
            block[(c, r)] = heal_seams(arr, CELL, FEATHER, horizontal=False)
    return block


# Pieces a 3x3 block lacks, keyed by which sides are OPEN (same family): 1-wide pipes, dead-end
# caps and a lone island. Sheet positions (col, row) must match HD_TILES.dirtBlock.shapes.
SHAPE_CELLS = {'TB': (0, 4), 'LR': (1, 4), 'T': (2, 4), 'B': (3, 4), 'L': (4, 4), 'R': (0, 5), '': (1, 5)}


SHAPE_GAIN = 1.6  # facing edge bands overlap in a 1-wide lane: push the product back to a solid core


def extra_shapes(block):
    """Synthesize the 7 missing pieces: center dirt with alpha = product of the CLOSED sides' edge
    alphas (T cell alpha is grass-at-top, etc.), so a 1-wide lane gets edges on both sides. The
    band widths stay those of the painted edge cells, so the pieces butt onto them without a step."""
    edge_alpha = {'T': block[(1, 0)][..., 3], 'B': block[(1, 2)][..., 3], 'L': block[(0, 1)][..., 3], 'R': block[(2, 1)][..., 3]}
    shapes = {}
    for open_sides, pos in SHAPE_CELLS.items():
        alpha = np.ones_like(edge_alpha['T'])
        for side, a in edge_alpha.items():
            if side not in open_sides:
                alpha = alpha * a / 255.0
        arr = block[(1, 1)].copy()
        arr[..., 3] = np.clip(alpha * SHAPE_GAIN, 0, 1) * 255
        shapes[pos] = open_alpha(arr)  # drops the flecks the painted flowers leave in the grass band
    return shapes


def build_path(img, cols, rows, margin):
    cells = split_cells(img, cols, rows, margin)
    block = heal_block({k: rgba_cell(v, opaque=(k == (1, 1))) for k, v in cells.items()})
    sheet = Image.new('RGBA', (5 * CELL, 6 * CELL), (0, 0, 0, 0))
    for (c, r), arr in block.items():
        paste(sheet, arr, c, r)
    for i, q in enumerate(macro_quadrants(macro_from_cells(flips(block[(1, 1)]), 2, 2, CELL, FEATHER))):
        paste(sheet, q, i, 3)
    for i, wedge in enumerate(inner_corner_masks(block[(1, 0)][..., 3], block[(0, 1)][..., 3])):
        paste(sheet, wedge, 4, i)
    for (c, r), arr in extra_shapes(block).items():
        paste(sheet, arr, c, r)
    return sheet


def build_water(img, cols, rows, margin):
    cells = split_cells(img, cols, rows, margin)
    block = heal_block({k: rgba_cell(v, opaque=(k == (1, 1))) for k, v in cells.items() if k[1] < 3})
    anim = [heal_seams(rgba_cell(cells[(c, 3)], opaque=True), CELL, FEATHER) for c in (0, 1)]
    center, *anim = equalize_means([block[(1, 1)], *anim])
    block[(1, 1)] = center
    sheet = Image.new('RGBA', (3 * CELL, 4 * CELL), (0, 0, 0, 0))
    for (c, r), arr in block.items():
        paste(sheet, arr, c, r)
    for c, arr in enumerate(anim):
        paste(sheet, arr, c, 3)
    paste(sheet, rgba_cell(cells[(2, 3)], opaque=True), 2, 3)
    return sheet


def build_floors(img, cols, rows, margin):
    cells = split_cells(img, cols, rows, margin)
    sheet = Image.new('RGBA', (4 * CELL, 2 * CELL), (0, 0, 0, 0))
    pairs = {}
    for r in range(rows):
        for c0 in (0, 2):
            pair = equalize_means([rgba_cell(cells[(c0 + i, r)], opaque=True) for i in (0, 1)])
            macro = macro_from_cells(pair, 2, 1, CELL, FEATHER)
            pairs[(c0, r)] = [macro[:, :CELL], macro[:, CELL:]]
            for i, arr in enumerate(pairs[(c0, r)]):
                paste(sheet, arr, c0 + i, r)
    return sheet, pairs


def build_plaza(stone_pair):
    """2x2 seamless stone macro: the two castle-floor cells + mirrored copies, healed together."""
    c0, c1 = stone_pair
    macro = macro_from_cells([c0, c1, c1[:, ::-1], c0[:, ::-1]], 2, 2, CELL, FEATHER)
    sheet = Image.new('RGBA', (2 * CELL, 2 * CELL), (0, 0, 0, 0))
    for i, q in enumerate(macro_quadrants(macro)):
        paste(sheet, q, i % 2, i // 2)
    return sheet


def build_walls(img, cols, rows, margin):
    cells = split_cells(img, cols, rows, margin)
    sheet = Image.new('RGBA', (cols * CELL, CELL), (0, 0, 0, 0))
    for c in range(cols):
        arr = rgba_cell(cells[(c, 0)], opaque=True)
        paste(sheet, heal_seams(arr, CELL, FEATHER, vertical=(c >= 3)), c, 0)  # cliffs tile left-right only
    return sheet


def build_portal(img, cols, rows, _margin):
    def threshold(arr):
        arr[..., 3] = np.where(arr[..., 3] > ALPHA_THRESHOLD, arr[..., 3], 0)
        return arr
    cells = split_cells(img, cols, rows, 0)
    sheet = Image.new('RGBA', (cols * CELL, CELL), (0, 0, 0, 0))
    for c in range(cols):
        arr = threshold(premultiplied_resize(threshold(to_f32(cells[(c, 0)])), SIZE))
        paste(sheet, arr, c, 0)
    return sheet


def contact_sheet(sheets, path):
    rows = list(sheets.items())
    w = max(s.width for _, s in rows) + 8
    h = sum(s.height + 20 for _, s in rows)
    out = Image.new('RGBA', (w, h), (48, 48, 64, 255))
    d = ImageDraw.Draw(out)
    y = 0
    for name, s in rows:
        d.text((4, y + 2), f'{name} {s.width}x{s.height}', fill=(255, 255, 0, 255))
        y += 16
        out.paste(s, (4, y), s)
        y += s.height + 4
    out.save(path)


def write_readme(sheets):
    lines = [
        '# HD ground tile sheets (normalized)', '',
        'Generated from the raw sheets in `assets/hd/tiles/*.png` by `tools/build-hd-tiles.py`',
        '(`npm run hd:tiles`). Every cell is cropped inside its painted panel border, LANCZOS-resized to',
        f'exactly {CELL}x{CELL}px = 16 world units * pixelsPerUnit (4) and seam-healed so it tiles without a',
        'visible lattice -- see the script docstring and `tools/hd_tile_texture_utils.py`. Matches `HD_TILES`',
        'in `src/graphics/asset-atlas.js`.', '', '## Cell map', '',
    ]
    for name, s in sheets.items():
        lines.append(f'### {name} ({s.width}x{s.height}, {s.width // CELL}x{s.height // CELL} cells)')
        lines.extend(f'- {note}' for note in CELL_NOTES[name])
        lines.append('')
    lines += [
        '## Rebuild', '', '```', 'python tools/build-hd-tiles.py', '# or: npm run hd:tiles', '```', '',
        'Missing raw files are skipped with a warning -- the set can grow one sheet at a time.',
    ]
    (OUT_DIR / 'README.md').write_text('\n'.join(lines), encoding='utf-8')


BUILDERS = {
    'grass.png': build_grass, 'path.png': build_path, 'water.png': build_water,
    'walls.png': build_walls, 'portal.png': build_portal,
}


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sheets = {}
    for name, (cols, rows, margin) in GRIDS.items():
        img = load(name)
        if img is None:
            continue
        if name == 'floors.png':
            sheets[name], pairs = build_floors(img, cols, rows, margin)
            sheets['plaza.png'] = build_plaza(pairs[(2, 0)])
        else:
            sheets[name] = BUILDERS[name](img, cols, rows, margin)
    if not sheets:
        print('ERROR: no sheets were produced (no input files found in', SRC_DIR, ')')
        sys.exit(1)
    for name, sheet in sheets.items():
        sheet.save(OUT_DIR / name)
        print(f'{name} {sheet.width}x{sheet.height}')
    contact_sheet(sheets, OUT_DIR / 'contact-sheet.png')
    print('contact-sheet.png written')
    write_readme(sheets)
    print('RESULT: OK')


if __name__ == '__main__':
    main()

"""Normalizes raw HD prop sheets (assets/hd/props/*.png) into clean per-prop-type canvases.

Same resolution-independence model as build-hd-tiles.py/build-hd-hero.py (pixelsPerUnit 4), but
props differ from both: raw sheets hold several unrelated objects (trees/rocks/buildings) that
are NOT laid out on an exact pixel grid, so cells can't be split evenly like the flat tile sheets
in build-hd-tiles.py -- items are placed irregularly with uneven gaps, and a few sheets have a
faint painted glow/halo around each object (same "haze" look as pine_tree.png's background glow).

Pipeline per sheet:
1. Haze-threshold alpha (<=ALPHA_THRESHOLD -> 0) ONCE for the whole sheet -- this both removes
   the glow halo from the final art AND makes column-gap segmentation below reliable (a halo that
   extends to the sheet edge would otherwise look like "no gap" between two items).
2. Split into per-object segments: most sheets segment automatically by finding fully-transparent
   gap columns between opaque regions (SHEETS[...]['mode'] == 'auto'); `pine_tree.png` and
   `round_tree.png` each hold exactly one object (the whole sheet, 'whole'); `desert.png`'s palm
   and cactus touch by a few stray antialiased pixels even above the haze threshold, so it uses an
   explicit manual column split instead ('manual').
3. Per segment: tight-crop to its real alpha bbox, then LANCZOS-resize (premultiplied alpha, to
   avoid dark/light edge fringing -- same trick as portal.png in build-hd-tiles.py) so the LARGER
   of width/height exactly fills that prop's target HD canvas (world size x4, PROP_TARGETS below),
   preserving aspect ratio ("fit by the limiting dimension"). Paste bottom-center onto a
   transparent canvas of that exact target size -- a tall narrow object (e.g. a fence post) ends
   up centered horizontally with some side padding rather than stretched to fill a square box.
4. Threshold alpha again after resize (LANCZOS can soften a hard cut back into a faint haze).

Two source trees are reused for two prop types each (pine/pineDark share pine_tree.png's only
tree; roundTree/bigTree share round_tree.png's only tree) -- both output files are identical
pixels, matching the two map prop names that have no distinct art of their own.

Only files that exist in SRC_DIR are processed; missing ones are skipped with a warning, so the
prop set can grow one sheet at a time. Safe to rerun.

Usage: python tools/build-hd-props.py [src_dir] [out_dir]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

SRC_DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/hd/props')
OUT_DIR = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/hd/props/normalized')

PIXELS_PER_UNIT = 4       # matches HD_TILES / HD_HERO -- 4 source px per world unit
ALPHA_THRESHOLD = 48      # faint glow/halo haze -> fully transparent (before AND after resize)
MIN_GAP_COLUMNS = 2       # ignore gaps narrower than this when auto-segmenting (noise guard)

# Target world size (w, h) per prop type -- MUST match width/height in src/world/prop-definitions.js
# (that file is the source of truth for gameplay; this table just needs to stay in sync with it).
PROP_TARGETS = {
    'pine': (32, 48), 'pineDark': (32, 48), 'roundTree': (48, 48), 'bigTree': (48, 48),
    'smallTree': (32, 32), 'deadTree': (32, 32),
    'bush': (16, 16), 'bush2': (16, 16), 'stones': (16, 16),
    'rockBrown': (48, 48), 'rockGrey': (48, 48), 'rockSmallGrey': (32, 32), 'rockSmallBrown': (32, 32),
    'stump': (32, 32),
    'palm': (32, 48), 'cactus': (16, 32), 'bones': (32, 16),
    'house': (64, 48), 'house2': (64, 48), 'house3': (64, 48),
    'fence': (16, 24), 'signpost': (16, 24), 'pillar': (16, 48), 'torch': (16, 32), 'crystal': (16, 32),
    # Optional combined source sheet for Oakhaven props. Current standalone landmark sprites are
    # authored by tools/build-village-landmarks.py; this sheet is not required for the build.
    'well': (32, 48), 'lanternPost': (16, 32), 'marketStall': (48, 48), 'cropPlot': (32, 16),
    'scarecrow': (16, 32), 'woodpile': (32, 32), 'campfire': (16, 16), 'barrel': (16, 16),
    'crate': (16, 16), 'rowboat': (32, 16),
}

# filename -> how to split it into per-object segments + the prop type(s) each segment maps to,
# left to right. `None` in `outputs` skips a segment with no map use (small_trees.png's middle
# "small pine" cell -- optional per the brief, not wired into any prop type).
SHEETS = {
    'pine_tree.png': {'mode': 'whole', 'outputs': ['pine', 'pineDark']},
    'round_tree.png': {'mode': 'whole', 'outputs': ['roundTree', 'bigTree']},
    'small_trees.png': {'mode': 'auto', 'outputs': ['smallTree', None, 'deadTree']},
    'bushes_rocks.png': {'mode': 'auto', 'outputs': ['bush', 'bush2', 'rockSmallGrey', 'rockSmallBrown', 'stump', 'stones']},
    'big_rocks.png': {'mode': 'auto', 'outputs': ['rockGrey', 'rockBrown']},
    'village.png': {'mode': 'auto', 'outputs': ['fence', 'signpost', 'pillar', 'torch', 'crystal', 'bones']},
    'houses.png': {'mode': 'auto', 'outputs': ['house', 'house2', 'house3']},
    # palm/cactus touch by a few stray antialiased px even past the haze threshold -- split by hand
    # at the real local-minimum column (verified: alpha-column-sum dips to ~12px around x=877).
    'desert.png': {'mode': 'manual', 'splits': [877], 'outputs': ['palm', 'cactus']},
    # Village set (TO PAINT): one RGBA sheet, transparent background, objects left-to-right in
    # exactly this order, >= MIN_GAP_COLUMNS fully transparent columns between neighbours.
    'village_props.png': {'mode': 'auto', 'outputs': [
        'well', 'lanternPost', 'marketStall', 'cropPlot', 'scarecrow', 'woodpile', 'campfire', 'barrel', 'crate', 'rowboat',
    ]},
}

CELL_NOTES = {
    'pine_tree.png': ['whole sheet = 1 pine tree, reused as both `pine` and `pineDark`'],
    'round_tree.png': ['whole sheet = 1 round tree, reused as both `roundTree` and `bigTree`'],
    'small_trees.png': ['small leafy tree -> smallTree', 'small pine (unused)', 'dead tree -> deadTree'],
    'bushes_rocks.png': ['bush', 'flowering bush -> bush2', 'grey rock -> rockSmallGrey', 'brown rock -> rockSmallBrown', 'stump', 'stones (non-solid)'],
    'big_rocks.png': ['grey boulders -> rockGrey', 'brown boulders -> rockBrown'],
    'village.png': ['fence', 'signpost', 'pillar', 'torch', 'crystal', 'bones (non-solid)'],
    'houses.png': ['red roof -> house', 'brown roof -> house2', 'stone roof -> house3'],
    'desert.png': ['palm', 'cactus'],
    'village_props.png': [
        'well (roofed stone well, 2x3 tiles)', 'lanternPost (wooden post + hanging lantern, 1x2)',
        'marketStall (striped-awning stall with goods, 3x3)', 'cropPlot (one row of vegetables, 2x1)',
        'scarecrow (1x2)', 'woodpile (log shelter, 2x2)', 'campfire (1x1)', 'barrel (1x1)', 'crate (1x1)',
        'rowboat (2x1, moored on water)',
    ],
}


def threshold_alpha(arr, thresh=ALPHA_THRESHOLD):
    """Faint haze (alpha <= thresh) -> fully transparent; kept pixels keep their alpha. Mutates a copy."""
    out = arr.copy()
    out[..., 3] = np.where(out[..., 3] > thresh, out[..., 3], 0)
    return out


def bbox_of(alpha):
    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def column_segments(alpha, min_gap=MIN_GAP_COLUMNS):
    """Runs of columns that contain any opaque pixel, separated by >= min_gap empty columns."""
    has_content = (alpha > 0).any(axis=0)
    segs = []
    x = 0
    w = len(has_content)
    while x < w:
        if not has_content[x]:
            x += 1
            continue
        start = x
        while x < w:
            # look ahead: treat a short empty run as still part of this segment (anti-alias gaps)
            if has_content[x]:
                x += 1
                continue
            gap_start = x
            while x < w and not has_content[x]:
                x += 1
            if x < w and (x - gap_start) < min_gap:
                continue  # short gap -- keep merging into this segment
            break
        segs.append((start, x))
    return segs


def premultiplied_resize(img, size):
    """Premultiplied-alpha LANCZOS resize -- avoids dark/light fringing at soft painted edges."""
    arr = np.array(img).astype(np.float32)
    alpha = arr[..., 3:4] / 255.0
    premult = np.concatenate([arr[..., :3] * alpha, arr[..., 3:4]], axis=2)
    small = Image.fromarray(premult.astype(np.uint8), 'RGBA').resize(size, Image.LANCZOS)
    s = np.array(small).astype(np.float32)
    a = s[..., 3:4]
    rgb = np.where(a > 0, s[..., :3] / np.maximum(a / 255.0, 1e-6), 0)
    out = np.clip(np.concatenate([rgb, a], axis=2), 0, 255).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


def fit_bottom_center(cropped, target_w, target_h):
    """Scale `cropped` so its larger dimension (relative to the target box) exactly fills that
    axis, preserving aspect ratio, then paste it bottom-center onto a target_w x target_h
    transparent canvas (object's base sits on the canvas's bottom edge)."""
    bw, bh = cropped.size
    scale = min(target_w / bw, target_h / bh)
    new_w, new_h = max(1, round(bw * scale)), max(1, round(bh * scale))
    resized = premultiplied_resize(cropped, (new_w, new_h))
    resized = Image.fromarray(threshold_alpha(np.array(resized)), 'RGBA')
    canvas = Image.new('RGBA', (target_w, target_h), (0, 0, 0, 0))
    x_off = (target_w - new_w) // 2
    y_off = target_h - new_h
    canvas.paste(resized, (x_off, y_off), resized)
    return canvas


def segment_bounds(name, cfg, width):
    if cfg['mode'] == 'whole':
        return [(0, width)]
    if cfg['mode'] == 'manual':
        points = [0, *cfg['splits'], width]
        return [(points[i], points[i + 1]) for i in range(len(points) - 1)]
    raise ValueError(f'auto mode resolved elsewhere for {name}')


def build_sheet(name, cfg, results):
    path = SRC_DIR / name
    if not path.exists():
        print(f'WARN skipping {name}: {path} not found')
        return
    img = Image.open(path).convert('RGBA')
    arr = threshold_alpha(np.array(img))

    outputs = cfg['outputs']
    if cfg['mode'] == 'whole':
        bounds = [(0, img.width)] * len(outputs)  # same single object reused for every output name
    elif cfg['mode'] == 'auto':
        bounds = column_segments(arr[..., 3])
    else:
        bounds = segment_bounds(name, cfg, img.width)

    if len(bounds) != len(outputs):
        print(f'WARN {name}: expected {len(outputs)} segments, found {len(bounds)} {bounds} -- skipping')
        return

    for (x0, x1), prop_type in zip(bounds, outputs):
        if prop_type is None:
            continue
        segment = arr[:, x0:x1, :]
        bbox = bbox_of(segment[..., 3])
        if bbox is None:
            print(f'WARN {name}: empty segment for {prop_type} ({x0},{x1}) -- skipping')
            continue
        bx0, by0, bx1, by1 = bbox
        cropped = Image.fromarray(segment[by0:by1, bx0:bx1, :], 'RGBA')
        target_w_units, target_h_units = PROP_TARGETS[prop_type]
        canvas = fit_bottom_center(cropped, target_w_units * PIXELS_PER_UNIT, target_h_units * PIXELS_PER_UNIT)
        canvas.save(OUT_DIR / f'{prop_type}.png')
        results[prop_type] = canvas
        print(f'{prop_type}.png {canvas.width}x{canvas.height} (from {name} segment {x0}-{x1}, bbox {bx1 - bx0}x{by1 - by0})')


def contact_sheet(results, path):
    names = sorted(results)
    cols = 6
    rows = -(-len(names) // cols)
    cell = 140
    out = Image.new('RGBA', (cols * cell, rows * cell), (48, 48, 64, 255))
    d = ImageDraw.Draw(out)
    for i, name in enumerate(names):
        r, c = divmod(i, cols)
        img = results[name]
        scale = min((cell - 16) / img.width, (cell - 30) / img.height, 1.0)
        thumb = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
        x = c * cell + (cell - thumb.width) // 2
        y = r * cell + 20 + (cell - 30 - thumb.height)
        out.paste(thumb, (x, y), thumb)
        d.text((c * cell + 4, r * cell + 2), name, fill=(255, 255, 0, 255))
    out.save(path)


def write_readme(results):
    lines = [
        '# HD prop sheets (normalized)', '',
        'Generated from the raw sheets in `assets/hd/props/*.png` by `tools/build-hd-props.py`',
        '(`npm run hd:props`). Each raw sheet holds several hand-painted objects placed irregularly',
        '(not an exact grid); the build script removes a faint glow/halo (alpha <= 48 -> transparent),',
        'splits the sheet into per-object segments (empty-column gaps, or a manual split where two',
        'objects touch by a few stray px), tight-crops each object, and fits it bottom-center into',
        'its prop type\'s HD canvas (world size x4, matching `src/world/prop-definitions.js`).', '',
        '## Prop -> source mapping', '',
    ]
    for name, cfg in SHEETS.items():
        if name not in CELL_NOTES:
            continue
        lines.append(f'### {name}')
        lines.extend(f'- {note}' for note in CELL_NOTES[name])
        lines.append('')
    lines += [
        '## Rebuild', '', '```', 'python tools/build-hd-props.py', '# or: npm run hd:props', '```', '',
        'Missing raw files are skipped with a warning -- the set can grow one sheet at a time.',
    ]
    (OUT_DIR / 'README.md').write_text('\n'.join(lines), encoding='utf-8')


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    results = {}
    for name, cfg in SHEETS.items():
        build_sheet(name, cfg, results)

    if not results:
        print('ERROR: no prop art was produced (no input files found in', SRC_DIR, ')')
        sys.exit(1)

    missing = sorted(set(PROP_TARGETS) - set(results))
    if missing:
        print('WARN: no art produced for these prop types (check SHEETS/PROP_TARGETS):', missing)

    contact_sheet(results, OUT_DIR / 'contact-sheet.png')
    print('contact-sheet.png written')
    write_readme(results)
    print('RESULT: OK')


if __name__ == '__main__':
    main()

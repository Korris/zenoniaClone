"""Normalizes raw ChatGPT hero sheets (assets/hd/hero/*.png) into clean HD animation sheets.

Unlike build-hero-sprites.py (pixel-art pipeline: hard alpha, palette quantization, 1px outline),
this is a hand-painted HD pipeline: soft alpha is kept, no outline, no quantization. Output frames
are 96x96 (24 world units * pixelsPerUnit=4, matching HD_HERO in src/graphics/asset-atlas.js) so
the engine can draw them at the same world-space size as the 24x24 pixel-art hero set.

Input sheets: a 4-row x N-col grid per animation (rows = DOWN/UP/LEFT/RIGHT, columns = frames in
time order). Cells are not expected to be exactly even -- grid lines are snapped to the emptiest
(most transparent) gaps instead of a fixed spacing. Only files that exist in SRC_DIR are processed;
missing ones are skipped with a warning (lets the hero set grow one animation at a time, and the
script is safe to rerun as more sheets show up).

Some sheets arrive with real (soft) alpha; others come back fully opaque with a painted two-tone
checkerboard standing in for transparency. Both are handled: real alpha gets a haze threshold,
opaque sheets get color-keyed against their sampled checkerboard tile colors (see
`is_checkerboard_sheet`/`checkerboard_alpha`).

ChatGPT sheets come back at different native resolutions per file (e.g. idle cells ~313px with a
~310px tall body, walk cells ~256px with a ~237px tall body) -- a single scale derived from idle
would shrink every other animation. So the scale is computed PER SHEET from that sheet's own
row 0 / col 0 frame (DOWN-facing, first frame -- the neutral/standing pose, including for
attack/death sheets where it's the wind-up stance) so every animation's body ends up ~82px tall at
96x96 (superseded: scale now matches head size, see compute_scale_for_sheet). Per-animation scale is recorded in scale.json for reference/debugging.

Usage: python tools/build-hd-hero.py [src_dir] [out_dir]
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from hero_head_scale import head_template, relative_head_scale

SRC_DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/hd/hero')
OUT_DIR = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/hd/hero/normalized')

ROWS = ['down', 'up', 'left', 'right']  # matches CHARACTER_DIRECTIONS order in the engine
FRAME = 96                              # output frame size: 24 world units * pixelsPerUnit (4)
TARGET_BODY_HEIGHT = 82                 # px, row0/col0 body height of the reference (idle) sheet
REFERENCE_ANIM = 'idle'                 # built first; its head is the size every other sheet matches
ALPHA_THRESHOLD = 48                    # alpha <= 48 (incl. the faint 1-40 haze) -> fully transparent
CHECKERBOARD_DISTANCE = 40              # color distance to a sampled bg tile -> treated as background
                                         # (generous: flood_from_border() guards against eating pale character colors)
MIN_COMPONENT_PX = 60                   # smaller foreground blobs (checkerboard noise) get dropped
SCALE_FILE = 'scale.json'
FACE_SIZE = 152  # HUD portrait + inventory panel faceset

# Frames per row, per animation. Rows are always the 4 facings above.
FRAME_COUNTS = {
    'idle': 4, 'walk': 6, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2, 'death': 5,
}


def refine_boundaries(profile, start, end, count):
    """`count` evenly-spaced boundaries over [start, end), each snapped to the emptiest point in
    its window -- lets an unevenly-drawn ChatGPT grid still split cleanly between frames."""
    span = (end - start) / count
    bounds = [start]
    for k in range(1, count):
        guess = start + k * span
        lo, hi = int(guess - span / 3), int(guess + span / 3)
        window = profile[lo:hi]
        bounds.append(lo + int(np.argmin(window)) if len(window) else int(guess))
    bounds.append(end)
    return bounds


def grid_lines(mask, axis, count):
    """Snap `count` boundaries along `axis` (0=rows, 1=cols) of a boolean mask to alpha-empty gaps."""
    profile = mask.sum(axis=1 if axis == 0 else 0)
    total = mask.shape[axis]
    occupied = np.where(profile > 0)[0]
    start, end = (int(occupied.min()), int(occupied.max()) + 1) if len(occupied) else (0, total)
    return refine_boundaries(profile, start, end, count)


def threshold_alpha(arr):
    """Faint haze (alpha 1-40, and anything <= 48) becomes fully transparent; kept pixels retain
    their original alpha so edges stay soft -- this is a hand-painted sheet, not a pixel cutout."""
    out = arr.copy()
    out[..., 3] = np.where(arr[..., 3] > ALPHA_THRESHOLD, arr[..., 3], 0)
    return out


def is_checkerboard_sheet(arr):
    """ChatGPT sometimes "fakes" transparency by painting a two-tone checkerboard instead of using
    a real alpha channel -- such a sheet comes back (almost) fully opaque."""
    return (arr[..., 3] > 250).mean() > 0.98


def sample_background_colors(rgb, patch=80, iters=12):
    """Two dominant colors from the sheet's 4 corners via a tiny k-means (k=2). Checkerboard tiles
    are often dithered/anti-aliased so no single exact RGB value dominates a raw pixel count --
    clustering is robust to that where nearest-unique-color counting (picking the 2 most frequent
    exact colors) is not (it can return two near-duplicates of the SAME tile)."""
    h, w = rgb.shape[:2]
    patches = [rgb[:patch, :patch], rgb[:patch, w - patch:], rgb[h - patch:, :patch], rgb[h - patch:, w - patch:]]
    samples = np.concatenate([p.reshape(-1, 3) for p in patches], axis=0).astype(np.float64)
    rng = np.random.default_rng(0)  # fixed seed: deterministic, reproducible builds
    centers = samples[rng.choice(len(samples), 2, replace=False)]
    for _ in range(iters):
        dist = ((samples[:, None, :] - centers[None, :, :]) ** 2).sum(axis=2)
        assign = dist.argmin(axis=1)
        new_centers = np.array([
            samples[assign == i].mean(axis=0) if (assign == i).any() else centers[i] for i in range(2)
        ])
        if np.allclose(new_centers, centers, atol=0.5):
            centers = new_centers
            break
        centers = new_centers
    return centers


def label_components(mask):
    """4-connected component labels for a boolean mask via iterative BFS (no scipy dependency).
    Returns (labels int32 array, {label: pixel count})."""
    labels = np.zeros(mask.shape, dtype=np.int32)
    visited = np.zeros(mask.shape, dtype=bool)
    h, w = mask.shape
    sizes = {}
    current = 0
    ys, xs = np.where(mask)
    for y0, x0 in zip(ys.tolist(), xs.tolist()):
        if visited[y0, x0]:
            continue
        current += 1
        stack = [(y0, x0)]
        visited[y0, x0] = True
        size = 0
        while stack:
            y, x = stack.pop()
            labels[y, x] = current
            size += 1
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    stack.append((ny, nx))
        sizes[current] = size
    return labels, sizes


def drop_small_components(mask, min_size):
    labels, sizes = label_components(mask)
    keep_labels = [label for label, size in sizes.items() if size >= min_size]
    return np.isin(labels, keep_labels)


def flood_from_border(mask):
    """Keep only the connected component(s) of a boolean mask that touch the image border. Used
    to turn a raw color-distance guess into an accurate background mask: a checkerboard tile is
    dithered/anti-aliased enough that a per-pixel distance test alone misses much of it (and a
    generous threshold alone would risk eating similarly-pale character colors like a cream
    tunic) -- but only the ACTUAL backdrop is contiguous with the sheet's outer edge, so pixels cut
    off from the border by the character's silhouette are never touched even if their color matches."""
    labels, _ = label_components(mask)
    border_labels = set(labels[0, :]) | set(labels[-1, :]) | set(labels[:, 0]) | set(labels[:, -1])
    border_labels.discard(0)
    return np.isin(labels, list(border_labels))


def checkerboard_alpha(rgb):
    """Key out a painted checkerboard background: sample its two tile colors, keep only the
    color-matching region that's contiguous with the sheet's outer edge (handles dithering without
    risking pale character colors), drop tiny leftover specks, soften the edge by ~1px. Returns a
    uint8 alpha array the same size as `rgb`. Painted FX (e.g. blue slash streaks) survive since
    they're far in color from the grey/tan checkerboard tiles."""
    bg_colors = sample_background_colors(rgb)
    bg_color_mask = np.zeros(rgb.shape[:2], dtype=bool)
    for color in bg_colors:
        dist = np.sqrt(((rgb.astype(np.float64) - color) ** 2).sum(axis=2))
        bg_color_mask |= dist < CHECKERBOARD_DISTANCE
    bg_mask = flood_from_border(bg_color_mask)
    fg_mask = drop_small_components(~bg_mask, MIN_COMPONENT_PX)
    alpha = (fg_mask * 255).astype(np.uint8)
    return np.array(Image.fromarray(alpha, 'L').filter(ImageFilter.GaussianBlur(1)))


def normalize_alpha(img):
    """RGBA array with a clean alpha channel: real (soft) alpha sheets get the haze threshold;
    sheets that are (almost) fully opaque get checkerboard color-keying instead."""
    arr = np.array(img.convert('RGBA'))
    if is_checkerboard_sheet(arr):
        print('  detected a painted checkerboard background -- color-keying instead of alpha')
        arr = arr.copy()
        arr[..., 3] = checkerboard_alpha(arr[..., :3])
        return arr
    return threshold_alpha(arr)


def split_sheet(img, cols):
    """4 rows x `cols` cells; grid lines are snapped independently per row band so a wobbly grid
    still lines up. Returns a list of row-lists of RGBA numpy arrays (already alpha-normalized)."""
    arr = normalize_alpha(img)
    mask = arr[..., 3] > 0
    row_bounds = grid_lines(mask, axis=0, count=4)
    cells = []
    for r in range(4):
        y0, y1 = row_bounds[r], row_bounds[r + 1]
        band_mask = mask[y0:y1]
        col_bounds = grid_lines(band_mask, axis=1, count=cols)
        cells.append([arr[y0:y1, col_bounds[c]:col_bounds[c + 1]] for c in range(cols)])
    return cells


def tight_bbox(cell_arr):
    """Crop a cell down to its non-transparent bounding box, or None if the cell is empty."""
    ys, xs = np.where(cell_arr[..., 3] > 0)
    if len(ys) == 0:
        return None
    return cell_arr[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def downscale(rgba_img, scale):
    """Premultiplied-alpha LANCZOS resize -- avoids dark/light fringing at soft edges. No outline,
    no palette quantization (unlike build-hero-sprites.py): this is hand-painted HD art."""
    arr = np.array(rgba_img).astype(np.float32)
    alpha = arr[..., 3:4] / 255.0
    premult = np.concatenate([arr[..., :3] * alpha, arr[..., 3:4]], axis=2)
    w = max(1, round(rgba_img.width * scale))
    h = max(1, round(rgba_img.height * scale))
    small = Image.fromarray(premult.astype(np.uint8), 'RGBA').resize((w, h), Image.LANCZOS)
    s = np.array(small).astype(np.float32)
    a = s[..., 3:4]
    rgb = np.where(a > 0, s[..., :3] / np.maximum(a / 255.0, 1e-6), 0)
    out = np.clip(np.concatenate([rgb, a], axis=2), 0, 255).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


def place_in_frame(img, size=FRAME):
    """Center horizontally, feet (bbox bottom) on the frame's bottom row. Clips from the top/left
    if a pose overflows the frame (e.g. a raised sword) -- feet pivot must stay exact."""
    frame = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    bbox = img.getbbox()
    if not bbox:
        return frame
    cropped = img.crop(bbox)
    if cropped.width > size or cropped.height > size:
        print(f'WARN frame {cropped.width}x{cropped.height} overflows {size}x{size}; clipping')
        cropped = cropped.crop((max(0, cropped.width - size), max(0, cropped.height - size), cropped.width, cropped.height))
    x = (size - cropped.width) // 2
    y = size - cropped.height
    frame.paste(cropped, (x, y), cropped)
    return frame


# Set by the reference sheet (idle, built first): its scale and its head template.
HEAD_REF = {'scale': None, 'template': None}


def compute_scale_for_sheet(anim, cells):
    """Per-sheet scale so the HEAD is the same size in every animation (see hero_head_scale.py).
    The reference sheet (idle) maps its row0/col0 body to TARGET_BODY_HEIGHT; every other sheet is
    scaled by its head size relative to that reference. Falls back to body height when there is no
    reference yet or no frame matches. Returns (scale, relative head size or None)."""
    first = tight_bbox(cells[0][0])
    if first is None:
        raise RuntimeError('row 0 col 0 (down, frame 0) is empty -- cannot compute a scale for this sheet')
    body_scale = TARGET_BODY_HEIGHT / float(first.shape[0])
    if anim == REFERENCE_ANIM or HEAD_REF['template'] is None:
        if anim == REFERENCE_ANIM:  # one head template per facing, from each row's first frame
            firsts = [tight_bbox(row[0]) for row in cells]
            HEAD_REF.update(scale=body_scale, template=[head_template(b) if b is not None else None for b in firsts])
        return body_scale, None
    rel = relative_head_scale(HEAD_REF['template'], [[tight_bbox(c) for c in row] for row in cells])
    return (HEAD_REF['scale'] / rel, rel) if rel else (body_scale, None)


def build_sheet(anim, cols):
    path = SRC_DIR / f'{anim}.png'
    if not path.exists():
        print(f'WARN skipping {anim}: {path} not found')
        return None, None
    img = Image.open(path).convert('RGBA')
    cells = split_sheet(img, cols)
    scale, rel = compute_scale_for_sheet(anim, cells)
    print(f'{anim}: scale {scale:.4f}' + (f' (head {rel:.2f}x the {REFERENCE_ANIM} head)' if rel else ' (body height)'))
    sheet = Image.new('RGBA', (cols * FRAME, 4 * FRAME), (0, 0, 0, 0))
    empty = []
    for r in range(4):
        for c in range(cols):
            bbox_arr = tight_bbox(cells[r][c])
            if bbox_arr is None:
                empty.append((ROWS[r], c))
                continue
            frame = place_in_frame(downscale(Image.fromarray(bbox_arr, 'RGBA'), scale))
            sheet.paste(frame, (c * FRAME, r * FRAME))
    if empty:
        print(f'WARN {anim}.png: empty frames at {empty}')
    record = {'scale': scale, 'headVsReference': rel, 'frame': FRAME}
    return sheet, record


def build_face():
    """hero_face.png is a single portrait (not a 4-row grid) -- centered crop/downscale to a square
    FACE_SIZE, used by the HUD portrait and inventory panel faceset (not bottom-pivoted like a body)."""
    path = SRC_DIR / 'hero_face.png'
    if not path.exists():
        print('WARN skipping hero_face: assets/hd/hero/hero_face.png not found')
        return None
    img = Image.open(path).convert('RGBA')
    arr = normalize_alpha(img)
    bbox_arr = tight_bbox(arr)
    if bbox_arr is None:
        print('WARN hero_face.png: no visible content after alpha normalization')
        return None
    cropped = Image.fromarray(bbox_arr, 'RGBA')
    scale = min((FACE_SIZE - 4) / cropped.width, (FACE_SIZE - 4) / cropped.height)
    small = downscale(cropped, scale)
    frame = Image.new('RGBA', (FACE_SIZE, FACE_SIZE), (0, 0, 0, 0))
    frame.paste(small, ((FACE_SIZE - small.width) // 2, (FACE_SIZE - small.height) // 2), small)
    return frame


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


def write_readme(sheets, scale_records, face):
    rows = []
    for name, cols in FRAME_COUNTS.items():
        if name not in sheets:
            continue
        sheet = sheets[name]
        rec = scale_records[name]
        rows.append(f"| {name}.png | {cols} | {sheet.width}x{sheet.height} | {rec['scale']:.4f} (head x{rec['headVsReference'] or 1:.2f}) |")
    table = '\n'.join(rows)
    face_note = f'- `hero_face.png` ({face.width}x{face.height}): HUD portrait + inventory faceset, centered (not bottom-pivoted).' if face else ''
    content = f"""# HD hero sheets (normalized)

Generated from the raw ChatGPT sheets in `assets/hd/hero/*.png` by `tools/build-hd-hero.py`.
Hand-painted HD art (soft alpha, no outline, no palette quantization) -- not the pixel-art
pipeline in `assets/custom/hero/`. Frames are 96x96 px = 24 world units * pixelsPerUnit (4),
so the engine can draw them at the exact same world-space size as the 24x24 pixel hero set.

## Sheets

| File | Frames/row | Size | Scale (per-sheet) |
|------|-----------:|------|--------------------|
{table}

- Rows: `0 = DOWN`, `1 = UP`, rows 2/3 = the side views. Which side row faces left/right differs
  per sheet (some sheets paint both facing right) -- the engine maps them via `HD_HERO_SIDE_ROWS`
  in `src/graphics/asset-atlas.js`, mirroring the right-facing row where no left one exists.
- Pivot: feet on the bottom row (y = 95), body centered horizontally (x = 48).
- Scale is computed PER SHEET so the HEAD is the same size in every animation: idle's front body is
  82px tall, every other sheet is matched to idle's head by multi-scale template matching
  (tools/hero_head_scale.py). Body height can't be used -- crouched poses (dash, lunges) have short
  bodies and matching them inflated the head. See `scale.json`.
{face_note}

## Rebuild

```
python tools/build-hd-hero.py
# or: npm run hd:build
```

Add a new raw sheet to `assets/hd/hero/<anim>.png` (4 rows x N cols, see FRAME_COUNTS in the
script) and rerun -- missing files are skipped with a warning, existing ones are all reprocessed.
Drop `assets/hd/hero/hero_face.png` (any resolution, square-ish) to (re)generate the portrait.
"""
    (OUT_DIR / 'README.md').write_text(content, encoding='utf-8')


def validate(sheets):
    failures = []
    for anim, cols in FRAME_COUNTS.items():
        sheet = sheets.get(anim)
        if sheet is None:
            continue
        expected = (cols * FRAME, 4 * FRAME)
        if sheet.mode != 'RGBA':
            failures.append(f'{anim}.png: mode {sheet.mode}, expected RGBA')
        if sheet.size != expected:
            failures.append(f'{anim}.png: size {sheet.size}, expected {expected}')
            continue
        arr = np.array(sheet)[..., 3]
        for r in range(4):
            for c in range(cols):
                cell = arr[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME]
                if cell.sum() == 0:
                    failures.append(f'{anim}.png: frame row {ROWS[r]} col {c} is empty')
                elif cell[-4:].sum() == 0:
                    failures.append(f'{anim}.png: frame row {ROWS[r]} col {c} feet are not near the bottom row')
                elif cell[-1].sum() == 0:
                    print(f'WARN {anim}.png: frame row {ROWS[r]} col {c} feet are 1-3px off the bottom row (lunge/attack pose)')
    for msg in failures:
        print('FAIL', msg)
    return failures


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    scale_path = OUT_DIR / SCALE_FILE
    scale_records = json.loads(scale_path.read_text()) if scale_path.exists() else {}

    sheets = {}
    for anim, cols in FRAME_COUNTS.items():
        sheet, record = build_sheet(anim, cols)
        if sheet is None:
            continue
        sheets[anim] = sheet
        scale_records[anim] = record
        sheet.save(OUT_DIR / f'{anim}.png')
        print(f'{anim}.png {sheet.width}x{sheet.height}')

    face = build_face()
    if face is not None:
        face.save(OUT_DIR / 'hero_face.png')
        print(f'hero_face.png {face.width}x{face.height}')

    if not sheets and face is None:
        print('ERROR: no sheets were produced (no input files found in', SRC_DIR, ')')
        sys.exit(1)

    scale_path.write_text(json.dumps(scale_records, indent=2))
    montage = dict(sheets)
    if face is not None:
        montage['hero_face'] = face
    contact_sheet(montage, OUT_DIR / 'contact-sheet.png')
    print('contact-sheet.png written')
    write_readme(sheets, scale_records, face)

    failures = validate(sheets)
    if face is not None and face.size != (FACE_SIZE, FACE_SIZE):
        msg = f'hero_face.png: size {face.size}, expected {(FACE_SIZE, FACE_SIZE)}'
        print('FAIL', msg)
        failures.append(msg)
    print('RESULT:', 'OK' if not failures else f'{len(failures)} failure(s)')
    sys.exit(1 if failures else 0)


if __name__ == '__main__':
    main()

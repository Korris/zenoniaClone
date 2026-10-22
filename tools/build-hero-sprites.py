"""Builds the hero sprite set from the ChatGPT concept sheet (assets/source/character-concept.png).

Pipeline per animation block: locate frame cells by projecting a foreground mask, cut each
cell, remove the painted background (true alpha, holes filled), scale every frame with ONE global
factor so proportions/pivot stay identical, quantize to a shared <=32 color palette, and pack
into RGBA sheets (rows = down/up/left/right, columns = frames). Effects are single-row strips.

Usage: python tools/build-hero-sprites.py [concept.png] [out_dir]
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/source/character-concept.png')
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else 'assets/custom/hero')
FRAME = 24
MAX_COLORS = 32
ROWS = ['down', 'up', 'left', 'right']

# Loose block rectangles on the concept sheet: (x0, y0, x1, y1, frames)
CHARACTER_BLOCKS = {
    'idle': (20, 5, 310, 305, 4), 'walk': (340, 5, 780, 305, 6),
    'attack1': (820, 5, 1140, 305, 4), 'attack2': (1200, 5, 1520, 305, 4),
    'attack3': (20, 360, 570, 690, 6), 'dash': (555, 360, 840, 690, 3),
    'hurt': (870, 360, 1050, 690, 2), 'death': (1080, 360, 1520, 690, 5),
}
EFFECT_BLOCKS = {  # (rect, frames, frame size, mask mode)
    'slash_small': ((30, 735, 570, 840), 6, 32, 'white'), 'slash_big': ((590, 730, 1520, 845), 8, 48, 'white-soft'),
    'dash_trail': ((270, 885, 690, 955), 4, 32, 'lum'), 'hit_spark': ((820, 890, 1220, 950), 5, 16, 'lum'),
}


def foreground_mask(rgb, bg_color, threshold):
    """Pixels whose color is far from the estimated background color."""
    dist = np.sqrt(((rgb.astype(np.int32) - bg_color.astype(np.int32)) ** 2).sum(axis=2))
    return dist > threshold


def estimate_background(rgb):
    """Median of the border pixels of a region (the painted background is smooth)."""
    border = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    return np.median(border, axis=0)


def split_runs(profile, count, min_gap=3):
    """Split a 1-D occupancy profile into `count` runs using the largest gaps."""
    occupied = profile > 0
    runs, start = [], None
    for i, on in enumerate(list(occupied) + [False]):
        if on and start is None:
            start = i
        elif not on and start is not None:
            runs.append([start, i])
            start = None
    # merge runs separated by tiny gaps (sword tips, sparks)
    merged = [runs[0]] if runs else []
    for run in runs[1:]:
        if run[0] - merged[-1][1] <= min_gap:
            merged[-1][1] = run[1]
        else:
            merged.append(run)
    merged.sort(key=lambda r: r[1] - r[0], reverse=True)
    picked = sorted(merged[:count])
    if len(picked) != count:
        raise RuntimeError(f'expected {count} runs, found {len(picked)}')
    return picked


def fill_holes(mask):
    """Anything not reachable from the border through background becomes foreground."""
    h, w = mask.shape
    img = Image.fromarray(np.where(mask, 255, 0).astype(np.uint8))
    padded = Image.new('L', (w + 2, h + 2), 0)
    padded.paste(img, (1, 1))
    ImageDraw.floodfill(padded, (0, 0), 128)
    arr = np.array(padded)[1:-1, 1:-1]
    return arr != 128


def cut_cell(rgb, rect, threshold, bright=None):
    """Extract one frame: alpha from background distance (or brightness for FX), holes filled."""
    x0, y0, x1, y1 = rect
    cell = rgb[y0:y1, x0:x1]
    mask = brightness_mask(cell, bright) if bright else fill_holes(foreground_mask(cell, estimate_background(cell), threshold))
    ys, xs = np.where(mask)
    if len(ys) == 0:
        return Image.new('RGBA', (1, 1), (0, 0, 0, 0))
    cy0, cy1, cx0, cx1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgba = np.dstack([cell[cy0:cy1, cx0:cx1], np.where(mask[cy0:cy1, cx0:cx1], 255, 0).astype(np.uint8)])
    return Image.fromarray(rgba, 'RGBA')


def refine_boundaries(profile, start, end, count):
    """Expected boundaries at even spacing, each snapped to the emptiest column nearby."""
    span = (end - start) / count
    bounds = [start]
    for k in range(1, count):
        guess = start + k * span
        lo, hi = int(guess - span / 3), int(guess + span / 3)
        window = profile[lo:hi]
        bounds.append(lo + int(np.argmin(window)) if len(window) else int(guess))
    bounds.append(end)
    return bounds


def locate_cells(rgb, block, rows, cols, threshold, bright=None):
    """Trim the block to its foreground bbox, split it into a grid whose lines are snapped to the
    emptiest gaps between frames; every cell is tightened later by cut_cell."""
    x0, y0, x1, y1 = block[:4]
    region = rgb[y0:y1, x0:x1]
    mask = brightness_mask(region, bright) if bright else foreground_mask(region, estimate_background(region), threshold)
    colsum, rowsum = mask.sum(axis=0), mask.sum(axis=1)
    xs = np.where(colsum > 2)[0]
    ys = np.where(rowsum > 2)[0]
    bx0, bx1, by0, by1 = int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1
    xb = refine_boundaries(colsum, bx0, bx1, cols)
    yb = refine_boundaries(rowsum, by0, by1, rows) if rows > 1 else [by0, by1]
    cells = []
    for r in range(rows):
        for c in range(cols):
            cells.append((x0 + xb[c], y0 + yb[r], x0 + xb[c + 1], y0 + yb[r + 1]))
    return cells


def brightness_mask(rgb, mode='lum'):
    """Effects are bright strokes on a dark glowing background. 'white' keeps only the
    white/cyan core of a slash (red channel high), 'lum' keeps anything clearly brighter than
    the border."""
    if mode == 'white':
        return (rgb[..., 0] > 135) & (rgb[..., 0] > rgb[..., 2] * 0.72)
    if mode == 'white-soft':  # fading tail frames of the big slash are dimmer; reject the blue glow
        return (rgb[..., 0] > 112) & (rgb[..., 0] > rgb[..., 2] * 0.72)
    lum = rgb.astype(np.int32).sum(axis=2) / 3
    bg = np.median(np.concatenate([lum[0], lum[-1], lum[:, 0], lum[:, -1]]))
    return lum > bg + 45


def downscale(img, scale):
    """Lanczos with premultiplied alpha + unsharp mask so pixels stay crisp, then a hard alpha edge."""
    arr = np.array(img).astype(np.float32)
    alpha = arr[..., 3:4] / 255.0
    pre = np.concatenate([arr[..., :3] * alpha, arr[..., 3:4]], axis=2)
    small = Image.fromarray(pre.astype(np.uint8), 'RGBA').resize(
        (max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    s = np.array(small).astype(np.float32)
    a = s[..., 3:4]
    rgb = np.where(a > 0, s[..., :3] / np.maximum(a / 255.0, 1e-6), 0)
    sharp = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), 'RGB').filter(ImageFilter.UnsharpMask(radius=1, percent=140, threshold=2))
    out = np.concatenate([np.array(sharp), np.where(a >= 112, 255, 0)], axis=2).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


OUTLINE_COLOR = (34, 26, 40)


def add_outline(img):
    """1px dark outline around the silhouette (classic pixel-art rim), keeps binary alpha."""
    arr = np.array(img)
    alpha = arr[..., 3] > 0
    grown = alpha.copy()
    grown[1:, :] |= alpha[:-1, :]
    grown[:-1, :] |= alpha[1:, :]
    grown[:, 1:] |= alpha[:, :-1]
    grown[:, :-1] |= alpha[:, 1:]
    rim = grown & ~alpha
    arr[rim] = (*OUTLINE_COLOR, 255)
    return Image.fromarray(arr, 'RGBA')


def place_in_frame(img, size, bottom_align=True):
    """Center horizontally, feet (mask bottom) on the last row, clipped to size x size."""
    frame = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    bbox = img.getbbox()
    if not bbox:
        return frame
    img = img.crop(bbox)
    x = (size - img.width) // 2
    y = size - img.height if bottom_align else (size - img.height) // 2
    frame.paste(img, (x, y), img)
    return frame


def quantize_set(frames, max_colors):
    """Shared palette across all character frames (median cut), transparent pixels untouched."""
    strip = Image.new('RGBA', (sum(f.width for f in frames), max(f.height for f in frames)))
    x = 0
    for f in frames:
        strip.paste(f, (x, 0))
        x += f.width
    opaque = strip.convert('RGB')
    pal = opaque.quantize(colors=max_colors, method=Image.MEDIANCUT, dither=Image.NONE)
    out, x = [], 0
    for f in frames:
        q = pal.crop((x, 0, x + f.width, f.height)).convert('RGB')
        rgba = np.dstack([np.array(q), np.array(f)[..., 3]])
        out.append(Image.fromarray(rgba, 'RGBA'))
        x += f.width
    return out


def build_characters(rgb):
    raw = {}
    for name, block in CHARACTER_BLOCKS.items():
        cells = locate_cells(rgb, block, 4, block[4], threshold=70)
        raw[name] = [cut_cell(rgb, c, threshold=70) for c in cells]
    # one global scale: idle hero body = 22px tall; never let a frame exceed 24px wide
    idle_height = np.median([f.height for f in raw['idle']])
    widest = max(f.width for frames in raw.values() for f in frames)
    scale = 21 / idle_height  # body = 21px; frames taller/wider than 24px lose a few sword-tip pixels
    print(f'global scale {scale:.3f} (idle height {idle_height:.0f}px, widest {widest}px)')
    scaled = {name: [place_in_frame(downscale(f, scale), FRAME) for f in frames] for name, frames in raw.items()}
    order = [(name, i) for name, frames in scaled.items() for i in range(len(frames))]
    quantized = quantize_set([scaled[n][i] for n, i in order], MAX_COLORS - 1)  # +1 outline color
    for (name, i), q in zip(order, quantized):
        scaled[name][i] = place_in_frame(add_outline(q), FRAME)
    sheets = {}
    for name, block in CHARACTER_BLOCKS.items():
        cols = block[4]
        sheet = Image.new('RGBA', (cols * FRAME, 4 * FRAME), (0, 0, 0, 0))
        for idx, frame in enumerate(scaled[name]):
            sheet.paste(frame, ((idx % cols) * FRAME, (idx // cols) * FRAME))
        sheets[name] = sheet
    return sheets


def build_effects(rgb):
    sheets = {}
    for name, (rect, count, size, mode) in EFFECT_BLOCKS.items():
        cells = locate_cells(rgb, rect, 1, count, threshold=90, bright=mode)
        frames = []
        for c in cells:
            img = cut_cell(rgb, c, threshold=90, bright=mode)
            if img.width <= 1 and frames:
                # tail frame too faint to key: fade out by shrinking the previous frame
                prev = frames[-1].crop(frames[-1].getbbox())
                img = prev.resize((max(1, int(prev.width * 0.8)), max(1, int(prev.height * 0.8))), Image.NEAREST)
                frames.append(place_in_frame(img, size, bottom_align=False))
                continue
            scale = min((size - 2) / img.width, (size - 2) / img.height)
            frames.append(place_in_frame(downscale(img, scale), size, bottom_align=False))
        sheet = Image.new('RGBA', (count * size, size), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            sheet.paste(f, (i * size, 0))
        sheets[name] = sheet
    return sheets


def contact_sheet(sheets, path, scale=3):
    rows = []
    for name, sheet in sheets.items():
        big = sheet.resize((sheet.width * scale, sheet.height * scale), Image.NEAREST)
        rows.append((name, big))
    w = max(b.width for _, b in rows) + 8
    h = sum(b.height + 20 for _, b in rows)
    out = Image.new('RGBA', (w, h), (48, 48, 64, 255))
    d = ImageDraw.Draw(out)
    y = 0
    for name, big in rows:
        d.text((4, y + 2), f'{name} {big.width // scale}x{big.height // scale}', fill=(255, 255, 0, 255))
        y += 16
        out.paste(big, (4, y), big)
        y += big.height + 4
    out.save(path)


def debug_overlay(rgb, path):
    img = Image.fromarray(rgb).convert('RGBA')
    d = ImageDraw.Draw(img)
    for name, block in CHARACTER_BLOCKS.items():
        for c in locate_cells(rgb, block, 4, block[4], threshold=70):
            d.rectangle(c, outline=(255, 0, 255, 255))
    for name, (rect, count, _, mode) in EFFECT_BLOCKS.items():
        for c in locate_cells(rgb, rect, 1, count, threshold=90, bright=mode):
            d.rectangle(c, outline=(0, 255, 0, 255))
    img.save(path)


def main():
    rgb = np.array(Image.open(SRC).convert('RGB'))
    OUT.mkdir(parents=True, exist_ok=True)
    debug_overlay(rgb, OUT / 'debug-cells.png')
    sheets = {**build_characters(rgb), **build_effects(rgb)}
    for name, sheet in sheets.items():
        sheet.save(OUT / f'{name}.png')
        print(f'{name}.png {sheet.width}x{sheet.height}')
    contact_sheet(sheets, OUT / 'contact-sheet.png')
    print('contact-sheet.png written')


if __name__ == '__main__':
    main()

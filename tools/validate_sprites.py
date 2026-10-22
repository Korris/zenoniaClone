"""Validates the hero sprite set in assets/custom/hero (sizes, RGBA alpha, frame grid, palette).

Usage: python tools/validate_sprites.py [dir]   -> exit code 1 on any failure
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image

DIR = Path(sys.argv[1] if len(sys.argv) > 1 else 'assets/custom/hero')
FRAME = 24
MAX_COLORS = 32
CHARACTER_SHEETS = {  # name: (frames per row, width, height)
    'idle': (4, 96, 96), 'walk': (6, 144, 96), 'attack1': (4, 96, 96), 'attack2': (4, 96, 96),
    'attack3': (6, 144, 96), 'dash': (3, 72, 96), 'hurt': (2, 48, 96), 'death': (5, 120, 96),
}
EFFECT_SHEETS = {  # name: (frames, frame size, width, height)
    'slash_small': (6, 32, 192, 32), 'slash_big': (8, 48, 384, 48), 'dash_trail': (4, 32, 128, 32), 'hit_spark': (5, 16, 80, 16),
}

failures = []


def fail(msg):
    failures.append(msg)
    print('FAIL', msg)


def check_png(path, width, height):
    if not path.exists():
        fail(f'{path.name}: missing')
        return None
    img = Image.open(path)
    if img.mode != 'RGBA':
        fail(f'{path.name}: mode {img.mode}, expected RGBA')
        img = img.convert('RGBA')
    if img.size != (width, height):
        fail(f'{path.name}: size {img.size}, expected {(width, height)}')
    alpha = np.array(img)[..., 3]
    if not (alpha == 0).any():
        fail(f'{path.name}: no transparent pixels (background not removed?)')
    if not ((alpha > 0) & (alpha < 255)).sum() == 0:
        fail(f'{path.name}: semi-transparent pixels found (anti-aliasing?)')
    return img


def check_frames(name, img, frame_w, frame_h, cols, rows, feet_on_bottom):
    arr = np.array(img)[..., 3]
    for r in range(rows):
        for c in range(cols):
            cell = arr[r * frame_h:(r + 1) * frame_h, c * frame_w:(c + 1) * frame_w]
            if cell.sum() == 0:
                fail(f'{name}.png: frame row {r} col {c} is empty')
                continue
            if feet_on_bottom and cell[-1].sum() == 0:
                fail(f'{name}.png: frame row {r} col {c} does not touch the bottom row (pivot)')
    # content must not spill: everything outside the grid is impossible by construction, so check
    # that each frame's content stays off the left/right edge pairs at once (clipped sword tips)
    edges = (arr[:, 0::frame_w].sum() + arr[:, frame_w - 1::frame_w].sum())
    if edges > 0:
        print(f'WARN {name}.png: {int((edges > 0).sum())} frames touch a side edge (wide sword poses are clipped)')


def check_palette(images):
    colors = set()
    for img in images:
        arr = np.array(img)
        opaque = arr[arr[..., 3] == 255][:, :3]
        colors.update(map(tuple, opaque))
    print(f'character palette: {len(colors)} colors')
    if len(colors) > MAX_COLORS:
        fail(f'character sheets use {len(colors)} colors, max {MAX_COLORS}')


def main():
    print(f'validating {DIR}')
    chars = []
    for name, (cols, w, h) in CHARACTER_SHEETS.items():
        img = check_png(DIR / f'{name}.png', w, h)
        if img is None:
            continue
        chars.append(img)
        check_frames(name, img, FRAME, FRAME, cols, 4, feet_on_bottom=True)
    check_palette(chars)
    for name, (count, size, w, h) in EFFECT_SHEETS.items():
        img = check_png(DIR / f'{name}.png', w, h)
        if img is not None:
            check_frames(name, img, size, size, count, 1, feet_on_bottom=False)
    if not (DIR / 'contact-sheet.png').exists():
        fail('contact-sheet.png missing')
    print('RESULT:', 'OK' if not failures else f'{len(failures)} failure(s)')
    sys.exit(1 if failures else 0)


if __name__ == '__main__':
    main()

"""Builds the Frost Hollow battle-map backdrop from the ChatGPT painting in assets/hd/map/FrostHollow/.

Input:  ground-raw-v3.png  1536x1024 opaque painted map (cold Zenonia-5 palette): the whole 36x24-tile
                           backdrop -- same contract as Oakhaven's ground (see build-oakhaven-village-art.py).
Output: assets/hd/map/FrostHollow/ground.webp   2304x1536 = 36*16*4 x 24*16*4 (4 px per world unit), WebP q92.
        assets/hd/map/FrostHollow/tile-grid-preview.png  the painting with the 36x24 tile grid + indices drawn
                           on top -- used to classify GROUND_ROWS in src/world/maps/frost-hollow-map.js by eye.

ChatGPT paints at most 1536x1024, so (like the Oakhaven fallback path) the painting is upscaled 1.5x
with LANCZOS and an unsharp mask restores edge contrast. Safe to rerun.
Usage: python tools/build-frost-hollow-art.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

SRC = Path('assets/hd/map/FrostHollow')
PX_PER_UNIT = 4
TILE = 16
MAP_TILES = (36, 24)


def build_ground():
    size = (MAP_TILES[0] * TILE * PX_PER_UNIT, MAP_TILES[1] * TILE * PX_PER_UNIT)
    raw = Image.open(SRC / 'ground-raw-v3.png').convert('RGB')
    ground = raw.resize(size, Image.Resampling.LANCZOS).filter(
        ImageFilter.UnsharpMask(radius=1.5, percent=110, threshold=2))
    ground.save(SRC / 'ground.webp', quality=92, method=6)
    print(f'  ground {size[0]}x{size[1]}')
    return raw


def grid_preview(raw):
    """Painting at 2x with the tile grid and tx/ty labels, for hand-classifying walkability."""
    cols, rows = MAP_TILES
    img = raw.resize((raw.width * 2, raw.height * 2), Image.Resampling.LANCZOS)
    cw, ch = img.width / cols, img.height / rows
    d = ImageDraw.Draw(img, 'RGBA')
    for tx in range(cols + 1):
        d.line([(tx * cw, 0), (tx * cw, img.height)], fill=(255, 255, 0, 110), width=1)
    for ty in range(rows + 1):
        d.line([(0, ty * ch), (img.width, ty * ch)], fill=(255, 255, 0, 110), width=1)
    for ty in range(rows):
        for tx in range(cols):
            d.text((tx * cw + 3, ty * ch + 2), f'{tx},{ty}', fill=(255, 255, 255, 230))
    img.save(SRC / 'tile-grid-preview.png')
    print(f'  grid preview {img.width}x{img.height}')


if __name__ == '__main__':
    grid_preview(build_ground())

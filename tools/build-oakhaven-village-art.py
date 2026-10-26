"""Builds Oakhaven Village v6 art from the raw ChatGPT sheets in assets/hd/map/oakhaven-v6/.

Inputs (raw, as downloaded from ChatGPT):
  ground-raw-v2.png  1536x1024 opaque painted ground (grass, roads, plaza, river, bridge, dock, farm,
                     forest edge) -- the whole map backdrop, 36x24 tiles.
  houses-raw-v1.png  5 buildings on transparent alpha (faint dark halo), reading order:
                     elder manor, Mia's cottage, hunter lodge / healer cottage, carpenter workshop.
  props-raw-v1.png   15 props on transparent alpha (faint halo), 5x3 reading order (PROP_ORDER).

Outputs:
  assets/hd/map/oakhaven-v6/ground.webp   backdrop at 4 px per world unit (36*16*4 x 24*16*4); lossy
                                          WebP q92 (~1.4 MB vs ~6.7 MB PNG) since it loads at every boot
  assets/hd/props/normalized/<type>.png   one canvas per prop type, sized exactly
                                          PROP_TILES[type] * 16 * 4 px (bottom-centre fit)

Pipeline per sheet: alpha <= HALO_ALPHA -> 0 (removes ChatGPT's glow halo), label connected blobs
on a dilated low-res mask (merges grass tufts / pebbles with their object), drop specks, sort into
reading order (row bands by centre y, then x), tight-crop, premultiplied LANCZOS resize.
PROP_TILES MUST match width/height in src/world/prop-definitions.js. Safe to rerun.
Usage: python tools/build-oakhaven-village-art.py
"""
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

SRC = Path('assets/hd/map/oakhaven-v6')
PROP_OUT = Path('assets/hd/props/normalized')
PX_PER_UNIT = 4
TILE = 16
MAP_TILES = (36, 24)
HALO_ALPHA = 96         # ChatGPT's soft halo sits well below this; object pixels are ~255
DOWN = 4                # labeling resolution divisor
DILATE = 3              # low-res dilation (px) so detached tufts/pebbles join their object
MIN_BLOB = 300          # low-res pixels; smaller blobs are stray specks
ROW_BAND = 140          # sheet px: centres closer than this in y share a row

HOUSE_ORDER = ['elderHouse', 'cottageRed', 'hunterLodge', 'healerCottage', 'carpenterWorkshop']
PROP_ORDER = [
    'well', 'lanternPost', 'marketStall', 'scarecrow', 'woodpile',
    'campfire', 'barrel', 'crate', 'rowboat', 'villageFence',
    'villageOak', 'villagePine', 'villageBush', 'villageFlowerBush', 'villageSignpost',
]
# World size in tiles (w, h) per prop -- keep in sync with src/world/prop-definitions.js.
PROP_TILES = {
    'elderHouse': (5.75, 5.0), 'cottageRed': (3.5, 3.75), 'hunterLodge': (4.25, 3.75),
    'healerCottage': (4.75, 3.75), 'carpenterWorkshop': (6.0, 3.5),
    'well': (2.25, 2.5), 'lanternPost': (1.25, 2.5), 'marketStall': (2.75, 2.5), 'scarecrow': (1.75, 2.0),
    'woodpile': (2.25, 1.875), 'campfire': (1.75, 1.375), 'barrel': (1.0, 1.25), 'crate': (1.25, 1.125),
    'rowboat': (1.25, 2.25), 'villageFence': (2.0, 1.0),
    'villageOak': (3.5, 3.75), 'villagePine': (2.25, 3.75), 'villageBush': (1.75, 1.75),
    'villageFlowerBush': (2.0, 1.75), 'villageSignpost': (1.5, 2.25),
}


def label_blobs(alpha):
    """Connected components of the (dilated, downsampled) opaque mask -> list of full-res bboxes."""
    small = Image.fromarray(((alpha > 0) * 255).astype(np.uint8)).resize(
        (alpha.shape[1] // DOWN, alpha.shape[0] // DOWN), Image.Resampling.BOX)
    mask = np.asarray(small.filter(ImageFilter.MaxFilter(DILATE))) > 0
    seen = np.zeros_like(mask)
    h, w = mask.shape
    boxes = []
    for sy, sx in zip(*np.nonzero(mask)):
        if seen[sy, sx]:
            continue
        q = deque([(sy, sx)])
        seen[sy, sx] = True
        ys, xs = [], []
        while q:
            y, x = q.popleft()
            ys.append(y)
            xs.append(x)
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    q.append((ny, nx))
        if len(ys) >= MIN_BLOB:
            boxes.append((min(xs) * DOWN, min(ys) * DOWN, (max(xs) + 1) * DOWN, (max(ys) + 1) * DOWN))
    return boxes


def reading_order(boxes):
    """Group boxes into rows by centre y, then sort each row left to right."""
    boxes = sorted(boxes, key=lambda b: (b[1] + b[3]) / 2)
    rows = []
    for b in boxes:
        cy = (b[1] + b[3]) / 2
        if rows and abs(cy - rows[-1][0]) < ROW_BAND:
            rows[-1][1].append(b)
        else:
            rows.append([cy, [b]])
    return [b for _, row in rows for b in sorted(row, key=lambda b: b[0])]


def resize_premultiplied(img, size):
    """LANCZOS on premultiplied RGBA so transparent edges don't fringe dark/light."""
    arr = np.asarray(img, np.float32)
    arr[..., :3] *= arr[..., 3:4] / 255
    pre = Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA').resize(size, Image.Resampling.LANCZOS)
    out = np.asarray(pre, np.float32)
    a = out[..., 3:4]
    out[..., :3] = np.where(a > 0, out[..., :3] * 255 / np.maximum(a, 1), 0)
    out[..., 3] = np.where(out[..., 3] > HALO_ALPHA // 2, out[..., 3], 0)
    return Image.fromarray(out.clip(0, 255).astype(np.uint8), 'RGBA')


def cut_sheet(path, names):
    rgba = np.asarray(Image.open(path).convert('RGBA')).copy()
    rgba[..., 3] = np.where(rgba[..., 3] > HALO_ALPHA, rgba[..., 3], 0)
    boxes = reading_order(label_blobs(rgba[..., 3]))
    if len(boxes) != len(names):
        raise SystemExit(f'{path.name}: found {len(boxes)} objects, expected {len(names)}: {boxes}')
    for name, (x0, y0, x1, y1) in zip(names, boxes):
        crop = rgba[y0:y1, x0:x1]
        ys, xs = np.nonzero(crop[..., 3])
        crop = Image.fromarray(crop[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 'RGBA')
        tw, th = PROP_TILES[name]
        size = (round(tw * TILE * PX_PER_UNIT), round(th * TILE * PX_PER_UNIT))
        resize_premultiplied(crop, size).save(PROP_OUT / f'{name}.png')
        print(f'  {name:18s} src {crop.width}x{crop.height} -> {size[0]}x{size[1]}')


def build_ground():
    size = (MAP_TILES[0] * TILE * PX_PER_UNIT, MAP_TILES[1] * TILE * PX_PER_UNIT)
    Image.open(SRC / 'ground-raw-v2.png').convert('RGB').resize(size, Image.Resampling.LANCZOS).save(
        SRC / 'ground.webp', quality=92, method=6)
    print(f'  ground {size[0]}x{size[1]}')


if __name__ == '__main__':
    PROP_OUT.mkdir(parents=True, exist_ok=True)
    build_ground()
    cut_sheet(SRC / 'houses-raw-v1.png', HOUSE_ORDER)
    cut_sheet(SRC / 'props-raw-v1.png', PROP_ORDER)

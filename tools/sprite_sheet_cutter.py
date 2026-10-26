"""Cuts a ChatGPT sprite sheet (separate objects on transparent alpha, faint glow halo) into one
normalized PNG per object. Shared by build-oakhaven-village-art.py (village buildings/props) and
build-wizard-fx.py (spell effects).

Pipeline: alpha <= HALO_ALPHA -> 0 (removes the halo), label connected blobs on a dilated low-res
mask (merges detached tufts/sparkles with their object), drop specks, sort into reading order (row
bands by centre y, then x), tight-crop, premultiplied LANCZOS resize to each object's target size.
"""
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

HALO_ALPHA = 96         # ChatGPT's soft halo sits well below this; object pixels are ~255
DOWN = 4                # labeling resolution divisor
DILATE = 3              # low-res dilation (px) so detached bits join their object
MIN_BLOB = 300          # low-res pixels; smaller blobs are stray specks
ROW_BAND = 140          # sheet px: centres closer than this in y share a row


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


def cut_sheet(path, names, sizes, out_dir):
    """Cut `path` into len(names) objects (reading order) saved as out_dir/<name>.png, each resized
    to sizes[name] = (w, h) px. Raises if the object count doesn't match."""
    rgba = np.asarray(Image.open(path).convert('RGBA')).copy()
    rgba[..., 3] = np.where(rgba[..., 3] > HALO_ALPHA, rgba[..., 3], 0)
    boxes = reading_order(label_blobs(rgba[..., 3]))
    if len(boxes) != len(names):
        raise SystemExit(f'{path.name}: found {len(boxes)} objects, expected {len(names)}: {boxes}')
    for name, (x0, y0, x1, y1) in zip(names, boxes):
        crop = rgba[y0:y1, x0:x1]
        ys, xs = np.nonzero(crop[..., 3])
        crop = Image.fromarray(crop[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 'RGBA')
        resize_premultiplied(crop, sizes[name]).save(out_dir / f'{name}.png')
        print(f'  {name:18s} src {crop.width}x{crop.height} -> {sizes[name][0]}x{sizes[name][1]}')

"""Previews of the head layer in every facing (Tools/build-head-layer.py dirs-split / dirs-check): mask overlays and game-order composites."""
import os

import numpy as np
from PIL import Image

import head_layer_core as core
import head_layer_dirs as hd

BG = (60, 70, 90)
FRAME = core.FRAME


def zoom(rgba, z=3, rows=150):
    return core.on_bg(rgba, BG).convert("RGB").crop((0, 0, FRAME, rows)).resize((FRAME * z, rows * z), Image.NEAREST)


def masks_sheet(cls, out, path, z=3):
    """Per job row: 4 frames with the erased head tinted cyan (kept neck pixels magenta), the style 0 composite, and the original frame."""
    tiles = []
    for (an, d), v in out.items():
        sh = hd.sheet(cls, an)
        row_a, row_b = [], []
        for f in range(4):
            fr = hd.cell(sh, hd.DIR_ROW[d], f)
            vis = np.array(core.on_bg(fr, BG).convert("RGB")).astype(np.float64)
            head = v["heads"][f]
            erased = (v["headless"][f][..., 3] == 0) & (fr[..., 3] > 0)
            vis[erased] = vis[erased] * 0.5 + np.array([0, 255, 255]) * 0.5
            vis[head & ~erased] = vis[head & ~erased] * 0.4 + np.array([255, 0, 255]) * 0.6
            row_a.append(Image.fromarray(vis.astype(np.uint8)).crop((0, 0, FRAME, 150)).resize((FRAME * z, 150 * z), Image.NEAREST))
            row_b.append(zoom(hd.composite(v["headless"][f], hd.head_of_ref(cls, d), v["anchors"][f]), z))
        row_c = [zoom(hd.cell(sh, hd.DIR_ROW[d], f), z) for f in range(4)]
        tiles.append(row_a + row_b + row_c)
    save_grid(tiles, path)


def save_grid(tiles, path):
    w, h = tiles[0][0].size
    im = Image.new("RGB", (w * max(len(r) for r in tiles), h * len(tiles)))
    for r, row in enumerate(tiles):
        for c, t in enumerate(row):
            im.paste(t, (c * w, r * h))
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    im.save(path)
    print("->", path)


def check_sheet(cls, styles, path, z=2):
    """Rows = (anim, dir), columns = styles x 4 frames, composited from the written files the way the game draws."""
    out = hd.read_out(cls)
    tiles = []
    for (an, d), v in out.items():
        row = []
        for s in styles:
            head = hd.head_of(cls, s, d)
            for f in range(4):
                row.append(zoom(hd.composite(v["headless"][f], head, v["anchors"][f]) if head is not None else v["headless"][f], z))
        tiles.append(row)
    save_grid(tiles, path)

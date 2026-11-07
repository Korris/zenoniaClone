"""Shared image helpers of the head layer tool (Tools/build-head-layer.py): frames, alpha maths, breathing warps, the polyline head mask,
sheet and anchor writing. Straight-alpha RGBA float arrays (0-255) unless a name says pm (premultiplied)."""
import json
import os

import numpy as np
from PIL import Image
from scipy import ndimage

FRAME = 192
RES = "Assets/Resources"


def paths(cls):
    body = f"{RES}/Sprites/classes/{cls}/body"
    return {"bare": f"{body}/bare/idle.png", "headless": f"{body}/headless/idle.png", "heads": f"{RES}/Sprites/classes/{cls}/heads",
            "anchors": f"{RES}/Data/classes/{cls}/head-anchors.json", "art": f"Assets/ArtLibrary/classes/{cls}/heads"}


def frames(cls):
    """Row 0 (idle facing down) of the class's bare sheet: 4 straight-alpha frames."""
    a = np.array(Image.open(paths(cls)["bare"]).convert("RGBA")).astype(np.float64)
    return [a[0:FRAME, i * FRAME:(i + 1) * FRAME] for i in range(4)]


def pm(rgba):
    o = rgba.astype(np.float64).copy()
    o[..., :3] *= o[..., 3:4] / 255
    return o


def straight(p, cut=1.0):
    out = p.copy()
    a = np.maximum(p[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(p[..., :3] / a, 0, 255)
    out[..., 3] = np.clip(out[..., 3], 0, 255)
    out[p[..., 3] < cut] = 0
    return out


def u8(rgba):
    return np.clip(rgba, 0, 255).round().astype(np.uint8)


def on_bg(rgba, bg=(0, 255, 0)):
    im = Image.new("RGBA", (rgba.shape[1], rgba.shape[0]), tuple(bg) + (255,))
    im.alpha_composite(Image.fromarray(u8(rgba)))
    return im


def over(dst, src):
    """Straight-alpha src over dst."""
    sa, da = src[..., 3:4] / 255, dst[..., 3:4] / 255
    oa = sa + da * (1 - sa)
    rgb = (src[..., :3] * sa + dst[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6)
    return np.dstack([rgb, oa * 255])


def warp_y(rgba, src_y_of, order=1):
    """Resamples rows: output row centre y takes source row src_y_of(y) (premultiplied, like the GPU for order 1)."""
    p = pm(rgba)
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    sy = src_y_of(y)
    out = np.dstack([ndimage.map_coordinates(p[..., c], [sy, x], order=order, mode="constant") for c in range(4)])
    return straight(np.clip(out, 0, 255))


def scale_src(s, edge, dy=0.0):
    """Source row of a vertical scale by s about the row edge `edge` (frame px), then a move of dy px down."""
    return lambda y: edge - (edge - (y + 0.5 - dy)) / s - 0.5


def place_head(head, a, order=1):
    """Head cell placed for one frame by its anchor {x, y, sy} the way HeroHeadLayer does (scale about the cell bottom, then move)."""
    s = a.get("sy", 1.0) or 1.0
    p = pm(head)
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    sy = FRAME - (FRAME - (y + 0.5 - a["y"])) / s - 0.5
    out = np.dstack([ndimage.map_coordinates(p[..., c], [sy, x - a["x"]], order=order, mode="constant") for c in range(4)])
    return straight(np.clip(out, 0, 255))


def figure_edges(f0):
    rows = np.where((f0[..., 3] > 128).any(1))[0]
    return int(rows[0]), int(rows[-1]) + 1  # top row, sole edge


def measure_scale(fr, rows=110):
    """Per frame: the vertical scale about the sole edge that best maps frame 0 onto it over the top `rows` rows (0.0005 steps)."""
    top, edge = figure_edges(fr[0])
    out = [1.0]
    for f in range(1, 4):
        best = min((np.abs(pm(warp_y(fr[0], scale_src(s, edge), 3))[:rows] - pm(fr[f])[:rows]).mean(), s)
                   for s in np.arange(0.975, 1.0251, 0.0005))
        out.append(round(float(best[1]), 4))
    return out, edge


def polyline(pts):
    """Per column cut row from [(x, y), ...] (linear between points, flat past the ends)."""
    xs, ys = zip(*pts)
    return np.interp(np.arange(FRAME), xs, ys)


def assign_faint(mask, al, limit):
    """Pixels with alpha 1-8 join the side of the nearest solid pixel (else the body keeps a ghost outline)."""
    solid = al > 8
    _, (sy, sx) = ndimage.distance_transform_edt(~solid, return_indices=True)
    rows = np.arange(FRAME)[:, None]
    return mask | ((al > 0) & ~solid & (rows <= limit) & mask[sy, sx])


def largest(mask, extra_min=0):
    """Largest 4-connected region, plus any other region of at least extra_min px (0 = only the largest)."""
    lab, n = ndimage.label(mask)
    if n <= 1:
        return mask
    sizes = ndimage.sum(mask, lab, range(1, n + 1))
    keep = [1 + int(np.argmax(sizes))] + ([1 + i for i, s in enumerate(sizes) if extra_min and s >= extra_min])
    return np.isin(lab, keep)


def write_row0(path, rows):
    """Row 0 of a 768 x 768 sheet; other rows kept (blank on a new sheet)."""
    sheet = np.array(Image.open(path).convert("RGBA")) if os.path.exists(path) else np.zeros((FRAME * 4, FRAME * 4, 4), np.uint8)
    for f, r in enumerate(rows):
        sheet[0:FRAME, f * FRAME:(f + 1) * FRAME] = u8(r)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(sheet).save(path)


def write_anchors(path, frames_xy, tool="Tools/build-head-layer.py"):
    note = ("Head layer offsets per frame (frame px, y down) from the head art's own place (heads/<style>/<dir>.png is a full 192 cell placed "
            f"for frame 0); sy = vertical scale about the cell bottom (breathing), applied before the move. {tool}.")
    data = {"note": note, "anims": [{"anim": "idle", "dir": "down", "frames": frames_xy}]}
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
        fh.write("\n")


def read_anchors(path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)["anims"][0]["frames"]

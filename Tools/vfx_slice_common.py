"""Shared helpers for slicing painted VFX sprite sheets (rows x cols of frames separated by transparent gutters).
Used by Tools/slice-vfx-sheets.py (ranged-attack VFX) and Tools/slice-skill-vfx.py (skill VFX).
Needs numpy, Pillow and opencv-python (cv2 connected components)."""
import json, os
import numpy as np, cv2
from PIL import Image

ALPHA_T = 10        # alpha counted as "ink" when looking for gutters / trimming
SPECK_PX = 12       # connected components smaller than this are stray specks
GUTTER_MIN = 6      # px of empty projection that counts as a gutter


def clean_mask(a, alpha_t=ALPHA_T, speck_px=SPECK_PX):
    """Ink mask of an alpha channel with tiny isolated specks removed."""
    m = (a > alpha_t).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    keep = np.zeros(n, bool)
    keep[1:] = st[1:, cv2.CC_STAT_AREA] >= speck_px
    return keep[lab]


def raw_spans(proj, gutter_min=GUTTER_MIN):
    """All ink spans of a 1-D projection, merged across gaps <= gutter_min."""
    idx = np.flatnonzero(proj > 0)
    if len(idx) == 0: return []
    out, s, p = [], idx[0], idx[0]
    for i in idx[1:]:
        if i - p > gutter_min: out.append((s, p + 1)); s = i
        p = i
    out.append((s, p + 1))
    return out


def spans(proj, count, gutter_min=GUTTER_MIN):
    """Ink spans of a 1-D projection, merged across gaps < gutter_min; keep the `count` largest, in order."""
    out = raw_spans(proj, gutter_min)
    if len(out) != count:
        out = sorted(sorted(out, key=lambda r: -proj[r[0]:r[1]].sum())[:count])
    return out


def trim_box(mask):
    """(y0, y1, x0, x1) bounding box of the ink in a mask (exclusive ends)."""
    ys, xs = np.nonzero(mask)
    return ys.min(), ys.max() + 1, xs.min(), xs.max() + 1


def head_pivot(rgba, mask, pct=75):
    """Projectile pivot = centroid of the white-hot pixels in the right half of the bright core (the head)."""
    rgb = rgba[..., :3].astype(int)
    hot = mask & (rgb.min(axis=2) > 215) & (rgba[..., 3] > 200)
    ys, xs = np.nonzero(hot)
    if len(xs) < 8:
        ys, xs = np.nonzero(mask & (rgba[..., 3] > 200))
    xr = np.percentile(xs, pct)           # head sits at the leading (right) end of the bright core
    sel = xs >= xr
    return float(xs[sel].mean()), float(ys[sel].mean())


def from_black(rgb):
    """Black-background sheet -> RGBA: alpha = max(R,G,B), colour un-premultiplied (glow survives, black vanishes)."""
    rgb = rgb[..., :3].astype(np.float32)
    a = rgb.max(axis=2)
    safe = np.maximum(a, 1.0)[..., None]
    col = np.clip(rgb * 255.0 / safe, 0, 255)
    return np.dstack([col, a]).astype(np.uint8)


def write_frame(folder, name, fr, px, py):
    """Save a trimmed RGBA frame and return its JSON entry (pivot py measured from the bottom)."""
    h, w = fr.shape[:2]
    Image.fromarray(fr).save(os.path.join(folder, name + ".png"))
    return {"name": name, "w": int(w), "h": int(h), "px": round(px, 1), "py": round(h - py, 1)}


def write_json(path, meta):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f: json.dump(meta, f, indent=1)

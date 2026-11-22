"""RO Fantasy heroes: walk / attack source rows whose painted head is 5-6 % off the idle head of the same direction (art batch B report,
plans/reports/ro-fantasy-heroes-art-b-261119.md). Instead of a redraw (coordinator 19/11), the cut fits the BODY on the idle height and
scales only the head region to the idle skull width (smooth blend above the chin, like the NORMALIZE fix of the Mechanic attacks in
docs/sprite-rules.md). Used by Tools/ro_fantasy_body_cut.motion_rows.
"""
import numpy as np
from PIL import Image
from scipy import ndimage

# (class, anim, painted direction): rows cut on the body height + head rescaled (factor measured: idle skull width / row skull width)
HEAD_FIX = {("wizard", "attack", "up"), ("mechanic", "walk", "right"), ("mechanic", "attack", "down-right"),
            ("assassin", "attack", "down-right"), ("cleric", "attack", "right"),
            ("warrior", "walk", "up")}  # warrior V2 22/11: walk-up head drawn ~12 % big
BAND = 14  # blend rows above the chin (head scale fades to 1 at the chin)


def scale_head(cell, k, skull_top, skull_w):
    """Cell (RGBA, feet fixed) with the head scaled by k around the chin centre; the body under the chin keeps every pixel."""
    a = np.asarray(cell).astype(np.float32)
    h, w = a.shape[:2]
    chin = skull_top + 1.0 * skull_w
    al = a[..., 3] > 128
    cols = np.nonzero(al[int(skull_top):int(skull_top + 0.6 * skull_w)].any(0))[0]
    cx = float(cols.mean()) if cols.size else w / 2
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    t = np.clip((chin - yy) / BAND, 0.0, 1.0)  # 1 well above the chin, 0 at / below it
    ke = 1.0 + (k - 1.0) * t
    sy = chin + (yy - chin) / ke
    sx = cx + (xx - cx) / ke
    pm = a.copy()
    pm[..., :3] *= pm[..., 3:4] / 255.0  # premultiplied: no dark fringe from transparent pixels
    out = np.stack([ndimage.map_coordinates(pm[..., c], [sy, sx], order=1, cval=0.0) for c in range(4)], -1)
    alpha = np.clip(out[..., 3:4], 1e-3, None)
    out[..., :3] = np.where(out[..., 3:4] > 0.5, out[..., :3] * 255.0 / alpha, 0)
    return Image.fromarray(np.clip(out, 0, 255).round().astype(np.uint8), "RGBA")

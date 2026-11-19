"""Split a painted hair accessory (Mechanic goggles + strap) out of a layered hair strip (Tools/build-hero-layered-game-assets.py).

The accessory = the class accent pixels of hero_layered_hair.ACCENTS (copper, lens blue, strap leather) + the dark / coloured outline
touching them (not the grey hair). It goes to its own strip (same cells and anchor, drawn over the hair without recolouring, hidden when
any hat is worn: HeroLayeredView). The hole it leaves in the hair is filled with the nearest hair pixel, only inside the hair's closed
silhouette, on the skull cap above the brows and on the upper half of the goggles band of the front / side cells, so the forehead under
the lenses stays bare skin (the body is drawn there) and hats sit on (mostly) full hair.
"""
import numpy as np
from scipy import ndimage

import hero_recolor_core as core
from hero_layered_hair import ACCENTS, AX, AY, CELL_W, REF_W


def split(strip, cls):
    """(hair without the accessory, accessory) RGBA uint8 arrays, or (strip, None) when the class has no accessory / none is left."""
    if cls != "mechanic":
        return strip, None
    a = strip.astype(np.float32)
    rgb = a[..., :3] / 255.0
    h, s, v = core.hsv(rgb)
    solid = strip[..., 3] > 8
    acc = solid & ACCENTS[cls](h, s, v, a[..., :3])  # the accent test takes 0-255 rgb
    acc = ndimage.binary_opening(acc, iterations=1) | (acc & ndimage.binary_dilation(ndimage.binary_opening(acc, iterations=1), iterations=1))
    acc = ndimage.binary_fill_holes(ndimage.binary_closing(acc, iterations=3)) & solid  # whole lenses (white shine, pale blues)
    if acc.sum() < 50:
        return strip, None
    grey = solid & (s < 0.12) & (v > 0.3)
    near = ndimage.binary_dilation(acc, iterations=3)
    edge = near & solid & ~grey & ((v < 0.4) | (s > 0.12))  # outlines and shading of the goggles / strap
    acc = ndimage.binary_fill_holes(acc | edge) & solid
    out_acc = np.zeros_like(strip)
    out_acc[acc] = strip[acc]
    hair = strip.copy()
    hair[acc] = 0
    rim = ndimage.binary_dilation(acc, iterations=3) & ~acc & (v < 0.55)  # the strap's shadow / outline left on the hair
    hair[rim] = 0
    keep = hair[..., 3] > 8
    lab, n = ndimage.label(keep)
    if n > 1:  # specks cut off by the split
        sizes = ndimage.sum(keep, lab, range(1, n + 1))
        keep &= np.isin(lab, [i + 1 for i, z in enumerate(sizes) if z >= 40])
        hair[~keep] = 0
    acc_hole = acc | rim
    inside = (ndimage.binary_closing(keep, structure=np.ones((25, 3), bool)) | ndimage.binary_closing(keep, iterations=9)) & acc_hole  # holes enclosed by hair: refill with the nearest hair pixel
    # front cells (down, down-right; side: added): the goggles sat on the fringe; their upper half was hair, the lower half lies on the forehead (skin)
    front = acc_hole[:, :3 * CELL_W]
    upper = np.zeros_like(front)
    for x in np.nonzero(front.any(0))[0]:
        ys = np.nonzero(front[:, x])[0]
        upper[ys[ys <= (ys[0] + ys[-1]) / 2 + 1], x] = True
    inside[:, :2 * CELL_W] = upper[:, :2 * CELL_W]
    inside[:, 2 * CELL_W:3 * CELL_W] |= upper[:, 2 * CELL_W:]
    # the scalp the goggles covered (ChatGPT painted no hair under them): the skull cap above the brows of every cell
    yy, xx = np.mgrid[0:strip.shape[0], 0:strip.shape[1]]
    cx, cy, r = (xx % CELL_W) - AX, yy - (AY + 0.5 * REF_W), 0.75 * REF_W
    inside |= acc_hole & (cx * cx + cy * cy <= r * r) & (yy < AY + 0.5 * REF_W)
    if inside.any() and keep.any():
        idx = ndimage.distance_transform_edt(~keep, return_distances=False, return_indices=True)
        hair[inside] = strip[idx[0][inside], idx[1][inside]]
    return hair, out_acc

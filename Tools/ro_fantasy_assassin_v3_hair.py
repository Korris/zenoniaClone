"""Asteria V3 Assassin hair (2026-11-26, RO_FANTASY_VARIANT=assassin-v3): small coloured hair accessories kept in the hair layer.

The generic accessory test (build-ro-fantasy-hero-art-check.hair_layer) keeps saturated pieces touching the hair only above 3000 source
px (old Assassin headband, Mechanic goggles). The V3 styles carry small gold pieces (style 2 Shadow Bob: star hair clip + drop earring,
~300-2000 px): they were dropped, leaving a pale hole in the hair. Here: saturated pieces of at least MIN_PX touching the hair, except
red ones (the red eyes and the red scarf collar ChatGPT repaints under each head) and pieces mostly ringed by skin (eyes, mouth).
Called by Tools/ro_fantasy_hair_cells.layers.
"""
import numpy as np
from scipy import ndimage

MIN_PX = 150
SAT = 0.28


def small_accessories(hr, keep, skin):
    r, g, b, a = (hr[..., i] for i in range(4))
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    red = (r > 0.45) & (g - b < 0.08) & (r - g > 0.25)  # red eyes / scarf: split off first, an eye touching a clip merged with it
    cand = (a > 0) & (s > SAT) & ~keep & ~(g - np.maximum(r, b) > 0.1) & ~red
    near = ndimage.binary_dilation(keep, iterations=6)
    lab, n = ndimage.label(cand, structure=np.ones((3, 3), bool))
    out = np.zeros(cand.shape, bool)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        if m.sum() < MIN_PX or not near[sl][m].any():
            continue
        px = hr[sl][m]
        if np.median(px[:, 0]) > 0.45 and np.median(px[:, 1] - px[:, 2]) < 0.05:
            continue  # red: eyes, scarf
        pad = 3
        y0, x0 = max(0, sl[0].start - pad), max(0, sl[1].start - pad)
        y1, x1 = sl[0].stop + pad, sl[1].stop + pad
        mm = lab[y0:y1, x0:x1] == i
        ring = ndimage.binary_dilation(mm, iterations=pad) & ~mm
        if ring.any() and skin[y0:y1, x0:x1][ring].mean() > 0.6:
            continue  # ringed by skin: a facial feature
        out[sl] |= ndimage.binary_fill_holes(m) & (a[sl] > 0)  # the red gem set in the gold clip
    # red ribbons (style 3 half-up): tall narrow red pieces touching the hair; eyes are round and ringed by skin, the scarf collar is wide
    lab, n = ndimage.label(red & (a > 0) & ~keep, structure=np.ones((3, 3), bool))
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if m.sum() >= MIN_PX and h > 1.6 * w and near[sl][m].any() and skin[sl][ndimage.binary_dilation(m, iterations=3) & ~m].mean() < 0.3:
            out[sl] |= m
    return out & ~(g > np.maximum(r, b))  # key spill at ribbon / clip edges (green specks): never an accessory colour here

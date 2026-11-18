"""Hair layer of the layered hero (Tools/build-hero-layered-game-assets.py): ChatGPT hair strips -> per-direction hair cells.

A hair source (src/hair-<class>-5dir-chatgpt-N.png, ~2172x724) is the bald-head reference strip (ref/bald-head-<gender>-5dir.png,
a 3x upscale of the 5 idle heads cropped by build-hero-layered-sheets.refs) shrunk and padded by ChatGPT, hair painted on top.
fit_head() finds, per head, the map cell px (idle cell of that direction) -> source px by matching the visible skin (face, ears)
of the bald idle head with the source; extract() keeps only the hair (pixels that differ from the bald head, grey or a class accent
colour, never skin / collar), turns the grey into neutral grey stretched on the HeroRecolor ramp; cut() resamples one head into a
hair cell whose anchor (AX, AY) is the skull top and whose scale matches skull width REF_W.
"""
import numpy as np
from PIL import Image
from scipy import ndimage

import hero_recolor_core as core

CELL_W, CELL_H, AX, AY, REF_W = 224, 192, 112, 40, 75.0
# Accent colours kept as painted (excluded from the dye): Mechanic goggles (copper + light blue lens), Assassin red ribbon.
ACCENTS = {
    'mechanic': lambda h, s, v, rgb: ((h >= 15) & (h <= 45) & (s > 0.35) & (v > 0.35)) | ((rgb[..., 2] > rgb[..., 0] + 30) & (v > 0.45)),
    'assassin': lambda h, s, v, rgb: ((h <= 15) | (h >= 345)) & (s > 0.45) & (v > 0.35),
}
# grey lightness percentiles -> ramp knots of HeroRecolor.shader: outline ~#242424, body of the hair = the palette colour, shine ~#E0E0E0
L_PCT, L_OUT = [0, 3, 50, 97, 100], [0.10, 0.14, 0.60, 0.88, 0.95]


def skin(a):
    r, g, b, al = (a[..., i].astype(int) for i in range(4))
    return (al > 128) & (r > 200) & (g > 150) & (b > 110) & (r - b > 25) & (r - b < 110)


def head_crop(info):
    """Crop box of a head in its idle cell, exactly as build-hero-layered-sheets.refs cut the reference strip."""
    x, top, w = info
    return round(x - w * 0.8), top - 10, round(x + w * 0.8), top + round(w * 1.15)


def nominal(infos, cells, src_size, ref_scale=3):
    """Per head (cell_x0, cell_y0, S, ox, oy): source px = (cell px - cell_x0/y0) * S + (ox, oy), from the strip layout + ChatGPT's resize."""
    crops = [head_crop(i) for i in infos]
    hw = max(c[2] - c[0] for c in crops)
    hh = max(c[3] - c[1] for c in crops)
    sw, sh = ((hw + 40) * 5 + 40) * ref_scale, (hh + 80) * ref_scale
    s = src_size[0] / sw                      # ChatGPT keeps the width, pads the height
    pad_y = (src_size[1] - sh * s) / 2
    out = []
    for i, c in enumerate(crops):
        px = 40 + i * (hw + 40) + (hw - (c[2] - c[0])) // 2
        out.append((c[0], c[1], ref_scale * s, px * ref_scale * s, 40 * ref_scale * s + pad_y))
    return out


def bald_in_source(cell, crop, S):
    """The idle cell's head crop upscaled by S (RGBA uint8)."""
    h = cell.crop(crop)
    return np.asarray(h.resize((max(1, round(h.width * S)), max(1, round(h.height * S))), Image.LANCZOS))


def fit_head(src, cell, info, nom, S, search=18):
    """Best (score, S, ox, oy) of one head at source scale S: bald skin over source skin, bald skin over empty background penalised
    (else a too large head wins: the hair hides the skull)."""
    x0, y0, S0, ox0, oy0 = nom
    crop = head_crop(info)
    src_skin, src_clear = skin(src), src[..., 3] < 128
    bm = skin(bald_in_source(cell, crop, S))
    hgt, wid = bm.shape
    cx, cy = ox0 * S / S0, src.shape[0] / 2 + (oy0 - src.shape[0] / 2) * S / S0  # the whole strip scales together
    best = None
    for dy in range(-search, search + 1, 2):
        for dx in range(-search, search + 1, 2):
            X, Y = int(round(cx + dx)), int(round(cy + dy))
            if X < 0 or Y < 0 or X + wid > src.shape[1] or Y + hgt > src.shape[0]:
                continue
            ss, sc = src_skin[Y:Y + hgt, X:X + wid], src_clear[Y:Y + hgt, X:X + wid]
            score = np.count_nonzero(ss & bm) - 2 * np.count_nonzero(ss & ~bm) - 2 * np.count_nonzero(sc & bm)
            if best is None or score > best[0]:
                best = (score, X, Y)
    score, X, Y = best
    # cell px p -> source px: (p - crop origin) * S + (X, Y)
    return score, (S, X - crop[0] * S, Y - crop[1] * S)


def fit_all(src, cells, infos, noms):
    """One ChatGPT resize for the whole strip: the scale with the best summed score, then each head's own offset."""
    S0 = noms[0][2]
    best = None
    for S in np.arange(S0 * 0.94, S0 * 1.06 + 1e-9, S0 * 0.006):
        got = [fit_head(src, c, i, n, S) for c, i, n in zip(cells, infos, noms)]
        total = sum(g[0] for g in got)
        if best is None or total > best[0]:
            best = (total, [g[1] for g in got])
    return best[1]


def extract(src, cell, info, fit, cls, xlim=None):
    """Hair-only RGBA (source resolution, same size as src) for one head; grey made neutral and stretched to the ramp."""
    S, ox, oy = fit
    crop = head_crop(info)
    bald = bald_in_source(cell, crop, S)
    X, Y = int(round(crop[0] * S + ox)), int(round(crop[1] * S + oy))
    ref = np.zeros_like(src)
    hgt, wid = bald.shape[:2]
    ys, xs = slice(max(Y, 0), min(Y + hgt, src.shape[0])), slice(max(X, 0), min(X + wid, src.shape[1]))
    ref[ys, xs] = bald[ys.start - Y:ys.stop - Y, xs.start - X:xs.stop - X]
    rgbf = src[..., :3].astype(np.float32)
    h, s, v = core.hsv(rgbf / 255.0)
    differs = (ref[..., 3] < 128) | (np.abs(rgbf - ref[..., :3].astype(np.float32)).max(-1) > 45)
    chroma = rgbf.max(-1) - rgbf.min(-1)
    grey = chroma <= 34
    ref_c = ref[..., :3].astype(np.float32)
    differs |= grey & (ref_c.max(-1) - ref_c.min(-1) > 34)  # light grey hair over the skull's skin shine is still hair
    accent = ACCENTS[cls](h, s, v, rgbf) if cls in ACCENTS else np.zeros(grey.shape, bool)
    magenta = core.outfit_mask(src)
    keep = (src[..., 3] > 8) & differs & ~skin(src) & ~magenta & (grey | accent)
    # region of this head only: a box around the bald head, wide enough for a ponytail
    region = np.zeros_like(keep)
    xa, xb = (max(0, X - int(wid * 0.75)), X + int(wid * 1.75)) if xlim is None else (max(0, int(xlim[0])), int(xlim[1]))
    region[max(0, Y - int(hgt * 0.4)):Y + int(hgt * 1.25), xa:xb] = True  # xlim: halfway to the next heads (a ponytail reaches far)
    keep &= region
    dark = (v < 0.32) & (src[..., 3] > 8) & differs & ~magenta  # outlines of the hair / goggles in a brownish black
    _, _, ref_v = core.hsv(ref[..., :3].astype(np.float32) / 255.0)
    dark &= ~ndimage.binary_dilation((ref[..., 3] > 128) & (ref_v < 0.45), iterations=3)  # not the face / jaw outline redrawn 1-2 px off
    keep |= dark & ndimage.binary_dilation(keep, iterations=3) & region
    lab, n = ndimage.label(ndimage.binary_dilation(keep, iterations=2))
    if n > 1:  # drop specks: keep blobs of at least 3 % of the largest
        sizes = ndimage.sum(keep, lab, range(1, n + 1))
        keep &= np.isin(lab, [i + 1 for i, z in enumerate(sizes) if z >= 0.03 * sizes.max()])
    out = np.zeros_like(src)
    out[keep] = src[keep]
    g = keep & ~accent
    lum = (rgbf @ np.array([0.299, 0.587, 0.114], np.float32)) / 255.0
    if g.any():
        lo = np.percentile(lum[g], L_PCT)
        nl = np.interp(lum, lo, L_OUT) * 255.0
        for c in range(3):
            out[..., c] = np.where(g, nl, out[..., c])
    return out


def cut(hair_src, fit, info, dx=0.0, dy=0.0, grow=1.0):
    """Hair cell (CELL_W x CELL_H RGBA): skull top of the idle head on (AX, AY), skull width scaled to REF_W. dx, dy, grow: the hair moved
    / grown (around the skull centre) on the idle head, from best_cut."""
    S, ox, oy = fit
    x, top, w = info
    k = w / REF_W                                   # cell px per hair-cell px
    cy = top + 0.5 * w
    def src_pt(px, py):
        cx_, cy_ = x + (px - AX) * k, top + (py - AY) * k  # idle cell px of this hair-cell px
        cx_, cy_ = x + (cx_ - x - dx) / grow, cy + (cy_ - cy - dy) / grow
        return cx_ * S + ox, cy_ * S + oy
    x0, y0 = src_pt(0, 0)
    x1, y1 = src_pt(CELL_W, CELL_H)
    pad = 400  # the box may reach past the source edge (first / last head)
    a = np.pad(hair_src.astype(np.float32), ((pad, pad), (pad, pad), (0, 0)))
    pm = Image.fromarray(np.dstack([a[..., :3] * a[..., 3:4] / 255.0, a[..., 3]]).clip(0, 255).astype(np.uint8))
    box = (x0 + pad, y0 + pad, x1 + pad, y1 + pad)
    r = np.asarray(pm.resize((CELL_W, CELL_H), Image.LANCZOS, box=box)).astype(np.float32)
    al = r[..., 3:4]
    rgb = np.where(al > 0, r[..., :3] * 255.0 / np.maximum(al, 1), 0)
    out = np.dstack([rgb, al]).clip(0, 255).astype(np.uint8)
    out[out[..., 3] < 6] = 0
    return out


def best_cut(hair_src, fit, info, crown):
    """ChatGPT's hair sits a few px off the skull in places (a brown arc of the bald skull outline showed at the crown): try small shifts and
    growths of the hair on the idle head, keep the one leaving the fewest crown pixels (`crown`: bool mask in idle cell px) uncovered."""
    x, top, w = info
    k = w / REF_W
    S, ox, oy = fit
    # only the source around this head (speed): the hair cell's source box plus a margin for the shifts
    x0, y0 = int((x - AX * k - 12) * S + ox), int((top - AY * k - 12) * S + oy)
    x1, y1 = int((x + (CELL_W - AX) * k + 12) * S + ox), int((top + (CELL_H - AY) * k + 12) * S + oy)
    x0, y0 = max(x0, 0), max(y0, 0)
    hair_src = hair_src[y0:max(y1, y0 + 1), x0:max(x1, x0 + 1)]
    fit = (S, ox - x0, oy - y0)
    best = None
    for grow in (1.0, 1.03, 1.06):
        for dy in range(-3, 4):
            for dx in range(-4, 5):
                c = cut(hair_src, fit, info, dx, dy, grow)
                lay = np.zeros(crown.shape, bool)
                # hair cell px -> idle cell px at the idle frame (k = w / REF_W): nearest sampling is enough for a count
                ys, xs = np.nonzero(c[..., 3] > 100)
                cx = np.round(x + (xs - AX) * k).astype(int)
                cy = np.round(top + (ys - AY) * k).astype(int)
                ok = (cx >= 0) & (cy >= 0) & (cx < crown.shape[1]) & (cy < crown.shape[0])
                lay[cy[ok], cx[ok]] = True
                lay = ndimage.binary_closing(lay, iterations=1)
                score = np.count_nonzero(crown & ~lay) + 6 * (grow - 1) * 100 + 0.5 * (abs(dx) + abs(dy))
                if best is None or score < best[0]:
                    best = (score, c, dx, dy, grow, np.count_nonzero(crown & ~lay))
    return best[1:]

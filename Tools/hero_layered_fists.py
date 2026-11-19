"""Fist detection on the layered hero body sheets (Tools/build-hero-layered-grips.py).

The gloves are brown leather (#8A5A34 / #5E3A1E), the same leather as the belt and the boots. A glove blob is found as: saturated
leather pixels (hue 8-40, S > 0.4, V > 0.28), opened with a 4 px disc (drops the thin belt band, outline strokes, eyes), components above
the boots, not inside / on the outline of the head (eyes, ears), not a short blob on the belt row (buckle). The fist centre is the centroid
of the leather pixels of the component (grown back 3 px into the unopened mask).

assign(): which blob is the character's RIGHT (R, weapon) / LEFT (L, offhand) fist on a painted row: down / down-right: R = smaller x;
up / up-right: R = larger x; right (side view): R = the near hand = the bigger blob (the far one is partly hidden). One blob of a pair
missing = that hand is hidden (position guessed, `vis` false); one big blob = both fists together (two-hand grip). Frames the rules get
wrong go into OVERRIDES of Tools/build-hero-layered-grips.py.
"""
import numpy as np
from scipy import ndimage

CW, CH, FEET = 320, 256, 244
DEBUG = False


def hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn + 1e-6
    h = np.where(mx == r, (g - b) / d % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, d / (mx + 1e-6), mx


def leather(cell):
    a = np.asarray(cell).astype(np.float32)
    h, s, v = hsv(a[..., :3] / 255.0)
    return (a[..., 3] > 128) & (h >= 8) & (h <= 40) & (s > 0.4) & (v > 0.28)


def belt_row(lm, cx, y0=145, y1=186, need=18):
    """Cell row of the belt: the row with the longest horizontal leather run (>= need px) in the torso band, else None."""
    best, by = 0, None
    for y in range(y0, y1):
        row = np.concatenate(([0], lm[y].astype(np.int8), [0]))
        d = np.diff(row)
        st, en = np.nonzero(d == 1)[0], np.nonzero(d == -1)[0]
        ok = (st <= cx + 22) & (en >= cx - 22) & (en - st <= 70)  # the belt crosses the body centre (an arm held out does not)
        if ok.any():
            r = int((en - st)[ok].max())
            if r > best:
                best, by = r, y
    return by if best >= need else None


def disc(r):
    y, x = np.mgrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= r * r


def skin_of(cell):
    al = np.asarray(cell)[..., 3] > 128
    px = np.asarray(cell).astype(int)
    return al & (px[..., 0] > 200) & (px[..., 1] > 150) & (px[..., 2] > 110) & (px[..., 0] - px[..., 2] > 20)


def head_mask(cell):
    """Bald head + face: the largest skin blob above row 150 (bare arms cut off by a 6 px opening), grown 3 px, holes filled."""
    skin = skin_of(cell)
    lab0, n0 = ndimage.label(ndimage.binary_opening(skin & (np.arange(skin.shape[0])[:, None] < 150), structure=disc(6)))
    if not n0:
        return np.zeros_like(skin)
    big = int(np.argmax(ndimage.sum(skin, lab0, range(1, n0 + 1)))) + 1
    return ndimage.binary_fill_holes(ndimage.binary_dilation(lab0 == big, iterations=3))


def fists(cell, belt=None):
    """[(x, y, area, mask)] glove blobs of a 320x256 cell (cell px, y down), biggest first."""
    lm = leather(cell)
    al = np.asarray(cell)[..., 3] > 128
    skin = skin_of(cell)
    ys = np.nonzero(al.any(1))[0]
    bottom = ys[-1] if len(ys) else FEET
    head = head_mask(cell)
    near_head = ndimage.binary_dilation(head, iterations=8)
    core = ndimage.binary_opening(lm, structure=disc(3))
    if belt is not None:  # boots: everything 30 px under the belt (a fist touching a boot top would merge with it)
        core[int(belt + 30):] = False
    lab, n = ndimage.label(core)
    out = []
    for i in range(1, n + 1):
        m = lab == i
        yy, xx = np.nonzero(m)
        if len(yy) < 40:
            continue
        h, w = yy.max() - yy.min() + 1, xx.max() - xx.min() + 1
        if yy.max() > bottom - 30 and yy.min() > bottom - 70:  # boots
            continue
        full = ndimage.binary_dilation(m, iterations=3) & lm
        fy, fx = np.nonzero(full)
        fh = fy.max() - fy.min() + 1
        fw = fx.max() - fx.min() + 1
        if full.sum() < 140 or fh < 12 or fy.mean() > bottom - 40:  # collar lace, belt buckle bits
            continue
        if DEBUG: print("cand", round(fx.mean()), round(fy.mean()), int(full.sum()), fy.min(), fy.max(), "head", head[int(round(fy.mean())), int(round(fx.mean()))], "belt", belt)
        cyx = int(round(fy.mean())), int(round(fx.mean()))
        if head[cyx]:  # eyes, ear: brown inside the face
            continue
        ring = ndimage.binary_dilation(full, iterations=4) & ~ndimage.binary_dilation(full, iterations=1)
        if near_head[cyx] and skin[ring].mean() > 0.45:  # ear on the head outline
            continue
        if belt is not None and fy.mean() > belt + 24:  # boot cuffs (fists hang at most ~ 15 px under the belt)
            continue
        if belt is not None and abs(fy.mean() - belt) <= 9 and fh <= 17:  # belt band + buckle (a fist is taller)
            continue
        out.append((float(fx.mean()), float(fy.mean()), int(full.sum()), full))
    out.sort(key=lambda t: -t[2])
    return out


def ring_cover(cell, x, y, r0=11, r1=19):
    """Share of opaque body pixels in a ring around (x, y): high = the fist lies over the torso, low = out in the air."""
    al = np.asarray(cell)[..., 3] > 128
    yy, xx = np.mgrid[0:al.shape[0], 0:al.shape[1]]
    d2 = (xx - x) ** 2 + (yy - y) ** 2
    ring = (d2 >= r0 * r0) & (d2 <= r1 * r1)
    return float(al[ring].mean()) if ring.any() else 0.0


def hand_mask(cell, blob):
    """Pixels of the hand layer for one glove blob: the blob + its dark outline (within 2 px, any dark brown / grey)."""
    a = np.asarray(cell).astype(np.float32)
    v = a[..., :3].max(-1) / 255.0
    dark = (a[..., 3] > 20) & (v < 0.45)
    grown = ndimage.binary_dilation(blob, iterations=2)
    m = blob | (grown & dark)
    return ndimage.binary_fill_holes(m)


def assign(blobs, row, head_x, prev=None):
    """{'R': [x, y, visible], 'L': [...]} for the painted row (0 down .. 4 up). prev: the previous frame's table (hidden-hand guess)."""
    b = [(x, y, a) for x, y, a, _ in blobs[:2]]
    if len(b) == 2:
        if row == 2:
            near, far = (b[0], b[1]) if b[0][2] >= b[1][2] else (b[1], b[0])
            return {"R": [near[0], near[1], True], "L": [far[0], far[1], True]}
        lo, hi = sorted(b, key=lambda t: t[0])
        r, l = (lo, hi) if row in (0, 1) else (hi, lo)
        return {"R": [r[0], r[1], True], "L": [l[0], l[1], True]}
    if len(b) == 1:
        x, y, a = b[0]
        if a > 520:  # both fists together
            return {"R": [x, y, True], "L": [x, y, True]}
        side_right = x > head_x
        if row == 2:
            mine = "R"
        elif row in (0, 1):
            mine = "L" if side_right else "R"
        else:
            mine = "R" if side_right else "L"
        other = "L" if mine == "R" else "R"
        out = {mine: [x, y, True]}
        out[other] = [head_x + 0.3 * (x - head_x), max(y, 168.0), False]  # hidden far hand: at the hip behind the body
        return out
    if prev is not None:
        return {k: [v[0], v[1], False] for k, v in prev.items()}
    return {"R": [head_x - 25, 175, False], "L": [head_x + 25, 175, False]}

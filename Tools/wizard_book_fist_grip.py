"""Wizard spellbook held in the closed left fist, idle facing down (2026-11-08, designer: one book size for every tier, the fist grips it).

Used by Tools/build-wizard-tier-staff-book.py (`write book`, `check`):
  - fist_masks(t): the left fist of tier t (body/set-tN, T0 = Lv1 body) cut from the bare idle-down frames by skin colour + its dark
    outline, one mask per breathing frame. The old hand layers came from the Lv1 fist mask moved per tier and missed on some frames
    (T6 frames 0, 2 drew the sleeve, T5/T6/T9 a strip of cape), which would paint robe over the book.
  - write_hand_layers(): row 0 of every tier's hand/idle.png = right fist kept (x < SPLIT_X) + this left fist, drawn over the book.
  - book_grip(...): grip of a book art so its lower rim at the inner (body side) part sits across the fist: the rim crosses the fist at
    RIM_IN of its height (knuckles above the rim, the rest of the fist in front of the cover), the fist centre INNER of the book width
    from the book's inner edge. bob_y(t): per-frame y of the fist (OverlayEntry.bobY) so book and fist breathe together.
"""
import numpy as np
from PIL import Image
from scipy import ndimage

FRAME = 192
SPLIT_X = 100                 # hand layer: x < SPLIT_X is the staff (right) fist, kept as it is
# search box of the left fist per tier (x0, y0, x1, y1), from the fist + 2 px: a loose box takes the pale cape of T8 or the gold-white
# cuff of T3/T9, both skin-like
FIST_BOX = [(116, 121, 133, 144), (116, 120, 135, 141), (114, 119, 133, 140), (116, 121, 134, 143), (115, 123, 134, 146),
            (115, 113, 134, 136), (114, 118, 134, 141), (115, 117, 136, 139), (116, 117, 138, 139), (117, 114, 133, 139)]
INNER = 0.18                  # fist centre this fraction of the book width from the book's inner (left) edge
RIM_IN = 0.42                 # the book's lower rim crosses the fist this far down from the fist top


def sheet_dir(body, t):
    return f"{body}/set-t{t}" if t else body


def _skin(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (r > 140) & (r - g > 12) & (b > 0.42 * r) & (g > 0.45 * r) & (g - b < 50) & (b <= g + 4)  # not the gold-white or lavender cuffs


def fist_mask(frame_rgba, t):
    """Boolean FRAME x FRAME mask of the left fist in one bare frame (float/uint8 RGBA array)."""
    a = frame_rgba.astype(np.float32)
    x0, y0, x1, y1 = FIST_BOX[t]
    box = np.zeros((FRAME, FRAME), bool)
    box[y0:y1, x0:x1] = True
    skin = _skin(a[..., :3]) & (a[..., 3] > 128) & box
    lab, n = ndimage.label(skin, structure=np.ones((3, 3)))
    if not n:
        raise RuntimeError("no fist found")
    sizes = ndimage.sum_labels(np.ones_like(lab), lab, index=np.arange(1, n + 1))
    m = lab == (int(np.argmax(sizes)) + 1)
    m = ndimage.binary_fill_holes(ndimage.binary_closing(m, iterations=1) | m)
    lum = a[..., :3].mean(-1)
    ring = ndimage.binary_dilation(m, iterations=1) & ~m & (a[..., 3] > 60) & (lum < 110) & box  # the dark outline round the fist
    return ndimage.binary_fill_holes(m | ring)


def fist_masks(body, t):
    sh = np.asarray(Image.open(f"{sheet_dir(body, t)}/bare/idle.png").convert("RGBA"))
    return [fist_mask(sh[:FRAME, f * FRAME:(f + 1) * FRAME], t) for f in range(4)]


def fist_box(m):
    ys, xs = np.where(m)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def grip_point(m):
    """Frame point the book's grip goes to: fist centre x, RIM_IN down the fist."""
    x0, y0, x1, y1 = fist_box(m)
    return (x0 + x1) / 2, y0 + RIM_IN * (y1 - y0)


def bob_y(masks):
    """Integer per-frame y offset of the fist (mean row of the mask) from frame 0."""
    c0 = np.where(masks[0])[0].mean()
    return [int(round(np.where(m)[0].mean() - c0)) for m in masks]


def book_grip(rgba, anchor, s, fist0):
    """(gripX, gripY) in art px: the book's lower rim at the column INNER of its width from the inner edge lands on grip_point(fist0)."""
    a = rgba[..., 3] > 100
    ys, xs = np.where(a)
    cx = int(round(xs.min() + INNER * (xs.max() - xs.min())))
    rim = np.where(a[:, cx])[0].max()     # rim at that column (perspective: the cover's lowest point is elsewhere)
    px, py = grip_point(fist0)
    return cx - (px - anchor[0]) / s, rim - (py - anchor[1]) / s


def write_hand_layers(body, tiers=range(10)):
    """Row 0 of hand/idle.png per tier: right fist kept, left fist recut from the bare frames."""
    for t in tiers:
        d = sheet_dir(body, t)
        bare = np.asarray(Image.open(f"{d}/bare/idle.png").convert("RGBA"))
        hand = np.asarray(Image.open(f"{d}/hand/idle.png").convert("RGBA")).copy()
        for f, m in enumerate(fist_masks(body, t)):
            sl = (slice(0, FRAME), slice(f * FRAME, (f + 1) * FRAME))
            cell = hand[sl].copy()
            cell[:, SPLIT_X:] = 0
            src = bare[sl]
            cell[m] = src[m]
            hand[sl] = cell
        Image.fromarray(hand).save(f"{d}/hand/idle.png")

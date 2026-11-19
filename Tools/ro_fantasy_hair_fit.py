"""Fit one ChatGPT hair head (drawn on top of the bald head ref) back onto the bald body head.

Score for a given scale + offset (computed for every offset at once with FFT correlation):
  + 2 * skin of the hair image landing on body skin   (face, ears, nape line up)
  - 3 * skin of the hair image landing off body skin  (face shifted onto armour / background)
  - 3 * body skull pixels not covered by the hair image (skull must sit inside hair + face)
"""
import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.signal import fftconvolve

DOWN = 2  # work at half resolution


def _small(mask, s):
    h, w = mask.shape
    im = Image.fromarray(mask.astype(np.uint8) * 255)
    im = im.resize((max(1, int(w * s / DOWN)), max(1, int(h * s / DOWN))), Image.BILINEAR)
    return np.asarray(im) > 127


def _corr(a, b):
    """corr[y, x] = sum(a[y + i, x + j] * b[i, j]) for every placement of b inside padded a."""
    return fftconvolve(a.astype(np.float32), b[::-1, ::-1].astype(np.float32), mode='full')


def fit(body_skin, body_alpha, head_box, hair_skin, hair_alpha, scales):
    """Return (score, scale, x, y): hair cell top-left in body pixel coords at that scale."""
    bx0, bx1, by0, by1 = head_box
    pad = int((bx1 - bx0) * 0.6)
    y0, x0 = max(0, by0 - pad), max(0, bx0 - pad)
    y1, x1 = by1 + pad // 2, bx1 + pad
    bs = _small(body_skin[y0:y1, x0:x1], 1)
    head = np.zeros_like(body_alpha, bool)
    head[by0:by1, bx0:bx1] = body_alpha[by0:by1, bx0:bx1]
    # skull only (upper 70 % of the head box), the chin line can differ
    head[by0 + int((by1 - by0) * 0.7):by1] = False
    bh = _small(head[y0:y1, x0:x1], 1)
    bs_d = ndimage.binary_dilation(bs, iterations=2)
    best = None
    for s in scales:
        hs = _small(hair_skin, s)
        ha = _small(hair_alpha, s)
        if hs.shape[0] >= bs.shape[0] + 200 or hs.shape[1] >= bs.shape[1] + 200:
            continue
        good = _corr(bs, hs)
        bad = _corr(~bs_d, hs)
        # skull pixels uncovered = skull_total - covered
        covered = fftconvolve(bh.astype(np.float32), ha[::-1, ::-1].astype(np.float32), mode='full')
        # fractions: share of body face covered, share of hair-image face off body, share of skull uncovered
        n = max(hs.sum(), 1)
        score = 2 * good / max(bs.sum(), 1) - 3 * bad / n - 3 * (bh.sum() - covered) / max(bh.sum(), 1)
        iy, ix = np.unravel_index(np.argmax(score), score.shape)
        sc = score[iy, ix]
        if best is None or sc > best[0]:
            # full-mode index -> top-left of hair cell inside the padded body window
            ty, tx = iy - (hs.shape[0] - 1), ix - (hs.shape[1] - 1)
            best = (sc, s, x0 + tx * DOWN, y0 + ty * DOWN)
    return best

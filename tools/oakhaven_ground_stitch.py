"""Stitches ChatGPT's 4 high-res quadrant redraws of the Oakhaven ground into one sharp backdrop.

Why: ChatGPT paints at most 1536x1024, but the backdrop needs 2304x1536, so a single painting must
be UPSCALED (soft). Instead the 1536x1024 painting (ground-raw-v2.png) is cut into 4 overlapping
864x576 quadrants (QUADRANTS), each upscaled to 1536x1024 and redrawn by ChatGPT with fine detail
while keeping every element in place. Each redraw is then DOWNscaled into its slot (sharp), and:
  1. aligned to the original by phase correlation (redraws drift a few px), translation only;
  2. colour-matched (per-channel mean/std) to the original, so quadrants don't differ in tone;
  3. feather-blended across the overlaps with linear ramps, so no seam shows.
Collision (GROUND_ROWS in oakhaven-village-map.js) was classified from ground-raw-v2.png, and the
alignment keeps the stitched result registered to it.
"""
import numpy as np
from PIL import Image, ImageFilter

RAW_SIZE = (1536, 1024)
CROP = (864, 576)
# name -> top-left of its crop in raw (ground-raw-v2.png) pixels
QUADRANTS = {'nw': (0, 0), 'ne': (672, 0), 'sw': (0, 448), 'se': (672, 448)}
MAX_SHIFT = 24  # px in output space; larger "shifts" are correlation noise, ignored
FEATHER = 96    # output px of cross-fade, centred in each 288/192 px overlap


def quadrant_inputs(raw):
    """The 4 upscaled crops to send to ChatGPT (name -> 1536x1024 image)."""
    return {n: raw.crop((x, y, x + CROP[0], y + CROP[1])).resize(RAW_SIZE, Image.Resampling.LANCZOS)
            for n, (x, y) in QUADRANTS.items()}


def _gray(img):
    return np.asarray(img.convert('L').filter(ImageFilter.GaussianBlur(2)), np.float32)


def _phase_shift(ref, moving):
    """(dy, dx) to apply to `moving` so it lines up with `ref` (same-size grayscale arrays)."""
    f = np.fft.fft2(ref - ref.mean()) * np.conj(np.fft.fft2(moving - moving.mean()))
    corr = np.fft.ifft2(f / (np.abs(f) + 1e-6)).real
    dy, dx = np.unravel_index(np.argmax(corr), corr.shape)
    h, w = corr.shape
    dy = dy - h if dy > h // 2 else dy
    dx = dx - w if dx > w // 2 else dx
    return (int(dy), int(dx)) if max(abs(dy), abs(dx)) <= MAX_SHIFT else (0, 0)


def _match_colour(arr, ref):
    """Per-channel mean/std transfer from `ref` onto `arr` (float RGB arrays)."""
    out = arr.copy()
    for c in range(3):
        a, r = arr[..., c], ref[..., c]
        out[..., c] = (a - a.mean()) / (a.std() + 1e-6) * r.std() + r.mean()
    return out


def _ramp(length, overlap_lo, overlap_hi):
    """1D weight: 1 inside, 0 beyond the overlap's centre line, with a FEATHER-wide linear blend
    centred in the overlap on the side(s) given (in local px). A narrow feather limits ghosting:
    two redraws never match pixel-for-pixel, so a wide cross-fade shows a soft double image."""
    w = np.ones(length, np.float32)
    for overlap, lo_side in ((overlap_lo, True), (overlap_hi, False)):
        if not overlap:
            continue
        start = (overlap - FEATHER) // 2  # feather band starts this far into the overlap
        ramp = np.linspace(0, 1, FEATHER, endpoint=False)
        band = np.concatenate([np.zeros(start), ramp, np.ones(overlap - start - FEATHER)]).astype(np.float32)
        if lo_side:
            w[:overlap] = band
        else:
            w[length - overlap:] = band[::-1]
    return w


def stitch_ground(raw, redraws, size):
    """raw: the 1536x1024 painting; redraws: name -> ChatGPT redraw (1536x1024); size: output (w, h)."""
    k = size[0] / RAW_SIZE[0]
    base = np.asarray(raw.convert('RGB').resize(size, Image.Resampling.LANCZOS), np.float32)
    acc = np.zeros_like(base)
    wsum = np.zeros(base.shape[:2], np.float32)
    ov_x = round((CROP[0] - QUADRANTS['ne'][0]) * k)  # overlap widths in output px
    ov_y = round((CROP[1] - QUADRANTS['sw'][1]) * k)
    for name, (x, y) in QUADRANTS.items():
        ox, oy, w, h = round(x * k), round(y * k), round(CROP[0] * k), round(CROP[1] * k)
        ref = base[oy:oy + h, ox:ox + w]
        tile = redraws[name].convert('RGB').resize((w, h), Image.Resampling.LANCZOS)
        dy, dx = _phase_shift(_gray(Image.fromarray(ref.astype(np.uint8))), _gray(tile))
        arr = np.roll(np.asarray(tile, np.float32), (dy, dx), axis=(0, 1))
        arr = _match_colour(arr, ref)
        wx = _ramp(w, ov_x if x > 0 else 0, ov_x if x == 0 else 0)
        wy = _ramp(h, ov_y if y > 0 else 0, ov_y if y == 0 else 0)
        weight = np.outer(wy, wx)
        acc[oy:oy + h, ox:ox + w] += arr * weight[..., None]
        wsum[oy:oy + h, ox:ox + w] += weight
        print(f'  quadrant {name}: shift dy={dy} dx={dx}')
    out = acc / np.maximum(wsum, 1e-6)[..., None]
    # Light unsharp: the redraws carry real detail, this only restores the crispness lost to the
    # 1536 -> 1296 px LANCZOS fit, so the ground sits in the props' sharpness range without halos.
    return Image.fromarray(out.clip(0, 255).astype(np.uint8), 'RGB').filter(
        ImageFilter.UnsharpMask(radius=1, percent=60, threshold=2))

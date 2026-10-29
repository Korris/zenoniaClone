"""Geometry helpers for tools/cut-weapon-overlays.py (components, tilt, grip, glow hue)."""
import colorsys
import math
import numpy as np
from PIL import Image


def label_components(mask):
    """8-connected labelling of a boolean mask; returns list of (ys, xs) index arrays."""
    h, w = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    comps = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        stack, ys, xs = [(y0, x0)], [], []
        seen[y0, x0] = True
        while stack:
            y, x = stack.pop()
            ys.append(y)
            xs.append(x)
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((ny, nx))
        comps.append((np.array(ys), np.array(xs)))
    return comps


def split_columns(alpha_band, parts=6):
    """Return [(x0, x1)] column ranges split at the `parts-1` widest empty-column gutters."""
    cols = np.nonzero((alpha_band > 0).any(axis=0))[0]
    gaps = [(int(cols[i + 1] - cols[i] - 1), i) for i in range(len(cols) - 1) if cols[i + 1] - cols[i] > 1]
    cuts = sorted(i for _, i in sorted(gaps, reverse=True)[:parts - 1])
    if len(cuts) != parts - 1:
        raise ValueError(f'expected {parts - 1} gutters, found {len(cuts)}')
    starts = [cols[0]] + [cols[i + 1] for i in cuts]
    ends = [cols[i] for i in cuts] + [cols[-1]]
    return [(int(s), int(e) + 1) for s, e in zip(starts, ends)]


def tilt_degrees(alpha):
    """Angle (deg, +ve = top leans right) of the principal axis of opaque pixels vs vertical."""
    ys, xs = np.nonzero(alpha > 0)
    pts = np.stack([xs - xs.mean(), ys - ys.mean()])
    vals, vecs = np.linalg.eigh(np.cov(pts))
    vx, vy = vecs[:, np.argmax(vals)]
    if vy > 0:
        vx, vy = -vx, -vy  # point up
    return math.degrees(math.atan2(vx, -vy))


def rotate_upright(img, angle):
    """Rotate so a top leaning right by `angle` deg becomes vertical (PIL rotates CCW for +ve)."""
    return img.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)


def trim(img, pad=4):
    crop = img.crop(img.getbbox())
    out = Image.new('RGBA', (crop.width + pad * 2, crop.height + pad * 2))
    out.paste(crop, (pad, pad))
    return out


def detect_grip(img, hafted_fraction=None):
    """Grip (x, y) in image px. Swords: midpoint between guard (widest row in lower 45% of the
    main body) and pommel bottom. Hafted: `hafted_fraction` of body length up from the bottom."""
    alpha = np.array(img)[..., 3] > 0
    ys, xs = max(label_components(alpha), key=lambda c: len(c[0]))
    body = np.zeros_like(alpha)
    body[ys, xs] = True
    top, bottom = int(ys.min()), int(ys.max())
    length = bottom - top + 1
    if hafted_fraction is not None:
        gy = int(round(bottom - hafted_fraction * length))
    else:
        guard = max(range(int(bottom - 0.45 * length), bottom + 1), key=lambda y: (int(body[y].sum()), -y))
        gy = (guard + bottom) // 2
    gx = int(np.median(np.nonzero(body[max(gy - 2, 0):gy + 3])[1]))
    return gx, gy


def glow_hex(img):
    """Dominant non-gold hue of saturated bright px -> '#rrggbb' (crisp anime-bright)."""
    arr = np.array(img)
    px = arr[..., :3].reshape(-1, 3)[arr[..., 3].reshape(-1) > 0] / 255.0
    hsv = np.array([colorsys.rgb_to_hsv(*p) for p in px])
    hsv = hsv[(hsv[:, 1] > 0.5) & (hsv[:, 2] > 0.6)]
    hue = (hsv[:, 0] * 360).astype(int) % 360
    hue = hue[(hue < 40) | (hue > 60)]  # drop gold trim, keep orange fire and red
    hist = np.bincount(hue, minlength=360).astype(float)
    smooth = sum(np.roll(hist, k) for k in range(-12, 13))
    r, g, b = colorsys.hsv_to_rgb(int(np.argmax(smooth)) / 360, 0.65, 1.0)
    return '#%02x%02x%02x' % (round(r * 255), round(g * 255), round(b * 255))

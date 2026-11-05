"""Helpers for build-willowbrook-large-ground.py: noise, texture synthesis from sample patches, stamps.

Loaded with importlib (hyphenated file name). All images are float32 HxWx3 in 0..255 unless noted.
"""
import numpy as np
import cv2
from scipy import ndimage as ndi


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def fractal(shape, scales, rng, weights=None):
    """Value noise 0..1-ish: sum of random grids of cell size `scales` (px) smoothly upsampled."""
    h, w = shape
    out = np.zeros(shape, np.float32)
    weights = weights or [1.0 / (i + 1) for i in range(len(scales))]
    for s, wt in zip([int(v) for v in scales], weights):
        gh, gw = max(2, int(h / s) + 3), max(2, int(w / s) + 3)
        g = rng.random((gh, gw)).astype(np.float32)
        big = cv2.resize(g, (gw * int(s), gh * int(s)), interpolation=cv2.INTER_CUBIC)
        oy, ox = rng.integers(0, int(s)), rng.integers(0, int(s))
        out += wt * big[oy:oy + h, ox:ox + w]
    out -= out.min()
    out /= max(out.max(), 1e-6)
    return out


def blur(a, sigma):
    return cv2.GaussianBlur(a, (0, 0), sigma)


def warp(a, rng, amp, scale):
    """Displace a 2D field with smooth noise so straight tile edges turn organic."""
    h, w = a.shape
    dx = (fractal((h, w), [scale, scale / 2.5], rng) - 0.5) * 2 * amp
    dy = (fractal((h, w), [scale, scale / 2.5], rng) - 0.5) * 2 * amp
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    return cv2.remap(a, xx + dx, yy + dy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)


def synth(patches, ps, stride, shape, rng):
    """Texture of `shape` from random flipped `patches` (ps x ps) with sin^2 windows; the detail
    (patch minus its mean colour) is summed with 1/sqrt(sum w^2) so contrast stays that of the source.
    Returns (detail HxWx3, mean colour)."""
    h, w = shape
    mean = np.mean([p.mean((0, 1)) for p in patches], 0)
    hp = [p - p.mean((0, 1)) for p in patches]
    k = np.sin(np.pi * (np.arange(ps) + 0.5) / ps) ** 2
    win = np.outer(k, k).astype(np.float32)
    acc = np.zeros((h, w, 3), np.float32)
    wsq = np.zeros((h, w), np.float32)
    jit = max(1, stride // 4)
    for y0 in range(-ps // 2, h, stride):
        for x0 in range(-ps // 2, w, stride):
            y, x = y0 + rng.integers(-jit, jit + 1), x0 + rng.integers(-jit, jit + 1)
            p = hp[rng.integers(len(hp))]
            if rng.random() < 0.5: p = p[:, ::-1]
            if rng.random() < 0.5: p = p[::-1]
            ya, xa, yb, xb = max(y, 0), max(x, 0), min(y + ps, h), min(x + ps, w)
            if yb <= ya or xb <= xa: continue
            ww = win[ya - y:yb - y, xa - x:xb - x]
            acc[ya:yb, xa:xb] += p[ya - y:yb - y, xa - x:xb - x] * ww[..., None]
            wsq[ya:yb, xa:xb] += ww * ww
    return acc / np.sqrt(np.maximum(wsq, 1e-3))[..., None], mean


def flagstones(shape, rng, spacing=58):
    """Paving: voronoi flagstones, per-stone grey-beige tint, dark joints. Returns RGB float."""
    from scipy.spatial import cKDTree
    h, w = shape
    n = int(h * w / (spacing * spacing) * 1.2)
    pts = np.stack([rng.random(n) * h, rng.random(n) * w], 1)
    yy, xx = np.mgrid[0:h:2, 0:w:2]
    q = np.stack([yy.ravel(), xx.ravel()], 1).astype(np.float32)
    q += (rng.random(q.shape) - 0.5) * 2
    d, idx = cKDTree(pts).query(q, k=2)
    gap = (d[:, 1] - d[:, 0]).reshape(yy.shape)
    sid = idx[:, 0].reshape(yy.shape)
    tint = (0.82 + 0.3 * rng.random(n)).astype(np.float32)
    warm = (rng.random(n) - 0.5).astype(np.float32)
    base = np.stack([160 + 14 * warm, 150 + 6 * warm, 124 - 8 * warm], -1)
    col = base[sid] * tint[sid][..., None]
    joint = 0.35 + 0.65 * smoothstep(0.0, 4.5, gap)
    bev = 0.9 + 0.1 * smoothstep(0, 12, gap)
    col = col * (joint * bev)[..., None] + np.array([78, 70, 48], np.float32) * (1 - joint)[..., None]
    return cv2.resize(col.astype(np.float32), (w, h), interpolation=cv2.INTER_LINEAR)


def planks(shape, rng, horizontal=True):
    """Weathered wooden deck planks (plank seams perpendicular to travel)."""
    h, w = shape
    n = h if horizontal else w
    pw = 22
    idx = (np.arange(n) // pw)
    tint = 0.85 + 0.3 * rng.random(idx.max() + 2)
    line = (np.arange(n) % pw)
    seam = 1.0 - 0.38 * (line < 2) - 0.12 * (line == 2)
    prof = (tint[idx] * seam).astype(np.float32)
    prof = prof[:, None] if horizontal else prof[None, :]
    base = np.array([158, 119, 72], np.float32)
    grain = fractal((h, w), [3, 9, 30], rng) - 0.5
    grain = cv2.resize(grain[:: 1, :], (w, h))
    col = base * prof[..., None] * (1 + 0.22 * grain[..., None])
    return col.astype(np.float32)


def furrows(shape, rng, period=30):
    """Tilled soil: dark furrows / lighter ridges along x, wobbly."""
    h, w = shape
    yy = np.arange(h, dtype=np.float32)[:, None]
    wob = (fractal((h, w), [120, 40], rng) - 0.5) * 14
    s = 0.5 + 0.5 * np.sin(2 * np.pi * (yy + wob * 0.5) / period)
    return s

"""Mechanic RO3 Soft Fantasy V1 masks (2026-11-21, every Mechanic skill + basic attack + turret shots redone as shader FX).

Grey masks on black like every light-skill texture (LightBlob / LightParticle read the red channel, colour comes from the material),
written by `Tools/build-light-skill-textures.py gen tech-ring tech-reticle smoke-puff muzzle-flare`:
  tech-ring     ground ring of the Mechanic's tech skills (EMP Burst, Target Lock scan, Deploy Turret, Orbital Cannon target zone): main band at
                0.955 of the quad radius (same as holy-rune-ring, so code can size it on the gameplay radius), segmented inner band, 24 ticks
  tech-reticle  lock-on reticle: broken ring with 4 gaps, 4 inner ticks, 4 corner brackets, small centre dot (Target Lock, Orbital Cannon, Rocket Barrage)
  smoke-puff    lumpy soft cloud (explosions, rocket exhaust); dissolves with LightBlob _Dissolve
  muzzle-flare  muzzle discharge pointing +x (2:1 quad): bright core at the left end, tapered cone + 2 side prongs (shots, cannon, turret)
"""
import numpy as np


def _grid(w, h=None):
    h = h or w
    y, x = np.mgrid[0:h, 0:w]
    return x / (w - 1) * 2 - 1, y / (h - 1) * 2 - 1


def _band(r, c, w):
    return np.exp(-((r - c) / w) ** 2)


def _ang_dist(a, c):
    """Smallest angle between a and c (radians)."""
    return np.abs((a - c + np.pi) % (2 * np.pi) - np.pi)


def tech_ring(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    outer = _band(r, 0.955, 0.012)
    seg = (np.cos(a * 12) > -0.55).astype(float)                   # 12 segments with short gaps
    inner = _band(r, 0.86, 0.008) * seg * 0.75
    k = a * 24 / (2 * np.pi)
    frac = k - np.floor(k)
    tick_on = np.exp(-(np.minimum(frac, 1 - frac) / 0.07) ** 2)    # 24 thin radial ticks
    long_tick = (np.round(k) % 2 == 0)
    ticks = tick_on * ((r > 0.885) & (r < np.where(long_tick, 0.94, 0.915))) * 0.8
    notches = sum(np.exp(-(_ang_dist(a, k * np.pi / 2) / 0.05) ** 2) for k in range(4)) * _band(r, 0.78, 0.018) * 0.7
    glow = np.exp(-((r - 0.955) / 0.08) ** 2) * 0.12
    return np.clip(outer + inner + ticks + notches + glow, 0, 1) * (r < 1)


def tech_reticle(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    gap = np.min([_ang_dist(a, k * np.pi / 2) for k in range(4)], axis=0)
    ring = _band(r, 0.66, 0.025) * np.clip((gap - 0.2) / 0.05, 0, 1)
    ticks = np.zeros_like(x)
    for k in range(4):
        c, sn = np.cos(k * np.pi / 2), np.sin(k * np.pi / 2)
        along = x * c + y * sn; side = -x * sn + y * c
        ticks = np.maximum(ticks, np.exp(-(side / 0.03) ** 2) * ((along > 0.36) & (along < 0.56)))
    corner = np.min([_ang_dist(a, np.pi / 4 + k * np.pi / 2) for k in range(4)], axis=0)
    brackets = _band(r, 0.9, 0.022) * (corner < 0.3) * 0.85
    dot = np.exp(-(r / 0.07) ** 2)
    return np.clip(ring + ticks * 0.9 + brackets + dot, 0, 1) * (r < 1)


def smoke_puff(s=128, seed=29):
    rng = np.random.default_rng(seed)
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    lobes = 0.06 * np.sin(a * 3 + rng.random() * 6) + 0.035 * np.sin(a * 7 + rng.random() * 6) + 0.02 * np.sin(a * 11 + rng.random() * 6)
    edge = np.clip((0.78 + lobes - r) / 0.16, 0, 1)
    # soft inner variation (not the dissolve: the shader adds its own noise)
    n = np.zeros_like(x)
    for k, f in enumerate((3, 6, 11)):
        ph = rng.random(2) * 6
        n += np.sin(x * f + ph[0]) * np.cos(y * f * 1.1 + ph[1]) / (k + 1.5)
    body = 0.72 + 0.14 * n - 0.18 * (y + 0.3).clip(0) # a little lighter towards the top
    return np.clip(edge * body, 0, 1)


def muzzle_flare(w=256, h=128):
    x, y = _grid(w, h)
    u = (x + 1) / 2                                                 # 0 at the muzzle (left) .. 1 at the tip (right)
    core = np.exp(-(((u - 0.12) / 0.11) ** 2 + (y / 0.42) ** 2))
    width = 0.62 * (1 - u) ** 1.1 + 0.02
    cone = np.exp(-(y / width) ** 2) * np.clip(u / 0.06, 0, 1) * (1 - u) ** 0.8
    prongs = np.zeros_like(x)
    for sgn in (-1, 1):
        d = np.abs(y - sgn * 0.9 * (u - 0.08)) / (0.09 * (1 - u) + 0.01)
        prongs = np.maximum(prongs, np.exp(-d ** 2) * np.clip((u - 0.08) / 0.05, 0, 1) * np.clip((0.55 - u) / 0.2, 0, 1) * 0.6)
    return np.clip(core * 1.1 + cone * 0.85 + prongs, 0, 1)


MAKERS = {"tech-ring": tech_ring, "tech-reticle": tech_reticle, "smoke-puff": smoke_puff, "muzzle-flare": muzzle_flare}
GAME_SIZE = {"tech-ring": (512, 512), "tech-reticle": (128, 128), "smoke-puff": (64, 64), "muzzle-flare": (128, 64)}

"""Cleric RO3 Soft Fantasy V1 masks (2026-11-21, docs/reference/ro3-skill-style/ro3-acolyte-priest-skill-style-chatgpt.md section 4).

Grey masks on black like every light-skill texture (LightBlob reads the red channel, colour comes from the material), written by
`Tools/build-light-skill-textures.py gen holy-hexagon holy-star4 holy-diamond holy-grid holy-rune-ring holy-cross-latin holy-wings holy-streak`:
  holy-hexagon      Divine Aegis ground seal: pointy-top hexagon, double outline, dots on the vertices, faint fill, small inner hexagon
  holy-star4        4-point star (concave sides), solid with a brighter core: Spirit Blessing emblem, Divine Light impact
  holy-diamond      rhombus bolt head (taller than wide), brighter centre: Divine Light / basic attack bolt
  holy-grid         Sanctuary floor: disc of square tiles, lines brighter at the crossings, faded towards the rim
  holy-rune-ring    boundary ring: main band at 0.955 of the quad radius, thin inner band, rune ticks / dots between, 8 small crosses inside
  holy-cross-latin  thin Latin cross (long lower arm): Purification
  holy-wings        pair of soft angel wings (2:1 quad), empty centre column so the hero is never covered: Guardian Spirit, Divine Ascension
  holy-streak       one soft vertical light streak (1:4 quad), bright low, fading up: Heal rays, holy ray from above, light column
"""
import numpy as np


def _grid(w, h=None):
    h = h or w
    y, x = np.mgrid[0:h, 0:w]
    return x / (w - 1) * 2 - 1, y / (h - 1) * 2 - 1  # image rows go down: y = +1 is the bottom of the picture


def _seg_dist(x, y, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(x - (ax + t * dx), y - (ay + t * dy))


def _polygon_line(x, y, verts, width):
    d = np.full(x.shape, 9.0)
    for k in range(len(verts)):
        ax, ay = verts[k]; bx, by = verts[(k + 1) % len(verts)]
        d = np.minimum(d, _seg_dist(x, y, ax, ay, bx, by))
    return np.exp(-(d / width) ** 2)


def _hex(radius):
    return [(np.cos(np.pi / 2 + k * np.pi / 3) * radius, np.sin(np.pi / 2 + k * np.pi / 3) * radius) for k in range(6)]


def holy_hexagon(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y)
    outer = _polygon_line(x, y, _hex(0.93), 0.02)
    inner = _polygon_line(x, y, _hex(0.81), 0.011) * 0.7
    core = _polygon_line(x, y, _hex(0.24), 0.01) * 0.6
    dots = sum(np.exp(-(np.hypot(x - vx, y - vy) / 0.035) ** 2) for vx, vy in _hex(0.93))
    apothem = 0.81 * np.cos(np.pi / 6)
    inside = np.ones_like(x, bool)
    for k in range(6):
        a = k * np.pi / 3
        inside &= (x * np.cos(a) + y * np.sin(a)) < apothem
    fill = inside * 0.1
    return np.clip(outer + inner + core + dots + fill, 0, 1) * (r < 1)


def holy_star4(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y)
    e = 0.55
    v = (np.abs(x) ** e + np.abs(y) ** e) ** (1 / e)  # astroid-like: concave sides, sharp points on the axes
    body = np.clip((0.95 - v) / 0.06, 0, 1) * 0.8
    core = np.exp(-(r / 0.2) ** 2) * 0.5
    return np.clip(body + core, 0, 1) * (r < 1)


def holy_diamond(s=128):
    x, y = _grid(s)
    d = np.abs(x) / 0.5 + np.abs(y) / 0.95
    return np.clip(np.clip((1 - d) / 0.08, 0, 1) * (0.55 + 0.45 * np.clip(1 - d, 0, 1) ** 0.7), 0, 1)


def holy_grid(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y)
    step, w = 0.25, 0.011
    gx = np.abs(((x + step / 2) % step) - step / 2)
    gy = np.abs(((y + step / 2) % step) - step / 2)
    lines = np.maximum(np.exp(-(gx / w) ** 2), np.exp(-(gy / w) ** 2)) * 0.6
    cross = np.exp(-(np.hypot(gx, gy) / 0.03) ** 2)
    fade = np.clip((0.95 - r) / 0.25, 0, 1)
    return np.clip((lines + cross + 0.06) * fade, 0, 1) * (r < 1)


def holy_rune_ring(s=512, seed=53):
    rng = np.random.default_rng(seed)
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    def band(c, w): return np.exp(-((r - c) / w) ** 2)
    rings = band(0.955, 0.017) + band(0.885, 0.009) * 0.7 + band(0.92, 0.05) * 0.1
    n = 40
    seg = (a / (2 * np.pi) + 0.5) * n; frac = seg - np.floor(seg); idx = np.floor(seg).astype(int) % n
    kind = rng.integers(0, 3, n)[idx]
    tick = np.exp(-((frac - 0.5) / 0.05) ** 2) * (np.abs(r - 0.92) < 0.022) * (kind == 0)
    dot = np.exp(-(((frac - 0.5) * 0.15) ** 2 + (r - 0.92) ** 2) / 0.00012) * (kind == 1)
    runes = (tick + dot) * 0.8
    crosses = np.zeros_like(r)
    for k in range(8):
        cx, cy = np.cos(k * np.pi / 4) * 0.8, np.sin(k * np.pi / 4) * 0.8
        arm = (np.abs(x - cx) < 0.032) & (np.abs(y - cy) < 0.008) | (np.abs(y - cy) < 0.032) & (np.abs(x - cx) < 0.008)
        crosses += arm * 0.75
    return np.clip(rings + runes + crosses, 0, 1) * (r < 1)


def holy_cross_latin(s=256):
    x, y = _grid(s)
    def bar(cx, cy, hw, hh, soft=0.02):
        return np.clip((hw - np.abs(x - cx)) / soft, 0, 1) * np.clip((hh - np.abs(y - cy)) / soft, 0, 1)
    vert = bar(0, 0.05, 0.07, 0.88)
    horz = bar(0, -0.38, 0.5, 0.07)
    shape = np.maximum(vert, horz) * 0.85
    centre = np.maximum(bar(0, 0.05, 0.02, 0.82, 0.03), bar(0, -0.38, 0.44, 0.02, 0.03)) * 0.35
    glow = np.exp(-(np.hypot(x, y + 0.3) / 0.45) ** 2) * 0.12
    return np.clip(shape + centre + glow, 0, 1)


def holy_wings(w=512, h=256):
    x, y = _grid(w, h)
    u, v = x * 2, y                                                  # square pixels: u -2..2 across, v -1..1 down (128 px per unit)
    out = np.zeros_like(x)
    for side in (1, -1):
        su, sv = 0.32 * side, 0.6                                    # shoulder point (low middle of the quad)
        for ang, ln, wd in [(14, 1.55, 0.13), (32, 1.5, 0.14), (50, 1.35, 0.14), (68, 1.12, 0.13), (86, 0.9, 0.11)]:
            t = np.deg2rad(ang)
            eu, ev = su + np.cos(t) * ln * side, sv - np.sin(t) * ln  # up and outward
            d = _seg_dist(u, v, su, sv, eu, ev)
            along = np.clip(np.hypot(u - su, v - sv) / ln, 0, 1)
            width = wd * (0.5 + 0.5 * np.sin(np.pi * np.clip(along, 0.08, 1)))  # feathers swell in the middle, taper at both ends
            fill = np.clip((width - d) / 0.03, 0, 1)
            rim = np.exp(-((d - width) / 0.02) ** 2) * (along > 0.05)
            out = np.maximum(out, fill * (0.42 + 0.3 * (1 - along)) + rim * 0.8)
    centre_gap = np.clip((np.abs(u) - 0.24) / 0.1, 0, 1)            # nothing over the hero's body
    edge = np.clip((1 - np.abs(v)) / 0.05, 0, 1) * np.clip((2 - np.abs(u)) / 0.05, 0, 1)
    return np.clip(out * centre_gap * edge, 0, 1)


def holy_streak(w=64, h=256):
    x, y = _grid(w, h)
    v = (1 - y) / 2                                                   # 0 bottom .. 1 top
    env = np.clip(v / 0.12, 0, 1) * (1 - v) ** 1.3
    return np.clip(np.exp(-(x / 0.34) ** 2) * env * 1.3 + np.exp(-(x / 0.1) ** 2) * env * 0.5, 0, 1)


MAKERS = {"holy-hexagon": holy_hexagon, "holy-star4": holy_star4, "holy-diamond": holy_diamond, "holy-grid": holy_grid,
          "holy-rune-ring": holy_rune_ring, "holy-cross-latin": holy_cross_latin, "holy-wings": holy_wings, "holy-streak": holy_streak}
GAME_SIZE = {"holy-hexagon": (256, 256), "holy-star4": (128, 128), "holy-diamond": (64, 64), "holy-grid": (256, 256),
             "holy-rune-ring": (512, 512), "holy-cross-latin": (128, 128), "holy-wings": (256, 128), "holy-streak": (32, 128)}

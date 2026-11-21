"""Cleric buff receive sigils, RO3 Soft Fantasy V1 "strong" buffs (2026-11-21, user: "skill buff dam và defense hiện tại đơn giản quá").

Ground rune circles of the Spirit Blessing (+ATK) / Divine Aegis (+DEF) receive burst, after the concept images the user liked
(docs/reference/cleric-buff-status-concept/tay-buff-concept-1.png / -2.png: rune circle with a star under the hero) but in the RO3 shapes
(ATK 4-point star, DEF hexagon). Grey masks on black like every light-skill texture (LightBlob reads the red channel), written by
`Tools/build-light-skill-textures.py gen holy-sigil-star holy-sigil-hex`. Strokes sized for a ~36-unit quad (~120 px at 720p): main band
~3 px, secondary ~1.5 px (RO3 rule 6). The shader's _Reveal sweep draws them in clockwise from the top.
  holy-sigil-star   outer double ring with rune ticks / dots between, a large 4-point star (outline + faint fill) with its tips on the inner ring,
                    a small 4-point star turned 45 deg, a tiny centre ring, dots on the large star's tips
  holy-sigil-hex    outer double ring with rune ticks, a hexagon (double outline) inscribed in it, a faint hexagram, a small inner hexagon,
                    dots on the hexagon's corners
"""
import numpy as np

from light_skill_textures_cleric_ro3 import _grid, _polygon_line, _hex


def _rings_and_runes(x, y, seed):
    """Outer main band 0.955, inner thin band 0.885, rune ticks / dots in between (40 slots), a faint wash under the band."""
    rng = np.random.default_rng(seed)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    def band(c, w): return np.exp(-((r - c) / w) ** 2)
    rings = band(0.955, 0.022) + band(0.885, 0.011) * 0.75 + band(0.92, 0.05) * 0.1
    n = 40
    seg = (a / (2 * np.pi) + 0.5) * n; frac = seg - np.floor(seg); idx = np.floor(seg).astype(int) % n
    kind = rng.integers(0, 3, n)[idx]
    tick = np.exp(-((frac - 0.5) / 0.06) ** 2) * (np.abs(r - 0.92) < 0.024) * (kind == 0)
    dot = np.exp(-(((frac - 0.5) * 0.15) ** 2 + (r - 0.92) ** 2) / 0.00014) * (kind == 1)
    return rings + (tick + dot) * 0.8


def _star4(x, y, radius, rot, e=0.62, stroke=0.024):
    """Outline (soft stroke ~`stroke` wide in quad units) and inside of a concave 4-point star with its tips at `radius`, turned `rot` rad."""
    c, s = np.cos(rot), np.sin(rot)
    u, v = (x * c + y * s) / radius, (-x * s + y * c) / radius
    f = (np.abs(u) ** e + np.abs(v) ** e) ** (1 / e)  # 1 on the outline
    return np.exp(-((f - 1) * radius / stroke) ** 2), np.clip((1 - f) / 0.04, 0, 1)


def holy_sigil_star(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y)
    big_line, big_in = _star4(x, y, 0.86, 0.0)
    small_line, small_in = _star4(x, y, 0.52, np.pi / 4, 0.75, 0.014)
    tips = sum(np.exp(-(np.hypot(x - tx, y - ty) / 0.03) ** 2) for tx, ty in [(0.86, 0), (-0.86, 0), (0, 0.86), (0, -0.86)])
    centre = np.exp(-((r - 0.13) / 0.012) ** 2) * 0.8
    shape = (_rings_and_runes(x, y, 61) + big_line * 0.95 + big_in * (0.1 + 0.12 * np.exp(-(r / 0.35) ** 2)) + small_line * 0.7 * (1 - big_line) + small_in * 0.05 + tips * 0.9 + centre)
    return np.clip(shape, 0, 1) * (r < 1)


def holy_sigil_hex(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y)
    hexa = _polygon_line(x, y, _hex(0.86), 0.022) + _polygon_line(x, y, _hex(0.78), 0.011) * 0.65
    tri_up = [(np.cos(np.pi / 2 + k * 2 * np.pi / 3) * 0.78, np.sin(np.pi / 2 + k * 2 * np.pi / 3) * 0.78) for k in range(3)]
    tri_dn = [(px, -py) for px, py in tri_up]
    hexagram = (_polygon_line(x, y, tri_up, 0.01) + _polygon_line(x, y, tri_dn, 0.01)) * 0.4
    core = _polygon_line(x, y, _hex(0.22), 0.012) * 0.75
    dots = sum(np.exp(-(np.hypot(x - vx, y - vy) / 0.032) ** 2) for vx, vy in _hex(0.86))
    apothem = 0.78 * np.cos(np.pi / 6)
    inside = np.ones_like(x, bool)
    for k in range(6):
        a = k * np.pi / 3
        inside &= (x * np.cos(a) + y * np.sin(a)) < apothem
    shape = _rings_and_runes(x, y, 67) + hexa + hexagram + core + dots * 0.9 + inside * 0.08
    return np.clip(shape, 0, 1) * (r < 1)


MAKERS = {"holy-sigil-star": holy_sigil_star, "holy-sigil-hex": holy_sigil_hex}
GAME_SIZE = {"holy-sigil-star": (512, 512), "holy-sigil-hex": (512, 512)}

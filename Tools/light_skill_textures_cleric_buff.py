"""Cleric buff status masks (Spirit Blessing = ATK, Divine Aegis = DEF; concept docs/reference/cleric-buff-status-concept/tay-buff-concept-1..4.png).

Grey masks on black like every light-skill texture (the LightBlob shader reads the red channel, colour comes from the material), used by
`Tools/build-light-skill-textures.py gen buff-compass buff-dome buff-emblem-sword buff-emblem-shield buff-arrow`:
  buff-compass        ground rune circle: outer double ring, 8-point compass star (4 long points to the rim, 4 short), inner rings, rune dots
  buff-dome           see-through dome FACE on an upright quad (W x 1.06 W): base ellipse centre at v = 0.28, brighter towards the silhouette,
                      highlight streak upper left; the centre (where the face is) stays faint
  buff-dome-rim       the dome's rim lines only (fresnel rim drawn brighter than the face); buff-dome-side = only the two flanks of that rim
  buff-emblem-*-outline  thin outline round the emblem glyph / badge (drawn behind it in a dark tone so the emblem stays crisp on snow)
  buff-emblem-sword   round badge: ring + faint disc + diagonal sword (tip up-right)
  buff-emblem-shield  heater shield outline + faint fill + cross
  buff-arrow          small up arrow
"""
import numpy as np


def _grid(s):
    y, x = np.mgrid[0:s, 0:s] / (s - 1) * 2 - 1  # image rows go down: y = +1 is the bottom of the picture
    return x, y


def _seg_dist(x, y, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(x - (ax + t * dx), y - (ay + t * dy))


def buff_compass(s=512, seed=41):
    rng = np.random.default_rng(seed)
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    def band(c, w): return np.exp(-((r - c) / w) ** 2)
    rings = band(0.955, 0.016) + band(0.895, 0.011) * 0.75 + band(0.62, 0.011) * 0.8 + band(0.30, 0.01) * 0.65 + band(0.11, 0.012) * 0.7
    # 8-point star: long points (N E S W) to 0.95, short diagonals to 0.6, valleys at 0.17
    verts = []
    for k in range(16):
        ang = k * np.pi / 8
        rad = 0.95 if k % 4 == 0 else 0.6 if k % 2 == 0 else 0.17
        verts.append((np.cos(ang) * rad, np.sin(ang) * rad))
    d = np.full(x.shape, 9.0)
    for k in range(16):
        ax, ay = verts[k]; bx, by = verts[(k + 1) % 16]
        d = np.minimum(d, _seg_dist(x, y, ax, ay, bx, by))
    star_line = np.exp(-(d / 0.012) ** 2)
    # star fill (star-shaped about the centre): distance along the ray to the edge of its sector
    sector = np.floor(((a % (2 * np.pi)) / (np.pi / 8))).astype(int) % 16
    v = np.array(verts)
    p0 = v[sector]; p1 = v[(sector + 1) % 16]
    ex, ey = p1[..., 0] - p0[..., 0], p1[..., 1] - p0[..., 1]
    ux, uy = np.cos(a), np.sin(a)
    den = ux * ey - uy * ex
    edge_r = (p0[..., 0] * ey - p0[..., 1] * ex) / np.where(np.abs(den) < 1e-6, 1e-6, den)
    fill = (r < edge_r) * 0.16
    # centre spokes (the long points' midlines) and rune dots between the 0.62 and 0.895 rings
    spokes = sum(np.exp(-(_seg_dist(x, y, 0, 0, np.cos(k * np.pi / 2) * 0.95, np.sin(k * np.pi / 2) * 0.95) / 0.007) ** 2) for k in range(4)) * 0.5
    n = 32
    seg = (a / (2 * np.pi) + 0.5) * n; frac = seg - np.floor(seg); idx = np.floor(seg).astype(int) % n
    kind = rng.integers(0, 3, n)[idx]
    dot = np.exp(-(((frac - 0.5) * 0.2) ** 2 + (r - 0.76) ** 2) / 0.00018) * (kind == 0)
    tick = np.exp(-((frac - 0.5) / 0.06) ** 2) * (np.abs(r - 0.76) < 0.035) * (kind == 1) * 0.8
    runes = (dot + tick) * 0.75
    # small diamonds on the outer ring at the 8 compass directions
    diam = np.zeros_like(r)
    for k in range(8):
        cx, cy = np.cos(k * np.pi / 4) * 0.925, np.sin(k * np.pi / 4) * 0.925
        diam += np.exp(-((np.abs(x - cx) + np.abs(y - cy) - 0.035) / 0.01) ** 2) * 0.9
    glow = np.exp(-(r / 0.55) ** 2) * 0.1
    return np.clip(rings + star_line + fill + spokes + runes + diam + glow, 0, 1) * (r < 1)


def _dome_parts(s=256):
    """u, v in 0..1 (v up). Dome: upper half of the ellipse centre (0.5, 0.28), radii (0.47, 0.68); front base arc: lower half of radii (0.47, 0.235).
    Returns (face, rim, side): face = fresnel fill + highlight streak, rim = silhouette + base lines, side = rim kept only on the two flanks."""
    yy, xx = np.mgrid[0:s, 0:s] / (s - 1)
    u, v = xx, 1 - yy
    cx, cv, rx, rv, rb = 0.5, 0.28, 0.47, 0.68, 0.235
    du = (u - cx) / rx
    top = np.hypot(du, (v - cv) / rv)                  # < 1 inside the dome (above the base line)
    base = np.hypot(du, (v - cv) / rb)                 # < 1 inside the base ellipse
    inside = ((v >= cv) & (top < 1)) | ((v < cv) & (base < 1))
    e = np.where(v >= cv, top, base)                    # 0 centre .. 1 silhouette
    fill = (0.1 + 0.55 * np.clip(e, 0, 1) ** 4) * inside  # fresnel: faint in the middle, brighter at the edge
    hl = np.exp(-((np.hypot((u - 0.52) / 0.36, (v - 0.36) / 0.5) - 1) / 0.035) ** 2) * np.clip((0.46 - u) / 0.12, 0, 1) * np.clip((v - 0.5) / 0.12, 0, 1) * 0.7  # highlight streak, upper left
    rim = np.exp(-((top - 1) / 0.022) ** 2) * (v >= cv - 0.01)
    front = np.exp(-((base - 1) / 0.03) ** 2) * (v < cv + 0.005) * 0.85
    back = np.exp(-((base - 1) / 0.025) ** 2) * (v >= cv) * 0.3    # base line behind the hero (mostly hidden)
    rims = np.clip(rim + front + back, 0, 1)
    side = np.clip(rim * np.clip((np.abs(u - 0.5) - 0.2) / 0.15, 0, 1) * np.clip((0.82 - v) / 0.18, 0, 1), 0, 1)  # flanks only: no cap over the head
    return np.clip(fill + hl, 0, 1), rims, side


def buff_dome(s=256): return _dome_parts(s)[0]
def buff_dome_rim(s=256): return _dome_parts(s)[1]
def buff_dome_side(s=256): return _dome_parts(s)[2]


def _outline(mask, px):
    """Thin outline round the bright parts of a mask (glyph + badge lines): dilated shape minus the shape, soft edged."""
    hard = mask > 0.55
    d = hard.copy()
    for _ in range(px):
        p = np.pad(d, 1)
        d = p[1:-1, 1:-1] | p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
    o = (d & ~hard).astype(float)
    p = np.pad(o, 1)
    return np.clip(sum(p[1 + dy:1 + dy + o.shape[0], 1 + dx:1 + dx + o.shape[1]] for dy in (-1, 0, 1) for dx in (-1, 0, 1)) / 9 * 1.6, 0, 1)


def buff_emblem_sword(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y)
    ring = np.exp(-((r - 0.86) / 0.035) ** 2)
    disc = (r < 0.84) * 0.28
    c, sn = np.cos(np.pi / 4), np.sin(np.pi / 4)
    uu = x * c - y * sn                                 # along the blade, tip up-right (image y down)
    vv = x * sn + y * c
    def soft(dist, w=0.02): return np.clip(0.5 - dist / w, 0, 1)
    tip = 0.64; taper = 0.16
    half = np.where(uu > tip - taper, 0.07 * np.clip((tip - uu) / taper, 0, 1), 0.07)
    blade = soft(np.maximum(np.abs(vv) - half, np.maximum(-0.12 - uu, uu - tip)))
    guard = soft(np.maximum(np.abs(uu + 0.16) - 0.035, np.abs(vv) - 0.25))
    grip = soft(np.maximum(np.abs(vv) - 0.04, np.maximum(-0.4 - uu, uu + 0.16)))
    pommel = soft(np.hypot(uu + 0.45, vv) - 0.06)
    sword = np.clip(blade + guard + grip + pommel, 0, 1)
    return np.clip(np.maximum(ring + disc, sword), 0, 1) * (r < 1)


def buff_emblem_shield(s=256):
    x, y = _grid(s)
    top, mid, bot, hw = -0.78, 0.15, 0.9, 0.66
    # half width per row: flat top with a small dip, straight sides to `mid`, then curving in to the point at `bot`
    t = np.clip((y - mid) / (bot - mid), 0, 1)
    half = np.where(y < mid, hw, hw * np.sqrt(np.clip(1 - t ** 1.6, 0, 1)))
    top_y = top + 0.06 * (1 - (x / hw) ** 2)            # top edge dips a little in the middle
    inside = (np.abs(x) < half) & (y > top_y) & (y < bot)
    dist_side = half - np.abs(x)
    dist = np.minimum(dist_side, np.minimum(y - top_y, bot - y))
    outline = np.exp(-(np.where(inside, dist, -np.minimum(dist_side, 0) + np.maximum(top_y - y, 0)) / 0.035) ** 2) * (np.abs(x) < hw + 0.08) * (y > top - 0.1) * (y < bot + 0.06) * ((half > 0.03) | inside)
    fill = inside * 0.3
    cross = inside * ((np.abs(x) < 0.07) & (y > top_y + 0.14) & (y < bot - 0.22) | (np.abs(y + 0.18) < 0.07) & (np.abs(x) < half - 0.16))
    return np.clip(np.maximum(outline + fill, cross * 1.0), 0, 1)


def buff_arrow(s=128):
    x, y = _grid(s)
    head = (y > -0.9) & (y < -0.05) & (np.abs(x) < (y + 0.9) * 0.82)
    stem = (np.abs(x) < 0.22) & (y >= -0.06) & (y < 0.88)
    a = (head | stem).astype(float)
    # soften the edges a little (box blur 3x3)
    p = np.pad(a, 1)
    return np.clip(sum(p[1 + dy:1 + dy + s, 1 + dx:1 + dx + s] for dy in (-1, 0, 1) for dx in (-1, 0, 1)) / 9, 0, 1)


def buff_emblem_sword_outline(s=256): return _outline(buff_emblem_sword(s), 7)
def buff_emblem_shield_outline(s=256): return _outline(buff_emblem_shield(s), 7)


MAKERS = {"buff-compass": buff_compass, "buff-dome": buff_dome, "buff-dome-rim": buff_dome_rim, "buff-dome-side": buff_dome_side,
          "buff-emblem-sword-outline": buff_emblem_sword_outline, "buff-emblem-shield-outline": buff_emblem_shield_outline, "buff-emblem-sword": buff_emblem_sword,
          "buff-emblem-shield": buff_emblem_shield, "buff-arrow": buff_arrow}
GAME_SIZE = {"buff-compass": (256, 256), "buff-dome": (128, 128), "buff-dome-rim": (128, 128), "buff-dome-side": (128, 128),
             "buff-emblem-sword-outline": (128, 128), "buff-emblem-shield-outline": (128, 128), "buff-emblem-sword": (128, 128), "buff-emblem-shield": (128, 128), "buff-arrow": (64, 64)}

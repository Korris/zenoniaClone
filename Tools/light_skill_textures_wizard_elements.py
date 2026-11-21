"""Wizard element masks, RO3 Soft Fantasy V1 (2026-11-21, user: "làm lại hết skill của warrior, wizard, mechanic đổi qua kiểu mới").

Grey masks on black like every light-skill texture (LightBlob reads the red channel; colour comes from the material, ElementLightLook),
written by `Tools/build-light-skill-textures.py gen ice-shard snowflake frost-ring fire-head fire-burst hex-cells swirl-fire swirl-ice storm-cloud`:
  ice-shard    one ice crystal standing up (1:3 quad): pointed top, short taper at the foot, bright ridge down the middle, darker side facet;
               bolt heads (Arcane Lance), falling Ice Storm shards, Frost Nova crystals standing round the caster, glacier spikes
  snowflake    6-arm snowflake with side branches and a small hexagon centre: Frost Nova emblem, frost bolt head, Ice Storm landings
  frost-ring   ground boundary ring (band at 0.955 of the quad radius) with 12 small crystal teeth on it, a faint inner ring with 6 snow ticks: ice areas
  fire-head    fireball pointing RIGHT (+x): round hot head, three wavy flame tongues trailing to the left; turned along the flight in code
  fire-burst   8 flame petals bursting outward from a soft centre: Fireball blast, Elemental Burst fire half
  hex-cells    honeycomb sphere face (cells brighter towards the rim) + rim band: Mana Shield bubble
  swirl-fire   one spiral arm (thick outside, thin at the centre); swirl-ice is the same arm turned 180 degrees: Elemental Burst
  storm-cloud  soft flat-bottomed cloud (2:1 quad), brighter top edge: Ice Storm cloud over the area
"""
import numpy as np


def _grid(w, h=None):
    h = h or w
    y, x = np.mgrid[0:h, 0:w]
    return x / (w - 1) * 2 - 1, 1 - y / (h - 1) * 2  # x -1..1 left->right, y -1..1 bottom->top (y = +1 is the top of the picture)


def _seg_dist(x, y, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(x - (ax + t * dx), y - (ay + t * dy))


def ice_shard(w=96, h=288):
    x, y = _grid(w, h)
    # half width along the height: 0 at the tip (y=1), widest at y=-0.55, short taper to the foot (y=-1)
    hw = np.where(y > -0.55, 0.78 * np.clip((1 - y) / 1.55, 0, 1) ** 0.85, 0.78 * np.clip((y + 1) / 0.45, 0, 1) ** 0.7 * 0.8 + 0.78 * 0.2)
    inside = np.clip((hw - np.abs(x)) / 0.06, 0, 1)
    side = np.where(x < 0, 0.62, 0.78)                                       # left facet darker than the right one
    ridge = np.exp(-(x / 0.07) ** 2) * 0.35                                   # bright ridge down the middle
    glint = np.exp(-((x + 0.2) ** 2 / 0.01 + (y - 0.35) ** 2 / 0.02)) * 0.25  # small highlight near the top
    return np.clip(inside * (side + ridge + glint), 0, 1)


def snowflake(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y)
    out = np.zeros_like(x)
    for k in range(6):
        a = np.pi / 2 + k * np.pi / 3
        ex, ey = np.cos(a) * 0.92, np.sin(a) * 0.92
        out = np.maximum(out, np.exp(-(_seg_dist(x, y, 0, 0, ex, ey) / 0.045) ** 2))
        for t, ln in ((0.45, 0.28), (0.7, 0.2)):                               # two pairs of side branches per arm, pointing outward
            bx, by = np.cos(a) * 0.92 * t, np.sin(a) * 0.92 * t
            for side in (1, -1):
                b = a + side * np.pi / 3.2
                out = np.maximum(out, np.exp(-(_seg_dist(x, y, bx, by, bx + np.cos(b) * ln, by + np.sin(b) * ln) / 0.035) ** 2) * 0.9)
    hexc = np.zeros_like(x)
    for k in range(6):
        a0, a1 = np.pi / 2 + k * np.pi / 3, np.pi / 2 + (k + 1) * np.pi / 3
        hexc = np.maximum(hexc, np.exp(-(_seg_dist(x, y, np.cos(a0) * 0.2, np.sin(a0) * 0.2, np.cos(a1) * 0.2, np.sin(a1) * 0.2) / 0.03) ** 2))
    core = np.exp(-(r / 0.12) ** 2) * 0.6
    return np.clip(out * 0.9 + hexc * 0.8 + core, 0, 1) * (r < 1)


def frost_ring(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    def band(c, w): return np.exp(-((r - c) / w) ** 2)
    rings = band(0.955, 0.017) + band(0.875, 0.009) * 0.65 + band(0.915, 0.05) * 0.1
    teeth = np.zeros_like(r)
    for k in range(12):                                                       # small crystal diamonds sitting on the ring, pointing out
        ca = k * np.pi / 6
        u = (x * np.cos(ca) + y * np.sin(ca)) - 0.915                         # along the radius
        v = -x * np.sin(ca) + y * np.cos(ca)                                  # across
        d = np.abs(u) / 0.1 + np.abs(v) / 0.04
        teeth = np.maximum(teeth, np.clip((1 - d) / 0.2, 0, 1) * (0.7 + 0.3 * (k % 2 == 0)))
    inner = band(0.6, 0.008) * 0.5                                            # faint inner ring with 6 short snow ticks on it
    for k in range(6):
        ca = np.pi / 2 + k * np.pi / 3
        inner = np.maximum(inner, np.exp(-(_seg_dist(x, y, np.cos(ca) * 0.5, np.sin(ca) * 0.5, np.cos(ca) * 0.72, np.sin(ca) * 0.72) / 0.012) ** 2) * 0.55)
    star = inner
    return np.clip(rings + teeth + star, 0, 1) * (r < 1)


def fire_head(s=192):
    x, y = _grid(s)
    hx = 0.38
    head = np.clip((0.4 - np.hypot(x - hx, y)) / 0.08, 0, 1)
    tongues = np.zeros_like(x)
    for off, amp, ln, wd in ((0.0, 0.1, 1.25, 0.24), (0.2, 0.08, 0.95, 0.15), (-0.2, 0.08, 1.0, 0.15)):
        t = np.clip((hx - x) / ln, 0, 1)                                     # 0 at the head .. 1 at the tip of the tongue
        cy = off * t * 1.6 + amp * np.sin(t * np.pi * 2.2 + off * 9) * t
        width = wd * (1 - t) ** 0.8
        tongues = np.maximum(tongues, np.clip((width - np.abs(y - cy)) / 0.05, 0, 1) * (x < hx) * (t < 1) * (1 - t * 0.55))
    core = np.exp(-(np.hypot(x - hx - 0.05, y) / 0.18) ** 2) * 0.45
    return np.clip(np.maximum(head * 0.85, tongues * 0.8) + core, 0, 1)


def fire_burst(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    out = np.zeros_like(x)
    for k in range(8):
        ca = k * np.pi / 4 + (0.12 if k % 2 else 0)
        ln = 0.92 if k % 2 == 0 else 0.72
        u = x * np.cos(ca) + y * np.sin(ca); v = -x * np.sin(ca) + y * np.cos(ca)
        t = np.clip((u - 0.15) / (ln - 0.15), 0, 1)
        width = 0.2 * np.sin(np.pi * np.clip(t * 0.85 + 0.15, 0, 1)) * (1 - t) ** 0.4  # teardrop: round inner end, pointed outer tip
        out = np.maximum(out, np.clip((width - np.abs(v)) / 0.04, 0, 1) * (u > 0.1) * (u < ln) * (0.95 - 0.35 * t))
    core = np.exp(-(r / 0.34) ** 2) * 0.75
    return np.clip(out + core, 0, 1) * (r < 1)


def hex_cells(s=256):
    x, y = _grid(s)
    r = np.hypot(x, y)
    size = 0.16                                                               # hexagon cell radius
    q = (np.sqrt(3) / 3 * x - y / 3) / size; rr = (2 / 3 * y) / size        # axial coords (pointy-top)
    cx, cz = q, rr; cy = -cx - cz
    rx, ry, rz = np.round(cx), np.round(cy), np.round(cz)                    # cube rounding to the nearest cell
    dx, dy, dz = np.abs(rx - cx), np.abs(ry - cy), np.abs(rz - cz)
    fix_x = (dx > dy) & (dx > dz); fix_y = ~fix_x & (dy > dz)
    rx = np.where(fix_x, -ry - rz, rx)
    rz = np.where(~fix_x & ~fix_y, -rx - ry, rz)
    px = size * np.sqrt(3) * (rx + rz / 2); py = size * 1.5 * rz             # centre of the hexagon the pixel is in
    lx, ly = np.abs(x - px), np.abs(y - py)
    hexd = np.maximum(lx, lx * 0.5 + ly * np.cos(np.pi / 6))                  # pointy-top hexagon: edge normals at 0 / +-60 degrees
    edge = np.exp(-((hexd - size * 0.866) / 0.018) ** 2)
    sphere = 0.35 + 0.65 * np.clip(r / 0.95, 0, 1) ** 2.5                      # cells read stronger near the rim (sphere seen face on)
    rim = np.exp(-((r - 0.95) / 0.03) ** 2)
    fill = 0.08 * (r < 0.95)
    return np.clip(edge * sphere * 0.8 + rim + fill, 0, 1) * (r < 1)


def _swirl(s, turn):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x) + turn
    out = np.zeros_like(x)
    for k in range(-1, 2):                                                    # arm: r grows with the angle, sampled over neighbouring windings
        th = (a + 2 * np.pi * k)
        t = th / (1.35 * np.pi)                                               # 0 centre .. 1 outer end over ~0.7 turn
        ok = (t > 0) & (t < 1)
        cr = 0.08 + 0.84 * t
        width = 0.03 + 0.17 * np.sin(np.pi * np.clip(t, 0, 1)) ** 0.8 * np.clip(t, 0, 1) ** 0.5
        out = np.maximum(out, np.clip((width - np.abs(r - cr)) / 0.04, 0, 1) * ok * (0.55 + 0.45 * t))
    return np.clip(out + np.exp(-(r / 0.12) ** 2) * 0.4, 0, 1) * (r < 1)


def swirl_fire(s=256):
    return _swirl(s, 0.0)


def swirl_ice(s=256):
    return _swirl(s, np.pi)


def storm_cloud(w=512, h=256):
    x, y = _grid(w, h)
    u, v = x * 2, y                                                           # square units: u -2..2, v -1..1
    blobs = [(-1.25, -0.25, 0.55), (-0.55, 0.1, 0.72), (0.25, 0.25, 0.8), (1.0, 0.0, 0.62), (1.55, -0.3, 0.42), (-1.7, -0.45, 0.35)]
    body = np.zeros_like(x)
    for bu, bv, br in blobs:
        body = np.maximum(body, np.clip((br - np.hypot(u - bu, v - bv)) / 0.1, 0, 1))
    body *= np.clip((v + 0.62) / 0.12, 0, 1)                                 # flat bottom
    top = np.clip(v + 0.3, 0, 1) * 0.35                                       # brighter upper half
    return np.clip(body * (0.55 + top), 0, 1)


MAKERS = {"ice-shard": ice_shard, "snowflake": snowflake, "frost-ring": frost_ring, "fire-head": fire_head, "fire-burst": fire_burst,
          "hex-cells": hex_cells, "swirl-fire": swirl_fire, "swirl-ice": swirl_ice, "storm-cloud": storm_cloud}
GAME_SIZE = {"ice-shard": (48, 144), "snowflake": (128, 128), "frost-ring": (512, 512), "fire-head": (96, 96), "fire-burst": (128, 128),
             "hex-cells": (256, 256), "swirl-fire": (256, 256), "swirl-ice": (256, 256), "storm-cloud": (256, 128)}


if __name__ == "__main__":
    # Self-contained writer (the shared build-light-skill-textures.py is edited by other class agents at the same time):
    #   python Tools/light_skill_textures_wizard_elements.py [names...]
    # Procedural source -> Assets/ArtLibrary/fx/light-skill/<name>-procedural.png, game copy -> Assets/Resources/Sprites/fx/light-skill/<name>.png
    import sys
    from pathlib import Path
    from PIL import Image
    root = Path(__file__).resolve().parent.parent
    only = sys.argv[1:]
    for name, make in MAKERS.items():
        if only and name not in only: continue
        im = Image.fromarray((np.clip(make(), 0, 1) * 255).astype(np.uint8), "L")
        for path, size in ((root / "Assets/ArtLibrary/fx/light-skill" / (name + "-procedural.png"), None),
                           (root / "Assets/Resources/Sprites/fx/light-skill" / (name + ".png"), GAME_SIZE[name])):
            path.parent.mkdir(parents=True, exist_ok=True)
            (im.resize(size, Image.LANCZOS) if size else im).convert("RGB").save(path)
            print(path)

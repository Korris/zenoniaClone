"""Archer light-skill masks, RO3 Soft Fantasy V1 (2026-11-26, user: "skill của archer sao ko thấy hiệu ứng gì vậy").

Grey masks on black like every light-skill texture (LightBlob reads the red channel; colour comes from the material, ArcherLightLook).
Procedural stand-ins (small mask textures are allowed for particles / shader quads, docs/sprite-rules.md top block; no FX frames):
  archer-eye        hawk eye glyph: almond outline, iris ring, pupil, small glint, 3 short lashes on top: Eagle Eye emblem
  archer-vine-ring  ground ring of two vines twisting round each other (band at 0.9 of the quad radius) with 12 leaves on it: Binding Shot
  archer-leaf       one leaf standing up (tip at the top), bright midrib and a short stem: leaf flurries of Wind Step / Binding Shot / Arrow Rain
  archer-wind-arc   one wind gust: a crescent stroke opening to the right (+x), thick in the middle and tapering at both ends, plus a thinner
                    second stroke inside it: wind bursts (turned along the push in code), Piercing Arrow wind spiral

Run: python Tools/light_skill_textures_archer.py   (writes Assets/Resources/Sprites/fx/light-skill/<name>.png)
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
GAME = ROOT / "Assets/Resources/Sprites/fx/light-skill"


def _grid(w, h=None):
    h = h or w
    y, x = np.mgrid[0:h, 0:w]
    return x / (w - 1) * 2 - 1, 1 - y / (h - 1) * 2  # x -1..1 left->right, y -1..1 bottom->top


def archer_eye(s=256):
    x, y = _grid(s)
    # almond: two circular arcs meeting at the corners (+-0.92, 0)
    c = 0.95  # arc centre offset
    rr = np.hypot(0.92, c)
    upper = np.hypot(x, y + c) - rr      # < 0 below the upper lid arc
    lower = np.hypot(x, y - c) - rr      # < 0 above the lower lid arc
    inside = np.maximum(upper, lower)    # < 0 inside the almond
    outline = np.exp(-(inside / 0.035) ** 2) * (np.abs(x) < 0.96)
    r = np.hypot(x, y)
    iris = np.exp(-((r - 0.36) / 0.035) ** 2) * (inside < 0.02)
    iris_fill = np.clip(1 - r / 0.36, 0, 1) ** 0.4 * 0.35 * (inside < 0)
    pupil = np.exp(-(np.hypot(x / 0.55, y) / 0.16) ** 4)
    glint = np.exp(-(np.hypot(x + 0.12, y - 0.13) / 0.06) ** 2) * -0.8
    lashes = np.zeros_like(x)
    for ax, tilt in ((-0.5, -0.45), (0.0, 0.0), (0.5, 0.45)):  # three short strokes fanning up from the upper lid
        y0 = -c + np.sqrt(max(rr * rr - ax * ax, 0))
        bx, by = ax + np.sin(tilt) * 0.2, y0 + 0.06 + np.cos(tilt) * 0.2
        dx, dy = bx - ax, by - (y0 + 0.06)
        t = np.clip(((x - ax) * dx + (y - y0 - 0.06) * dy) / (dx * dx + dy * dy), 0, 1)
        d = np.hypot(x - (ax + t * dx), y - (y0 + 0.06 + t * dy))
        lashes = np.maximum(lashes, np.exp(-(d / (0.035 * (1 - 0.6 * t))) ** 2))
    out = outline + iris + iris_fill + pupil * 0.95 + lashes * 0.85
    out = out + glint * (pupil > 0.3)
    return np.clip(out, 0, 1) * (r < 1)


def _leaf_at(x, y, cx, cy, ang, length, width):
    """Soft leaf (pointed both ends) centred at (cx, cy), long axis at `ang`."""
    u = (x - cx) * np.cos(ang) + (y - cy) * np.sin(ang)
    v = -(x - cx) * np.sin(ang) + (y - cy) * np.cos(ang)
    t = np.clip(u / length, -1, 1)
    hw = width * (1 - t * t) ** 0.8
    body = np.clip((hw - np.abs(v)) / (0.25 * width + 1e-6), 0, 1) * (np.abs(u) < length)
    rib = np.exp(-(v / (0.12 * width + 1e-6)) ** 2) * (np.abs(u) < length * 0.9) * 0.3
    return np.clip(body * 0.8 + rib, 0, 1)


def archer_vine_ring(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    out = np.zeros_like(x)
    for ph in (0.0, np.pi):  # two vines, half a turn apart, twisting round the 0.9 circle
        rc = 0.9 + 0.035 * np.sin(a * 9 + ph)
        thick = 0.016 + 0.006 * np.cos(a * 9 + ph)
        out = np.maximum(out, np.exp(-((r - rc) / thick) ** 2) * (0.85 if ph else 1.0))
    out = np.maximum(out, np.exp(-((r - 0.9) / 0.06) ** 2) * 0.12)  # faint body between them
    for k in range(12):  # leaves sticking out of the ring, alternating out / in
        ang = k * np.pi / 6 + 0.12
        side = 1 if k % 2 == 0 else -1
        cx, cy = np.cos(ang) * (0.9 + side * 0.055), np.sin(ang) * (0.9 + side * 0.055)
        tilt = ang + side * 0.5 + np.pi / 2 * 0.4
        out = np.maximum(out, _leaf_at(x, y, cx, cy, tilt, 0.11, 0.045) * 0.9)
    inner = np.exp(-((r - 0.62) / 0.008) ** 2) * 0.35  # faint inner circle
    return np.clip(out + inner, 0, 1) * (r < 1)


def archer_leaf(s=128):
    x, y = _grid(s)
    leaf = _leaf_at(x, y, 0.0, 0.08, np.pi / 2, 0.82, 0.36)
    stem = np.exp(-(x / 0.03) ** 2) * (y < -0.7) * (y > -0.98) * 0.8
    return np.clip(leaf + stem, 0, 1)


def archer_wind_arc(s=256):
    x, y = _grid(s)
    out = np.zeros_like(x)
    for cx, rad, w, k in ((-0.55, 1.15, 0.07, 1.0), (-0.75, 1.0, 0.035, 0.7)):
        r = np.hypot(x - cx, y)
        a = np.arctan2(y, x - cx)
        span = 0.95  # +-rad of the arc, opening to +x
        t = np.clip(np.abs(a) / span, 0, 1)
        thick = w * (1 - t ** 2) ** 1.2
        stroke = np.exp(-((r - rad) / (thick + 1e-4)) ** 2) * (np.abs(a) < span)
        out = np.maximum(out, stroke * k)
    return np.clip(out, 0, 1) * (np.hypot(x, y) < 1.4)


MAKERS = {"archer-eye": archer_eye, "archer-vine-ring": archer_vine_ring, "archer-leaf": archer_leaf, "archer-wind-arc": archer_wind_arc}
GAME_SIZE = {"archer-eye": (256, 256), "archer-vine-ring": (512, 512), "archer-leaf": (128, 128), "archer-wind-arc": (256, 256)}


def main():
    GAME.mkdir(parents=True, exist_ok=True)
    for name, make in MAKERS.items():
        a = make()
        im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "L").resize(GAME_SIZE[name], Image.LANCZOS)
        p = GAME / (name + ".png")
        im.convert("RGB").save(p)
        print(p)


if __name__ == "__main__":
    main()

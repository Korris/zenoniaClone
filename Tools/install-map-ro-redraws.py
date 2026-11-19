"""Installs the ChatGPT "RO Soft Fantasy" redraws of map grounds (19/11/2026, account Tan Pham), same layout as before, so rows / props are unchanged.

Usage: python Tools/install-map-ro-redraws.py [sunken|oakhaven|all] [--check]
 Sources: Assets/ArtLibrary/maps/<map>/ro-soft-redraw/*-chatgpt-N.png (1536x1024) + the layout image each one was drawn from (*-layout.png).
 sunken:   two redraws of the 58x24 map: west = cols 0-35, east = cols 22-57 (each 36x24 tiles). Each is scaled x1.5 to 64 px / tile, the east one is
           colour-matched to the west one on the shared columns, and they cross-fade over cols 27-29 (water, the narrow NE causeway and the 2-tile
           causeway only). Output: sunken-reliquary-ground-r0c0 / r0c1 (1856x1536) + -minimap (348x144).
 oakhaven: one redraw of the whole 36x24 village, scaled to 2304x1536.
 --check   writes 50 % blends of each redraw over its layout image to docs/reference/maps-ro-soft-fantasy/ (compare before accepting a redraw).
The redraws are already in the RO tone: Tools/bake-map-ro-soft-tone.py skips these grounds (REDRAWN there) and only grades their props.
"""
import os, sys
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIB = os.path.join(ROOT, "Assets", "ArtLibrary", "maps")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
REF = os.path.join(ROOT, "docs", "reference", "maps-ro-soft-fantasy")
T = 64
SUNKEN = {"west": ("sunken-west-ro-redraw-chatgpt-1.png", "sunken-west-cols0-35-layout.png", 0),
          "east": ("sunken-east-ro-redraw-chatgpt-1.png", "sunken-east-cols22-57-layout.png", 22)}
OAKHAVEN = ("oakhaven-ro-redraw-chatgpt-1.png", "oakhaven-layout.png")
FADE_COLS = (27, 30)        # east takes over from col 27 to col 30


def load(path, size=None):
    im = Image.open(path).convert("RGB")
    if size: im = im.resize(size, Image.LANCZOS)
    return np.asarray(im).astype(np.float32)


def sunken():
    d = os.path.join(LIB, "sunken-reliquary", "ro-soft-redraw")
    W, H = 58 * T, 24 * T
    west = load(os.path.join(d, SUNKEN["west"][0]), (36 * T, H))
    east = load(os.path.join(d, SUNKEN["east"][0]), (36 * T, H))
    x0 = SUNKEN["east"][2] * T
    # colour-match east to west on the shared columns 22-35 (mean / std per channel)
    ov_w = west[:, x0:]; ov_e = east[:, :36 * T - x0]
    east = (east - ov_e.mean((0, 1))) / (ov_e.std((0, 1)) + 1e-3) * ov_w.std((0, 1)) + ov_w.mean((0, 1))
    out = np.zeros((H, W, 3), np.float32)
    out[:, :36 * T] = west
    xs = np.arange(W, dtype=np.float32)
    a = np.clip((xs - FADE_COLS[0] * T) / ((FADE_COLS[1] - FADE_COLS[0]) * T), 0, 1)
    a = (a * a * (3 - 2 * a))[None, :, None]
    e_full = np.zeros_like(out); e_full[:, x0:] = east
    out = out * (1 - a) + e_full * a
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    half = W // 2
    for c in range(2):
        p = os.path.join(TILES, "sunken-reliquary-ground-r0c%d.png" % c)
        img.crop((c * half, 0, (c + 1) * half, H)).save(p, optimize=True); print(p)
    p = os.path.join(TILES, "sunken-reliquary-ground-minimap.png")
    img.resize((58 * 6, 24 * 6), Image.LANCZOS).save(p); print(p)


def oakhaven():
    d = os.path.join(LIB, "oakhaven-village", "ro-soft-redraw")
    img = Image.open(os.path.join(d, OAKHAVEN[0])).convert("RGB").resize((36 * T, 24 * T), Image.LANCZOS)
    p = os.path.join(TILES, "oakhaven-ground.png")
    img.save(p, optimize=True); print(p)


def check():
    os.makedirs(REF, exist_ok=True)
    pairs = [(os.path.join(LIB, "sunken-reliquary", "ro-soft-redraw"), r, l, "sunken-" + k) for k, (r, l, _) in SUNKEN.items()]
    pairs.append((os.path.join(LIB, "oakhaven-village", "ro-soft-redraw"), OAKHAVEN[0], OAKHAVEN[1], "oakhaven"))
    for d, r, l, name in pairs:
        if not os.path.exists(os.path.join(d, r)): continue
        a = Image.open(os.path.join(d, l)).convert("RGB"); b = Image.open(os.path.join(d, r)).convert("RGB").resize(a.size)
        p = os.path.join(REF, "%s-ro-redraw-blend50-over-layout.jpg" % name)
        Image.blend(a, b, 0.5).save(p, quality=88); print(p)


if __name__ == "__main__":
    which = next((a for a in sys.argv[1:] if not a.startswith("--")), "all")
    if "--check" in sys.argv: check()
    if which in ("sunken", "all"): sunken()
    if which in ("oakhaven", "all") and os.path.exists(os.path.join(LIB, "oakhaven-village", "ro-soft-redraw", OAKHAVEN[0])): oakhaven()

"""Assembles the 48x32 Sunbreak Foothills ground from the four ChatGPT detail quadrants (each 1536x1024 = 24x16 tiles at 64 px/tile, no upscale).

Usage: python Tools/build-sunbreak-foothills-painted.py [--preview]
 Output: Assets/Resources/Sprites/village/tiles/sunbreak-foothills-ground-r{r}c{c}.png (2x2 chunks of 1536x1024, r0 = top) + -minimap.png (288x192).
 The quadrants butt against each other (no duplicated strip), so there is nothing to min-cut through. Instead:
  1. WARPS    local shifts that slide a feature along the seam until it meets its other half (stream under the west bridge, stream / road at the middle seam).
  2. levelling the low-frequency colour step across each seam is split between both sides and fades out over LEVEL_REACH px.
  3. crossfade each side is blended with the mirror image of the other over +-XFADE px, which removes the hard line without ghosting further out.
  4. PATCHES  clone stamps (source rect -> destination, feathered) for a feature a seam still cuts in two.
 --preview writes the stitched overview and 1:1 crops around every seam to plans/261113-1010-world-map-new-maps/layout-sunbreak/visuals/.
"""
import os, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "sunbreak-foothills")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PLAN = os.path.join(ROOT, "plans", "261113-1010-world-map-new-maps", "layout-sunbreak")
VIS = os.path.join(PLAN, "visuals")
NAME = "sunbreak-foothills-ground"
QUADS = {(0, 0): "r0c0-chatgpt-1", (0, 1): "r0c1-chatgpt-1", (1, 0): "r1c0-chatgpt-2", (1, 1): "r1c1-chatgpt-1"}
QW, QH = 1536, 1024
W, H = QW * 2, QH * 2
LEVEL_REACH = 220     # px over which a seam's colour step fades out
LEVEL_SIGMA = 90      # px of smoothing along the seam (only the slow colour drift is levelled, not the texture)
XFADE = 14            # half width of the mirror crossfade

# (quadrant r, c, axis the pixels move along 'x'|'y', shift px at the seam, centre along the seam, half extent along the seam, reach away from the seam, seam side 'top'|'bottom'|'left'|'right' of the quadrant)
# A positive shift moves the picture right / down. The shift is full at the seam edge and at the centre, zero `reach` px away from the seam and `half` px along it.
WARPS = [
    # stream under the west bridge: r1c0 starts it ~75 px too far left
    (1, 0, "x", 78, 930, 330, 300, "top"),
    # middle seam, stream above the east bridge: r1c0's north bank ends ~45 px higher than r1c1's
    (1, 0, "y", 40, 150, 110, 260, "right"),
]

# (dst x, dst y, w, h, src x, src y, feather px) in full-map pixels, applied after the blend
PATCHES = [
]


def load(n):
    return np.asarray(Image.open(os.path.join(SRC, "sunbreak-foothills-" + n + ".png")).convert("RGB")).astype(np.float32)


def smooth(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3 - 2 * t)


def warp(img, axis, shift, centre, half, reach, side):
    h, w = img.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    if side in ("top", "bottom"):
        away = ys if side == "top" else (h - 1 - ys); along = xs
    else:
        away = xs if side == "left" else (w - 1 - xs); along = ys
    k = smooth(1 - away / reach) * smooth(1 - np.abs(along - centre) / half)
    d = (shift * k).astype(np.float32)
    mx, my = (xs - d, ys) if axis == "x" else (xs, ys - d)
    return cv2.remap(img, mx, my, cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_REPLICATE)


def level(canvas, vertical, pos):
    """Splits the slow colour step across the seam at `pos` (a column when vertical) between both sides."""
    a = canvas if vertical else canvas.transpose(1, 0, 2)
    s = 24
    diff = a[:, pos:pos + s].mean(1) - a[:, pos - s:pos].mean(1)          # (len, 3): right minus left
    diff = cv2.GaussianBlur(diff[None], (0, 0), LEVEL_SIGMA)[0]
    diff = np.clip(diff, -28, 28)
    fall = smooth(1 - np.arange(LEVEL_REACH, dtype=np.float32) / LEVEL_REACH)   # 1 at the seam
    a[:, pos:pos + LEVEL_REACH] -= 0.5 * diff[:, None, :] * fall[None, :, None]
    a[:, pos - LEVEL_REACH:pos] += 0.5 * diff[:, None, :] * fall[::-1][None, :, None]


def crossfade(canvas, vertical, pos):
    a = canvas if vertical else canvas.transpose(1, 0, 2)
    left = a[:, pos - XFADE:pos].copy(); right = a[:, pos:pos + XFADE].copy()
    wgt = (0.5 * (1 - (np.arange(XFADE, dtype=np.float32) + 0.5) / XFADE))[None, :, None]   # weight of the mirrored other side, 0.5 at the seam
    a[:, pos:pos + XFADE] = right * (1 - wgt) + left[:, ::-1] * wgt
    a[:, pos - XFADE:pos] = left * (1 - wgt[:, ::-1]) + right[:, ::-1] * wgt[:, ::-1]


def patch(canvas, dx, dy, w, h, sx, sy, feather):
    m = np.zeros((h, w), np.float32); m[feather:h - feather, feather:w - feather] = 1
    m = cv2.GaussianBlur(m, (0, 0), max(1.0, feather / 2.0))[..., None]
    canvas[dy:dy + h, dx:dx + w] = canvas[dy:dy + h, dx:dx + w] * (1 - m) + canvas[sy:sy + h, sx:sx + w].copy() * m


def assemble():
    q = {k: load(n) for k, n in QUADS.items()}
    for (r, c, axis, shift, centre, half, reach, side) in WARPS:
        q[(r, c)] = warp(q[(r, c)], axis, shift, centre, half, reach, side)
    canvas = np.zeros((H, W, 3), np.float32)
    for (r, c), im in q.items(): canvas[r * QH:(r + 1) * QH, c * QW:(c + 1) * QW] = im
    level(canvas, True, QW); level(canvas, False, QH)
    crossfade(canvas, True, QW); crossfade(canvas, False, QH)
    for p in PATCHES: patch(canvas, *p)
    return np.clip(canvas, 0, 255).astype(np.uint8)


def save(img):
    full = Image.fromarray(img)
    for r in range(2):
        for c in range(2):
            full.crop((c * QW, r * QH, (c + 1) * QW, (r + 1) * QH)).save(os.path.join(TILES, f"{NAME}-r{r}c{c}.png"), optimize=True)
    full.resize((288, 192), Image.LANCZOS).save(os.path.join(TILES, NAME + "-minimap.png"), optimize=True)
    return full


if __name__ == "__main__":
    img = assemble()
    full = save(img)
    if "--preview" in sys.argv:
        os.makedirs(VIS, exist_ok=True)
        full.resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "stitched-overview.png"))
        for i, x in enumerate(range(0, W, 768)): full.crop((x, QH - 256, x + 768, QH + 256)).save(os.path.join(VIS, f"seam-h{i}.png"))
        for i, y in enumerate(range(0, H, 512)): full.crop((QW - 384, y, QW + 384, y + 512)).save(os.path.join(VIS, f"seam-v{i}.png"))
    print("saved chunks + minimap")

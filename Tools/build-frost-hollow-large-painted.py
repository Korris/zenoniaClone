"""Assembles the 72x48 Frost Hollow ground from the ChatGPT outpaint pieces (plan 261105-1240-frost-hollow-large-map, phase 4).

Usage: python Tools/build-frost-hollow-large-painted.py [--preview] [--nearest]
 Source space = 3072x2048 px (42.667 px/tile). Pieces are registered to <0.4 px (phase correlation), so they overlap exactly:
   ground-only (1536x1024) at 0,0 | e1 at 1024,0 | s1 at 0,512 | se1 at 1024,512 | s2 at 0,1024 | se2 at 1024,1024 | fa (1024x1536) at 2048,0 | fb at 2048,512
 Layers are laid down in that order. Each piece is colour matched (per-channel gain/offset) on the area where it duplicates the canvas, then wins
 over the canvas on the far side of a min-cost seam (narrow band next to its new part, a few px of feather), so ground-only stays exact inside.
 Final = x1.5 (Lanczos; --nearest for hard pixels) = 4608x3072 (64 px/tile), cut into 2x2 chunks of 2304x1536
 (Assets/Resources/Sprites/tiles/frost-hollow-large-ground-r{r}c{c}.png, r0 = top) + a 432x288 minimap picture.
The seam helpers are generic here (build-willowbrook-large-painted.py ties its own to Willowbrook constants and fixed piece names).
Choice of x1.5 filter: Lanczos matches the soft look of the old in-game backdrop (frost-hollow-ground.png); --nearest gives uneven 1/2 px pixels.
"""
import os, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LARGE = os.path.join(ROOT, "Assets", "ArtLibrary", "hd", "map", "frost-hollow", "large")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "tiles")
PLAN = os.path.join(ROOT, "plans", "261105-1240-frost-hollow-large-map")
PREV = os.path.join(PLAN, "visuals")
W, H = 3072, 2048
SEAM_SIGMA = 4

# name, file, x, y, seams. seam = (axis, side, band_lo, band_hi, range_lo, range_hi): the piece wins on the far side (axis 'v': x greater than the path;
# axis 'h': y greater) of a min-cost path through band_lo..band_hi (canvas px on the seam axis), searched over range_lo..range_hi along the edge.
PIECES = [
    ("ground-only", "ground-only-chatgpt", 0, 0, []),
    ("e1", "e1-chatgpt", 1024, 0, [("v", 1440, 1536, 0, 1024)]),
    ("s1", "s1-chatgpt", 0, 512, [("h", 928, 1024, 0, 1536)]),
    ("se1", "se1-chatgpt", 1024, 512, [("h", 928, 1024, 1536, 2560), ("v", 1100, 1500, 1024, 1536)]),
    ("s2", "s2-chatgpt", 0, 1024, [("h", 1440, 1536, 0, 1536)]),
    ("se2", "se2-chatgpt", 1024, 1024, [("h", 1440, 1536, 1536, 2560), ("v", 1100, 1500, 1536, 2048)]),
    ("fa", "fa-chatgpt", 2048, 0, [("v", 2464, 2560, 0, 1536)]),
    ("fb", "fb-chatgpt", 2048, 512, [("h", 1440, 1536, 2048, 3072), ("v", 2464, 2560, 1536, 2048)]),
]


def load(name):
    return np.asarray(Image.open(os.path.join(LARGE, name + ".png")).convert("RGB")).astype(np.float32)


def seam_path(a, b, axis, b0, b1, r0, r1):
    """Min-cost path (position on the seam axis for every index r0..r1 along the edge) through the colour difference of canvas a and piece b."""
    if axis == "h":
        a, b = a.transpose(1, 0, 2), b.transpose(1, 0, 2)
    d = ((a[r0:r1, b0:b1] - b[r0:r1, b0:b1]) ** 2).sum(-1)
    d = cv2.blur(d, (9, 9))
    n, w = d.shape
    cost = d.copy(); back = np.zeros((n, w), np.int8)
    for i in range(1, n):
        prev = cost[i - 1]
        cand = np.stack([np.r_[np.inf, prev[:-1]], prev, np.r_[prev[1:], np.inf]])
        k = cand.argmin(0); back[i] = k - 1
        cost[i] += cand[k, np.arange(w)]
    x = int(cost[-1].argmin()); path = np.zeros(n, int)
    for i in range(n - 1, -1, -1):
        path[i] = x
        if i > 0: x += int(back[i, x])
    return path + b0


def wins_mask(a, b, seams):
    """1 where the piece (b) beats the canvas (a) over the full canvas; product of its seam half-planes."""
    m = np.ones((H, W), np.float32)
    xs = np.arange(W)[None, :]; ys = np.arange(H)[:, None]
    for axis, b0, b1, r0, r1 in seams:
        p = seam_path(a, b, axis, b0, b1, r0, r1)
        if axis == "v":
            full = np.full(H, p[0], int); full[r0:r1] = p; full[r1:] = p[-1]
            m *= (xs > full[:, None]).astype(np.float32)
        else:
            full = np.full(W, p[0], int); full[r0:r1] = p; full[r1:] = p[-1]
            m *= (ys > full[None, :]).astype(np.float32)
    return cv2.GaussianBlur(m, (0, 0), SEAM_SIGMA)


def match_color(img, ref, overlap):
    """Per-channel gain (clipped 0.93-1.07) + offset so img matches ref on the overlap pixels."""
    out = img.copy()
    for c in range(3):
        s, r = img[..., c][overlap], ref[..., c][overlap]
        g = float(np.clip(r.std() / max(s.std(), 1e-3), 0.93, 1.07))
        out[..., c] = img[..., c] * g + (r.mean() - g * s.mean())
    return np.clip(out, 0, 255)


def assemble():
    canvas = np.zeros((H, W, 3), np.float32); defined = np.zeros((H, W), bool)
    report = []
    for name, f, ox, oy, seams in PIECES:
        img = load(f); h, w = img.shape[:2]
        full = np.zeros((H, W, 3), np.float32); frame = np.zeros((H, W), bool)
        full[oy:oy + h, ox:ox + w] = img; frame[oy:oy + h, ox:ox + w] = True
        overlap = frame & defined
        if overlap.any():
            before = np.abs(full - canvas)[overlap].mean()
            full = match_color(full, canvas, overlap); full[~frame] = 0
            report.append((name, int(overlap.sum()), round(float(before), 2), round(float(np.abs(full - canvas)[overlap].mean()), 2)))
            win = wins_mask(canvas, full, seams)
            alpha = frame * np.where(defined, win, 1.0)
        else:
            alpha = frame.astype(np.float32)
        canvas = canvas * (1 - alpha[..., None]) + full * alpha[..., None]
        defined |= frame
    assert defined.all(), "canvas has holes"
    return np.clip(canvas + 0.5, 0, 255).astype(np.uint8), report


def upscale(img, nearest):
    return Image.fromarray(img).resize((4608, 3072), Image.NEAREST if nearest else Image.LANCZOS)


def save(img, nearest):
    full = upscale(img, nearest)
    for r in range(2):
        for c in range(2):
            full.crop((c * 2304, r * 1536, (c + 1) * 2304, (r + 1) * 1536)).save(os.path.join(TILES, f"frost-hollow-large-ground-r{r}c{c}.png"), optimize=True)
    full.resize((432, 288), Image.LANCZOS).save(os.path.join(TILES, "frost-hollow-large-ground-minimap.png"), optimize=True)


# 1:1 seam crops (source px, 900x600 windows): old|east, old|south, east|se, ... plus the chunk-join centre
CROPS = {"seam-old-e1": (1100, 350), "seam-old-s1": (400, 760), "seam-e1-se1": (1900, 760), "seam-s1-se1": (1100, 1100), "seam-s1-s2": (500, 1250),
         "seam-se1-se2": (1900, 1250), "seam-s2-se2": (1100, 1600), "seam-e1-fa": (2250, 350), "seam-se1-fa": (2250, 1000), "seam-fa-fb": (2300, 1250),
         "seam-se2-fb": (2250, 1550), "seam-corner-old-e1-s1-se1": (1200, 760)}

if __name__ == "__main__":
    img, report = assemble()
    for r in report: print("piece %-4s overlap px %9d  mean |diff| before match %.2f after %.2f" % r)
    nearest = "--nearest" in sys.argv
    if "--preview" in sys.argv:
        os.makedirs(PREV, exist_ok=True)
        Image.fromarray(img).resize((1536, 1024), Image.LANCZOS).save(os.path.join(PREV, "painted-full.jpg"), quality=88)
        for n, (x, y) in CROPS.items():
            Image.fromarray(img[y:y + 600, x:x + 900]).save(os.path.join(PREV, n + ".jpg"), quality=92)
    Image.fromarray(img).save(os.path.join(os.environ.get("TEMP", "."), "fh-assembled-3072.png"))
    save(img, nearest)
    print("saved 4 chunks + minimap", "(nearest)" if nearest else "(lanczos)")

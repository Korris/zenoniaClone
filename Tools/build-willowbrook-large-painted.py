"""Assembles the 72x48 Willowbrook Meadows ground from the ChatGPT outpaint pieces.

Usage: python Tools/build-willowbrook-large-painted.py [--preview]
 Source space = 3072x2048 px (36x24-tile pieces of 1536x1024 at 42.667 px/tile); final = x1.5 = 4608x3072 (64 px/tile), cut into 2x2 chunks of 2304x1536
 (Assets/Resources/Sprites/village/tiles/willowbrook-large-ground-r{r}c{c}.png, r0 = top) + a 432x288 minimap picture.
 Layers (back to front): old section (exact) < east < top strip (nw|ne min-cut) < bottom strip (sw|se min-cut, SE wins) < fareast.
 Every piece is colour matched (per-channel gain/offset) to its neighbour on the area where the two paintings duplicate each other.
"""
import os, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LARGE = os.path.join(ROOT, "Assets", "ArtLibrary", "hd", "map", "willowbrook-meadows", "large")
OLDP = os.path.join(ROOT, "Assets", "ArtLibrary", "hd", "map", "willowbrook-meadows", "willowbrook-meadows-ground-chatgpt.png")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PREV = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map", "visuals")
W, H = 3072, 2048
FEATHER = 96          # px of blend into the old / east paintings (they duplicate each other there, so no ghosting)
SEAM_SIGMA = 7        # narrow feather around a min-cut seam


def load(path):
    return np.asarray(Image.open(path).convert("RGB")).astype(np.float32)


def piece(n):
    return load(os.path.join(LARGE, n + "-chatgpt.png"))


def ramp(v, a, b):
    return np.clip((v - a) / float(b - a), 0.0, 1.0)


def min_cut_mask(a, b, x0, x1, y0, y1, hw, hh):
    """Mask (1 = b) of a canvas hw x hh: vertical min-cost path through columns x0..x1 over rows y0..y1; left of it a, right of it b.
    a and b are full-canvas float images. Rows outside y0..y1 follow the path ends."""
    d = ((a[y0:y1, x0:x1] - b[y0:y1, x0:x1]) ** 2).sum(-1)
    d = cv2.blur(d, (9, 9))
    h, w = d.shape
    cost = d.copy(); back = np.zeros((h, w), np.int8)
    for y in range(1, h):
        prev = cost[y - 1]
        cand = np.stack([np.r_[np.inf, prev[:-1]], prev, np.r_[prev[1:], np.inf]])
        k = cand.argmin(0); back[y] = k - 1
        cost[y] += cand[k, np.arange(w)]
    x = int(cost[-1].argmin()); path = np.zeros(h, int)
    for y in range(h - 1, -1, -1):
        path[y] = x; x += int(back[y, x]) if y > 0 else 0
    px = np.full(hh, 0, int)
    px[y0:y1] = path + x0; px[:y0] = px[y0]; px[y1:] = px[y1 - 1]
    xs = np.arange(hw)[None, :]
    m = (xs > px[:, None]).astype(np.float32)
    return cv2.GaussianBlur(m, (0, 0), SEAM_SIGMA), px


def place(img, ox, oy):
    c = np.zeros((H, W, 3), np.float32); m = np.zeros((H, W), np.float32)
    h, w = img.shape[:2]
    c[oy:oy + h, ox:ox + w] = img; m[oy:oy + h, ox:ox + w] = 1
    return c, m


def over(base, layer, alpha):
    return base * (1 - alpha[..., None]) + layer * alpha[..., None]


def assemble():
    old = load(OLDP)
    canvas, _ = place(old, 0, 512)
    xs = np.arange(W, dtype=np.float32)[None, :].repeat(H, 0); ys = np.arange(H, dtype=np.float32)[:, None].repeat(W, 1)

    # east (1024,512): left 512 px duplicate old; blend into old over its last FEATHER px
    east = piece("east")
    east = match_color_with(east, east[:, :512], old[:, 1024:])
    ec, _ = place(east, 1024, 512)
    canvas = over(canvas, ec, ramp(xs, 1536 - FEATHER, 1536) * ((ys >= 512) & (ys < 1536)))

    # top strip: nw (matched to old) | ne (matched to east), seam through their overlap x 1024..1536
    nw = piece("nw"); nw = match_color_with(nw, nw[512:], old[:512])
    ne = piece("ne"); ne = match_color_with(ne, ne[512:], east[:512])
    nwc, _ = place(nw, 0, 0); nec, _ = place(ne, 1024, 0)
    m, px = min_cut_mask(nwc, nec, 1088, 1472, 0, 608, W, H)
    top = over(nwc, nec, m)
    canvas = over(canvas, top, 1 - ramp(ys, 512, 512 + FEATHER))

    # bottom strip: sw (matched to old) | se (matched to east), SE wins, seam left of the pond (x < ~1300)
    sw = piece("sw"); sw = match_color_with(sw, sw[:512], old[512:])
    se = piece("se"); se = match_color_with(se, se[:512], east[512:])
    swc, _ = place(sw, 0, 1024); sec, _ = place(se, 1024, 1024)
    m2, px2 = min_cut_mask(swc, sec, 1060, 1300, 1440, 2048, W, H)
    bot = over(swc, sec, m2)
    canvas = over(canvas, bot, ramp(ys, 1536 - FEATHER, 1536))

    # fareast: 1024x1536 drawn at scale 0.75 for source region x 1707..3072, y 0..2048
    fe = np.asarray(Image.open(os.path.join(LARGE, "fareast-chatgpt.png")).convert("RGB").resize((1366, 2048), Image.LANCZOS)).astype(np.float32)
    fe = match_color_with(fe, fe[:, :640], canvas[:, 1707:1707 + 640])
    fc = np.zeros((H, W, 3), np.float32); fc[:, 1707:1707 + 1366] = fe[:, :W - 1707]
    canvas = over(canvas, fc, ramp(xs, 2560 - FEATHER, 2560) * (xs >= 1707))
    return np.clip(canvas, 0, 255).astype(np.uint8), (px, px2)


def match_color_with(img, src_region, ref_region):
    """Colour-match the whole img using gain/offset estimated from src_region -> ref_region."""
    out = img.copy()
    for c in range(3):
        g = np.clip(ref_region[..., c].std() / max(src_region[..., c].std(), 1e-3), 0.93, 1.07)
        o = ref_region[..., c].mean() - g * src_region[..., c].mean()
        out[..., c] = img[..., c] * g + o
    return np.clip(out, 0, 255)


def save(canvas):
    full = Image.fromarray(canvas).resize((4608, 3072), Image.LANCZOS)
    for r in range(2):
        for c in range(2):
            full.crop((c * 2304, r * 1536, (c + 1) * 2304, (r + 1) * 1536)).save(os.path.join(TILES, f"willowbrook-large-ground-r{r}c{c}.png"), optimize=True)
    full.resize((432, 288), Image.LANCZOS).save(os.path.join(TILES, "willowbrook-large-ground-minimap.png"), optimize=True)
    return full


if __name__ == "__main__":
    img, seams = assemble()
    Image.fromarray(img).save(os.path.join(os.environ.get("TEMP", "."), "wb-assembled-3072.png"))
    if "--preview" in sys.argv:
        os.makedirs(PREV, exist_ok=True)
        Image.fromarray(img).resize((1536, 1024), Image.LANCZOS).save(os.path.join(PREV, "painted-full.jpg"), quality=88)
        crops = {"seam-nw-old": (700, 380), "seam-nw-ne": (1000, 250), "seam-old-east": (1250, 700), "seam-east-ne": (1700, 380),
                 "seam-sw-se": (900, 1500), "seam-old-sw": (500, 1380), "seam-east-se": (1700, 1400), "seam-fareast-top": (2300, 150),
                 "seam-fareast-mid": (2300, 900), "seam-fareast-bot": (2300, 1500)}
        for n, (x, y) in crops.items():
            Image.fromarray(img[y:y + 600, x:x + 900]).save(os.path.join(PREV, n + ".jpg"), quality=90)
    save(img)
    print("saved chunks + minimap")

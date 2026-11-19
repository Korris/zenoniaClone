"""ChatGPT "RO Soft Fantasy" redraws of map grounds, drawn window by window on the CURRENT layout (20/11/2026), so rows, props, portals and spawns
stay valid. Generalises Tools/install-map-ro-redraws.py (Oakhaven / Sunken, 19/11) to every map, including the 48x32 and 72x48 ones.

    python Tools/install-map-ro-redraw-windows.py layout  <map>   # write the layout image of each window (send it to ChatGPT, one image per window)
    python Tools/install-map-ro-redraw-windows.py install <map>   # paste the redraws back into the ground chunks
    python Tools/install-map-ro-redraw-windows.py check   <map>   # 50 % blend of each redraw over its layout (accept only when it matches tile by tile)
    python Tools/install-map-ro-redraw-windows.py sheet   <map>   # before / after contact sheet in docs/reference/maps-ro-soft-redraw/

Windows are tile rects with a 3:2 aspect (ChatGPT draws 1536x1024). Overlapping windows are colour-matched to what is already placed (mean / std per
channel on the overlap) and cross-faded with a linear ramp over the overlap, so there is no hard seam.
Sources: Assets/ArtLibrary/maps/<map>/ro-soft-redraw/<map>-w<i>-layout.png + <map>-w<i>-chatgpt-<N>.png (highest N wins).
"Before" = the graded ground the layouts were cut from, saved once as <map>-before-ground.jpg in the same folder (the sheet uses it).
The minimap keeps its old art (it has props baked in). Redrawn grounds are listed in REDRAWN of Tools/bake-map-ro-soft-tone.py (no second grade).
"""
import os, re, sys, glob
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIB = os.path.join(ROOT, "Assets", "ArtLibrary", "maps")
RES = os.path.join(ROOT, "Assets", "Resources", "Sprites")
REF = os.path.join(ROOT, "docs", "reference", "maps-ro-soft-redraw")
T = 64
V = "village/tiles/"
# map -> (tiles W, H), chunk grid (rows, cols) and file pattern, windows (x0, y0, w, h) in tiles
FULL36 = [(0, 0, 36, 24)]
MAPS = {
    "willowbrook-meadows": ((72, 48), (2, 2), V + "willowbrook-large-ground-r{r}c{c}.png", [(0, 0, 39, 26), (33, 0, 39, 26), (0, 22, 39, 26), (33, 22, 39, 26)]),
    "frost-hollow":        ((72, 48), (2, 2), "tiles/frost-hollow-large-ground-r{r}c{c}.png", [(0, 0, 39, 26), (33, 0, 39, 26), (0, 22, 39, 26), (33, 22, 39, 26)]),
    "sunbreak-foothills":  ((48, 32), (2, 2), V + "sunbreak-foothills-ground-r{r}c{c}.png", [(0, 0, 27, 18), (21, 0, 27, 18), (0, 14, 27, 18), (21, 14, 27, 18)]),
    "scorpion-gorge":      ((48, 32), (2, 2), V + "scorpion-gorge-ground-r{r}c{c}.png", [(0, 0, 27, 18), (21, 0, 27, 18), (0, 14, 27, 18), (21, 14, 27, 18)]),
    "thatch-village":      ((36, 24), (1, 1), V + "thatch-village-ground.png", FULL36),
    "bramblewood-forest":  ((36, 24), (1, 1), V + "bramblewood-forest-ground.png", FULL36),
    "rootburrow":          ((36, 24), (1, 1), V + "rootburrow-ground.png", FULL36),
    "sunspire-oasis":      ((36, 24), (1, 1), V + "sunspire-oasis-ground.png", FULL36),
    "amber-dunes":         ((36, 24), (1, 1), V + "amber-dunes-ground.png", FULL36),
    "emberfen-marsh":      ((36, 24), (1, 1), V + "emberfen-marsh-ground.png", FULL36),
    "frostfang-den":       ((36, 24), (1, 1), V + "frostfang-den-ground.png", FULL36),
    "thornheart-chamber":  ((24, 16), (1, 1), V + "thornheart-chamber-ground.png", [(0, 0, 24, 16)]),
}
LAYOUT_SIZE = (1536, 1024)
MAX_FLOW = 24.0     # px at layout size; more than that = ChatGPT moved something, redraw instead


def cfg(key):
    (tw, th), (rows, cols), pat, wins = MAPS[key]
    return tw, th, rows, cols, pat, wins


def src_dir(key):
    d = os.path.join(LIB, key, "ro-soft-redraw"); os.makedirs(d, exist_ok=True); return d


def chunk_paths(key):
    tw, th, rows, cols, pat, _ = cfg(key)
    return [[os.path.join(RES, pat.format(r=r, c=c)) for c in range(cols)] for r in range(rows)]


def read_ground(key):
    tw, th, *_ = cfg(key)
    out = Image.new("RGB", (tw * T, th * T))
    y = 0
    for row in chunk_paths(key):
        x = 0
        for p in row:
            im = Image.open(p).convert("RGB"); out.paste(im, (x, y)); x += im.width
        y += im.height
    if out.size != (tw * T, th * T): raise SystemExit("ground size %s != %s" % (out.size, (tw * T, th * T)))
    return out


def write_ground(key, img):
    paths = chunk_paths(key); rows, cols = len(paths), len(paths[0])
    cw, ch = img.width // cols, img.height // rows
    for r in range(rows):
        for c in range(cols):
            img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)).save(paths[r][c], optimize=True); print(paths[r][c])


def before_path(key): return os.path.join(src_dir(key), "%s-before-ground.jpg" % key)


def layout(key):
    d = src_dir(key)
    if not os.path.exists(before_path(key)): read_ground(key).save(before_path(key), quality=93)
    g = Image.open(before_path(key)).convert("RGB")
    for i, (x0, y0, w, h) in enumerate(cfg(key)[5]):
        p = os.path.join(d, "%s-w%d-layout.png" % (key, i))
        g.crop((x0 * T, y0 * T, (x0 + w) * T, (y0 + h) * T)).resize(LAYOUT_SIZE, Image.LANCZOS).save(p); print(p)


def redraw_path(key, i):
    fs = glob.glob(os.path.join(src_dir(key), "%s-w%d-chatgpt-*.png" % (key, i)))
    if not fs: return None
    return max(fs, key=lambda f: int(re.search(r"-(\d+)\.png$", f).group(1)))


def align(red, lay):
    """Warp a redraw (1536x1024 RGB uint8) onto its layout: affine ECC, then a heavily smoothed DIS optical flow (<= MAX_FLOW px) so roads,
    streams and cliff edges land on the same tiles. Returns (aligned image, mean |flow| px)."""
    import cv2
    from scipy.ndimage import gaussian_filter
    g = lambda a: cv2.GaussianBlur(cv2.cvtColor(a, cv2.COLOR_RGB2GRAY), (0, 0), 3)
    A, B = g(lay), g(red)
    M = np.eye(2, 3, dtype=np.float32)
    try:
        _, M = cv2.findTransformECC(A.astype(np.float32), B.astype(np.float32), M, cv2.MOTION_AFFINE,
                                    (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-5), None, 5)
    except cv2.error:
        M = np.eye(2, 3, dtype=np.float32)
    h, w = A.shape
    red1 = cv2.warpAffine(red, M, (w, h), flags=cv2.INTER_LINEAR | cv2.WARP_INVERSE_MAP, borderMode=cv2.BORDER_REFLECT)
    dis = cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
    f = dis.calc(A, g(red1), None)
    for k in range(2): f[..., k] = gaussian_filter(f[..., k], 28)
    mag = np.sqrt((f ** 2).sum(-1)); f *= np.minimum(1, MAX_FLOW / np.maximum(mag, 1e-3))[..., None]
    xs, ys = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    out = cv2.remap(red1, xs + f[..., 0], ys + f[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
    return out, float(np.sqrt((f ** 2).sum(-1)).mean()), M


def load_aligned(key, i):
    d = src_dir(key)
    lay = np.asarray(Image.open(os.path.join(d, "%s-w%d-layout.png" % (key, i))).convert("RGB"))
    red = np.asarray(Image.open(redraw_path(key, i)).convert("RGB").resize(LAYOUT_SIZE, Image.LANCZOS))
    out, mf, M = align(red, lay)
    print("window %d: affine %s, mean flow %.1f px" % (i, np.round(M, 3).tolist(), mf))
    return Image.fromarray(out)


def ramp(n, lo_fade, hi_fade):
    """1-D weight over n px: rises over lo_fade px at the start, falls over hi_fade px at the end."""
    x = np.arange(n, dtype=np.float32) + 0.5
    a = np.ones(n, np.float32)
    if lo_fade: a = np.minimum(a, x / lo_fade)
    if hi_fade: a = np.minimum(a, (n - x) / hi_fade)
    return np.clip(a, 0, 1)


def install(key):
    tw, th, _, _, _, wins = cfg(key)
    W, H = tw * T, th * T
    acc = np.zeros((H, W, 3), np.float32); wsum = np.zeros((H, W), np.float32)
    base = np.asarray(Image.open(before_path(key)).convert("RGB")).astype(np.float32)
    for i, (x0, y0, w, h) in enumerate(wins):
        p = redraw_path(key, i)
        if not p: raise SystemExit("missing redraw for window %d" % i)
        print("window", i, os.path.basename(p))
        im = np.asarray(load_aligned(key, i).resize((w * T, h * T), Image.LANCZOS)).astype(np.float32)
        X0, Y0 = x0 * T, y0 * T
        # fade only on sides that overlap another window: width = the overlap with that neighbour
        def ov(side):
            best = 0
            for j, (a, b, c, d) in enumerate(wins):
                if j == i: continue
                if side in "lr" and not (b < y0 + h and b + d > y0): continue
                if side in "tb" and not (a < x0 + w and a + c > x0): continue
                if side == "l" and a < x0 < a + c: best = max(best, a + c - x0)
                if side == "r" and a < x0 + w < a + c: best = max(best, x0 + w - a)
                if side == "t" and b < y0 < b + d: best = max(best, b + d - y0)
                if side == "b" and b < y0 + h < b + d: best = max(best, y0 + h - b)
            return best * T
        wx = ramp(w * T, ov("l"), ov("r")); wy = ramp(h * T, ov("t"), ov("b"))
        wgt = (wy[:, None] * wx[None, :]); wgt = wgt * wgt * (3 - 2 * wgt)
        sl = (slice(Y0, Y0 + h * T), slice(X0, X0 + w * T))
        placed = wsum[sl] > 0.05
        if placed.sum() > 1000:     # colour-match to what earlier windows already put there
            cur = acc[sl][placed] / wsum[sl][placed][:, None]; new = im[placed]
            im = (im - new.mean(0)) / (new.std(0) + 1e-3) * cur.std(0) + cur.mean(0)
        acc[sl] += im * wgt[..., None]; wsum[sl] += wgt
    hole = wsum < 1e-4
    out = np.where(hole[..., None], base, acc / np.maximum(wsum, 1e-4)[..., None])
    write_ground(key, Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)))


def check(key):
    os.makedirs(REF, exist_ok=True); d = src_dir(key)
    for i in range(len(cfg(key)[5])):
        p = redraw_path(key, i)
        if not p: continue
        a = Image.open(os.path.join(d, "%s-w%d-layout.png" % (key, i))).convert("RGB"); b = load_aligned(key, i)
        o = os.path.join(REF, "%s-w%d-blend50-over-layout.jpg" % (key, i)); Image.blend(a, b, 0.5).save(o, quality=85); print(o)


def sheet(key):
    os.makedirs(REF, exist_ok=True)
    a = Image.open(before_path(key)).convert("RGB"); b = read_ground(key)
    for im in (a, b): im.thumbnail((1100, 1100))
    S = Image.new("RGB", (a.width * 2 + 12, a.height + 28), (24, 24, 30)); dr = ImageDraw.Draw(S)
    S.paste(a, (0, 28)); S.paste(b, (a.width + 12, 28))
    dr.text((8, 8), "TRUOC (chinh mau 19/11)  " + key, fill=(255, 255, 255)); dr.text((a.width + 20, 8), "SAU (ChatGPT ve lai RO Soft Fantasy 20/11)", fill=(255, 255, 255))
    o = os.path.join(REF, "%s-before-after.jpg" % key); S.save(o, quality=86); print(o)


if __name__ == "__main__":
    if len(sys.argv) < 3 or sys.argv[2] not in MAPS: raise SystemExit(__doc__ + "\nmaps: " + ", ".join(MAPS))
    {"layout": layout, "install": install, "check": check, "sheet": sheet}[sys.argv[1]](sys.argv[2])

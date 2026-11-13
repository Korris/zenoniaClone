"""Installs the REDRAWN ground of a 72x48 map (Willowbrook Meadows, Frost Hollow) from its four ChatGPT quarters.

Usage: python Tools/build-large-ground-from-redraw-quarters.py <willowbrook|frost-hollow> [--preview]
 Source: Assets/ArtLibrary/maps/<map>/redraw/<prefix>-r{r}c{c}-redraw-chatgpt-<n>.png, each 1536x1024 = one 36x24-tile quarter (42.667 px/tile). The highest
 numbered file without "-old" in its name is taken. The quarters ABUT (no overlap, unlike the outpaint pieces of build-*-large-painted.py), so there is no
 min-cut: 1) every quarter is colour matched (gain/offset per channel) to its already placed
 neighbours on the strips either side of their shared edge; 2) the step left across each edge is low-passed along the edge and spread into both quarters
 (half each, fading over SPREAD px), so brightness has no jump while the texture stays untouched; 3) each side is continued band px (per map) past the
 edge by its own mirror image and a min-cost seam (seam_path of build-frost-hollow-large-painted.py) is cut through that artificial overlap, so cut flower
 clumps, road offsets and shore lines end on a natural outline instead of a straight line.
 WARPS (per map) first slide part of a quarter vertically where a feature straddling an edge was drawn at another height (Willowbrook's pond: the r1c0
 half sat ~60 px lower than the r1c1 half).
 Output = x1.5 Lanczos = 4608x3072 (64 px/tile) cut into 2x2 chunks of 2304x1536 + a 432x288 minimap, written over the map's current textures
 (same Resources paths; the old ones stay in git). --preview also writes plans/<plan>/visuals/redraw-*.jpg.
Then: build-<map>-large-rows.py (collision), preview-<map>-large-composite.py --minimap (props baked into the minimap).
"""
import glob, importlib.util, os, re, sys, time
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS = {
    "willowbrook": dict(src="willowbrook-meadows", prefix="willowbrook-large", tiles=("village", "tiles"), out="willowbrook-large-ground",
                        plan="261105-1052-willowbrook-large-hunting-map", gain=(0.80, 1.25), band=48,
                        # (r, c, x0, x1, y0, y1, dy): rows below y1 move by dy px (negative = up) at column x1, nothing moves at column x0 or above row y0
                        warps=[(1, 0, 1150, 1536, 300, 560, -60)]),
    "frost-hollow": dict(src="frost-hollow", prefix="frost-hollow-large", tiles=("tiles",), out="frost-hollow-large-ground",
                         plan="261105-1240-frost-hollow-large-map", gain=(0.93, 1.07), band=24),
}
QW, QH = 1536, 1024
W, H = 2 * QW, 2 * QH
STRIP = 96      # px either side of a shared edge used for the colour match
SPREAD = 220    # px over which the leftover step across an edge fades into each quarter
CUT_SIGMA = 2.0
EDGE_SIGMA = 60 # low-pass (along the edge) of that step: only brightness/tint, not texture


spec = importlib.util.spec_from_file_location("fhp", os.path.join(os.path.dirname(os.path.abspath(__file__)), "build-frost-hollow-large-painted.py"))
fhp = importlib.util.module_from_spec(spec); spec.loader.exec_module(fhp)


def quarter_path(cfg, r, c):
    pat = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", cfg["src"], "redraw", "%s-r%dc%d-redraw-chatgpt-*.png" % (cfg["prefix"], r, c))
    best = None
    for p in glob.glob(pat):
        m = re.search(r"chatgpt-(\d+)\.png$", p)      # "-old" variants have a suffix after the number, so they do not match
        if m and (best is None or int(m[1]) > best[0]): best = (int(m[1]), p)
    if best is None: raise SystemExit("no quarter for r%dc%d (%s)" % (r, c, pat))
    return best[1]


def match(img, src, ref, gain):
    """match_color_with of build-willowbrook-large-painted.py with the gain range of the map (Willowbrook's r0c1 is far darker than the 0.93-1.07 the outpaint pieces needed)."""
    out = img.copy()
    for ch in range(3):
        g = np.clip(ref[..., ch].std() / max(src[..., ch].std(), 1e-3), gain[0], gain[1])
        out[..., ch] = img[..., ch] * g + ref[..., ch].mean() - g * src[..., ch].mean()
    return np.clip(out, 0, 255)


def assemble(cfg):
    q = {}
    for r in range(2):
        for c in range(2):
            p = quarter_path(cfg, r, c); print("r%dc%d" % (r, c), os.path.basename(p))
            im = Image.open(p).convert("RGB")
            if im.size != (QW, QH): raise SystemExit("%s is %s, expected %dx%d" % (p, im.size, QW, QH))
            q[r, c] = np.asarray(im).astype(np.float32)
    for r, c, x0, x1, y0, y1, dy in cfg.get("warps", []): q[r, c] = warp(q[r, c], x0, x1, y0, y1, dy)
    g = cfg["gain"]
    # r0c0 is the reference; r0c1 to its right edge, r1c0 to its bottom edge, r1c1 to both neighbours
    q[0, 1] = match(q[0, 1], q[0, 1][:, :STRIP], q[0, 0][:, -STRIP:], g)
    q[1, 0] = match(q[1, 0], q[1, 0][:STRIP], q[0, 0][-STRIP:], g)
    src = np.concatenate([q[1, 1][:STRIP].reshape(-1, 3), q[1, 1][:, :STRIP].reshape(-1, 3)])[None]
    ref = np.concatenate([q[0, 1][-STRIP:].reshape(-1, 3), q[1, 0][:, -STRIP:].reshape(-1, 3)])[None]
    q[1, 1] = match(q[1, 1], src, ref, g)
    canvas = np.zeros((H, W, 3), np.float32)
    for (r, c), im in q.items(): canvas[r * QH:(r + 1) * QH, c * QW:(c + 1) * QW] = im
    canvas = level_edge(canvas, "v", cfg["band"]); canvas = level_edge(canvas, "h", cfg["band"])
    return np.clip(canvas + 0.5, 0, 255).astype(np.uint8)


def smooth(v, a, b):
    t = np.clip((v - a) / float(b - a), 0, 1)
    return t * t * (3 - 2 * t)


def warp(img, x0, x1, y0, y1, dy):
    h, w = img.shape[:2]
    xs, ys = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    shift = dy * smooth(xs, x0, x1) * smooth(ys, y0, y1)
    return cv2.remap(img, xs, ys - shift, cv2.INTER_CUBIC, borderMode=cv2.BORDER_REFLECT)


def level_edge(canvas, axis, BAND):
    """Removes the low-frequency colour step across the middle edge (axis 'v': x = QW; 'h': y = QH) and cuts a min-cost seam through the mirrored overlap."""
    a = canvas if axis == "v" else canvas.transpose(1, 0, 2)
    mid = a.shape[1] // 2
    step = a[:, mid:mid + 12].mean(1) - a[:, mid - 12:mid].mean(1)                 # (n, 3): far side minus near side
    step = cv2.GaussianBlur(step[None].astype(np.float32), (0, 0), EDGE_SIGMA)[0]  # along the edge
    fall = np.clip(1 - np.abs(np.arange(a.shape[1]) - (mid - 0.5)) / SPREAD, 0, 1) ** 2
    sign = np.where(np.arange(a.shape[1]) < mid, 0.5, -0.5) * fall
    a = a + step[:, None, :] * sign[None, :, None]
    # each side continued past the edge by its own mirror image = an artificial overlap of 2 x BAND px; the min-cost path through it (seam_path of
    # build-frost-hollow-large-painted.py) runs where the two agree, so a road or shore ends on a believable outline instead of a straight line or a ghost
    near, far = a.copy(), a.copy()
    near[:, mid:mid + BAND] = a[:, mid - BAND:mid][:, ::-1]
    far[:, mid - BAND:mid] = a[:, mid:mid + BAND][:, ::-1]
    path = fhp.seam_path(near, far, "v", mid - BAND, mid + BAND, 0, a.shape[0])
    m = (np.arange(a.shape[1])[None, :] > path[:, None]).astype(np.float32)
    m = cv2.GaussianBlur(m, (0, 0), CUT_SIGMA)[..., None]
    a = near * (1 - m) + far * m
    return a if axis == "v" else a.transpose(1, 0, 2)


def save(cfg, img):
    tiles = os.path.join(ROOT, "Assets", "Resources", "Sprites", *cfg["tiles"])
    full = Image.fromarray(img).resize((4608, 3072), Image.LANCZOS)
    for r in range(2):
        for c in range(2):
            write(full.crop((c * 2304, r * 1536, (c + 1) * 2304, (r + 1) * 1536)), os.path.join(tiles, "%s-r%dc%d.png" % (cfg["out"], r, c)))
    write(full.resize((432, 288), Image.LANCZOS), os.path.join(tiles, cfg["out"] + "-minimap.png"))


def write(im, path):
    """The open Unity editor may hold a texture for a moment while it reimports: retry instead of leaving half the chunks old."""
    for attempt in range(10):
        try: im.save(path, optimize=True); return
        except OSError:
            if attempt == 9: raise
            time.sleep(1.0)


if __name__ == "__main__":
    name = sys.argv[1] if len(sys.argv) > 1 else ""
    if name not in MAPS: raise SystemExit(__doc__)
    cfg = MAPS[name]
    img = assemble(cfg)
    if "--preview" in sys.argv:
        vis = os.path.join(ROOT, "plans", cfg["plan"], "visuals"); os.makedirs(vis, exist_ok=True)
        Image.fromarray(img).resize((1536, 1024), Image.LANCZOS).save(os.path.join(vis, "redraw-full.jpg"), quality=88)
        Image.fromarray(img[H // 2 - 300:H // 2 + 300]).resize((1536, 300), Image.LANCZOS).save(os.path.join(vis, "redraw-seam-h.jpg"), quality=90)
        for i, y in enumerate((0, 512, 1024, 1448)):
            Image.fromarray(img[y:y + 600, W // 2 - 450:W // 2 + 450]).save(os.path.join(vis, "redraw-seam-v%d.jpg" % i), quality=90)
        for i, x in enumerate((0, 700, 1400, 2172)):
            Image.fromarray(img[H // 2 - 300:H // 2 + 300, x:x + 900]).save(os.path.join(vis, "redraw-seam-h%d.jpg" % i), quality=90)
    save(cfg, img)
    print("saved 4 chunks + minimap for", name)

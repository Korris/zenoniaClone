"""Builds the 72x48 Willowbrook Meadows ground (64 px/tile = 4608x3072) as 2x2 chunks + minimap.

Usage: python Tools/build-willowbrook-large-ground.py [build|debug]
 Texture splat: grass/dirt/water textures are synthesised from patches of the old painted ground, blended by masks from
 the 72x48 rows (g grass, d road, w/B water, T forest = dark grass, area stamps for farm/ruins/bridge), then the old
 36x24 painting is pasted unchanged at tile (0,12) with a 3-tile feather where its edges were opened.
 Outputs (Assets/Resources/Sprites/village/tiles): willowbrook-large-ground-r{0,1}c{0,1}.png (2304x1536) and -minimap.png (432x288).
"""
import importlib.util, json, os, sys
import numpy as np, cv2
from PIL import Image
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLAN = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
OLD = os.path.join(TILES, "willowbrook-meadows-ground.png")
NAME = "willowbrook-large-ground"
T, COLS, ROWS = 64, 72, 48
FW, FH = COLS * T, ROWS * T                 # 4608 x 3072
S = 2                                        # mask space is 1/S of full size
OX, OY, OW, OH = 0, 12 * T, 36 * T, 24 * T   # old section rect (full-res px)
spec = importlib.util.spec_from_file_location("hp", os.path.join(os.path.dirname(__file__), "willowbrook-ground-synth-helpers.py"))
hp = importlib.util.module_from_spec(spec); spec.loader.exec_module(hp)
ss = hp.smoothstep
rng = np.random.default_rng(20261105)


def load_rows():
    rows = [l.rstrip("\n") for l in open(os.path.join(PLAN, "layout", "willowbrook-72x48-rows.txt")) if l.strip()]
    assert len(rows) == ROWS and all(len(r) == COLS for r in rows), "rows must be 72x48"
    return np.array([list(r) for r in rows])


def up(a, f=S):  # mask space -> full res
    return cv2.resize(a, (a.shape[1] * f, a.shape[0] * f), interpolation=cv2.INTER_LINEAR)


def tile_field(cls, chars):
    return cv2.resize(np.isin(cls, list(chars)).astype(np.float32), (COLS * T // S, ROWS * T // S), interpolation=cv2.INTER_NEAREST)


def painted_masks(old):
    """Water and road masks of the old painting (colour classification), full res of the old image."""
    P = 64   # edge-replicate so the morphology below does not eat the stream/road where it leaves the painting
    old = np.pad(old, ((P, P), (P, P), (0, 0)), mode="edge")
    b = cv2.GaussianBlur(old, (0, 0), 3); sm = b.sum(2) + 1
    br, rr = b[..., 2] / sm, b[..., 0] / sm
    w = (br > 0.26) & (b[..., 1] / sm < 0.38)
    w = ndi.binary_opening(w, iterations=2)
    lab, n = ndi.label(w); sz = ndi.sum(w, lab, range(1, n + 1)); w = np.isin(lab, 1 + np.where(sz > 3000)[0])
    w = ndi.binary_fill_holes(ndi.binary_closing(w, iterations=10)); w = ndi.binary_dilation(w, iterations=6)
    d = (br > 0.185) & (sm > 430) & (br < 0.27) & ~w
    d = ndi.binary_opening(d, iterations=3)
    lab, n = ndi.label(d); sz = ndi.sum(d, lab, range(1, n + 1)); d = np.isin(lab, 1 + np.where(sz > 6000)[0])
    return w[P:-P, P:-P], d[P:-P, P:-P]


def sample_patches(old, pw, pd):
    """Patch pools (grass 192, dirt 80, water 80) cut from the old painting where there is nothing but that material."""
    mn = old.min(2)
    flo = (old[..., 2] > 125) | (mn > 150) | ((old[..., 0] > 225) & (old[..., 2] < 80))
    b = cv2.GaussianBlur(old, (0, 0), 4); sm = b.sum(2) + 1
    nong = ((b[..., 2] / sm > 0.2) | (sm > 470) | flo | cv2.dilate(pw.astype(np.uint8), np.ones((90, 90))).astype(bool)
            | cv2.dilate(pd.astype(np.uint8), np.ones((40, 40))).astype(bool))
    pools = {}
    def windows(mask, P, step, tol):
        ii = cv2.integral(mask.astype(np.uint8)).astype(np.int64); out = []
        for y in range(0, mask.shape[0] - P, step):
            for x in range(0, mask.shape[1] - P, step):
                if ii[y + P, x + P] - ii[y, x + P] - ii[y + P, x] + ii[y, x] <= P * P * tol: out.append((x, y))
        sel = []
        for i in rng.permutation(len(out)):
            x, y = out[i]
            if all(abs(x - a) > P * 0.5 or abs(y - c) > P * 0.6 for a, c in sel): sel.append((x, y))
        return [old[y:y + P, x:x + P] for x, y in sel]
    pools["grass"] = windows(nong, 192, 16, 0.004)
    # dirt/water: windows fully inside the eroded painted mask (inverted = "bad" pixels)
    pools["dirt"] = windows(~ndi.binary_erosion(pd, iterations=7), 40, 4, 0.02)
    pools["water"] = [q for q in windows(~ndi.binary_erosion(pw, iterations=10), 48, 6, 0.0)
                      if (q.sum(2) < 250).mean() < 0.02 and q.sum(2).mean() > 330]
    for k, v in pools.items(): print("patch pool", k, len(v))
    return pools


def build_masks(cls, old_w, old_d):
    """Soft material fields in mask space (1/S res). Returns dict of float32 arrays."""
    h, w = FH // S, FW // S
    rw = hp.warp(hp.warp(tile_field(cls, "wB"), rng, 10, 150), rng, 9, 50); rd = hp.warp(hp.warp(tile_field(cls, "d"), rng, 12, 150), rng, 9, 50)
    rt = tile_field(cls, "T")
    rw = hp.blur(rw, 11); rd = hp.blur(rd, 10)
    # old painting masks, clamp-extended outward (stream/road continue straight out of the old edge)
    ow = cv2.resize(old_w.astype(np.float32), (OW // S, OH // S)); od = cv2.resize(old_d.astype(np.float32), (OW // S, OH // S))
    yy, xx = np.mgrid[0:h, 0:w]
    y0, x0 = OY // S, OX // S
    cy, cx = np.clip(yy - y0, 0, ow.shape[0] - 1), np.clip(xx - x0, 0, ow.shape[1] - 1)
    dout = np.maximum.reduce([np.zeros_like(yy), y0 - yy, yy - (y0 + ow.shape[0] - 1), xx - (x0 + ow.shape[1] - 1)]).astype(np.float32)
    wp = 1 - ss(0, 176 / S * 1.0 * S / 2, dout)   # painted shape fades out over 176 mask px (5.5 tiles)
    wp = 1 - ss(0, 88, dout)
    water = wp * hp.blur(ow[cy, cx], 3) + (1 - wp) * rw
    dirt = wp * hp.blur(od[cy, cx], 3) + (1 - wp) * rd
    inside = (dout == 0) & (yy >= y0) & (xx >= x0) & (yy < y0 + ow.shape[0]) & (xx < x0 + ow.shape[1])
    dark = hp.blur(rt, 20) * (1 - wp) * 1.7
    dark = np.clip(dark + 0.0, 0, 1)
    dry = ss(0.56, 0.78, hp.fractal((h, w), [160, 70, 30], rng))
    return dict(water=water.astype(np.float32), dirt=dirt.astype(np.float32), dark=dark.astype(np.float32), dry=dry.astype(np.float32), wp=wp.astype(np.float32))


def rect_field(tx, ty, tw, th, pad=0.0):
    a = np.zeros((FH // S, FW // S), np.float32)
    a[int((ty - pad) * T / S):int((ty + th + pad) * T / S), int((tx - pad) * T / S):int((tx + tw + pad) * T / S)] = 1
    return hp.blur(hp.warp(a, rng, 12, 60), 5)


def main(mode):
    cls = load_rows(); lay = json.load(open(os.path.join(PLAN, "layout", "willowbrook-72x48-layout.json")))
    old = np.asarray(Image.open(OLD).convert("RGB")).astype(np.float32)
    pw, pd = painted_masks(old)
    pools = sample_patches(old, pw, pd)
    m = build_masks(cls, pw, pd)
    shape = (FH, FW)
    # --- full-res weights
    nz = hp.fractal(shape, [5, 11], rng)
    water = ss(0.42, 0.58, up(m["water"]) + (hp.fractal(shape, [24, 9], rng) - 0.5) * 0.16)
    dirt = ss(0.36, 0.64, up(m["dirt"]) + (nz - 0.5) * 0.3 + (hp.fractal(shape, [60], rng) - 0.5) * 0.25) * (1 - water)
    dist_w = ndi.distance_transform_edt(water < 0.5) / 1.0       # px from water (outside)
    depth = ndi.distance_transform_edt(water >= 0.5)              # px inside water
    bank = np.clip(1 - dist_w / 48.0, 0, 1) * (1 - water)
    del nz
    dark = up(m["dark"]); dry = up(m["dry"]) * 0.85
    dry = np.clip(dry + 0.5 * np.clip(1 - ndi.distance_transform_edt(dirt < 0.5) / 60, 0, 1) * (hp.fractal(shape, [30], rng) > 0.45), 0, 1)
    # --- materials
    g_hp, g_mean = hp.synth(pools["grass"], 192, 96, shape, rng)
    low = hp.fractal(shape, [420, 170, 70], rng) - 0.5
    lowc = np.stack([1 + 0.16 * low, 1 + 0.10 * low, 1 - 0.20 * low], -1)
    grass = (g_mean + g_hp * 1.05) * lowc * np.array([0.97, 0.985, 0.88], np.float32)
    del g_hp
    gdark = grass * np.array([0.60, 0.70, 0.80], np.float32) + np.array([0, 4, 6], np.float32)
    gdry = grass * np.array([1.05, 1.02, 1.18], np.float32) + np.array([8, 4, 2], np.float32)
    grass = grass * (1 - dark[..., None]) + gdark * dark[..., None]
    grass = grass * (1 - dry[..., None] * (1 - dark[..., None])) + gdry * (dry * (1 - dark))[..., None]
    del gdark, gdry
    moist = np.array([0.80, 0.84, 0.78], np.float32)
    grass = grass * (1 - 0.45 * bank[..., None]) + grass * moist * 0.45 * bank[..., None] * 1.0
    d_hp, d_mean = hp.synth(pools["dirt"], 40, 20, shape, rng)
    dirtc = (d_mean * 0.90 + d_hp * 0.95) * (1 + 0.34 * (hp.fractal(shape, [220, 70, 24], rng) - 0.5))[..., None]
    peb = (hp.fractal(shape, [6], rng) > 0.80)[..., None] * 0.0
    dirtc *= np.array([1.0, 0.97, 0.90], np.float32)
    del d_hp
    w_hp, w_mean = hp.synth(pools["water"], 48, 24, shape, rng)
    watc = (w_mean + w_hp * 1.0) * (1 - 0.30 * ss(0, 60, depth) * 0.0)[..., None]
    watc = watc * (0.80 + 0.20 * (1 - ss(0, 70, depth)))[..., None]
    del w_hp
    out = grass.copy(); del grass
    # mud rim where grass meets water, then water itself (shore foam-free, slight dark edge)
    rim = (1 - ss(0, 26, dist_w)) * (1 - water) * (0.6 + 0.4 * hp.fractal(shape, [30, 10], rng))
    rimc = np.array([88, 92, 48], np.float32)
    out = out * (1 - 0.55 * rim[..., None]) + rimc * 0.55 * rim[..., None]
    out = out * (1 - dirt[..., None]) + dirtc * dirt[..., None]
    out = out * (1 - water[..., None]) + watc * water[..., None]
    del dirtc, watc
    stamps(out, lay, cls, dirt, water)
    scatter_flowers(out, old, lay, m)
    # --- old painting on top with feather (top, right, bottom edges only)
    yy, xx = np.mgrid[0:OH, 0:OW]
    din = np.minimum.reduce([yy, OH - 1 - yy, OW - 1 - xx]).astype(np.float32)
    a = ss(0, 192, din)[..., None]
    out[OY:OY + OH, OX:OX + OW] = out[OY:OY + OH, OX:OX + OW] * (1 - a) + old * a
    final = np.clip(out, 0, 255).astype(np.uint8)
    save(final, mode)


def stamps(out, lay, cls, dirt, water):
    from_h = lambda a: up(a)
    ar = lay["areas"]
    # ruins paving
    r = ar["ruins"]; f = ss(0.35, 0.65, from_h(rect_field(r["tx"], r["ty"], r["w"], r["h"])) + (hp.fractal(out.shape[:2], [90, 30], rng) - 0.5) * 0.45)
    x0, y0, x1, y1 = [int(v * T) for v in (r["tx"] - 1, r["ty"] - 1, r["tx"] + r["w"] + 1, r["ty"] + r["h"] + 1)]
    f = f[y0:y1, x0:x1, None]; fl = hp.flagstones((y1 - y0, x1 - x0), rng)
    fl *= (0.9 + 0.2 * (hp.fractal(fl.shape[:2], [60, 20], rng)))[..., None]
    moss = (hp.smoothstep(0.52, 0.72, hp.fractal(fl.shape[:2], [70, 26, 9], rng)) * 0.75)[..., None]   # grass creeping over the paving
    fl = fl * (1 - moss) + out[y0:y1, x0:x1] * moss
    out[y0:y1, x0:x1] = out[y0:y1, x0:x1] * (1 - f) + fl * f
    # farm: tilled soil with furrows
    fm = ar["farm"]
    f = ss(0.4, 0.6, from_h(rect_field(fm["tx"], fm["ty"], fm["w"], fm["h"])) + (hp.fractal(out.shape[:2], [70, 22], rng) - 0.5) * 0.3)
    x0, y0, x1, y1 = [int(v * T) for v in (fm["tx"] - 1, fm["ty"] - 1, fm["tx"] + fm["w"] + 1, fm["ty"] + fm["h"] + 1)]
    soil = np.array([146, 105, 62], np.float32); fur = hp.furrows((y1 - y0, x1 - x0), rng, 38)
    gr = (hp.fractal(fur.shape, [3, 8], rng) - 0.5) * 36
    sc = soil * (0.74 + 0.36 * fur)[..., None] + gr[..., None] * 0.7
    f = f[y0:y1, x0:x1, None]
    out[y0:y1, x0:x1] = out[y0:y1, x0:x1] * (1 - f) + sc * f
    # new bridge (B tiles outside the old section): planks, rails, shadow on the water
    bt = np.isin(cls, ["B"]); bt[12:36, 0:36] = False
    if bt.any():
        deck = cv2.resize(bt.astype(np.float32), (FW, FH), interpolation=cv2.INTER_NEAREST)
        deck = ss(0.5, 0.56, cv2.GaussianBlur(deck, (0, 0), 14)); deck = cv2.GaussianBlur(deck, (0, 0), 1.5)
        shadow = cv2.GaussianBlur(np.roll(deck, 12, 0), (0, 0), 7) * 0.45
        out *= (1 - shadow[..., None] * (1 - deck[..., None]))
        pl = hp.planks((FH, FW), rng, True)
        inner = cv2.erode(deck, np.ones((13, 13)))
        rail = (deck - inner)[..., None]
        deckc = pl * (1 - rail) + np.array([92, 64, 38], np.float32) * rail
        out[:] = out * (1 - deck[..., None]) + deckc * deck[..., None]


def scatter_flowers(out, old, lay, m):
    """Pastes ~280 flower/tuft clusters cut from the old painting onto open grass (never on the old section)."""
    b = cv2.GaussianBlur(old, (0, 0), 25)
    mn = old.min(2)
    fl = ((old[..., 2] > 112) | (mn > 140) | ((old[..., 0] > 220) & (old[..., 2] < 85)) | (np.abs(old - b).max(2) > 55))
    fl = cv2.morphologyEx(fl.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((9, 9)))
    fl = cv2.dilate(fl, np.ones((13, 13)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(fl)
    clips = []
    for i in range(1, n):
        x, y, w, h, a = st[i]
        if 900 < a < 30000 and w < 260 and h < 200 and y > 40 and y + h < old.shape[0] - 40:
            al = cv2.GaussianBlur((lab[y:y + h, x:x + w] == i).astype(np.float32), (0, 0), 7)
            clips.append((old[y:y + h, x:x + w], np.clip(al * 1.15 - 0.1, 0, 1)))
    print("flower clips", len(clips))
    ok = ((1 - m["dark"]) * (1 - np.clip(m["water"] * 3, 0, 1)) * (1 - np.clip(m["dirt"] * 3, 0, 1)))
    for k in ("ruins", "farm"):                      # stamps keep their own look
        a = lay["areas"][k]; ok[int((a["ty"] - 1) * T / S):int((a["ty"] + a["h"] + 1) * T / S), int((a["tx"] - 1) * T / S):int((a["tx"] + a["w"] + 1) * T / S)] = 0
    ok[(OY - 100) // S:(OY + OH + 100) // S, :(OX + OW + 100) // S] = 0   # old section keeps its own flowers
    wm = m["water"]
    bank_pts = np.argwhere((wm > 0.10) & (wm < 0.27) & (ok > -1) & (m["dirt"] < 0.2) & (m["wp"] < 0.05))

    def paste(c, al, cy, cx):
        h, w = al.shape; y, x = cy - h // 2, cx - w // 2
        if y < 0 or x < 0 or y + h > FH or x + w > FW: return False
        if rng.random() < 0.5: c, al = c[:, ::-1], al[:, ::-1]
        bg = c[al < 0.05].mean(0) if (al < 0.05).sum() > 20 else c.mean((0, 1))
        out[y:y + h, x:x + w] += (c - bg) * al[..., None]          # detail transfer: keeps the local ground colour
        return True

    placed = tries = 0
    while placed < 300 and tries < 8000 and clips:
        tries += 1
        c, al = clips[rng.integers(len(clips))]
        cy, cx = rng.integers(0, FH), rng.integers(0, FW)
        h, w = al.shape
        if ok[cy // S, cx // S] < 0.97 or ok[min(cy // S + h // 2 // S, FH // S - 1), min(cx // S + w // 2 // S, FW // S - 1)] < 0.9 or m["dark"][cy // S, cx // S] > 0.05: continue
        placed += paste(c, al, cy, cx)
    bp = 0
    for _ in range(200):
        if len(bank_pts) == 0 or not clips: break
        c, al = clips[rng.integers(len(clips))]
        yx = bank_pts[rng.integers(len(bank_pts))]
        bp += paste(c, al, int(yx[0]) * S, int(yx[1]) * S)
    print("flowers placed", placed, "bank tufts", bp)


def save(final, mode):
    for r in range(2):
        for c in range(2):
            Image.fromarray(final[r * FH // 2:(r + 1) * FH // 2, c * FW // 2:(c + 1) * FW // 2]).save(os.path.join(TILES, f"{NAME}-r{r}c{c}.png"), optimize=True)
    Image.fromarray(final).resize((432, 288), Image.LANCZOS).save(os.path.join(TILES, f"{NAME}-minimap.png"), optimize=True)
    print("saved chunks + minimap")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "build")

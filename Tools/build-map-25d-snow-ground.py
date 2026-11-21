"""Mid-value cool snow ground of a 2.5D snow map from one ChatGPT 2x2 swatch atlas + the rows (first: Frost Hollow, 21/11/2026).

    python Tools/build-map-25d-snow-ground.py frost-hollow [--preview]     the map's ground chunks (same files the game loads)
    python Tools/build-map-25d-snow-ground.py frost-hollow border          <ground>-border.png: the render-only band past the map edges

Why (user 21/11: "sao không design lại map tuyết luôn"; ChatGPT art direction docs/reference/frost-hollow-25d/snow-25d-art-direction-chatgpt.md):
the old near-white snow (L ~0.85-0.95) ate the pale translucent RO-trailer skill FX, so the snow is redone MID-VALUE (main #8999AF, Y ~0.31)
instead of making the FX opaque. And at the 58 deg camera the cliffs painted into the old ground would lie flat and tilt, so the ground keeps only
flat materials and the cliffs become standing props (Tools/place-map-25d-dungeon-props.py frost-hollow):
  1. snow: atlas top-left (cool snow) graded to #8999AF, ~15 % light patches (top-right, #B2BED0) and ~20 % shadow / lavender snow
     (bottom-left, #6F7F98) in broad 2-5 tile noise patches; fine grain is the swatch's own;
  2. packed path on the lane tiles (plans/261105-1240-frost-hollow-large-map/layout/frost-hollow-72x48-lanes.txt), atlas bottom-right
     graded to #848D9B, edge feathered over ~0.35 tile with snow nibbling in, no white border;
  3. solid tiles ('C' cliff / plateau): rocky snow mass (shadow swatch graded #6E7D93) deepening to #56657B 1.5+ tiles from the floor (they sit
     behind the standing cliffs and pines), fake AO band on the floor at the wall foot;
  4. frozen river ('w'): dark turquoise ice #417F9D, deep channel #315B7A down the middle, thin crust streaks along the flow, slate rim #425B73;
     bridges ('B') keep the old painted planks (darkened to the new value range);
  5. walkable frozen lake (blue pixels of the old ground on walkable tiles): #638CA5 with frosted patches #8FAABB, thin cracks #486F88,
     a slate rim #425B73 and a lavender shore band.
Sources: Assets/ArtLibrary/maps/<map>/snow-25d/*-ground-atlas-chatgpt-N.png (highest N wins); the old ground is saved once to
<map>-before-ground.jpg there before the chunks are overwritten. To convert another snow map: add an entry to MAPS.
"""
import os, re, sys, glob, importlib.util
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "Assets", "Resources", "Sprites")
_spec = importlib.util.spec_from_file_location("g25", os.path.join(ROOT, "Tools", "build-map-25d-ground.py"))
g25 = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(g25)   # seamless / grade / value_noise / periodic_noise (DRY)
T = 64
MAPS = {
    "frost-hollow": dict(size=(72, 48), chunks=(2, 2), pattern="tiles/frost-hollow-large-ground-r{r}c{c}.png",
                         rows_file="Assets/Scripts/Maps/FrostHollowLayout.cs", row_chars="CTwBs", solid="CTw", water="w", bridge="B",
                         lanes="plans/261105-1240-frost-hollow-large-map/layout/frost-hollow-72x48-lanes.txt"),
}
SNOW, SNOW_LIGHT, SNOW_SHADE, LAVENDER = (0x89, 0x99, 0xAF), (0xB2, 0xBE, 0xD0), (0x6F, 0x7F, 0x98), (0x92, 0x99, 0xB0)
PATH, ROCK, ROCK_DEEP = (0x84, 0x8D, 0x9B), (0x6E, 0x7D, 0x93), (0x56, 0x65, 0x7B)
RIVER, RIVER_DEEP, RIM = (0x41, 0x7F, 0x9D), (0x31, 0x5B, 0x7A), (0x42, 0x5B, 0x73)
LAKE, LAKE_FROST, CRACK, SHORE = (0x63, 0x8C, 0xA5), (0x8F, 0xAA, 0xBB), (0x52, 0x7A, 0x91), (0x8C, 0x8D, 0xAA)
CONTACT = (0x52, 0x64, 0x7A)   # ChatGPT review fix 1: dark ground-contact strip at the cliff foot, ties the standing pieces together
TEX_TILE = 768        # px one swatch covers on the map (12 tiles)
FEATHER = 22          # px: snow / path transition (~0.35 m)


def lib(key): return os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "snow-25d")


def latest(key):
    files = glob.glob(os.path.join(lib(key), "*-ground-atlas-chatgpt-*.png"))
    if not files: raise SystemExit("no ground atlas in " + lib(key))
    return max(files, key=lambda p: int(re.findall(r"-(\d+)\.png$", p)[0]))


def seamless(tex):
    """Tileable swatch: the swatch and its copies rolled by half in x, y and both, each weighted so a copy's own seam (at its middle)
    always has weight 0 (the shared g25.seamless takes min(x, y) and leaves half-tile seam lines near the tile edges)."""
    h, w = tex.shape[:2]
    wx = np.clip(np.minimum(np.arange(w), w - 1 - np.arange(w)) / (w * 0.5) / 0.4, 0, 1)[None, :, None]
    wy = np.clip(np.minimum(np.arange(h), h - 1 - np.arange(h)) / (h * 0.5) / 0.4, 0, 1)[:, None, None]
    rx, ry = np.roll(tex, w // 2, 1), np.roll(tex, h // 2, 0)
    rxy = np.roll(rx, h // 2, 0)
    return tex * wx * wy + rx * (1 - wx) * wy + ry * wx * (1 - wy) + rxy * (1 - wx) * (1 - wy)


def quadrant(key, qi, target, keep_std=0.9):
    """Square middle of atlas quadrant qi (0 TL, 1 TR, 2 BL, 3 BR), made tileable and graded to `target`, TEX_TILE px."""
    a = np.asarray(Image.open(latest(key)).convert("RGB")).astype(np.float32)
    h, w = a.shape[:2]; qh, qw = h // 2, w // 2
    y0, x0 = (qi // 2) * qh, (qi % 2) * qw
    s = min(qh, qw) - 40
    q = a[y0 + (qh - s) // 2:y0 + (qh + s) // 2, x0 + (qw - s) // 2:x0 + (qw + s) // 2]
    q = g25.grade(seamless(q), target, keep_std)
    return cv2.resize(q, (TEX_TILE, TEX_TILE), interpolation=cv2.INTER_AREA)


def tile(tex, W, H):
    return np.tile(tex, (H // tex.shape[0] + 1, W // tex.shape[1] + 1, 1))[:H, :W]


def chunk_paths(cfg):
    rows, cols = cfg["chunks"]
    return [[os.path.join(RES, cfg["pattern"].format(r=r, c=c)) for c in range(cols)] for r in range(rows)]


def read_ground(cfg):
    w, h = cfg["size"]; out = Image.new("RGB", (w * T, h * T)); y = 0
    for row in chunk_paths(cfg):
        x = 0
        for p in row:
            im = Image.open(p).convert("RGB"); out.paste(im, (x, y)); x += im.width
        y += im.height
    return out


def rows_of(cfg):
    w, h = cfg["size"]
    rows = re.findall(r'"([%s]{%d})"' % (cfg["row_chars"], w), open(os.path.join(ROOT, cfg["rows_file"]), encoding="utf8").read())
    assert len(rows) == h, len(rows)
    return rows


def up(mask_t, W, H, blur):
    m = cv2.resize(mask_t.astype(np.float32), (W, H), interpolation=cv2.INTER_NEAREST)
    return cv2.GaussianBlur(m, (0, 0), blur) if blur else m


def snow_field(key, W, H, seed=3):
    """Main snow with broad light / shadow patches (65 / 15 / 20 %)."""
    base = tile(quadrant(key, 0, SNOW), W, H)
    light = tile(quadrant(key, 1, SNOW_LIGHT, 0.75), W, H)
    shade = tile(quadrant(key, 2, SNOW_SHADE), W, H)
    n1 = g25.value_noise(W, H, 3 * T, seed) * 0.65 + g25.value_noise(W, H, int(1.3 * T), seed + 1) * 0.35
    n2 = g25.value_noise(W, H, 4 * T, seed + 2) * 0.6 + g25.value_noise(W, H, int(1.6 * T), seed + 3) * 0.4
    kl = np.clip((n1 - 0.58) / 0.20, 0, 1)[..., None] * 0.6      # ~15 % light patches, soft wide ramp, never the full white swatch
    ks = np.clip((n2 - 0.56) / 0.20, 0, 1)[..., None] * 0.75 * (1 - kl)  # ~20 % shadow / lavender snow
    out = base * (1 - kl - ks) + light * kl + shade * ks
    # ChatGPT review 21/11 (Frost Hollow 8.2/10, fix 3): broad muted lavender snow #9299B0 over ~15 % of the open ground, soft 1-3 tile edges
    kv = np.clip((g25.value_noise(W, H, int(2.5 * T), seed + 5) - 0.6) / 0.18, 0, 1)[..., None] * 0.55 * (1 - kl)
    out = out * (1 - kv) + (base / np.array(SNOW, np.float32)) * np.array(LAVENDER, np.float32) * kv
    return out * (1 + (g25.value_noise(W, H, 6 * T, seed + 9) - 0.5) * 0.08)[..., None]   # +-4 % over ~6 tiles hides the repeat


def border(key, cfg):
    """Seamless 1024 px snow tile, a shade cooler / darker than the map's snow (View25DGroundApron)."""
    n = 1024
    tex = cv2.resize(quadrant(key, 0, SNOW), (n, n), interpolation=cv2.INTER_AREA)
    shade = cv2.resize(quadrant(key, 2, SNOW_SHADE), (n, n), interpolation=cv2.INTER_AREA)
    k = np.clip((g25.periodic_noise(n, 5, 7) - 0.5) * 3, 0, 1)[..., None] * 0.6
    tex = tex * (1 - k) + shade * k
    out = os.path.join(RES, cfg["pattern"].replace("-r{r}c{c}", "-border"))
    Image.fromarray(np.clip(tex * 0.97, 0, 255).astype(np.uint8)).save(out, optimize=True); print(out)


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "frost-hollow"
    cfg = MAPS[key]
    if "border" in sys.argv[2:]: border(key, cfg); return
    tw, th = cfg["size"]; W, H = tw * T, th * T
    os.makedirs(lib(key), exist_ok=True)
    before = os.path.join(lib(key), key + "-before-ground.jpg")
    if not os.path.exists(before):
        read_ground(cfg).save(before, quality=95); print("saved the old ground:", before)
    old = np.asarray(Image.open(before).convert("RGB")).astype(np.float32)
    rows = rows_of(cfg)
    solid_t = np.array([[c in cfg["solid"] for c in r] for r in rows], np.uint8)
    water_t = np.array([[c in cfg["water"] for c in r] for r in rows], np.uint8)
    bridge_t = np.array([[c in cfg["bridge"] for c in r] for r in rows], np.uint8)
    lanes = [l.rstrip("\n") for l in open(os.path.join(ROOT, cfg["lanes"]), encoding="utf8")][:th]
    lane_t = np.array([[c == "r" for c in l.ljust(tw, ".")[:tw]] for l in lanes], np.uint8) & (1 - solid_t)

    out = snow_field(key, W, H)

    # --- packed path on the lanes
    pf = up(lane_t, W, H, 26)
    pf += (g25.value_noise(W, H, 28, 7) - 0.5) * 0.45 + (g25.value_noise(W, H, 9, 8) - 0.5) * 0.25
    hard = (pf > 0.5).astype(np.float32)
    pm = np.clip(cv2.GaussianBlur(hard, (0, 0), FEATHER / 2.6) * 1.15 - 0.075, 0, 1)
    path = tile(quadrant(key, 3, PATH, 0.8), W, H)
    path *= (1 + (g25.value_noise(W, H, int(1.5 * T), 42) - 0.5) * 0.08)[..., None]
    out = out * (1 - pm[..., None]) + path * pm[..., None]

    # --- walkable frozen lake: blue pixels of the old ground on walkable, non-river tiles
    q = cv2.resize(old, (W // 4, H // 4), interpolation=cv2.INTER_AREA)
    R, G, B = q[..., 0], q[..., 1], q[..., 2]
    blue = ((B > R + 38) & (G > R + 10)).astype(np.uint8)
    walk_q = cv2.resize((1 - solid_t - bridge_t).clip(0, 1).astype(np.uint8), (W // 4, H // 4), interpolation=cv2.INTER_NEAREST)
    river_q = cv2.dilate(cv2.resize(water_t | bridge_t, (W // 4, H // 4), interpolation=cv2.INTER_NEAREST), np.ones((25, 25), np.uint8))
    lake = blue & walk_q & (1 - river_q)
    lake = cv2.morphologyEx(lake, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(lake)
    lake = np.isin(lab, [i for i in range(1, n) if st[i, 4] >= 1500]).astype(np.uint8)   # only the big lake (>= ~6 tiles)
    lake = cv2.morphologyEx(lake, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
    lf = cv2.GaussianBlur(cv2.resize(lake.astype(np.float32), (W, H), interpolation=cv2.INTER_LINEAR), (0, 0), 6)
    lf += (g25.value_noise(W, H, 20, 51) - 0.5) * 0.3
    lhard = (lf > 0.5).astype(np.uint8)
    lm = np.clip(cv2.GaussianBlur(lhard.astype(np.float32), (0, 0), 2.0), 0, 1)
    din = cv2.distanceTransform(lhard, cv2.DIST_L2, 5)
    dout = cv2.distanceTransform(1 - lhard, cv2.DIST_L2, 5)
    grain = tile(quadrant(key, 1, (128, 128, 128), 0.35), W, H) / 128.0                   # the light-snow swatch as a frost texture
    ice = np.ones((H, W, 3), np.float32) * np.array(LAKE, np.float32) * grain
    frost = np.clip((g25.value_noise(W, H, int(1.8 * T), 52) - 0.58) / 0.12, 0, 1)[..., None] * 0.8
    ice = ice * (1 - frost) + np.array(LAKE_FROST, np.float32) * grain * frost
    ridge = np.abs(g25.value_noise(W, H, int(1.2 * T), 53) - 0.5)                          # thin cracks: zero crossings of a noise field
    ridge2 = np.abs(g25.value_noise(W, H, int(0.6 * T), 55) - 0.5)
    crack = np.maximum(np.clip(1 - ridge / 0.012, 0, 1) * (g25.value_noise(W, H, 2 * T, 54) > 0.4), np.clip(1 - ridge2 / 0.01, 0, 1) * (g25.value_noise(W, H, 2 * T, 56) > 0.55))
    ice = ice * (1 - crack[..., None] * 0.6) + np.array(CRACK, np.float32) * crack[..., None] * 0.6
    rim = np.clip(1 - din / 10.0, 0, 1)[..., None]
    ice = ice * (1 - rim) + np.array(RIM, np.float32) * rim
    shore = (np.clip(1 - dout / 16.0, 0, 1) * (1 - lm))[..., None] * 0.55
    out = out * (1 - shore) + np.array(SHORE, np.float32) * shore
    out = out * (1 - lm[..., None]) + ice * lm[..., None]

    # --- solid mass (cliff / plateau tiles): rocky snow, deeper inside the mass
    sf = up(solid_t & (1 - water_t), W, H, 18)
    sf += (g25.value_noise(W, H, 30, 61) - 0.5) * 0.4 + (g25.value_noise(W, H, 10, 62) - 0.5) * 0.2
    shard = (sf > 0.5).astype(np.uint8)
    sm = np.clip(cv2.GaussianBlur(shard.astype(np.float32), (0, 0), 3.0), 0, 1)
    dsol = cv2.distanceTransform(shard, cv2.DIST_L2, 5)
    rock = tile(quadrant(key, 2, ROCK, 1.0), W, H)
    deep = np.clip((dsol - 1.2 * T) / (2.0 * T), 0, 1)[..., None] * 0.8
    rock = rock * (1 - deep) + np.array(ROCK_DEEP, np.float32) * (rock / np.array(ROCK, np.float32)) * deep
    floor_d = cv2.distanceTransform(1 - shard, cv2.DIST_L2, 5)
    ao = (1 - np.clip(floor_d / 26.0, 0, 1)) * 0.22 * (1 - lm)                             # floor side: darker at the wall foot
    out *= (1 - ao)[..., None]
    strip = ((1 - np.clip((floor_d - 4) / 12.0, 0, 1)) * (floor_d > 0) * 0.45 * (1 - lm))[..., None]   # ~0.2 tile contact band #52647A
    out = out * (1 - strip) + np.array(CONTACT, np.float32) * strip
    out = out * (1 - sm[..., None]) + rock * sm[..., None]

    # --- frozen river + bridges
    wf = up(water_t, W, H, 8)
    wf += (g25.value_noise(W, H, 24, 71) - 0.5) * 0.35
    whard = (wf > 0.5).astype(np.uint8)
    wm = np.clip(cv2.GaussianBlur(whard.astype(np.float32), (0, 0), 2.0), 0, 1)
    wdin = cv2.distanceTransform(whard, cv2.DIST_L2, 5)
    depth = np.clip((wdin - 12) / 60.0, 0, 1)[..., None]
    river = np.array(RIVER, np.float32) * (1 - depth) + np.array(RIVER_DEEP, np.float32) * depth
    streak = cv2.resize(np.random.RandomState(72).rand(H // 6, W // 48).astype(np.float32), (W, H), interpolation=cv2.INTER_CUBIC)  # long along y (the river runs north-south)
    crust = np.clip((streak - 0.62) / 0.2, 0, 1)[..., None] * 0.35
    river = river * (1 - crust) + np.array(LAKE_FROST, np.float32) * 0.85 * crust
    river *= grain * 0.6 + 0.4
    wrim = np.clip(1 - wdin / 9.0, 0, 1)[..., None]
    river = river * (1 - wrim) + np.array(RIM, np.float32) * wrim
    out = out * (1 - wm[..., None]) + river * wm[..., None]
    bm = np.clip(cv2.GaussianBlur(up(bridge_t, W, H, 0), (0, 0), 3.0) * 1.3, 0, 1)[..., None]
    out = out * (1 - bm) + old * 0.78 * bm                                                    # old painted planks, toned into the new value range

    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    rn, cn = cfg["chunks"]; cw, ch = W // cn, H // rn
    for r, row in enumerate(chunk_paths(cfg)):
        for c, p in enumerate(row):
            img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)).save(p, optimize=True); print(p)
    if "--preview" in sys.argv:
        img.resize((1536, 1024), Image.LANCZOS).save(os.path.join(lib(key), key + "-ground-preview.jpg"), quality=88)
    y = (np.asarray(img).astype(np.float32) / 255) ** 2.2 @ np.array([0.2126, 0.7152, 0.0722])
    walk = cv2.resize((1 - solid_t).astype(np.uint8), (W, H), interpolation=cv2.INTER_NEAREST) > 0
    print("walkable ground Y: median %.3f, p10 %.3f, p90 %.3f, share > 0.55: %.1f %%" % (np.median(y[walk]), np.percentile(y[walk], 10), np.percentile(y[walk], 90), 100 * (y[walk] > 0.55).mean()))


if __name__ == "__main__":
    main()

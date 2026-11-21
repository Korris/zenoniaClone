"""Simple Midgard-style ground for a 2.5D map, built from ChatGPT texture swatches + masks (first: Willowbrook Meadows, 21/11/2026).

    python Tools/build-map-25d-ground.py willowbrook-meadows [--preview]

Why not a full repaint: the roads, stream, pond and paving must stay on the exact tiles the rows / props / spawns use, and the look wanted is flat
(docs/reference/willowbrook-25d/willowbrook-forest-art-direction-chatgpt.md): one calm grass, darker forest floor under the tree belts, flat ochre
dirt with chunky stepped edges, almost no flowers. So the ground is composed in code:
  1. masks from the map's previous ground (<map>-before-ground.jpg, saved the first time, before the chunks are overwritten): dirt = ochre pixels (roads, trails, dirt patches),
     widened a little and given a stepped jagged edge; keep = water + its banks (rows 'w' / blue pixels) and the KEEP rects (paving, crop field),
     copied from the old ground with a feathered edge;
  2. grass swatch (seamless, graded to the target palette) everywhere, low-frequency tone so the repeat does not show;
  3. forest floor: grass darkened toward FOREST / DEEP by the tree-belt mask of Tools/place-map-25d-forest.py (blurred over ~1.5 tiles);
  4. dirt swatch inside the dirt mask, a darker rim just inside its edge.
RO3 pass (21/11/2026, docs/reference/willowbrook-25d/willowbrook-25d-vs-ro3-feel-chatgpt.md sections 3-4): olive grass #83A65C (light #ACC47B,
dark #607D4D), warm road #D3B17B with a #AF9067 rim and uneven darker ruts; micro detail (fine noise + short slanted blade streaks + dirt grain)
at ~11 % opacity, macro patches 1.5-3 m at +-6 % value; the road edge is feathered over ~0.35 m (22 px) with grass nibbling 0.1-0.25 m into it,
no stepped pixel edge. Only the grass / road transition is soft: the textures themselves stay sharp. 1 m = 1 tile = 64 px of ground art.
Writes the ground chunks of the map (same files the game loads) and, with --preview, a small preview in the forest-25d folder. The minimap is
rebaked after this by the map's composite preview tool (--minimap), which also draws the props.
Border ground (camera loop 21/11/2026, docs/reference/willowbrook-25d/camera-loop/round-1-chatgpt.md item 3): `... <map> border` writes
<pattern>-border.png, a seamless 1024 px (16 tiles) forest-floor grass tile graded to the mean of the map's outer 2 tiles of grass, that
View25DGroundApron tiles past every map edge (80-90 % grass, a few darker / lighter patches, no road, paving or water).
Sources: Assets/ArtLibrary/maps/<map>/forest-25d/ (*-grass-chatgpt-N.png, *-dirt-chatgpt-N.png, highest N wins; *-forest-mask.png).
To convert another map: add an entry to MAPS.
"""
import os, re, sys, glob
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "Assets", "Resources", "Sprites")
T = 64
MAPS = {
    "willowbrook-meadows": dict(size=(72, 48), chunks=(2, 2), pattern="village/tiles/willowbrook-large-ground-r{r}c{c}.png",
                                rows_file="Assets/Scripts/Maps/WillowbrookMeadowsMap.cs",
                                keep=[(43, 0, 27, 11), (0, 38, 12, 8)]),  # tile rects (x, y, w, h): ruin paving NE, crop field SW
}
# Target palette (RO3 comparison, section 3: olive grass, warm road; the forest floor keeps its darker belt)
GRASS, GRASS_LIGHT, FOREST, DEEP = (0x83, 0xA6, 0x5C), (0xAC, 0xC4, 0x7B), (0x60, 0x7D, 0x4D), (0x50, 0x6C, 0x45)
DIRT, DIRT_EDGE = (0xD3, 0xB1, 0x7B), (0xAF, 0x90, 0x67)
ROAD_WIDEN = 14      # px at full size added on each side of the old painted trails (~2 tile wide roads)
FEATHER = 22         # px: grass / road transition width (~0.35 m), replaces the old 8 px stepped edge
DETAIL = 0.10        # opacity of the micro detail layer (spec 10-12 %)
MACRO = 0.06         # +- value of the 1.5-3 m grass patches (spec 5-8 %)
TEX_TILE = 1024      # px a swatch covers on the map (16 tiles)
MIN_PATCH = 700      # quarter-size px (~2.7 tiles): smaller old dirt spots become grass (Midgard keeps only a few bold patches)


def lib(key): return os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "forest-25d")


def latest(key, kind):
    files = glob.glob(os.path.join(lib(key), "*-%s-chatgpt-*.png" % kind))
    if not files: raise SystemExit("no %s swatch in %s" % (kind, lib(key)))
    return max(files, key=lambda p: int(re.findall(r"-(\d+)\.png$", p)[0]))


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


def seamless(tex):
    """Tileable swatch: the swatch cross-faded with itself shifted by half, the shifted copy taking over toward the edges."""
    h, w = tex.shape[:2]
    sh = np.roll(np.roll(tex, h // 2, 0), w // 2, 1)
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.minimum(np.minimum(xx, w - 1 - xx) / (w * 0.5), np.minimum(yy, h - 1 - yy) / (h * 0.5))
    wgt = np.clip(d / 0.35, 0, 1)[..., None]
    return tex * wgt + sh * (1 - wgt)


def grade(tex, target, keep_std=0.9):
    m = tex.reshape(-1, 3).mean(0)
    return np.clip((tex - m) * keep_std + np.array(target, float), 0, 255)


def tile(tex, W, H):
    tex = cv2.resize(tex.astype(np.float32), (TEX_TILE, TEX_TILE), interpolation=cv2.INTER_AREA)
    reps = (H // TEX_TILE + 1, W // TEX_TILE + 1)
    return np.tile(tex, (reps[0], reps[1], 1))[:H, :W]


def value_noise(W, H, cell, seed):
    rng = np.random.RandomState(seed)
    g = rng.rand(H // cell + 2, W // cell + 2).astype(np.float32)
    return cv2.resize(g, (W + 2 * cell, H + 2 * cell), interpolation=cv2.INTER_CUBIC)[cell:cell + H, cell:cell + W]


def micro_detail(W, H, seed):
    """Multiplier ~1 +- DETAIL: fine noise (0.08-0.2 m) + short slanted blade streaks (0.12-0.3 m) so the grass is not one flat colour."""
    rng = np.random.RandomState(seed)
    # crisp grain, not mottling: 6-12 px cells read as smudges once the ground is foreshortened and downscaled (first in-game look, 21/11)
    fine = (value_noise(W, H, 3, seed) - 0.5) * 0.7 + (value_noise(W, H, 7, seed + 1) - 0.5) * 0.3
    blades = np.zeros((H, W), np.float32)
    for lean, sign in ((3, 1.0), (-2, -0.7), (1, 0.8)):  # three blade leans, light and dark, sparse: tufts, not rain
        dots = (rng.rand(H // 2, W // 2).astype(np.float32) > 0.9975).astype(np.float32)
        dots = cv2.resize(dots, (W, H), interpolation=cv2.INTER_NEAREST)
        k = np.zeros((13, 13), np.float32); cv2.line(k, (6 - lean, 12), (6 + lean, 3), 1.0, 1); k /= k.sum()
        blades += cv2.filter2D(dots, -1, k) * 4.0 * sign
    d = np.clip(fine * 1.3 + blades * 0.6, -1, 1)
    return 1 + d * DETAIL


def periodic_noise(n, cells, seed):
    """Tileable value noise n x n: a cells x cells random grid wrapped and cubic-upsampled (no seam when tiled)."""
    g = np.random.RandomState(seed).rand(cells, cells).astype(np.float32)
    g = np.tile(g, (3, 3))
    big = cv2.resize(g, (3 * n, 3 * n), interpolation=cv2.INTER_CUBIC)
    return big[n:2 * n, n:2 * n]


def border(key, cfg):
    """Seamless forest-floor grass tile for the ground past the map edges, matched to the colour of the map's own outer grass."""
    ground = np.asarray(read_ground(cfg)).astype(np.float32)
    H, W = ground.shape[:2]; b = 2 * T
    ring = np.concatenate([ground[:b].reshape(-1, 3), ground[-b:].reshape(-1, 3), ground[:, :b].reshape(-1, 3), ground[:, -b:].reshape(-1, 3)])
    grassy = ring[(ring[:, 1] > ring[:, 2] + 25) & (ring[:, 1] >= ring[:, 0])]  # grass only: no road, water, paving
    mean = np.median(grassy, 0); print("outer grass mean", mean.round())
    n = TEX_TILE
    sw = seamless(np.asarray(Image.open(latest(key, "grass")).convert("RGB")).astype(np.float32))
    tex = cv2.resize(grade(sw, mean), (n, n), interpolation=cv2.INTER_AREA)
    # patches 1.5-4 m (+-6 %), toward GRASS_LIGHT / DEEP hues, and a few darker forest-floor pools (10-20 % of the area)
    patch = (periodic_noise(n, 8, 5) - 0.5) * 2 * 0.6 + (periodic_noise(n, 5, 6) - 0.5) * 2 * 0.4
    k_light = np.array(GRASS_LIGHT, float) / np.array(GRASS, float) - 1
    k_dark = np.array(FOREST, float) / np.array(GRASS, float) - 1
    pw = np.clip(patch, -1, 1)[..., None]
    tex *= 1 + np.where(pw > 0, k_light * pw * (MACRO / 0.25), -k_dark * pw * (MACRO / 0.25))
    pool = np.clip((periodic_noise(n, 4, 9) - 0.62) * 4, 0, 1)[..., None]
    tex *= 1 + (np.array(DEEP, float) / np.array(GRASS, float) - 1) * 0.55 * pool
    fine = (periodic_noise(n, n // 3, 31) - 0.5) * 0.7 + (periodic_noise(n, n // 7, 32) - 0.5) * 0.3
    tex *= (1 + np.clip(fine * 1.3, -1, 1) * DETAIL)[..., None]
    tex *= (mean / tex.reshape(-1, 3).mean(0))  # keep the overall colour on the map's outer grass: the seam is a soft fade, not a step
    out = os.path.join(RES, cfg["pattern"].replace("-r{r}c{c}", "-border"))
    Image.fromarray(np.clip(tex, 0, 255).astype(np.uint8)).save(out, optimize=True); print(out)


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "willowbrook-meadows"
    cfg = MAPS[key]
    if "border" in sys.argv[2:]:
        border(key, cfg); return; tw, th = cfg["size"]; W, H = tw * T, th * T
    os.makedirs(lib(key), exist_ok=True)
    before = os.path.join(lib(key), key + "-before-ground.jpg")  # q95: masks + kept water / paving pixels
    if not os.path.exists(before):
        read_ground(cfg).save(before, quality=95); print("saved the old ground:", before)
    old = np.asarray(Image.open(before).convert("RGB")).astype(np.float32)
    assert old.shape[:2] == (H, W), old.shape

    # --- masks from the old ground (quarter size = 16 px per tile)
    q = cv2.resize(old, (W // 4, H // 4), interpolation=cv2.INTER_AREA).astype(np.int32)
    R, G, B = q[..., 0], q[..., 1], q[..., 2]
    dirt = ((R > G + 12) & (B > 85) & (R > 150)).astype(np.uint8)
    dirt = cv2.morphologyEx(dirt, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(dirt)
    dirt = np.isin(lab, [i for i in range(1, n) if st[i, 4] >= MIN_PATCH]).astype(np.uint8)
    rows = re.findall(r'"([TgwBd]{%d})"' % tw, open(os.path.join(ROOT, cfg["rows_file"]), encoding="utf8").read())
    wt = np.array([[c in "wB" for c in r] for r in rows], np.uint8)
    water_px = ((B > R + 30) & (B > G - 10)).astype(np.uint8)
    keep = cv2.resize(wt, (W // 4, H // 4), interpolation=cv2.INTER_NEAREST) | water_px
    keep = cv2.dilate(cv2.morphologyEx(keep, cv2.MORPH_CLOSE, np.ones((7, 7), np.uint8)), np.ones((5, 5), np.uint8))
    grassy = (G > B + 40).astype(np.uint8)  # old grass (paving stones are grey, the crop field brown): becomes new grass
    for x, y, w, h in cfg["keep"]:
        r = np.zeros_like(keep); r[y * 16:(y + h) * 16, x * 16:(x + w) * 16] = 1
        k = cv2.morphologyEx(r & (1 - grassy), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
        keep |= cv2.morphologyEx(k, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    dirt[keep > 0] = 0

    # --- dirt: widen, jagged stepped edge, full size
    dq = cv2.dilate(dirt, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * (ROAD_WIDEN // 4) + 1,) * 2))
    df = cv2.GaussianBlur(cv2.resize(dq.astype(np.float32), (W, H), interpolation=cv2.INTER_LINEAR), (0, 0), 7)
    df += (value_noise(W, H, 28, 7) - 0.5) * 0.55
    # grass nibbling into the road: fine noise (0.1-0.25 m) only matters near the 0.5 edge, then a smooth ramp ~FEATHER px wide
    df += (value_noise(W, H, 10, 11) - 0.5) * 0.30 + (value_noise(W, H, 4, 12) - 0.5) * 0.12
    hard = (df > 0.5).astype(np.float32)
    dm = np.clip(cv2.GaussianBlur(hard, (0, 0), FEATHER / 2.6) * 1.15 - 0.075, 0, 1)
    km = cv2.GaussianBlur(cv2.resize(keep.astype(np.float32), (W, H), interpolation=cv2.INTER_LINEAR), (0, 0), 6)
    km = np.clip((km - 0.15) / 0.7, 0, 1)
    dm *= np.clip((0.5 - km) * 4, 0, 1)
    # darker rim just inside the edge (soft), and uneven ruts along the road
    inner = cv2.GaussianBlur(cv2.erode(hard, np.ones((15, 15), np.uint8)), (0, 0), 6)
    rim = np.clip(dm - inner, 0, 1) * 0.8 + np.clip(value_noise(W, H, 40, 21) - 0.55, 0, 1) * 1.1 * inner

    # --- grass + forest floor
    grass = tile(grade(seamless(np.asarray(Image.open(latest(key, "grass")).convert("RGB")).astype(np.float32)), GRASS), W, H)
    tone = (value_noise(W, H, 6 * T, 3) - 0.5) * 0.10 + 1.0  # +-5 % brightness over ~6 tiles: hides the swatch repeat
    grass *= tone[..., None]
    # macro patches 1.5-3 m: light grass / dark grass, +-MACRO value with a matching hue shift
    patch = (value_noise(W, H, 2 * T, 5) - 0.5) * 2 * 0.6 + (value_noise(W, H, 3 * T, 6) - 0.5) * 2 * 0.4
    k_light = np.array(GRASS_LIGHT, float) / np.array(GRASS, float) - 1
    k_dark = np.array(FOREST, float) / np.array(GRASS, float) - 1
    pw = np.clip(patch, -1, 1)[..., None]
    grass *= 1 + np.where(pw > 0, k_light * pw * (MACRO / 0.25), -k_dark * pw * (MACRO / 0.25))
    grass *= micro_detail(W, H, 31)[..., None]
    fm = np.asarray(Image.open(os.path.join(lib(key), key + "-forest-mask.png")).convert("L")).astype(np.float32) / 255
    fm = cv2.GaussianBlur(cv2.resize(cv2.GaussianBlur(fm, (0, 0), 1.0), (W, H), interpolation=cv2.INTER_CUBIC), (0, 0), 0.6 * T)
    k_forest = np.array(FOREST, float) / np.array(GRASS, float)
    k_deep = np.array(DEEP, float) / np.array(GRASS, float)
    mul = 1 + (k_forest - 1) * np.clip(fm * 1.6, 0, 1)[..., None] + (k_deep - k_forest) * np.clip(fm * 1.6 - 1, 0, 0.6)[..., None]
    out = grass * mul

    # --- dirt
    dirt_tex = tile(grade(seamless(np.asarray(Image.open(latest(key, "dirt")).convert("RGB")).astype(np.float32)), DIRT, 0.8), W, H)
    dirt_tex = dirt_tex * (1 - np.clip(rim, 0, 1)[..., None] * (1 - np.array(DIRT_EDGE, float) / np.array(DIRT, float)))
    dirt_tex *= (1 + (value_noise(W, H, 3, 41) - 0.5) * 2 * 0.08)[..., None]  # dirt grain 6-10 %
    dirt_tex *= (1 + (value_noise(W, H, int(1.6 * T), 42) - 0.5) * 2 * 0.04)[..., None]  # broad worn patches
    out = out * (1 - dm[..., None]) + dirt_tex * dm[..., None]

    # --- kept old water / banks / paving
    out = out * (1 - km[..., None]) + old * km[..., None]
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))

    rows_n, cols_n = cfg["chunks"]; cw, ch = W // cols_n, H // rows_n
    for r, row in enumerate(chunk_paths(cfg)):
        for c, p in enumerate(row):
            img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)).save(p, optimize=True); print(p)
    if "--preview" in sys.argv:
        img.resize((1536, 1024), Image.LANCZOS).save(os.path.join(lib(key), key + "-ground-preview.jpg"), quality=88)


if __name__ == "__main__":
    main()

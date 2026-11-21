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
Writes the ground chunks of the map (same files the game loads) and, with --preview, a small preview in the forest-25d folder. The minimap is
rebaked after this by the map's composite preview tool (--minimap), which also draws the props.
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
# Target palette (ChatGPT spec section 2)
GRASS, FOREST, DEEP = (0x76, 0xB4, 0x5B), (0x52, 0x7D, 0x43), (0x42, 0x6F, 0x40)
DIRT, DIRT_EDGE = (0xD2, 0xA7, 0x65), (0xB5, 0x8F, 0x58)
ROAD_WIDEN = 14      # px at full size added on each side of the old painted trails (~2 tile wide roads)
STEP = 8             # px: edge steps of the dirt (chunky Midgard edge)
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


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "willowbrook-meadows"
    cfg = MAPS[key]; tw, th = cfg["size"]; W, H = tw * T, th * T
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
    dm = (df > 0.5).astype(np.uint8)
    dm = cv2.resize(cv2.resize(dm, (W // STEP, H // STEP), interpolation=cv2.INTER_AREA), (W, H), interpolation=cv2.INTER_NEAREST).astype(np.float32)
    km = cv2.GaussianBlur(cv2.resize(keep.astype(np.float32), (W, H), interpolation=cv2.INTER_LINEAR), (0, 0), 6)
    km = np.clip((km - 0.15) / 0.7, 0, 1)
    dm *= (km < 0.5)
    rim = dm - cv2.erode(dm, np.ones((7, 7), np.uint8))

    # --- grass + forest floor
    grass = tile(grade(seamless(np.asarray(Image.open(latest(key, "grass")).convert("RGB")).astype(np.float32)), GRASS), W, H)
    tone = (value_noise(W, H, 6 * T, 3) - 0.5) * 0.10 + 1.0  # +-5 % brightness over ~6 tiles: hides the swatch repeat
    grass *= tone[..., None]
    fm = np.asarray(Image.open(os.path.join(lib(key), key + "-forest-mask.png")).convert("L")).astype(np.float32) / 255
    fm = cv2.GaussianBlur(cv2.resize(cv2.GaussianBlur(fm, (0, 0), 1.0), (W, H), interpolation=cv2.INTER_CUBIC), (0, 0), 0.6 * T)
    k_forest = np.array(FOREST, float) / np.array(GRASS, float)
    k_deep = np.array(DEEP, float) / np.array(GRASS, float)
    mul = 1 + (k_forest - 1) * np.clip(fm * 1.6, 0, 1)[..., None] + (k_deep - k_forest) * np.clip(fm * 1.6 - 1, 0, 0.6)[..., None]
    out = grass * mul

    # --- dirt
    dirt_tex = tile(grade(seamless(np.asarray(Image.open(latest(key, "dirt")).convert("RGB")).astype(np.float32)), DIRT, 0.8), W, H)
    dirt_tex = dirt_tex * (1 - rim[..., None] * (1 - np.array(DIRT_EDGE, float) / np.array(DIRT, float)))
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

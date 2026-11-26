"""Taurus V4 phase 04: 2.5D ground of Willowbrook Village (V2 100x100) and Clover Meadows (96x64) from the map ROWS + ChatGPT material swatches.

    python Tools/build-tau-25d-ground.py village|meadows [--preview]      # ground chunks (+ preview)
    python Tools/build-tau-25d-ground.py village|meadows border           # <ground>-border.png (forest apron past the map edges)

Why rows and not a mask from an old painting (Tools/build-map-25d-ground.py): the Taurus maps were greyboxed, so the rows ARE the layout. Every tile
char picks a material (MATERIALS below, swatches drawn by ChatGPT in the Taurus production chat, Assets/ArtLibrary/maps/taurus/ground/<material>.png,
seamless square, top-down, no standing objects, no cast shadows). Per material: the swatch is made tileable (4-way roll blend), graded to its target colour,
tiled at TEX_TILE px per repeat; the material masks are feathered (~0.35 m) with a little noise so road / plaza / grass edges are soft and organic,
not tile steps. Under solid tiles (buildings, trees) the material of the tile beneath the prop is used (UNDER). Macro patches + low-frequency tone hide the
repeat (docs/map-design-rules.md "Map 2.5D" 13). 1 tile = 64 px of ground art; written as chunks <GroundTexture>-r{r}c{c}.png (each <= 2048 px).
The minimap (with props) is baked by Tools/preview-tau-25d-composite.py --minimap after this.
"""
import os, re, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
LIB = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "taurus", "ground")
T = 64
TEX_TILE = 192        # default px of map one swatch repeat covers (3 tiles)
REPEAT = {"plaza": 6.0, "water": 4.0, "ancient": 4.0, "dirt": 3.5, "bank": 2.5}   # tiles per swatch repeat (paving stones ~0.4-0.6 m, roots ~1 m)
FEATHER = 22          # px: material transition width (~0.35 m)

# plaza.png = the cobbles of the V2 lake concept since 25/11/2026 (wv2-paving-tile-chatgpt-1.png, ~9 stones per repeat -> 6 tiles).
# char -> material swatch key; target colours = BIBLE part1 III palette / the design chat's ground list. Solid chars draw what lies under their prop.
MAPS = {
    # V2 (25/11/2026) 100x100 (first 160x160 at 40 px / tile), 5x5 chunks of 1280; x = meadow outside the wall
    "village": dict(cls="TauWillowbrookVillageMap.cs", size=(100, 100), chunks=(5, 5), px=64, ground="tau-willowbrook-village-ground",
                    chars={"g": "grass", "d": "dirt", "p": "plaza", "w": "water", "B": "water", "T": "grass", "t": "grass", "x": "grass"}, post="terrace"),
    "meadows": dict(cls="TauCloverMeadowsMap.cs", size=(96, 64), chunks=(4, 4), ground="tau-clover-meadows-ground",
                    chars={"g": "grass", "d": "dirt", "w": "water", "B": "water", "T": "grassShade", "t": "grassShade", "x": "grassShade",
                           "h": "grassLight", "m": "forest", "b": "forest", "e": "grassShade", "r": "ancient", "n": "forest"}),
}
# The design chat's material library T-MAT01..09 (docs/reference/taurus-v4-art/taurus-environment-production-brief-chatgpt.txt), cut from the 3x3 sheet by `cut`.
SHEET_ORDER = ["grass", "grassLight", "grassShade", "dirt", "plaza", "water", "bank", "forest", "ancient"]
# Taurus Color & Lighting V2.0 "Golden Adventure Daylight" (ChatGPT design chat 24/11/2026, docs/reference/taurus-v4-art/palette/taurus-palette-spec-chatgpt.txt):
# mean render colour per material T-MAT01..09. Grass family: offset grade (keeps the painted contrast); the others: per-channel gain toward the mean (no flat fill).
PALETTE = {"grass": (0x88, 0xB6, 0x5A), "grassLight": (0xAB, 0xD3, 0x6C), "grassShade": (0x64, 0x8C, 0x49)}   # V2.1 (ChatGPT review of the in-game shots)
GAIN_PALETTE = {"dirt": (0xCF, 0xA8, 0x72), "plaza": (0xD5, 0xBE, 0x98), "water": (0x4B, 0xA8, 0xCC), "bank": (0x8B, 0x9D, 0x67), "forest": (0x8B, 0x86, 0x57), "ancient": (0xA6, 0x9C, 0x73)}
# zone tints over the same material (meadows): multiplier per char (BIBLE brightness ladder is feel, not exposure: keep it gentle)
TINT = {"b": 0.97, "n": 0.98, "e": 0.98}
ZONE_SOFT = {"grass", "grassLight", "grassShade", "forest", "ancient"}   # grass-family zone grounds blend wide; road / plaza / water keep a crisp ~0.35 m edge
BANK_PX = 26          # wet-bank band (T-MAT07) on the land side of every water edge (~0.4 m)


def rows_of(cfg):
    w, h = cfg["size"]
    s = open(os.path.join(ROOT, "Assets", "Scripts", "Maps", cfg["cls"]), encoding="utf-8").read()
    s = s[s.index("// BEGIN ROWS"):s.index("// END ROWS")]
    rows = re.findall(r'"([A-Za-z]{%d})"' % w, s)
    assert len(rows) == h, (len(rows), h)
    return rows


def seamless(tex):
    """Tileable swatch: blend of the swatch and its x / y / xy half-rolls, each weighted to zero exactly on its own seam (map-design-rules Sổ lỗi 21/11)."""
    h, w = tex.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    wx = np.sin(np.pi * (xx + 0.5) / w) ** 2; wy = np.sin(np.pi * (yy + 0.5) / h) ** 2   # 1 mid, 0 at the edges
    parts = [(tex, wx * wy), (np.roll(tex, w // 2, 1), (1 - wx) * wy), (np.roll(tex, h // 2, 0), wx * (1 - wy)), (np.roll(np.roll(tex, h // 2, 0), w // 2, 1), (1 - wx) * (1 - wy))]
    num = sum(p * wt[..., None] for p, wt in parts); den = sum(wt for _, wt in parts)[..., None]
    return num / den


def swatch(key):
    p = os.path.join(LIB, key + ".png")
    if not os.path.exists(p): raise SystemExit("missing swatch " + p)
    tex = seamless(np.asarray(Image.open(p).convert("RGB")).astype(np.float32))
    tgt = PALETTE.get(key)
    if tgt is not None:
        m = tex.reshape(-1, 3).mean(0)
        tex = np.clip((tex - m) * 0.95 + np.array(tgt, float), 0, 255)
    g = GAIN_PALETTE.get(key)
    if key == "plaza": tex = np.clip((tex - tex.reshape(-1, 3).mean(0)) * 1.12 + tex.reshape(-1, 3).mean(0), 0, 255)   # V2.1: grout 10-15 % clearer
    if key == "dirt": tex = np.clip((tex - tex.reshape(-1, 3).mean(0)) * 1.09 + tex.reshape(-1, 3).mean(0), 0, 255)    # V2.1: light / dark of the road +8-10 %
    if g is not None:
        m = tex.reshape(-1, 3).mean(0)
        tex = np.clip(tex * (np.array(g, float) / np.maximum(m, 1.0)), 0, 255)
    n = int(round(REPEAT.get(key, TEX_TILE / T) * T))
    return cv2.resize(tex, (n, n), interpolation=cv2.INTER_AREA if n < tex.shape[0] else cv2.INTER_CUBIC)


def tiled(tex, W, H, ox=0, oy=0):
    n = tex.shape[0]; ox %= n; oy %= n
    big = np.tile(tex, (H // n + 2, W // n + 2, 1))
    return big[oy:oy + H, ox:ox + W]


def value_noise(W, H, cell, seed):
    rng = np.random.RandomState(seed)
    g = rng.rand(H // cell + 3, W // cell + 3).astype(np.float32)
    return cv2.resize(g, ((W // cell + 3) * cell, (H // cell + 3) * cell), interpolation=cv2.INTER_CUBIC)[cell:cell + H, cell:cell + W]


def raised_terrace_ground(out, rows):
    """Willowbrook's raised Guild terrace (26/11, ChatGPT review "chưa liền mạch": the court up there read as the same plane as the plaza below).
    The plateau's ground is 9 % darker and a little less saturated (formal court above the bright plaza), a pale coping strip of dressed stone runs
    along the inside of the retaining wall, and soft contact shadows lie at the foot of the wall (16 %) and of the stairs (20 %) on the plaza."""
    sys.path.insert(0, os.path.join(ROOT, "Tools"))
    import tau_village_greybox_layout as VL
    H, W = out.shape[:2]
    x0, y0, x1, y1 = VL.PLATEAU; wy0, wy1 = VL.TERRACE_WALL_ROWS; s0, s1 = VL.TERRACE_STAIRS
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32) / T            # tile coords of every pixel
    inside = ((xx >= x0) & (xx < x1 + 1) & (yy >= y0) & (yy < wy0)).astype(np.float32)
    inside = cv2.GaussianBlur(inside, (0, 0), 0.15 * T)
    grey = out.mean(axis=2, keepdims=True)
    court = (out * 0.91) * 0.92 + grey * 0.91 * 0.08               # darker, 8 % toward grey
    out = out * (1 - inside[..., None]) + court * inside[..., None]
    along = (xx >= x0) & (xx < x1 + 1) & ~((xx >= s0) & (xx < s1 + 1))
    # coping strip: 0.55 tile of pale dressed stone just inside the wall top, a thin dark joint on its inner edge
    strip = (along & (yy >= wy0 - 0.75) & (yy < wy0 - 0.2)).astype(np.float32)
    strip = cv2.GaussianBlur(strip, (0, 0), 0.04 * T)
    stone = np.array([0xDA, 0xCB, 0xB2], np.float32) * (0.94 + 0.06 * np.sin(xx * 6.283 / 1.1))[..., None]   # slabs ~1.1 m
    out = out * (1 - 0.85 * strip[..., None]) + stone * 0.85 * strip[..., None]
    joint = (along & (np.abs(yy - (wy0 - 0.75)) < 0.035)).astype(np.float32)
    out *= (1 - 0.35 * joint)[..., None]
    # contact shadows on the plaza: wall foot (bottom edge of the wall rows), stairs foot (one row further south)
    def foot(y_edge, mask_x, depth, alpha):
        d = (yy - y_edge) / depth
        return np.where(mask_x & (d >= 0) & (d <= 1), alpha * (1 - d) ** 1.6, 0).astype(np.float32)
    shade = foot(wy1 + 1, along, 0.55, 0.16) + foot(wy1 + 2, (xx >= s0 - 0.3) & (xx < s1 + 1.3), 0.45, 0.20)
    shade = cv2.GaussianBlur(shade, (0, 0), 0.06 * T)
    return out * (1 - shade)[..., None]


def build(key, cfg):
    rows = rows_of(cfg); tw, th = cfg["size"]; W, H = tw * T, th * T
    k = T / 64.0          # the px constants below were tuned at 64 px / tile
    chars = cfg["chars"]
    mats = sorted(set(chars.values()), key=lambda m: list(chars.values()).index(m))
    # per-material tile masks -> feathered px masks with noisy edges
    edge_noise = (value_noise(W, H, max(2, int(10 * k)), 11) - 0.5) * 0.30 + (value_noise(W, H, max(2, int(4 * k)), 12) - 0.5) * 0.12 + (value_noise(W, H, int(28 * k), 7) - 0.5) * 0.35
    zone_noise = (value_noise(W, H, 2 * T, 21) - 0.5) * 0.45 + (value_noise(W, H, T // 2, 22) - 0.5) * 0.15
    weights = {}
    for m in mats:
        under = cfg.get("under", {})   # chars that also lay this material UNDER their own (village `k`: dirt slot on plaza, no grass halo round it)
        tm = np.array([[chars.get(c) == m or under.get(c) == m for c in r] for r in rows], np.float32)
        big = cv2.resize(tm, (W, H), interpolation=cv2.INTER_NEAREST)
        if m in ZONE_SOFT:   # zone grounds (rows give them as rectangles): wide noisy blend over ~3 tiles so no zone reads as a box
            soft = np.clip(cv2.GaussianBlur(big, (0, 0), 1.6 * T) + zone_noise, 0, None)
            weights[m] = soft ** 3 + (1e-4 if m == "grass" else 0)   # normalised among the zone grounds below; grass is the fallback where none reaches
            continue
        soft = cv2.GaussianBlur(big, (0, 0), (28 if m != "water" else 14) * k) + (edge_noise if m != "water" else edge_noise * 0.3)  # ~0.45 tile: tile steps become rounded organic edges
        hard = (soft > 0.5).astype(np.float32)
        weights[m] = cv2.GaussianBlur(hard, (0, 0), (FEATHER / 2.6 if m != "water" else 4) * k)
    if "water" in weights:  # T-MAT07 wet bank: a soft band on the land just outside the water
        wh = (weights["water"] > 0.5).astype(np.uint8)
        bp = int(round(BANK_PX * k)); band = cv2.dilate(wh, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * bp + 1,) * 2)).astype(np.float32) - wh
        band = cv2.GaussianBlur(np.clip(band + edge_noise * 0.6, 0, 1), (0, 0), 6 * k)
        for m in list(weights):
            if m != "water": weights[m] = weights[m] * (1 - band)
        weights["bank"] = band; mats.append("bank")
    if cfg.get("under") and "dirt" in weights and "plaza" in weights: weights["plaza"] = weights["plaza"] * (1 - weights["dirt"])   # dirt sits on top
    # two layers: the grass-family zone grounds blended among themselves (wide), then road / plaza / water / bank laid over them with their crisp alpha
    zone = [m for m in mats if m in ZONE_SOFT]; crisp = [m for m in mats if m not in ZONE_SOFT]
    ztot = sum(weights[m] for m in zone) + 1e-6
    out = np.zeros((H, W, 3), np.float32)
    for i, m in enumerate(mats):
        if m in zone: out += tiled(swatch(m), W, H, i * 197, i * 331) * (weights[m] / ztot)[..., None]
    ctot = sum(weights[m] for m in crisp) + 1e-6
    ca = np.clip(ctot, 0, 1)
    over = np.zeros((H, W, 3), np.float32)
    for i, m in enumerate(mats):
        if m in crisp: over += tiled(swatch(m), W, H, i * 197, i * 331) * (weights[m] / ctot)[..., None]
    out = out * (1 - ca[..., None]) + over * ca[..., None]
    tot = ztot + ctot
    # zone tint (meadows), feathered like the materials
    tint = np.ones((th, tw), np.float32)
    for y, r in enumerate(rows):
        for x, c in enumerate(r): tint[y, x] = TINT.get(c, 1.0)
    tint = cv2.GaussianBlur(cv2.resize(tint, (W, H), interpolation=cv2.INTER_LINEAR), (0, 0), 1.5 * T)
    water = np.clip(weights.get("water", np.zeros((H, W), np.float32)), 0, 1)
    land = 1 - water
    # low-frequency tone (+-5 % over ~6 tiles) and macro patches (1.5-3 m, +-5 %) on land only: the swatch repeat never shows
    tone = 1 + ((value_noise(W, H, 6 * T, 3) - 0.5) * 0.10 + (value_noise(W, H, 2 * T, 5) - 0.5) * 0.06 + (value_noise(W, H, 3 * T, 6) - 0.5) * 0.04)
    out *= (1 + (tone * tint - 1) * land)[..., None]
    if cfg.get("post") == "terrace": out = raised_terrace_ground(out, rows)
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    rn, cn = cfg["chunks"]; cw, ch = W // cn, H // rn
    for r in range(rn):
        for c in range(cn):
            p = os.path.join(TILES, "%s-r%dc%d.png" % (cfg["ground"], r, c))
            img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)).save(p, optimize=True); print(p)
    full = os.path.join(LIB, "%s-full.jpg" % cfg["ground"])
    img.resize((W // 2, H // 2), Image.LANCZOS).save(full, quality=90); print(full)
    return img


def border(key, cfg):
    """Seamless forest-floor tile for View25DGroundApron (past every map edge), graded to the map's outer 2 tiles."""
    n = 1024
    tex = cv2.resize(seamless(np.asarray(Image.open(os.path.join(LIB, "forest.png")).convert("RGB")).astype(np.float32)), (n, n), interpolation=cv2.INTER_AREA)
    rn, cn = cfg["chunks"]
    ring = []
    for r in range(rn):
        for c in range(cn):
            im = np.asarray(Image.open(os.path.join(TILES, "%s-r%dc%d.png" % (cfg["ground"], r, c))).convert("RGB")).astype(np.float32)
            b = 2 * T
            if r == 0: ring.append(im[:b].reshape(-1, 3))
            if r == rn - 1: ring.append(im[-b:].reshape(-1, 3))
            if c == 0: ring.append(im[:, :b].reshape(-1, 3))
            if c == cn - 1: ring.append(im[:, -b:].reshape(-1, 3))
    ring = np.concatenate(ring)
    grassy = ring[(ring[:, 1] > ring[:, 2] + 20) & (ring[:, 1] >= ring[:, 0])]
    mean = np.median(grassy, 0) * 0.96; print("outer ground", mean.round())
    tex = np.clip((tex - tex.reshape(-1, 3).mean(0)) * 0.9 + mean, 0, 255)
    out = os.path.join(TILES, cfg["ground"] + "-border.png")
    Image.fromarray(tex.astype(np.uint8)).save(out, optimize=True); print(out)


def cut(sheet):
    """Split the 3x3 material sheet (cells separated by #FF00FF gaps) into Assets/ArtLibrary/maps/taurus/ground/<material>.png."""
    im = np.asarray(Image.open(sheet).convert("RGB")).astype(np.int32)
    mag = (im[..., 0] > 200) & (im[..., 2] > 200) & (im[..., 1] < 80)
    cols = np.nonzero(mag.mean(0) < 0.5)[0]; rows = np.nonzero(mag.mean(1) < 0.5)[0]
    def runs(ix):
        out, a = [], ix[0]
        for i, j in zip(ix, ix[1:]):
            if j != i + 1: out.append((a, i)); a = j
        out.append((a, ix[-1])); return [r for r in out if r[1] - r[0] > 100]
    rr, cc = runs(rows), runs(cols)
    assert len(rr) == 3 and len(cc) == 3, (rr, cc)
    for k, name in enumerate(SHEET_ORDER):
        (y0, y1), (x0, x1) = rr[k // 3], cc[k % 3]
        cell = Image.fromarray(im[y0 + 6:y1 - 5, x0 + 6:x1 - 5].astype(np.uint8))
        cell.save(os.path.join(LIB, name + ".png")); print(name, cell.size)


def main():
    key = sys.argv[1]
    if key == "cut": cut(sys.argv[2]); return
    cfg = MAPS[key]
    global T
    T = cfg.get("px", T)   # px per tile of this map's ground (village V2: 40)
    if "border" in sys.argv[2:]: border(key, cfg); return
    img = build(key, cfg)
    if "--preview" in sys.argv:
        img.resize((1536, 1024), Image.LANCZOS).save(os.path.join(LIB, key + "-ground-preview.jpg"), quality=88)


if __name__ == "__main__":
    main()

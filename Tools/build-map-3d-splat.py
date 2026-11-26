"""Splat control + water mask for the 3D map prototype (Map3D, first: Willowbrook Meadows, 22/11/2026).

    python Tools/build-map-3d-splat.py willowbrook-meadows [--preview]

The 3D terrain (Assets/Scripts/Map3D/Map3DTerrain.cs) must keep the roads, paving and water on the exact tiles of the 2.5D ground, so the layer
weights are read from that ground (the chunks Tools/build-map-25d-ground.py wrote) instead of painted again:
  R = dirt road / trail (warm ochre pixels), G = forest floor (the tree-belt mask of place-map-25d-forest.py, blurred),
  B = stone paving (pale low-saturation pixels inside the ruin rect) + crop-field rows, A = water (blue pixels, closed + softened).
Written as raw bytes (TextAsset), 8 texels per tile, rows top-down like the map rows: the build copies Resources without .meta files, so a PNG would
import compressed / sRGB / non-readable; Map3DTerrain builds a linear Texture2D from these bytes and reads the water channel on the CPU for the riverbed.
Header: 4 bytes 'M3DS', uint16 width, uint16 height, then width * height * 4 bytes RGBA.
"""
import os, re, sys, struct
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "Assets", "Resources")
PX = 8  # texels per tile in the control map
MAPS = {
    "willowbrook-meadows": dict(size=(72, 48), chunks=(2, 2), pattern="Sprites/village/tiles/willowbrook-large-ground-r{r}c{c}.png",
                                forest="Assets/ArtLibrary/maps/willowbrook-meadows/forest-25d/willowbrook-meadows-forest-mask.png",
                                paving=[(43, 0, 27, 11)], crop=[(0, 38, 12, 8)], out="Map3D/willowbrook-meadows-splat.bytes"),
}


# Taurus maps (24/11/2026): the layout IS the rows (greybox), so the layers come from the tile chars, not from a painted ground:
# d = dirt road, p = plaza paving, w / B = water (B = bridge deck, the stream bed still sinks under it), T / t = forest floor (feathered).
TAU = {
    "tau-willowbrook-village": dict(size=(100, 100), cls="TauWillowbrookVillageMap.cs", paving="pk"),   # V2 25/11: k = market shop slot (dirt on paving)
    "tau-clover-meadows": dict(size=(96, 64), cls="TauCloverMeadowsMap.cs", paving=""),
}


def tau_rows(cfg):
    w, h = cfg["size"]
    t = open(os.path.join(ROOT, "Assets", "Scripts", "Maps", cfg["cls"]), encoding="utf-8").read()
    t = t[t.index("// BEGIN ROWS"):t.index("// END ROWS")]
    rows = re.findall(r'"([A-Za-z]{%d})"' % w, t)
    assert len(rows) == h, (len(rows), h)
    return rows


def tau_main(key):
    cfg = TAU[key]
    W, H = cfg["size"]
    rows = tau_rows(cfg)
    ch = np.array([[ord(c) for c in r] for r in rows], np.int32)
    rng = np.random.RandomState(7)
    lo = lambda: cv2.resize(rng.rand(H // 3 + 2, W // 3 + 2).astype(np.float32) - 0.5, (W * PX, H * PX), interpolation=cv2.INTER_CUBIC)
    gx, gy = np.meshgrid(np.arange(W * PX, dtype=np.float32), np.arange(H * PX, dtype=np.float32))
    mx, my = gx + lo() * 0.9 * PX, gy + lo() * 0.9 * PX   # edges wander up to ~0.45 tile: no tile staircase on road / plaza / forest borders
    big = lambda m: cv2.remap(np.kron(m.astype(np.float32), np.ones((PX, PX), np.float32)), mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
    soft = lambda m, s: np.clip(cv2.GaussianBlur(m.astype(np.float32), (0, 0), s), 0, 1)
    def round_edge(m, sg):  # blur wide then re-sharpen: tile corners become curves, the edge stays fairly crisp
        b = soft(m, sg)
        return soft(np.clip((b - 0.3) / 0.4, 0, 1) ** 2 * (3 - 2 * np.clip((b - 0.3) / 0.4, 0, 1)), 1.0)
    dirt = round_edge(big(np.isin(ch, [ord("d"), ord("k")])), 4.0)   # k (village market slot) = a dirt square
    pave = round_edge(big(np.isin(ch, [ord(c) for c in cfg["paving"]])), 6.0) if cfg["paving"] else np.zeros((H * PX, W * PX), np.float32)
    if "k" in cfg["paving"]: pave = pave * (1 - dirt)   # village market slots: the dirt square sits ON the paving (the terrain shader would draw paving over it)
    water = big(np.isin(ch, [ord("w"), ord("B")]))
    wsoft = soft(water, 3.0)
    wsoft = np.clip((wsoft - 0.3) / 0.4, 0, 1); wsoft = soft(wsoft * wsoft * (3 - 2 * wsoft), 1.0)
    forest = soft(big(np.isin(ch, [ord("T"), ord("t")])), PX * 0.9)
    forest = np.clip(forest * (1 - dirt) * (1 - pave), 0, 1)
    out = np.stack([dirt, forest, pave, wsoft], 2)
    out8 = np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8)
    path = os.path.join(RES, "Map3D", key + "-splat.bytes")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(b"M3DS" + struct.pack("<HH", W * PX, H * PX) + out8.tobytes())
    print("wrote", path, out8.shape, "dirt %.1f%% forest %.1f%% pave %.1f%% water %.1f%%" % tuple(100 * out[..., i].mean() for i in range(4)))
    if "--preview" in sys.argv:
        prev = os.path.join(ROOT, "plans", "261124-0930-taurus-v4-new-world", "reports", key + "-splat-preview.png")
        Image.fromarray(out8[..., :3]).resize((W * PX * 2, H * PX * 2), Image.NEAREST).save(prev)
        print("preview", prev)


def read_ground(cfg):
    rows, cols = cfg["chunks"]
    strips = []
    for r in range(rows):
        strips.append(np.concatenate([np.asarray(Image.open(os.path.join(RES, cfg["pattern"].format(r=r, c=c))).convert("RGB")) for c in range(cols)], 1))
    return np.concatenate(strips, 0).astype(np.float32)


def rect_mask(W, H, rects):
    m = np.zeros((H * PX, W * PX), np.float32)
    for x, y, w, h in rects: m[y * PX:(y + h) * PX, x * PX:(x + w) * PX] = 1
    return m


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "willowbrook-meadows"
    if key in TAU: return tau_main(key)
    cfg = MAPS[key]
    W, H = cfg["size"]
    g = read_ground(cfg)
    small = cv2.resize(g, (W * PX, H * PX), interpolation=cv2.INTER_AREA)
    R, G, B = small[..., 0], small[..., 1], small[..., 2]
    mx, mn = small.max(2), small.min(2)
    sat = (mx - mn) / np.maximum(mx, 1)
    water = ((B > R + 30) & (B > G - 10)).astype(np.uint8)
    water = cv2.morphologyEx(water, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))
    dirt = ((R > G + 6) & (R > B + 40) & (mx > 120)).astype(np.float32)
    dirt[cv2.dilate(water, np.ones((5, 5), np.uint8)) > 0] = 0  # bank pebbles are not road
    pave = (rect_mask(W, H, cfg["paving"]) * ((sat < 0.30) & (mx > 140) & (water == 0))).astype(np.float32)
    pave = cv2.morphologyEx(pave, cv2.MORPH_CLOSE, np.ones((7, 7), np.uint8))  # one courtyard, not speckles between the slab joints
    pave = cv2.morphologyEx(pave, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    dirt[pave > 0] = 0
    pave = np.maximum(pave, rect_mask(W, H, cfg["crop"]) * (R > G))  # furrows read as packed earth + stone in the B layer
    forest = np.asarray(Image.open(os.path.join(ROOT, cfg["forest"])).convert("L")).astype(np.float32) / 255.0
    forest = cv2.GaussianBlur(cv2.resize(forest, (W * PX, H * PX), interpolation=cv2.INTER_LINEAR), (0, 0), PX * 1.2)
    soft = lambda m, s: np.clip(cv2.GaussianBlur(m.astype(np.float32), (0, 0), s), 0, 1)
    dirt = soft(dirt, 0.9)
    pave = soft(pave, 0.8)
    wsoft = soft(water, 2.4)  # the painted banks step with the tiles: round them (the 3D bed and water sheet follow this channel)
    wsoft = np.clip((wsoft - 0.3) / 0.4, 0, 1); wsoft = soft(wsoft * wsoft * (3 - 2 * wsoft), 0.8)
    forest = np.clip(forest * (1 - dirt) * (1 - pave), 0, 1)
    out = np.stack([dirt, forest, pave, wsoft], 2)
    out8 = np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8)
    path = os.path.join(RES, cfg["out"])
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(b"M3DS" + struct.pack("<HH", W * PX, H * PX) + out8.tobytes())
    print("wrote", path, out8.shape, "dirt %.1f%% forest %.1f%% pave %.1f%% water %.1f%%" % tuple(100 * out[..., i].mean() for i in range(4)))
    if "--preview" in sys.argv:
        prev = os.path.join(ROOT, "plans", "261122-1100-map-3d-proto", "splat-preview.png")
        os.makedirs(os.path.dirname(prev), exist_ok=True)
        Image.fromarray(out8[..., :3]).resize((W * PX * 2, H * PX * 2), Image.NEAREST).save(prev)
        print("preview", prev)


if __name__ == "__main__":
    main()

"""Flat ground of a dark 2.5D dungeon from two ChatGPT swatches + the rows (first: Rootburrow, 21/11/2026).

    python Tools/build-map-25d-dungeon-ground.py rootburrow [--preview]     the map's ground texture (same file the game loads)
    python Tools/build-map-25d-dungeon-ground.py rootburrow border          <ground>-border.png: the render-only band past the map edges

Why (ChatGPT art direction, docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md section B): at the 58 deg camera
the old painted root walls (seen straight from above) would lie flat and tilt; the ground becomes only two flat materials and every wall is a
standing prop (Tools/place-map-25d-dungeon-props.py):
  1. Floor swatch (V2: lavender cave flagstones #77718F, indigo joints, thin cyan cracks) on the walkable tiles ('d'), graded to its mean (painted albedo:
     the dark comes from DungeonLights, never painted in); V1 (brown earth) was dropped by the user, it is still in git;
  2. SolidBase swatch (dark violet cave rock #514C70, petrified roots) on the solid tiles, deepening toward the indigo void colour (#17152F)
     3+ tiles from the floor (the mass between the halls);
  3. transition: an irregular mask edge 0.35-0.55 m wide (noise), plus a fake AO band 0.25-0.45 m wide on the floor along the walls (x0.72);
  4. water tiles ('w', the old pond) become root mass like any solid tile: the old painted pond kept as pixels read as a bright square in the dark.
Big maps write chunks <ground>-r0c{c}.png side by side (MAPS chunks). Sources: the same folder, *-floor-chatgpt-N.png,
*-solid-chatgpt-N.png (highest N wins). To convert another dungeon: add an entry to MAPS.
"""
import os, re, sys, glob
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "Assets", "Resources", "Sprites")
T = 64
MAPS = {"rootburrow": dict(size=(60, 24), ground="village/tiles/rootburrow-ground.png", chunks=2, rows_file="Assets/Scripts/Maps/RootburrowMap.cs")}  # V3 60x24, 2 chunks
FLOOR, SOLID, VOID = (0x7C, 0x6E, 0xA2), (0x52, 0x48, 0x76), (0x17, 0x15, 0x2F)   # V2 violet cave (user 21/11: purple like the RO3 clip, not brown)
TEX_TILE = 768       # px one swatch covers (12 tiles)
EDGE = 30            # px: half width of the irregular floor / solid edge (~0.45 m)
AO = 24              # px: fake ambient occlusion band on the floor along the walls (~0.37 m)


def lib(key): return os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "dark-25d")


def latest(key, kind):
    files = glob.glob(os.path.join(lib(key), "*-%s-chatgpt-*.png" % kind))
    if not files: raise SystemExit("no %s swatch in %s" % (kind, lib(key)))
    return max(files, key=lambda p: int(re.findall(r"-(\d+)\.png$", p)[0]))


def seamless(tex):
    h, w = tex.shape[:2]
    sh = np.roll(np.roll(tex, h // 2, 0), w // 2, 1)
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.minimum(np.minimum(xx, w - 1 - xx) / (w * 0.5), np.minimum(yy, h - 1 - yy) / (h * 0.5))
    wgt = np.clip(d / 0.35, 0, 1)[..., None]
    return tex * wgt + sh * (1 - wgt)


def grade(tex, target, keep_std=0.9):
    m = tex.reshape(-1, 3).mean(0)
    return np.clip((tex - m) * keep_std + np.array(target, float), 0, 255)


def swatch(key, kind, target, size):
    sw = np.asarray(Image.open(latest(key, kind)).convert("RGB")).astype(np.float32)
    s = min(sw.shape[:2]); sw = sw[:s, :s]                       # square middle of the swatch image
    return cv2.resize(grade(seamless(sw), target), (size, size), interpolation=cv2.INTER_AREA)


def tile(tex, W, H):
    reps = (H // tex.shape[0] + 1, W // tex.shape[1] + 1)
    return np.tile(tex, (reps[0], reps[1], 1))[:H, :W]


def value_noise(W, H, cell, seed):
    g = np.random.RandomState(seed).rand(H // cell + 2, W // cell + 2).astype(np.float32)
    return cv2.resize(g, (W + 2 * cell, H + 2 * cell), interpolation=cv2.INTER_CUBIC)[cell:cell + H, cell:cell + W]


def rows_of(cfg):
    w, h = cfg["size"]
    rows = re.findall(r'"([TgwBd]{%d})"' % w, open(os.path.join(ROOT, cfg["rows_file"]), encoding="utf8").read())
    assert len(rows) == h, len(rows)
    return rows


def border(key, cfg):
    """Seamless 1024 px tile of the solid ground, a bit darker, with darker pools: the band past the map edges (View25DGroundApron)."""
    n = 1024
    tex = swatch(key, "solid", tuple(int(c * 0.82) for c in SOLID), n)
    g = np.random.RandomState(9).rand(4, 4).astype(np.float32)
    pool = cv2.resize(np.tile(g, (3, 3)), (3 * n, 3 * n), interpolation=cv2.INTER_CUBIC)[n:2 * n, n:2 * n]
    k = np.clip((pool - 0.55) * 3, 0, 1)[..., None]
    tex = tex * (1 - k * 0.45) + np.array(VOID, float) * k * 0.45
    out = os.path.join(RES, cfg["ground"].replace(".png", "-border.png"))
    Image.fromarray(np.clip(tex, 0, 255).astype(np.uint8)).save(out, optimize=True); print(out)


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    cfg = MAPS[key]
    if "border" in sys.argv[2:]: border(key, cfg); return
    tw, th = cfg["size"]; W, H = tw * T, th * T
    os.makedirs(lib(key), exist_ok=True)
    gpath = os.path.join(RES, cfg["ground"])
    rows = rows_of(cfg)
    floor_t = np.array([[c in "dg" for c in r] for r in rows], np.uint8)

    # floor mask: tiles -> px, an irregular edge (noise around the 0.5 level), soft over a few px
    f = cv2.resize(floor_t.astype(np.float32), (W, H), interpolation=cv2.INTER_NEAREST)
    f = cv2.GaussianBlur(f, (0, 0), EDGE * 0.55)
    f += (value_noise(W, H, 40, 3) - 0.5) * 0.45 + (value_noise(W, H, 12, 4) - 0.5) * 0.18
    hard = (f > 0.5).astype(np.float32)
    fm = np.clip(cv2.GaussianBlur(hard, (0, 0), 3.0), 0, 1)
    # distance from the floor in px (for the AO band on the floor side and the void on the solid side)
    dist_out = cv2.distanceTransform((1 - hard).astype(np.uint8), cv2.DIST_L2, 5)   # solid px: distance to the floor
    dist_in = cv2.distanceTransform(hard.astype(np.uint8), cv2.DIST_L2, 5)          # floor px: distance to the wall

    floor = tile(swatch(key, "floor", FLOOR, TEX_TILE), W, H)
    floor *= (1 + (value_noise(W, H, 5 * T, 7) - 0.5) * 0.10)[..., None]            # +-5 % over ~5 tiles: hides the repeat
    solid = tile(swatch(key, "solid", SOLID, TEX_TILE), W, H)
    solid *= (1 + (value_noise(W, H, 4 * T, 8) - 0.5) * 0.12)[..., None]
    deep = np.clip((dist_out - 1.5 * T) / (2.5 * T), 0, 1)[..., None] * 0.65         # 1.5 -> 4 tiles into the mass: toward the void colour
    solid = solid * (1 - deep) + np.array(VOID, float) * deep
    ao = (1 - np.clip(dist_in / AO, 0, 1)) * 0.28                                     # floor side: darker right at the wall foot
    floor *= (1 - ao)[..., None]
    out = floor * fm[..., None] + solid * (1 - fm[..., None])

    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    n = cfg.get("chunks", 1)
    if n == 1: img.save(gpath, optimize=True); print("wrote", gpath)
    else:                                                              # <ground>-r0c{c}.png, side by side (WorldBuilder's chunk names)
        cw = W // n
        for c in range(n):
            out = gpath.replace(".png", "-r0c%d.png" % c); img.crop((c * cw, 0, (c + 1) * cw, H)).save(out, optimize=True); print("wrote", out)
    if "--preview" in sys.argv:
        img.resize((1152, 768), Image.LANCZOS).save(os.path.join(lib(key), key + "-ground-preview.jpg"), quality=88)


if __name__ == "__main__":
    main()

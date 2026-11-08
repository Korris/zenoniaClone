"""Tier-set gear icons: armor / gloves / boots per class + one SHARED necklace set (slot id "helmet"), 10 tiers each (run from anywhere, Python 3 + Pillow + numpy + scipy).

Source: one ChatGPT image per class + slot, 1536x1024, 2 rows of 5 icons (top T0-T4, bottom T5-T9) on a flat key colour,
saved as Assets/ArtLibrary/classes/<class>/gear-icons-src/<slot>-chatgpt-<n>.png (SOURCES picks the approved one).
The helmet slot is shown as a NECKLACE shared by every class (designer 08/11, no hats): source Assets/ArtLibrary/custom/necklace-icons/necklace-chatgpt-1.png,
installed over the shared helmet-0..9 (existing .meta kept); Tools/build-item-icons.py no longer writes helmet-N.
Output: Assets/Resources/Sprites/icons/<slot>-<class>-<tier>.png (96 px, content max ICON_MAX, ~10% margin for enhance glow),
read by ItemInfo.ClassIconName / LootSprites.Icon; the shared "<slot>-<tier>" icons stay as fallback (fighter, ground loot).
.meta copied from armor-0.png.meta with a new guid (kept on reruns).

  python Tools/build-class-gear-icons.py write <class|all> [slot]   cut + install armor / gloves / boots
  python Tools/build-class-gear-icons.py write shared               cut + install the shared necklaces (helmet-N)
  python Tools/build-class-gear-icons.py sheet <class|all>          docs/reference/gear-icons-<class>-t0-t9.png (shared necklace + 3 class rows x 10 tiers)
"""
import os, re, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "icons")
META_TEMPLATE = os.path.join(ICONS, "armor-0.png.meta")
SLOTS = ["armor", "gloves", "boots"]  # per class; the necklace row (slot "helmet") is shared
CLASSES = ["warrior", "wizard", "mechanic", "assassin", "cleric"]
SIZE, ICON_MAX = 96, 78  # 78/96: ~10% margin on each side so the enhance glow / shine is not clipped
KEYS = {"green": (0, 255, 0), "magenta": (255, 0, 255)}
CLASS_KEY = {"warrior": "green", "wizard": "green", "mechanic": "green", "assassin": "green", "cleric": "magenta"}
DESPILL_TIERS = {"wizard": {9}, "assassin": {9}}  # tiers with soft glow (blue flames) that picked up the green key inside: clamp G <= max(R, B) on the whole icon
SOURCES = {}  # (class, slot) -> file name when the approved image is not "<slot>-chatgpt-1.png"
FLOOD_DIST, EDGE_LO, EDGE_HI, EDGE_PX = 110, 70, 170, 3
HOLE_DIST, HOLE_MIN_PX = 60, 40


NECKLACE_SRC = os.path.join(ROOT, "Assets", "ArtLibrary", "custom", "necklace-icons", "necklace-chatgpt-1.png")
NECKLACE_KEY = "magenta"


def src_path(cls, slot):
    return os.path.join(ROOT, "Assets", "ArtLibrary", "classes", cls, "gear-icons-src", SOURCES.get((cls, slot), slot + "-chatgpt-1.png"))


def key_out(rgb, key):
    """RGBA from a flat-key image: background = key-like pixels connected to the border; soft alpha + despill only near it."""
    a = rgb.astype(np.float32)
    dist = np.sqrt(((a - np.array(key, np.float32)) ** 2).sum(2))
    near = dist < FLOOD_DIST
    lab, _ = ndimage.label(near)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    # holes: near-pure key colour enclosed by the art (inside a helmet opening, a halo ring) is background too
    pure, _ = ndimage.label(dist < HOLE_DIST)
    sizes = np.bincount(pure.ravel())
    bg |= (sizes[pure] >= HOLE_MIN_PX) & (pure > 0)
    edge = ndimage.binary_dilation(bg, iterations=EDGE_PX) & ~bg
    alpha = np.where(bg, 0.0, 1.0)
    alpha[edge] = np.clip((dist[edge] - EDGE_LO) / (EDGE_HI - EDGE_LO), 0, 1)
    out = a.copy()
    r, g, b = out[..., 0], out[..., 1], out[..., 2]
    if key == KEYS["green"]:
        g[edge] = np.minimum(g[edge], np.maximum(r[edge], b[edge]))
    else:  # magenta spill lifts red and blue together
        m = np.minimum(r, b)
        cap = g + 25
        over = edge & (m > cap)
        r[over] -= (m - cap)[over]; b[over] -= (m - cap)[over]
    rgba = np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)
    return rgba


def cells(rgba):
    """10 icon crops: connected parts grouped into a 2 x 5 grid by centroid (top row T0-T4)."""
    h, w = rgba.shape[:2]
    mask = rgba[..., 3] > 40
    lab, n = ndimage.label(mask)
    groups = {i: [] for i in range(10)}
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        if area < 60: continue  # specks
        cy, cx = ndimage.center_of_mass(lab == i)
        groups[(0 if cy < h / 2 else 5) + min(4, int(cx / (w / 5)))].append(i)
    out = []
    for t in range(10):
        if not groups[t]: sys.exit("tier %d: no icon found in its cell" % t)
        m = np.isin(lab, groups[t])
        ys, xs = np.where(m)
        crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[..., 3] = np.where(m[ys.min():ys.max() + 1, xs.min():xs.max() + 1], crop[..., 3], 0)
        out.append(Image.fromarray(crop, "RGBA"))
    return out


def fit(im):
    k = ICON_MAX / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    out.paste(im, ((SIZE - im.width) // 2, (SIZE - im.height) // 2), im)
    return out


def write_meta(png):
    meta = png + ".meta"
    if os.path.exists(meta): return  # keep the guid Unity already knows
    text = open(META_TEMPLATE, encoding="utf-8").read()
    open(meta, "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1))


def write_shared_necklace():
    if not os.path.exists(NECKLACE_SRC): print("skip (no source):", NECKLACE_SRC); return
    rgba = key_out(np.array(Image.open(NECKLACE_SRC).convert("RGB")), KEYS[NECKLACE_KEY])
    for t, c in enumerate(cells(rgba)):
        png = os.path.join(ICONS, "helmet-%d.png" % t)
        fit(c).save(png)
        write_meta(png)
    print("wrote shared necklaces helmet-0..9")


def write(cls, slot):
    path = src_path(cls, slot)
    if not os.path.exists(path): print("skip (no source):", path); return
    rgba = key_out(np.array(Image.open(path).convert("RGB")), KEYS[CLASS_KEY[cls]])
    for t, c in enumerate(cells(rgba)):
        if t in DESPILL_TIERS.get(cls, ()) and CLASS_KEY[cls] == "green":
            a = np.array(c); a[..., 1] = np.minimum(a[..., 1], np.maximum(a[..., 0], a[..., 2])); c = Image.fromarray(a, "RGBA")
        png = os.path.join(ICONS, "%s-%s-%d.png" % (slot, cls, t))
        fit(c).save(png)
        write_meta(png)
    print("wrote", slot, cls, "x10")


def sheet(cls):
    cell, pad = 104, 4
    rows = [("necklace", "helmet-%d")] + [(slot, slot + "-" + cls + "-%d") for slot in SLOTS]
    W, H = 60 + 10 * cell, 24 + len(rows) * cell
    im = Image.new("RGBA", (W, H), (34, 30, 40, 255)); d = ImageDraw.Draw(im)
    for t in range(10): d.text((60 + t * cell + 40, 6), "T%d" % t, fill=(230, 220, 200))
    for r, (label, pattern) in enumerate(rows):
        d.text((6, 24 + r * cell + 44), label, fill=(230, 220, 200))
        for t in range(10):
            x, y = 60 + t * cell, 24 + r * cell
            d.rectangle((x + pad, y + pad, x + cell - pad, y + cell - pad), fill=(22, 20, 28), outline=(90, 80, 70))
            p = os.path.join(ICONS, pattern % t + ".png")
            if os.path.exists(p): im.alpha_composite(Image.open(p).convert("RGBA"), (x + 4, y + 4))
    out = os.path.join(ROOT, "docs", "reference", "gear-icons-%s-t0-t9.png" % cls)
    im.save(out); print("sheet", out)


if __name__ == "__main__":
    if len(sys.argv) < 3: sys.exit(__doc__)
    cmd, who = sys.argv[1], sys.argv[2]
    if cmd == "write" and who == "shared": write_shared_necklace(); sys.exit(0)
    for cls in CLASSES if who == "all" else [who]:
        if cmd == "write":
            for slot in [sys.argv[3]] if len(sys.argv) > 3 else SLOTS: write(cls, slot)
        elif cmd == "sheet": sheet(cls)
        else: sys.exit(__doc__)

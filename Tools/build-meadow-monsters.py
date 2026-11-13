"""Builds the meadow monster sheets (Willowbrook) from the ChatGPT 4x4 sheets on a #FF00FF background.

Source (Assets/ArtLibrary/monsters/meadow/<src>-walk-chatgpt.png and <src>-attack-chatgpt.png, 1254x1254, rows: down, up, left, right)
 -> Assets/Resources/Sprites/monsters/<id>.png         768x768  (4x4 cells of 192, pivot bottom-center: feet on the cell's last row)
 -> Assets/Resources/Sprites/monsters/<id>-attack.png  1024x1024 (4x4 cells of 256, feet 224 px down = pivot 32 px above the bottom)
Previews: plans/261105-1052-willowbrook-large-hunting-map/visuals/meadow-*.png

Usage: python Tools/build-meadow-monsters.py [id ...]     (no id = every monster in MONSTERS whose 4 source files exist)

Steps per sheet: key out magenta (unmix the pink edge pixels, so no halo), split into 4x4 by the empty valleys of the alpha projection (ChatGPT cells are not
exact thirds), then one uniform scale per monster (walk and attack share it, so the body size is identical) and the feet of the grounded frames on the
game baseline. Frames in the air (hop, leap) keep their lift above the row's ground line. Frames are centred on the body (largest blob, ignoring swoosh lines)
and shifted, never clipped.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SRC_DIR = os.path.join(ROOT, "Assets", "ArtLibrary", "monsters", "meadow")
FROST_DIR = os.path.join(ROOT, "Assets", "ArtLibrary", "monsters", "frost")  # Frost Hollow deep-area monsters (same sheet format)
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Sprites", "monsters")
VIS_DIR = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map", "visuals")

WALK_CELL, ATK_CELL = 192, 256
ATK_FEET = 224          # feet row in the attack cell (pivot 32 px above the bottom)
MARGIN = 3              # frames never come closer than this to the cell edge

# id -> (source file stem, target median body height in walk-cell px, comment). The height is measured on the median walk frame (ears/horns
# included) and, together with the width cap below, sets the one scale. Reference heights: frostSlime ~100, iceGoblin ~152, hero ~164.
MONSTERS = {
    "meadowSlime": ("meadow-slime", 100, "Lv1"),
    "hornRabbit": ("horn-rabbit", 125, "Lv2"),
    "thornBoar": ("thorn-boar", 175, "Lv3"),
    "meadowMantis": ("meadow-mantis", 172, "Lv5"),
    "brambleTreant": ("bramble-treant", 190, "Lv6"),
    # Frost Hollow deep area (sources in FROST_DIR)
    "snowFox": ("snow-fox", 120, "Lv3"),
    "iceImp": ("ice-imp", 135, "Lv4"),
    "frostBear": ("frost-bear", 175, "Lv5"),
    "iceWraith": ("ice-wraith", 165, "Lv7"),
    "frostbiteYeti": ("frostbite-yeti", 185, "Lv8"),
    "crystalGolem": ("crystal-golem", 190, "Lv9"),
}
FROST_IDS = {"snowFox", "iceImp", "frostBear", "iceWraith", "frostbiteYeti", "crystalGolem"}

# World Map 2.1 monsters (13/11/2026): id -> source folder under Assets/ArtLibrary/monsters. These sheets carry a baked dark purple ground shadow
# (same hue as the #FF00FF key, only darker). The game draws its own shadow under every monster (Monster.cs), so the baked one is removed (DROP_SHADOW);
# for the flyers its centre is first taken as the ground point, so the body keeps its hover height above the feet line (FLYERS).
MONSTERS.update({
    "forestWolf": ("forest-wolf", 140, "Lv7"),
    "thornSprite": ("thorn-sprite", 130, "Lv9, flyer"),
    "thornfangAlpha": ("thornfang-alpha", 185, "Lv10 map boss"),
    "thornheartTreant": ("thornheart-treant", 188, "Lv10 dungeon boss"),
    "highlandIbex": ("highland-ibex", 168, "Lv16"),
    "dustscaleLizard": ("dustscale-lizard", 120, "Lv17"),
    "cliffHarpy": ("cliff-harpy", 150, "Lv18, flyer"),
    "stonebackGuardian": ("stoneback-guardian", 185, "Lv20"),
    "ridgehornBehemoth": ("ridgehorn-behemoth", 185, "Lv20 map boss"),
})
WORLD2_DIRS = {
    "forestWolf": "bramblewood", "thornSprite": "bramblewood", "thornfangAlpha": "bramblewood", "thornheartTreant": "rootburrow",
    "highlandIbex": "sunbreak", "dustscaleLizard": "sunbreak", "cliffHarpy": "sunbreak", "stonebackGuardian": "sunbreak", "ridgehornBehemoth": "sunbreak",
}
DROP_SHADOW = set(WORLD2_DIRS)
FLYERS = {"thornSprite", "cliffHarpy"}
WORLD2_VIS_DIR = os.path.join(ROOT, "plans", "visuals", "world2-monsters")


# ---------------------------------------------------------------- keying
def shadow_mask(px):
    """Baked ground shadow: pixels on the magenta hue line (R ~ B, G ~ 0) but darker than the background."""
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    hi = np.maximum(r, b)
    core = (hi > 0.18) & (hi < 0.86) & ((np.minimum(r, b) - g) > 0.55 * hi) & (np.abs(r - b) < 0.25 * hi)   # Stoneback shadow is a greyer (150,30,130)
    # only real blobs (a shadow ellipse is thousands of px); single dark-purple outline pixels of the art stay
    lab, n = ndimage.label(core)
    if n:
        sizes = ndimage.sum(core, lab, index=np.arange(1, n + 1))
        core = np.isin(lab, 1 + np.where(sizes >= 60)[0])
    return core


def key_magenta(path, drop_shadow=False):
    """RGB on #FF00FF -> float RGBA (0..1, straight alpha). With drop_shadow returns (rgba, removed shadow mask). Alpha from the magenta excess min(R,B)-G (255 on the background, <=30 on any art here),
    colour unmixed from the background (P = a*C + (1-a)*M) so edge pixels lose their pink fringe."""
    px = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32) / 255.0
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    excess = np.minimum(r, b) - g
    lo, hi = 30 / 255.0, 235 / 255.0
    a = np.clip(1.0 - (excess - lo) / (hi - lo), 0.0, 1.0)
    bg = np.array([1.0, 0.0, 1.0], dtype=np.float32)
    safe = np.maximum(a, 1e-3)[..., None]
    rgb = np.clip((px - (1.0 - a)[..., None] * bg) / safe, 0.0, 1.0)
    # despill: where the unmixed colour still leans magenta (excess over G), pull R and B down to G + a small allowance
    ex2 = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
    spill = (a < 0.999) & (ex2 > 0.12)
    cap = rgb[..., 1] + 0.12
    for ch in (0, 2):
        rgb[..., ch] = np.where(spill, np.minimum(rgb[..., ch], cap), rgb[..., ch])
    a = np.where(a < 0.04, 0.0, a)
    shadow = None
    if drop_shadow:
        shadow = shadow_mask(px)
        ring = ndimage.binary_dilation(shadow, iterations=4) & (a < 0.45)   # faint dark ring left where the shadow fades into the background
        a = np.where(shadow | ring, 0.0, a)
    # drop stray specks (JPEG-like noise) but keep thin swoosh lines: components under 25 px that touch nothing big
    lab, n = ndimage.label(ndimage.binary_dilation(a > 0.3, iterations=2))
    if n:
        sizes = ndimage.sum(a > 0.3, lab, index=np.arange(1, n + 1))
        for i, s in enumerate(sizes, 1):
            if s < 25:
                a[lab == i] = 0.0
    rgb = rgb * (a > 0)[..., None]
    out = np.dstack([rgb, a]).astype(np.float32)
    return (out, shadow) if drop_shadow else out


# ---------------------------------------------------------------- splitting
def valleys(profile, parts, min_run=4):
    """Cut positions between `parts` groups: the centre of the widest empty run (profile == 0) around each expected quarter line."""
    n = len(profile)
    empty = profile <= 0
    runs, start = [], None
    for i, e in enumerate(list(empty) + [False]):
        if e and start is None:
            start = i
        if not e and start is not None:
            runs.append((start, i)); start = None
    cuts = []
    for k in range(1, parts):
        target = n * k / parts
        cands = [(abs((s + e) / 2 - target) - (e - s) * 2, (s + e) // 2) for s, e in runs if e - s >= min_run and abs((s + e) / 2 - target) < n / parts * 0.45]
        if not cands:
            cuts.append(int(target))
            print("  warning: no empty gap near line %d of %d, using the plain split" % (k, parts))
        else:
            cuts.append(min(cands)[1])
    return [0] + cuts + [n]


def split_cells(rgba):
    """16 (y0, y1, x0, x1) boxes, row-major. Rows from the global y valleys, columns from each row's x valleys."""
    alpha = rgba[..., 3] > 0.2
    ys = valleys(alpha.sum(1), 4)
    boxes = []
    for r in range(4):
        y0, y1 = ys[r], ys[r + 1]
        xs = valleys(alpha[y0:y1].sum(0), 4)
        for c in range(4):
            boxes.append((y0, y1, xs[c], xs[c + 1]))
    return boxes


def extract(rgba, box, ground_mask=None):
    """Crop a cell and return (crop, bbox_of_all, body_cx) where body_cx is the x centre of the largest blob (the creature, not the swoosh lines).
    ground_mask (flyers): the removed baked shadow; its centre row in this cell is the ground, so "bottom" is that row instead of the body's lowest row."""
    y0, y1, x0, x1 = box
    cell = rgba[y0:y1, x0:x1]
    mask = cell[..., 3] > 0.3
    ys, xs = np.where(mask)
    if len(ys) == 0:
        raise ValueError("empty cell %s" % (box,))
    lab, n = ndimage.label(ndimage.binary_dilation(mask, iterations=3))
    sizes = ndimage.sum(mask, lab, index=np.arange(1, n + 1))
    main = lab == (1 + int(np.argmax(sizes)))
    mys, mxs = np.where(main & mask)
    crop = cell[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    ox, oy = xs.min(), ys.min()
    bottom = float(mys.max() + 1)
    if ground_mask is not None:
        gys = np.where(ground_mask[y0:y1, x0:x1])[0]
        if len(gys) < 60:
            raise ValueError("flyer cell %s has no baked shadow to anchor on" % (box,))
        bottom = max(bottom, float(np.median(gys)))
    return {
        "img": crop,
        "bottom": bottom,                 # main blob's lowest row (flyers: the ground under it), cell coordinates
        "main_cx": float((mxs.min() + mxs.max() + 1) / 2 - ox),  # in crop coordinates
        "main_h": float(mys.max() - mys.min() + 1),
        "ox": ox, "oy": oy,
        "w": crop.shape[1], "h": crop.shape[0],
    }


# ---------------------------------------------------------------- placement
def resize(img, scale):
    h, w = img.shape[:2]
    nw, nh = max(1, round(w * scale)), max(1, round(h * scale))
    pre = img.copy()
    pre[..., :3] *= pre[..., 3:4]  # premultiply, so the colour never bleeds from transparent pixels
    chans = [np.asarray(Image.fromarray(np.ascontiguousarray(pre[..., i]), "F").resize((nw, nh), Image.LANCZOS)) for i in range(4)]
    out = np.dstack(chans)
    out[..., 3] = np.clip(out[..., 3], 0, 1)
    out[..., :3] = np.clip(out[..., :3] / np.maximum(out[..., 3:4], 1e-3), 0, 1)
    out[..., :3] *= (out[..., 3:4] > 0.003)
    return out


def fit_scale(walk, atk, target_h, cell_w, cell_h):
    """One scale: median body height of the walk frames = target, reduced if any frame (walk or attack, swooshes included) would not fit its cell."""
    s = target_h / float(np.median([f["main_h"] for f in walk]))
    cap = s
    for frames, cw, ch in ((walk, cell_w, WALK_CELL), (atk, ATK_CELL, ATK_FEET)):
        for f in frames:
            cap = min(cap, (cw - 2 * MARGIN) / f["w"], (ch - MARGIN) / max(f["h"], f["bottom"] - f["oy"]))
    return s, cap


def place(frames, scale, cell, feet_y):
    """Canvas (cell x cell) per frame list index. Grounded frames sit on feet_y; airborne ones keep their lift over their row's ground."""
    out = []
    for r in range(4):
        row = frames[r * 4:(r + 1) * 4]
        ground = float(np.median([f["bottom"] for f in row]))
        for f in row:
            small = resize(f["img"], scale)
            lift = max(0.0, ground - f["bottom"]) * scale
            lift = lift if lift > 0.06 * f["main_h"] * scale else 0.0   # jitter of a few px is not a hop
            bottom_main = (f["bottom"] - f["oy"]) * scale                   # main blob bottom inside the resized crop
            top = feet_y - lift - bottom_main
            left = cell / 2 - f["main_cx"] * scale
            h, w = small.shape[:2]
            left = min(max(left, MARGIN), cell - MARGIN - w)                  # shift to fit, never clip
            top = min(max(top, 0), cell - h)
            canvas = np.zeros((cell, cell, 4), np.float32)
            ty, tx = int(round(top)), int(round(left))
            canvas[ty:ty + h, tx:tx + w] = small
            out.append(canvas)
    return out


def sheet_from(canvases, cell):
    sheet = np.zeros((4 * cell, 4 * cell, 4), np.float32)
    for i, c in enumerate(canvases):
        r, k = divmod(i, 4)
        sheet[r * cell:(r + 1) * cell, k * cell:(k + 1) * cell] = c
    return sheet


def to_image(arr):
    return Image.fromarray(np.round(arr * 255).astype(np.uint8), "RGBA")


# ---------------------------------------------------------------- previews / checks
def halo_count(arr):
    """Pixels that are visibly magenta-ish: opaque enough and min(R,B) well above G."""
    m = (arr[..., 3] > 0.05) & ((np.minimum(arr[..., 0], arr[..., 2]) - arr[..., 1]) > 0.2) & (arr[..., 0] > 0.55) & (arr[..., 2] > 0.45)
    return int(m.sum())


def on_bg(img, color, zoom=1):
    bg = Image.new("RGBA", img.size, color)
    bg.alpha_composite(img)
    return bg.resize((img.width * zoom, img.height * zoom), Image.NEAREST) if zoom != 1 else bg


def write_previews(mid, walk_img, atk_img):
    vis_dir = WORLD2_VIS_DIR if mid in WORLD2_DIRS else VIS_DIR
    os.makedirs(vis_dir, exist_ok=True)
    dark, light = (30, 32, 40, 255), (232, 236, 228, 255)
    parts = []
    for img in (walk_img, atk_img):
        parts.append(on_bg(img, dark)); parts.append(on_bg(img, light))
    w = walk_img.width + atk_img.width
    sheet = Image.new("RGBA", (w * 2 + 30, max(walk_img.height, atk_img.height) + 0), (90, 90, 90, 255))
    sheet.paste(parts[0], (0, 0)); sheet.paste(parts[2], (walk_img.width + 10, 0))
    sheet.paste(parts[1], (w + 20, 0)); sheet.paste(parts[3], (w + 20 + walk_img.width + 10, 0))
    sheet.convert("RGB").save(os.path.join(vis_dir, "meadow-%s-preview.png" % mid))


def hero_idle_down():
    p = os.path.join(ROOT, "Assets", "Resources", "Sprites", "classes", "warrior", "body", "bare", "idle.png")
    if not os.path.exists(p):
        return None
    im = Image.open(p).convert("RGBA")
    cell = im.height // 4 if im.width != im.height else im.height // 4
    c = Image.new("RGBA", (192, 192))
    c.paste(im.crop((0, 0, min(192, im.width), 192)))
    return c


def write_scale_compare(done):
    """In-scale line-up on grass green: every new monster (down and right, walk frame 0) next to frostSlime, iceGoblin and the hero."""
    items = []
    for name in ("frostSlime", "iceGoblin"):
        im = Image.open(os.path.join(OUT_DIR, name + ".png")).convert("RGBA")
        items.append((name, im.crop((0, 0, 192, 192)), im.crop((3 * 192, 0, 4 * 192, 192))))
    hero = hero_idle_down()
    if hero is not None:
        items.append(("hero", hero, None))
    for mid in done:
        im = Image.open(os.path.join(OUT_DIR, mid + ".png")).convert("RGBA")
        items.append((mid, im.crop((0, 0, 192, 192)), im.crop((3 * 192, 0, 4 * 192, 192))))
    W = sum(192 * (2 if r is not None else 1) for _, _, r in items) + 20
    sheet = Image.new("RGBA", (W, 240), (96, 140, 76, 255))
    d = ImageDraw.Draw(sheet)
    x = 10
    for name, down, right in items:
        for im in (down, right):
            if im is None:
                continue
            sheet.alpha_composite(im, (x, 20)); x += 192
        d.text((x - 192 * (2 if right is not None else 1) + 4, 4), name, fill=(255, 255, 255, 255))
    d.line([(0, 20 + 192), (W, 20 + 192)], fill=(255, 255, 0, 255))
    vis_dir = WORLD2_VIS_DIR if all(m in WORLD2_DIRS for m in done) else VIS_DIR   # a World Map 2.1 run must not overwrite the meadow line-up
    os.makedirs(vis_dir, exist_ok=True)
    sheet.convert("RGB").save(os.path.join(vis_dir, "meadow-scale-compare.png"))


# ---------------------------------------------------------------- main
def build(mid):
    stem, target_h, note = MONSTERS[mid]
    src_dir = FROST_DIR if mid in FROST_IDS else SRC_DIR
    if mid in WORLD2_DIRS:
        src_dir = os.path.join(ROOT, "Assets", "ArtLibrary", "monsters", WORLD2_DIRS[mid])
    wp = os.path.join(src_dir, stem + "-walk-chatgpt.png")
    ap = os.path.join(src_dir, stem + "-attack-chatgpt.png")
    if not (os.path.exists(wp) and os.path.exists(ap)):
        return False
    print("%s (%s)" % (mid, note))
    if mid in DROP_SHADOW:
        (wk, wsh), (ak, ash) = key_magenta(wp, True), key_magenta(ap, True)
        print("  baked shadow removed: walk %d px, attack %d px" % (int(wsh.sum()), int(ash.sum())))
        if mid not in FLYERS:
            wsh = ash = None
    else:
        wk, ak, wsh, ash = key_magenta(wp), key_magenta(ap), None, None
    walk = [extract(wk, b, wsh) for b in split_cells(wk)]
    atk = [extract(ak, b, ash) for b in split_cells(ak)]
    want, cap = fit_scale(walk, atk, target_h, WALK_CELL, ATK_CELL)
    scale = min(want, cap)
    med_h = np.median([f["main_h"] for f in walk]) * scale
    print("  scale %.3f (wanted %.3f, fit cap %.3f), median walk body height %.0f px" % (scale, want, cap, med_h))
    walk_arr = sheet_from(place(walk, scale, WALK_CELL, WALK_CELL), WALK_CELL)
    atk_arr = sheet_from(place(atk, scale, ATK_CELL, ATK_FEET), ATK_CELL)
    os.makedirs(OUT_DIR, exist_ok=True)
    wi, ai = to_image(walk_arr), to_image(atk_arr)
    wi.save(os.path.join(OUT_DIR, mid + ".png")); ai.save(os.path.join(OUT_DIR, mid + "-attack.png"))
    print("  magenta-ish pixels: walk %d, attack %d" % (halo_count(walk_arr), halo_count(atk_arr)))
    for name, arr, cell in (("walk", walk_arr, WALK_CELL), ("attack", atk_arr, ATK_CELL)):
        hs = []
        for r in range(4):
            hs.append([int((arr[r * cell:(r + 1) * cell, k * cell:(k + 1) * cell, 3] > 0.3).any(1).sum()) for k in range(4)])
        print("  %s opaque rows per cell: %s" % (name, hs))
    write_previews(mid, wi, ai)
    return True


if __name__ == "__main__":
    ids = sys.argv[1:] or list(MONSTERS)
    done = [m for m in ids if build(m)]
    if not done:
        print("no monster had all four source files")
        sys.exit(1)
    write_scale_compare(done)

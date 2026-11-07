"""Preview only, writes to OUT (not game files): Warrior looks with a personal hair colour and an armour look driven by the equipped armour.
  python Tools/preview-warrior-hair-armor-recolor.py masks              mask overlay of a few frames (hair red, cloth orange, metal cyan, gold yellow)
  python Tools/preview-warrior-hair-armor-recolor.py lineup             the 5 armour looks side by side, idle down + walk right
  python Tools/preview-warrior-hair-armor-recolor.py check HAIR ARMOR   one look over idle / walk / melee1 in 4 directions, to spot leaks
  python Tools/preview-warrior-hair-armor-recolor.py crowd BG.png       35 warriors at game scale in three level bands
  python Tools/preview-warrior-hair-armor-recolor.py pauldrons HAIR ARMOR  dragon pauldron overlay test over idle / walk, 4 directions
Frames are composed the way the game draws them (Tools/compose-hero-frame.py: body/bare + weapon overlays + hand layer).
Masks per 192 px cell: red pixels split into hair (+ red eyes) and the rest (eroded red cores classed by the head band, top 47% of the figure,
every red pixel takes the class of the nearest core); of the rest, bright saturated red = cloth (cape, scarf, shirt); dark / greyish non-skin,
non-fur, non-gold pixels = armour metal and leather. Recolour keeps the shading: hue replaced, saturation and value scaled."""
import sys, os, importlib
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = "D:/zenoniaClone"
sys.path.insert(0, ROOT + "/Tools")
os.chdir(ROOT)
chf = importlib.import_module("compose-hero-frame")

OUT = "C:/temp/recolor-preview"
os.makedirs(OUT, exist_ok=True)
BARE = "Assets/Resources/Sprites/classes/warrior/body/bare/{}.png"

# Personal choice at character creation. (name, hue deg or None = keep, sat factor, value factor)
HAIR = [("red", None, 1, 1), ("charcoal", 222, 0.3, 0.55), ("brown", 22, 0.75, 0.62), ("silver", 210, 0.08, 1.25),
        ("blonde", 44, 0.7, 1.12), ("blue", 214, 0.85, 0.85), ("purple", 278, 0.7, 0.8), ("jade", 160, 0.48, 0.7)]

# Armour looks: comes ONLY from the equipped armour (two of the 10 ItemDefs armour tiers per look).
# cloth / metal: (hue, sat factor, value factor) or None = keep; metal sat factor < 0 means "set saturation to -x".
ARMOR = [
    ("rookie",   "Leather Vest / Steel Plate",       (28, 0.5, 0.62),  None),
    ("frost",    "Bronze Plate / Frost Plate",       (205, 0.55, 0.95), (210, -0.10, 1.45)),
    ("crimson",  "Flame Plate / Royal Plate",        None,              (225, -0.08, 0.75)),
    ("arcane",   "Verdant Armor / Arcane Armor",     (272, 0.62, 0.72), (262, -0.16, 1.15)),
    ("dragon",   "Celestial Armor / Dragon Armor",   (352, 0.95, 0.42), (42, -0.62, 1.25)),
]


def hsv(rgb):
    """Vectorised RGB (0..1) -> H (deg), S, V."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    v = rgb.max(-1); mn = rgb.min(-1); d = v - mn
    s = np.where(v > 0, d / np.maximum(v, 1e-6), 0)
    dd = np.maximum(d, 1e-6)
    h = np.where(v == r, (g - b) / dd % 6, np.where(v == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60
    return np.where(d > 0, h, 0), s, v


def rgb_from(h, s, v):
    h = (h % 360) / 60; i = np.floor(h).astype(int) % 6; f = h - np.floor(h)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    sel = [np.stack(c, -1) for c in ((v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q))]
    out = np.zeros(h.shape + (3,))
    for k in range(6): out[i == k] = sel[k][i == k]
    return out


def masks(cell):
    """hair, cloth, metal, gold masks for one 192x192 RGBA cell."""
    a = np.asarray(cell)
    al = a[..., 3] > 20
    rgb = a[..., :3].astype(np.float32) / 255
    h, s, v = hsv(rgb)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    red = al & (r > 55 / 255) & (r > 1.45 * np.maximum(g, b))
    hair = np.zeros_like(al)
    ys = np.where(al)[0]
    if len(ys) and red.any():
        top, bot = ys.min(), ys.max()
        above = np.arange(a.shape[0])[:, None] < top + 0.47 * (bot - top)
        core = ndimage.binary_erosion(red, iterations=2)
        lab, n = ndimage.label(core, structure=np.ones((3, 3)))
        if n == 0: lab, n = ndimage.label(red, structure=np.ones((3, 3)))
        is_hair = np.zeros(n + 1, bool)
        for i in range(1, n + 1):
            comp = lab == i
            is_hair[i] = (comp & above).sum() / comp.sum() > 0.5
        _, (iy, ix) = ndimage.distance_transform_edt(lab == 0, return_indices=True)
        hair = red & is_hair[lab[iy, ix]]
        warm = al & (r > 110 / 255) & (r > 1.15 * np.maximum(g, b)) & ~red   # orange highlights at the hair edge
        hair |= warm & ndimage.binary_dilation(hair, iterations=2) & above
        # every red pixel inside the face box (skin of the head band) goes with the hair: the eyes sometimes touch the bangs and
        # sometimes stand alone, and classing them per frame made them flicker between hair and cape colour
        face = al & above & (h > 5) & (h < 40) & (s > 0.12) & (s < 0.6) & (v > 0.75)
        if face.any():
            fy, fx = np.where(face)
            box = np.zeros_like(al); box[fy.min():fy.max() + 1, fx.min():fx.max() + 1] = True
            hair |= red & box
    reddish = (h < 20) | (h > 340)
    cloth = al & ~hair & reddish & (s > 0.6) & (v > 0.42)
    # the cape's dark folds are still strongly red: grow the cloth into them (armour plates are browner, less saturated)
    cloth = ndimage.binary_propagation(cloth, mask=al & ~hair & reddish & (s > 0.62) & (v > 0.12))
    gold = al & ~hair & (h > 28) & (h < 60) & (s > 0.45) & (v > 0.45)
    skin = al & (h > 5) & (h < 40) & (s > 0.12) & (s < 0.6) & (v > 0.75)
    fur = al & (v > 0.75) & (s < 0.25)
    metal = al & ~hair & ~cloth & ~gold & ~skin & ~fur & (v > 0.13) & (v < 0.75)
    return hair, cloth, metal, gold


def apply(a, mask, spec):
    if spec is None or spec[0] is None or not mask.any(): return
    hue, sf, vf = spec
    rgb = a[mask][:, :3].astype(np.float32) / 255
    _, s, v = hsv(rgb)
    s2 = np.full_like(s, -sf) if sf < 0 else np.minimum(1, s * sf)
    out = rgb_from(np.full_like(s, hue), s2, np.minimum(1, v * vf))
    a[mask, :3] = (out * 255).round().astype(np.uint8)


_anchors = chf.load_gear("warrior")[1]
DIRS = {v: k for k, v in chf.ROWS.items()}


def slot_masks(metal, anim, row, frame):
    """Splits the armour metal of one cell into body, gloves (around the sword and shield hands, from the gear anchors) and
    boots (lowest 24% of the figure)."""
    ys = np.where(metal)[0]
    gloves = np.zeros_like(metal); boots = np.zeros_like(metal)
    if len(ys) == 0: return metal, gloves, boots
    top, bot = ys.min(), ys.max()
    boots = metal & (np.arange(192)[:, None] > bot - 0.24 * (bot - top))
    yy, xx = np.mgrid[0:192, 0:192]
    for part in ("sword", "shield"):
        frames = _anchors.get((part, anim, DIRS[row])) or []
        a = frames[frame] if frame < len(frames) else None
        if a: gloves |= metal & ((xx - a["x"]) ** 2 + (yy - a["y"]) ** 2 < 15 ** 2)
    gloves &= ~boots
    return metal & ~gloves & ~boots, gloves, boots


_cache = {}
def sheet_variant(anim, hair, armor, gloves=None, boots=None):
    """Body sheet recoloured: hair (personal), cloth + body metal from the armour, gloves and boots metal from their own looks."""
    gloves = gloves or armor
    boots = boots or armor
    key = (anim, hair[0], armor[0], gloves[0], boots[0])
    if key in _cache: return _cache[key]
    a = np.array(Image.open(BARE.format(anim)).convert("RGBA"))
    for y in range(0, a.shape[0], 192):
        for x in range(0, a.shape[1], 192):
            sub = a[y:y + 192, x:x + 192].copy()
            hm, cm, mm, _ = masks(sub)
            body, gm, bm = slot_masks(mm, anim, y // 192, x // 192)
            apply(sub, hm, hair[1:]); apply(sub, cm, armor[2]); apply(sub, body, armor[3])
            apply(sub, gm, gloves[3]); apply(sub, bm, boots[3])
            a[y:y + 192, x:x + 192] = sub
    path = OUT + "/bare-" + "-".join(key) + ".png"
    Image.fromarray(a).save(path)
    _cache[key] = path
    return path


PIECE_DIR = "Assets/ArtLibrary/classes/warrior/armor-src/{}-{}.png"   # set, piece: dragon-pauldron-front-l, steel-chest-front ...
PAULDRON_W = {"front": 38, "side": 38, "back": 38}   # px in the 192 cell (dragon: the horn makes the art wider)
KIT_SCALE = {"dragon": 1.0, "steel": 0.74}           # pauldron width factor per kit (steel domes have no horn)
_pieces = {}


def pauldron(name, width, kit="dragon"):
    return piece(kit, "pauldron-" + name, width)


def piece(kit, name, width):
    key = (kit, name, width)
    if key not in _pieces:
        im = Image.open(PIECE_DIR.format(kit, name)).convert("RGBA")
        _pieces[key] = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    return _pieces[key]


def fur_collar(cell):
    """Largest white fur blob below the face: (mask, rows, cols) or None."""
    a = np.asarray(cell)
    al = a[..., 3] > 20
    _, s, v = hsv(a[..., :3].astype(np.float32) / 255)
    fur = al & (v > 0.72) & (s < 0.25)
    lab, n = ndimage.label(fur, structure=np.ones((3, 3)))
    if n == 0: return None
    sizes = ndimage.sum(fur, lab, range(1, n + 1))
    big = lab == (int(np.argmax(sizes)) + 1)
    by = np.where(big)[0]
    # the scarf splits the collar into two halves in front: keep every sizeable fur blob in the rows of the biggest one
    m = np.zeros_like(fur)
    for i in range(1, n + 1):
        comp = lab == i
        cy = np.where(comp)[0].mean()
        if sizes[i - 1] >= 0.25 * sizes.max() and by.min() - 4 <= cy <= by.max() + 4: m |= comp
    ys, xs = np.where(m)
    return m, ys, xs


CHEST_W = {"front": 0.66, "side": 0.5}   # chest plate width as a share of the fur collar width


def pauldron_layer(anim, d, frame, kit="dragon"):
    """192 px RGBA layer with an armour kit's pieces placed on this frame (read from the original bare sheet): the chest plate under the
    fur collar (front and side views; the cape covers the back) and the pauldrons on the collar ends. kits: dragon (pauldrons), steel."""
    row = chf.ROWS[d]
    cell = Image.open(BARE.format(anim)).convert("RGBA").crop((frame * 192, row * 192, frame * 192 + 192, row * 192 + 192))
    layer = Image.new("RGBA", (192, 192), (0, 0, 0, 0))
    fc = fur_collar(cell)
    if fc is None: return layer
    _, ys, xs = fc
    has_chest = os.path.exists(PIECE_DIR.format(kit, "chest-front"))

    def put(p, cx, cy, ay=0.7):   # (cx, cy) = where the piece's anchor goes: 50% across, ay down the piece
        layer.alpha_composite(p, (int(round(cx - p.width * 0.5)), int(round(cy - p.height * ay))))

    x0, x1 = xs.min(), xs.max()
    if d in ("down", "up"):
        view = "front" if d == "down" else "back"
        if has_chest and d == "down":
            put(piece(kit, "chest-front", int((x1 - x0) * CHEST_W["front"])), (x0 + x1) / 2, ys.max() - 6, 0.0)
        yl = ys[xs <= x0 + 3].mean(); yr = ys[xs >= x1 - 3].mean()
        w = int(PAULDRON_W[view] * KIT_SCALE.get(kit, 1.0))
        put(pauldron(view + "-l", w, kit), x0 + 4, yl + 10)
        put(pauldron(view + "-r", w, kit), x1 - 4, yr + 10)
    else:
        sign = 1 if d == "right" else -1
        if has_chest:
            c = piece(kit, "chest-side", int((x1 - x0) * CHEST_W["side"]))
            if d == "left": c = c.transpose(Image.FLIP_LEFT_RIGHT)
            put(c, xs.mean() + sign * 3, ys.max() - 6, 0.0)
        p = pauldron("side", int(PAULDRON_W["side"] * KIT_SCALE.get(kit, 1.0)), kit)
        if d == "left": p = p.transpose(Image.FLIP_LEFT_RIGHT)
        back = 0 if kit == "dragon" else 5   # the shoulder sits behind the chin: shift the smaller domes toward the back
        put(p, xs.mean() - sign * back, ys.max() + 2)
    return layer


def kit_check(kit, anims=("idle", "walk")):
    """One armour kit over the given anims, 4 directions x 4 frames, plus a 2x zoom of the first frame of each row."""
    rows = []
    for anim in anims:
        for d in ("down", "up", "left", "right"):
            rows.append([chf.compose("warrior", anim, d, k, pad=40, over=pauldron_layer(anim, d, k, kit)) for k in range(4)])
    S = rows[0][0].size[0]
    im = Image.new("RGBA", (S * 4, S * len(rows)), (58, 62, 70, 255))
    for r, fr in enumerate(rows):
        for k, f in enumerate(fr): im.alpha_composite(f, (k * S, r * S))
    im.save(OUT + "/kit-check-" + kit + ".png")


HAND = "Assets/Resources/Sprites/classes/warrior/body/hand/{}.png"


def hand_variant(anim, gloves):
    """The fist layer drawn over the grip is glove too: recolour its metal / leather pixels with the gloves look."""
    path = HAND.format(anim)
    if not os.path.exists(path) or gloves[3] is None: return None
    key = ("hand", anim, gloves[0])
    if key in _cache: return _cache[key]
    a = np.array(Image.open(path).convert("RGBA"))
    al = a[..., 3] > 20
    h, s, v = hsv(a[..., :3].astype(np.float32) / 255)
    skin = (h > 5) & (h < 40) & (s > 0.12) & (s < 0.6) & (v > 0.75)
    fur = (v > 0.75) & (s < 0.25)
    apply(a, al & ~skin & ~fur & (v > 0.13), gloves[3])
    out = OUT + "/hand-" + anim + "-" + gloves[0] + ".png"
    Image.fromarray(a).save(out)
    _cache[key] = out
    return out


def hero(anim, d, frame, hair, armor, tier=0, pad=40, pauldrons=None, gloves=None, boots=None):
    """The pauldrons are part of the dragon armour (idle / walk only: attack poses have no shoulder placement yet)."""
    if pauldrons is None: pauldrons = armor[0] == "dragon" and anim in ("idle", "walk")
    over = pauldron_layer(anim, d, frame) if pauldrons else None
    return chf.compose("warrior", anim, d, frame, bare_path=sheet_variant(anim, hair, armor, gloves, boots), tier=tier, pad=pad, over=over,
                       hand_path=hand_variant(anim, gloves or armor))


def mix_lineup():
    """Armour, gloves and boots picked separately: a few sets side by side, idle down and walk right."""
    S = 272
    sets = [(4, 4, 4), (4, 1, 1), (4, 2, 3), (1, 4, 2), (2, 3, 4), (3, 1, 0)]
    im = Image.new("RGBA", (S * len(sets), S * 2 + 40), (58, 62, 70, 255))
    dr = ImageDraw.Draw(im)
    for i, (ai, gi, bi) in enumerate(sets):
        for r, (anim, d, fr) in enumerate((("idle", "down", 0), ("walk", "right", 1))):
            im.alpha_composite(hero(anim, d, fr, HAIR[0], ARMOR[ai], gloves=ARMOR[gi], boots=ARMOR[bi]), (i * S, 30 + r * S))
        dr.text((i * S + 8, 8), "A " + ARMOR[ai][0] + " / G " + ARMOR[gi][0] + " / B " + ARMOR[bi][0], fill=(235, 235, 235, 255))
    im = im.resize((int(im.width * 1.5), int(im.height * 1.5)), Image.LANCZOS)
    im.save(OUT + "/armor-mix-lineup.png")


def mixed(bg_path, seed=11):
    """Like crowd, but armour, gloves and boots each picked on their own from the band's looks (players mix pieces)."""
    rng = np.random.default_rng(seed)
    bg = Image.open(bg_path).convert("RGBA")
    W, Hh = bg.size
    spots = [(x + int(rng.integers(-18, 19)), y + int(rng.integers(-12, 13))) for y in range(130, Hh - 10, 78) for x in range(70, W - 40, 100)]
    poses = [("idle", "down"), ("walk", "right"), ("walk", "left"), ("idle", "down"), ("walk", "down"), ("melee1", "right"), ("walk", "up")]
    base = [(int(rng.integers(len(poses))), int(rng.integers(4)), HAIR[rng.integers(len(HAIR))]) for _ in spots]
    bands = [("Lv1", [0], [0]), ("Lv10-40", [0, 1, 1, 2], [1, 2, 3, 4, 5]), ("Lv40-60", [2, 3, 3, 4], [5, 6, 7, 8, 9])]
    out = Image.new("RGBA", (W * 3 + 40, Hh), (0, 0, 0, 255))
    for side, (label, looks, tiers) in enumerate(bands):
        canvas = bg.copy()
        pick = lambda: ARMOR[looks[rng.integers(len(looks))]]
        picks = [(xy, b, pick(), pick(), pick(), tiers[rng.integers(len(tiers))]) for xy, b in zip(spots, base)]
        for (x, y), (pi, fr, h), arm, gl, bo, tier in sorted(picks, key=lambda t: t[0][1]):
            anim, d = poses[pi]
            f = hero(anim, d, fr, h, arm, tier, gloves=gl, boots=bo)
            f = f.resize((f.width // 2, f.height // 2), Image.LANCZOS)
            canvas.alpha_composite(f, (x - f.width // 2, y - int(f.height * 0.78)))
        dr = ImageDraw.Draw(canvas)
        dr.rectangle((0, 0, 90, 22), fill=(0, 0, 0, 170)); dr.text((8, 5), label, fill=(255, 255, 255, 255))
        out.alpha_composite(canvas, (side * (W + 20), 0))
    out.save(OUT + "/armor-mixed-crowd-by-level.png")


def pauldron_check(h, arm):
    """Dragon look with pauldrons over idle and walk, 4 directions x 4 frames."""
    rows = [[hero(anim, d, k, h, arm, pauldrons=True) for k in range(4)] for anim in ("idle", "walk") for d in ("down", "up", "left", "right")]
    S = rows[0][0].size[0]
    im = Image.new("RGBA", (S * 4, S * len(rows)), (58, 62, 70, 255))
    for r, fr in enumerate(rows):
        for k, f in enumerate(fr): im.alpha_composite(f, (k * S, r * S))
    im.save(f"{OUT}/pauldron-check-{h[0]}-{arm[0]}.png")


def mask_view():
    rows = []
    for anim, row, fr in (("idle", 0, 0), ("walk", 3, 1), ("melee1", 2, 0), ("walk", 1, 2)):
        a = np.array(Image.open(BARE.format(anim)).convert("RGBA"))[row * 192:row * 192 + 192, fr * 192:fr * 192 + 192]
        hm, cm, mm, gm = masks(a)
        vis = (a[..., :3] * 0.3).astype(np.uint8) * (a[..., 3:] > 20)
        for m, c in ((mm, (0, 200, 255)), (gm, (255, 220, 0)), (cm, (255, 120, 0)), (hm, (255, 0, 0))): vis[m] = c
        rows.append(np.concatenate([(a[..., :3] * (a[..., 3:] > 20)).astype(np.uint8), vis], 1))
    Image.fromarray(np.concatenate(rows, 0)).resize((768, 384 * len(rows)), Image.NEAREST).save(f"{OUT}/armor-masks.png")


def lineup():
    """5 armour looks + dragon with pauldrons (same red hair, starter sword), idle down and walk right, 1.5x, with labels."""
    S = 272
    cols = [(arm, None) for arm in ARMOR]
    im = Image.new("RGBA", (S * len(cols), S * 2 + 40), (58, 62, 70, 255))
    dr = ImageDraw.Draw(im)
    for i, (arm, pl) in enumerate(cols):
        im.alpha_composite(hero("idle", "down", 0, HAIR[0], arm, pauldrons=pl), (i * S, 30))
        im.alpha_composite(hero("walk", "right", 1, HAIR[0], arm, pauldrons=pl), (i * S, S + 30))
        dr.text((i * S + 12, 8), arm[0] + (" + pauldrons" if pl else "") + ("" if pl else ": " + arm[1]), fill=(235, 235, 235, 255))
    im = im.resize((int(im.width * 1.5), int(im.height * 1.5)), Image.LANCZOS)
    im.save(f"{OUT}/armor-lineup.png")


def consistency(h, arm):
    rows = []
    for anim in ("idle", "walk", "melee1"):
        for d in ("down", "up", "left", "right"):
            rows.append([hero(anim, d, k, h, arm) for k in range(4)])
    S = rows[0][0].size[0]
    im = Image.new("RGBA", (S * 4, S * len(rows)), (58, 62, 70, 255))
    for r, fr in enumerate(rows):
        for k, f in enumerate(fr): im.alpha_composite(f, (k * S, r * S))
    im.save(f"{OUT}/armor-check-{h[0]}-{arm[0]}.png")


def crowd(bg_path, seed=5):
    """Same 35 spots in three level bands; hair always random (personal), armour and weapon from gear that fits the band:
    Lv1 all rookie armour + starter weapon; Lv10-40 rookie / frost / crimson, weapon tiers 1-5; Lv40-60 crimson / arcane / dragon, tiers 5-9."""
    rng = np.random.default_rng(seed)
    bg = Image.open(bg_path).convert("RGBA")
    W, Hh = bg.size
    scale = 0.5
    spots = [(x + int(rng.integers(-18, 19)), y + int(rng.integers(-12, 13))) for y in range(130, Hh - 10, 78) for x in range(70, W - 40, 100)]
    poses = [("idle", "down"), ("walk", "right"), ("walk", "left"), ("idle", "down"), ("walk", "down"), ("melee1", "right"), ("walk", "up")]
    base = [(int(rng.integers(len(poses))), int(rng.integers(4)), HAIR[rng.integers(len(HAIR))]) for _ in spots]
    bands = [("Lv1", [0], [0]), ("Lv10-40", [0, 1, 1, 2], [1, 2, 3, 4, 5]), ("Lv40-60", [2, 3, 3, 4], [5, 6, 7, 8, 9])]
    out = Image.new("RGBA", (W * 3 + 40, Hh), (0, 0, 0, 255))
    for side, (label, looks, tiers) in enumerate(bands):
        canvas = bg.copy()
        picks = [(xy, b, ARMOR[looks[rng.integers(len(looks))]], tiers[rng.integers(len(tiers))]) for xy, b in zip(spots, base)]
        for (x, y), (pi, fr, h), arm, tier in sorted(picks, key=lambda t: t[0][1]):
            anim, d = poses[pi]
            f = hero(anim, d, fr, h, arm, tier)
            f = f.resize((int(f.width * scale), int(f.height * scale)), Image.LANCZOS)
            canvas.alpha_composite(f, (x - f.width // 2, y - int(f.height * 0.78)))
        dr = ImageDraw.Draw(canvas)
        dr.rectangle((0, 0, 90, 22), fill=(0, 0, 0, 170)); dr.text((8, 5), label, fill=(255, 255, 255, 255))
        out.alpha_composite(canvas, (side * (W + 20), 0))
    out.save(f"{OUT}/armor-crowd-by-level.png")


if __name__ == "__main__":
    what = sys.argv[1]
    if what == "masks": mask_view()
    elif what == "lineup": lineup()
    elif what == "check": consistency(HAIR[int(sys.argv[2])], ARMOR[int(sys.argv[3])])
    elif what == "crowd": crowd(sys.argv[2])
    elif what == "mixed": mixed(sys.argv[2])
    elif what == "mix-lineup": mix_lineup()
    elif what == "kit": kit_check(sys.argv[2], tuple(sys.argv[3].split(",")) if len(sys.argv) > 3 else ("idle", "walk"))
    elif what == "pauldrons": pauldron_check(HAIR[int(sys.argv[2])], ARMOR[int(sys.argv[3])])

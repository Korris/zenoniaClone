"""Mechanic in the new style: one gun in the right hand, the other hand free (look approved in Assets/ArtLibrary/custom/idle-down-set/mechanic-one-gun-src.png).
Sources: Assets/ArtLibrary/classes/mechanic/new-style/src/, ChatGPT art on flat #00FF00 or #FF00FF: <anim>-<dir>.png one row of frames per image, gun-<tier>-<kind>.png, turret-<tier>-<kind>.png.
Old sheets, art and anchors: new-style/old/. Gauntlet versions that were rejected: new-style/rejected-gauntlet/ (never installed).
dash keeps the old sheets (brief), death its baked art, idle down (row 0) is installed separately and is not touched here.
Replaces Tools/build-mechanic-gun.py for every anim but dash, whose old sheets and anchors stay. Do not run that tool again.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-mechanic-new-style.py measure            scale and head measurements of every source row
  python Tools/build-mechanic-new-style.py cut OUT_DIR        frames with a pixel grid, for reading fist positions; Resources untouched
  python Tools/build-mechanic-new-style.py preview OUT_DIR    sheets and anchors as copies, composited the way the game draws; Resources untouched
  python Tools/build-mechanic-new-style.py write              bare, armed, hand sheets (idle row 0 kept) and the gun anchors of these anims in held-mechanic.json
  python Tools/build-mechanic-new-style.py gun [tier...]      cut gun art to weapons/gun, update the grips in held-mechanic.json; only tiers with both images
  python Tools/build-mechanic-new-style.py turret [tier...]   the same for companion/art and the companion grips
  python Tools/build-mechanic-new-style.py icons              rebuild only the weapon- and offhand-mechanic-<tier> icons, as Tools/build-item-icons.py does
"""
import importlib.util, json, math, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

spec = importlib.util.spec_from_file_location("warrior_move", "Tools/build-warrior-move.py")
wm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wm)

FRAME, HEIGHT, SOLE_ROW = 192, 164, 188  # the approved idle down row stands from row 25 to row 188
SRC = "Assets/ArtLibrary/classes/mechanic/new-style/src"
REF = "Assets/ArtLibrary/custom/idle-down-set/mechanic-one-gun-src.png"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
DIRS = ["down", "up", "left", "right"]
# (anim, dir): (source file, painted frames). attack2 left is the mirror of its own painting: there the extended arm is the far one, the gun hand.
PAINTED = {
    ("idle", "up"): ("idle-up", 1), ("idle", "right"): ("idle-right", 1),
    ("walk", "down"): ("walk-down", 4), ("walk", "up"): ("walk-up", 4), ("walk", "right"): ("walk-right", 4),
    ("hurt", "down"): ("hurt-down", 2), ("hurt", "up"): ("hurt-up", 2), ("hurt", "right"): ("hurt-right", 2),
    ("attack1", "down"): ("attack1-down", 4), ("attack1", "up"): ("attack1-up", 4), ("attack1", "left"): ("attack1-left", 4), ("attack1", "right"): ("attack1-right", 4),
    ("attack2", "down"): ("attack2-down", 4), ("attack2", "up"): ("attack2-up", 4), ("attack2", "right"): ("attack2-right", 4), ("attack2", "mirror-left"): ("attack2-left-mirror-of-this", 4),
    ("attack3", "down"): ("attack3-down", 6), ("attack3", "up"): ("attack3-up", 6), ("attack3", "right"): ("attack3-right", 6),
}
HEAD_X = 96
COPY = {("attack1", "down", 0): 3}  # (anim, dir, frame): frame to reuse; ChatGPT dropped the raised fist in attack1 down frame 0, which the old row has as frame 3

# Gun: the right hand always. Row: (fist x, y, aim, flip, behind), as in build-mechanic-gun.py; aim and flip kept from its table where the pose exists.
F, B = False, True
GUN_SCALE, FIST_R, FEATHER = 0.23, 10.0, 1.0
NEW_FIST = {
    ("idle", "up"): [(135, 91, -63, 1, B)] * 4,
    ("idle", "right"): [(89, 143, 35, 1, F)] * 4,  # new design: low ready, muzzle forward and down; raised it crossed the chest
    ("idle", "left"): [(64, 137, 145, -1, B)] * 4,  # the far fist of the mirrored painting
    ("walk", "down"): [(73, 136, 122, -1, F), (65, 140, 108, -1, F), (73, 132, 124, -1, F), (65, 140, 110, -1, F)],
    ("walk", "up"): [(134, 140, 55, 1, B), (125, 132, 75, 1, B), (132, 136, 54, 1, B), (125, 134, 75, 1, B)],
    ("walk", "right"): [(67, 144, 120, 1, F), (104, 148, 62, 1, F), (127, 132.5, 39, 1, F), (107.5, 150, 62, 1, F)],
    ("walk", "left"): [(58, 127, 155, -1, B), (71, 138, 135, -1, B), (117, 146, 135, -1, B), (71, 148, 130, -1, B)],  # far fists of the mirrored painting
    ("hurt", "down"): [(53, 103, -135, -1, F), (86, 170, 145, -1, F)],  # frame 1 was on the other hand in the old table
    ("hurt", "up"): [(53, 110, -125, -1, B), (128, 138, 70, 1, B)],
    ("hurt", "right"): [(55, 93, -128, -1, F), (105, 147, 25, 1, F)],
    ("attack1", "down"): [(69, 108, 180, -1, F), (66, 108, 180, -1, F), (54, 103, 160, -1, F), (70, 108, 180, -1, F)],
    ("attack1", "up"): [(140, 102, -20, 1, B), (141, 103, -20, 1, B), (140, 103, -23, 1, B), (141, 98, -20, 1, B)],
    ("attack1", "left"): [(45, 111, 180, -1, B), (41.5, 111, 180, -1, B), (48, 116, -165, -1, B), (46, 111, 180, -1, B)],
    ("attack1", "right"): [(152, 109, -12, 1, F), (151, 109, -20, 1, F), (150.5, 114, 6, 1, F), (150.5, 109, -12, 1, F)],
    ("attack2", "down"): [(54, 101, 180, -1, F), (54, 101, 180, -1, F), (52, 101, 175, -1, F), (56, 101, 180, -1, F)],  # no old anchors: the old row fired from the gauntlet, now the gun hand is the extended arm
    ("attack2", "up"): [(136, 91, -20, 1, B), (136, 91, -20, 1, B), (137, 91, -23, 1, B), (141, 92, -20, 1, B)],
    ("attack2", "left"): [(50, 103, 180, -1, B), (50, 103, 180, -1, B), (47, 103, -175, -1, B), (45, 103, 180, -1, B)],
    ("attack2", "right"): [(151, 107.5, 0, 1, F), (151, 107.5, 0, 1, F), (159, 113, -5, 1, F), (152, 109, 0, 1, F)],
    ("attack3", "down"): [(98, 121, 5, 1, F), (94.5, 121, 5, 1, F), (102.5, 134, 5, 1, F), (78, 105, 175, -1, F), (99, 128, 175, -1, F), (94, 121, 5, 1, F)],
    ("attack3", "up"): [(137, 107, -5, 1, B), (136.5, 104, -5, 1, B), (137, 102.5, -5, 1, B), (138, 105, 0, 1, B), (53, 108, -170, -1, B), (134, 107, -10, 1, B)],
    ("attack3", "right"): [(137, 119, -5, 1, F), (136.5, 119, -5, 1, F), (137, 120, -5, 1, F), (138, 105, 0, 1, F), (131, 120, -15, 1, F), (131, 119, -5, 1, F)],
}
MIRRORED = {"attack3", "hurt"}  # HeroClassDef.MirrorLeft: the game flips the right row, so the left anchors are the right ones mirrored
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/mechanic/body"
GEAR = f"{RES}/Data/classes/mechanic/held-mechanic.json"
COUNT = {"idle": 4, "walk": 4, "hurt": 2, "attack1": 4, "attack2": 4, "attack3": 6}

# Guns: same silhouette in every tier. The first painting sets the canvas and the grip.
GUN_DIR = f"{RES}/Sprites/classes/mechanic/weapons/gun"
GUN_LENGTH = 258  # art px rear to muzzle, as the old art: same length on screen at s = 0.23
MASTER = "gun-scrap-base"
MASTER_GRIP = (262, 680)  # middle of the handle in the master painting, read at full size
PAD = 4
# Turret: stands on the ground, about 40% of hero height on screen at CompanionScale 0.26. Grip = centre of the base at the feet row (HeroCompanion, CompanionGrounded).
TURRET_DIR = f"{RES}/Sprites/classes/mechanic/companion/art"
TURRET_SAMPLE = "Assets/ArtLibrary/classes/mechanic/turret-src/turret-sample.png"
COMPANION_SCALE = 0.26
TURRET_HEIGHT = round(0.40 * HEIGHT / COMPANION_SCALE)


def hair_mask(pm):
    """Strong yellow of the hair, on premultiplied RGBA."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g, b = [pm[..., i] / a[..., 0] for i in range(3)]
    return (r > 190) & (g > 130) & (b < 150) & (r - b > 70) & (pm[..., 3] > 128)


def hair_size(pm):
    """sqrt of the hair area: scales with the art and changes little with the view, unlike height in a crouch."""
    return float(np.sqrt(hair_mask(pm).sum()))


def painted(name, count):
    pm = wm.key_green(np.array(Image.open(f"{SRC}/{name}.png").convert("RGB")))
    return [wm.trimmed(pm[:, a:b]) for a, b in wm.split(pm[..., 3], count)]


def hair_target():
    ref = wm.key_green(np.array(Image.open(REF).convert("RGB")))
    ref[:, 850:] = 0  # the robot floats on the right of the reference
    ref = wm.trimmed(ref)
    return hair_size(ref) * HEIGHT / ref.shape[0]


def measure():
    target = hair_target()
    print(f"hair size at {HEIGHT} px tall: {target:.1f}")
    for key, (name, count) in PAINTED.items():
        if not os.path.exists(f"{SRC}/{name}.png"):
            print(key, "no source yet")
            continue
        frames = painted(name, count)
        hs = [hair_size(f) for f in frames]
        hmax = max(f.shape[0] for f in frames)
        print(f"{key[0]:8s}{key[1]:12s} frames {len(frames)} heights {[f.shape[0] for f in frames]} widths {[f.shape[1] for f in frames]} "
              f"k_by_height {HEIGHT / hmax:.3f} k_by_hair {target / np.median(hs):.3f}")


def row_scale(key, frames, target):
    """Standing rows take 164 px tall. Back views paint the hair larger, so they split the difference with the head measure. Hurt crouches: head measure only."""
    by_height = HEIGHT / max(f.shape[0] for f in frames)
    by_hair = target / float(np.median([hair_size(f) for f in frames]))
    if key[0] == "hurt":
        return by_hair
    if key[1] == "up" and key[0] != "idle":
        return (by_height + by_hair) / 2
    return by_height


def hair_center(pm):
    xs = np.where(hair_mask(pm).sum(0) >= 2)[0]
    return (int(xs[0]) + int(xs[-1]) + 1) // 2


def place(f, base_x):
    """One frame on its 192 cell: sole on SOLE_ROW. Returns the cell and whether art was lost past the cell."""
    cell = np.zeros((FRAME, FRAME, 4))
    y = SOLE_ROW + 1 - f.shape[0]
    x0, y0 = max(0, -base_x), max(0, -y)
    ex, ey = min(FRAME, base_x + f.shape[1]), min(FRAME, y + f.shape[0])
    cell[y + y0:ey, base_x + x0:ex] = f[y0:y0 + ey - y - y0, x0:x0 + ex - base_x - x0]
    return cell, base_x < 0 or base_x + f.shape[1] > FRAME or y < 0


def row_cells(key):
    """Premultiplied 192 cells of one painted row; the head stays on one column so turning does not jump."""
    name, count = PAINTED[key]
    raw = painted(name, count)
    k = row_scale(key, raw, hair_target())
    frames = [wm.resize_pm(f, k) for f in raw]
    if key[1] == "mirror-left":
        frames = [f[:, ::-1].copy() for f in frames]
    frames = [frames[COPY.get((key[0], key[1], i), i)] for i in range(len(frames))]
    base = HEAD_X - hair_center(frames[0])
    cells, lost = [], []
    for i, f in enumerate(frames):
        cell, clipped = place(f, base + (hair_center(frames[0]) - hair_center(f)))
        cells.append(cell)
        if clipped:
            lost.append(i)
    return cells, k, lost


def cut(out):
    os.makedirs(out, exist_ok=True)
    z = 3
    for key, (name, _) in PAINTED.items():
        if not os.path.exists(f"{SRC}/{name}.png"):
            continue
        cells, k, lost = row_cells(key)
        tops = [int(np.where((c[..., 3] > 128).any(1))[0][0]) for c in cells]
        print(key, f"k {k:.3f} tops {tops} hair {[round(hair_size(c), 1) for c in cells]} clipped {lost}")
        w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for f, c in enumerate(cells):
            im = Image.fromarray(wm.to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (f * FRAME * z, 0), im)
            for v in range(0, FRAME, 16):
                strong = v % 48 == 0
                d.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=(255, 255, 0) if strong else (110, 110, 110))
                d.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=(255, 255, 0) if strong else (110, 110, 110))
                if strong:
                    d.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w.save(f"{out}/grid-{key[0]}-{key[1]}.png")


def normal(deg):
    return ((deg + 180) % 360) - 180


def table(anim, direction):
    if anim in MIRRORED and direction == "left":
        return [(FRAME - x, y, normal(180 - aim), -flip, behind) for x, y, aim, flip, behind in NEW_FIST[(anim, "right")]]
    return NEW_FIST[(anim, direction)]


def lifted(anim, y, f):
    return y + wm.LIFT[f] * float(wm.lift_at(np.array(float(y)))) if anim == "idle" else y


def anchor(anim, f, row):
    x, y, aim, flip, behind = row
    return {"x": x, "y": round(lifted(anim, y, f), 3), "angle": normal(aim if flip > 0 else aim - 180), "s": GUN_SCALE, "sx": flip, "behind": behind, "hidden": False, "fallback": False}


def fist_mask(cx, cy):
    y, x = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    return np.clip((FIST_R - np.hypot(x - cx, y - cy)) / FEATHER, 0, 1)


def all_rows():
    """{(anim, row index): premultiplied cells} with the left rows filled in."""
    out = {}
    for key in PAINTED:
        if not os.path.exists(f"{SRC}/{PAINTED[key][0]}.png"):
            raise SystemExit(f"missing source {key}")
        cells, _, lost = row_cells(key)
        if lost:
            print("clipped", key, lost)
        anim, direction = key
        if anim == "idle":
            cells = [wm.breathe(cells[0], f) for f in range(4)]
        if direction == "mirror-left":
            out[(anim, ROWS["left"])] = cells
            continue
        out[(anim, ROWS[direction])] = cells
        if direction == "right" and anim in ("idle", "walk", "hurt", "attack3"):
            out[(anim, ROWS["left"])] = [c[:, ::-1].copy() for c in cells]
    return out


def write_sheets(folder):
    """Reads each live sheet and replaces only these rows, so idle row 0 stays as it is."""
    rows = all_rows()
    for anim, cols in COUNT.items():
        for layer in ("bare", "armed", "hand"):
            os.makedirs(f"{folder}/{layer}", exist_ok=True)
            live = f"{BODY}/{layer}/{anim}.png"
            sheet = np.array(Image.open(live).convert("RGBA")) if os.path.exists(live) else np.zeros((4 * FRAME, cols * FRAME, 4), np.uint8)
            for (a, row), cells in rows.items():
                if a != anim:
                    continue
                direction = DIRS[row]
                for f, cell in enumerate(cells):
                    x0, y0 = f * FRAME, row * FRAME
                    if layer == "hand":
                        x, y, _, _, behind = table(anim, direction)[f]
                        art = np.zeros_like(cell) if behind else cell * fist_mask(x, lifted(anim, y, f))[..., None]
                    else:
                        art = cell
                    sheet[y0:y0 + FRAME, x0:x0 + FRAME] = wm.to_rgba(art)
            Image.fromarray(sheet, "RGBA").save(f"{folder}/{layer}/{anim}.png")


def write_meta():
    """New sheets take the settings of their neighbours, with their own guid. A guid Unity already knows stays."""
    for layer in ("bare", "hand"):
        path = f"{BODY}/{layer}/attack2.png.meta"
        if not os.path.exists(path):
            src = open(f"{BODY}/{layer}/attack1.png.meta", "rb").read().decode()
            open(path, "wb").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1).encode())


def new_anchors(old):
    wanted = {(anim, d): [anchor(anim, f, r) for f, r in enumerate(table(anim, d))] for anim in COUNT for d in DIRS if not (anim == "idle" and d == "down")}
    done = set()
    for a in old:
        key = (a["anim"], a["dir"])
        if a["part"] == "gun" and key in wanted:
            assert len(a["frames"]) == len(wanted[key]), key
            a["frames"] = wanted[key]
            done.add(key)
    for key, frames in wanted.items():
        if key not in done:
            old.append({"part": "gun", "anim": key[0], "dir": key[1], "frames": frames})
    return old


def write_anchors(path):
    """Only the anchors list of this one-line file changes."""
    raw = open(path, "rb").read().decode()
    head, sep, tail = raw.partition('"anchors":')
    old = json.loads("{" + sep + tail[: tail.rindex("}")] + "}")["anchors"]
    open(path, "wb").write((head + sep + json.dumps(new_anchors(old), separators=(",", ":")) + tail[tail.rindex("}"):]).encode())


def load_compose():
    spec2 = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec2)
    spec2.loader.exec_module(comp)
    return comp


def contact(out, anchors=None, bare=None, hand=None, pad=50, tier=0):
    """One contact sheet and GIFs per anim, beside the idle down row, composited as the game draws."""
    comp = load_compose()
    if anchors is None:
        _, anchors = comp.load_gear("mechanic")
    size = FRAME + 2 * pad
    ref = Image.open(f"{BODY}/armed/idle.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    for anim, count in COUNT.items():
        dirs = ["up", "left", "right"] if anim == "idle" else DIRS
        sheet = Image.new("RGBA", (ref.width + size * count, size * len(dirs)), (58, 62, 70, 255))
        sheet.alpha_composite(ref, (0, 0))
        for r, d in enumerate(dirs):
            frames = []
            for f in range(count):
                c = comp.compose("mechanic", anim, d, f, f"{bare}/{anim}.png" if bare else None, anchors, f"{hand}/{anim}.png" if hand else None, tier=tier, pad=pad)
                sheet.alpha_composite(c, (ref.width + f * size, r * size))
                bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
                bg.alpha_composite(c)
                frames.append(bg.resize((size * 2, size * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
            frames[0].save(f"{out}/{anim}-{d}.gif", save_all=True, append_images=frames[1:], duration={"idle": 240, "walk": 160, "hurt": 200}.get(anim, 120), loop=0, disposal=2)
        sheet.convert("RGB").resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{out}/{anim}.png")


def preview(out):
    os.makedirs(out, exist_ok=True)
    write_sheets(out)
    copy = f"{out}/held-mechanic.json"
    shutil.copy2(GEAR, copy)
    write_anchors(copy)
    comp = load_compose()
    _, anchors = comp.load_gear("mechanic")
    for a in json.load(open(copy, encoding="utf-8"))["anchors"]:
        if not (a["anim"] == "idle" and a["dir"] == "down"):
            anchors[(a["part"], a["anim"], a["dir"])] = a["frames"]
    contact(out, anchors, f"{out}/bare", f"{out}/hand")


def key_any(path):
    """Straight RGBA from art on flat #00FF00 or #FF00FF, whichever the corners are; edges unmixed from the key. Only what is joined to the biggest piece stays."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    key = np.median(np.concatenate([rgb[:20, :20].reshape(-1, 3), rgb[-20:, -20:].reshape(-1, 3)]), axis=0)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = (g - np.maximum(r, b)) if key[1] > 128 else (np.minimum(r, b) - g)
    alpha = 1 - np.clip((excess - 30) / 150, 0, 1)
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0), 0, 255)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(lab == 1 + int(np.argmax(sizes)), iterations=3)
    return np.dstack([out, alpha * 255])


def bbox(art):
    ys, xs = np.nonzero(art[..., 3] > 128)
    return int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1


def fit(art, w, h):
    """Trimmed art resized to the master's box on a padded canvas, premultiplied while resizing."""
    x0, x1, y0, y1 = bbox(art)
    part = art[y0:y1, x0:x1].copy()
    part[..., :3] *= part[..., 3:4] / 255
    chans = [np.array(Image.fromarray(part[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]
    canvas = np.zeros((h + 2 * PAD, w + 2 * PAD, 4))
    canvas[PAD:PAD + h, PAD:PAD + w] = np.clip(np.dstack(chans), 0, 255)
    return wm.to_rgba(canvas)


def gun_box():
    x0, x1, y0, y1 = bbox(key_any(f"{SRC}/{MASTER}.png"))
    k = GUN_LENGTH / (x1 - x0)
    w, h = GUN_LENGTH, round((y1 - y0) * k)
    return w, h, {"w": w + 2 * PAD, "h": h + 2 * PAD, "gripX": round((MASTER_GRIP[0] - x0) * k) + PAD, "gripY": round((MASTER_GRIP[1] - y0) * k) + PAD}


def turret_box():
    """Grip: middle of the base (front foot, measured at x = 645 in the sample; the muzzle makes the box lopsided), on the feet row."""
    x0, x1, y0, y1 = bbox(key_any(TURRET_SAMPLE))
    h = TURRET_HEIGHT
    w = round((x1 - x0) * h / (y1 - y0))
    return w, h, {"w": w + 2 * PAD, "h": h + 2 * PAD, "gripX": round((645 - x0) * w / (x1 - x0)) + PAD, "gripY": h + PAD}


def write_tiers(tiers, prefix, folder, key, box):
    """Only tiers with both images. Only baseGrip and enhancedGrip of this list in the one-line file."""
    w, h, grip = box
    raw = open(GEAR, "rb").read().decode()
    start = raw.index(f'"{key}":[')
    end = raw.index("]", start)
    part = raw[start:end]
    done = []
    for tier in tiers:
        arts = {}
        for kind in ("base", "enhanced"):
            path = f"{SRC}/{prefix}-{tier}-{kind}.png"
            if os.path.exists(path):
                art = key_any(path)
                x0, x1, y0, y1 = bbox(art)
                print(tier, kind, "aspect", round((x1 - x0) / (y1 - y0), 3), "master", round(w / h, 3))
                arts[kind] = fit(art, w, h)
        if len(arts) < 2:
            print(tier, "not both images: left as it is")
            continue
        for kind, rgba in arts.items():
            Image.fromarray(rgba, "RGBA").save(f"{folder}/{tier}-{kind}.png")
        entry = json.dumps(grip, separators=(",", ":"))
        part, n = re.subn(r'(\{"name":"' + tier + r'","glow":"#[0-9a-f]+","baseGrip":)\{[^}]*\},"enhancedGrip":\{[^}]*\}\}', lambda m: m.group(1) + entry + ',"enhancedGrip":' + entry + "}", part, count=1)
        assert n == 1, tier
        done.append(tier)
    open(GEAR, "wb").write((raw[:start] + part + raw[end:]).encode())
    print("written", done, grip)


def icon(path, tilt):
    """Same steps as overlay() and fit() in build-item-icons.py; that tool rebuilds every class's icons when run."""
    im = Image.open(path).convert("RGBA")
    if tilt:
        im = im.rotate(tilt, expand=True, resample=Image.BICUBIC)
    im = im.crop(im.getbbox())
    k = 88 / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    out.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    return out


def write_icons():
    gear = json.load(open(GEAR, encoding="utf-8"))
    for tier, e in enumerate(gear["weapon"]):
        icon(f"{GUN_DIR}/{e['name']}-base.png", 45).save(f"{RES}/Sprites/icons/weapon-mechanic-{tier}.png")
    for tier, e in enumerate(gear["companion"]):
        icon(f"{TURRET_DIR}/{e['name']}-base.png", 0).save(f"{RES}/Sprites/icons/offhand-mechanic-{tier}.png")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "measure":
        measure()
    elif cmd == "cut":
        cut(sys.argv[2])
    elif cmd == "preview":
        preview(sys.argv[2])
    elif cmd == "write":
        write_sheets(BODY)
        write_meta()
        write_anchors(GEAR)
        print("sheets and anchors written")
    elif cmd == "gun":
        write_tiers(sys.argv[2:], "gun", GUN_DIR, "weapon", gun_box())
    elif cmd == "turret":
        write_tiers(sys.argv[2:], "turret", TURRET_DIR, "companion", turret_box())
    elif cmd == "icons":
        write_icons()

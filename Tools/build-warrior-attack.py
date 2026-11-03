"""Warrior melee/attack sheets in the new style. Poses follow the OLD sheets, because the slash FX follows them: each row is a ChatGPT redraw of the old row.

Sources: Assets/ArtLibrary/classes/warrior/new-style/attack/src/<anim>-<dir>.png (one row of frames on flat #00FF00).
Old sheets, kept for reference: .../attack/old-sheets/<layer>-<anim>.png. Old anchors: .../attack/old-sheets/held-gear-melee-old.json.
Writes, only for rows listed in FISTS below:
  Resources/Sprites/classes/warrior/body/{bare,armed,hand}/<anim>.png   armed row = bare row, hand row = the sword fist only
  Resources/Data/classes/warrior/held-gear-melee.json                   sword: x, y on the new fist, angle kept from the old anchors, s = 0.34
                                                                        shield: on the left fist, s = 0.27, shown
Other rows and other anims are left alone.

Usage: python Tools/build-warrior-attack.py cut <anim> <dir>     cut one source row into 192 px frames in ATK_OUT, no game file touched
       python Tools/build-warrior-attack.py write [<anim> <dir>] write sheets and anchors for one row, or for every row in FISTS
"""
import json, math, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/classes/warrior/new-style/attack")
BODY = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/body")
GEAR = os.path.join(ROOT, "Assets/Resources/Data/classes/warrior/held-gear-melee.json")
OLD_GEAR = os.path.join(SRC, "old-sheets/held-gear-melee-old.json")
OUT = os.environ.get("ATK_OUT", os.path.join(SRC, "cut"))
FRAME, FEET_Y = 192, 186
STAND_H = 164.0
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"melee3up": 6, "melee1": 6, "melee2": 6, "melee3": 6, "melee4": 6, "attack1": 4, "attack2": 4, "attack3": 6}
SWORD_S, SHIELD_S = 0.34, 0.27
SWORD_FIST_R = 8  # hand layer radius: tight on the fist, a wide mask repaints arm and cape over the handle

# (anim, dir): one tuple per frame, read from the cut frames:
# sword fist x, y, shield fist x, y, sword behind body, shield behind body, shield angle
# Fist coordinates are for the row cut with dx 0 (env ATK_NODX=1); write() adds the row's dx.
def R(*frames, sb=False, lb=True, la=10):
    return [(f[0], f[1], f[2], f[3], sb, lb, la) for f in frames]
FISTS = {
    ("melee3up", "down"): R((57, 170, 134, 150), (95, 168, 136, 150), (80, 133, 126, 142), (75, 108, 127, 108), (60, 33, 128, 105), (63, 80, 127, 108), lb=False, la=4),
    ("melee3up", "up"): R((150, 158, 37, 120), (162, 148, 45, 113), (150, 105, 45, 113), (150, 62, 42, 98), (155, 13, 45, 85), (153, 57, 44, 103), la=-10),
    ("melee3up", "left"): R((133, 160, 40, 140), (32, 155, 40, 138), (25, 128, 42, 113), (15, 80, 55, 112), (100, 15, 55, 98), (30, 82, 52, 113), sb=True, lb=False, la=-10),
    ("melee3up", "right"): R((57, 155, 130, 137), (106, 162, 127, 134), (117, 138, 131, 118), (155, 82, 132, 121), (131, 15, 130, 100), (157, 80, 128, 123)),
    ("melee1", "right"): R((60, 95, 131, 135), (57, 106, 133, 136), (52, 103, 141, 136), (50, 104, 143, 133), (110, 139, 146, 136), (108, 139, 143, 136)),
    ("melee1", "down"): R((51, 96, 140, 135), (48, 116, 141, 136), (52, 108, 143, 136), (60, 116, 150, 135), (74, 135, 138, 135), (66, 133, 136, 133), lb=False, la=4),
    ("melee2", "right"): R((94, 78, 147, 127), (96, 63, 144, 135), (62, 103, 137, 135), (88, 130, 138, 138), (101, 138, 138, 136), (108, 136, 146, 139)),
    ("melee2", "down"): R((53, 50, 125, 137), (57, 79, 126, 136), (58, 103, 127, 137), (88, 138, 125, 136), (86, 139, 127, 136), (90, 135, 131, 136), lb=False, la=4),
    ("melee3", "right"): R((83, 120, 132, 136), (55, 104, 129, 138), (101, 130, 129, 136), (101, 130, 129, 136), (101, 144, 130, 136), (93, 139, 128, 135)),
    ("melee3", "down"): R((68, 144, 135, 135), (45, 101, 130, 135), (44, 107, 133, 135), (50, 98, 134, 136), (50, 98, 134, 136), (69, 146, 130, 136), lb=False, la=4),
    ("melee4", "right"): R((78, 56, 153, 134), (92, 22, 148, 112), (128, 147, 140, 135), (128, 149, 141, 143), (129, 151, 139, 141), (119, 154, 134, 141)),
    ("melee2", "left"): R((103.5, 75, 48.5, 129.5), (114, 69, 39, 131), (93, 119, 39, 131), (103, 141, 41, 133), (92, 148, 45, 135), (93, 148, 43, 135), sb=True, lb=False, la=-10),
    ("melee3", "left"): R((118, 141, 49, 124), (149, 96, 49, 124), (81, 124, 49, 124), (82, 124, 49, 124), (74, 132, 49, 126), (80, 133, 49, 124), sb=True, lb=False, la=-10),
    ("melee4", "left"): R((125, 53, 47, 128), (107, 22, 46, 113), (87, 144, 55, 131), (69, 152, 44, 144), (80, 144, 46, 132), (86, 146, 47, 132), sb=True, lb=False, la=-10),
    ("melee1", "up"): R((140, 84, 43, 121), (146, 100, 40, 121), (146, 100, 40, 121), (144, 101, 41, 114), (144, 101, 41, 114), (144, 101, 41, 114), la=-10),
    ("melee1", "left"): R((135, 97, 37, 131), (130, 88, 35, 131), (140, 104, 37, 131), (138, 104, 37, 132), (92, 148, 38, 135), (93, 148, 37, 135), sb=True, lb=False, la=-10),
    ("melee2", "up"): R((153, 46, 57, 101), (161, 78, 57, 101), (153, 97, 49, 101), (153, 133, 60, 98), (166, 126, 61, 99), (165, 126, 61, 99), la=-10),
    ("melee3", "up"): R((141, 92, 40, 108), (144, 87, 37, 108), (150, 104, 38, 108), (141, 104, 37, 112), (139, 136, 38, 112), (150, 138, 38, 112), la=-10),
    ("melee4", "up"): R((143, 47, 51, 98), (124, 4, 52, 82), (140, 54, 47, 99), (110, 152, 57, 120), (54, 146, 60, 108), (137, 114, 56, 113), la=-10),
    ("attack3", "down"): R((54, 134, 128, 132), (54, 72, 123, 135), (55, 103, 131, 137), (60, 138, 126, 138), (80, 130, 105, 130), (55, 136, 130, 136), lb=False, la=4),
    ("attack3", "up"): R((137, 129, 54, 129), (135, 60, 55, 128), (149, 100, 55, 128), (137, 130, 54, 124), (136, 130, 55, 127), (136, 129, 55, 127), la=-10),
    ("attack3", "right"): R((84, 129, 137, 128), (51, 73, 138, 129), (84, 126, 138.5, 124), (100, 138, 138, 135), (96, 139, 139, 135), (84, 133, 136, 132)),
    ("attack3", "left"): R((41, 132, 93.5, 144), (48, 132, 99, 144), (13.5, 66, 99, 143), (31, 133, 101, 138), (20, 114, 102, 141), (41.5, 132, 102, 141), sb=True, lb=False, la=-10),
    ("melee4", "down"): R((51, 50, 131, 134), (52, 22, 127, 113), (63.5, 73, 138.5, 145), (107, 146, 134, 139), (139, 148, 136, 136), (113.5, 146.5, 134, 136), lb=False, la=4),
}
# melee3up (combo hit 3, rising slash, drawn new 2026-11-03): no old row, so the sword angle per frame is set here
# (degrees clockwise on screen, 0 = blade up, 90 = right, 180 = down). Fists go into FISTS after reading the cut frames.
NEW_ANGLES = {
    ("melee3up", "right"): [-150, 150, 110, 50, -5, 35],
    ("melee3up", "left"): [150, -150, -110, -50, 5, -35],
    ("melee3up", "down"): [180, -160, -120, -70, -15, -35],
    ("melee3up", "up"): [170, 150, 100, 55, 10, 35],
}

# melee3up: crouched frames have the shield fist at the chin, so the shield sits lower and further out there (values above),
# and frames whose raised far arm is painted over the hair draw the sword in front of the body.
SWORD_FRONT = {("melee3up", "left", 3), ("melee3up", "left", 4), ("melee3up", "left", 5)}

# (anim, dir, frame): frame to reuse when ChatGPT moved the arm against the old row.
COPY = {("melee1", "up", 1): 2, ("melee1", "up", 4): 3, ("melee1", "up", 5): 3, ("melee3", "right", 2): 3, ("melee3", "down", 4): 3}
# (anim, dir): art scale when the row's tallest art is not a standing pose. (anim, dir, frame): dx, dy.
SCALE = {}
NUDGE = {("melee4", "left", 0): (0, -16), ("melee4", "right", 0): (0, -16), ("melee4", "down", 0): (0, -28), ("melee4", "down", 2): (28, -20), ("melee4", "down", 3): (20, 0), ("melee4", "down", 4): (9, 0)}  # jump frames ChatGPT left on the ground; melee4 down 2-4 also step sideways, as the old row does, so the fist meets the old one
ERASE = {("melee3", "up"): (20, 85, 62, 134)}  # x0, y0, x1, y1 of a baked shield, for the row cut with dx 0
DEFAULT_K = 0.457  # melee4's ready pose is crouched, so it cannot set the scale; the mean of the other rows keeps the head the same size
ROW_DX = {}  # (anim, dir): dx of the whole row, chosen so the fists fall as near the old fists as they can with the feet still
# (anim, dir): (file in src/fix/, frames it replaces, left to right). The first paint of these frames put the fist 40-70 px off the old sheet.
PATCH = {("melee4", "down"): ("melee4-down-fix-2.png", [2, 3, 4, 5]), ("melee2", "right"): ("melee2-right-f0.png", [0]), ("melee2", "left"): ("melee2-left-f0.png", [0])}


OLD = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in json.load(open(OLD_GEAR, encoding="utf-8"))["anchors"]}
# attack3 (the cast pose) has its old anchors in held-gear.json; its new ones are added to held-gear-melee.json, which the game reads over it.
OLD.update({(a["part"], a["anim"], a["dir"]): a["frames"] for a in json.load(open(os.path.join(ROOT, "Assets/Resources/Data/classes/warrior/held-gear.json"), encoding="utf-8"))["anchors"] if a["anim"] == "attack3"})
HEAD_DX = {
    ("melee1", "down"): -2, ("melee1", "up"): 17, ("melee1", "left"): -6, ("melee1", "right"): 5,  # up: 24 measured, 17 keeps the cape in the cell
    ("melee2", "down"): -12, ("melee2", "up"): 3, ("melee2", "left"): -11, ("melee2", "right"): 10,
    ("melee3", "down"): 0, ("melee3", "up"): 17, ("melee3", "left"): -5, ("melee3", "right"): 18,
    ("melee4", "down"): -10, ("melee4", "up"): 15, ("melee4", "left"): 1, ("melee4", "right"): 8,
    ("attack3", "down"): 5, ("attack3", "up"): 0, ("attack3", "left"): 12, ("attack3", "right"): 8,
}


def key_green(path):
    """Straight-alpha RGBA from a flat green render, edge pixels unmixed from the key."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    e = g - np.maximum(r, b)
    key = np.median(np.concatenate([rgb[:20, :20].reshape(-1, 3), rgb[-20:, -20:].reshape(-1, 3)]), axis=0)
    e_bg = float(key[1] - max(key[0], key[2]))
    alpha = 1 - np.clip((e - 0.12 * e_bg) / (0.75 * e_bg), 0, 1)
    a = alpha[..., None]
    out = np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0)
    out = np.clip(out, 0, 255)
    out[..., 1] = np.minimum(out[..., 1], np.maximum(out[..., 0], out[..., 2]) + 6)  # despill
    olive = (out[..., 1] >= 0.95 * out[..., 0]) & (out[..., 2] < 0.6 * out[..., 1]) & (out[..., 1] > 25)  # green left at torn cape edges: no art here has this hue
    out[..., 1][olive] = 0.45 * out[..., 0][olive]
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    return np.dstack([out, alpha * 255]).astype(np.uint8)


def split(rgba, n):
    """n boxes of connected art, left to right; specks are dropped. Equal grid if the frames touch."""
    mask = rgba[..., 3] > 40
    lab, cnt = ndimage.label(ndimage.binary_dilation(mask, iterations=2))
    objs = ndimage.find_objects(lab)
    sizes = ndimage.sum(mask, lab, range(1, cnt + 1))
    big = sorted([(o, i + 1) for i, (o, s) in enumerate(zip(objs, sizes)) if s > 0.15 * max(sizes)], key=lambda t: t[0][1].start)
    if len(big) != n:
        print("  warning:", len(big), "blobs, expected", n, "- equal grid")
        w = rgba.shape[1] / n
        return [rgba[:, int(i * w):int((i + 1) * w)] for i in range(n)]
    return [rgba[:, o[1]] * (lab[:, o[1]] == label)[..., None] for o, label in big]  # full height: the source rows say who is in the air


def destar(rgba):
    """The approved cape is plain red, but ChatGPT copies the old cape's yellow stars: yellow specks with mostly red around them become cape red. The gold belt has dark around it."""
    rgb = rgba[..., :3].astype(np.int16)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    op = rgba[..., 3] > 128
    yellow = op & (r > 170) & (g > 95) & (b < 130) & (g > b + 30)
    red = op & (r > 130) & (g < 80) & (b < 80)
    lab, cnt = ndimage.label(ndimage.binary_dilation(yellow, iterations=1))
    out = rgba.copy()
    for i, o in enumerate(ndimage.find_objects(lab)):
        box = tuple(slice(max(0, s.start - 5), s.stop + 5) for s in o)
        blob = lab[box] == i + 1
        ring = ndimage.binary_dilation(blob, iterations=4) & ~blob
        if blob.sum() < 700 and red[box][ring].sum() > 0.6 * (op[box][ring].sum() + 1) and op[box][ring].mean() > 0.6:
            out[box][blob, 0], out[box][blob, 1], out[box][blob, 2] = 200, 28, 24
    return out


def erase_shield(fr, box):
    """ChatGPT copied the old row's shield into melee3 up. Metal and outline pixels in box go, unless next to the cape, which keeps its outline; the fur and hair are not grey, and stray bits are dropped."""
    a = np.asarray(fr).copy()
    x0, y0, x1, y1 = box
    rgb = a[..., :3].astype(np.int16)
    mx, mn = rgb.max(-1), rgb.min(-1)
    red = (a[..., 3] > 128) & (rgb[..., 0] > 130) & (rgb[..., 1] < 90) & (rgb[..., 2] < 90)
    region = np.zeros(a.shape[:2], bool)
    region[y0:y1, x0:x1] = True
    kill = region & (mn < 195) & (mx - mn < 90) & ~ndimage.binary_dilation(red, iterations=2)
    a[..., 3][kill] = 0
    lab, cnt = ndimage.label(a[..., 3] > 40)
    if cnt > 1:
        sizes = ndimage.sum(a[..., 3] > 40, lab, range(1, cnt + 1))
        a[..., 3][(lab != int(np.argmax(sizes)) + 1) & (lab > 0)] = 0
    return Image.fromarray(a)


def bbox(a, thr=40):
    ys, xs = np.nonzero(a[..., 3] > thr)
    return xs.min(), xs.max(), ys.min(), ys.max()


def feet_cx(a):
    """Centre x of the lowest 12% of the art: where the feet are."""
    x0, x1, y0, y1 = bbox(a)
    low = a[int(y1 - 0.12 * (y1 - y0)):y1 + 1]
    xs = np.nonzero(low[..., 3].max(0) > 40)[0]
    return (xs.min() + xs.max()) / 2


def old_fist(anim, direction, c):
    """Where the old sheet's fist was: the handle's middle, back along the blade by (230 - 217) art px at the old s."""
    if ("sword", anim, direction) not in OLD:
        return FISTS[(anim, direction)][c][:2]
    a = OLD[("sword", anim, direction)][c]
    th = math.radians(a["angle"])
    return a["x"] - math.sin(th) * 8.84, a["y"] + math.cos(th) * 8.84


def row_dx(anim, direction):
    """Row offset so the head stands where the old sheet had it (measured), because the slash FX was placed against that body."""
    return 0 if os.environ.get("ATK_NODX") else HEAD_DX.get((anim, direction), 0)


def hair_width(a):
    """Width of the strong red in the top of the art: a size the raised arm and the cape do not change."""
    ys = np.nonzero((a[..., 3] > 128).any(1))[0]
    top = a[ys[0]:ys[0] + int(0.3 * (ys[-1] - ys[0]))]
    r, g, b = [top[..., c].astype(np.int16) for c in range(3)]
    xs = np.nonzero(((r > 150) & (g < 90) & (b < 90) & (top[..., 3] > 128)).sum(0) >= 2)[0]
    return xs[-1] - xs[0] + 1


def apply_patch(anim, direction, parts):
    """Frames repainted later replace their part: same hair width as the row's last frame, feet on the row's ground."""
    file, frames = PATCH[(anim, direction)]
    pieces = split(destar(key_green(os.path.join(SRC, "src", "fix", file))), len(frames))
    ground = max(bbox(p)[3] for p in parts)
    k = hair_width(parts[-1]) / max(hair_width(p[bbox(p)[2]:bbox(p)[3] + 1]) for p in pieces)
    for frame, piece in zip(frames, pieces):
        x0, x1, y0, y1 = bbox(piece)
        im = Image.fromarray(piece[y0:y1 + 1, x0:x1 + 1])
        im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
        canvas = Image.new("RGBA", (im.width + 4, parts[0].shape[0]))
        canvas.alpha_composite(im, (2, ground + 1 - im.height))
        parts[frame] = np.asarray(canvas)
    return parts


def cut_row(anim, direction):
    """192 px frames, all scaled alike so the row's tallest art is 164 px, feet on FEET_Y, centred on the feet (NUDGE moves a frame on purpose)."""
    src = destar(key_green(os.path.join(SRC, "src", f"{anim}-{direction}.png")))
    parts = split(src, COUNT[anim])
    if (anim, direction) in PATCH:
        parts = apply_patch(anim, direction, parts)
    # The last frame is the ready pose: raised arms and jumps would shrink the whole row if the tallest art set the scale.
    last = bbox(parts[-1])
    k = SCALE.get((anim, direction), DEFAULT_K if anim == "melee4" else STAND_H / (last[3] - last[2] + 1))
    bottoms = [bbox(p)[3] for p in parts]
    frames = []
    for i, p in enumerate(parts):
        x0, x1, y0, y1 = bbox(p)
        p = p[y0:y1 + 1, x0:x1 + 1]
        im = Image.fromarray(p).resize((max(1, round(p.shape[1] * k)), max(1, round(p.shape[0] * k))), Image.LANCZOS)
        dx, dy = NUDGE.get((anim, direction, i), (0, 0))
        # Feet within 6 px of the ground snap to it (fixes jitter); further up is a real jump, kept.
        air = (max(bottoms) - bottoms[i]) * k
        ox, oy = round(FRAME / 2 + row_dx(anim, direction) - feet_cx(np.asarray(im)) + dx), round(FEET_Y - im.height + 1 + dy - (air if air > 6 else 0))
        sx, sy = max(0, -ox), max(0, -oy)
        fr = Image.new("RGBA", (FRAME, FRAME))
        fr.alpha_composite(im.crop((sx, sy, min(im.width, FRAME - ox), min(im.height, FRAME - oy))), (ox + sx, oy + sy))
        frames.append(fr)
    frames = [frames[COPY.get((anim, direction, i), i)] for i in range(len(frames))]
    if (anim, direction) in ERASE:
        x0, y0, x1, y1 = ERASE[(anim, direction)]
        d = row_dx(anim, direction)
        frames = [erase_shield(f, (x0 + d, y0, x1 + d, y1)) for f in frames]
    return frames, k


def on_body(frame, x, y):
    """The fist has body all round it, rather than sticking out of the silhouette."""
    a = np.asarray(frame)[..., 3] > 128
    ys, xs = np.mgrid[0:FRAME, 0:FRAME]
    r = np.hypot(xs - x, ys - y)
    ring = (r >= 13) & (r <= 17)
    return a[ring].mean() > 0.8


def write(anim, direction):
    frames, _ = cut_row(anim, direction)
    row, n = ROWS[direction], COUNT[anim]
    dx = row_dx(anim, direction)
    fists = []
    for c, f in enumerate(FISTS[(anim, direction)]):
        nx, ny = NUDGE.get((anim, direction, c), (0, 0))
        lift = (-4, 10) if direction == "left" else (0, 0)  # the near shield would hide the chin: down and forward, still over the fist
        fists.append((f[0] + dx + nx, f[1] + ny, f[2] + dx + nx + lift[0], f[3] + ny + lift[1], f[4], f[5], f[6]))
    ys, xs = np.mgrid[0:FRAME, 0:FRAME]
    for layer in ("bare", "armed", "hand"):
        path = os.path.join(BODY, layer, f"{anim}.png")
        sheet = Image.open(path).convert("RGBA") if os.path.exists(path) else Image.new("RGBA", (FRAME * n, FRAME * 4))
        for c, fr in enumerate(frames):
            sheet.paste((0, 0, 0, 0), (c * FRAME, row * FRAME, (c + 1) * FRAME, (row + 1) * FRAME))
            if layer == "hand":
                m = np.asarray(fr).copy()
                m[..., 3] = (m[..., 3] * np.clip(SWORD_FIST_R + 0.5 - np.hypot(xs - fists[c][0], ys - fists[c][1]), 0, 1)).astype(np.uint8)
                fr = Image.fromarray(m)
            sheet.paste(fr, (c * FRAME, row * FRAME))
        sheet.save(path)
    # Quick read-modify-write: other tools write this file too. Angles always come from the old file, so a re-run cannot drift.
    old = OLD
    data = json.load(open(GEAR, encoding="utf-8"))
    for part in ("sword", "shield"):
        if not any(e["part"] == part and e["anim"] == anim and e["dir"] == direction for e in data["anchors"]):
            seed = OLD.get((part, anim, direction)) or [{"s": 0.34, "behind": False} for _ in range(COUNT[anim])]
            data["anchors"].append({"part": part, "anim": anim, "dir": direction, "frames": [dict(f) for f in seed]})
    for a in data["anchors"]:
        if a["anim"] != anim or a["dir"] != direction:
            continue
        for c, fr in enumerate(a["frames"]):
            sx, sy, lx, ly, sb, lb, la = fists[c]
            fr.update(fallback=False, hidden=False, sx=1)
            if a["part"] == "sword":
                # Draw order stays the old one too; facing left the sword hand is the far hand (rule 4), so always behind.
                was = old.get(("sword", anim, direction), [{"behind": direction == "left", "angle": 0}] * COUNT[anim])[c]
                if (anim, direction) in NEW_ANGLES:
                    was = dict(was, angle=NEW_ANGLES[(anim, direction)][c])
                # A far fist that crosses in front of the chest must be seen gripping: the blade goes over the body there, under the fist layer.
                across = direction == "left" and on_body(frames[c], sx, sy)
                front = (anim, direction, c) in SWORD_FRONT
                fr.update(x=sx, y=sy, s=SWORD_S, behind=(sb and not across and not front) if direction == "left" else was["behind"], angle=was["angle"])
            else:
                fr.update(x=lx, y=ly, s=SHIELD_S, behind=lb, angle=la)
    with open(GEAR, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=1)
    errs = [round(math.hypot(fists[c][0] - old_fist(anim, direction, c)[0], fists[c][1] - old_fist(anim, direction, c)[1])) for c in range(len(fists))]
    print("written", anim, direction, "row dx", dx, "fist distance from old, px:", errs)


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "cut":
        anim, direction = sys.argv[2:4]
        frames, k = cut_row(anim, direction)
        os.makedirs(OUT, exist_ok=True)
        for i, f in enumerate(frames):
            f.save(os.path.join(OUT, f"{anim}-{direction}-{i}.png"))
        print(anim, direction, "scale", round(k, 4), "boxes", [f.getbbox() for f in frames])
    elif cmd == "write":
        for anim, direction in ([tuple(sys.argv[2:4])] if len(sys.argv) > 2 else list(FISTS)):
            write(anim, direction)

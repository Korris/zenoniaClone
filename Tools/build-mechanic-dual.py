"""Mechanic trial: one pistol in each hand (one weapon item) and a cannon on the back (offhand layer). Tier-0 art on every tier. Decided 2026-11-01, s = 0.15 is an exception pending the user's view.
Sources: Assets/ArtLibrary/classes/mechanic/new-style/dual-src/ (cannon-*, pistol-side, body/*). Bodies as they were before the trial: new-style/pre-dual/, which is what this tool reads, so it can run again.

Combo: hit 1 attack1 (flipped rows: the left pistol's arm is out), hit 2 attack2 (right pistol), hit 3 attack3 (both, also the skill cast pose), hit 4 attack4 (cannon off the back).

Usage (project root; Pillow, numpy, scipy):
  python Tools/build-mechanic-dual.py backup          copy the live sheets to new-style/pre-dual/ once
  python Tools/build-mechanic-dual.py grid OUT        new body rows with a pixel grid, for reading fists
  python Tools/build-mechanic-dual.py preview OUT     everything composed as the game draws, under OUT only
  python Tools/build-mechanic-dual.py write           sheets, art, held-mechanic-dual.json. Way back: see the bottom of this docstring.

Way back: in HeroClassDef.Mechanic restore GearFile "classes/mechanic/held-mechanic", the single "gun" part, CompanionFolder, drop ComboAnims and attack4; copy new-style/pre-dual/{bare,armed,hand} over body/. The 20 gun PNGs and held-mechanic.json are not touched.
"""
import importlib.util, json, math, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

spec = importlib.util.spec_from_file_location("bm", "Tools/build-mechanic-new-style.py")
bm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bm)
wm = bm.wm

FRAME = 192
SRC = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src"
OLD = "Assets/ArtLibrary/classes/mechanic/new-style/pre-dual"
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/mechanic/body"
WEAPONS = f"{RES}/Sprites/classes/mechanic/weapons"
GEAR = f"{RES}/Data/classes/mechanic/held-mechanic.json"
GEAR_DUAL = f"{RES}/Data/classes/mechanic/held-mechanic-dual.json"
TIER_GRIPS = f"{SRC}/tiers/grips.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
DIRS = ["down", "up", "left", "right"]
COUNT = {"idle": 4, "walk": 4, "attack1": 4, "attack2": 4, "attack3": 6, "attack4": 6, "rocket": 1, "dash": 3, "hurt": 2}
HELD_ANIMS = ("attack4", "rocket")  # both hands on the cannon: pistols hidden, the back layer is the cannon itself
MIRRORED = ("attack3", "attack4", "rocket", "dash", "hurt")
T, F = True, False

PISTOL_S, PISTOL_LEN = 0.26, 258  # 2026-11-04: s 0.15 -> 0.26 (x1.73), 39 -> 67 frame px long: the user found the pistols too small. The anchors in held-mechanic-dual.json were scaled the same way; `muzzles` then gives the HeroFx tables
CANNON_LEN = 168.0  # frame px, tip to tail
CANNON_S = 0.5  # art is cut at twice its size on screen
SIDE_SX = 0.9  # the barrel lies across the back: a little shorter seen from the side
FIST_R = 9.5
VIEW_W, VIEW_GRIP = 125, 0.67  # pistol seen along the barrel: art px wide, fist's place down the height (just under the trigger guard, keeps the ring off the chin)
GRIP_PAINTED = (185, 640, 933)  # middle of the pistol's handle in the 933 px wide cut


def key(path):
    """Straight RGBA from flat #00FF00; edges unmixed; only what joins the biggest piece stays."""
    rgb = np.asarray((path if isinstance(path, Image.Image) else Image.open(path)).convert("RGB")).astype(np.float32)
    k = np.array([0, 255, 0], np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 30) / 150, 0, 1)
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * k) / np.maximum(a, 0.02), 0), 0, 255)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(lab == 1 + int(np.argmax(sizes)), iterations=3)
    spill = (g > np.maximum(r, b)) & (alpha < 0.98)
    out[..., 1] = np.where(spill, np.maximum(out[..., 0], out[..., 2]), out[..., 1])
    im = Image.fromarray(np.dstack([out, alpha * 255]).astype(np.uint8), "RGBA")
    return im.crop(im.getbbox())


def axis_length(im):
    ys, xs = np.nonzero(np.asarray(im)[..., 3] > 128)
    pts = np.stack([xs, ys], 1).astype(float)
    c = pts.mean(0)
    _, _, vt = np.linalg.svd(pts[::37] - c, full_matrices=False)
    p = (pts - c) @ vt[0]
    return p.max() - p.min()


def pistol_view(raw, half):
    """A pistol seen along its barrel, from one half of pistol-front-back.png, padded so the fist's place on the handle is the image centre: anchors with `art` pivot at the centre."""
    w, h = raw.size
    im = key(raw.crop((0, 0, w // 2, h) if half == 0 else (w // 2, 0, w, h)))
    k = VIEW_W / im.width
    im = im.resize((VIEW_W, round(im.height * k)), Image.LANCZOS)
    grip = round(im.height * VIEW_GRIP)
    out = Image.new("RGBA", (im.width, 2 * max(grip, im.height - grip)), (0, 0, 0, 0))
    out.alpha_composite(im, (0, out.height // 2 - grip))
    return out


def arts():
    a = {}
    raw = Image.open(f"{SRC}/pistol-front-back.png")
    a["pistol-front"], a["pistol-back"] = pistol_view(raw, 0), pistol_view(raw, 1)
    for n in ("front", "back", "side"):
        im = key(f"{SRC}/cannon-{n}.png")
        k = 2 * CANNON_LEN / axis_length(im)
        a[n] = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    im = key(f"{SRC}/pistol-side.png")
    k = PISTOL_LEN / im.width
    a["pistol"] = im.resize((PISTOL_LEN, round(im.height * k)), Image.LANCZOS)
    return a


PISTOL_GRIP = (round(GRIP_PAINTED[0] * PISTOL_LEN / GRIP_PAINTED[2]), round(GRIP_PAINTED[1] * PISTOL_LEN / GRIP_PAINTED[2]))
# muzzle direction of each cannon art, degrees clockwise from +x, measured on the cuts
CANNON_BASE = {"front": -44.0, "back": -135.0, "side": 0.0}


# ---- bodies ----
def old_sheet(layer, anim):
    return np.array(Image.open(f"{OLD}/{layer}/{anim}.png").convert("RGBA"))


def premul(rgba):
    p = rgba.astype(np.float64)
    p[..., :3] *= p[..., 3:4] / 255
    return p


def old_cell(anim, row, f):
    s = old_sheet("bare", anim)
    return premul(s[row * FRAME:(row + 1) * FRAME, f * FRAME:(f + 1) * FRAME])


def load_cut(name):
    """A cut row of the attack agent: 192 px cells, sole on row 188."""
    s = np.array(Image.open(f"{SRC}/attack/{name}.png").convert("RGBA"))
    return [premul(s[:FRAME, i:i + FRAME]) for i in range(0, s.shape[1], FRAME)]


def read_fists():
    """attack/fists.txt: {row name: [(A, B) per frame]}."""
    out, row = {}, None
    for line in open(f"{SRC}/attack/fists.txt", encoding="utf-8"):
        m = re.match(r"([\w-]+): ", line)
        if m:
            row = m.group(1)
            out[row] = []
        m = re.match(r"\s*frame \d+: A (\d+) (\d+)\s+B (\d+) (\d+)", line)
        if m:
            v = [int(x) for x in m.groups()]
            out[row].append(((v[0], v[1]), (v[2], v[3])))
    return out


FISTS = read_fists()
# Which cut frame plays when. The bolt leaves at 140 of 480 ms, inside the second of six frames, so that frame is already the firing stance, the recoil follows and the swing plays on the way back.
# Down and up: hit4-down, hit4-up on hand hold the cannon wrongly (user), so frame 1, fists together, stands in for every firing frame until the vertical rows are drawn.
HIT4_ORDER = {"right": [0, 2, 3, 4, 1, 5], "down": [0, 1, 1, 1, 1, 5], "up": [0, 1, 1, 1, 1, 5]}


def painted_row(name, count):
    pm = wm.key_green(np.array(Image.open(f"{SRC}/body/{name}.png").convert("RGB")))
    pm[:8], pm[-8:], pm[:, :8], pm[:, -8:] = 0, 0, 0, 0  # stray pixels at the image edge stretch the box
    return [wm.trimmed(pm[:, a:b]) for a, b in wm.split(pm[..., 3], count)]


def cells_of(name, count, scale_key):
    raw = painted_row(name, count)
    target = bm.hair_target()
    by_height = bm.HEIGHT / max(f.shape[0] for f in raw)
    by_hair = target / float(np.median([bm.hair_size(f) for f in raw]))
    k = scale_key if isinstance(scale_key, float) else {"height": by_height, "hair": by_hair, "mean": (by_height + by_hair) / 2}[scale_key]
    frames = [wm.resize_pm(f, k) for f in raw]
    base = bm.HEAD_X - bm.hair_center(frames[0])
    out = []
    for f in frames:
        cell, clipped = bm.place(f, base + (bm.hair_center(frames[0]) - bm.hair_center(f)))
        if clipped:
            print("clipped", name)
        out.append(cell)
    print(name, "k", round(k, 3), "by_height", round(by_height, 3), "by_hair", round(by_hair, 3), "tops", [int(np.where((c[..., 3] > 128).any(1))[0][0]) for c in out])
    return out


def new_bodies():
    """{(anim, row): premultiplied cells} for every row the trial changes."""
    b = {}
    # idle down, up: arms as idle right, seen from the front and the back (user, 2026-11-02). Cut with its breathing by build-mechanic-dual-move.py.
    for d, row in (("down", 0), ("up", 1)):
        s = np.array(Image.open(f"{SRC}/move/idle-{d}.png").convert("RGBA"))
        b[("idle", row)] = [premul(s[:FRAME, i:i + FRAME]) for i in range(0, s.shape[1], FRAME)]
    b[("attack3", 0)] = load_cut("attack3-down")
    cuts = {d: load_cut(f"hit4-{d}") for d in ("down", "up", "right")}
    for d, row in (("down", 0), ("up", 1), ("right", 3)):
        b[("attack4", row)] = [cuts[d][i] for i in HIT4_ORDER[d]]
    # Stand-ins until attack/rocket-<dir>.png exists: the frame of hit 4 whose fists are up.
    for d, row, f in (("down", 0, 0), ("up", 1, 1), ("right", 3, 1)):
        path = f"{SRC}/attack/rocket-{d}.png"
        b[("rocket", row)] = [load_cut(f"rocket-{d}")[1]] if os.path.exists(path) else [cuts[d][f]]
    # idle, walk sideways: the move agent's rows. Left is the mirror: both hands hold the same art.
    for anim in ("idle", "walk"):
        s = np.array(Image.open(f"{SRC}/move/{anim}-right.png").convert("RGBA"))
        cells = [premul(s[:FRAME, i:i + FRAME]) for i in range(0, s.shape[1], FRAME)]
        b[(anim, 3)] = cells
        b[(anim, 2)] = [c[:, ::-1].copy() for c in cells]
    # hit 1: the old attack1 flipped, so the arm out is the left one. Right is the old left row flipped: its far arm is the hero's left when facing right.
    n = 4
    b[("attack1", 0)] = [old_cell("attack1", 0, f)[:, ::-1].copy() for f in range(n)]
    b[("attack1", 1)] = [old_cell("attack1", 1, f)[:, ::-1].copy() for f in range(n)]
    b[("attack1", 3)] = [old_cell("attack1", 2, f)[:, ::-1].copy() for f in range(n)]
    b[("attack1", 2)] = [old_cell("attack1", 3, f)[:, ::-1].copy() for f in range(n)]
    return b


# ---- anchors. Table rows: x, y, aim (clockwise from +x, the muzzle's real direction), flip, behind. None = no fist shows: the pistol is hidden ----
def A(x, y, aim, flip, behind):
    return dict(x=x, y=y, angle=(aim if flip > 0 else aim - 180), sx=flip, behind=behind)


def mirror(a):
    return None if a is None else dict(x=FRAME - a["x"], y=a["y"], angle=-a["angle"], sx=-a["sx"], behind=a["behind"])


LEFT = {  # the hero's left hand, read from grids
    ("idle", "right"): [A(130, 136, 35, 1, T)] * 4,
    ("idle", "left"): [A(100, 143, 145, -1, F)] * 4,
    ("walk", "down"): [A(127, 137, 60, 1, F), A(124, 134, 62, 1, F), A(123, 132, 60, 1, F), A(121, 134, 62, 1, F)],
    ("walk", "up"): [A(61, 139, 120, -1, T), A(63, 136, 120, -1, T), A(63, 136, 120, -1, T), A(61, 139, 120, -1, T)],
    ("walk", "right"): [A(133, 127, -8, 1, T), A(121, 137, 35, 1, T), None, A(121, 136, 35, 1, T)],
    ("walk", "left"): [A(125, 144, 60, -1, F), A(84, 148, 118, -1, F), A(65, 132, 141, -1, F), A(84, 150, 118, -1, F)],
    ("attack2", "down"): [A(134, 134, 60, 1, F), A(136, 134, 60, 1, F), A(134, 134, 60, 1, F), A(138, 134, 60, 1, F)],
    ("attack2", "up"): [A(48, 143, 120, -1, T), A(48, 143, 120, -1, T), A(48, 143, 120, -1, T), A(50, 143, 120, -1, T)],
    ("attack2", "left"): [A(132, 138, 120, -1, F), A(131, 138, 120, -1, F), A(131, 138, 120, -1, F), A(127, 140, 120, -1, F)],
    ("attack2", "right"): [None] * 4,  # far arm is not painted
    ("attack3", "up"): [A(58, 109, 175, -1, T), A(56, 109, 175, -1, T), A(60, 110, 175, -1, T), A(52, 92, 175, -1, T), A(56, 109, 175, -1, T), A(57, 109, 175, -1, T)],
    ("attack3", "right"): [A(141, 114, -5, 1, T), A(141, 114, -5, 1, T), A(141, 115, -5, 1, T), A(142, 100, 0, 1, T), A(135, 115, -15, 1, T), A(135, 114, -5, 1, T)],
    ("hurt", "down"): [A(123, 120, 10, 1, F), A(116, 170, 60, 1, F)],
    ("hurt", "up"): [dict(x=53, y=110, angle=55, sx=-1, behind=T), None],
    ("hurt", "right"): [None, None],
    ("dash", "down"): [None] * 3, ("dash", "up"): [None] * 3, ("dash", "left"): [None] * 3, ("dash", "right"): [None] * 3,
}
LEFT[("attack3", "left")] = [mirror(a) for a in LEFT[("attack3", "right")]]
LEFT[("hurt", "left")] = [None, None]
OLD_LEFT_ATTACK1 = {  # the free fist of the old attack1 rows, before they are flipped
    "down": [A(125, 136, 60, 1, F), A(127, 134, 60, 1, F), A(132, 134, 60, 1, F), A(125, 134, 60, 1, F)],
    "up": [A(61, 146, 120, -1, T), A(60, 148, 120, -1, T), A(50, 148, 120, -1, T), A(58, 148, 120, -1, T)],
    "left": [A(134, 144, 120, -1, F), A(132, 146, 120, -1, F), A(134, 144, 120, -1, F), A(132, 144, 120, -1, F)],
    "right": [None] * 4,
}
# (near fist x, y, aim, flip, far fist x, y, aim, flip) facing right, from move/fists.txt. Near = right hand, in front of the body; far = left hand, behind it.
MOVE_RIGHT = {
    "idle": [(123, 128, 15, 1, 150, 102, 0, 1)] * 4,
    "walk": [(65, 139, 120, -1, 144, 112, -5, 1), (114, 134, 20, 1, 145, 103, -5, 1), (135, 110, -5, 1, 127, 136, 30, 1), (113, 135, 20, 1, 144, 103, -5, 1)],
}
RIGHT_FIX = {("attack3", "up", 4): dict(x=135, y=106, angle=-5, sx=1, behind=T), ("hurt", "up", 0): None, ("hurt", "up", 1): dict(x=128, y=138, angle=70, sx=1, behind=T)}

# Screen-left is the hero's right hand on a down row.
ATTACK3_DOWN = {"right": [], "left": []}
IDLE_DOWN = {"right": None, "left": None}
# Facing the camera or away the pistols point along the facing (user, 2026-11-02): one art seen along the barrel, on every tier until each tier has its own.
IDLE_UP = {"left": (57, 114), "right": (132, 92)}  # from behind screen-left is the hero's left hand


def load_fists():
    """Read from Tools grids of the new rows, about 3 px accurate."""
    global ATTACK3_DOWN, IDLE_DOWN
    IDLE_DOWN = {"right": (81, 127), "left": (116, 107)}
    ATTACK3_DOWN = {"right": [p[0] for p in FISTS["attack3-down"]], "left": [p[1] for p in FISTS["attack3-down"]]}


# Cannon on the back: centre relative to the head (hair centre x, top of hair).
BACK = {
    "down": dict(art="front", dx=6, dy=74, aim=-44, flip=1, behind=T),
    "up": dict(art="back", dx=-10, dy=84, aim=-149, flip=1, behind=F),
    "right": dict(art="side", dx=-30, dy=62, aim=-135, flip=1, behind=T),
    "left": dict(art="side", dx=30, dy=62, aim=-45, flip=-1, behind=T),
}


def cannon_anchor(art, x, y, aim, flip, behind, length=1.0):
    base = CANNON_BASE[art] if flip > 0 else 180 - CANNON_BASE[art]
    angle = aim - base
    k = SIDE_SX if art == "side" else length
    return dict(x=round(float(x), 2), y=round(float(y), 2), angle=round(float(((angle + 180) % 360) - 180), 2), s=CANNON_S, sx=flip * k, behind=behind, hidden=False, fallback=False, art=f"{art}")


def head_of(cell):
    p = cell
    m = bm.hair_mask(p)
    ys, xs = np.nonzero(m)
    return (xs.min() + xs.max()) / 2, ys.min()


def full(a, s):
    if a is None:
        return dict(x=96, y=96, angle=0, s=s, sx=1, behind=False, hidden=True, fallback=False)
    out = dict(x=a["x"], y=a["y"], angle=a["angle"], s=s, sx=a.get("sx", 1), behind=a["behind"], hidden=False, fallback=False)
    if a.get("art"):
        out["art"] = a["art"]
    return out


def build_anchors(bodies):
    load_fists()
    live = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in json.load(open(GEAR, encoding="utf-8"))["anchors"]}
    out = {}

    def cell(anim, d, f):
        row = 3 if d == "left" and anim in MIRRORED else ROWS[d]
        c = bodies[(anim, row)][f] if (anim, row) in bodies else old_cell(anim, row, f)
        return c[:, ::-1] if row != ROWS[d] else c

    for anim, n in COUNT.items():
        for d in DIRS:
            for f in range(n):
                # the two pistols
                if anim in HELD_ANIMS:
                    r, l = None, None
                elif anim == "attack1":
                    src = {"down": "down", "up": "up", "right": "left", "left": "right"}[d]
                    l = mirror(live[("gun", "attack1", src)][f])
                    r = mirror(OLD_LEFT_ATTACK1[src][f])
                elif anim in MOVE_RIGHT and d in ("right", "left"):
                    nx, ny, na, nf, fx, fy, fa, ff = MOVE_RIGHT[anim][f]
                    if anim == "idle":
                        ny, fy = bm.lifted("idle", ny, f), bm.lifted("idle", fy, f)
                    r, l = A(nx, ny, na, nf, F), A(fx, fy, fa, ff, T)
                    if d == "left":
                        r, l = mirror(l), mirror(r)  # near is the left hand now, far the right
                elif anim == "idle" and d == "down":
                    r, l = [dict(A(p[0], bm.lifted("idle", p[1], f), 0, 1, F), art="pistol-front") for p in (IDLE_DOWN["right"], IDLE_DOWN["left"])]
                elif anim == "idle" and d == "up":
                    r, l = [dict(A(p[0], bm.lifted("idle", p[1], f), 0, 1, F), art="pistol-back") for p in (IDLE_UP["right"], IDLE_UP["left"])]
                elif anim == "attack3" and d == "down":
                    r, l = A(*ATTACK3_DOWN["right"][f], 180, -1, F), A(*ATTACK3_DOWN["left"][f], 0, 1, F)
                else:
                    r = RIGHT_FIX.get((anim, d, f), live[("gun", anim, d)][f])
                    l = LEFT[(anim, d)][f]
                if d == "up" and anim in ("attack1", "attack3") and l is not None:
                    l = dict(l, behind=False)  # that arm is under the cannon on an up row: pistol and fist go over the barrel
                out.setdefault(("gun", anim, d), []).append(full(r, PISTOL_S))
                out.setdefault(("gunL", anim, d), []).append(full(l, PISTOL_S))
                # the cannon
                if anim in HELD_ANIMS:
                    continue
                hx, top = head_of(cell(anim, d, f))
                b = BACK[d]
                out.setdefault(("cannon", anim, d), []).append(cannon_anchor(b["art"], hx + b["dx"], top + b["dy"], b["aim"], b["flip"], b["behind"]))
    held = held_cannon(bodies, cell)
    for d, frames in held.items():
        out[("cannon", "attack4", d)] = frames
    for d in ("down", "up", "right"):
        out[("cannon", "rocket", d)] = [cannon_anchor(*ROCKET[d])]
    art_, x, y, aim, flip, behind = ROCKET["right"]
    out[("cannon", "rocket", "left")] = [cannon_anchor(art_, FRAME - x, y, 180 - aim, -flip, behind)]
    return out


def along(rear, fore, frac_from_tail):
    """Centre of a cannon that passes through both fists, the rear fist at frac_from_tail of its length from the tail."""
    dx, dy = fore[0] - rear[0], fore[1] - rear[1]
    d = math.hypot(dx, dy) or 1
    aim = math.degrees(math.atan2(dy, dx))
    off = (0.5 - frac_from_tail) * CANNON_LEN
    return rear[0] + dx / d * off, rear[1] + dy / d * off, aim


def held_cannon(bodies, cell):
    """Hit 4. Timing inside the existing 480 ms: 6 frames of 80 ms, the bolt leaves at 140 ms, so frame 0 grabs it off the back, 1 swings, 2-4 fire and recoil, 5 lifts it back."""
    out = {}
    for d in ("down", "up", "right"):
        out[d] = [cannon_anchor(*h, length=HELD_LEN.get((d, f), 1.0)) for f, h in enumerate(HELD[d])]
    out["left"] = [cannon_anchor(art, FRAME - x, y, 180 - aim, -flip, behind) for art, x, y, aim, flip, behind in HELD["right"]]
    return out


def on_line(art, rear, fore, frac, flip, behind):
    x, y, aim = along(rear, fore, frac)
    return art, x, y, aim, flip, behind


def at(art, point, aim, frac, flip, behind, length=1.0):
    """Cannon centre so the barrel at `aim` passes through `point`, frac of its length from the tail."""
    off = (0.5 - frac) * CANNON_LEN * length
    return art, point[0] + math.cos(math.radians(aim)) * off, point[1] + math.sin(math.radians(aim)) * off, aim, flip, behind


def mid(p):
    return (p[0][0] + p[1][0]) / 2, (p[0][1] + p[1][1]) / 2


R, D, U = FISTS["hit4-right"], FISTS["hit4-down"], FISTS["hit4-up"]
DOWN_LEN = 0.8  # pointing at the viewer's feet: a little foreshortened
HELD = {
    "right": [on_line("side", (90, 100), R[0][1], 0.5, 1, T)] + [on_line("side", R[i][0], R[i][1], 0.22, 1, F) for i in (2, 3, 4)] + [on_line("side", R[1][0], R[1][1], 0.35, 1, F), on_line("side", R[5][0], R[5][1], 0.3, 1, F)],
    "down": [at("front", mid(D[0]), -44, 0.5, 1, T)] + [at("front", mid(D[1]), 90, 0.35, 1, F, DOWN_LEN)] * 4 + [at("front", mid(D[5]), 90, 0.35, 1, F, DOWN_LEN)],
    "up": [at("back", mid(U[0]), -149, 0.5, 1, F)] + [at("back", mid(U[1]), -90, 0.35, 1, T)] * 4 + [at("back", mid(U[5]), -90, 0.35, 1, T)],
}
HELD_LEN = {("down", f): DOWN_LEN for f in range(1, 6)}
# fists drawn over the cannon when it is in front of the body
HELD_FISTS = {
    "right": [[]] + [list(R[i]) for i in (2, 3, 4)] + [list(R[1]), list(R[5])],
    "down": [[]] + [list(D[1])] * 4 + [list(D[5])],
    "up": [list(U[0])] + [[]] * 5,
    "left": [[]] * 6,
}
ROCKET = {"right": at("side", mid(R[1]), -45, 0.35, 1, F), "down": at("front", mid(D[0]), -44, 0.3, 1, F), "up": at("back", mid(U[1]), -50, 0.4, 1, T)}
ROCKET_FISTS = {"right": list(R[1]), "down": list(D[0]), "up": [], "left": []}


def hand_cell(cell, fronts):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    m = np.zeros((FRAME, FRAME))
    for x, y, r in fronts:
        m = np.maximum(m, np.clip(r - np.hypot(xx - x, yy - y), 0, 1))
    return cell * m[..., None]


def fronts_of(anchors, anim, d, f):
    fr = []
    for part in ("gun", "gunL"):
        a = anchors[(part, anim, d)][f]
        if not a["hidden"] and not a["behind"]:
            fr.append((a["x"], a["y"], FIST_R))
    if anim == "attack4":
        fr += [(h[0], h[1], FIST_R + 1) for h in HELD_FISTS[d][f]]
    if anim == "rocket":
        fr += [(h[0], h[1], FIST_R + 1) for h in ROCKET_FISTS[d]]
    return fr


def weapon_manifest(live, pistol):
    """One entry per tier under the tier's name: pistol/<name>-base|enhanced.png, and the cannon's cannon/<name>/ follows it. Grips measured by Tools/build-mechanic-pistol-tiers.py; a tier not drawn yet holds the master, so the master's grip."""
    grip = {"w": pistol.width, "h": pistol.height, "gripX": PISTOL_GRIP[0], "gripY": PISTOL_GRIP[1]}
    tiers = json.load(open(TIER_GRIPS, encoding="utf-8")) if os.path.exists(TIER_GRIPS) else {}
    return [{"name": e["name"], "glow": e["glow"], "baseGrip": tiers.get(e["name"], {}).get("base", grip), "enhancedGrip": tiers.get(e["name"], {}).get("enhanced", grip)} for e in live["weapon"]]


def write_all(root, bodies, anchors, art):
    """root/Sprites-style layout under root; game paths when root is RES."""
    body = f"{root}/Sprites/classes/mechanic/body"
    for layer in ("bare", "armed", "hand"):
        os.makedirs(f"{body}/{layer}", exist_ok=True)
    for anim, n in COUNT.items():
        if anim == "dash":
            continue
        bare = old_sheet("bare", anim) if anim not in HELD_ANIMS else np.zeros((4 * FRAME, n * FRAME, 4), np.uint8)
        hand = old_sheet("hand", anim) if anim not in HELD_ANIMS else np.zeros((4 * FRAME, n * FRAME, 4), np.uint8)
        for r, d in enumerate(DIRS):
            mirrored = d == "left" and anim in MIRRORED
            for f in range(n):
                y0, x0 = r * FRAME, f * FRAME
                if (anim, r) in bodies:
                    c = bodies[(anim, r)][f]
                    bare[y0:y0 + FRAME, x0:x0 + FRAME] = wm.to_rgba(c)
                elif anim not in HELD_ANIMS:
                    c = premul(bare[y0:y0 + FRAME, x0:x0 + FRAME])
                else:
                    continue
                if mirrored:
                    continue  # the game flips the right row, hand layer included
                hand[y0:y0 + FRAME, x0:x0 + FRAME] = wm.to_rgba(hand_cell(c, fronts_of(anchors, anim, d, f)))
        Image.fromarray(bare, "RGBA").save(f"{body}/bare/{anim}.png")
        Image.fromarray(hand, "RGBA").save(f"{body}/hand/{anim}.png")
        # armed: only the frame count, and row 0 of idle for the bag portrait
        if anim in ("attack1", "attack3") + HELD_ANIMS:
            Image.fromarray(bare, "RGBA").save(f"{body}/armed/{anim}.png")
    armed_idle = old_sheet("armed", "idle")
    for f in range(4):
        c = bodies[("idle", 0)][f]
        armed_idle[0:FRAME, f * FRAME:(f + 1) * FRAME] = wm.to_rgba(c)
    Image.fromarray(armed_idle, "RGBA").save(f"{body}/armed/idle.png")
    os.makedirs(f"{root}/Sprites/classes/mechanic/weapons/pistol", exist_ok=True)
    os.makedirs(f"{root}/Sprites/classes/mechanic/weapons/cannon", exist_ok=True)
    art["pistol"].save(f"{root}/Sprites/classes/mechanic/weapons/pistol/pistol-base.png")
    art["pistol"].save(f"{root}/Sprites/classes/mechanic/weapons/pistol/pistol-enhanced.png")
    for n in ("pistol-front", "pistol-back"):
        art[n].save(f"{root}/Sprites/classes/mechanic/weapons/pistol/{n}.png")
    for n in ("front", "back", "side"):
        art[n].save(f"{root}/Sprites/classes/mechanic/weapons/cannon/{n}.png")
    live = json.load(open(GEAR, encoding="utf-8"))
    weapon = weapon_manifest(live, art["pistol"])
    sets =[{"part": k[0], "anim": k[1], "dir": k[2], "frames": v} for k, v in anchors.items()]
    os.makedirs(f"{root}/Data/classes/mechanic", exist_ok=True)
    with open(f"{root}/Data/classes/mechanic/held-mechanic-dual.json", "w", encoding="utf-8") as fp:
        json.dump({"weapon": weapon, "companion": live["companion"], "companionFloat": live.get("companionFloat", []), "anchors": sets}, fp, separators=(",", ":"))


def write_meta():
    """New files take a neighbour's settings with their own guid; a guid Unity already knows stays."""
    pairs = [(f"{BODY}/{l}/{a}.png", f"{BODY}/{l}/attack1.png.meta") for l in ("bare", "armed", "hand") for a in HELD_ANIMS]
    for n in ("pistol/pistol-base", "pistol/pistol-enhanced", "pistol/pistol-front", "pistol/pistol-back", "cannon/front", "cannon/back", "cannon/side"):
        pairs.append((f"{WEAPONS}/{n}.png", f"{WEAPONS}/gun/scrap-base.png.meta"))
    pairs.append((GEAR_DUAL, GEAR + ".meta"))
    for path, src in pairs:
        if os.path.exists(path + ".meta"):
            continue
        text = open(src, "rb").read().decode()
        open(path + ".meta", "wb").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1).encode())


# ---- pictures, as HeroRenderer draws ----
def place(canvas, art, grip, a, pad):
    k = a["s"]
    sx = a.get("sx", 1) or 1
    th = math.radians(a["angle"])
    cos, sin = math.cos(th), math.sin(th)
    ax, bx = cos / (k * sx), sin / (k * sx)
    ay, by = -sin / k, cos / k
    px, py = a["x"] + pad, a["y"] + pad
    canvas.alpha_composite(art.transform(canvas.size, Image.AFFINE, (ax, bx, grip[0] - ax * px - bx * py, ay, by, grip[1] - ay * px - by * py), Image.BICUBIC))


def compose(root, art, anchors, anim, d, f, pad=70):
    mirrored = d == "left" and anim in MIRRORED
    row = 3 if mirrored else ROWS[d]
    body = f"{root}/Sprites/classes/mechanic/body"
    def sheet(layer):
        path = f"{body}/{layer}/{anim}.png"
        if not os.path.exists(path):
            path = f"{BODY}/{layer}/{anim}.png"
        im = Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))
        return im.transpose(Image.FLIP_LEFT_RIGHT) if mirrored else im
    canvas = Image.new("RGBA", (FRAME + 2 * pad,) * 2, (0, 0, 0, 0))
    def part(p, behind):
        a = anchors[(p, anim, d)][f]
        if a.get("hidden") or bool(a["behind"]) != behind:
            return
        if p == "cannon":
            im = art[a["art"]]
            place(canvas, im, (im.width / 2, im.height / 2), a, pad)
        else:
            im = art[a["art"]] if a.get("art") else art["pistol"]
            place(canvas, im, (im.width / 2, im.height / 2) if a.get("art") else PISTOL_GRIP, a, pad)
    for behind in (True, False):
        for p in ("cannon", "gunL", "gun"):
            part(p, behind)
        if behind:
            canvas.alpha_composite(sheet("bare"), (pad, pad))
    canvas.alpha_composite(sheet("hand"), (pad, pad))
    return canvas


def grid(out):
    os.makedirs(out, exist_ok=True)
    b = new_bodies()
    z = 3
    for (anim, row), cells in b.items():
        n = len(cells)
        w = Image.new("RGB", (FRAME * z * n, FRAME * z), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for f, c in enumerate(cells):
            im = Image.fromarray(wm.to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (f * FRAME * z, 0), im)
            for v in range(0, FRAME, 16):
                s = v % 48 == 0
                col = (255, 255, 0) if s else (110, 110, 110)
                d.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=col)
                d.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=col)
                if s:
                    d.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w.save(f"{out}/new-{anim}-{DIRS[row]}.png")


def preview(out):
    os.makedirs(out, exist_ok=True)
    bodies = new_bodies()
    anchors = build_anchors(bodies)
    art = arts()
    write_all(out, bodies, anchors, art)
    size = FRAME + 140
    plan = [(a, d) for a in ("idle", "attack1", "attack2", "attack3", "attack4", "walk", "hurt", "dash") for d in DIRS]
    for a in ("idle", "attack1", "attack2", "attack3", "attack4", "walk", "hurt", "dash"):
        rows = [p for p in plan if p[0] == a]
        n = COUNT[a]
        s = Image.new("RGBA", (70 + n * size, 4 * size), (58, 62, 70, 255))
        d_ = ImageDraw.Draw(s)
        for r, (anim, d) in enumerate(rows):
            d_.text((4, r * size + 4), f"{anim}\n{d}", fill=(255, 255, 0))
            for f in range(n):
                s.alpha_composite(compose(out, art, anchors, anim, d, f), (70 + f * size, r * size))
        s.convert("RGB").save(f"{out}/sheet-{a}.png")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "backup":
        for layer in ("bare", "armed", "hand"):
            os.makedirs(f"{OLD}/{layer}", exist_ok=True)
            for anim in ("idle", "attack1", "attack2", "attack3", "walk", "hurt", "dash"):
                path = f"{BODY}/{layer}/{anim}.png"
                if os.path.exists(path) and not os.path.exists(f"{OLD}/{layer}/{anim}.png"):
                    shutil.copy2(path, f"{OLD}/{layer}/{anim}.png")
    elif cmd == "grid":
        grid(sys.argv[2])
    elif cmd == "preview":
        preview(sys.argv[2])
    elif cmd == "write":
        bodies = new_bodies()
        write_all(RES, bodies, build_anchors(bodies), arts())
        write_meta()
        print("written")
    elif cmd == "muzzles":
        # world offsets from the feet of each muzzle, for HeroFx.MechanicMuzzle: firing frames, pistol tip = right end of the art at mid height of the barrel
        bodies = new_bodies()
        an = build_anchors(bodies)
        art = arts()
        tip = (PISTOL_LEN - PISTOL_GRIP[0], round(art["pistol"].height * 0.31) - PISTOL_GRIP[1])  # art px from the grip
        def pistol(p, anim, d, f):
            a = an[(p, anim, d)][f]
            if a["hidden"]:
                return None
            th = math.radians(a["angle"])
            sx = a["sx"] * a["s"]
            x, y = tip[0] * sx, tip[1] * a["s"]
            return a["x"] + x * math.cos(th) - y * math.sin(th), a["y"] + x * math.sin(th) + y * math.cos(th)
        def cannon(d, f):
            a = an[("cannon", "attack4", d)][f]
            base = CANNON_BASE[a["art"]] if a["sx"] > 0 else 180 - CANNON_BASE[a["art"]]
            aim = math.radians(a["angle"] + base)
            k = abs(a["sx"])
            return a["x"] + CANNON_LEN * k / 2 * math.cos(aim), a["y"] + CANNON_LEN * k / 2 * math.sin(aim)
        for d in DIRS:
            row = [pistol("gunL", "attack1", d, 1), pistol("gun", "attack2", d, 1), pistol("gun", "attack3", d, 1), pistol("gunL", "attack3", d, 1), cannon(d, 1)]
            print(d, ["none" if p is None else f"{(p[0]-96)/8:.1f}f, {(192-p[1])/8:.1f}f" for p in row], "frame px", [None if p is None else (round(p[0]), round(p[1])) for p in row])

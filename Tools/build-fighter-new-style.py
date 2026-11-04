"""Fighter (brawler) body sheets in the new style, cut from ChatGPT strips on flat #00FF00, plus the glove anchors.
Trimmed copy of Tools/build-assassin-new-style.py (plans/261104-0025-fighter-class/phase-02-body-sprites.md).

Sources: Assets/ArtLibrary/classes/fighter/new-style/src/<anim>-<dir>.png, one row of frames, dir in down / up / right.
Left rows are the right rows mirrored (HeroClassDef.MirrorLeft is empty, so the sheet holds the mirrored row and the anchors swap hands).
The hands are empty in the art; the worn gloves draw at the anchors: part `main` = right fist, part `off` = left fist. Glove art has the
knuckles pointing right and the thumb up; anchor `angle` = clockwise degrees from pointing-right to where the knuckles point, `sx` = -1
flips the art so the thumb faces the body midline on the far-side fist. Facing right the near fist is the right hand (main).

Rows are HEIGHT px head to sole standing, soles on SOLE_ROW; crouched rows keep the head size of the idle row (head width).
Idle rows use frame 0 breathed (LIFT), like the other classes: four separately painted frames flicker.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-fighter-new-style.py cut OUT_DIR       frames on a pixel grid with the auto fist candidates marked, to fill FISTS
  python Tools/build-fighter-new-style.py fists OUT_DIR     frames with the chosen fists marked, M main and O off
  python Tools/build-fighter-new-style.py preview OUT_DIR   sheets and composites with a glove at each fist as the game draws; Resources untouched
  python Tools/build-fighter-new-style.py write             bare and armed sheets, metas, anchors of held-fighter.json
  python Tools/build-fighter-new-style.py armed             armed sheets only, from the first tier as installed
"""
import importlib.util, json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


H = load_module("fighter_strip_helpers", os.path.join(os.path.dirname(os.path.abspath(__file__)), "fighter-strip-helpers.py"))
key_green, split, resize_pm, to_rgba, box, place, centre_x, lift_at, breathe, grid_sheet, new_guid_copy = (
    H.key_green, H.split, H.resize_pm, H.to_rgba, H.box, H.place, H.centre_x, H.lift_at, H.breathe, H.grid_sheet, H.new_guid_copy)

FRAME, HEIGHT, SOLE_ROW = 192, 164, 188
LIFT = H.LIFT  # breathing, frame px
GLOVE_S, DRAW_SCALE = 0.24, 1.0  # one glove size on every frame (rule 3); HeroClassDef Fighter parts DrawScale. 0.18 (36 px) looked like a plain boxing glove; 0.24 = 48 px, nearer the hero-select gauntlets (2026-11-04)
CHEST_Y, GUARD_R = 112.0, 20.0
GUARD = {"right": -60.0, "down": 90.0, "up": -90.0}  # knuckle direction of a fist tucked at the chin
CONE = {"right": (-110.0, 40.0), "down": (30.0, 150.0), "up": (-150.0, -30.0)}  # knuckles always face the enemy
HEAD_FIT = {}  # (anim, dir): factor on the head-width scale, read against idle at the preview
COUNT = {"idle": 4, "walk": 4, "hurt": 2, "melee1": 6, "melee2": 6, "melee3": 6, "melee4": 6, "dash": 3}
DIRS = ("down", "up", "right")
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
RES = "Assets/Resources"
GEAR = f"{RES}/Data/classes/fighter/held-fighter.json"
BODY = f"{RES}/Sprites/classes/fighter/body"
MODEL_BODY = f"{RES}/Sprites/classes/assassin/body"
GLOVES = f"{RES}/Sprites/classes/fighter/weapons/glove"
SRC = "Assets/ArtLibrary/classes/fighter/new-style/src"
# Fist centres per frame in the cut cell (px), read from the `cut` grid: (main = right fist, off = left fist).
# down: screen left = right hand = main. up: screen right = right hand = main. right: near fist = right hand = main.
# None = that fist is hidden behind the body (anchor `hidden`); a hidden guard fist seen from the back gets a guessed point and `behind`.
FISTS = {
    ("idle", "right"): [((107, 115), (127, 110))],
    ("melee1", "right"): [((100, 108), (118, 100)), ((104, 110), (130, 98)), ((104, 109), (158, 100)), ((102, 108), (148, 103)), ((98, 108), (120, 99)), ((104, 106), (120, 98))],
    ("melee2", "right"): [((100, 107), (118, 99)), ((130, 98), (104, 110)), ((160, 99), (104, 109)), ((150, 101), (102, 108)), ((122, 99), (98, 108)), ((104, 106), (120, 98))],
    ("melee3", "right"): [((100, 109), (122, 98)), ((104, 112), (136, 90)), ((100, 112), (140, 84)), ((102, 112), (136, 82)), ((106, 110), (126, 101)), ((102, 106), (118, 98))],
    ("melee4", "right"): [((118, 112), (100, 104)), ((120, 108), (102, 100)), ((123, 70), (104, 94)), ((120, 8), (118, 62)), ((128, 45), (118, 92)), ((102, 106), (118, 98))],
    ("hurt", "right"): [((60, 121), (118, 118)), ((58, 134), (138, 100))],
    ("dash", "right"): [((134, 121), (156, 117)), ((146, 120), (162, 118)), ((118, 112), (136, 103))],
    ("idle", "down"): [((68, 138), (123, 138))],  # 2026-11-04: relaxed stance, arms down (the guard pose hid the face under 48 px gloves); redrawn 1:1 from the guard strip so the chibi proportions stay
    ("walk", "down"): [((66, 133), (122, 133)), ((67, 134), (122, 134)), ((66, 133), (122, 133)), ((67, 134), (122, 134))],  # 2026-11-04: arms swing low at the sides, redrawn 1:1 from the guard strip
    ("melee1", "down"): [((78, 100), (110, 99)), ((78, 100), (112, 98)), ((76, 99), (112, 101)), ((76, 99), (110, 99)), ((78, 100), (110, 98)), ((78, 100), (110, 98))],
    ("melee2", "down"): [((78, 99), (110, 98)), ((78, 100), (110, 98)), ((78, 102), (112, 98)), ((76, 101), (110, 98)), ((78, 100), (110, 98)), ((78, 100), (110, 98))],
    ("melee3", "down"): [((78, 100), (110, 98)), ((77, 100), (129, 90)), ((100, 94), (70, 104)), ((103, 95), (64, 102)), ((80, 100), (112, 96)), ((78, 100), (110, 98))],
    ("melee4", "down"): [((68, 130), (112, 127)), ((76, 112), (108, 110)), ((84, 77), (111, 84)), ((76, 8), (111, 83)), ((76, 32), (111, 90)), ((80, 101), (109, 99))],
    ("hurt", "down"): [((62, 118), (128, 118)), ((50, 110), (152, 110))],
    ("dash", "down"): [((76, 105), (112, 106)), ((67, 102), (129, 102)), ((80, 100), (120, 100))],
    ("idle", "up"): [((128, 86), (67, 87))],
    ("walk", "up"): [((125, 84), (68, 84)), ((128, 84), (69, 84)), ((126, 84), (68, 84)), ((126, 84), (68, 84))],
    ("melee1", "up"): [((124, 81), (60, 80)), ((126, 80), (62, 80)), ((123, 78), (58, 56)), ((124, 79), (60, 55)), ((124, 79), (61, 69)), ((124, 81), (60, 80))],
    ("melee2", "up"): [((124, 81), (60, 80)), ((139, 71), (62, 80)), ((131, 59), (62, 80)), ((130, 57), (62, 80)), ((133, 68), (62, 80)), ((124, 81), (60, 80))],
    ("melee3", "up"): [((127, 74), (55, 73)), ((128, 74), (50, 71)), ((128, 76), (116, 62)), ((127, 75), (137, 56)), ((127, 74), (57, 73)), ((127, 74), (54, 73))],
    ("melee4", "up"): [((129, 114), (63, 89)), ((130, 81), (60, 85)), ((129, 65), (60, 65)), ((128, 13), (62, 65)), ((129, 35), (60, 65)), ((120, 65), (60, 65))],
    ("hurt", "up"): [((139, 93), (52, 94)), ((146, 83), (35, 100))],
    ("dash", "up"): [((129, 81), (59, 81)), ((119, 78), (48, 78)), ((131, 81), (63, 81))],
    ("walk", "right"): [((101, 115), (123, 108)), ((110, 116), (130, 107)), ((104, 114), (122, 108)), ((108, 116), (130, 107))],
}
# (anim, dir, frame): (main, off) overrides of the behind flags (None keeps the rule). Back view: a punch extended away rises beside the
# hair, where the rule would hide the glove behind the head.
BEHIND = {("melee1", "up", 2): (None, False), ("melee1", "up", 3): (None, False), ("melee1", "up", 4): (None, False),
          ("melee2", "up", 2): (False, None), ("melee2", "up", 3): (False, None), ("melee2", "up", 4): (False, None),
          ("melee3", "up", 3): (None, False),  # hook impact: the fist comes out past the far side of the head
          **{("melee4", "up", f): (False, None) for f in range(5)}}  # uppercut: the fist rises beside the body, then above the hair
# (anim, dir, frame): (main or None, off or None) knuckle directions. Front and back rows: the uppercut rises up the screen (knuckles up,
# outside the facing cone) and the hook crosses the body (knuckles along the swing).
ANGLE_FIX = {("idle", "down", 0): (95, 85), ("idle", "down", 1): (95, 85), ("idle", "down", 2): (95, 85), ("idle", "down", 3): (95, 85),  # hanging fists: knuckles down, a touch outward
             ("walk", "down", 0): (95, 85), ("walk", "down", 1): (95, 85), ("walk", "down", 2): (95, 85), ("walk", "down", 3): (95, 85),
             ("melee3", "down", 2): (None, 180), ("melee3", "down", 3): (None, 180),
             ("melee4", "down", 2): (-90, None), ("melee4", "down", 3): (-90, None), ("melee4", "down", 4): (-90, None)}


def source(anim, direction):
    p = f"{SRC}/{anim}-{direction}.png"
    return p if os.path.exists(p) else None


def head_width(pm):
    """Widest row of the hair mass (top 30% of the figure): the size check between rows of one direction."""
    a = pm[..., 3] > 128
    rows = np.where(a.any(1))[0]
    top, h = rows[0], rows[-1] - rows[0] + 1
    return int(a[top:top + int(0.3 * h)].sum(1).max())


HAIR_R, HAIR_GB = 71.0, 0.42  # mean red of the dark-brown hair in the right-facing strips (the first ones drawn); the front and back strips came out auburn


def match_hair(pm):
    """Dark reds (hair, pants, harness: r well above g and b, not the bright headband or armour) scaled so the hair's red matches the right rows."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g, b = pm[..., 0] / a[..., 0], pm[..., 1] / a[..., 0], pm[..., 2] / a[..., 0]
    dark = (pm[..., 3] > 128) & (r > 1.5 * np.maximum(g, b)) & (r < 190) & (r > 40)
    rows = np.where((pm[..., 3] > 128).any(1))[0]
    head = np.zeros_like(dark)
    head[rows[0]:rows[0] + int(0.4 * (rows[-1] - rows[0]))] = True
    mean = r[dark & head].mean() if (dark & head).sum() > 100 else HAIR_R
    k = float(np.clip(HAIR_R / mean, 0.5, 1.0))
    out = pm.copy()
    out[..., 0] = np.where(dark, pm[..., 0] * k, pm[..., 0])
    for c in (1, 2):  # the back strips' hair is pure red (g, b near 15): lift them to the brown of the side rows (g, b about 0.42 r)
        out[..., c] = np.where(dark, np.maximum(pm[..., c], HAIR_GB * out[..., 0]), pm[..., c])
    return out


def row_cells(anim, direction, heads):
    """[192 px premultiplied cells] of a painted row and its scale. Soles snap to SOLE_ROW unless the frame is in the air (> 6 px)."""
    pm = key_green(source(anim, direction))
    if direction != "right":
        pm = match_hair(pm)
    parts = split(pm, COUNT[anim])
    if anim == "idle":
        k = HEIGHT / parts[0][0].shape[0]
        heads[direction] = head_width(resize_pm(parts[0][0], k))
    elif anim in ("walk", "dash"):  # the tallest frame stands HEIGHT (the dash's streaming hair would fool the head measure)
        k = HEIGHT / max(p.shape[0] for p, _ in parts)
    else:  # crouched or lunging rows would get a bigger head at 164 px: same head width as idle in this direction
        k = heads[direction] / float(np.median([head_width(p) for p, _ in parts])) * HEAD_FIT.get((anim, direction), 1.0)
    bottom = max(y + p.shape[0] for p, y in parts)
    small = [(resize_pm(p, k), (bottom - (y + p.shape[0])) * k) for p, y in parts]
    cells = []
    for f, air in small:
        y = SOLE_ROW + 1 - f.shape[0] - (round(air) if air > 6 else 0)
        xs = np.where((f[..., 3] > 8).any(0))[0]
        x = int(np.clip(round(FRAME / 2 - centre_x(f)), 3 - xs[0], FRAME - 4 - xs[-1]))  # feet centred, a long punch slid back inside the cell
        cells.append(place(f, y, x))
    if anim == "idle":
        cells = [breathe(cells[0], i) for i in range(4)]
    return cells, k


def skin_fists(cell):
    """Candidate fist centres: compact skin blobs below the face, after an opening that removes the forearms."""
    a = np.maximum(cell[..., 3], 1e-3) / 255
    r, g, b = cell[..., 0] / a, cell[..., 1] / a, cell[..., 2] / a
    skin = (cell[..., 3] > 128) & (r > 150) & (g > 85) & (g < 205) & (b < 175) & (r - b > 40) & (r > g)
    yy, xx = np.mgrid[-5:6, -5:6]
    opened = ndimage.binary_opening(skin, structure=(xx ** 2 + yy ** 2) <= 25)
    labels, n = ndimage.label(opened)
    out = []
    for i in range(1, n + 1):
        ys, xs = np.where(labels == i)
        if 30 <= len(xs) <= 420:
            out.append((int(round(xs.mean())), int(round(ys.mean())), len(xs)))
    return out


def rows_available():
    return [(a, d) for a in COUNT for d in DIRS if source(a, d)]


def cut(out):
    os.makedirs(out, exist_ok=True)
    heads = {}
    for anim, direction in sorted(rows_available(), key=lambda t: t[0] != "idle"):
        cells, k = row_cells(anim, direction, heads)
        boxes = [box(c[..., 3]) for c in cells]
        cands = {i: skin_fists(c) for i, c in enumerate(cells)}
        print(f"{anim:7s} {direction:5s} k {k:.3f} top {[b[1] for b in boxes]} sole {[b[3] - 1 for b in boxes]} head {[head_width(c) for c in cells]}")
        print("   fist candidates", {i: [(x, y) for x, y, _ in v] for i, v in cands.items()})
        grid_sheet(cells, f"{out}/grid-{anim}-{direction}.png", {i: [(x, y, f"{x},{y}", (0, 255, 255)) for x, y, _ in v] for i, v in cands.items()})


def head_x(cell):
    a = cell[..., 3] > 128
    top = np.where(a.any(1))[0][0]
    xs = np.where(a[top:top + 40].any(0))[0]
    return (xs[0] + xs[-1]) / 2


def knuckle_angle(cell, x, y, direction):
    """Knuckle direction: along the arm from the chest out through the fist, clamped to the facing cone; the guard angle when tucked in."""
    dx, dy = x - head_x(cell), y - CHEST_Y
    if math.hypot(dx, dy) < GUARD_R:
        return GUARD[direction]
    a = math.degrees(math.atan2(dy, dx))
    lo, hi = CONE[direction]
    if lo <= a <= hi:
        return round(a, 1)
    return lo if min(abs(a - lo), 360 - abs(a - lo)) < min(abs(a - hi), 360 - abs(a - hi)) else hi


def on_body(cell, x, y):
    ys, xs = np.mgrid[0:FRAME, 0:FRAME]
    r = np.hypot(xs - x, ys - y)
    return (cell[..., 3] > 128)[(r >= 13) & (r <= 17)].mean() > 0.8


def anchor(x, y, knuckles, flip, behind, hidden=False):
    """sx = -1 mirrors the glove art so the thumb faces the midline; its angle is then measured from pointing-left."""
    return {"x": round(float(x), 3), "y": round(float(y), 3), "angle": float(knuckles - 180 if flip else knuckles), "s": GLOVE_S, "sx": -1 if flip else 1,
            "behind": bool(behind), "hidden": bool(hidden), "fallback": False}


def flipped(a, behind):
    return {**a, "x": round(FRAME - a["x"], 3), "angle": -a["angle"], "sx": -a["sx"], "behind": behind}


def fists_of_row(anim, direction):
    table = FISTS[(anim, direction)]
    if anim != "idle":
        return table
    lift = lambda p, f: None if p is None else (p[0], p[1] + LIFT[f] * float(lift_at(np.array(float(p[1])))))
    return [(lift(table[0][0], f), lift(table[0][1], f)) for f in range(4)]


def build(partial=False):
    """{anim: {row: cells}}, {(part, anim, dir): anchor frames}; anims with all three painted directions (partial: any painted row)."""
    heads, sheets, anchors = {}, {}, {}
    ready = lambda a, d: source(a, d) and (a, d) in FISTS
    anims = [a for a in COUNT if (any if partial else all)(ready(a, d) for d in DIRS)]
    for anim in sorted(anims, key=lambda a: a != "idle"):
        for direction in [d for d in DIRS if ready(anim, d)]:
            cells, _ = row_cells(anim, direction, heads)
            fists = fists_of_row(anim, direction)
            mains, offs = [], []
            for f, cell in enumerate(cells):
                pair = []
                for part, p in zip(("main", "off"), fists[f]):
                    if p is None:
                        pair.append(anchor(FRAME / 2, CHEST_Y, GUARD[direction], False, True, hidden=True))
                        continue
                    x, y = p
                    body = on_body(cell, x, y)
                    if direction == "right":  # the far (left) fist hides behind the back; in front of or past the chest it shows
                        behind = part == "off" and not body and x < head_x(cell) - 8
                    elif direction == "up":  # seen from the back, a fist in front of the chest or beside the head (guard, punch away, uppercut) is behind the body
                        behind = body or abs(x - head_x(cell)) < 42
                    else:
                        behind = False
                    fix = BEHIND.get((anim, direction, f), (None, None))[0 if part == "main" else 1]
                    behind = behind if fix is None else fix
                    ang = knuckle_angle(cell, x, y, direction)
                    fix = ANGLE_FIX.get((anim, direction, f), (None, None))[0 if part == "main" else 1]
                    pair.append(anchor(x, y, ang if fix is None else fix, direction != "right" and part == "off", behind))
                mains.append(pair[0])
                offs.append(pair[1])
            anchors[("main", anim, direction)], anchors[("off", anim, direction)] = mains, offs
            sheets.setdefault(anim, {})[ROWS[direction]] = cells
            if direction == "right":  # mirrored row: the near fist is now the left hand (off), the far fist the right hand (main)
                sheets[anim][ROWS["left"]] = [c[:, ::-1].copy() for c in cells]
                anchors[("main", anim, "left")] = [flipped(offs[f], offs[f]["behind"]) for f in range(len(cells))]
                anchors[("off", anim, "left")] = [flipped(mains[f], False) for f in range(len(cells))]
    return sheets, anchors


def load_compose():
    return load_module("compose_hero_frame", "Tools/compose-hero-frame.py")


def glove_art():
    """(art, grip) of the first tier as installed, or a drawn placeholder disc with a knuckle mark on the right."""
    file = json.load(open(GEAR, encoding="utf-8"))
    entry = file["weapon"][0]
    path = f"{GLOVES}/{entry['name']}-base.png"
    if os.path.exists(path):
        return Image.open(path).convert("RGBA"), entry["baseGrip"]
    im = Image.new("RGBA", (200, 200), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((20, 30, 180, 170), fill=(150, 90, 50, 255), outline=(40, 20, 10, 255), width=8)
    d.ellipse((120, 50, 185, 150), fill=(190, 120, 70, 255), outline=(40, 20, 10, 255), width=6)
    d.ellipse((40, 40, 90, 80), fill=(210, 150, 100, 255))
    return im, {"w": 200, "h": 200, "gripX": 100, "gripY": 100}


def composite(comp, cell, pair, art, grip, pad=0, bg=None):
    """Body with the gloves at the anchors, drawn as the game draws: behind parts, body, front parts."""
    canvas = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), bg or (0, 0, 0, 0))
    for behind in (True, False):
        for a in pair:
            if bool(a["behind"]) == behind and not a["hidden"]:
                comp.place(canvas, art, grip, a, DRAW_SCALE, 0.0, pad)
        if behind:
            canvas.alpha_composite(cell, (pad, pad))
    return canvas


def write_sheets(bare_dir, armed_dir, sheets, anchors):
    comp = load_compose()
    art, grip = glove_art()
    for folder in (bare_dir, armed_dir):
        os.makedirs(folder, exist_ok=True)
    for anim, rows in sheets.items():
        n = max(len(c) for c in rows.values())
        bare = Image.new("RGBA", (FRAME * n, FRAME * 4), (0, 0, 0, 0))
        armed = Image.new("RGBA", (FRAME * n, FRAME * 4), (0, 0, 0, 0))
        for row, cells in rows.items():
            direction = [k for k, v in ROWS.items() if v == row][0]
            for f, cell in enumerate(cells):
                body = Image.fromarray(to_rgba(cell), "RGBA")
                bare.paste(body, (f * FRAME, row * FRAME))
                pair = (anchors[("off", anim, direction)][f], anchors[("main", anim, direction)][f])
                armed.paste(composite(comp, body, pair, art, grip), (f * FRAME, row * FRAME))
        bare.save(f"{bare_dir}/{anim}.png")
        armed.save(f"{armed_dir}/{anim}.png")
        if anim == "hurt":  # death: no row painted, HeroRenderer loads armed/death, so the hit pose stands in
            bare.save(f"{bare_dir}/death.png")
            armed.save(f"{armed_dir}/death.png")


def write_anchors(anchors):
    """Read, change, write straight away: another tool edits the `weapon` list of this file. Keeps the compact one-line format."""
    raw = open(GEAR, "rb").read()
    data = json.loads(raw)
    keep = [a for a in data.get("anchors", []) if (a["part"], a["anim"], a["dir"]) not in anchors]
    data["anchors"] = keep + [{"part": p, "anim": a, "dir": d, "frames": fr} for (p, a, d), fr in anchors.items()]
    open(GEAR, "wb").write(json.dumps(data, separators=(",", ":")).encode())


def write_metas(anims):
    new_guid_copy(f"{RES}/Sprites/classes/assassin.meta", f"{RES}/Sprites/classes/fighter.meta")
    new_guid_copy(f"{MODEL_BODY}.meta", f"{BODY}.meta")
    for kind in ("bare", "armed"):
        new_guid_copy(f"{MODEL_BODY}/{kind}.meta", f"{BODY}/{kind}.meta")
        for anim in anims + ["death"]:
            new_guid_copy(f"{MODEL_BODY}/bare/idle.png.meta", f"{BODY}/{kind}/{anim}.png.meta")


def preview(out, pad=40):
    """Sheets as copies and composites the way the game draws, with GIFs. Resources untouched."""
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    sheets, anchors = build(partial=True)
    write_sheets(f"{out}/bare", f"{out}/armed", sheets, anchors)
    art, grip = glove_art()
    size = FRAME + 2 * pad
    ms = {"idle": 240, "walk": 160, "hurt": 200, "dash": 120}
    for anim, rows in sheets.items():
        n = max(len(c) for c in rows.values())
        names = [d for d in ("down", "up", "left", "right") if ROWS[d] in rows]
        sheet = Image.new("RGBA", (size * n, size * len(names)), (58, 62, 70, 255))
        for r, d in enumerate(names):
            gif = []
            for f, cell in enumerate(rows[ROWS[d]]):
                pair = (anchors[("off", anim, d)][f], anchors[("main", anim, d)][f])
                c = composite(comp, Image.fromarray(to_rgba(cell), "RGBA"), pair, art, grip, pad, (58, 62, 70, 255))
                sheet.alpha_composite(c, (f * size, r * size))
                gif.append(c.convert("P", palette=Image.ADAPTIVE))
            ImageDraw.Draw(sheet).text((4, r * size + 4), f"{anim} {d}", fill=(255, 255, 0))
            gif[0].save(f"{out}/{anim}-{d}.gif", save_all=True, append_images=gif[1:], duration=ms.get(anim, 90), loop=0, disposal=2)
        sheet.convert("RGB").save(f"{out}/{anim}.png")
    print("previews in", out, "anims", list(sheets))


def debug_fists(out):
    os.makedirs(out, exist_ok=True)
    sheets, anchors = build(partial=True)
    for anim, rows in sheets.items():
        for d in [d for d in DIRS if ROWS[d] in rows]:
            marks = {}
            for f in range(len(rows[ROWS[d]])):
                for part, col in (("main", (0, 255, 255)), ("off", (255, 255, 0))):
                    a = anchors[(part, anim, d)][f]
                    if not a["hidden"]:
                        marks.setdefault(f, []).append((a["x"], a["y"], f"{part[0].upper()} {a['angle']:.0f}{' b' if a['behind'] else ''}", col))
            grid_sheet(rows[ROWS[d]], f"{out}/fists-{anim}-{d}.png", marks, z=2)


def write_armed():
    sheets, anchors = build()
    write_sheets(f"{out_tmp()}/bare", f"{BODY}/armed", sheets, anchors)
    print("armed written", list(sheets))


def out_tmp():
    p = os.path.join(os.environ.get("TEMP", "."), "fighter-bare-tmp")
    os.makedirs(p, exist_ok=True)
    return p


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "cut":
        cut(sys.argv[2])
    elif cmd == "fists":
        debug_fists(sys.argv[2])
    elif cmd == "preview":
        preview(sys.argv[2])
    elif cmd == "armed":
        write_armed()
    elif cmd == "write":
        s_, a_ = build()
        write_sheets(f"{BODY}/bare", f"{BODY}/armed", s_, a_)
        write_metas(list(s_))
        write_anchors(a_)
        print("written", list(s_))

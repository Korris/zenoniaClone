"""Assassin in the new style: idle (rows 1-3), walk, hurt, melee1-4, nightfall; bodies cut from ChatGPT art on flat #00FF00.
Follows docs/new-style-class-brief.md. dash and idle down (row 0) stay.

Sources: Assets/ArtLibrary/classes/assassin/new-style/src/<anim>-<dir>.png (one row of frames) and mv-<anim>-<dir>.png.
Old art, read for poses and angles: new-style/old/. The left rows are the right rows mirrored, as the old melee and nightfall rows were.
Two daggers of one art, so mirroring does not show as a hand swap in the picture. The anchors still keep the parts on their hands:
facing right the near fist is the right hand (main); facing left the far fist is the right hand (main).

Every frame of a row shares one scale. Rows are 164 px head to sole at their standing height, soles on SOLE_ROW.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-assassin-new-style.py measure              old soles and heights, head width of idle down
  python Tools/build-assassin-new-style.py cut OUT_DIR          frames with a pixel grid; Resources untouched
  python Tools/build-assassin-new-style.py heads OUT.png        first frame of every row beside idle down
  python Tools/build-assassin-new-style.py fists OUT_DIR        frames with the chosen fists marked
  python Tools/build-assassin-new-style.py preview OUT_DIR      sheets and composites as copies; Resources untouched
  python Tools/build-assassin-new-style.py write                bare, armed, hand sheets of complete anims, anchors in held-assassin-melee.json
  python Tools/build-assassin-new-style.py dagger-sheet OUT_DIR all tiers with a pair image in dagger-src/, grips measured; Resources untouched
  python Tools/build-assassin-new-style.py dagger-preview OUT_DIR TIER [base|enhanced]   one tier on the hero, every direction; Resources untouched
  python Tools/build-assassin-new-style.py dagger-install [TIER ...]  art, grips and icons of tiers with a pair image, default all of them
  python Tools/build-assassin-new-style.py armed                armed sheets only, from the first tier as installed (Shadow Clone); prints what the 192 px cell cuts
"""
import importlib.util, json, math, os, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

FRAME, HEIGHT, SOLE_ROW = 192, 164, 190  # idle down stands on row 190
LIFT = [0.0, -0.7, -1.4, -0.7]  # breathing, frame px, as in build-warrior-move.py
ANKLE_Y, WAIST_Y = 174.0, 129.0
HEAD_FIT = {("hurt", "down"): 0.9}  # eye span read against idle down at 2x
HEAD_GROW = {("walk", "down"): (1.1, 122, 111)}  # ChatGPT painted a smaller head: factor, neck row, head centre x
NECK_BLEND = 12
DAGGER_S = 0.243  # the class's melee size, on every frame (rule 3)
FIST_R, FEATHER = 8.5, 1.0
# Fist centres per frame in the cut row (cell px), read from the `cut` grid: (main fist, off fist). An idle row has one pose, so one entry.
# down: screen left = right hand = main. up: screen right = right hand = main. right: near (the arm drawn over the body) = right hand = main.
FISTS = {
    ("walk", "down"): [((65, 134), (125, 141)), ((67, 132), (124, 140)), ((65, 134), (122, 140)), ((69, 136), (125, 141))],
    ("walk", "up"): [((124, 143), (47, 146)), ((122, 143), (48, 147)), ((124, 147), (43, 145)), ((115, 152), (35, 137))],
    ("walk", "right"): [((79, 152), (132, 144)), ((91, 154), (126, 151)), ((81, 151), (132, 143)), ((98, 154), (125, 154))],
    ("idle", "up"): [((134, 142), (56, 141))],
    ("idle", "right"): [((92, 151), (131, 149))],
    ("hurt", "down"): [((61, 133), (115, 143)), ((56, 127), (120, 150))],
    ("hurt", "up"): [((117, 149), (41, 142)), ((123, 148), (50, 143))],
    ("hurt", "right"): [((85, 151), (135, 147)), ((81, 152), (135, 151))],
    ("melee1", "down"): [((100, 137), (122, 137)), ((141, 112), (130, 142)), ((98, 125), (136, 147)), ((57, 126), (137, 149)), ((68, 155), (138, 148)), ((93, 137), (119, 137))],
    ("melee1", "up"): [((128, 138), (87, 138)), ((150, 102), (86, 140)), ((156, 122), (89, 147)), ((172, 122), (88, 149)), ((150, 155), (81, 147)), ((123, 132), (86, 138))],
    ("melee1", "right"): [((136, 137), (128, 134)), ((86, 113), (136, 137)), ((146, 123), (89, 148)), ((167, 128), (87, 148)), ((143, 157), (110, 150)), ((134, 137), (128, 134))],
    ("melee2", "down"): [((43, 117), (125, 147)), ((91, 145), (110, 175)), ((86, 140), (146, 103)), ((80, 130), (143, 55)), ((80, 138), (140, 80)), ((81, 130), (106, 130))],
    ("melee2", "up"): [((150, 112), (68, 145)), ((130, 130), (71, 163)), ((131, 128), (51, 88)), ((128, 122), (48, 45)), ((141, 72), (75, 138)), ((123, 125), (83, 125))],
    ("melee2", "right"): [((180, 118), (90, 147)), ((101, 147), (141, 175)), ((96, 142), (163, 90)), ((100, 128), (165, 47)), ((95, 137), (166, 63)), ((124, 135), (144, 128))],
    ("melee3", "down"): [((97, 138), (155, 62)), ((100, 45), (130, 45)), ((103, 118), (131, 118)), ((68, 183), (163, 183)), ((68, 183), (165, 183)), ((106, 127), (124, 127))],
    ("melee3", "up"): [((132, 135), (60, 55)), ((120, 53), (91, 53)), ((136, 120), (86, 115)), ((160, 180), (55, 180)), ((161, 178), (56, 180)), ((131, 130), (79, 132))],
    ("melee3", "right"): [((92, 138), (140, 45)), ((130, 27), (108, 30)), ((148, 112), (113, 110)), ((150, 182), (65, 163)), ((148, 182), (66, 172)), ((141, 122), (124, 125))],
    ("melee4", "down"): [((73, 148), (125, 148)), ((70, 141), (128, 136)), ((70, 141), (114, 147)), ((95, 152), (112, 152)), ((72, 133), (122, 133)), ((84, 124), (100, 124))],
    ("melee4", "up"): [((123, 137), (67, 138)), ((146, 92), (80, 133)), ((123, 135), (66, 95)), ((147, 92), (73, 98)), ((130, 128), (79, 132)), ((121, 127), (76, 132))],
    ("melee4", "right"): [((78, 147), (70, 145)), ((165, 124), (78, 148)), ((62, 147), (180, 122)), ((162, 121), (160, 133)), ((117, 135), (102, 134)), ((126, 118), (114, 124))],
}
NEW_DESIGN = {"right": ("idle", "walk")}  # designed fresh, not laid on the old row
RES = "Assets/Resources"
GEAR, GEAR_MELEE = f"{RES}/Data/classes/assassin/held-assassin.json", f"{RES}/Data/classes/assassin/held-assassin-melee.json"
BODY = f"{RES}/Sprites/classes/assassin/body"
LIB = "Assets/ArtLibrary/classes/assassin/new-style"
SRC, OLD = f"{LIB}/src", f"{LIB}/old"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"idle": 1, "walk": 4, "hurt": 2, "melee1": 6, "melee2": 6, "melee3": 6, "melee4": 6, "nightfall": 6}


def source(anim, direction):
    for name in (f"mv-{anim}-{direction}", f"{anim}-{direction}"):
        if os.path.exists(f"{SRC}/{name}.png"):
            return f"{SRC}/{name}.png"
    return None


def key_green(path):
    """Premultiplied RGBA floats from art on flat green. The art has no green, so every green pixel counts, holes between the legs too."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0  # ChatGPT leaves off-colour pixels at the image edge
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        keep = np.isin(fg, 1 + np.where(sizes >= 400)[0])
        alpha = alpha * ndimage.binary_dilation(keep, iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def split(pm, count):
    """(art, top row) of count boxes of connected art, left to right, each masked to its own blobs so a neighbour's ponytail cannot come along."""
    mask = pm[..., 3] > 128
    labels, n = ndimage.label(ndimage.binary_dilation(mask, iterations=2))
    objs = ndimage.find_objects(labels)
    sizes = ndimage.sum(mask, labels, range(1, n + 1))
    big = sorted([(o, i + 1) for i, (o, s) in enumerate(zip(objs, sizes)) if s > 0.15 * max(sizes)], key=lambda t: t[0][1].start)
    if len(big) != count:
        raise SystemExit(f"{len(big)} blobs, expected {count}")
    return [(pm[o] * (labels[o] == label)[..., None], o[0].start) for o, label in big]


def resize_pm(pm, scale):
    w, h = round(pm.shape[1] * scale), round(pm.shape[0] * scale)
    return np.clip(np.dstack([np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]), 0, 255)


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def box(alpha, thr=128):
    ys, xs = np.where(alpha > thr)
    return (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1) if len(xs) else None


def measure():
    for anim in COUNT:
        a = np.asarray(Image.open(f"{OLD}/bare/{anim}.png").convert("RGBA"))[..., 3]
        for d, r in ROWS.items():
            bs = [box(a[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME]) for c in range(a.shape[1] // FRAME)]
            print(f"{anim:9s} {d:5s} top {[b[1] for b in bs]} sole {[b[3] - 1 for b in bs]}")
    print("idle down live", box(np.asarray(Image.open(f"{BODY}/bare/idle.png").convert("RGBA"))[:FRAME, :FRAME, 3]))


def hair_width(pm):
    """Width of the purple hair, ponytail included, in the top 45% of the figure: the size check between rows of one direction."""
    a = np.maximum(pm[..., 3], 1e-3) / 255
    r, g, b = pm[..., 0] / a, pm[..., 1] / a, pm[..., 2] / a
    mask = (b > g + 25) & (r > g + 8) & (pm[..., 3] > 128)
    xs = np.where(mask[: int(pm.shape[0] * 0.45)].sum(0) >= 2)[0]
    return int(xs[-1] - xs[0] + 1)


def old_row(anim, direction):
    a = np.asarray(Image.open(f"{OLD}/bare/{anim}.png").convert("RGBA")).astype(np.float64)
    r = ROWS[direction]
    return [a[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME] for c in range(a.shape[1] // FRAME)]


def row_scale(anim, direction, parts, walk_hair):
    if anim in REDRAWN:  # painted fresh at one size per strip: the ready stance (frame 0) stands READY_H tall
        return READY_H * HEAD_FIT.get((anim, direction), 1.0) / parts[0][0].shape[0]
    if anim in ("walk", "idle"):
        return HEIGHT / max(p.shape[0] for p, _ in parts)
    # Crouched poses would come out with a bigger head at 164 px: same hair width as walk in this direction, then HEAD_FIT, read against idle down.
    return walk_hair[direction] / hair_width(parts[0][0]) * HEAD_FIT.get((anim, direction), 1.0)


def place(part, y, x):
    cell = np.zeros((FRAME, FRAME, 4))
    sx0, sy0 = max(0, -x), max(0, -y)
    ex, ey = min(FRAME, x + part.shape[1]), min(FRAME, y + part.shape[0])
    cell[y + sy0:ey, x + sx0:ex] = part[sy0:ey - y, sx0:ex - x]
    return cell


def centre_x(part, lo=0.75):
    """Middle of the feet: the stance holds still between frames, as in the old rows."""
    xs = np.where((part[int(part.shape[0] * lo):, :, 3] > 128).any(0))[0]
    return (xs[0] + xs[-1]) / 2


def row_dx(cells, olds):
    """dx laying the row on the old row, by the alpha below the head. The slash FX was placed against the old body."""
    best, best_dx = -1, 0
    a = sum(c[75:, :, 3] > 128 for c in cells)
    b = sum(c[75:, :, 3] > 128 for c in olds)
    for dx in range(-40, 41):
        score = (np.roll(a, dx, axis=1) * b).sum()
        if score > best:
            best, best_dx = score, dx
    return best_dx


def row_cells(anim, direction, walk_hair):
    """[192 px premultiplied cells] of a painted row. Heights between frames stay as painted; a sole within 6 px of the ground snaps to it."""
    pm = key_green(source(anim, direction))
    if anim in REDRAWN:
        pm = match_purple(pm)
    parts = split(pm, COUNT[anim])
    k = row_scale(anim, direction, parts, walk_hair)
    bottom = max(y + p.shape[0] for p, y in parts)
    small = [(resize_pm(p, k), (bottom - (y + p.shape[0])) * k) for p, y in parts]
    if anim == "walk":
        walk_hair[direction] = hair_width(small[0][0])
    cells = []
    for f, air in small:
        y = SOLE_ROW + 1 - f.shape[0] - (round(air) if air > 6 else 0)
        cells.append(place(f, y, round(FRAME / 2 - centre_x(f))))
    if (anim, direction) in HEAD_GROW:
        cells = [grow_head(c, *HEAD_GROW[(anim, direction)]) for c in cells]
    if anim == "idle":
        return [breathe(cells[0], i) for i in range(4)], k, 0
    olds = old_row(anim, direction)
    dx = row_dx(cells, olds) if anim not in NEW_DESIGN.get(direction, ()) else 0
    if anim in REDRAWN:
        # A lunge reaches past the cell when centred on the feet (and np.roll would wrap it round): place again at the row's dx,
        # slid back inside, 3 px from the edge. Frames that fit land exactly where the roll put them.
        out = []
        for (f, air) in small:
            y = SOLE_ROW + 1 - f.shape[0] - (round(air) if air > 6 else 0)
            xs = np.where((f[..., 3] > 8).any(0))[0]
            x = int(np.clip(round(FRAME / 2 - centre_x(f)) + dx, 3 - xs[0], FRAME - 4 - xs[-1]))
            out.append(place(f, y, x))
        if (anim, direction) not in HEAD_GROW:
            out = grow_redrawn_heads(out)
        return out, k, dx
    return [np.roll(c, dx, axis=1) for c in cells], k, dx


def grow_redrawn_heads(cells):
    # measured on the most upright frame (not always frame 0: hit 4 starts crouched)
    tall = [np.where((c[..., 3] > 128).any(1))[0][-1] - hair_top(c) for c in cells]
    up = int(np.argmax(tall))
    drop = int(NECK_FRAC * tall[up])
    heads = [head_of(c, drop) for c in cells]
    widths = [head_width(c, h[0], drop) for c, h in zip(cells, heads)]
    ref = float(np.median(widths))  # not one frame: arms raised beside the head widen a jump frame's measure
    # ChatGPT paints the head bigger in a crouch: every frame's head comes to the strip's median size, then REDRAWN_HEAD bigger
    ks = [REDRAWN_HEAD * float(np.clip(ref / w, 0.75, 1.0)) for w in widths]
    cells = [grow_head(c, k, *h, half=HEAD_HALF) for c, k, h in zip(cells, ks, heads)]
    return cells


def grow_head(pm, k, neck, cx, half=None):
    """Head alone k times bigger, growing up from the neck; the size eases back to 1 over NECK_BLEND rows below it, so arms and fists stay put."""
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    t = np.clip((neck + NECK_BLEND - y) / NECK_BLEND, 0, 1)
    if half is not None:  # only the head's columns: an arm stretched out at shoulder height stays as painted
        t = t * np.clip((half + NECK_BLEND - np.abs(x - cx)) / NECK_BLEND, 0, 1)
    src_x = cx + (x - cx) / (1 + (k - 1) * t)
    src_y = np.where(y < neck, neck - (neck - y) / k, y)
    return np.stack([ndimage.map_coordinates(pm[..., c], [src_y, src_x], order=3, mode="constant") for c in range(4)], axis=-1).clip(0, 255)


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    src_y = y - LIFT[f] * lift_at(y)
    return np.clip(np.dstack([ndimage.map_coordinates(pm[..., c], [src_y, x], order=3, mode="constant") for c in range(4)]), 0, 255)


def cut(out):
    os.makedirs(out, exist_ok=True)
    walk_hair = {}
    z = 3
    order = sorted(((a, d) for a in COUNT for d in ROWS if source(a, d)), key=lambda t: t[0] != "walk")
    for anim, direction in order:
        cells, k, dx = row_cells(anim, direction, walk_hair)
        boxes = [box(c[..., 3]) for c in cells]
        print(f"{anim:9s} {direction:5s} k {k:.3f} dx {dx:+d} top {[b[1] for b in boxes]} sole {[b[3] - 1 for b in boxes]} x {[(b[0], b[2]) for b in boxes][:2]} hair {hair_width(trim(cells[0]))}")
        w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for i, c in enumerate(cells):
            im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (i * FRAME * z, 0), im)
            for v in range(0, FRAME, 24):
                col = (255, 255, 0) if v % 48 == 0 else (110, 110, 110)
                d.line([(i * FRAME * z + v * z, 0), (i * FRAME * z + v * z, FRAME * z)], fill=col)
                d.line([(i * FRAME * z, v * z), ((i + 1) * FRAME * z, v * z)], fill=col)
                if v % 48 == 0:
                    d.text((i * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((i * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w.save(f"{out}/grid-{anim}-{direction}.png")


def head_sheet(out):
    """First frame of every row beside the approved idle down, to read head sizes."""
    h = {}
    order = sorted(((a, d) for a in COUNT for d in ("down", "up", "right") if source(a, d)), key=lambda t: t[0] != "walk")
    ref = Image.open(f"{BODY}/armed/idle.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    cols = 7
    w = Image.new("RGB", (FRAME * cols, FRAME * (1 + len(order) // cols)), (58, 62, 70))
    w.paste(ref, (0, 0), ref)
    for i, (a, d) in enumerate(order, 1):
        im = Image.fromarray(to_rgba(row_cells(a, d, h)[0][0]), "RGBA")
        w.paste(im, (FRAME * (i % cols), FRAME * (i // cols)), im)
        ImageDraw.Draw(w).text((FRAME * (i % cols) + 4, FRAME * (i // cols) + 4), f"{a} {d}", fill=(255, 255, 0))
    w.save(out)


def trim(c):
    b = box(c[..., 3])
    return c[b[1]:b[3], b[0]:b[2]]


# Old anchors held these daggers outside the body with the tip pointing in; on the real fist that blade would cross the face or body (rule 2), so the tip goes out.
# (anim, dir, frame): (main angle or None, off angle or None).
ANGLE_FIX = {("melee1", "down", 1): (None, 120), ("melee1", "down", 3): (None, 120), ("melee2", "down", 3): (None, 125), ("melee2", "down", 4): (None, 125), ("melee2", "up", 2): (125, None)}


# Combo hits drawn fresh on 2026-11-03 (clear slashes, chat https://chatgpt.com/c/6ae991ac-a098-83ec-9910-5a4320ecabba): the old angles
# no longer fit the poses, so a dagger points along its arm, from the chest out through the fist. A fist close to the chest (guard) holds its
# dagger tip up and forward. NEW_ANGLE overrides single frames: (anim, dir, frame): (main or None, off or None).
REDRAWN = ("melee1", "melee2", "melee3", "melee4")
CHEST_Y, GUARD_R, READY_H = 112.0, 22.0, 158
GUARD = {"right": (40, 25), "down": (-30, 30), "up": (30, -30)}  # (main, off) tip angles at guard, clockwise from straight up
NEW_ANGLE = {
    # hit 1: wind-up and mid-sweep fists sit by the face: blade out, not across it
    ("melee1", "down", 2): (-80, None), ("melee1", "right", 1): (-95, None),
    # hit 2 wind-up: the fist by the knee trails its blade back, not down through the floor
    ("melee2", "down", 1): (None, -115), ("melee2", "up", 1): (None, -115), ("melee2", "right", 1): (None, 115),
    ("melee2", "down", 5): (-150, 150),
    ("melee3", "down", 2): (-150, 150), ("melee3", "down", 5): (-150, 150),  # chop and ready: blades down past the fists, not up at the face  # ready stance: blades down, not across the face
    # hit 3 crouch: fists by the floor sweep their blades out level, not into the ground
    ("melee3", "right", 3): (100, -100), ("melee3", "right", 4): (100, -100), ("melee3", "down", 3): (-100, 100), ("melee3", "down", 4): (-100, 100), ("melee3", "up", 3): (100, -100), ("melee3", "up", 4): (100, -100),
    # hit 4 coil: fists back at the hip, blades already pointing at the target
    ("melee4", "down", 5): (-150, 150), ("melee4", "up", 5): (150, -150),  # ready stance: blades down, not across the face
    ("melee4", "right", 0): (70, 70), ("melee4", "right", 1): (None, 70), ("melee4", "right", 2): (70, None),  # fist at the hip waits with its blade forward
}


REDRAWN_HEAD, NECK_FRAC, HEAD_HALF = 1.2, 0.5, 40  # these strips came with a smaller head than idle down; the chin sits about half way down the figure


def hair_top(cell):
    """First row where the figure is wide enough to be the head; thinner rows above are raised fists or hair spikes."""
    return int(np.where((cell[..., 3] > 128).sum(1) >= 34)[0][0])


def head_width(cell, neck, drop):
    """Width of the head at eye level (the widest run of filled columns), the size measure between frames of one strip."""
    y = neck - int(drop * 0.4)
    full = (cell[y - 6:y + 6, :, 3] > 128).sum(0) >= 10
    runs, n = ndimage.label(full)
    return float(ndimage.sum(full, runs, range(1, n + 1)).max())


def head_of(cell, drop):
    """(neck row, head centre x) of one frame: the neck sits a fixed drop below the top of the head (the head keeps its size when he
    crouches or jumps); the centre is found per frame because the head moves with the lunge."""
    neck = hair_top(cell) + drop
    full = (cell[neck - 25:neck, :, 3] > 128).sum(0) >= 15  # face columns; an arm across this band is too thin to count
    runs, n = ndimage.label(full)
    biggest = 1 + int(np.argmax(ndimage.sum(full, runs, range(1, n + 1))))
    xs = np.where(runs == biggest)[0]
    return neck, (xs[0] + xs[-1]) / 2


PURPLE_HUE, PURPLE_V = 305.0, 0.42  # mean hue and value of the hair and cloth purple in the walk, idle and hurt strips


def match_purple(pm):
    """The combo strips came out pinker and brighter than the rest of the set: shift their purples (hue 280-345) to the set's mean
    hue and value. Red scarf, skin and gold are outside that band and stay."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    rgb = np.clip(pm[..., :3] / a / 255, 0, 1)
    mx, mn = rgb.max(-1), rgb.min(-1)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    d = np.maximum(mx - mn, 1e-6)
    hue = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    band = (hue > 280) & (hue < 345) & (mx - mn > 0.08) & (pm[..., 3] > 128)
    shift = PURPLE_HUE - hue[band].mean()
    scale = min(1.0, PURPLE_V / mx[band].mean())  # only ever darkens: the strips drawn from the hero-select art are already darker, as that art is
    w = np.clip(np.minimum(hue - 270, 355 - hue) / 10, 0, 1) * ((mx - mn) > 0.08)  # fade out at the edges of the band
    h2 = (hue + shift * w) % 360
    v2 = np.clip(mx * (1 + (scale - 1) * w), 0, 1)
    s2 = (mx - mn) / np.maximum(mx, 1e-6)
    c = v2 * s2
    x = c * (1 - np.abs((h2 / 60) % 2 - 1))
    k = (h2 // 60).astype(int)
    zero = np.zeros_like(c)
    table = [(c, x, zero), (x, c, zero), (zero, c, x), (zero, x, c), (x, zero, c), (c, zero, x)]
    out = np.zeros_like(rgb)
    for i, (rr, gg, bb) in enumerate(table):
        sel = k == i
        out[sel] = np.stack([rr[sel], gg[sel], bb[sel]], -1)
    out += (v2 - c)[..., None]
    res = pm.copy()
    res[..., :3] = out * 255 * a
    return res


def chest_x(cell):
    xs = np.where((cell[95:130, :, 3] > 128).any(0))[0]
    return (xs[0] + xs[-1]) / 2


def arm_angle(cell, x, y, guard):
    """Tip angle (clockwise from up) pointing from the chest out through the fist; guard angle when the fist is tucked in."""
    dx, dy = x - chest_x(cell), y - CHEST_Y
    if math.hypot(dx, dy) < GUARD_R:
        return guard
    return round(math.degrees(math.atan2(dx, -dy)), 1)


def directions(anim):
    return ("up", "right") if anim == "idle" else ("down", "up", "right")  # idle down is done: row 0 stays


def complete_anims():
    """Only anims with every painted direction go into the game; the rest keep their old sheets (brief)."""
    return [a for a in COUNT if all(source(a, d) for d in directions(a))]


def old_anchors():
    out = {}
    for path in (f"{OLD}/held-assassin-old.json", f"{OLD}/held-assassin-melee-old.json"):
        for a in json.load(open(path, encoding="utf-8"))["anchors"]:
            out[(a["part"], a["anim"], a["dir"])] = a["frames"]
    return out


def fists_of_row(anim, direction):
    table = FISTS[(anim, direction)]
    if anim != "idle":
        return table
    return [tuple((x, y + LIFT[f] * float(lift_at(np.array(float(y))))) for x, y in table[0]) for f in range(4)]


def angles_of_frame(om, oo, f, mx, ox):
    """Old angles, kept frame by frame because the slash FX follows them. They go with the fist on the same side of the screen: the old sheets
    had main on either fist, and the parts must stay on their hands (rule 4) without the blade changing direction."""
    n = len(om)
    am, ao = angle_of(om, f % n), angle_of(oo, f % n)
    a, b = om[f % n], oo[f % n]
    if not (a.get("fallback") or b.get("fallback") or a.get("hidden") or b.get("hidden")) and (a["x"] - b["x"]) * (mx - ox) < 0:
        am, ao = ao, am
    return am, ao


def on_body(cell, x, y):
    """The fist has body all round it, rather than sticking out of the silhouette."""
    ys, xs = np.mgrid[0:FRAME, 0:FRAME]
    r = np.hypot(xs - x, ys - y)
    return (cell[..., 3] > 128)[(r >= 13) & (r <= 17)].mean() > 0.8


def fist_mask(x, y):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    return np.clip((FIST_R - np.hypot(xx - x, yy - y)) / FEATHER, 0, 1)


def angle_of(frames, f):
    """The old angle of this frame; a fallback or hidden frame has none, so its neighbour's."""
    n = len(frames)
    for i in (f, (f + 1) % n, (f - 1) % n):
        if not frames[i].get("fallback") and not frames[i].get("hidden"):
            return frames[i]["angle"]
    return 0


def anchor(x, y, angle, behind):
    return {"x": round(float(x), 3), "y": round(float(y), 3), "angle": float(angle), "s": DAGGER_S, "sx": 1, "behind": bool(behind), "hidden": False, "fallback": False}


def flipped(a, behind):
    out = dict(a)
    out.update(x=round(FRAME - a["x"], 3), angle=-a["angle"], behind=behind)
    return out


def build():
    """{anim: {row: cells}}, hand {anim: {row: cells}}, {(part, anim, dir): anchor frames}."""
    walk_hair, sheets, hands, anchors = {}, {}, {}, {}
    old = old_anchors()
    for anim in sorted(complete_anims(), key=lambda a: a != "walk"):
        for direction in directions(anim):
            cells, k, dx = row_cells(anim, direction, walk_hair)
            fists = fists_of_row(anim, direction)
            n = len(cells)
            om, oo = old[("main", anim, direction)], old[("off", anim, direction)]
            mains, offs, hand = [], [], []
            for f in range(n):
                (mx, my), (ox, oy) = fists[f]
                # Sideways the off hand is the far hand: behind the body unless the fist is across the chest (rule 4 and its on_body exception).
                far_behind = direction == "right" and not on_body(cells[f], ox, oy)
                main_behind = False
                if anim in REDRAWN and direction == "up":  # seen from the back, a fist held in front of the chest hides its dagger behind the body
                    main_behind, far_behind = on_body(cells[f], mx, my), on_body(cells[f], ox, oy)
                if anim in REDRAWN:
                    am, ao = arm_angle(cells[f], mx, my, GUARD[direction][0]), arm_angle(cells[f], ox, oy, GUARD[direction][1])
                else:
                    am, ao = angles_of_frame(om, oo, f, mx, ox)
                fix = NEW_ANGLE.get((anim, direction, f), (None, None))
                am, ao = am if fix[0] is None else fix[0], ao if fix[1] is None else fix[1]
                fix = ANGLE_FIX.get((anim, direction, f), (None, None))
                am, ao = am if fix[0] is None else fix[0], ao if fix[1] is None else fix[1]
                mains.append(anchor(mx, my, am, main_behind))
                offs.append(anchor(ox, oy, ao, far_behind))
                mask = np.maximum(0 if main_behind else fist_mask(mx, my), 0 if far_behind else fist_mask(ox, oy))
                hand.append(cells[f] * mask[..., None])
            anchors[("main", anim, direction)], anchors[("off", anim, direction)] = mains, offs
            sheets.setdefault(anim, {})[ROWS[direction]] = cells
            hands.setdefault(anim, {})[ROWS[direction]] = hand
            if direction == "right":
                # Mirrored row: the near fist is now the left hand (off), the far fist the right hand (main).
                sheets[anim][ROWS["left"]] = [c[:, ::-1].copy() for c in cells]
                hands[anim][ROWS["left"]] = [c[:, ::-1].copy() for c in hand]
                anchors[("main", anim, "left")] = [flipped(offs[f], offs[f]["behind"]) for f in range(n)]
                anchors[("off", anim, "left")] = [flipped(mains[f], False) for f in range(n)]
    return sheets, hands, anchors


def load_compose():
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    return comp


def armed_cell(comp, bare_cell, frames, hand_cell, file):
    """Body with the first tier's daggers baked in, drawn as the game draws them: the Shadow Clone is cut from these sheets, tinted dark (ShadowCloneFx)."""
    canvas = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    entry = file["weapon"][0]
    art = Image.open(f"{RES}/Sprites/classes/assassin/weapons/dagger/{entry['name']}-base.png").convert("RGBA")

    def gear(behind):
        for a in frames:
            if bool(a["behind"]) == behind and not a["hidden"]:
                comp.place(canvas, art, entry["baseGrip"], a, 1.3, 0.0)
    gear(True)
    canvas.alpha_composite(bare_cell)
    gear(False)
    canvas.alpha_composite(hand_cell)
    return canvas


def write_sheets(bare_dir, armed_dir, hand_dir, sheets, hands, anchors):
    comp = load_compose()
    file = json.load(open(GEAR, encoding="utf-8"))
    for folder in (bare_dir, armed_dir, hand_dir):
        os.makedirs(folder, exist_ok=True)
    clone_anims = ("idle", "walk", "melee1", "melee2", "melee3")  # what ShadowCloneFx draws
    for anim, rows in sheets.items():
        bare = Image.open(f"{OLD}/bare/{anim}.png").convert("RGBA")
        armed = Image.open(f"{BODY}/armed/{anim}.png").convert("RGBA")  # the live sheet: idle row 0 is the approved art
        hand = Image.new("RGBA", bare.size, (0, 0, 0, 0))
        for row, cells in rows.items():
            direction = [k for k, v in ROWS.items() if v == row][0]
            for f, cell in enumerate(cells):
                at = (f * FRAME, row * FRAME)
                area = at + (at[0] + FRAME, at[1] + FRAME)
                body = Image.fromarray(to_rgba(cell), "RGBA")
                over = Image.fromarray(to_rgba(hands[anim][row][f]), "RGBA")
                bare.paste((0, 0, 0, 0), area)
                bare.paste(body, at)
                hand.paste(over, at)
                pic = body
                if anim in clone_anims:
                    pic = armed_cell(comp, body, (anchors[("off", anim, direction)][f], anchors[("main", anim, direction)][f]), over, file)
                armed.paste((0, 0, 0, 0), area)
                armed.paste(pic, at)
        bare.save(f"{bare_dir}/{anim}.png")
        armed.save(f"{armed_dir}/{anim}.png")
        hand.save(f"{hand_dir}/{anim}.png")


def write_anchors(path, anchors):
    """Read, change, write straight away; rows of other anims and idle down stay. Keeps the file's indent and line ends."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    done = set()
    for a in data["anchors"]:
        key = (a["part"], a["anim"], a["dir"])
        if key in anchors:
            a["frames"] = anchors[key]
            done.add(key)
    for key, frames in anchors.items():
        if key not in done:
            data["anchors"].append({"part": key[0], "anim": key[1], "dir": key[2], "frames": frames})
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def new_guid_copy(model, path):
    """A sheet Unity imports by itself comes out compressed and capped at 2048: copy the settings of one of the same kind, with a new guid."""
    if os.path.exists(path):
        return
    lines = open(model, encoding="utf-8", newline="").read().split("\n")
    lines[1] = f"guid: {uuid.uuid4().hex}" + ("\r" if lines[1].endswith("\r") else "")
    open(path, "w", encoding="utf-8", newline="").write("\n".join(lines))


def write_metas(hand_dir, anims):
    for anim in anims:
        new_guid_copy(f"{RES}/Sprites/classes/warrior/body/hand/idle.png.meta", f"{hand_dir}/{anim}.png.meta")
    new_guid_copy(f"{RES}/Sprites/classes/warrior/body/hand.meta", f"{hand_dir}.meta")


def preview(out, pad=50):
    """Sheets as copies, composited the way the game draws, each row beside the approved idle down, with GIFs. Resources untouched."""
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    sheets, hands, anchors = build()
    write_sheets(f"{out}/bare", f"{out}/armed", f"{out}/hand", sheets, hands, anchors)
    _, live = comp.load_gear("assassin")
    live.update(anchors)
    size = FRAME + 2 * pad
    ref = Image.open(f"{BODY}/armed/idle.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    ms = {"idle": 240, "walk": 160, "hurt": 200}
    for anim, rows in sheets.items():
        n = len(rows[ROWS["up"]])
        names = [d for d in ("down", "up", "left", "right") if ROWS[d] in rows]
        sheet = Image.new("RGBA", (FRAME + size * n, size * len(names)), (58, 62, 70, 255))
        sheet.alpha_composite(ref, (0, pad))
        for r, d in enumerate(names):
            gif = []
            for f in range(n):
                c = comp.compose("assassin", anim, d, f, f"{out}/bare/{anim}.png", live, f"{out}/hand/{anim}.png", pad=pad)
                sheet.alpha_composite(c, (FRAME + f * size, r * size))
                bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
                bg.alpha_composite(c)
                gif.append(bg.convert("P", palette=Image.ADAPTIVE))
            gif[0].save(f"{out}/{anim}-{d}.gif", save_all=True, append_images=gif[1:], duration=ms.get(anim, 90), loop=0, disposal=2)
        sheet.convert("RGB").save(f"{out}/{anim}.png")


def debug_fists(out):
    """Every frame with the fists of the table marked, M main and O off."""
    os.makedirs(out, exist_ok=True)
    sheets, _, anchors = build()
    z = 2
    for anim, rows in sheets.items():
        n = len(rows[ROWS["up"]])
        names = directions(anim)
        w = Image.new("RGB", (FRAME * z * n, FRAME * z * len(names)), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for r, name in enumerate(names):
            for f, c in enumerate(rows[ROWS[name]]):
                im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
                w.paste(im, (f * FRAME * z, r * FRAME * z), im)
                for part, col in (("main", (0, 255, 255)), ("off", (255, 255, 0))):
                    a = anchors[(part, anim, name)][f]
                    x, y = f * FRAME * z + a["x"] * z, r * FRAME * z + a["y"] * z
                    d.ellipse((x - FIST_R * z, y - FIST_R * z, x + FIST_R * z, y + FIST_R * z), outline=col)
                    d.text((x + 8, y - 6), part[0].upper(), fill=col)
        w.save(f"{out}/fists-{anim}.png")


# ---------------------------------------------------------------- daggers
# One ChatGPT image per tier, two daggers side by side (base left, enhanced right), in new-style/dagger-src/<tier>-pair-chatgpt.png.
# Standard silhouette: dagger-src/dagger-standard-sample.png, the blade cut from hero-select/assassin.png with placeholder hilt bars.
DAGGERS = f"{RES}/Sprites/classes/assassin/weapons/dagger"
DAGGER_SRC = f"{LIB}/dagger-src"
ICONS = f"{RES}/Sprites/icons"
DAGGER_LEN = 258  # art px, tip to pommel: the old daggers' length, so the size on screen stays (s and DrawScale do not change)
DAGGER_PAD = 4
MAGENTA_KEY = {"venom"}
CHARM_TIERS = {"moonlight"}  # a charm hangs beside the handle: grip measured on the handle axis only  # green art: drawn on #FF00FF


def key_pair(path, tier):
    """Premultiplied art off a flat key. Same green rule as key_green; magenta for the tier whose art is green, holes between the hooks too."""
    if tier not in MAGENTA_KEY:
        return key_green(path)
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((np.minimum(r, b) - g - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(fg, 1 + np.where(sizes >= 400)[0]), iterations=3)
    return np.dstack([np.minimum(r, np.maximum(g, b)) * alpha, g * alpha, np.minimum(b, np.maximum(r, g)) * alpha, alpha * 255])  # spill off the edge pixels


def measure_grip(a, axis_only=False):
    """(gripX, gripY) middle of the handle, read from the alpha of an upright dagger: between the bottom of the crossguard and the top of the pommel."""
    mask = a > 128
    rows = np.where(mask.any(1))[0]
    top, bottom = rows[0], rows[-1]
    # axis_only: only the columns of the handle axis (centred on the pommel), so a charm hanging beside the handle (moonlight) does not widen its rows
    pommel = np.where(mask[bottom - max(3, int(0.03 * (bottom - top))):bottom + 1].any(0))[0]
    cx, half = (pommel[0] + pommel[-1]) / 2, (pommel[-1] - pommel[0]) / 2 + 3
    cols = np.abs(np.arange(mask.shape[1]) - cx) <= half
    low_rows = np.arange(mask.shape[0]) >= top + 0.6 * (bottom - top)
    if axis_only:
        mask = mask & (cols[None, :] | ~low_rows[:, None])
    widths = mask.sum(1)
    h = bottom - top + 1
    low = int(top + 0.6 * h)  # the crossguard and handle are in the lower part of the art
    guard = low + int(np.argmax(widths[low:]))
    hand = int(np.median(widths[guard + int(0.05 * h):bottom - int(0.08 * h)]))
    y = guard
    while widths[y] > 1.35 * hand:
        y += 1
    guard_bottom = y
    while y < bottom and widths[y] <= 1.35 * hand:
        y += 1
    pommel_top = y
    grip_y = (guard_bottom + pommel_top) // 2
    xs = np.where(mask[grip_y])[0]
    return int(round((xs[0] + xs[-1]) / 2)), int(grip_y), {"guard": int(guard), "guardBottom": int(guard_bottom), "pommelTop": int(pommel_top), "handle": hand}


def cut_pair(tier):
    """{'base': (RGBA image, grip dict), 'enhanced': ...}: left and right dagger, one scale each, length DAGGER_LEN."""
    pm = key_pair(f"{DAGGER_SRC}/{tier}-pair-chatgpt.png", tier)
    labels, n = ndimage.label(ndimage.binary_dilation(pm[..., 3] > 128, iterations=2))
    sizes = ndimage.sum(pm[..., 3] > 128, labels, range(1, n + 1))
    big = sorted([(o, i + 1) for i, (o, s) in enumerate(zip(ndimage.find_objects(labels), sizes)) if s > 0.25 * max(sizes)], key=lambda t: t[0][1].start)
    if len(big) != 2:
        raise SystemExit(f"{tier}: {len(big)} blobs, expected 2")
    out = {}
    for kind, (o, label) in zip(("base", "enhanced"), big):
        part = pm[o] * (labels[o] == label)[..., None]
        small = resize_pm(part, DAGGER_LEN / part.shape[0])
        canvas = np.zeros((small.shape[0] + 2 * DAGGER_PAD, small.shape[1] + 2 * DAGGER_PAD, 4))
        canvas[DAGGER_PAD:DAGGER_PAD + small.shape[0], DAGGER_PAD:DAGGER_PAD + small.shape[1]] = small
        gx, gy, info = measure_grip(canvas[..., 3], tier in CHARM_TIERS)
        out[kind] = (Image.fromarray(to_rgba(canvas), "RGBA"), {"w": canvas.shape[1], "h": canvas.shape[0], "gripX": gx, "gripY": gy}, info)
    return out


def dagger_tiers():
    return [e["name"] for e in json.load(open(GEAR, encoding="utf-8"))["weapon"]]


def complete_tiers():
    return [t for t in dagger_tiers() if os.path.exists(f"{DAGGER_SRC}/{t}-pair-chatgpt.png")]


def hero_row(comp, art, grip, anim, direction, anchors, pad):
    """Every frame of a row drawn as the game draws it, with this dagger in both hands."""
    frames = []
    bare, hand = f"{BODY}/bare/{anim}.png", f"{BODY}/hand/{anim}.png"
    n = len(anchors[("main", anim, direction)])
    for f in range(n):
        pair = (anchors[("off", anim, direction)][f], anchors[("main", anim, direction)][f])
        canvas = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (58, 62, 70, 255))
        if not os.path.exists(bare) or any(a.get("fallback") for a in pair):
            canvas.alpha_composite(comp.cell(f"{BODY}/armed/{anim}.png", ROWS[direction], f), (pad, pad))
            frames.append(canvas)
            continue
        for behind in (True, False):
            for a in pair:
                if bool(a.get("behind")) == behind and not a.get("hidden"):
                    comp.place(canvas, art, grip, a, 1.3, 0.0, pad)
            if behind:
                canvas.alpha_composite(comp.cell(bare, ROWS[direction], f), (pad, pad))
        if os.path.exists(hand):
            canvas.alpha_composite(comp.cell(hand, ROWS[direction], f), (pad, pad))
        frames.append(canvas)
    return frames


def dagger_preview(out, tier, kind="base", anims=("idle", "walk", "hurt", "melee1", "melee2", "melee3", "melee4")):
    """Composites of one tier on the hero, all four directions, from the cut art: Resources untouched. Zoomed 2x, a row per direction."""
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    art, grip, _ = cut_pair(tier)[kind]
    _, anchors = comp.load_gear("assassin")
    pad = 50
    size = FRAME + 2 * pad
    for anim in anims:
        rows = [(d, hero_row(comp, art, grip, anim, d, anchors, pad)) for d in ROWS if ("main", anim, d) in anchors]
        cols = max(len(r) for _, r in rows)
        sheet = Image.new("RGB", (size * cols, size * len(rows)), (58, 62, 70))
        for r, (d, frames) in enumerate(rows):
            for f, c in enumerate(frames):
                sheet.paste(c.convert("RGB"), (f * size, r * size))
            ImageDraw.Draw(sheet).text((4, r * size + 4), f"{anim} {d}", fill=(255, 255, 0))
        sheet.save(f"{out}/hero-{tier}-{kind}-{anim}.png")
    print("previews in", out)


def dagger_sheet(out):
    """All tiers drawn so far, base over enhanced, on dark and on light; their grips marked, at the size the game draws them beside it."""
    tiers = complete_tiers()
    cuts = {t: cut_pair(t) for t in tiers}
    cw, ch = 150, 290
    for bg, name in (((58, 62, 70), "dark"), ((205, 205, 200), "light")):
        w = Image.new("RGB", (cw * len(tiers), ch * 2), bg)
        d = ImageDraw.Draw(w)
        for i, t in enumerate(tiers):
            for j, kind in enumerate(("base", "enhanced")):
                art, g, _ = cuts[t][kind]
                w.paste(art, (i * cw + 5, j * ch + 12), art)
                x, y = i * cw + 5 + g["gripX"], j * ch + 12 + g["gripY"]
                d.line([(x - 4, y), (x + 4, y)], fill=(0, 255, 255))
                d.line([(x, y - 4), (x, y + 4)], fill=(0, 255, 255))
            d.text((i * cw + 4, 0), t, fill=(255, 255, 0) if name == "dark" else (0, 0, 0))
        w.save(f"{out}/tiers-{name}.png")
    for t in tiers:
        print(t, {k: (v[1], v[2]) for k, v in cuts[t].items()})


def install_icons(names):
    all_names = dagger_tiers()
    for tier in names:
        i = all_names.index(tier)
        art = Image.open(f"{DAGGERS}/{tier}-base.png").convert("RGBA")
        for prefix, tilt in (("weapon", -45), ("offhand", 45)):  # build-item-icons.py: overlay(), fit()
            im = art.rotate(tilt, expand=True, resample=Image.BICUBIC)
            im = im.crop(im.getbbox())
            k = 88 / max(im.width, im.height)
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
            icon = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
            icon.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
            icon.save(f"{ICONS}/{prefix}-assassin-{i}.png")


def dagger_install(names):
    """Art, grips, icons of complete tiers; the rest keep their old art. Old art is in new-style/old/dagger, old icons in new-style/old/icons."""
    os.makedirs(f"{OLD}/icons", exist_ok=True)
    for i in range(10):
        for prefix in ("weapon", "offhand"):
            backup = f"{OLD}/icons/{prefix}-assassin-{i}.png"
            if not os.path.exists(backup):
                Image.open(f"{ICONS}/{prefix}-assassin-{i}.png").save(backup)
    raw = open(GEAR, "rb").read()
    data = json.loads(raw)
    for tier in names:
        cut_ = cut_pair(tier)
        entry = next(e for e in data["weapon"] if e["name"] == tier)
        for kind, field in (("base", "baseGrip"), ("enhanced", "enhancedGrip")):
            cut_[kind][0].save(f"{DAGGERS}/{tier}-{kind}.png")
            entry[field] = cut_[kind][1]
    text = json.dumps(data, separators=(",", ":"))
    open(GEAR, "wb").write(text.encode())
    install_icons(names)
    print("installed", names)


def write_armed():
    """Only the armed sheets, rebuilt with the first tier's dagger as it now is: the Shadow Clone is cut from them. bare, hand and anchors are not touched."""
    comp = load_compose()
    file = json.load(open(GEAR, encoding="utf-8"))
    sheets, hands, anchors = build()
    clipped = {}
    for anim, rows in sheets.items():
        if anim not in ("idle", "walk", "melee1", "melee2", "melee3"):
            continue
        armed = Image.open(f"{BODY}/armed/{anim}.png").convert("RGBA")
        for row, cells in rows.items():
            direction = [k for k, v in ROWS.items() if v == row][0]
            for f, cell in enumerate(cells):
                at = (f * FRAME, row * FRAME)
                pair = (anchors[("off", anim, direction)][f], anchors[("main", anim, direction)][f])
                pic = armed_cell(comp, Image.fromarray(to_rgba(cell), "RGBA"), pair, Image.fromarray(to_rgba(hands[anim][row][f]), "RGBA"), file)
                armed.paste((0, 0, 0, 0), at + (at[0] + FRAME, at[1] + FRAME))
                armed.paste(pic, at)
                big = dagger_outside(comp, pair, file)
                if big:
                    clipped[(anim, direction, f)] = big
        armed.save(f"{BODY}/armed/{anim}.png")
    print("armed written;", len(clipped), "frames with dagger past the 192 px cell, art px cut:", clipped)


def dagger_outside(comp, pair, file):
    """Dagger pixels that fall outside the cell, so the Shadow Clone, cut from that cell, loses them."""
    entry = file["weapon"][0]
    art = Image.open(f"{DAGGERS}/{entry['name']}-base.png").convert("RGBA")
    pad = 80
    canvas = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
    for a in pair:
        if not a["hidden"]:
            comp.place(canvas, art, entry["baseGrip"], a, 1.3, 0.0, pad)
    al = np.asarray(canvas)[..., 3] > 128
    inside = al[pad:pad + FRAME, pad:pad + FRAME].sum()
    return int(al.sum() - inside)


if __name__ == "__main__":
    if sys.argv[1] == "dagger-sheet":
        dagger_sheet(sys.argv[2])
    elif sys.argv[1] == "dagger-preview":
        dagger_preview(sys.argv[2], sys.argv[3], *(sys.argv[4:5]))
    elif sys.argv[1] == "dagger-install":
        dagger_install(sys.argv[2:] or complete_tiers())
    elif sys.argv[1] == "armed":
        write_armed()
    elif sys.argv[1] == "measure":
        measure()
    elif sys.argv[1] == "cut":
        cut(sys.argv[2])
    elif sys.argv[1] == "heads":
        head_sheet(sys.argv[2])
    elif sys.argv[1] == "fists":
        debug_fists(sys.argv[2])
    elif sys.argv[1] == "preview":
        preview(sys.argv[2])
    elif sys.argv[1] == "write":
        s_, h_, a_ = build()
        write_sheets(f"{BODY}/bare", f"{BODY}/armed", f"{BODY}/hand", s_, h_, a_)
        write_metas(f"{BODY}/hand", list(s_))
        write_anchors(GEAR_MELEE, a_)
        print("written", list(s_))

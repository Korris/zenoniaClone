"""Wizard body sheets and staff anchors in the new style, cut from ChatGPT art on flat #00FF00. dash, death and idle facing down are left alone (brief).

Sources: Assets/ArtLibrary/classes/wizard/new-style/src/wizard-<anim>-<dir>.png, one row of frames per image. Old art and anchors: new-style/old/.
Rows drawn to the old row (attacks, hurt, walk down and up, idle up) keep the feet of the old frame, because the FX follows them; idle and walk left and right are new designs, centred.
Every frame of a row shares one scale, set by the standing height of the row against the approved idle down: the head stays the size of that head.
The staff has one size (S) everywhere and stays in the right hand: near arm facing right, far arm facing left. Old angles are kept unless ANGLE overrides them.

Usage (from the project root; needs Pillow, numpy, scipy):
  python Tools/build-wizard-new-style.py cut OUT_DIR      frames with a pixel grid, for reading the fists; Resources untouched
  python Tools/build-wizard-new-style.py preview OUT_DIR  sheets and anchors as copies, composited the way the game draws; Resources untouched
  python Tools/build-wizard-new-style.py write            bare, armed, hand sheets and the anchors in held-wizard-melee.json
"""
import importlib.util, json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

FRAME = 192
SOLE_ROW = 188  # sole row of the approved idle down
S, DRAW = 0.447, 1.0
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/wizard/body"
ANCHORS = f"{RES}/Data/classes/wizard/held-wizard-melee.json"
NS = "Assets/ArtLibrary/classes/wizard/new-style"
SRC = f"{NS}/src"
OLD_BARE = f"{NS}/old/bare"
OLD_GEAR = f"{NS}/old/held-wizard.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"idle": 4, "walk": 4, "hurt": 2, "attack1": 4, "attack2": 4, "attack3": 6}
PAINTED = {("idle", "up"): 1, ("idle", "right"): 1, ("idle", "left"): 1}  # one painted pose, breathing added here
NEW_DESIGN = {("idle", "right"), ("idle", "left"), ("walk", "right"), ("walk", "left")}
IDLE_POSE_LIFT = [0.0, -0.7, -1.4, -0.7]  # breathing, frame px, as in build-warrior-move.py
ANKLE_Y, WAIST_Y = 172.0, 128.0
FIST_R, FEATHER = 8.0, 1.0
STAND_H = 160  # hood top (row 29) to sole of the approved idle down: its head size, measured; the 164 of that row includes the staff
SCALE_FRAME = {}  # (anim, dir): frame whose height sets the scale; default: the median frame height


def key_green(path):
    """Premultiplied RGBA floats. The art has no green, so every green pixel counts, holes between the legs too. The ground of these images fades to darker green."""
    rgb = np.array(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    ratio = g - np.maximum(r, b)
    alpha = 1 - np.clip((ratio - 12) / 60, 0, 1)
    lab, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        keep = np.isin(lab, 1 + np.where(sizes >= 400)[0])
        alpha = alpha * ndimage.binary_dilation(keep, iterations=3)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def split(pm, n):
    """n figures left to right: connected art, small bits join the nearest figure by column."""
    mask = pm[..., 3] > 128
    lab, cnt = ndimage.label(ndimage.binary_dilation(mask, iterations=3))
    objs = ndimage.find_objects(lab)
    sizes = ndimage.sum(mask, lab, range(1, cnt + 1))
    order = np.argsort(sizes)[::-1][:n]
    big = sorted(order, key=lambda i: objs[i][1].start)
    if len(big) < n:
        raise SystemExit(f"{len(big)} figures, expected {n}")
    out = []
    for i in big:
        o = objs[i]
        m = (lab[o] == i + 1)[..., None]
        out.append(pm[o] * m)
    return [trim(f) for f in out]


def trim(f):
    ys = np.where((f[..., 3] > 128).any(1))[0]
    xs = np.where((f[..., 3] > 128).any(0))[0]
    return f[ys[0]:ys[-1] + 1, xs[0]:xs[-1] + 1]


def resize_pm(pm, k):
    w, h = max(1, round(pm.shape[1] * k)), max(1, round(pm.shape[0] * k))
    return np.clip(np.dstack([np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]), 0, 255)


def feet_cx(alpha):
    ys = np.where((alpha > 128).any(1))[0]
    low = alpha[int(ys[-1] - 0.1 * (ys[-1] - ys[0])):ys[-1] + 1]
    xs = np.where((low > 128).any(0))[0]
    return (xs[0] + xs[-1]) / 2


def old_cell(anim, row, f):
    im = np.array(Image.open(f"{OLD_BARE}/{anim}.png").convert("RGBA"))
    return im[row * FRAME:(row + 1) * FRAME, f * FRAME:(f + 1) * FRAME]


def row_scale(key, frames):
    hs = [f.shape[0] for f in frames]
    h = hs[SCALE_FRAME[key]] if key in SCALE_FRAME else float(np.median(hs))
    return STAND_H / h


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    src_y = y - IDLE_POSE_LIFT[f] * lift_at(y)
    return np.clip(np.dstack([ndimage.map_coordinates(pm[..., c], [src_y, x], order=3, mode="constant") for c in range(4)]), 0, 255)


def row_cells(key):
    anim, d = key
    n = PAINTED.get(key, COUNT[anim])
    raw = split(key_green(f"{SRC}/wizard-{anim}-{d}.png"), n)
    k = row_scale(key, raw)
    k *= ROW_SCALE.get(key, 1)
    cells = []
    # Move rows keep one foot line, or a step jumps sideways; attacks follow each old frame.
    old_x = [feet_cx(old_cell(anim, ROWS[d], i)[..., 3]) for i in range(COUNT[anim])]
    row_x = float(np.median(old_x))
    for i, f in enumerate(raw):
        f = resize_pm(f, k)
        if key in NEW_DESIGN:
            cx = 96.0 + ROW_DX.get(key, 0)
        elif anim.startswith("attack"):
            cx = old_x[i] + ROW_DX.get(key, 0)
        else:
            cx = row_x + ROW_DX.get(key, 0)
        x = round(cx - feet_cx(f[..., 3]))
        y = SOLE_ROW + 1 - f.shape[0]
        cell = np.zeros((FRAME, FRAME, 4))
        sx0, sy0 = max(0, -x), max(0, -y)
        ex, ey = min(FRAME, x + f.shape[1]), min(FRAME, y + f.shape[0])
        cell[y + sy0:ey, x + sx0:ex] = f[sy0:ey - y, sx0:ex - x]
        if x < 0 or y < 0 or x + f.shape[1] > FRAME:
            print("CLIPPED", key, i, "x", x, x + f.shape[1], "y", y)
        cells.append(cell)
    if n == 1 and anim == "idle":
        cells = [breathe(cells[0], f) for f in range(4)]
    return cells, k


ROW_DX = {}
ROW_SCALE = {}
ALL_ROWS = [("idle", "up"), ("idle", "left"), ("idle", "right"), ("walk", "down"), ("walk", "up"), ("walk", "left"), ("walk", "right"),
            ("hurt", "down"), ("hurt", "up"), ("hurt", "right"), ("hurt", "left"),
            ("attack1", "down"), ("attack1", "up"), ("attack1", "left"), ("attack1", "right"),
            ("attack2", "down"), ("attack2", "up"), ("attack2", "left"), ("attack2", "right"),
            ("attack3", "down"), ("attack3", "up"), ("attack3", "left"), ("attack3", "right")]


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def cut(out, only=None):
    os.makedirs(out, exist_ok=True)
    z = 4
    for key in ALL_ROWS:
        if only and key[0] not in only:
            continue
        if not os.path.exists(f"{SRC}/wizard-{key[0]}-{key[1]}.png"):
            continue
        cells, k = row_cells(key)
        boxes = []
        w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
        for f, c in enumerate(cells):
            im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (f * FRAME * z, 0), im)
            ys, xs = np.where(c[..., 3] > 128)
            boxes.append((int(ys.min()), int(ys.max()), int(xs.min()), int(xs.max())))
        d = ImageDraw.Draw(w)
        for f in range(len(cells)):
            for v in range(0, FRAME, 16):
                col = (255, 255, 0) if v % 48 == 0 else (110, 110, 110)
                d.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=col)
                d.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=col)
                if v % 48 == 0:
                    d.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w = w.resize((w.width // 2, w.height // 2), Image.LANCZOS)
        w.save(f"{out}/grid-{key[0]}-{key[1]}.png")
        print(key, "scale", round(k, 3), boxes)


# Staff fist per frame, cell px, read from the fists sheets: (x, y, behind). Facing down the right hand is screen left; up, screen right; right, the near arm.
# behind = the staff goes behind the body where it would cross the face or where the far arm holds it; the body then draws the fist itself.
FISTS = {
    ("idle", "up"): [(130.5, 129.5, True)],
    ("idle", "left"): [(52, 126, True)],
    ("idle", "right"): [(90.5, 147, False)],
    ("walk", "down"): [(62, 127.5, False), (57.5, 134.5, False), (62.5, 131, False), (59.5, 134.5, False)],
    ("walk", "up"): [(129.5, 129, True), (142, 131.5, True), (147, 118, True), (143.5, 131, True)],
    ("walk", "left"): [(53, 127, True), (64.5, 123, True), (60, 125.5, True), (45, 125, True)],
    ("walk", "right"): [(113.5, 143.5, False), (112, 145, False), (115.5, 138, False), (118.5, 143, False)],
    ("hurt", "down"): [(65, 95, False), (65, 118, False)],
    ("hurt", "up"): [(142, 66, True), (153, 99, True)],
    ("hurt", "right"): [(98, 130, False), (157, 111, True)],
    ("hurt", "left"): [(32, 110, True), (35, 111, True)],  # the right row mirrored; the far fist holds the staff
    ("attack1", "down"): [(50, 125.5, True), (51, 70.5, False), (69, 120.5, False), (73, 126, False)],
    ("attack1", "up"): [(154, 75, True), (148, 60.5, True), (163.5, 77.5, False), (142, 72.5, True)],
    ("attack1", "right"): [(135, 116, True), (63.5, 59.5, True), (128.5, 114.5, False), (124.5, 114.5, False)],
    ("attack2", "down"): [(45, 87.5, False), (49.5, 60.5, False), (89.5, 126, False), (92.5, 122, False)],
    ("attack2", "up"): [(142, 51.5, False), (131, 73, True), (158.5, 103.5, False), (155, 105, False)],
    ("attack2", "right"): [(143, 124, True), (64, 74, True), (122, 109, False), (129.5, 109.5, False)],
    ("attack3", "right"): [(146, 123, False), (163.5, 54.5, True), (132.5, 128, False), (157, 111, False), (143.5, 116.5, False), (144.5, 119.5, False)],
}
# Clockwise, tip up = 0. Attacks and hurt keep the old angle; these are the new designs and the frames that would cross the face.
ANGLE = {
    ("idle", "up"): [18] * 4, ("idle", "left"): [-20] * 4, ("idle", "right"): [45] * 4,
    ("walk", "down"): [-18] * 4, ("walk", "up"): [18] * 4, ("walk", "left"): [-22] * 4, ("walk", "right"): [28, 26, 30, 26],
}
ANGLE_FRAME = {("attack2", "down", 0): 140, ("attack2", "down", 2): -120}  # the old angle puts the shaft across the face (116, -160)
MIRRORED = {("hurt", "left"): ("hurt", "right"), ("attack1", "left"): ("attack1", "right"), ("attack2", "left"): ("attack2", "right")}
HAND_META = "Assets/Resources/Sprites/classes/warrior/body/hand/melee1.png.meta"


def old_anchors():
    return {(a["part"], a["anim"], a["dir"]): a["frames"] for a in json.load(open(OLD_GEAR, encoding="utf-8"))["anchors"]}


def fist_at(key, f):
    t = FISTS[key]
    x, y, behind = t[0] if len(t) == 1 else t[f]
    if len(t) == 1:  # one painted pose: the fist breathes with the body
        y += IDLE_POSE_LIFT[f] * float(lift_at(np.array(y)))
    return x, y, behind


def all_cells():
    """{key: [premultiplied cells]}: painted rows, then the mirrored ones."""
    out = {}
    for key in FISTS:
        if key not in MIRRORED:
            out[key] = row_cells(key)[0]
    for key, src in MIRRORED.items():
        out[key] = [c[:, ::-1].copy() for c in out[src]]
    return out


def hand_cell(key, f, cell):
    src = MIRRORED.get(key)
    if key[1] == "left" and src and key[0] != "hurt":  # game: right row flipped, hand layer included
        x, y, behind = fist_at(src, f)
        x = FRAME - x
    else:
        x, y, behind = fist_at(key, f)
    if behind:
        return np.zeros_like(cell)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    return cell * np.clip((FIST_R - np.hypot(xx - x, yy - y)) / FEATHER, 0, 1)[..., None]


def anchors_table():
    """Every row: drawn rows on the new fists; rows still old (attack3 down, up, left, dash) only get the one size."""
    old = old_anchors()
    wanted = {}
    for key in FISTS:
        anim, d = key
        frames = []
        for f in range(COUNT[anim]):
            if key in MIRRORED and anim != "hurt":
                x, y, behind = fist_at(MIRRORED[key], f)
                x = FRAME - x
                angle = -old[("staff", anim, "right")][f]["angle"]
            else:
                x, y, behind = fist_at(key, f)
                angle = ANGLE[key][f] if key in ANGLE else ANGLE_FRAME.get((anim, d, f), old[("staff", anim, d)][f]["angle"])
            frames.append({"x": round(float(x), 3), "y": round(float(y), 3), "angle": angle, "s": S, "sx": 1, "behind": bool(behind), "hidden": False, "fallback": False})
        wanted[("staff", anim, d)] = frames
    for (part, anim, d), frames in old.items():
        if (part, anim, d) in wanted or anim == "death" or (anim, d) == ("idle", "down"):
            continue
        wanted[(part, anim, d)] = [dict(a, s=S) for a in frames]
    return wanted


def write_anchors(path):
    """Read, change, write straight away. idle down stays as it is: the fallback row."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    wanted = anchors_table()
    data["anchors"] = [a for a in data["anchors"] if (a["part"], a["anim"], a["dir"]) not in wanted]
    for (part, anim, d), frames in wanted.items():
        data["anchors"].append({"part": part, "anim": anim, "dir": d, "frames": frames})
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def write_sheets(bare_dir, armed_dir, hand_dir, metas=False):
    import uuid
    cells = all_cells()
    for folder in (bare_dir, armed_dir, hand_dir):
        os.makedirs(folder, exist_ok=True)
    if metas and not os.path.exists(hand_dir + ".meta"):
        tpl = open("Assets/Resources/Sprites/classes/warrior/body/hand.meta", encoding="utf-8").read().split("\n")
        open(hand_dir + ".meta", "w", encoding="utf-8", newline="\n").write("\n".join(f"guid: {uuid.uuid4().hex}" if l.startswith("guid:") else l for l in tpl))
    for anim in COUNT:
        bare = np.array(Image.open(f"{BODY}/bare/{anim}.png").convert("RGBA"))
        armed = np.array(Image.open(f"{BODY}/armed/{anim}.png").convert("RGBA"))
        hand_live = f"{BODY}/hand/{anim}.png"
        hand = np.array(Image.open(hand_live).convert("RGBA")) if os.path.exists(hand_live) else np.zeros_like(bare)
        for key, row in cells.items():
            if key[0] != anim:
                continue
            for f, c in enumerate(row):
                x0, y0 = f * FRAME, ROWS[key[1]] * FRAME
                rgba = to_rgba(c)
                bare[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                armed[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                hand[y0:y0 + FRAME, x0:x0 + FRAME] = to_rgba(hand_cell(key, f, c))
        if metas and not os.path.exists(f"{hand_dir}/{anim}.png.meta"):  # before the png: Unity's own meta compresses and caps the sheet (log 2026-11-01)
            tpl = open(HAND_META, encoding="utf-8").read().split("\n")
            open(f"{hand_dir}/{anim}.png.meta", "w", encoding="utf-8", newline="\n").write("\n".join(f"guid: {uuid.uuid4().hex}" if l.startswith("guid:") else l for l in tpl))
        Image.fromarray(bare, "RGBA").save(f"{bare_dir}/{anim}.png")
        Image.fromarray(armed, "RGBA").save(f"{armed_dir}/{anim}.png")
        Image.fromarray(hand, "RGBA").save(f"{hand_dir}/{anim}.png")


def preview(out, tier=0, pad=60):
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    os.makedirs(out, exist_ok=True)
    bare_dir, armed_dir, hand_dir = f"{out}/sheets/bare", f"{out}/sheets/armed", f"{out}/sheets/hand"
    write_sheets(bare_dir, armed_dir, hand_dir)
    _, anchors = comp.load_gear("wizard")
    anchors.update(anchors_table())
    size = FRAME + 2 * pad
    ref = Image.open(f"{BODY}/armed/idle.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    for anim, n in COUNT.items():
        sheet = Image.new("RGBA", (size * (n + 1), size * 4), (58, 62, 70, 255))
        for r, d in enumerate(ROWS):
            sheet.alpha_composite(ref, (pad, r * size + pad))
            frames = []
            for f in range(n):
                c = comp.compose("wizard", anim, d, f, f"{bare_dir}/{anim}.png", anchors, f"{hand_dir}/{anim}.png", tier=tier, pad=pad)
                sheet.alpha_composite(c, ((f + 1) * size, r * size))
                bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
                bg.alpha_composite(c)
                frames.append(bg.resize((size * 2, size * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
            frames[0].save(f"{out}/{anim}-{d}.gif", save_all=True, append_images=frames[1:], duration=200, loop=0, disposal=2)
        sheet.convert("RGB").save(f"{out}/{anim}.png")


if __name__ == "__main__":
    if sys.argv[1] == "cut":
        cut(sys.argv[2], sys.argv[3:] or None)
    elif sys.argv[1] == "preview":
        preview(sys.argv[2])
    elif sys.argv[1] == "write":
        write_sheets(f"{BODY}/bare", f"{BODY}/armed", f"{BODY}/hand", metas=True)
        write_anchors(ANCHORS)
        print("sheets and anchors written")

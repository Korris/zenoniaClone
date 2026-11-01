"""Cleric body sheets in the new style, without wings, cut from ChatGPT art on flat #FF00FF (the art has green eyes, so green cannot be the key).
Sources: Assets/ArtLibrary/classes/cleric/new-style/src/<anim>-<dir>.png, one row of frames per image. Old sheets and old anchors: new-style/old/.
Row 0 of idle (idle facing down) and dash are not touched. Only rows that have a source image are written; the others keep the old art.

The staff is always in her right hand: screen left facing down, screen right facing up, the forward near fist facing right, the forward far fist facing left.
Anchors: x, y on that fist (FIST_FIX corrects a wrong automatic pick), angle kept from the old anchors, s = 0.52 and sx = 1 in every frame, no fallback.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-cleric-new-style.py cut OUT_DIR       frames with a pixel grid and the detected fists; Resources untouched
  python Tools/build-cleric-new-style.py preview OUT_DIR   sheets and anchors as copies, composited the way the game draws; Resources untouched
  python Tools/build-cleric-new-style.py write             bare, armed and hand sheets, and the anchors of these rows in held-cleric-melee.json"""
import importlib.util, json, os, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

FRAME, HEIGHT, SOLE_ROW = 192, 164, 190
ROOT_ART = "Assets/ArtLibrary/classes/cleric/new-style"
SRC, OLD = f"{ROOT_ART}/src", f"{ROOT_ART}/old"
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/cleric/body"
ANCHORS = f"{RES}/Data/classes/cleric/held-cleric-melee.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"idle": 1, "walk": 4, "hurt": 2, "attack1": 4, "attack2": 4, "attack3": 6, "clericcast": 3}
SHEET_COLS = {"idle": 4, "walk": 4, "hurt": 2, "attack1": 4, "attack2": 4, "attack3": 6, "clericcast": 3}
STAFF_S = 0.52
LIFT = [0.0, -0.7, -1.4, -0.7]  # idle breathing, as in build-warrior-move.py
ANKLE_Y, WAIST_Y = 174.0, 129.0
FIST_R, FEATHER = 7.5, 1.0
CHIN_Y, THIGH_Y = 104, 146  # cell rows: the head ends at the chin, skin below THIGH_Y is legs
# Idle facing up is frame 3 of walk up: same painted arm, feet together. Saves a drawing and keeps the row matched.
BORROW = {("idle", "up"): ("walk", "up", 3)}
# (anim, dir, frame): fist in cell px, where the automatic pick is wrong. Read from the `cut` grids.
FIST_FIX = {("attack1", "left", 1): (61, 58), ("attack2", "left", 1): (61, 58), ("attack1", "up", 1): (140, 69), ("attack2", "up", 1): (142, 77), ("attack2", "up", 0): (134, 121),
            ("attack3", "up", 0): (127, 143), ("attack3", "up", 2): (127, 143), ("attack3", "up", 4): (127, 143),
            ("attack2", "right", 1): (132, 65), ("attack2", "down", 2): (86, 149), ("attack3", "down", 5): (83, 140),
            ("walk", "right", 1): (125, 144), ("walk", "right", 2): (134, 138), ("walk", "right", 3): (125, 146), ("walk", "up", 3): (142, 111),
            ("idle", "up", 0): (142, 111), ("idle", "up", 1): (142, 110.5), ("idle", "up", 2): (142, 110), ("idle", "up", 3): (142, 110.5)}
# (anim, dir): angle per frame, where the old anchors do not apply because the fist no longer moves that way.
ANGLE_FIX = {("walk", "down"): [-6, -8, -6, -4], ("hurt", "up"): [30, 30], ("hurt", "right"): [30, 29]}
# (anim, dir, frame): one frame's angle: the old one pointed the staff at the ground from a fist that is now low.
FRAME_ANGLE = {("attack1", "up", 2): 60, ("attack1", "down", 2): -45, ("attack1", "right", 0): 40, ("attack3", "down", 3): -60, ("attack3", "down", 5): -60, ("attack3", "right", 0): 60, ("attack3", "right", 4): 60}
# (anim, dir, frame): behind flag, where the row default is wrong.
BEHIND_FIX = {}


def key_magenta(rgb):
    """Premultiplied RGBA floats. Edge pixels are unmixed from the key so no pink fringe stays."""
    r, g, b = [rgb[..., i].astype(np.float64) for i in range(3)]
    excess = np.minimum(r, b) - g
    alpha = 1 - np.clip((excess - 30) / 120, 0, 1)
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        keep = np.isin(fg, 1 + np.where(sizes >= 400)[0])
        alpha = alpha * ndimage.binary_dilation(keep, iterations=3)
    a = np.maximum(alpha, 0.02)
    r2 = np.clip((r - (1 - alpha) * 255) / a, 0, 255)
    b2 = np.clip((b - (1 - alpha) * 255) / a, 0, 255)
    g2 = np.clip(g / a, 0, 255)
    return np.dstack([r2 * alpha, g2 * alpha, b2 * alpha, alpha * 255])


def split(alpha, count):
    cols = (alpha > 128).sum(0) > 0
    labels, n = ndimage.label(cols)
    runs = [np.where(labels == i)[0][[0, -1]] for i in range(1, n + 1)]
    runs = sorted(runs, key=lambda r: r[0] - r[1])[:count]
    return sorted((int(a), int(b) + 1) for a, b in runs)


def trimmed(cell):
    ys = np.where((cell[..., 3] > 128).any(1))[0]
    xs = np.where((cell[..., 3] > 128).any(0))[0]
    return cell[ys[0]:ys[-1] + 1, xs[0]:xs[-1] + 1]


def resize_pm(pm, scale):
    w, h = round(pm.shape[1] * scale), round(pm.shape[0] * scale)
    chans = [np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]
    return np.clip(np.dstack(chans), 0, 255)


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def painted(anim, direction):
    pm = key_magenta(np.array(Image.open(f"{SRC}/{anim}-{direction}.png").convert("RGB")))
    runs = split(pm[..., 3], COUNT[anim])
    assert len(runs) == COUNT[anim], (anim, direction, runs)
    return [trimmed(pm[:, a:b]) for a, b in runs]


def hat_cx(cell):
    """Centre of the top 22% of the figure: only the hat, above any raised fist."""
    top = cell[: max(4, int(cell.shape[0] * 0.22)), :, 3] > 128
    xs = np.where(top.any(0))[0]
    return (xs[0] + xs[-1]) / 2


def place_row(frames):
    """One scale for the row, soles on SOLE_ROW, hats in line; the row is centred by the lower body of its first frame."""
    h = float(np.median([f.shape[0] for f in frames]))
    scale = HEIGHT / h
    scaled = [resize_pm(f, scale) for f in frames]
    low = scaled[0][scaled[0].shape[0] // 2:, :, 3] > 128
    body = np.where(low.any(0))[0]
    base = FRAME / 2 - ((body[0] + body[-1]) / 2 - hat_cx(scaled[0]))
    out, notes = [], []
    for f in scaled:
        cell = np.zeros((FRAME, FRAME, 4))
        x, y = round(base - hat_cx(f)), SOLE_ROW + 1 - f.shape[0]
        sx0, sy0 = max(0, -x), max(0, -y)
        ex, ey = min(FRAME, x + f.shape[1]), min(FRAME, y + f.shape[0])
        cell[y + sy0:ey, x + sx0:ex] = f[sy0:ey - y, sx0:ex - x]
        if x < 0 or x + f.shape[1] > FRAME or y < 0:
            notes.append(f"clipped x {x}..{x + f.shape[1]} y {y}")
        out.append(cell)
    return out, scale, notes


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    src_y = y - LIFT[f] * lift_at(y)
    return np.clip(np.dstack([ndimage.map_coordinates(pm[..., c], [src_y, x], order=3, mode="constant") for c in range(4)]), 0, 255)


def all_rows():
    """{(anim, dir): [premultiplied cells]}, in sheet frame count, and a measure table."""
    rows, table = {}, []
    for name in sorted(n for n in os.listdir(SRC) if n.endswith(".png")):  # Unity writes .meta files beside them
        anim, direction = name[:-4].split("-")
        cells, scale, notes = place_row(painted(anim, direction))
        if anim == "idle":
            cells = [breathe(cells[0], f) for f in range(4)]
        rows[(anim, direction)] = cells
        tops = [int(np.where((c[..., 3] > 128).any(1))[0][0]) for c in cells]
        table.append(f"{anim:10s} {direction:5s} scale {scale:.3f} top {tops} {' '.join(notes)}")
    for (anim, direction), (src_anim, src_dir, frame) in BORROW.items():
        if (anim, direction) not in rows and (src_anim, src_dir) in rows:
            rows[(anim, direction)] = [breathe(rows[(src_anim, src_dir)][frame], f) for f in range(4)]
    return rows, table


def skin_blobs(cell):
    """Fist candidates: skin blobs outside the head (hat width, above the chin) and above the thighs."""
    rgba = to_rgba(cell).astype(np.int32)
    r, g, b, a = [rgba[..., i] for i in range(4)]
    mask = (a > 200) & (r > 195) & (g > 95) & (g < 228) & (b > 70) & (b < 212) & (r - g > 18) & (g - b < 45) & (g - b > 2)  # hair is yellower: g - b about 70
    top = np.where((a > 128).any(1))[0][0]
    hat = np.where((a[top:top + 40] > 128).any(0))[0]
    mask[:CHIN_Y, max(0, hat[0] - 1):hat[-1] + 2] = False
    mask[THIGH_Y:] = False
    mask = ndimage.binary_closing(mask, iterations=1)
    lab, n = ndimage.label(mask)
    eyes = (a > 200) & (g > r + 25) & (g > b)  # green eyes mark the cheek, which can sit outside the hat's width
    out = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if ys.mean() < CHIN_Y + 8 and eyes[ndimage.binary_dilation(lab == i, iterations=12)].any():  # lower down, cyan robe gems would match
            continue
        if len(ys) >= 25 and xs.max() - xs.min() < 34 and ys.max() - ys.min() < 34:
            out.append((round(float(xs.mean()), 1), round(float(ys.mean()), 1), len(ys)))
    return out


def staff_fist(anim, direction, f, cell):
    if (anim, direction, f) in FIST_FIX:
        return FIST_FIX[(anim, direction, f)]
    blobs = skin_blobs(cell)
    if not blobs:
        return None
    left = direction in ("down", "left")
    if anim == "clericcast":  # the old cast row holds the staff in the raised hand, whichever side that is: follow its old anchor
        left = old_anchors()[("staff", anim, direction)][f]["x"] < FRAME / 2
    pick = min(blobs, key=lambda b: b[0]) if left else max(blobs, key=lambda b: b[0])
    return pick[0], pick[1]


def old_anchors():
    base = json.load(open(f"{OLD}/held-cleric.json", encoding="utf-8"))["anchors"]
    out = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in base}
    for a in json.load(open(f"{OLD}/held-cleric-melee.json", encoding="utf-8"))["anchors"]:
        out[(a["part"], a["anim"], a["dir"])] = a["frames"]
    return out


def old_angle(old, anim, direction, f):
    if (anim, direction, f) in FRAME_ANGLE:
        return FRAME_ANGLE[(anim, direction, f)]
    if (anim, direction) in ANGLE_FIX:
        return ANGLE_FIX[(anim, direction)][f]
    frames = old[("staff", anim, direction)]
    if not frames[f].get("fallback"):
        return frames[f]["angle"]
    for d in range(1, len(frames)):  # a fallback frame has no angle: take the nearest frame that has one
        for i in (f - d, f + d):
            if 0 <= i < len(frames) and not frames[i].get("fallback"):
                return frames[i]["angle"]
    return 0


def behind_for(anim, direction, f):
    if (anim, direction, f) in BEHIND_FIX:
        return BEHIND_FIX[(anim, direction, f)]
    return direction in ("up", "left")  # back view and the far arm: the body and that fist draw over the staff


def anchors_for(rows):
    old = old_anchors()
    wanted, missing = {}, []
    for (anim, direction), cells in rows.items():
        frames = []
        for f, cell in enumerate(cells):
            fist = staff_fist(anim, direction, f, cell)
            if fist is None:
                missing.append((anim, direction, f))
                fist = (96, 120)
            frames.append({"x": fist[0], "y": fist[1], "angle": old_angle(old, anim, direction, f), "s": STAFF_S, "sx": 1,
                           "behind": behind_for(anim, direction, f), "hidden": False, "fallback": False})
        wanted[("staff", anim, direction)] = frames
    return wanted, missing


def fist_mask(cx, cy):
    y, x = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    return np.clip((FIST_R - np.hypot(x - cx, y - cy)) / FEATHER, 0, 1)


def write_sheets(rows, wanted, bare_dir, armed_dir, hand_dir):
    for folder in (bare_dir, armed_dir, hand_dir):
        os.makedirs(folder, exist_ok=True)
    for anim in sorted({k[0] for k in rows}):
        live = np.array(Image.open(f"{BODY}/bare/{anim}.png").convert("RGBA"))
        armed = np.array(Image.open(f"{BODY}/armed/{anim}.png").convert("RGBA"))
        hand_path = f"{BODY}/hand/{anim}.png"
        hand = np.array(Image.open(hand_path).convert("RGBA")) if os.path.exists(hand_path) else np.zeros_like(live)
        assert live.shape[1] == SHEET_COLS[anim] * FRAME, anim
        for direction, row in ROWS.items():
            if (anim, direction) not in rows:
                continue
            for f, cell in enumerate(rows[(anim, direction)]):
                x0, y0 = f * FRAME, row * FRAME
                rgba = to_rgba(cell)
                live[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                armed[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                a = wanted[("staff", anim, direction)][f]
                over = np.zeros_like(cell) if a["behind"] else cell * fist_mask(a["x"], a["y"])[..., None]
                hand[y0:y0 + FRAME, x0:x0 + FRAME] = to_rgba(over)
        Image.fromarray(live, "RGBA").save(f"{bare_dir}/{anim}.png")
        Image.fromarray(armed, "RGBA").save(f"{armed_dir}/{anim}.png")
        Image.fromarray(hand, "RGBA").save(f"{hand_dir}/{anim}.png")


def write_anchors(path, wanted):
    """Read, change, write straight away. idle facing down stays as it is."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    done = set()
    for a in data["anchors"]:
        key = (a["part"], a["anim"], a["dir"])
        if key in wanted:
            a["frames"] = wanted[key]
            done.add(key)
    for key, frames in wanted.items():
        if key not in done:
            data["anchors"].append({"part": key[0], "anim": key[1], "dir": key[2], "frames": frames})
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def write_metas():
    """New hand sheets take the import settings of body/bare, or Unity compresses them and they blur against the body."""
    folder = f"{BODY}/hand"
    if not os.path.exists(folder + ".meta"):
        open(folder + ".meta", "w", newline="\n").write(open(f"{BODY}/bare.meta").read().replace(
            open(f"{BODY}/bare.meta").read().split("guid: ")[1].split()[0], uuid.uuid4().hex))
    for name in os.listdir(folder):
        if name.endswith(".png") and not os.path.exists(f"{folder}/{name}.meta"):
            text = open(f"{BODY}/bare/{name}.meta").read()
            open(f"{folder}/{name}.meta", "w", newline="\n").write(text.replace(text.split("guid: ")[1].split()[0], uuid.uuid4().hex))


def load_compose():
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    return comp


def cut(out):
    os.makedirs(out, exist_ok=True)
    rows, table = all_rows()
    print("\n".join(table))
    z = 3
    for (anim, direction), cells in rows.items():
        if anim == "idle":
            cells = cells[:1]
        w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for f, c in enumerate(cells):
            im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (f * FRAME * z, 0), im)
            ox = f * FRAME * z
            for v in range(0, FRAME, 16):
                col = (255, 255, 0) if v % 48 == 0 else (110, 110, 110)
                d.line([(ox + v * z, 0), (ox + v * z, 8)], fill=col)
                d.line([(ox, v * z), (ox + 8, v * z)], fill=col)
                if v % 48 == 0:
                    d.text((ox + v * z + 2, 10), str(v), fill=col)
                    d.text((ox + 10, v * z + 2), str(v), fill=col)
            for bx, by, n in skin_blobs(c):
                d.ellipse([ox + bx * z - 4, by * z - 4, ox + bx * z + 4, by * z + 4], outline=(0, 255, 255))
            fist = staff_fist(anim, direction, f, c)
            if fist:
                d.ellipse([ox + fist[0] * z - 7, fist[1] * z - 7, ox + fist[0] * z + 7, fist[1] * z + 7], outline=(255, 0, 0), width=2)
        w.save(f"{out}/grid-{anim}-{direction}.png")


def preview(out, pad=50):
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    rows, table = all_rows()
    wanted, missing = anchors_for(rows)
    print("\n".join(table))
    print("no fist found:", missing)
    bare_dir, armed_dir, hand_dir = f"{out}/bare", f"{out}/armed", f"{out}/hand"
    write_sheets(rows, wanted, bare_dir, armed_dir, hand_dir)
    _, anchors = comp.load_gear("cleric")
    anchors.update(wanted)
    ref = Image.open(f"{BODY}/armed/idle.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    size = FRAME + 2 * pad
    for anim in sorted({k[0] for k in rows}):
        cols = SHEET_COLS[anim]
        sheet = Image.new("RGBA", (size + size * cols, size * 4), (58, 62, 70, 255))
        sheet.alpha_composite(ref, (pad, pad))
        for direction, r in ROWS.items():
            if (anim, direction) not in rows:
                continue
            frames = []
            for f in range(cols):
                c = comp.compose("cleric", anim, direction, f, f"{bare_dir}/{anim}.png", anchors, f"{hand_dir}/{anim}.png", pad=pad)
                sheet.alpha_composite(c, (size + f * size, r * size))
                bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
                bg.alpha_composite(c)
                frames.append(bg.convert("P", palette=Image.ADAPTIVE))
            frames[0].save(f"{out}/{anim}-{direction}.gif", save_all=True, append_images=frames[1:], duration=160, loop=0, disposal=2)
        sheet.convert("RGB").save(f"{out}/{anim}.png")


if __name__ == "__main__":
    if sys.argv[1] == "cut":
        cut(sys.argv[2])
    elif sys.argv[1] == "preview":
        preview(sys.argv[2])
    elif sys.argv[1] == "write":
        rows, _ = all_rows()
        wanted, missing = anchors_for(rows)
        if missing:
            raise SystemExit(f"no fist found: {missing}")
        write_sheets(rows, wanted, f"{BODY}/bare", f"{BODY}/armed", f"{BODY}/hand")
        write_metas()
        write_anchors(ANCHORS, wanted)
        print("written", len(rows), "rows")

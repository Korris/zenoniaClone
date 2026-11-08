"""Warrior idle (rows 1-3), walk and hurt in the new style, cut from ChatGPT art on flat #00FF00. dash is left alone (user decision).
Sources: Assets/ArtLibrary/classes/warrior/new-style/move/src/mv-<anim>-<dir>.png, one row of frames per image.
walk and idle facing right are designed fresh from the style reference; the other rows are the old rows redrawn. Old sheets are in new-style/move/old/.
The left row is the right row mirrored, so the gear changes arms there (rule 4): near arm = shield in front, far arm = sword behind the body.

Every frame of a row shares one scale. Rows are 164 px head to sole at their tallest; hurt crouches, so it takes the scale of walk in the same direction by hair width.
Soles land on row 186, frames line up on the head. An idle row is one painted pose with a small breathing lift: AI frames of one pose jitter.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-warrior-move.py cut OUT_DIR       frames with a pixel grid, for reading fist positions; Resources untouched
  python Tools/build-warrior-move.py preview OUT_DIR   sheets and anchors as copies, composited the way the game draws; Resources untouched
  python Tools/build-warrior-move.py write             bare, armed, hand sheets and the anchors of these anims in held-gear-melee.json"""
import copy, importlib.util, json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

FRAME, HEIGHT, SOLE_ROW = 192, 164, 186
# ChatGPT painted this row with a smaller head than the approved idle-down (68 px against 77, chin row 93): (factor, neck row).
HEAD_GROW = {("walk", "down"): (1.1, 95)}
NECK_BLEND = 12
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/warrior/body"
ANCHORS = f"{RES}/Data/classes/warrior/held-gear-melee.json"
SRC = "Assets/ArtLibrary/classes/warrior/new-style/move/src"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
PAINTED = {("idle", "up"): 4, ("idle", "right"): 4, ("idle", "left"): 1, ("walk", "down"): 4, ("walk", "up"): 4, ("walk", "right"): 4,
           ("hurt", "down"): 2, ("hurt", "up"): 2, ("hurt", "right"): 2}
IDLE_POSE = 0  # the painted frame an idle row is made from
LIFT = [0.0, -0.7, -1.4, -0.7]  # breathing, frame px, as in build-warrior-idle-down.py; 0 at the ankles, full from the waist up
ANKLE_Y, WAIST_Y = 170.0, 125.0
HEAD_X = 96  # hair centre in the cell, so turning around does not jump

SWORD_S, SHIELD_S, SWORD_DRAW, HAND_Y = 0.34, 0.27, 2.0, 217  # HAND_Y: sword art row of the middle of the handle
FIST_R, FEATHER = 8.5, 1.0  # fist layer; tight, or it repaints arm and cape over the handle
SHIELD_ABOVE_FIST = 6  # the strap sits on the forearm, just above the fist

# Fist centres per frame in the painted row (cell px), read from the `cut` grid at 4x.
# down: screen left = right hand = sword. up: screen right = right hand = sword. right: near = right hand = sword.
# Each entry is (sword fist, shield fist) for down, up and right. An idle row has one pose, so one entry.
FISTS = {
    ("walk", "down"): [((68, 131), (129, 132)), ((68, 131), (130, 130)), ((68, 129), (129, 129)), ((68, 129), (129, 130))],
    ("walk", "up"): [((134, 125), (58, 112)), ((130.5, 119), (46, 119)), ((134, 111), (52, 115)), ((130, 121), (44, 119))],
    ("walk", "right"): [((68, 138), (134, 123)), ((81.5, 144), (112, 140)), ((127, 127), (68, 127)), ((92, 144), (113, 139))],
    ("idle", "up"): [((138, 128), (58, 116))],
    ("idle", "right"): [((89, 137), (134, 119))],
    ("idle", "left"): [((60, 136), (99, 142))],  # painted facing left: far fist low = sword, near fist = shield. A mirrored row raises the sword arm. The fist is at (53, 140); the sword sits 8 px up its own axis so the fist closes on the handle.
    ("hurt", "down"): [((65.5, 151), (136, 151)), ((67, 152), (137, 151))],
    ("hurt", "up"): [((147, 136), (56, 127)), ((147, 136), (58, 127))],
    ("hurt", "right"): [((80, 136), (129, 137)), ((78.5, 136), (130, 137))],
}
# Clockwise, tip up = 0. The sword swings with the near arm when walking sideways.
SWORD_ANGLE = {("idle", "left"): [-120] * 4, ("walk", "down"): [-139, -135, -131, -135], ("walk", "up"): [139, 135, 131, 135], ("walk", "right"): [150, 125, 105, 125],
               ("idle", "up"): [135] * 4, ("idle", "right"): [120] * 4,
               ("hurt", "down"): [-140, -140], ("hurt", "up"): [140, 140], ("hurt", "right"): [125, 125]}
SHIELD_ANGLE = {("idle", "left"): [-9] * 4, ("walk", "down"): [2, 0, -2, 0], ("walk", "up"): [-2, 0, 2, 0], ("walk", "right"): [10, 8, 6, 8],
                ("idle", "up"): [0] * 4, ("idle", "right"): [9] * 4,
                ("hurt", "down"): [-10, -10], ("hurt", "up"): [0, 0], ("hurt", "right"): [9, 9]}
LEFT_WALK_SWORD = [-105, -125, -150, -125]  # the far arm swings the other way: walk right half a step later, mirrored (arm forward -105, arm back -150). Arm back the tip stays forward-down; 125 there flipped the blade behind the hero (2026-11-07)
ROW_SHIFT = {("hurt", "right"): 8}  # the leaning hair would put the cape past the cell


def key_green(rgb):
    """Premultiplied RGBA floats from art on flat green. The art has no green, so every green pixel counts, holes between the legs too."""
    r, g, b = [rgb[..., i].astype(np.float64) for i in range(3)]
    excess = g - np.maximum(r, b)
    alpha = 1 - np.clip((excess - 12) / 60, 0, 1)
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        keep = np.isin(fg, 1 + np.where(sizes >= 400)[0])
        alpha = alpha * ndimage.binary_dilation(keep, iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def resize_pm(pm, scale):
    w, h = round(pm.shape[1] * scale), round(pm.shape[0] * scale)
    chans = [np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]
    return np.clip(np.dstack(chans), 0, 255)


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


def hair_box(pm):
    """x0, x1 of the hair: strong red in the top 40% of the figure (the cape is lower)."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g, b = [pm[..., i] / a[..., 0] for i in range(3)]
    mask = (r > 150) & (g < 90) & (b < 90) & (pm[..., 3] > 128)
    top = mask[: int(pm.shape[0] * 0.4)]
    xs = np.where(top.sum(0) >= 2)[0]
    return int(xs[0]), int(xs[-1]) + 1


def hair_tint(pm):
    """Green/red of the hair: some images came out more orange."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g, b = [pm[..., i] / a[..., 0] for i in range(3)]
    top = np.zeros(r.shape, bool)
    top[: int(pm.shape[0] * 0.3)] = True
    mask = top & (r > 150) & (r - g > 60) & (pm[..., 3] > 200)
    return float(np.median(g[mask] / r[mask]))


def match_red(pm, target):
    """Pulls strong red pixels (hair, cape) to the tint of the approved art. Leaves skin and gold alone."""
    tint = hair_tint(pm)
    gain = float(np.clip(target / tint, 0.5, 1.2))
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g = pm[..., 0] / a[..., 0], pm[..., 1] / a[..., 0]
    weight = np.clip((r - g - 90) / 40, 0, 1)
    out = pm.copy()
    out[..., 1] = pm[..., 1] * (1 - weight + weight * gain)
    out[..., 2] = pm[..., 2] * (1 - weight + weight * gain)
    return out


def painted(anim, direction):
    path = f"{SRC}/mv-{anim}-{direction}.png"
    pm = key_green(np.array(Image.open(path).convert("RGB")))
    return [trimmed(pm[:, a:b]) for a, b in split(pm[..., 3], PAINTED[(anim, direction)])]


def head_offset(ref, cell, size=60, reach=8):
    """dx that lays the top of cell on the top of ref, by the alpha and red of the head."""
    best, best_dx = None, 0
    h = min(size, ref.shape[0], cell.shape[0])
    a, b = ref[:h, :, 0] + ref[:h, :, 3], cell[:h, :, 0] + cell[:h, :, 3]
    w = max(a.shape[1], b.shape[1]) + 2 * reach
    pa = np.zeros((h, w))
    pa[:, reach:reach + a.shape[1]] = a
    for dx in range(-reach, reach + 1):
        pb = np.zeros((h, w))
        pb[:, reach + dx:reach + dx + b.shape[1]] = b
        err = ((pa - pb) ** 2).mean()
        if best is None or err < best:
            best, best_dx = err, dx
    return best_dx


def grow_head(pm, k, neck):
    """Head alone k times bigger, growing up from the neck; the size eases back to 1 over NECK_BLEND rows below it, so shoulders, arms and fists stay where the anchors expect them."""
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    t = np.clip((neck + NECK_BLEND - y) / NECK_BLEND, 0, 1)
    across = 1 + (k - 1) * t
    src_x = HEAD_X + (x - HEAD_X) / across
    src_y = np.where(y < neck, neck - (neck - y) / k, y)
    return np.stack([ndimage.map_coordinates(pm[..., c], [src_y, src_x], order=3, mode="constant") for c in range(4)], axis=-1).clip(0, 255)


def row_cells():
    """{(anim, dir): [premultiplied 192 cells]} plus a table of what was measured."""
    scales, hair, table, cells = {}, {}, [], {}
    target = hair_tint(np.array(Image.open(f"{BODY}/armed/idle-down-big.png").convert("RGBA").crop((0, 0, 288, 240)), dtype=np.float64) * [1, 1, 1, 1])
    for key in (("walk", "down"), ("walk", "up"), ("walk", "right"), ("idle", "up"), ("idle", "right"), ("idle", "left"), ("hurt", "down"), ("hurt", "up"), ("hurt", "right")):
        anim, direction = key
        raw = [match_red(f, target) for f in painted(anim, direction)]
        if anim == "hurt":
            x0, x1 = hair_box(raw[0])
            scale = hair[direction] / (x1 - x0)
        else:
            scale = HEIGHT / max(f.shape[0] for f in raw)
        scales[key] = scale
        frames = [resize_pm(f, scale) for f in raw]
        if anim == "walk":
            x0, x1 = hair_box(frames[0])
            hair[direction] = x1 - x0
        if anim == "idle":
            frames = [frames[IDLE_POSE]] * 4
        x0, x1 = hair_box(frames[0])
        base = HEAD_X - (x0 + x1) // 2 + ROW_SHIFT.get(key, 0)
        out = []
        for i, f in enumerate(frames):
            cell = np.zeros((FRAME, FRAME, 4))
            x = base + head_offset(frames[0], f)
            y = SOLE_ROW + 1 - f.shape[0]
            sx0, sy0 = max(0, -x), max(0, -y)
            ex, ey = min(FRAME, x + f.shape[1]), min(FRAME, y + f.shape[0])
            cell[y + sy0:ey, x + sx0:ex] = f[sy0:sy0 + ey - y - sy0, sx0:sx0 + ex - x - sx0]
            if x < 0 or x + f.shape[1] > FRAME or y < 0:
                table.append(f"CLIPPED {key} frame {i}: x {x}..{x + f.shape[1]} y {y}")
            out.append(cell)
        if anim == "idle":
            out = [breathe(out[0], f) for f in range(4)]
        if key in HEAD_GROW:
            out = [grow_head(c, *HEAD_GROW[key]) for c in out]
        cells[key] = out
        measure = [np.where((c[..., 3] > 128).any(1))[0] for c in out]
        widths = [np.where((c[..., 3] > 128).any(0))[0] for c in out]
        table.append(f"{anim:5s} {direction:5s} scale {scale:.3f} top {[int(m[0]) for m in measure]} sole {[int(m[-1]) for m in measure]} x {[(int(w[0]), int(w[-1])) for w in widths]} hair {hair_box(trimmed(out[0]))}")
    return cells, table


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    src_y = y - LIFT[f] * lift_at(y)
    return np.clip(np.dstack([ndimage.map_coordinates(pm[..., c], [src_y, x], order=3, mode="constant") for c in range(4)]), 0, 255)


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def fist_mask(cx, cy):
    y, x = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    return np.clip((FIST_R - np.hypot(x - cx, y - cy)) / FEATHER, 0, 1)


def mirrored(cells):
    return [c[:, ::-1].copy() for c in cells]


def fists_for(anim, direction, f):
    """(near or sword fist, far or shield fist) in this row's frame, with the breathing lift of an idle row."""
    painted_left = (anim, direction) in FISTS and direction == "left"
    table = FISTS[(anim, direction if direction != "left" or painted_left else "right")]
    a, b = table[0 if anim == "idle" else f]
    if anim == "idle":
        a = (a[0], a[1] + LIFT[f] * float(lift_at(np.array(a[1]))))
        b = (b[0], b[1] + LIFT[f] * float(lift_at(np.array(b[1]))))
    if direction == "left" and not painted_left:
        a, b = (FRAME - a[0], a[1]), (FRAME - b[0], b[1])
    return a, b


def sword_at(fist, angle, grip_y):
    th = math.radians(angle)
    off = (HAND_Y - grip_y) * SWORD_S * SWORD_DRAW
    return round(fist[0] + math.sin(th) * off, 3), round(fist[1] - math.cos(th) * off, 3)


def anchors_for(anim, direction, f, grip_y):
    """Sword and shield anchor of one frame. The hand layer goes over whatever is in front."""
    a, b = fists_for(anim, direction, f)
    painted_left = (anim, direction) in FISTS and direction == "left"
    src = direction if direction != "left" or painted_left else "right"
    sword_angle, shield_angle = SWORD_ANGLE[(anim, src)][f], SHIELD_ANGLE[(anim, src)][f]
    if direction == "left" and not painted_left:
        sword_angle, shield_angle = -sword_angle, -shield_angle
        if anim == "walk":
            sword_angle = LEFT_WALK_SWORD[f]
        sword_fist, shield_fist = b, a  # mirrored: the near arm has the shield, the far fist the sword
    else:
        sword_fist, shield_fist = a, b
    behind_sword = direction in ("up", "left")
    behind_shield = direction in ("up", "right")
    x, y = sword_at(sword_fist, sword_angle, grip_y)
    sword = {"x": x, "y": y, "angle": sword_angle, "s": SWORD_S, "sx": 1, "behind": behind_sword, "hidden": False, "fallback": False}
    shield = {"x": round(shield_fist[0], 3), "y": round(shield_fist[1] - SHIELD_ABOVE_FIST, 3), "angle": shield_angle, "s": SHIELD_S, "sx": 1,
              "behind": behind_shield, "hidden": False, "fallback": False}
    return sword, shield


def sheets():
    """{anim: {row: [cells]}} premultiplied, the left row included."""
    cells, table = row_cells()
    out = {}
    for (anim, direction), row in cells.items():
        out.setdefault(anim, {})[ROWS[direction]] = row
        if direction == "right" and (anim, "left") not in cells:
            out[anim][ROWS["left"]] = mirrored(row)
    return out, table


def hand_cell(anim, row, f, cell):
    """Only the sword fist where the sword is in front of the body: down and right. The shield hides its own hand, as in idle-down-big;
    facing up and left the sword is behind the body, which draws that fist itself."""
    direction = [k for k, v in ROWS.items() if v == row][0]
    if direction in ("up", "left"):
        return np.zeros_like(cell)
    return cell * fist_mask(*fists_for(anim, direction, f)[0])[..., None]


def write_sheets(bare_dir, armed_dir, hand_dir):
    data, _ = sheets()
    for anim, rows in data.items():
        cols = len(rows[ROWS["up"]])
        for folder in (bare_dir, armed_dir, hand_dir):
            os.makedirs(folder, exist_ok=True)
        bare_path, armed_path, hand_path = f"{bare_dir}/{anim}.png", f"{armed_dir}/{anim}.png", f"{hand_dir}/{anim}.png"
        bare = np.array(Image.open(f"{BODY}/bare/{anim}.png").convert("RGBA"))
        armed = np.array(Image.open(f"{BODY}/armed/{anim}.png").convert("RGBA"))
        live_hand = f"{BODY}/hand/{anim}.png"
        hand = np.array(Image.open(live_hand).convert("RGBA")) if os.path.exists(live_hand) else np.zeros_like(bare)
        for row, frames in rows.items():
            for f, cell in enumerate(frames):
                x0, y0 = f * FRAME, row * FRAME
                rgba = to_rgba(cell)
                bare[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                armed[y0:y0 + FRAME, x0:x0 + FRAME] = rgba
                hand[y0:y0 + FRAME, x0:x0 + FRAME] = to_rgba(hand_cell(anim, row, f, cell))
        assert cols * FRAME == bare.shape[1], anim
        Image.fromarray(bare, "RGBA").save(bare_path)
        Image.fromarray(armed, "RGBA").save(armed_path)
        Image.fromarray(hand, "RGBA").save(hand_path)


def write_anchors(path):
    """Read, change, write straight away: other tools write this file too. Only these anims' rows; idle down stays."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    grip_y = json.load(open(f"{RES}/Data/classes/warrior/held-gear.json", encoding="utf-8"))["sword"][0]["baseGrip"]["gripY"]
    wanted = {}
    for (anim, direction) in PAINTED:
        for d in ([direction, "left"] if direction == "right" and (anim, "left") not in PAINTED else [direction]):
            frames = [anchors_for(anim, d, f, grip_y) for f in range(4 if anim == "idle" else PAINTED[(anim, direction)])]
            wanted[("sword", anim, d)] = [p[0] for p in frames]
            wanted[("shield", anim, d)] = [p[1] for p in frames]
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


def load_compose():
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    return comp


def cut(out):
    os.makedirs(out, exist_ok=True)
    cells, table = row_cells()
    print("\n".join(table))
    z = 4
    for (anim, direction), row in cells.items():
        w = Image.new("RGB", (FRAME * z * len(row), FRAME * z), (58, 62, 70))
        for f, c in enumerate(row):
            im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (f * FRAME * z, 0), im)
        d = ImageDraw.Draw(w)
        for f in range(len(row)):
            for v in range(0, FRAME, 16):
                d.line([(f * FRAME * z + v * z, 200), (f * FRAME * z + v * z, FRAME * z)], fill=(255, 255, 0) if v % 48 == 0 else (120, 120, 120))
                if v % 48 == 0:
                    d.text((f * FRAME * z + v * z + 2, 202), str(v), fill=(255, 255, 0))
            for v in range(48, FRAME, 16):
                d.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=(255, 255, 0) if v % 48 == 0 else (120, 120, 120))
                if v % 48 == 0:
                    d.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w.save(f"{out}/grid-{anim}-{direction}.png")


def preview(out, pad=50):
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    bare_dir, armed_dir, hand_dir = f"{out}/bare", f"{out}/armed", f"{out}/hand"
    write_sheets(bare_dir, armed_dir, hand_dir)
    anch = f"{out}/anchors.json"
    open(anch, "wb").write(open(ANCHORS, "rb").read())
    write_anchors(anch)
    _, anchors = comp.load_gear("warrior")
    for a in json.load(open(anch, encoding="utf-8"))["anchors"]:
        anchors[(a["part"], a["anim"], a["dir"])] = a["frames"]
    size = FRAME + 2 * pad
    ref = Image.open(f"{BODY}/armed/idle-down-big.png").convert("RGBA").crop((0, 0, 288, 240))
    for anim, count in (("idle", 4), ("walk", 4), ("hurt", 2)):
        dirs = ["up", "left", "right"] if anim == "idle" else ["down", "up", "left", "right"]
        sheet = Image.new("RGBA", (ref.width + size * count, size * len(dirs)), (58, 62, 70, 255))
        sheet.alpha_composite(ref, (0, 0))
        for r, d in enumerate(dirs):
            frames = []
            for f in range(count):
                c = comp.compose("warrior", anim, d, f, f"{bare_dir}/{anim}.png", anchors, f"{hand_dir}/{anim}.png", pad=pad)
                sheet.alpha_composite(c, (ref.width + f * size, r * size))
                bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
                bg.alpha_composite(c)
                frames.append(bg.resize((size * 2, size * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
            ms = {"idle": 240, "walk": 160, "hurt": 200}[anim]
            frames[0].save(f"{out}/{anim}-{d}.gif", save_all=True, append_images=frames[1:], duration=ms, loop=0, disposal=2)
        sheet.convert("RGB").resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{out}/{anim}.png")


if __name__ == "__main__":
    if sys.argv[1] == "cut":
        cut(sys.argv[2])
    elif sys.argv[1] == "preview":
        preview(sys.argv[2])
        print("preview written")
    else:
        write_sheets(f"{BODY}/bare", f"{BODY}/armed", f"{BODY}/hand")
        write_anchors(ANCHORS)
        print("sheets and anchors written")

"""Warrior idle facing down: a calm ready stance made from the old upright frame by a smooth warp, no new painting.
Writes row 0 of body/bare/idle.png, body/hand/idle.png and body/armed/idle.png, plus the idle/down anchors in held-gear-melee.json. Rows 1-3 are left as they are.

The warp moves the feet apart and the fists away from the body; head, collar and chest stay put. The four frames are that one pose with a small breathing lift:
feet planted, nothing sideways. Fists and anchors follow the lift, so sword and shield stay in the hand.
The armed row has no baked gear, like the old one; the bag portrait (InventoryPanel.SetPortrait) shows its frame 0.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-warrior-idle-down.py preview OUT_DIR   works on copies, nothing in Resources changes
  python Tools/build-warrior-idle-down.py write
Source is idle-down-src/old-bare-idle.png, so running it again gives the same result.
SUPERSEDED by Tools/build-warrior-idle-down-doll.py: `write` would put the old warped body back over the new row, so it refuses. `preview` still works."""
import json, math, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.interpolate import RBFInterpolator

FRAME, RES = 192, "Assets/Resources"
BODY = f"{RES}/Sprites/classes/warrior/body"
ANCHORS = f"{RES}/Data/classes/warrior/held-gear-melee.json"
SOURCE = "Assets/ArtLibrary/classes/warrior/idle-down-src/old-bare-idle.png"


def group(points, dx, dy):
    return [((x, y), (x + dx, y + dy)) for x, y in points]


# (source point, where it goes), frame px, read from the old frame 0 at 5x.
CONTROLS = (
    # hair and face do not move
    group([(96, 14), (55, 45), (140, 45), (50, 60), (140, 60), (75, 80), (115, 80), (95, 90)], 0, 0)
    # collar, shoulders and chest do not move, so the shoulders stay level
    + group([(55, 85), (133, 85), (60, 98), (127, 98), (80, 100), (108, 100), (93, 112), (80, 120), (108, 120), (93, 128)], 0, 0)
    # hips
    + group([(82, 138), (106, 138)], 0, 0)
    # arm at screen left: sword hand out from the body
    + group([(62, 112), (70, 112)], -4, 0)
    + group([(56, 124), (72, 124), (56, 136), (72, 136), (64, 130)], -9, 0)
    # arm at screen right: shield arm out and the fist a little higher
    + group([(124, 112), (116, 112)], 4, 0)
    + group([(116, 124), (133, 124), (116, 136), (133, 136), (124, 130)], 8, -3)
    # cape tips move a little less than the fists, or they would bunch up against them
    + group([(22, 132), (36, 145), (60, 154)], -3, 0)
    + group([(163, 140), (150, 148), (130, 154)], 3, 0)
    # legs: knees part, boots move as one piece. The leg at screen left takes more, so the weight sits on the other one.
    + group([(70, 148), (86, 148)], -5, 0)
    + group([(100, 148), (116, 148)], 4, 0)
    + group([(62, 180), (86, 180), (62, 187), (86, 187), (72, 165)], -10, 0)
    + group([(100, 180), (124, 180), (100, 187), (124, 187), (113, 165)], 7, -2)
)

SWORD_FIST, SHIELD_FIST = (55.0, 130.5), (132.0, 127.0)  # after the warp, frame px; middle of the fist
SWORD_ANGLE = -135.0  # tip down and out, the same as walk facing down
SWORD_S, SHIELD_S, HAND_Y, SWORD_DRAW = 0.34, 0.362, 230, 2.0  # s stays; HAND_Y is the art row of the middle of the handle (build-hand-overlay.py)
SHIELD_AT, SHIELD_ANGLE, SHIELD_BEHIND = (141.0, 124.0), 4.0, False  # over the cape, under the arm and fist of the hand layer. Behind the body the cape would hide it.
RX, RY, FEATHER = 11.0, 10.5, 1.0  # fist ellipse of the layer above the gear; tight, a wider one would cut the handle with cape pixels
# Forearm capsules of that layer, (x0, y0, x1, y1, radius): the sword pommel goes behind the forearm instead of lying on it, and the shield hangs behind the whole arm.
SWORD_ARM, SHIELD_ARM = (69, 102, 59, 122, 12.0), (119, 102, 130, 120, 11.5)
SHARPEN = 0.5  # the warp softens the art; unsharp mask amount, radius 1 px

LIFT = [0.0, -0.7, -1.4, -0.7]  # breathing, frame px; 0 at the ankles, full from the waist up
ANKLE_Y, WAIST_Y = 170.0, 125.0


def premultiplied(cell):
    pm = cell.astype(np.float64)
    pm[..., :3] *= pm[..., 3:4] / 255
    return pm


def unpremultiply(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[..., 3] = np.clip(pm[..., 3], 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def sample(pm, src_y, src_x):
    out = [ndimage.map_coordinates(pm[..., c], [src_y, src_x], order=3, mode="constant") for c in range(4)]
    return np.clip(np.dstack(out), 0, 255)


def pose():
    """The old frame 0 warped by a thin-plate spline through CONTROLS."""
    old = np.array(Image.open(SOURCE).convert("RGBA"))[:FRAME, :FRAME]
    src = np.array([c[0] for c in CONTROLS], dtype=np.float64)
    dst = np.array([c[1] for c in CONTROLS], dtype=np.float64)
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    grid = np.column_stack([x.ravel(), y.ravel()])
    mapped = RBFInterpolator(dst, src, kernel="thin_plate_spline", smoothing=0.5)(grid)
    pm = sample(premultiplied(old), mapped[:, 1].reshape(x.shape), mapped[:, 0].reshape(x.shape))
    for c in range(3):
        sharp = pm[..., c] + SHARPEN * (pm[..., c] - ndimage.gaussian_filter(pm[..., c], 1.0))
        pm[..., c] = np.clip(sharp, 0, pm[..., 3])  # premultiplied: colour never above alpha, so no bright halo on the edge
    return pm


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    return sample(pm, y - LIFT[f] * lift_at(y), x)


def moved(point, f):
    x, y = point
    return x, y + LIFT[f] * float(lift_at(np.array(y)))


def fist_mask(point):
    cx, cy = point
    y, x = np.mgrid[0:FRAME, 0:FRAME]
    d = np.sqrt(((x + 0.5 - cx) / RX) ** 2 + ((y + 0.5 - cy) / RY) ** 2)
    return np.clip((1 - d) * min(RX, RY) / FEATHER, 0, 1)


def arm_mask(capsule, f):
    x0, y0, x1, y1, r = capsule
    dy = moved((0, y1), f)[1] - y1
    y, x = np.mgrid[0:FRAME, 0:FRAME] + 0.5
    ax, ay = x1 - x0, y1 - y0
    t = np.clip(((x - x0) * ax + (y - y0 - dy) * ay) / (ax * ax + ay * ay), 0, 1)
    d = np.hypot(x - x0 - t * ax, y - y0 - dy - t * ay)
    return np.clip((r - d) / FEATHER, 0, 1)


def anchors_for(f, grip_y, sword_angle=None):
    sword_angle = SWORD_ANGLE if sword_angle is None else sword_angle
    th = math.radians(sword_angle)
    off = (HAND_Y - grip_y) * SWORD_S * SWORD_DRAW
    cx, cy = moved(SWORD_FIST, f)
    sx, sy = moved(SHIELD_AT, f)
    return ({"x": round(cx + math.sin(th) * off, 3), "y": round(cy - math.cos(th) * off, 3), "angle": sword_angle, "s": SWORD_S, "sx": 1,
             "behind": False, "hidden": False, "fallback": False},
            {"x": round(sx, 3), "y": round(sy, 3), "angle": SHIELD_ANGLE, "s": SHIELD_S, "sx": 1,
             "behind": SHIELD_BEHIND, "hidden": False, "fallback": False})


def replace_row0(path, cells):
    im = np.array(Image.open(path).convert("RGBA"))
    for f, cell in enumerate(cells):
        im[0:FRAME, f * FRAME:(f + 1) * FRAME] = cell
    Image.fromarray(im, "RGBA").save(path)


def write(bare_path=f"{BODY}/bare/idle.png", hand_path=f"{BODY}/hand/idle.png", armed_path=f"{BODY}/armed/idle.png", anchors_path=ANCHORS):
    base = pose()
    bare = [unpremultiply(breathe(base, f)) for f in range(4)]
    replace_row0(bare_path, bare)
    replace_row0(armed_path, bare)
    hands = []
    for f, cell in enumerate(bare):
        h = cell.astype(np.float64)
        masks = [fist_mask(moved(SWORD_FIST, f)), fist_mask(moved(SHIELD_FIST, f)), arm_mask(SWORD_ARM, f), arm_mask(SHIELD_ARM, f)]
        h[..., 3] *= np.maximum.reduce(masks)
        hands.append(h.round().astype(np.uint8))
    replace_row0(hand_path, hands)
    raw = open(anchors_path, "rb").read()
    data = json.loads(raw)
    grip_y = json.load(open(f"{RES}/Data/classes/warrior/held-gear.json"))["sword"][0]["baseGrip"]["gripY"]
    for a in data["anchors"]:
        if a["anim"] == "idle" and a["dir"] == "down":
            i = 0 if a["part"] == "sword" else 1
            a["frames"] = [anchors_for(f, grip_y)[i] for f in range(4)]
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(anchors_path, "wb").write(text.encode())


def load_compose():
    import importlib.util
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    return comp


def preview(out, pad=50):
    """Composites copies the way the game draws: pose.png is the bare frame at 5x, strip.png the frames with gear, close.png both fists at 6x."""
    comp = load_compose()
    os.makedirs(out, exist_ok=True)
    bare, hand, armed, anch = f"{out}/bare-idle.png", f"{out}/hand-idle.png", f"{out}/armed-idle.png", f"{out}/anchors.json"
    shutil.copy(f"{BODY}/bare/idle.png", bare)
    shutil.copy(f"{BODY}/hand/idle.png", hand)
    shutil.copy(f"{BODY}/armed/idle.png", armed)
    shutil.copy(ANCHORS, anch)
    write(bare, hand, armed, anch)
    _, anchors = comp.load_gear("warrior")
    for a in json.load(open(anch))["anchors"]:
        anchors[(a["part"], a["anim"], a["dir"])] = a["frames"]
    size = FRAME + 2 * pad
    strip = Image.new("RGBA", (size * 4, size), (58, 62, 70, 255))
    for f in range(4):
        strip.alpha_composite(comp.compose("warrior", "idle", "down", f, bare, anchors, hand, pad=pad), (f * size, 0))
    strip.convert("RGB").save(f"{out}/strip.png")
    flat = Image.new("RGBA", (FRAME, FRAME), (58, 62, 70, 255))
    flat.alpha_composite(Image.open(bare).convert("RGBA").crop((0, 0, FRAME, FRAME)))
    flat.resize((FRAME * 5, FRAME * 5), Image.NEAREST).convert("RGB").save(f"{out}/pose.png")
    x0, x1 = int(SWORD_FIST[0]) + pad - 30, int(SHIELD_FIST[0]) + pad + 40
    y0 = int(SWORD_FIST[1]) + pad - 45
    strip.crop((x0, y0, x1, y0 + 90)).convert("RGB").resize(((x1 - x0) * 6, 540), Image.NEAREST).save(f"{out}/close.png")


if __name__ == "__main__":
    if sys.argv[1] == "preview":
        preview(sys.argv[2])
        print("preview written")
    else:
        raise SystemExit("superseded: row 0 of idle now comes from Tools/build-warrior-idle-down-doll.py")

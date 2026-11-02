"""Warrior idle facing down as a paper doll: the approved pose (custom/idle-down-set/warrior-long-sword-src.png) painted again without gear, so sword and shield follow the equipment at this pose too.
Source: Assets/ArtLibrary/classes/warrior/new-style/idle-down/warrior-idle-down-bare-1.png, flat #00FF00. It keeps the source's framing, so SCALE and FEET are those of the approved art.
Writes row 0 of body/bare/idle.png and body/armed/idle.png (the bag portrait reads the armed row, which has no gear in it), the sword fist in row 0 of body/hand/idle.png,
and the idle/down anchors in held-gear-melee.json. Rows 1-3 stay. Old rows are kept once in new-style/idle-down/old-<layer>-idle-row0.png.

This row used to come from Tools/build-idle-down-set.py and Tools/build-warrior-idle-down.py: neither writes it any more.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-warrior-idle-down-doll.py preview OUT_DIR   works on copies, Resources untouched
  python Tools/build-warrior-idle-down-doll.py write"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image

spec = importlib.util.spec_from_file_location("warrior_move", "Tools/build-warrior-move.py")
mv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mv)

SRC = "Assets/ArtLibrary/classes/warrior/new-style/idle-down"
FRAME, HEIGHT, SOLE_ROW, HEAD_X = mv.FRAME, mv.HEIGHT, mv.SOLE_ROW, mv.HEAD_X
SWORD_S, SHIELD_S = float(os.environ.get("DOLL_SWORD_S", mv.SWORD_S)), mv.SHIELD_S  # DOLL_SWORD_S only to preview another sword size: the game keeps one size


def _env(name, default, parse=lambda v: v):
    """Another pose can be built without editing this file: DOLL_PAINTING, DOLL_SWORD_FIST=x,y, DOLL_SHIELD_FIST=x,y, DOLL_SWORD_ANGLE, DOLL_SHIELD_ANGLE, DOLL_SWORD_BEHIND=1."""
    v = os.environ.get(name)
    return default if v is None else parse(v)


def _pair(v):
    a, b = v.split(",")
    return float(a), float(b)


PAINTING = _env("DOLL_PAINTING", f"{SRC}/warrior-idle-down-bare-1.png")
SWORD_ANGLE, SHIELD_ANGLE = _env("DOLL_SWORD_ANGLE", -135, float), _env("DOLL_SHIELD_ANGLE", 4, float)  # walk down is -135, so the sword does not jump; the approved shield leans 4
SWORD_BEHIND = _env("DOLL_SWORD_BEHIND", False, lambda v: v == "1")
# Fist centres in the upright pose, frame px, read from the `preview` grid. The shield strap sits on the forearm, as on the other rows.
SWORD_FIST = _env("DOLL_SWORD_FIST", (55.5, 133.0), _pair)
SHIELD_FIST = _env("DOLL_SHIELD_FIST", (129.5, 129.0), _pair)
HEAD_GROW = _env("DOLL_HEAD_GROW", None, _pair)


def pose():
    """One premultiplied 192 cell: tallest art HEIGHT, soles on SOLE_ROW, centred between the boots as the approved art."""
    pm = mv.key_green(np.array(Image.open(PAINTING).convert("RGB")))
    art = mv.trimmed(pm)
    scale = HEIGHT / art.shape[0]
    scaled = mv.resize_pm(art, scale)
    ys, xs = np.where(pm[..., 3] > 128)
    feet = xs[ys > ys.max() - 40]
    x = round(FRAME / 2 - ((feet.min() + feet.max()) / 2 - xs.min()) * scale)
    y = SOLE_ROW + 1 - scaled.shape[0]
    if x < 0 or x + scaled.shape[1] > FRAME or y < 0:
        raise SystemExit(f"art leaves the cell: x {x}..{x + scaled.shape[1]} y {y}")
    cell = np.zeros((FRAME, FRAME, 4))
    cell[y:y + scaled.shape[0], x:x + scaled.shape[1]] = scaled
    print(f"scale {scale:.4f} x {x}..{x + scaled.shape[1]} y {y}..{y + scaled.shape[0] - 1}")
    if HEAD_GROW:  # DOLL_HEAD_GROW=k,neckRow: the head alone k times bigger, when a repaint came out with a smaller head than the rows in the game
        cell = mv.grow_head(cell, HEAD_GROW[0], int(HEAD_GROW[1]))
    return cell


def lifted(point, f):
    return point[0], point[1] + mv.LIFT[f] * float(mv.lift_at(np.array(point[1])))


def anchors():
    sword, shield = [], []
    for f in range(4):
        x, y = lifted(SWORD_FIST, f)
        sword.append({"x": round(x, 3), "y": round(y, 3), "angle": SWORD_ANGLE, "s": SWORD_S, "sx": 1, "behind": SWORD_BEHIND, "hidden": False, "fallback": False})
        x, y = lifted(SHIELD_FIST, f)
        shield.append({"x": round(x, 3), "y": round(y - mv.SHIELD_ABOVE_FIST, 3), "angle": SHIELD_ANGLE, "s": SHIELD_S, "sx": 1, "behind": False, "hidden": False, "fallback": False})
    return sword, shield


def write_sheets(bare_dir, armed_dir, hand_dir, cell):
    for layer, folder in (("bare", bare_dir), ("armed", armed_dir), ("hand", hand_dir)):
        os.makedirs(folder, exist_ok=True)
        sheet = Image.open(f"{mv.BODY}/{layer}/idle.png").convert("RGBA")
        backup = f"{SRC}/old-{layer}-idle-row0.png"
        if not os.path.exists(backup):
            sheet.crop((0, 0, sheet.width, FRAME)).save(backup)
        px = np.array(sheet)
        for f in range(4):
            frame = mv.breathe(cell, f)
            if layer == "hand":
                frame = np.zeros_like(frame) if SWORD_BEHIND else frame * mv.fist_mask(*lifted(SWORD_FIST, f))[..., None]
            px[0:FRAME, f * FRAME:(f + 1) * FRAME] = mv.to_rgba(frame)
        Image.fromarray(px, "RGBA").save(f"{folder}/idle.png")


def write_anchors(path):
    """Read, change, write straight away: other tools write this file too."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    sword, shield = anchors()
    for a in data["anchors"]:
        if a["anim"] == "idle" and a["dir"] == "down":
            a["frames"] = sword if a["part"] == "sword" else shield
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def preview(out, zooms=(2, 4), pad=50):
    comp = mv.load_compose()
    bare, armed, hand = f"{out}/bare", f"{out}/armed", f"{out}/hand"
    write_sheets(bare, armed, hand, pose())
    _, table = comp.load_gear("warrior")
    table[("sword", "idle", "down")], table[("shield", "idle", "down")] = anchors()
    size = FRAME + 2 * pad
    file, _ = comp.load_gear("warrior")
    tiers = range(len(file["sword"])) if os.environ.get("ALL_TIERS") else [0]
    for bg in ((58, 62, 70), (205, 205, 195)):
        strip = Image.new("RGBA", (size * 4, size), bg + (255,))
        for f in range(4):
            strip.alpha_composite(comp.compose("warrior", "idle", "down", f, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad), (f * size, 0))
        for z in zooms:
            strip.convert("RGB").resize((strip.width * z // 2, strip.height * z // 2), Image.NEAREST).save(f"{out}/idle-down-{bg[0]}-z{z}.png")
    # beside the walk down that follows it, and a fist close-up
    side = Image.new("RGBA", (size * 2, size), (58, 62, 70, 255))
    side.alpha_composite(comp.compose("warrior", "idle", "down", 0, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad), (0, 0))
    side.alpha_composite(comp.compose("warrior", "walk", "down", 0, pad=pad), (size, 0))
    side.convert("RGB").resize((size * 4, size * 2), Image.NEAREST).save(f"{out}/idle-vs-walk-down.png")
    c = comp.compose("warrior", "idle", "down", 0, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad)
    bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
    bg.alpha_composite(c)
    fx, fy = SWORD_FIST
    bg.crop((int(fx + pad - 30), int(fy + pad - 30), int(fx + pad + 30), int(fy + pad + 30))).resize((480, 480), Image.NEAREST).convert("RGB").save(f"{out}/idle-down-fist-z8.png")
    frames = []
    for f in range(4):
        c = comp.compose("warrior", "idle", "down", f, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad)
        bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
        bg.alpha_composite(c)
        frames.append(bg.resize((size * 2, size * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
    frames[0].save(f"{out}/idle-down.gif", save_all=True, append_images=frames[1:], duration=240, loop=0, disposal=2)


if __name__ == "__main__":
    if sys.argv[1] == "preview":
        os.makedirs(sys.argv[2], exist_ok=True)
        preview(sys.argv[2])
    else:
        write_sheets(f"{mv.BODY}/bare", f"{mv.BODY}/armed", f"{mv.BODY}/hand", pose())
        write_anchors(mv.ANCHORS)
        print("row 0 and anchors written")

"""Warrior idle, all 4 directions, in the centred design: sword held straight up and down beside the leg, body on the centre line
(design chat https://chatgpt.com/c/6ae8644a-23f8-83ec-a15f-4e32aa0e5b50, account Tay, 2026-11-02/03).
Sources, flat #00FF00, in Assets/ArtLibrary/classes/warrior/new-style/centered/:
  v2-idle-down-bare.png, v2-idle-up-bare.png   one figure, empty hands, proportions of the old idle (first set idle-*-bare.png was too thin)
  v2-idle-sides-bare.png                       two figures: facing left, then facing right
  design-down.png, design-sides.png        the approved paintings WITH sword and shield: gear positions are read from these
Writes rows 0-3 of body/bare/idle.png, body/armed/idle.png (armed = bare: the bag portrait reads armed row 0, the game draws bare + gear),
the sword fist in body/hand/idle.png where the sword is in front, and the idle anchors in held-gear-melee.json. Old rows are backed up once in centered/old/.
Every row is one painted pose with the breathing lift of build-warrior-move.py, 164 px tall, soles on row 186, hair centred on column 96.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-warrior-centered-idle.py grid OUT_DIR      4x grids of the bare cell and of the design mapped into the same cell, to read GEAR
  python Tools/build-warrior-centered-idle.py preview OUT_DIR   sheets and anchors as copies, composited the way the game draws, plus a GIF
  python Tools/build-warrior-centered-idle.py write"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

spec = importlib.util.spec_from_file_location("warrior_move", "Tools/build-warrior-move.py")
mv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mv)

SRC = "Assets/ArtLibrary/classes/warrior/new-style/centered"
FRAME, HEIGHT, SOLE_ROW, HEAD_X = mv.FRAME, mv.HEIGHT, mv.SOLE_ROW, mv.HEAD_X
SWORD_S, SHIELD_S = float(os.environ.get("SWORD_S", mv.SWORD_S)), mv.SHIELD_S  # SWORD_S env: preview another sword size
# Painted files: (file, figure index among the figures in that file, left to right). The design files hold the same figures with gear.
# v2 (2026-11-03): repainted with the stocky proportions of the old idle (big head, short legs); the first thin set stays as idle-*-bare.png, rejected by the user.
BARE = {"down": ("v2-idle-down-bare.png", 0), "up": ("v2-idle-up-bare.png", 0), "left": ("v2-idle-sides-bare.png", 0), "right": ("v2-idle-sides-bare.png", 1)}
DESIGN = {"down": ("design-down.png", 0), "left": ("design-sides.png", 0), "right": ("design-sides.png", 1)}
# Gear per direction, cell px, read from the `grid` images: sword = middle of the handle (the fist), shield = centre of the shield face.
# Sword tip down = angle 180. up has no design painting: its fists are read from the bare grid, the sword at the right fist (his right hand).
GEAR = {
    "down": {"sword": (60.0, 103.0), "sword_angle": 180, "sword_behind": False, "shield": (131.0, 111.0), "shield_angle": 6, "shield_behind": False},
    "up": {"sword": (137.0, 82.0), "sword_angle": 180, "sword_behind": False, "shield": (56.0, 110.0), "shield_angle": 0, "shield_behind": True},
    "left": {"sword": (89.0, 121.0), "sword_angle": 180, "sword_behind": True, "shield": (74.0, 117.0), "shield_angle": -8, "shield_behind": False},
    "right": {"sword": (124.0, 89.0), "sword_angle": 180, "sword_behind": False, "shield": (70.0, 112.0), "shield_angle": 8, "shield_behind": True},
}


def figures(name, count):
    """Premultiplied trimmed figures of one painting, left to right."""
    pm = mv.key_green(np.array(Image.open(f"{SRC}/{name}").convert("RGB")))
    return [mv.trimmed(pm[:, a:b]) for a, b in mv.split(pm[..., 3], count)]


def cells():
    """{dir: (cell of the bare pose, placement)}; placement = (scale, x, y) of the trimmed art in the cell, for mapping the design."""
    target = mv.hair_tint(np.array(Image.open(f"{mv.BODY}/armed/idle-down-big.png").convert("RGBA").crop((0, 0, 288, 240)), dtype=np.float64))
    out = {}
    for direction, (name, idx) in BARE.items():
        count = 2 if "sides" in name else 1
        art = mv.match_red(figures(name, count)[idx], target)
        scale = HEIGHT / art.shape[0]
        scaled = mv.resize_pm(art, scale)
        x0, x1 = mv.hair_box(scaled)
        x = HEAD_X - (x0 + x1) // 2
        y = SOLE_ROW + 1 - scaled.shape[0]
        if x < 0 or x + scaled.shape[1] > FRAME or y < 0:
            raise SystemExit(f"{direction}: art leaves the cell, x {x}..{x + scaled.shape[1]} y {y}")
        cell = np.zeros((FRAME, FRAME, 4))
        cell[y:y + scaled.shape[0], x:x + scaled.shape[1]] = scaled
        print(f"{direction:5s} scale {scale:.4f} x {x}..{x + scaled.shape[1]} y {y}..{y + scaled.shape[0] - 1} hair {x0 + x}..{x1 + x}")
        out[direction] = (cell, (scale, x, y, x0, x1))
    return out


def hair_rows(pm):
    """y0, y1 of the hair: strong red in the top 40% of the figure."""
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    r, g, b = [pm[..., i] / a[..., 0] for i in range(3)]
    mask = (r > 150) & (g < 90) & (b < 90) & (pm[..., 3] > 128)
    rows = np.where(mask.any(1))[0]
    top = rows[0]
    mask[int(top + (rows[-1] - top) * 0.4):] = False
    rows = np.where(mask.sum(1) >= 2)[0]
    return int(rows[0]), int(rows[-1]) + 1


def design_cell(direction, bare_place):
    """The approved painting with gear laid into the cell so its hair lands on the bare row's hair: same scale by hair height, same hair box."""
    name, idx = DESIGN[direction]
    art = figures(name, 2 if "sides" in name else 1)[idx]
    bare = cells_cache[direction][0]
    k = (hair_rows(bare)[1] - hair_rows(bare)[0]) / (hair_rows(art)[1] - hair_rows(art)[0])  # hair height: its width is unreliable next to the red cape
    scaled = mv.resize_pm(art, k)
    dx0, dx1 = mv.hair_box(scaled)
    cx = round(HEAD_X - (dx0 + dx1) / 2)
    cy = hair_rows(bare)[0] - hair_rows(scaled)[0]
    big = np.zeros((FRAME * 2, FRAME * 2, 4))
    ox, oy = FRAME // 2 + cx, FRAME // 2 + cy
    big[oy:oy + scaled.shape[0], ox:ox + scaled.shape[1]] = scaled
    return big[FRAME // 2:FRAME // 2 + FRAME, FRAME // 2:FRAME // 2 + FRAME]


def grid_image(cell, marks=()):
    z = 4
    im = Image.fromarray(mv.to_rgba(cell), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
    w = Image.new("RGB", im.size, (58, 62, 70))
    w.paste(im, (0, 0), im)
    d = ImageDraw.Draw(w)
    for v in range(0, FRAME, 8):
        col = (255, 255, 0) if v % 32 == 0 else (110, 110, 110)
        d.line([(v * z, 0), (v * z, FRAME * z)], fill=col)
        d.line([(0, v * z), (FRAME * z, v * z)], fill=col)
        if v % 32 == 0:
            d.text((v * z + 2, 2), str(v), fill=(255, 255, 0))
            d.text((2, v * z + 2), str(v), fill=(255, 255, 0))
    for (px, py), col in marks:
        d.ellipse([px * z - 6, py * z - 6, px * z + 6, py * z + 6], outline=col, width=2)
    return w


def anchors_for(direction):
    g = GEAR[direction]
    sword, shield = [], []
    for f in range(4):
        sx, sy = g["sword"]
        sy = sy + mv.LIFT[f] * float(mv.lift_at(np.array(sy)))
        sword.append({"x": round(sx, 3), "y": round(sy, 3), "angle": g["sword_angle"], "s": SWORD_S, "sx": 1, "behind": g["sword_behind"], "hidden": False, "fallback": False})
        hx, hy = g["shield"]
        hy = hy + mv.LIFT[f] * float(mv.lift_at(np.array(hy)))
        shield.append({"x": round(hx, 3), "y": round(hy, 3), "angle": g["shield_angle"], "s": SHIELD_S, "sx": 1, "behind": g["shield_behind"], "hidden": False, "fallback": False})
    return sword, shield


def write_sheets(bare_dir, armed_dir, hand_dir, data):
    for layer, folder in (("bare", bare_dir), ("armed", armed_dir), ("hand", hand_dir)):
        os.makedirs(folder, exist_ok=True)
        sheet = Image.open(f"{mv.BODY}/{layer}/idle.png").convert("RGBA")
        backup = f"{SRC}/old/{layer}-idle.png"
        if folder.startswith(mv.BODY) and not os.path.exists(backup):
            os.makedirs(f"{SRC}/old", exist_ok=True)
            sheet.save(backup)
        px = np.array(sheet)
        for direction, (cell, _) in data.items():
            row = mv.ROWS[direction]
            for f in range(4):
                frame = mv.breathe(cell, f)
                if layer == "hand":
                    g = GEAR[direction]
                    if g["sword_behind"]:
                        frame = np.zeros_like(frame)
                    else:
                        sx, sy = g["sword"]
                        frame = frame * mv.fist_mask(sx, sy + mv.LIFT[f] * float(mv.lift_at(np.array(sy))))[..., None]
                px[row * FRAME:(row + 1) * FRAME, f * FRAME:(f + 1) * FRAME] = mv.to_rgba(frame)
        Image.fromarray(px, "RGBA").save(f"{folder}/idle.png")


def write_anchors(path):
    """Read, change, write straight away: other tools write this file too. Only the idle rows."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    for a in data["anchors"]:
        if a["anim"] == "idle" and a["dir"] in GEAR:
            sword, shield = anchors_for(a["dir"])
            a["frames"] = sword if a["part"] == "sword" else shield
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def preview(out, pad=50):
    comp = mv.load_compose()
    data = cells_cache
    bare, armed, hand = f"{out}/bare", f"{out}/armed", f"{out}/hand"
    write_sheets(bare, armed, hand, data)
    _, table = comp.load_gear("warrior")
    for d in GEAR:
        table[("sword", "idle", d)], table[("shield", "idle", d)] = anchors_for(d)
    size = FRAME + 2 * pad
    for bg in ((58, 62, 70), (205, 205, 195)):
        sheet = Image.new("RGBA", (size * 4, size * 4), bg + (255,))
        for r, d in enumerate(("down", "up", "left", "right")):
            for f in range(4):
                sheet.alpha_composite(comp.compose("warrior", "idle", d, f, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad), (f * size, r * size))
        sheet.convert("RGB").resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{out}/idle-{bg[0]}-z2.png")
    frames = []
    for f in range(4):
        strip = Image.new("RGBA", (size * 4, size), (58, 62, 70, 255))
        for c, d in enumerate(("down", "up", "left", "right")):
            strip.alpha_composite(comp.compose("warrior", "idle", d, f, f"{bare}/idle.png", table, f"{hand}/idle.png", pad=pad), (c * size, 0))
        frames.append(strip.resize((strip.width * 2, strip.height * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
    frames[0].save(f"{out}/idle.gif", save_all=True, append_images=frames[1:], duration=240, loop=0, disposal=2)


if __name__ == "__main__":
    cells_cache = cells()
    cmd = sys.argv[1]
    if cmd == "grid":
        os.makedirs(sys.argv[2], exist_ok=True)
        for d, (cell, place) in cells_cache.items():
            g = GEAR[d]
            marks = [(g["sword"], (0, 255, 255)), (g["shield"], (255, 0, 255))]
            grid_image(cell, marks).save(f"{sys.argv[2]}/grid-bare-{d}.png")
            if d in DESIGN:
                grid_image(design_cell(d, place), marks).save(f"{sys.argv[2]}/grid-design-{d}.png")
    elif cmd == "preview":
        os.makedirs(sys.argv[2], exist_ok=True)
        preview(sys.argv[2])
    elif cmd == "write":
        write_sheets(f"{mv.BODY}/bare", f"{mv.BODY}/armed", f"{mv.BODY}/hand", cells_cache)
        write_anchors(mv.ANCHORS)
        print("idle rows 0-3 and anchors written")

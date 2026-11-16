"""Warrior walk facing DOWN with 8 painted frames instead of 4 (trial, user 2026-11-16: "di chuyển không mượt").
Source: one ChatGPT image, 2 rows x 4 frames on flat #00FF00, bare hands: Assets/ArtLibrary/classes/warrior/new-style/walk-8/src/.
A sheet has one frame count for all its rows (SpriteSheet.Grid), so every walk sheet becomes 8 columns: row 0 (down) is the new art,
rows 1-3 (up, left, right) keep their 4 frames, each shown twice, and so do the walk rows of the armour outfits that have a walk sheet.
The cycle keeps its length (HeroClassDef.WalkCycleMs), so the other rows look exactly as before.

Frames are scaled together (164 px head to sole at the tallest), heads line up on column 96 and on one hair-top row plus the step's own
bob (BOB): a foot swung toward the camera hangs lower than the planted one, so soles cannot be the reference as in build-warrior-move.py.

Usage (project root; Pillow, numpy, scipy):
  python Tools/build-warrior-walk-down-8-frames.py cut OUT_DIR      frames with a pixel grid (fist positions), measurements; Resources untouched
  python Tools/build-warrior-walk-down-8-frames.py preview OUT_DIR  the 8 frames composited the way the game draws + a GIF; Resources untouched
  python Tools/build-warrior-walk-down-8-frames.py write            bare / armed / hand walk sheets (8 columns) + sword and shield anchors
  python Tools/build-warrior-walk-down-8-frames.py head             after write: headless walk row 0 + head anchors of the 8 frames (hairstyles)"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage

spec = importlib.util.spec_from_file_location("warrior_move", "Tools/build-warrior-move.py")
mv = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mv)

FRAME, N = mv.FRAME, 8
SRC = "Assets/ArtLibrary/classes/warrior/new-style/walk-8/src/warrior-walk-down-8-frames-chatgpt-1.png"
HAIR_TOP = 23  # hair-top row of idle facing down (164 px figure, soles on row 186)
BOB = [0, 1, 0, -1, 0, 1, 0, -1]  # frame px down: contact, down, passing, up, twice
HEAD_GROW = None  # (factor, neck row) when the painted head is smaller than idle; set after `cut`
# Fist centres per frame (cell px), read from the `cut` grid: (sword fist = screen left, shield fist = screen right).
FISTS = [((70, 127), (124, 126)), ((70, 128.5), (125, 127.5)), ((70, 127.5), (125, 126.5)), ((69, 126), (126.5, 125)),
         ((70, 126.5), (124, 126.5)), ((70, 128), (125, 128)), ((70, 127.5), (125, 126.5)), ((69, 125.5), (126.5, 125.5))]
SWORD_ANGLE = [-139, -137, -135, -133, -131, -133, -135, -137]  # the old 4-frame swing, with the in-betweens
SHIELD_ANGLE = [2, 1, 0, -1, -2, -1, 0, 1]


def painted():
    """The 8 frames, trimmed, premultiplied, reading order."""
    pm = mv.key_green(np.array(Image.open(SRC).convert("RGB")))
    rows = (pm[..., 3] > 128).sum(1) > 0
    labels, n = ndimage.label(rows)
    runs = sorted((np.where(labels == i)[0][[0, -1]] for i in range(1, n + 1)), key=lambda r: r[0] - r[1])[:2]
    out = []
    for a, b in sorted((int(a), int(b) + 1) for a, b in runs):
        band = pm[a:b]
        out += [mv.trimmed(band[:, x0:x1]) for x0, x1 in mv.split(band[..., 3], 4)]
    assert len(out) == N, len(out)
    return out


def cells():
    """[8 premultiplied 192 cells] and what was measured."""
    target = mv.hair_tint(np.array(Image.open(f"{mv.BODY}/armed/idle-down-big.png").convert("RGBA").crop((0, 0, 288, 240)), dtype=np.float64))
    raw = [mv.match_red(f, target) for f in painted()]
    scale = mv.HEIGHT / max(f.shape[0] for f in raw)
    frames = [mv.resize_pm(f, scale) for f in raw]
    x0, x1 = mv.hair_box(frames[0])
    base = mv.HEAD_X - (x0 + x1) // 2
    out, table = [], [f"scale {scale:.3f}"]
    for i, f in enumerate(frames):
        cell = np.zeros((FRAME, FRAME, 4))
        x, y = base + mv.head_offset(frames[0], f), HAIR_TOP + BOB[i]
        h, w = min(f.shape[0], FRAME - y), f.shape[1]
        cell[y:y + h, x:x + w] = f[:h]
        if HEAD_GROW:
            cell = mv.grow_head(cell, *HEAD_GROW)
        out.append(cell)
        ys = np.where((cell[..., 3] > 128).any(1))[0]
        hx = mv.hair_box(mv.trimmed(cell))
        table.append(f"frame {i}: top {ys[0]} sole {ys[-1]} hair width {hx[1] - hx[0]} painted height {f.shape[0]}")
    return out, table


def anchors(f, grip_y):
    sword_fist, shield_fist = FISTS[f]
    x, y = mv.sword_at(sword_fist, SWORD_ANGLE[f], grip_y)
    sword = {"x": x, "y": y, "angle": SWORD_ANGLE[f], "s": mv.SWORD_S, "sx": 1, "behind": False, "hidden": False, "fallback": False}
    shield = {"x": round(shield_fist[0], 3), "y": round(shield_fist[1] - mv.SHIELD_ABOVE_FIST, 3), "angle": SHIELD_ANGLE[f], "s": mv.SHIELD_S, "sx": 1,
              "behind": False, "hidden": False, "fallback": False}
    return sword, shield


def doubled(sheet):
    """A 4-column sheet as 8 columns, every frame twice. An 8-column sheet is returned as it is (the tool was run before)."""
    if sheet.shape[1] == FRAME * N:
        return sheet.copy()
    assert sheet.shape[1] == FRAME * 4, sheet.shape
    out = np.zeros((sheet.shape[0], FRAME * N, 4), np.uint8)
    for f in range(N):
        out[:, f * FRAME:(f + 1) * FRAME] = sheet[:, (f // 2) * FRAME:(f // 2 + 1) * FRAME]
    return out


def write_sheets(body, out_body):
    """Every walk sheet under body/ as 8 columns. Row 0 = the new art in bare / armed / hand, also in the armour outfits that have a walk
    sheet (set-t1, set-t2): their own facing-down row was painted on the old poses and would not fit the new fists and head, so walking
    down they show the Lv1 body, as the outfits without a walk sheet (set-t3..t9) do in every facing. headless is written by `head`."""
    row0, _ = cells()
    for root, _, files in os.walk(body):
        if "walk.png" not in files:
            continue
        rel = os.path.relpath(root, body).replace("\\", "/")
        sheet = doubled(np.array(Image.open(f"{root}/walk.png").convert("RGBA")))
        kind = rel.split("/")[-1]
        if kind in ("bare", "armed", "hand"):
            for f, cell in enumerate(row0):
                if kind == "hand":
                    cell = cell * mv.fist_mask(*FISTS[f][0])[..., None]  # only the sword fist, as in build-warrior-move.py
                sheet[0:FRAME, f * FRAME:(f + 1) * FRAME] = mv.to_rgba(cell)
        os.makedirs(f"{out_body}/{rel}", exist_ok=True)
        Image.fromarray(sheet, "RGBA").save(f"{out_body}/{rel}/walk.png")


def write_head_layer():
    """After `write`: headless row 0 (Lv1 and outfits) and the head anchors of walk-down for the 8 frames, with the head layer tool's own
    registration and cut (Tools/head_layer_dirs.py); the other facings' anchors are doubled. Head scale back to the idle size."""
    sys.path.insert(0, "Tools")
    import head_layer_core as core
    import head_layer_dirs as hd
    hd.jobs = lambda cls, anims=None: [("walk", "down")]
    _, out = hd.build("warrior")
    v = out[("walk", "down")]
    assert len(v["headless"]) == N, len(v["headless"])
    for root, _, files in os.walk(mv.BODY):
        if "walk.png" in files and os.path.basename(root) == "headless":
            path = f"{root}/walk.png"
            sheet = doubled(np.array(Image.open(path).convert("RGBA")))
            for f, c in enumerate(v["headless"]):
                sheet[0:FRAME, f * FRAME:(f + 1) * FRAME] = core.u8(c)
            Image.fromarray(sheet, "RGBA").save(path)
    path = core.paths("warrior")["anchors"]
    data = json.load(open(path, encoding="utf-8"))
    for e in data["anims"]:
        if e["anim"] != "walk":
            continue
        if e["dir"] == "down":
            e["frames"] = v["anchors"]
        elif len(e["frames"]) == 4:
            e["frames"] = [dict(e["frames"][f // 2]) for f in range(N)]
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
        fh.write(chr(10))
    hd.normalize_scale("warrior", ["walk"])


def write_anchors(path):
    """Sword and shield walk anchors: down = the 8 new frames, the other facings doubled. Read, change, write straight away."""
    raw = open(path, "rb").read()
    data = json.loads(raw)
    grip_y = json.load(open(f"{mv.RES}/Data/classes/warrior/held-gear.json", encoding="utf-8"))["sword"][0]["baseGrip"]["gripY"]
    new = [anchors(f, grip_y) for f in range(N)]
    for a in data["anchors"]:
        if a["anim"] != "walk" or a["part"] not in ("sword", "shield"):
            continue
        if a["dir"] == "down":
            a["frames"] = [p[0 if a["part"] == "sword" else 1] for p in new]
        elif len(a["frames"]) == 4:
            a["frames"] = [json.loads(json.dumps(a["frames"][f // 2])) for f in range(N)]
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


def cut(out):
    os.makedirs(out, exist_ok=True)
    row, table = cells()
    print("\n".join(table))
    z = 4
    from PIL import ImageDraw
    for half in range(2):
        w = Image.new("RGB", (FRAME * z * 4, FRAME * z), (58, 62, 70))
        d = ImageDraw.Draw(w)
        for k in range(4):
            im = Image.fromarray(mv.to_rgba(row[half * 4 + k]), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(im, (k * FRAME * z, 0), im)
            for v in range(0, FRAME, 16):
                d.line([(k * FRAME * z + v * z, 400), (k * FRAME * z + v * z, FRAME * z)], fill=(255, 255, 0) if v % 48 == 0 else (120, 120, 120))
                d.text((k * FRAME * z + v * z + 2, 402), str(v), fill=(255, 255, 0))
            for v in range(96, FRAME, 16):
                d.line([(k * FRAME * z, v * z), ((k + 1) * FRAME * z, v * z)], fill=(255, 255, 0) if v % 48 == 0 else (120, 120, 120))
                d.text((k * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        w.save(f"{out}/grid-walk-down-{half * 4}-{half * 4 + 3}.png")


def preview(out, pad=50):
    comp = mv.load_compose()
    os.makedirs(out, exist_ok=True)
    write_sheets(mv.BODY, f"{out}/body")
    anch = f"{out}/anchors.json"
    open(anch, "wb").write(open(mv.ANCHORS, "rb").read())
    write_anchors(anch)
    _, table = comp.load_gear("warrior")
    for a in json.load(open(anch, encoding="utf-8"))["anchors"]:
        table[(a["part"], a["anim"], a["dir"])] = a["frames"]
    size = FRAME + 2 * pad
    sheet = Image.new("RGBA", (size * N, size), (58, 62, 70, 255))
    gif = []
    for f in range(N):
        c = comp.compose("warrior", "walk", "down", f, f"{out}/body/bare/walk.png", table, f"{out}/body/hand/walk.png", pad=pad)
        sheet.alpha_composite(c, (f * size, 0))
        bg = Image.new("RGBA", c.size, (58, 62, 70, 255))
        bg.alpha_composite(c)
        gif.append(bg.resize((size * 2, size * 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE))
    gif[0].save(f"{out}/walk-down-8.gif", save_all=True, append_images=gif[1:], duration=70, loop=0, disposal=2)
    sheet.convert("RGB").resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(f"{out}/walk-down-8.png")


if __name__ == "__main__":
    if sys.argv[1] == "cut":
        cut(sys.argv[2])
    elif sys.argv[1] == "preview":
        preview(sys.argv[2])
        print("preview written")
    elif sys.argv[1] == "head":
        write_head_layer()
        print("headless walk row 0 and head anchors written")
    else:
        write_sheets(mv.BODY, mv.BODY)
        write_anchors(mv.ANCHORS)
        print("walk sheets (8 columns) and anchors written")

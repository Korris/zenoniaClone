"""Mechanic dual pistols: body rows with no weapons and clear fists (idle right, down, up, walk right; attack2 right still to draw). Decided 2026-11-01.
Sources: Assets/ArtLibrary/classes/mechanic/new-style/dual-src/move/raw/, ChatGPT art on flat #00FF00. Output goes to move/ only: nothing here writes to Assets/Resources, json or code; the code agent installs.

Usage (project root; Pillow, numpy, scipy):
  python Tools/build-mechanic-dual-move.py measure      height, head and colours of the new rows beside the installed rows
  python Tools/build-mechanic-dual-move.py cut          cut sheets move/<anim>-<dir>.png, 192 px cells, sole on row 188
  python Tools/build-mechanic-dual-move.py grid OUT     the same cells at 4x with a pixel grid and the FISTS table marked, for reading and checking fists
  python Tools/build-mechanic-dual-move.py fists        write move/fists.txt from FISTS
  python Tools/build-mechanic-dual-move.py contact OUT  new rows beside neighbouring rows, 2x and 1x
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image, ImageDraw

spec = importlib.util.spec_from_file_location("bm", "Tools/build-mechanic-new-style.py")
bm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bm)
wm = bm.wm

FRAME, HEIGHT, SOLE, HEAD_X = 192, 164, 188, 96
MOVE = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src/move"
RAW = f"{MOVE}/raw"
BARE = "Assets/Resources/Sprites/classes/mechanic/body/bare"
# (anim, dir): (raw file, painted frames, frames in the cut sheet)
ROWS = {("idle", "right"): ("mech-move-idle-right-1", 1, 4), ("walk", "right"): ("mech-move-walk-right-1", 4, 4), ("idle", "down"): ("mech-move-idle-down-1", 1, 4), ("idle", "up"): ("mech-move-idle-up-1", 1, 4)}
# Head grown after cutting, as HEAD_GROW in build-warrior-move.py: (k, neck row). A row scaled to 164 px can come out with a smaller head than idle down.
HEAD_GROW = {}
# Column of the middle of the head in the cell, as the installed row of the same anim, so nothing jumps when the rows change.
HEAD_COL = {("idle", "right"): 96, ("walk", "right"): 96, ("idle", "down"): 96, ("idle", "up"): 96}
# Colours of the installed idle right row, the anchor: hair and skin are pulled to these medians.
HAIR_RGB, SKIN_RGB = (253, 205, 95), (254, 219, 185)
# (near fist, far fist) per frame, cell px, read from the grid. Facing right the near hand is the hero's right hand.
FISTS = {
    ("idle", "right"): [((123, 128), (150, 102))],
    ("walk", "right"): [((65, 139), (144, 112)), ((114, 134), (145, 103)), ((135, 110), (127, 136)), ((113, 135), (144, 103))],
    # Facing the camera or away there is no near and far: (screen-left fist, screen-right fist).
    ("idle", "down"): [((81, 127), (116, 107))],
    ("idle", "up"): [((57, 114), (132, 92))],
}


def load_row(key):
    name, count = ROWS[key][:2]
    pm = wm.key_green(np.array(Image.open(f"{RAW}/{name}.png").convert("RGB")))
    pm[:8], pm[-8:], pm[:, :8], pm[:, -8:] = 0, 0, 0, 0
    return [wm.trimmed(pm[:, a:b]) for a, b in wm.split(pm[..., 3], count)]


def masks(cell):
    a = np.maximum(cell[..., 3:4], 1e-3) / 255
    rgb = cell[..., :3] / a
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hair = bm.hair_mask(cell)
    skin = (cell[..., 3] > 200) & (r > 225) & (g > 170) & (g < 228) & (b > 140) & (b < 208) & (r - b > 35) & ~hair
    return rgb, hair, skin


def medians(cells):
    hs, ss = [], []
    for c in cells:
        rgb, hair, skin = masks(c)
        hs.append(rgb[hair])
        ss.append(rgb[skin])
    hs, ss = np.concatenate(hs), np.concatenate(ss)
    return [int(v) for v in np.median(hs, 0)], ([int(v) for v in np.median(ss, 0)] if len(ss) > 5 else None)


def match_colours(cells):
    """Same idea as match_red in build-warrior-move.py: one gain per channel on hair pixels and one on skin pixels, measured over the whole row."""
    h, s = medians(cells)
    out = []
    for c in cells:
        _, hair, skin = masks(c)
        c = c.copy()
        for mask, now, target in ((hair, h, HAIR_RGB), (skin, s, SKIN_RGB)):
            if now is None:
                continue
            gain = np.clip(np.array(target, float) / np.maximum(now, 1), 0.85, 1.2)
            for i in range(3):
                c[..., i] = np.where(mask, np.minimum(c[..., i] * gain[i], c[..., 3]), c[..., i])
        out.append(c)
    return out


def head_box(pm):
    """Left, right, top of the hair above the chin. bm.hair_center also counts gold on belts and bracers, which moved the head 7 px between walk frames."""
    top = np.where((pm[..., 3] > 128).any(1))[0][0]
    m = bm.hair_mask(pm)[top:top + 60]
    xs = np.where(m.sum(0) >= 2)[0]
    return int(xs[0]), int(xs[-1]) + 1, int(top)


def head_x(pm):
    a, b, _ = head_box(pm)
    return (a + b) // 2


def row_cells(key):
    raw = load_row(key)
    k = HEIGHT / max(f.shape[0] for f in raw)
    frames = [wm.resize_pm(f, k) for f in raw]
    cells = []
    for f in frames:
        cell, clipped = bm.place(f, HEAD_COL[key] - head_x(f))
        if clipped:
            print("CLIPPED", key)
        if key in HEAD_GROW:
            cell = wm.grow_head(cell, *HEAD_GROW[key])
        cells.append(cell)
    cells = match_colours(cells)
    if key[0] == "idle":
        cells = [wm.breathe(cells[0], f) for f in range(ROWS[key][2])]
    return cells, k


def sheet_cells(path, row, count):
    s = np.array(Image.open(path).convert("RGBA")).astype(np.float64)
    s[..., :3] *= s[..., 3:4] / 255
    return [s[row * FRAME:(row + 1) * FRAME, f * FRAME:(f + 1) * FRAME] for f in range(count)]


def row_img(cells, z=1, bg=(58, 62, 70), cols=None):
    cols = cols or len(cells)
    w = Image.new("RGB", (FRAME * z * cols, FRAME * z), bg)
    for f, c in enumerate(cells):
        im = Image.fromarray(wm.to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
        w.paste(im, (f * FRAME * z, 0), im)
    return w


def measure():
    print("target hair size", round(bm.hair_target(), 1))
    for name, cells in (("installed idle right", sheet_cells(f"{BARE}/idle.png", 3, 1)), ("installed walk right", sheet_cells(f"{BARE}/walk.png", 3, 4)), ("installed attack2 right", sheet_cells(f"{BARE}/attack2.png", 3, 4))):
        print(f"{name:24s} head {[head_box(c) for c in cells]} colours {medians(cells)}")
    for key in ROWS:
        cells, k = row_cells(key)
        tops = [int(np.where((c[..., 3] > 128).any(1))[0][0]) for c in cells]
        soles = [int(np.where((c[..., 3] > 128).any(1))[0][-1]) for c in cells]
        print(f"{key[0]:5s}{key[1]:6s} k {k:.3f} tops {tops} soles {soles} head {[head_box(c) for c in cells]} colours {medians(cells)}")


def cut():
    for key in ROWS:
        cells, _ = row_cells(key)
        w = Image.new("RGBA", (FRAME * len(cells), FRAME), (0, 0, 0, 0))
        for f, c in enumerate(cells):
            w.paste(Image.fromarray(wm.to_rgba(c), "RGBA"), (f * FRAME, 0))
        w.save(f"{MOVE}/{key[0]}-{key[1]}.png")
        print("wrote", key, w.size)


def grid(out):
    os.makedirs(out, exist_ok=True)
    z = 4
    for key in ROWS:
        cells = row_cells(key)[0][:ROWS[key][1]]
        w = row_img(cells, z)
        d = ImageDraw.Draw(w)
        for f in range(len(cells)):
            for v in range(0, FRAME, 16):
                strong = v % 48 == 0
                col = (255, 255, 0) if strong else (120, 120, 120)
                d.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=col)
                d.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=col)
                if strong:
                    d.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
            if key in FISTS:
                for (x, y), col in zip(FISTS[key][f], ((255, 0, 255), (0, 255, 255))):
                    X, Y = f * FRAME * z + x * z, y * z
                    d.ellipse([X - 12, Y - 12, X + 12, Y + 12], outline=col, width=3)
                    d.line([X - 18, Y, X + 18, Y], fill=col)
                    d.line([X, Y - 18, X, Y + 18], fill=col)
        w.save(f"{out}/grid-{key[0]}-{key[1]}.png")


def mirrored_fists(key):
    """Left row as the mirror of a right row: x -> 192 - x. The fist near on screen is now the hero's left hand."""
    return [((FRAME - n[0], n[1]), (FRAME - fr[0], fr[1])) for n, fr in FISTS[key]]


def write_fists():
    L = ["# Mechanic dual pistols: fist centres in the 192 px cell, read from a 4x grid, about 2 px accurate.",
         "# Facing right: near = hero's right hand, far = hero's left hand. Idle: painted once, add the breathing lift wm.LIFT[f] * wm.lift_at(y) per frame, as anchors do.",
         "# Left may be the mirror of right, because both pistols are the same art: x -> 192 - x, near = hero's left hand, far = hero's right hand. Near pistol in front of the body, far pistol behind it, in both directions.",
         ""]
    for key, rows in FISTS.items():
        L.append(f"{key[0]} {key[1]}")
        L += [f"  frame {f}: near {n[0]} {n[1]}   far {fr[0]} {fr[1]}" for f, (n, fr) in enumerate(rows)]
        if key[1] == "right":
            L.append(f"{key[0]} left (mirror)")
            L += [f"  frame {f}: near {n[0]} {n[1]}   far {fr[0]} {fr[1]}" for f, (n, fr) in enumerate(mirrored_fists(key))]
        L.append("")
    L += ["attack2 right: NOT DRAWN, image limit reached.",
          "idle down, idle up: near = screen-left fist, far = screen-right fist.",
          ""]
    with open(f"{MOVE}/fists.txt", "w", newline="\n") as fh:
        fh.write("\n".join(L))


def contact(out):
    os.makedirs(out, exist_ok=True)
    z = 2
    new_idle = row_cells(("idle", "right"))[0][:1]
    new_walk = row_cells(("walk", "right"))[0]
    rows = [("idle right OLD (installed)", sheet_cells(f"{BARE}/idle.png", 3, 1)),
            ("idle right NEW", new_idle),
            ("walk right OLD (installed)", sheet_cells(f"{BARE}/walk.png", 3, 4)),
            ("walk right NEW", new_walk),
            ("walk left NEW = mirror", [c[:, ::-1].copy() for c in new_walk]),
            ("attack2 right installed (neighbour, not redrawn yet)", sheet_cells(f"{BARE}/attack2.png", 3, 4))]
    w = Image.new("RGB", (FRAME * z * 4, (FRAME * z + 22) * len(rows)), (40, 44, 52))
    d = ImageDraw.Draw(w)
    y = 0
    for name, cells in rows:
        d.text((6, y + 5), name, fill=(255, 255, 255))
        w.paste(row_img(cells, z, cols=4), (0, y + 22))
        y += FRAME * z + 22
    w.save(f"{out}/contact-new-vs-neighbours.png")
    n = new_idle + new_walk + sheet_cells(f"{BARE}/idle.png", 0, 1) + sheet_cells(f"{BARE}/attack1.png", 3, 1) + sheet_cells(f"{BARE}/attack3.png", 3, 1)
    row_img(n, 1).save(f"{out}/contact-1x.png")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "measure"
    if cmd == "measure":
        measure()
    elif cmd == "cut":
        cut()
    elif cmd == "fists":
        write_fists()
    elif cmd == "grid":
        grid(sys.argv[2])
    elif cmd == "contact":
        contact(sys.argv[2])

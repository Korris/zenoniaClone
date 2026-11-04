"""Idle facing down as a paper doll for a class whose painted row (Tools/build-idle-down-set.py) has the weapon drawn in: Wizard, Cleric.
ChatGPT redrew the painted frame 1:1 with empty fists (new-style/idle-down-doll/<class>-bare-chatgpt.png, flat #00FF00, chat
https://chatgpt.com/c/6aea0959-50c4-83ec-be08-fc7b177e7272, 2026-11-03). That figure is keyed, scaled to the painted figure's height
(head top to sole) and stood on the same sole row and boot centre, breathed into the 4 frames like the painted row (BREATH), and written as
body/bare/idle.png row 0; the fists are cut from it into body/hand/idle.png row 0; the idle/down anchors of held-<class>.json and
held-<class>-melee.json get the fist positions and lose `fallback`, so the worn weapon, shield or book draws through the anchors.
The Assassin's row keeps its painted body (Tools/build-assassin-idle-down-doll.py); the Warrior has build-warrior-idle-down-doll.py.

  python Tools/build-idle-down-doll-from-redraw.py grid <class>    OUT/<class>-grid.png: the fitted frame 0 on a 10 px grid, to read the fists
  python Tools/build-idle-down-doll-from-redraw.py check <class>   OUT/<class>-check.png: painted row, new bare row, fists; nothing written
  python Tools/build-idle-down-doll-from-redraw.py write <class>   bare row 0, hand row 0, anchors (originals kept in new-style/idle-down-doll/old/)
OUT defaults to plans/visuals/idle-down-doll. Run from the project root; Pillow, numpy, scipy.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

RES = "Assets/Resources"
FRAME = 192
BREATH = (0, 1, 2, 1)  # build-idle-down-set.py: rows the figure is shorter by, per frame, soles planted

# gear: anchor file stem. parts: fist centre (frame px, after fitting) and the anchor's angle / s / behind per held part; read off `grid`.
# extra_fists: fists that hold nothing (the wizard's book hand), cut into the hand sheet all the same. hand=False: no fist disc over that part (a shield covers the hand).
CLASSES = {
    "wizard": dict(gear="held-wizard", parts={"staff": dict(fist=(60, 114), angle=-18.0, s=0.447, behind=False)}, extra_fists=[(120, 132)]),  # staff in the left fist, the book hand is a bare fist
    # cleric: the shield hangs on the right forearm and hides that hand, so no fist disc is cut over it (hand=False)
    "cleric": dict(gear="held-cleric", parts={"staff": dict(fist=(65, 118), angle=8.0, s=0.52, behind=False), "shield": dict(fist=(128, 140), angle=5.0, s=0.27, behind=False, hand=False)}, extra_fists=[]),
}
FIST_R = 9


def paths(cls):
    body = f"{RES}/Sprites/classes/{cls}/body"
    lib = f"Assets/ArtLibrary/classes/{cls}/new-style/idle-down-doll"
    return body, lib, f"{lib}/{cls}-bare-chatgpt.png"


def key_green(path):
    """Premultiplied RGBA floats from art on flat #00FF00 (build-assassin-new-style.key_green)."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(fg, 1 + np.where(sizes >= 400)[0]), iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def to_image(pm):
    a = np.maximum(pm[..., 3:4], 1e-6)
    rgb = np.clip(pm[..., :3] / a * 255, 0, 255)
    return Image.fromarray(np.dstack([rgb, np.clip(pm[..., 3], 0, 255)]).round().astype(np.uint8), "RGBA")


def painted_metrics(body):
    """Head top, sole row and boot centre of the painted frame 0, the figure the redraw must stand like."""
    a = np.asarray(Image.open(f"{body}/armed/idle.png").convert("RGBA"))[:FRAME, :FRAME, 3] > 128
    sole = np.where(a.any(1))[0][-1]
    head = np.where(a[:, 80:112].any(1))[0][0]
    boots = np.where(a[sole - 2:sole + 1].any(0))[0]
    return head, sole, (boots[0] + boots[-1]) / 2


def fitted(cls):
    """The redrawn figure as one RGBA image standing like the painted one: (image, sole row, x of its left edge)."""
    body, lib, src = paths(cls)
    pm = key_green(src)
    a = pm[..., 3] > 128
    rows, cols = np.where(a.any(1))[0], np.where(a.any(0))[0]
    cx = pm.shape[1] // 2
    head = np.where(a[:, cx - 40:cx + 40].any(1))[0][0]
    sole = rows[-1]
    boots = np.where(a[sole - 4:sole + 1].any(0))[0]
    p_head, p_sole, p_centre = painted_metrics(body)
    k = (p_sole - p_head) / (sole - head)
    fig = to_image(pm[rows[0]:sole + 1, cols[0]:cols[-1] + 1])
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    centre = ((boots[0] + boots[-1]) / 2 - cols[0]) * k
    x0 = round(p_centre - centre)
    return fig, p_sole, x0


def frames(cls):
    fig, sole, x0 = fitted(cls)
    out = []
    for less in BREATH:
        cell = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        im = fig.resize((fig.width, fig.height - less), Image.LANCZOS) if less else fig
        cell.alpha_composite(crop_to_cell(im, x0, sole + 1 - im.height))
        out.append(cell)
    return out


def crop_to_cell(im, x, y):
    cell = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    cell.paste(im, (x, y), im)
    return cell


def fist_mask(cls, cell):
    cfg = CLASSES[cls]
    yy, xx = np.mgrid[:FRAME, :FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for cx, cy in [p["fist"] for p in cfg["parts"].values() if p.get("hand", True)] + cfg["extra_fists"]:
        m |= (xx - cx) ** 2 + (yy - cy) ** 2 <= FIST_R ** 2
    return m & (np.asarray(cell)[..., 3] > 0)


def grid(cls, out):
    cell = frames(cls)[0]
    z = 5
    im = cell.resize((FRAME * z, FRAME * z), Image.NEAREST)
    bg = Image.new("RGBA", im.size, (58, 62, 70, 255))
    bg.alpha_composite(im)
    d = ImageDraw.Draw(bg)
    for g in range(0, FRAME, 10):
        d.line((g * z, 0, g * z, FRAME * z), fill=(255, 255, 255, 60))
        d.line((0, g * z, FRAME * z, g * z), fill=(255, 255, 255, 60))
        if g % 20 == 0:
            d.text((g * z + 2, 2), str(g), fill=(255, 255, 0, 255))
            d.text((2, g * z + 2), str(g), fill=(255, 255, 0, 255))
    bg.save(f"{out}/{cls}-grid.png")
    print("grid", f"{out}/{cls}-grid.png")


def check(cls, out, write):
    body, lib, _ = paths(cls)
    cells = frames(cls)
    painted = Image.open(f"{body}/armed/idle.png").convert("RGBA")
    sheet = Image.new("RGBA", (FRAME * len(cells), FRAME * 3), (58, 62, 70, 255))
    bare = np.asarray(Image.open(f"{body}/bare/idle.png").convert("RGBA")).copy()
    hand = np.asarray(Image.open(f"{body}/hand/idle.png").convert("RGBA")).copy()
    for i, cell in enumerate(cells):
        arr = np.asarray(cell)
        fists = np.zeros_like(arr)
        fm = fist_mask(cls, cell)
        fists[fm] = arr[fm]
        bare[:FRAME, i * FRAME:(i + 1) * FRAME] = arr
        hand[:FRAME, i * FRAME:(i + 1) * FRAME] = fists
        sheet.alpha_composite(painted.crop((i * FRAME, 0, (i + 1) * FRAME, FRAME)), (i * FRAME, 0))
        sheet.alpha_composite(cell, (i * FRAME, FRAME))
        sheet.alpha_composite(Image.fromarray(fists, "RGBA"), (i * FRAME, 2 * FRAME))
    sheet.save(f"{out}/{cls}-check.png")
    print("check", f"{out}/{cls}-check.png", "(rows: painted, bare, fists)")
    if not write:
        return
    old = f"{lib}/old"
    os.makedirs(old, exist_ok=True)
    for name in ("bare", "hand"):
        backup = f"{old}/{name}-idle.png"
        if not os.path.exists(backup):
            Image.open(f"{body}/{name}/idle.png").save(backup)
    Image.fromarray(bare, "RGBA").save(f"{body}/bare/idle.png")
    Image.fromarray(hand, "RGBA").save(f"{body}/hand/idle.png")
    write_anchors(cls, len(cells))


def write_anchors(cls, n):
    """idle/down of every held part in both anchor files: the melee file wins in the game (HeldGearData)."""
    cfg = CLASSES[cls]
    for path, kw in ((f"{RES}/Data/classes/{cls}/{cfg['gear']}.json", {"separators": (",", ":")}), (f"{RES}/Data/classes/{cls}/{cfg['gear']}-melee.json", {"indent": 1})):
        if not os.path.exists(path):
            continue
        raw = open(path, encoding="utf-8").read()
        data = json.loads(raw)
        if "\n " in raw[:40] and kw.get("separators"):
            kw = {"indent": 1}  # keep the file's own layout
        rest = [e for e in data["anchors"] if not (e["anim"] == "idle" and e["dir"] == "down")]
        for part, p in cfg["parts"].items():
            x, y = p["fist"]
            fr = {"x": float(x), "y": float(y), "angle": p["angle"], "s": p["s"], "sx": 1, "behind": p["behind"], "hidden": False, "fallback": False}
            rest.append({"part": part, "anim": "idle", "dir": "down", "frames": [dict(fr) for _ in range(n)]})
        data["anchors"] = rest
        open(path, "w", encoding="utf-8").write(json.dumps(data, **kw))
        print("anchors", path)


if __name__ == "__main__":
    cmd, cls = sys.argv[1], sys.argv[2]
    out = os.environ.get("OUT", "plans/visuals/idle-down-doll")
    os.makedirs(out, exist_ok=True)
    if cmd == "grid":
        grid(cls, out)
    elif cmd in ("check", "write"):
        check(cls, out, cmd == "write")
    else:
        raise SystemExit(__doc__)

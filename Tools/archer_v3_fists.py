"""Archer V3 fists (26/11): the Ranger's fists are BARE SKIN under brown bracers, so the brown-glove detector of the RO Fantasy
pipeline (hero_layered_fists) finds nothing. Here: skin blobs below the chin, the lowest / outermost one per side = the fist, then the
character's hands by facing (Dir8 geometry, camera from the south):
  R (right hand = string / arrow hand) is screen-LEFT facing down, down-right, down-left; screen-RIGHT facing up-right, up, up-left;
  facing right it is the NEAR hand, facing left the FAR hand.  L (left hand) holds the BOW.
FIX entries (read on the marked sheets docs/reference/character-v3/archer-v3/check/fists-archer-<anim>.png, cell px) win.
Writes Assets/ArtLibrary/classes/archer/ro-fantasy-v3/sheets/archer-fists.json (same layout as <class>-fists.json of the pipeline).
"""
import json

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

T, F = True, False
# (anim, row, frame) -> {hand: [x, y, visible]}  (row = painted row: idle / walk 0-4, attack 0-7 in Dir8 order)
FIX = {
    # walk 8f (26/11), read on the 3x row crops (Tools: D:/temp/archer-v3-work/rowcrops.py): skin detection mixed up upper arms / cloak
    ("walk", 0, 0): {"R": [128, 170, T], "L": [203, 163, T]},
    ("walk", 0, 1): {"R": [133, 170, T], "L": [202, 163, T]},
    ("walk", 0, 2): {"R": [133, 155, T], "L": [203, 157, T]},
    ("walk", 0, 3): {"R": [130, 155, T], "L": [200, 157, T]},
    ("walk", 0, 4): {"R": [130, 163, T], "L": [202, 158, T]},
    ("walk", 0, 5): {"R": [117, 165, T], "L": [190, 165, T]},
    ("walk", 0, 6): {"R": [120, 157, T], "L": [190, 150, T]},
    ("walk", 0, 7): {"R": [120, 158, T], "L": [190, 150, T]},
    ("walk", 1, 0): {"R": [143, 170, T], "L": [193, 165, T]},
    ("walk", 1, 1): {"R": [140, 177, T], "L": [192, 163, T]},
    ("walk", 1, 2): {"R": [140, 160, T], "L": [193, 158, T]},
    ("walk", 1, 3): {"R": [142, 162, T], "L": [190, 157, T]},
    ("walk", 1, 4): {"R": [147, 167, T], "L": [188, 153, T]},
    ("walk", 1, 5): {"R": [147, 170, T], "L": [193, 160, T]},
    ("walk", 1, 6): {"R": [143, 157, T], "L": [193, 158, T]},
    ("walk", 1, 7): {"R": [153, 160, T], "L": [190, 153, T]},
    ("walk", 3, 0): {"R": [212, 165, T], "L": [113, 162, T]},
    ("walk", 3, 1): {"R": [207, 175, T], "L": [120, 140, T]},
    ("walk", 3, 2): {"R": [217, 157, T], "L": [108, 170, T]},
    ("walk", 3, 3): {"R": [218, 135, T], "L": [107, 150, T]},
    ("walk", 3, 4): {"R": [212, 160, T], "L": [112, 157, T]},
    ("walk", 3, 5): {"R": [213, 157, T], "L": [113, 162, T]},
    ("walk", 3, 6): {"R": [203, 157, T], "L": [113, 160, T]},
    ("walk", 3, 7): {"R": [220, 143, T], "L": [115, 170, T]},
    ("walk", 4, 0): {"R": [198, 155, T], "L": [112, 172, T]},
    ("walk", 4, 1): {"R": [205, 157, T], "L": [112, 168, T]},
    ("walk", 4, 2): {"R": [205, 133, T], "L": [113, 158, T]},
    ("walk", 4, 3): {"R": [200, 133, T], "L": [115, 155, T]},
    ("walk", 4, 4): {"R": [198, 152, T], "L": [113, 158, T]},
    ("walk", 4, 5): {"R": [207, 158, T], "L": [113, 160, T]},
    ("walk", 4, 6): {"R": [208, 167, T], "L": [117, 160, T]},
    ("walk", 4, 7): {"R": [203, 170, T], "L": [120, 153, T]},
    ("walk", 2, 0): {"R": [160, 175, T], "L": [150, 165, F]},
    ("walk", 2, 1): {"R": [167, 175, T], "L": [150, 165, F]},
    ("walk", 2, 2): {"R": [187, 143, T], "L": [150, 163, F]},
    ("walk", 2, 3): {"R": [183, 152, T], "L": [115, 160, T]},
    ("walk", 2, 4): {"R": [183, 152, T], "L": [123, 152, T]},
    ("walk", 2, 5): {"R": [167, 173, T], "L": [150, 165, F]},
    ("walk", 2, 6): {"R": [182, 165, T], "L": [150, 165, F]},
    ("walk", 2, 7): {"R": [137, 180, T], "L": [185, 152, T]},
    ("idle", 3, 0): {"L": [121, 166, T]},  # far fist of the 3/4 back view: tiny skin patch under the bracer, read on the sheet
    # attack 8 dirs x 8f (install 26/11), bow fist L read on 1.6x crops with a cell-px grid (the skin detector took bracers, the cloak or the
    # string hand). Rest frames f0 / f7 hang the left hand on its side of the body; up-right f1-f6: the bow arm reaches up-right on the far
    # side of the head (not painted) -> hidden grip ahead of the head, bow drawn behind the body
    ("attack", 1, 0): {"L": [203, 160, T]}, ("attack", 1, 6): {"L": [228, 146, T]}, ("attack", 1, 7): {"L": [200, 163, T]},
    ("attack", 2, 0): {"L": [198, 158, T]}, ("attack", 2, 2): {"L": [222, 119, T]}, ("attack", 2, 3): {"L": [233, 109, T]},
    ("attack", 2, 4): {"L": [233, 109, T]},
    ("attack", 3, 0): {"L": [108, 178, T]}, ("attack", 3, 7): {"L": [104, 178, T]},
    **{("attack", 3, f): {"L": [205, 80, F]} for f in range(1, 7)},
    ("attack", 4, 0): {"L": [98, 163, T]}, ("attack", 4, 1): {"L": [95, 92, T]}, ("attack", 4, 2): {"L": [103, 86, T]},
    ("attack", 4, 3): {"L": [78, 66, T]}, ("attack", 4, 4): {"L": [78, 65, T]}, ("attack", 4, 5): {"L": [92, 88, T]},
    ("attack", 4, 6): {"L": [92, 90, T]}, ("attack", 4, 7): {"L": [100, 160, T]},
    ("attack", 5, 1): {"L": [98, 103, T]}, ("attack", 5, 3): {"L": [100, 93, T]}, ("attack", 5, 4): {"L": [100, 95, T]},
    ("attack", 7, 0): {"L": [190, 160, T]}, ("attack", 7, 1): {"L": [160, 157, T]}, ("attack", 7, 2): {"L": [130, 148, T]},
    ("attack", 7, 5): {"L": [96, 148, T]}, ("attack", 7, 6): {"L": [130, 150, T]}, ("attack", 7, 7): {"L": [188, 160, T]},
}
# fist blob centre band, in skull widths under the skull top: upper arms ~1.5, idle fists ~1.85, knee plates / cloak specks ~2.2
BAND = {"idle": (1.55, 2.05), "walk": (1.62, 2.08), "attack": (0.85, 2.08)}
SCREEN_LEFT_R = {"idle": (0, 1), "walk": (0, 1), "attack": (0, 1, 7)}
SCREEN_RIGHT_R = {"idle": (3, 4), "walk": (3, 4), "attack": (3, 4, 5)}


def skin(a):
    r, g, b, al = [a[..., i].astype(np.float32) / 255 for i in range(4)]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    return (al > 0.5) & (r > g) & (g > b) & (r > 0.92) & (s > 0.1) & (s < 0.5) & ((r - g) < 0.28)


def detect(cell, head, anim, row):
    """{'R': [x, y, vis], 'L': [...]} from skin blobs under the chin."""
    a = np.asarray(cell)
    hx, hy, w = head["x"], head["y"], head["w"]
    m = skin(a)
    lo, hi = BAND.get(anim, BAND["idle"])
    lab, n = ndimage.label(m)
    face = set(np.unique(lab[: int(hy + 0.6 * w)])) - {0}  # the face skin blob (a drawing fist touching the chin merges into it)
    blobs = []
    for i in range(1, n + 1):
        if i in face:
            continue
        ys, xs = np.nonzero(lab == i)
        if len(ys) >= 10 and hy + lo * w <= ys.mean() <= hy + hi * w:
            blobs.append((float(xs.mean()), float(ys.mean()), len(ys), float(ys.max())))
    if anim == "attack":
        return attack_hands(blobs, hx, hy, w, row)
    # per side keep the biggest blob of the band (upper arms sit above it, knee plates / cloak specks below it)
    sides = {}
    for x, y, nn, ymax in blobs:
        k = "l" if x < hx else "r"
        if k not in sides or nn > sides[k][2]:
            sides[k] = (x, y, nn, ymax)
    out = {}
    if row in SCREEN_LEFT_R.get(anim, ()) or row in SCREEN_RIGHT_R.get(anim, ()):
        rk, lk = ("l", "r") if row in SCREEN_LEFT_R.get(anim, ()) else ("r", "l")
        for hand, k in (("R", rk), ("L", lk)):
            out[hand] = [round(sides[k][0], 1), round(sides[k][1], 1), True] if k in sides else [hx, hy + 2.2 * w, False]
    else:  # side views: the biggest blob is the near hand (facing right: R; facing left: L), the other one the far hand
        bs = sorted(blobs, key=lambda b: -b[2])
        near, far = ("R", "L") if row == 2 else ("L", "R")
        out[near] = [round(bs[0][0], 1), round(bs[0][1], 1), True] if bs else [hx, hy + 2.2 * w, False]
        out[far] = [round(bs[1][0], 1), round(bs[1][1], 1), True] if len(bs) > 1 else [out[near][0] + (8 if row == 2 else -8), out[near][1] - 4, False]
    return out


# attack: the bow arm (L) reaches toward the target: the blob furthest that way; the other blob is the drawing hand (R)
ATTACK_L_SIDE = {0: 1, 1: 1, 2: 1, 3: 1, 4: -1, 5: -1, 6: -1, 7: -1}  # +1 = L is the right-most blob, -1 = left-most


def attack_hands(blobs, hx, hy, w, row):
    hidden = [hx, hy + 1.6 * w, False]
    if not blobs:
        return {"R": list(hidden), "L": list(hidden)}
    bs = sorted(blobs, key=lambda b: -b[2])[:3]
    s = ATTACK_L_SIDE[row]
    lb = max(bs, key=lambda b: s * b[0])
    rest = [b for b in bs if b is not lb]
    out = {"L": [round(lb[0], 1), round(lb[1], 1), True]}
    out["R"] = [round(rest[0][0], 1), round(rest[0][1], 1), True] if rest else list(hidden)
    return out


def snap(cell, p, r=16):
    """A hand-read fist (x, y, vis) moved onto the centroid of the skin blob nearest to it (reading a 1.6x grid is off by ~5-10 px)."""
    m = skin(np.asarray(cell))
    yy, xx = np.nonzero(m)
    if not len(xx):
        return p
    d = (xx - p[0]) ** 2 + (yy - p[1]) ** 2
    i = int(np.argmin(d))
    if d[i] > r * r:
        return p
    lab, _ = ndimage.label(m)
    blob = lab == lab[yy[i], xx[i]]
    near = blob & ((np.mgrid[0:m.shape[0], 0:m.shape[1]][1] - xx[i]) ** 2 + (np.mgrid[0:m.shape[0], 0:m.shape[1]][0] - yy[i]) ** 2 <= 14 * 14)
    by, bx = np.nonzero(near)
    return [round(float(bx.mean()), 1), round(float(by.mean()), 1), True]


def table(cells, heads):
    """cells {anim: [row][f] PIL cell}; heads {(anim, row, f): head anchor}."""
    out = {}
    for anim, rows in cells.items():
        out[anim] = []
        for r, row in enumerate(rows):
            got = []
            for f, c in enumerate(row):
                t = detect(c, heads[(anim, r, f)], anim, r)
                fix = FIX.get((anim, r, f), {})
                t.update({k: list(v) for k, v in fix.items()})
                if anim == "attack" and "L" in fix and fix["L"][2]:
                    t["L"] = snap(c, t["L"])
                got.append(t)
            out[anim].append(got)
    return out


def marked(cells, tab, dst, X0=70, X1=250, Y0=90, Y1=250, S=3):
    for anim, rows in cells.items():
        n, nr = len(rows[0]), len(rows)
        cw, ch = (X1 - X0) * S, (Y1 - Y0) * S
        img = Image.new("RGBA", (n * cw, nr * ch), (200, 210, 190, 255))
        d = ImageDraw.Draw(img)
        for r in range(nr):
            for f in range(n):
                ox, oy = f * cw, r * ch
                img.alpha_composite(rows[r][f].crop((X0, Y0, X1, Y1)).resize((cw, ch), Image.NEAREST), (ox, oy))
                for gx in range(X0, X1, 10):
                    d.line((ox + (gx - X0) * S, oy, ox + (gx - X0) * S, oy + ch), fill=(0, 0, 0, 140 if gx % 50 == 0 else 45))
                    d.text((ox + (gx - X0) * S + 2, oy + 2), str(gx), fill=(200, 0, 0))
                for gy in range(Y0, Y1, 10):
                    d.line((ox, oy + (gy - Y0) * S, ox + cw, oy + (gy - Y0) * S), fill=(0, 0, 0, 140 if gy % 50 == 0 else 45))
                    d.text((ox + 2, oy + (gy - Y0) * S + 2), str(gy), fill=(0, 0, 200))
                for k, col in (("R", (230, 0, 0)), ("L", (0, 60, 255))):
                    x, y, vis = tab[anim][r][f][k]
                    X, Y = ox + (x - X0) * S, oy + (y - Y0) * S
                    d.ellipse((X - 7, Y - 7, X + 7, Y + 7), outline=col, fill=col if vis else None, width=3)
                    d.text((X + 9, Y - 6 + (12 if k == "L" else 0)), f"{k}{int(x)},{int(y)}", fill=col)
                d.text((ox + 40, oy + 14), f"r{r} f{f}", fill=(0, 0, 0))
                d.rectangle((ox, oy, ox + cw - 1, oy + ch - 1), outline=(255, 0, 255), width=2)
        img.convert("RGB").save(dst / f"fists-archer-{anim}.png")


def run(bc, ga, g, sheets_dir, check_dir):
    anims, heads, cells = g.body("archer")
    tab = table(cells, heads)
    sheets_dir.mkdir(parents=True, exist_ok=True)
    (sheets_dir / "archer-fists.json").write_text(json.dumps({
        "note": "Archer V3 fists (Tools/archer_v3_fists.py): [x, y, visible], cell px; R = right hand (string / arrow), L = left hand (BOW).",
        "anims": tab}, indent=0), encoding="utf-8")
    marked(cells, tab, check_dir)
    # hand layer = the fist drawn again over the bow grip. Attack rows are painted for all 8 dirs (never mirrored): only the bow fist L, the
    # string hand holds nothing (its disc would cut the bow where the hands cross). Idle / walk left dirs mirror the right rows, where the bow
    # sits in the mirrored R fist: keep both fists there.
    hand_tab = {a: [[dict(c, R=[c["R"][0], c["R"][1], False]) if a == "attack" else c for c in row] for row in rows] for a, rows in tab.items()}
    g.hand_sheet(hand_tab, anims, cells, "archer")
    return tab, anims, heads, cells

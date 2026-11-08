"""Wizard basic attack (4-hit combo) body rows repainted by ChatGPT after the board docs/reference/wizard-basic-attack-board.png (2026-11-08):
  attack1 Arcane Thrust (staff, right hand), attack2 Frost Page (book, left hand), attack3 Twin Sigil (staff raised + book forward),
  attack4 Astral Nova (both hands push the orb; new anim, 6 frames). attack1-3 are 4 frames (attack3 is also the generic skill cast pose).
ChatGPT paints each row with EMPTY fists, bare-headed (hairstyle 0), on flat #00FF00 (sources in ArtLibrary/classes/wizard/attack-board/body/,
the images sent in ../ref/). This tool fits every painted frame onto the game frame (docs/sprite-rules.md, ChatGPT rule 8): the body is scaled
so neck-to-sole equals idle's of the same facing, the head alone is shrunk to idle's hair width by a smooth warp above the neck (same method as
Tools/build-mechanic-attack-down-repaint.py NORMALIZE), soles on idle's sole row, feet centred on idle's feet. Then it writes per row:
  body/bare/<anim>.png    the fitted frames
  body/hand/<anim>.png    discs around the fists drawn in front of the staff / book (also saved as ArtLibrary/.../body/hand/<anim>.pre-head.png,
                          the base Tools/build-head-layer.py anims-write adds arms-over-head to)
  body/armed/<anim>.png   the frame composed with the T0 staff + book (fallback art; its row length is the anim's frame count in the game)
  held-wizard-melee.json  staff + book anchors of attack1-4 (staff in the RIGHT fist, book in the LEFT fist, sizes as idle: staff s 0.447,
                          book s 0.25 and its idle grip on the lower inner edge), poses from Tools/wizard_attack_repaint_poses.py
Facing LEFT is not a plain mirror (quy tắc tư thế cầm 4): left attack1 = right attack2 mirrored and left attack2 = right attack1 mirrored (the far
arm is the right arm facing left), left attack4 = right attack4 mirrored (fists side by side, staff / book swapped onto the matching fist),
left attack3 painted on its own (right arm raised = the far arm). Down / up attack2 = attack1 mirrored (front / back view: the other arm).
After `write` run:  python Tools/build-head-layer.py wizard anims-write attack1,attack2,attack3,attack4
                    python Tools/build-head-layer.py wizard anims-normscale attack1,attack2,attack3,attack4
                    python Tools/build-wizard-attack-board-repaint.py clean-hand   (drops violet back hair taken for an arm over the head)

  python Tools/build-wizard-attack-board-repaint.py grid OUT_DIR [anim-dir ...]    fitted frames, 4x, 8 px grid, fists marked (red = right, cyan = left)
  [TIER=N] python Tools/build-wizard-attack-board-repaint.py preview OUT.png [anim-dir ...]   rows with staff + book as they would be written
  python Tools/build-wizard-attack-board-repaint.py write [anim-dir ...]           sheets + anchors (default: every job)
  python Tools/build-wizard-attack-board-repaint.py check OUT.png                  idle | attack1-4, 4 facings, composed like the game + head ratios
  python Tools/build-wizard-attack-board-repaint.py cast-tips                       C# table of the cast flash points (HeroFx.WizardCastTip)
  python Tools/build-wizard-attack-board-repaint.py hair-check OUT.png             5 hairstyles x every attack frame x 4 facings (head layer check)
"""
import json
import os
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
sys.path.insert(0, "Tools")
import wizard_attack_compose as wc  # noqa: E402
from head_layer_classes import skin  # noqa: E402
from wizard_attack_repaint_poses import FIST_FIX, POSES, BOOK_S, STAFF_S  # noqa: E402

FRAME = 192
SRC = "Assets/ArtLibrary/classes/wizard/attack-board/body"
BODY = "Assets/Resources/Sprites/classes/wizard/body"
PRE_HEAD = "Assets/ArtLibrary/classes/wizard/body/hand"
MELEE = "Assets/Resources/Data/classes/wizard/held-wizard-melee.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"attack1": 4, "attack2": 4, "attack3": 4, "attack4": 6}
JOBS = {  # (anim, dir) -> painted strip
    ("attack1", "down"): f"{SRC}/attack1-down-chatgpt-1.png",
    ("attack1", "right"): f"{SRC}/attack1-right-chatgpt-1.png",
    ("attack1", "up"): f"{SRC}/attack1-up-chatgpt-1.png",
    ("attack2", "right"): f"{SRC}/attack2-right-chatgpt-1.png",
    ("attack3", "down"): f"{SRC}/attack3-down-chatgpt-1.png",
    ("attack3", "right"): f"{SRC}/attack3-right-chatgpt-1.png",
    ("attack3", "up"): f"{SRC}/attack3-up-chatgpt-1.png",
    ("attack3", "left"): f"{SRC}/attack3-left-chatgpt-1.png",
    ("attack4", "down"): f"{SRC}/attack4-down-chatgpt-1.png",
    ("attack4", "right"): f"{SRC}/attack4-right-chatgpt-1.png",
    ("attack4", "up"): f"{SRC}/attack4-up-chatgpt-1.png",
}
# (anim, dir) -> (source anim, source dir): drawn mirrored; the source's right fist becomes this row's left fist and the other way round
MIRRORS = {("attack2", "down"): ("attack1", "down"), ("attack2", "up"): ("attack1", "up"),
           ("attack1", "left"): ("attack2", "right"), ("attack2", "left"): ("attack1", "right"), ("attack4", "left"): ("attack4", "right")}
FIST_R = 8.5
# Painted frame shown at each game frame. The game spreads the frames evenly over the whole hit (ComboSystem.PhaseProgress over windup + active
# + recover) and the bolt leaves at the end of the windup: hits 1-3 at 70 of 260 ms = game frame 1 of 4, hit 4 at 130 of 470 ms = frame 1 of 6.
# So the painted release pose (frame 2) is shown from game frame 1 and held; the swing / pull-back frame between is dropped.
ORDER = {"attack1": [0, 2, 2, 3], "attack2": [0, 2, 2, 3], "attack3": [1, 2, 2, 3], "attack4": [1, 2, 3, 4, 4, 5]}


def ordered(anim, items):
    return [items[i] for i in ORDER[anim]]
HEAD_BLEND = 14.0


def hair(a):
    """Lavender-white hair of the Wizard (style 0)."""
    r, g, b = (a[..., i].astype(int) for i in range(3))
    return (b >= r - 6) & (r > 165) & (g > 135) & (b > 190) & (b - g > 8)


def hair_width(im):
    """Width of the hair in the top 30 % of the figure, px."""
    a = np.asarray(im)
    al = a[..., 3] > 100
    ys = np.nonzero(al)[0]
    top, bot = ys.min(), ys.max()
    m = al & hair(a)
    xs = np.flatnonzero(m[top:top + int(0.3 * (bot - top + 1))].sum(0) > 1)
    return float(xs.max() - xs.min() + 1)


def feet_x(im):
    al = np.asarray(im)[..., 3] > 100
    ys, xs = np.nonzero(al)
    sel = ys > ys.max() - 10
    return (xs[sel].min() + xs[sel].max()) / 2


def idle_ref(d):
    """Idle frame 0 of facing d as the game draws it (headless + head style 0, no gear): hair width, hair top, neck row, sole row, feet x."""
    im = wc.compose("idle", d, 0, anchors=wc.no_gear())
    ys = np.nonzero(np.asarray(im)[..., 3] > 100)[0]
    hl = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA"))[ROWS[d] * FRAME:(ROWS[d] + 1) * FRAME, :FRAME, 3] > 100
    neck = int(np.flatnonzero(hl[:, 70:122].any(1)).min())
    return hair_width(im), int(ys.min()), neck, int(ys.max()), feet_x(im)


def key_green(path):
    """Straight RGBA of a strip on flat #00FF00: alpha from the distance to the key colour, green spill clamped on soft edges."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 40) / 70, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    edge = alpha < 0.98
    a[..., 1] = np.where(edge, np.minimum(g, np.maximum(r, b) + 12), g)
    a[..., 3] = alpha * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def pieces(path, count):
    """The painted frames left to right, each cropped to its box (blobs joined by a 6 px dilation, biggest `count`)."""
    fig = key_green(path)
    al = np.asarray(fig)[..., 3] > 100
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=6))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    best = sorted(1 + np.argsort(sizes)[::-1][:count], key=lambda i: np.nonzero(lab == i)[1].min())
    out = []
    for i in best:
        part = np.asarray(fig).copy()
        part[..., 3] = np.where(lab == i, part[..., 3], 0)
        im = Image.fromarray(part)
        out.append(im.crop(im.getbbox()))
    return out


def _shrink(q, dist):
    t = np.clip(dist / HEAD_BLEND, 0, 1)
    return 1 - (1 - q) * t * t * (3 - 2 * t)


def _lift(q, dist):
    ts = np.linspace(0, 1, 65)
    xs = np.asarray(dist, float)[..., None] * ts
    return np.trapezoid(_shrink(q, xs), xs, axis=-1)


def warp(cell, p):
    """Head shrink q above the neck row (smooth over HEAD_BLEND px), about the head centre column (inverse map, premultiplied bilinear)."""
    if p["q"] == 1.0:
        return cell
    src_d = np.linspace(0, FRAME, 4 * FRAME + 1)
    out_d = _lift(p["q"], src_d)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME].astype(float) + 0.5
    d_o = p["neck"] - yy
    d_s = np.where(d_o > 0, np.interp(d_o, out_d, src_d), d_o)
    r = np.where(d_o > 0, _shrink(p["q"], np.maximum(d_s, 0)), 1.0)
    sy = p["neck"] - d_s - 0.5
    sx = p["cx"] + (xx - p["cx"]) / r - 0.5
    a = np.asarray(cell).astype(float)
    pm = np.dstack([a[..., :3] * a[..., 3:4] / 255, a[..., 3]])
    res = np.dstack([ndimage.map_coordinates(pm[..., i], [sy, sx], order=1, mode="constant") for i in range(4)])
    al = res[..., 3:4]
    rgb = np.where(al > 0.5, res[..., :3] * 255 / np.maximum(al, 1e-6), 0)
    return Image.fromarray(np.dstack([np.clip(rgb, 0, 255), np.clip(al, 0, 255)]).round().astype(np.uint8), "RGBA")


def top_bot(im):
    ys = np.nonzero(np.asarray(im)[..., 3] > 100)[0]
    return int(ys.min()), int(ys.max())


def head_cx(im, top, bot):
    a = np.asarray(im)
    m = (a[..., 3] > 100) & hair(a)
    xs = np.nonzero(m[top:top + int(0.3 * (bot - top + 1))])[1]
    return (xs.min() + xs.max()) / 2


_CACHE = {}


def fitted(anim, d):
    """New 192 cells of a row: painted frames normalised to idle's head and body (one scale for the whole row), or a mirrored row."""
    key = (anim, d)
    if key in _CACHE:
        return _CACHE[key]
    if key in MIRRORS:
        cells = [c.transpose(Image.FLIP_LEFT_RIGHT) for c in fitted(*MIRRORS[key])]
        _CACHE[key] = cells
        return cells
    hw, itop, ineck, ibot, ifx = idle_ref(d)
    figs = pieces(JOBS[key], COUNT[anim])
    news = [top_bot(fg) for fg in figs]
    k_head = hw / float(np.median([hair_width(fg) for fg in figs]))
    head_raw = (ineck - itop) / k_head  # raw rows from the hair top to the neck when the head is drawn at idle's size
    k = (ibot - ineck) / float(np.median([b - (t + head_raw) for t, b in news]))
    q = k_head / k
    params = []
    for fg, (t, b) in zip(figs, news):
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        ft, fb = top_bot(im)
        ox, oy = int(round(ifx - feet_x(im))), int(ibot - fb)
        params.append(dict(im=im, ox=ox, oy=oy, q=q, neck=oy + (t + head_raw) * k, cx=ox + head_cx(im, ft, fb)))
    cells = []
    for _ in range(2):  # the warp leaves the hair's lower part less shrunk: correct q once so the built head measures idle's hair width
        cells = []
        for p in params:
            c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
            c.alpha_composite(p["im"], (p["ox"], p["oy"]))
            cells.append(warp(c, p))
        built = float(np.median([hair_width(c) for c in cells]))
        for p in params:
            p["q"] *= hw / built
    print(f"{anim} {d}: body scale {k:.3f}, head shrink {params[0]['q']:.3f}, hair {built:.1f} px (idle {hw:.1f})")
    _CACHE[key] = cells
    return cells


def auto_fists(cell, d):
    """Skin blobs that are fists (not the face): [(x, y)] sorted screen-left to right."""
    a = np.asarray(cell)
    m = skin(a) & (a[..., 3] > 200)
    lab, n = ndimage.label(ndimage.binary_closing(m, iterations=1))
    if n == 0:
        return []
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    face = 1 + int(np.argmax(sizes)) if d != "up" else -1
    out = []
    for i in range(1, n + 1):
        if i == face or sizes[i - 1] < 25:
            continue
        ys, xs = np.nonzero(lab == i)
        if ys.mean() > 165:  # light boot leather near the soles
            continue
        out.append((round(float(xs.mean()), 1), round(float(ys.mean()), 1), int(sizes[i - 1])))
    out.sort(key=lambda t: -t[2])
    return sorted([(x, y) for x, y, _ in out[:2]], key=lambda t: t[0])


def fists_of(anim, d):
    """Per frame {"R": (x, y), "L": (x, y)}: the hero's right fist (staff) and left fist (book)."""
    key = (anim, d)
    if key in MIRRORS:
        src = fists_of(*MIRRORS[key])
        return [{"R": (FRAME - f["L"][0], f["L"][1]), "L": (FRAME - f["R"][0], f["R"][1])} for f in src]
    out = []
    for f, c in enumerate(fitted(anim, d)):
        fix = FIST_FIX.get(key, {}).get(f)
        if fix:
            out.append({"R": fix[0], "L": fix[1]})
            continue
        pts = auto_fists(c, d)
        if len(pts) < 2:
            pts = (pts + [(96.0, 120.0)] * 2)[:2]
        # down: the right hand is on screen-left; up: on screen-right; right / left facings need FIST_FIX (near / far)
        if d == "up":
            out.append({"R": pts[1], "L": pts[0]})
        elif d == "down":
            out.append({"R": pts[0], "L": pts[1]})
        elif d == "right":
            out.append({"R": pts[1], "L": pts[0]})
        else:
            out.append({"R": pts[0], "L": pts[1]})
    return out


def pose_of(anim, d, f):
    """Staff / book pose of a frame (POSES has every row, mirrored ones too: the staff of a mirrored row sits on the source's book fist)."""
    return POSES[(anim, d)][f]


def anchors_of(anim, d):
    fs = fists_of(anim, d)
    staff, book = [], []
    for f, fist in enumerate(fs):
        p = pose_of(anim, d, f)
        staff.append({"x": round(fist["R"][0], 1), "y": round(fist["R"][1], 1), "angle": p["staff"]["angle"], "s": STAFF_S, "sx": 1,
                      "behind": p["staff"]["behind"], "hidden": False, "fallback": False})
        book.append({"x": round(fist["L"][0], 1), "y": round(fist["L"][1], 1), "angle": p["book"]["angle"], "s": BOOK_S, "sx": p["book"]["sx"],
                     "behind": p["book"]["behind"], "hidden": False, "fallback": False})
    return {"staff": staff, "book": book}


def fist_layer(cell, pts):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for x, y in pts:
        m |= (xx - x) ** 2 + (yy - y) ** 2 <= FIST_R ** 2
    a = np.asarray(cell).copy()
    a[..., 3] = np.where(m, a[..., 3], 0)
    return Image.fromarray(a)


def hand_cells(anim, d):
    """Fist discs of the fists whose item is drawn in front (the fist covers the handle / the book's edge)."""
    an = anchors_of(anim, d)
    out = []
    for f, c in enumerate(fitted(anim, d)):
        pts = [(a["x"], a["y"]) for a in (an["staff"][f], an["book"][f]) if not a["behind"]]
        out.append(fist_layer(c, pts))
    return out


def parse(args):
    jobs = list(JOBS) + list(MIRRORS)
    if not args:
        return jobs
    want = [tuple(a.split("-")) for a in args]
    return [j for j in jobs if j in want]


def grid(out, jobs):
    os.makedirs(out, exist_ok=True)
    z = 4
    for anim, d in jobs:
        cells = fitted(anim, d)
        fists = fists_of(anim, d)
        w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
        dr = ImageDraw.Draw(w)
        for f, c in enumerate(cells):
            big = c.resize((FRAME * z, FRAME * z), Image.NEAREST)
            w.paste(big, (f * FRAME * z, 0), big)
            for v in range(0, FRAME, 8):
                col = (255, 255, 0) if v % 32 == 0 else (95, 95, 95)
                dr.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=col)
                dr.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=col)
                if v % 32 == 0:
                    dr.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    dr.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
            for k, col in (("R", (255, 40, 40)), ("L", (0, 220, 255))):
                x, y = fists[f][k]
                dr.ellipse(((f * FRAME + x - FIST_R) * z, (y - FIST_R) * z, (f * FRAME + x + FIST_R) * z, (y + FIST_R) * z), outline=col, width=3)
            dr.text((f * FRAME * z + 6, FRAME * z - 16), f"f{f} R{fists[f]['R']} L{fists[f]['L']}", fill=(255, 255, 255))
        w.save(f"{out}/{anim}-{d}-grid.png")
        print(f"{out}/{anim}-{d}-grid.png")


def preview(out, jobs, tier=0):
    """Rows composed with the staff + book of a tier from the anchors this tool would write (painted head, nothing written to the game)."""
    import tempfile
    _, loaded = wc.chf.load_gear("wizard")
    tmp = tempfile.mkdtemp()
    pad, z = 40, 2
    size = FRAME + 2 * pad
    rows = []
    for anim, d in jobs:
        cells, hands, an = fitted(anim, d), hand_cells(anim, d), anchors_of(anim, d)
        bare, hand = f"{tmp}/bare.png", f"{tmp}/hand.png"
        sb = Image.new("RGBA", (FRAME * len(cells), FRAME * 4), (0, 0, 0, 0))
        sh = sb.copy()
        for f, (c, h) in enumerate(zip(cells, hands)):
            sb.paste(c, (f * FRAME, ROWS[d] * FRAME))
            sh.paste(h, (f * FRAME, ROWS[d] * FRAME))
        sb.save(bare)
        sh.save(hand)
        anchors = dict(loaded)
        anchors[("staff", anim, d)] = an["staff"]
        anchors[("book", anim, d)] = an["book"]
        rows.append([wc.chf.compose("wizard", anim, d, f, bare_path=bare, anchors=anchors, hand_path=hand, tier=tier, pad=pad) for f in range(len(cells))])
    w = Image.new("RGB", (size * max(len(r) for r in rows), size * len(rows)), (58, 62, 70))
    for r, row in enumerate(rows):
        for f, im in enumerate(row):
            w.paste(wc.on_bg(im), (f * size, r * size))
    w = w.resize((w.width * z, w.height * z), Image.NEAREST)
    w.save(out)
    print(out, w.size)


def ensure_sheet(path, anim):
    n = COUNT[anim]
    if os.path.exists(path):
        im = Image.open(path).convert("RGBA")
        if im.width == n * FRAME and im.height == 4 * FRAME:
            return im
        old = f"{SRC}/old/{os.path.basename(os.path.dirname(path))}-{anim}.png"
        os.makedirs(os.path.dirname(old), exist_ok=True)
        if not os.path.exists(old):
            shutil.copyfile(path, old)
            print("backup", old)
    return Image.new("RGBA", (n * FRAME, 4 * FRAME), (0, 0, 0, 0))


def write_meta(png, like):
    """New sheet next to an existing one: clone its .meta with a fresh guid (no compression, 4096, as the Lv1 sheets)."""
    meta = png + ".meta"
    if os.path.exists(meta):
        return
    import uuid
    txt = open(like + ".meta", encoding="utf-8").read()
    import re
    txt = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, txt, count=1)
    open(meta, "w", encoding="utf-8").write(txt)
    print("meta", meta)


def write(jobs):
    data = json.load(open(MELEE, encoding="utf-8"))
    sheets = {}
    for anim, d in jobs:
        for layer in ("bare", "hand", "armed"):
            path = f"{BODY}/{layer}/{anim}.png"
            if path not in sheets:
                sheets[path] = ensure_sheet(path, anim)
    for anim, d in jobs:
        r = ROWS[d]
        cells, hands = ordered(anim, fitted(anim, d)), ordered(anim, hand_cells(anim, d))
        an = {k: ordered(anim, v) for k, v in anchors_of(anim, d).items()}
        for part in ("staff", "book"):
            data["anchors"] = [a for a in data["anchors"] if not (a["part"] == part and a["anim"] == anim and a["dir"] == d)]
            data["anchors"].append({"part": part, "anim": anim, "dir": d, "frames": an[part]})
        for f in range(COUNT[anim]):
            box = (f * FRAME, r * FRAME)
            blank = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
            sheets[f"{BODY}/bare/{anim}.png"].paste(cells[f], box)
            sheets[f"{BODY}/hand/{anim}.png"].paste(hands[f], box)
            sheets[f"{BODY}/armed/{anim}.png"].paste(blank, box)
        print(anim, d, "row ready")
    for path, im in sheets.items():
        im.save(path)
        write_meta(path, path.replace("attack4", "attack1"))
    for anim in sorted({a for a, _ in jobs}):  # the hand sheet is also the head tool's base (arms over the head are added onto it)
        os.makedirs(PRE_HEAD, exist_ok=True)
        shutil.copyfile(f"{BODY}/hand/{anim}.png", f"{PRE_HEAD}/{anim}.pre-head.png")
    with open(MELEE, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
        fh.write("\n")
    # armed = the frame composed with the T0 staff + book (needs the anchors and sheets above on disk)
    for anim in sorted({a for a, _ in jobs}):
        path = f"{BODY}/armed/{anim}.png"
        im = Image.open(path).convert("RGBA")
        for a2, d in jobs:
            if a2 != anim:
                continue
            for f in range(COUNT[anim]):
                im.paste(wc.chf.compose("wizard", anim, d, f), (f * FRAME, ROWS[d] * FRAME))
        im.save(path)
    print("wrote", len(jobs), "rows; now run build-head-layer.py wizard anims-write / anims-normscale")


def head_ratio(im):
    """Hair width / figure height (hair top to sole) of a composed frame."""
    t, b = top_bot(im)
    return hair_width(im) / (b - t + 1)


def check(out):
    """idle | attack1..4 (every frame) for 4 facings, composed like the game (head layer style 0, T0 gear), with head ratio vs idle."""
    pad, z = 40, 2
    size = FRAME + 2 * pad
    cols = 1 + sum(COUNT.values())
    sheet = Image.new("RGB", (cols * size, 4 * (size + 14) + 20), (34, 36, 44))
    dr = ImageDraw.Draw(sheet)
    for r, d in enumerate(("down", "right", "up", "left")):
        y = 20 + r * (size + 14)
        idle = wc.compose("idle", d, 0, pad=pad)
        iref = head_ratio(wc.compose("idle", d, 0, anchors=wc.no_gear()))
        sheet.paste(wc.on_bg(idle, (70, 90, 120)), (0, y))
        dr.text((4, y + size), f"{d} idle {iref:.3f}", fill=(255, 255, 0))
        x = size
        for anim in ("attack1", "attack2", "attack3", "attack4"):
            for f in range(COUNT[anim]):
                im = wc.compose(anim, d, f, pad=pad)
                hr = head_ratio(wc.compose(anim, d, f, anchors=wc.no_gear()))
                sheet.paste(wc.on_bg(im), (x, y))
                dev = (hr / iref - 1) * 100
                dr.text((x + 4, y + size), f"{anim[-1]}.{f} {dev:+.0f}%", fill=(255, 120, 120) if abs(dev) > 5 else (200, 255, 200))
                x += size
    for i, t in enumerate(("idle", "attack1 Arcane Thrust", "attack2 Frost Page", "attack3 Twin Sigil", "attack4 Astral Nova")):
        dr.text((4 + (0 if i == 0 else size * (1 + sum(list(COUNT.values())[:i - 1]))), 4), t, fill=(255, 255, 255))
    sheet = sheet.resize((sheet.width * z // 2, sheet.height * z // 2), Image.NEAREST)
    sheet.save(out)
    print(out, sheet.size, "(numbers: head width / height vs idle of the same facing)")


def clean_hand():
    """After build-head-layer.py anims-write: keep only real arm pixels of its arms-over-head layer. Its test takes the Wizard's violet back hair
    (not in the class hair test) for an arm, so purple hair specks were drawn over every hairstyle facing up (2026-11-08). Additions that are
    violet / lavender hair, or a dark outline not touching a kept arm pixel, are dropped; the fist discs (pre-head sheet) stay."""
    for anim in COUNT:
        path = f"{BODY}/hand/{anim}.png"
        h = np.asarray(Image.open(path).convert("RGBA")).astype(int)
        p = np.asarray(Image.open(f"{PRE_HEAD}/{anim}.pre-head.png").convert("RGBA")).astype(int)
        add = (h[..., 3] > 0) & (np.abs(h - p).sum(-1) > 0)
        r, g, b = h[..., 0], h[..., 1], h[..., 2]
        violet = (b > g + 10) & (r > g)
        lum = h[..., :3] @ [0.3, 0.59, 0.11]
        keep = add & ~violet & ~hair(h) & (lum >= 55)
        keep |= add & (lum < 55) & ndimage.binary_dilation(keep, iterations=1)
        lab, n = ndimage.label(keep, np.ones((3, 3)))  # an arm is a solid piece; hair highlights / ear specks are a few px
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        keep &= np.isin(lab, [i + 1 for i in range(n) if sizes[i] >= 30])
        # Wizard poses never bring an arm in front of the head: the raised Twin Sigil arm goes up and BACK (facing left it is the far arm),
        # so even a real arm piece drawn over the hairstyle is wrong (a white sleeve patch on the hair, attack3 left). Keep none.
        keep[:] = False
        out = p.copy()
        out[keep] = h[keep]
        Image.fromarray(out.astype(np.uint8), "RGBA").save(path)
        print(anim, "arm-over-head px kept", int(keep.sum()), "of", int(add.sum()))


CAST_FRAME = 1  # game frame where the bolt leaves (ORDER): the cast flash sits on the hand that casts it at that frame


def cast_tips():
    """C# table for HeroFx.WizardCastTip: per hit (1-4) and facing (down, up, left, right) the cast flash point in world units from the feet
    (8 frame px = 1 unit, figure scale 1), at the release frame: hits 1 and 3 the T0 staff crystal, hit 2 the book's centre, hit 4 between the fists.
    Re-run and paste after changing poses / fists (the flash used to sit on the chest and its 14-unit star covered the body facing down)."""
    import math
    hw = json.load(open("Assets/Resources/Data/classes/wizard/held-wizard.json", encoding="utf-8"))
    staff, book = hw["weapon"][0], hw["book"][0]
    art = np.asarray(Image.open(f"Assets/Resources/Sprites/classes/wizard/weapons/staff/{staff['name']}-base.png").convert("RGBA")).astype(int)
    blue = (art[..., 3] > 200) & (art[..., 2] > art[..., 0] + 60)
    ys, xs = np.nonzero(blue[:120])
    crystal = (xs.mean() - staff["baseGrip"]["gripX"], ys.mean() - staff["baseGrip"]["gripY"])
    bg = book["baseGrip"]
    book_c = (bg["w"] / 2 - bg["gripX"], bg["h"] / 2 - bg["gripY"])

    def tip(a, v):
        th = math.radians(a["angle"])
        dx, dy = v[0] * a["s"] * a.get("sx", 1), v[1] * a["s"]
        return a["x"] + dx * math.cos(th) - dy * math.sin(th), a["y"] + dx * math.sin(th) + dy * math.cos(th)

    rows = []
    for hit, anim in enumerate(("attack1", "attack2", "attack3", "attack4"), 1):
        cells = []
        for d in ("down", "up", "left", "right"):
            an = {k: ordered(anim, v) for k, v in anchors_of(anim, d).items()}
            s, b = an["staff"][CAST_FRAME], an["book"][CAST_FRAME]
            if hit == 2:
                x, y = tip(b, book_c)
            elif hit == 4:
                x, y = (s["x"] + b["x"]) / 2, (s["y"] + b["y"]) / 2
            else:
                x, y = tip(s, crystal)
            cells.append(f"new Vector2({(x - 96) / 8:.1f}f, {(192 - y) / 8:.1f}f)")
        rows.append("{ " + ", ".join(cells) + " }")
    print("static readonly Vector2[][] WizardCastTip = {\n    " + ",\n    ".join(rows) + "\n};")


def hair_check(out, styles=range(5)):
    """Every hairstyle (heads/<n>) x every attack frame x 4 facings, composed like the game (T0 staff + book): head placed, sized, neck closed?"""
    pad = 14
    size = FRAME + 2 * pad
    cols = sum(COUNT.values())
    rows = [(d, st) for d in ("down", "right", "up", "left") for st in styles]
    sheet = Image.new("RGB", (60 + cols * size, len(rows) * size), (34, 36, 44))
    dr = ImageDraw.Draw(sheet)
    for r, (d, st) in enumerate(rows):
        dr.text((4, r * size + size // 2), f"{d}\nstyle {st}", fill=(255, 255, 0))
        x = 60
        for anim in ("attack1", "attack2", "attack3", "attack4"):
            for f in range(COUNT[anim]):
                sheet.paste(wc.on_bg(wc.compose(anim, d, f, style=st, pad=pad), (70, 90, 120) if st % 2 else (58, 62, 70)), (x, r * size))
                x += size
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "grid":
        grid(sys.argv[2], parse(sys.argv[3:]))
    elif cmd == "write":
        write(parse(sys.argv[2:]))
    elif cmd == "preview":
        preview(sys.argv[2], parse(sys.argv[3:]), int(os.environ.get("TIER", "0")))
    elif cmd == "check":
        check(sys.argv[2])
    elif cmd == "cast-tips":
        cast_tips()
    elif cmd == "clean-hand":
        clean_hand()
    elif cmd == "hair-check":
        hair_check(sys.argv[2])
    else:
        print(__doc__)

"""Mechanic attack1-4 body rows repainted by ChatGPT, per the approved board docs/reference/mechanic-basic-attack-board.png (2026-11-08).
Started for facing DOWN (attack1 = the hero's RIGHT arm shoots, attack2 = the LEFT arm, attack3 = both); extended the same day to the
other facings and to attack4 (cannon on the shoulder). ChatGPT repaints an in-game row with empty fists (no pistols, no cannon) on flat
#00FF00; this tool fits each painted frame onto the old frame of the same row (one hair-top-to-sole scale for the whole row, soles on the
old sole row, feet on the old frame 0's feet), writes that row of body/bare/<anim>.png and of the fist layer body/hand/<anim>.png (disc
FIST_R around each fist drawn in front), and the anchors of that row in held-mechanic-dual.json:
  attack1-3: x, y of gun (the hero's right hand) / gunL (left hand) on the fists. Down / up art and angles are written afterwards by
             Tools/build-mechanic-tier-weapon-fit.py `write` (AIM_HANDS, AIM_REST_FRAMES): run it after this tool. Right / left rows get
             their pose here (SIDE_POSE: side art pointing right, the resting hand hanging forward-down).
  attack4:   pistols hidden, cannon placed through the two fists (FISTS = (rear fist, front fist), the barrel along that line).
Facing LEFT is not a plain mirror (docs/sprite-rules.md, quy tắc tư thế cầm 4): facing right the right hand is the near one, facing
left it is the far one, so left attack1 (right pistol, far arm) = right attack2 (far arm) mirrored, and left attack2 = right attack1
mirrored, with gun / gunL swapped (MIRRORS). attack3 / attack4 left are drawn by the game as right mirrored (HeroClassDef.MirrorLeft).
Up attack2 = up attack1 mirrored (back view: the screen-left arm is the left arm).

Measures are always taken on the backups in old/ (bare-<anim>.png, hand-<anim>.png, saved before the first write), never on the sheet
this tool wrote (docs/sprite-rules.md 2026-11-05: a tool that measures what it wrote drifts each run).

  python Tools/build-mechanic-attack-down-repaint.py grid OUT_DIR [anim-dir ...]   fitted frames on an 8 px grid at 4x, to read FISTS
  python Tools/build-mechanic-attack-down-repaint.py write [anim-dir ...]          bare + hand rows + anchors (default: every job), then the head layer
  python Tools/build-mechanic-attack-down-repaint.py head [anim ...]               head layer only: body/headless/<anim>.png, head-anchors.json (anims-write
                                                                                    + anims-normscale), arm pixels over the head into hand/<anim>.png
Sources: JOBS (attack-down-repaint/ for the first three down rows, attack-board-repaint/ for the rest, refs sent in ref/).
Fist table: Tools/mechanic_attack_repaint_fists.py.
"""
import importlib.util, json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
sys.path.insert(0, "Tools")


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


oi = load("oi", "Tools/build-mechanic-outfit-idle-down.py")
from mechanic_attack_repaint_fists import FISTS, SIDE_POSE, CANNON_LINE

FRAME = 192
SRC = "Assets/ArtLibrary/classes/mechanic/new-style/attack-down-repaint"
SRC2 = "Assets/ArtLibrary/classes/mechanic/new-style/attack-board-repaint"
OLD = f"{SRC}/old"
BODY = "Assets/Resources/Sprites/classes/mechanic/body"
GEAR_DUAL = "Assets/Resources/Data/classes/mechanic/held-mechanic-dual.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COUNT = {"attack1": 4, "attack2": 4, "attack3": 6, "attack4": 6}
JOBS = {  # (anim, dir) -> painted source
    ("attack1", "down"): f"{SRC}/attack1-down-chatgpt-1.png",
    ("attack2", "down"): f"{SRC}/attack2-down-chatgpt-1.png",
    ("attack3", "down"): f"{SRC2}/attack3-down-chatgpt-2.png",  # v1 (attack-down-repaint/) had the fists at face height: the pistols covered the face
    ("attack1", "right"): f"{SRC2}/attack1-right-chatgpt-1.png",  # the near (right) arm; the old row was the far arm like attack2 (user 2026-11-08)
    ("attack2", "right"): f"{SRC2}/attack2-right-chatgpt-1.png",
    ("attack1", "up"): f"{SRC2}/attack1-up-chatgpt-1.png",
    ("attack3", "up"): f"{SRC2}/attack3-up-chatgpt-1.png",
    ("attack4", "down"): f"{SRC2}/attack4-down-chatgpt-1.png",
    ("attack4", "right"): f"{SRC2}/attack4-right-chatgpt-1.png",
    ("attack3", "right"): "old",  # not repainted: the old row, head-normalised (NORMALIZE), its fist layer and pistol anchors moved with it
    ("attack4", "up"): "old",  # same (hair 81 px against idle up 74); its cannon anchors were moved once with the row, write leaves them
}
# (anim, dir) -> (source anim, source dir, "new" = that job's fitted row / "old" = the backup row), drawn mirrored, gun <-> gunL
MIRRORS = {("attack2", "up"): ("attack1", "up", "new"), ("attack1", "left"): ("attack2", "right", "new"),
           ("attack2", "left"): ("attack1", "right", "new")}
FIST_R = 9.5


def ensure_backup(anim):
    for layer in ("bare", "hand"):
        dst = f"{OLD}/{layer}-{anim}.png"
        if not os.path.exists(dst):
            shutil.copyfile(f"{BODY}/{layer}/{anim}.png", dst)
            print("backup", dst)


def old_cell(layer, anim, d, f):
    r = ROWS[d]
    return Image.open(f"{OLD}/{layer}-{anim}.png").convert("RGBA").crop((f * FRAME, r * FRAME, f * FRAME + FRAME, r * FRAME + FRAME))


def pieces(path, count):
    """The painted frames left to right, each cropped to its box."""
    fig = oi.key_flat(path)
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


def feet_x(im):
    al = np.asarray(im)[..., 3] > 100
    ys, xs = np.nonzero(al)
    sel = ys > ys.max() - 12
    return (xs[sel].min() + xs[sel].max()) / 2


# Head size (user 2026-11-08): ChatGPT draws the head bigger than the game's idle, so a row fitted by its total height had a big head on a short
# body (attack4 down/right, attack1-4 right, attack1 up: head 0.51-0.56 of the height against 0.47 idle). For these rows the body is scaled so the
# neck-to-sole length equals idle's of the same facing (uniform, no stretch), then the head alone is shrunk to idle's size (blond hair width in the
# top quarter) by a smooth warp above the neck: full shrink HEAD_BLEND px above the neck row, none at it, so an arm crossing the neck line bends
# a little instead of breaking. Down attack1-3 and up attack3 were within 5 % of idle (user: leave them) and keep the height fit. attack3 right is the
# old (not repainted) row, attack4 up too (9 % over idle up): normalised the same way from their own cells.
NORMALIZE = {("attack4", "down"), ("attack4", "right"), ("attack1", "right"), ("attack2", "right"), ("attack3", "right"), ("attack1", "up"), ("attack4", "up")}
IDLE_ROW = {"down": 0, "up": 1, "left": 2, "right": 3}
HEAD_BLEND = 14.0


def hair_width(im):
    """Width of the blond hair in the top quarter of the figure (bare sheets: no brass weapon to confuse it), px."""
    a = np.asarray(im).astype(int)
    al = a[..., 3] > 100
    ys = np.nonzero(al)[0]
    top, bot = ys.min(), ys.max()
    rgb = a[..., :3]
    blond = al & (rgb[..., 0] > 190) & (rgb[..., 1] > 130) & (rgb[..., 2] < 140) & (rgb[..., 0] - rgb[..., 2] > 90)
    xs = np.flatnonzero(blond[top:top + int(0.25 * (bot - top + 1))].sum(0) > 1)
    return float(xs.max() - xs.min() + 1)


def idle_ref(d):
    """Idle frame 0 of facing d: (hair width, hair top, neck row = top of the headless body, sole row)."""
    r = IDLE_ROW[d]
    idle = Image.open(f"{BODY}/bare/idle.png").convert("RGBA").crop((0, r * FRAME, FRAME, r * FRAME + FRAME))
    hl = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA").crop((0, r * FRAME, FRAME, r * FRAME + FRAME)))[..., 3] > 100
    ys = np.nonzero(np.asarray(idle)[..., 3] > 100)[0]
    return hair_width(idle), int(ys.min()), int(np.flatnonzero(hl[:, 80:112].any(1)).min()), int(ys.max())


def sources(anim, d):
    """Raw frames of a row: the painted pieces, or (JOBS value "old") the backup row's own cells."""
    if JOBS[(anim, d)] == "old":
        return [old_cell("bare", anim, d, f) for f in range(COUNT[anim])], [old_cell("hand", anim, d, f) for f in range(COUNT[anim])]
    return pieces(JOBS[(anim, d)], COUNT[anim]), None


def fit_params(anim, d):
    """Per frame dict(k, ox, oy, im, neck, cx, q): cell = warp(raw scaled by k placed at ox, oy); q = head shrink above the neck row (1 = none)."""
    figs, _ = sources(anim, d)
    olds = [oi.metrics(old_cell("bare", anim, d, f)) for f in range(COUNT[anim])]
    news = [oi.metrics(fg) for fg in figs]
    fx0 = feet_x(old_cell("bare", anim, d, 0))
    if (anim, d) in NORMALIZE:
        hw, itop, ineck, ibot = idle_ref(d)
        k_head = hw / float(np.median([hair_width(fg) for fg in figs]))
        head_raw = (ineck - itop) / k_head  # raw rows from the hair top to the neck, the head drawn at idle's proportions
        k = (ibot - ineck) / float(np.median([n[1] - (n[0] + head_raw) for n in news]))
        q = k_head / k
    else:
        k = float(np.median([(o[1] - o[0] + 1) / (n[1] - n[0] + 1) for o, n in zip(olds, news)]))
        head_raw, q = 0.0, 1.0
    out = []
    for fg, (top, bot, hx), (ntop, nbot, nhx) in zip(figs, olds, news):
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        ftop, fbot, fhx = oi.metrics(im)
        ox, oy = int(round(fx0 - feet_x(im))), int(bot - fbot)
        out.append(dict(k=k, ox=ox, oy=oy, im=im, q=q, neck=oy + (ntop + head_raw) * k, cx=ox + fhx))
    if q != 1.0:  # the warp leaves the bottom of the hair less shrunk: correct q once so the built head measures idle's hair width
        built = []
        for p in out:
            c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
            c.alpha_composite(p["im"], (p["ox"], p["oy"]))
            built.append(hair_width(warp(c, p)))
        q2 = q * hw / float(np.median(built))
        for p in out:
            p["q"] = q2
        q = q2
    print(anim, d, "body scale", round(k, 3), "head shrink", round(q, 3))
    return out


def _shrink(p, dist):
    """Horizontal / vertical scale at `dist` px above the neck: 1 at the neck, q from HEAD_BLEND up (smoothstep between)."""
    t = np.clip(dist / HEAD_BLEND, 0, 1)
    return 1 - (1 - p["q"]) * t * t * (3 - 2 * t)


def _lift(p, dist):
    """How far above the neck a point `dist` px above it ends up (integral of _shrink)."""
    ts = np.linspace(0, 1, 65)
    xs = np.asarray(dist, float)[..., None] * ts
    return np.trapezoid(_shrink(p, xs), xs, axis=-1)


def warp(cell, p):
    """The head shrink of fit_params on a placed 192 cell (inverse map, bilinear on premultiplied alpha)."""
    if p["q"] == 1.0:
        return cell
    from scipy import ndimage as nd
    src_d = np.linspace(0, FRAME, 4 * FRAME + 1)
    out_d = _lift(p, src_d)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME].astype(float) + 0.5
    d_o = p["neck"] - yy
    d_s = np.where(d_o > 0, np.interp(d_o, out_d, src_d), d_o)
    r = np.where(d_o > 0, _shrink(p, np.maximum(d_s, 0)), 1.0)
    sy = p["neck"] - d_s - 0.5
    sx = p["cx"] + (xx - p["cx"]) / r - 0.5
    a = np.asarray(cell).astype(float)
    pm = np.dstack([a[..., :3] * a[..., 3:4] / 255, a[..., 3]])
    res = np.dstack([nd.map_coordinates(pm[..., i], [sy, sx], order=1, mode="constant") for i in range(4)])
    al = res[..., 3:4]
    rgb = np.where(al > 0.5, res[..., :3] * 255 / np.maximum(al, 1e-6), 0)
    return Image.fromarray(np.dstack([np.clip(rgb, 0, 255), np.clip(al, 0, 255)]).round().astype(np.uint8), "RGBA")


def raw_to_cell(p, x, y):
    """A raw-frame point through the same scale, placement and head warp as the frame."""
    cx, cy = p["ox"] + x * p["k"], p["oy"] + y * p["k"]
    d = p["neck"] - cy
    if p["q"] != 1.0 and d > 0:
        cx, cy = p["cx"] + (cx - p["cx"]) * float(_shrink(p, d)), p["neck"] - float(_lift(p, d))
    return cx, cy


def fitted(anim, d):
    """New 192 cells of the row (see fit_params)."""
    if (anim, d) in MIRRORS:
        sa, sd, kind = MIRRORS[(anim, d)]
        cells = fitted(sa, sd) if kind == "new" else [old_cell("bare", sa, sd, f) for f in range(COUNT[sa])]
        return [c.transpose(Image.FLIP_LEFT_RIGHT) for c in cells]
    cells = []
    for p in fit_params(anim, d):
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.alpha_composite(p["im"], (p["ox"], p["oy"]))
        cells.append(warp(c, p))
    return cells


def hand_cells(anim, d):
    """Fist layer of a JOBS "old" row: its own backup fist layer through the same transform as its body."""
    _, hands = sources(anim, d)
    cells = []
    for p, h in zip(fit_params(anim, d), hands):
        im = h.resize((max(1, round(h.width * p["k"])), max(1, round(h.height * p["k"]))), Image.LANCZOS)
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.alpha_composite(im, (p["ox"], p["oy"]))
        cells.append(warp(c, p))
    return cells


def fists_of(anim, d):
    """Per frame [(x, y, drawn in front)] of the fist layer."""
    if (anim, d) in MIRRORS:
        sa, sd, _ = MIRRORS[(anim, d)]
        return [[(FRAME - x, y, front) for x, y, front in fr] for fr in fists_of(sa, sd)]
    out = []
    for f, pair in enumerate(FISTS[(anim, d)]):
        fr = []
        for i, p in enumerate(pair):
            if p is None:
                continue
            front = True
            if anim == "attack4":
                front = not (CANNON_LINE[d]["behind"][f] if isinstance(CANNON_LINE[d]["behind"], list) else CANNON_LINE[d]["behind"])
                front = front and (d != "right" or i == 0)  # facing right the far (front) fist is under the barrel
            elif d in SIDE_POSE.get(anim, {}):
                front = not SIDE_POSE[anim][d][("gun", "gunL")[i]]["behind"]
            fr.append((p[0], p[1], front))
        out.append(fr)
    return out


def fist_layer(cell, fists):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for x, y, front in fists:
        if front:
            m |= (xx - x) ** 2 + (yy - y) ** 2 <= FIST_R ** 2
    a = np.asarray(cell).copy()
    a[..., 3] = np.where(m, a[..., 3], 0)
    return Image.fromarray(a)


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
        fists = fists_of(anim, d) if (anim, d) in FISTS or (anim, d) in MIRRORS else [[]] * len(cells)
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
            for x, y, front in fists[f]:
                dr.ellipse(((f * FRAME + x - FIST_R) * z, (y - FIST_R) * z, (f * FRAME + x + FIST_R) * z, (y + FIST_R) * z), outline=(255, 0, 0) if front else (0, 160, 255), width=3)
        w.save(f"{out}/{anim}-{d}-grid.png")
        print(f"{out}/{anim}-{d}-grid.png")


def write_anchors(data, anim, d):
    by = {a["part"]: a for a in data["anchors"] if a["anim"] == anim and a["dir"] == d}
    if (anim, d) in MIRRORS:
        sa, sd, _ = MIRRORS[(anim, d)]
        src = {a["part"]: a for a in data["anchors"] if a["anim"] == sa and a["dir"] == sd}
        for part, other in (("gun", "gunL"), ("gunL", "gun")):
            frames = []
            for fr in src[other]["frames"]:
                m = dict(fr, x=FRAME - fr["x"], angle=-fr["angle"], sx=-fr.get("sx", 1))
                frames.append(m)
            by[part]["frames"] = frames
        return
    if anim == "attack4" and JOBS.get((anim, d)) == "old":
        return  # old row: its anchors were moved once with the head normalisation, nothing to rebuild from
    if anim == "attack4":
        bd = load("bd", "Tools/build-mechanic-dual.py")
        for part in ("gun", "gunL"):
            for fr in by[part]["frames"]:
                fr.update(x=96, y=96, angle=0, sx=1, behind=False, hidden=True)
                fr.pop("art", None)
        art, frac, behind = CANNON_LINE[d]["art"], CANNON_LINE[d]["frac"], CANNON_LINE[d]["behind"]
        for f, (rear, fore) in enumerate(FISTS[(anim, d)]):
            x, y, aim = bd.along(rear, fore, frac[f] if isinstance(frac, list) else frac)
            by["cannon"]["frames"][f] = bd.cannon_anchor(art, x, y, aim, 1, behind[f] if isinstance(behind, list) else behind, CANNON_LINE[d].get("length", 1.0))
        mirror_left(data, anim, d, by)
        return
    pose = SIDE_POSE.get(anim, {}).get(d)
    if JOBS.get((anim, d)) == "old":  # pistols only move with the fists (FISTS converted with the row), pose kept
        pose = None
    for i, part in enumerate(("gun", "gunL")):
        for f, fr in enumerate(by[part]["frames"]):
            p = FISTS[(anim, d)][f][i]
            if p is None:
                fr.update(hidden=True)
                continue
            fr.update(x=p[0], y=p[1], hidden=False)
            if pose:
                fr.pop("art", None)
                fr.update(angle=pose[part]["angle"], sx=pose[part]["sx"], behind=pose[part]["behind"])
    if anim == "attack3":
        mirror_left(data, anim, d, by)


def mirror_left(data, anim, d, by):
    """The game draws attack3 / attack4 left as the right row mirrored (HeroClassDef.MirrorLeft): same pose, mirrored anchors."""
    if d != "right":
        return
    left = {a["part"]: a for a in data["anchors"] if a["anim"] == anim and a["dir"] == "left"}
    for part in ("gun", "gunL", "cannon"):
        left[part]["frames"] = [dict(fr, x=round(FRAME - fr["x"], 2), angle=-fr["angle"], sx=-fr.get("sx", 1)) for fr in by[part]["frames"]]


def write(jobs):
    data = json.load(open(GEAR_DUAL, encoding="utf-8"))
    for anim in sorted({a for a, _ in jobs}):
        ensure_backup(anim)
    for anim, d in jobs:
        old_row = JOBS.get((anim, d)) == "old"
        cells = fitted(anim, d)
        fists = None if old_row else fists_of(anim, d)
        hands = hand_cells(anim, d) if old_row else None
        r = ROWS[d]
        for layer in ("bare", "hand"):
            path = f"{BODY}/{layer}/{anim}.png"
            sheet = Image.open(path).convert("RGBA")
            sheet.paste(Image.new("RGBA", (FRAME * COUNT[anim], FRAME), (0, 0, 0, 0)), (0, r * FRAME))
            for f, c in enumerate(cells):
                sheet.paste(c if layer == "bare" else hands[f] if old_row else fist_layer(c, fists[f]), (f * FRAME, r * FRAME))
            sheet.save(path)
        write_anchors(data, anim, d)
        refresh_pre_head(anim, r)
        print(anim, d, "written")
    json.dump(data, open(GEAR_DUAL, "w", encoding="utf-8"), separators=(",", ":"))
    head_layer(sorted({a for a, _ in jobs}))


# Hairstyle head layer (docs/sprite-rules.md "Hình mẫu nhân vật"): body/headless/<anim>.png + head anchors, always cut from the sheets this
# tool just wrote (after NORMALIZE), so rerunning the repaint never leaves the headless sheets on an older pose.
PRE_HEAD = "Assets/ArtLibrary/classes/mechanic/body/hand/{}.pre-head.png"


def refresh_pre_head(anim, r):
    """build-head-layer.py anims-write rebuilds hand/<anim>.png from <anim>.pre-head.png (+ arm pixels over the head): put the fist row this
    tool just wrote into that copy, or the next head-layer run would bring back the old fist layer."""
    path, pre = f"{BODY}/hand/{anim}.png", PRE_HEAD.format(anim)
    os.makedirs(os.path.dirname(pre), exist_ok=True)
    hand = Image.open(path).convert("RGBA")
    if not os.path.exists(pre) or Image.open(pre).size != hand.size:
        hand.save(pre)
        return
    base = Image.open(pre).convert("RGBA")
    base.paste(hand.crop((0, r * FRAME, hand.width, (r + 1) * FRAME)), (0, r * FRAME))
    base.save(pre)


def head_layer(anims):
    """Headless sheets, head anchors (4 facings x every frame) and arm-over-head pixels of these anims, head at idle size."""
    import subprocess
    names = ",".join(anims)
    for args in (("anims-write", names), ("anims-normscale", names)):
        subprocess.run([sys.executable, "Tools/build-head-layer.py", "mechanic", *args], check=True)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "grid":
        grid(sys.argv[2], parse(sys.argv[3:]))
    elif cmd == "write":
        write(parse(sys.argv[2:]))
    elif cmd == "head":  # head layer only, from the sheets as they are (default: attack1-4)
        for a in sys.argv[2:] or COUNT:
            ensure_backup(a)
            if not os.path.exists(PRE_HEAD.format(a)):
                refresh_pre_head(a, 0)
        head_layer(sys.argv[2:] or list(COUNT))
    else:
        print(__doc__)

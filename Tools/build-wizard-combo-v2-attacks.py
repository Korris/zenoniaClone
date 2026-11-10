"""Wizard combo v2 (10/11/2026): attack1-4 facing DOWN redrawn by ChatGPT as real staff strikes (board in
Assets/ArtLibrary/classes/wizard/attack-board/northwind-combo-v2/). Each ChatGPT image has two rows: the top row WITH the staff (to read its
angle), the bottom row the same poses BALD with empty fists (installed). Reuses Tools/build-wizard-northwind-attacks.py (fit by chin -> sole,
bald head -> head anchor + headless sheet, fist layer, armed sheet) with its own frame counts, ORDER and poses (Tools/wizard_combo_v2_poses.py).
The other facings keep their old frames: the sheets, head anchors and staff / book anchors of up / left / right are widened to the new frame
count by repeating their last frame (`migrate`, run by `write`).

  py Tools/build-wizard-combo-v2-attacks.py rows                  split every hitN image into hitN-down-staff.png / hitN-down-bald.png
  py Tools/build-wizard-combo-v2-attacks.py angles                staff angle per painted frame, read on the staff row (paste into POSES)
  py Tools/build-wizard-combo-v2-attacks.py grid OUT_DIR          fitted bald frames 4x with the fists (red = right / staff, cyan = left)
  py Tools/build-wizard-combo-v2-attacks.py preview OUT_DIR       composed like the game (hair 0, T0 staff / book), painted frame order
  py Tools/build-wizard-combo-v2-attacks.py write                 sheets + anchors (down rows), old copies in northwind-combo-v2/old/
  py Tools/build-wizard-combo-v2-attacks.py check OUT.png [dirs]  hairstyles 0, 2, 4 x every frame x facings (default down), as the game draws
  py Tools/build-wizard-combo-v2-attacks.py cast-tips             C# table for HeroFx.WizardCastTip
The project tools hard-code D:/zenoniaClone; this one patches that to its own repo while loading them.
"""
import importlib.util, json, os, re, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
os.chdir(ROOT)
sys.path.insert(0, f"{ROOT}/Tools")


def _load(name, path):
    """A project tool with its hard-coded root replaced by this repo; its own loaders replaced by this one (nested tools get patched too)."""
    src = open(path, encoding="utf-8").read().replace('ROOT = "D:/zenoniaClone"', f'ROOT = "{ROOT}"')
    src = re.sub(r"^def (_?load)\(", r"def \1_unpatched(", src, flags=re.M)
    m = importlib.util.module_from_spec(importlib.util.spec_from_file_location(name, path))
    m.__dict__["_load"] = _load; m.__dict__["load"] = _load
    exec(compile(src, path, "exec"), m.__dict__)
    return m


sys.modules["wizard_attack_compose"] = _load("wizard_attack_compose", f"{ROOT}/Tools/wizard_attack_compose.py")  # `import`ed by the base tool
nwa = _load("nwa", f"{ROOT}/Tools/build-wizard-northwind-attacks.py")
wb, nw = nwa.wb, nwa.nw
import wizard_combo_v2_poses as cv

F = 192
SRC = "Assets/ArtLibrary/classes/wizard/attack-board/northwind-combo-v2"
ANIMS = ["attack1", "attack2", "attack3", "attack4"]
JOBS = [(a, "down") for a in ANIMS]
nwa.SRC, nwa.OLD = SRC, f"{SRC}/old"
wb.COUNT, wb.ORDER, wb.MIRRORS = cv.COUNT, cv.ORDER, {}
wb.POSES, wb.FIST_FIX, wb.CAST_FRAME = cv.POSES, {}, cv.CAST_FRAME
wb.JOBS = {(a, "down"): f"{SRC}/hit{i}-down-bald.png" for i, a in enumerate(ANIMS, 1)}
nwa.OLD_FIST, nwa.NW_FIST_FIX = {}, cv.FIST_FIX
_anchors_of = wb.anchors_of


def anchors_of(anim, d):
    """wb.anchors_of plus the v2 pose fields: a hidden book, a planted staff (`at`)."""
    an = _anchors_of(anim, d)
    for f, p in enumerate(cv.POSES[(anim, d)]):
        an["book"][f]["hidden"] = bool(p["book"].get("hidden"))
        if p["staff"].get("at"):
            an["staff"][f]["x"], an["staff"][f]["y"] = p["staff"]["at"]
        if p["staff"].get("slide"):                              # grip moved along the shaft toward the crystal: the butt ends at the fist
            th = np.radians(an["staff"][f]["angle"]); k = p["staff"]["slide"]
            an["staff"][f]["x"] = round(an["staff"][f]["x"] + k * float(np.sin(th)), 1)
            an["staff"][f]["y"] = round(an["staff"][f]["y"] - k * float(np.cos(th)), 1)
    return an


wb.anchors_of = anchors_of


def hand_cells(anim, d):
    """Fist discs at the FISTS (wb.hand_cells takes the anchor points, which a planted staff moves off the hands): the right fist when the
    staff is in front, the left fist unless the book is behind (hidden book = both hands on the staff)."""
    an = anchors_of(anim, d); fs = wb.fists_of(anim, d)
    out = []
    for f, c in enumerate(wb.fitted(anim, d)):
        pts = ([fs[f]["R"]] if not an["staff"][f]["behind"] else []) + ([fs[f]["L"]] if not an["book"][f]["behind"] else [])
        out.append(wb.fist_layer(c, pts))
    return out


wb.hand_cells = hand_cells


def head_blob(cell):
    """The drawn bald head = the BIGGEST skin blob (nwa takes the highest: fists raised over the head won the slam wind-up)."""
    a = np.asarray(cell); sk = nwa.skin(a)
    lab, n = ndimage.label(sk)
    if n == 0:
        return np.zeros(sk.shape, bool)
    return lab == (1 + int(np.argmax(ndimage.sum(sk, lab, range(1, n + 1)))))


nwa.head_blob = head_blob


def pieces_cols(path, count):
    """The painted frames by empty columns (1 px gap is enough): wide arms of neighbouring frames come within the 12 px that wb.pieces joins.
    Fewer runs than frames: the widest runs are halved; more: the smallest are dropped."""
    fig = wb.key_green(path)
    al = np.asarray(fig)[..., 3] > 100
    runs = _runs(al.sum(0) > 0)
    while len(runs) > count:
        runs.remove(min(runs, key=lambda r: r[1] - r[0]))
    while len(runs) < count:
        s0, e0 = max(runs, key=lambda r: r[1] - r[0]); i = runs.index((s0, e0)); m = (s0 + e0) // 2
        runs[i:i + 1] = [(s0, m), (m, e0)]
    out = []
    for s0, e0 in runs:
        im = fig.crop((s0, 0, e0, fig.height))
        out.append(im.crop(im.getbbox()))
    return out


wb.pieces = pieces_cols


def fitted(anim, d):
    """nwa.fitted with the LAST painted frame (nearly idle in every v2 strip) setting the body scale: the tallest frame of a strip is the
    wind-up on tiptoe (hit 3), which shrank the whole row."""
    key = (anim, d)
    if key in nwa._CACHE:
        return nwa._CACHE[key]
    figs = wb.pieces(wb.JOBS[key], wb.COUNT[anim])
    k = nwa.BODY_H / float(figs[-1].height - 1 - nwa.drawn_chin(figs[-1]))
    fx, sole = nwa.idle_feet(d)
    cells = []
    for fg in figs:
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        _, fb = wb.top_bot(im)
        c = Image.new("RGBA", (F, F)); c.alpha_composite(im, (int(round(fx - wb.feet_x(im))), int(sole - fb)))
        cells.append(c)
    print(f"{anim} {d}: scale {k:.3f} (last frame)")
    nwa._CACHE[key] = cells
    return cells


nwa.fitted = wb.fitted = fitted


def stacked_fists(anim, frames):
    """Both fists on one vertical staff merge into one skin blob: split it at its median row (R = lower fist, L = upper)."""
    out = {}
    for f in frames:
        c = fitted(anim, "down")[f]
        a = np.asarray(c); sk = nwa.skin(a) & ~ndimage.binary_dilation(nwa.head_blob(c), iterations=2)
        lab, n = ndimage.label(ndimage.binary_closing(sk, iterations=1))
        if n == 0:
            continue
        big = lab == (1 + np.argmax(ndimage.sum(sk, lab, range(1, n + 1))))
        ys, xs = np.nonzero(big); mid = np.median(ys)
        lo, hi = (xs[ys >= mid], ys[ys >= mid]), (xs[ys < mid], ys[ys < mid])
        out[f] = ((round(float(lo[0].mean()), 1), round(float(lo[1].mean()), 1)), (round(float(hi[0].mean()), 1), round(float(hi[1].mean()), 1)))
    return out


def rows():
    """Split each two-row ChatGPT image at the widest green band between the rows."""
    for i in range(1, 5):
        src = f"{SRC}/wizard-combo-v2-hit{i}-down-chatgpt-1.png"
        if not os.path.exists(src):
            print("missing", src); continue
        im = Image.open(src).convert("RGB"); a = np.asarray(im).astype(int)
        fg = ~((a[..., 1] > 180) & (a[..., 0] < 110) & (a[..., 2] < 110))
        occ = fg.sum(1) > 2
        ys = np.nonzero(occ)[0]
        gaps = [(s, e) for s, e in _runs(~occ) if ys.min() < s < ys.max()]
        s, e = max(gaps, key=lambda g: g[1] - g[0]); cut = (s + e) // 2
        im.crop((0, 0, im.width, cut)).save(f"{SRC}/hit{i}-down-staff.png")
        im.crop((0, cut, im.width, im.height)).save(f"{SRC}/hit{i}-down-bald.png")
        print(f"hit{i}: cut at row {cut} (gap {s}-{e})")


def _runs(mask):
    out, start = [], None
    for y, v in enumerate(mask):
        if v and start is None: start = y
        if not v and start is not None: out.append((start, y)); start = None
    if start is not None: out.append((start, len(mask)))
    return out


def staff_angle(fig):
    """Clockwise degrees (0 = crystal up) of the painted staff: PCA axis of the tan wood, the crystal end = the end with more bright blue."""
    a = np.asarray(fig).astype(int); r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    wood = (al > 128) & (r > 135) & (g > 85) & (b < 125) & (r - b > 55) & (r - g > 20) & ~((r > 200) & (g > 150) & (b > 110))
    lab, n = ndimage.label(ndimage.binary_dilation(wood, iterations=4))
    if n == 0:
        return None
    big = lab == (1 + np.argmax(ndimage.sum(wood, lab, range(1, n + 1))))
    ys, xs = np.nonzero(wood & big)
    pts = np.stack([xs, ys], 1).astype(float); c = pts.mean(0)
    _, _, vt = np.linalg.svd(pts - c, full_matrices=False); ax = vt[0]
    ice = (al > 128) & (b > 190) & (g > 150) & (r < 170)
    proj = (pts - c) @ ax; ends = [c + ax * proj.min(), c + ax * proj.max()]
    yy, xx = np.nonzero(ice)
    score = [((xx - e[0]) ** 2 + (yy - e[1]) ** 2 < 40 ** 2).sum() for e in ends]
    tip = ends[int(np.argmax(score))]; v = tip - c
    return float(np.degrees(np.arctan2(v[0], -v[1])))


def angles():
    for i, anim in enumerate(ANIMS, 1):
        if not os.path.exists(f"{SRC}/hit{i}-down-staff.png"):
            continue
        figs = wb.pieces(f"{SRC}/hit{i}-down-staff.png", cv.COUNT[anim])
        print(anim, [None if (x := staff_angle(fg)) is None else round(x) for fg in figs])


def migrate():
    """Widen the sheets / anchors of the facings that are not redrawn to the new frame count (last frame repeated)."""
    heads = json.load(open(nwa.HEAD_JSON, encoding="utf-8")); melee = json.load(open(wb.MELEE, encoding="utf-8"))
    for anim in ANIMS:
        n = cv.COUNT[anim]
        for layer in ("bare", "hand", "armed", "headless"):
            p = f"{nwa.BODY}/{layer}/{anim}.png"
            if not os.path.exists(p):
                continue
            im = Image.open(p).convert("RGBA"); old_n = im.width // F
            if old_n == n:
                continue
            out = Image.new("RGBA", (n * F, 4 * F))
            for f in range(n):
                sf = min(f, old_n - 1)
                out.paste(im.crop((sf * F, 0, (sf + 1) * F, 4 * F)), (f * F, 0))
            out.save(p); print("widened", p, old_n, "->", n)
        for e in heads["anims"]:
            if e["anim"] == anim and len(e["frames"]) not in (0, n):
                e["frames"] = [e["frames"][min(f, len(e["frames"]) - 1)] for f in range(n)]
        for e in melee["anchors"]:
            if e["anim"] == anim and len(e["frames"]) not in (0, n):
                e["frames"] = [e["frames"][min(f, len(e["frames"]) - 1)] for f in range(n)]
    json.dump(heads, open(nwa.HEAD_JSON, "w", encoding="utf-8"), indent=2)
    with open(wb.MELEE, "w", encoding="utf-8") as fh:
        json.dump(melee, fh, indent=1); fh.write("\n")


def cast_tips():
    """C# table for HeroFx.WizardCastTip like wb.cast_tips (world units from the feet at the release frame, 8 frame px = 1 unit): hits 1 and 3
    the T0 staff crystal, hit 2 the book's centre (frost crescent leaves the staff fist: the staff crystal too), hit 4 the planted staff's crystal
    (wb: between the fists, which put the finisher's flash over the body). Facings without a v2 strip read their anchors from held-wizard-melee.json."""
    import math
    data = json.load(open(wb.MELEE, encoding="utf-8"))
    stored = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in data["anchors"]}
    hw = json.load(open("Assets/Resources/Data/classes/wizard/held-wizard.json", encoding="utf-8"))
    staff = hw["weapon"][0]
    art = np.asarray(Image.open(f"Assets/Resources/Sprites/classes/wizard/weapons/staff/{staff['name']}-base.png").convert("RGBA")).astype(int)
    blue = (art[..., 3] > 200) & (art[..., 2] > art[..., 0] + 60)
    ys, xs = np.nonzero(blue[:120])
    crystal = (xs.mean() - staff["baseGrip"]["gripX"], ys.mean() - staff["baseGrip"]["gripY"])

    def tip(a, v):
        th = math.radians(a["angle"])
        dx, dy = v[0] * a["s"] * a.get("sx", 1), v[1] * a["s"]
        return a["x"] + dx * math.cos(th) - dy * math.sin(th), a["y"] + dx * math.sin(th) + dy * math.cos(th)

    rows = []
    for anim in ANIMS:
        cells = []
        for d in ("down", "up", "left", "right"):
            an = anchors_of(anim, d)["staff"] if (anim, d) in wb.JOBS else stored[("staff", anim, d)]
            x, y = tip(an[min(cv.CAST_FRAME, len(an) - 1)], crystal)
            cells.append(f"new Vector2({(x - 96) / 8:.1f}f, {(192 - y) / 8:.1f}f)")
        rows.append("{ " + ", ".join(cells) + " }")
    nl = chr(10)
    print("static readonly Vector2[][] WizardCastTip = {" + nl + "    " + ("," + nl + "    ").join(rows) + nl + "};")


def check(out, styles=(0, 2, 4), dirs=("down",)):
    """nwa.check for the given facings, wide enough for 8 frames (nwa's sheet is 6 cells wide)."""
    rows = [[nwa.compose(anim, d, f, st) for f in range(wb.COUNT[anim])] for anim in ANIMS for d in dirs for st in styles]
    W = F + 48
    sheet = Image.new("RGBA", (W * max(len(r) for r in rows), W * len(rows)), (58, 62, 70, 255))
    for i, row in enumerate(rows):
        for j, im in enumerate(row):
            sheet.alpha_composite(im, (j * W, i * W))
    sheet.convert("RGB").save(out); print(out, sheet.size)


def preview(out):
    os.makedirs(out, exist_ok=True)
    wb.preview(f"{out}/preview-down.png", JOBS)


def write():
    os.makedirs(nwa.OLD, exist_ok=True)
    for anim in ANIMS:                                   # once: the sheets and anchors before v2
        for layer in ("bare", "hand", "armed", "headless"):
            p = f"{nwa.BODY}/{layer}/{anim}.png"
            if os.path.exists(p) and not os.path.exists(f"{nwa.OLD}/{layer}-{anim}.png"):
                shutil.copyfile(p, f"{nwa.OLD}/{layer}-{anim}.png")
    for p in (nwa.HEAD_JSON, wb.MELEE):
        if not os.path.exists(f"{nwa.OLD}/{os.path.basename(p)}"):
            shutil.copyfile(p, f"{nwa.OLD}/{os.path.basename(p)}")
    migrate()
    nwa.write(JOBS)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "rows": rows()
    elif cmd == "angles": angles()
    elif cmd == "grid": wb.grid(sys.argv[2], JOBS)
    elif cmd == "stacked": print(stacked_fists(sys.argv[2], [int(x) for x in sys.argv[3:]]))
    elif cmd == "preview": preview(sys.argv[2])
    elif cmd == "write": write()
    elif cmd == "check": check(sys.argv[2], dirs=tuple(sys.argv[3].split(",")) if len(sys.argv) > 3 else ("down",))
    elif cmd == "cast-tips": cast_tips()
    else: print(__doc__)

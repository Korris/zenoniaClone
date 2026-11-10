"""Wizard `walk` (4 facings, 4 frames) in the Northwind T0 outfit with the layered head (10/11/2026), after the combo v2 pipeline.
ChatGPT draws one image per facing pair, FOUR rows of 4 frames (facing A with the staff, facing A bald + empty fists, facing B with the
staff, facing B bald): Assets/ArtLibrary/classes/wizard/walk-board/northwind-walk/wizard-northwind-walk-<a>-<b>-chatgpt-N.png.
Only the bald rows are installed; the staff rows give the staff angle (`angles`) and which fist holds it.
Reuses Tools/build-wizard-northwind-attacks.py (fit by chin -> sole = idle's 95 px, bald head -> head anchor + headless sheet, fist layer,
armed sheet, staff / book anchors) with the walk's frame count and poses; the book is hidden while walking (as before).

  py Tools/build-wizard-northwind-walk.py rows                 split the 4-row images into walk-<dir>-staff.png / walk-<dir>-bald.png
  py Tools/build-wizard-northwind-walk.py angles               painted staff angle per frame and facing (paste into POSES)
  py Tools/build-wizard-northwind-walk.py grid OUT_DIR         fitted bald frames 4x with the fists (red = right / staff, cyan = left)
  py Tools/build-wizard-northwind-walk.py write [dir ...]      sheets + anchors (old copies once in northwind-walk/old/)
  py Tools/build-wizard-northwind-walk.py check OUT.png        hairstyles 0, 2, 4 x 4 frames x 4 facings, composed as the game draws
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


sys.modules["wizard_attack_compose"] = _load("wizard_attack_compose", f"{ROOT}/Tools/wizard_attack_compose.py")
nwa = _load("nwa", f"{ROOT}/Tools/build-wizard-northwind-attacks.py")
wb, nw = nwa.wb, nwa.nw
from wizard_combo_v2_poses import _p  # noqa: E402

F = 192
SRC = "Assets/ArtLibrary/classes/wizard/walk-board/northwind-walk"
DIRS = ["down", "up", "left", "right"]
PAIRS = [("down", "up"), ("right", "left")]                  # the two ChatGPT images, 4 rows each
IMAGES = {p: f"{SRC}/wizard-northwind-walk-{p[0]}-{p[1]}-chatgpt-1.png" for p in PAIRS}
JOBS = [("walk", d) for d in DIRS]
nwa.SRC, nwa.OLD, nwa.ANIMS = SRC, f"{SRC}/old", ["walk"]
wb.COUNT, wb.ORDER, wb.MIRRORS = {"walk": 4}, {"walk": [0, 1, 2, 3]}, {}
wb.FIST_FIX, nwa.OLD_FIST, nwa.NW_FIST_FIX = {}, {}, {}
wb.JOBS = {("walk", d): f"{SRC}/walk-{d}-bald.png" for d in DIRS}
# Staff angle (clockwise, 0 = crystal up) and side per facing, as the old walk anchors (held-wizard-melee.json before 10/11): the staff hangs
# in the right hand, behind the body when that hand is the far one (up, left). Replace the angles with the `angles` output if they differ.
# Painted angles (`angles`, 10/11): down -4/1/-5/-5, up 4/20/21/1, right 9/13/19/12, left 20/24/19/26; smoothed to a small swing with the arm.
wb.POSES = {("walk", "down"): [_p(a, book_hidden=True) for a in (-5, -2, -5, -2)],
            ("walk", "up"): [_p(a, book_hidden=True, staff_behind=True) for a in (6, 14, 14, 6)],
            ("walk", "left"): [_p(a, book_hidden=True, staff_behind=True) for a in (-22, -18, -22, -18)],   # staff at the forward fist behind the body, tilted forward as idle left: painted +20 (leaning back) hid the crystal behind the head
            ("walk", "right"): [_p(a, book_hidden=True) for a in (10, 14, 16, 12)]}
# (dir) -> {frame: ((right fist x, y), (left fist x, y))} frame px after `fit`, read on the `grid` images when the automatic fists are wrong
FIST_FIX = {"up": {0: ((132.0, 124.0), (66.0, 131.0)), 2: ((130.0, 124.0), (58.0, 132.3))}}   # up 0, 2: the staff hand swings toward the viewer, hidden behind its cuff
nwa.NW_FIST_FIX = {("walk", d): v for d, v in FIST_FIX.items()}
_anchors_of = wb.anchors_of


def anchors_of(anim, d):
    an = _anchors_of(anim, d)
    for f, p in enumerate(wb.POSES[(anim, d)]):
        an["book"][f]["hidden"] = bool(p["book"].get("hidden"))
    return an


def hand_cells(anim, d):
    """Fist discs at the fists whose item is drawn in front (the staff fist; the book is hidden so never the left one)."""
    an = anchors_of(anim, d); fs = wb.fists_of(anim, d)
    out = []
    for f, c in enumerate(wb.fitted(anim, d)):
        pts = ([fs[f]["R"]] if not an["staff"][f]["behind"] else []) + ([fs[f]["L"]] if not (an["book"][f]["behind"] or an["book"][f]["hidden"]) else [])
        out.append(wb.fist_layer(c, pts))
    return out


def head_blob(cell):
    """The drawn bald head = the biggest skin blob."""
    a = np.asarray(cell); sk = nwa.skin(a)
    lab, n = ndimage.label(sk)
    if n == 0:
        return np.zeros(sk.shape, bool)
    return lab == (1 + int(np.argmax(ndimage.sum(sk, lab, range(1, n + 1)))))


def pieces_cols(path, count):
    """The painted frames by empty columns; fewer runs than frames: the widest runs are halved; more: the smallest are dropped."""
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


_current_dir = ["down"]
_fists_of = nwa.fists_of


def fists_of(anim, d):
    _current_dir[0] = d
    return _fists_of(anim, d)


def auto_fists(cell):
    """nwa.auto_fists without the nape / neck skin (the walk never lifts a hand above the chest: fists are below row 110)."""
    a = np.asarray(cell)
    m = nwa.skin(a); hb = ndimage.binary_dilation(head_blob(cell), iterations=1)
    min_y = 120 if _current_dir[0] == "up" else 110                       # up: the nape skin under the collar is bigger than the fists
    lab, n = ndimage.label(ndimage.binary_closing(m & ~hb, iterations=1))
    out = []
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(ys) < 20 or ys.mean() > 165 or ys.mean() < min_y:
            continue
        out.append((round(float(xs.mean()), 1), round(float(ys.mean()), 1), len(ys)))
    out.sort(key=lambda t: -t[2])
    return sorted([(x, y) for x, y, _ in out[:2]], key=lambda t: t[0])


wb.anchors_of, wb.hand_cells, nwa.head_blob, wb.pieces, nwa.auto_fists = anchors_of, hand_cells, head_blob, pieces_cols, auto_fists
wb.fists_of = nwa.fists_of = fists_of


def _runs(mask):
    out, start = [], None
    for y, v in enumerate(mask):
        if v and start is None: start = y
        if not v and start is not None: out.append((start, y)); start = None
    if start is not None: out.append((start, len(mask)))
    return out


def rows():
    """Split each 4-row ChatGPT image at the 3 widest green bands between the rows: staff / bald rows of facing A, then of facing B."""
    for (a, b), src in IMAGES.items():
        if not os.path.exists(src):
            print("missing", src); continue
        im = Image.open(src).convert("RGB"); arr = np.asarray(im).astype(int)
        fg = ~((arr[..., 1] > 180) & (arr[..., 0] < 110) & (arr[..., 2] < 110))
        occ = fg.sum(1) > 2
        ys = np.nonzero(occ)[0]
        gaps = sorted([(s, e) for s, e in _runs(~occ) if ys.min() < s < ys.max()], key=lambda g: g[0] - g[1])[:3]
        cuts = sorted((s + e) // 2 for s, e in gaps)
        assert len(cuts) == 3, f"{src}: {len(cuts)} row gaps"
        bounds = [0] + cuts + [im.height]
        names = [f"walk-{a}-staff", f"walk-{a}-bald", f"walk-{b}-staff", f"walk-{b}-bald"]
        for i, n in enumerate(names):
            im.crop((0, bounds[i], im.width, bounds[i + 1])).save(f"{SRC}/{n}.png")
        print(os.path.basename(src), "cut at", cuts)


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
    for d in DIRS:
        p = f"{SRC}/walk-{d}-staff.png"
        if os.path.exists(p):
            print(d, [None if (x := staff_angle(fg)) is None else round(x) for fg in pieces_cols(p, 4)])


def check(out, styles=(0, 2, 4)):
    rows_ = [[nwa.compose("walk", d, f, st) for f in range(4)] for d in DIRS for st in styles]
    W = F + 48
    sheet = Image.new("RGBA", (W * 4, W * len(rows_)), (58, 62, 70, 255))
    for i, row in enumerate(rows_):
        for j, im in enumerate(row):
            sheet.alpha_composite(im, (j * W, i * W))
    sheet.convert("RGB").save(out); print(out, sheet.size)


def write(dirs):
    jobs = [("walk", d) for d in dirs]
    os.makedirs(nwa.OLD, exist_ok=True)
    for layer in ("bare", "hand", "armed", "headless"):
        p = f"{nwa.BODY}/{layer}/walk.png"
        if os.path.exists(p) and not os.path.exists(f"{nwa.OLD}/{layer}-walk.png"):
            shutil.copyfile(p, f"{nwa.OLD}/{layer}-walk.png")
    for p in (nwa.HEAD_JSON, wb.MELEE):
        if not os.path.exists(f"{nwa.OLD}/{os.path.basename(p)}"):
            shutil.copyfile(p, f"{nwa.OLD}/{os.path.basename(p)}")
    nwa.write(jobs)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "rows": rows()
    elif cmd == "angles": angles()
    elif cmd == "grid": wb.grid(sys.argv[2], [j for j in JOBS if os.path.exists(wb.JOBS[j])])
    elif cmd == "write": write(sys.argv[2:] or [d for d in DIRS if os.path.exists(wb.JOBS[("walk", d)])])
    elif cmd == "check": check(sys.argv[2])
    else: print(__doc__)

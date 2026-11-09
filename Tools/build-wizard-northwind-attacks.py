"""Wizard attack1-4 (4 facings) in the Northwind T0 outfit with the layered head (heads/face + hair, as idle).

ChatGPT redrew every old attack strip (ArtLibrary/classes/wizard/attack-board/body/) frame by frame in the Northwind outfit, BALD, empty fists
(sources: ArtLibrary/classes/wizard/attack-board/northwind-body/<anim>-<dir>-chatgpt-1.png, same poses). This tool reuses
Tools/build-wizard-attack-board-repaint.py for the staff / book anchors (POSES), the fist layer, the armed sheet and the frame ORDER / MIRRORS,
and replaces two steps:
  fit   one scale per row so the drawn bald head is as wide as the game head (heads/face at s 0.833, rule 8), soles on idle's sole row,
        feet on idle's feet column; no head warp (the old tool shrank a haired head to idle's hair width).
  head  per frame the face template (s 0.833, left = right mirrored) is registered on the drawn bald head (move + tilt), giving the head anchor
        of head-anchors.json; the drawn head is removed from the body (template grown 2 px; navy robe and lower collar fur kept) -> headless.
        bare = headless + face + hair 0 (default dye), the fallback / portrait art.

  py Tools/build-wizard-northwind-attacks.py grid OUT_DIR [anim-dir ...]    fitted frames 4x with fists (red = right / staff, cyan = left / book)
  py Tools/build-wizard-northwind-attacks.py write [anim-dir ...]           every sheet + anchor (old copies once in northwind-body/old/)
  py Tools/build-wizard-northwind-attacks.py check OUT.png                  composed like the game: hairstyles 0, 2, 4 x every frame x 4 facings
"""
import importlib.util, json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
sys.path.insert(0, "Tools")


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


wb = _load("wb", f"{ROOT}/Tools/build-wizard-attack-board-repaint.py")      # old attack tool (poses, fists, write)
nw = _load("nw", f"{ROOT}/Tools/build-wizard-northwind-idle-dirs.py")       # idle tool (head anchors, face / hair compose)

F = 192
SRC = "Assets/ArtLibrary/classes/wizard/attack-board/northwind-body"
OLD = f"{SRC}/old"
BODY, HEADS = wb.BODY, nw.HEADS
HEAD_JSON = nw.HEAD_JSON
S = nw.SIDE_S                                                              # head scale of every facing (as idle)
ANIMS = ["attack1", "attack2", "attack3", "attack4"]
wb.JOBS = {k: f"{SRC}/{os.path.basename(v)}" for k, v in wb.JOBS.items()}
OLD_FIST = dict(wb.FIST_FIX)                                               # fists read on the old strips (same poses): R / L hints
wb.FIST_FIX = {}                                                           # new art: fists re-read below (NW_FIST_FIX)
NW_FIST_FIX = {}                                                           # (anim, dir) -> {frame: ((right x, y), (left x, y))} in painted order


def template(d):
    """Face template alpha of a facing at head scale S, unmoved (left = right mirrored), and its skin mask."""
    f = Image.open(f"{HEADS}/face/{'right' if d == 'left' else d}.png").convert("RGBA")
    if d == "left":
        f = f.transpose(Image.FLIP_LEFT_RIGHT)
    return f


def placed(img, a):
    """Cell moved like HeroHeadLayer: scale s / s*sy about the bottom centre, rotate clockwise by angle about it, then move x, y."""
    s = a.get("s") or 1.0; sy = s * (a.get("sy") or 1.0); th = np.radians(a.get("angle", 0.0)); c, n = np.cos(th), np.sin(th)
    px, py = F / 2, float(F)
    A, B = c / s, n / s
    D, E = -n / sy, c / sy
    C = px - A * (px + a["x"]) - B * (py + a["y"])
    Fo = py - D * (px + a["x"]) - E * (py + a["y"])
    return img.transform((F, F), Image.AFFINE, (A, B, C, D, E, Fo), Image.BILINEAR)


def skin(a):
    r, g, b, al = (a[..., i].astype(int) for i in range(4))
    return (al > 128) & (r > 200) & (g > 140) & (b > 100) & (r - b > 30) & (g < 240)


def skull_width(im):
    """Width of the bald scalp skin (the highest big skin blob): fists raised beside the head do not count."""
    xs = np.nonzero(head_blob(im).any(0))[0]
    return float(xs[-1] - xs[0] + 1) if len(xs) else 1.0


def head_width_src(fig):
    return skull_width(fig)


def game_head_width(d):
    return skull_width(placed(template(d), {"x": 0, "y": 0, "s": S}))


def idle_feet(d):
    hl = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA"))[wb.ROWS[d] * F:(wb.ROWS[d] + 1) * F, :F]
    return wb.feet_x(Image.fromarray(hl)), nw.SOLE


_CACHE = {}


def fitted(anim, d):
    """Painted frames scaled so the bald head matches the game head, soles on idle's sole row, feet on idle's feet (mirrored rows mirror)."""
    key = (anim, d)
    if key in _CACHE:
        return _CACHE[key]
    if key in wb.MIRRORS:
        _CACHE[key] = [c.transpose(Image.FLIP_LEFT_RIGHT) for c in fitted(*wb.MIRRORS[key])]
        return _CACHE[key]
    figs = wb.pieces(wb.JOBS[key], wb.COUNT[anim])
    k = game_head_width(d) / float(np.median([head_width_src(fg) for fg in figs]))
    fx, sole = idle_feet(d)
    cells = []
    for fg in figs:
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        _, fb = wb.top_bot(im)
        c = Image.new("RGBA", (F, F)); c.alpha_composite(im, (int(round(fx - wb.feet_x(im))), int(sole - fb)))
        cells.append(c)
    print(f"{anim} {d}: scale {k:.3f}")
    _CACHE[key] = cells
    return cells


wb.fitted = fitted


def fists_of(anim, d):
    key = (anim, d)
    if key in wb.MIRRORS:
        src = fists_of(*wb.MIRRORS[key])
        return [{"R": (F - f["L"][0], f["L"][1]), "L": (F - f["R"][0], f["R"][1])} for f in src]
    out = []
    for f, c in enumerate(fitted(anim, d)):
        fix = NW_FIST_FIX.get(key, {}).get(f)
        if fix:
            out.append({"R": fix[0], "L": fix[1]}); continue
        hb = ndimage.binary_dilation(head_blob(c), iterations=2)            # never the bald head (the up view has no face to drop)
        pts = [p for p in auto_fists(c) if not hb[int(p[1]), int(p[0])]]
        hint = OLD_FIST.get(key, {}).get(f)
        if hint:                                                       # the old fists tell near / far: nearest detected point, else the old point
            dist = lambda p, q: (p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2
            R = min(pts, key=lambda p: dist(p, hint[0])) if pts else hint[0]
            rest = [p for p in pts if p != R]
            L = min(rest, key=lambda p: dist(p, hint[1])) if rest else hint[1]
            R = R if dist(R, hint[0]) < 400 else hint[0]
            L = L if dist(L, hint[1]) < 400 else hint[1]
            out.append({"R": R, "L": L}); continue
        pts = (pts + [(96.0, 120.0)] * 2)[:2]
        pts.sort(key=lambda t: t[0])
        out.append({"R": pts[1], "L": pts[0]} if d in ("up", "right") else {"R": pts[0], "L": pts[1]})
    return out


wb.fists_of = fists_of


def auto_fists(cell):
    """Skin blobs that are fists: every skin blob of 25+ px except the bald head, above the boots; the 2 biggest, left to right."""
    a = np.asarray(cell)
    m = skin(a); hb = ndimage.binary_dilation(head_blob(cell), iterations=1)
    lab, n = ndimage.label(ndimage.binary_closing(m & ~hb, iterations=1))
    out = []
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(ys) < 25 or ys.mean() > 165:
            continue
        out.append((round(float(xs.mean()), 1), round(float(ys.mean()), 1), len(ys)))
    out.sort(key=lambda t: -t[2])
    return sorted([(x, y) for x, y, _ in out[:2]], key=lambda t: t[0])


def register(cell, d):
    """Head anchor {x, y, s, angle} placing the face template on the drawn bald head (skin IoU, search move +-8 px, tilt +-15)."""
    a = np.asarray(cell)
    sk = skin(a)
    lab, n = ndimage.label(sk)
    if n == 0:
        return {"x": 0.0, "y": 0.0, "s": S, "sy": 1.0, "angle": 0.0}
    sizes = ndimage.sum(sk, lab, range(1, n + 1))
    tops = [np.nonzero(lab == i + 1)[0].min() for i in range(n)]
    cand = [i for i in range(n) if sizes[i] > 60]
    head = 1 + min(cand or range(n), key=lambda i: tops[i])           # the highest big skin blob is the face / scalp
    target = lab == head
    tf = template(d)
    tsk = skin(np.asarray(tf))
    base = {"x": 0.0, "y": 0.0, "s": S, "sy": 1.0, "angle": 0.0}
    t0 = np.asarray(placed(Image.fromarray((tsk * 255).astype(np.uint8)).convert("L").convert("RGBA"), base))[..., 0] > 128
    ty, tx = np.nonzero(t0); gy, gx = np.nonzero(target)
    x0, y0 = gx.mean() - tx.mean(), gy.min() - ty.min()
    tmask = Image.fromarray((tsk * 255).astype(np.uint8)).convert("RGBA")
    best = None
    for ang in range(-15, 16, 3):
        for dx in range(-8, 9, 2):
            for dy in range(-8, 9, 2):
                an = dict(base, x=float(x0 + dx), y=float(y0 + dy), angle=float(ang))
                m = np.asarray(placed(tmask, an))[..., 0] > 128
                iou = (m & target).sum() / max(1, (m | target).sum())
                if best is None or iou > best[0]:
                    best = (iou, an)
    iou, an = best
    for dx in (-1, 0, 1):                                              # 1 px refine
        for dy in (-1, 0, 1):
            a2 = dict(an, x=an["x"] + dx, y=an["y"] + dy)
            m = np.asarray(placed(tmask, a2))[..., 0] > 128
            v = (m & target).sum() / max(1, (m | target).sum())
            if v > iou:
                iou, best = v, (v, a2)
    return dict(best[1], x=round(best[1]["x"], 2), y=round(best[1]["y"], 2), iou=round(float(iou), 3))


def head_blob(cell):
    """Skin mask of the drawn bald head: the highest big skin blob (fists lower or smaller)."""
    a = np.asarray(cell)
    sk = skin(a)
    lab, n = ndimage.label(sk)
    if n == 0:
        return np.zeros(sk.shape, bool)
    sizes = ndimage.sum(sk, lab, range(1, n + 1))
    tops = [np.nonzero(lab == i + 1)[0].min() for i in range(n)]
    cand = [i for i in range(n) if sizes[i] > 60] or list(range(n))
    return lab == 1 + min(cand, key=lambda i: tops[i])


def headless(cell, d, an):
    """Body without the drawn head: the drawn head (its skin blob grown 3 px with the holes filled: outline, eyes, brows, ears) down to the
    game chin, plus the registered face template grown 2 px; navy cloth outside the template and the collar fur below the chin stay."""
    a = np.asarray(cell).astype(np.int32)
    t = np.asarray(placed(template(d), an))[..., 3] > 128
    ys = np.nonzero(t.any(1))[0]
    chin = ys.max() if len(ys) else 0
    sk = skin(np.asarray(cell)); lab, n = ndimage.label(sk)
    near_t = ndimage.binary_dilation(t, iterations=6)
    ears = np.isin(lab, [i for i in range(1, n + 1) if (near_t & (lab == i)).any() and np.nonzero(lab == i)[0].mean() < chin])
    drawn = ndimage.binary_fill_holes(ndimage.binary_dilation(head_blob(cell) | ears, iterations=3))
    dark = (np.asarray(cell)[..., :3].astype(int).sum(-1) < 200) & ndimage.binary_dilation(t, iterations=5)  # thin outline arcs of a bigger skull
    drawn |= dark
    drawn[chin + 2:] = False
    kill = drawn | ndimage.binary_dilation(t, iterations=2)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    navy = (b > r + 25) & (b > g) & (r < 120) & ~t
    navy[:chin - 4] = False                                             # above the chin blue is the drawn eyes / brows
    fur = (np.minimum(np.minimum(r, g), b) > 150) & ((np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b)) < 60)
    fur[:chin - 6] = False
    out = a.copy(); out[kill & ~navy & ~fur] = 0
    lab, n = ndimage.label(out[..., 3] > 0)
    near = ndimage.binary_dilation(kill, iterations=3)
    for i in range(1, n + 1):
        m = lab == i
        if m.sum() < 25 and near[m].any():
            out[m] = 0
    return Image.fromarray(out.astype(np.uint8))


def head_cell(d, an, style=0):
    face = template(d)
    face.alpha_composite(Image.fromarray(nw.dyed_hair(d, style).astype(np.uint8)))
    return placed(face, an)


def under_cell(d, an, style=0):
    p = f"{HEADS}/hair-under/{style}/{'right' if d == 'left' else d}.png"
    if not os.path.exists(p):
        return Image.new("RGBA", (F, F))
    return placed(Image.fromarray(nw.dyed_hair(d, style, path=p).astype(np.uint8)), an)


def jobs_of(args):
    return wb.parse(args)


def write(jobs):
    os.makedirs(OLD, exist_ok=True)
    for an in ANIMS:
        for layer in ("bare", "hand", "armed", "headless"):
            p = f"{BODY}/{layer}/{an}.png"
            if os.path.exists(p) and not os.path.exists(f"{OLD}/{layer}-{an}.png"):
                shutil.copyfile(p, f"{OLD}/{layer}-{an}.png")
    for p in (HEAD_JSON, wb.MELEE):
        if not os.path.exists(f"{OLD}/{os.path.basename(p)}"):
            shutil.copyfile(p, f"{OLD}/{os.path.basename(p)}")
    wb.write(jobs)                                                      # bare (with the drawn bald head), hand, armed, staff / book anchors
    heads = json.load(open(HEAD_JSON, encoding="utf-8"))
    sheets = {}
    for anim, d in jobs:
        bare = Image.open(f"{BODY}/bare/{anim}.png").convert("RGBA")
        hp = f"{BODY}/headless/{anim}.png"
        if hp not in sheets:
            sheets[hp] = Image.open(hp).convert("RGBA") if os.path.exists(hp) else Image.new("RGBA", bare.size)
            if sheets[hp].size != bare.size:
                sheets[hp] = Image.new("RGBA", bare.size)
        frames = []
        for f in range(wb.COUNT[anim]):
            box = (f * F, wb.ROWS[d] * F, (f + 1) * F, (wb.ROWS[d] + 1) * F)
            cell = bare.crop(box)
            an = register(cell, d)
            hl = headless(cell, d, an)
            sheets[hp].paste(hl, box[:2])
            full = under_cell(d, an); full.alpha_composite(hl); full.alpha_composite(head_cell(d, an))
            bare.paste(full, box[:2])
            frames.append({k: an[k] for k in ("x", "y", "s", "sy", "angle")})
            print(f"  {anim} {d} f{f}: head IoU {an['iou']}, angle {an['angle']}")
        bare.save(f"{BODY}/bare/{anim}.png")
        heads["anims"] = [e for e in heads["anims"] if not (e["anim"] == anim and e["dir"] == d)]
        heads["anims"].append({"anim": anim, "dir": d, "frames": frames})
    for p, im in sheets.items():
        im.save(p)
    json.dump(heads, open(HEAD_JSON, "w", encoding="utf-8"), indent=2)
    for anim in sorted({a for a, _ in jobs}):                          # armed again: composed on the new bare
        path = f"{BODY}/armed/{anim}.png"
        im = Image.open(path).convert("RGBA")
        for a2, d in jobs:
            if a2 == anim:
                for f in range(wb.COUNT[anim]):
                    im.paste(wb.wc.chf.compose("wizard", anim, d, f), (f * F, wb.ROWS[d] * F))
        im.save(path)
    print("wrote", len(jobs), "rows (headless + head anchors)")


def compose(anim, d, f, style=0, tier=0, pad=24):
    """A frame as the game draws it: hair-under, headless body, face + hair, staff / book, hand layer."""
    ha = {(e["anim"], e["dir"]): e["frames"] for e in json.load(open(HEAD_JSON, encoding="utf-8"))["anims"]}
    an = ha[(anim, d)][f]
    hl = Image.open(f"{BODY}/headless/{anim}.png").convert("RGBA")
    tmp = hl.copy()
    c = under_cell(d, an, style); c.alpha_composite(hl.crop((f * F, wb.ROWS[d] * F, (f + 1) * F, (wb.ROWS[d] + 1) * F)))
    tmp.paste(c, (f * F, wb.ROWS[d] * F))
    p = f"{SRC}/_tmp-headless.png"; tmp.save(p)
    return wb.wc.chf.compose("wizard", anim, d, f, bare_path=p, tier=tier, pad=pad, over=head_cell(d, an, style))


def check(out, styles=(0, 2, 4)):
    rows = []
    for anim in ANIMS:
        for d in ("down", "up", "left", "right"):
            for st in styles:
                rows.append([compose(anim, d, f, st) for f in range(wb.COUNT[anim])])
    W = F + 48
    sheet = Image.new("RGBA", (W * 6, W * len(rows)), (58, 62, 70, 255))
    for i, row in enumerate(rows):
        for j, im in enumerate(row):
            sheet.alpha_composite(im, (j * W, i * W))
    sheet.convert("RGB").save(out)
    print(out, sheet.size)


def grid(out, jobs):
    wb.grid(out, jobs)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "grid":
        grid(sys.argv[2], jobs_of(sys.argv[3:]))
    elif cmd == "write":
        write(jobs_of(sys.argv[2:]))
    elif cmd == "check":
        check(sys.argv[2])
    else:
        print(__doc__)

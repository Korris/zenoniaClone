"""RO Soft Fantasy monster redraw (2026-11-20): cuts the ChatGPT 4 x 3 sheets (4 frames x rows down, up, right; left = right mirrored)
into the game's monster sheets, same cells, feet line and body height as the sheet they replace.

Source  Assets/ArtLibrary/monsters/ro-soft/<id>-{walk,attack}-chatgpt-N.png   (highest N wins; 1536x1024 or any size, flat #FF00FF or #00FF00,
        the key colour is read from the image corners)
Output  Assets/Resources/Sprites/monsters/<id>.png / <id>-attack.png   (4x4, rows down, up, left, right; cells from MonsterDef: 192/256,
        Wanted bosses 384/512). Existing .meta files are kept.
Before  Assets/ArtLibrary/monsters/ro-soft/old/<id>.png / <id>-attack.png  (copy of the sheet as it was before the first build, written by `ref`)

  py Tools/build-ro-soft-monsters.py ref <id>      old copy + ChatGPT reference images ref/<id>-{walk,attack}-ref.png (old frames on the 4x3 layout)
  py Tools/build-ro-soft-monsters.py build <id>    cut + install
  py Tools/build-ro-soft-monsters.py check <id>    docs/reference/monsters-ro-soft-redraw/<id>-before-after.png (every frame, old over new, hero for scale)

Body height: the median walk-frame body height of the OLD sheet (measured in `ref`, kept in ro-soft/heights.json), so the in-game size,
the HP bar and the hit box stay where they were. Keying, splitting and placing are the meadow tool's (Tools/build-meadow-monsters.py).
"""
import glob, importlib.util, json, os, re, shutil, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/monsters/ro-soft"
OUT = f"{ROOT}/Assets/Resources/Sprites/monsters"
DOCS = f"{ROOT}/docs/reference/monsters-ro-soft-redraw"
HEIGHTS = f"{SRC}/heights.json"
BIG = {"rimeclawTyrant", "glacialColossus"}          # Wanted bosses: 384 / 512 cells, feet 448
# flyers hover: the old sheets anchored them on a baked shadow (meadow tool FLYERS); the new art has no shadow, so the body is lifted by
# this many walk-cell px above the feet line (the game draws its own ground shadow at the feet)
HOVER = {"thornSprite": 16, "cliffHarpy": 16, "amberWisp": 12, "ruinWisp": 12, "iceWraith": 10, "sandWraith": 10}
# ChatGPT sometimes paints the attack sheet at a smaller body size than the walk sheet: for these the attack frames get their own scale so the
# stance frame (column 4, "back to guard") matches the walk body height (clamped to +-25 %)
ATK_MATCH = {"iceWraith", "dustscaleLizard", "highlandIbex"}
SRC_ROWS = (0, 1, 3)                                 # game rows drawn by ChatGPT: down, up, right (left = mirrored right)

_spec = importlib.util.spec_from_file_location("meadow", f"{ROOT}/Tools/build-meadow-monsters.py")
mm = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(mm)
_spec2 = importlib.util.spec_from_file_location("wanted", f"{ROOT}/Tools/build-wanted-boss-sheets.py")
wb = importlib.util.module_from_spec(_spec2); _spec2.loader.exec_module(wb)


def cells(mid):
    """(walk cell, attack cell, attack feet row)."""
    return (384, 512, 448) if mid in BIG else (192, 256, 224)


def latest(mid, kind):
    paths = glob.glob(f"{SRC}/{mid}-{kind}-chatgpt-*.png")
    if not paths:
        raise SystemExit(f"no source {mid}-{kind}-chatgpt-N.png in {SRC}")
    return max(paths, key=lambda p: int(re.findall(r"-(\d+)\.png$", p)[0]))


def key_any(path):
    """Key by the background colour found in the corners: #00FF00 -> green key, otherwise magenta."""
    px = np.asarray(Image.open(path).convert("RGB"))
    c = np.concatenate([px[:8, :8].reshape(-1, 3), px[-8:, -8:].reshape(-1, 3), px[:8, -8:].reshape(-1, 3)]).mean(0)
    return wb.key_green(path) if c[1] > 150 and c[0] < 100 and c[2] < 100 else mm.key_magenta(path)


def frames_4x3(path):
    """12 frames (rows down, up, right) -> 16 in game order down, up, left, right (left = right row mirrored)."""
    rgba = key_any(path)
    alpha = rgba[..., 3] > 0.2
    ys = mm.valleys(alpha.sum(1), 3)
    rows = []
    for r in range(3):
        xs = mm.valleys(alpha[ys[r]:ys[r + 1]].sum(0), 4)
        rows.append([mm.extract(rgba, (ys[r], ys[r + 1], xs[c], xs[c + 1])) for c in range(4)])
    left = []
    for f in rows[2]:
        g = dict(f)
        g["img"] = f["img"][:, ::-1].copy()
        g["main_cx"] = f["w"] - f["main_cx"]
        left.append(g)
    return rows[0] + rows[1] + left + rows[2]


def grid_frames(png, cell):
    im = Image.open(png).convert("RGBA")
    return [[im.crop((c * cell, r * cell, (c + 1) * cell, (r + 1) * cell)) for c in range(4)] for r in range(4)]


def body_h(frame):
    a = np.asarray(frame)[..., 3] > 76
    ys = np.where(a.any(1))[0]
    return int(ys.max() - ys.min() + 1) if len(ys) else 0


def load_heights():
    return json.load(open(HEIGHTS)) if os.path.exists(HEIGHTS) else {}


def ref(mid):
    """Keep the old sheets, measure the old body height, write the two 1536x1024 reference images (old frames on the 4x3 layout)."""
    os.makedirs(f"{SRC}/old", exist_ok=True); os.makedirs(f"{SRC}/ref", exist_ok=True)
    for suf in ("", "-attack"):
        dst = f"{SRC}/old/{mid}{suf}.png"
        if not os.path.exists(dst):
            shutil.copyfile(f"{OUT}/{mid}{suf}.png", dst)
    wc, ac, _ = cells(mid)
    walk = grid_frames(f"{SRC}/old/{mid}.png", wc)
    hs = load_heights()
    if mid not in hs:
        hs[mid] = int(np.median([body_h(f) for row in walk for f in row]))
        json.dump(hs, open(HEIGHTS, "w"), indent=1, sort_keys=True)
    for kind, cell in (("walk", wc), ("attack", ac)):
        fr = grid_frames(f"{SRC}/old/{mid}{'' if kind == 'walk' else '-attack'}.png", cell)
        W, H = 1536, 1024
        cw, ch = W // 4, H // 3
        sheet = Image.new("RGBA", (W, H), (255, 0, 255, 255))
        boxes = [fr[gr][c].getbbox() for gr in SRC_ROWS for c in range(4)]
        k = min(min((cw - 40) / (b[2] - b[0]), (ch - 30) / (b[3] - b[1])) for b in boxes if b)   # one scale: relative sizes kept
        for ri, gr in enumerate(SRC_ROWS):
            for c in range(4):
                f = fr[gr][c]
                bb = f.getbbox()
                if not bb:
                    continue
                f = f.crop(bb)
                f = f.resize((max(1, int(f.width * k)), max(1, int(f.height * k))), Image.NEAREST)
                sheet.alpha_composite(f, (c * cw + (cw - f.width) // 2, ri * ch + ch - 15 - f.height))
        sheet.convert("RGB").save(f"{SRC}/ref/{mid}-{kind}-ref.png")
    print(f"{mid}: old body height {hs[mid]} px, refs in {SRC}/ref/")


def build(mid):
    hs = load_heights()
    if mid not in hs:
        raise SystemExit(f"run `ref {mid}` first (old body height unknown)")
    wc, ac, feet = cells(mid)
    walk, atk = frames_4x3(latest(mid, "walk")), frames_4x3(latest(mid, "attack"))
    mm.WALK_CELL, mm.ATK_CELL, mm.ATK_FEET = wc, ac, feet
    want, cap = mm.fit_scale(walk, atk, hs[mid], wc, wc)
    s = min(want, cap)
    lift = HOVER.get(mid, 0) * wc // 192
    if lift:
        s = min(s, (wc - lift - mm.MARGIN) / max(f["h"] for f in walk), (feet - lift - mm.MARGIN) / max(f["h"] for f in atk))
    sa = s
    if mid in ATK_MATCH:
        k = float(np.median([f["main_h"] for f in walk])) / float(np.median([atk[r * 4 + 3]["main_h"] for r in range(4)]))
        sa = s * min(1.25, max(0.8, k))
        sa = min([sa] + [min((ac - 2 * mm.MARGIN) / f["w"], (feet - lift - mm.MARGIN) / f["h"]) for f in atk])
        print(f"  attack scale matched to the walk body: x{sa / s:.3f}")
    w = mm.sheet_from(mm.place(walk, s, wc, wc - lift), wc)
    a = mm.sheet_from(mm.place(atk, sa, ac, feet - lift), ac)
    mm.to_image(w).save(f"{OUT}/{mid}.png"); mm.to_image(a).save(f"{OUT}/{mid}-attack.png")
    nh = int(np.median([body_h(f) for row in grid_frames(f"{OUT}/{mid}.png", wc) for f in row]))
    print(f"{mid}: scale {s:.3f} (wanted {want:.3f}, cap {cap:.3f}); body height old {hs[mid]} new {nh} px; "
          f"halo walk {mm.halo_count(w)} attack {mm.halo_count(a)}")


def hero_ref(cell):
    """RO Fantasy warrior idle (down) scaled to the monster pixel density (hero body 164 px in a 192 cell)."""
    p = f"{ROOT}/Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/idle.png"
    js = json.load(open(f"{ROOT}/Assets/Resources/Data/hero-layered/ro-fantasy/warrior/body.json"))
    im = Image.open(p).convert("RGBA").crop((0, 0, js["cellW"], js["cellH"]))
    k = 164.0 / js["bodyPx"] * cell / 192.0
    im = im.resize((int(im.width * k), int(im.height * k)), Image.LANCZOS)
    c = Image.new("RGBA", (cell, cell))
    c.alpha_composite(im, ((cell - im.width) // 2, cell - int(js["feetY"] * k) - 1))
    return c


def check(mid):
    """Old (top) over new (bottom), walk then attack, all 4 rows x 4 frames, on light stone; the hero next to each walk block."""
    os.makedirs(DOCS, exist_ok=True)
    wc, ac, feet = cells(mid)
    blocks = []
    for label, base in (("before", f"{SRC}/old/{mid}"), ("after", f"{OUT}/{mid}")):
        for cell, suf in ((wc, ""), (ac, "-attack")):
            blocks.append((f"{label} {('walk' if not suf else 'attack')}", grid_frames(base + suf + ".png", cell), cell))
    hero = hero_ref(wc)
    pad, top = 6, 18
    W = (wc + pad) * 5 + (ac + pad) * 4 + 3 * pad
    H = 2 * (top + 4 * (ac + pad))
    sheet = Image.new("RGBA", (W, H), (226, 218, 198, 255))
    d = ImageDraw.Draw(sheet)
    for i, (label, fr, cell) in enumerate(blocks):
        y0 = (i // 2) * (top + 4 * (ac + pad))
        x0 = 0 if i % 2 == 0 else (wc + pad) * 5 + 2 * pad
        d.text((x0 + 4, y0 + 3), f"{mid} {label}  rows: down, up, left, right", fill=(60, 40, 30, 255))
        for r in range(4):
            ry = y0 + top + r * (ac + pad)
            if i % 2 == 0:
                sheet.alpha_composite(hero, (x0, ry + ac - wc))
            for c in range(4):
                cx = x0 + (wc + pad if i % 2 == 0 else 0) + c * (cell + pad)
                cy = ry + ac - cell
                d.rectangle([cx, cy, cx + cell - 1, cy + cell - 1], outline=(190, 178, 150, 255))
                feet_y = cy + (cell - 1 if cell == wc else feet)
                d.line([(cx, feet_y), (cx + cell - 1, feet_y)], fill=(200, 80, 60, 255))
                sheet.alpha_composite(fr[r][c], (cx, cy))
    out = f"{DOCS}/{mid}-before-after.png"
    sheet.convert("RGB").save(out)
    print(out)


if __name__ == "__main__":
    cmd, ids = sys.argv[1], sys.argv[2:]
    for m in ids:
        {"ref": ref, "build": build, "check": check}[cmd](m)

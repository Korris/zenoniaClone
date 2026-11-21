"""RO Fantasy heroes (2026-11-19): one painted body per CLASS -> game body sheets of the layered renderer (Tools/build-ro-fantasy-hero-game-assets.py).

Sources (read only), Assets/ArtLibrary/ro-fantasy-heroes/src/, flat #00FF00, newest -chatgpt-N wins:
  body-<class>-idle-5dir-chatgpt-N.png                    1 row: down, down-right, right, up-right, up (bald, empty fists)
  body-<class>-<walk|attack>-<a>--<b>-chatgpt-N.png   2 rows x 4 frames (ONE attack per class: every combo step and skill pose), row 1 = direction a, row 2 = b (b may be "none"),
                                                          same layout as the common layered bodies (Tools/build-hero-layered-sheets.py)
An anim is cut only when all 5 painted directions exist; until then the game draws the idle frame for it (stand-in motion in code,
HeroRenderer.Layered.cs). The outfit keeps its real colours: the mask is empty (no outfit recolour), FixedOutfit in class.json.

Cell 320 x 256, feet centre (160, 244), bald figure BALD_PX tall (skull top to feet) like the common layered bodies. Walk frames are
re-centred on the idle skull x of their direction. Head anchor = skull top centre + skull width (row median), as the common bodies.
"""
import re
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/src"
CW, CH, FEET, BALD_PX, PAINTED = 320, 256, 244, 188, 5
DRAWN = ["down", "down-right", "right", "up-right", "up"]
# attack painted for all 8 Dir8 dirs (Warrior 20/11: the left dirs keep the sword in the right hand, a mirrored right row swaps the hands);
# sources may pair any two of them (e.g. body-warrior-attack-left--up-left-chatgpt-1.png). Rows then follow Dir8 order.
DRAWN8 = DRAWN + ["up-left", "left", "down-left"]
MIRROR_OF = [0, 1, 2, 3, 4, 3, 2, 1]  # Dir8 -> the painted right-side row with the same view (idle skull width reference)
MOTIONS = [("walk", 4), ("attack", 4)]  # one lock-target attack per class (user 19/11), no attack1-3
CLASSES = ["warrior", "wizard", "mechanic", "assassin", "cleric"]
# 8-frame attack (20/11, smooth chop: 4 key poses + in-betweens over the same 400 ms): ONE direction per image, 2 rows x 4 = frames 0-3, 4-7
ATTACK8F_RE = re.compile(r"^body-(?P<c>[a-z]+)-attack-(?P<d>[a-z-]+?)-8f-chatgpt-(?P<n>\d+)\.png$")
MOTION_RE = re.compile(r"^body-(?P<c>[a-z]+)-(?P<anim>walk|attack)-(?P<a>[a-z-]+)--(?P<b>[a-z-]+)-chatgpt-(?P<n>\d+)\.png$")

art_check = import_module("build-ro-fantasy-hero-art-check")  # load_rgba: green key + white letterbox + green iris refill
pilot = import_module("build-ro-soft-8dir-pilot")              # figures_with_pos
sheets_tool = import_module("build-hero-layered-sheets")       # head_anchor, feet_x
head_fix = import_module("ro_fantasy_head_fix")                # HEAD_FIX rows: head scaled to the idle skull, body kept
attack8_cut = import_module("ro_fantasy_attack8_cut")          # 8-frame one-direction attack sources (guard on idle height + lunge travel)


def newest(pattern):
    files = sorted(SRC.glob(pattern), key=lambda p: int(re.search(r"chatgpt-(\d+)", p.name).group(1)))
    return files[-1] if files else None


def keyed(path):
    """Source keyed to RGBA uint8 (PIL). Specks of the green key left inside the figure (enclosed by the outline, so the key kept them)
    take the colour of the nearest non-green pixel (blobs up to 150 px; green irises are bigger)."""
    a = (art_check.load_rgba(str(path)) * 255).round().astype(np.uint8)
    c = a[..., :3].astype(int)
    green = (a[..., 3] > 0) & (c[..., 1] - np.maximum(c[..., 0], c[..., 2]) > 40)
    from scipy import ndimage
    lab, n = ndimage.label(green)
    if n:  # green eyes (Mechanic, Cleric) are big blobs: keep them
        sizes = ndimage.sum(green, lab, range(1, n + 1))
        green &= np.isin(lab, 1 + np.nonzero(sizes <= 150)[0])
    if green.any():
        idx = ndimage.distance_transform_edt(green | (a[..., 3] == 0), return_distances=False, return_indices=True)
        a[green, :3] = a[idx[0][green], idx[1][green], :3]
    return Image.fromarray(a, "RGBA")


def despeck(fig, edge=5, gap=10, small=60, debris=0.0015):
    """Motion figures (keyed by pilot.key_green, no speck pass). Green / olive pixels where the outline meets the #00FF00 key
    (g above max(r, b) by more than `gap`, or olive g ~ r >> b; within `edge` px of transparency; blobs up to 150 px) are key spill:
    erased. Small enclosed green specks (g - max(r, b) > 40, up to `small` px; green irises are far bigger and lie inside the face)
    take the nearest non-green colour. Then loose bits under `debris` of the figure area (outline crumbs of an erased twig) go too.
    Seen 19/11: Cleric walk / attack up, hem between the legs; Assassin walk right f0, olive twig beside the scarf."""
    from scipy import ndimage
    a = np.asarray(fig.convert("RGBA")).copy()
    c = a[..., :3].astype(int)
    op = a[..., 3] > 0
    gex = c[..., 1] - np.maximum(c[..., 0], c[..., 2])
    near = ndimage.binary_dilation(a[..., 3] < 128, iterations=edge)
    olive = (c[..., 1] >= c[..., 0] - 20) & (c[..., 1] - c[..., 2] > 30) & (c[..., :2].max(-1) < 140)  # key + dark outline (gold: r >> g)

    def small_blobs(m, cap, structure=None):
        lab, n = ndimage.label(m, structure)
        if not n:
            return np.zeros(m.shape, bool)
        sizes = ndimage.sum(m, lab, range(1, n + 1))
        return np.isin(lab, 1 + np.nonzero(sizes <= cap)[0])
    spill = (gex > gap) | olive
    a[(a[..., 3] <= 40) & spill, 3] = 0  # faint green halo: erased first, it would chain every spill blob into one ring
    a[small_blobs((a[..., 3] > 40) & spill & near, 150), 3] = 0
    inner = small_blobs(op & (gex > 40) & ~near, small)
    src = (a[..., 3] >= 128) & ~inner
    if inner.any() and src.any():
        idx = ndimage.distance_transform_edt(~src, return_distances=False, return_indices=True)
        a[inner, :3] = a[idx[0][inner], idx[1][inner], :3]
    op = a[..., 3] >= 128
    a[small_blobs(op, debris * op.sum(), np.ones((3, 3))), 3] = 0  # loose crumbs, 8-connected so thin diagonal tips stay
    return Image.fromarray(a, "RGBA")


def place(fig, scale):
    """(cell, (ox, oy, s)): figure resized by scale, feet centre on (CW/2, FEET). cell px = fig px * s + (ox, oy)."""
    f = fig.resize((max(1, round(fig.width * scale)), max(1, round(fig.height * scale))), Image.LANCZOS)
    ox, oy = round(CW / 2 - sheets_tool.feet_x(f)), FEET - f.height
    cell = Image.new("RGBA", (CW, CH))
    cell.paste(f, (ox, oy), f)
    return cell, (ox, oy, f.height / fig.height)


def idle(cls):
    """[(cell, xf)] for the 5 painted dirs; xf = (src_x0, src_y0, scale, ox, oy): cell px = (src px - src_x0/y0) * scale + (ox, oy)."""
    src = newest(f"body-{cls}-idle-5dir-chatgpt-*.png")
    out = []
    for fig, x0, y0 in pilot.figures_with_pos(keyed(src), 5):
        cell, (ox, oy, sc) = place(fig, BALD_PX / fig.height)
        out.append((cell, (x0, y0, sc, ox, oy)))
    return out, src.name


def shift_x(cell, dx):
    out = Image.new("RGBA", cell.size)
    if dx >= 0:
        out.alpha_composite(cell, (dx, 0))
    else:
        out.alpha_composite(cell.crop((-dx, 0, CW, CH)), (0, 0))
    return out


def skull_w(fig):
    """Median width of the top of a figure (skull band 6-22 % of its height)."""
    al = np.asarray(fig)[..., 3] > 128
    ys = np.nonzero(al.any(1))[0]
    top, h = ys[0], ys[-1] - ys[0]
    w = [np.count_nonzero(al[y]) for y in range(top + int(h * 0.06), top + int(h * 0.22))]
    return float(np.median(w)) if w else fig.width


def motion_rows(cls, idle_figs):
    """{anim: [row][frame] cells} for the motion anims whose 5 painted dirs all exist (others: absent -> idle stand-in)."""
    found = {}
    for path in sorted(SRC.glob(f"body-{cls}-*-chatgpt-*.png")):
        m = MOTION_RE.match(path.name)
        if not m or m["c"] != cls:
            continue
        key = (m["anim"], m["a"], m["b"])
        if key not in found or int(m["n"]) > found[key][0]:
            found[key] = (int(m["n"]), path)
    found8 = {}
    for path in sorted(SRC.glob(f"body-{cls}-attack-*-8f-chatgpt-*.png")):
        m = ATTACK8F_RE.match(path.name)
        if m and m["c"] == cls and m["d"] in DRAWN8 and (m["d"] not in found8 or int(m["n"]) > found8[m["d"]][0]):
            found8[m["d"]] = (int(m["n"]), path)
    out = {}
    for anim, n in MOTIONS:
        by_dir = {}
        if anim == "attack" and found8:  # 8-frame sources win over the 4-frame pairs
            found = {("attack", d, "none"): v for d, v in found8.items()}
            n = 8
        for (a, da, db), (_, path) in sorted(found.items()):
            if a != anim:
                continue
            dirs = [d for d in (da, db) if d != "none"]
            if n == 8:  # one direction, frames 0-7, designed lunge travel (Tools/ro_fantasy_attack8_cut.py, 22/11)
                fig_i = idle_figs[MIRROR_OF[DRAWN8.index(dirs[0])]]
                by_dir[dirs[0]] = attack8_cut.cut(path, skull_w(fig_i), place, skull_w, despeck, BALD_PX, f"{cls} {anim} {dirs[0]}", dirs[0])
                continue
            else:
                rows = [[despeck(f) for f in row] for row in sheets_tool.rows_of(path, n, len(dirs))]
            for d, row in zip(dirs, rows):
                fig_i = idle_figs[MIRROR_OF[DRAWN8.index(d)]]
                # idle_figs are the cropped idle cells (already at game scale): attacks keep the idle skull width, walks the idle height
                fix = (cls, anim, d) in head_fix.HEAD_FIX
                if anim == "walk" or fix:  # head-fix rows: body on the idle height, then the head alone to the idle skull width
                    s = BALD_PX / float(np.median([f.height for f in row]))
                else:
                    s = skull_w(fig_i) / float(np.median([skull_w(f) for f in row]))
                cells = [despeck(place(f, s)[0]) for f in row]  # again at game scale: LANCZOS leaves olive fringes of bigger source blobs
                if fix:
                    k = skull_w(fig_i) / float(np.median([skull_w(f) * s for f in row]))
                    cells = [head_fix.scale_head(c, k, *sheets_tool.head_anchor(c)[1:3]) for c in cells]
                    print(f"{cls} {anim} {d}: head x{k:.3f} (body on the idle height)")
                by_dir[d] = cells
        if anim == "attack" and all(d in by_dir for d in DRAWN8):
            out[anim] = [by_dir[d] for d in DRAWN8]
            print(f"{cls} {anim}: all 8 dirs painted (left rows not mirrored)")
        elif all(d in by_dir for d in DRAWN):
            out[anim] = [by_dir[d] for d in DRAWN]
            print(f"{cls} {anim}: cut from {len(found)} source file(s)")
        elif by_dir:
            print(f"{cls} {anim}: only {sorted(by_dir)} painted, idle stand-in until all 5 directions exist")
    return out


def recentre_walk(rows, idle_cells):
    if "walk" not in rows:
        return
    for r in range(PAINTED):
        ix = sheets_tool.head_anchor(idle_cells[r])[0]
        rows["walk"][r] = [shift_x(c, int(round(ix - sheets_tool.head_anchor(c)[0]))) for c in rows["walk"][r]]


def head_anchors(frames):
    """[(anim, dir, f, x, y, w)]: per-frame skull top, row-median skull width."""
    out = []
    for anim, rows in frames.items():
        for r, row in enumerate(rows):
            got = [sheets_tool.head_anchor(c) for c in row]
            w = float(np.median([a[2] for a in got]))
            out += [(anim, r, f, a[0], a[1], w) for f, a in enumerate(got)]
    return out


def sheet_of(rows):
    n = len(rows[0])
    sheet = Image.new("RGBA", (CW * n, CH * len(rows)))  # 5 painted rows, or 8 (Dir8 order) for an all-dirs attack
    for r, row in enumerate(rows):
        for f, c in enumerate(row):
            sheet.alpha_composite(c, (f * CW, r * CH))
    return sheet

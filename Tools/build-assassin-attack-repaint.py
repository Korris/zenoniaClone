"""Assassin basic attack melee1-4 body rows repainted by ChatGPT per the board docs/reference/assassin-basic-attack-board.png (2026-11-08),
the same way as the Mechanic (Tools/build-mechanic-attack-down-repaint.py):
  melee1 Crescent Cut (right-hand forehand slash), melee2 Rising Fang (left-hand rising backhand), melee3 Cross Rend (both daggers X chop),
  melee4 Shadow Lunge (lunge + double stab). Timing / reach unchanged (ComboConfigs.AssassinHits).

ChatGPT paints one row of 6 frames per (anim, facing) with EMPTY fists on flat #00FF00 (attack-board/body/<anim>-<dir>-chatgpt-N.png).
This tool keys the strip, scales the whole row by one factor so the ready frame (frame 6) is as tall as idle frame 0 of the same facing
(hair top to sole), puts every frame's feet centre on idle's feet centre and its soles on idle's sole row, and writes:
  body/bare/<anim>.png row, body/hand/<anim>.png row (disc FIST_R around each fist drawn in front of the body),
  body/armed/<anim>.png row (bare + tier-0 daggers baked in, what ShadowCloneFx tints), anchors main/off of that row in held-assassin-melee.json.
Facing LEFT = the right row mirrored with main <-> off swapped, so the right-hand dagger (main) stays on her right hand, now the far hand
(docs/sprite-rules.md, quy tắc tư thế cầm 4; both daggers are one art, so the picture shows no swap).
Fist / dagger table: Tools/assassin_attack_repaint_fists.py. Old sheets are backed up once to attack-board/old/ before the first write.
After `write`, run the head layer (every new anim must take the hairstyles, docs/sprite-rules.md "Hình mẫu nhân vật"):
  python Tools/build-head-layer.py assassin anims-write melee1,melee2,melee3,melee4
  python Tools/build-head-layer.py assassin anims-normscale melee1,melee2,melee3,melee4
  python Tools/build-assassin-attack-repaint.py clean-headless   (ponytail tips left outside the head mask)

  python Tools/build-assassin-attack-repaint.py grid OUT_DIR [anim-dir ...]   fitted frames on an 8 px grid at 4x + fists marked, to read the table
  python Tools/build-assassin-attack-repaint.py heads OUT.png                 head ratio per frame against idle (headband width), contact sheet
  python Tools/build-assassin-attack-repaint.py preview OUT.png [anim-dir ...] rows with tier-0 daggers as the game draws them (left too), offline
  python Tools/build-assassin-attack-repaint.py write [anim-dir ...]          bare + hand + armed rows + anchors (default: every job with a source)
"""
import importlib.util, json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

os.chdir("D:/zenoniaClone")
sys.path.insert(0, "Tools")


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


oi = load("oi", "Tools/build-mechanic-outfit-idle-down.py")  # key_flat: flat-key cut with soft edges
chf = load("chf", "Tools/compose-hero-frame.py")
from assassin_attack_repaint_fists import FISTS

FRAME, COUNT, FIST_R, DAGGER_S = 192, 6, 8.5, 0.243
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
ANIMS = ("melee1", "melee2", "melee3", "melee4")
LIB = "Assets/ArtLibrary/classes/assassin/attack-board"
SRC, OLD = f"{LIB}/body", f"{LIB}/old"
BODY = "Assets/Resources/Sprites/classes/assassin/body"
GEAR, GEAR_MELEE = "Assets/Resources/Data/classes/assassin/held-assassin.json", "Assets/Resources/Data/classes/assassin/held-assassin-melee.json"


def source(anim, d):
    """Newest painted strip of a row (…-chatgpt-N.png, highest N), None when not painted yet."""
    best = None
    for n in range(1, 10):
        p = f"{SRC}/{anim}-{d}-chatgpt-{n}.png"
        if os.path.exists(p):
            best = p
    return best


def jobs(args):
    have = [(a, d) for a in ANIMS for d in ("down", "right", "up") if source(a, d)]
    if args:
        want = {tuple(x.split("-")) for x in args}
        have = [j for j in have if j in want]
    return have


def pieces(path):
    """The 6 painted frames left to right, each cropped to its own blobs."""
    fig = oi.key_flat(path)
    al = np.asarray(fig)[..., 3] > 100
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=6))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    best = sorted(1 + np.argsort(sizes)[::-1][:COUNT], key=lambda i: np.nonzero(lab == i)[1].min())
    if n < COUNT or sizes[np.array(best) - 1].min() < 0.3 * sizes.max():
        # a ponytail touches the next frame: the frames are evenly spaced, so give every blob to the sixth of the strip its centre falls in
        lab, n = ndimage.label(al, np.ones((3, 3)))
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        bins = np.zeros_like(lab)
        width = al.shape[1] / COUNT
        for i in range(1, n + 1):
            if sizes[i - 1] < 30:
                continue
            cx = ndimage.center_of_mass(lab == i)[1]
            bins[lab == i] = min(COUNT, int(cx // width) + 1)
        lab, best = bins, list(range(1, COUNT + 1))
    out = []
    for i in best:
        part = np.asarray(fig).copy()
        part[..., 3] = np.where(lab == i, part[..., 3], 0)
        im = Image.fromarray(part)
        out.append(im.crop(im.getbbox()))
    if len(out) != COUNT:
        raise SystemExit(f"{path}: {len(out)} frames, expected {COUNT}")
    return out


def idle_cell(d):
    r = ROWS[d]
    return Image.open(f"{BODY}/bare/idle.png").convert("RGBA").crop((0, r * FRAME, FRAME, r * FRAME + FRAME))


def feet_x(im):
    ys, xs = np.nonzero(np.asarray(im)[..., 3] > 100)
    sel = ys > ys.max() - 12
    return (xs[sel].min() + xs[sel].max()) / 2


def extent(im):
    ys = np.nonzero(np.asarray(im)[..., 3] > 100)[0]
    return int(ys.min()), int(ys.max())


def headband_width(im):
    """Width of the red headband (widest run of red rows in the upper 45 % of the figure), px; None if not visible (facing up)."""
    a = np.asarray(im).astype(int)
    al = a[..., 3] > 100
    top, bot = extent(im)
    red = al & (a[..., 0] > 160) & (a[..., 1] < 80) & (a[..., 2] < 90)
    band = red[top:top + int(0.45 * (bot - top))]
    widths = []
    for row in band:
        xs = np.flatnonzero(row)
        if len(xs) > 4:
            lab, n = ndimage.label(row)
            sizes = ndimage.sum(row, lab, range(1, n + 1))
            widths.append(sizes.max())
    if not widths:
        return None
    widths = sorted(widths)[-4:]  # the band's widest rows, not the bow
    return float(np.median(widths))


def fitted(anim, d):
    """New 192 cells of a painted row; left = right mirrored."""
    if d == "left":
        return [c.transpose(Image.FLIP_LEFT_RIGHT) for c in fitted(anim, "right")]
    figs = pieces(source(anim, d))
    idle = idle_cell(d)
    itop, ibot = extent(idle)
    ifx = feet_x(idle)
    k = (ibot - itop + 1) / figs[-1].height  # frame 6 is the ready stance, painted like idle
    cells = []
    for fg in figs:
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.alpha_composite(im, (int(round(ifx - feet_x(im))), ibot + 1 - im.height))
        cells.append(c)
    return cells


def fist_rows(anim, d):
    """Per frame the two anchors (main, off) as dicts x, y, angle, behind; left from right mirrored, parts swapped."""
    if d == "left":
        rows = []
        for main, off in fist_rows(anim, "right"):
            m = lambda a: dict(a, x=FRAME - a["x"], angle=-a["angle"])
            rows.append((m(off), m(main)))  # mirrored picture: the right-hand dagger is now on the other fist
        return rows
    out = []
    for fr in FISTS[(anim, d)]:
        out.append(tuple(dict(x=float(p[0]), y=float(p[1]), angle=float(p[2]), behind=bool(p[3]), hidden=len(p) > 4 and bool(p[4])) for p in fr))
    return out


def fist_layer(cell, anchors):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for a in anchors:
        if not a["behind"] and not a.get("hidden"):
            m |= (xx - a["x"]) ** 2 + (yy - a["y"]) ** 2 <= FIST_R ** 2
    arr = np.asarray(cell).copy()
    arr[..., 3] = np.where(m, arr[..., 3], 0)
    return Image.fromarray(arr)


def anchor_frame(a):
    return {"x": round(a["x"], 2), "y": round(a["y"], 2), "angle": round(a["angle"], 1), "s": DAGGER_S, "sx": 1,
            "behind": a["behind"], "hidden": bool(a.get("hidden")), "fallback": False}


def armed_cell(bare, hand, anchors, file, tier=0):
    """Body with a tier's daggers drawn as HeroRenderer does (behind, body, front, fist layer); tier 0 = the armed sheet (Shadow Clone)."""
    canvas = Image.new("RGBA", bare.size, (0, 0, 0, 0))
    entry = file["weapon"][tier]
    art = Image.open(f"Assets/Resources/Sprites/classes/assassin/weapons/dagger/{entry['name']}-base.png").convert("RGBA")
    for behind in (True, False):
        if not behind:
            canvas.alpha_composite(bare)
        for a in anchors:
            if a["behind"] == behind and not a.get("hidden"):
                chf.place(canvas, art, entry["baseGrip"], anchor_frame(a), 1.3, 0.0)
    canvas.alpha_composite(hand)
    return canvas


def grid(out, todo):
    os.makedirs(out, exist_ok=True)
    z = 4
    for anim, d in todo:
        cells = fitted(anim, d)
        fists = fist_rows(anim, d) if (anim, d) in FISTS else [()] * COUNT
        w = Image.new("RGB", (FRAME * z * COUNT, FRAME * z), (58, 62, 70))
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
            for i, a in enumerate(fists[f]):
                x, y = a["x"], a["y"]
                dr.ellipse(((f * FRAME + x - FIST_R) * z, (y - FIST_R) * z, (f * FRAME + x + FIST_R) * z, (y + FIST_R) * z),
                           outline=(255, 0, 0) if i == 0 else (0, 160, 255), width=3)
        w.save(f"{out}/{anim}-{d}-grid.png")
        print(f"{out}/{anim}-{d}-grid.png")


def heads(out):
    """Rule 8 check: headband width / figure height per frame, against idle of the same facing."""
    rows = []
    for anim, d in jobs([]):
        idle = idle_cell(d)
        it, ib = extent(idle)
        iw = headband_width(idle)
        cells = fitted(anim, d)
        ratios = []
        for c in cells:
            w = headband_width(c)
            ratios.append(None if (w is None or iw is None) else w / iw)
        print(anim, d, "headband vs idle", ["-" if r is None else "%.2f" % r for r in ratios])
        rows.append([idle] + cells)
    if rows:
        sheet = Image.new("RGBA", (FRAME * 7, FRAME * len(rows)), (58, 62, 70, 255))
        for r, row in enumerate(rows):
            for f, c in enumerate(row):
                sheet.alpha_composite(c, (f * FRAME, r * FRAME))
        sheet.convert("RGB").save(out)
        print(out)


def preview(out, todo, pad=60, z=2):
    """Rows as the game draws them (tier-0 daggers behind / body / daggers in front / fist layer), offline, Resources untouched; left rows too."""
    file = json.load(open(GEAR, encoding="utf-8"))
    rows = []
    for anim, d in todo:
        rows.append((anim, d))
        if d == "right":
            rows.append((anim, "left"))
    rows = [r for r in rows if (r[0], r[1] if r[1] != "left" else "right") in FISTS]
    size = FRAME + 2 * pad
    sheet = Image.new("RGBA", (size * COUNT, size * len(rows)), (58, 62, 70, 255))
    for r, (anim, d) in enumerate(rows):
        for f, (c, fr) in enumerate(zip(fitted(anim, d), fist_rows(anim, d))):
            big = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            body = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            body.alpha_composite(c, (pad, pad))
            hand = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            hand.alpha_composite(fist_layer(c, fr), (pad, pad))
            shifted = [dict(a, x=a["x"] + pad, y=a["y"] + pad) for a in fr]
            big.alpha_composite(armed_cell(body, hand, shifted, file))
            sheet.alpha_composite(big, (f * size, r * size))
    sheet = sheet.resize((sheet.width * z // 2, sheet.height * z // 2), Image.LANCZOS) if z != 2 else sheet
    sheet.convert("RGB").save(out)
    print(out, [f"{a}-{d}" for a, d in rows])


def clean_headless(min_px=200):
    """After build-head-layer.py anims-write: drop small floating bits (ponytail tips cut off outside the head mask, seen as dark strokes beside
    the hairstyle heads) from body/headless/melee1-4.png; every piece under min_px px not joined to the body goes."""
    for anim in ANIMS:
        path = f"{BODY}/headless/{anim}.png"
        sheet = np.asarray(Image.open(path).convert("RGBA")).copy()
        dropped = 0
        for r in range(4):
            for f in range(COUNT):
                cell = sheet[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME]
                for _ in range(3):  # 1-2 px dark hair-outline strands still joined to the scarf: few solid neighbours in a 5x5 window
                    solid = (cell[..., 3] > 100).astype(float)
                    near = ndimage.uniform_filter(solid, 5) * 25
                    rgb = cell[..., :3].astype(int)
                    darkish = (rgb.max(-1) < 90) | ((rgb[..., 1] > rgb[..., 0]) & (rgb[..., 1] > rgb[..., 2]))
                    strand = (cell[..., 3] > 0) & (near < 8) & darkish
                    cell[strand] = 0
                lab, n = ndimage.label(cell[..., 3] > 8, np.ones((3, 3)))
                if n < 2:
                    continue
                sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
                big = 1 + int(np.argmax(sizes))
                for i in range(1, n + 1):
                    if i != big and sizes[i - 1] < min_px:
                        cell[lab == i] = 0
                        dropped += 1
        Image.fromarray(sheet).save(path)
        print(path, "dropped", dropped, "pieces")


def ensure_backup():
    os.makedirs(OLD, exist_ok=True)
    for anim in ANIMS:
        for layer in ("bare", "hand", "armed"):
            dst = f"{OLD}/{layer}-{anim}.png"
            if not os.path.exists(dst):
                shutil.copyfile(f"{BODY}/{layer}/{anim}.png", dst)
                print("backup", dst)


def write(todo):
    ensure_backup()
    file = json.load(open(GEAR, encoding="utf-8"))
    data = json.load(open(GEAR_MELEE, encoding="utf-8"))
    rows = []
    for anim, d in todo:
        rows.append((anim, d))
        if d == "right":
            rows.append((anim, "left"))
    for anim, d in rows:
        if (anim, d if d != "left" else "right") not in FISTS:
            print("skip (no fist table)", anim, d)
            continue
        cells, fists = fitted(anim, d), fist_rows(anim, d)
        r = ROWS[d]
        sheets = {layer: Image.open(f"{BODY}/{layer}/{anim}.png").convert("RGBA") for layer in ("bare", "hand", "armed")}
        for s in sheets.values():
            s.paste(Image.new("RGBA", (FRAME * COUNT, FRAME), (0, 0, 0, 0)), (0, r * FRAME))
        for f, c in enumerate(cells):
            hand = fist_layer(c, fists[f])
            sheets["bare"].paste(c, (f * FRAME, r * FRAME))
            sheets["hand"].paste(hand, (f * FRAME, r * FRAME))
            sheets["armed"].paste(armed_cell(c, hand, fists[f], file), (f * FRAME, r * FRAME))
        for layer, s in sheets.items():
            s.save(f"{BODY}/{layer}/{anim}.png")
        # build-head-layer.py anims-write rebuilds body/hand from its first copy ArtLibrary/classes/assassin/body/hand/<anim>.pre-head.png:
        # keep that copy's row in step, or a rerun brings back the old fists (seen 2026-11-08 on melee1 up)
        pre = f"Assets/ArtLibrary/classes/assassin/body/hand/{anim}.pre-head.png"
        if os.path.exists(pre):
            ph = Image.open(pre).convert("RGBA")
            ph.paste(sheets["hand"].crop((0, r * FRAME, ph.width, (r + 1) * FRAME)), (0, r * FRAME))
            ph.save(pre)
        by = {a["part"]: a for a in data["anchors"] if a["anim"] == anim and a["dir"] == d}
        for i, part in enumerate(("main", "off")):
            by[part]["frames"] = [anchor_frame(fr[i]) for fr in fists]
        print(anim, d, "written")
    json.dump(data, open(GEAR_MELEE, "w", encoding="utf-8"), indent=1)  # the file is indented by 1 space (docs/sprite-rules.md 2026-11-01)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "grid":
        grid(sys.argv[2], jobs(sys.argv[3:]))
    elif cmd == "preview":
        preview(sys.argv[2], jobs(sys.argv[3:]))
    elif cmd == "clean-headless":
        clean_headless()
    elif cmd == "heads":
        heads(sys.argv[2])
    elif cmd == "write":
        write(jobs(sys.argv[2:]))
    else:
        print(__doc__)

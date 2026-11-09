"""Wizard idle (all 4 facings) in the Northwind T0 outfit, bald so the layered head (heads/face + heads/hair) goes on top.

Source: ArtLibrary/classes/wizard/outfits/northwind-t0-idle-dirs/src/wizard-northwind-t0-idle-4dirs-bald-chatgpt-1.png
(ChatGPT, 4 bald figures on #00FF00: down, up, right, left). The Lv1 body rows of idle are replaced (T0 armour = Lv1 body;
T1-T9 keep their own set-tN sheets).

Fit: each figure is scaled so bald head top -> sole spans the old idle height (template head top .. row 189, rule 8:
head and body keep the idle size), and moved sideways so its drawn bald head lies on heads/face/<dir> (left = right
mirrored). So the head anchor of frame 0 is (0, 0, s 1). Breathing: the body scales vertically about the sole so the
neck rises 0 / 0.75 / 1 / 0.75 px, the head anchor y and staff anchor y follow (as the old rows).
Headless = figure minus the drawn head (template mask grown 2 px, white fur kept). Bare = headless + face + hair 0 at
its default dye. Hand (down: both fists, right: the staff fist; staff in front there) cut from the body.
Down: staff / book use the centred `idle-stance` art of body/held-anchors.json, centre = fist, so the anchor is the new fist;
held-wizard-melee.json keeps its old offset from the fist. The down head anchor stays the old one (s 0.833, breathing by sy), the drawn front head is that small too.

  py Tools/build-wizard-northwind-idle-dirs.py fit       -> staging/ sheets + preview.png (not in Resources)
  py Tools/build-wizard-northwind-idle-dirs.py install   -> copies staging into Resources + anchors (backs up old/ once)
"""
import importlib.util, json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
WORK = f"{ROOT}/Assets/ArtLibrary/classes/wizard/outfits/northwind-t0-idle-dirs"
SRC = f"{WORK}/src/wizard-northwind-t0-idle-4dirs-bald-chatgpt-1.png"
STAGE, OLD = f"{WORK}/staging", f"{WORK}/old"
RES = f"{ROOT}/Assets/Resources"
BODY, HEADS = f"{RES}/Sprites/classes/wizard/body", f"{RES}/Sprites/classes/wizard/heads"
HEAD_JSON = f"{RES}/Data/classes/wizard/head-anchors.json"
HELD_JSON = f"{RES}/Data/classes/wizard/held-wizard-melee.json"
OUTFIT_JSON = f"{BODY}/held-anchors.json"     # Lv1 body: staff / book of idle down (art idle-stance)
F, SOLE, NECK = 192, 189, 104           # cell, sole row, neck row the breathing is measured at
FUR_FROM = 85                           # collar fur kept under the head mask only from this row down
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
SRC_ORDER = ["down", "up", "right", "left"]
DIRS = ["down", "up", "left", "right"]
OLD_DOWN_FISTS = {"staff": (55.6, 104.5), "book": (124.9, 124.9)}   # fists of the replaced down row (old hand layer)
BREATH = [0.0, 0.75, 1.0, 0.75]         # neck lift per frame, px (old idle rows)
STYLE0_DYE = (0xEE, 0xEA, 0xF2)         # hair-dyes.json styles[0]
SIDE_S = 0.8333333333333334             # head scale of the up / left / right facings, as the front head
STAFF_ANGLE = {"right": 70}            # old 45 put the crystal across the face; 60 still touched the mouth; 70 = in front of the chest


def key_green(a):
    """RGBA float from the ChatGPT sheet: opacity by distance to #00FF00, green spill clamped (the outfit has no green)."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum.reduce([r, b, 255 - g]) - 40) / 80.0, 0, 1)
    g = np.minimum(g, np.maximum(r, b) + 10)
    return np.dstack([r, g, b, alpha * 255]).astype(np.float32)


def figures():
    a = key_green(np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32))
    lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 128, iterations=4))
    objs = sorted([s for s in ndimage.find_objects(lab) if (lab[s] > 0).sum() > 20000], key=lambda s: s[1].start)
    assert len(objs) == 4, f"expected 4 figures, got {len(objs)}"
    return {d: a[s] for d, s in zip(SRC_ORDER, objs)}


def resize(fig, w, h):
    """Premultiplied LANCZOS so the keyed edge does not pick up dark fringes."""
    pre = fig.copy(); pre[..., :3] *= pre[..., 3:4] / 255.0
    im = np.dstack([np.asarray(Image.fromarray(pre[..., c]).resize((w, h), Image.LANCZOS)) for c in range(4)])
    im = np.clip(im, 0, 255); al = im[..., 3:4]
    im[..., :3] = np.where(al > 0.5, im[..., :3] * 255.0 / np.maximum(al, 1e-3), 0)
    im[..., 3] = np.where(im[..., 3] < 20, 0, im[..., 3])
    return np.clip(im, 0, 255)


def raw_face_rows(d):
    """Top and chin rows of heads/face/<dir> as painted (s 1, no move)."""
    a = np.asarray(Image.open(f"{HEADS}/face/{'right' if d == 'left' else d}.png").convert("RGBA"))[..., 3] > 128
    ys = np.where(a.any(1))[0]
    return ys[0], ys[-1]


def down_anchor(f):
    src = f"{OLD}/head-anchors.json" if os.path.exists(f"{OLD}/head-anchors.json") else HEAD_JSON
    return next(e for e in json.load(open(src, encoding="utf-8"))["anims"] if e["anim"] == "idle" and e["dir"] == "down")["frames"][f]


def front_chin():
    """Chin row of the game's front head (old down anchor): the other facings put their chin there too."""
    a = down_anchor(0)
    return F - (F - raw_face_rows("down")[1]) * a["s"] + a["y"]


def head_anchor(d, f):
    """Down keeps the old front head anchor (s 0.833, raised 27 px, breathing by sy: the chibi front head is drawn smaller).
    The other facings use the same head scale SIDE_S with the chin on the front chin row (user 10/11: side heads were 20 % bigger
    and the body shorter than the front), neck lift BREATH."""
    if d == "down":
        return down_anchor(f)
    y = front_chin() - (F - (F - raw_face_rows(d)[1]) * SIDE_S)
    return {"x": 0.0, "y": round(y - BREATH[f], 3), "s": SIDE_S, "sy": 1.0, "angle": 0.0}


def placed(img, a):
    """192 cell moved like HeroHeadLayer: scale s (x) / s*sy (y) about the cell bottom centre, then move x, y (no rotation used here)."""
    s = a.get("s") or 1.0; sy = s * (a.get("sy") or 1.0)
    return img.transform((F, F), Image.AFFINE, (1 / s, 0, F / 2 - (F / 2 + a["x"]) / s, 0, 1 / sy, F - (F + a["y"]) / sy), Image.BILINEAR)


def body_frame(cell, d, f):
    if d == "down":                                              # vertical scale about the cell bottom by the head's sy
        sy = head_anchor(d, f).get("sy") or 1.0
        if sy == 1.0:
            return cell
        im = resize(cell, F, round(F * sy)); out = np.zeros_like(cell); out[F - im.shape[0]:] = im
        return out
    return breathe(cell, BREATH[f])


def map_y(d, f, y):
    if d == "down":
        return F - (F - y) * (head_anchor(d, f).get("sy") or 1.0)
    return breath_y(y, BREATH[f])


def template(d):
    face = Image.open(f"{HEADS}/face/{'right' if d == 'left' else d}.png").convert("RGBA")
    if d == "left":
        face = face.transpose(Image.FLIP_LEFT_RIGHT)
    return np.asarray(placed(face, head_anchor(d, 0)))[..., 3] > 128


def fit(d, fig):
    """Cell with the figure: height from the face template top to SOLE, x so the drawn head overlaps the template best."""
    t = template(d); top = np.where(t.any(1))[0][0]
    rows = np.where(fig[..., 3].max(1) > 128)[0]
    fig = fig[rows[0]:rows[-1] + 1]
    k = (SOLE - top + 1) / fig.shape[0]
    im = resize(fig, round(fig.shape[1] * k), SOLE - top + 1)
    band = t[top:top + 40]
    best = None
    for x0 in range(0, F - im.shape[1] + 1):
        m = np.zeros((40, F), bool); m[:, x0:x0 + im.shape[1]] = im[:40, :, 3] > 128
        iou = (m & band).sum() / max(1, (m | band).sum())
        if best is None or iou > best[0]:
            best = (iou, x0)
    cell = np.zeros((F, F, 4), np.float32)
    cell[top:SOLE + 1, best[1]:best[1] + im.shape[1]] = im
    print(f"{d}: scale {k:.3f}, head IoU {best[0]:.3f}, x0 {best[1]}")
    return cell if d == "down" else refit_side(d, fig)


def refit_side(d, fig):
    """Up / left / right: the drawn head is bigger than the game head (SIDE_S); scale the figure so chin -> sole equals the
    front's, centre the drawn head on the game head. Drawn chin found by matching the head at s 1 (IoU 0.97+)."""
    t1top, t1chin = raw_face_rows(d)
    k0 = (SOLE - t1top + 1) / fig.shape[0]
    chin_src = (t1chin - t1top) / k0
    chin = front_chin()
    k = (SOLE - chin) / (fig.shape[0] - 1 - chin_src)
    h = round(fig.shape[0] * k); im = resize(fig, round(fig.shape[1] * k), h)
    top = SOLE + 1 - h
    head = im[:round(chin_src * k), :, 3] > 128
    hx = np.where(head.any(0))[0]
    tx = np.where(template(d).any(0))[0]
    x0 = int(round((tx[0] + tx[-1]) / 2 - (hx[0] + hx[-1]) / 2))
    cell = np.zeros((F, F, 4), np.float32)
    cell[max(top, 0):SOLE + 1, x0:x0 + im.shape[1]] = im[max(0, -top):]
    print(f"  {d} refit: scale {k:.3f} (head s {SIDE_S:.3f}), chin row {chin:.1f}, x0 {x0}")
    return cell


def headless(d, cell):
    """Remove the drawn bald head under the template (grown 2 px); white fur of the collar stays."""
    grow = ndimage.binary_dilation(template(d), iterations=2)
    r, g, b = cell[..., 0], cell[..., 1], cell[..., 2]
    fur = (np.minimum.reduce([r, g, b]) > 150) & (np.maximum.reduce([r, g, b]) - np.minimum.reduce([r, g, b]) < 60)
    fur[:FUR_FROM] = False                                       # above it white = the shine of the bald scalp
    out = cell.copy(); out[grow & ~fur] = 0
    if d == "down":                                              # the drawn front ears stick out past the smaller game head
        wide = ndimage.binary_dilation(template(d), iterations=8)
        wide[np.where(template(d).any(1))[0][-1] + 2:] = False   # only down to the chin of the game head
        out[wide & ~fur] = 0
    else:                                                        # the whole drawn head (bigger than the game head) above the chin
        chin = int(round(front_chin())) + 1
        fur2 = fur.copy(); fur2[:chin - 12] = False
        above = np.zeros((F, F), bool); above[:chin] = True
        out[above & ~fur2] = 0
    lab, n = ndimage.label(out[..., 3] > 0)                       # crumbs left around the head
    for i in range(1, n + 1):
        m = lab == i
        near_head = ndimage.binary_dilation(grow, iterations=3)[m].any() or (d != "down" and np.where(m)[0].min() < front_chin() + 2)
        if m.sum() < 15 and near_head:
            out[m] = 0
    return out


def breathe(cell, lift):
    """Vertical scale about the sole so row NECK rises by lift px."""
    if lift == 0:
        return cell
    k = (SOLE + 1 - NECK + lift) / (SOLE + 1 - NECK)
    h = round((SOLE + 1) * k)
    im = resize(cell[:SOLE + 1], F, h)
    out = np.zeros_like(cell); out[SOLE + 1 - min(h, SOLE + 1):SOLE + 1] = im[h - min(h, SOLE + 1):]
    return out


def breath_y(y, lift):
    return SOLE - (SOLE - y) * (SOLE + 1 - NECK + lift) / (SOLE + 1 - NECK)


def fists(cell, from_row=110):
    """Centres of the fist skin blobs below the head (down: the raised fist reaches row ~100, its ears end higher)."""
    r, g, b, a = (cell[..., i] for i in range(4))
    sk = (a > 200) & (r > 200) & (g > 140) & (b > 100) & (r - b > 30) & (g < 240)
    sk[:from_row] = False
    lab, n = ndimage.label(sk)
    out = [ndimage.center_of_mass(lab == i)[::-1] for i in range(1, n + 1) if (lab == i).sum() >= 20]
    return [(round(x, 1), round(y, 1)) for x, y in out]


def staff_fist(d, fs):
    """The staff hand: up = screen right (behind), left = the forward fist (behind), right = the side fist on the robe (front)."""
    big = [f for f in fs if f[1] > 95]
    if d == "up":
        return max(big)
    return min(big)                                              # down: screen left fist; left: forward fist; right: side fist


def book_fist(fs):
    return max(f for f in fs if f[1] > 95)                       # down: screen right fist


def hand_layer(cell, fist, radius=9):
    r, g, b, a = (cell[..., i] for i in range(4))
    yy, xx = np.mgrid[:F, :F]
    disk = (xx - fist[0]) ** 2 + (yy - fist[1]) ** 2 <= radius ** 2
    sk = (a > 0) & (r > 190) & (g > 130) & (r - b > 25)
    keep = disk & (sk | (ndimage.binary_dilation(sk & disk, iterations=1) & (r + g + b < 250)))
    out = np.zeros_like(cell); out[keep] = cell[keep]
    return out


def dyed_hair(d, style=0, dye=STYLE0_DYE, path=None):
    p = path or f"{HEADS}/hair/{style}/{'right' if d == 'left' else d}.png"
    h = np.asarray(Image.open(p).convert("RGBA")).astype(np.float32)
    if d == "left":
        h = h[:, ::-1]
    c = h[..., :3]; mx, mn = c.max(2), c.min(2); l = c.mean(2) / 255
    dc = np.array(dye, np.float32) / 255
    lo = dc * 0.16 + (dc - dc * 0.16) * (np.clip(l / 0.6, 0, 1) ** 0.9)[..., None]
    hi = dc + (1 - dc) * (np.clip((l - 0.6) / 0.4, 0, 1) * 0.75)[..., None]
    o = np.where((l <= 0.6)[..., None], lo, hi) * 255
    gray = (mx - mn) <= 30
    h[..., :3] = np.where(gray[..., None], o, c)
    return h


def under_img(d, style=0, f=0):
    """hair-under/<style> (behind the body) placed like the head; an empty cell when the style has none."""
    p = f"{HEADS}/hair-under/{style}/{'right' if d == 'left' else d}.png"
    if not os.path.exists(p):
        return Image.new("RGBA", (F, F))
    h = dyed_hair(d, style, path=p)
    return placed(Image.fromarray(h.astype(np.uint8)), head_anchor(d, f))


def head_img(d, style=0, f=0):
    face = Image.open(f"{HEADS}/face/{'right' if d == 'left' else d}.png").convert("RGBA")
    if d == "left":
        face = face.transpose(Image.FLIP_LEFT_RIGHT)
    face.alpha_composite(Image.fromarray(dyed_hair(d, style).astype(np.uint8)))
    return placed(face, head_anchor(d, f))


def sheet_rows(path_old, cells_by_dir):
    s = Image.open(path_old).convert("RGBA")
    for d, cells in cells_by_dir.items():
        for f, c in enumerate(cells):
            s.paste(Image.fromarray(c.astype(np.uint8)), (f * F, ROWS[d] * F))
    return s


def cmd_fit():
    os.makedirs(STAGE, exist_ok=True)
    figs = figures()
    bodies, heads_, hands, bares, anchors = {}, {}, {}, {}, {}
    for d in DIRS:
        cell = fit(d, figs[d])
        hl0 = headless(d, cell)
        fs = fists(cell, 96 if d == "down" else 110); sf = staff_fist(d, fs); bf = book_fist(fs) if d == "down" else None
        print(f"  fists {fs} -> staff at {sf}, book at {bf}")
        bodies[d] = [body_frame(hl0, d, f) for f in range(4)]
        hands[d] = []
        for f in range(4):
            h = np.zeros_like(cell)
            for fist in ([sf] if d == "right" else [sf, bf] if d == "down" else []):
                h = np.maximum(h, hand_layer(body_frame(cell, d, f), (fist[0], map_y(d, f, fist[1]))))
            hands[d].append(h)
        bares[d] = []
        for f, l in enumerate(BREATH):
            b = Image.fromarray(bodies[d][f].astype(np.uint8)); b.alpha_composite(head_img(d, 0, f)); bares[d].append(np.asarray(b).astype(np.float32))
        anchors[d] = {"head": [head_anchor(d, f) for f in range(4)],
                      "staff": [round(map_y(d, f, sf[1]), 3) for f in range(4)], "staffX": float(sf[0]), "staffAngle": STAFF_ANGLE.get(d)}
        if bf is not None:
            anchors[d]["bookX"] = float(bf[0]); anchors[d]["book"] = [round(map_y(d, f, bf[1]), 3) for f in range(4)]
            anchors[d]["staffFist"] = [float(sf[0]), float(sf[1])]; anchors[d]["bookFist"] = [float(bf[0]), float(bf[1])]
    sheet_rows(f"{BODY}/headless/idle.png", bodies).save(f"{STAGE}/headless-idle.png")
    sheet_rows(f"{BODY}/bare/idle.png", bares).save(f"{STAGE}/bare-idle.png")
    sheet_rows(f"{BODY}/hand/idle.png", hands).save(f"{STAGE}/hand-idle.png")
    json.dump(anchors, open(f"{STAGE}/anchors.json", "w"), indent=1)
    preview(anchors)


def staged_gear(anchors):
    chf = load("chf", f"{ROOT}/Tools/compose-hero-frame.py")
    _, gear = chf.load_gear("wizard")
    for d in DIRS:
        if d == "down":
            continue                                             # down draws the outfit idle-stance art: compose_down
        old = gear[("staff", "idle", d)]
        ang = anchors[d].get("staffAngle")
        gear[("staff", "idle", d)] = [dict(old[f], x=anchors[d]["staffX"], y=anchors[d]["staff"][f], **({"angle": ang} if ang is not None else {})) for f in range(4)]
    return chf, gear


def outfit_down(anchors):
    """body/held-anchors.json idle-down frames moved onto the new fists."""
    o = json.load(open(OUTFIT_JSON, encoding="utf-8"))
    out = {}
    for e in o["anchors"]:
        if e["anim"] == "idle" and e["dir"] == "down" and e["part"] in ("staff", "book"):
            k = e["part"]
            out[k] = [dict(fr, x=anchors["down"][k + "X"], y=anchors["down"][k][f]) for f, fr in enumerate(e["frames"])]
    return out


def compose_down(f, style, tier, anchors, sheets=STAGE, pad=40):
    """Idle down as HeroRenderer draws it: headless, head, staff / book idle-stance art (centred, s, clockwise angle, artY), hand layer."""
    import math
    chf = load("chf", f"{ROOT}/Tools/compose-hero-frame.py")
    file, _ = chf.load_gear("wizard")
    cv = Image.new("RGBA", (F + 2 * pad, F + 2 * pad))
    hl = f"{sheets}/headless-idle.png" if sheets == STAGE else f"{BODY}/headless/idle.png"
    hd = f"{sheets}/hand-idle.png" if sheets == STAGE else f"{BODY}/hand/idle.png"
    cv.alpha_composite(under_img("down", style, f), (pad, pad))
    cv.alpha_composite(chf.cell(hl, 0, f), (pad, pad))
    cv.alpha_composite(head_img("down", style, f), (pad, pad))
    for part, folder, key in (("staff", "staff", "weapon"), ("book", "book", "book")):
        a = outfit_down(anchors)[part][f]
        art = Image.open(f"{RES}/Sprites/classes/wizard/weapons/{folder}/{file[key][tier]['name']}/{a['art']}.png").convert("RGBA")
        k = a["s"]; th = math.radians(a["angle"]); c, n = math.cos(th), math.sin(th)
        px, py = a["x"] + pad, a["y"] + a.get("artY", 0) + pad
        ax, bx, ay, by = c / k, n / k, -n / k, c / k
        cx = art.width / 2 - ax * px - bx * py; cy = art.height / 2 - ay * px - by * py
        cv.alpha_composite(art.transform(cv.size, Image.AFFINE, (ax, bx, cx, ay, by, cy), Image.BILINEAR))
    cv.alpha_composite(chf.cell(hd, 0, f), (pad, pad))
    return cv


def preview(anchors):
    chf, gear = staged_gear(anchors)
    tiles = []
    for d in DIRS:
        for tier in (0, 9):
            for style in range(6):
                for f in (0, 2):
                    if d == "down":
                        im = compose_down(f, style, tier, anchors)
                    else:
                        im = chf.compose("wizard", "idle", d, f, bare_path=f"{STAGE}/headless-idle.png", hand_path=f"{STAGE}/hand-idle.png",
                                         anchors=gear, tier=tier, pad=40, over=head_img(d, style, f))
                    tiles.append(im)
    cols = 24
    out = Image.new("RGBA", (cols * 272, (len(tiles) + cols - 1) // cols * 272), (58, 62, 70, 255))
    for i, t in enumerate(tiles):
        out.alpha_composite(t, ((i % cols) * 272, (i // cols) * 272))
    out.save(f"{STAGE}/preview.png")
    print("preview", f"{STAGE}/preview.png")


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


def cmd_install():
    anchors = json.load(open(f"{STAGE}/anchors.json"))
    os.makedirs(OLD, exist_ok=True)
    for kind in ("headless", "bare", "hand"):
        if not os.path.exists(f"{OLD}/{kind}-idle.png"):
            shutil.copy(f"{BODY}/{kind}/idle.png", f"{OLD}/{kind}-idle.png")
        shutil.copy(f"{STAGE}/{kind}-idle.png", f"{BODY}/{kind}/idle.png")
    for p in (HEAD_JSON, HELD_JSON, OUTFIT_JSON):
        if not os.path.exists(f"{OLD}/{os.path.basename(p)}"):
            shutil.copy(p, f"{OLD}/{os.path.basename(p)}")
    h = json.load(open(HEAD_JSON, encoding="utf-8"))
    for e in h["anims"]:
        if e["anim"] == "idle" and e["dir"] in DIRS:
            e["frames"] = anchors[e["dir"]]["head"]
    json.dump(h, open(HEAD_JSON, "w", encoding="utf-8"), indent=2)
    g = json.load(open(HELD_JSON, encoding="utf-8"))
    for e in g["anchors"]:
        if e["anim"] == "idle" and e["dir"] == "down" and e["part"] in ("staff", "book"):
            k = e["part"]; fx, fy = anchors["down"][k + "Fist"]; ox, oy = OLD_DOWN_FISTS[k]
            og = json.load(open(f"{OLD}/{os.path.basename(HELD_JSON)}", encoding="utf-8"))   # from the pre-install copy: re-runs give the same result
            g0 = next(x for x in og["anchors"] if (x["part"], x["anim"], x["dir"]) == (k, "idle", "down"))["frames"][0]
            x0, y0 = g0["x"], g0["y"]
            for f, fr in enumerate(e["frames"]):                  # no-art anchor: keep its old offset from the fist
                fr["x"] = round(x0 - ox + fx, 3); fr["y"] = round(map_y("down", f, y0 - oy + fy), 3)
            continue
        if e["part"] == "staff" and e["anim"] == "idle" and e["dir"] in DIRS:
            for f, fr in enumerate(e["frames"]):
                fr["x"], fr["y"] = anchors[e["dir"]]["staffX"], anchors[e["dir"]]["staff"][f]
                if anchors[e["dir"]].get("staffAngle") is not None:
                    fr["angle"] = anchors[e["dir"]]["staffAngle"]
    json.dump(g, open(HELD_JSON, "w", encoding="utf-8"), indent=1)
    o = json.load(open(OUTFIT_JSON, encoding="utf-8"))
    moved = outfit_down(anchors)
    for e in o["anchors"]:
        if e["anim"] == "idle" and e["dir"] == "down" and e["part"] in moved:
            e["frames"] = moved[e["part"]]
    json.dump(o, open(OUTFIT_JSON, "w", encoding="utf-8"), indent=2)
    print("installed idle (4 facings); old copies in", OLD)


if __name__ == "__main__":
    {"fit": cmd_fit, "install": cmd_install}[sys.argv[1]]()

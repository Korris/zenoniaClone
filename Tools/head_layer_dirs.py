"""Head layer for idle + walk in every facing (2026-11-07), on top of the idle-down prototype (head_layer_masks / head_layer_styles).

One head per facing and style: heads/<style>/<dir>.png is a full 192 cell placed where frame 0 of `idle` in that facing has its head
(down: the existing idle-down head). Left has no head file: the game draws the right head mirrored (HeroHeadLayer), so left frames are
registered against the mirrored right head. Every other frame (idle breathing, walk bob/tilt, even walk frames painted with a slightly
different head) gets an anchor {x, y, s, sy, angle}: scale s (and sy vertically) and rotate by angle (degrees, clockwise on screen) about
the cell's bottom centre, then move by (x, y) frame px, y down. The anchor is found by registering the reference head onto the frame
(least absolute difference over the head, coarse to fine). The frame's own head (everything above the neck cut moved with the anchor)
is erased from the headless sheet; the drawn head is always the reference head of the chosen style, so walk frames show the idle face.
"""
import json
import os

import numpy as np
from PIL import Image
from scipy import ndimage

import head_layer_core as core
from head_layer_classes import CLASSES
from head_layer_dir_classes import DIRS

FRAME = core.FRAME
RES = core.RES
DIR_ROW = {"down": 0, "up": 1, "left": 2, "right": 3}
PIV = (FRAME / 2, float(FRAME))
IDENT = {"x": 0.0, "y": 0.0, "s": 1.0, "sy": 1.0, "angle": 0.0}


def sheet(cls, anim, kind="bare"):
    return np.array(Image.open(f"{RES}/Sprites/classes/{cls}/body/{kind}/{anim}.png").convert("RGBA")).astype(np.float64)


def cell(sh, row, f):
    return sh[row * FRAME:(row + 1) * FRAME, f * FRAME:(f + 1) * FRAME].copy()


def _src_coords(a, ys, xs):
    """Source (y, x) of output pixel centres for the anchor transform (inverse map)."""
    s, sy, ang = a.get("s", 1.0) or 1.0, a.get("sy", 1.0) or 1.0, np.radians(a.get("angle", 0.0))
    qx = xs + 0.5 - PIV[0] - a["x"]
    qy = ys + 0.5 - PIV[1] - a["y"]
    c, n = np.cos(ang), np.sin(ang)
    rx, ry = c * qx + n * qy, -n * qx + c * qy  # rotate back
    return ry / (s * sy) + PIV[1] - 0.5, rx / s + PIV[0] - 0.5


def place(head, a, order=1, ys=None, xs=None):
    """Head cell placed by an anchor the way HeroHeadLayer draws it (scale, rotate about the cell bottom centre, move)."""
    if ys is None:
        ys, xs = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    sy_, sx_ = _src_coords(a, ys, xs)
    p = core.pm(head)
    out = np.stack([ndimage.map_coordinates(p[..., c], [sy_, sx_], order=order, mode="constant") for c in range(4)], -1)
    return np.clip(out, 0, 255)  # premultiplied


def fwd(a, x, y):
    """Forward transform of a frame-0 point (pixel centre coordinates)."""
    s, sy, ang = a.get("s", 1.0) or 1.0, a.get("sy", 1.0) or 1.0, np.radians(a.get("angle", 0.0))
    qx, qy = (x + 0.5 - PIV[0]) * s, (y + 0.5 - PIV[1]) * s * sy
    c, n = np.cos(ang), np.sin(ang)
    return c * qx - n * qy + PIV[0] + a["x"] - 0.5, n * qx + c * qy + PIV[1] + a["y"] - 0.5


def cut_cfg(cls, d):
    """Neck cut settings of a facing (down = the idle-down prototype's polyline settings, Warrior has its own in DIRS)."""
    if d in DIRS[cls]:
        return DIRS[cls][d]
    c = CLASSES[cls]
    return {"cut": c["cut"], "chin": c["chin"], "chin_head": c["chin_head"], "keep": c["keep"]}


def head_mask(fr, cfg, a, hair):
    """Head pixels of one frame: solid above the cut moved by the anchor, plus chin-box pixels the class test accepts."""
    al = fr[..., 3]
    solid = al > 8
    light = solid & (fr[..., :3] @ [0.3, 0.59, 0.11] >= 90)
    rows = np.arange(FRAME)[:, None] * np.ones((1, FRAME))
    cols = np.ones((FRAME, 1)) * np.arange(FRAME)[None, :]
    pts = [fwd(a, x, y) for x, y in cfg["cut"]]
    cut = core.polyline(sorted(pts))[None, :]
    head = solid & (rows < cut)
    limit = float(np.max(cut))
    if cfg.get("chin"):
        x0, x1, y0, y1 = cfg["chin"]
        (bx0, by0), (bx1, by1) = fwd(a, x0, y0), fwd(a, x1, y1)
        box = (cols >= bx0) & (cols < bx1) & (rows >= by0) & (rows < by1)
        ok = cfg["chin_head"](fr) & light
        _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
        head |= solid & box & np.where(light, ok, ok[iy, ix])
        limit = max(limit, by1)
    head = core.largest(head, extra_min=12)
    if cfg.get("hair_to"):  # hair hanging below the neck cut (ponytail, back tips over the scarf) goes with the head when it joins it
        _, hy = fwd(a, 0, cfg["hair_to"])
        hl = hair(fr) & solid
        _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
        hl |= solid & ~light & hl[iy, ix]
        hl &= (rows < hy) & ~head
        lab, n = ndimage.label(hl | head, np.ones((3, 3)))
        ids = np.unique(lab[head])
        head |= hl & np.isin(lab, ids[ids > 0])
        limit = max(limit, hy)
    head = core.assign_faint(head, al, int(limit))
    kx0, kx1 = cfg["keep"]
    kx0, kx1 = fwd(a, kx0, 0)[0], fwd(a, kx1, 0)[0]
    keep = head & (al > 250) & (cols >= kx0) & (cols < kx1) & ~hair(fr) & ndimage.binary_dilation(solid & ~head, iterations=2)
    return head, keep


def head_cell(fr, head):
    o = fr.copy()
    o[~head] = 0
    return o


def mirror(c):
    return c[:, ::-1].copy()


def register(ref, fr, start=None):
    """Anchor placing the reference head best onto the frame's head: integer shift, then scale / tilt, then sub-pixel."""
    ra = ref[..., 3] > 0
    ys_, xs_ = np.where(ra)
    y0, y1 = max(ys_.min() - 16, 0), min(ys_.max() - 4, FRAME)  # stop above the chin: the neck and collar differ between frames
    x0, x1 = max(xs_.min() - 16, 0), min(xs_.max() + 17, FRAME)
    yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float64)
    tgt = core.pm(fr)[y0:y1, x0:x1]

    def cost(a):
        p = place(ref, a, 1, yy, xx)
        m = (p[..., 3] > 40) | (tgt[..., 3] > 40)
        return np.abs(p - tgt)[m].mean() if m.any() else 1e9

    best = (cost(dict(IDENT)), dict(IDENT))
    base = start or IDENT
    for dx in range(-14, 15, 2):
        for dy in range(-14, 15, 2):
            a = dict(base, x=base["x"] + dx, y=base["y"] + dy)
            best = min(best, (cost(a), a), key=lambda t: t[0])
    for ds, dang, dxy, n in ((0.02, 2.0, 1.0, 2), (0.008, 1.0, 0.5, 1), (0.003, 0.4, 0.25, 1)):
        improved = True
        while improved:
            improved = False
            b = best[1]
            for k in range(-n, n + 1):
                for g in range(-n, n + 1):
                    for i in (-1, 0, 1):
                        for j in (-1, 0, 1):
                            a = dict(b, s=b["s"] + k * ds, angle=b["angle"] + g * dang, x=b["x"] + i * dxy, y=b["y"] + j * dxy)
                            if abs(a["angle"]) > 10 or not 0.85 < a["s"] < 1.15:
                                continue
                            c = cost(a)
                            if c < best[0] - 1e-6:
                                best, improved = (c, a), True
    a = best[1]
    return {"x": round(a["x"], 3), "y": round(a["y"], 3), "s": round(a["s"], 4), "sy": 1.0, "angle": round(a["angle"], 2)}, best[0]


def hair_test(cls):
    return CLASSES[cls]["hair"]


def ref_head(cls, d):
    """Style 0 reference head of a facing (full cell, straight alpha) and the idle frame-0 cut mask it came from."""
    if d == "down":
        return np.array(Image.open(f"{RES}/Sprites/classes/{cls}/heads/0/down.png").convert("RGBA")).astype(np.float64)
    if d == "left":
        return mirror(ref_head(cls, "right"))
    f0 = cell(sheet(cls, "idle"), DIR_ROW[d], 0)
    head, _ = head_mask(f0, cut_cfg(cls, d), IDENT, hair_test(cls))
    return head_cell(f0, head)


def jobs(cls):
    """(anim, dir) rows this module builds: every idle / walk row except idle-down (the prototype's)."""
    return [(an, d) for an in ("idle", "walk") for d in ("down", "up", "right", "left") if not (an == "idle" and d == "down")]


def build(cls, log=print):
    """Anchors, headless frames and head masks of every job row. Left rows use the right cut mirrored through the anchor."""
    hair = hair_test(cls)
    refs = {d: ref_head(cls, d) for d in ("down", "up", "right", "left")}
    out = {}
    for an, d in jobs(cls):
        sh = sheet(cls, an)
        cfg = cut_cfg(cls, "right" if d == "left" else d)
        if d == "left":  # mirror the cut of the right facing into left frame-0 space; the anchor then moves it per frame
            cfg = dict(cfg, cut=[(FRAME - 1 - x, y) for x, y in cfg["cut"]],
                       chin=(lambda c: (FRAME - c[1], FRAME - c[0], c[2], c[3]))(cfg["chin"]) if cfg.get("chin") else None,
                       keep=(FRAME - cfg["keep"][1], FRAME - cfg["keep"][0]))
        frames, cells, heads = [], [], []
        prev = None
        for f in range(4):
            fr = cell(sh, DIR_ROW[d], f)
            if an == "idle" and f == 0 and d in ("up", "right"):
                a, c = dict(IDENT), 0.0
            else:
                a, c = register(refs[d], fr, prev)
            prev = a
            head, keep = head_mask(fr, cfg, a, hair)
            hl = fr.copy()
            hl[head & ~keep] = 0
            # old hair tips the cut left floating beside the erased head (a dash left of the ear once a shorter style is on)
            lab, n = ndimage.label(hl[..., 3] > 8, np.ones((3, 3)))
            near = ndimage.binary_dilation(head, iterations=3)
            hairy = hair(fr) | (fr[..., :3] @ [0.3, 0.59, 0.11] < 60)  # hair colour or its dark outline
            for i in range(1, n + 1):
                blob = lab == i
                size = blob.sum()
                # Assassin: ponytail tips below `hair_to` stayed on the body and float once a shorter style is on
                if (size <= 24 and (blob & near).any()) or (size <= 80 and (blob & hairy).sum() > 0.6 * size):
                    hl[blob] = 0
            frames.append(a)
            cells.append(hl)
            heads.append(head)
            log(f"{cls} {an} {d} f{f}: {a} cost {c:.2f} head px {int(head.sum())}")
        out[(an, d)] = {"anchors": frames, "headless": cells, "heads": heads}
    return refs, out


def write(cls, refs, out):
    p = core.paths(cls)
    for an in ("idle", "walk"):
        path = f"{RES}/Sprites/classes/{cls}/body/headless/{an}.png"
        sh = np.array(Image.open(path).convert("RGBA")) if os.path.exists(path) else np.zeros((FRAME * 4, FRAME * 4, 4), np.uint8)
        for (a2, d), v in out.items():
            if a2 != an:
                continue
            r = DIR_ROW[d]
            for f, c in enumerate(v["headless"]):
                sh[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME] = core.u8(c)
        Image.fromarray(sh).save(path)
    for d in ("up", "right"):
        Image.fromarray(core.u8(refs[d])).save(f"{p['heads']}/0/{d}.png")
    with open(p["anchors"], encoding="utf-8") as fh:
        data = json.load(fh)
    keep = [x for x in data["anims"] if (x["anim"], x["dir"]) not in out]
    data["anims"] = keep + [{"anim": an, "dir": d, "frames": v["anchors"]} for (an, d), v in out.items()]
    data["note"] = data["note"].rstrip(".") + ("; s = uniform scale and angle = clockwise degrees about the cell bottom centre (Tools/head_layer_dirs.py). "
                                               "Left facings draw the right head mirrored.")
    with open(p["anchors"], "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
        fh.write("\n")


def read_out(cls):
    """Anchors and headless frames as written (for `check` after `write`)."""
    p = core.paths(cls)
    with open(p["anchors"], encoding="utf-8") as fh:
        anims = {(x["anim"], x["dir"]): x["frames"] for x in json.load(fh)["anims"]}
    out = {}
    for an in ("idle", "walk"):
        sh = sheet(cls, an, "headless")
        for d in ("down", "up", "right", "left"):
            if (an, d) in anims:
                out[(an, d)] = {"anchors": anims[(an, d)], "headless": [cell(sh, DIR_ROW[d], f) for f in range(4)]}
    return out


def head_of(cls, style, d):
    """Head art of a style and facing as the game picks it (left = right mirrored); None when not painted."""
    hd = core.paths(cls)["heads"]
    if d == "left":
        h = head_of(cls, style, "right")
        return None if h is None else mirror(h)
    f = f"{hd}/{style}/{d}.png"
    return np.array(Image.open(f).convert("RGBA")).astype(np.float64) if os.path.isfile(f) else None


def head_of_ref(cls, d):
    return ref_head(cls, d)


def composite(headless, head, a):
    return core.over(headless, core.straight(place(head, a)))

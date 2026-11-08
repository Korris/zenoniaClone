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
# Classes whose idle bare sheets still wear the dropped hat while heads/0 is bare-headed (Cleric 2026-11-07): anims-write registers new attack
# rows (painted bare-headed) against heads/0/<dir>.png instead of the hat head cut from idle.
STYLE0_REF = {"cleric"}


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


def head_mask(fr, cfg, a, hair, ref=None):
    """Head pixels of one frame: solid above the cut moved by the anchor, plus chin-box pixels the class test accepts.
    ref (attack anims): the reference head placed by the anchor; above the cut only pixels inside its silhouette (+3 px) or of the hair
    colour count as head, so an arm raised over the head (Warrior melee4 jump) stays on the body."""
    al = fr[..., 3]
    solid = al > 8
    light = solid & (fr[..., :3] @ [0.3, 0.59, 0.11] >= 90)
    rows = np.arange(FRAME)[:, None] * np.ones((1, FRAME))
    cols = np.ones((FRAME, 1)) * np.arange(FRAME)[None, :]
    pts = [fwd(a, x, y) for x, y in cfg["cut"]]
    cut = core.polyline(sorted(pts))[None, :]
    head = solid & (rows < cut)
    if ref is not None:
        inside = ndimage.binary_dilation(ref[..., 3] > 40, iterations=3)
        hl = hair(fr) & solid
        hl |= solid & ~light & ndimage.binary_dilation(hl, iterations=2)  # dark outlines of the old hair spikes (within 2 px of the hair)
        head &= inside | hl
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


def arm_over_head(fr, head, ref, hair):
    """Attack frames: pixels of an arm / gauntlet raised across the head (Warrior melee3up, melee4). Inside the reference head's silhouette
    they are not hair, not skin, and belong to a blob that also reaches outside the silhouette (the arm comes in from outside), so the
    head's own outline never counts. Written into the hand sheet (drawn above the head layer) so the arm stays in front of the hair."""
    from head_layer_classes import skin
    solid = fr[..., 3] > 8
    inside = ndimage.binary_dilation(ref[..., 3] > 40, iterations=1)
    lum = fr[..., :3] @ [0.3, 0.59, 0.11]
    cand = solid & ~hair(fr) & ~skin(fr) & (lum >= 55)  # gauntlet metal / leather; the hair's near-black outline is not an arm
    lab, n = ndimage.label(cand, np.ones((3, 3)))
    over = np.zeros(cand.shape, bool)
    for i in range(1, n + 1):
        blob = lab == i
        out_px, in_px = (blob & ~inside).sum(), (blob & inside & head).sum()
        if in_px >= 6 and out_px >= 40:
            over |= blob & inside & head
    over |= ndimage.binary_dilation(over, iterations=1) & (lum < 55)  # the arm's own outline
    return over & solid & head


_HAND = {}


def hand_alpha(cls, anim, d, f):
    """Fist layer pixels of a frame (body/hand/<anim>.png as painted, i.e. its ArtLibrary .pre-head.png copy when there is one)."""
    if (cls, anim) not in _HAND:
        path = f"{RES}/Sprites/classes/{cls}/body/hand/{anim}.png"
        pre = path.replace(".png", ".pre-head.png").replace("/Resources/", "/ArtLibrary/").replace("/Sprites/", "/")
        src = pre if os.path.exists(pre) else path
        _HAND[(cls, anim)] = np.array(Image.open(src).convert("RGBA"))[..., 3] > 8 if os.path.exists(src) else None
    m = _HAND[(cls, anim)]
    r = DIR_ROW[d]
    if m is None or (f + 1) * FRAME > m.shape[1]:
        return np.zeros((FRAME, FRAME), bool)
    return m[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME].copy()


def attack_guard(cls, anim, d, f, head, placed, fr):
    """Attack rows of a class whose hair test also takes arm colours (Mechanic 2026-11-08: brown gloves, leather and the pink shade of the
    forearms pass the dark-hair clause, so fists and forearms raised beside the head were erased with it). Head pixels must lie within
    `attack_guard` px of the placed reference head (+3 px dilation) and never on the fist layer; arm pixels left inside it (attack_guard_arms)
    stay on the body. Returns (head, fist mask, arm mask): the arm pixels also go into the hand sheet so they stay in front of any hairstyle."""
    near = ndimage.binary_dilation(placed[..., 3] > 40, iterations=3 + CLASSES[cls]["attack_guard"])
    fist = hand_alpha(cls, anim, d, f)
    head = head & near & ~fist
    arm = attack_guard_arms(cls, fr, head, placed, fist)
    return head & ~arm, fist, arm


def attack_guard_arms(cls, fr, head, placed, fist):
    """Arm pixels inside the head mask: blobs of bright pixels that are not the hair's core colour (`hair_core`), joined to a fist or running
    25+ px outside the reference head above its lowest rows (forearm, glove), and small inside it (a face / neck blob is large)."""
    solid = fr[..., 3] > 8
    lum = fr[..., :3] @ [0.3, 0.59, 0.11]
    # not the hair: no core colour, bright, and either off the hair test or bluish enough for skin shade (b >= 70; the old hair's orange tips and
    # its dark shade beside a glove have b < 60 and would be drawn over the new head from the hand sheet)
    loose = ~CLASSES[cls]["hair"](fr) | (fr[..., 2] >= 70) | ndimage.binary_dilation(fist, iterations=2)
    cand = (solid & ~CLASSES[cls]["hair_core"](fr) & (lum >= 60) & loose) | fist
    inside = ndimage.binary_dilation(placed[..., 3] > 40, iterations=1)
    ys = np.where(head.any(1))[0]
    if not len(ys):
        return np.zeros_like(head)
    above = np.arange(FRAME)[:, None] < ys.max() - 4
    lab, n = ndimage.label(cand, np.ones((3, 3)))
    arm = np.zeros_like(head)
    for i in range(1, n + 1):
        blob = lab == i
        hit = blob & head
        held = (blob & fist).any()  # a forearm joined to its fist may lie well inside the old hair (attack3 up, arms raised beside the head)
        if hit.any() and hit.sum() <= (450 if held else 160) and (held or (blob & ~inside & above).sum() >= 25):
            arm |= blob
    arm &= head
    return arm | (ndimage.binary_dilation(arm, iterations=1) & head & solid & (lum < 60))  # the arm's own dark outline


def head_cell(fr, head):
    o = fr.copy()
    o[~head] = 0
    return o


def mirror(c):
    return c[:, ::-1].copy()


def register(ref, fr, start=None, s_range=(0.85, 1.15), ang_max=10):
    """Anchor placing the reference head best onto the frame's head: integer shift, then scale / tilt, then sub-pixel."""
    ra = ref[..., 3] > 0
    ys_, xs_ = np.where(ra)
    mg = 16 if ang_max <= 10 else 36  # ang_max > 10: a head thrown back (start tilted, see build) leaves the narrow window and the fit slides
    y0, y1 = max(ys_.min() - mg, 0), min(ys_.max() - 4, FRAME)  # stop above the chin: the neck and collar differ between frames
    x0, x1 = max(xs_.min() - mg, 0), min(xs_.max() + mg + 1, FRAME)
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
                            if abs(a["angle"]) > max(10, ang_max) or not s_range[0] < a["s"] < s_range[1]:
                                continue
                            c = cost(a)
                            if c < best[0] - 1e-6:
                                best, improved = (c, a), True
    a = best[1]
    return {"x": round(float(a["x"]), 3), "y": round(float(a["y"]), 3), "s": round(a["s"], 4), "sy": 1.0, "angle": round(a["angle"], 2)}, best[0]


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


def jobs(cls, anims=None):
    """(anim, dir) rows this module builds: every idle / walk row except idle-down (the prototype's), or every row of the given anims."""
    if anims:
        return [(an, d) for an in anims for d in ("down", "up", "right", "left")]
    return [(an, d) for an in ("idle", "walk") for d in ("down", "up", "right", "left") if not (an == "idle" and d == "down")]


def frame_count(sh):
    return sh.shape[1] // FRAME


def build(cls, log=print, anims=None):
    """Anchors, headless frames and head masks of every job row. Left rows use the right cut mirrored through the anchor."""
    hair = hair_test(cls)
    refs = {d: ref_head(cls, d) for d in ("down", "up", "right", "left")}
    if anims and cls in STYLE0_REF:  # attack rows painted bare-headed: register the drawn style 0 head, not the old hat cut from idle
        refs = {d: head_of(cls, 0, d) for d in ("down", "up", "right", "left")}
    out = {}
    for an, d in jobs(cls, anims):
        sh = sheet(cls, an)
        cfg = cut_cfg(cls, "right" if d == "left" else d)
        if d == "left":  # mirror the cut of the right facing into left frame-0 space; the anchor then moves it per frame
            cfg = dict(cfg, cut=[(FRAME - 1 - x, y) for x, y in cfg["cut"]],
                       chin=(lambda c: (FRAME - c[1], FRAME - c[0], c[2], c[3]))(cfg["chin"]) if cfg.get("chin") else None,
                       keep=(FRAME - cfg["keep"][1], FRAME - cfg["keep"][0]))
        frames, cells, heads, overs = [], [], [], []
        prev = None
        for f in range(frame_count(sh)):
            fr = cell(sh, DIR_ROW[d], f)
            if an == "idle" and f == 0 and d in ("up", "right"):
                a, c = dict(IDENT), 0.0
            else:
                tilt = CLASSES[cls].get("attack_tilt", {}).get((an, "right" if d == "left" else d, f)) if anims else None
                start, amax = prev, 10
                if tilt is not None:  # head thrown back past 10 degrees: start the fit at that tilt, head centre (~127 px up) kept in place
                    tilt = -tilt if d == "left" else tilt
                    b = prev or IDENT
                    t0, t1 = np.radians(b["angle"]), np.radians(tilt)
                    start = dict(b, angle=float(tilt), x=b["x"] - 127 * (np.sin(t1) - np.sin(t0)), y=b["y"] - 127 * (np.cos(t0) - np.cos(t1)))
                    amax = abs(tilt) + 10
                a, c = register(refs[d], fr, start, CLASSES[cls].get("attack_s_range", (0.85, 1.15)) if anims else (0.85, 1.15), amax)
            prev = a
            placed = core.straight(place(refs[d], a)) if anims else None
            head, keep = head_mask(fr, cfg, a, hair, placed)
            fist = arm = None
            if anims and CLASSES[cls].get("attack_guard") is not None:
                head, fist, arm = attack_guard(cls, an, d, f, head, placed, fr)
            if anims:
                over = fr.copy()
                om = arm_over_head(fr, head, placed, hair)
                if arm is not None:
                    om |= arm
                over[~om] = 0
                overs.append(over)
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
            if fist is not None:  # fists and arms stay on the body whatever the cleanup took (the hand layer draws them above the head anyway)
                hl[fist | arm] = fr[fist | arm]
            frames.append(a)
            cells.append(hl)
            heads.append(head)
            log(f"{cls} {an} {d} f{f}: {a} cost {c:.2f} head px {int(head.sum())}")
        out[(an, d)] = {"anchors": frames, "headless": cells, "heads": heads, "over": overs}
    return refs, out


def write(cls, refs, out, heads=True):
    """heads=False (attack anims): only headless sheets and anchors; the style 0 head files stay (Warrior right is scaled, dirs-scale)."""
    p = core.paths(cls)
    for an in sorted({a for a, _ in out}):
        path = f"{RES}/Sprites/classes/{cls}/body/headless/{an}.png"
        n = len(next(v for (a2, _), v in out.items() if a2 == an)["headless"])
        sh = np.array(Image.open(path).convert("RGBA")) if os.path.exists(path) else np.zeros((FRAME * 4, FRAME * n, 4), np.uint8)
        for (a2, d), v in out.items():
            if a2 != an:
                continue
            r = DIR_ROW[d]
            for f, c in enumerate(v["headless"]):
                sh[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME] = core.u8(c)
        Image.fromarray(sh).save(path)
    if not heads:  # arms raised across the head go into the hand sheet (above the head layer); the original sheet is kept as <anim>.pre-head.png
        for an in sorted({a for a, _ in out}):
            path = f"{RES}/Sprites/classes/{cls}/body/hand/{an}.png"
            orig = path.replace(".png", ".pre-head.png").replace("/Resources/", "/ArtLibrary/").replace("/Sprites/", "/")
            os.makedirs(os.path.dirname(orig), exist_ok=True)
            if not os.path.exists(orig) and os.path.exists(path):
                Image.open(path).save(orig)
            base = np.array(Image.open(orig).convert("RGBA")).astype(np.float64) if os.path.exists(orig) else None
            if base is None:
                continue
            for (a2, d), v in out.items():
                if a2 != an:
                    continue
                r = DIR_ROW[d]
                for f, o in enumerate(v.get("over", [])):
                    if o[..., 3].max() == 0:
                        continue
                    c = base[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME]
                    base[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME] = core.over(c, o)
            Image.fromarray(core.u8(base)).save(path)
    for d in ("up", "right") if heads else ():
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
    for an in sorted({x for x, _ in anims}):
        if not os.path.exists(f"{RES}/Sprites/classes/{cls}/body/headless/{an}.png"):
            continue
        sh = sheet(cls, an, "headless")
        for d in ("down", "up", "right", "left"):
            if (an, d) in anims:
                out[(an, d)] = {"anchors": anims[(an, d)], "headless": [cell(sh, DIR_ROW[d], f) for f in range(len(anims[(an, d)]))]}
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


def normalize_scale(cls, anims, target=1.0):
    """Set the head scale s of every frame of these anims to `target` (the idle size), keeping the chin where the anchor put it, so the
    head stays on the neck. Warrior 2026-11-07: the painted attack / walk-down heads are 7-15% bigger than idle, the registration
    followed them and drew the hairstyle head that much bigger (the designer saw big heads when attacking)."""
    p = core.paths(cls)
    with open(p["anchors"], encoding="utf-8") as fh:
        data = json.load(fh)
    chins = {}
    for d in ("down", "up", "right", "left"):
        a = ref_head(cls, d)[..., 3] > 128
        ys, xs = np.where(a)
        chins[d] = (xs[ys >= ys.max() - 6].mean(), float(ys.max()))
    for e in data["anims"]:
        if e["anim"] not in anims:
            continue
        cx, cy = chins[e["dir"]]
        for f in e["frames"]:
            tx, ty = fwd(f, cx, cy)
            g = dict(f, s=target)
            nx, ny = fwd(g, cx, cy)
            f["s"] = target
            f["x"] = round(f["x"] + tx - nx, 3)
            f["y"] = round(f["y"] + ty - ny, 3)
    with open(p["anchors"], "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)
        fh.write("\n")
    print("head scale ->", target, "for", anims)


def above_neck(cls, anim, shape):
    """Sheet-sized mask: rows above each frame's neck cut (the cut of that facing moved by the frame's head anchor; left = right mirrored)."""
    with open(core.paths(cls)["anchors"], encoding="utf-8") as fh:
        anc = {(x["anim"], x["dir"]): x["frames"] for x in json.load(fh)["anims"]}
    m = np.zeros(shape[:2], bool)
    rows = np.arange(FRAME)[:, None]
    for d, r in DIR_ROW.items():
        if (anim, d) not in anc:
            continue
        cfg = cut_cfg(cls, "right" if d == "left" else d)
        pts0 = [(FRAME - 1 - x, y) for x, y in cfg["cut"]] if d == "left" else cfg["cut"]
        for f, a in enumerate(anc[(anim, d)]):
            if (f + 1) * FRAME > shape[1]:
                break
            cut = core.polyline(sorted(fwd(a, x, y) for x, y in pts0))[None, :]
            m[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME] = rows < cut
    return m


def outfit_headless(cls, outfit, grow=2):
    """Headless sheets of an outfit (body/<outfit>/bare repainted from the Lv1 rows with the same poses, Tools/build-warrior-outfit-sheets.py):
    erase what the Lv1 head layer erased in the same cell, plus the outfit's own hair-coloured pixels within `grow` px of it (the repaint's
    head is not pixel-identical). The Lv1 head anchors are reused as they are. Writes body/<outfit>/headless/<anim>.png."""
    hair = hair_test(cls)
    src = f"{RES}/Sprites/classes/{cls}/body"
    os.makedirs(f"{src}/{outfit}/headless", exist_ok=True)
    for name in sorted(os.listdir(f"{src}/headless")):
        if not name.endswith(".png") or not os.path.exists(f"{src}/{outfit}/bare/{name}"):
            continue
        lv1 = np.array(Image.open(f"{src}/bare/{name}").convert("RGBA")).astype(np.float64)
        hl = np.array(Image.open(f"{src}/headless/{name}").convert("RGBA")).astype(np.float64)
        ob = np.array(Image.open(f"{src}/{outfit}/bare/{name}").convert("RGBA")).astype(np.float64)
        if lv1.shape != ob.shape or hl.shape != ob.shape:
            print("skip (size)", name)
            continue
        erased = (lv1[..., 3] > 8) & (hl[..., 3] <= 8)
        near = ndimage.binary_dilation(erased, iterations=grow)
        oh = hair(ob) & (ob[..., 3] > 8)
        dark = (ob[..., :3] @ [0.3, 0.59, 0.11] < 60) & ndimage.binary_dilation(oh, iterations=1)
        # only above the neck cut of that frame: below it the body must stay solid or a 1-2 px gap opens under the head (T2 back view)
        above = above_neck(cls, name[:-4], ob.shape)
        cut = erased | (near & (oh | dark) & above)
        far = ndimage.binary_dilation(erased, iterations=10) & above
        cut |= far & (oh | dark)
        # keep the 2 rows just above the neck cut: the head rises up to 1 px while breathing and would open a gap there (T2 back view)
        lifted = np.zeros_like(above)
        lifted[:-2] = above[2:]
        cut &= ~(above & ~lifted)
        out = ob.copy()
        out[cut] = 0
        # leftover specks of the repaint's hair outline (faint, near-black) near the head: small blobs above the neck not joined to the body
        lab, n = ndimage.label(out[..., 3] > 8, np.ones((3, 3)))
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        hit = np.unique(lab[far])
        small = np.isin(lab, [i for i in hit if i > 0 and sizes[i - 1] <= 40])
        out[small & ndimage.binary_dilation(far, iterations=2)] = 0
        # hand-painted fill behind the head (e.g. T9 wing membrane hidden by the painted hair): ArtLibrary/.../outfits/<outfit>/headless-patch/<anim>.png,
        # drawn under the cut sheet so a rerun keeps it
        patch = f"{RES.replace('Resources', 'ArtLibrary')}/classes/{cls}/outfits/{outfit}/headless-patch/{name}"
        if os.path.exists(patch):
            out = core.over(np.array(Image.open(patch).convert("RGBA")).astype(np.float64), out)
        Image.fromarray(core.u8(out)).save(f"{src}/{outfit}/headless/{name}")
        print("wrote", f"{outfit}/headless/{name}", int(cut.sum()), "px erased")

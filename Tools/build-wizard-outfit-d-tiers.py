"""Wizard outfit D armour tiers T1-T9, idle facing DOWN only (14/11/2026, plans/261114-0821-wizard-outfit-d-redesign step 6).

Same body path as T0 (Lv1): the BALD body with its painted head + the hair layer on top (HeroHeadLayer.BaldBody.cs, outfit branch), so
hairstyles and dyes keep working on every tier. Other facings / anims of the tiers keep the old path (body/set-tN/headless + heads/face).

Sources: Assets/ArtLibrary/classes/wizard/outfits/outfit-d-tiers/src/wizard-outfit-d-tN-idle-down-bald-chatgpt-K.png (ChatGPT, one bald
figure in the T0 palm-up pose, outfit copied from the board -4 piece; prompts prompt-*.txt there). T1 on #FF00FF (green outfit).
Fit: scale so chin -> sole equals T0's fitted down cell (Tools/build-wizard-outfit-d-idle-dirs.py), skull centred on T0's skull; then the
head alone is warped to T0's head height when it differs by more than HEAD_TOL (sprite-rules art rule 8). Breathing = T0's.

Writes per tier N (Resources/Sprites/classes/wizard/body/set-tN/):
  bald/idle.png            row 0 = the 4 breathing frames (other rows empty: no hair anchor there, old path)
  hand/idle.png            row 0 = staff fist + open palm (drawn above staff / book)
  bare/idle.png            row 0 = bald + default hair style 0 (portrait / fallback), other rows untouched
  held-anchors.json        staff (art idle-stance, artY as T0) + book (idle-stance centred on its bottom centre, on the palm), idle down
  hair-anchors.json        idle down hair anchors (hair scale HAIR_SCALE like T0)
Backup once: outfit-d-tiers/old/set-tN/.

  py Tools/build-wizard-outfit-d-tiers.py fit [N,..]     -> staging/set-tN/ + staging/tiers-measure.json
  py Tools/build-wizard-outfit-d-tiers.py install [N,..] -> Resources
  py Tools/build-wizard-outfit-d-tiers.py ground [N,..]  -> idle-down staff butt on the sole (artY), T0 too (after the T0 idle tool's install)
  py Tools/build-wizard-outfit-d-tiers.py check OUT.png [styles]   all tiers (T0 from Resources) x 4 frames, composed like the game
"""
import glob, importlib.util, json, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
WORK = f"{ROOT}/Assets/ArtLibrary/classes/wizard/outfits/outfit-d-tiers"
STAGE, OLD = f"{WORK}/staging", f"{WORK}/old"
RES = f"{ROOT}/Assets/Resources"
BODY, WEAP = f"{RES}/Sprites/classes/wizard/body", f"{RES}/Sprites/classes/wizard/weapons"
DATA = f"{RES}/Data/classes/wizard"
F = 192
HEAD_TOL = 0.03
# Tiers redrawn 15/11 with the arm pose of board -4 (user: book and staff covered the face): rough fist / palm spots to search around
# (fitted cell px; the board's T9 fist is at 0.47 H, 0.29 H left of centre; the palm at 0.62 H, 0.27 H right)
POSE_HANDS = {n: ((52.0, 124.0), (134.0, 131.0)) for n in range(1, 10)}   # T9 v4 pose; T1-T8 redrawn in the same pose 15/11
POSE_BOOK_DX = {n: 4.0 for n in range(1, 10)}   # the open book sits a little outward on the palm (its inner edge clears the jaw)
TIER_NAMES = ["apprentice", "wood", "crystal", "moon", "arcane", "flame", "ice", "nature", "holy", "void"]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


nwd = load("nwd", f"{ROOT}/Tools/build-wizard-outfit-d-idle-dirs.py")
_hob = [None]


def hob():
    """Tools/build-wizard-hair-on-bald-body.py (slow import: loads every outfit D tool), only for hair anchors / hair cells."""
    if _hob[0] is None:
        _hob[0] = load("hob", f"{ROOT}/Tools/build-wizard-hair-on-bald-body.py")
    return _hob[0]


def key_magenta(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum.reduce([g, 255 - r, 255 - b]) - 40) / 80.0, 0, 1)
    r = np.minimum(r, np.maximum(g, b) + 90); b = np.minimum(b, np.maximum(g, r) + 90)
    return np.dstack([r, g, b, alpha * 255]).astype(np.float32)


def source(n):
    files = sorted(glob.glob(f"{WORK}/src/wizard-outfit-d-t{n}-idle-down-bald-chatgpt-*.png"))
    assert files, f"no source for T{n}"
    p = files[-1]
    rgb = np.asarray(Image.open(p).convert("RGB"))
    magenta = rgb[:8, :8, 1].mean() < 100 and rgb[:8, :8, 0].mean() > 180
    return p, nwd.single_figure(p, key_magenta if magenta else None)


def skin(a):
    r, g, b, al = (a[..., i] for i in range(4))
    # g >= b - 4: skin is warm (g above b), pink crystals (T7) have b > g; g - b < 45: light gold armour (T4, T8) has g far above b
    return (al > 128) & (r > 200) & (g > 140) & (b > 100) & (r - b > 30) & (g < 240) & (g >= b - 4) & (g - b < 45)


def skull(a):
    """(top row, chin row, centre x) of the biggest skin blob (the bald head; fists and palm are smaller)."""
    sk = skin(a); lab, n = ndimage.label(sk)
    i = 1 + int(np.argmax(ndimage.sum(sk, lab, range(1, n + 1))))
    ys, xs = np.nonzero(lab == i)
    return int(ys.min()), int(ys.max()), float((xs.min() + xs.max()) / 2), int(xs.max() - xs.min() + 1)


def sole_row(a, cx, half):
    band = a[:, max(0, int(cx - half)):int(cx + half), 3] > 128
    return int(np.nonzero(band.any(1))[0].max())


_T0 = [None]


def t0_cell():
    if _T0[0] is None:
        figs = nwd.figures()
        _T0[0] = nwd.fit("down", figs["down"])
    return _T0[0]


def head_warp(abr, cell, q, neck, cx):
    """Head grown / shrunk by q above the neck row. Growing: only the HEAD is taken from the warped cell (the bigger warped skull covers
    the old one); wings, pauldrons and the staff side keep their drawn shape -- warping the whole band above the neck bent the T9 dragon
    wings (15/11, user: "cong queo"). Shrinking keeps the whole warp (the old, bigger head would stick out)."""
    w = np.asarray(abr.warp(Image.fromarray(cell.astype(np.uint8)), {"q": q, "neck": neck, "cx": cx})).astype(np.float32)
    if q <= 1.0:
        return w
    sk = skin(w); sk[neck + 2:] = False
    lab, n = ndimage.label(sk)
    if not n:
        return w
    head = lab == 1 + int(np.argmax(ndimage.sum(sk, lab, range(1, n + 1))))
    ys, xs = np.nonzero(head)                                    # the skull + ears (darker pink, not "skin") + outline: an ellipse
    cy, cx2 = (ys.min() + neck) / 2, (xs.min() + xs.max()) / 2   # around the skull, 9 px wider at the sides for the ears
    ry, rx = (neck - ys.min()) / 2 + 3, (xs.max() - xs.min()) / 2 + 9
    yy, xx = np.mgrid[:F, :F]
    head = ((yy - cy) / ry) ** 2 + ((xx - cx2) / rx) ** 2 <= 1.0
    head &= w[..., 3] > 0
    head[neck + 2:] = False
    out = cell.copy(); out[head] = w[head]
    return out


def fit_tier(fig):
    c0 = t0_cell(); top0, chin0, cx0, w0 = skull(c0)
    top, chin, cx, w = skull(fig)
    sole = sole_row(fig, cx, w * 0.45)
    k = (nwd.SOLE - chin0) / (sole - chin)
    im = nwd.resize(fig, round(fig.shape[1] * k), round(fig.shape[0] * k))
    y0 = nwd.SOLE - round(sole * k); x0 = int(round(cx0 - cx * k))
    cell = np.zeros((F, F, 4), np.float32)
    ys, xs = max(0, y0), max(0, x0)
    ye, xe = min(F, y0 + im.shape[0]), min(F, x0 + im.shape[1])
    cell[ys:ye, xs:xe] = im[ys - y0:ye - y0, xs - x0:xe - x0]
    lost = float(im[..., 3].sum() - cell[..., 3].sum()) / 255
    # head = the bald skull skin blob (top -> chin); nwd.head_rows also catches the raised fists below the chin
    t1, c1, hx, _ = skull(cell); head_before = c1 - t1 + 1
    target = chin0 - top0 + 1
    q = target / head_before
    abr = load("abr", f"{ROOT}/Tools/build-wizard-attack-board-repaint.py")
    if abs(q - 1) > HEAD_TOL:
        cell = head_warp(abr, cell, q, c1 + 1, hx)
    t2, c2, hx2, _ = skull(cell)
    for _ in range(3):                                           # the smooth warp undershoots big factors (T9 v3: 38 -> 49): repeat
        if abs(target / (c2 - t2 + 1) - 1) <= HEAD_TOL:
            break
        q2 = target / (c2 - t2 + 1); q *= q2
        cell = head_warp(abr, cell, q2, c2 + 1, hx2)
        t2, c2, hx2, _ = skull(cell)
    meas = {"scale": round(k, 4), "headT0": target, "headBefore": head_before, "headAfter": c2 - t2 + 1, "warp": round(q, 3) if abs(q - 1) > HEAD_TOL else 1.0,
            "clippedAlphaPx": round(lost, 1), "figureH": int(nwd.SOLE - np.nonzero(cell[..., 3].max(1) > 128)[0][0] + 1),
            "skullT0": [top0, chin0, w0], "skull": list(skull(cell))[:2] + [skull(cell)[3]]}
    return cell, meas


def hand_blobs(cell, from_row=96):
    """Skin blobs below the head, the skin mask closed by 1 px first: the open palm is cut into finger pieces by its dark lines (T3)."""
    sk = skin(cell); sk[:from_row] = False
    lab, n = ndimage.label(ndimage.binary_dilation(sk, iterations=1))
    out = []
    for i in range(1, n + 1):
        m = (lab == i) & sk
        if m.sum() >= 15:
            ys, xs = np.nonzero(m)
            out.append({"c": (round(float(xs.mean()), 1), round(float(ys.mean()), 1)), "mask": m})
    return out


def seat_of(blob):
    """Book bottom centre on the open palm: top of the hand blob within its middle third, x at its centre, 2 px into the palm."""
    ys, xs = np.nonzero(blob["mask"])
    x0, x1 = xs.min(), xs.max(); mid = (xs >= x0 + (x1 - x0) / 3) & (xs <= x1 - (x1 - x0) / 3)
    return round(float((x0 + x1) / 2), 1), round(float(ys[mid].min()) + 2, 1)


def frames_of(cell):
    return [nwd.body_frame(cell, "down", f) for f in range(4)]


def tier_anchors(cell, n=0):
    # the hands sit where T0's do (same pose): nearest hand blob to T0's staff fist / palm (the generic picker grabbed a neck or a skin-coloured
    # gem on T3 / T4); a blob more than 14 px away means the redraw moved the hand -- stop
    b0 = hand_blobs(t0_cell()); s0 = min(b0, key=lambda q: q["c"][0]); p0 = max(b0, key=lambda q: q["c"][0])
    if n in POSE_HANDS:                                          # redrawn in the board -4 arm pose: its own hand spots (15/11, T9 first)
        s0, p0 = ({"c": POSE_HANDS[n][0]}, {"c": POSE_HANDS[n][1]})
    sk = skin(cell); yy, xx = np.mgrid[:F, :F]

    def local(ref, r=12):
        """Skin pixels within r px of T0's hand, the blob kept connected to the pixel nearest that centre (white / gold armour next to
        the hand passes the skin test too: T7 merged the staff fist with the sleeve)."""
        m = sk & ((xx - ref["c"][0]) ** 2 + (yy - ref["c"][1]) ** 2 <= r * r)
        assert m.sum() >= 15, f"no hand near T0's {ref['c']}"
        ys, xs = np.nonzero(m)
        return {"c": (round(float(xs.mean()), 1), round(float(ys.mean()), 1)), "mask": m}
    sb, pb = (local(s0, 16), local(p0, 18)) if n in POSE_HANDS else (local(s0), local(p0, 14))
    sf, bf, seat = sb["c"], pb["c"], seat_of(pb)
    seat = (seat[0] + POSE_BOOK_DX.get(n, 0.0), seat[1])
    staff, book = [], []
    for f in range(4):
        staff.append({"x": float(sf[0]), "y": round(nwd.map_y("down", f, sf[1]), 3), "s": 0.447, "angle": 0, "sx": 1, "behind": False,
                      "hidden": False, "fallback": False, "art": "idle-stance", "artY": nwd.STAFF_ART_Y})
        book.append({"x": float(seat[0]), "y": round(nwd.map_y("down", f, seat[1]), 3), "s": 0.25, "angle": nwd.BOOK_ANGLE_DOWN, "sx": 1,
                     "behind": False, "hidden": False, "fallback": False, "art": "idle-stance"})
    return sf, bf, seat, {"anchors": [{"part": "staff", "anim": "idle", "dir": "down", "frames": staff},
                                      {"part": "book", "anim": "idle", "dir": "down", "frames": book}]}


def hands_of(cell, sf, bf):
    out = []
    for f in range(4):
        fr = nwd.body_frame(cell, "down", f); h = np.zeros_like(cell)
        for fist in (sf, bf):
            h = np.maximum(h, nwd.hand_layer(fr, (fist[0], nwd.map_y("down", f, fist[1])), radius=11 if fist is bf else 9))
        out.append(h)
    return out


def hair_doc(frames):
    H = hob()
    fr = H.hair_anchors([Image.fromarray(c.astype(np.uint8)) for c in frames], "down")
    iou = [x.pop("iou") for x in fr]
    doc = {"anims": [{"anim": "idle", "dir": "down", "frames": fr}]}
    H.rescale(doc, H.HAIR_SCALE)
    return doc, iou


def tiers_arg(i):
    return [int(x) for x in sys.argv[i].split(",")] if len(sys.argv) > i else list(range(1, 10))


def cmd_fit():
    measure = json.load(open(f"{STAGE}/tiers-measure.json")) if os.path.exists(f"{STAGE}/tiers-measure.json") else {}
    for n in tiers_arg(2):
        path, fig = source(n)
        cell, meas = fit_tier(fig)
        frames = frames_of(cell)
        sf, bf, seat, held = tier_anchors(cell, n)
        hands = hands_of(cell, sf, bf)
        doc, iou = hair_doc(frames)
        d = f"{STAGE}/set-t{n}"; os.makedirs(d, exist_ok=True)
        for name, cells in (("bald", frames), ("hand", hands)):
            sheet = Image.new("RGBA", (4 * F, F))
            for f, c in enumerate(cells):
                sheet.paste(Image.fromarray(c.astype(np.uint8)), (f * F, 0))
            sheet.save(f"{d}/{name}-row0.png")
        json.dump(held, open(f"{d}/held-anchors.json", "w"), indent=2)
        json.dump(doc, open(f"{d}/hair-anchors.json", "w"), indent=1)
        meas.update(source=os.path.basename(path), staffFist=sf, bookFist=bf, bookSeat=seat, hairIoU=iou)
        measure[f"t{n}"] = meas
        print(f"T{n}", json.dumps({k: meas[k] for k in ("scale", "headT0", "headBefore", "headAfter", "warp", "clippedAlphaPx", "figureH")}),
              "fists", sf, bf, "seat", seat, "hair iou", iou)
    c0 = t0_cell(); h0 = skull(c0)
    measure["t0"] = {"head": h0[1] - h0[0] + 1, "headW": h0[3], "figureH": int(nwd.SOLE - np.nonzero(c0[..., 3].max(1) > 128)[0][0] + 1)}
    json.dump(measure, open(f"{STAGE}/tiers-measure.json", "w"), indent=1)


def write_meta(path, like):
    if os.path.exists(path + ".meta") or not os.path.exists(like + ".meta"):
        return
    txt = open(like + ".meta", encoding="utf-8").read()
    open(path + ".meta", "w", encoding="utf-8").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, txt, count=1))


def backup(path, n):
    dst = f"{OLD}/set-t{n}/{os.path.relpath(path, f'{BODY}/set-t{n}').replace(os.sep, '__').replace('/', '__')}"
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if os.path.exists(path) and not os.path.exists(dst):
        shutil.copy(path, dst)


def paste_row0(sheet_path, row_img, like):
    if os.path.exists(sheet_path):
        s = Image.open(sheet_path).convert("RGBA")
    else:
        lk = Image.open(like); s = Image.new("RGBA", lk.size)
    s.paste(Image.new("RGBA", (s.width, F)), (0, 0)); s.paste(row_img, (0, 0)); s.save(sheet_path)


def cmd_install():
    H = hob()
    for n in tiers_arg(2):
        d, out = f"{STAGE}/set-t{n}", f"{BODY}/set-t{n}"
        for p in (f"{out}/bald/idle.png", f"{out}/hand/idle.png", f"{out}/bare/idle.png", f"{out}/held-anchors.json", f"{out}/hair-anchors.json"):
            backup(p, n)
        os.makedirs(f"{out}/bald", exist_ok=True)
        bald = Image.open(f"{d}/bald-row0.png").convert("RGBA")
        paste_row0(f"{out}/bald/idle.png", bald, f"{out}/bare/idle.png")
        write_meta(f"{out}/bald/idle.png", f"{BODY}/bald/idle.png")
        if not os.path.exists(f"{out}/bald.meta") and os.path.exists(f"{out}/hand.meta"):
            open(f"{out}/bald.meta", "w").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, open(f"{out}/hand.meta").read(), count=1))
        paste_row0(f"{out}/hand/idle.png", Image.open(f"{d}/hand-row0.png").convert("RGBA"), f"{out}/bare/idle.png")
        doc = json.load(open(f"{d}/hair-anchors.json"))
        bare = Image.new("RGBA", (4 * F, F))
        for f in range(4):
            c = bald.crop((f * F, 0, (f + 1) * F, F))
            c.alpha_composite(H.hair_cell("down", doc["anims"][0]["frames"][f], 0)); bare.paste(c, (f * F, 0))
        paste_row0(f"{out}/bare/idle.png", bare, f"{out}/bare/idle.png")
        for name in ("held-anchors.json", "hair-anchors.json"):
            shutil.copy(f"{d}/{name}", f"{out}/{name}")
        write_meta(f"{out}/hair-anchors.json", f"{out}/head-anchors.json")
        print(f"installed set-t{n}, staff butt on soles", ground_staff(n))


def ground_staff(n):
    """Idle down (user 15/11): the staff BUTT stands on the sole line in every breathing frame, like a staff planted on the ground. Only the
    idle-down staff anchors' artY move (the art + grip stay, so walk / attack keep their hold): butt row of the tier's idle-stance art, centred
    at (x, y + artY), lands on that frame's lowest body row (the sole, measured on the tier's bald sheet)."""
    folder = BODY if n == 0 else f"{BODY}/set-t{n}"
    art = np.asarray(Image.open(f"{WEAP}/staff/{TIER_NAMES[n]}/idle-stance.png"))[..., 3] > 80
    butt = np.nonzero(art.any(1))[0].max() + 1                    # one past the last opaque row: the butt's bottom edge
    path = f"{folder}/held-anchors.json"
    doc = json.load(open(path, encoding="utf-8"))
    bald = np.asarray(Image.open(f"{folder}/bald/idle.png").convert("RGBA"))[..., 3]
    soles = []
    for e in doc["anchors"]:
        if (e["part"], e["anim"], e["dir"]) != ("staff", "idle", "down"):
            continue
        for f, fr in enumerate(e["frames"]):
            cell = bald[:F, f * F:(f + 1) * F]
            sole = int(np.nonzero(cell[:, 60:132].max(1) > 128)[0].max()) + 1   # bottom edge of the feet (centre band)
            sole = min(sole, nwd.SOLE + 1)                        # a cape hem hanging past the boots (T9 v3) is not the ground
            d = (butt - art.shape[0] / 2) * fr["s"]
            fr["artY"] = round(sole - d - fr["y"], 2)
            soles.append(sole)
    json.dump(doc, open(path, "w", encoding="utf-8"), indent=2)
    return soles


def compose(n, f, style, pad=40):
    """Idle down frame of tier n as HeroRenderer draws it on the bald path: bald body, hair, staff, book (idle-stance centred at x, y+artY),
    hand layer. n = 0: the Lv1 sheets + body/held-anchors.json + Data hair-anchors.json."""
    H = hob()
    folder = BODY if n == 0 else f"{BODY}/set-t{n}"
    held = json.load(open(f"{folder}/held-anchors.json", encoding="utf-8"))["anchors"]
    hdoc = json.load(open(f"{DATA}/hair-anchors.json" if n == 0 else f"{folder}/hair-anchors.json", encoding="utf-8"))
    an = next(e for e in hdoc["anims"] if e["anim"] == "idle" and e["dir"] == "down")["frames"][f]
    cv = Image.new("RGBA", (F + 2 * pad, F + 2 * pad))
    box = (f * F, 0, (f + 1) * F, F)
    cv.alpha_composite(Image.open(f"{folder}/bald/idle.png").convert("RGBA").crop(box), (pad, pad))
    cv.alpha_composite(H.hair_cell("down", an, style), (pad, pad))
    for part in ("staff", "book"):
        a = next(e for e in held if e["part"] == part and e["anim"] == "idle" and e["dir"] == "down")["frames"][f]
        art = Image.open(f"{WEAP}/{part}/{TIER_NAMES[n]}/{a['art']}.png").convert("RGBA")
        art = art.resize((max(1, round(art.width * a["s"])), max(1, round(art.height * a["s"]))), Image.LANCZOS)
        if a.get("angle"):
            art = art.rotate(-a["angle"], resample=Image.BICUBIC, expand=True)
        cx, cy = a["x"] + pad, a["y"] + a.get("artY", 0) + pad
        cv.alpha_composite(art, (int(round(cx - art.width / 2)), int(round(cy - art.height / 2))))
    cv.alpha_composite(Image.open(f"{folder}/hand/idle.png").convert("RGBA").crop(box), (pad, pad))
    return cv


def cmd_check():
    out = sys.argv[2]; styles = [int(x) for x in sys.argv[3].split(",")] if len(sys.argv) > 3 else [0, 2]
    W, top = F + 80, 90
    sheet = Image.new("RGBA", (W * 4 * len(styles), (F + 80 + top) * 10), (58, 62, 70, 255))
    for n in range(10):
        if n and not os.path.exists(f"{BODY}/set-t{n}/hair-anchors.json"):
            continue
        for j, st in enumerate(styles):
            for f in range(4):
                sheet.alpha_composite(compose(n, f, st, pad=40), ((j * 4 + f) * W, n * (F + 80 + top) + top))
    sheet.convert("RGB").save(out); print(out, sheet.size)


if __name__ == "__main__":
    if sys.argv[1] == "ground":       # every tier incl. T0 (run after build-wizard-outfit-d-idle-dirs.py install, which rewrites T0's artY)
        for n in ([int(x) for x in sys.argv[2].split(",")] if len(sys.argv) > 2 else range(10)):
            print(f"T{n} staff butt on soles", ground_staff(n))
    else:
        {"fit": cmd_fit, "install": cmd_install, "check": cmd_check}[sys.argv[1]]()

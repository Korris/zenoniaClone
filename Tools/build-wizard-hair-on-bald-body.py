"""Wizard "hair on the painted bald head": the body keeps the bald head ChatGPT painted in every frame (its face, mouth, tilt) and only the
hair is a swappable layer. Replaces the cut-the-head pipeline (body/headless + shared heads/face) for the Lv1 / T0 Northwind body.

Reads the fitted bald frames straight from the tools that installed each animation (nothing of theirs is rewritten):
  idle            Tools/build-wizard-northwind-idle-dirs.py
  walk            Tools/build-wizard-northwind-walk.py
  attack1-4 down  Tools/build-wizard-combo-v2-attacks.py
  attack1-4 other Tools/build-wizard-northwind-attacks.py (rows widened to the v2 frame count, last frame repeated)

Writes (Resources, wizard):
  Sprites/.../body/bald/<anim>.png       the body with its painted bald head, same grid as body/bare
  Sprites/.../body/over-hair/<anim>.png  arms / fists raised onto the head, drawn again above the hair (facing down / left / right)
  Data/.../hair-anchors.json             per frame where the hair cells (heads/hair; cell space of heads/face) sit on the painted skull:
                                         x, y, s as head-anchors.json. One scale per row (the painted skull width), position by skull overlap.

  py Tools/build-wizard-hair-on-bald-body.py verify            painted frames against the installed headless sheets (frame order check)
  py Tools/build-wizard-hair-on-bald-body.py write             the sheets + anchors
  py Tools/build-wizard-hair-on-bald-body.py check OUT_DIR [styles]   composed like the game, one PNG per animation (styles default 0,2,4)
"""
import importlib.util, json, os, sys, tempfile
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
F = 192
DIRS = ["down", "up", "left", "right"]
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
ANIMS = ["idle", "walk", "attack1", "attack2", "attack3", "attack4"]
BODY = f"{ROOT}/Assets/Resources/Sprites/classes/wizard/body"
HEADS = f"{ROOT}/Assets/Resources/Sprites/classes/wizard/heads"
DATA = f"{ROOT}/Assets/Resources/Data/classes/wizard"
OVER_DIRS = ("down", "left", "right")   # seen from behind a raised arm is beyond the head: the hair stays on top


def _import(name, file):
    spec = importlib.util.spec_from_file_location(name, f"{ROOT}/Tools/{file}")
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


cv = _import("cv2tool", "build-wizard-combo-v2-attacks.py")           # attacks facing down; also the root-patching loader + idle tool
wk = _import("walktool", "build-wizard-northwind-walk.py")
nwa0 = cv._load("nwa0", f"{ROOT}/Tools/build-wizard-northwind-attacks.py")  # a fresh copy: the old strips of the other facings
nw = cv.nw
skin = cv.nwa.skin
placed = cv.nwa.placed


def painted(anim):
    """{dir: [192 px RGBA cells]} of an animation: the fitted painted frames in game frame order, bald head on."""
    if anim == "idle":
        figs = nw.figures()
        return {d: [Image.fromarray(np.asarray(nw.body_frame(c, d, f)).astype(np.uint8)) for f in range(4)]
                for d in DIRS for c in [nw.fit(d, figs[d])]}
    if anim == "walk":
        return {d: wk.wb.ordered("walk", wk.wb.fitted("walk", d)) for d in DIRS}
    n = cv.cv.COUNT[anim]
    out = {"down": cv.wb.ordered(anim, cv.wb.fitted(anim, "down"))}
    for d in ("up", "left", "right"):
        cells = nwa0.wb.ordered(anim, nwa0.wb.fitted(anim, d))
        out[d] = [cells[min(f, len(cells) - 1)] for f in range(n)]
    return out


def head_blob(cell):
    """Skin of the painted bald head: the biggest skin blob (fists are smaller, also when raised above it)."""
    sk = skin(np.asarray(cell)); lab, n = ndimage.label(sk)
    if n == 0:
        return np.zeros(sk.shape, bool)
    return lab == 1 + int(np.argmax(ndimage.sum(sk, lab, range(1, n + 1))))


def face_template(d):
    """heads/face of a facing (left = right mirrored): the bald head the hair cells were painted on."""
    f = Image.open(f"{HEADS}/face/{'right' if d == 'left' else d}.png").convert("RGBA")
    return f.transpose(Image.FLIP_LEFT_RIGHT) if d == "left" else f


def width(mask):
    xs = np.nonzero(mask.any(0))[0]
    return float(xs[-1] - xs[0] + 1) if len(xs) else 0.0


def shifted(mask, dx, dy):
    out = np.zeros_like(mask)
    ys, xs = np.nonzero(mask); ys, xs = ys + dy, xs + dx
    ok = (ys >= 0) & (ys < F) & (xs >= 0) & (xs < F)
    out[ys[ok], xs[ok]] = True
    return out


def hair_anchors(cells, d):
    """Anchor of the hair cells per frame. Scale: painted skull width / template skull width, the row's median (hair does not pump with a
    hand over the scalp). Position: the best skull overlap within 6 px of the matched skull tops or the matched chins."""
    tpl = face_template(d)
    tw = width(head_blob(tpl))
    blobs = [head_blob(c) for c in cells]
    s = float(np.median([width(b) / tw for b in blobs]))
    base = head_blob(placed(tpl, {"x": 0.0, "y": 0.0, "s": s}))
    bys, bxs = np.nonzero(base)
    out = []
    for b in blobs:
        ys, xs = np.nonzero(b)
        low = b[ys.max() - 12:ys.max() + 1].any(0).nonzero()[0]         # the jaw: fists raised onto the scalp join the blob above it
        guesses = [(int(round((xs.min() + xs.max()) / 2 - (bxs.min() + bxs.max()) / 2)), int(ys.min() - bys.min())),
                   (int(round((low.min() + low.max()) / 2 - (bxs.min() + bxs.max()) / 2)), int(ys.max() - bys.max()))]
        best = (-1.0, 0, 0)
        for gx, gy in guesses:                                          # skull tops matched, or chins (a covered / joined scalp)
            for dy in range(-6, 7):
                for dx in range(-6, 7):
                    m = shifted(base, gx + dx, gy + dy)
                    iou = (m & b).sum() / max(1, (m | b).sum()) - 0.002 * (abs(dx) + abs(dy))   # ties: nearest to the guess
                    if iou > best[0]:
                        best = (iou, gx + dx, gy + dy)
        out.append({"x": float(best[1]), "y": float(best[2]), "s": round(s, 4), "sy": 1.0, "angle": 0.0, "iou": round(best[0], 3)})
    return out


def over_hair(cell, d):
    """Body pixels to draw again above the hair: parts beside the head that reach above its middle (raised sleeves, fists).
    Nothing else: the hair lies on the collar and shoulders. Never redraw the body below the chin row above the hair: that showed as a
    straight line across long hair wherever the collar rises above the chin (every sideways frame)."""
    a = np.asarray(cell)
    out = a.copy(); out[..., 3] = 0                                     # clear pixels keep the body colours: no dark fringe at the layer's edge
    if d not in OVER_DIRS:
        return Image.fromarray(out)
    b = head_blob(cell)
    ys = np.nonzero(b.any(1))[0]
    if not len(ys):
        return Image.fromarray(out)
    top, chin = ys[0], ys[-1]; mid = (top + chin) // 2
    head = ndimage.binary_fill_holes(ndimage.binary_dilation(b, iterations=3))
    cand = (a[..., 3] > 0) & ~head; cand[chin:] = False
    lab, n = ndimage.label(cand)
    for i in range(1, n + 1):
        m = lab == i
        if m.sum() >= 40 and np.nonzero(m.any(1))[0][0] < mid:
            out[m, 3] = a[m, 3]
    return Image.fromarray(out)


def verify():
    """Painted frames must be the installed frames: every opaque headless pixel equals the painted one (the head is all that was removed)."""
    for anim in ANIMS:
        hl = np.asarray(Image.open(f"{BODY}/headless/{anim}.png").convert("RGBA")).astype(int)
        for d, cells in painted(anim).items():
            fr = []
            for f, c in enumerate(cells):
                h = hl[ROWS[d] * F:(ROWS[d] + 1) * F, f * F:(f + 1) * F]; p = np.asarray(c).astype(int)
                m = h[..., 3] > 200
                fr.append(round(float((np.abs(h[m] - p[m]).max(1) <= 2).mean()) if m.any() else 0.0, 2))
            print(anim, d, "same pixels", fr)


def write():
    like = f"{BODY}/headless/idle.png"
    anchors = []
    for anim in ANIMS:
        rows = painted(anim)
        n = len(rows["down"])
        bald, over = Image.new("RGBA", (n * F, 4 * F)), Image.new("RGBA", (n * F, 4 * F))
        for d in DIRS:
            frames = hair_anchors(rows[d], d)
            print(anim, d, "s", frames[0]["s"], "iou", [fr.pop("iou") for fr in frames])
            anchors.append({"anim": anim, "dir": d, "frames": frames})
            for f, c in enumerate(rows[d]):
                bald.paste(c, (f * F, ROWS[d] * F)); over.paste(over_hair(c, d), (f * F, ROWS[d] * F))
        for layer, im in (("bald", bald), ("over-hair", over)):
            os.makedirs(f"{BODY}/{layer}", exist_ok=True)
            p = f"{BODY}/{layer}/{anim}.png"; im.save(p); cv.wb.write_meta(p, like)
    with open(f"{DATA}/hair-anchors.json", "w", encoding="utf-8") as fh:
        json.dump({"anims": anchors}, fh, indent=1); fh.write("\n")
    print("wrote body/bald, body/over-hair,", f"{DATA}/hair-anchors.json")


def hair_cell(d, an, style):
    """The hair of a style placed by the frame's anchor (the whole hair is one layer, drawn above the body)."""
    p = f"{HEADS}/hair/{style}/{'right' if d == 'left' else d}.png"
    dyes = json.load(open(f"{DATA}/hair-dyes.json", encoding="utf-8"))["styles"]
    dye = tuple(int(dyes[style][i:i + 2], 16) for i in (1, 3, 5))
    return placed(Image.fromarray(nw.dyed_hair(d, style, dye=dye, path=p).astype(np.uint8)), an)


def compose(anim, d, f, style, pad=24):
    """A frame as the game draws it: bald body, whole hair, over-hair, staff / book, hand layer."""
    an = next(e for e in json.load(open(f"{DATA}/hair-anchors.json", encoding="utf-8"))["anims"] if e["anim"] == anim and e["dir"] == d)["frames"][f]
    box = (f * F, ROWS[d] * F, (f + 1) * F, (ROWS[d] + 1) * F)
    bald = Image.open(f"{BODY}/bald/{anim}.png").convert("RGBA")
    body = bald.crop(box)
    sheet = Image.new("RGBA", bald.size); sheet.paste(body, box[:2])
    tmp = f"{tempfile.gettempdir()}/wizard-bald-compose.png".replace("\\", "/"); sheet.save(tmp)
    top = hair_cell(d, an, style)
    top.alpha_composite(Image.open(f"{BODY}/over-hair/{anim}.png").convert("RGBA").crop(box))
    return cv.wb.wc.chf.compose("wizard", anim, d, f, bare_path=tmp, tier=0, pad=pad, over=top)


def check(out_dir, styles=(0, 2, 4)):
    os.makedirs(out_dir, exist_ok=True)
    W = F + 48
    for anim in ANIMS:
        n = Image.open(f"{BODY}/bald/{anim}.png").width // F
        sheet = Image.new("RGBA", (W * n, W * 4 * len(styles)), (58, 62, 70, 255))
        for i, d in enumerate(DIRS):
            for j, st in enumerate(styles):
                for f in range(n):
                    sheet.alpha_composite(compose(anim, d, f, st), (f * W, (i * len(styles) + j) * W))
        p = f"{out_dir}/{anim}.png"; sheet.convert("RGB").save(p); print(p, sheet.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "verify": verify()
    elif cmd == "write": write()
    elif cmd == "check": check(sys.argv[2], tuple(int(x) for x in sys.argv[3].split(",")) if len(sys.argv) > 3 else (0, 2, 4))
    else: print(__doc__)

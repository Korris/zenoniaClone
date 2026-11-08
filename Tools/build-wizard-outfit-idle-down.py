"""Wizard armour outfits T1..T9, idle facing down only (2026-11-07, design docs/reference/wizard-sets-10-tiers-ladder-approved.png).
Like the Warrior outfits (Tools/build-warrior-outfit-sheets.py): ChatGPT repaints the in-game idle-down frame (Lv1 headless body + hairstyle 0
head, empty fists, bare head) in each tier's outfit on flat #00FF00. The painted figure is fitted onto that frame: same height (hair top
to sole), soles on the same row, hair centred on the same column; breathed into 4 frames the way the Lv1 row breathes (BREATH, a vertical
squash about the soles, as Tools/build-idle-down-doll-from-redraw.py). The fist layer is the new pixels inside the Lv1 fist mask.
T0 is the Lv1 body itself (no outfit folder).

Sources: Assets/ArtLibrary/classes/wizard/outfits/set-tN/src/idle-down.png; inputs sent to ChatGPT in .../set-tN/ref/.
Writes Assets/Resources/Sprites/classes/wizard/body/set-tN/{bare,hand}/idle.png (row 0 new, rows 1-3 = Lv1) + .meta (new guid),
then run `python Tools/build-head-layer.py wizard outfit-headless set-tN` for the hairstyle layer.

  python Tools/build-wizard-outfit-idle-down.py ref                 pose ref + design crops of every tier
  python Tools/build-wizard-outfit-idle-down.py check OUT.png [N..] Lv1 vs fitted outfits (frame 0 + 1, headless + head 0), nothing written
  python Tools/build-wizard-outfit-idle-down.py write N [N..]       sheets + metas of these tiers
  python Tools/build-wizard-outfit-idle-down.py compare OUT.png N.. design crop (set-tN/ref/design-large.png) beside the fitted frame, same height, to check part by part
  python Tools/build-wizard-outfit-idle-down.py metas               .meta for set-tN/headless (after outfit-headless), copied from bare/idle.png.meta
"""
import os, re, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
FRAME = 192
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/wizard/body"
HEAD0 = f"{RES}/Sprites/classes/wizard/heads/0/down.png"
ART = "Assets/ArtLibrary/classes/wizard/outfits"
BREATH = (0, 1, 2, 1)  # rows shorter per frame, soles planted (the Lv1 row's breathing)
DESIGN = "docs/reference/wizard-sets-10-tiers-ladder-approved.png"
DESIGN_T0 = "docs/reference/wizard-sets-10-tiers-ladder-v3.png"  # T0 redrawn after the hero-select art
HEAD_X = [111, 272, 451, 645, 836, 1038, 1250, 1474, 1741, 2026]  # head centre of each tier in the design sheets


def cell(path, row=0, f=0):
    return Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))


def lv1_frame():
    """The in-game idle-down frame 0: headless body + hairstyle 0 head (bare head, no hood)."""
    c = cell(f"{BODY}/headless/idle.png")
    c.alpha_composite(Image.open(HEAD0).convert("RGBA"))
    return c


def key_green(path):
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # distance from pure #00FF00, so the green coats of T1 (moss) survive: only near-key pixels go transparent
    d = np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 40) / 70, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(lab == 1 + int(np.argmax(sizes)), iterations=3)  # the figure only
    edge = alpha < 0.98
    a[..., 1] = np.where(edge, np.minimum(g, np.maximum(r, b) + 20), g)  # green spill on the soft edge only
    a[..., 3] = alpha * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def metrics(im):
    """Hair top, sole row, hair centre column (top 20% of the figure)."""
    al = np.asarray(im)[..., 3] > 100
    ys, xs = np.where(al)
    top, bot = ys.min(), ys.max()
    head = xs[ys < top + 0.2 * (bot - top)]
    return top, bot, (head.min() + head.max()) / 2


def skin_blobs(im, y0=0, y1=None):
    """(x, y, size) of the pale skin pieces (face, fists) between rows y0 and y1, left to right."""
    a = np.asarray(im.convert("RGBA")).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    skin = (al > 200) & (r > 225) & (g > 180) & (b > 150) & (r - b > 20) & (r - b < 90)
    skin[:y0] = False
    if y1 is not None:
        skin[y1:] = False
    lab, n = ndimage.label(skin)
    out = []
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        if len(ys) > 25:
            out.append((xs.mean(), ys.mean(), len(ys)))
    return sorted(out)


def face_x(im, top):
    """Centre column of the face: the biggest skin piece in the head rows (spiky hair is not symmetric, so its centre drifts)."""
    blobs = skin_blobs(im, top + 30, top + 80)
    return max(blobs, key=lambda b: b[2])[0] if blobs else None


def fitted(tier):
    fig = key_green(f"{ART}/set-t{tier}/src/idle-down.png")
    fig = fig.crop(fig.getbbox())
    lv1 = lv1_frame()
    top, bot, hx = metrics(lv1)
    ftop, fbot, _ = metrics(fig)
    k = (bot - top + 1) / (fbot - ftop + 1)
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    ftop, fbot, fhx = metrics(fig)
    fx, lx = face_x(fig, ftop), face_x(lv1, top)
    if fx is not None and lx is not None:  # 2026-11-08: centre the face under the head layer (hair centre left bodies 4 px off)
        hx, fhx = lx, fx
    return fig, int(round(hx - fhx)), bot - fbot  # figure, x offset, y offset of its top-left


def fist_mask(frame, lv1_fist):
    """The Lv1 fist mask with each fist moved onto the fist the outfit painting actually has (nearest skin piece below the head)."""
    lab, n = ndimage.label(lv1_fist)
    out = np.zeros_like(lv1_fist)
    blobs = [b for b in skin_blobs(frame, 98, 145) if b[2] < 150]
    for i in range(1, n + 1):
        ys, xs = np.where(lab == i)
        cx, cy = xs.mean(), ys.mean()
        near = min(blobs, key=lambda b: (b[0] - cx) ** 2 + (b[1] - cy) ** 2) if blobs else None
        dx, dy = (0, 0) if near is None or abs(near[0] - cx) > 12 or abs(near[1] - cy) > 12 else (round(near[0] - cx), round(near[1] - cy))
        out[np.clip(ys + dy, 0, FRAME - 1), np.clip(xs + dx, 0, FRAME - 1)] = True
    return out


def frames(tier):
    fig, x, y = fitted(tier)
    sole = y + fig.height  # one past the sole edge
    out = []
    for less in BREATH:
        im = fig.resize((fig.width, fig.height - less), Image.LANCZOS) if less else fig
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.paste(im, (x, sole - im.height), im)
        out.append(c)
    return out


def ref():
    """Pose ref (in-game frame 0 on #00FF00, 5x) and the design crop of every tier."""
    os.makedirs(f"{ART}/ref", exist_ok=True)
    f0 = lv1_frame()
    x0, y0, x1, y1 = f0.getbbox()
    bg = Image.new("RGBA", (FRAME + 40, FRAME + 40), (0, 255, 0, 255))
    bg.alpha_composite(f0, (20, 20))
    pose = bg.crop((x0 + 8, y0 + 8, x1 + 32, y1 + 32)).convert("RGB")
    pose.resize((pose.width * 5, pose.height * 5), Image.NEAREST).save(f"{ART}/ref/idle-down-pose-lv1.png")
    for t in range(10):
        src = Image.open(DESIGN_T0 if t == 0 else DESIGN).convert("RGB")
        l = 0 if t == 0 else (HEAD_X[t - 1] + HEAD_X[t]) // 2 - 10
        r = src.width if t == 9 else (HEAD_X[t] + HEAD_X[t + 1]) // 2 + 10
        os.makedirs(f"{ART}/set-t{t}/ref", exist_ok=True)
        c = src.crop((l, 90, r, 560))
        c.resize((c.width * 2, c.height * 2), Image.LANCZOS).save(f"{ART}/set-t{t}/ref/design.png")
    print("ref written:", f"{ART}/ref/idle-down-pose-lv1.png", "+ set-t0..9/ref/design.png")


def headless_preview(c):
    """Rough in-game look for `check`: the outfit with head 0 on top (the real headless cut is build-head-layer.py outfit-headless)."""
    out = c.copy()
    out.alpha_composite(Image.open(HEAD0).convert("RGBA"))
    return out


def check(path, tiers):
    tiers = tiers or [t for t in range(1, 10) if os.path.exists(f"{ART}/set-t{t}/src/idle-down.png")]
    z, S = 2, FRAME
    im = Image.new("RGBA", (S * (2 * len(tiers) + 1), S), (58, 62, 70, 255))
    im.alpha_composite(lv1_frame(), (0, 0))
    for i, t in enumerate(tiers):
        fr = frames(t)
        im.alpha_composite(fr[0], ((2 * i + 1) * S, 0))
        im.alpha_composite(headless_preview(fr[2]), ((2 * i + 2) * S, 0))
    im.resize((im.width * z // 2, im.height * z // 2), Image.NEAREST).save(path)
    print(path, tiers)


def compare(path, tiers):
    H = 420
    cols = []
    for t in tiers:
        d = Image.open(f"{ART}/set-t{t}/ref/design-large.png").convert("RGBA")
        d = d.resize((round(d.width * H / d.height), H), Image.LANCZOS)
        f = frames(t)[0]
        f = f.crop(f.getbbox())
        f = f.resize((round(f.width * (H - 20) / f.height), H - 20), Image.NEAREST)
        cols.append((d, f))
    W = sum(d.width + f.width + 30 for d, f in cols)
    im = Image.new("RGBA", (W, H), (58, 62, 70, 255))
    x = 0
    for d, f in cols:
        im.alpha_composite(d, (x, 0)); x += d.width + 5
        im.alpha_composite(f, (x, 10)); x += f.width + 25
    im.convert("RGB").save(path)
    print(path)


def meta_copy(src_png, dst_png):
    raw = open(src_png + ".meta", encoding="utf-8").read()
    raw = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, raw, count=1)
    if not os.path.exists(dst_png + ".meta"):
        open(dst_png + ".meta", "w", encoding="utf-8").write(raw)


def folder_meta(path):
    if not os.path.exists(path + ".meta"):
        open(path + ".meta", "w", encoding="utf-8").write(
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def write(tiers):
    for t in tiers:
        fr = frames(t)
        out = f"{BODY}/set-t{t}"
        bare = Image.open(f"{BODY}/bare/idle.png").convert("RGBA")
        hand = Image.open(f"{BODY}/hand/idle.png").convert("RGBA")
        fist = np.asarray(hand)[:FRAME, :FRAME * 4, 3] > 0
        for i, c in enumerate(fr):
            bare.paste(c, (i * FRAME, 0))
            arr = np.asarray(c).copy()
            arr[~fist_mask(c, fist[:, i * FRAME:(i + 1) * FRAME])] = 0
            hand.paste(Image.fromarray(arr), (i * FRAME, 0))
        for sub, im in (("bare", bare), ("hand", hand)):
            os.makedirs(f"{out}/{sub}", exist_ok=True)
            im.save(f"{out}/{sub}/idle.png")
            meta_copy(f"{BODY}/{sub}/idle.png", f"{out}/{sub}/idle.png")
            folder_meta(f"{out}/{sub}")
        folder_meta(out)
        print("written", out)


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "ref":
        ref()
    elif cmd == "check":
        check(sys.argv[2], [int(v) for v in sys.argv[3:]])
    elif cmd == "compare":
        compare(sys.argv[2], [int(v) for v in sys.argv[3:]])
    elif cmd == "metas":
        for t in range(1, 10):
            hl = f"{BODY}/set-t{t}/headless"
            if os.path.exists(f"{hl}/idle.png"):
                meta_copy(f"{BODY}/bare/idle.png", f"{hl}/idle.png")
                folder_meta(hl)
    elif cmd == "write":
        write([int(v) for v in sys.argv[2:]])
    else:
        print(__doc__)

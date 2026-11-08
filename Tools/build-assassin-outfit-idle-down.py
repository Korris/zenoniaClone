"""Assassin armour outfits T1..T9, idle facing down only (2026-11-08, design docs/reference/assassin-sets-10-tiers-ladder-v3.png,
= ArtLibrary/classes/assassin/outfits/design-ladder-v3/assassin-10-tiers-v3-chatgpt-2.png). Same recipe as the Wizard and Mechanic
(Tools/build-wizard-outfit-idle-down.py, build-mechanic-outfit-idle-down.py): ChatGPT repaints the in-game idle-down frame (Lv1 bare idle
row 0 = headless body + hairstyle 0 head, empty fists, no daggers) in each tier's outfit on a flat key colour, copying the outfit from a 3x
nearest crop of that tier on the design sheet (set-tN/ref/design-large.png). Pose, proportions and pixel style come from the game frame.

The painted figure is fitted onto the Lv1 frame: same height (hair top incl. ponytail to sole), soles on the same row, face centre (the
skin around the eyes, between headband and mask) on the same column, so the hairstyle head layer sits where the Lv1 head is. Breathing:
the Lv1 row squashes about the cell bottom (head-anchors sy 1, 0.996, 0.992, 0.996), so the 4 frames do the same. The fist layer
(body/hand) is the new pixels inside the Lv1 fist mask, moved onto the painted fist when it is off (glove found by template match).
T0 is the Lv1 body itself (no outfit folder).

Sources: Assets/ArtLibrary/classes/assassin/outfits/set-tN/src/idle-down.png on flat #00FF00 (KEY_MAGENTA tiers on #FF00FF).
Writes Assets/Resources/Sprites/classes/assassin/body/set-tN/{bare,hand}/idle.png (row 0 new, rows 1-3 = Lv1) + .meta (new guid), then
`python Tools/build-head-layer.py assassin outfit-headless set-tN` and `... metas` here for the headless .meta.

  python Tools/build-assassin-outfit-idle-down.py ref                  pose ref + design crops of every tier
  python Tools/build-assassin-outfit-idle-down.py check OUT.png [N..]  Lv1 vs fitted outfits (frame 0, 2x) + game-size strip, nothing written
  python Tools/build-assassin-outfit-idle-down.py compare OUT.png N..  design crop beside the fitted frame, same height, part by part
  python Tools/build-assassin-outfit-idle-down.py write N [N..]        sheets + metas of these tiers
  python Tools/build-assassin-outfit-idle-down.py clean-headless N..  drop loose ponytail bits of the repaint (after outfit-headless)
  python Tools/build-assassin-outfit-idle-down.py headless-patch N.. outfit pixels taken for hair put back (T8 pads), then rerun outfit-headless + clean-headless
  python Tools/build-assassin-outfit-idle-down.py metas                .meta for set-tN/headless (after outfit-headless)
"""
import os, re, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
FRAME = 192
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/assassin/body"
ART = "Assets/ArtLibrary/classes/assassin/outfits"
DESIGN = f"{ART}/design-ladder-v3/assassin-10-tiers-v3-chatgpt-2.png"
SY = (1.0, 0.996, 0.992, 0.996)  # Lv1 idle-down breathing: vertical scale about the cell bottom (head-anchors.json)
# design sheet per tier (measured 2026-11-08): face centre column, hair top row (purple, ponytail incl.), sole row
D_FACE = [117, 296, 471, 651, 838, 1041, 1254, 1478, 1716, 2004]
D_TOP = [240, 236, 232, 227, 218, 209, 203, 191, 180, 178]
D_SOLE = [527, 527, 528, 528, 528, 528, 532, 528, 529, 538]
D_SPLIT = [0, 185, 355, 530, 715, 905, 1110, 1340, 1580, 1840, 2172]  # columns between the figures (neighbours' ponytails overlap)
KEY_MAGENTA = set()  # tiers painted on #FF00FF (none so far: T3 jade is dark enough for the distance key)


def cell(path, row=0, f=0):
    return Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))


def lv1_frame():
    return cell(f"{BODY}/bare/idle.png")


def key_flat(path, magenta=False):
    """Straight RGBA of the figure on a flat #00FF00 (or #FF00FF): opacity by distance from the pure key, biggest pieces kept."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.maximum(np.maximum(255 - r, 255 - b), g) if magenta else np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 40) / 70, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        keep = [i + 1 for i in range(n) if sizes[i] >= 0.01 * sizes.max()]  # figure + big floating parts (flames, halo)
        alpha = alpha * ndimage.binary_dilation(np.isin(lab, keep), iterations=3)
    edge = alpha < 0.98
    if magenta:
        cap = g + 20
        a[..., 0] = np.where(edge, np.minimum(r, cap), r)
        a[..., 2] = np.where(edge, np.minimum(b, cap), b)
    else:
        a[..., 1] = np.where(edge, np.minimum(g, np.maximum(r, b) + 20), g)
    a[..., 3] = alpha * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def hair_top(im):
    """Top row of the purple hair (a halo or flames above it do not count)."""
    a = np.asarray(im).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    purple = (al > 150) & (b > 50) & (r > 35) & (g < r - 10) & (g < b - 5) & (np.abs(r - b) < 80)
    rows = np.where(purple.sum(1) >= 2)[0]
    return int(rows.min())


def sole(im):
    al = np.asarray(im)[..., 3] > 100
    return int(np.where(al.any(1))[0].max())


def face_x(im, top, bot):
    """Centre column of the face skin (around the eyes) in the head rows."""
    a = np.asarray(im).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    skin = (al > 200) & (r > 200) & (g > 140) & (b > 110) & (r - b > 25) & (r - b < 110)
    h = bot - top
    skin[: top + int(0.3 * h)] = False
    skin[top + int(0.62 * h):] = False
    lab, n = ndimage.label(skin)
    if not n:
        return None
    sizes = ndimage.sum(skin, lab, range(1, n + 1))
    ys, xs = np.where(lab == 1 + int(np.argmax(sizes)))
    return float(xs.mean())


def fitted(tier):
    fig = key_flat(f"{ART}/set-t{tier}/src/idle-down.png", tier in KEY_MAGENTA)
    fig = fig.crop(fig.getbbox())
    lv1 = lv1_frame()
    top, bot = hair_top(lv1), sole(lv1)
    ftop, fbot = hair_top(fig), sole(fig)
    k = (bot - top + 1) / (fbot - ftop + 1)
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    ftop, fbot = hair_top(fig), sole(fig)
    fx, lx = face_x(fig, ftop, fbot), face_x(lv1, top, bot)
    if fx is None:
        raise SystemExit(f"T{tier}: no face skin found")
    y = bot - fbot
    if y < 2:  # something above the hair (T9 lunar halo) would leave the cell: squash the rows above the hair top into the room left
        above = fig.crop((0, 0, fig.width, ftop)).resize((fig.width, max(1, ftop + y - 2)), Image.LANCZOS)
        rest = fig.crop((0, ftop, fig.width, fig.height))
        fig = Image.new("RGBA", (fig.width, above.height + rest.height), (0, 0, 0, 0))
        fig.paste(above, (0, 0))
        fig.paste(rest, (0, above.height))
        y = bot - (fig.height - 1)
    return fig, int(round(lx - fx)), y  # figure, x / y offset of its top-left in the cell


def frames(tier):
    fig, x, y = fitted(tier)
    base = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    base.paste(fig, (x, y), fig)  # paste clips a figure wider than the cell (alpha_composite refuses negative offsets)
    out = []
    for sy in SY:
        if sy == 1.0:
            out.append(base)
            continue
        h = round(FRAME * sy)
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.alpha_composite(base.resize((FRAME, h), Image.LANCZOS), (0, FRAME - h))
        out.append(c)
    return out


def design_crop(t, up=0.12, down=0.04, side=0.62, top_override=None):
    src = Image.open(DESIGN).convert("RGB")
    H = D_SOLE[t] - D_TOP[t]
    y0 = top_override if top_override is not None else D_TOP[t] - up * H
    box = (D_FACE[t] - side * H, y0, D_FACE[t] + side * H, D_SOLE[t] + down * H)
    c = np.asarray(src.crop(tuple(int(round(v)) for v in box))).copy()
    xs = np.arange(c.shape[1]) + int(round(box[0]))
    c[:, (xs < D_SPLIT[t]) | (xs >= D_SPLIT[t + 1])] = (0, 255, 0)
    # neighbours' daggers and hair reach into the crop: keep only the pieces that reach the middle half of the columns
    r, g, b = [c[..., i].astype(int) for i in range(3)]
    fg = np.maximum(np.maximum(r, b), 255 - g) > 60
    lab, n = ndimage.label(ndimage.binary_dilation(fg, iterations=1))
    w = c.shape[1]
    mid = np.unique(lab[:, w // 4: 3 * w // 4])
    c[~np.isin(lab, mid[mid > 0])] = (0, 255, 0)
    return Image.fromarray(c)


def ref():
    """Pose ref (Lv1 frame 0 on #00FF00, 5x) and per tier: design crop 2x (design.png) and a tight 3x nearest crop (design-large.png)."""
    os.makedirs(f"{ART}/ref", exist_ok=True)
    f0 = lv1_frame()
    x0, y0, x1, y1 = f0.getbbox()
    bg = Image.new("RGBA", (FRAME + 40, FRAME + 40), (0, 255, 0, 255))
    bg.alpha_composite(f0, (20, 20))
    pose = bg.crop((x0 + 8, y0 + 8, x1 + 32, y1 + 32)).convert("RGB")
    pose.resize((pose.width * 5, pose.height * 5), Image.NEAREST).save(f"{ART}/ref/idle-down-pose-lv1.png")
    for t in range(10):
        os.makedirs(f"{ART}/set-t{t}/ref", exist_ok=True)
        c = design_crop(t, top_override=100 if t == 9 else None)
        c.resize((c.width * 2, c.height * 2), Image.LANCZOS).save(f"{ART}/set-t{t}/ref/design.png")
        big = design_crop(t, up=0.05, down=0.03, side=0.5, top_override=100 if t == 9 else None)
        big.resize((big.width * 3, big.height * 3), Image.NEAREST).save(f"{ART}/set-t{t}/ref/design-large.png")
    print("ref written:", f"{ART}/ref/idle-down-pose-lv1.png", "+ set-t0..9/ref/design*.png")


def check(path, tiers):
    tiers = tiers or [t for t in range(1, 10) if os.path.exists(f"{ART}/set-t{t}/src/idle-down.png")]
    cells = [lv1_frame()] + [frames(t)[0] for t in tiers]
    big = Image.new("RGBA", (FRAME * 2 * len(cells), FRAME * 2), (58, 62, 70, 255))
    small = Image.new("RGBA", (100 * len(cells), 100), (58, 62, 70, 255))
    fist = np.asarray(cell(f"{BODY}/hand/idle.png"))[..., 3] > 0
    edge = fist & ~ndimage.binary_erosion(fist)
    for i, c in enumerate(cells):
        cc = c.copy()
        arr = np.asarray(cc).copy()
        arr[edge] = (0, 255, 255, 255)  # Lv1 fist mask outline: the painted fist should sit inside it
        big.alpha_composite(Image.fromarray(arr).resize((FRAME * 2, FRAME * 2), Image.NEAREST), (i * FRAME * 2, 0))
        small.alpha_composite(c.resize((100, 100), Image.LANCZOS), (i * 100, 0))
    out = Image.new("RGBA", (big.width, big.height + small.height), (58, 62, 70, 255))
    out.alpha_composite(big)
    out.alpha_composite(small, (0, big.height))
    out.save(path)
    print(path, "Lv1 +", tiers)


def compare(path, tiers):
    H = 420
    cols = []
    for t in tiers:
        d = design_crop(t).convert("RGBA")
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


FIST_SHIFT = {}  # tier: ((dx, dy) main fist, (dx, dy) off fist) when the painted fist is off the Lv1 mask (read off `check`)


def fist_mask(lv1_fist, tier):
    """The Lv1 fist mask, each fist (left = main, right = off) moved by FIST_SHIFT[tier]."""
    lab, n = ndimage.label(ndimage.binary_closing(lv1_fist, iterations=2))
    out = np.zeros_like(lv1_fist)
    shifts = FIST_SHIFT.get(tier, ((0, 0), (0, 0)))
    for i in range(1, n + 1):
        ys, xs = np.where((lab == i) & lv1_fist)
        if not len(ys):
            continue
        dx, dy = shifts[0] if xs.mean() < FRAME / 2 else shifts[1]
        out[np.clip(ys + dy, 0, FRAME - 1), np.clip(xs + dx, 0, FRAME - 1)] = True
    return out


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
            arr[~fist_mask(fist[:, i * FRAME:(i + 1) * FRAME], t)] = 0
            hand.paste(Image.fromarray(arr), (i * FRAME, 0))
        folder_meta(out)
        for sub, im in (("bare", bare), ("hand", hand)):
            os.makedirs(f"{out}/{sub}", exist_ok=True)
            meta_copy(f"{BODY}/{sub}/idle.png", f"{out}/{sub}/idle.png")  # .meta first: Unity may be open on this project
            im.save(f"{out}/{sub}/idle.png")
            folder_meta(f"{out}/{sub}")
        print("written", out)


def clean_headless(tiers):
    """After outfit-headless: drop loose bits of the repaint's own ponytail left beside the head (a purple streak beside hairstyles
    without a ponytail): pieces in row 0 not joined to the body whose pixels are mostly hair purple / dark outline."""
    for t in tiers:
        path = f"{BODY}/set-t{t}/headless/idle.png"
        im = np.asarray(Image.open(path).convert("RGBA")).copy()
        pp = f"{ART}/set-t{t}/headless-patch/idle.png"
        keep = np.asarray(Image.open(pp).convert("RGBA"))[..., 3] > 0 if os.path.exists(pp) else np.zeros(im.shape[:2], bool)
        n_erased = 0
        for f in range(4):
            c = im[:FRAME, f * FRAME:(f + 1) * FRAME]
            kp = keep[:FRAME, f * FRAME:(f + 1) * FRAME]
            r, g, b, al = [c[..., i].astype(int) for i in range(4)]
            solid = al > 8
            lab, n = ndimage.label(solid, np.ones((3, 3)))
            # thin hair strands (<= 2 px) joined to the body but outside the Lv1 headless silhouette
            lv1 = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA"))[:FRAME, f * FRAME:(f + 1) * FRAME, 3] > 8
            hair0 = ((b > 40) & (g < r - 5) & (g < b) & (np.abs(r - b) < 90)) | ((r + g + b) < 150)
            thin = solid & ~ndimage.binary_opening(solid, np.ones((3, 3))) & hair0 & ~ndimage.binary_dilation(lv1, iterations=2)
            thin[120:] = False
            thin &= ~kp
            c[thin] = 0
            n_erased += int(thin.sum())
            solid = c[..., 3] > 8
            lab, n = ndimage.label(solid, np.ones((3, 3)))
            if n < 2:
                continue
            sizes = ndimage.sum(solid, lab, range(1, n + 1))
            body = 1 + int(np.argmax(sizes))
            hairish = ((b > 40) & (g < r - 5) & (g < b) & (np.abs(r - b) < 90)) | ((r + g + b) < 150)
            for i in range(1, n + 1):
                if i == body:
                    continue
                m = (lab == i) & ~kp
                if hairish[m].mean() > 0.6 and sizes[i - 1] < 400:
                    c[m] = 0
                    n_erased += int(sizes[i - 1])
        Image.fromarray(im).save(path)
        print("cleaned", path, n_erased, "px")


def headless_patch(tiers, x_min=50, y_min=95):
    """Outfit pixels that outfit-headless took for hair (purple crystal pads of T8 beside the neck) put back: pixels of set-tN/bare row 0
    erased in set-tN/headless but outside the Lv1 head cut (+1 px), right of the ponytail (x >= x_min) and below the hair (y >= y_min).
    Written to ArtLibrary/.../set-tN/headless-patch/idle.png, which outfit-headless draws under the cut sheet: rerun it after this."""
    for t in tiers:
        bare = np.asarray(Image.open(f"{BODY}/set-t{t}/bare/idle.png").convert("RGBA"))
        hl = np.asarray(Image.open(f"{BODY}/set-t{t}/headless/idle.png").convert("RGBA"))
        lv1 = np.asarray(Image.open(f"{BODY}/bare/idle.png").convert("RGBA"))
        lv1h = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA"))
        patch = np.zeros_like(bare)
        for f in range(4):
            sl = (slice(0, FRAME), slice(f * FRAME, (f + 1) * FRAME))
            cut = (lv1[sl][..., 3] > 8) & (lv1h[sl][..., 3] <= 8)
            m = (bare[sl][..., 3] > 8) & (hl[sl][..., 3] <= 8) & ~ndimage.binary_dilation(cut, iterations=1)
            # also inside the Lv1 head cut (its hair spreads over the shoulders): bright crystal / silver outfit pixels at shoulder height
            px = bare[sl].astype(int)
            bright = ((px[..., 0] > 140) & (px[..., 2] > 150)) | (px[..., :3].min(2) > 150)
            m |= (bare[sl][..., 3] > 8) & (hl[sl][..., 3] <= 8) & bright & (np.arange(FRAME)[:, None] >= y_min - 6)
            m[:y_min - 6] = False
            m[:, :x_min] = False
            p = patch[sl]
            p[m] = bare[sl][m]
        out = f"{ART}/set-t{t}/headless-patch"
        os.makedirs(out, exist_ok=True)
        Image.fromarray(patch).save(f"{out}/idle.png")
        print("patch", out, int((patch[..., 3] > 0).sum()), "px")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "ref":
        ref()
    elif cmd == "check":
        check(sys.argv[2], [int(v) for v in sys.argv[3:]])
    elif cmd == "compare":
        compare(sys.argv[2], [int(v) for v in sys.argv[3:]])
    elif cmd == "write":
        write([int(v) for v in sys.argv[2:]])
    elif cmd == "clean-headless":
        clean_headless([int(v) for v in sys.argv[2:]])
    elif cmd == "headless-patch":
        headless_patch([int(v) for v in sys.argv[2:]])
    elif cmd == "metas":
        for t in range(1, 10):
            hl = f"{BODY}/set-t{t}/headless"
            if os.path.exists(f"{hl}/idle.png"):
                meta_copy(f"{BODY}/bare/idle.png", f"{hl}/idle.png")
                folder_meta(hl)
    else:
        print(__doc__)

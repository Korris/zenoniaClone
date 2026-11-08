"""Mechanic armour outfits T1..T9, idle facing down only (2026-11-07, design docs/reference/mechanic-sets-10-tiers-ladder-v3.png).
Same recipe as the Wizard (Tools/build-wizard-outfit-idle-down.py) and Warrior outfits: ChatGPT repaints the in-game idle-down frame
(Lv1 body/bare/idle.png row 0, empty fists, goggles, no pistols, no cannon) in each tier's outfit on a flat key colour. The design sheet is
only a reference for colours, materials and motifs; pose, proportions and pixel style come from the game frame.
The painted figure is fitted onto the Lv1 frame: same height (hair top to sole), soles on the same row, hair centred on the same column;
breathed into 4 frames like the Lv1 row (GROW: the figure grows 0/1/2/1 px upward from the sole, Tools/mechanic-idle-doll-helpers.py).
The fist layer (body/hand) is the new pixels inside the Lv1 fist mask. T0 is the Lv1 body itself (no outfit folder).

Sources: Assets/ArtLibrary/classes/mechanic/outfits/set-tN/src/idle-down.png on flat #00FF00 (KEY_MAGENTA tiers on #FF00FF);
inputs sent to ChatGPT in .../set-tN/ref/ and .../outfits/ref/.
Writes Assets/Resources/Sprites/classes/mechanic/body/set-tN/{bare,hand}/idle.png (row 0 new, rows 1-3 = Lv1) + .meta (new guid),
then run `python Tools/build-head-layer.py mechanic outfit-headless set-tN` for the hairstyle layer.

  python Tools/build-mechanic-outfit-idle-down.py ref                 pose ref + design crops of every tier
  python Tools/build-mechanic-outfit-idle-down.py check OUT.png [N..] Lv1 vs fitted outfits (frame 0 at 2x, + game size strip), nothing written
  python Tools/build-mechanic-outfit-idle-down.py write N [N..]       sheets + metas of these tiers
"""
import os, re, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
FRAME = 192
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/mechanic/body"
ART = "Assets/ArtLibrary/classes/mechanic/outfits"
GROW = (0, 1, 2, 1)  # px the figure grows upward from the sole in frames 0-3 (the Lv1 row's breathing)
DESIGN = "Assets/ArtLibrary/classes/mechanic/outfits/design-ladder-v3/mechanic-10-tiers-v3-chatgpt-2.png"  # docs/reference/...-v3.png is now an in-game composite (08/11)
# column borders between the 10 figures of the design sheet (full-size px), T0 | T1 | ... | T9
DESIGN_X = [0, 188, 360, 532, 705, 878, 1065, 1252, 1485, 1710, 2172]
KEY_MAGENTA = {4}  # jade / emerald outfit painted on #FF00FF instead of #00FF00


def cell(path, row=0, f=0):
    return Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))


def lv1_frame():
    """The in-game idle-down frame 0 of the Lv1 body (head included)."""
    return cell(f"{BODY}/bare/idle.png")


def key_flat(path, magenta=False):
    """Straight RGBA of the figure on a flat #00FF00 (or #FF00FF) background: only near-key pixels go transparent, biggest piece kept."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    if magenta:
        d = np.maximum(np.maximum(255 - r, 255 - b), g)
    else:
        d = np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 40) / 70, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        # the figure plus big floating parts (T9 halo ring above the hair); specks and stray sparks go
        keep = [i + 1 for i in range(n) if sizes[i] >= 0.01 * sizes.max()]
        alpha = alpha * ndimage.binary_dilation(np.isin(lab, keep), iterations=3)
    edge = alpha < 0.98
    if magenta:  # key spill on the soft edge only
        cap = g + 20
        a[..., 0] = np.where(edge, np.minimum(r, cap), r)
        a[..., 2] = np.where(edge, np.minimum(b, cap), b)
    else:
        a[..., 1] = np.where(edge, np.minimum(g, np.maximum(r, b) + 20), g)
    a[..., 3] = alpha * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def metrics(im):
    """Hair top, sole row, hair centre column (top 20% of the figure). Measured on the biggest piece only, so a floating halo
    above the hair does not count as the top."""
    al = np.asarray(im)[..., 3] > 100
    lab, n = ndimage.label(al, np.ones((3, 3)))
    if n > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        al = lab == 1 + int(np.argmax(sizes))
    ys, xs = np.where(al)
    top, bot = ys.min(), ys.max()
    # top = the blond hair's top when the figure has something above it (T9 halo ring joined to the wings)
    rgb = np.asarray(im)[..., :3].astype(int)
    blond = al & (rgb[..., 0] > 190) & (rgb[..., 1] > 140) & (rgb[..., 2] < 120) & (rgb[..., 0] - rgb[..., 2] > 110)
    hy = np.where(blond.any(1))[0]
    if len(hy) and hy.min() > top + 12:
        top = hy.min()
    head = xs[(ys >= top) & (ys < top + 0.2 * (bot - top))]
    return top, bot, (head.min() + head.max()) / 2


def fitted(tier):
    fig = key_flat(f"{ART}/set-t{tier}/src/idle-down.png", tier in KEY_MAGENTA)
    fig = fig.crop(fig.getbbox())
    top, bot, hx = metrics(lv1_frame())
    ftop, fbot, _ = metrics(fig)
    k = (bot - top + 1) / (fbot - ftop + 1)
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    ftop, fbot, fhx = metrics(fig)
    y = bot - fbot
    if y < 3:  # something above the hair (T9 halo) would leave the cell: squash the rows above the hair top into the room left
        above = fig.crop((0, 0, fig.width, ftop)).resize((fig.width, max(1, ftop + y - 3)), Image.LANCZOS)
        rest = fig.crop((0, ftop, fig.width, fig.height))
        fig = Image.new("RGBA", (fig.width, above.height + rest.height), (0, 0, 0, 0))
        fig.paste(above, (0, 0))
        fig.paste(rest, (0, above.height))
        y = bot - (fig.height - 1)
    return fig, int(round(hx - fhx)), y  # figure, x offset, y offset of its top-left


def frames(tier):
    fig, x, y = fitted(tier)
    sole = y + fig.height  # one past the sole edge
    out = []
    for grow in GROW:
        im = fig.resize((fig.width, fig.height + grow), Image.LANCZOS) if grow else fig
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.paste(im, (x, sole - im.height), im)
        out.append(c)
    return out


def ref():
    """Pose ref (in-game frame 0 on the key colour, 5x) and the design crop of every tier (2x)."""
    os.makedirs(f"{ART}/ref", exist_ok=True)
    f0 = lv1_frame()
    x0, y0, x1, y1 = f0.getbbox()
    for name, key in (("idle-down-pose-lv1.png", (0, 255, 0, 255)), ("idle-down-pose-lv1-magenta.png", (255, 0, 255, 255))):
        bg = Image.new("RGBA", (FRAME + 40, FRAME + 40), key)
        bg.alpha_composite(f0, (20, 20))
        pose = bg.crop((x0 + 8, y0 + 8, x1 + 32, y1 + 32)).convert("RGB")
        pose.resize((pose.width * 5, pose.height * 5), Image.NEAREST).save(f"{ART}/ref/{name}")
    src = Image.open(DESIGN).convert("RGB")
    for t in range(10):
        os.makedirs(f"{ART}/set-t{t}/ref", exist_ok=True)
        c = src.crop((DESIGN_X[t], 0, DESIGN_X[t + 1], src.height))
        c.resize((c.width * 2, c.height * 2), Image.LANCZOS).save(f"{ART}/set-t{t}/ref/design.png")
        # large tight crop (3x nearest) so ChatGPT copies the outfit part by part instead of drifting (designer, 2026-11-07)
        big = src.crop((DESIGN_X[t], 40 if t == 9 else 120, DESIGN_X[t + 1], 545))
        big.resize((big.width * 3, big.height * 3), Image.NEAREST).save(f"{ART}/set-t{t}/ref/design-large.png")
    print("ref written:", f"{ART}/ref/idle-down-pose-lv1*.png", "+ set-t0..9/ref/design.png")


def check(path, tiers):
    """Top: Lv1 and each fitted outfit, frame 0 at 2x. Bottom: the same at game size (~85 px tall) on grey, to judge readability."""
    tiers = tiers or [t for t in range(1, 10) if os.path.exists(f"{ART}/set-t{t}/src/idle-down.png")]
    cells = [lv1_frame()] + [frames(t)[0] for t in tiers]
    big = Image.new("RGBA", (FRAME * 2 * len(cells), FRAME * 2), (58, 62, 70, 255))
    small = Image.new("RGBA", (100 * len(cells), 100), (58, 62, 70, 255))
    for i, c in enumerate(cells):
        big.alpha_composite(c.resize((FRAME * 2, FRAME * 2), Image.NEAREST), (i * FRAME * 2, 0))
        small.alpha_composite(c.resize((100, 100), Image.LANCZOS), (i * 100, 0))
    out = Image.new("RGBA", (big.width, big.height + small.height), (58, 62, 70, 255))
    out.alpha_composite(big)
    out.alpha_composite(small, (0, big.height))
    out.save(path)
    print(path, "Lv1 +", tiers)


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
            arr[~fist[:, i * FRAME:(i + 1) * FRAME]] = 0
            hand.paste(Image.fromarray(arr), (i * FRAME, 0))
        folder_meta(out)
        for sub, im in (("bare", bare), ("hand", hand)):
            os.makedirs(f"{out}/{sub}", exist_ok=True)
            meta_copy(f"{BODY}/{sub}/idle.png", f"{out}/{sub}/idle.png")  # .meta first: Unity may be open on this project
            im.save(f"{out}/{sub}/idle.png")
            folder_meta(f"{out}/{sub}")
        print("written", out)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "ref":
        ref()
    elif cmd == "check":
        check(sys.argv[2], [int(v) for v in sys.argv[3:]])
    elif cmd == "write":
        write([int(v) for v in sys.argv[2:]])
    else:
        print(__doc__)

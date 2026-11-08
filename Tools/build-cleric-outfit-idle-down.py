"""Cleric armour outfits T1..T9, idle facing down only (2026-11-08, design docs/reference/cleric-sets-10-tiers-ladder-v3.png).
Same recipe as the Wizard (Tools/build-wizard-outfit-idle-down.py, whose helpers are reused): ChatGPT repaints the in-game idle-down frame
(Lv1 headless body + hairstyle 0 head = bare blonde head, empty fists, no staff, no shield) in each tier's outfit, copying the outfit from a
4x nearest crop of that tier on the design sheet. The painted figure is fitted onto the frame: same height (hair top to sole), soles on the
same row, face centred on the same column; breathed into 4 frames like the Lv1 row (BREATH, a vertical squash about the soles).
The fist layer is the new pixels inside the Lv1 fist mask (only the staff fist: the shield hides the other hand). T0 = the Lv1 body.

Sources: Assets/ArtLibrary/classes/cleric/outfits/set-tN/src/idle-down.png on flat #00FF00 (KEY_MAGENTA tiers on #FF00FF);
inputs sent to ChatGPT: outfits/ref/idle-down-pose-lv1*.png (game frame 5x), set-tN/ref/design-large.png (sheet crop 4x).
Writes Assets/Resources/Sprites/classes/cleric/body/set-tN/{bare,hand}/idle.png (row 0 new, rows 1-3 = Lv1) + .meta (new guid),
then run `python Tools/build-head-layer.py cleric outfit-headless set-tN` and `... metas` here.

  python Tools/build-cleric-outfit-idle-down.py ref                   pose ref + design crops of every tier
  python Tools/build-cleric-outfit-idle-down.py check OUT.png [N..]   Lv1 vs fitted outfits (frame 0 2x + game size strip), nothing written
  python Tools/build-cleric-outfit-idle-down.py compare OUT.png N..   design crop beside the fitted frame, same height, part by part
  python Tools/build-cleric-outfit-idle-down.py write N [N..]         sheets + metas of these tiers
  python Tools/build-cleric-outfit-idle-down.py metas                 .meta for set-tN/headless (after outfit-headless)
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
FRAME = 192
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/cleric/body"
HEAD0 = f"{RES}/Sprites/classes/cleric/heads/0/down.png"
ART = "Assets/ArtLibrary/classes/cleric/outfits"
DESIGN = "docs/reference/cleric-sets-10-tiers-ladder-v3.png"  # = outfits/design-ladder-v3/cleric-10-tiers-v3-chatgpt-2.png
BREATH = (0, 1, 2, 1)
KEY_MAGENTA = {3, 9}  # teal / emerald outfits painted on #FF00FF
# head centre column, hair top, sole row of each tier on the design sheet (measured 2026-11-08)
HEAD_X = [128, 311, 501, 685, 847, 1060, 1268, 1490, 1695, 1975]
HAIR_TOP = [283, 282, 282, 281, 279, 275, 273, 273, 270, 268]
SOLE = [550, 550, 550, 550, 550, 551, 552, 551, 551, 556]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


wo = load("wo", "Tools/build-wizard-outfit-idle-down.py")  # metrics, skin_blobs, face_x, fist_mask, meta_copy, folder_meta
mo = load("mo", "Tools/build-mechanic-outfit-idle-down.py")  # key_flat (green or magenta key)
os.chdir(ROOT)


def lv1_frame():
    """The in-game idle-down frame 0: headless body + hairstyle 0 head (bare blonde head, no hat)."""
    c = wo.cell(f"{BODY}/headless/idle.png")
    c.alpha_composite(Image.open(HEAD0).convert("RGBA"))
    return c


def fitted(tier):
    fig = mo.key_flat(f"{ART}/set-t{tier}/src/idle-down.png", tier in KEY_MAGENTA)
    fig = fig.crop(fig.getbbox())
    lv1 = lv1_frame()
    top, bot, hx = mo.metrics(lv1)
    ftop, fbot, _ = mo.metrics(fig)
    k = (bot - top + 1) / (fbot - ftop + 1)
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    ftop, fbot, fhx = mo.metrics(fig)
    fx, lx = wo.face_x(fig, ftop), wo.face_x(lv1, top)
    if fx is not None and lx is not None:  # centre the face under the head layer
        hx, fhx = lx, fx
    return fig, int(round(hx - fhx)), bot - fbot


def frames(tier):
    fig, x, y = fitted(tier)
    sole = y + fig.height
    out = []
    for less in BREATH:
        im = fig.resize((fig.width, fig.height - less), Image.LANCZOS) if less else fig
        c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        c.paste(im, (x, sole - im.height), im)
        out.append(c)
    return out


def ref():
    """Pose ref (game frame 0 on both keys, 5x) and the design crop of every tier (4x nearest, hero only, hair top to soles)."""
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
        l, r = (HEAD_X[t] - 225, src.width - 4) if t == 9 else (HEAD_X[t] - 108, HEAD_X[t] + 108)
        big = src.crop((l, HAIR_TOP[t] - 25, r, SOLE[t] + 8))
        if t == 9:  # the T8 shield pokes into the left edge: anything there that is not green / gold wing becomes key
            a = np.asarray(big).astype(int).copy()
            zone = np.zeros(a.shape[:2], bool)
            zone[:, :1812 - l] = True
            wing = (a[..., 1] - a[..., 2] > 40) & (a[..., 1] > 90)
            a[zone & ~wing] = (255, 0, 255)
            big = Image.fromarray(a.astype(np.uint8))
        big.resize((big.width * 4, big.height * 4), Image.NEAREST).save(f"{ART}/set-t{t}/ref/design-large.png")
    print("ref written:", f"{ART}/ref/idle-down-pose-lv1*.png", "+ set-t0..9/ref/design-large.png")


def check(path, tiers):
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
    im = Image.new("RGBA", (sum(d.width + f.width + 30 for d, f in cols), H), (58, 62, 70, 255))
    x = 0
    for d, f in cols:
        im.alpha_composite(d, (x, 0)); x += d.width + 5
        im.alpha_composite(f, (x, 10)); x += f.width + 25
    im.convert("RGB").save(path)
    print(path)


def headless_patch(t):
    """T9 leaf wings reach into the Lv1 hat area, which outfit-headless erases (ragged notch beside the head): the wing-coloured pixels
    of the outfit row 0 inside that area go to outfits/set-tN/headless-patch/idle.png (drawn under the cut sheet, kept on reruns).
    Wing = green / yellow-green (g >= 0.9 r and g > b + 40); the blonde hair (g < r) stays erased."""
    lv1 = np.asarray(Image.open(f"{BODY}/bare/idle.png").convert("RGBA"))[:FRAME].astype(int)
    hl = np.asarray(Image.open(f"{BODY}/headless/idle.png").convert("RGBA"))[:FRAME].astype(int)
    ob = np.asarray(Image.open(f"{BODY}/set-t{t}/bare/idle.png").convert("RGBA")).astype(int)
    erased = (lv1[..., 3] > 8) & (hl[..., 3] <= 8)
    r, g, b = ob[:FRAME, ..., 0], ob[:FRAME, ..., 1], ob[:FRAME, ..., 2]
    wing = (g >= 0.9 * r) & (g > b + 40) & (ob[:FRAME, ..., 3] > 8)
    out = np.zeros_like(ob)
    out[:FRAME][erased & wing] = ob[:FRAME][erased & wing]
    os.makedirs(f"{ART}/set-t{t}/headless-patch", exist_ok=True)
    Image.fromarray(out.astype(np.uint8)).save(f"{ART}/set-t{t}/headless-patch/idle.png")
    print("patch", int((erased & wing).sum()), "px; now run build-head-layer.py cleric outfit-headless", f"set-t{t}")


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
            arr[~wo.fist_mask(c, fist[:, i * FRAME:(i + 1) * FRAME])] = 0
            hand.paste(Image.fromarray(arr), (i * FRAME, 0))
        wo.folder_meta(out)
        for sub, im in (("bare", bare), ("hand", hand)):
            os.makedirs(f"{out}/{sub}", exist_ok=True)
            wo.meta_copy(f"{BODY}/{sub}/idle.png", f"{out}/{sub}/idle.png")  # .meta first: Unity may be open on this project
            im.save(f"{out}/{sub}/idle.png")
            wo.folder_meta(f"{out}/{sub}")
        print("written", out)


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
    elif cmd == "patch":
        headless_patch(int(sys.argv[2]))
    elif cmd == "metas":
        for t in range(1, 10):
            hl = f"{BODY}/set-t{t}/headless"
            if os.path.exists(f"{hl}/idle.png"):
                wo.meta_copy(f"{BODY}/bare/idle.png", f"{hl}/idle.png")
                wo.folder_meta(hl)
    else:
        print(__doc__)

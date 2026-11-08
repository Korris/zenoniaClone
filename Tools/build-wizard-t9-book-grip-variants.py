"""Wizard T9 spellbook grip, two variants for the designer to compare (2026-11-08, idle facing down only, other tiers untouched).
Question + ChatGPT's ranking: Assets/ArtLibrary/classes/wizard/outfits/book-grip-question/answer-chatgpt.txt.

  v1 open palm: ChatGPT repainted the in-game T9 frame 0 with only the left arm changed (forearm forward, upturned cupped palm,
     variant1/wizard-t9-palm-v1-chatgpt-1.png). The redraw is fitted onto frame 0 (scale + shift found by matching the unchanged
     body), only the pixels that changed inside the left-arm box are pasted (bare + headless), frames 1-3 get the same patch squashed
     like the row's breathing (BREATH rows shorter, soles planted). Hand layer = right fist as before + the left thumb tip only.
     The book's grip is its lower inner edge, placed on the palm; bobY follows the palm per frame.
  v2 floating book: the T9 body/hand stay as they are; the book art gets a baked ice-blue/white glow (under the book, around it,
     above the pages; saturated blue so it shows on snow) and floats above the left fist (GAP px), bobY bobs it up 0/1/2/1 px.

Each variant is staged in book-grip-question/variant1|variant2/ (body sheets, book-void.png, entry.json) and installed with `install`.
The book anchor (held-wizard-melee.json, shared by every tier) is not changed: the T9 entry's grip + bobY (OverlayEntry.bobY) do it.
Rerunning build-wizard-tier-staff-book.py `write book` or build-wizard-outfit-idle-down.py `write 9` undoes the install: install again.

  python Tools/build-wizard-t9-book-grip-variants.py stage1 | stage2       build the staged files
  python Tools/build-wizard-t9-book-grip-variants.py check OUT.png         4 frames of both variants as the game draws them (6x crops)
  python Tools/build-wizard-t9-book-grip-variants.py install 1|2|0         copy a variant (0 = original T9) into Resources
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/wizard/body"
LIVE_SET = f"{BODY}/set-t9"
LIVE_BOOK = f"{RES}/Sprites/classes/wizard/weapons/book"
GEAR = f"{RES}/Data/classes/wizard/held-wizard.json"
HEAD0 = f"{RES}/Sprites/classes/wizard/heads/0/down.png"
Q = "Assets/ArtLibrary/classes/wizard/outfits/book-grip-question"
ORIG = f"{Q}/original-t9"
FRAME, SOLE, TOP = 192, 189, 37            # T9 idle-down figure: hair top row 37, one past the soles 189
BREATH = (0, 1, 2, 1)                      # as build-wizard-outfit-idle-down.py
ANCHOR, S = (120.0, 132.0), 0.25           # book anchor (shared by every tier) and its scale
ARM_BOX = (106, 98, 156, 142)              # left arm area of frame 0 (x0, y0, x1, y1) where the redraw may change pixels
REF_OFF, REF_K = (7, 21), 4                # variant1/t9-idle-down-frame0-4x.png = frame crop from (7, 21), 4x


def cell(path, f):
    return Image.open(path).convert("RGBA").crop((f * FRAME, 0, f * FRAME + FRAME, FRAME))


def key_green(im):
    a = np.asarray(im.convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum(np.maximum(r, b), 255 - g) - 40) / 70, 0, 1)
    a[..., 1] = np.where(alpha < 0.98, np.minimum(g, np.maximum(r, b) + 20), g)
    a[..., 3] = alpha * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def squash(im, less):
    """The row's breathing: figure rows TOP..SOLE resampled `less` rows shorter, soles planted."""
    if not less:
        return im.copy()
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    part = im.crop((0, TOP, FRAME, SOLE)).resize((FRAME, SOLE - TOP - less), Image.LANCZOS)
    out.paste(part, (0, TOP + less))
    return out


def fitted_redraw():
    """ChatGPT redraw keyed and brought to frame px, aligned on frame 0 outside the arm box (best of small scale/shift)."""
    src = key_green(Image.open(f"{Q}/variant1/wizard-t9-palm-v1-chatgpt-1.png"))
    ref = Image.open(f"{Q}/variant1/t9-idle-down-frame0-4x.png")
    old = cell(f"{LIVE_ORIG('headless')}", 0)
    old.alpha_composite(Image.open(HEAD0).convert("RGBA"))
    o = np.asarray(old).astype(np.float32)
    keep = np.ones((FRAME, FRAME), bool)
    x0, y0, x1, y1 = ARM_BOX
    keep[y0:y1, x0:x1] = False
    best = None
    for k in np.linspace(0.985, 1.015, 7):
        w = src.width * ref.width / src.width / REF_K * k
        small = src.resize((round(w), round(src.height * w / src.width)), Image.LANCZOS)
        for dx in range(-3, 4):
            for dy in range(-3, 4):
                c = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
                c.paste(small, (REF_OFF[0] + dx, REF_OFF[1] + dy), small)
                n = np.asarray(c).astype(np.float32)
                err = np.abs(n - o)[keep].mean()
                if best is None or err < best[0]:
                    best = (err, k, dx, dy, c)
    print("fit err %.2f scale %.3f shift %d,%d" % best[:4])
    return best[4], old


def LIVE_ORIG(kind):
    return f"{ORIG}/{kind}-idle.png"


def stage1():
    new, old = fitted_redraw()
    n = np.asarray(new).astype(int).copy()
    n[..., 1] = np.minimum(n[..., 1], np.maximum(n[..., 0], n[..., 2]) + 10)  # T9 has no green: the key leaves green-tinted outlines
    new = Image.fromarray(n.astype(np.uint8))
    o = np.asarray(old).astype(int)
    x0, y0, x1, y1 = ARM_BOX
    diff = (np.abs(n - o)[..., :3].max(-1) > 40) | (np.abs(n[..., 3] - o[..., 3]) > 60)
    box = np.zeros_like(diff)
    box[y0:y1, x0:x1] = True
    mask = ndimage.binary_dilation(ndimage.binary_opening(diff & box), iterations=1) & box
    out = f"{Q}/variant1"
    hand_old = Image.open(LIVE_ORIG("hand")).convert("RGBA")
    sheets = {k: Image.open(LIVE_ORIG(k)).convert("RGBA") for k in ("bare", "headless")}
    hand = hand_old.copy()
    hand.paste(Image.new("RGBA", (FRAME * 4, FRAME), (0, 0, 0, 0)), (0, 0))  # row 0 rebuilt below
    thumb = thumb_mask(n)
    for f, less in enumerate(BREATH):
        m = np.asarray(squash(Image.fromarray((mask * 255).astype(np.uint8)).convert("RGBA"), less))[..., 0] > 127
        nf = np.asarray(squash(new, less))
        for k, sh in sheets.items():
            fr = np.asarray(sh.crop((f * FRAME, 0, f * FRAME + FRAME, FRAME))).copy()
            fr[m] = nf[m]
            sh.paste(Image.fromarray(fr), (f * FRAME, 0))
        hr = np.asarray(hand_old.crop((f * FRAME, 0, f * FRAME + FRAME, FRAME))).copy()
        hr[:, 100:] = 0                                            # old left fist out, right fist kept
        tm = np.asarray(squash(Image.fromarray((thumb * 255).astype(np.uint8)).convert("RGBA"), less))[..., 0] > 127
        hr[tm] = nf[tm]
        hand.paste(Image.fromarray(hr), (f * FRAME, 0))
    for k, sh in sheets.items():
        sh.save(f"{out}/{k}-idle.png")
    hand.save(f"{out}/hand-idle.png")
    # book: grip at the lower inner edge (INNER of the width from the left, LIFT art px above the bottom), sitting on the palm top
    book = Image.open(f"{ORIG}/book-void.png").convert("RGBA")
    shutil.copy2(f"{ORIG}/book-void.png", f"{out}/book-void.png")
    a = np.asarray(book)[..., 3] > 100
    ys, xs = np.where(a)
    px, py = PALM
    cx = int(round(xs.min() + INNER * (xs.max() - xs.min())))
    bottom = np.where(a[:, cx])[0].max()           # the rim at that column (the corners sit higher than the front's lowest point)
    gx = cx - (px - ANCHOR[0]) / S
    gy = bottom - LIFT - (py - ANCHOR[1]) / S
    bob = [round((SOLE - py) * less / (SOLE - TOP), 2) for less in BREATH]  # the palm sinks with the breathing squash
    entry(out, book.size, gx, gy, bob)
    print("variant1 staged; changed px in frame 0:", int(mask.sum()), "thumb px:", int(thumb.sum()), "bob", bob)


PALM = (132, 121)    # v1: frame-0 point on the palm top where the book's lower inner edge rests (read off the 8x crop)
INNER, LIFT = 0.15, 0
THUMB_BOX = (123, 112, 131, 121)  # v1: thumb tip pixels drawn over the book (x0, y0, x1, y1), set after looking at the palm


def thumb_mask(n):
    m = np.zeros((FRAME, FRAME), bool)
    x0, y0, x1, y1 = THUMB_BOX
    m[y0:y1, x0:x1] = n[y0:y1, x0:x1, 3] > 128
    return m


def entry(out, size, gx, gy, bob):
    g = {"w": size[0], "h": size[1], "gripX": int(round(gx)), "gripY": int(round(gy))}
    e = {"name": "void", "glow": "#59c8ff", "baseGrip": g, "enhancedGrip": dict(g), "bobY": bob}
    json.dump(e, open(f"{out}/entry.json", "w"), indent=1)


GAP = 4                         # v2: px (frame) between the fist top and the book bottom
FLOAT_DX = 10                   # v2: book's inner edge this many px right of the fist's left edge
GLOW_PAD = 28                   # art px of glow around the book


def glow_book():
    book = Image.open(f"{ORIG}/book-void.png").convert("RGBA")
    W, H = book.width + 2 * GLOW_PAD, book.height + 2 * GLOW_PAD
    mask = Image.new("L", (W, H), 0)
    mask.paste(book.getchannel("A"), (GLOW_PAD, GLOW_PAD))
    layers = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def blob(alpha_l, rgb, peak):
        a = np.asarray(alpha_l).astype(np.float32) / 255 * peak
        layer = Image.fromarray(np.dstack([np.full_like(a, c) for c in rgb] + [a * 255]).astype(np.uint8))
        layers.alpha_composite(layer)

    aura = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(9))
    blob(aura, (40, 160, 255), 0.65)                       # soft rim around the whole book
    a = np.asarray(book)[..., 3] > 100
    ys, xs = np.where(a)
    under = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(under)
    cx, by = GLOW_PAD + (xs.min() + xs.max()) / 2, GLOW_PAD + ys.max()
    d.ellipse((cx - 0.38 * book.width, by - 10, cx + 0.38 * book.width, by + 22), fill=255)
    blob(under.filter(ImageFilter.GaussianBlur(8)), (50, 180, 255), 0.9)   # glow pool under the book
    pages = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(pages)
    ty = GLOW_PAD + ys.min()
    d.ellipse((cx - 0.30 * book.width, ty - 22, cx + 0.30 * book.width, ty + 14), fill=255)
    blob(pages.filter(ImageFilter.GaussianBlur(7)), (120, 215, 255), 0.75)    # light rising off the pages
    for sx, sy, r, rgb in ((0.12, 0.92, 7, (255, 225, 120)), (0.86, 0.95, 6, (220, 248, 255)), (0.5, 0.06, 6, (255, 255, 255)),
                           (0.30, 0.02, 5, (255, 225, 120)), (0.95, 0.30, 5, (220, 248, 255))):
        sp = Image.new("L", (W, H), 0)
        d = ImageDraw.Draw(sp)
        x, y = sx * W, sy * H
        d.polygon([(x, y - r), (x + r / 3, y - r / 3), (x + r, y), (x + r / 3, y + r / 3), (x, y + r), (x - r / 3, y + r / 3),
                   (x - r, y), (x - r / 3, y - r / 3)], fill=255)
        blob(sp, rgb, 1.0)
    layers.alpha_composite(book, (GLOW_PAD, GLOW_PAD))
    return layers, (xs.min() + GLOW_PAD, ys.max() + GLOW_PAD)


def stage2():
    out = f"{Q}/variant2"
    os.makedirs(out, exist_ok=True)
    for k in ("bare", "hand", "headless"):
        shutil.copy2(LIVE_ORIG(k), f"{out}/{k}-idle.png")
    art, (bl, bb) = glow_book()
    art.save(f"{out}/book-void.png")
    fist = np.asarray(cell(LIVE_ORIG("hand"), 0))[..., 3] > 0
    ys, xs = np.where(fist[:, 100:])
    fx0, fy0 = xs.min() + 100, ys.min()
    # book bottom-left (bl, bb) lands at (fist left + FLOAT_DX, fist top - GAP)
    gx = bl - (fx0 + FLOAT_DX - ANCHOR[0]) / S
    gy = bb - (fy0 - GAP - ANCHOR[1]) / S
    entry(out, art.size, gx, gy, [0, -1, -2, -1])
    print("variant2 staged; fist top", fy0, "book bottom at", fy0 - GAP)


def compose(v, f, pad=40):
    d = f"{Q}/variant{v}" if v else ORIG
    e = json.load(open(f"{d}/entry.json" if v else f"{ORIG}/book-void-entry.json"))
    c = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (58, 62, 70, 255))
    c.alpha_composite(cell(f"{d}/headless-idle.png", f), (pad, pad))
    head = Image.open(HEAD0).convert("RGBA")
    if BREATH[f]:  # head layer: sy squash about the cell bottom (head-anchors.json), roughly
        head = squash(head, BREATH[f])
    c.alpha_composite(head, (pad, pad))
    book = Image.open(f"{d}/book-void.png").convert("RGBA")
    g = e["baseGrip"]
    bob = (e.get("bobY") or [0] * 4)[f]
    small = book.resize((round(book.width * S), round(book.height * S)), Image.LANCZOS)
    x = ANCHOR[0] - g["gripX"] * S
    y = ANCHOR[1] + bob - g["gripY"] * S
    c.alpha_composite(small, (round(x) + pad, round(y) + pad))
    c.alpha_composite(cell(f"{d}/hand-idle.png", f), (pad, pad))
    return c


def check(path):
    rows = []
    for v in (0, 1, 2):
        frames = [compose(v, f).crop((40 + 70, 40 + 50, 40 + 200, 40 + 150)) for f in range(4)]
        row = Image.new("RGBA", (134 * 4, 100), (40, 42, 48, 255))
        for i, fr in enumerate(frames):
            row.paste(fr, (i * 134, 0))
        rows.append(row.resize((row.width * 3, row.height * 3), Image.NEAREST))
    sheet = Image.new("RGBA", (rows[0].width, sum(r.height for r in rows) + 20), (20, 20, 20, 255))
    for i, r in enumerate(rows):
        sheet.paste(r, (0, i * (r.height + 10)))
    sheet.save(path)
    print(path, "(rows: original, v1 palm, v2 floating)")


def install(v):
    d = f"{Q}/variant{v}" if v else ORIG
    for k in ("bare", "hand", "headless"):
        shutil.copy2(f"{d}/{k}-idle.png", f"{LIVE_SET}/{k}/idle.png")
    for kind in ("base", "enhanced"):
        shutil.copy2(f"{d}/book-void.png", f"{LIVE_BOOK}/void-{kind}.png")
    e = json.load(open(f"{d}/entry.json" if v else f"{ORIG}/book-void-entry.json"))
    data = json.load(open(GEAR, encoding="utf-8"))
    assert data["book"][9]["name"] == "void"
    data["book"][9] = e
    open(GEAR, "w", encoding="utf-8").write(json.dumps(data, separators=(",", ":")))
    print("installed T9 variant", v or "original")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "stage1":
        stage1()
    elif cmd == "stage2":
        stage2()
    elif cmd == "check":
        check(sys.argv[2])
    elif cmd == "install":
        install(int(sys.argv[2]))
    else:
        print(__doc__)

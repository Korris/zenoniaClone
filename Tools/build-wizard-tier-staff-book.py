"""Wizard staff + spellbook of the 10 armour tiers (2026-11-07, design docs/reference/wizard-sets-10-tiers-ladder-approved.png, T0 from -v3.png).
ChatGPT draws 5 staffs (upright) or 5 books per image, left to right = tiers, on a flat key: T0-T4 on #FF00FF (T1's green gem), T5-T9 on
#00FF00 (T8's pink). Sources: Assets/ArtLibrary/classes/wizard/outfits/gear-src/{staffs,books}-t0-t4.png, -t5-t9.png.

Staff: cut like Tools/build-wizard-new-style.py, drawn at S 0.447 everywhere, but the art of each tier is resampled to the length the design
sheet gives it relative to the hero (STAFF_FIT, 2026-11-08) and the grip row is the one that puts the staff top where the sheet has it; written over the manifest names (apprentice .. void, the enhanced file = the base art: FeelConfig.EnhanceUsesBaseArt),
glow per the new palette, weapon-wizard-N icons. Old art: ArtLibrary/classes/wizard/new-style/old/staff-pre-tier-sets/.
Book (offhand, new part "book", HeroClassDef.Wizard, optional): Sprites/classes/wizard/weapons/book/<name>-base|enhanced.png, manifest `book`
in held-wizard.json, anchor only on idle facing down (held-wizard-melee.json) at the bare off-hand fist, offhand-wizard-N icons. One width for
every tier (BOOK_W, designer 08/11); the fist grips the book by its lower inner rim and the hand layer row 0 of every tier is recut so the
fist is drawn over the book, bobY per tier follows the fist (Tools/wizard_book_fist_grip.py; `write book` rewrites those hand layers).

  python Tools/build-wizard-tier-staff-book.py check OUT_DIR        cut both, idle-down composites per tier (outfit body if written, head 0),
                                                                    + 4 breathing frames of the left hand per tier, 4x (fist recut as `write`)
  python Tools/build-wizard-tier-staff-book.py write staff|book     install
"""
import importlib.util, json, os, shutil, sys, uuid, re
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = str(Path(__file__).resolve().parents[1])
os.chdir(ROOT)
sys.path.insert(0, ROOT + "/Tools")
RES = "Assets/Resources"
SRC = "Assets/ArtLibrary/classes/wizard/outfits/gear-src"
STAFF_LIVE = f"{RES}/Sprites/classes/wizard/weapons/staff"
BOOK_LIVE = f"{RES}/Sprites/classes/wizard/weapons/book"
GEAR = f"{RES}/Data/classes/wizard/held-wizard.json"
MELEE = f"{RES}/Data/classes/wizard/held-wizard-melee.json"
ICONS = f"{RES}/Sprites/icons"
OLD = "Assets/ArtLibrary/classes/wizard/new-style/old/staff-pre-tier-sets"
FRAME = 192
GLOW = ["#4f9dff", "#7bd04a", "#ffb53d", "#ff7a2e", "#a35cff", "#ffc93d", "#3fe3e0", "#ff3b3b", "#ff5ad8", "#59c8ff"]
# Per-tier size and place, measured on docs/reference/wizard-sets-10-tiers-ladder-v3.png (2026-11-08, the designer: staff, book and robe
# relative to the hero exactly as the sheet; exception to "one weapon size", docs/sprite-rules.md). Units: hero height H (hair top
# to soles; in game the idle-down frame 0 is 151 px from row 37 to row 188, head 0 centred on column 98).
H_GAME, HAIR_TOP, HEAD_CX = 151.0, 37.0, 98.0
# staff: drawn in the bare right fist at STAFF_ANCHOR, top leaning left STAFF_ANGLE degrees (the sheet's staffs stand nearly upright);
# (top, bottom) of the staff in H below the hair top: the length grows from 0.94 H (T0) to 1.75 H (T9), the art is resampled to it at S
STAFF_ANCHOR, STAFF_ANGLE = (60.0, 114.0), -5.0
STAFF_FIT = [(-0.012, 0.930), (-0.113, 0.950), (-0.201, 0.996), (-0.261, 1.008), (-0.319, 1.008),
             (-0.377, 1.016), (-0.437, 1.008), (-0.452, 1.023), (-0.566, 1.043), (-0.680, 1.066)]
# book (2026-11-08, designer: "same size for every tier, the hand grips the book"): every tier's book BOOK_W wide (T0's size on the sheet,
# a size a fist can hold), anchored at BOOK_FIST; per tier the grip puts the book's lower rim, at its inner (body) side, across the closed
# left fist, and the hand layer redraws that fist over it (Tools/wizard_book_fist_grip.py); bobY makes the book follow the fist per frame.
BOOK_FIST, BOOK_S, BOOK_W, BOOK_ANGLE = (120.0, 132.0), 0.25, 0.25, 8.0   # angle: top leaning out (clockwise), about the grip


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


wns = load("wns", "Tools/build-wizard-new-style.py")
hs = wns.load_hs()
chf = load("chf", "Tools/compose-hero-frame.py")
import wizard_book_fist_grip as fist


def key_out(path, key):
    """Straight RGBA floats 0..255 by distance from the pure key colour: hs.key_out's excess test ate the violet crystal of T4 on magenta."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    kc = np.array([0, 255, 0] if key == "green" else [255, 0, 255], np.float32)
    d = np.maximum(np.maximum(r, b), 255 - g) if key == "green" else np.maximum(np.maximum(255 - r, g), 255 - b)
    alpha = np.clip((d - 40) / 70, 0, 1)
    a = np.maximum(alpha[..., None], 0.02)
    out = np.clip((rgb - (1 - a) * kc) / a, 0, 255)  # unmix the key from soft edge pixels
    return np.dstack([out, alpha * 255]).astype(np.float32)


def split(a, count):
    """The count biggest pieces, left to right, each trimmed (as build-wizard-new-style.staff_figures)."""
    lab, n = ndimage.label(a[..., 3] > 8, structure=np.ones((3, 3)))
    sizes = ndimage.sum_labels(np.ones_like(lab), lab, index=np.arange(1, n + 1))
    big = np.argsort(sizes)[::-1][:count]
    objs = ndimage.find_objects(lab)
    return [a[objs[i]] * (lab[objs[i]] == i + 1)[..., None] for i in sorted(big, key=lambda i: objs[i][1].start)]


def figures(kind):
    """10 RGBA float crops (tier order) from the two images of a kind."""
    out = []
    for g, key in (("t0-t4", "magenta"), ("t5-t9", "green")):
        out += split(key_out(f"{SRC}/{kind}-{g}.png", key), 5)
    return out


def staff_fit(fig, t):
    """Staff art of tier t resampled so that, drawn at S in the fist at STAFF_ANGLE, it spans STAFF_FIT[t]; grip on the shaft axis."""
    top, bot = STAFF_FIT[t]
    cos = np.cos(np.radians(STAFF_ANGLE))
    rows = (bot - top) * H_GAME / cos / wns.S
    k = rows / fig.shape[0]
    small = hs.resize(fig, (max(1, round(fig.shape[1] * k)), round(rows)))
    pad = wns.STAFF_PAD
    canvas = np.zeros((small.shape[0] + 2 * pad, small.shape[1] + 2 * pad, 4), np.float32)
    canvas[pad:pad + small.shape[0], pad:pad + small.shape[1]] = small
    gy = int(round(pad + (STAFF_ANCHOR[1] - (HAIR_TOP + top * H_GAME)) / cos / wns.S))
    solid = canvas[..., 3] > 128
    h = canvas.shape[0]
    mids = [(np.flatnonzero(solid[y]).min() + np.flatnonzero(solid[y]).max() + 1) / 2 for y in range(h * 2 // 3, h * 9 // 10) if solid[y].any()]
    gx = int(round(np.median(mids)))  # shaft axis from the lower shaft (the grip row may cross a ring or an ornament)
    return canvas, {"w": canvas.shape[1], "h": h, "gripX": gx, "gripY": gy}, 0, 0


def staffs():
    return [staff_fit(f, t) for t, f in enumerate(figures("staffs"))]


def books():
    """(art, grip, bobY) per tier: same width for all, grip on the lower inner rim at the tier's left fist."""
    res = []
    body = f"{RES}/Sprites/classes/wizard/body"
    for t, f in enumerate(figures("books")):
        wpx = BOOK_W * H_GAME / BOOK_S
        small = hs.resize(f, (round(wpx), max(1, round(f.shape[0] * wpx / f.shape[1]))))
        pad = 3
        canvas = np.zeros((small.shape[0] + 2 * pad, small.shape[1] + 2 * pad, 4), np.float32)
        canvas[pad:pad + small.shape[0], pad:pad + small.shape[1]] = small
        masks = fist.fist_masks(body, t)
        gx, gy = fist.book_grip(canvas, BOOK_FIST, BOOK_S, masks[0])
        grip = {"w": canvas.shape[1], "h": canvas.shape[0], "gripX": int(round(gx)), "gripY": int(round(gy))}
        res.append((canvas, grip, fist.bob_y(masks)))
    return res


def names():
    return [e["name"] for e in json.load(open(GEAR, encoding="utf-8"))["weapon"]]


def compose_tier(t, staff, book, pad=40, f=0):
    """Idle down frame f of tier t as the game draws it: outfit headless body (Lv1 if none) + head 0, staff, book, fists (left fist
    recut as `write book` does). Head breathing is not applied (only the hand area is looked at for f > 0)."""
    body = f"{RES}/Sprites/classes/wizard/body"
    sub = f"{body}/set-t{t}" if t and os.path.exists(f"{body}/set-t{t}/bare/idle.png") else body
    hl = f"{sub}/headless/idle.png" if os.path.exists(f"{sub}/headless/idle.png") else f"{body}/headless/idle.png"
    c = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
    c.alpha_composite(chf.cell(hl, 0, f), (pad, pad))
    c.alpha_composite(Image.open(f"{RES}/Sprites/classes/wizard/heads/0/down.png").convert("RGBA"), (pad, pad))
    a_staff = {"x": STAFF_ANCHOR[0], "y": STAFF_ANCHOR[1], "angle": STAFF_ANGLE, "s": wns.S, "sx": 1}
    chf.place(c, hs.to_image(staff[0]), staff[1], a_staff, 1.0, 0.0, pad)
    a_book = {"x": BOOK_FIST[0], "y": BOOK_FIST[1] + book[2][f], "angle": BOOK_ANGLE, "s": BOOK_S, "sx": 1}
    chf.place(c, hs.to_image(book[0]), book[1], a_book, 1.0, 0.0, pad)
    hand = np.asarray(chf.cell(f"{sub}/hand/idle.png", 0, f)).copy()
    hand[:, fist.SPLIT_X:] = 0
    m = fist.fist_masks(body, t)[f]
    hand[m] = np.asarray(chf.cell(f"{sub}/bare/idle.png", 0, f))[m]
    c.alpha_composite(Image.fromarray(hand), (pad, pad))
    return c


def check(out):
    os.makedirs(out, exist_ok=True)
    st, bk = staffs(), books()
    for t in range(10):
        print(t, "staff", st[t][1], "run", st[t][2], "book", bk[t][1], "bobY", bk[t][2])
    size = FRAME + 80
    sheet = Image.new("RGBA", (size * 10, size), (58, 62, 70, 255))
    for t in range(10):
        sheet.alpha_composite(compose_tier(t, st[t], bk[t]), (t * size, 0))
    sheet.save(f"{out}/wizard-tiers-idle-down.png")
    game = sheet.resize((sheet.width * 85 // 160, sheet.height * 85 // 160), Image.LANCZOS)  # ~85 px hero on screen
    game.save(f"{out}/wizard-tiers-idle-down-gamesize.png")
    box, k = (40 + 95, 40 + 90, 40 + 175, 40 + 160), 4   # left hand + book of the frame
    bw, bh = box[2] - box[0], box[3] - box[1]
    hands = Image.new("RGBA", (bw * k * 4, bh * k * 10), (58, 62, 70, 255))
    for t in range(10):
        for f in range(4):
            fr = Image.new("RGBA", (FRAME + 80, FRAME + 80), (58, 62, 70, 255))
            fr.alpha_composite(compose_tier(t, st[t], bk[t], f=f))
            hands.paste(fr.crop(box).resize((bw * k, bh * k), Image.NEAREST), (f * bw * k, t * bh * k))
    hands.save(f"{out}/wizard-tiers-book-hand-4-frames.png")
    print(out)


def meta_like(src, dst):
    if os.path.exists(dst + ".meta") or not os.path.exists(src + ".meta"):
        return
    raw = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, open(src + ".meta", encoding="utf-8").read(), count=1)
    open(dst + ".meta", "w", encoding="utf-8").write(raw)


def icon(rgba, path, rotate):
    im = hs.to_image(rgba)
    if rotate:
        im = im.rotate(-45, expand=True, resample=Image.BICUBIC)
    im = im.crop(im.getbbox())
    k = 88 / max(im.size)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    out.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    out.save(path)


def save_gear(data):
    open(GEAR, "w", encoding="utf-8").write(json.dumps(data, separators=(",", ":")))


def write_staff():
    os.makedirs(f"{OLD}/icons", exist_ok=True)
    data = json.load(open(GEAR, encoding="utf-8"))
    for t, (rgba, grip, _, _) in enumerate(staffs()):
        n = data["weapon"][t]["name"]
        for kind in ("base", "enhanced"):
            live = f"{STAFF_LIVE}/{n}-{kind}.png"
            if not os.path.exists(f"{OLD}/{n}-{kind}.png"):
                shutil.copy2(live, f"{OLD}/{n}-{kind}.png")
            hs.to_image(rgba).save(live)
        data["weapon"][t].update(glow=GLOW[t], baseGrip=grip, enhancedGrip=dict(grip))
        ic = f"{ICONS}/weapon-wizard-{t}.png"
        if not os.path.exists(f"{OLD}/icons/weapon-wizard-{t}.png"):
            shutil.copy2(ic, f"{OLD}/icons/weapon-wizard-{t}.png")
        icon(rgba, ic, True)
    save_gear(data)
    m = json.load(open(MELEE, encoding="utf-8"))
    for e in m["anchors"]:
        if e["part"] == "staff" and e["anim"] == "idle" and e["dir"] == "down":
            for fr in e["frames"]:
                fr.update(x=STAFF_ANCHOR[0], y=STAFF_ANCHOR[1], angle=STAFF_ANGLE, s=wns.S)
    open(MELEE, "w", encoding="utf-8").write(json.dumps(m, indent=1))
    print("staffs written (idle down staff angle", STAFF_ANGLE, ")")


def write_book():
    os.makedirs(BOOK_LIVE, exist_ok=True)
    os.makedirs(f"{OLD}/icons", exist_ok=True)
    data = json.load(open(GEAR, encoding="utf-8"))
    nm = names()
    entries = []
    for t, (rgba, grip, bob) in enumerate(books()):
        for kind in ("base", "enhanced"):
            p = f"{BOOK_LIVE}/{nm[t]}-{kind}.png"
            hs.to_image(rgba).save(p)
            meta_like(f"{STAFF_LIVE}/{nm[t]}-{kind}.png", p)
        e = {"name": nm[t], "glow": GLOW[t], "baseGrip": grip, "enhancedGrip": dict(grip)}
        if any(bob):
            e["bobY"] = bob
        entries.append(e)
        ic = f"{ICONS}/offhand-wizard-{t}.png"
        if not os.path.exists(f"{OLD}/icons/offhand-wizard-{t}.png"):
            shutil.copy2(ic, f"{OLD}/icons/offhand-wizard-{t}.png")
        icon(rgba, ic, False)
    data["book"] = entries
    save_gear(data)
    fist.write_hand_layers(f"{RES}/Sprites/classes/wizard/body")  # left fist of every tier recut, drawn over the book
    if not os.path.exists(BOOK_LIVE + ".meta"):
        open(BOOK_LIVE + ".meta", "w", encoding="utf-8").write(
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    m = json.load(open(MELEE, encoding="utf-8"))
    m["anchors"] = [e for e in m["anchors"] if e["part"] != "book"]
    fr = {"x": BOOK_FIST[0], "y": BOOK_FIST[1], "angle": BOOK_ANGLE, "s": BOOK_S, "sx": 1, "behind": False, "hidden": False, "fallback": False}
    m["anchors"].append({"part": "book", "anim": "idle", "dir": "down", "frames": [dict(fr) for _ in range(4)]})
    open(MELEE, "w", encoding="utf-8").write(json.dumps(m, indent=1))
    print("books written")


if __name__ == "__main__":
    if sys.argv[1] == "check":
        check(sys.argv[2])
    elif sys.argv[1] == "write":
        {"staff": write_staff, "book": write_book}[sys.argv[2]]()
    else:
        print(__doc__)

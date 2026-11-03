"""Warrior weapons cut from ChatGPT sheets of four upright weapons on flat #00FF00: A normal, A enhanced, B normal, B enhanced
(chat "Generate Weapon Sheet" https://chatgpt.com/c/6ae8df35-51c8-83ec-aeb3-b3d02ac96f6c, account Tay, 2026-11-03; designs from the user's reference
Assets/ArtLibrary/classes/warrior/sword-src/ref-mu-weapons-4dir.png and ref-mu-weapons-my-hero.png).
Each weapon keeps its own silhouette; all are scaled to ART_H px tall, so on screen (x 0.34 x 2) they are about 0.83 of the hero, as in the reference.
gripX, gripY = centre of the handle: the narrowest run of rows in the lower part of the weapon, above the pommel; the fist closes there.
Python 3 + Pillow + numpy + scipy.

  python Tools/build-warrior-weapons-from-sheet.py check OUT_DIR    cut, measure, composite on the hero (idle down, idle right, melee1 right); Resources untouched
  python Tools/build-warrior-weapons-from-sheet.py write            sprites + grips in held-gear.json, old sprites kept once in sword-src/old-mu-<date>/, then icons
"""
import importlib.util, json, os, shutil, subprocess, sys, datetime
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PART = os.environ.get("PART", "sword")  # sword (default) or shield: the shield set of the same reference, PART=shield
SRC = os.path.join(ROOT, "Assets/ArtLibrary/classes/warrior", "shield-src" if PART == "shield" else "sword-src", os.environ.get("SHEET_SET", "mu-ref-v2"))  # SHEET_SET=mu-ref for the first, shorter sword set
RES = os.path.join(ROOT, "Assets/Resources")
SWORDS = os.path.join(RES, "Sprites/classes/warrior/weapons", PART)
GEAR = os.path.join(RES, "Data/classes/warrior/held-gear.json")
# Art px tall. The size on screen is art px x s x DrawScale, so the art height IS the weapon size: sword 250 = 170 frame px = 21 units (about the hero),
# shield 330 = 71 frame px = 9 units (380 hid the face in idle down). In the game (4 screen px per unit) that is a 0.34x / 0.11x downscale through
# the GPU mipmaps, which looks soft; the art gets an unsharp mask to compensate (tried cutting at 2x the screen size on 2026-11-03: it just shrank the gear).
ART_H = int(os.environ.get("ART_H", 330 if PART == "shield" else 250))
ICON_PX = 104  # icons draw at 50-52 px in the bag: 104 is their mip level 1

# sheet file: the two tiers it holds, in column order (normal, enhanced, normal, enhanced)
SHEETS = {
    "pair1-lightning-lance.png": ["iron", "bronze"],
    "pair1-lightning-spear.png": ["iron", "bronze"],
    "shield-pair1-lightning-lance.png": ["iron", "bronze"],
    "shield-pair2-crimson-balrog.png": ["crimson", "flame"],
    "shield-pair3-frost-thunder.png": ["frost", "thunder"],
    "shield-pair4-chaos-reaper.png": ["verdant", "shadow"],
    "shield-pair5-divine-dragon.png": ["holy", "dragon"],
    "pair2-crimson-balrog.png": ["crimson", "flame"],
    "pair3-frost-thunder.png": ["frost", "thunder"],
    "pair4-chaos-reaper.png": ["verdant", "shadow"],
    "pair5-divine-dragon.png": ["holy", "dragon"],
}


def key_green(rgb):
    r, g, b = [rgb[..., i].astype(np.float64) for i in range(3)]
    excess = g - np.maximum(r, b)
    alpha = 1 - np.clip((excess - 12) / 60, 0, 1)
    g2 = np.minimum(g, np.maximum(r, b))  # no green fringe
    return np.dstack([r, g2, b, alpha * 255])


def four_weapons(path):
    """The four weapons of a sheet, left to right, each a straight RGBA crop. Small detached parts join the weapon whose columns they share."""
    rgba = key_green(np.array(Image.open(path).convert("RGB")))
    solid = rgba[..., 3] > 128
    lab, n = ndimage.label(solid)
    sizes = ndimage.sum(solid, lab, range(1, n + 1))
    objs = ndimage.find_objects(lab)
    mains = sorted([i for i in np.argsort(sizes)[::-1][:4]], key=lambda i: objs[i][1].start)
    if len(mains) < 4:
        raise SystemExit(f"{path}: found {len(mains)} weapons")
    groups = [lab == i + 1 for i in mains]
    for i in range(n):
        if i in mains or sizes[i] < 20:
            continue
        o = objs[i]
        cx = (o[1].start + o[1].stop) / 2
        for k, m in enumerate(mains):
            mo = objs[m]
            if mo[1].start - 12 <= cx <= mo[1].stop + 12:
                groups[k] |= lab == i + 1
                break
    out = []
    for gmask in groups:
        ys, xs = np.where(gmask)
        crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[..., 3] *= gmask[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        out.append(crop)
    return out


def straighten(rgba):
    """Rotate so the long axis is vertical (principal axis of the solid pixels), only for small tilts."""
    solid = rgba[..., 3] > 128
    ys, xs = np.where(solid)
    cov = np.cov(np.vstack([xs - xs.mean(), ys - ys.mean()]))
    w, v = np.linalg.eigh(cov)
    ax = v[:, np.argmax(w)]
    angle = np.degrees(np.arctan2(ax[0], ax[1]))  # 0 = vertical
    if angle > 90: angle -= 180
    if angle < -90: angle += 180
    if abs(angle) < 0.5 or abs(angle) > 12:
        return rgba, angle
    im = Image.fromarray(rgba.round().astype(np.uint8), "RGBA").rotate(angle, resample=Image.BICUBIC, expand=True)
    arr = np.array(im).astype(np.float64)
    ys, xs = np.where(arr[..., 3] > 128)
    return arr[ys.min():ys.max() + 1, xs.min():xs.max() + 1], angle


def shrink(im, height):
    """Lanczos to `height` px tall plus a light unsharp mask: a GPU mipmap chain would blur the small art."""
    k = height / im.height
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    return im.filter(ImageFilter.UnsharpMask(radius=1, percent=60, threshold=2))


def fit(rgba):
    return shrink(Image.fromarray(rgba.round().astype(np.uint8), "RGBA"), ART_H)


def icon_of(rgba):
    """Bag icon straight from the ChatGPT crop (not from the small game art): fitted in an ICON_PX square with a 4 px margin."""
    im = Image.fromarray(rgba.round().astype(np.uint8), "RGBA")
    if PART != "shield":
        im = im.rotate(-45, expand=True, resample=Image.BICUBIC)  # weapons lie diagonally in the bag, as the other classes' icons (build-item-icons.py)
    im = im.crop(im.getbbox())
    k = (ICON_PX - 8) / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1, percent=60, threshold=2))
    icon = Image.new("RGBA", (ICON_PX, ICON_PX), (0, 0, 0, 0))
    icon.paste(im, ((ICON_PX - im.width) // 2, (ICON_PX - im.height) // 2), im)
    return icon


def grip_of(im):
    """Shield: its centre, as the old shield art. Sword: centre of the handle: in the lower 45% of the art, the longest run of rows no wider than 1.5 x the thinnest row, skipping the bottom 6% (pommel)."""
    if PART == "shield":
        return {"w": im.width, "h": im.height, "gripX": im.width / 2, "gripY": im.height / 2}
    a = np.array(im)[..., 3] > 100
    h = a.shape[0]
    widths = np.array([(np.where(row)[0].max() - np.where(row)[0].min() + 1) if row.any() else 0 for row in a])
    lo, hi = int(h * 0.55), int(h * 0.94)
    seg = widths[lo:hi]
    thin = max(2, seg[seg > 0].min())
    ok = (seg > 0) & (seg <= thin * 1.5)
    best, cur, start, bstart = 0, 0, lo, lo
    for i, v in enumerate(ok):
        if v:
            cur += 1
            if cur > best: best, bstart = cur, lo + i - cur + 1
        else:
            cur = 0
    gy = bstart + best // 2
    row = np.where(a[gy])[0]
    gx = (row.min() + row.max()) / 2
    return {"w": im.width, "h": im.height, "gripX": round(float(gx), 1), "gripY": int(gy)}


def cut_all():
    """{tier: (base Image, enhanced Image, base grip, enhanced grip, tilt angles)} for the sheets that exist."""
    out = {}
    for sheet, tiers in SHEETS.items():
        path = os.path.join(SRC, sheet)
        if not os.path.exists(path):
            print(f"missing {sheet}: {tiers} keep their old art")
            continue
        parts = four_weapons(path)
        for k, tier in enumerate(tiers):
            base, a1 = straighten(parts[2 * k])
            enh, a2 = straighten(parts[2 * k + 1])
            bi, ei = fit(base), fit(enh)
            out[tier] = (bi, ei, grip_of(bi), grip_of(ei), (round(a1, 1), round(a2, 1)), icon_of(base))
    return out


def load_compose():
    spec = importlib.util.spec_from_file_location("compose_hero_frame", os.path.join(ROOT, "Tools/compose-hero-frame.py"))
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    return comp


def check(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    cut = cut_all()
    comp = load_compose()
    file, anchors = comp.load_gear("warrior")
    names = [e["name"] for e in file[PART]]
    font = ImageFont.truetype("arial.ttf", 20)
    pad = 50
    size = 192 + 2 * pad
    sheet = Image.new("RGB", (size * len(cut) + 10, size * 3 + 40), (58, 62, 70))
    d = ImageDraw.Draw(sheet)
    # composite with the cut art: write it to a scratch weapon folder that compose reads through a patched RES
    scratch = os.path.join(out_dir, "res")
    for tier, (bi, ei, gb, ge, tilt, _) in cut.items():
        folder = os.path.join(scratch, "Sprites/classes/warrior/weapons", PART)
        os.makedirs(folder, exist_ok=True)
        bi.save(os.path.join(folder, f"{tier}-base.png"))
        ei.save(os.path.join(folder, f"{tier}-enhanced.png"))
        print(f"{tier:8s} base {gb} enhanced {ge} tilt {tilt}")
    # compose wants everything under RES: point it at a copy that holds the real sheets + the cut swords
    for sub in ("Sprites/classes/warrior/body", "Sprites/classes/warrior/weapons/" + ("sword" if PART == "shield" else "shield"), "Data/classes/warrior"):
        dst = os.path.join(scratch, sub)
        if not os.path.exists(dst):
            shutil.copytree(os.path.join(RES, sub), dst)
    gear = json.load(open(os.path.join(scratch, "Data/classes/warrior/held-gear.json"), encoding="utf-8"))
    for e in gear[PART]:
        if e["name"] in cut:
            e["baseGrip"], e["enhancedGrip"] = cut[e["name"]][2], cut[e["name"]][3]
    json.dump(gear, open(os.path.join(scratch, "Data/classes/warrior/held-gear.json"), "w", encoding="utf-8"))
    comp.RES = scratch
    for k, tier in enumerate([n for n in names if n in cut]):
        t = names.index(tier)
        for r, (anim, dr, f) in enumerate((("idle", "down", 0), ("idle", "right", 0), ("melee1", "right", 3))):
            c = comp.compose("warrior", anim, dr, f, tier=t, pad=pad)
            sheet.paste(c, (k * size + 5, 40 + r * size), c)
        d.text((k * size + 10, 8), f"{t} {tier}", font=font, fill="yellow")
    sheet.save(os.path.join(out_dir, "weapons-on-hero.png"))
    print("preview", os.path.join(out_dir, "weapons-on-hero.png"))


def write():
    cut = cut_all()
    backup = os.path.join(os.path.dirname(SRC), f"old-mu-{PART}-{datetime.date.today():%Y%m%d}")
    os.makedirs(backup, exist_ok=True)
    gear_raw = open(GEAR, "rb").read()
    gear = json.loads(gear_raw)
    names = [e["name"] for e in gear[PART]]
    for tier, (bi, ei, gb, ge, _, icon) in cut.items():
        icon.save(os.path.join(RES, "Sprites/icons", f"{'offhand' if PART == 'shield' else 'weapon'}-warrior-{names.index(tier)}.png"))
        for kind, im in (("base", bi), ("enhanced", ei)):
            dst = os.path.join(SWORDS, f"{tier}-{kind}.png")
            keep = os.path.join(backup, f"{tier}-{kind}.png")
            if os.path.exists(dst) and not os.path.exists(keep):
                shutil.copy2(dst, keep)
            im.save(dst)
        for e in gear[PART]:
            if e["name"] == tier:
                e["baseGrip"], e["enhancedGrip"] = gb, ge
    text = json.dumps(gear, separators=(",", ":")) if b"\n" not in gear_raw.strip() else json.dumps(gear, indent=1)
    open(GEAR, "wb").write(text.encode("utf-8"))
    print("sprites, grips and icons written:", ", ".join(cut))


if __name__ == "__main__":
    if sys.argv[1] == "check":
        check(sys.argv[2])
    elif sys.argv[1] == "write":
        write()

"""Fighter glove tiers: cut one ChatGPT image per tier (base glove left, enhanced right, flat #00FF00 key) into 200x200 art, grips, icons.
Follows the dagger section of build-assassin-new-style.py. Rules in docs/sprite-rules.md.

Sources: Assets/ArtLibrary/classes/fighter/new-style/glove-src/<tier>-pair-chatgpt.png, knuckles pointing RIGHT in both copies
(the game mirrors the art for the other hand). The base glove is scaled to GLOVE_W px wide on a CANVAS px square; the enhanced copy
uses the same factor as its base and sits on the same fist centre, so switching to +N does not move the glove in the hand.
Grip = centroid of the solid (alpha > 200) pixels of the base glove, same point for the enhanced one.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-fighter-gloves.py check OUT_DIR        cut every tier with a pair image into OUT_DIR, grips marked; Resources untouched
  python Tools/build-fighter-gloves.py install [TIER ...]   art + grips into Resources and held-fighter.json, then icons; default all tiers with a pair image
  python Tools/build-fighter-gloves.py icons [TIER ...]     96x96 icons from the installed base art
  python Tools/build-fighter-gloves.py sheet OUT_DIR        all tiers on dark and light, grips marked
"""
import json, os, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

RES = "Assets/Resources"
GEAR = f"{RES}/Data/classes/fighter/held-fighter.json"
GLOVES = f"{RES}/Sprites/classes/fighter/weapons/glove"
SRC = "Assets/ArtLibrary/classes/fighter/new-style/glove-src"
ICONS = f"{RES}/Sprites/icons"
META_MODEL = f"{RES}/Sprites/classes/assassin/weapons/dagger/bronze-fang-base.png.meta"
ICON_META_MODEL = f"{ICONS}/weapon-assassin-0.png.meta"
FOLDER_META_MODEL = f"{RES}/Sprites/classes/assassin/weapons/dagger.meta"
CANVAS, GLOVE_W = 200, 170
PAD = 3  # the base glove keeps this much clear canvas on every side (bilinear filtering bleeds at the texture edge)
FIST_PART = 0.6  # the grip is the centre of the knuckle end: the right 60% of the glove's solid width (the cuff and laces trail behind the wrist)
MAGENTA_KEY = set()  # tiers drawn on #FF00FF because the glove is green
BLOB_MIN = 400


def key_flat(path, magenta=False):
    """Premultiplied RGBA floats off a flat key, as key_green/key_pair in build-assassin-new-style.py: glow fringe kept by dilating the solid blobs."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = (np.minimum(r, b) - g) if magenta else (g - np.maximum(r, b))
    alpha = 1 - np.clip((excess - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0  # off-colour pixels at the image edge
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(fg, 1 + np.where(sizes >= BLOB_MIN)[0]), iterations=3)
    if magenta:
        r, b = np.minimum(r, np.maximum(g, b)), np.minimum(b, np.maximum(r, g))  # magenta spill off the edge pixels
    else:
        g = np.minimum(g, np.maximum(r, b))  # green spill
    return np.dstack([r * alpha, g * alpha, b * alpha, alpha * 255])


def resize_pm(pm, scale):
    w, h = max(1, round(pm.shape[1] * scale)), max(1, round(pm.shape[0] * scale))
    return np.clip(np.dstack([np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]), 0, 255)


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def two_blobs(pm):
    """(slice, label) of the two biggest blobs, left to right: base then enhanced. The enhanced glow may be a loose halo, so blobs are joined by a wide dilation first."""
    solid = pm[..., 3] > 128
    labels, n = ndimage.label(ndimage.binary_dilation(solid, iterations=12))
    sizes = ndimage.sum(solid, labels, range(1, n + 1))
    big = sorted([(o, i + 1, s) for i, (o, s) in enumerate(zip(ndimage.find_objects(labels), sizes)) if s > 0.2 * max(sizes)], key=lambda t: t[0][1].start)
    if len(big) != 2:
        raise SystemExit(f"{len(big)} blobs, expected 2: sizes {[int(s) for _, _, s in big]}")
    return [(o, label) for o, label, _ in big]


def centroid(alpha, thr=200, fist_only=False):
    """Centre of the solid pixels. fist_only: of the knuckle end only (right FIST_PART of the solid width), so a long cuff or laces do not pull the grip back to the wrist."""
    ys, xs = np.where(alpha > thr)
    if fist_only:
        keep = xs >= xs.min() + (1 - FIST_PART) * (xs.max() - xs.min())
        xs, ys = xs[keep], ys[keep]
    return float(xs.mean()), float(ys.mean())


def best_overlap(a, b, reach=60):
    """(dx, dy) to place mask b so it covers mask a best (b's origin at a's origin + (dx, dy)); coarse search at 1/4 scale, then refined at full size."""
    a4, b4 = a[::4, ::4], b[::4, ::4]
    r = reach // 4
    _, cx, cy = max((_overlap(a4, b4, dx, dy), dx * 4, dy * 4) for dx in range(-r, r + 1) for dy in range(-r, r + 1))
    _, fx, fy = max((_overlap(a, b, dx, dy), dx, dy) for dx in range(cx - 4, cx + 5) for dy in range(cy - 4, cy + 5))
    return fx, fy


def _overlap(a, b, dx, dy):
    h, w = a.shape
    bh, bw = b.shape
    ax0, ay0 = max(0, dx), max(0, dy)
    bx0, by0 = max(0, -dx), max(0, -dy)
    ww, hh = min(w - ax0, bw - bx0), min(h - ay0, bh - by0)
    if ww <= 0 or hh <= 0:
        return -1
    return int((a[ay0:ay0 + hh, ax0:ax0 + ww] & b[by0:by0 + hh, bx0:bx0 + ww]).sum())


def cut_pair(tier):
    """{'base': (RGBA image, grip dict), 'enhanced': ...}: base scaled to GLOVE_W wide, enhanced by the same factor, both centred on the base fist centre."""
    pm = key_flat(f"{SRC}/{tier}-pair-chatgpt.png", tier in MAGENTA_KEY)
    labels = ndimage.label(ndimage.binary_dilation(pm[..., 3] > 128, iterations=12))[0]
    parts = []
    for o, label in two_blobs(pm):
        part = pm[o] * (labels[o] == label)[..., None]
        xs = np.where((part[..., 3] > 200).any(0))[0]
        parts.append((part, xs[-1] - xs[0] + 1))
    k = GLOVE_W / parts[0][1]  # one scale for both, from the solid width of the base glove
    out, target = {}, None
    for kind, (part, _) in zip(("base", "enhanced"), parts):
        small = resize_pm(part, k)
        cx, cy = centroid(small[..., 3], fist_only=True)
        if target is None:  # base: fist at the canvas centre, then shifted so a long cuff is not cut off; the enhanced fist goes to that same point
            ox, oy = round(CANVAS / 2 - cx), round(CANVAS / 2 - cy)
            ox = max(PAD, min(ox, CANVAS - PAD - small.shape[1])) if small.shape[1] <= CANVAS - 2 * PAD else ox
            oy = max(PAD, min(oy, CANVAS - PAD - small.shape[0])) if small.shape[0] <= CANVAS - 2 * PAD else oy
            target = (cx + ox, cy + oy)
        else:  # enhanced: same design plus glow, flames or sparks that would pull its centroid, so it is laid over the base by silhouette match
            dx, dy = best_overlap(base_small[..., 3] > 200, small[..., 3] > 200)
            ox, oy = base_offset[0] + dx, base_offset[1] + dy
        if kind == "base":
            base_small, base_offset = small, (ox, oy)
        canvas = np.zeros((CANVAS, CANVAS, 4))
        sy0, sx0 = max(0, -oy), max(0, -ox)
        dy0, dx0 = max(0, oy), max(0, ox)
        h, w = min(small.shape[0] - sy0, CANVAS - dy0), min(small.shape[1] - sx0, CANVAS - dx0)
        if h < small.shape[0] - sy0 or w < small.shape[1] - sx0:
            print(f"  {tier} {kind}: {small.shape[1]}x{small.shape[0]} at ({ox}, {oy}) clipped to the {CANVAS} px canvas")
        canvas[dy0:dy0 + h, dx0:dx0 + w] = small[sy0:sy0 + h, sx0:sx0 + w]
        out[kind] = (Image.fromarray(to_rgba(canvas), "RGBA"), {"w": CANVAS, "h": CANVAS, "gripX": int(round(target[0])), "gripY": int(round(target[1]))})
    return out


def tiers():
    return [e["name"] for e in json.load(open(GEAR, encoding="utf-8"))["weapon"]]


def complete_tiers():
    return [t for t in tiers() if os.path.exists(f"{SRC}/{t}-pair-chatgpt.png")]


def mark(d, x, y, col=(0, 255, 255)):
    d.line([(x - 5, y), (x + 5, y)], fill=col)
    d.line([(x, y - 5), (x, y + 5)], fill=col)


def check(out):
    os.makedirs(out, exist_ok=True)
    for t in complete_tiers():
        cuts = cut_pair(t)
        w = Image.new("RGB", (CANVAS * 2 + 30, CANVAS + 20), (58, 62, 70))
        for j, kind in enumerate(("base", "enhanced")):
            art, g = cuts[kind]
            at = (10 + j * (CANVAS + 10), 10)
            w.paste(art, at, art)
            mark(ImageDraw.Draw(w), at[0] + g["gripX"], at[1] + g["gripY"])
        w.save(f"{out}/{t}.png")
        print(t, {k: v[1] for k, v in cuts.items()})


def sheet(out):
    """All tiers, base over enhanced, on dark and light, grips marked. Drawn at full art size and at the in-game size (s 0.18, 36 px) beside each."""
    os.makedirs(out, exist_ok=True)
    names = complete_tiers()
    cuts = {t: cut_pair(t) for t in names}
    cw, ch = CANVAS + 50, CANVAS + 14
    for bg, name in (((58, 62, 70), "dark"), ((205, 205, 200), "light")):
        w = Image.new("RGB", (cw * len(names), ch * 2 + 14), bg)
        d = ImageDraw.Draw(w)
        for i, t in enumerate(names):
            for j, kind in enumerate(("base", "enhanced")):
                art, g = cuts[t][kind]
                at = (i * cw, 14 + j * ch)
                w.paste(art, at, art)
                mark(d, at[0] + g["gripX"], at[1] + g["gripY"])
                small = art.resize((36, 36), Image.LANCZOS)  # about the size the game draws it
                w.paste(small, (at[0] + CANVAS + 6, at[1] + CANVAS // 2 - 18), small)
            d.text((i * cw + 4, 1), t, fill=(255, 255, 0) if name == "dark" else (0, 0, 0))
        w.save(f"{out}/gloves-{name}.png")
    for t in names:
        print(t, {k: v[1] for k, v in cuts[t].items()})


def new_guid_copy(model, path):
    """Copy the import settings of one of the same kind with a new guid, so Unity does not compress the new png (build-assassin-new-style.py)."""
    if os.path.exists(path):
        return
    lines = open(model, encoding="utf-8", newline="").read().split("\n")
    lines[1] = f"guid: {uuid.uuid4().hex}" + ("\r" if lines[1].endswith("\r") else "")
    open(path, "w", encoding="utf-8", newline="").write("\n".join(lines))


def install_icons(names):
    """96x96 like build-assassin-new-style.py install_icons, without the tilt: a glove is already horizontal. About 10% margin."""
    all_names = tiers()
    for tier in names:
        i = all_names.index(tier)
        art = Image.open(f"{GLOVES}/{tier}-base.png").convert("RGBA")
        im = art.crop(art.getbbox())
        k = 86 / max(im.width, im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        for prefix, flip in (("weapon", False), ("offhand", True)):
            pic = im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im  # the offhand is the left glove: knuckles left
            icon = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
            icon.paste(pic, ((96 - pic.width) // 2, (96 - pic.height) // 2), pic)
            path = f"{ICONS}/{prefix}-fighter-{i}.png"
            icon.save(path)
            new_guid_copy(ICON_META_MODEL, path + ".meta")
    print("icons", names)


def install(names):
    """Art, grips and icons of the named tiers; held-fighter.json re-read right before the write, only baseGrip/enhancedGrip of those tiers change."""
    os.makedirs(GLOVES, exist_ok=True)
    new_guid_copy(FOLDER_META_MODEL, f"{RES}/Sprites/classes/fighter/weapons.meta")
    new_guid_copy(FOLDER_META_MODEL, f"{GLOVES}.meta")
    cuts = {t: cut_pair(t) for t in names}
    for tier, cut_ in cuts.items():
        for kind in ("base", "enhanced"):
            path = f"{GLOVES}/{tier}-{kind}.png"
            cut_[kind][0].save(path)
            new_guid_copy(META_MODEL, path + ".meta")
    raw = open(GEAR, "rb").read()  # another tool edits `anchors` of this file: read right before the write, keep its formatting style
    data = json.loads(raw)
    for tier, cut_ in cuts.items():
        entry = next(e for e in data["weapon"] if e["name"] == tier)
        entry["baseGrip"], entry["enhancedGrip"] = cut_["base"][1], cut_["enhanced"][1]
    compact = b"\n" not in raw.strip()
    text = json.dumps(data, separators=(",", ":")) if compact else json.dumps(data, indent=1)
    open(GEAR, "wb").write(text.encode())
    install_icons(names)
    print("installed", names, {t: c["base"][1] for t, c in cuts.items()})


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "check":
        check(sys.argv[2])
    elif cmd == "sheet":
        sheet(sys.argv[2])
    elif cmd == "install":
        install(sys.argv[2:] or complete_tiers())
    elif cmd == "icons":
        install_icons(sys.argv[2:] or complete_tiers())
    else:
        raise SystemExit(__doc__)

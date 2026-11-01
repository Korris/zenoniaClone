"""Cleric spirit companion in the new style: cuts the ChatGPT pair images (base left, enhanced right) on flat #0000FF.
Blue key because the tiers hold green (gems, bows, leaves) and pink-peach (fault log: pink ears beside #FF00FF).
Every tier is scaled and placed to overlap acolyte-base most, so the silhouette and the pivot stay put between tiers.

Usage (from the project root; needs Pillow and numpy):
  python Tools/build-cleric-spirit.py backup                  copies art, float and offhand-cleric icons once to companion-src/backup/
  python Tools/build-cleric-spirit.py cut [tier...]           cut only, into the preview folder; prints fit and colour checks
  python Tools/build-cleric-spirit.py install [tier...]       writes companion/art and the companion grips in held-cleric.json
  python Tools/build-cleric-spirit.py float                   writes companion/float from src/float-views.png (w, h, grip as before)
  python Tools/build-cleric-spirit.py icons                   rebuilds offhand-cleric-<tier> for tiers whose art is new
  python Tools/build-cleric-spirit.py preview                 sheet of all tiers and composites beside the hero, from the installed art
  python Tools/build-cleric-spirit.py drafts [tier...]        sheet and composites of the cut sources; writes nothing in the game
Sources: Assets/ArtLibrary/classes/cleric/companion-src/src/<tier>-pair.png, float-views.png. Touches nothing in fx/, skills/, weapons/, body/."""
import importlib.util, json, os, re, shutil, sys
import numpy as np
from PIL import Image, ImageDraw

RES = "Assets/Resources"
ART = f"{RES}/Sprites/classes/cleric/companion/art"
FLOAT = f"{RES}/Sprites/classes/cleric/companion/float"
ICONS = f"{RES}/Sprites/icons"
GEAR = f"{RES}/Data/classes/cleric/held-cleric.json"
SRC_ROOT = "Assets/ArtLibrary/classes/cleric/companion-src"
SRC = f"{SRC_ROOT}/src"
BACKUP = f"{SRC_ROOT}/backup"
OUT = os.environ.get("SPIRIT_PREVIEW", r"C:\Users\korri\AppData\Local\Temp\claude\D--zenoniaClone\fe6a3c77-b3df-4eba-936f-3b0510d923d2\scratchpad\cleric-spirit-new")
TIERS = ["acolyte", "blossom", "dawn", "seraph", "moonlit", "radiant", "halo", "verdant", "celestial", "archangel"]

CANVAS = 300                 # working square; the pivot of every tier is its centre
K = 0.29                     # source px -> art px for acolyte, gives about 213 px tall like the old art
SCALE = 0.3                  # HeroClassDef.CompanionScale: frame px per art px
FRAME_PER_UNIT = 8           # GameConfig: 192 frame px over 24 world units
OFFSETS = {"down": (11, 3), "up": (-11, -3), "left": (10, 0), "right": (-10, 0)}  # HeroCompanion.Offsets
HOVER = 11


def key_blue(path):
    """RGBA from the flat blue image. Blue is in no tier's palette, so every bluish pixel is background, holes included."""
    a = np.array(Image.open(path).convert("RGB")).astype(float)
    a[:6], a[-6:], a[:, :6], a[:, -6:] = 0, 0, 0, 0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    excess = np.clip(b - np.maximum(r, g), 0, None)
    bg = np.median(b[excess > 150]) if (excess > 150).any() else 255.0
    alpha = 1 - np.clip((excess - 14) / (bg * 0.82), 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    safe = np.maximum(alpha, 0.01)
    # the fringe mixes into the black outline: take the blue back out, colour stays dark
    out = np.empty(a.shape[:2] + (4,), np.uint8)
    out[..., 0] = np.clip(r, 0, 255)
    out[..., 1] = np.clip(g, 0, 255)
    out[..., 2] = np.clip(np.where(alpha < 1, np.minimum(b - (1 - alpha) * bg, np.maximum(r, g)), b), 0, 255)
    out[..., 3] = np.round(alpha * 255)
    del safe
    return out


def components(mask):
    """Connected areas of a boolean array as (area, x0, y0, x1, y1, label array index), 4-connected, without scipy."""
    h, w = mask.shape
    labels = np.zeros((h, w), np.int32)
    found = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if labels[y0, x0]:
            continue
        n = len(found) + 1
        stack, area = [(y0, x0)], 0
        labels[y0, x0] = n
        x_lo, x_hi, y_lo, y_hi = x0, x0, y0, y0
        while stack:
            y, x = stack.pop()
            area += 1
            x_lo, x_hi, y_lo, y_hi = min(x_lo, x), max(x_hi, x), min(y_lo, y), max(y_hi, y)
            for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not labels[ny, nx]:
                    labels[ny, nx] = n
                    stack.append((ny, nx))
        found.append((area, x_lo, y_lo, x_hi + 1, y_hi + 1))
    return labels, found


def split_pair(path):
    """Left and right creature. Cuts at the emptiest column near the middle; drops specks."""
    rgba = key_blue(path)
    h, w = rgba.shape[:2]
    solid = rgba[..., 3] > 128
    cols = solid.sum(axis=0)
    lo, hi = int(w * 0.38), int(w * 0.62)
    empty = np.where(cols[lo:hi] == 0)[0]
    if len(empty) == 0:
        print(path, "copies touch: no empty column between them")
        cut = lo + int(np.argmin(cols[lo:hi]))
    else:
        cut = lo + int(np.median(empty))
    pair = []
    for part in (rgba[:, :cut], rgba[:, cut:]):
        part = part.copy()
        labels, found = components(part[..., 3] > 128)
        biggest = max(f[0] for f in found)
        keep = np.isin(labels, [i + 1 for i, f in enumerate(found) if f[0] >= max(60, biggest * 0.0008)])
        # keep the soft fringe around kept areas: grow by 3 px
        grown = keep.copy()
        for _ in range(3):
            g = grown.copy()
            g[1:] |= grown[:-1]; g[:-1] |= grown[1:]; g[:, 1:] |= grown[:, :-1]; g[:, :-1] |= grown[:, 1:]
            grown = g
        part[..., 3] = np.where(grown, part[..., 3], 0)
        ys, xs = np.nonzero(part[..., 3])
        pair.append(Image.fromarray(part[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA"))
    return pair


def mask_of(im):
    return np.array(im.getchannel("A")) > 128


def place_on_canvas(im, k, ox, oy):
    """im scaled by k, its centre at canvas centre + (ox, oy)."""
    w, h = max(1, round(im.width * k)), max(1, round(im.height * k))
    small = im.resize((w, h), Image.LANCZOS)
    out = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    out.alpha_composite(small, (CANVAS // 2 - w // 2 + ox, CANVAS // 2 - h // 2 + oy)) if (CANVAS // 2 - w // 2 + ox >= 0 and CANVAS // 2 - h // 2 + oy >= 0) else None
    return out


def template_mask():
    base, _ = split_pair(f"{SRC}/acolyte-pair.png")
    return mask_of(place_on_canvas(base, K, 0, 0))


def best_fit(im, template):
    """Scale and offset where im overlaps the acolyte silhouette most. Motifs outside it only lower the score a little."""
    best = (-1, K, 0, 0)
    for s in np.arange(0.82, 1.2001, 0.02):
        k = K * s
        m = mask_of(place_on_canvas(im, k, 0, 0))
        for dy in range(-40, 41, 4):
            my = np.roll(m, dy, axis=0)
            for dx in range(-24, 25, 4):
                mxy = np.roll(my, dx, axis=1)
                iou = (mxy & template).sum() / max(1, (mxy | template).sum())
                if iou > best[0]:
                    best = (iou, k, dx, dy)
    iou, k, dx, dy = best
    for ddy in range(-3, 4):
        for ddx in range(-3, 4):
            m = np.roll(np.roll(mask_of(place_on_canvas(im, k, 0, 0)), dy + ddy, 0), dx + ddx, 1)
            v = (m & template).sum() / max(1, (m | template).sum())
            if v > best[0]:
                best = (v, k, dx + ddx, dy + ddy)
    return best


def checks(tier, kind, art):
    a = np.array(art).astype(int)
    solid = a[..., 3] > 200
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = (np.minimum(r, b) - g > 15) & solid
    blue = (b - np.maximum(r, g) > 30) & solid
    semi = ((a[..., 3] > 20) & (a[..., 3] < 235)).sum()
    print(f"  {tier}-{kind}: size {art.size} pink {pink.sum()} blue {blue.sum()} soft-edge {semi}")


def cut(tier, template):
    """Returns {kind: (cropped art, gripX, gripY)}."""
    base, enhanced = split_pair(f"{SRC}/{tier}-pair.png")
    result = {}
    for kind, im in (("base", base), ("enhanced", enhanced)):
        iou, k, dx, dy = (1.0, K, 0, 0) if tier == "acolyte" and kind == "base" else best_fit(im, template)
        placed = place_on_canvas(im, k, dx, dy)
        x0, y0, x1, y1 = placed.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
        x0, y0, x1, y1 = max(0, x0 - 3), max(0, y0 - 3), min(CANVAS, x1 + 3), min(CANVAS, y1 + 3)
        art = placed.crop((x0, y0, x1, y1))
        grip_x, grip_y = CANVAS // 2 - x0, CANVAS // 2 - y0
        print(f"  {tier}-{kind}: overlap with acolyte {iou:.3f}, scale {k:.3f}, offset ({dx}, {dy}), grip ({grip_x}, {grip_y})")
        checks(tier, kind, art)
        result[kind] = (art, grip_x, grip_y)
    return result


def write_grips(entries):
    """Only baseGrip and enhancedGrip of named companion tiers in the one-line file: another agent edits weapon grips in it."""
    raw = open(GEAR, "rb").read().decode()
    start = raw.index('"companion":[')
    end = raw.index("]", start)
    part = raw[start:end]
    for tier, (b, e) in entries.items():
        js = lambda g: json.dumps(g, separators=(",", ":"))
        part, n = re.subn(r'(\{"name":"' + tier + r'","glow":"#[0-9a-f]+","baseGrip":)\{[^}]*\},"enhancedGrip":\{[^}]*\}\}',
                          lambda m: m.group(1) + js(b) + ',"enhancedGrip":' + js(e) + "}", part, count=1)
        assert n == 1, tier
    open(GEAR, "wb").write((raw[:start] + part + raw[end:]).encode())
    json.load(open(GEAR, encoding="utf-8"))


def install(tiers):
    template = template_mask()
    grips = {}
    for tier in tiers:
        print(tier)
        cutout = cut(tier, template)
        for kind, (art, gx, gy) in cutout.items():
            art.save(f"{ART}/{tier}-{kind}.png")
        grips[tier] = tuple({"w": cutout[k][0].width, "h": cutout[k][0].height, "gripX": cutout[k][1], "gripY": cutout[k][2]} for k in ("base", "enhanced"))
    write_grips(grips)
    print("installed", tiers)


def flap(im, amount, root, reach, side):
    """Bends the wings: rows move up with the distance from the body past `root`. side: -1 left, 1 right, 0 both. Ears and head stay."""
    a = np.array(im).astype(float)
    h, w = a.shape[:2]
    cx = w / 2
    out = np.zeros_like(a)
    for x in range(w):
        d = x - cx
        away = abs(d) - root
        if away <= 0 or (side and np.sign(d) != side):
            out[:, x] = a[:, x]
            continue
        shift = amount * (min(away, reach) / reach) ** 1.5
        ys = np.arange(h) + shift   # source row for each output row
        y0 = np.clip(np.floor(ys).astype(int), 0, h - 1)
        y1 = np.clip(y0 + 1, 0, h - 1)
        t = (ys - np.floor(ys))[:, None]
        # premultiplied so the fringe does not smear
        c0, c1 = a[y0, x], a[y1, x]
        al = c0[:, 3:] * (1 - t) + c1[:, 3:] * t
        rgb = c0[:, :3] * c0[:, 3:] * (1 - t) + c1[:, :3] * c1[:, 3:] * t
        out[:, x, :3] = rgb / np.maximum(al, 1)
        out[:, x, 3:] = al
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")


FLOAT_PHASE = [0, 7, 0, -7]   # art px at the wing tip, four frames at 140 ms


def float_views():
    """front, back, side (facing left) in one image, in this order, left to right."""
    rgba = key_blue(f"{SRC}/float-views.png")
    labels, found = components(rgba[..., 3] > 128)
    big = max(f[0] for f in found)
    h, w = rgba.shape[:2]
    bounds = np.linspace(0, w, 4).astype(int)
    views = []
    solid = rgba[..., 3] > 128
    cols = solid.sum(axis=0)
    # cut at the emptiest columns near each third
    cuts = [0]
    for b in bounds[1:-1]:
        lo, hi = b - 90, b + 90
        empty = np.where(cols[lo:hi] == 0)[0]
        cuts.append(lo + (int(np.median(empty)) if len(empty) else int(np.argmin(cols[lo:hi]))))
    cuts.append(w)
    for i in range(3):
        part = rgba[:, cuts[i]:cuts[i + 1]]
        ys, xs = np.nonzero(part[..., 3] > 8)
        views.append(Image.fromarray(part[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA"))
    return views


def write_float():
    gear = json.load(open(GEAR, encoding="utf-8"))
    front, back, side = float_views()
    template = template_mask()
    views = {"down": front, "up": back, "left": side, "right": side.transpose(Image.FLIP_LEFT_RIGHT)}
    for loop in gear["companionFloat"]:
        d = loop["dir"]
        iou, k, dx, dy = best_fit(views[d], template)
        print(d, f"overlap {iou:.3f} scale {k:.3f} offset ({dx},{dy})")
        placed = place_on_canvas(views[d], k, 0, 0)
        for i, f in enumerate(loop["frames"]):
            side_flag = {"down": 0, "up": 0, "left": 1, "right": -1}[d]
            moved = flap(placed, FLOAT_PHASE[i], 38, 80, side_flag)
            canvas = Image.new("RGBA", (f["w"], f["h"]), (0, 0, 0, 0))
            box_x, box_y = f["gripX"] - CANVAS // 2, f["gripY"] - CANVAS // 2
            canvas.paste(moved, (box_x, box_y), moved)
            lost = np.array(moved.getchannel("A")).astype(int).sum() - np.array(canvas.getchannel("A")).astype(int).sum()
            if lost > 255 * 4:
                print(f"  {d}-{i}: {lost // 255} px cut by the {f['w']}x{f['h']} frame")
            canvas.save(f"{FLOAT}/{d}-{i}.png")


def icon(path):
    """Same steps as fit() in build-item-icons.py with tilt 0."""
    im = Image.open(path).convert("RGBA")
    im = im.crop(im.getbbox())
    k = 88 / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    out.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    return out


def write_icons(tiers):
    for tier in tiers:
        icon(f"{ART}/{tier}-base.png").save(f"{ICONS}/offhand-cleric-{TIERS.index(tier)}.png")


def backup():
    if os.path.exists(BACKUP):
        print("backup exists, left as it is")
        return
    for sub, folder in (("art", ART), ("float", FLOAT)):
        os.makedirs(f"{BACKUP}/{sub}")
        for f in os.listdir(folder):
            if f.endswith(".png"):
                shutil.copy2(f"{folder}/{f}", f"{BACKUP}/{sub}/{f}")
    shutil.copy2(GEAR, f"{BACKUP}/held-cleric.json.txt")
    os.makedirs(f"{BACKUP}/icons")
    for i in range(10):
        shutil.copy2(f"{ICONS}/offhand-cleric-{i}.png", f"{BACKUP}/icons/offhand-cleric-{i}.png")
    print("backed up")


def compose_tool():
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def beside_hero(hero, art, grip, facing, zoom=3):
    """Hero frame and companion at the same frame px scale, HeroCompanion's follow offset and hover, no bob."""
    pad = 120
    out = Image.new("RGBA", (192 + 2 * pad, 192 + pad), (58, 62, 70, 255))
    ox, oy = OFFSETS[facing]
    cx, cy = pad + 96 + ox * FRAME_PER_UNIT, pad + 192 - (oy + HOVER) * FRAME_PER_UNIT
    w, h = max(1, round(art.width * SCALE)), max(1, round(art.height * SCALE))
    small = art.resize((w, h), Image.LANCZOS)
    pos = (round(cx - grip[0] * SCALE), round(cy - grip[1] * SCALE))
    if oy >= 0:
        out.alpha_composite(small, pos)   # behind the hero: higher on screen, lower order
    out.alpha_composite(hero, (pad, pad))
    if oy < 0:
        out.alpha_composite(small, pos)
    return out.resize((out.width * zoom, out.height * zoom), Image.NEAREST if zoom >= 3 else Image.LANCZOS)


def preview():
    os.makedirs(OUT, exist_ok=True)
    gear = json.load(open(GEAR, encoding="utf-8"))
    entries = {e["name"]: e for e in gear["companion"]}
    # every tier, both kinds, on dark and light
    cell = 250
    sheet = Image.new("RGBA", (cell * 5, cell * 4), (58, 62, 70, 255))
    d = ImageDraw.Draw(sheet)
    for i, t in enumerate(TIERS):
        for j, kind in enumerate(("base", "enhanced")):
            x, y = (i % 5) * cell, ((i // 5) * 2 + j) * cell
            art = Image.open(f"{ART}/{t}-{kind}.png").convert("RGBA")
            g = entries[t][kind + "Grip"]
            if j == 1:
                d.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(205, 208, 200, 255))
            sheet.alpha_composite(art, (x + cell // 2 - g["gripX"], y + cell // 2 - g["gripY"]))
            d.text((x + 4, y + 2), f"{i} {t}-{kind}", fill=(255, 255, 255, 255) if j == 0 else (0, 0, 0, 255))
    sheet.save(f"{OUT}/sheet-all-tiers.png")
    comp = compose_tool()
    hero = comp.compose("cleric", "idle", "down", 0)
    row = []
    for t in TIERS:
        for kind in ("base", "enhanced"):
            g = entries[t][kind + "Grip"]
            row.append(beside_hero(hero, Image.open(f"{ART}/{t}-{kind}.png").convert("RGBA"), (g["gripX"], g["gripY"]), "down", 1))
    w, h = row[0].size
    grid = Image.new("RGBA", (w * 5, h * 4))
    for i, im in enumerate(row):
        grid.paste(im, ((i // 2 % 5) * w, ((i // 2 // 5) * 2 + i % 2) * h))
    grid.save(f"{OUT}/composite-all-tiers.png")
    # float: each facing in the game's position, four frames
    loops = {l["dir"]: l["frames"] for l in gear["companionFloat"]}
    strip = []
    for facing in ("down", "up", "left", "right"):
        h_im = comp.compose("cleric", "idle", facing, 0)
        for i in (0, 1, 3):
            f = loops[facing][i]
            strip.append(beside_hero(h_im, Image.open(f"{FLOAT}/{facing}-{i}.png").convert("RGBA"), (f["gripX"], f["gripY"]), facing, 1))
    w, h = strip[0].size
    grid = Image.new("RGBA", (w * 3, h * 4))
    for i, im in enumerate(strip):
        grid.paste(im, ((i % 3) * w, (i // 3) * h))
    grid.save(f"{OUT}/composite-float.png")
    icons = Image.new("RGBA", (96 * 10, 96 * 2), (58, 62, 70, 255))
    for i in range(10):
        a = Image.open(f"{ICONS}/offhand-cleric-{i}.png").convert("RGBA")
        icons.alpha_composite(a, (i * 96, 0))
        b = Image.open(f"{BACKUP}/icons/offhand-cleric-{i}.png").convert("RGBA")
        icons.alpha_composite(b, (i * 96, 96))
    icons.save(f"{OUT}/icons-new-top-old-bottom.png")
    print("preview in", OUT)


def drafts(tiers):
    """Sheet and composites of the cut sources, into the preview folder only: writes nothing in the game."""
    os.makedirs(OUT, exist_ok=True)
    template = template_mask()
    hero = compose_tool().compose("cleric", "idle", "down", 0)
    cell = 250
    rows = (len(tiers) + 4) // 5
    sheet = Image.new("RGBA", (cell * 5, cell * rows * 2), (58, 62, 70, 255))
    d = ImageDraw.Draw(sheet)
    beside = []
    for i, tier in enumerate(tiers):
        print(tier)
        for j, (kind, (art, gx, gy)) in enumerate(cut(tier, template).items()):
            x, y = (i % 5) * cell, ((i // 5) * 2 + j) * cell
            if j == 1:
                d.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(205, 208, 200, 255))
            sheet.alpha_composite(art, (x + cell // 2 - gx, y + cell // 2 - gy))
            d.text((x + 4, y + 2), f"{TIERS.index(tier)} {tier}-{kind}", fill=(255, 255, 255, 255) if j == 0 else (0, 0, 0, 255))
            beside.append(beside_hero(hero, art, (gx, gy), "down", 1))
    sheet.save(f"{OUT}/drafts-sheet.png")
    w, h = beside[0].size
    grid = Image.new("RGBA", (w * 5, h * rows * 2))
    for n, im in enumerate(beside):
        grid.paste(im, ((n // 2 % 5) * w, ((n // 2 // 5) * 2 + n % 2) * h))
    grid.save(f"{OUT}/drafts-beside-hero.png")
    print("drafts in", OUT)


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    tiers = sys.argv[2:] or [t for t in TIERS if os.path.exists(f"{SRC}/{t}-pair.png")]
    if cmd == "backup":
        backup()
    elif cmd == "cut":
        template = template_mask()
        os.makedirs(OUT, exist_ok=True)
        for tier in tiers:
            print(tier)
            for kind, (art, gx, gy) in cut(tier, template).items():
                bg = Image.new("RGBA", art.size, (58, 62, 70, 255))
                bg.alpha_composite(art)
                bg.resize((art.width * 2, art.height * 2), Image.LANCZOS).save(f"{OUT}/cut-{tier}-{kind}.png")
    elif cmd == "install":
        install(tiers)
    elif cmd == "float":
        write_float()
    elif cmd == "icons":
        write_icons(tiers)
    elif cmd == "preview":
        preview()
    elif cmd == "drafts":
        drafts(tiers)
    else:
        print(__doc__)


if __name__ == "__main__":
    main()

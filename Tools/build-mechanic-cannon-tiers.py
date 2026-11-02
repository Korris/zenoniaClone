"""Mechanic cannon tiers: one ChatGPT image per tier holds the three views; they are cut to the size of the live art so the anchors fit unchanged. Tier 0 (scrap) keeps the live art. No enhanced art: the code draws no enhance effect on anchor art.

Originals: Assets/ArtLibrary/classes/mechanic/new-style/dual-src/tiers/cannon/<tier>-chatgpt.png, flat #00FF00, side view along the top, front below left, back below right. A view redrawn alone: <tier>-<view>-chatgpt.png wins over that view of the set.
Game finds cannon/<tier>/<view>.png by the manifest's `name` (GearOverlays.CenteredTier), falling back to cannon/<view>.png.

Run from the project root; Pillow, numpy, scipy.
  python Tools/build-mechanic-cannon-tiers.py masters                sheet of the three masters, uploaded with every prompt
  python Tools/build-mechanic-cannon-tiers.py check OUT <tier>...    cut, measure and picture under OUT, nothing goes to the game
  python Tools/build-mechanic-cannon-tiers.py install <tier>...      write cannon/<tier>/{front,back,side}.png with their .meta
  python Tools/build-mechanic-cannon-tiers.py icons                  offhand-mechanic-0..9 from each side view. Old ones kept once in tiers/cannon/old-icons/
  python Tools/build-mechanic-cannon-tiers.py sheet OUT              every tier on the hero in four directions and the icons, under OUT only
"""
import importlib.util, json, math, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

spec = importlib.util.spec_from_file_location("bd", "Tools/build-mechanic-dual.py")
bd = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bd)

TIERS = ["scrap", "brass", "copper", "plasma", "ember", "frost", "volt", "gatling", "star", "dragon"]
ORIG = f"{bd.SRC}/tiers/cannon"
CANNON = f"{bd.WEAPONS}/cannon"
ICONS = f"{bd.RES}/Sprites/icons"
VIEWS = ("front", "back", "side")
ICON_SIZE, ICON_BOX, ICON_TILT = 96, 77, 45  # about 10% empty on every side: the enhance effect will be drawn in the slot around it
GREY = (58, 62, 70)


def masters():
    """Side along the top, front below left, back below right: the layout every prompt asks for."""
    os.makedirs(ORIG, exist_ok=True)
    s = Image.new("RGB", (1536, 1024), (0, 255, 0))
    side = Image.open(f"{bd.SRC}/cannon-side.png").convert("RGB")
    s.paste(side.resize((1536, round(side.height * 1536 / side.width)), Image.LANCZOS), (0, 0))
    for x, v in ((0, "front"), (768, "back")):
        s.paste(Image.open(f"{bd.SRC}/cannon-{v}.png").convert("RGB").resize((512, 512), Image.LANCZOS), (x + 128, 512))
    s.save(f"{ORIG}/cannon-masters.png")


def key_pieces(path, count):
    """The `count` biggest solid pieces of a flat #00FF00 image: straight RGBA with unmixed edges, as (top, left, image)."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    g, mx = rgb[..., 1], np.maximum(rgb[..., 0], rgb[..., 2])
    alpha = 1 - np.clip((g - mx - 30) / 150, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0  # stray pixels at the image edge stretch the box
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * np.array([0, 255, 0], np.float32)) / np.maximum(a, 0.02), 0), 0, 255)
    out[..., 1] = np.where((g > mx) & (alpha < 0.98), mx, out[..., 1])  # green spill on the edge
    lab, n = ndimage.label(alpha > 0.5, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    pieces = []
    for i in 1 + np.argsort(sizes)[::-1][:count]:
        keep = ndimage.binary_dilation(lab == i, iterations=3)
        ys, xs = np.nonzero(keep)
        part = np.dstack([out, alpha * keep * 255])[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        pieces.append((int(ys.min()), int(xs.min()), Image.fromarray(part.astype(np.uint8), "RGBA")))
    print(os.path.basename(path), "pieces", n, "sizes", [int(v) for v in np.sort(sizes)[::-1][:count + 1]])
    return pieces


def axis_angle(rgba):
    """Direction of the barrel in degrees (y down, -90..90)."""
    ys, xs = np.nonzero(np.asarray(rgba)[..., 3] > 128)
    pts = np.stack([xs, ys], 1).astype(float)
    pts -= pts.mean(0)
    _, _, vt = np.linalg.svd(pts[::17], full_matrices=False)
    a = math.degrees(math.atan2(vt[0][1], vt[0][0]))
    return (a + 90) % 180 - 90


def fit(part, target_rgba):
    """Turn `part` onto the master's axis, resize with LANCZOS and place it on the master's canvas where the silhouettes overlap most. Never past the canvas: that would cut the outline."""
    target = np.asarray(target_rgba)[..., 3] > 128
    H, W = target.shape
    turn = axis_angle(part) - axis_angle(target_rgba)
    turn = (turn + 90) % 180 - 90
    part = part.rotate(turn, expand=True, resample=Image.BICUBIC)
    part = part.crop(Image.fromarray((np.asarray(part)[..., 3] > 128).astype(np.uint8) * 255).getbbox())
    mask = part.getchannel("A")
    best = (-1, None)
    for w in range(W - 14, W + 1, 2):
        for h in range(H - 14, H + 1, 2):
            m = np.asarray(mask.resize((w, h), Image.BILINEAR)) > 127
            ms = int(m.sum())
            for dy in range(0, H - h + 1, 1):
                for dx in range(0, W - w + 1, 1):
                    inter = int((m & target[dy:dy + h, dx:dx + w]).sum())
                    iou = inter / (ms + int(target.sum()) - inter)
                    if iou > best[0]:
                        best = (iou, (w, h, dx, dy))
    iou, (w, h, dx, dy) = best
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    canvas.paste(part.resize((w, h), Image.LANCZOS), (dx, dy))
    squash = (w / part.width) / (h / part.height)
    return canvas, dict(iou=round(iou, 3), turn=round(turn, 1), fit=[w, h, dx, dy], source=list(part.size), squash=round(squash, 3))


def live(view):
    return Image.open(f"{CANNON}/{view}.png").convert("RGBA")


def cut(tier):
    """Told apart by shape: the long thin piece is the side view, of the other two the left one is the front."""
    pieces = key_pieces(f"{ORIG}/{tier}-chatgpt.png", 3)
    pieces.sort(key=lambda p: p[2].width / p[2].height)
    named = dict(zip(("front", "back"), sorted(pieces[:2], key=lambda p: p[1])))
    named["side"] = pieces[2]
    done, infos = {}, {}
    for view in VIEWS:
        alone = f"{ORIG}/{tier}-{view}-chatgpt.png"
        part = key_pieces(alone, 1)[0][2] if os.path.exists(alone) else named[view][2]
        done[view], infos[view] = fit(part, live(view))
        print(tier, view, json.dumps(infos[view]))
    return done, infos


def art_for(tier):
    """What the game would draw for this tier: its own view, else the live one."""
    return {v: Image.open(f"{CANNON}/{tier}/{v}.png" if os.path.exists(f"{CANNON}/{tier}/{v}.png") else f"{CANNON}/{v}.png").convert("RGBA") for v in VIEWS}


def live_anchors():
    data = json.load(open(bd.GEAR_DUAL, encoding="utf-8"))
    return {(a["part"], a["anim"], a["dir"]): a["frames"] for a in data["anchors"]}, data["weapon"]


POSES = [("idle", d, 0) for d in bd.DIRS] + [("attack4", "right", 1), ("attack4", "left", 1)]
CROP = (40, 20, 292, 272)  # frame plus padding, around the hero


def hero_row(views, anchors, base, label):
    """One row: the poses, as HeroRenderer draws them."""
    art = dict(base)
    art.update(views)
    cw, ch = CROP[2] - CROP[0], CROP[3] - CROP[1]
    row = Image.new("RGB", (90 + len(POSES) * cw, ch), GREY)
    ImageDraw.Draw(row).text((4, 4), label, fill=(255, 255, 0))
    for i, (anim, d, f) in enumerate(POSES):
        c = bd.compose(bd.RES, art, anchors, anim, d, f).crop(CROP)
        row.paste(c, (90 + i * cw, 0), c)
    return row


def check(out, tier):
    """Top: the three cuts at 1x and the difference from the master (red: art outside the master, blue: master not covered). Below: on the hero."""
    os.makedirs(out, exist_ok=True)
    done, _ = cut(tier)
    anchors, _ = live_anchors()
    row = hero_row(done, anchors, bd.arts(), tier)
    tops = Image.new("RGB", (row.width, 280 * 2), GREY)
    x = 0
    for v in VIEWS:
        im, m = done[v], live(v)
        tops.paste(im, (x, 5), im)
        a, b = np.asarray(im)[..., 3] > 128, np.asarray(m)[..., 3] > 128
        diff = np.zeros(a.shape + (3,), np.uint8)
        diff[a & b] = (90, 90, 90)
        diff[a & ~b] = (255, 60, 60)
        diff[~a & b] = (60, 120, 255)
        tops.paste(Image.fromarray(diff), (x, 285))
        x += im.width + 10
    s = Image.new("RGB", (row.width, tops.height + row.height), GREY)
    s.paste(tops, (0, 0))
    s.paste(row, (0, tops.height))
    s.save(f"{out}/check-{tier}.png")
    # enlarged hero, to read the art at the size it is seen
    row.resize((row.width * 2, row.height * 2), Image.LANCZOS).save(f"{out}/check-{tier}-hero-2x.png")


def write_png_with_meta(im, path, meta_from):
    """The .meta goes first: Unity, often open on this project, would otherwise make its own for the new PNG."""
    if not os.path.exists(path + ".meta"):
        text = open(meta_from, "rb").read().decode()
        with open(path + ".meta", "wb") as fp:
            fp.write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1).encode())
    im.save(path)


def install(tier):
    folder = f"{CANNON}/{tier}"
    if not os.path.isdir(folder):
        text = open(f"{bd.SRC}/move.meta", "rb").read().decode()  # a folder's .meta, so every machine gets the same guid
        with open(folder + ".meta", "wb") as fp:
            fp.write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1).encode())
        os.makedirs(folder)
    done, _ = cut(tier)
    for view, im in done.items():
        write_png_with_meta(im, f"{folder}/{view}.png", f"{CANNON}/side.png.meta")


def icon_frame():
    """Scale and offset from the live side art, so every tier has one size and one centre whatever its edge does."""
    box = live("side").rotate(ICON_TILT, expand=True, resample=Image.BICUBIC).getbbox()
    w, h = box[2] - box[0], box[3] - box[1]
    k = ICON_BOX / max(w, h)
    return k, ((ICON_SIZE - w * k) / 2 - box[0] * k, (ICON_SIZE - h * k) / 2 - box[1] * k)


def icon(side, frame):
    k, (ox, oy) = frame
    im = side.rotate(ICON_TILT, expand=True, resample=Image.BICUBIC)
    small = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    out = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    out.paste(small, (round(ox), round(oy)))
    return out


def write_icons():
    keep = f"{ORIG}/old-icons"
    os.makedirs(keep, exist_ok=True)
    frame = icon_frame()
    for i, tier in enumerate(TIERS):
        path = f"{ICONS}/offhand-mechanic-{i}.png"
        if os.path.exists(path) and not os.path.exists(f"{keep}/offhand-mechanic-{i}.png"):
            shutil.copy2(path, f"{keep}/offhand-mechanic-{i}.png")
        im = icon(art_for(tier)["side"], frame)
        im.save(path)
        print(path, "box", im.getbbox())


def sheet(out):
    os.makedirs(out, exist_ok=True)
    anchors, _ = live_anchors()
    base = bd.arts()
    rows = [hero_row(art_for(t), anchors, base, t) for t in TIERS]
    W, H = rows[0].size
    contact = Image.new("RGB", (W, H * len(rows)), GREY)
    for i, r in enumerate(rows):
        contact.paste(r, (0, i * H))
    contact.save(f"{out}/contact-cannon-tiers-on-hero.png")
    for t in range(0, 10, 5):
        contact.crop((0, t * H, W, (t + 5) * H)).save(f"{out}/contact-cannon-tiers-on-hero-{t}-{t + 4}.png")
    z = 2
    s = Image.new("RGB", (5 * (ICON_SIZE * z + 10) + 10, 4 * (ICON_SIZE * z + 10) + 10), (40, 40, 44))
    d = ImageDraw.Draw(s)
    for i in range(10):
        im = Image.open(f"{ICONS}/offhand-mechanic-{i}.png").convert("RGBA").resize((ICON_SIZE * z, ICON_SIZE * z), Image.LANCZOS)
        x, y = 10 + (i % 5) * (ICON_SIZE * z + 10), 10 + (i // 5) * 2 * (ICON_SIZE * z + 10)
        for yy, bg in ((y, (64, 54, 44)), (y + ICON_SIZE * z + 10, (214, 208, 196))):  # on a dark slot and a light one
            d.rectangle((x, yy, x + ICON_SIZE * z, yy + ICON_SIZE * z), fill=bg)
            s.paste(im, (x, yy), im)
    s.save(f"{out}/contact-cannon-icons.png")


if __name__ == "__main__":
    cmd, args = sys.argv[1], sys.argv[2:]
    if cmd == "masters":
        masters()
    elif cmd == "check":
        for t in args[1:]:
            check(args[0], t)
    elif cmd == "install":
        for t in args:
            install(t)
    elif cmd == "icons":
        write_icons()
    elif cmd == "sheet":
        sheet(args[0])
    else:
        raise SystemExit(__doc__)

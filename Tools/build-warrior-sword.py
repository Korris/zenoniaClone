"""Cuts the Warrior's swords from their ChatGPT originals, all in the silhouette of iron, which was traced from the mock's blade. Python 3 + Pillow + numpy + scipy.

  build-warrior-sword.py template            trace the painted Warrior blade, print its measurements, write the reference and silhouette template
  build-warrior-sword.py check <tier>...     cut and measure against iron-base, write nothing to Resources
  build-warrior-sword.py <tier>...           cut, write the sprites, update those tiers' grips in held-gear.json
  build-warrior-sword.py iron-cut            iron only: the very sword of warrior.png, not a repaint, in both iron sprites

  build-warrior-sword.py cut-template        write sword-src/template-cut.png, the cut iron with its crack neutralised, for the paint prompts
  build-warrior-sword.py parallel [check] <tier>...   cut sword-src/parallel-<tier>-<kind>-src.png, painted to the first iron's shape, keeping their own width
  build-warrior-sword.py icons               rebuild only the weapon-warrior-<tier> icons, as Tools/build-item-icons.py does
  build-warrior-sword.py mu [check] <tier>...   cut sword-src/mu-<tier>-src.png, one image holding the normal sword (left) and the enhanced one (right),
                                             each with its own silhouette (MU-style redesign, 2026-11-03): keyed, levelled, scaled so the handle centre
                                             sits on GRIP_ROW, pommel squeezed into the rows left; writes w, h, gripX, gripY. Old sprites go to sword-src/old-<date>/

Painted tiers are fitted to the frame of the cut iron-base.png: same canvas, placed where their alpha overlaps iron's most.

Reads Assets/ArtLibrary/classes/warrior/sword-src/<tier>-<base|enhanced>-src.png. The first run for a tier keeps its old sprites in sword-thin/.
gripX and gripY are the centre of the handle: the fist closes there.
"""
import importlib.util
import json
import math
import re
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/ArtLibrary/classes/warrior"
SPRITES = ROOT / "Assets/Resources/Sprites/classes/warrior/weapons/sword"
GEAR = ROOT / "Assets/Resources/Data/classes/warrior/held-gear.json"
HEIGHT = 262
GRIP_ROW = 217
HANDLE_MAX_W = 20  # rows narrower than this, between guard and pommel, are the handle
BLADE_ROWS = 150  # rows above the guard, where every tier must keep iron's outline
KEYS = {"verdant": "magenta"}  # green art on the magenta key; the rest on green

spec = importlib.util.spec_from_file_location("hs", ROOT / "Tools/build-hero-select-art.py")
hs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hs)


# The Warrior's sword in warrior.png: its tip, and the angle of its axis from horizontal found by levelling both edges.
HERO_ART = ROOT / "Assets/ArtLibrary/custom/hero-select/warrior.png"
TIP = (1108, 1110)
AXIS_DEG = 29.71
GUARD_ROW = 720  # upright rows, in the crop below: where the guard's arms meet the blade
PANEL_TIP_ROW, PANEL_SHOULDER_ROW, PANEL_W_RATIO = 200, 325, 0.54  # read off the upright crop


def upright():  # measuring frame only
    im = Image.open(HERO_ART).convert("RGB")
    return im.rotate(AXIS_DEG + 90, resample=Image.BICUBIC, center=TIP, translate=(627 - TIP[0], 150 - TIP[1])).crop((300, 100, 950, 1000))


def trace(im):
    """Outermost bright steel per row on each side. Neighbouring shield and arm steel give outliers, so the median of a few rows is used."""
    a = np.asarray(im).astype(float)
    steel = ((a.max(2) - a.min(2)) / np.maximum(a.max(2), 1) < 0.2) & (a.mean(2) > 140)
    left, right = {}, {}
    for y in range(60, GUARD_ROW):
        lx, rx = np.flatnonzero(steel[y, 226:325]), np.flatnonzero(steel[y, 330:428])
        if len(lx) and len(rx):
            left[y], right[y] = 226 + lx.min(), 330 + rx.max()
    return left, right


def measure():
    im = upright()
    left, right = trace(im)
    rows = sorted(left)
    width = lambda a, b: float(np.median([right[y] - left[y] for y in rows if a <= y < b]))
    tip_row = 46
    left_sh = next(y for y in rows if y > 200 and left[y + 10] >= left[y] - 1)
    right_sh = next(y for y in rows if y > 200 and right[y + 10] <= right[y] + 1)
    length = GUARD_ROW - tip_row
    m = dict(tip_row=tip_row, length=length, width_top=width(330, 370), width_mid=width(480, 520), width_low=width(590, 620),
             left_shoulder=left_sh, right_shoulder=right_sh)
    m["ratio"] = round(m["width_top"] / length, 3)
    m["ratio_low"] = round(m["width_low"] / length, 3)
    m["tip_fraction"] = round(((left_sh + right_sh) / 2 - tip_row) / length, 3)
    return im, m


def template(m):
    """1024x1536, sword 262*5 px tall with the handle where the sprite needs it. Symmetric: the mock's two shoulders are averaged."""
    k, W, H = 5, 1024, 1536
    cx, top = W / 2, (H - 262 * k) / 2
    pt = lambda x, y: (cx + x * k, top + y * k)
    blade = 174  # rows from the tip to the guard
    hw_top = m["ratio"] * blade / 2
    hw_low = m["ratio_low"] * blade / 2
    sh = m["tip_fraction"] * blade
    side = [(0, 0), (hw_top, sh), (hw_low, blade - 8), (hw_low + 3, blade - 2), (hw_low - 2, blade + 4)]
    im = Image.new("RGB", (W, H), (0, 255, 0))
    d = ImageDraw.Draw(im)
    d.polygon([pt(x, y) for x, y in side] + [pt(-x, y) for x, y in reversed(side)], fill=(205, 205, 212), outline=(0, 0, 0), width=7)
    # The darker centre panel, where the mock has it.
    p0 = (PANEL_TIP_ROW - m["tip_row"]) / m["length"] * blade
    p1 = (PANEL_SHOULDER_ROW - m["tip_row"]) / m["length"] * blade
    pw = hw_low * PANEL_W_RATIO
    d.polygon([pt(0, p0), pt(pw, p1), pt(pw, blade), pt(-pw, blade), pt(-pw, p1)], fill=(95, 95, 100), outline=(30, 30, 30), width=4)
    # Guard, in the mock's width ratio; handle and pommel fixed by the sprite's rows.
    gw = hw_top * 1.44
    guard = [(gw, 178), (gw - 6, 172), (gw - 14, 180), (hw_low, 176), (8, 174), (0, 168), (-8, 174), (-hw_low, 176), (-gw + 14, 180), (-gw + 6, 172), (-gw, 178),
             (-gw + 4, 190), (-gw + 14, 186), (-10, 190), (-8, 197), (8, 197), (10, 190), (gw - 14, 186), (gw - 4, 190)]
    d.polygon([pt(x, y) for x, y in guard], fill=(205, 165, 80), outline=(0, 0, 0), width=6)
    d.rectangle((*pt(-6, 197), *pt(6, 237)), fill=(110, 45, 35), outline=(0, 0, 0), width=6)
    pom = [(0, 262), (10, 250), (9, 242), (6, 237), (-6, 237), (-9, 242), (-10, 250)]
    d.polygon([pt(x, y) for x, y in pom], fill=(205, 165, 80), outline=(0, 0, 0), width=6)
    return im


def make_template():
    im, m = measure()
    print(json.dumps(m))
    src = ART / "sword-src"
    im.crop((180, 20, 470, 880)).resize((580, 1720), Image.LANCZOS).save(src / "mock-blade-upright.png")
    template(m).save(src / "sword-template.png")


# Traced at 6x on the upright sword, clockwise from the tip along the outer edge of the black outline. Coordinates are upright() below.
BLADE_GUARD = [(301, 33), (399, 311), (389, 672), (390, 682), (414, 695), (407, 707), (390, 722), (386, 733), (388, 750), (391, 758),
               (407, 757), (402, 745), (396, 733), (416, 739), (431, 760), (424, 766), (420, 773), (420, 785), (430, 800), (438, 807), (444, 812),
               (438, 818), (425, 838), (422, 850), (408, 866), (398, 869), (395, 863), (387, 843), (373, 828), (360, 822), (330, 820), (326, 826),
               (326, 838), (320, 846), (305, 849), (291, 847), (281, 843), (276, 835), (271, 817), (265, 802), (252, 788), (235, 778), (213, 777),
               (205, 782), (203, 765), (200, 751), (192, 757), (186, 769), (183, 765), (181, 750), (172, 732), (175, 729), (197, 716), (203, 707),
               (213, 704), (215, 698), (198, 672), (213, 642), (219, 633), (214, 622), (214, 610), (204, 255)]
HANDLE_POMMEL = [(283, 951), (332.5, 951), (332.5, 1004), (347, 1025), (349, 1050), (330, 1078), (305, 1084), (280, 1078), (262, 1050), (263, 1025), (283, 1004)]
HANDLE_X = (284, 334)  # columns of the visible handle
HANDLE_SRC = (976, 982)  # its plain dark rows, repeated to fill what the fist hides
HIDDEN = (845, 951)  # rows the fist hides
COLLAR_MID, LOWER_MID = 300.0, 309.0  # handle axis at the top and bottom of the hidden part
HANDLE_TOP, HANDLE_BOTTOM = 847, 1006  # collar's lower edge to the pommel's upper edge
CUT_S = 4


def blade_edges(up):
    """The blade's sides per row, outer edge of the black outline. Edges bow slightly, so they are measured, not drawn straight:
    outermost bright steel near the traced line, a smooth curve through it, then the outline's median thickness, because armour behind can look like outline."""
    a = np.asarray(up.resize((up.width // CUT_S, up.height // CUT_S), Image.LANCZOS)).astype(float)
    rgb, alpha = a[..., :3], a[..., 3]
    steel = ((rgb.max(2) - rgb.min(2)) / np.maximum(rgb.max(2), 1) < 0.22) & (rgb.mean(2) > 135) & (alpha > 128)
    lum = rgb.mean(2)
    line = lambda y, p, q: p[0] + (q[0] - p[0]) * (y - p[1]) / (q[1] - p[1])
    left_seg = lambda y: line(y, (301, 33), (204, 255)) if y < 255 else line(y, (204, 255), (214, 610))
    right_seg = lambda y: line(y, (301, 33), (399, 311)) if y < 311 else line(y, (399, 311), (389, 672))
    sides = {}
    for name, seg, rows, sign in (("left", left_seg, range(44, 610), -1), ("right", right_seg, range(44, 668), 1)):
        pos, thick = [], []
        for y in rows:
            g = seg(y)
            xs = np.arange(int(g) - 8, int(g) + 26) if sign < 0 else np.arange(int(g) - 25, int(g) + 9)
            hit = xs[steel[y, xs]]
            if not len(hit):
                pos.append(np.nan); thick.append(np.nan)
                continue
            e = hit.min() if sign < 0 else hit.max()
            t = 0
            while t < 9 and alpha[y, e + sign * (t + 1)] > 128 and lum[y, e + sign * (t + 1)] < 95:
                t += 1
            pos.append(e); thick.append(t)
        pos, thick = np.array(pos, float), np.array(thick, float)
        rows_a = np.array(list(rows), float)
        corner = 258 if sign < 0 else 315
        fitted = np.zeros(len(pos))
        for part in (rows_a < corner, rows_a >= corner):
            keep = part & ~np.isnan(pos)
            for _ in range(4):  # shield and arm steel behind the edge are outliers: refit without them
                c = np.polyfit(rows_a[keep], pos[keep], 2)
                keep = part & ~np.isnan(pos) & (np.abs(np.polyval(c, rows_a) - pos) < 1.5)
            fitted[part] = np.polyval(c, rows_a[part])
        pos = fitted
        t = float(np.nanmedian(thick))
        sides[name] = [(float(p + sign * t), y) for p, y in zip(pos, rows)]
        print(name, "outline thickness", t)
    return sides


def blade_guard_polygon(up):
    """The traced hilt, with the two long blade edges replaced by their measured rows."""
    sides = blade_edges(up)
    right_end = BLADE_GUARD.index((389, 672))
    left_start = BLADE_GUARD.index((214, 610))
    return [BLADE_GUARD[0]] + sides["right"] + BLADE_GUARD[right_end + 1: left_start + 1] + sides["left"][::-1]


def upright_rgba(s=CUT_S):
    """warrior.png keyed, then rotated about the tip so the blade stands up: tip near (301, 40) in a 600x1160 frame, times s."""
    keyed = hs.to_image(hs.key_out(HERO_ART, hs.GREEN, glow=True))
    big = keyed.resize((keyed.width * s, keyed.height * s), Image.LANCZOS)
    a = math.radians(AXIS_DEG + 90)
    c, sn = math.cos(a), math.sin(a)
    tx, ty = TIP[0] * s, TIP[1] * s
    return big.transform((600 * s, 1160 * s), Image.AFFINE, (c, -sn, tx - c * 300 * s + sn * 40 * s, sn, c, ty - sn * 300 * s - c * 40 * s), resample=Image.BICUBIC)


def polygon_mask(points, size, s=CUT_S):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).polygon([(x * s, y * s) for x, y in points], fill=255)
    return np.asarray(m).astype(np.float32) / 255


def cut_exact(hires_only=False):
    """Blade and guard, handle and pommel are source pixels. Only the handle behind the fist is composited, from the handle's own visible rows."""
    s = CUT_S
    rotated = upright_rgba()
    poly = blade_guard_polygon(rotated)
    up = np.asarray(rotated).astype(np.float32)
    h, w = up.shape[:2]
    out = np.zeros_like(up)
    lower = up.copy()
    lower[..., 3] *= polygon_mask(HANDLE_POMMEL, (w, h))
    # Hidden handle first: mirrored repeats of the visible rows, along the axis from collar to lower handle.
    x0, x1 = HANDLE_X[0] * s, HANDLE_X[1] * s
    y0, y1 = HANDLE_SRC[0] * s, HANDLE_SRC[1] * s
    period = y1 - y0
    for y in range(HIDDEN[0] * s, (HIDDEN[1] + 1) * s):
        phase = (HIDDEN[1] * s - y) % (2 * period)
        src = y0 + (phase if phase < period else 2 * period - 1 - phase)
        t = (y / s - HIDDEN[0]) / (HIDDEN[1] - HIDDEN[0])
        mid = COLLAR_MID + (LOWER_MID - COLLAR_MID) * t
        dx = round((mid - LOWER_MID) * s)
        out[y, x0 + dx: x1 + dx] = lower[src, x0: x1]
    a = lower[..., 3:] / 255
    out = out * (1 - a) + lower * a
    top = up.copy()
    top[..., 3] *= polygon_mask(poly, (w, h))
    a = top[..., 3:] / 255
    rgb_alpha = top[..., 3] + out[..., 3] * (1 - a[..., 0])
    rgb = (top[..., :3] * a + out[..., :3] * (out[..., 3:] / 255) * (1 - a)) / np.maximum(rgb_alpha[..., None] / 255, 1e-3)
    full = np.dstack([np.clip(rgb, 0, 255), rgb_alpha])

    ys, xs = np.nonzero(full[..., 3] > 8)
    part = full[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
    if hires_only:
        return part
    origin_y, origin_x = ys.min() / s, xs.min() / s
    # One scale for the whole sword, keeping its proportions: the handle centre lands on GRIP_ROW; the rows left above HEIGHT stay empty.
    centre = (HANDLE_TOP + HANDLE_BOTTOM) / 2
    k = GRIP_ROW / (centre - origin_y)
    size = (round(part.shape[1] / s * k), round(part.shape[0] / s * k))
    small = hs.resize(part, size)
    canvas = np.zeros((HEIGHT, size[0], 4), np.float32)
    canvas[: size[1]] = small[: HEIGHT]
    axis = LOWER_MID + (COLLAR_MID - LOWER_MID) * (HIDDEN[1] - centre) / (HIDDEN[1] - HIDDEN[0])
    grip_entry = {"w": size[0], "h": HEIGHT, "gripX": round((axis - origin_x) * k), "gripY": GRIP_ROW}
    print("scale", round(k, 4), "art rows", size[1], "entry", grip_entry)
    return canvas, grip_entry


def write_cut_template():
    """1024x1536 on the green key, sword 1314 px tall. The panel's red is greyed so the prompts do not hand every tier iron's crack."""
    part = cut_exact(hires_only=True)
    k = 1314 / part.shape[0]
    art = hs.resize(part, (round(part.shape[1] * k), 1314))
    rgb, alpha = art[..., :3].copy(), art[..., 3] / 255
    panel = np.zeros(alpha.shape, bool)
    x0, x1 = round(art.shape[1] * 0.28), round(art.shape[1] * 0.72)
    panel[:round(1314 * 0.70), x0:x1] = True
    red = panel & (rgb[..., 0] - rgb[..., 1] > 45) & (rgb[..., 0] > rgb[..., 2])
    rgb[red] = (72, 70, 72)
    out = np.zeros((1536, 1024, 3), np.float32)
    out[...] = (0, 255, 0)
    y, x = (1536 - 1314) // 2, (1024 - art.shape[1]) // 2
    a = alpha[..., None]
    out[y: y + 1314, x: x + art.shape[1]] = rgb * a + out[y: y + 1314, x: x + art.shape[1]] * (1 - a)
    # A flat panel down to the guard's spike, then only the leftover red around it. Read off this template at 1024x1536.
    im = Image.fromarray(out.round().astype(np.uint8))
    d = ImageDraw.Draw(im)
    d.polygon([(487, 322), (548, 482), (547, 880), (440, 880), (436, 450)], fill=(104, 96, 94))
    arr = np.asarray(im).astype(np.int32)
    low = np.zeros(arr.shape[:2], bool)
    low[880:1030, 425:565] = True
    near = ndimage.binary_dilation(low & (arr[..., 0] - arr[..., 1] > 60) & (arr[..., 1] < 110), iterations=2) & low
    arr[near] = (60, 50, 50)
    Image.fromarray(arr.astype(np.uint8)).save(ART / "sword-src/template-cut.png")


def write_icons():
    """Same steps as overlay() and fit() in build-item-icons.py, which rebuilds every class's icons when run."""
    names = [e["name"] for e in json.loads(GEAR.read_text(encoding="utf-8"))["sword"]]
    out = ROOT / "Assets/Resources/Sprites/icons"
    for tier, name in enumerate(names):
        im = Image.open(SPRITES / f"{name}-base.png").convert("RGBA").rotate(-45, expand=True, resample=Image.BICUBIC)
        im = im.crop(im.getbbox())
        k = 88 / max(im.width, im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        icon = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        icon.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
        icon.save(out / f"weapon-warrior-{tier}.png")


def write_exact():
    canvas, entry = cut_exact()
    for kind in ("base", "enhanced"):
        hs.to_image(canvas).save(SPRITES / f"iron-{kind}.png")
    update_gear({"iron": {"base": entry, "enhanced": entry}})


def handle_rows(solid, max_w):
    """Top and bottom row of the narrow run between guard and pommel, walking up from the pommel's widest row."""
    h = solid.shape[0]
    widths = solid.sum(axis=1)
    start = h - h // 6 + int(np.argmax(widths[h - h // 6:]))
    rows = [y for y in range(start, 0, -1) if widths[y] <= max_w]
    run = [rows[0]]
    for y in rows[1:]:
        if y != run[-1] - 1:
            break
        run.append(y)
    return run[-1], run[0]


def iron_cut():
    return np.asarray(Image.open(SPRITES / "iron-base.png").convert("RGBA")).astype(np.float32)


def cut(tier, kind, iron):
    """Resize and place the painted sword on iron's canvas where its alpha overlaps iron's most, keeping its proportions within the search."""
    a = hs.keep_largest(hs.key_out(ART / f"sword-src/{tier}-{kind}-src.png", KEYS.get(tier, hs.GREEN)))
    ys, xs = np.nonzero(a[..., 3] > 128)
    part = a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
    target = iron[..., 3] > 128
    H, W = target.shape
    ty, tx = np.nonzero(target)
    tw, th = int(tx.max() - tx.min() + 1), int(ty.max() - ty.min() + 1)
    mask = Image.fromarray((part[..., 3] > 128).astype(np.uint8) * 255)
    best = (-1, None)
    for w in range(tw - 6, tw + 5):
        for h in range(th - 6, th + 5):
            m = np.asarray(mask.resize((w, h), Image.BILINEAR)) > 127
            for dy in range(int(ty.min()) - 4, int(ty.min()) + 5):
                for dx in range(int(tx.min()) - 4, int(tx.min()) + 5):
                    x0, y0, x1, y1 = max(dx, 0), max(dy, 0), min(dx + w, W), min(dy + h, H)
                    if x1 <= x0 or y1 <= y0:
                        continue
                    inter = int((m[y0 - dy: y1 - dy, x0 - dx: x1 - dx] & target[y0:y1, x0:x1]).sum())
                    iou = inter / (int(m.sum()) + int(target.sum()) - inter)
                    if iou > best[0]:
                        best = (iou, (w, h, dx, dy))
    iou, (w, h, dx, dy) = best
    small = hs.resize(part, (w, h))
    canvas = np.zeros((H, W, 4), np.float32)
    x0, y0, x1, y1 = max(dx, 0), max(dy, 0), min(dx + w, W), min(dy + h, H)
    canvas[y0:y1, x0:x1] = small[y0 - dy: y1 - dy, x0 - dx: x1 - dx]
    return canvas, dict(iou=round(iou, 3), fit=[w, h, dx, dy], lost=round(float(small[..., 3].sum() - canvas[..., 3].sum()) / 255))


def grip(rgba):
    solid = rgba[..., 3] > 128
    y0, y1 = handle_rows(solid, HANDLE_MAX_W)
    mid = (y0 + y1) // 2
    cols = np.flatnonzero(solid[mid])
    return int(cols.min() + cols.max() + 1) // 2, (y0 + y1 + 1) // 2, y1 - y0 + 1, int(cols.max() - cols.min() + 1)


def row_devs(rgba, iron):
    """Largest difference of the left and right edge per row, for the blade rows and for all rows."""
    a, b = rgba[..., 3] > 128, iron[..., 3] > 128
    devs = {}
    for y in range(a.shape[0]):
        xa, xb = np.flatnonzero(a[y]), np.flatnonzero(b[y])
        if len(xa) and len(xb):
            devs[y] = max(abs(int(xa.min()) - int(xb.min())), abs(int(xa.max()) - int(xb.max())))
        elif len(xa) or len(xb):
            devs[y] = 99
    blade = [devs[y] for y in devs if y < BLADE_ROWS]
    return max(blade), max(devs.values()), sum(1 for y in devs if devs[y] > 2)


def report(rgba, iron, extra):
    gx, gy, length, handle_w = grip(rgba)
    blade, whole, over = row_devs(rgba, iron)
    return dict(w=rgba.shape[1], h=rgba.shape[0], gripX=gx, gripY=gy, handle=length, handle_w=handle_w, blade_dev=blade, all_dev=whole, rows_over_2=over, **extra)


def cut_parallel(tier, kind):
    """Their own proportions: one scale from tip to handle so the handle centre lands near GRIP_ROW, the pommel takes the rest of HEIGHT."""
    a = hs.keep_largest(hs.key_out(ART / f"sword-src/parallel-{tier}-{kind}-src.png", KEYS.get(tier, hs.GREEN)))
    ys, xs = np.nonzero(a[..., 3] > 128)
    part = a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
    solid = part[..., 3] > 128
    top, bottom = handle_rows(solid, round(HANDLE_MAX_W * part.shape[0] / HEIGHT))
    k = GRIP_ROW / ((top + bottom + 1) / 2)
    w = round(part.shape[1] * k)
    upper_h = round((bottom + 1) * k)
    upper = hs.resize(part[: bottom + 1], (w, upper_h))
    lower = hs.resize(part[bottom + 1:], (w, HEIGHT - upper_h))
    return np.vstack([upper, lower])


def main_parallel(tiers, write):
    ref = np.asarray(Image.open(ART / "sword-src/parallel-iron-base.png").convert("RGBA")).astype(np.float32)
    ref_gx = grip(ref)[0]
    entries = {}
    for tier in tiers:
        done = {}
        for kind in ("base", "enhanced"):
            rgba = cut_parallel(tier, kind)
            gx, gy, length, handle_w = grip(rgba)
            pad = np.zeros((HEIGHT, 140, 4), np.float32)  # compare edges with both swords' handles on one column
            pad_ref = pad.copy()
            pad[:, 70 - gx: 70 - gx + rgba.shape[1]] = rgba
            pad_ref[:, 70 - ref_gx: 70 - ref_gx + ref.shape[1]] = ref
            blade, whole, over = row_devs(pad, pad_ref)
            solid = rgba[..., 3] > 128
            r = dict(w=rgba.shape[1], h=rgba.shape[0], gripX=gx, gripY=gy)
            print(tier, kind, json.dumps(r), "handle", length, "handle_w", handle_w, "blade_dev", blade, "all_dev", whole, "rows_over_2", over,
                  "centre_col_at_grip", (np.flatnonzero(solid[gy]).min() + np.flatnonzero(solid[gy]).max() + 1) / 2)
            done[kind] = (rgba, r)
        if write:
            for kind, (rgba, _) in done.items():
                keep = ART / f"sword-thin/{tier}-{kind}.png"
                if not keep.exists():
                    shutil.copy2(SPRITES / f"{tier}-{kind}.png", keep)
                hs.to_image(rgba).save(SPRITES / f"{tier}-{kind}.png")
            entries[tier] = {kind: r for kind, (_, r) in done.items()}
    if write and entries:
        update_gear(entries)


def split_pair(rgba):
    """Left and right sword of a two-sword sheet: the widest empty column band near the middle separates them. Specks are already dropped by key_out."""
    cols = rgba[..., 3].max(axis=0) > 8
    w = len(cols)
    gaps, start = [], None
    for x in range(w):
        if not cols[x] and start is None:
            start = x
        elif cols[x] and start is not None:
            gaps.append((start, x)); start = None
    mid = [(a, b) for a, b in gaps if a > w * 0.25 and b < w * 0.75]
    if not mid:
        raise SystemExit("no empty column between the two swords")
    a, b = max(mid, key=lambda g: g[1] - g[0])
    cut = (a + b) // 2
    return rgba[:, :cut], rgba[:, cut:]


def level(rgba):
    """Rotates the sword so its long axis stands vertical, found from the alpha's principal axis. Small tilts only; the art is drawn upright."""
    ys, xs = np.nonzero(rgba[..., 3] > 128)
    cov = np.cov(np.vstack([xs - xs.mean(), ys - ys.mean()]))
    val, vec = np.linalg.eigh(cov)
    vx, vy = vec[:, int(np.argmax(val))]
    deg = math.degrees(math.atan2(vx, vy))  # 0 when the long axis is vertical
    if deg > 90:
        deg -= 180
    if deg < -90:
        deg += 180
    if abs(deg) < 0.3:
        return rgba, 0.0
    im = hs.to_image(rgba).rotate(-deg, resample=Image.BICUBIC, expand=True)
    return np.asarray(im).astype(np.float32), deg


def handle_run(solid):
    """Rows of the plain grip, between the guard and the pommel: the guard is the widest row in the lower half, the grip the narrowest row
    below it (the pommel's rounded end, the last 8% of rows, does not count), and the run is every row around that narrowest one
    no wider than 1.3 times it. Returns (top, bottom) inclusive."""
    h = solid.shape[0]
    widths = solid.sum(axis=1)
    lo = int(h * 0.5)
    guard = lo + int(np.argmax(widths[lo: int(h * 0.92)]))
    tail = int(h * 0.08)
    narrowest = guard + int(np.argmin(np.where(widths[guard: h - tail] > 0, widths[guard: h - tail], 10 ** 6)))
    limit = widths[narrowest] * 1.3
    top = narrowest
    while top > guard and 0 < widths[top - 1] <= limit:
        top -= 1
    bottom = narrowest
    while bottom < h - 1 and 0 < widths[bottom + 1] <= limit:
        bottom += 1
    return top, bottom


def cut_mu_one(rgba):
    """One sword, its own silhouette: one scale from tip to grip centre so that centre lands on GRIP_ROW; what hangs below (grip bottom, pommel)
    keeps that scale when it fits, else is squeezed into the rows left, as the parallel cut does."""
    rgba, deg = level(rgba)
    ys, xs = np.nonzero(rgba[..., 3] > 8)
    part = rgba[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
    solid = part[..., 3] > 128
    top, bottom = handle_run(solid)
    centre = (top + bottom + 1) / 2
    k = GRIP_ROW / centre
    w = round(part.shape[1] * k)
    upper_h = round((bottom + 1) * k)
    upper = hs.resize(part[: bottom + 1], (w, upper_h))
    rest = part.shape[0] - (bottom + 1)
    lower_h = min(HEIGHT - upper_h, round(rest * k))
    lower = hs.resize(part[bottom + 1:], (w, lower_h))
    canvas = np.zeros((HEIGHT, w, 4), np.float32)
    canvas[:upper_h] = upper
    canvas[upper_h: upper_h + lower_h] = lower
    mid = int(round(centre * k))
    cols = np.flatnonzero(canvas[mid, :, 3] > 128)
    entry = {"w": w, "h": HEIGHT, "gripX": int(cols.min() + cols.max() + 1) // 2, "gripY": GRIP_ROW}
    info = dict(tilt=round(deg, 2), src_h=int(part.shape[0]), src_w=int(part.shape[1]), scale=round(k, 4), handle_rows=[int(round(top * k)), int(round(bottom * k))],
                handle_w=int(cols.max() - cols.min() + 1), pommel_squeeze=round(lower_h / max(rest * k, 1), 3), art_rows=upper_h + lower_h)
    return canvas, entry, info


def main_mu(tiers, write):
    from datetime import date
    backup = ART / f"sword-src/old-{date.today():%Y%m%d}"
    entries = {}
    for tier in tiers:
        src = ART / f"sword-src/mu-{tier}-src.png"
        if not src.exists():
            print(tier, "no source yet"); continue
        sheet = hs.key_out(src, KEYS.get(tier, hs.GREEN))
        done = {}
        for kind, half in zip(("base", "enhanced"), split_pair(sheet)):
            rgba, entry, info = cut_mu_one(half)
            print(tier, kind, json.dumps(entry), json.dumps(info))
            done[kind] = (rgba, entry)
        if write:
            backup.mkdir(exist_ok=True)
            for kind, (rgba, entry) in done.items():
                old = SPRITES / f"{tier}-{kind}.png"
                if old.exists() and not (backup / old.name).exists():
                    shutil.copy2(old, backup / old.name)
                hs.to_image(rgba).save(old)
            entries[tier] = {kind: entry for kind, (_, entry) in done.items()}
    if write and entries:
        update_gear(entries)


def update_gear(entries):
    """Only baseGrip and enhancedGrip of these tiers in the sword list; the shield list has the same names."""
    text = GEAR.read_text(encoding="utf-8")
    head, sep, tail = text.partition('"shield":[')
    for tier, e in entries.items():
        new = lambda m: m.group(1) + json.dumps(e["base"], separators=(",", ":")) + ',"enhancedGrip":' + json.dumps(e["enhanced"], separators=(",", ":")) + "}"
        head, n = re.subn(r'(\{"name":"' + tier + r'","glow":"#[0-9a-f]+","baseGrip":)\{[^}]*\},"enhancedGrip":\{[^}]*\}\}', new, head, count=1)
        if n != 1:
            raise SystemExit(f"{tier} entry not found")
    GEAR.write_text(head + sep + tail, encoding="utf-8")


def main(tiers, write):
    iron = iron_cut()
    iron_report = report(iron, iron, {})
    print("iron", json.dumps(iron_report))
    # iron's entry is its true handle centre; the row-width measure reads a little lower because it counts the collar and pommel rim.
    entry_iron = json.loads(GEAR.read_text(encoding="utf-8"))["sword"][0]["baseGrip"]
    off_x, off_y = entry_iron["gripX"] - iron_report["gripX"], entry_iron["gripY"] - iron_report["gripY"]
    entries = {}
    for tier in tiers:
        done = {}
        for kind in ("base", "enhanced"):
            if not (ART / f"sword-src/{tier}-{kind}-src.png").exists():
                print(tier, kind, "no original yet")
                continue
            rgba, extra = cut(tier, kind, iron)
            r = report(rgba, iron, extra)
            r["gripX"] += off_x
            r["gripY"] += off_y
            print(tier, kind, json.dumps(r))
            done[kind] = (rgba, {k: r[k] for k in ("w", "h", "gripX", "gripY")})
        if write and len(done) == 2:
            for kind, (rgba, entry) in done.items():
                keep = ART / f"sword-thin/{tier}-{kind}.png"
                if not keep.exists():
                    shutil.copy2(SPRITES / f"{tier}-{kind}.png", keep)
                hs.to_image(rgba).save(SPRITES / f"{tier}-{kind}.png")
            entries[tier] = {kind: entry for kind, (_, entry) in done.items()}
    if write and entries:
        update_gear(entries)


if __name__ == "__main__":
    args = sys.argv[1:]
    if args == ["template"]:
        make_template()
    elif args == ["iron-cut"]:
        write_exact()
    elif args[:1] == ["parallel"]:
        if args[1:2] == ["check"]:
            main_parallel(args[2:], False)
        else:
            main_parallel(args[1:], True)
    elif args[:1] == ["mu"]:
        if args[1:2] == ["check"]:
            main_mu(args[2:], False)
        else:
            main_mu(args[1:], True)
    elif args == ["cut-template"]:
        write_cut_template()
    elif args == ["icons"]:
        write_icons()
    elif args[:1] == ["check"]:
        main(args[1:], False)
    else:
        main(args, True)

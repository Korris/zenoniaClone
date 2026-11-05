"""Helpers for Tools/build-mechanic-idle-doll-from-chatgpt.py: keying the ChatGPT panels, fitting a figure into the 192 px idle cell the way the
current row stands (head top to sole), breathing, landmark masks (cannon lens / tail cap, hair width) and the game composite via
Tools/compose-mechanic-cannon-check.py. Pillow, numpy, scipy; run from the project root."""
import importlib.util
import math
import os

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

RES = "Assets/Resources"
SPR = f"{RES}/Sprites/classes/mechanic"
BODY = f"{SPR}/body"
GEAR = f"{RES}/Data/classes/mechanic/held-mechanic-dual.json"
SRC = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src/cannon-back-lr"
FRAME = 192
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
DIRS = ("down", "up", "left", "right")
GROW = (0, 1, 2, 1)  # px the figure grows from the sole in frames 0-3 (current rows are 164/165/166/165 tall)
FIST_R = 9


def key_green(path):
    """Premultiplied RGBA floats from art on flat #00FF00 (build-idle-down-doll-from-redraw.key_green)."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(fg, 1 + np.where(sizes >= 400)[0]), iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def to_image(pm):
    a = np.maximum(pm[..., 3:4], 1e-6)
    rgb = np.clip(pm[..., :3] / a * 255, 0, 255)
    return Image.fromarray(np.dstack([rgb, np.clip(pm[..., 3], 0, 255)]).round().astype(np.uint8), "RGBA")


def panels(pm):
    """The figures of a two-panel sheet, split at the widest empty column gap in the middle half; a one-figure sheet gives one panel.
    Each panel is (array, x0): the full-height slice and its left column in the sheet."""
    cols = pm[..., 3].max(0) > 128
    w = len(cols)
    best, run, start = (0, 0), 0, 0
    for x in range(w // 4, 3 * w // 4):
        run = run + 1 if not cols[x] else 0
        if run > best[0]:
            best, start = (run, x), x - run + 1
    if best[0] < 20:
        return [(pm, 0)]
    split = start + best[0] // 2
    return [(pm[:, :split], 0), (pm[:, split:], split)]


def metrics(pm):
    """Head top, sole row and boot centre column of one keyed figure. Head top is read in the columns above the boots so a cannon muzzle
    sticking out past the hair does not count."""
    a = pm[..., 3] > 128
    sole = np.where(a.any(1))[0][-1]
    boots = np.where(a[sole - 4:sole + 1].any(0))[0]
    centre = (boots[0] + boots[-1]) / 2
    cols = np.where(a.any(0))[0]
    half = max(40, int(0.2 * (cols[-1] - cols[0])))
    head = np.where(a[:, int(centre) - half:int(centre) + half].any(1))[0][0]
    return dict(head=int(head), sole=int(sole), centre=float(centre))


def current_metrics(direction):
    """Head top, sole and boot centre of the current bare idle frame 0 of that row (the figure the new one must stand like)."""
    r = ROWS[direction]
    # Always measure the ORIGINAL rows (backed up by `write` once): measuring the sheet this tool wrote drifts the figure on every rerun
    # (the new cape touches the sole rows and moves the "boot centre" by ~20 px), leaving the FISTS table pointing beside the gloves.
    src = f"{SRC}/old/bare-idle.png"
    if not os.path.exists(src):
        src = f"{BODY}/bare/idle.png"
    a = np.asarray(Image.open(src).convert("RGBA"))[r * FRAME:(r + 1) * FRAME, :FRAME, 3] > 128
    sole = np.where(a.any(1))[0][-1]
    head = np.where(a[:, 76:116].any(1))[0][0]
    boots = np.where(a[sole - 2:sole + 1].any(0))[0]
    return dict(head=int(head), sole=int(sole), centre=(boots[0] + boots[-1]) / 2)


class Fit:
    """Source px -> frame px of one figure: scale k so head-to-sole matches the current row, sole on the current sole row, boots centred
    on the current boot centre."""

    def __init__(self, pm, target):
        self.pm, self.m, self.t = pm, metrics(pm), target
        self.k = (target["sole"] - target["head"]) / (self.m["sole"] - self.m["head"])

    def to_frame(self, x, y):
        return (self.t["centre"] + (x - self.m["centre"]) * self.k, self.t["sole"] + (y - self.m["sole"]) * self.k)

    def figure(self):
        """The whole figure resized, and the frame position of its top-left corner (floats)."""
        a = self.pm[..., 3] > 128
        rows, cols = np.where(a.any(1))[0], np.where(a.any(0))[0]
        fig = to_image(self.pm[rows[0]:rows[-1] + 1, cols[0]:cols[-1] + 1])
        fig = fig.resize((max(1, round(fig.width * self.k)), max(1, round(fig.height * self.k))), Image.LANCZOS)
        fx, fy = self.to_frame(cols[0], rows[0])
        return fig, fx, fy

    def cell(self, grow=0):
        """One 192 px cell with the figure standing on the sole row, grown `grow` px upward from the sole (breathing)."""
        fig, fx, fy = self.figure()
        if grow:
            fig = fig.resize((fig.width, fig.height + grow), Image.LANCZOS)
        cell = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        cell.paste(fig, (round(fx), self.t["sole"] + 1 - fig.height), fig)
        return cell

    def breathe_y(self, y, grow):
        """A frame-0 anchor row moved like the stretched figure (the sole stays, everything above rises in proportion)."""
        h = self.t["sole"] + 1 - self.t["head"]
        return self.t["sole"] + 1 - (self.t["sole"] + 1 - y) * (h + grow) / h


def blobs(mask, min_px):
    """Centroid (x, y) and size of every connected region of `mask` with at least min_px pixels, biggest first."""
    lab, n = ndimage.label(mask)
    out = []
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(xs) >= min_px:
            out.append(((xs.mean(), ys.mean()), len(xs)))
    return sorted(out, key=lambda t: -t[1])


def lens_mask(pm):
    """Glowing blue lens pixels (cannon muzzle, goggles, pistol tubes) of a premultiplied or straight RGBA array."""
    im = np.asarray(to_image(pm) if pm.dtype != np.uint8 else pm).astype(int)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    return (a > 128) & (b > 190) & (r < 140) & (g > 110)


def cap_mask(pm):
    """Grey metal pixels (the cannon's tail cap, pistol barrels)."""
    im = np.asarray(to_image(pm) if pm.dtype != np.uint8 else pm).astype(int)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    # the caps are a warm pinkish grey (about 185, 150, 150): red a little above green and blue, green and blue alike
    return (a > 128) & (r > 120) & (r < 245) & (np.abs(g - b) < 22) & (r - g > 8) & (r - g < 60)


def fist_mask(cell):
    """Dark brown glove leather of a 192 px cell (the fists), used to mark fist candidates on the grid."""
    im = np.asarray(cell).astype(int)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    return (a > 128) & (r > 55) & (r < 135) & (g > 25) & (g < 80) & (b < 60) & (r - g > 25) & (g >= b)


def hair_width(cell, top_rows=70):
    """Widest row of yellow hair in the top rows of a 192 px cell, and the row it is on."""
    im = np.asarray(cell).astype(int)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    hair = (a > 128) & (r > 190) & (g > 130) & (b < 120) & (r - b > 100)
    hair[top_rows + 20:] = False
    best = (0, 0)
    for y in range(hair.shape[0]):
        xs = np.nonzero(hair[y])[0]
        if len(xs) and xs[-1] - xs[0] + 1 > best[0]:
            best = (int(xs[-1] - xs[0] + 1), y)
    return best


def width_at(cell, y):
    xs = np.nonzero(np.asarray(cell)[y, :, 3] > 128)[0]
    return int(xs[-1] - xs[0] + 1) if len(xs) else 0


def fist_disc(cell, fists):
    """The cell's pixels inside radius FIST_R of every fist centre (the hand layer)."""
    arr = np.asarray(cell)
    yy, xx = np.mgrid[:FRAME, :FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for cx, cy in fists:
        m |= (xx - cx) ** 2 + (yy - cy) ** 2 <= FIST_R ** 2
    out = np.zeros_like(arr)
    out[m] = arr[m]
    return Image.fromarray(out, "RGBA")


def load_composer():
    spec = importlib.util.spec_from_file_location("cm", "Tools/compose-mechanic-cannon-check.py")
    cm = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(cm)
    return cm


def compose_with(cm, cells, file, anchors, direction, frame, tier_i, pad):
    """compose() of the check tool with the bare and hand cells of `cells[direction][frame]` (bare, hand) instead of the sheets on disk."""
    original = cm.cell

    def cell(path, row, f, mirror=False):
        if "/body/bare/" in path:
            return cells[direction][f][0]
        if "/body/hand/" in path:
            return cells[direction][f][1]
        return original(path, row, f, mirror)

    cm.cell = cell
    try:
        return cm.compose(file, anchors, "idle", direction, frame, tier_i, pad)
    finally:
        cm.cell = original


def direction_of(a, b):
    """Degrees clockwise from +x (y down) of the vector a -> b."""
    return math.degrees(math.atan2(b[1] - a[1], b[0] - a[0]))


def draw_grid(dr, ox, z):
    for g in range(0, FRAME, 10):
        dr.line((ox + g * z, 0, ox + g * z, FRAME * z), fill=(255, 255, 255, 70 if g % 50 else 140))
        dr.line((ox, g * z, ox + FRAME * z, g * z), fill=(255, 255, 255, 70 if g % 50 else 140))
        if g % 20 == 0:
            dr.text((ox + g * z + 2, 2), str(g), fill=(255, 255, 0, 255))
            dr.text((ox + 2, g * z + 2), str(g), fill=(255, 255, 0, 255))


def mark(dr, ox, z, pts, colour, name):
    for (x, y), n in pts:
        px, py = ox + x * z, y * z
        dr.ellipse((px - 6, py - 6, px + 6, py + 6), outline=colour, width=2)
        dr.text((px + 8, py - 6), f"{name} {x:.0f},{y:.0f} n{n}", fill=colour)


def contact_sheet(out, cells, anchors, armed_cells, cm, file):
    """OUT/check.png: per view the 4 composed frames, the armed drawing fitted to the row and the current frame 0; plus 4x zooms per view."""
    z, labels = 2, ("new f0", "new f1", "new f2", "new f3", "chatgpt armed", "current armed f0", "current bare f0")
    sheet = Image.new("RGBA", (len(labels) * FRAME, 4 * FRAME), (88, 120, 76, 255))
    cur_armed = Image.open(f"{BODY}/armed/idle.png").convert("RGBA")
    cur_bare = Image.open(f"{BODY}/bare/idle.png").convert("RGBA")
    for row, d in enumerate(DIRS):
        for f in range(4):
            sheet.alpha_composite(compose_with(cm, cells, file, anchors, d, f, 0, 0), (f * FRAME, row * FRAME))
        sheet.alpha_composite(armed_cells[d], (4 * FRAME, row * FRAME))
        r = ROWS[d]
        sheet.alpha_composite(cur_armed.crop((0, r * FRAME, FRAME, (r + 1) * FRAME)), (5 * FRAME, row * FRAME))
        sheet.alpha_composite(cur_bare.crop((0, r * FRAME, FRAME, (r + 1) * FRAME)), (6 * FRAME, row * FRAME))
    dr = ImageDraw.Draw(sheet)
    for i, label in enumerate(labels):
        dr.text((i * FRAME + 4, 4), label, fill=(255, 255, 0, 255))
    for row, d in enumerate(DIRS):
        pair = sheet.crop((0, row * FRAME, FRAME, (row + 1) * FRAME))
        big = Image.new("RGBA", (2 * FRAME, FRAME), (88, 120, 76, 255))
        big.alpha_composite(pair)
        big.alpha_composite(sheet.crop((4 * FRAME, row * FRAME, 5 * FRAME, (row + 1) * FRAME)), (FRAME, 0))
        big.resize((big.width * 4, big.height * 4), Image.NEAREST).convert("RGB").save(f"{out}/check-zoom-{d}.png")
    sheet = sheet.resize((sheet.width * z, sheet.height * z), Image.NEAREST)
    sheet.convert("RGB").save(f"{out}/check.png")
    print("check", f"{out}/check.png", "missing art:", sorted(cm.MISSING) or "none")


# ---- colour match: ChatGPT paints the leather redder than the walk rows (user: "walk nâu, idle đỏ", 2026-11-05) ----
LEATHER_ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}


def _hsv(rgb):
    mx, mn = rgb.max(2), rgb.min(2)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    d = np.maximum(mx - mn, 1e-6)
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0), mx


def leather_mean(cell):
    """Mean colour of the brown leather (hue 14-38, saturated, mid-dark) of a 192 px RGBA cell."""
    a = np.asarray(cell).astype(float)
    h, sat, v = _hsv(a[..., :3] / 255)
    sel = (a[..., 3] > 200) & (h > 14) & (h < 38) & (sat > 0.35) & (v > 0.15) & (v < 0.75)
    return a[..., :3][sel].mean(0) if sel.sum() > 50 else None


def match_leather(cell, direction):
    """Scale the brown/orange hues of `cell` so their leather mean matches the walk row's (frame 0) of the same direction."""
    row = LEATHER_ROWS[direction]
    walk = Image.open(f"{BODY}/bare/walk.png").convert("RGBA").crop((0, row * FRAME, FRAME, (row + 1) * FRAME))
    ref, cur = leather_mean(walk), leather_mean(cell)
    if ref is None or cur is None:
        return cell
    gain = np.clip(ref / np.maximum(cur, 1), 0.8, 1.25)
    a = np.asarray(cell).astype(float)
    h, sat, v = _hsv(a[..., :3] / 255)
    sel = (a[..., 3] > 0) & (h > 8) & (h < 45) & (sat > 0.25)  # leather, boots, gloves; not the red cape, not the yellow hair, not skin
    out = a.copy()
    out[..., :3][sel] = np.clip(a[..., :3][sel] * gain, 0, 255)
    return Image.fromarray(out.round().astype(np.uint8), "RGBA")


# ---- grip fists: ChatGPT will not redraw an empty hand as a gripping fist (fingers wrapped, seen from the finger side; it draws the
# knuckle side every time, 3 tries on 2026-11-05). So the fitted ARMED drawing lends its fists: inside a disc around each fist only the
# dark glove / outline pixels are kept (the pistol is gold, silver, blue and light wood), and they are pasted over the bare cell.
GRAFT_R = 12


def graft_fists(bare_cell, armed_cell, fists, r=GRAFT_R):
    """bare_cell with the gripping fists of armed_cell pasted at `fists` (frame px); returns the new cell and the pasted-pixels mask."""
    b = np.asarray(bare_cell).astype(np.uint8).copy()
    a = np.asarray(armed_cell).astype(np.uint8)
    rgb = a[..., :3].astype(int)
    dark = (rgb.max(2) < 120) & (a[..., 3] > 0)  # glove leather and outlines; gold / silver / blue / wood of the pistol are brighter
    yy, xx = np.mgrid[:FRAME, :FRAME]
    pasted = np.zeros((FRAME, FRAME), bool)
    for cx, cy in fists:
        disc = (xx - cx) ** 2 + (yy - cy) ** 2 <= r * r
        # fill small gaps inside the disc (highlights on the knuckles) so the fist stays one solid blob
        m = ndimage.binary_closing(dark & disc, iterations=2) & disc & (a[..., 3] > 0)
        lab, n = ndimage.label(m)  # keep the fist only: the pistol's outline arcs inside the disc are smaller fragments
        if n > 1:
            sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
            m = lab == (1 + int(np.argmax(sizes)))
        m = ndimage.binary_fill_holes(m)  # knuckle highlights inside the glove
        pasted |= m
    b[pasted] = a[pasted]
    return Image.fromarray(b, "RGBA"), pasted

"""Sunken Reliquary 58x24: the Mire Hydra Queen's round arena (about 70 floor tiles, far too small for a 30-40 player Wanted fight) becomes a rounded
arena of 19x15 tiles on 22 new columns east of the old map. No new art: everything is re-cut from the original 36x24 painting.

Usage: python Tools/build-sunken-reliquary-wide-arena.py [--rows]
 Source: Assets/ArtLibrary/maps/sunken-reliquary/sunken-reliquary-ground-36x24-original.png (the untouched old ground, NOT the RO-graded one).
 Output: Assets/Resources/Sprites/village/tiles/sunken-reliquary-ground-r0c0.png / -r0c1.png (2 chunks of 1856x1536, 29 tiles each, under the 2048 px
 Standalone import cap so nothing is downscaled) + -minimap.png (348x144). The output is UNGRADED: run Tools/bake-map-ro-soft-tone.py apply afterwards
 (this script also refreshes the tone tool's backup of the original, so a later revert gives back this ungraded wide map).
 --rows prints the 58-column collision rows for SunkenReliquaryMap.cs (same geometry as the painting).

How the arena is built (all in the ring's "round" space, y scaled by K because the painted ring is a little squashed):
 * the 4 quarters of the old ring are kept pixel for pixel as the corners of the new arena;
 * the straight walls between them are the ring unrolled: each pixel samples the old ring at the same distance from the wall line, at an angle that
   swings back and forth (triangle wave) around the matching side of the old circle, so the bricks keep their size and lighting;
 * the floor is flagstone patches quilted from the big hall, colour-matched to the arena floor, with the old medallion (x1.25) in the middle;
 * water: patches quilted from open water; the jungle bands at the top and bottom are copied from further west;
 * a 2-tile causeway (rows 15-16) replaces the old 1-tile bridge from the hall: the old causeway's curbs and bricks stretched to the new width.
"""
import os, sys, shutil
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "sunken-reliquary", "sunken-reliquary-ground-36x24-original.png")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
BACKUP = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "ro-soft-originals~", "Resources", "Sprites", "village", "tiles")
NAME = "sunken-reliquary-ground"
T = 64
OLD_COLS, COLS, ROWS = 36, 58, 24
W, H = COLS * T, ROWS * T

# Old ring (measured on the painting): centre, y squash, inner floor edge and outer wall radius in round space
CX, CY, K = 1919.0, 872.0, 1.08
R_IN, R_WALL, R_FADE0, R_FADE1 = 300.0, 362.0, 360.0, 378.0
# New arena: floor tiles x 35..53, y 5..19 (inclusive)
FX0, FX1, FY0, FY1 = 35, 54, 5, 20
NCX, NCY = (FX0 + FX1) * T / 2.0, (FY0 + FY1) * T / 2.0
BX = (FX1 - FX0) * T / 2.0 - R_IN              # half extent of the box of corner centres (round space)
BY = (FY1 - FY0) * T / 2.0 * K - R_IN
SWING_TB, SWING_LR = 4, 2                        # half periods of the triangle wave along the top / bottom and the side walls
MEDALLION_R, MEDALLION_SCALE = 248.0, 1.25
CAUSEWAY_ROWS = (15, 16)
WATER_PATCH = 112
CAUSEWAY_X0, CAUSEWAY_X1 = 1396, FX0 * T + 24


def load():
    return np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)


def bilinear(img, xs, ys):
    return cv2.remap(img, xs.astype(np.float32), ys.astype(np.float32), cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)


def smooth(e0, e1, v):
    t = np.clip((v - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def tri(phase):
    f = np.mod(phase, 2.0)
    return np.where(f < 1, f, 2 - f)


def quilt(src, rects, w, h, patch=160, step=112, seed=1):
    """Fills w x h with patches cut at random from `rects` (x0, y0, x1, y1 in src), blended with raised-cosine weights so no seam shows."""
    rng = np.random.default_rng(seed)
    acc = np.zeros((h, w, 3), np.float32); wsum = np.zeros((h, w, 1), np.float32)
    win1 = np.sin(np.linspace(0, np.pi, patch)) ** 2 + 1e-3
    win = (win1[:, None] * win1[None, :])[..., None]
    for y in range(-patch // 2, h, step):
        for x in range(-patch // 2, w, step):
            x0, y0, x1, y1 = rects[rng.integers(len(rects))]
            sx = rng.integers(x0, x1 - patch); sy = rng.integers(y0, y1 - patch)
            p = src[sy:sy + patch, sx:sx + patch]
            if rng.random() < 0.5: p = p[:, ::-1]
            dx0, dy0 = max(x, 0), max(y, 0); dx1, dy1 = min(x + patch, w), min(y + patch, h)
            if dx1 <= dx0 or dy1 <= dy0: continue
            acc[dy0:dy1, dx0:dx1] += p[dy0 - y:dy1 - y, dx0 - x:dx1 - x] * win[dy0 - y:dy1 - y, dx0 - x:dx1 - x]
            wsum[dy0:dy1, dx0:dx1] += win[dy0 - y:dy1 - y, dx0 - x:dx1 - x]
    return acc / wsum


def water_rects(src):
    """Open-water squares of the old painting (teal, no stone or leaves), the quilting source for the new water."""
    hsv = cv2.cvtColor(np.clip(src, 0, 255).astype(np.uint8), cv2.COLOR_RGB2HSV)
    h, s, v = hsv[..., 0].astype(int) * 2, hsv[..., 1], hsv[..., 2]
    teal = ((h > 140) & (h < 205) & (s > 70) & (v > 25) & (v < 210)).astype(np.float32)
    wet = ((h > 60) & (h < 205) & (s > 70) & (v < 190)).astype(np.float32)   # teal water or the dark lily pads floating on it
    frac, frac_t = cv2.blur(wet, (WATER_PATCH, WATER_PATCH)), cv2.blur(teal, (WATER_PATCH, WATER_PATCH))
    rects = []
    hp = WATER_PATCH // 2
    for y in range(16, src.shape[0] - WATER_PATCH - 16, 8):
        for x in range(16, OLD_COLS * T - WATER_PATCH - 16, 8):
            if frac[y + hp, x + hp] > 0.9 and frac_t[y + hp, x + hp] > 0.55: rects.append((x, y, x + WATER_PATCH + 1, y + WATER_PATCH + 1))
    if len(rects) < 8: raise SystemExit("too few open-water patches: %d" % len(rects))
    return rects


def round_coords(xs, ys):
    """Per new pixel: (source x, source y, distance from the wall line in round space). Inside the corner box -> distance 0."""
    dxn = xs - NCX; dyn = (ys - NCY) * K
    qx = np.clip(dxn, -BX, BX); qy = np.clip(dyn, -BY, BY)
    ex = dxn - qx; ey = dyn - qy
    r = np.hypot(ex, ey)
    ang = np.arctan2(ey, ex)                       # image angles: -90 = up, 0 = right, 90 = down
    in_x = np.abs(dxn) <= BX; in_y = np.abs(dyn) <= BY
    top_bot = in_x & ~in_y; sides = in_y & ~in_x
    u = dxn + BX; v = dyn + BY
    off_tb = tri(u / (2 * BX) * SWING_TB) * (2 * BX / SWING_TB) / ((R_IN + R_WALL) / 2)
    off_lr = tri(v / (2 * BY) * SWING_LR) * (2 * BY / SWING_LR) / ((R_IN + R_WALL) / 2)
    a = ang.copy()
    a = np.where(top_bot & (ey < 0), -np.pi / 2 + off_tb, a)
    a = np.where(top_bot & (ey > 0), np.pi / 2 - off_tb, a)
    a = np.where(sides & (ex < 0), np.pi - off_lr, a)
    a = np.where(sides & (ex > 0), off_lr, a)
    sx = CX + r * np.cos(a); sy = CY + r * np.sin(a) / K
    return sx, sy, r


def build():
    src = load()
    out = np.zeros((H, W, 3), np.float32)
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)

    # 1. water everywhere east of the hall, then the old painting where it stays (west part, NE room, the jungle in the old arena's corners)
    out[:] = quilt(src, water_rects(src), W, H, patch=WATER_PATCH, step=72, seed=7)
    keep = np.zeros((H, W), np.float32)
    keep[:, :OLD_COLS * T] = 1.0
    old_r = np.hypot(xs - CX, (ys - CY) * K)
    keep *= smooth(380, 410, old_r)                         # drop the old ring and its bridge stub
    keep[:, :OLD_COLS * T] *= 1 - smooth(OLD_COLS * T - 64, OLD_COLS * T - 8, xs[:, :OLD_COLS * T])
    out = out * (1 - keep[..., None]) + src_pad(src) * keep[..., None]

    # 2. jungle bands along the top and bottom of the new columns, copied from further west (rows of trees, no walls)
    for band_y0, band_y1, fade_in in ((0, 100, (50, 96)), (1270, H, (1270, 1330))):
        sx0 = 400
        x0 = OLD_COLS * T - 96
        strip = src[band_y0:band_y1, sx0:sx0 + (W - x0)]
        a = np.ones(strip.shape[:2], np.float32)
        yy = np.arange(band_y0, band_y1, dtype=np.float32)[:, None]
        a *= (1 - smooth(fade_in[0], fade_in[1], yy)) if band_y0 == 0 else smooth(fade_in[0], fade_in[1], yy)
        a *= smooth(0, 96, np.arange(strip.shape[1], dtype=np.float32))[None, :]
        out[band_y0:band_y1, x0:] = out[band_y0:band_y1, x0:] * (1 - a[..., None]) + strip * a[..., None]

    # 3. the arena: ring unrolled around the new box, floor quilted from the hall, medallion in the middle
    sx, sy, r = round_coords(xs, ys)
    ring = bilinear(src, sx, sy)
    floor_rects = [(300, 868, 1300, 1112), (240, 150, 760, 470), (1150, 160, 1600, 470)]
    floor = quilt(src, floor_rects, W, H, patch=192, step=128, seed=3)
    # colour-match the quilted floor to the arena's own floor ring (mean / std per channel)
    ref = src[((old_r > 225) & (old_r < 285))[:, :src.shape[1]]]
    m = (r == 0) | (r < R_IN - 60)
    fl = floor[m]
    floor = (floor - fl.mean(0)) / (fl.std(0) + 1e-3) * ref.std(0) * 0.9 + ref.mean(0)
    # medallion, scaled up around its own centre
    mr = np.hypot(xs - NCX, (ys - NCY) * K)
    msx = CX + (xs - NCX) / MEDALLION_SCALE; msy = CY + (ys - NCY) / MEDALLION_SCALE
    med = bilinear(src, msx, msy)
    med_a = 1 - smooth(MEDALLION_R * MEDALLION_SCALE - 36, MEDALLION_R * MEDALLION_SCALE, mr)
    floor = floor * (1 - med_a[..., None]) + med * med_a[..., None]
    ring_a = smooth(R_IN - 70, R_IN - 30, r)               # ring sample keeps the moss and shade along the inner wall
    arena = floor * (1 - ring_a[..., None]) + ring * ring_a[..., None]
    alpha = 1 - smooth(R_FADE0, R_FADE1, r)
    out = out * (1 - alpha[..., None]) + arena * alpha[..., None]

    # 4. the causeway from the hall, 2 tiles wide
    out = causeway(src, out)
    return np.clip(out, 0, 255).astype(np.uint8)


def src_pad(src):
    p = np.zeros((H, W, 3), np.float32)
    p[:, :src.shape[1]] = src
    return p


def causeway(src, out):
    """Old causeway pieces (measured on the painting): top curb y 933-949, bricks 949-996, bottom curb + face 996-1024, clean x 1440-1552."""
    top, body, bot = src[931:950, 1440:1552], src[950:996, 1440:1552], src[996:1026, 1440:1552]
    y0 = CAUSEWAY_ROWS[0] * T - 14
    y1 = (CAUSEWAY_ROWS[1] + 1) * T + 4
    body_h = (y1 - y0) - top.shape[0] - bot.shape[0]
    reps = int(np.ceil(body_h / body.shape[0]))
    tall_body = np.concatenate([body] * reps, 0)[:body_h]
    piece = np.concatenate([top, tall_body, bot], 0)       # 112 px wide
    L = CAUSEWAY_X1 - CAUSEWAY_X0
    reps = int(np.ceil(L / 112)) + 1
    # alternate the piece and its mirror so the brick rows run on without a hard repeat
    row = np.concatenate([piece if i % 2 == 0 else piece[:, ::-1] for i in range(reps)], 1)[:, :L]
    a = np.ones(row.shape[:2], np.float32)
    xx = np.arange(L, dtype=np.float32)[None, :]
    a *= smooth(0, 18, xx) * (1 - smooth(L - 30, L, xx))
    yy = np.arange(row.shape[0], dtype=np.float32)[:, None]
    a *= smooth(0, 3, yy) * (1 - smooth(row.shape[0] - 3, row.shape[0], yy))
    seg = out[y0:y0 + row.shape[0], CAUSEWAY_X0:CAUSEWAY_X1]
    out[y0:y0 + row.shape[0], CAUSEWAY_X0:CAUSEWAY_X1] = seg * (1 - a[..., None]) + row * a[..., None]
    return out


OLD_ROWS = None


def old_rows():
    import re
    cs = open(os.path.join(ROOT, "Assets", "Scripts", "Maps", "SunkenReliquaryMap.cs"), encoding="utf-8").read()
    body = cs[cs.index("static readonly string[] Rows"):cs.index("};", cs.index("static readonly string[] Rows"))]
    rows = re.findall(r'"([A-Za-z]+)"', body)
    assert len(rows) == ROWS, len(rows)
    return [r[:OLD_COLS] for r in rows]


def rows():
    """Collision rows: the old 36 columns with the old ring blocked, the 2-tile causeway, the new arena floor (tile centre well inside the floor edge)."""
    g = [list(r + "T" * (COLS - len(r))) for r in old_rows()]
    for y in range(7, 20):
        for x in range(26, OLD_COLS): g[y][x] = "T"
    for y in CAUSEWAY_ROWS:
        for x in range(22, FX0): g[y][x] = "d"
    for y in range(ROWS):
        for x in range(COLS):
            px, py = np.array([(x + 0.5) * T], np.float32), np.array([(y + 0.5) * T], np.float32)
            _, _, r = round_coords(px, py)
            if r[0] < R_IN - 20: g[y][x] = "p"
    for y in range(ROWS - 3, ROWS): g[y] = ["T"] * COLS
    return ["".join(r) for r in g]


def main():
    if "--rows" in sys.argv:
        for r in rows(): print('            "%s",' % r)
        return
    img = build()
    half = W // 2
    os.makedirs(BACKUP, exist_ok=True)
    for c in range(2):
        p = os.path.join(TILES, "%s-r0c%d.png" % (NAME, c))
        Image.fromarray(img[:, c * half:(c + 1) * half]).save(p, optimize=True)
        shutil.copyfile(p, os.path.join(BACKUP, os.path.basename(p)))
        print(p)
    mini = Image.fromarray(img).resize((COLS * 6, ROWS * 6), Image.LANCZOS)
    p = os.path.join(TILES, NAME + "-minimap.png")
    mini.save(p)
    shutil.copyfile(p, os.path.join(BACKUP, os.path.basename(p)))
    print(p)


if __name__ == "__main__":
    main()

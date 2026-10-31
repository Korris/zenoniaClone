"""
Builds a melee attack animation for the Unity hero from a pair of ChatGPT sheets (6 frames x 3 rows: down, up, right):
  Assets/ArtLibrary/anim/<class>/attack<N>-armed.png       (weapon drawn solid, only used to MEASURE the weapon)
  Assets/ArtLibrary/anim/<class>/attack<N>-weaponless.png  (the body the game draws)
Outputs, in the game's 192px frame format (6 columns x 4 rows: down, up, left = mirrored right, right):
  Resources/Sprites/<armedFolder>/melee<N>.png   baked armed frames (renderer fallback when an anchor is missing)
  Resources/Sprites/<bareFolder>/melee<N>.png    weaponless body frames
  Resources/Data/<gearFile>-melee.json            per-frame weapon anchors (merged: other anims kept)
  plans/reports/melee-shots/<class>-melee<N>.png  preview: armed | weaponless + measured weapon overlay

Weapon anchor, warrior (fit="sword", see sword_lines): a sword-shaped matched filter (red core, silver edges, gold cross-guard at one end) finds the
blade's angle on the armed frame, so the guard end cannot be mistaken for the tip and the curved swing trails do not match. The grip then goes on the
fist of the WEAPONLESS frame, because that is the body the game draws and its arm is not always where the armed sheet has it. s = the drawn blade
length measured on the armed sheet / (art length * draw_scale), the same for every frame of the anim.
Other single-weapon classes: straight line fitted (RANSAC + PCA) to the pixels the armed sheet adds over the weaponless one; the grip end is the end
nearest the gold crossguard or the fist; s = weapon length / art grip-to-tip length; behind = the body covers the blade.
Usage: python Tools/build-melee-sheets.py warrior 2
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "ArtLibrary", "anim")
RES = os.path.join(ROOT, "Assets", "Resources")
FRAME = 192
COLS, ROWS = 6, 3
DIRS = ["down", "up", "right"]  # sheet rows; "left" is the mirrored right row

# Per class: sprite folders, the anchor file, the weapon part + its tier-0 art (grip-to-tip length), parts hidden during the anim,
# and the reference body (bare idle, facing down) whose height and feet the new frames are scaled to.
CLASSES = {
    "warrior": dict(armed="hero", bare="bare", gear="held-gear", part="sword", art="sword/iron-base.png", grip=None,
                    draw_scale=1.6, hidden=["shield"], gold=True,  # draw_scale = HeroClassDef sword GearPartSpec (keep in sync)
                    # Height, feet and centre of the bare idle these sheets were cut against. Pinned: idle.png has been re-cut since, and
                    # following it would rescale every melee frame.
                    ref=(156, 186, 100.5),
                    fit="sword", guard_y=192, hand_y=230,  # iron-base art rows: cross-guard, middle of the handle
                    sword_s=0.34,  # same s as the idle and walk anchors in held-gear-melee.json
                    # Frames the detection cannot read. Keys: (melee n, dir, frame). "search": blade angle range to look in (trail or a ghost blade
                    # scores higher elsewhere). "angle": no blade drawn at all, use this angle. "fist": the bare fist, where it cannot be told from armour.
                    # "behind": override the measured draw order.
                    fixes={
                        (1, "up", 0): dict(search=(40, 60), fist=(185, 84)), (1, "up", 1): dict(search=(75, 95)), (1, "up", 2): dict(search=(65, 90), fist=(168, 98)),
                        (1, "right", 5): dict(fist=(107, 129)),
                        (2, "down", 0): dict(fist=(65, 45)), (2, "down", 1): dict(fist=(65, 67)), (2, "down", 2): dict(fist=(73, 100)), (2, "down", 3): dict(fist=(97, 140)), (2, "down", 5): dict(fist=(53, 135)),
                        (2, "up", 0): dict(fist=(172, 50)), (2, "up", 3): dict(fist=(152, 135)), (2, "up", 4): dict(fist=(175, 131)), (2, "up", 5): dict(fist=(160, 131)),
                        (2, "right", 0): dict(fist=(113, 70)), (2, "right", 1): dict(fist=(138, 76)), (2, "right", 2): dict(search=(-55, -30), fist=(125, 120)),
                        (2, "right", 3): dict(fist=(128, 140)), (2, "right", 4): dict(fist=(140, 138)), (2, "right", 5): dict(fist=(118, 137)),
                        (3, "down", 0): dict(fist=(65, 135)), (3, "down", 5): dict(fist=(55, 135)),
                        (3, "up", 0): dict(fist=(175, 125)), (3, "up", 5): dict(fist=(162, 128)),
                        (3, "right", 0): dict(fist=(93, 133)), (3, "right", 2): dict(fist=(140, 125)), (3, "right", 3): dict(angle=95, behind=True, fist=(112, 124)),
                        (3, "right", 4): dict(fist=(118, 132)), (3, "right", 5): dict(fist=(115, 134)),
                        (4, "down", 0): dict(fist=(67, 18)), (4, "down", 1): dict(fist=(73, 6)), (4, "down", 2): dict(search=(15, 40), fist=(86, 50)), (4, "down", 3): dict(fist=(117, 137)), (4, "down", 4): dict(fist=(140, 138)),
                        (4, "up", 0): dict(fist=(168, 18)), (4, "up", 1): dict(fist=(133, 4)), (4, "up", 2): dict(fist=(165, 33)), (4, "up", 3): dict(fist=(136, 130)), (4, "up", 4): dict(fist=(80, 125)), (4, "up", 5): dict(fist=(170, 115)),
                        (4, "right", 0): dict(fist=(92, 36)), (4, "right", 1): dict(fist=(116, 26)), (4, "right", 2): dict(fist=(134, 128), behind=True, fallback=True),  # armed and bare arms disagree at the swing's peak: draw the baked armed frame
                        (4, "right", 3): dict(fist=(140, 141)), (4, "right", 4): dict(fist=(148, 132)), (4, "right", 5): dict(fist=(122, 137)),
                    }),
    # Two daggers: parts main (character's right hand, drawn in front) + off (left hand). draw_scale = HeroClassDef dagger GearPartSpec;
    # dagger_frac = grip-to-tip dagger length (what shows beyond the fist) as a fraction of the idle body height (the user wants ~40%).
    "assassin": dict(armed="assassin", bare="assassin-bare", gear="held-assassin", part="main", parts=["main", "off"],
                     art="assassin-dagger/iron-kunai-base.png", manifest_key="weapon", grip=None, draw_scale=1.3, hidden=[], gold=False,
                     dagger_frac=0.40,
                     # Hand corrections where detection cannot tell (motion streaks, blades over the torso). Keys: (melee n, dir, frame).
                     # "copy": frame index whose lines replace this one; "flip": parts whose grip/tip are swapped.
                     fixes={(4, "down", 2): dict(copy=3), (4, "up", 2): dict(copy=3), (4, "right", 2): dict(copy=3),
                            (4, "up", 1): dict(flip=["main"]), (4, "up", 4): dict(flip=["main"])}),
}


def load(path):
    return np.asarray(Image.open(path).convert("RGBA")).astype(np.int16)


def split_bounds(alpha_profile, n, length):
    """Cut positions between n cells: the emptiest line within +-20% of each equal-grid boundary."""
    cuts = [0]
    for i in range(1, n):
        c = int(length * i / n)
        lo, hi = max(1, c - int(length * 0.2 / n * 2)), min(length - 1, c + int(length * 0.2 / n * 2))
        window = alpha_profile[lo:hi]
        cuts.append(lo + int(np.argmin(window)))
    cuts.append(length)
    return cuts


def place(dst, im, x, y):
    """alpha_composite that accepts negative / overflowing offsets (the part outside dst is clipped)."""
    x, y = int(round(x)), int(round(y))
    cx0, cy0 = max(0, -x), max(0, -y)
    cx1, cy1 = min(im.width, dst.width - x), min(im.height, dst.height - y)
    if cx1 > cx0 and cy1 > cy0:
        dst.alpha_composite(im.crop((cx0, cy0, cx1, cy1)), (x + cx0, y + cy0))


def bbox(alpha, thr=40):
    ys, xs = np.nonzero(alpha > thr)
    if len(xs) == 0:
        return None
    return xs.min(), xs.max(), ys.min(), ys.max()


def weapon_line(armed, bare, gold, body_centre):
    """Grip point, tip point (cell px) of the weapon the armed cell adds over the bare one, plus a 'behind' flag; None if not visible."""
    a_al, b_al = armed[..., 3], bare[..., 3]
    diff = np.abs(armed[..., :3] - bare[..., :3]).sum(-1)
    mask = (a_al > 170) & ((b_al < 60) | (diff > 110))
    ys, xs = np.nonzero(mask)
    if len(xs) < 120:
        return None
    pts = np.stack([xs, ys], 1).astype(np.float64)
    rng = np.random.default_rng(7)
    sample = pts[rng.choice(len(pts), min(len(pts), 3000), replace=False)]
    best, best_score = None, 0
    for _ in range(600):  # RANSAC: the blade is the longest dense straight structure (smears are curved)
        p, q = sample[rng.choice(len(sample), 2, replace=False)]
        d = q - p
        norm = math.hypot(*d)
        if norm < 25:
            continue
        nrm = np.array([-d[1], d[0]]) / norm
        dist = np.abs((sample - p) @ nrm)
        inl = dist < 7
        if inl.sum() < 40:
            continue
        t = (sample[inl] - p) @ (d / norm)
        span = np.ptp(t)
        score = inl.sum() * min(1.0, span / 60.0)
        if score > best_score:
            best_score, best = score, (p, d / norm)
    if best is None:
        return None
    p, u = best
    nrm = np.array([-u[1], u[0]])
    inl = np.abs((pts - p) @ nrm) < 8
    band = pts[inl]
    centre = band.mean(0)  # PCA refine on the band
    _, _, vt = np.linalg.svd(band - centre, full_matrices=False)
    u = vt[0]
    t = (band - centre) @ u
    # Longest contiguous run along the axis (gaps up to 10 px) = the blade + hilt.
    order = np.sort(t)
    runs, start = [], order[0]
    for i in range(1, len(order)):
        if order[i] - order[i - 1] > 10:
            runs.append((start, order[i - 1]))
            start = order[i]
    runs.append((start, order[-1]))
    t0, t1 = max(runs, key=lambda r: ((t >= r[0]) & (t <= r[1])).sum())
    end0, end1 = centre + u * t0, centre + u * t1
    # Grip end, first choice: the cross-guard makes the hilt end clearly WIDER across the blade axis than the pointed tip.
    nrm_u = np.array([-u[1], u[0]])
    seg = pts[(np.abs((pts - centre) @ nrm_u) < 40)]
    ts, ws = (seg - centre) @ u, np.abs((seg - centre) @ nrm_u)
    span = t1 - t0
    def spread(lo, hi):
        sel = (ts >= lo) & (ts <= hi)
        return np.percentile(ws[sel], 90) if sel.sum() > 10 else 0.0
    w0, w1 = spread(t0, t0 + 0.3 * span), spread(t1 - 0.3 * span, t1)
    if max(w0, w1) > 1.35 * max(1.0, min(w0, w1)):
        if w1 > w0:
            end0, end1 = end1, end0
        grip = end0 + (end1 - end0) * 0.06
        return finish(armed, b_al, diff, grip, end1)
    # Fallback: nearest the gold cross-guard if there is one, else nearest the body centre.
    anchor = body_centre
    if gold:
        r, g, b = armed[..., 0], armed[..., 1], armed[..., 2]
        gm = mask & (r > 170) & (g > 110) & (b < 110) & (r - b > 90)
        gy, gx = np.nonzero(gm)
        if len(gx) > 15:
            gp = np.stack([gx, gy], 1).astype(np.float64)
            near = np.abs((gp - centre) @ np.array([-u[1], u[0]])) < 30
            if near.sum() > 8:
                anchor = gp[near].mean(0)
    if np.linalg.norm(end0 - anchor) > np.linalg.norm(end1 - anchor):
        end0, end1 = end1, end0
    grip = end0 + (end1 - end0) * 0.06  # the hand sits a little inside the pommel end
    return finish(armed, b_al, diff, grip, end1)


def finish(armed, b_al, diff, grip, tip):
    """(grip, tip, behind): behind = along the blade the weaponless body is opaque and unchanged in the armed frame (body covers it)."""
    end1 = tip
    behind = front = 0
    for k in np.linspace(0.15, 0.95, 24):
        x, y = (grip + (end1 - grip) * k).round().astype(int)
        if 0 <= y < armed.shape[0] and 0 <= x < armed.shape[1] and b_al[y, x] > 200:
            if diff[y, x] < 45:
                behind += 1
            else:
                front += 1
    return grip, end1, behind > front and behind >= 4


def sword_masks(frame, bare):
    """Blade colours in an armed frame: red core, silver edges, gold guard, plus `same` = pixels the weaponless frame also has (the body)."""
    rgb = frame[..., :3].astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(-1), rgb.min(-1)
    sat = (mx - mn) / np.maximum(mx, 1)
    op = frame[..., 3] > 170
    red = op & (r > 130) & (g < 110) & (b < 110) & (sat > 0.5)
    silver = op & (mn > 110) & (sat < 0.3)
    gold = op & (r > 110) & (g > 70) & (b < 120) & (r > g * 1.12) & (g > b * 1.2)
    same = (np.abs(frame[..., :3].astype(int) - bare[..., :3].astype(int)).sum(-1) < 40) & (bare[..., 3] > 180)
    return red, silver, gold, same


SWORD_PAD = 96  # the blade may leave the 192px frame (the sheets clip it); pixels outside count for nothing
SWORD_SIZE = FRAME + 2 * SWORD_PAD
SWORD_FILTERS = {}


def sword_filter(length, angle):
    """Frequency domain sword (3 channels: core, edge, guard) of guard-to-tip `length`, guard centre at the origin, and its total weight."""
    key = (length, angle)
    if key not in SWORD_FILTERS:
        th = math.radians(angle)
        ux, uy = math.sin(th), -math.cos(th)
        rad = int(length + 24)
        xs, ys = np.meshgrid(np.arange(-rad, rad + 1), np.arange(-rad, rad + 1))
        t = xs * ux + ys * uy
        w = np.abs(ys * ux - xs * uy)
        taper = np.clip((length - t) * 0.75, 0, 9)  # half width, narrowing to the point
        core = (w <= np.minimum(3.5, taper - 3)) & (t >= 8) & (t <= length - 10)
        edge = ((w >= 5.5) & (w <= np.maximum(5.6, taper)) & (t >= 2) & (t <= length)) | ((t > length - 12) & (w <= taper) & (t <= length))
        guard = (np.abs(t) <= 4.5) & (w <= 19)
        weights = (1.0, 1.0, 1.6)
        T = np.zeros((3, SWORD_SIZE, SWORD_SIZE), np.float32)
        for i, m in enumerate((core, edge, guard)):
            iy, ix = np.nonzero(m)
            T[i, ys[iy, ix] % SWORD_SIZE, xs[iy, ix] % SWORD_SIZE] = weights[i]
        SWORD_FILTERS[key] = (np.conj(np.fft.rfft2(T)), float(T.sum()))
    return SWORD_FILTERS[key]


def fit_sword(frame, bare, angles):
    """Matched filter over `angles` (coarse then fine). Returns (guard centre, angle in degrees) of the sword in an armed frame."""
    red, silver, gold, same = sword_masks(frame, bare)
    W = np.where(same, 0.15, 1.0)
    G = np.zeros((3, SWORD_SIZE, SWORD_SIZE), np.float32)
    for i, (ch, miss) in enumerate(((red, 0.35), (silver, 0.35), (gold, 0.25))):
        G[i, SWORD_PAD:SWORD_PAD + FRAME, SWORD_PAD:SWORD_PAD + FRAME] = (ch * 1.0 - miss * (~ch)) * W
    fg = np.fft.rfft2(G)

    def search(lengths, angs):
        best = None
        for length in lengths:
            for ang in angs:
                ft, mass = sword_filter(length, ang)
                sc = np.fft.irfft2((fg * ft).sum(0), s=(SWORD_SIZE, SWORD_SIZE)) / mass
                m = SWORD_PAD - 14
                sc = sc[m:SWORD_SIZE - m, m:SWORD_SIZE - m]
                y, x = np.unravel_index(int(np.argmax(sc)), sc.shape)
                if best is None or sc[y, x] > best[0]:
                    best = (float(sc[y, x]), x - 14.0, y - 14.0, float(ang), length)
        return best

    _, _, _, ang, length = search((50, 60, 70, 80, 90), angles)
    _, x, y, ang, _ = search(range(length - 10, length + 11, 4), np.arange(ang - 4, ang + 5, 2))
    return np.array([x, y]), ang


def blade_reach(frame, bare, guard, u):
    """Guard-to-tip length of the drawn blade along axis u, and whether the frame edge cut it off."""
    red, silver, _, same = sword_masks(frame, bare)
    blade = (red | silver) & ~same
    v = np.array([-u[1], u[0]])
    reach, gap = 0.0, 0
    for t in np.arange(8.0, 170.0):
        p = guard + u * t
        if not (0 <= p[0] < FRAME and 0 <= p[1] < FRAME):
            return reach, True
        if any(blade[int(q[1]), int(q[0])] for q in (p + v * w for w in (-5, -2.5, 0, 2.5, 5)) if 0 <= q[0] < FRAME and 0 <= q[1] < FRAME):
            reach, gap = t, 0
        else:
            gap += 1
            if gap > 6:
                break
    return reach, False


def disk_sum(mask, r):
    """Per pixel count of mask pixels within radius r, and the disk's area."""
    y, x = np.ogrid[-r:r + 1, -r:r + 1]
    d = (x * x + y * y <= r * r).astype(np.float32)
    n = FRAME + 2 * r + 1
    out = np.fft.irfft2(np.fft.rfft2(mask.astype(np.float32), s=(n, n)) * np.fft.rfft2(d, s=(n, n)), s=(n, n))
    return out[r:r + FRAME, r:r + FRAME], float(d.sum())


def find_fist(bare, hint):
    """Centre of the gauntlet in a weaponless frame, near hint (where the armed frame holds the handle). A fist is a dark metal blob, usually at the
    end of the arm and so sticking out of the silhouette. Below the knees the same metal is the boots. Falls back to hint when nothing qualifies."""
    rgb = bare[..., :3].astype(np.float32)
    mx, mn = rgb.max(-1), rgb.min(-1)
    sat = (mx - mn) / np.maximum(mx, 1)
    dark = (bare[..., 3] > 180) & (sat < 0.38) & (mx > 25) & (mx < 150)
    opaque = bare[..., 3] > 120
    inner, n_inner = disk_sum(dark, 8)
    inner = inner / n_inner
    big, n_big = disk_sum(opaque, 19)
    mid_, n_mid = disk_sum(opaque, 13)
    ys, xs = np.mgrid[0:FRAME, 0:FRAME]
    dist = np.hypot(xs - hint[0], ys - hint[1])
    score = inner - 0.55 * (big - mid_) / (n_big - n_mid) - dist / 80.0
    score[(dist > 55) | (ys > 150) | (inner < 0.45)] = -9
    if score.max() <= -9:
        return np.array(hint, dtype=np.float64)
    y, x = np.unravel_index(int(np.argmax(score)), score.shape)
    near = dark & (np.hypot(xs - x, ys - y) <= 10)
    return np.array([xs[near].mean(), ys[near].mean()])


def sword_lines(cfg, n, cells, gy):
    """{dir: [(grip, tip, behind)]} in frame px for the armed / bare frame pairs in cells. One blade length per anim, measured on the frames where
    the sheet shows the whole blade; art rows guard_y, hand_y and gy (the art's grip) turn it into the grip offset from the fist."""
    fits, reaches = {}, []
    for d, row in cells.items():
        for c, (a, b) in enumerate(row):
            fx = cfg["fixes"].get((n, d, c), {})
            if "angle" in fx:
                fits[d, c] = (None, fx["angle"], fx)
                continue
            lo, hi = fx.get("search", (-180, 175))
            guard, ang = fit_sword(a, b, range(lo, hi + 1, 5))
            fits[d, c] = (guard, ang, fx)
            th = math.radians(ang)
            reach, clipped = blade_reach(a, b, guard, np.array([math.sin(th), -math.cos(th)]))
            if not clipped:
                reaches.append(reach)
    # A reach only ever falls short (foreshortening, a dark outline breaking the scan), so take a high percentile rather than the median.
    k = float(np.percentile(reaches, 85)) / cfg["guard_y"]  # frame px per art px
    if "sword_s" in cfg:  # the sword keeps one size through idle, walk and every swing; the baked blade only gives the angle
        k = cfg["sword_s"] * cfg["draw_scale"]
    length = gy * k
    out = {}
    for d, row in cells.items():
        out[d] = []
        for c, (a, b) in enumerate(row):
            guard, ang, fx = fits[d, c]
            th = math.radians(ang)
            u = np.array([math.sin(th), -math.cos(th)])
            if "fist" in fx:
                fist = np.array(fx["fist"], dtype=np.float64)
            else:
                fist = find_fist(b, guard - u * (cfg["hand_y"] - cfg["guard_y"]) * k)
            grip = fist + u * (cfg["hand_y"] - gy) * k  # the handle's middle in the fist; art rows grow towards the pommel, gy is above hand_y
            behind = fx.get("behind")
            if behind is None:
                behind = guard is not None and sword_behind(a, b, guard, u, length)
            out[d].append((grip, grip + u * length, behind))
    return out


def sword_behind(frame, bare, guard, u, length):
    """The armed frame shows the body, not the blade, along most of the blade's stretch over the body."""
    red, silver, _, same = sword_masks(frame, bare)
    blade = (red | silver) & ~same
    over = shown = 0
    for t in np.linspace(0.1, 0.9, 24) * length:
        x, y = (guard + u * t).astype(int)
        if 0 <= x < FRAME and 0 <= y < FRAME and bare[y, x, 3] > 200:
            over += 1
            shown += int(blade[y, x])
    return over >= 6 and shown < over * 0.25


def fit_blade(pts, rng, exclude=None):
    """One straight blade line through pts (RANSAC, PCA refine). Returns (centre, u, t0, t1, inlier_mask) or None."""
    if len(pts) < 100:
        return None
    sample = pts[rng.choice(len(pts), min(len(pts), 3000), replace=False)]
    best, best_score = None, 0
    for _ in range(700):
        p, q = sample[rng.choice(len(sample), 2, replace=False)]
        d = q - p
        norm = math.hypot(*d)
        if norm < 30:
            continue
        u = d / norm
        nrm = np.array([-u[1], u[0]])
        inl = np.abs((sample - p) @ nrm) < 5
        if inl.sum() < 40:
            continue
        span = np.ptp((sample[inl] - p) @ u)
        score = inl.sum() * min(1.0, span / 60.0)
        if score > best_score:
            best_score, best = score, (p, u)
    if best is None:
        return None
    p, u = best
    for _ in range(2):  # refine: PCA of the band around the current line
        nrm = np.array([-u[1], u[0]])
        band = pts[np.abs((pts - p) @ nrm) < 7]
        if len(band) < 40:
            return None
        p = band.mean(0)
        u = np.linalg.svd(band - p, full_matrices=False)[2][0]
    nrm = np.array([-u[1], u[0]])
    band_mask = np.abs((pts - p) @ nrm) < 7
    t = (pts[band_mask] - p) @ u
    order = np.sort(t)
    runs, start = [], order[0]
    for i in range(1, len(order)):
        if order[i] - order[i - 1] > 8:
            runs.append((start, order[i - 1]))
            start = order[i]
    runs.append((start, order[-1]))
    t0, t1 = max(runs, key=lambda r: ((t >= r[0]) & (t <= r[1])).sum())
    if t1 - t0 < 35:
        return None
    tall = (pts - p) @ u
    return p, u, t0, t1, band_mask & (tall >= t0 - 6) & (tall <= t1 + 6)


def components(mask, grow=4, min_area=150):
    """Connected blobs of the mask (bridged by `grow` px), as arrays of pixel (x, y) points, biggest first."""
    big = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * grow + 1))) > 0
    seen = np.zeros_like(big)
    h, w = big.shape
    blobs = []
    for y0, x0 in zip(*np.nonzero(big & ~seen)):
        if seen[y0, x0]:
            continue
        stack, seen[y0, x0] = [(y0, x0)], True
        pix = []
        while stack:
            y, x = stack.pop()
            pix.append((y, x))
            for yy, xx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= yy < h and 0 <= xx < w and big[yy, xx] and not seen[yy, xx]:
                    seen[yy, xx] = True
                    stack.append((yy, xx))
        pix = np.array(pix)
        pts = np.stack([pix[:, 1], pix[:, 0]], 1)
        pts = pts[mask[pts[:, 1], pts[:, 0]]].astype(np.float64)  # back to the undilated pixels
        if len(pts) >= min_area:
            blobs.append(pts)
    return sorted(blobs, key=len, reverse=True)


def width_ratio(pts, a, b):
    """Blade width near end a divided by width near end b (the blade is wide at the guard / base and tapers to the tip)."""
    u = (b - a) / max(1e-6, np.linalg.norm(b - a))
    t, w = (pts - a) @ u, (pts - a) @ np.array([-u[1], u[0]])
    length = np.linalg.norm(b - a)
    def width(lo, hi):
        sel = (t >= lo * length) & (t <= hi * length)
        return np.percentile(w[sel], 95) - np.percentile(w[sel], 5) if sel.sum() > 8 else 1.0
    return width(0, 0.35) / max(1.0, width(0.65, 1.0))


def chord(pts):
    """Ends of a (possibly curved) blade blob: its extreme points along the principal axis."""
    c = pts.mean(0)
    u = np.linalg.svd(pts - c, full_matrices=False)[2][0]
    t = (pts - c) @ u
    return pts[np.argmin(t)], pts[np.argmax(t)]


def dual_lines(armed, bare, count, rng):
    """Up to `count` blades the armed cell adds over the bare one: list of (end_a, end_b, behind), ends unordered (grip not yet chosen)."""
    a_al, b_al = armed[..., 3], bare[..., 3]
    # The two ChatGPT sheets are NOT pixel-aligned (the body is redrawn), so a diff mask picks up the whole body. The blades are the only
    # saturated violet in the cell (hair is dark and grey-violet, the scarf red); the translucent glow / streak pixels fail alpha > 200.
    r, g, bl = armed[..., 0], armed[..., 1], armed[..., 2]
    mask = (a_al > 200) & (bl > 100) & (bl - g > 60) & (bl > r + 15)
    # Curved blades: when the mask splits into `count` separate blobs, each blob's chord is the blade (crossing blades merge -> RANSAC below).
    # Opening (erode then grow) drops the thin motion streaks / glow arcs and keeps the thick blades; weaker opening if that loses a blade.
    raw_mask = mask
    out = []
    for size in (5, 3):
        im = Image.fromarray((raw_mask * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(size)).filter(ImageFilter.MaxFilter(size))
        mask = np.asarray(im) > 0
        blobs = components(mask)
        if len(blobs) >= count and all(len(b) > 0.35 * len(blobs[0]) for b in blobs[:count]):
            for b in blobs[:count]:
                e0, e1 = chord(b)
                if np.linalg.norm(e1 - e0) >= 35:
                    out.append((e0, e1, width_ratio(b, e0, e1)))
            if len(out) == count:
                return out
            out = []
    # Crossing / merged blades: RANSAC on the lightly opened mask.
    ys, xs = np.nonzero(mask)
    pts = np.stack([xs, ys], 1).astype(np.float64)
    for _ in range(count):
        fit = fit_blade(pts, rng)
        if fit is None:
            break
        p, u, t0, t1, inl = fit
        # The second blade must be about as long as the first one (a streak chord is shorter), else it is not a blade.
        if out and (t1 - t0) < 0.6 * np.linalg.norm(out[0][1] - out[0][0]):
            break
        a, b = p + u * t0, p + u * t1
        out.append((a, b, width_ratio(pts[inl], a, b)))
        pts = pts[~inl]
    return out


def body_distance(bare_alpha, pt):
    """Pixels from pt to the nearest opaque weaponless-body pixel (0 when pt is on the body)."""
    ys, xs = np.nonzero(bare_alpha[::2, ::2] > 200)
    if len(xs) == 0:
        return 1e9
    return float(np.sqrt(((xs * 2 - pt[0]) ** 2 + (ys * 2 - pt[1]) ** 2).min()))


def orient_grip_first(lines, bare_alpha, body_centre):
    """Order each (a, b, width ratio) as (grip, tip, ambiguous). Grip = the end clearly nearer the weaponless body (the fist; the width
    ratio of the two ends was tried as a cue but is unreliable on curved blades). Undecided (crossing blades) -> ambiguous, resolved temporally by resolve_ambiguous."""
    res = []
    for a, b, ratio in lines:
        da, db = body_distance(bare_alpha, a), body_distance(bare_alpha, b)
        if abs(da - db) >= 8:
            res.append((a, b, False) if da < db else (b, a, False))
        else:
            res.append((a, b, True) if np.linalg.norm(a - body_centre) <= np.linalg.norm(b - body_centre) else (b, a, True))
    return res


def mid(ln):
    return (ln[0] + ln[1]) / 2


def assign_hands(per_frame, direction):
    """Per frame {main, off} -> line or None. Frame 0 by hand side (down: character's right = image left; up: image right;
    right-facing: the near-side hand = lower on screen); later frames follow the nearest previous blade midpoint so the hands never swap."""
    seed_key = {"down": lambda l: mid(l)[0], "up": lambda l: -mid(l)[0], "right": lambda l: -mid(l)[1]}[direction]
    out, prev = [], None
    for lines in per_frame:
        slot = {"main": None, "off": None}
        if prev is None:
            for name, ln in zip(["main", "off"], sorted(lines, key=seed_key)):
                slot[name] = ln
        else:
            cand = sorted((np.linalg.norm(mid(ln) - mid(prev[name])), name, i) for name in ("main", "off") if prev[name] is not None
                          for i, ln in enumerate(lines))
            used = set()
            for d, name, i in cand:
                if slot[name] is None and i not in used:
                    slot[name] = lines[i]
                    used.add(i)
            # A blade with no previous hand (that hand was never seen yet) takes the free slot.
            for i, ln in enumerate(lines):
                if i not in used:
                    free = [k for k in slot if slot[k] is None]
                    if free:
                        slot[free[0]] = ln
        out.append(slot)
        prev = {k: (slot[k] if slot[k] is not None else (prev[k] if prev else None)) for k in slot}
    return out


def resolve_ambiguous(slots):
    """Flip grip/tip of ambiguous blades so the grip stays near the same hand's grip in the neighbouring (certain) frames."""
    for name in ("main", "off"):
        for c, slot in enumerate(slots):
            ln = slot[name]
            if ln is None or not ln[2]:
                continue
            refs = [slots[k][name][0] for k in (c - 1, c + 1) if 0 <= k < len(slots) and slots[k][name] is not None and not slots[k][name][2]]
            if refs:
                ref = np.mean(refs, 0)
                if np.linalg.norm(ln[1] - ref) < np.linalg.norm(ln[0] - ref):
                    slot[name] = (ln[1], ln[0], False)
                else:
                    slot[name] = (ln[0], ln[1], False)


def main():
    cls, n = sys.argv[1], int(sys.argv[2])
    cfg = CLASSES[cls]
    armed = load(os.path.join(ART, cls, f"attack{n}-armed.png"))
    bare = load(os.path.join(ART, cls, f"attack{n}-weaponless.png"))
    if armed.shape != bare.shape:
        sys.exit("armed / weaponless sizes differ")
    H, W = armed.shape[:2]
    alpha = np.maximum(armed[..., 3], bare[..., 3])
    xcuts = split_bounds(alpha.sum(0), COLS, W)
    ycuts = split_bounds(alpha.sum(1), ROWS, H)

    if "ref" in cfg:
        ref_h, ref_feet, ref_cx = cfg["ref"]
    else:
        ref = np.asarray(Image.open(os.path.join(RES, "Sprites", cfg["bare"], "idle.png")).convert("RGBA"))[:FRAME, :FRAME, 3]
        rx0, rx1, ry0, ry1 = bbox(ref)
        ref_h, ref_feet, ref_cx = ry1 - ry0, ry1, (rx0 + rx1) / 2

    def cell(img, r, c):
        return img[ycuts[r]:ycuts[r + 1], xcuts[c]:xcuts[c + 1]]

    ready = bbox(cell(bare, 0, COLS - 1)[..., 3])
    scale = ref_h / (ready[3] - ready[2])

    art = Image.open(os.path.join(RES, "Sprites", cfg["art"])).convert("RGBA")
    if cfg["grip"] is None:  # tier-0 grip from the manifest, so new weapon art never needs a code change here
        manifest = json.load(open(os.path.join(RES, "Data", cfg["gear"] + ".json")))
        g = manifest[cfg.get("manifest_key", cfg["part"])][0]["baseGrip"]
        cfg["grip"] = (g["gripX"], g["gripY"])
    gx, gy = cfg["grip"]
    art_len = gy  # tier-0 art: tip at the top edge, grip at gy
    parts = cfg.get("parts", [cfg["part"]])
    dual = len(parts) > 1
    ab = art.getbbox()
    art_total = ab[3] - ab[1]                 # pommel-to-tip length of the art
    grip_frac = (ab[3] - gy) / art_total      # how far the grip sits in from the pommel end (fraction of the length)
    rng = np.random.default_rng(7)

    out_armed = Image.new("RGBA", (FRAME * COLS, FRAME * 4))
    out_bare = Image.new("RGBA", (FRAME * COLS, FRAME * 4))
    preview = Image.new("RGBA", (FRAME * COLS * 2, FRAME * 3), (40, 48, 60, 255))
    lines = {}
    sword_fit = cfg.get("fit") == "sword"
    cells = {d: [] for d in DIRS}  # sword_fit: (armed, bare) frame pairs
    for r, d in enumerate(DIRS):
        rb = bbox(cell(bare, r, COLS - 1)[..., 3])  # this row's ready frame sets the row's feet line and centre
        feet, cx = rb[3], (rb[0] + rb[1]) / 2
        out_row = [0, 1, 3][r]  # down 0, up 1, right 3 (left 2 is mirrored below)
        frames = []
        raw_cells = []
        for c in range(COLS):
            a, b = cell(armed, r, c), cell(bare, r, c)
            bb = bbox(b[..., 3])
            body_centre = np.array([(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2]) if bb else np.array([cx, feet - 100.0])
            if dual:
                raw = dual_lines(a, b, len(parts), rng)
                raw = orient_grip_first(raw, b[..., 3], body_centre)
                raw_cells.append(raw)
                line = None
            elif sword_fit:
                line = None
            else:
                line = weapon_line(a, b, cfg["gold"], body_centre)
            # cell px -> frame px
            ox, oy = ref_cx - cx * scale, ref_feet - feet * scale
            size = (max(1, round(a.shape[1] * scale)), max(1, round(a.shape[0] * scale)))
            pair = []
            for src, dst in ((a, out_armed), (b, out_bare)):
                im = Image.fromarray(src.astype(np.uint8)).resize(size, Image.LANCZOS)
                frame = Image.new("RGBA", (FRAME, FRAME))
                place(frame, im, ox, oy)
                dst.paste(frame, (c * FRAME, out_row * FRAME))
                pair.append(np.asarray(frame))
            cells[d].append(pair)
            if dual:
                continue
            if line is None:
                frames.append(None)
            else:
                grip, tip, behind = line
                g = grip * scale + [ox, oy]
                t = tip * scale + [ox, oy]
                frames.append((g, t, behind))
        if dual:  # per frame {part: (grip, tip, behind)} in frame px
            ox, oy = ref_cx - cx * scale, ref_feet - feet * scale
            slots = assign_hands(raw_cells, d)
            resolve_ambiguous(slots)
            for (fn, fd, ff), fx in cfg.get("fixes", {}).items():
                if fn != n or fd != d:
                    continue
                if "copy" in fx:
                    slots[ff] = dict(slots[fx["copy"]])
                for name in fx.get("flip", []):
                    ln = slots[ff][name]
                    if ln is not None:
                        slots[ff][name] = (ln[1], ln[0], False)
            total = cfg["dagger_frac"] * ref_h / (1 - grip_frac)  # pommel to tip
            for slot in slots:
                fr = {}
                for name, ln in slot.items():
                    if ln is None:
                        continue
                    a0, t0, bh = ln[0] * scale + [ox, oy], ln[1] * scale + [ox, oy], False
                    u = (t0 - a0) / max(1e-6, np.linalg.norm(t0 - a0))
                    # Tip-anchored: the tip is the crisp end of the mask (the pommel is often hidden in the fist); the grip lies one
                    # (grip-to-tip) art length back along the line, so the (bigger) dagger keeps the sheet's tip and grows into the hand.
                    reach = (1 - grip_frac) * total
                    g0 = a0 if np.linalg.norm(t0 - a0) > 1.15 * reach else t0 - u * reach  # long blade (X cross): hold at the fist end
                    fr[name] = (g0, t0, bh)
                frames.append(fr)
        lines[d] = frames
    if sword_fit:
        lines = sword_lines(cfg, n, cells, gy)
    # Left row = mirrored right row.
    for dst in (out_armed, out_bare):
        right = dst.crop((0, 3 * FRAME, FRAME * COLS, 4 * FRAME))
        for c in range(COLS):
            dst.paste(right.crop((c * FRAME, 0, (c + 1) * FRAME, FRAME)).transpose(Image.FLIP_LEFT_RIGHT), (c * FRAME, 2 * FRAME))
    def mirror(f):
        return None if f is None else (np.array([FRAME - f[0][0], f[0][1]]), np.array([FRAME - f[1][0], f[1][1]]), f[2])
    if dual:
        lines["left"] = [{k: mirror(v) for k, v in fr.items()} for fr in lines["right"]]
        s = round(cfg["dagger_frac"] * ref_h / (art_len * cfg["draw_scale"]), 3)
        length = cfg["dagger_frac"] * ref_h
    else:
        lines["left"] = [mirror(f) for f in lines["right"]]
        lengths = [np.linalg.norm(f[1] - f[0]) for fs in lines.values() for f in fs if f is not None]
        length = float(np.median(lengths)) if lengths else 70.0
        s = round(length / (art_len * cfg["draw_scale"]), 3)
    anchors = []
    for d in ["down", "up", "left", "right"]:
      for part in parts:
        frames = []
        for f in ([fr.get(part) for fr in lines[d]] if dual else lines[d]):
            if f is None:
                frames.append(dict(x=96, y=96, angle=0, s=s, sx=1, behind=False, hidden=True, fallback=False))
                continue
            g, t, behind = f
            ang = math.degrees(math.atan2(t[0] - g[0], -(t[1] - g[1])))  # clockwise from straight up, y-down canvas
            frames.append(dict(x=round(float(g[0]), 1), y=round(float(g[1]), 1), angle=round(ang, 1), s=s,
                               sx=-1 if d == "left" else 1, behind=bool(behind), hidden=False, fallback=False))
        for (fn, fd, ff), fx in cfg.get("fixes", {}).items():
            if fn == n and (fd == d or (fd == "right" and d == "left")) and fx.get("fallback"):
                frames[ff]["fallback"] = True  # HeroRenderer draws the baked armed frame here
        anchors.append(dict(part=part, anim=f"melee{n}", dir=d, frames=frames))
        for hidden in cfg["hidden"]:
            anchors.append(dict(part=hidden, anim=f"melee{n}", dir=d,
                                frames=[dict(x=96, y=96, angle=0, s=0.3, sx=1, behind=False, hidden=True, fallback=False)] * COLS))

    out_armed.save(os.path.join(RES, "Sprites", cfg["armed"], f"melee{n}.png"))
    out_bare.save(os.path.join(RES, "Sprites", cfg["bare"], f"melee{n}.png"))
    data_path = os.path.join(RES, "Data", f"{cfg['gear']}-melee.json")
    text = open(data_path).read() if os.path.exists(data_path) else '{"anchors": []}'
    data = json.loads(text)
    # New anchors take the place of the old ones, so a re-run only changes what moved.
    keep = [i for i, a in enumerate(data["anchors"]) if a["anim"] == f"melee{n}"]
    at = keep[0] if keep else len(data["anchors"])
    data["anchors"] = [a for a in data["anchors"][:at] if a["anim"] != f"melee{n}"] + anchors + [a for a in data["anchors"][at:] if a["anim"] != f"melee{n}"]
    with open(data_path, "w") as f:
        json.dump(data, f, indent=1)
        if text.endswith("\n"):
            f.write("\n")

    # Preview: armed frame | weaponless + overlay drawn the way HeroRenderer.ApplyPart places it.
    for r, d in enumerate(["down", "up", "right"]):
        src_row = [0, 1, 3][r]
        for c in range(COLS):
            preview.alpha_composite(out_armed.crop((c * FRAME, src_row * FRAME, (c + 1) * FRAME, (src_row + 1) * FRAME)), (c * FRAME, r * FRAME))
            body = out_bare.crop((c * FRAME, src_row * FRAME, (c + 1) * FRAME, (src_row + 1) * FRAME))
            comp = body.copy()
            # Same draw order as HeroRenderer: behind layers (off first) under the body, front layers (off first, main last) over it.
            for behind_pass in (True, False):
                for part in parts:
                    a = next(x for x in anchors if x["part"] == part and x["dir"] == d)["frames"][c]
                    if a["hidden"] or a["behind"] != behind_pass:
                        continue
                    k = a["s"] * cfg["draw_scale"]
                    w = art.resize((max(1, round(art.width * k)), max(1, round(art.height * k))), Image.LANCZOS)
                    pad = Image.new("RGBA", (w.width * 3, w.height * 3))
                    pad.paste(w, (w.width, w.height))
                    pivot = (w.width + gx * k, w.height + gy * k)
                    rot = pad.rotate(-a["angle"], center=pivot, resample=Image.BICUBIC)
                    layer = Image.new("RGBA", (FRAME, FRAME))
                    place(layer, rot, a["x"] - pivot[0], a["y"] - pivot[1])
                    if behind_pass:
                        under = Image.new("RGBA", (FRAME, FRAME)); under.alpha_composite(layer); under.alpha_composite(comp); comp = under
                    else:
                        comp.alpha_composite(layer)
            preview.alpha_composite(comp, ((COLS + c) * FRAME, r * FRAME))
    shots = os.path.join(ROOT, "plans", "reports", "melee-shots")
    os.makedirs(shots, exist_ok=True)
    preview.save(os.path.join(shots, f"{cls}-melee{n}.png"))
    print(f"{cls} melee{n}: scale {scale:.3f}, cuts x {xcuts} y {ycuts}, weapon s {s}, length {length:.1f}px")
    for d in ["down", "up", "right"]:
        fmt = lambda f: "-" if f is None else f"({f[0][0]:.0f},{f[0][1]:.0f}) {'B' if f[2] else 'F'}"
        print(" ", d, [" / ".join(f"{k}:{fmt(v)}" for k, v in f.items()) for f in lines[d]] if dual else [fmt(f) for f in lines[d]])


if __name__ == "__main__":
    main()

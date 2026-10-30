"""
Builds a melee attack animation for the Unity hero from a pair of ChatGPT sheets (6 frames x 3 rows: down, up, right):
  Assets/ArtLibrary/anim/<class>/attack<N>-armed.png       (weapon drawn solid, only used to MEASURE the weapon)
  Assets/ArtLibrary/anim/<class>/attack<N>-weaponless.png  (the body the game draws)
Outputs, in the game's 192px frame format (6 columns x 4 rows: down, up, left = mirrored right, right):
  Resources/Sprites/<armedFolder>/melee<N>.png   baked armed frames (renderer fallback when an anchor is missing)
  Resources/Sprites/<bareFolder>/melee<N>.png    weaponless body frames
  Resources/Data/<gearFile>-melee.json            per-frame weapon anchors (merged: other anims kept)
  plans/reports/melee-shots/<class>-melee<N>.png  preview: armed | weaponless + measured weapon overlay

Weapon anchor = straight line fitted (RANSAC + PCA) to the pixels the armed sheet adds over the weaponless one; the grip end is the end
nearest the gold crossguard (warrior) or the fist; s = weapon length / art grip-to-tip length; behind = the body covers the blade.
Usage: python Tools/build-melee-sheets.py warrior 2
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "ArtLibrary", "anim")
RES = os.path.join(ROOT, "Assets", "Resources")
FRAME = 192
COLS, ROWS = 6, 3
DIRS = ["down", "up", "right"]  # sheet rows; "left" is the mirrored right row

# Per class: sprite folders, the anchor file, the weapon part + its tier-0 art (grip-to-tip length), parts hidden during the anim,
# and the reference body (bare idle, facing down) whose height and feet the new frames are scaled to.
CLASSES = {
    "warrior": dict(armed="hero", bare="bare", gear="held-gear", part="sword", art="sword/iron-base.png", grip=(54, 221),
                    draw_scale=1.4, hidden=["shield"], gold=True),
    "assassin": dict(armed="assassin", bare="assassin-bare", gear="held-assassin", part="main", art="assassin-dagger/t1-base.png",
                     grip=None, draw_scale=1.0, hidden=[], gold=False),
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
    # Grip end: nearest the gold crossguard if there is one, else nearest the body centre.
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
    # Behind: along the blade, the weaponless body is opaque and unchanged in the armed frame => the body covers it.
    behind = front = 0
    for k in np.linspace(0.15, 0.95, 24):
        x, y = (grip + (end1 - grip) * k).round().astype(int)
        if 0 <= y < armed.shape[0] and 0 <= x < armed.shape[1] and b_al[y, x] > 200:
            if diff[y, x] < 45:
                behind += 1
            else:
                front += 1
    return grip, end1, behind > front and behind >= 4


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

    ref = np.asarray(Image.open(os.path.join(RES, "Sprites", cfg["bare"], "idle.png")).convert("RGBA"))[:FRAME, :FRAME, 3]
    rx0, rx1, ry0, ry1 = bbox(ref)
    ref_h, ref_feet, ref_cx = ry1 - ry0, ry1, (rx0 + rx1) / 2

    def cell(img, r, c):
        return img[ycuts[r]:ycuts[r + 1], xcuts[c]:xcuts[c + 1]]

    ready = bbox(cell(bare, 0, COLS - 1)[..., 3])
    scale = ref_h / (ready[3] - ready[2])

    art = Image.open(os.path.join(RES, "Sprites", cfg["art"])).convert("RGBA")
    gx, gy = cfg["grip"]
    art_len = gy  # tier-0 art: tip at the top edge, grip at gy

    out_armed = Image.new("RGBA", (FRAME * COLS, FRAME * 4))
    out_bare = Image.new("RGBA", (FRAME * COLS, FRAME * 4))
    preview = Image.new("RGBA", (FRAME * COLS * 2, FRAME * 3), (40, 48, 60, 255))
    lines = {}
    for r, d in enumerate(DIRS):
        rb = bbox(cell(bare, r, COLS - 1)[..., 3])  # this row's ready frame sets the row's feet line and centre
        feet, cx = rb[3], (rb[0] + rb[1]) / 2
        out_row = [0, 1, 3][r]  # down 0, up 1, right 3 (left 2 is mirrored below)
        frames = []
        for c in range(COLS):
            a, b = cell(armed, r, c), cell(bare, r, c)
            bb = bbox(b[..., 3])
            body_centre = np.array([(bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2]) if bb else np.array([cx, feet - 100.0])
            line = weapon_line(a, b, cfg["gold"], body_centre)
            # cell px -> frame px
            ox, oy = ref_cx - cx * scale, ref_feet - feet * scale
            size = (max(1, round(a.shape[1] * scale)), max(1, round(a.shape[0] * scale)))
            for src, dst in ((a, out_armed), (b, out_bare)):
                im = Image.fromarray(src.astype(np.uint8)).resize(size, Image.LANCZOS)
                frame = Image.new("RGBA", (FRAME, FRAME))
                place(frame, im, ox, oy)
                dst.paste(frame, (c * FRAME, out_row * FRAME))
            if line is None:
                frames.append(None)
            else:
                grip, tip, behind = line
                g = grip * scale + [ox, oy]
                t = tip * scale + [ox, oy]
                frames.append((g, t, behind))
        lines[d] = frames
    # Left row = mirrored right row.
    for dst in (out_armed, out_bare):
        right = dst.crop((0, 3 * FRAME, FRAME * COLS, 4 * FRAME))
        for c in range(COLS):
            dst.paste(right.crop((c * FRAME, 0, (c + 1) * FRAME, FRAME)).transpose(Image.FLIP_LEFT_RIGHT), (c * FRAME, 2 * FRAME))
    lines["left"] = [None if f is None else (np.array([FRAME - f[0][0], f[0][1]]), np.array([FRAME - f[1][0], f[1][1]]), f[2]) for f in lines["right"]]

    lengths = [np.linalg.norm(f[1] - f[0]) for fs in lines.values() for f in fs if f is not None]
    length = float(np.median(lengths)) if lengths else 70.0
    s = round(length / (art_len * cfg["draw_scale"]), 3)
    anchors = []
    for d in ["down", "up", "left", "right"]:
        frames = []
        for f in lines[d]:
            if f is None:
                frames.append(dict(x=96, y=96, angle=0, s=s, sx=1, behind=False, hidden=True, fallback=False))
                continue
            g, t, behind = f
            ang = math.degrees(math.atan2(t[0] - g[0], -(t[1] - g[1])))  # clockwise from straight up, y-down canvas
            frames.append(dict(x=round(float(g[0]), 1), y=round(float(g[1]), 1), angle=round(ang, 1), s=s,
                               sx=-1 if d == "left" else 1, behind=bool(behind), hidden=False, fallback=False))
        anchors.append(dict(part=cfg["part"], anim=f"melee{n}", dir=d, frames=frames))
        for hidden in cfg["hidden"]:
            anchors.append(dict(part=hidden, anim=f"melee{n}", dir=d,
                                frames=[dict(x=96, y=96, angle=0, s=0.3, sx=1, behind=False, hidden=True, fallback=False)] * COLS))

    out_armed.save(os.path.join(RES, "Sprites", cfg["armed"], f"melee{n}.png"))
    out_bare.save(os.path.join(RES, "Sprites", cfg["bare"], f"melee{n}.png"))
    data_path = os.path.join(RES, "Data", f"{cfg['gear']}-melee.json")
    data = json.load(open(data_path)) if os.path.exists(data_path) else {"anchors": []}
    data["anchors"] = [a for a in data["anchors"] if a["anim"] != f"melee{n}"] + anchors
    json.dump(data, open(data_path, "w"), indent=1)

    # Preview: armed frame | weaponless + overlay drawn the way HeroRenderer.ApplyPart places it.
    for r, d in enumerate(["down", "up", "right"]):
        src_row = [0, 1, 3][r]
        for c in range(COLS):
            preview.alpha_composite(out_armed.crop((c * FRAME, src_row * FRAME, (c + 1) * FRAME, (src_row + 1) * FRAME)), (c * FRAME, r * FRAME))
            body = out_bare.crop((c * FRAME, src_row * FRAME, (c + 1) * FRAME, (src_row + 1) * FRAME))
            a = anchors[["down", "up", "left", "right"].index(d) * (1 + len(cfg["hidden"]))]["frames"][c]
            layer = Image.new("RGBA", (FRAME, FRAME))
            if not a["hidden"]:
                k = a["s"] * cfg["draw_scale"]
                w = art.resize((max(1, round(art.width * k)), max(1, round(art.height * k))), Image.LANCZOS)
                pad = Image.new("RGBA", (w.width * 3, w.height * 3))
                pad.paste(w, (w.width, w.height))
                pivot = (w.width + gx * k, w.height + gy * k)
                rot = pad.rotate(-a["angle"], center=pivot, resample=Image.BICUBIC)
                place(layer, rot, a["x"] - pivot[0], a["y"] - pivot[1])
            comp = Image.new("RGBA", (FRAME, FRAME))
            if a["behind"]:
                comp.alpha_composite(layer); comp.alpha_composite(body)
            else:
                comp.alpha_composite(body); comp.alpha_composite(layer)
            preview.alpha_composite(comp, ((COLS + c) * FRAME, r * FRAME))
    shots = os.path.join(ROOT, "plans", "reports", "melee-shots")
    os.makedirs(shots, exist_ok=True)
    preview.save(os.path.join(shots, f"{cls}-melee{n}.png"))
    print(f"{cls} melee{n}: scale {scale:.3f}, cuts x {xcuts} y {ycuts}, weapon s {s}, length {length:.1f}px")
    for d in ["down", "up", "right"]:
        print(" ", d, ["-" if f is None else f"({f[0][0]:.0f},{f[0][1]:.0f}) {'B' if f[2] else 'F'}" for f in lines[d]])


if __name__ == "__main__":
    main()

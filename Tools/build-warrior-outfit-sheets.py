"""Warrior outfit sheets (one full body per armour tier, the designer's choice: every 10 levels a different outfit).
Each outfit is ChatGPT's repaint of the CURRENT body rows (Lv1 Leather Vest) with the same poses, so the gear anchors and the fist
positions of body/bare + body/hand + held-gear-melee.json are reused as they are; only the pixels change.

Sources: Assets/ArtLibrary/classes/warrior/outfits/<outfit>/src/<job>.png on flat #00FF00, jobs listed in JOBS
(reference strips sent to ChatGPT are in .../<outfit>/ref/, made from the current bare rows).
Every painted figure is fitted onto the old frame of the same cell: scaled so its height (hair top to sole) equals the old figure's,
soles on the old sole row, hair centred on the old hair centre. The fist layer is cut from the new figure inside the old fist mask.
Idle rows are one painted pose with the breathing lift of build-warrior-move.py (AI frames of one pose jitter).

Writes Assets/Resources/Sprites/classes/warrior/body/<outfit>/{bare,hand}/<anim>.png: rows not painted yet stay as the Lv1 rows.

Usage (project root):
  python Tools/build-warrior-outfit-sheets.py check <outfit> OUT.png    old vs new for every painted row, composed with gear
  python Tools/build-warrior-outfit-sheets.py write <outfit>"""
import os, sys, importlib
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
sys.path.insert(0, ROOT + "/Tools")
chf = importlib.import_module("compose-hero-frame")

FRAME = 192
BODY = "Assets/Resources/Sprites/classes/warrior/body"
ART = "Assets/ArtLibrary/classes/warrior/outfits"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
LIFT = [0.0, -0.7, -1.4, -0.7]   # idle breathing, as build-warrior-move.py
# job file -> list of (anim, dir, frames): the figures in the painting, left to right, map to these cells in order.
JOBS = {
    "idle-4dirs": [("idle", "down", None), ("idle", "up", None), ("idle", "left", None), ("idle", "right", None)],
    "idle-down": [("idle", "down", None)],
    "walk-down": [("walk", "down", [0, 1, 2, 3])],
    "walk-up": [("walk", "up", [0, 1, 2, 3])],
    "walk-right": [("walk", "right", [0, 1, 2, 3])],
    "melee1-down": [("melee1", "down", list(range(6)))],
    "melee1-up": [("melee1", "up", list(range(6)))],
    "melee1-left": [("melee1", "left", list(range(6)))],
    "melee1-right": [("melee1", "right", list(range(6)))],
}
MIRROR = {("walk", "left"): ("walk", "right")}   # rows made by mirroring another painted row (as the Lv1 walk left is)


def figures(path):
    """Painted figures left to right as RGBA crops (green keyed), the small specks dropped."""
    a = np.array(Image.open(path).convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = (g > 1.3 * np.maximum(r, b)) & (g > 110)
    fg = ~bg
    fg[:4] = fg[-4:] = False; fg[:, :4] = fg[:, -4:] = False
    lab, n = ndimage.label(ndimage.binary_closing(fg, iterations=2))
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    keep = [i + 1 for i in range(n) if sizes[i] > 0.15 * sizes.max()]
    out = []
    for k in keep:
        ys, xs = np.where(lab == k)
        y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
        crop = a[y0:y1 + 1, x0:x1 + 1].copy()
        m = (lab[y0:y1 + 1, x0:x1 + 1] == k) & fg[y0:y1 + 1, x0:x1 + 1]
        crop[..., 1] = np.minimum(crop[..., 1], np.maximum(crop[..., 0], crop[..., 2]) * 1.05)  # green spill
        crop[..., 3] = np.where(m, 255, 0)
        out.append((x0, Image.fromarray(crop.astype(np.uint8))))
    return [im for _, im in sorted(out, key=lambda t: t[0])]


def box(cell):
    a = np.asarray(cell)[..., 3] > 20
    ys, xs = np.where(a)
    top, bot = ys.min(), ys.max()
    head = xs[ys < top + 0.2 * (bot - top)]
    return top, bot, (head.min() + head.max()) / 2


def fit(fig, old_cell):
    """New figure scaled and placed on the old cell: same height, soles on the same row, hair centred the same."""
    top, bot, hx = box(old_cell)
    a = np.asarray(fig)[..., 3] > 20
    ys, xs = np.where(a)
    ftop, fbot = ys.min(), ys.max()
    k = (bot - top + 1) / (fbot - ftop + 1)
    fig = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
    a = np.asarray(fig)[..., 3] > 20
    ys, xs = np.where(a)
    head = xs[ys < ys.min() + 0.2 * (ys.max() - ys.min())]
    fhx = (head.min() + head.max()) / 2
    out = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    out.alpha_composite(fig, (int(round(hx - fhx)), int(round(bot - ys.max()))))
    return out


def lifted(cell, lift):
    """Breathing: everything above the knees moves up by `lift` px, a blend down to the ankles (sub-pixel through resampling)."""
    a = np.asarray(cell).astype(np.float32)
    ys = np.where(a[..., 3] > 20)[0]
    if lift == 0 or len(ys) == 0: return cell
    top, bot = ys.min(), ys.max()
    knee = top + 0.75 * (bot - top)
    out = np.zeros_like(a)
    for y in range(FRAME):
        w = 1.0 if y < knee else max(0.0, 1 - (y - knee) / max(1, bot - knee))
        sy = y - lift * w
        y0 = int(np.floor(sy)); t = sy - y0
        if 0 <= y0 < FRAME - 1: out[y] = a[y0] * (1 - t) + a[y0 + 1] * t
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))


def cell(sheet, row, f):
    return sheet.crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))


def build(outfit):
    """{anim: (bare sheet, hand sheet or None)} with every painted row replaced."""
    sheets = {}
    def sheet(anim):
        if anim not in sheets:
            bare = Image.open(f"{BODY}/bare/{anim}.png").convert("RGBA")
            hp = f"{BODY}/hand/{anim}.png"
            sheets[anim] = [bare.copy(), bare, Image.open(hp).convert("RGBA") if os.path.exists(hp) else None]
        return sheets[anim]
    done = set()
    for job, cells in JOBS.items():
        path = f"{ART}/{outfit}/src/{job}.png"
        if not os.path.exists(path): continue
        figs = figures(path)
        if job.startswith("idle"):
            if len(figs) != len(cells): print(f"  {job}: {len(figs)} figures, expected {len(cells)}: skipped"); continue
            pairs = [(anim, d, None, fig) for (anim, d, _), fig in zip(cells, figs)]
        else:
            anim, d, frames = cells[0]
            if len(figs) != len(frames): print(f"  {job}: {len(figs)} figures, expected {len(frames)}: skipped"); continue
            pairs = [(anim, d, f, fig) for f, fig in zip(frames, figs)]
        for anim, d, f, fig in pairs:
            new, old, _ = sheet(anim)
            row = ROWS[d]
            for fi in (range(new.width // FRAME) if f is None else [f]):
                oc = cell(old, row, fi)
                if np.asarray(oc)[..., 3].max() == 0: continue
                nc = fit(fig, cell(old, row, 0) if f is None else oc)
                if f is None: nc = lifted(nc, LIFT[fi % 4])
                new.paste(nc, (fi * FRAME, row * FRAME))
            done.add((anim, d))
    for (anim, d), (sa, sd) in MIRROR.items():
        if (sa, sd) in done and (anim, d) not in done:
            new = sheet(anim)[0]
            for fi in range(new.width // FRAME):
                src = cell(new, ROWS[sd], fi).transpose(Image.FLIP_LEFT_RIGHT)
                new.paste(src, (fi * FRAME, ROWS[d] * FRAME))
            done.add((anim, d))
    out = {}
    for anim, (new, old, hand) in sheets.items():
        newhand = None
        if hand is not None:   # fist layer: the new pixels inside the old fist mask
            m = np.asarray(hand)[..., 3] > 0
            nh = np.asarray(new).copy(); nh[~m] = 0
            newhand = Image.fromarray(nh)
        out[anim] = (new, newhand)
    return out, done


def check(outfit, path):
    sheets, done = build(outfit)
    rows = []
    for anim, d in sorted(done):
        n = Image.open(f"{BODY}/bare/{anim}.png").width // FRAME
        for which in ("old", "new"):
            frames = []
            for fi in range(n):
                if which == "old": frames.append(chf.compose("warrior", anim, d, fi, pad=30))
                else:
                    tmpb, tmph = f"C:/temp/outfit-{anim}-bare.png", f"C:/temp/outfit-{anim}-hand.png"
                    if not os.path.exists(tmpb + ".done"):
                        sheets[anim][0].save(tmpb)
                        if sheets[anim][1] is not None: sheets[anim][1].save(tmph)
                        open(tmpb + ".done", "w").close()
                    frames.append(chf.compose("warrior", anim, d, fi, pad=30, bare_path=tmpb, hand_path=tmph if sheets[anim][1] is not None else None))
            rows.append(frames)
    for f in os.listdir("C:/temp"):
        if f.startswith("outfit-") and f.endswith(".done"): os.remove("C:/temp/" + f)
    S = FRAME + 60
    W = max(len(r) for r in rows)
    im = Image.new("RGBA", (S * W, S * len(rows)), (58, 62, 70, 255))
    for r, fr in enumerate(rows):
        for k, f in enumerate(fr): im.alpha_composite(f, (k * S, r * S))
    im.save(path)
    print(path, sorted(done))


def write(outfit):
    sheets, done = build(outfit)
    for sub in ("bare", "hand"):
        os.makedirs(f"{BODY}/{outfit}/{sub}", exist_ok=True)
    for anim, (bare, hand) in sheets.items():
        bare.save(f"{BODY}/{outfit}/bare/{anim}.png")
        if hand is not None: hand.save(f"{BODY}/{outfit}/hand/{anim}.png")
    print("written", outfit, sorted(done))


if __name__ == "__main__":
    {"check": lambda: check(sys.argv[2], sys.argv[3]), "write": lambda: write(sys.argv[2])}[sys.argv[1]]()

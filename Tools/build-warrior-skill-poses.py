"""Warrior skill pose sheets body/{bare,armed}/{inferno,berserker}.png in the new style, frame by frame from the old sheets.

    python build-warrior-skill-poses.py backup         copy the old sheets to ArtLibrary/.../new-style/skill-poses/old (never overwrites)
    python build-warrior-skill-poses.py measure        cell-by-cell boxes of the old sheets
    python build-warrior-skill-poses.py ref <anim>     upload sheet: the old sheet on #00FF00, sized for ChatGPT's image
    python build-warrior-skill-poses.py cut <anim> [--dry] [--src=path]   new sheet into the game files, each frame fitted to the old frame's box

The code picks these frames by index and effects line up with them, so frame order, cell size and the box of each frame stay as they were.
bare and armed are the same picture (bodies without weapons), as the old pairs were.
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
BODY = ROOT / "Assets/Resources/Sprites/classes/warrior/body"
LIB = ROOT / "Assets/ArtLibrary/classes/warrior/new-style/skill-poses"
OLD, SRC, REF = LIB / "old", LIB / "src", LIB / "ref"
CELL = 192
ROWS = 4  # down, up, left, right
# anim -> (columns, size of the image ChatGPT makes)
ANIMS = {"inferno": (5, (1536, 1024)), "berserker": (3, (1024, 1536))}
STRETCH = 1.08


def backup():
    for anim in ANIMS:
        for kind in ("bare", "armed"):
            dst = OLD / kind / f"{anim}.png"
            if not dst.exists():
                dst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(BODY / kind / f"{anim}.png", dst)


def bbox(a, thr=8):
    ys, xs = np.where(a > thr)
    return (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1) if len(xs) else None


def old_sheet(anim):
    return Image.open(OLD / "bare" / f"{anim}.png").convert("RGBA")


def layout(anim):
    """Scale and top-left of the old sheet inside ChatGPT's image."""
    cols, (w, h) = ANIMS[anim]
    s = min(w / (cols * CELL), h / (ROWS * CELL))
    return s, (w - cols * CELL * s) / 2, (h - ROWS * CELL * s) / 2


def measure():
    for anim, (cols, _) in ANIMS.items():
        a = np.asarray(old_sheet(anim))[..., 3]
        for r in range(ROWS):
            print(anim, "row", r, [bbox(a[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL]) for c in range(cols)])


def ref(anim):
    cols, (w, h) = ANIMS[anim]
    s, ox, oy = layout(anim)
    REF.mkdir(parents=True, exist_ok=True)
    old = old_sheet(anim)
    small = old.resize((round(old.width * s), round(old.height * s)), Image.LANCZOS)
    sheet = Image.new("RGBA", (w, h), (0, 255, 0, 255))
    sheet.alpha_composite(small, (round(ox), round(oy)))
    sheet.convert("RGB").save(REF / f"{anim}.png")
    print(REF / f"{anim}.png", sheet.size, "scale", s)


def key_green(rgb):
    h, w = rgb.shape[:2]
    corners = np.concatenate([rgb[:12, :12].reshape(-1, 3), rgb[:12, -12:].reshape(-1, 3), rgb[-12:, :12].reshape(-1, 3), rgb[-12:, -12:].reshape(-1, 3)])
    key = np.median(corners, axis=0)
    excess = rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2])
    full = max(float(key[1] - max(key[0], key[2])), 1.0)
    alpha = 1 - np.clip((excess - 0.10 * full) / (0.70 * full), 0, 1)
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0), 0, 255)
    out[..., 1] = np.minimum(out[..., 1], np.maximum(out[..., 0], out[..., 2]))
    return np.dstack([out, alpha * 255])


def fit(art, box):
    """art: RGBA float array, tight crop. Premultiplied resize onto the old box; a slight stretch lands both axes on it."""
    bw, bh = box[2] - box[0], box[3] - box[1]
    ah, aw = art.shape[:2]
    s = min(bw / aw, bh / ah)
    w, h = max(1, round(aw * min(bw / aw, s * STRETCH))), max(1, round(ah * min(bh / ah, s * STRETCH)))
    pm = art.copy()
    pm[..., :3] *= art[..., 3:] / 255
    res = np.dstack([np.asarray(Image.fromarray(pm[..., c], "F").resize((w, h), Image.LANCZOS)) for c in range(4)])
    a = np.clip(res[..., 3:], 0, 255)
    res[..., :3] = np.where(a > 0.5, res[..., :3] * 255 / np.maximum(a, 0.5), 0)
    res[..., 3:] = a
    return Image.fromarray(np.clip(res + 0.5, 0, 255).astype(np.uint8), "RGBA")


def cut(anim, dry=False, src=None):
    cols, (w, h) = ANIMS[anim]
    im = Image.open(src or SRC / f"{anim}.png").convert("RGB")
    if im.size != (w, h):
        print("note: ChatGPT size", im.size, "resized to", (w, h))
        im = im.resize((w, h), Image.LANCZOS)
    full = key_green(np.asarray(im).astype(np.float32))
    s, ox, oy = layout(anim)
    old = np.asarray(old_sheet(anim))[..., 3]
    sheet = Image.new("RGBA", (cols * CELL, ROWS * CELL), (0, 0, 0, 0))
    # ChatGPT drifts off the grid, so a frame is its connected blobs, handed to the cell its centre of mass falls in; a straight cut would slice feet and hair.
    labels, n = ndimage.label(ndimage.binary_dilation(full[..., 3] > 24, iterations=2))
    owner = {}
    for i in range(1, n + 1):
        ys, xs = np.where(labels == i)
        if len(xs) < 30:
            continue
        c, r = int((xs.mean() - ox) / (CELL * s)), int((ys.mean() - oy) / (CELL * s))
        owner.setdefault((r, c), []).append(i)
    for r in range(ROWS):
        for c in range(cols):
            mine = owner.get((r, c), [])
            art = full.copy()
            art[..., 3] *= np.isin(labels, mine)
            b = bbox(art[..., 3], 24)
            box = bbox(old[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL])
            box = (max(box[0], 1), max(box[1], 1), min(box[2], CELL - 1), min(box[3], CELL - 1))
            frame = fit(art[b[1]:b[3], b[0]:b[2]], box)
            bw, bh = box[2] - box[0], box[3] - box[1]
            sheet.alpha_composite(frame, (c * CELL + box[0] + (bw - frame.width) // 2, r * CELL + box[1] + (bh - frame.height) // 2))
            print(anim, r, c, "blobs", len(mine), "art", b, "old box", box, "frame", frame.size)
    if dry:
        return
    # the resize can ring a little key green back into the outline; this art has no green
    arr = np.array(sheet)
    arr[..., 1] = np.minimum(arr[..., 1], np.maximum(arr[..., 0], arr[..., 2]).astype(np.int32).clip(0, 249) + 6)
    sheet = Image.fromarray(arr, "RGBA")
    for kind in ("bare", "armed"):
        sheet.save(BODY / kind / f"{anim}.png")


if __name__ == "__main__":
    cmd, args = sys.argv[1], [a for a in sys.argv[2:] if not a.startswith("--")]
    opts = dict(a[2:].split("=", 1) if "=" in a else (a[2:], True) for a in sys.argv[2:] if a.startswith("--"))
    if cmd == "backup":
        backup()
    elif cmd == "measure":
        measure()
    elif cmd == "ref":
        for a in args or ANIMS:
            ref(a)
    elif cmd == "cut":
        cut(args[0], dry="dry" in opts, src=opts.get("src"))

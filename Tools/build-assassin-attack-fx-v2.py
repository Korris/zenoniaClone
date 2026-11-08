"""Assassin basic-attack VFX v2, cut from the ChatGPT sheet drawn after the board docs/reference/assassin-basic-attack-board.png (2026-11-08).
Source: Assets/ArtLibrary/classes/assassin/vfx/attack-v2/assassin-attack-fx-v2-chatgpt-1.png, 5 rows x 4 frames on flat #00FF00
(chat Tay https://chatgpt.com/c/6af01f05-5c34-83ec-a10b-4d48236051be, ref board crop ref-board-fx-sheet.png). Rows -> AssassinAttackFx rows:
  0 crescent slash -> slash_N  (hit 1, drawn facing right, pivot = centre of the row's union box)
  1 rising arc     -> rise_N   (hit 2, same pivot)
  2 X cross        -> cross_N  (hit 3, same pivot)
  3 stab streak    -> stab_N   (hit 4, pointing right, pivot = left end (tail) of the row's union box, mid height)
  4 hit spark      -> spark_N  (hit 4 tip, centred)
Every frame of a row is cropped with the same box (cell coordinates), so the frames stay in place relative to each other.
Output: Assets/Resources/Sprites/classes/assassin/fx/attack-v2/<row>_N.png (+ .meta cloned from the Mechanic v2 set, new guid) and
Assets/Resources/Data/classes/assassin/vfx-attack-v2.json (w, h, pivot px from the bottom-left). The game sizes each row from the hit's reach
(AssassinAttackFx.Fit), so the output resolution only sets the sharpness.
  python Tools/build-assassin-attack-fx-v2.py cut       frames, json, metas
  python Tools/build-assassin-attack-fx-v2.py preview OUT.png   rows on a dark and a snow-white strip, next to an in-game hero frame
"""
import json, os, re, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

os.chdir("D:/zenoniaClone")
SRC = "Assets/ArtLibrary/classes/assassin/vfx/attack-v2/assassin-attack-fx-v2-chatgpt-1.png"
OUT = "Assets/Resources/Sprites/classes/assassin/fx/attack-v2"
DATA = "Assets/Resources/Data/classes/assassin/vfx-attack-v2.json"
META_SRC = "Assets/Resources/Sprites/classes/mechanic/fx/attack-v2/cast_0.png.meta"
ROWS, COLS = ["slash", "rise", "cross", "stab", "spark"], 4
SCALE = 0.55  # source px -> output px


def key_green(path):
    """Straight RGBA from art on flat #00FF00: alpha from the distance to the key colour; the art has no green, so G is clamped to
    max(R, B) everywhere (docs/sprite-rules.md 2026-11-08: half-transparent glows mix with the key)."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 40) / 90, 0, 1)
    rgb = np.dstack([r, np.minimum(g, np.maximum(r, b)), b])
    alpha[:4], alpha[-4:], alpha[:, :4], alpha[:, -4:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.3, np.ones((3, 3)))
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha[np.isin(lab, 1 + np.flatnonzero(sizes < 25))] = 0
    return np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8)


PAD = 40  # rows are cropped this far into the neighbouring rows, keeping only the blobs this cell owns


def owned(img, owner, idx, y0, y1, x0, x1):
    y0, y1 = max(0, y0), min(img.shape[0], y1)
    part = img[y0:y1, x0:x1].copy()
    part[..., 3] = np.where(owner[y0:y1, x0:x1] == idx, part[..., 3], 0)
    return part


def cut():
    img = key_green(SRC)
    H, W = img.shape[:2]
    ch, cw = H / len(ROWS), W / COLS
    # every blob belongs to the cell its centre falls in (the big spark pokes into the row above, a hard grid cut clipped it)
    lab, n = ndimage.label(ndimage.binary_dilation(img[..., 3] > 20, iterations=3), np.ones((3, 3)))
    owner = np.full(lab.shape, -1)
    for i, (cy, cx) in enumerate(ndimage.center_of_mass(np.ones_like(lab), lab, range(1, n + 1))):
        owner[lab == i + 1] = min(len(ROWS) - 1, int(cy // ch)) * COLS + min(COLS - 1, int(cx // cw))
    os.makedirs(OUT, exist_ok=True)
    meta_text = open(META_SRC, encoding="utf-8").read()
    frames = []
    for r, name in enumerate(ROWS):
        cells = [owned(img, owner, r * COLS + c, int(r * ch) - PAD, int((r + 1) * ch) + PAD, int(c * cw), int((c + 1) * cw)) for c in range(COLS)]
        h0, w0 = min(c.shape[0] for c in cells), min(c.shape[1] for c in cells)
        cells = [c[:h0, :w0] for c in cells]
        union = np.zeros((h0, w0), bool)
        for c in cells:
            union |= c[..., 3] > 20
        ys, xs = np.nonzero(union)
        x0, x1, y0, y1 = max(0, xs.min() - 3), min(w0, xs.max() + 4), max(0, ys.min() - 3), min(h0, ys.max() + 4)
        for i, c in enumerate(cells):
            im = Image.fromarray(c[y0:y1, x0:x1], "RGBA")
            im = im.resize((max(1, round(im.width * SCALE)), max(1, round(im.height * SCALE))), Image.LANCZOS)
            fname = f"{name}_{i}"
            im.save(f"{OUT}/{fname}.png")
            mp = f"{OUT}/{fname}.png.meta"
            if not os.path.exists(mp):
                open(mp, "w", encoding="utf-8", newline="").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta_text, count=1))
            px = 0.0 if name == "stab" else im.width / 2
            frames.append({"name": fname, "w": im.width, "h": im.height, "px": round(px, 1), "py": round(im.height / 2, 1)})
        print(name, "box", (x1 - x0, y1 - y0), "->", frames[-1]["w"], frames[-1]["h"])
    json.dump({"frames": frames}, open(DATA, "w", encoding="utf-8"), indent=1)
    if not os.path.exists(DATA + ".meta"):
        print("note: Unity creates", DATA + ".meta on import")
    print(DATA)


def preview(path):
    """Each row on dark and snow-white backgrounds, with an idle hero frame at game scale for size."""
    tiles = []
    for name in ROWS:
        row = [Image.open(f"{OUT}/{name}_{i}.png").convert("RGBA") for i in range(COLS)]
        tiles.append(row)
    cw = max(t.width for row in tiles for t in row) + 10
    chh = max(t.height for row in tiles for t in row) + 10
    out = Image.new("RGBA", (cw * COLS * 2, chh * len(ROWS)), (0, 0, 0, 255))
    for half, bg in enumerate([(40, 44, 54, 255), (235, 240, 248, 255)]):
        out.paste(Image.new("RGBA", (cw * COLS, chh * len(ROWS)), bg), (half * cw * COLS, 0))
        for r, row in enumerate(tiles):
            for c, t in enumerate(row):
                out.alpha_composite(t, (half * cw * COLS + c * cw + (cw - t.width) // 2, r * chh + (chh - t.height) // 2))
    out.convert("RGB").save(path)
    print(path, out.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cut":
        cut()
    elif cmd == "preview":
        preview(sys.argv[2])
    else:
        print(__doc__)

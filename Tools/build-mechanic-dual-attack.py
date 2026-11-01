"""Mechanic dual pistols: cuts the attack body rows (attack3 down, hit 4 down, up, right) from the raw ChatGPT rows into 192 px sheets. Art only: writes under new-style/dual-src/attack/ and OUT, never Resources.
Hit 4 is called attack4 in code. Fists come from FISTS below, read by eye from the grids and checked on the marked sheet.

Usage (project root; Pillow, numpy, scipy):
  python Tools/build-mechanic-dual-attack.py cut            cut sheets, fists.txt, measurements
  python Tools/build-mechanic-dual-attack.py grid OUT       cut frames with a pixel grid, for reading fists
  python Tools/build-mechanic-dual-attack.py contact OUT    each row under the old row, with fists marked, and one row per cut sheet at game size
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

spec = importlib.util.spec_from_file_location("bm", "Tools/build-mechanic-new-style.py")
bm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bm)
wm = bm.wm

FRAME, HEIGHT, SOLE = 192, 164, 188
DIR = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src/attack"
OLD = "Assets/ArtLibrary/classes/mechanic/new-style/pre-dual/bare/attack3.png"
# cut sheet: (raw image, row of the old attack3 to compare with)
ROWS = {"attack3-down": ("raw-attack3-down", 0), "hit4-down": ("raw-attack4-down-v2", 0), "hit4-up": ("raw-attack4-up", 1), "hit4-right": ("raw-attack4-right", 3)}
COUNT = 6
# per frame: (A, B), cell pixels, read from the grids. Which fist is A: see NOTES.
FISTS = {
    "attack3-down": [((45, 120), (137, 120)), ((50, 120), (143, 120)), ((51, 125), (139, 125)), ((53, 106), (133, 117)), ((51, 122), (143, 122)), ((46, 120), (138, 120))],
    "hit4-down": [((132, 80), (146, 84)), ((118, 127), (128, 117)), ((51, 147), (121, 118)), ((47, 135), (115, 118)), ((55, 148), (125, 118)), ((124, 128), (134, 118))],
    "hit4-up": [((55, 85), (59, 62)), ((135, 93), (135, 73)), ((51, 92), (149, 63)), ((45, 92), (143, 60)), ((53, 92), (150, 63)), ((59, 92), (58, 72))],
    "hit4-right": [((83, 75), (53, 72)), ((138, 100), (140, 75)), ((78, 120), (141, 114)), ((93, 118), (145, 112)), ((78, 120), (141, 113)), ((93, 113), (131, 110))],
}
NOTES = {
    "attack3-down": "facing the camera: A is on the left of the picture (the hero's right hand), B on the right of the picture (his left hand)",
    "hit4-down": "facing the camera: frames 2-4 A is the low arm on the left of the picture (his right hand), B the raised fist (his left hand). Frames 0, 1, 5 both fists are pressed together on the right of the picture, so A and B are close and their centres are the least certain",
    "hit4-up": "seen from behind: frames 2-4 A is on the left of the picture (his left hand), B the raised arm (his right hand). Frames 0, 1, 5 both fists are on one side, A lower, B upper. Most of a fist is hidden in this view, the centre is the round glove end",
    "hit4-right": "side view: A is the near arm, B the far arm",
}


def frames_of(name):
    """Connected pieces, the six largest as seeds; every other piece goes to the seed whose box centre is nearest. Never a straight cut (fault log: berserker, idle down)."""
    pm = wm.key_green(np.array(Image.open(f"{DIR}/{name}.png").convert("RGB")))
    pm[:8], pm[-8:], pm[:, :8], pm[:, -8:] = 0, 0, 0, 0
    lab, n = ndimage.label(pm[..., 3] > 128, structure=np.ones((3, 3)))
    boxes = ndimage.find_objects(lab)
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    seeds = sorted(np.argsort(sizes)[-COUNT:], key=lambda i: boxes[i][1].start)
    centre = lambda i: ((boxes[i][1].start + boxes[i][1].stop) / 2, (boxes[i][0].start + boxes[i][0].stop) / 2)
    owner = np.zeros(n + 1, int)
    for i in range(n):
        x, y = centre(i)
        owner[i + 1] = 1 + min(range(COUNT), key=lambda s: abs(centre(seeds[s])[0] - x)) if sizes[i] >= 3 else 0
    out = []
    for s in range(COUNT):
        m = ndimage.binary_dilation(owner[lab] == s + 1, iterations=3)
        cut = pm * m[..., None]
        out.append(wm.trimmed(cut))
    return out, [int(sizes[i]) for i in seeds], n


def row_cells(name):
    raw, sizes, pieces = frames_of(name)
    k = HEIGHT / max(f.shape[0] for f in raw)
    frames = [wm.resize_pm(f, k) for f in raw]
    hc = [bm.hair_center(f) for f in frames]
    base = bm.HEAD_X - hc[0]
    cells, lost = [], []
    for i, f in enumerate(frames):
        cell, clipped = bm.place(f, base + hc[0] - hc[i])
        cells.append(cell)
        if clipped:
            lost.append(i)
    return cells, k, lost, sizes, pieces


def sheet_of(cells):
    s = np.zeros((FRAME, FRAME * COUNT, 4), np.uint8)
    for f, c in enumerate(cells):
        s[:, f * FRAME:(f + 1) * FRAME] = wm.to_rgba(c)
    return s


def cut():
    target = bm.hair_target()
    print(f"hair size of idle down at {HEIGHT} px: {target:.1f}")
    for out, (name, _) in ROWS.items():
        cells, k, lost, sizes, pieces = row_cells(name)
        Image.fromarray(sheet_of(cells), "RGBA").save(f"{DIR}/{out}.png")
        tops = [int(np.where((c[..., 3] > 128).any(1))[0][0]) for c in cells]
        soles = [int(np.where((c[..., 3] > 128).any(1))[0][-1]) for c in cells]
        widths = [(int(np.where((c[..., 3] > 128).any(0))[0][0]), int(np.where((c[..., 3] > 128).any(0))[0][-1])) for c in cells]
        hair = [bm.hair_size(c) for c in cells]
        print(f"{out:13s} k {k:.3f} pieces {pieces} seeds {sizes} tops {tops} soles {soles} x-range {widths} clipped {lost}")
        print(f"{'':13s} hair {[round(h, 1) for h in hair]} median/target {np.median(hair) / target:.3f}")
    write_fists()


def write_fists():
    with open(f"{DIR}/fists.txt", "w", encoding="utf-8") as fp:
        fp.write("Fist centres, cell pixels in a 192 px cell, read from a grid by eye (about 3 px), checked on a marked sheet. The row note says which fist is A and which is B.\n\n")
        for out, rows in FISTS.items():
            fp.write(f"{out}: {NOTES[out]}\n")
            for f, (a, b) in enumerate(rows):
                fp.write(f"  frame {f}: A {a[0]} {a[1]}  B {b[0]} {b[1]}\n")
            fp.write("\n")


def grid(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    z = 3
    for out in ROWS:
        im = Image.open(f"{DIR}/{out}.png").convert("RGBA")
        for row_part, fr in (("a", range(3)), ("b", range(3, 6))):
            w = Image.new("RGB", (FRAME * z * 3, FRAME * z), (58, 62, 70))
            d = ImageDraw.Draw(w)
            for j, f in enumerate(fr):
                c = im.crop((f * FRAME, 0, (f + 1) * FRAME, FRAME)).resize((FRAME * z, FRAME * z), Image.NEAREST)
                w.paste(c, (j * FRAME * z, 0), c)
                for v in range(0, FRAME, 16):
                    strong = v % 48 == 0
                    col = (255, 255, 0) if strong else (120, 120, 120)
                    d.line([(j * FRAME * z + v * z, 0), (j * FRAME * z + v * z, FRAME * z)], fill=col)
                    d.line([(j * FRAME * z, v * z), ((j + 1) * FRAME * z, v * z)], fill=col)
                    d.text((j * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                    d.text((j * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
            w.save(f"{out_dir}/grid-{out}-{row_part}.png")


def contact(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    old = Image.open(OLD).convert("RGBA")
    ref = Image.open("Assets/ArtLibrary/classes/mechanic/new-style/pre-dual/bare/attack2.png").convert("RGBA").crop((0, 0, FRAME, FRAME))
    sheet = Image.new("RGB", (FRAME * 7, FRAME * 2 * len(ROWS)), (58, 62, 70))
    for r, (out, (_, old_row)) in enumerate(ROWS.items()):
        new = Image.open(f"{DIR}/{out}.png").convert("RGBA")
        d = ImageDraw.Draw(sheet)
        for f in range(COUNT):
            c = old.crop((f * FRAME, old_row * FRAME, (f + 1) * FRAME, (old_row + 1) * FRAME))
            sheet.paste(c, ((f + 1) * FRAME, r * 2 * FRAME), c)
            n = new.crop((f * FRAME, 0, (f + 1) * FRAME, FRAME))
            sheet.paste(n, ((f + 1) * FRAME, (r * 2 + 1) * FRAME), n)
            for p, col in zip(FISTS.get(out, [(None, None)] * COUNT)[f], ((0, 255, 255), (255, 0, 255))):
                if p:
                    x, y = (f + 1) * FRAME + p[0], (r * 2 + 1) * FRAME + p[1]
                    d.ellipse([x - 7, y - 7, x + 7, y + 7], outline=col)
                    d.line([x - 2, y, x + 2, y], fill=col)
                    d.line([x, y - 2, x, y + 2], fill=col)
        sheet.paste(ref, (0, (r * 2 + 1) * FRAME), ref)
        d.line([(0, (r * 2 + 1) * FRAME + SOLE), (FRAME * 7, (r * 2 + 1) * FRAME + SOLE)], fill=(90, 90, 90))
        d.text((4, r * 2 * FRAME + 4), f"old attack3 row {old_row}\nthen {out} below;\nbeside it the current\nattack2 down for scale", fill=(255, 255, 0))
    sheet.save(f"{out_dir}/contact.png")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "cut":
        cut()
    elif cmd == "grid":
        grid(sys.argv[2])
    elif cmd == "contact":
        contact(sys.argv[2])

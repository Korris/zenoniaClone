"""Compare strips + GIF of the Warrior facing-camera TEST rigs from `--record-rig` captures (60 fps, 360 frames each):
frame art (no flag) / cut-out rig (`--hero-rig`) / mesh rig (`--hero-rig-mesh`), same exe, same scene.

  python Tools/compose-warrior-rig-mesh-compare.py CAP_DIR OUT_DIR [attack_start]
CAP_DIR holds frames/, cut/, mesh/ (frame_00000.png ...). attack_start = first frame of a swing (default: found from the mesh run:
the first frame after 215 where the hero changes a lot from the frame before it). Writes compare-{idle,walk,attack}.jpg
(rows: frames, cut-out, mesh; every Nth frame, ms labels) and compare-3way-{walk,attack}.gif (3 columns, 30 fps).
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

CROP = (490, 210, 790, 610)
ROWS = [("frames", "frame art (now)"), ("cut", "cut-out rig --hero-rig"), ("mesh", "MESH rig --hero-rig-mesh")]


def load(cap, run, i):
    return Image.open(os.path.join(cap, run, "frame_%05d.png" % i)).convert("RGB").crop(CROP)


def find_attack(cap):
    prev = np.asarray(load(cap, "mesh", 224), np.int16)
    for i in range(225, 330):
        cur = np.asarray(load(cap, "mesh", i), np.int16)
        if np.abs(cur - prev).mean() > 6.0:
            return i - 1
        prev = cur
    return 222


def strip(cap, out, name, frames, step_ms):
    w, h = CROP[2] - CROP[0], CROP[3] - CROP[1]
    sheet = Image.new("RGB", (w * len(frames), (h + 16) * len(ROWS) + 16), (20, 20, 20))
    d = ImageDraw.Draw(sheet)
    for r, (run, label) in enumerate(ROWS):
        y = 16 + r * (h + 16)
        d.text((4, y - 13), label, fill=(255, 230, 120))
        for c, i in enumerate(frames):
            sheet.paste(load(cap, run, i), (c * w, y))
            if r == 0:
                d.text((c * w + 4, 2), "%d ms" % (c * step_ms), fill=(255, 255, 255))
    sheet.save(os.path.join(out, "compare-%s.jpg" % name), quality=88)


def gif(cap, out, name, frames):
    imgs = []
    for i in frames:
        row = Image.new("RGB", ((CROP[2] - CROP[0]) * 3, CROP[3] - CROP[1]))
        for c, (run, _l) in enumerate(ROWS):
            row.paste(load(cap, run, i), (c * (CROP[2] - CROP[0]), 0))
        imgs.append(row)
    imgs[0].save(os.path.join(out, "compare-3way-%s.gif" % name), save_all=True, append_images=imgs[1:], duration=33, loop=0)


def main():
    cap, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    a0 = int(sys.argv[3]) if len(sys.argv) > 3 else find_attack(cap)
    strip(cap, out, "idle", list(range(22, 82, 5)), 83)
    strip(cap, out, "walk", list(range(150, 186, 3)), 50)
    strip(cap, out, "attack", list(range(a0, a0 + 26, 2)), 33)
    gif(cap, out, "walk", list(range(150, 210, 2)))
    gif(cap, out, "attack", list(range(a0 - 4, a0 + 40, 2)))
    print("attack starts at frame", a0)


if __name__ == "__main__":
    main()

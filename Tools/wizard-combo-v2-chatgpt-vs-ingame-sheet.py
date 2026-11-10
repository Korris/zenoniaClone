"""Wizard combo v2, facing down: the ChatGPT strip (staff row) above the game frame that shows at the same moment, per hit.
Capture = --record-combo wizard --face down (Tools/capture-wizard-basic-attack-ingame.py run, any window size: the hero is at the centre).
Game frame of painted frame i of a hit = attack start + hit start + i * (hit ms / frame count), 60 fps.

  python Tools/wizard-combo-v2-chatgpt-vs-ingame-sheet.py CAPTURE_DIR/down OUT.png
"""
import importlib.util
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/classes/wizard/attack-board/northwind-combo-v2"
FPS, ATTACK_FROM = 60.0, 30
HITS = [("Crystal Thrust", 0, 350, 6), ("Frost Sweep", 350, 350, 6), ("Glacier Slam", 700, 350, 6), ("Blizzard Nova", 1050, 640, 8)]  # ms, ComboConfigs.WizardHits 2026-11-10
HALF = (170, 250, 170, 170)   # crop around the window centre: left, up, right, down
CELL_H = 420


def key_green(im):
    a = np.asarray(im.convert("RGBA")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    a[..., 3] = np.clip((np.maximum(np.maximum(r, b), 255 - g) - 40) / 70, 0, 1) * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def pieces(path, count):
    fig = key_green(Image.open(path))
    al = np.asarray(fig)[..., 3] > 100
    cols = al.sum(0) > 0
    runs, start = [], None
    for x, v in enumerate(cols):
        if v and start is None: start = x
        if not v and start is not None: runs.append((start, x)); start = None
    if start is not None: runs.append((start, len(cols)))
    while len(runs) > count:
        runs.remove(min(runs, key=lambda r: r[1] - r[0]))
    while len(runs) < count:   # a staff reaching into the next frame joins two runs: halve the widest
        s0, e0 = max(runs, key=lambda r: r[1] - r[0]); i = runs.index((s0, e0)); m = (s0 + e0) // 2
        runs[i:i + 1] = [(s0, m), (m, e0)]
    out = []
    for s, e in runs:
        im = fig.crop((s, 0, e, fig.height)); out.append(im.crop(im.getbbox()))
    return out


def main(folder, out):
    first = ATTACK_FROM + 1
    any_frame = Image.open(f"{folder}/frame_{first:05d}.png"); W, H = any_frame.size
    crop = (W // 2 - HALF[0], H // 2 - HALF[1], W // 2 + HALF[2], H // 2 + HALF[3])
    cw = crop[2] - crop[0]
    blocks = []
    for i, (name, start, ms, n) in enumerate(HITS, 1):
        painted = pieces(f"{SRC}/hit{i}-down-staff.png", n)
        top = Image.new("RGB", (cw * n, CELL_H), (40, 44, 52)); bot = Image.new("RGB", (cw * n, CELL_H), (20, 22, 30))
        dr = ImageDraw.Draw(bot)
        for f in range(n):
            p = painted[f]; k = (CELL_H - 20) / p.height
            p = p.resize((max(1, round(p.width * k)), CELL_H - 20), Image.LANCZOS)
            top.paste(p, (f * cw + (cw - p.width) // 2, 10), p)
            t_ms = start + f * ms / n
            g = first + round(t_ms / 1000.0 * FPS)
            im = Image.open(f"{folder}/frame_{g:05d}.png").convert("RGB").crop(crop)
            bot.paste(im, (f * cw, 0)); dr.text((f * cw + 4, 4), f"{round(t_ms)} ms", fill=(255, 255, 0))
        ImageDraw.Draw(top).text((6, 4), f"{i}. {name}: ChatGPT (trên) / trong game (dưới)", fill=(255, 255, 255))
        blocks += [top, bot]
    sheet = Image.new("RGB", (max(b.width for b in blocks), sum(b.height for b in blocks)), (20, 22, 30))
    y = 0
    for b in blocks:
        sheet.paste(b, (0, y)); y += b.height
    sheet.save(out); print(out, sheet.size)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

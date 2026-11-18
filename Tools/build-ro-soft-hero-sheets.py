"""RO Soft hero sheets for the game (2026-11-19): weapon and hair baked in, 8 directions, default outfit only.

Sources: ArtLibrary/classes/<class>/ro-soft-8dir/
  cut/idle-<dir>-raw.png                      approved idle per direction (from build-ro-soft-8dir-pilot.py cut)
  src/<class>-ro-anim-walk-<a>--<b>-chatgpt-N.png    2 rows x 4 frames: row 1 = direction a, row 2 = direction b
  src/<class>-ro-anim-attack-<a>--<b>-chatgpt-N.png  2 rows x ATTACK_FRAMES frames, same layout
Output: Resources/Sprites/classes/<class>/body/soft/{idle,walk,attack}.png, rows in DIRS order, CELL_W x CELL_H cells,
feet centre on (CELL_W/2, FEET_Y); Resources/Data/classes/<class>/soft-sheet.json (cell, feet, body height, frame counts).
Every figure is scaled by its own sheet row so the row median height = BODY_PX (attack: BODY_PX x the class attack/idle
ratio measured on the concept sheets), because ChatGPT draws each image at its own scale.
Commands: write <class|all> | check <class>  (check writes docs/reference/ro-soft-sheets/<class>-sheets-check.png)
"""
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from importlib import import_module

pilot = import_module("build-ro-soft-8dir-pilot")  # key_green, figures, art()

ROOT = Path(__file__).resolve().parent.parent
DIRS = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
CELL_W, CELL_H, FEET_Y = 320, 256, 244
BODY_PX = 200  # idle figure height in the cell = 20.5 world units (hero height today), so PPU = BODY_PX / 20.5
CLASSES = pilot.CLASSES
SRC_RE = re.compile(r"^(?P<c>[a-z]+)-ro-anim-(?P<anim>walk|attack)-(?P<a>[a-z-]+)--(?P<b>[a-z-]+)-chatgpt-(?P<n>\d+)\.png$")


def feet_x(fig):
    """Centre of the opaque pixels in the bottom 10 % (the feet), so a sword held out sideways does not shift the body."""
    al = np.asarray(fig)[..., 3] > 128
    h = al.shape[0]
    xs = np.nonzero(al[int(h * 0.9):])[1]
    return xs.mean() if len(xs) else fig.width / 2


def place(fig, scale):
    f = fig.resize((max(1, round(fig.width * scale)), max(1, round(fig.height * scale))), Image.LANCZOS)
    cell = Image.new("RGBA", (CELL_W, CELL_H))
    x, y = round(CELL_W / 2 - feet_x(f)), FEET_Y - f.height
    if y < 0 or x < 0 or x + f.width > CELL_W:
        print(f"  warn: figure {f.size} overflows the {CELL_W}x{CELL_H} cell at ({x},{y})")
    cell.alpha_composite(f, (x, y)) if x >= 0 and y >= 0 else cell.paste(f, (x, y), f)
    return cell


def grid_rows(path, per_row):
    """Figures of a 2-row sheet, split by the vertical centre of their boxes, each row left to right."""
    figs = pilot.figures_with_pos(pilot.key_green(Image.open(path)), per_row * 2)
    mids = [y + f.height / 2 for f, _, y in figs]
    cut = np.median(mids)
    top = [f for (f, _, _), m in zip(figs, mids) if m < cut]
    bottom = [f for (f, _, _), m in zip(figs, mids) if m >= cut]
    if len(top) != per_row or len(bottom) != per_row:
        raise SystemExit(f"{path.name}: rows of {len(top)} / {len(bottom)} figures, expected {per_row}")
    return top, bottom


def attack_ratio(cls):
    cut = pilot.art(cls) / "cut"
    hs = lambda pose: [Image.open(cut / f"{pose}-{d}-raw.png").height for d in pilot.RIGHT5]
    return float(np.mean(hs("attack")) / np.mean(hs("idle")))


def build(cls):
    art = pilot.art(cls)
    frames = {"idle": {}, "walk": {}, "attack": {}}
    for d in DIRS:
        raw = Image.open(art / "cut" / f"idle-{d}-raw.png")
        frames["idle"][d] = [place(raw, BODY_PX / raw.height)]
    ratio = attack_ratio(cls)
    for src in sorted((art / "src").glob(f"{cls}-ro-anim-*-chatgpt-*.png")):
        m = SRC_RE.match(src.name)
        if not m:
            continue
        anim = m["anim"]
        per_row = 4 if anim == "walk" else ATTACK_FRAMES
        for d, row in zip((m["a"], m["b"]), grid_rows(src, per_row)):
            target = BODY_PX if anim == "walk" else BODY_PX * ratio
            s = target / float(np.median([f.height for f in row]))
            frames[anim][d] = [place(f, s) for f in row]  # a later file (higher N) replaces the same direction
    return frames


ATTACK_FRAMES = 4


def write(cls):
    frames = build(cls)
    out = ROOT / f"Assets/Resources/Sprites/classes/{cls}/body/soft"
    out.mkdir(parents=True, exist_ok=True)
    counts = {}
    for anim, by_dir in frames.items():
        missing = [d for d in DIRS if d not in by_dir]
        if missing:
            print(f"{cls} {anim}: missing {missing}, sheet not written")
            continue
        n = max(len(v) for v in by_dir.values())
        sheet = Image.new("RGBA", (CELL_W * n, CELL_H * len(DIRS)))
        for r, d in enumerate(DIRS):
            for k, cell in enumerate(by_dir[d]):
                sheet.alpha_composite(cell, (k * CELL_W, r * CELL_H))
        sheet.save(out / f"{anim}.png")
        counts[anim] = n
        print(f"{cls} {anim}: {n} frames x 8 directions")
    meta = {"cellW": CELL_W, "cellH": CELL_H, "feetY": FEET_Y, "bodyPx": BODY_PX, "dirs": DIRS, "frames": counts}
    dst = ROOT / f"Assets/Resources/Data/classes/{cls}/soft-sheet.json"
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(json.dumps(meta, indent=1), encoding="utf-8")


def check(cls):
    """Contact sheet: every row of every anim on a mid floor colour with the feet line, at half size."""
    frames = build(cls)
    rows = [(a, d) for a in ("idle", "walk", "attack") for d in DIRS if d in frames[a]]
    n = max(len(frames[a][d]) for a, d in rows)
    out = Image.new("RGBA", (CELL_W * n // 2, CELL_H * len(rows) // 2), (205, 196, 176, 255))
    dr = ImageDraw.Draw(out)
    for r, (a, d) in enumerate(rows):
        y0 = r * CELL_H // 2
        dr.line((0, y0 + FEET_Y // 2, out.width, y0 + FEET_Y // 2), fill=(150, 140, 120, 255))
        dr.text((4, y0 + 4), f"{a} {d}", fill=(40, 40, 40, 255))
        for k, cell in enumerate(frames[a][d]):
            c = cell.resize((CELL_W // 2, CELL_H // 2), Image.LANCZOS)
            out.alpha_composite(c, (k * CELL_W // 2, y0))
    dst = ROOT / f"docs/reference/ro-soft-sheets/{cls}-sheets-check.png"
    dst.parent.mkdir(parents=True, exist_ok=True)
    out.convert("RGB").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    cmd, which = sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "all"
    for c in CLASSES if which == "all" else [which]:
        {"write": write, "check": check}[cmd](c)

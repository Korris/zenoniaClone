"""Contact sheet of an in-game `--record-choreo astral-cataclysm` capture (60 fps frames, cast on frame 24).

  python Tools/build-light-skill-capture-sheet.py <frames_dir> <out.png> [label] [--crop x0,y0,x1,y1] [--ms 0,100,...]

Picks the frames at the given ms after the cast, crops (default: whole frame), labels each tile with its time, 4 tiles per row.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

CAST_FRAME, FPS, COLS = 24, 60, 4
DEFAULT_MS = [0, 100, 200, 300, 450, 600, 800, 1000, 1200, 1400, 1600, 1800]


def main():
    args = sys.argv[1:]
    crop = None; ms = DEFAULT_MS
    if "--crop" in args:
        i = args.index("--crop"); crop = tuple(int(v) for v in args[i + 1].split(",")); del args[i:i + 2]
    if "--ms" in args:
        i = args.index("--ms"); ms = [int(v) for v in args[i + 1].split(",")]; del args[i:i + 2]
    frames, out = Path(args[0]), Path(args[1])
    label = args[2] if len(args) > 2 else ""
    tiles = []
    for t in ms:
        f = frames / ("frame_%05d.png" % (CAST_FRAME + round(t * FPS / 1000)))
        im = Image.open(f).convert("RGB")
        if crop: im = im.crop(crop)
        d = ImageDraw.Draw(im)
        d.rectangle([0, 0, 150, 22], fill=(0, 0, 0))
        d.text((6, 5), "%s %d ms" % (label, t), fill=(255, 255, 255))
        tiles.append(im)
    w, h = tiles[0].size
    rows = (len(tiles) + COLS - 1) // COLS
    sheet = Image.new("RGB", (w * COLS, h * rows), (20, 20, 20))
    for k, im in enumerate(tiles): sheet.paste(im, ((k % COLS) * w, (k // COLS) * h))
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()

"""Contact sheet of an in-game portal capture (`--shots t0,t0+0.15,... --shot-path P` writes P_1.png ... P_N.png).

  python Tools/build-portal-fx-capture-sheet.py <shot_prefix> <out.jpg> [label] [--crop x0,y0,x1,y1] [--step 0.15] [--cols 5]

Crops every shot (default: whole frame), labels each tile with its time from the first shot, `cols` tiles per row. Used for the map portal light
(MapPortalLightFx) in docs/reference/portal-fx/: a blink or a jumping spot shows up as one tile differing from its neighbours.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw


def main():
    args = sys.argv[1:]
    crop = None; step = 0.15; cols = 5
    for key in ("--crop", "--step", "--cols"):
        if key in args:
            i = args.index(key); v = args[i + 1]; del args[i:i + 2]
            if key == "--crop": crop = tuple(int(x) for x in v.split(","))
            elif key == "--step": step = float(v)
            else: cols = int(v)
    prefix, out = args[0], Path(args[1])
    label = args[2] if len(args) > 2 else ""
    shots = []
    n = 1
    while Path(f"{prefix}_{n}.png").exists():
        shots.append(Path(f"{prefix}_{n}.png")); n += 1
    if not shots: sys.exit("no shots at " + prefix + "_1.png")
    tiles = []
    for i, f in enumerate(shots):
        im = Image.open(f).convert("RGB")
        if crop: im = im.crop(crop)
        d = ImageDraw.Draw(im)
        d.rectangle((0, 0, 92, 18), fill=(0, 0, 0))
        d.text((4, 3), f"+{i * step:.2f} s", fill=(255, 255, 255))
        tiles.append(im)
    w, h = tiles[0].size
    rows = (len(tiles) + cols - 1) // cols
    head = 24 if label else 0
    sheet = Image.new("RGB", (w * cols, h * rows + head), (20, 20, 24))
    if label: ImageDraw.Draw(sheet).text((6, 6), label, fill=(255, 255, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % cols) * w, head + (i // cols) * h))
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out, quality=88)
    print(out, len(tiles), "shots")


if __name__ == "__main__":
    main()

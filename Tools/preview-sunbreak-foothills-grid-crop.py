"""Zoomed crop of the Sunbreak Foothills ground with the tile grid, every tile labelled, and (optionally) the collision rows tinted.
Usage: python Tools/preview-sunbreak-foothills-grid-crop.py x0 y0 x1 y1 out.png [--rows]   (tile rect, inclusive; 96 px per tile)
Used to hand-check stairs, bridges and ledge gaps when writing layout-sunbreak/sunbreak-foothills-rows-override.txt."""
import os, sys, importlib.util
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("rows", os.path.join(HERE, "build-sunbreak-foothills-rows.py")); rows_mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(rows_mod)
Z = 96

def crop(x0, y0, x1, y1, out, with_rows):
    full = rows_mod.ground()
    im = full.crop((x0 * 64, y0 * 64, (x1 + 1) * 64, (y1 + 1) * 64)).resize(((x1 - x0 + 1) * Z, (y1 - y0 + 1) * Z), Image.LANCZOS).convert("RGBA")
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    rows = [l.strip() for l in open(rows_mod.OUT)] if with_rows else None
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px, py = (x - x0) * Z, (y - y0) * Z
            if rows:
                ch = rows[y][x]
                fill = {"C": (255, 0, 0, 80), "w": (0, 40, 255, 100), "B": (255, 240, 0, 70), "s": (255, 240, 0, 70)}.get(ch)
                if fill: d.rectangle([px, py, px + Z - 1, py + Z - 1], fill=fill)
            d.rectangle([px, py, px + Z, py + Z], outline=(0, 0, 0, 120))
            d.text((px + 3, py + 2), "%d,%d" % (x, y), fill=(255, 255, 255, 255), stroke_width=1, stroke_fill=(0, 0, 0, 255))
    Image.alpha_composite(im, ov).convert("RGB").save(out)

if __name__ == "__main__":
    a = sys.argv
    crop(int(a[1]), int(a[2]), int(a[3]), int(a[4]), a[5], "--rows" in a)

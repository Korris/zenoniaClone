"""Cut a ChatGPT 2x2 four-view prop sheet on #FF00FF into the game's directional prop sprites (View25DDirectionalProps).

Sheet layout (as asked from ChatGPT): top-left = FRONT (seen from the south), top-right = BACK (from the north),
bottom-left = from the WEST end, bottom-right = from the EAST end. Every view is scaled by the same factor, so the front comes out
`--front-width` px wide (the width of the prop's current sprite, i.e. its SIZE in tiles x 64) and the others keep their size relative to it.
Output: Assets/Resources/Sprites/village/props/<type>.png (front), <type>_n.png, <type>_w.png, <type>_e.png, each cropped to its art,
transparent, bottom edge = the feet (sprites are pivoted bottom-centre).

    python Tools/cut-prop-4-views.py <sheet.png> <type> --front-width 160
    python Tools/cut-prop-4-views.py <diagonal-sheet.png> <type> --front-width 160 --diagonals <axis-sheet.png>   (_sw _se _ne _nw)
"""
import argparse
import os
import numpy as np
from PIL import Image

OUT = "Assets/Resources/Sprites/village/props"


def key_magenta(rgb):
    """Alpha from the distance to #FF00FF (soft edge) and magenta spill removed from the edge pixels."""
    a = rgb.astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = np.clip((np.minimum(r, b) - g) / 255.0, 0, 1)          # 1 on pure magenta, 0 on wood / metal
    alpha = np.clip((0.62 - mag) / 0.25, 0, 1)
    out = a.copy()
    fringe = alpha < 1                                              # pull r / b toward g on the soft edge: no pink halo
    out[..., 0] = np.where(fringe, np.minimum(r, g + (r - g) * alpha), r)
    out[..., 2] = np.where(fringe, np.minimum(b, g + (b - g) * alpha), b)
    return np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)


def crop(rgba):
    ys, xs = np.nonzero(rgba[..., 3] > 24)
    return rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("sheet"); ap.add_argument("type"); ap.add_argument("--front-width", type=int, required=True)
    ap.add_argument("--diagonals", help="this sheet holds the 4 diagonal views; the axis sheet (for the scale) is given here")
    a = ap.parse_args()
    def quads_of(path, layout):
        im = np.asarray(Image.open(path).convert("RGB"))
        h, w = im.shape[:2]
        return {k: crop(key_magenta(im[qy * h // 2:(qy + 1) * h // 2, qx * w // 2:(qx + 1) * w // 2])) for k, (qx, qy) in layout.items()}
    axis = {"": (0, 0), "_n": (1, 0), "_w": (0, 1), "_e": (1, 1)}
    # diagonal sheet: top-left from the south-west, top-right south-east; ChatGPT's bottom pair came out as seen from the NORTH-EAST (left) and
    # NORTH-WEST (right) - the near end of the bench is on the left in the left cell - so the cells are read by their perspective, not their label
    diag = {"_sw": (0, 0), "_se": (1, 0), "_ne": (0, 1), "_nw": (1, 1)}
    if a.diagonals:
        s = a.front_width / quads_of(a.diagonals, {"": (0, 0)})[""].shape[1]   # same px per metre as the axis views
        views = quads_of(a.sheet, diag)
    else:
        views = quads_of(a.sheet, axis)
        s = a.front_width / views[""].shape[1]
    for k, v in views.items():
        img = Image.fromarray(v, "RGBA")
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
        p = os.path.join(OUT, a.type + k + ".png")
        img.save(p)
        print(p, img.size)


if __name__ == "__main__":
    main()

"""Wizard V3: candidate fists (bare peach hands) per frame of a cut body sheet, drawn numbered on 3x grids for reading by eye.

  python Tools/wizard_v3_fist_candidates.py <anim> [rows]   -> docs/reference/character-v3/wizard-v3-test/check/fist-cands-<anim>-r<row>.png
Prints the candidates as JSON {"row,frame": [[x, y, area], ...]}. Skin is tightened against the cream / gold robe (red-green gap at
least 0.8 x the green-blue gap); the face (biggest skin blob in the top 45 % of the figure) and the legs (blobs reaching under row 200 or
taller than 26 px) are dropped. The Mage has bare hands, so the leather-glove detector of hero_layered_fists finds nothing.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SPR = ROOT / "Assets/Resources/Sprites/hero-layered/ro-fantasy/wizard/body"
OUT = ROOT / "docs/reference/character-v3/wizard-v3-test/check"
X0, X1, Y0, Y1, S = 60, 260, 40, 215, 3


def candidates(cell):
    a = np.asarray(cell).astype(np.float32) / 255
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    sk = (al > 0.5) & (r > 0.72) & (r > g) & (g > b) & (r - g >= 0.8 * (g - b)) & (r - b > 0.12) & (r - b < 0.45)
    ys = np.nonzero((al > 0.5).any(1))[0]
    top, h = ys[0], ys[-1] - ys[0]
    lab, n = ndimage.label(sk)
    blobs = []
    face, face_area = None, 0
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        if sl[0].start < top + 0.45 * h and m.sum() > face_area:
            face, face_area = i, m.sum()
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        area = int(m.sum())
        if i == face or area < 10 or sl[0].stop > 200 or sl[0].stop - sl[0].start > 26:
            continue
        yy, xx = np.nonzero(m)
        blobs.append([round(float(xx.mean() + sl[1].start), 1), round(float(yy.mean() + sl[0].start), 1), area])
    return blobs


def main(anim, rows=None):
    sh = Image.open(SPR / f"{anim}.png").convert("RGBA")
    n, nr = sh.width // 320, sh.height // 256
    res = {}
    cw, ch = (X1 - X0) * S, (Y1 - Y0) * S
    for r in (rows or range(nr)):
        img = Image.new("RGB", (cw * 4, ch * ((n + 3) // 4)), (200, 210, 190))
        d = ImageDraw.Draw(img)
        for f in range(n):
            cell = sh.crop((f * 320, r * 256, (f + 1) * 320, (r + 1) * 256))
            bl = candidates(cell)
            res[f"{r},{f}"] = bl
            ox, oy = (f % 4) * cw, (f // 4) * ch
            c = cell.crop((X0, Y0, X1, Y1)).resize((cw, ch), Image.NEAREST)
            bg = Image.new("RGBA", c.size, (200, 210, 190, 255))
            bg.alpha_composite(c)
            img.paste(bg.convert("RGB"), (ox, oy))
            for gx in range(X0, X1, 10):
                d.line((ox + (gx - X0) * S, oy, ox + (gx - X0) * S, oy + ch), fill=(0, 0, 0) if gx % 50 == 0 else (150, 150, 150))
                d.text((ox + (gx - X0) * S + 1, oy + 1), str(gx), fill=(200, 0, 0))
            for gy in range(Y0, Y1, 10):
                d.line((ox, oy + (gy - Y0) * S, ox + cw, oy + (gy - Y0) * S), fill=(0, 0, 0) if gy % 50 == 0 else (150, 150, 150))
                d.text((ox + 1, oy + (gy - Y0) * S + 1), str(gy), fill=(0, 0, 200))
            for k, (x, y, ar) in enumerate(bl):
                X, Y = ox + (x - X0) * S, oy + (y - Y0) * S
                d.ellipse((X - 7, Y - 7, X + 7, Y + 7), outline=(255, 0, 0), width=2)
                d.text((X + 8, Y - 8), f"{k}:{int(x)},{int(y)}", fill=(255, 0, 0))
            d.text((ox + 40, oy + 14), f"r{r} f{f}", fill=(0, 0, 0))
            d.rectangle((ox, oy, ox + cw - 1, oy + ch - 1), outline=(255, 0, 255), width=2)
        img.save(OUT / f"fist-cands-{anim}-r{r}.png")
    print(json.dumps(res))


if __name__ == "__main__":
    main(sys.argv[1], [int(x) for x in sys.argv[2].split(",")] if len(sys.argv) > 2 else None)

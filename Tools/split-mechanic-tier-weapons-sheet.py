"""Split one ChatGPT image holding a whole Mechanic weapon tier (2026-11-07, ladder v3) into the two sources the tier tools read.

Input (portrait, flat #00FF00): row 1 the cannon side view (tail left, muzzle right); row 2 the cannon lying diagonally twice
(left: muzzle up-right toward the viewer, right: muzzle up-left); row 3 the pistol pair side by side (left normal, right enhanced).
Output, original pixels re-laid on #00FF00:
  new-style/dual-src/tiers/cannon/<tier>-chatgpt.png   side along the top, front below left, back below right (build-mechanic-cannon-tiers.py)
  new-style/dual-src/tiers/<tier>-pair-chatgpt.png     base above enhanced (build-mechanic-pistol-tiers.py)
An existing output is moved once into old-20261107-v2/ (the art before ladder v3) before it is replaced.

  python Tools/split-mechanic-tier-weapons-sheet.py SHEET.png TIER [--protect-green]

--protect-green (jade / emerald tier): green pixels inside the art (not joined to the background around it) are pushed to teal
(blue raised to green - 30) so the tier tools' #00FF00 key does not turn the emerald cores half transparent.
"""
import os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
TIERS_DIR = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src/tiers"
KEY = (0, 255, 0)
PAD = 28  # px of background kept around each piece (the enhanced glow and sparks)


def pieces(rgb, count=5):
    """Boxes (y0, y1, x0, x1) of the `count` biggest non-key pieces."""
    g, mx = rgb[..., 1], np.maximum(rgb[..., 0], rgb[..., 2])
    solid = (g - mx) < 90
    solid[:6], solid[-6:], solid[:, :6], solid[:, -6:] = False, False, False, False
    lab, n = ndimage.label(ndimage.binary_closing(solid, iterations=2), np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    best = 1 + np.argsort(sizes)[::-1][:count]
    print("pieces", n, "kept px", [int(sizes[i - 1]) for i in best], "next", int(np.sort(sizes)[::-1][count]) if n > count else 0)
    out = []
    for i in best:
        ys, xs = np.nonzero(lab == i)
        out.append((ys.min(), ys.max() + 1, xs.min(), xs.max() + 1))
    return out


def crop(im, box):
    y0, y1, x0, x1 = box
    return im.crop((max(0, x0 - PAD), max(0, y0 - PAD), min(im.width, x1 + PAD), min(im.height, y1 + PAD)))


def backup(path):
    old = os.path.join(os.path.dirname(path), "old-20261107-v2")
    if os.path.exists(path) and not os.path.exists(os.path.join(old, os.path.basename(path))):
        os.makedirs(old, exist_ok=True)
        shutil.copy2(path, old)


def protect_green(im):
    a = np.asarray(im).astype(np.int32)
    g, mx = a[..., 1], np.maximum(a[..., 0], a[..., 2])
    greenish = (g - mx) > 30
    lab, _ = ndimage.label(greenish)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    outer = np.isin(lab, border[border > 0])
    inner = greenish & ~ndimage.binary_dilation(outer, iterations=2)
    a[..., 2] = np.where(inner, np.maximum(a[..., 2], g - 30), a[..., 2])
    print("protect-green: px pushed to teal", int(inner.sum()))
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def main(sheet, tier, green=False):
    im = Image.open(sheet).convert("RGB")
    if green:
        im = protect_green(im)
    boxes = pieces(np.asarray(im).astype(np.float32))
    boxes.sort(key=lambda b: (b[0] + b[1]) / 2)
    side = boxes[0]
    front, back = sorted(boxes[1:3], key=lambda b: b[2])
    base, enh = sorted(boxes[3:5], key=lambda b: b[2])
    # cannon: side on top, front lower left, back lower right
    s, f, b = crop(im, side), crop(im, front), crop(im, back)
    W = max(s.width, f.width + b.width + 60) + 80
    H = s.height + max(f.height, b.height) + 120
    can = Image.new("RGB", (W, H), KEY)
    can.paste(s, ((W - s.width) // 2, 40))
    can.paste(f, (40, s.height + 80))
    can.paste(b, (W - 40 - b.width, s.height + 80))
    # pistols: base above enhanced
    p, e = crop(im, base), crop(im, enh)
    W2, H2 = max(p.width, e.width) + 80, p.height + e.height + 120
    pis = Image.new("RGB", (W2, H2), KEY)
    pis.paste(p, ((W2 - p.width) // 2, 40))
    pis.paste(e, ((W2 - e.width) // 2, p.height + 80))
    for out, img in ((f"{TIERS_DIR}/cannon/{tier}-chatgpt.png", can), (f"{TIERS_DIR}/{tier}-pair-chatgpt.png", pis)):
        backup(out)
        img.save(out)
        print("wrote", out, img.size)


if __name__ == "__main__":
    if len(sys.argv) not in (3, 4):
        print(__doc__)
    else:
        main(sys.argv[1], sys.argv[2], "--protect-green" in sys.argv[3:])

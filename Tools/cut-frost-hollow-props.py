"""Cuts the 22 Frost Hollow props (fh*) from the ChatGPT prop sheet (#FF00FF background) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-frost-hollow-props.py [--check]    (--check writes only the preview sheet on snow into plans/.../visuals)
Source: Assets/ArtLibrary/hd/map/frost-hollow/large/prop-sheet-chatgpt.png (1536x1024). Keying, despill, contact shadow, canvas layout (width a multiple of 16 centred on the
footprint, bottom = bottom of the footprint row, content lifted 16 px) are shared with Tools/cut-willowbrook-extra-props.py.
Scale: the original painting (frost-hollow-ground-original.png, 64 px/tile) has big pines about 3.9 tiles tall, crystal clusters ~1.5 tiles, rune stones ~2 tiles, stumps ~0.6 tile,
so SCALE per prop below brings the sheet's objects to those sizes. Prints the PropDef rows to paste into FrostHollowPropDefs.cs.
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
SHEET = os.path.join(ROOT, "Assets/ArtLibrary/hd/map/frost-hollow/large/prop-sheet-chatgpt.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
VIS = os.path.join(ROOT, "plans/261105-1240-frost-hollow-large-map/visuals")

def load(name, file):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, file))
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m

mm = load("mm", "build-meadow-monsters.py")
wb = load("wb", "cut-willowbrook-extra-props.py")
wb.SHEET = SHEET     # clean() reads the sheet through this global

# (name, scale, footprint w, footprint h in tiles); rows of the sheet, left to right
ROWS = [
    [("fhPineBig", 0.80, 1, 1), ("fhPineMid", 0.80, 1, 1), ("fhPineSlim", 0.80, 1, 1), ("fhPineSapling", 0.80, 1, 1), ("fhDeadTree", 0.80, 1, 1), ("fhDeadTreeWide", 0.80, 1, 1)],
    [("fhCrystalBig", 0.55, 1, 1), ("fhCrystalMid", 0.55, 1, 1), ("fhCrystalSmall", 0.55, 1, 1), ("fhRuneTall", 0.60, 1, 1), ("fhRuneShort", 0.60, 1, 1), ("fhBoulder", 0.60, 2, 1)],
    [("fhRock", 0.60, 1, 1), ("fhRockSmall", 0.60, 1, 1), ("fhStump", 0.45, 1, 1), ("fhBones", 0.50, 1, 1), ("fhRibcage", 0.50, 2, 1), ("fhFence", 0.50, 2, 1)],
    [("fhTowerTall", 0.60, 2, 1), ("fhTowerShort", 0.60, 1, 1), ("fhTent", 0.55, 3, 2), ("fhCampfire", 0.50, 1, 1)],
]
BANDS = [330, 560, 740]      # y of the row boundaries on the sheet


def components(rgba):
    a = rgba[..., 3] > 0.55
    lab, n = ndi.label(ndi.binary_dilation(a, iterations=3))
    rows = [[] for _ in ROWS]
    for i in range(1, n + 1):
        ys, xs = np.where((lab == i) & a)
        if len(xs) < 400: continue
        box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
        cy = (box[1] + box[3]) / 2
        rows[sum(cy > b for b in BANDS)].append(box)
    for r, want in zip(rows, ROWS):
        if len(r) != len(want): raise SystemExit("row has %d objects, expected %d" % (len(r), len(want)))
        r.sort(key=lambda b: b[0])
    return rows


def trim_shadow(rgba, box):
    """The sheet's background is slightly darker around every object, which clean() turns into a grey haze that shows on snow. Keep soft shadow only in the bottom 22% of the
    object (the contact shadow) and drop its alpha elsewhere; pixels of the object itself (alpha > 0.5) are untouched."""
    x0, y0, x1, y1 = box
    out = rgba.copy()
    ys = np.arange(out.shape[0])[:, None]
    soft = out[..., 3] <= 0.5
    dist = ndi.distance_transform_edt(soft)     # px to the nearest object pixel
    top = y1 - 0.22 * (y1 - y0)
    fade = np.clip(1.0 - dist / 16.0, 0, 1) * np.clip((ys - top) / (0.08 * (y1 - y0) + 1), 0, 1)   # feather out sideways and upwards
    out[..., 3] = np.where(soft, out[..., 3] * fade, out[..., 3])
    out[..., :3] *= (out[..., 3:4] > 0)
    return out


def main():
    rgba = wb.clean(mm.key_magenta(SHEET))
    sprites = []
    for row, boxes in zip(ROWS, components(rgba)):
        for (name, sc, fw, fh), box in zip(row, boxes):
            sprites.append((name, wb.cut(trim_shadow(rgba, box), box, sc, fw), fw, fh))
    os.makedirs(VIS, exist_ok=True)
    if "--check" not in sys.argv:
        for name, img, fw, fh in sprites: img.save(os.path.join(OUT, name + ".png"), optimize=True)
    for name, img, fw, fh in sprites:
        print('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (name, img.width / 64.0, img.height / 64.0, fw, fh))
    sheet = Image.new("RGBA", (1700, 900), (232, 238, 246, 255)); x = y = 10; rowh = 0
    for name, img, fw, fh in sprites:
        if x + img.width > 1690: x = 10; y += rowh + 10; rowh = 0
        sheet.alpha_composite(img, (x, y)); x += img.width + 10; rowh = max(rowh, img.height)
    sheet.convert("RGB").save(os.path.join(VIS, "fh-props-sheet.png"))


if __name__ == "__main__":
    main()

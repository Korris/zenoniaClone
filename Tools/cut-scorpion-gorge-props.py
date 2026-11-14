"""Cuts the 20 Scorpion Gorge props (sg*) from the ChatGPT prop sheet (5 x 4 objects on #FF00FF) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-scorpion-gorge-props.py [--check]    (--check writes only the preview sheet on sand to plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/visuals)
Source: Assets/ArtLibrary/maps/scorpion-gorge/scorpion-gorge-prop-sheet-chatgpt-1.png (1536x1024).
Keying, despill, shadow rebuild, component search and canvas rules are the Willowbrook cutter's (Tools/cut-willowbrook-extra-props.py), imported from there.
"""
import importlib.util, os, sys
import numpy as np
from scipy import ndimage as ndi
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("wbcut", os.path.join(HERE, "cut-willowbrook-extra-props.py"))
wb = importlib.util.module_from_spec(spec); spec.loader.exec_module(wb)
wb.SHEET = os.path.join(ROOT, "Assets/ArtLibrary/maps/scorpion-gorge/scorpion-gorge-prop-sheet-chatgpt-1.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
VIS = os.path.join(ROOT, "plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/visuals")
# (name, scale, footprint w, footprint h in tiles). Footprint h 0 = walk-through decoration (docs/map-design-rules.md rule 3).
wb.ROWS = [
    [("sgCactusTall", 0.85, 1, 1), ("sgCactusSmall", 0.6, 1, 1), ("sgBarrelCactus", 0.55, 1, 1), ("sgDeadTree", 0.85, 1, 1), ("sgTumbleweed", 0.5, 1, 0)],
    [("sgRockPileBig", 0.85, 2, 1), ("sgRockPileMed", 0.7, 2, 1), ("sgStonePillar", 0.85, 1, 1), ("sgStoneArch", 0.9, 2, 1), ("sgRubble", 0.6, 1, 0)],
    [("sgRibcage", 0.6, 2, 0), ("sgBullSkull", 0.55, 1, 0), ("sgStele", 0.85, 1, 1), ("sgScorpionStatue", 0.75, 1, 1), ("sgBrokenPot", 0.55, 1, 0)],
    [("sgTent", 0.85, 2, 1), ("sgCampfire", 0.6, 1, 0), ("sgLanternPost", 0.85, 1, 1), ("sgCrateSacks", 0.75, 1, 1), ("sgThornBush", 0.65, 1, 0)],
]


def components(rgba):
    """Like wb.components, but with a wider gap-bridging dilation: the bone ribcage (row 3) has a few px of daylight between individual ribs."""
    a = rgba[..., 3] > 0.55
    lab, n = ndi.label(ndi.binary_dilation(a, iterations=10))
    objs = []
    for i in range(1, n + 1):
        ys, xs = np.where((lab == i) & a)
        if len(xs) < 400: continue
        objs.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    objs.sort(key=lambda b: (b[1] + b[3]) / 2)
    want = [len(r) for r in wb.ROWS]
    if len(objs) != sum(want): raise SystemExit("found %d objects, expected %d" % (len(objs), sum(want)))
    out, k = [], 0
    for n_ in want:
        out.append(sorted(objs[k:k + n_], key=lambda b: b[0])); k += n_
    return out


def main():
    rgba = wb.clean(wb.mm.key_magenta(wb.SHEET))
    sprites = []
    for row, boxes in zip(wb.ROWS, components(rgba)):
        for (name, sc, fw, fh), box in zip(row, boxes): sprites.append((name, wb.cut(rgba, box, sc, fw), fw, fh))
    os.makedirs(VIS, exist_ok=True)
    if "--check" not in sys.argv:
        for name, img, fw, fh in sprites: img.save(os.path.join(OUT, name + ".png"), optimize=True)
    for name, img, fw, fh in sprites: print('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (name, img.width / 64.0, img.height / 64.0, fw, fh))
    sheet = Image.new("RGBA", (1500, 900), (196, 148, 96, 255)); x = y = 10; rowh = 0
    for name, img, fw, fh in sprites:
        if x + img.width > 1490: x = 10; y += rowh + 10; rowh = 0
        sheet.alpha_composite(img, (x, y)); x += img.width + 10; rowh = max(rowh, img.height)
    sheet.convert("RGB").save(os.path.join(VIS, "sg-props-sheet.png"))


if __name__ == "__main__":
    main()

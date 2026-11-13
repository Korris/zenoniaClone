"""Cuts the 20 Bramblewood Forest props (bw*) from the ChatGPT prop sheet (5x4 objects on #FF00FF) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-bramblewood-props.py [--check]    (--check writes only the preview sheet on grass to the plan's visuals folder)
Source: Assets/ArtLibrary/maps/bramblewood-forest/bramblewood-forest-prop-sheet-chatgpt-1.png (1536x1024).
Keying, despill, shadow and canvas rules are those of Tools/cut-willowbrook-extra-props.py (imported, with its SHEET / ROWS swapped).
Scale: the ground is 1536 px for 36 tiles and is shown at 64 px per tile (x1.5). The sheet's trees (~280 px) are bigger than the ring trees
painted in the ground (~200 px), so trees get 0.7 x 1.5 = 1.05; bushes, rocks and small decoration were drawn bigger still against the concept
(bramble 260 px on the sheet, ~120 px in the concept) and get SMALL. Prints the PropDef rows for BramblewoodForestPropDefs.cs.
"""
import importlib.util, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("wbx", os.path.join(HERE, "cut-willowbrook-extra-props.py"))
wbx = importlib.util.module_from_spec(spec); spec.loader.exec_module(wbx)
wbx.SHEET = os.path.join(ROOT, "Assets/ArtLibrary/maps/bramblewood-forest/bramblewood-forest-prop-sheet-chatgpt-1.png")
VIS = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/visuals")
TREE, SMALL = 1.05, 0.80
# (name, scale, footprint w, footprint h): trees block the trunk tile, the two big rocks their base, everything else is walk-through (map rule 3)
wbx.ROWS = [
    [("bwOakBig", TREE, 1, 1), ("bwOakVine", TREE, 1, 1), ("bwOakRoots", TREE, 1, 1), ("bwOakMid", TREE, 1, 1), ("bwOakSmall", TREE, 1, 1)],
    [("bwBrambleBig", SMALL, 1, 0), ("bwBrambleMid", SMALL, 1, 0), ("bwBrambleSmall", SMALL, 1, 0), ("bwBush", SMALL, 1, 0), ("bwFern", SMALL, 1, 0)],
    [("bwBoulder", SMALL, 2, 1), ("bwRockBlocks", SMALL, 2, 1), ("bwRock", SMALL, 1, 0), ("bwRockSmall", SMALL, 1, 0), ("bwLogHollow", SMALL, 3, 0)],
    [("bwStump", 0.6, 1, 0), ("bwMushrooms", 0.6, 1, 0), ("bwLupine", SMALL, 1, 0), ("bwFlowers", SMALL, 1, 0), ("bwReeds", SMALL, 1, 0)],
]


def main():
    rgba = wbx.clean(wbx.mm.key_magenta(wbx.SHEET))
    sprites = []
    for row, boxes in zip(wbx.ROWS, wbx.components(rgba)):
        for (name, sc, fw, fh), box in zip(row, boxes):
            sprites.append((name, wbx.cut(rgba, box, sc, fw), fw, fh))
    os.makedirs(VIS, exist_ok=True)
    if "--check" not in sys.argv:
        for name, img, fw, fh in sprites: img.save(os.path.join(wbx.OUT, name + ".png"), optimize=True)
    for name, img, fw, fh in sprites:
        print('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (name, img.width / 64.0, img.height / 64.0, fw, fh))
    sheet = Image.new("RGBA", (1800, 900), (88, 120, 40, 255)); x = y = 10; rowh = 0
    for name, img, fw, fh in sprites:
        if x + img.width > 1790: x = 10; y += rowh + 10; rowh = 0
        sheet.alpha_composite(img, (x, y)); x += img.width + 10; rowh = max(rowh, img.height)
    sheet.convert("RGB").save(os.path.join(VIS, "bw-props-sheet.png"))


if __name__ == "__main__":
    main()

"""Cuts the 20 Sunbreak Foothills props (sb*) from the ChatGPT prop sheet (5 x 4 objects on #FF00FF) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-sunbreak-foothills-props.py [--check]    (--check writes only the preview sheet on grass to plans/261113-1010-world-map-new-maps/layout-sunbreak/visuals)
Source: Assets/ArtLibrary/maps/sunbreak-foothills/sunbreak-foothills-prop-sheet-chatgpt-1.png (1536x1024).
Keying, despill, shadow rebuild, component search and canvas rules are the Willowbrook cutter's (Tools/cut-willowbrook-extra-props.py), imported from there.
The ground is 64 px/tile and the sheet draws a pine ~260 px tall, so trees stay at scale 1 (about 4 tiles, like Frost Hollow's fhPineBig); rocks and small dressing are scaled down to sit beside the painted ledges.
Prints the PropDef rows (W, H in tiles) for Assets/Scripts/Maps/SunbreakFoothillsPropDefs.cs.
"""
import importlib.util, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("wbcut", os.path.join(HERE, "cut-willowbrook-extra-props.py"))
wb = importlib.util.module_from_spec(spec); spec.loader.exec_module(wb)
wb.SHEET = os.path.join(ROOT, "Assets/ArtLibrary/maps/sunbreak-foothills/sunbreak-foothills-prop-sheet-chatgpt-1.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
VIS = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/layout-sunbreak/visuals")
# (name, scale, footprint w, footprint h in tiles). Footprint h 0 = walk-through decoration (docs/map-design-rules.md rule 3).
wb.ROWS = [
    [("sbPine", 1.0, 1, 1), ("sbPineLean", 1.0, 1, 1), ("sbPineSnow", 1.0, 1, 1), ("sbPineSmall", 1.0, 1, 1), ("sbJuniper", 0.95, 1, 1)],
    [("sbDeadTree", 0.9, 1, 1), ("sbGrassTuft", 0.6, 1, 0), ("sbBush", 0.6, 1, 0), ("sbLupine", 0.6, 1, 0), ("sbThornBush", 0.6, 1, 0)],
    [("sbRockMossy", 0.8, 2, 1), ("sbRocks", 0.75, 2, 1), ("sbRockSnow", 0.75, 2, 1), ("sbSandstoneTall", 0.9, 2, 1), ("sbSandstoneLow", 0.8, 2, 1)],
    [("sbCairn", 0.7, 1, 1), ("sbMarkerPost", 0.8, 1, 1), ("sbIbexSkull", 0.6, 1, 0), ("sbLog", 0.7, 2, 0), ("sbStreamStones", 0.6, 1, 0)],
]


def main():
    rgba = wb.clean(wb.mm.key_magenta(wb.SHEET))
    sprites = []
    for row, boxes in zip(wb.ROWS, wb.components(rgba)):
        for (name, sc, fw, fh), box in zip(row, boxes): sprites.append((name, wb.cut(rgba, box, sc, fw), fw, fh))
    os.makedirs(VIS, exist_ok=True)
    if "--check" not in sys.argv:
        for name, img, fw, fh in sprites: img.save(os.path.join(OUT, name + ".png"), optimize=True)
    for name, img, fw, fh in sprites: print('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (name, img.width / 64.0, img.height / 64.0, fw, fh))
    sheet = Image.new("RGBA", (1500, 900), (150, 150, 48, 255)); x = y = 10; rowh = 0
    for name, img, fw, fh in sprites:
        if x + img.width > 1490: x = 10; y += rowh + 10; rowh = 0
        sheet.alpha_composite(img, (x, y)); x += img.width + 10; rowh = max(rowh, img.height)
    sheet.convert("RGB").save(os.path.join(VIS, "sb-props-sheet.png"))


if __name__ == "__main__":
    main()

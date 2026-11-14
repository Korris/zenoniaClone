"""Cuts the fallen-oak roadblock props of Bramblewood Forest (bwFallenOak, bwFallenOakBramble, bwBrambleThick) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-bramblewood-fallen-oak.py [--check]    (--check writes only the preview on grass to the plan's visuals folder)
Source: Assets/ArtLibrary/maps/bramblewood-forest/bramblewood-fallen-oak-prop-chatgpt-1.png (1254x1254, 3 objects in one row on #FF00FF).
Keying, despill, the dark-magenta contact shadow turned brown, and the canvas rules are those of Tools/cut-willowbrook-extra-props.py (imported).
Scale: the trunk is ~800 px tall on the sheet and should span ~4 road tiles at 64 px per tile -> 0.32; the loose bramble gets 0.45 so it matches bwBrambleBig.
The log lies across the dead east road end (rows already block x 34-35); its footprint is the bottom tile only.
"""
import importlib.util, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("wbx", os.path.join(HERE, "cut-willowbrook-extra-props.py"))
wbx = importlib.util.module_from_spec(spec); spec.loader.exec_module(wbx)
wbx.SHEET = os.path.join(ROOT, "Assets/ArtLibrary/maps/bramblewood-forest/bramblewood-fallen-oak-prop-chatgpt-1.png")
VIS = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/visuals")
wbx.ROWS = [[("bwFallenOak", 0.32, 1, 1), ("bwFallenOakBramble", 0.32, 1, 1), ("bwBrambleThick", 0.45, 1, 0)]]


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
    sheet = Image.new("RGBA", (700, 420), (88, 120, 40, 255)); x = 10
    for name, img, fw, fh in sprites:
        sheet.alpha_composite(img, (x, 410 - img.height)); x += img.width + 20
    sheet.convert("RGB").save(os.path.join(VIS, "bw-fallen-oak-props.png"))


if __name__ == "__main__":
    main()

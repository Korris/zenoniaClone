"""Cut the ChatGPT damage-number FX sheet (V3 "Comic Impact", docs/reference/damage-numbers-chatgpt-spec.md) into separate textures.

Source: Assets/ArtLibrary/ui/damage-font/damage-font-fx-v3-chatgpt-1.png (1254 px, #00FF00; the -v2 sheet is the unused
earlier set), drawn after the mockup damage-numbers-v2-mockup-chatgpt-1.png, chat
https://chatgpt.com/c/6af98129-e4e8-83ec-88ef-2c765b7f4edf. Writes Assets/Resources/Sprites/ui/damage-font/fx-<name>.png,
trimmed to the art + 4 px, at most MAX_W wide (DamageNumber.Extras.cs draws them scaled; missing files fall back to code shapes).
Usage: python Tools/build-damage-number-fx.py cut
"""
import importlib.util, sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("font", HERE / "build-damage-number-font.py")
font = importlib.util.module_from_spec(spec); spec.loader.exec_module(font)

SHEET = font.SRC / "damage-font-fx-v3-chatgpt-1.png"
MAX_W = 448
# name: rough (x0, y0, x1, y1) in the 1254 px source; the item is the biggest blob centred inside it
BOXES = {
    "crit-burst": (215, 0, 1040, 430),
    "critical-label": (25, 460, 695, 660),
    "slash": (710, 400, 1240, 705),
    "finisher-ring": (85, 700, 630, 970),
    "hit-flash": (710, 660, 1150, 985),
    "spark-0": (115, 1000, 315, 1195),
    "spark-1": (405, 1000, 560, 1195),
    "spark-2": (680, 1040, 800, 1155),
    "spark-3": (920, 995, 1100, 1210),
}


def cut():
    rgba = font.key_green(SHEET)
    lab, _ = ndi.label(ndi.binary_dilation(rgba[..., 3] > 8, iterations=3))
    objs = ndi.find_objects(lab)
    for name, (x0, y0, x1, y1) in BOXES.items():
        # the biggest item whose centre lies in the box (items can overlap each other's boxes, e.g. the slash tip)
        best = None
        for idx, sl in enumerate(objs):
            cy, cx = (sl[0].start + sl[0].stop) / 2, (sl[1].start + sl[1].stop) / 2
            area = (sl[0].stop - sl[0].start) * (sl[1].stop - sl[1].start)
            if x0 <= cx < x1 and y0 <= cy < y1 and (best is None or area > best[1]): best = (idx, area)
        if best is None: sys.exit(f"{name}: no item centred in the box")
        sl = objs[best[0]]
        crop = rgba[sl].copy()
        crop[lab[sl] != best[0] + 1, 3] = 0
        crop = np.pad(crop, ((4, 4), (4, 4), (0, 0)))
        k = min(1.0, MAX_W / crop.shape[1])
        img = font.scaled(crop, k) if k < 1 else crop
        font.save(img, font.OUT_TEX / f"fx-{name}.png")
        print(name, img.shape[1], "x", img.shape[0])


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "cut": cut()
    else: print(__doc__)

"""Warrior idle facing down, in the proportions of the other four classes: paper doll from a painted body without weapons. Writes row 0 of body/bare, body/hand and body/armed idle.png and the idle/down anchors.
Sword in the right hand (screen left) with the fist drawn over the handle; shield on the left arm (screen right), in front, its hand behind it as a shield is held. armed row 0 = bare, as before (the bag portrait reads it).
Source: Assets/ArtLibrary/classes/warrior/idle-down-src/sample-peer-bare.png; the same pose with baked gear is sample-peer-proportions.png. Previous rows are kept as calm-*-idle.png there.
Needs Pillow, numpy and scipy; run from the project root.
"""
import importlib.util
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets/ArtLibrary/classes/warrior/idle-down-src"
BODY = ROOT / "Assets/Resources/Sprites/classes/warrior/body"
ANCHORS = ROOT / "Assets/Resources/Data/classes/warrior/held-gear-melee.json"
CELL = 192
SCALE = 0.181              # 163 px head to sole, as the other four classes stand at idle-down
SOLE_ROW = 186
SOURCE_SOLE, SOURCE_TOP = 1011, 112
FEET_CENTRE = 635          # source x between the boots
SWORD_FIST = (458, 800)    # source px: middle of the fist
SHIELD_ARM = (800, 735)    # source px: where the shield's grip sits, on the forearm
SWORD_ANGLE = -135         # as walk down, so standing and walking do not jump
SWORD_S, SHIELD_S, SHIELD_ANGLE = 0.34, 0.362, 4
FIST_RADIUS = 7            # frame px: the fist only; wider repaints arm and cape over the handle, which then vanishes
BREATH = (0, 1, 2, 1)      # rows the figure is shorter by, per frame, soles planted

spec = importlib.util.spec_from_file_location("hs", ROOT / "Tools/build-hero-select-art.py")
hs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hs)


def to_frame(point, less):
    """Source pixel to frame pixel in a frame that is `less` rows shorter."""
    height = (SOURCE_SOLE - SOURCE_TOP) * SCALE
    y_scale = (height - less) / height
    return ((point[0] - FEET_CENTRE) * SCALE + CELL / 2, SOLE_ROW - (SOURCE_SOLE - point[1]) * SCALE * y_scale)


def frame_image(src, less):
    height = round((SOURCE_SOLE - SOURCE_TOP) * SCALE) - less
    width = round(src.width * SCALE)
    figure = src.crop((0, SOURCE_TOP, src.width, SOURCE_SOLE)).resize((width, height), Image.LANCZOS)
    cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    cell.paste(figure, (round(CELL / 2 - FEET_CENTRE * SCALE), SOLE_ROW - height), figure)
    return cell


def fist_only(cell, centre):
    yy, xx = np.mgrid[0:CELL, 0:CELL]
    keep = (xx + 0.5 - centre[0]) ** 2 + (yy + 0.5 - centre[1]) ** 2 <= FIST_RADIUS ** 2
    a = np.array(cell)
    a[..., 3] = np.where(keep, a[..., 3], 0)
    return Image.fromarray(a)


def replace_row0(path, cells):
    sheet = Image.open(path).convert("RGBA")
    sheet.paste((0, 0, 0, 0), (0, 0, sheet.width, CELL))
    for i, cell in enumerate(cells):
        sheet.paste(cell, (i * CELL, 0))
    sheet.save(path)


def main():
    for kind in ("bare", "hand", "armed"):
        backup = SRC / f"calm-{kind}-idle.png"
        if not backup.exists():
            shutil.copy(BODY / kind / "idle.png", backup)

    src = hs.to_image(hs.key_out(SRC / "sample-peer-bare.png", hs.MAGENTA))
    cells = [frame_image(src, less) for less in BREATH]
    replace_row0(BODY / "bare/idle.png", cells)
    replace_row0(BODY / "armed/idle.png", cells)
    replace_row0(BODY / "hand/idle.png", [fist_only(c, to_frame(SWORD_FIST, less)) for c, less in zip(cells, BREATH)])

    data = json.loads(ANCHORS.read_text(encoding="utf-8"))
    for entry in data["anchors"]:
        if entry["anim"] != "idle" or entry["dir"] != "down":
            continue
        sword = entry["part"] == "sword"
        frames = []
        for less in BREATH:
            x, y = to_frame(SWORD_FIST if sword else SHIELD_ARM, less)
            frames.append(dict(x=round(x, 3), y=round(y, 3), angle=SWORD_ANGLE if sword else SHIELD_ANGLE, s=SWORD_S if sword else SHIELD_S,
                               sx=1, behind=False, hidden=False, fallback=False))
        entry["frames"] = frames
    ANCHORS.write_text(json.dumps(data, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()

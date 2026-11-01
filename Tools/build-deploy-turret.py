"""Deploy Turret (Mechanic skill): cuts the one turret picture out of the scrap base painting, same keying and size as `build-mechanic-new-style.py turret`.
Writes Resources/Sprites/classes/mechanic/skills/deploy-turret/turret.png and prints the grip for TurretConfig: middle of the base (front foot), on the ground row.

  python Tools/build-deploy-turret.py
"""
import importlib.util, os
import numpy as np
from PIL import Image

spec = importlib.util.spec_from_file_location("mechanic_new_style", "Tools/build-mechanic-new-style.py")
ms = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ms)

SOURCE = f"{ms.SRC}/turret-scrap-base.png"
OUT = f"{ms.RES}/Sprites/classes/mechanic/skills/deploy-turret/turret.png"
BASE_MIDDLE_X = 458  # the front foot, read at full size: the muzzle makes the box lopsided
MUZZLE = (190, 290)  # middle of the barrel's mouth, read at full size


if __name__ == "__main__":
    art = ms.key_any(SOURCE)
    x0, x1, y0, y1 = ms.bbox(art)
    h = ms.TURRET_HEIGHT
    w = round((x1 - x0) * h / (y1 - y0))
    rgba = ms.fit(art, w, h)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(OUT)
    grip_x = round((BASE_MIDDLE_X - x0) * w / (x1 - x0)) + ms.PAD
    grip_y = h + ms.PAD
    k = h / (y1 - y0)
    print("box", (x0, x1, y0, y1), "canvas", w + 2 * ms.PAD, h + 2 * ms.PAD, "grip", grip_x, grip_y)
    print("muzzle, fraction of art height from the grip:", round((MUZZLE[0] - BASE_MIDDLE_X) * k / h, 3), round((y1 - MUZZLE[1]) * k / h, 3))
    print("edge pixels left of the key:", int(np.count_nonzero((rgba[..., 3] > 0) & (rgba[..., 1].astype(int) < 40) & (rgba[..., 0].astype(int) > 200) & (rgba[..., 2].astype(int) > 200))))

"""Cleric V3 (Priest, Asteria 26/11): ChatGPT star staff -> the weapon sprite of the cleric RO Fantasy layered set.

  python Tools/build-cleric-v3-staff.py write   Sprites/hero-layered/weapons/cleric-staff.png (+ .meta) and its grip in Data/hero-layered/weapons.json
  python Tools/build-cleric-v3-staff.py check   docs/reference/character-v3/cleric-v3/staff-cut-check.png (sprite x2, grip marked)

Source (read only, newest N): Assets/ArtLibrary/ro-fantasy-heroes/src/cleric-v3/weapons-cleric-staff-v3-chatgpt-N.png, flat #00FF00, the staff
UPRIGHT (star head at the top). Size: STAFF_PX tall in the 320 x 256 body cell (Priest bald figure 216 px; design chat: staff ~110 % of the bald
figure with the fist at chest height; the idle fists hang at the hip, so 95 % keeps the butt on the feet line). Grip = GRIP_FROM_TOP of the
length from the top (the idle fist then puts the butt near the feet and the star head beside the face), x = shaft centre on that row.
The per-frame grips are written by Tools/ro_fantasy_cleric_v3_grips.py (via build-ro-fantasy-hero-game-assets.py grips cleric).
Old staff + json backup: D:/temp/cleric-v2-backup/.
"""
import json
import sys
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ro_fantasy_body_cut as bc  # noqa: E402

ROOT = bc.ROOT
SRC = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/src/cleric-v3"
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons/cleric-staff.png"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
CHECK = ROOT / "docs/reference/character-v3/cleric-v3/staff-cut-check.png"
STAFF_PX = 205
GRIP_FROM_TOP = 0.62


def staff():
    files = sorted(SRC.glob("weapons-cleric-staff-v3-chatgpt-*.png"), key=lambda p: int(p.stem.rsplit("-", 1)[1]))
    a = np.asarray(bc.keyed(files[-1]))
    al = a[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=4))
    big = 1 + int(np.argmax(ndimage.sum(al, lab, range(1, n + 1))))
    a = a.copy()
    a[lab != big] = 0
    im = Image.fromarray(a, "RGBA")
    im = im.crop(im.getbbox())
    s = STAFF_PX / im.height
    im = im.resize((max(1, round(im.width * s)), STAFF_PX), Image.LANCZOS)
    arr = np.asarray(im).copy()
    arr[arr[..., 3] < 8] = 0
    im = Image.fromarray(arr, "RGBA")
    gy = round(STAFF_PX * GRIP_FROM_TOP, 1)
    gx = round(float(np.nonzero(arr[int(gy), :, 3] > 100)[0].mean()), 1)
    return im, gx, gy, files[-1].name


def main(cmd):
    im, gx, gy, name = staff()
    if cmd == "write":
        im.save(OUT)
        import_module("build-hero-layered-game-assets").meta(OUT, "idle")
        d = json.loads(DATA.read_text(encoding="utf-8"))
        d["grips"]["cleric-staff"] = [gx, gy]
        DATA.write_text(json.dumps(d, indent=1), encoding="utf-8")
        print("wrote", OUT.name, im.size, "grip", gx, gy, "from", name)
    elif cmd == "check":
        big = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
        bg = Image.new("RGBA", (big.width + 40, big.height + 20), (90, 96, 104, 255))
        bg.alpha_composite(big, (20, 10))
        d = ImageDraw.Draw(bg)
        x, y = 20 + gx * 2, 10 + gy * 2
        d.ellipse((x - 5, y - 5, x + 5, y + 5), outline=(255, 0, 0), width=2)
        CHECK.parent.mkdir(parents=True, exist_ok=True)
        bg.convert("RGB").save(CHECK)
        print("->", CHECK)
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "")

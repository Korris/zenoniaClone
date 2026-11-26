"""Mechanic V3 hand cannon (Asteria, 26/11): ChatGPT sprite -> the main weapon of the RO Fantasy mechanic layered set.

  python Tools/build-mechanic-v3-cannon.py write   Sprites/hero-layered/weapons/mechanic-cannon.png + grip in Data/hero-layered/weapons.json
  python Tools/build-mechanic-v3-cannon.py check   docs/reference/character-v3/mechanic-v3/production/cannon-cut-check.png (x3, grip marked)

Source (read only, newest N): Assets/ArtLibrary/ro-fantasy-heroes/src/mechanic-v3/weapon-mechanic-cannon-v3-chatgpt-N.png, flat #00FF00,
side view, muzzle UP, handle at the bottom. Size chosen by the design chat (26/11): length 55 % H, H = the 216 px V3 figure -> 119 px tall.
Grip = centre of the leather handle (brown wrap pixels), as the design chat specified ("pivot: chính giữa tay cầm").
The game draws one sprite turned per frame (HeroLayeredView), grips per frame: Tools/build-ro-fantasy-hero-game-assets.py grips mechanic
(Tools/ro_fantasy_mechanic_v3.py swaps the class weapons for this cannon, no offhand).
"""
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ro_fantasy_body_cut as bc  # noqa: E402

ROOT = bc.ROOT
SRC = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/src/mechanic-v3"
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons/mechanic-cannon.png"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
CHECK = ROOT / "docs/reference/character-v3/mechanic-v3/production/cannon-cut-check.png"
CANNON_PX = round(0.55 * 216)  # 119


def source():
    files = sorted(SRC.glob("weapon-mechanic-cannon-v3-chatgpt-*.png"), key=lambda p: int(re.search(r"chatgpt-(\d+)", p.name).group(1)))
    return files[-1]


def cut():
    a = np.asarray(bc.keyed(source()))
    al = a[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=4))
    biggest = 1 + int(np.argmax(ndimage.sum(al, lab, range(1, n + 1))))
    a = a.copy()
    a[lab != biggest] = 0
    img = Image.fromarray(a, "RGBA")
    img = img.crop(img.getbbox())
    s = CANNON_PX / img.height
    img = img.resize((max(1, round(img.width * s)), CANNON_PX), Image.LANCZOS)
    # grip: brown leather wrap of the handle (r > g > b, dark-mid, saturated), in the lower half
    c = np.asarray(img).astype(int)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    brown = (c[..., 3] > 128) & (r > g + 20) & (g > b + 5) & (r < 200) & (r > 70) & (r - b > 50)
    brown[: img.height // 2] = False
    lab, n = ndimage.label(brown)
    if not n:
        raise SystemExit("no leather handle found")
    k = 1 + int(np.argmax(ndimage.sum(brown, lab, range(1, n + 1))))
    ys, xs = np.nonzero(lab == k)
    return img, (round(float(xs.mean()), 1), round(float(ys.mean()), 1))


def write():
    img, (gx, gy) = cut()
    img.save(OUT)
    import_meta = __import__("importlib").import_module("build-hero-layered-game-assets")
    import_meta.meta(OUT, "idle")
    d = json.loads(DATA.read_text(encoding="utf-8"))  # re-read right before writing: shared with the other classes' tools
    d["grips"]["mechanic-cannon"] = [gx, gy]
    if "Mechanic V3" not in d["note"]:
        d["note"] += " Mechanic V3 26/11: mechanic-cannon from Tools/build-mechanic-v3-cannon.py."
    DATA.write_text(json.dumps(d, indent=1), encoding="utf-8")
    print("mechanic-cannon", img.size, "grip", (gx, gy))


def check():
    img, (gx, gy) = cut()
    big = Image.new("RGBA", img.size, (90, 100, 90, 255))
    big.alpha_composite(img)
    big = big.resize((img.width * 3, img.height * 3), Image.NEAREST)
    d = ImageDraw.Draw(big)
    d.ellipse((gx * 3 - 6, gy * 3 - 6, gx * 3 + 6, gy * 3 + 6), outline=(255, 0, 0), width=2)
    CHECK.parent.mkdir(parents=True, exist_ok=True)
    big.convert("RGB").save(CHECK)
    print("wrote", CHECK, img.size, "grip", (gx, gy))


if __name__ == "__main__":
    {"write": write, "check": check}.get(sys.argv[1] if len(sys.argv) > 1 else "", lambda: print(__doc__))()

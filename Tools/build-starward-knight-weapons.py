"""Starward Knight V3 sword + shield (2026-11-24): ChatGPT sheet -> the two weapon sprites of the warrior layered set.

  python Tools/build-starward-knight-weapons.py write   Sprites/hero-layered/weapons/{sword,warrior-shield}.png + grips in Data/hero-layered/weapons.json
  python Tools/build-starward-knight-weapons.py check   docs/reference/character-v3/starward-knight-v3/weapons-cut-check.png (both sprites x3, grip marked)

Source (read only, newest N): Assets/ArtLibrary/ro-fantasy-heroes/src/weapons-starward-knight-chatgpt-N.png, flat #00FF00, the sword UPRIGHT
(tip at the top) on the left, the shield upright on the right. Sizes chosen by the design chat (24/11, starward-knight-v3-design-answers-chatgpt.txt):
sword tip -> pommel 65 % H, shield 42 % H, H = the 216 px Starward figure in the 320 x 256 body cell -> 140 px / 91 px.
Grips: sword = middle of the handle (between the crossguard and the pommel), shield = its centre (strap behind the boss), like the old art.
Then run Tools/build-ro-fantasy-hero-game-assets.py grips warrior (class.json weapons + per-frame grips read these files).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ro_fantasy_body_cut as bc  # noqa: E402

ROOT = bc.ROOT
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
CHECK = ROOT / "docs/reference/character-v3/starward-knight-v3/weapons-cut-check.png"
SWORD_PX, SHIELD_PX = 140, 91
if bc.variant.VARIANT == "golden-paladin":  # test look (24/11): sizes chosen by its design chat, source in src/golden-paladin/
    CHECK = bc.variant.DOCS / "weapons-cut-check.png"
    SWORD_PX, SHIELD_PX = 151, 108  # design chat: sword 70 % H, tower shield 50 % H (H = 216)


def pieces():
    src = bc.newest("weapons-starward-knight-chatgpt-*.png")
    a = np.asarray(bc.keyed(src))
    al = a[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=6))
    sizes = ndimage.sum(al, lab, range(1, n + 1))
    keep = sorted(np.argsort(sizes)[-2:] + 1, key=lambda i: ndimage.find_objects(lab)[i - 1][1].start)
    out = []
    for i in keep:
        sl = ndimage.find_objects(lab)[i - 1]
        crop = a[sl].copy()
        crop[lab[sl] != i] = 0
        out.append(Image.fromarray(crop, "RGBA"))
    print(src.name, "->", [p.size for p in out])
    return out


def fit(img, px):
    img = img.crop(img.getbbox())
    return img.resize((max(1, round(img.width * px / img.height)), px), Image.LANCZOS)


def sword_grip(img):
    """(gx, gy): middle of the blue-wrapped handle (Knight sword, chatgpt-1: rows where the opaque width is narrow and mostly blue);
    fallback: the crossguard is the widest row of the lower 45 %, the grip 45 % of the way from it to the bottom (pommel)."""
    px = np.asarray(img).astype(int)
    al = px[..., 3] > 100
    w = al.sum(1)
    blue = (al & (px[..., 2] > px[..., 0] + 40)).sum(1)
    lo = int(img.height * 0.55)
    rows = [y for y in range(lo, img.height) if w[y] and w[y] <= 0.35 * w.max() and blue[y] >= 0.5 * w[y]]
    if len(rows) >= 5:
        gy = (rows[0] + rows[-1]) / 2
        cols = np.nonzero(al[int(round(gy))])[0]
        return round(float(cols.mean()), 1), round(float(gy), 1)
    lo = int(img.height * 0.55)
    guard = lo + int(np.argmax(w[lo:]))
    bottom = np.nonzero(w)[0][-1]
    # the guard band can be a few rows thick: start under its last wide row
    g_end = guard
    while g_end + 1 < bottom and w[g_end + 1] > 0.6 * w[guard]:
        g_end += 1
    gy = g_end + 0.45 * (bottom - g_end)
    cols = np.nonzero(al[int(round(gy))])[0]
    return round(float(cols.mean()), 1), round(float(gy), 1)


def build():
    sword, shield = pieces()
    sword, shield = fit(sword, SWORD_PX), fit(shield, SHIELD_PX)
    sg = sword_grip(sword)
    hg = (round(shield.width / 2, 1), round(shield.height / 2, 1))
    return sword, sg, shield, hg


def cmd_write():
    sword, sg, shield, hg = build()
    sword.save(OUT / "sword.png")
    shield.save(OUT / "warrior-shield.png")
    data = json.loads(DATA.read_text(encoding="utf-8"))
    data["grips"]["sword"] = list(sg)
    data["grips"]["warrior-shield"] = list(hg)
    data["note"] = (data["note"].split(" Starward")[0] + " Starward Knight V3 24/11: sword + warrior-shield from "
                    "Tools/build-starward-knight-weapons.py.")
    DATA.write_text(json.dumps(data, indent=1), encoding="utf-8")
    print("sword", sword.size, "grip", sg, "| shield", shield.size, "grip", hg)


def cmd_check():
    sword, sg, shield, hg = build()
    k = 3
    out = Image.new("RGBA", ((sword.width + shield.width + 30) * k, max(sword.height, shield.height) * k), (90, 90, 90, 255))
    x = 0
    d = ImageDraw.Draw(out)
    for img, (gx, gy) in ((sword, sg), (shield, hg)):
        big = img.resize((img.width * k, img.height * k), Image.NEAREST)
        out.alpha_composite(big, (x, 0))
        d.ellipse((x + gx * k - 5, gy * k - 5, x + gx * k + 5, gy * k + 5), outline=(255, 0, 0, 255), width=2)
        x += big.width + 30 * k
    CHECK.parent.mkdir(parents=True, exist_ok=True)
    out.save(CHECK)
    print(CHECK)


if __name__ == "__main__":
    {"write": cmd_write, "check": cmd_check}.get(sys.argv[1] if len(sys.argv) > 1 else "", lambda: print(__doc__))()

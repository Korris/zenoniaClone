"""Assassin (Rogue) Asteria V3 daggers (2026-11-26): ChatGPT sheet -> the dagger sprite of the RO Fantasy assassin layered set.

  python Tools/build-assassin-v3-daggers.py write   Sprites/hero-layered/weapons/dagger.png + grip in Data/hero-layered/weapons.json
  python Tools/build-assassin-v3-daggers.py check   docs/reference/character-v3/assassin-v3/daggers-cut-check.png (x3, grip marked)

Source (read only): Assets/ArtLibrary/ro-fantasy-heroes/src/assassin-v3/weapons-assassin-daggers-v3-chatgpt-N.png (newest N), flat
#00FF00, two identical daggers UPRIGHT (tip at the top); the left one (right-hand dagger) is used for both hands (`dagger` item, both
parts, hero_layered_grip_poses.WEAPONS). Size: design chat 26/11 = ~45 % of the figure height H (216 px V3 figure) -> DAGGER_PX.
Grip = middle of the wrapped handle (between the crossguard = widest row of the lower half and the pommel).
Then: RO_FANTASY_VARIANT=assassin-v3 python Tools/build-ro-fantasy-hero-game-assets.py grips assassin
Revert: D:/temp/assassin-v2-backup/dagger.png + weapons.json (old grip [15.5, 56.5]).
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
SRC = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/src/assassin-v3"
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons/dagger.png"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
CHECK = ROOT / "docs/reference/character-v3/assassin-v3/daggers-cut-check.png"
DAGGER_PX = 97  # 0.45 x 216


def source():
    files = sorted(SRC.glob("weapons-assassin-daggers-v3-chatgpt-*.png"), key=lambda p: int(re.search(r"chatgpt-(\d+)", p.name).group(1)))
    return files[-1]


def piece():
    src = source()
    a = np.asarray(bc.keyed(src))
    al = a[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=6))
    sizes = ndimage.sum(al, lab, range(1, n + 1))
    keep = sorted(np.argsort(sizes)[-2:] + 1, key=lambda i: ndimage.find_objects(lab)[i - 1][1].start)
    i = keep[0]  # left dagger
    sl = ndimage.find_objects(lab)[i - 1]
    crop = a[sl].copy()
    crop[lab[sl] != i] = 0
    img = Image.fromarray(crop, "RGBA")
    img = img.crop(img.getbbox())
    print(src.name, "->", img.size)
    return img.resize((max(1, round(img.width * DAGGER_PX / img.height)), DAGGER_PX), Image.LANCZOS)


def grip(img):
    al = np.asarray(img)[..., 3] > 100
    w = al.sum(1)
    lo = int(img.height * 0.5)
    guard = lo + int(np.argmax(w[lo:]))
    g_end = guard
    while g_end + 1 < img.height and w[g_end + 1] > 0.6 * w[guard]:
        g_end += 1
    rows = np.nonzero(w)[0]
    bottom = rows[-1]
    # pommel = the wide knob at the bottom: handle ends where the width grows again under the guard
    handle = []  # first contiguous narrow run under the guard; stops where the pommel knob widens again
    for y in range(g_end + 1, bottom):
        if w[y] <= 0.35 * w[guard] and (not handle or w[y] <= np.median([w[h] for h in handle]) + 1):
            handle.append(y)
        elif handle:
            break
    gy = (handle[0] + handle[-1]) / 2 if len(handle) >= 3 else g_end + 0.45 * (bottom - g_end)
    cols = np.nonzero(al[int(round(gy))])[0]
    return round(float(cols.mean()), 1), round(float(gy), 1)


def cmd_write():
    img = piece()
    g = grip(img)
    img.save(OUT)
    data = json.loads(DATA.read_text(encoding="utf-8"))
    data["grips"]["dagger"] = list(g)
    if "Assassin V3" not in data["note"]:
        data["note"] += " Assassin V3 26/11: dagger from Tools/build-assassin-v3-daggers.py."
    DATA.write_text(json.dumps(data, indent=1), encoding="utf-8")
    print("dagger", img.size, "grip", g)


def cmd_check():
    img = piece()
    gx, gy = grip(img)
    k = 3
    out = Image.new("RGBA", (img.width * k + 40, img.height * k + 20), (90, 90, 90, 255))
    out.alpha_composite(img.resize((img.width * k, img.height * k), Image.NEAREST), (20, 10))
    d = ImageDraw.Draw(out)
    d.ellipse((20 + gx * k - 5, 10 + gy * k - 5, 20 + gx * k + 5, 10 + gy * k + 5), outline=(255, 0, 0, 255), width=2)
    CHECK.parent.mkdir(parents=True, exist_ok=True)
    out.save(CHECK)
    print(CHECK, "grip", (gx, gy))


if __name__ == "__main__":
    {"write": cmd_write, "check": cmd_check}.get(sys.argv[1] if len(sys.argv) > 1 else "", lambda: print(__doc__))()

"""C# tables of where the Cleric basic attack FX leave the hero (fix 2026-11-08: the Halo Lance / Judgment Bloom used to start from the middle
of the body and covered her face for ~0.1 s, worst facing up and hit 4 facing right).

Per hit (1-4) and facing (down, up, left, right), on the release frame (game frame 1: the bolt leaves at the end of the windup, 70 / 260 ms
of a 4-frame hit and 130 / 460 ms of the 6-frame hit 4), in world units from the feet (8 frame px = 1 unit, figure scale 1):
  hits 1, 3, 4: the T0 staff crystal (attack anchors of held-cleric-melee.json + the crystal's place in the staff art);
  hit 2 (Aegis Ray, shield push): the centre of the T0 shield.
Paste the output into Rpg/Skills/RangedAimRules.cs (ClericTipX / ClericTipY, shared with the server: the bolt starts there) after changing
the attack poses (Tools/cleric_attack_fists.py) or the T0 staff / shield art.

  python Tools/build-cleric-attack-cast-tips.py [--check OUT.png]   prints the table; --check draws the points on the composed frames
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cleric_attack_compose as cc  # noqa: E402

DIRS = ("down", "up", "left", "right")
RELEASE_FRAME = 1
SHIELD_DRAW = 0.8  # HeroClassDef part draw scale of the shield (compose-hero-frame.py CLASSES)


def gear():
    hc = json.load(open(f"{cc.RES}/Data/classes/cleric/held-cleric.json", encoding="utf-8"))
    st, sh = hc["weapon"][0], hc["shield"][0]
    art = np.asarray(Image.open(f"{cc.RES}/Sprites/classes/cleric/weapons/staff/{st['name']}-base.png").convert("RGBA")).astype(int)
    blue = (art[..., 3] > 200) & (art[..., 2] > art[..., 0] + 60)
    ys, xs = np.nonzero(blue[:150])  # the crystal in the staff head (the butt has a small blue gem too)
    g = st["baseGrip"]
    crystal = (xs.mean() - g["gripX"], ys.mean() - g["gripY"])
    sg = sh["baseGrip"]
    shield_c = (sg["w"] / 2 - sg["gripX"], sg["h"] / 2 - sg["gripY"])
    return crystal, shield_c


def anchors():
    data = json.load(open(f"{cc.RES}/Data/classes/cleric/held-cleric-melee.json", encoding="utf-8"))
    return {(a["part"], a["anim"], a["dir"]): a["frames"] for a in data["anchors"]}


def place(a, v, draw=1.0):
    """Frame px of art offset v (from the grip) under anchor a, as HeroRenderer.ApplyPart / compose-hero-frame.place (angle clockwise)."""
    th = math.radians(a["angle"])
    k = a["s"] * draw
    dx, dy = v[0] * k * (a.get("sx", 1) or 1), v[1] * k
    return a["x"] + dx * math.cos(th) - dy * math.sin(th), a["y"] + dx * math.sin(th) + dy * math.cos(th)


def tips():
    crystal, shield_c = gear()
    an = anchors()
    out = {}
    for hit in range(1, 5):
        anim = f"attack{hit}"
        for d in DIRS:
            if hit == 2:
                x, y = place(an[("shield", anim, d)][RELEASE_FRAME], shield_c, SHIELD_DRAW)
            else:
                x, y = place(an[("staff", anim, d)][RELEASE_FRAME], crystal)
            out[(hit, d)] = (x, y)
    return out


def table(t):
    xs = ", ".join(f"{(t[(h, d)][0] - 96) / 8:.1f}f" for h in range(1, 5) for d in DIRS)
    ys = ", ".join(f"{(192 - t[(h, d)][1]) / 8:.1f}f" for h in range(1, 5) for d in DIRS)
    return f"static readonly float[] ClericTipX = {{ {xs} }};\nstatic readonly float[] ClericTipY = {{ {ys} }};"


def check(t, out):
    pad = 40
    size = cc.FRAME + 2 * pad
    sheet = Image.new("RGB", (size * 4, size * 4))
    for h in range(1, 5):
        for c, d in enumerate(DIRS):
            im = cc.on_bg(cc.frame(f"attack{h}", d, RELEASE_FRAME, style=2, pad=pad))
            dr = ImageDraw.Draw(im)
            x, y = t[(h, d)]
            dr.ellipse((x + pad - 4, y + pad - 4, x + pad + 4, y + pad + 4), outline=(255, 0, 0), width=2)
            sheet.paste(im, (c * size, (h - 1) * size))
    sheet.save(out)
    print("->", out)


if __name__ == "__main__":
    t = tips()
    print(table(t))
    if "--check" in sys.argv:
        check(t, sys.argv[sys.argv.index("--check") + 1])

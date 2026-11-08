"""Prints the Mechanic combo muzzle tables (RangedAimRules.MuzzleX/Y, HeroFx.Hit3MuzzleGun/GunL) from held-mechanic-dual.json:
the shooting gun's muzzle on the firing frame (frame 1) of attack1-3 with the T0 (scrap) pistol art, and the cannon muzzle of attack4 frame 1
(anchor centre + half of CANNON_LEN along the barrel). World units from the feet, 8 frame px = 1 unit, figure scale 1.
Re-run after Tools/build-mechanic-attack-down-repaint.py or Tools/build-mechanic-tier-weapon-fit.py change the anchors, paste into the C#.
  python Tools/measure-mechanic-attack-muzzles.py"""
import json, math, os
import numpy as np
from PIL import Image

os.chdir("D:/zenoniaClone")
SPR = "Assets/Resources/Sprites/classes/mechanic/weapons"
DIRS = ("down", "up", "left", "right")  # HeroController facing order
data = json.load(open("Assets/Resources/Data/classes/mechanic/held-mechanic-dual.json", encoding="utf-8"))
A = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in data["anchors"]}
w0 = data["weapon"][0]
side = np.asarray(Image.open(f"{SPR}/pistol/{w0['name']}-base.png").convert("RGBA"))[..., 3] > 128
xr = np.flatnonzero(side.any(0)).max()
ys = np.flatnonzero(side[:, xr - 6:xr + 1].any(1))


def view_muzzle(view):
    """Muzzle point of a per-tier view (canvas centre = grip): the front view's muzzle circle / the back view's top, measured on the art."""
    im = np.asarray(Image.open(f"{SPR}/pistol/{w0['name']}/{view}.png").convert("RGBA"))[..., 3] > 128
    rows = np.flatnonzero(im.any(1))
    top, bot = rows.min(), rows.max()
    h, w = im.shape
    frac = 0.31 if view == "pistol-front" else 0.2  # muzzle circle centre / rear sight, from the art top, of the art height (read on the T0 art)
    return w / 2, top + frac * (bot - top), w / 2, h / 2


MUZ = {None: (xr - 4, (ys.min() + ys.max()) / 2, w0["baseGrip"]["gripX"], w0["baseGrip"]["gripY"]),
       "pistol-front": view_muzzle("pistol-front"), "pistol-back": view_muzzle("pistol-back")}
SHOOT = {"attack1": ("gun",), "attack2": ("gunL",), "attack3": ("gun", "gunL")}
CANNON_BASE, CANNON_LEN = {"front": -44.0, "back": -135.0, "side": 0.0}, 168.0


def units(x, y):
    return (x - 96) / 8, (192 - y) / 8


def pistol(a):
    mx, my, gx, gy = MUZ[a.get("art")]
    k, sx, th = a["s"], a.get("sx", 1), math.radians(a["angle"])
    u, v = (mx - gx) * k * sx, (my - gy) * k
    return units(a["x"] + u * math.cos(th) - v * math.sin(th), a["y"] + u * math.sin(th) + v * math.cos(th))


def cannon(a):
    sx = a["sx"]
    base = CANNON_BASE[a["art"]] if sx > 0 else 180 - CANNON_BASE[a["art"]]
    aim = math.radians(a["angle"] + base)
    return units(a["x"] + math.cos(aim) * CANNON_LEN / 2 * abs(sx), a["y"] + math.sin(aim) * CANNON_LEN / 2 * abs(sx))


xs, ys_ = [], []
for anim in ("attack1", "attack2", "attack3"):
    for d in DIRS:
        pts = [pistol(A[(p, anim, d)][1]) for p in SHOOT[anim]]
        xs.append(sum(p[0] for p in pts) / len(pts)); ys_.append(sum(p[1] for p in pts) / len(pts))
for d in DIRS:
    x, y = cannon(A[("cannon", "attack4", d)][1])
    xs.append(x); ys_.append(y)
f = lambda v: ", ".join("%.1ff" % x for x in v)
print("MuzzleX = { %s };" % f(xs))
print("MuzzleY = { %s };" % f(ys_))
for p in ("gun", "gunL"):
    print("Hit3Muzzle%s = { %s };" % ("Gun" if p == "gun" else "GunL", ", ".join("new Vector2(%.1ff, %.1ff)" % pistol(A[(p, "attack3", d)][1]) for d in DIRS)))

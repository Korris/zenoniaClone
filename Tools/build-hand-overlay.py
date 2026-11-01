"""Warrior hands for idle and walk seen from the side. The left row is the right row mirrored, which would swap his hands, so the gear goes on the other arms there:
facing right the sword is in the near hand (above the body, the fist drawn over its handle) and the shield on the far arm;
facing left the shield is on the near arm (above the body) and the sword in the far hand (behind the body, which draws that fist over the handle).
Writes Assets/Resources/Sprites/classes/warrior/body/hand/{idle,walk}.png (same grid as body/bare/, only the near fist in the right row; HeroRenderer draws it above the sword)
and sets the sword and shield anchors of those rows in Assets/Resources/Data/classes/warrior/held-gear-melee.json. Safe to run again.
Usage: python Tools/build-hand-overlay.py   (from the project root; needs Pillow and numpy)"""
import json, math, os
import numpy as np
from PIL import Image

FRAME, COLS = 192, 4
RX, RY, FEATHER = 10.5, 9.5, 1.5  # fist ellipse in frame px; generous is safe, the layer repeats the body's own pixels
HAND_Y, DRAW_SCALE = 230, 2.0     # sword art row of the middle of the handle; HeroClassDef sword DrawScale
# Fist centres in the RIGHT row, read from the bare sheets at 5x (the left row is that row mirrored).
FISTS = {"idle": [(86.5, 136.5), (89, 136.5), (89, 136.5), (88, 136.5)],
         "walk": [(72.5, 137), (78, 136), (78, 137.5), (78, 136)]}
FAR_FISTS = {"idle": [(128, 132), (129, 130), (127, 131), (125, 131)],
             "walk": [(128, 130), (129, 128), (124, 129), (126, 127)]}
SHIELD_ABOVE_FIST = 8  # the strap sits on the forearm, above the near fist
RES = "Assets/Resources"
LEFT, RIGHT = 2, 3


def fist_mask(cx, cy):
    y, x = np.mgrid[0:FRAME, 0:FRAME]
    d = np.sqrt(((x + 0.5 - cx) / RX) ** 2 + ((y + 0.5 - cy) / RY) ** 2)
    return np.clip((1 - d) * min(RX, RY) / FEATHER, 0, 1)


def build_sheet(anim):
    bare = np.array(Image.open(f"{RES}/Sprites/classes/warrior/body/bare/{anim}.png").convert("RGBA")).astype(np.float64)
    path = f"{RES}/Sprites/classes/warrior/body/hand/{anim}.png"
    out = np.zeros_like(bare)
    if os.path.exists(path):  # row 0 of idle belongs to Tools/build-warrior-idle-down-doll.py; keep it
        out[:FRAME] = np.array(Image.open(path).convert("RGBA")).astype(np.float64)[:FRAME]
    for c, (cx, cy) in enumerate(FISTS[anim]):
        x0, y0 = c * FRAME, RIGHT * FRAME
        cell = bare[y0:y0 + FRAME, x0:x0 + FRAME].copy()
        cell[..., 3] *= fist_mask(cx, cy)
        out[y0:y0 + FRAME, x0:x0 + FRAME] = cell
    os.makedirs(f"{RES}/Sprites/classes/warrior/body/hand", exist_ok=True)
    Image.fromarray(out.round().astype(np.uint8), "RGBA").save(f"{RES}/Sprites/classes/warrior/body/hand/{anim}.png")


def move_anchors():
    path = f"{RES}/Data/classes/warrior/held-gear-melee.json"
    raw = open(path, "rb").read()
    data = json.loads(raw)
    grip_y = json.load(open(f"{RES}/Data/classes/warrior/held-gear.json"))["sword"][0]["baseGrip"]["gripY"]
    for a in data["anchors"]:
        if a["anim"] not in FISTS or a["dir"] not in ("left", "right"):
            continue
        left = a["dir"] == "left"
        for c, f in enumerate(a["frames"]):
            if a["part"] == "sword":
                th = math.radians(f["angle"])
                off = (HAND_Y - grip_y) * f["s"] * DRAW_SCALE
                cx, cy = (FAR_FISTS if left else FISTS)[a["anim"]][c]
                cx = FRAME - cx if left else cx
                f["x"], f["y"] = round(cx + math.sin(th) * off, 3), round(cy - math.cos(th) * off, 3)
                f["behind"] = left
            elif a["part"] == "shield" and left:
                cx, cy = FISTS[a["anim"]][c]
                f["x"], f["y"] = round(FRAME - cx, 3), round(cy - SHIELD_ABOVE_FIST, 3)
                f["behind"] = False
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode())


if __name__ == "__main__":
    for name in FISTS:
        build_sheet(name)
    move_anchors()
    print("bare-hand sheets and side idle/walk sword anchors written")

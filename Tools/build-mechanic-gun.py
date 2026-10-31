"""Mechanic gun anchors and fist layer, from a per-frame table read by eye off the bare and armed sheets.
Writes the "gun" entries of Assets/Resources/Data/classes/mechanic/held-mechanic.json (no other part of the file changes) and
Assets/Resources/Sprites/classes/mechanic/body/hand/<anim>.png: same grid as body/bare, only the fist of the frames where the gun is drawn in front of the body
(HeroRenderer draws that sheet above the gun). Safe to run again.
Usage: python Tools/build-mechanic-gun.py   (from the project root; needs Pillow and numpy)

A table row is (fist_x, fist_y, aim, flip, behind) in frame pixels of the bare sheet:
  aim   direction of the muzzle on screen, degrees clockwise from pointing right (0 right, 90 down, 180 left, -90 up)
  flip  1 = art as painted, the handle on the clockwise side of the muzzle (hangs down when aiming right); -1 = true mirror (hangs down when aiming left)
  behind  True = the gun is drawn under the body, which covers the handle with its own fist (hand farther from the camera than the gun, or a back view)
HeroRenderer rotates a sprite that was scaled first, so angle = aim with flip 1 and aim - 180 with flip -1.
The gun is in the hero's right hand, the arm opposite the gauntlet with the blue eye: viewer's left facing the camera, viewer's right from behind,
the near hand facing right, the far hand facing left. Anims in MIRRORED draw the right row flipped in the game, so their left anchors are the right ones mirrored.
Re-read the table whenever a mechanic body/bare sheet is repainted (docs/sprite-weapon-rules.md rule 5)."""
import json, math, os, re, uuid
import numpy as np
from PIL import Image

FRAME = 192
GUN_SCALE = 0.23  # art px to frame px, same for every anim (rule 3): the scrap art is 258 px rear to muzzle; the part of the baked gun clear of the body measures 57-62 px at idle and up to 50 px in attack1, where the hand hides the rest
FIST_R = 12.5, 12.5  # generous: the layer only repeats the body's own pixels, so where the bare sheet is empty the handle still shows
FEATHER = 1.5
RES = "Assets/Resources"
DIRS = ["down", "up", "left", "right"]
MIRRORED = {"attack3", "dash", "hurt"}  # HeroClassDef.MirrorLeft (death has no bare sheet)

F, B = False, True
FRAMES = {
    "idle": {
        "down": [(59, 128, -132, -1, F), (59, 127.5, -132, -1, F), (55, 128, -132, -1, F), (59, 128, -132, -1, F)],
        "up": [(143.5, 105, -63, 1, B), (143.5, 103, -63, 1, B), (141, 105, -63, 1, B), (141, 105, -63, 1, B)],
        "left": [(64, 123.5, -132, -1, B), (61.5, 122.5, -132, -1, B), (64, 125, -132, -1, B), (61.5, 125, -132, -1, B)],
        "right": [(143.5, 115, -50, 1, F), (138.5, 115, -50, 1, F), (138.5, 115, -50, 1, F), (138.5, 115, -50, 1, F)],
    },
    "walk": {
        "down": [(72, 132, 122, -1, F), (69.5, 142, 108, -1, F), (68.5, 142, 124, -1, F), (73.5, 142, 110, -1, F)],
        "up": [(128.5, 130, 55, 1, B), (126, 145, 75, 1, B), (136, 140, 54, 1, B), (123.5, 145, 75, 1, B)],
        "left": [(23.5, 125, 155, -1, B), (36, 140, 135, -1, B), (41, 140, 135, -1, B), (46, 135, 130, -1, B)],
        "right": [(68.5, 148, 120, 1, F), (96, 158, 62, 1, F), (146, 140, 39, 1, F), (93.5, 158, 62, 1, F)],
    },
    "attack1": {
        "down": [(83.5, 122.5, 180, -1, F), (68.5, 120, 180, -1, F), (41, 117.5, 160, -1, F), (83.5, 120, 180, -1, F)],
        "up": [(153.5, 106.5, -20, 1, B), (136, 105, -20, 1, B), (153.5, 107.5, -23, 1, B), (151, 105, -20, 1, B)],
        "left": [(48.5, 115, 180, -1, B), (58.5, 115, 180, -1, B), (41, 115, -165, -1, B), (48.5, 115, 180, -1, B)],
        "right": [(147, 115, -12, 1, F), (131, 110, -20, 1, F), (148, 122, 6, 1, F), (148, 110, -12, 1, F)],
    },
    "attack3": {
        "down": [(118, 130, 5, 1, F), (118, 130, 5, 1, F), (118, 130, 5, 1, F), (75, 125, 175, -1, F), (75, 125, 175, -1, F), (118, 130, 5, 1, F)],
        "up": [(158, 107, -5, 1, B), (151, 107, -5, 1, B), (136, 107, -5, 1, B), (153, 115, 0, 1, B), (51, 107, -170, -1, B), (153, 105, -10, 1, B)],
        "right": [(130, 124, -5, 1, F), (130, 127, -5, 1, F), (123, 128, -5, 1, F), (150, 128, 0, 1, F), (150, 118, -15, 1, F), (132, 126, -5, 1, F)],
    },
    "dash": {
        "down": [(40, 175, 150, -1, F), (155, 140, 20, 1, F), (60, 165, 155, -1, B)],
        "up": [(147.5, 123, 45, 1, B), (170, 140, 30, 1, B), (168, 168, 40, 1, B)],
        "right": [(172, 133, 45, 1, F), (168, 140, 3, 1, F), (166, 138, 18, 1, F)],
    },
    "hurt": {
        "down": [(57.5, 113, -135, -1, F), (148, 168, 35, 1, F)],
        "up": [(40, 105, -125, -1, B), (138, 143, 70, 1, B)],
        "right": [(60, 93, -128, -1, F), (135, 158, 25, 1, F)],
    },
}


def rows(anim):
    table = FRAMES[anim]
    out = dict(table)
    if anim in MIRRORED:
        out["left"] = [(FRAME - x, y, mirror_aim(aim), -flip, behind) for x, y, aim, flip, behind in table["right"]]
    return out


def mirror_aim(aim):
    return normal(180 - aim)


def normal(deg):
    return ((deg + 180) % 360) - 180


def anchor(row):
    x, y, aim, flip, behind = row
    angle = normal(aim if flip > 0 else aim - 180)
    return {"x": x, "y": y, "angle": angle, "s": GUN_SCALE, "sx": flip, "behind": behind, "hidden": False, "fallback": False}


def write_anchors():
    path = f"{RES}/Data/classes/mechanic/held-mechanic.json"
    raw = open(path, "rb").read().decode()
    head, sep, tail = raw.partition('"anchors":')
    old = json.loads("{" + sep + tail[: tail.rindex("}")] + "}")["anchors"]
    for a in old:
        if a["part"] == "gun" and a["anim"] in FRAMES:
            table = rows(a["anim"])[a["dir"]]
            assert len(table) == len(a["frames"]), (a["anim"], a["dir"])
            a["frames"] = [anchor(r) for r in table]
    text = json.dumps(old, separators=(",", ":"))
    open(path, "wb").write((head + sep + text + tail[tail.rindex("}"):]).encode())


def fist_mask(cx, cy):
    y, x = np.mgrid[0:FRAME, 0:FRAME]
    d = np.sqrt(((x + 0.5 - cx) / FIST_R[0]) ** 2 + ((y + 0.5 - cy) / FIST_R[1]) ** 2)
    return np.clip((1 - d) * min(FIST_R) / FEATHER, 0, 1)


def write_meta(anim, folder):
    src = open(f"{RES}/Sprites/classes/mechanic/body/bare/{anim}.png.meta", "rb").read().decode()
    path = f"{RES}/Sprites/{folder}/{anim}.png.meta"
    if os.path.exists(path):  # keep the guid Unity already knows
        return
    open(path, "wb").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1).encode())


def write_hand_sheets():
    folder = "classes/mechanic/body/hand"
    os.makedirs(f"{RES}/Sprites/{folder}", exist_ok=True)
    if not os.path.exists(f"{RES}/Sprites/{folder}.meta"):
        src = open(f"{RES}/Sprites/classes/mechanic/body/bare.meta", "rb").read().decode()
        open(f"{RES}/Sprites/{folder}.meta", "wb").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, src, count=1).encode())
    for anim in FRAMES:
        bare = np.array(Image.open(f"{RES}/Sprites/classes/mechanic/body/bare/{anim}.png").convert("RGBA")).astype(np.float64)
        out = np.zeros_like(bare)
        table = rows(anim)
        for dir_name, frames in table.items():
            if anim in MIRRORED and dir_name == "left":
                continue  # the game flips the right row
            r = DIRS.index(dir_name)
            for c, (x, y, _, _, behind) in enumerate(frames):
                if behind:
                    continue
                x0, y0 = c * FRAME, r * FRAME
                cell = bare[y0:y0 + FRAME, x0:x0 + FRAME].copy()
                cell[..., 3] *= fist_mask(x, y)
                out[y0:y0 + FRAME, x0:x0 + FRAME] = cell
        Image.fromarray(out.round().astype(np.uint8), "RGBA").save(f"{RES}/Sprites/{folder}/{anim}.png")
        write_meta(anim, folder)


if __name__ == "__main__":
    write_anchors()
    write_hand_sheets()
    print("mechanic gun anchors and hand sheets written")

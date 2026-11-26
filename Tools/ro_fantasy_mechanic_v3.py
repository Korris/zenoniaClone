"""Mechanic V3 (Asteria, 26/11): weapon set of the RO Fantasy mechanic = ONE hand cannon in the right fist, no offhand.

The design chat (Tay, https://chatgpt.com/c/6b079286-ba30-83ec-818a-cfffc11d9b9e) made the Engineer a one-hand cannon user
(art: Tools/build-mechanic-v3-cannon.py, 55 % H). The shared grip table (hero_layered_grip_poses.WEAPONS) still lists two pistols for
the common layered body; Tools/ro_fantasy_grips.cmd_grips calls:
  weapons_for_grips(poses)  -> context manager: WEAPONS["mechanic"] = cannon in both hands while class_grips runs (restored after)
  finish(cj)                -> drops the offhand entry + grips (left fist empty; HeroLayeredView hides a part with no sprite)
Pose rules stay the shared pistol ones ("pistol" kind: rest muzzle down-forward, attack along the arm / aim_raised).
"""
from contextlib import contextmanager

CANNON = ("mechanic-cannon", "pistol")


@contextmanager
def weapons_for_grips(poses):
    old = poses.WEAPONS.get("mechanic")
    poses.WEAPONS["mechanic"] = (CANNON, CANNON)
    try:
        yield
    finally:
        poses.WEAPONS["mechanic"] = old


WALK_LEFT_BACK_X, WALK_LEFT_BACK_ANGLE = 175, 165  # walk left: far fist behind the hip (cell x) -> muzzle down-back (0 = up, clockwise)
MUZZLE_FLOOR = 236  # cell row the muzzle may not pass (feet line 244): the 119 px cannon hanging from a hip fist reached under the feet


def finish(cj, poses):
    cj["weapons"] = [w for w in cj["weapons"] if w["part"] != "offhand"]
    cj["grips"] = [g for g in cj["grips"] if g["part"] != "offhand"]
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")  # grip -> muzzle (art top), art px
    for g in cj["grips"]:  # walk left (mirrored right walk, cannon in the FAR fist, behind the body): when the far fist swings back past
        # the hip the forward-down cannon ran under the coat and vanished on 4 of 8 frames (check 26/11) -> hang it down-back instead
        if g["part"] == "weapon" and g["anim"] == "walk" and g["dir"] == 6 and g["x"] > WALK_LEFT_BACK_X:
            g["angle"] = WALK_LEFT_BACK_ANGLE
    for g in cj["grips"]:  # every pose: turn a low-hanging cannon toward horizontal (same side) until the muzzle clears the floor
        a = g["angle"]
        g["angle"] = round(poses.keep_tip_above_ground(a, g["y"], gy, 1 if a >= 0 else -1, ground=MUZZLE_FLOOR), 1)


# Fists of the V3 art (cell px, read on 4x grids of the installed sheets): (class, anim, painted row, frame) -> {hand: [x, y, visible]}.
# Merged over ro_fantasy_grips.OVERRIDES (after the old V2 walk / attack tables of ro_fantasy_motion_fists, which they replace).
T, F = True, False
FISTS = {
    ("mechanic", "idle", 0, 0): {"R": [120, 164, T], "L": [202, 164, T]},
    ("mechanic", "idle", 1, 0): {"R": [124, 163, T], "L": [199, 162, T]},
    ("mechanic", "idle", 2, 0): {"R": [161, 162, T], "L": [172, 165, F]},
    ("mechanic", "idle", 3, 0): {"R": [195, 165, T], "L": [119, 167, T]},
    ("mechanic", "idle", 4, 0): {"R": [205, 160, T], "L": [118, 162, T]},
}

# walk 8 frames (body-mechanic-walk-<dir>-8f-chatgpt-1, 26/11), read on 3x grids: row -> [(R, L)] per frame, R = right hand (cannon)
_WALK = {
    0: [((118, 172), (200, 153)), ((120, 175), (200, 168)), ((120, 160), (197, 152)), ((120, 147), (203, 153)),
        ((118, 163), (193, 157)), ((118, 153), (197, 173)), ((120, 145), (190, 158)), ((117, 155), (200, 140))],
    1: [((120, 167), (197, 163)), ((130, 170), (198, 163)), ((130, 168), (193, 150)), ((117, 167), (188, 145)),
        ((133, 167), (193, 153)), ((130, 172), (195, 157)), ((120, 155), (193, 152)), ((140, 168), (188, 147))],
    2: [((170, 173), (185, 185)), ((167, 175), (183, 185)), ((183, 160), (127, 150)), ((180, 160), (123, 148)),
        ((133, 168), (192, 153)), ((158, 168), (195, 155)), ((183, 160), (127, 150)), ((183, 153), (133, 150))],
    3: [((193, 172), (117, 155)), ((187, 177), (122, 157)), ((193, 167), (110, 148)), ((203, 153), (110, 153)),
        ((207, 148), (117, 157)), ((192, 173), (118, 157)), ((203, 157), (113, 158)), ((185, 155), (123, 155))],
    4: [((197, 160), (118, 158)), ((198, 173), (118, 170)), ((197, 160), (115, 158)), ((203, 152), (117, 157)),
        ((205, 172), (117, 160)), ((200, 167), (117, 168)), ((203, 158), (117, 160)), ((197, 155), (117, 160))],
}
_HIDDEN_L = {(2, 0), (2, 1)}  # right view: the far fist behind the hip
for _r, _row in _WALK.items():
    for _f, (_R, _L) in enumerate(_row):
        FISTS[("mechanic", "walk", _r, _f)] = {"R": [*_R, T], "L": [*_L, (_r, _f) not in _HIDDEN_L]}

# Attack: 8 frames x 8 painted Dir8 rows (body-mechanic-attack-<dir>-8f-chatgpt-1, 26/11: hand-cannon shot, f0 brace, f1 raise + aim,
# f2 aim, f3 FIRE recoil, f4 recoil peak, f5 recover, f6 back, f7 = idle). Fists read on 3x grids of the cut cells: d8 -> [(R, L)] per frame.
_ATTACK = {
    0: [((111, 163), (207, 166)), ((124, 121), (178, 145)), ((113, 121), (174, 145)), ((102, 100), (178, 137)),
        ((106, 71), (178, 130)), ((124, 127), (185, 148)), ((122, 159), (207, 165)), ((113, 168), (207, 168))],
    1: [((115, 168), (201, 161)), ((160, 132), (194, 141)), ((190, 128), (189, 152)), ((113, 107), (192, 134)),
        ((115, 82), (194, 129)), ((131, 139), (190, 141)), ((119, 165), (201, 161)), ((119, 168), (203, 168))],
    2: [((122, 164), (201, 166)), ((200, 123), (180, 148)), ((216, 123), (178, 146)), ((113, 100), (200, 128)),
        ((119, 82), (200, 127)), ((135, 132), (183, 136)), ((124, 165), (191, 161)), ((122, 172), (196, 165))],
    3: [((187, 177), (104, 164)), ((207, 121), (182, 152)), ((214, 114), (104, 175)), ((178, 118), (102, 179)),
        ((185, 87), (115, 168)), ((203, 129), (120, 141)), ((196, 168), (120, 168)), ((200, 168), (117, 168))],
    # up: redrawn sheet body-mechanic-attack-up-8f-chatgpt-2 (26/11, replaces -1); rows 5-7 = the painted left dirs (26/11): up-left
    # chatgpt-3-f5fix (F5 = copy of F6), left chatgpt-1, down-left chatgpt-1. Read on 2x grids of the cut cells, snapped to the glove
    # pixels. Left dirs: R = the right hand = the far arm (it does the shot, painted, not mirrored)
    4: [((210, 174), (110, 173)), ((205, 91), (114, 146)), ((200, 71), (114, 152)), ((204, 76), (116, 152)),
        ((197, 56), (117, 144)), ((205, 92), (114, 148)), ((206, 127), (114, 163)), ((209, 173), (110, 172))],
    5: [((212, 168), (125, 172)), ((120, 89), (122, 172)), ((96, 73), (122, 173)), ((204, 93), (115, 152)),
        ((215, 76), (122, 151)), ((211, 168), (124, 171)), ((211, 168), (125, 171)), ((211, 167), (125, 171))],
    6: [((125, 157), (180, 159)), ((101, 114), (159, 147)), ((95, 113), (159, 147)), ((118, 102), (151, 128)),
        ((119, 89), (155, 128)), ((113, 137), (157, 149)), ((117, 153), (188, 160)), ((121, 162), (188, 160))],
    7: [((105, 170), (198, 170)), ((94, 127), (173, 141)), ((88, 125), (176, 141)), ((110, 100), (169, 140)),
        ((98, 83), (162, 141)), ((106, 142), (179, 143)), ((104, 163), (201, 169)), ((108, 169), (202, 169))],
}
_ATTACK_HIDDEN = {(3, 2, "L")}  # (d8, f, hand) painted behind the body
ATTACK_FISTS = {("mechanic", d8, f): {"R": [*R, (d8, f, "R") not in _ATTACK_HIDDEN], "L": [*L, (d8, f, "L") not in _ATTACK_HIDDEN]}
                for d8, row in _ATTACK.items() for f, (R, L) in enumerate(row)}
import ro_fantasy_attack8_grips as _a8  # noqa: E402  (ro_fantasy_grips.table_of reads 8-row attack fists from its FISTS)
_a8.FISTS.update(ATTACK_FISTS)

# cannon angle (0 = muzzle up, clockwise) while aiming f1-f5, per Dir8 (down: muzzle down and a bit to the right-hand side of the screen,
# reads as aimed at the viewer; up: a bit outward so it shows beside the back)
AIM = {0: -160, 1: 135, 2: 90, 3: 45, 4: 15, 5: -45, 6: -90, 7: -135}
KICK = {1: 0, 2: 0, 3: 28, 4: 45, 5: 12}  # recoil: muzzle turned this far toward up (f3 fire, f4 peak, f5 settling)
# the art's recoil throws the fist up beside / behind the head (down, down-right, right f3-f4): there the cannon points up-back like a
# shouldered kick instead of along AIM (which would cross the face). NOT mirrored to the painted left dirs (26/11): there the far (right)
# fist rises on the FACING side, in front of the face, so AIM + KICK reads right (a mirrored up-back angle pointed the muzzle over the head)
# frames whose art is already the rest pose although f is in KICK: up-left F5 is a copy of F6 (chatgpt-3-f5fix) -> idle grip, else the
# recoil angle hid the cannon behind the back for one frame between the raised F4 and the rest F6
REST = {(5, 5)}
ANGLE = {(0, 3): -60, (0, 4): -30, (1, 3): -40, (1, 4): -20, (2, 3): -30, (2, 4): -15}


def attack8(cj, poses):
    """Replace the weapon grips of the 8-row attack: the cannon on the painted R fist of each Dir8 row (no mirroring), aimed along AIM
    with the recoil KICK on f1-f5; f0 / f6 / f7 keep the idle grip angle + depth of that dir (no pop at the start / end)."""
    rows = {d8: [ATTACK_FISTS[("mechanic", d8, f)] for f in range(8)] for d8 in range(8) if ("mechanic", d8, 0) in ATTACK_FISTS}
    if len(rows) < 8:
        return False
    idle = {g["dir"]: g for g in cj["grips"] if g["part"] == "weapon" and g["anim"] == "idle" and g["f"] == 0}
    out = [g for g in cj["grips"] if not (g["part"] == "weapon" and g["anim"] == "attack")]
    for d8 in range(8):
        for f in range(8):
            x, y, vis = rows[d8][f]["R"]
            if (d8, f) in ANGLE and f in KICK:
                a = ANGLE[(d8, f)]
                behind = True
            elif f in KICK and (d8, f) not in REST:
                a = AIM[d8]
                a = a - KICK[f] if a > 0 else a + KICK[f]  # toward 0 (up)
                behind = d8 in (3, 4, 5) or not vis
            else:
                a, behind = idle[d8]["angle"], idle[d8]["behind"]
            out.append({"part": "weapon", "anim": "attack", "dir": d8, "f": f, "x": float(x), "y": float(y),
                        "angle": round(poses.wrap(a), 1), "behind": bool(behind), "flip": d8 >= 5})
    cj["grips"] = out
    return True

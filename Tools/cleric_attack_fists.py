"""Fists and staff angles of the Cleric attack rows (Tools/build-cleric-attack-body.py), read on its `grid` images (cell px, 192 frame).

FISTS[(anim, dir)] = per frame ((staff fist = her RIGHT hand), (shield fist = her LEFT hand)).
STAFF[(anim, dir)] = per frame staff angle, degrees clockwise from upright (crystal up), as held-cleric anchors.
Draw order (rule 4): staff in the right hand, shield on the left arm.
  down: staff in front (screen left), shield in front (screen right)    up: both behind the body (we see her back)
  right: staff near hand in front, shield far arm behind               left: shield near arm in front, staff far hand behind,
  except frames where the far fist is in front of the body (ON_BODY: drawn in front, under the fist layer, Warrior 2026-11-01 rule).
"""

FISTS = {
    # 1 Light Thrust: staff fist thrusts toward the target
    ("attack1", "down"): [((68, 135), (114, 125)), ((65, 132), (113, 126)), ((60, 125), (113, 125)), ((66, 132), (112, 126))],
    ("attack1", "up"): [((125, 134), (73, 134)), ((136, 113), (63, 140)), ((138, 104), (63, 138)), ((132, 117), (63, 140))],
    ("attack1", "left"): [((73, 129), (98, 139)), ((57, 122), (90, 134)), ((49, 120), (90, 132)), ((61, 125), (94, 134))],
    ("attack1", "right"): [((88, 139), (118, 129)), ((130, 123), (109, 131)), ((145, 121), (115, 132)), ((122, 127), (97, 131))],
    # 2 Aegis Ray: shield fist pushes toward the target, staff fist back at the hip
    ("attack2", "down"): [((68, 139), (110, 124)), ((66, 139), (118, 123)), ((67, 137), (127, 125)), ((65, 139), (109, 125))],
    ("attack2", "right"): [((88, 141), (118, 124)), ((97, 141), (134, 122)), ((98, 140), (147, 122)), ((93, 139), (120, 124))],
    # 3 Halo Lance: staff fist raised beside the head
    ("attack3", "down"): [((58, 113), (117, 140)), ((60, 77), (118, 140)), ((56, 83), (116, 140)), ((60, 119), (117, 140))],
    ("attack3", "up"): [((131, 104), (73, 132)), ((127, 71), (63, 138)), ((134, 75), (63, 137)), ((129, 113), (65, 132))],
    ("attack3", "left"): [((67, 104), (97, 142)), ((63, 69), (97, 142)), ((54, 75), (100, 142)), ((65, 115), (95, 141))],
    ("attack3", "right"): [((129, 111), (88, 140)), ((131, 77), (91, 139)), ((138, 83), (98, 138)), ((123, 119), (92, 138))],
    # 4 Judgment Bloom: staff fist lifted, then driven down (frames 2-4 crouched, staff planted: PLANT), shield arm out to the side
    ("attack4", "down"): [((63, 140), (115, 124)), ((73, 116), (115, 124)), ((83, 175), (121, 139)), ((86, 175), (124, 148)), ((76, 175), (123, 138)), ((68, 141), (116, 124))],
    ("attack4", "up"): [((124, 139), (67, 121)), ((115, 128), (67, 116)), ((113, 145), (64, 135)), ((110, 165), (60, 142)), ((112, 160), (61, 135)), ((113, 131), (65, 121))],
    ("attack4", "left"): [((66, 128), (106, 129)), ((69, 112), (96, 123)), ((61, 160), (101, 139)), ((59, 167), (101, 143)), ((63, 158), (108, 136)), ((70, 135), (104, 127))],
    ("attack4", "right"): [((120, 122), (101, 131)), ((122, 113), (108, 122)), ((110, 164), (103, 148)), ((121, 168), (86, 154)), ((115, 158), (96, 157)), ((120, 122), (91, 138))],
    # mirrored rows (MIRRORS): fists already converted (x -> 192 - x, right and left hand swapped)
    ("attack2", "left"): [((74, 129), (104, 139)), ((83, 131), (62, 123)), ((77, 132), (47, 121)), ((95, 131), (70, 127))],
    ("attack2", "up"): [((123, 132), (71, 136)), ((127, 138), (58, 109)), ((127, 140), (58, 98)), ((125, 136), (63, 119))],
}
# (anim, dir) -> (source anim, source dir): the fitted source row drawn mirrored. ChatGPT pushed the wrong arm in these rows of attack2
# (left: the far arm, up: the right arm); mirroring a row keeps near / far and swaps right / left, so the pushing arm becomes the left one.
MIRRORS = {("attack2", "left"): ("attack1", "right"), ("attack2", "up"): ("attack2", "up")}
# crystal direction of angle a = (sin a, -cos a); the butt (67 px below the grip) points the other way, so a thrust down keeps the
# crystal down-RIGHT (170) facing down: down-left would swing the butt across her face.
STAFF = {
    ("attack1", "down"): [-10, 150, 170, 140],
    ("attack1", "up"): [10, 20, 25, 15],
    ("attack1", "left"): [-20, -70, -90, -50],
    ("attack1", "right"): [-45, 70, 90, 50],
    ("attack2", "down"): [-8, -12, -15, -10],
    ("attack2", "up"): [10, 12, 12, 10],
    ("attack2", "left"): [50, 55, 55, 50],
    ("attack2", "right"): [-50, -55, -55, -50],
    # raised fist in front of the face facing right: tilting the crystal forward swings the butt across her face, so the staff stays upright
    # frames 0 and 3 (fist at the chin): an upright staff put the crystal on her hair / face, so it leans out
    ("attack3", "down"): [-20, -3, 5, -20],
    ("attack3", "up"): [0, 0, 5, 0],
    ("attack3", "left"): [-25, 10, 0, -25],
    ("attack3", "right"): [25, -10, 0, 25],
    ("attack4", "down"): [-20, -20, -15, -15, -15, -10],
    ("attack4", "up"): [0, 0, 0, 0, 0, 0],
    ("attack4", "left"): [-10, -25, -15, -15, -15, -10],
    ("attack4", "right"): [10, 25, 35, 35, 35, 10],  # planted staff leans forward clear of the crouched face
}
# Frames whose staff is planted in the ground: the grip anchor slides along the shaft so the butt end sits on the sole row and the fist
# stays on the shaft (the fist is low at the knee, a staff gripped there would sink 60 px under her feet). Measured for the T0 staff
# (butt 129 art px under the grip x s 0.52); a tier with a longer butt sinks a little, a shorter one floats a little.
PLANT = {("attack4", d, f) for d in ("down", "up", "left", "right") for f in (2, 3, 4)}
BUTT_PX = 67.0
SOLE_Y = 190.0
SHIELD_TILT = {"down": 5, "up": -5, "left": -8, "right": 8}
# shield centre = shield fist + offset: out and down so it does not cover the chin (build-cleric-shield.py FREE_FIST did the same)
SHIELD_OFF = {"down": (14, 12), "up": (-6, 4), "left": (6, 10), "right": (-4, 8)}
ON_BODY = {("attack2", "right", 1), ("attack2", "right", 2)}  # (anim, dir, frame) whose far-hand gear is drawn in front of the body (the pushed-out shield)


STAFF_BACK = {("attack2", "right")}  # staff held back behind her: drawn behind the body (in front, its shaft crossed the back of the hair)


def staff_behind(anim, d, f):
    if d == "up" or (anim, d) in STAFF_BACK:
        return True
    if d == "left":
        return (anim, d, f) not in ON_BODY
    return False


def shield_behind(anim, d, f):
    if d == "up":
        return True
    if d == "right":
        return (anim, d, f) not in ON_BODY
    return False

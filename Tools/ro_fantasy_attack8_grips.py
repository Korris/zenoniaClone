"""Warrior RO Fantasy attack painted for all 8 Dir8 dirs (2026-11-20): sword + shield grips straight from each painted row (no mirroring).

Called by Tools/ro_fantasy_grips.py cmd_grips when body/attack.png has 8 rows. The generic poses (hero_layered_grip_poses.class_grips) mirror
rows 1-3 for the left dirs and put the sword in the far fist of the mirrored art; a painted left row already has the sword fist in the right
hand, so every grip here reads its own row: weapon on the R fist, shield on the L fist of fists table row d8.

Sword angle (grip convention: 0 = tip up, clockwise on the y-down cell, 90 = tip right): SWORD[(d8, f)] when listed (read on the 8-dir check
sheet, docs/reference/ro-fantasy-heroes-ingame/warrior-attack-8dir.png), else the shoulder -> fist line. Key poses (ChatGPT animation
direction, ArtLibrary/ro-fantasy-heroes/prompts/warrior-attack-8dir.md): 8 frames x 50 ms, f1 wind-up key (sword raised behind the right
shoulder), f2 strike key (blade at the target; damage at 120 ms), f4 follow-through key (blade low past the target), f6 recover key, the
others in-betweens. A 4-frame art keeps working (frames = len(rows[d8])).
Depth: the sword goes behind the body when it swings away from the camera (up-right, up, up-left) or sits in the far fist (left, down-left)
unless that fist is painted over the torso; SWORD_BEHIND overrides per frame.
"""
import math

import hero_layered_fists as hf

CW = 320
SHOULDER_DY = 80  # skull top -> sword shoulder (cell px; V2 figure 208 px tall, head ~80 px)
import ro_fantasy_motion_fists as _mf  # noqa: E402

# (class, d8, f): {hand: [x, y, visible]}; V2 Adventure redraw 22/11: ro_fantasy_motion_fists.WARRIOR_V2_ATTACK
FISTS = {("warrior", d8, f): v for (d8, f), v in _mf.WARRIOR_V2_ATTACK.items()}
SWORD = {  # (d8, f): angle. V2 heavy slash 22/11 (impact frame f4 lands on the blow, WarriorSlashFeel): f0 crouch + f1 wind-up raised behind
          # the sword shoulder, f2 over the top, f3 mid-arc (fast), f4 IMPACT at the target, f5 overshoot +20-25 deg past f4 (ChatGPT score round, 22/11: +12 more so follow-through and impact differ), f6 recovering,
          # f7 = the idle angle of that dir (taken from the idle grip in apply(): no pop when the attack ends). Sweep: right / down-right /
          # up-right clockwise, down / up / left dirs counter-clockwise (WarriorSlashLightFx ribbon follows the same rule).
    (0, 0): -40, (0, 1): -20, (0, 2): -75, (0, 3): -120, (0, 4): -170, (0, 5): 128, (0, 6): -160,
    (1, 0): -60, (1, 1): -30, (1, 2): 20, (1, 3): 70, (1, 4): 120, (1, 5): 172, (1, 6): -150,
    (2, 0): -60, (2, 1): -25, (2, 2): 30, (2, 3): 80, (2, 4): 110, (2, 5): 162, (2, 6): 135,
    (3, 0): 70, (3, 1): 0, (3, 2): 30, (3, 3): 60, (3, 4): 90, (3, 5): 147, (3, 6): 130,
    (4, 0): 30, (4, 1): 10, (4, 2): 0, (4, 3): -40, (4, 4): -95, (4, 5): -137, (4, 6): 125,  # up: blade behind the body, impact / overshoot reach past the head / body on the left so they read
    (5, 0): 20, (5, 1): 0, (5, 2): -30, (5, 3): -60, (5, 4): -90, (5, 5): -147, (5, 6): 125,
    (6, 0): 60, (6, 1): 25, (6, 2): -30, (6, 3): -80, (6, 4): -110, (6, 5): -162, (6, 6): -135,
    (7, 0): 60, (7, 1): 30, (7, 2): -20, (7, 3): -70, (7, 4): -120, (7, 5): -172, (7, 6): -150,
}
SWORD_BEHIND = {(2, 0): True, (2, 1): True, (6, 0): True, (6, 1): True, (7, 0): True, (7, 1): True}  # (d8, f): bool; f0-f1 wind-up: the sword fist is pulled back behind the head / the far shoulder
SHIELD = {}        # (d8, f): angle (default: the idle shield angle of that dir)


def sword_angle(fx, fy, h):
    sx = h["x"]
    sy = h["y"] + SHOULDER_DY
    return math.degrees(math.atan2(fx - sx, sy - fy))


def apply(cj, rows, heads, cells, poses):
    """Replace the attack grips in class.json `cj` (rows = fists table of the attack, 8 rows)."""
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")
    idle_shield = {g["dir"]: g for g in cj["grips"] if g["part"] == "offhand" and g["anim"] == "idle" and g["f"] == 0}
    idle_sword = {g["dir"]: g for g in cj["grips"] if g["part"] == "weapon" and g["anim"] == "idle" and g["f"] == 0}
    grips = [g for g in cj["grips"] if g["anim"] != "attack"]
    for d8 in range(8):
        for f in range(len(rows[d8])):
            h = heads[("attack", d8, f)]
            cell = cells[d8][f]
            rx, ry, rvis = rows[d8][f]["R"]
            lx, ly, lvis = rows[d8][f]["L"]
            a = SWORD.get((d8, f))
            if f == len(rows[d8]) - 1 and idle_sword.get(d8) is not None:
                a = idle_sword[d8]["angle"]  # guard frame = the idle angle: no pop when the attack ends
            if a is None:
                a = sword_angle(rx, ry, h)
            a = poses.keep_tip_above_ground(a, ry, gy, 1 if math.sin(math.radians(a)) >= 0 else -1, ground=248)
            cover = hf.ring_cover(cell, rx, ry)
            behind = SWORD_BEHIND.get((d8, f), (not rvis) or d8 in (3, 4, 5) or (d8 in (6, 7) and cover < 0.55))
            grips.append({"part": "weapon", "anim": "attack", "dir": d8, "f": f, "x": round(rx, 1), "y": round(ry, 1),
                          "angle": round(poses.wrap(a), 1), "behind": bool(behind), "flip": False})
            i = idle_shield.get(d8)
            sa = SHIELD.get((d8, f), i["angle"] if i else 0.0)
            scover = hf.ring_cover(cell, lx, ly)
            grips.append({"part": "offhand", "anim": "attack", "dir": d8, "f": f, "x": round(lx, 1), "y": round(ly, 1),
                          "angle": round(sa, 1), "behind": bool(poses.behind_of(d8, "off", lvis, scover)),
                          "flip": bool(i["flip"]) if i else d8 >= 5})
    cj["grips"] = grips
    print("warrior attack: 8 painted dirs,", sum(g["anim"] == "attack" for g in grips), "grips")

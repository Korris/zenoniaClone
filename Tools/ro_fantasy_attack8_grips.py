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
SHOULDER_DY = 70  # skull top -> sword shoulder (cell px, figure 188 px tall, head ~80 px)
FISTS = {  # (class, d8, f): {hand: [x, y, visible]}, read on the 4x grids of the 22/11 art (auto detection took belt / cape / swapped hands)
    ("warrior", 0, 0): {'L': [190, 158, True]},
    ("warrior", 1, 1): {'L': [215, 137, True]},
    ("warrior", 1, 3): {'R': [170, 188, True], 'L': [171, 160, True]},
    ("warrior", 2, 0): {'L': [202, 180, True]},
    ("warrior", 2, 1): {'R': [126, 102, True], 'L': [205, 161, True]},
    ("warrior", 2, 2): {'L': [200, 170, False]},
    ("warrior", 2, 3): {'L': [215, 175, False]},
    ("warrior", 2, 4): {'R': [192, 200, True], 'L': [225, 170, True]},
    ("warrior", 2, 5): {'R': [190, 200, True], 'L': [193, 175, True]},
    ("warrior", 2, 6): {'L': [220, 164, True]},
    ("warrior", 2, 7): {'L': [197, 162, True]},
    ("warrior", 3, 0): {'L': [115, 156, True]},
    ("warrior", 3, 1): {'L': [100, 156, True]},
    ("warrior", 3, 2): {'L': [175, 170, False]},
    ("warrior", 3, 3): {'L': [190, 175, False]},
    ("warrior", 3, 4): {'R': [165, 185, False], 'L': [140, 180, False]},
    ("warrior", 3, 5): {'R': [151, 185, True], 'L': [200, 175, False]},
    ("warrior", 3, 6): {'L': [118, 168, False]},
    ("warrior", 3, 7): {'L': [118, 165, True]},
    ("warrior", 4, 0): {'L': [118, 177, True]},
    ("warrior", 4, 1): {'L': [106, 156, True]},
    ("warrior", 4, 2): {'L': [100, 147, True]},
    ("warrior", 4, 3): {'L': [109, 144, True]},
    ("warrior", 4, 4): {'R': [153, 165, True], 'L': [205, 170, False]},
    ("warrior", 4, 5): {'R': [124, 165, True], 'L': [190, 170, False]},
    ("warrior", 4, 6): {'R': [232, 165, False], 'L': [150, 165, True]},
    ("warrior", 4, 7): {'R': [205, 174, True], 'L': [114, 174, True]},
    ("warrior", 5, 0): {'L': [127, 168, True]},
    ("warrior", 5, 1): {'L': [117, 159, True]},
    ("warrior", 5, 2): {'R': [103, 132, True], 'L': [150, 165, False]},
    ("warrior", 5, 3): {'R': [85, 97, True], 'L': [109, 163, True]},
    ("warrior", 5, 4): {'R': [121, 165, True], 'L': [150, 165, False]},
    ("warrior", 5, 5): {'R': [81, 176, True], 'L': [140, 165, False]},
    ("warrior", 5, 6): {'R': [196, 160, True], 'L': [125, 158, True]},
    ("warrior", 5, 7): {'R': [196, 160, True], 'L': [121, 165, True]},
    ("warrior", 6, 0): {'R': [211, 138, True], 'L': [138, 166, True]},
    ("warrior", 6, 1): {'R': [202, 93, True], 'L': [129, 152, True]},
    ("warrior", 6, 2): {'L': [132, 156, True]},
    ("warrior", 6, 3): {'R': [70, 159, True], 'L': [134, 175, True]},
    ("warrior", 6, 5): {'R': [118, 191, True], 'L': [145, 174, True]},
    ("warrior", 6, 6): {'R': [150, 175, False], 'L': [132, 162, True]},
    ("warrior", 7, 0): {'R': [208, 135, True], 'L': [139, 176, True]},
    ("warrior", 7, 1): {'R': [192, 98, True], 'L': [121, 153, True]},
    ("warrior", 7, 2): {'L': [166, 152, True]},
    ("warrior", 7, 5): {'R': [139, 213, True], 'L': [170, 185, True]},
}
SWORD = {  # (d8, f): angle. 22/11 heavy slash: f0-f1 raised behind the sword shoulder, f2 over the top, f3 at the target (Dir8 facing),
          # f4-f5 low across the body (follow-through), f6 recovering, f7 = the idle angle of that dir (no pop when the attack ends)
    (0, 0): -40, (0, 1): -20, (0, 2): -75, (0, 3): -150, (0, 4): 160, (0, 5): 135, (0, 6): -165, (0, 7): -124,
    (1, 0): -60, (1, 1): -30, (1, 2): 30, (1, 3): 110, (1, 4): 150, (1, 5): 165, (1, 6): -140, (1, 7): -122,
    (2, 0): -60, (2, 1): -20, (2, 2): 45, (2, 3): 95, (2, 4): 135, (2, 5): 150, (2, 6): 135, (2, 7): 122,
    (3, 0): 70, (3, 1): 0, (3, 2): 30, (3, 3): 50, (3, 4): -135, (3, 5): -140, (3, 6): 130, (3, 7): 115,
    (4, 0): 30, (4, 1): 10, (4, 2): 0, (4, 3): -10, (4, 4): -130, (4, 5): -140, (4, 6): 130, (4, 7): 112,
    (5, 0): 20, (5, 1): 0, (5, 2): -30, (5, 3): -50, (5, 4): -130, (5, 5): -140, (5, 6): 125, (5, 7): 111,
    (6, 0): 60, (6, 1): 20, (6, 2): -45, (6, 3): -95, (6, 4): -135, (6, 5): -150, (6, 6): -135, (6, 7): -108,
    (7, 0): 60, (7, 1): 30, (7, 2): -30, (7, 3): -110, (7, 4): -150, (7, 5): -165, (7, 6): -140, (7, 7): -124,
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
    grips = [g for g in cj["grips"] if g["anim"] != "attack"]
    for d8 in range(8):
        for f in range(len(rows[d8])):
            h = heads[("attack", d8, f)]
            cell = cells[d8][f]
            rx, ry, rvis = rows[d8][f]["R"]
            lx, ly, lvis = rows[d8][f]["L"]
            a = SWORD.get((d8, f))
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

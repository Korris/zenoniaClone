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
FISTS = {}         # (class, d8, f): {hand: [x, y, visible]} read on fists-warrior-attack.png (replaces ro_fantasy_motion_fists for 8-row art)
SWORD = {}         # (d8, f): angle, filled after reading the art
SWORD_BEHIND = {}  # (d8, f): bool
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

"""Wizard V3 attack painted for all 8 Dir8 dirs (26/11/2026): staff + book grips straight from each painted row (no mirroring).

Called by Tools/build-wizard-v3-game-assets.py grips (after ro_fantasy_grips.steady_staff) when body/attack.png has 8 rows. The generic
poses (hero_layered_grip_poses.class_grips) mirror rows 1-3 for the left dirs; a painted left row already has the staff fist in the right
hand, so every grip here reads its own row of the fists table: staff on R, book on L.

Cast rhythm (ComboConfigs.WizardHits 100 / 80 / 170 ms, frame = progress x 8, bolt at 100 ms = frame 2, prompt
ArtLibrary/ro-fantasy-heroes/ref/wizard-v3-test/prompts/attack8-prompt.txt): f0 gather, f1 staff raised, f2 thrust (cast), f3 hold,
f4 recoil, f5 pull back, f6 recover, f7 guard = idle.
Staff angle (0 = head up, clockwise on the y-down cell): f0 / f6 / f7 the idle angle of that dir, f1 the cast angle tilted back by
WIND_BACK, f2-f4 the cast angle FWD[d8] (staff head leaning toward the target, never pointing down at the feet: on the down / up views a
thrust toward / away from the camera reads as the head tipping outward), f5 halfway back to idle.
Depth: behind the body facing away (up-right, up, up-left) or in the far fist (left, down-left) unless that fist is painted over the torso.
"""
import json
import math
from importlib import import_module

import hero_layered_fists as hf

FWD = {0: -28, 1: 52, 2: 78, 3: 32, 4: 22, 5: -32, 6: -78, 7: -52}
WIND_BACK = 28  # f1: degrees back from the cast angle (away from the target)
STAFF_HEAD_FLOOR = 226
# Book (held by its bottom edge, rising over the fist): the V3 cast keeps the left fist at the chest (y < 157), where a book in front
# covers the mouth / face (check 26/11). There it is drawn behind the body: only its edges show; at the hip (idle, guard) it stays in front.
BOOK_CHEST_Y = 157  # 168 on the 188 px body, x 216/188 about the feet
# Per-frame staff angles fixed after looking at the composed check (26/11): shaft across the face (down-right f5, right f1 raised behind the
# head) or the ring sitting on the head like a halo (left f0 / f6, staff in the hidden far fist). "rest" = the idle angle of that dir.
ANGLE_FIX = {(1, 5): "rest", (2, 1): 5, (6, 0): -30, (6, 6): -30}


def apply(cj):
    g = import_module("ro_fantasy_grips")
    poses = import_module("hero_layered_grip_poses")
    anims, heads, cells = g.body("wizard")
    if len(cells.get("attack", [])) != 8:
        return
    table = json.loads((g.FISTS / "wizard-fists.json").read_text(encoding="utf-8"))["anims"]["attack"]
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")
    idle_staff = {x["dir"]: x for x in cj["grips"] if x["part"] == "weapon" and x["anim"] == "idle" and x["f"] == 0}
    idle_book = {x["dir"]: x for x in cj["grips"] if x["part"] == "offhand" and x["anim"] == "idle" and x["f"] == 0}
    grips = [x for x in cj["grips"] if x["anim"] != "attack"]
    for d8 in range(8):
        n = len(table[d8])
        for f in range(n):
            rx, ry, rvis = table[d8][f]["R"]
            lx, ly, lvis = table[d8][f]["L"]
            rest = idle_staff[d8]["angle"]
            cast = FWD[d8]
            back = -WIND_BACK if cast >= 0 else WIND_BACK
            a = {0: rest, 1: cast + back, 2: cast, 3: cast, 4: cast + 0.25 * back, 5: (cast + rest) / 2, 6: rest}.get(f, rest)
            if f == n - 1:
                a = rest  # guard frame = the idle angle: no pop when the attack ends
            fix = ANGLE_FIX.get((d8, f))
            if fix is not None:
                a = rest if fix == "rest" else fix
            a = poses.keep_tip_above_ground(a, ry, gy, 1 if a >= 0 else -1, ground=STAFF_HEAD_FLOOR)
            cell = cells["attack"][d8][f]
            cover = hf.ring_cover(cell, rx, ry)
            behind = (not rvis) or d8 in (3, 4, 5) or (d8 in (6, 7) and cover < 0.55)
            grips.append({"part": "weapon", "anim": "attack", "dir": d8, "f": f, "x": round(rx, 1), "y": round(ry, 1),
                          "angle": round(poses.wrap(a), 1), "behind": bool(behind), "flip": False})
            b = idle_book.get(d8)
            scover = hf.ring_cover(cell, lx, ly)
            grips.append({"part": "offhand", "anim": "attack", "dir": d8, "f": f, "x": round(lx, 1), "y": round(ly, 1),
                          "angle": round(b["angle"] if b else 0.0, 1),
                          "behind": bool(ly < BOOK_CHEST_Y or poses.behind_of(d8, "off", lvis, scover)),
                          "flip": bool(b["flip"]) if b else d8 >= 5})
    cj["grips"] = grips
    print("wizard attack: 8 painted dirs,", sum(x["anim"] == "attack" for x in grips), "grips")

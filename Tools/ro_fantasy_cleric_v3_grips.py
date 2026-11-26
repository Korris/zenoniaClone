"""Cleric V3 (Priest, Asteria 26/11): fists + star-staff grips of the RO Fantasy layered cleric (called from Tools/ro_fantasy_grips.py,
`fists cleric` / `grips cleric` of Tools/build-ro-fantasy-hero-game-assets.py).

Why its own module: the V3 Priest has BARE hands (no leather gloves: the generic detector of hero_layered_fists looks for brown leather),
an 8-frame walk (5 drawn directions, left dirs mirrored) and an 8-frame attack painted for ALL 8 directions (26/11: 8-row sheet, row = d8,
the staff stays in the painted right fist, art flipped facing left like idle / walk), and holds ONLY the star staff in the right
hand (the V3 design has no shield: no offhand entry, so the offhand layer stays empty whatever is equipped).

Fists: skin-coloured blobs (distance to the face skin colour of the same frame, opened by a 3 px disc so gold trim specks drop out), away
from the head, above the hem (legs), then FIX (read on the 4x grids docs/reference/ro-fantasy-heroes-ingame/fists-cleric-<anim>.png) wins.
Hands: rows 0-1 (down, down-right) R = screen-left fist; row 2 (right) R = the near fist; rows 3-4 (up-right, up) R = screen-right fist.
Grips (staff only, grip convention of hero_layered_grip_poses: 0 = head up, clockwise):
  idle: upright, tilted outward (rest pose); walk: the staff head stays where it is at idle for that direction while the fist swings
  (like the Wizard steady_staff); attack: STAFF_ATTACK[(row, f)] when listed, else along the arm (shoulder -> fist); last frame = idle angle.
  Left dirs (5-7): mirrored painted rows 3-1, the staff in the far (mirrored left) fist, art flipped, angle mirrored.
  Depth: behind the body facing up / up-right / up-left, when the fist is hidden, or when it would cover the face (after a small tilt).
"""
import json
import math
from importlib import import_module

import numpy as np
from scipy import ndimage

import hero_layered_fists as hf
import ro_fantasy_body_cut as bc

poses = import_module("hero_layered_grip_poses")
CW, CH = bc.CW, bc.CH
T, F = True, False
HEM_Y = 205
THIGH_Y, THIGH_DX = 172, 26
WALK_SWAY_MAX = 14  # walk: staff angle within this many degrees of the idle angle of its direction  # fists never sit below this cell row (thighs / knees between the boots are skin too)

# (anim, painted row, frame) -> {hand: [x, y, visible]}: fixes read on the 4x grids
FIX = {
    ("idle", 0, 0): {"R": [118, 158, T], "L": [202, 158, T]},
    ("idle", 1, 0): {"R": [127, 159, T], "L": [196, 161, T]},
    ("idle", 2, 0): {"R": [162, 160, T], "L": [172, 157, F]},
    ("idle", 3, 0): {"R": [190, 157, T], "L": [117, 156, T]},
    ("idle", 4, 0): {"R": [205, 158, T], "L": [116, 158, T]},
    # walk 8f (src/cleric-v3/body-cleric-walk-<dir>-8f-chatgpt-1): only the staff fist R matters
    ("walk", 0, 6): {"R": [114, 152, T]},
    ("walk", 4, 1): {"R": [203, 166, T]}, ("walk", 4, 3): {"R": [201, 150, T]},
    # attack 8f (src/cleric-v3/body-cleric-attack-<dir>-8f-chatgpt-1), read on 2-3x grids of the cut cells; L matters on rows 1-3 (staff fist of the mirrored left dirs)
    ("attack", 0, 0): {"R": [119, 158, T], "L": [201, 158, T]},
    ("attack", 0, 1): {"R": [123, 122, T], "L": [198, 142, T]},
    ("attack", 0, 2): {"R": [120, 78, T], "L": [191, 135, T]},
    ("attack", 0, 3): {"R": [145, 131, T], "L": [193, 126, T]},
    ("attack", 0, 4): {"R": [142, 126, T], "L": [193, 122, T]},
    ("attack", 0, 5): {"R": [129, 142, T], "L": [193, 135, T]},
    ("attack", 0, 6): {"R": [123, 142, T], "L": [194, 142, T]},
    ("attack", 0, 7): {"R": [119, 155, T], "L": [201, 155, T]},
    ("attack", 1, 0): {"R": [127, 158, T], "L": [193, 151, T]},
    ("attack", 1, 1): {"R": [125, 123, T], "L": [198, 146, T]},
    ("attack", 1, 2): {"R": [122, 89, T], "L": [188, 138, T]},
    ("attack", 1, 3): {"R": [151, 138, T], "L": [189, 133, T]},
    ("attack", 1, 4): {"R": [154, 139, T], "L": [191, 133, T]},
    ("attack", 1, 5): {"R": [134, 140, T], "L": [195, 138, T]},
    ("attack", 1, 6): {"R": [129, 144, T], "L": [195, 142, T]},
    ("attack", 1, 7): {"R": [125, 158, T], "L": [191, 155, T]},
    ("attack", 3, 0): {"R": [192, 162, T], "L": [116, 154, T]},
    ("attack", 3, 1): {"R": [192, 138, T], "L": [110, 156, T]},
    ("attack", 3, 2): {"R": [206, 86, T], "L": [108, 150, T]},
    ("attack", 3, 3): {"R": [218, 90, T], "L": [108, 154, T]},
    ("attack", 3, 4): {"R": [224, 82, T], "L": [108, 152, T]},
    ("attack", 3, 5): {"R": [218, 92, T], "L": [108, 152, T]},
    ("attack", 3, 6): {"R": [204, 120, T], "L": [110, 152, T]},
    ("attack", 3, 7): {"R": [192, 156, T], "L": [114, 154, T]},
    ("attack", 4, 0): {"R": [204, 156, T], "L": [118, 156, T]},
    ("attack", 4, 1): {"R": [204, 116, T], "L": [116, 172, T]},
    ("attack", 4, 2): {"R": [208, 82, T], "L": [116, 148, T]},
    ("attack", 4, 3): {"R": [208, 60, T], "L": [116, 140, T]},
    ("attack", 4, 4): {"R": [210, 56, T], "L": [116, 128, T]},
    ("attack", 4, 5): {"R": [208, 86, T], "L": [114, 144, T]},
    ("attack", 4, 6): {"R": [204, 120, T], "L": [112, 148, T]},
    ("attack", 4, 7): {"R": [204, 156, T], "L": [116, 156, T]},
    ("attack", 2, 0): {"R": [145, 158, T], "L": [150, 155, F]},
    ("attack", 2, 1): {"R": [120, 124, T], "L": [182, 142, T]},
    ("attack", 2, 2): {"R": [122, 91, T], "L": [186, 135, T]},
    ("attack", 2, 3): {"R": [219, 131, T], "L": [180, 154, T]},
    ("attack", 2, 4): {"R": [232, 130, T], "L": [190, 152, T]},
    ("attack", 2, 5): {"R": [192, 132, T], "L": [188, 152, F]},
    ("attack", 2, 6): {"R": [132, 147, T], "L": [182, 147, T]},
    ("attack", 2, 7): {"R": [142, 165, T], "L": [150, 160, F]},
    # attack 8f painted LEFT dirs (26/11, 8-row attack sheet: row = d8): R = the character's right (staff) fist, read on 2x grids of the cut cells
    # up-left (src/cleric-v3/body-cleric-attack-up-left-8f-chatgpt-3): screen-right arm thrusts up beside the head, screen-left fist low
    ("attack", 5, 0): {"R": [190, 163, T], "L": [115, 159, T]},
    ("attack", 5, 1): {"R": [196, 98, T], "L": [118, 161, T]},
    ("attack", 5, 2): {"R": [198, 76, T], "L": [120, 157, T]},
    ("attack", 5, 3): {"R": [208, 68, T], "L": [117, 155, T]},
    ("attack", 5, 4): {"R": [210, 62, T], "L": [115, 155, T]},
    ("attack", 5, 5): {"R": [200, 80, T], "L": [121, 155, T]},
    ("attack", 5, 6): {"R": [192, 100, T], "L": [117, 158, T]},
    ("attack", 5, 7): {"R": [196, 158, F], "L": [120, 157, T]},
    # left (-chatgpt-1): the far (right) arm strikes forward-left in front of the chest, near (left) fist at the hip
    ("attack", 6, 0): {"R": [141, 154, T], "L": [154, 159, T]},
    ("attack", 6, 1): {"R": [133, 122, T], "L": [170, 158, T]},
    ("attack", 6, 2): {"R": [115, 78, T], "L": [170, 158, T]},
    ("attack", 6, 3): {"R": [91, 112, T], "L": [165, 158, T]},
    ("attack", 6, 4): {"R": [89, 115, T], "L": [181, 160, T]},
    ("attack", 6, 5): {"R": [103, 116, T], "L": [170, 158, T]},
    ("attack", 6, 6): {"R": [135, 120, T], "L": [158, 158, T]},
    ("attack", 6, 7): {"R": [148, 157, F], "L": [152, 155, T]},
    # down-left (-chatgpt-1): screen-left (right) arm strikes, screen-right fist at the hip
    ("attack", 7, 0): {"R": [127, 155, T], "L": [190, 156, T]},
    ("attack", 7, 1): {"R": [125, 125, T], "L": [198, 162, T]},
    ("attack", 7, 2): {"R": [115, 85, T], "L": [195, 155, T]},
    ("attack", 7, 3): {"R": [115, 131, T], "L": [209, 155, T]},
    ("attack", 7, 4): {"R": [112, 135, T], "L": [216, 152, T]},
    ("attack", 7, 5): {"R": [120, 132, T], "L": [214, 152, T]},
    ("attack", 7, 6): {"R": [132, 122, T], "L": [200, 156, T]},
    ("attack", 7, 7): {"R": [126, 156, T], "L": [190, 155, T]},
}
# (painted row, frame) -> staff angle for the attack (read on cleric-attack-8dir.png); others: arm line
STAFF_ATTACK = {}
# Attack frames whose arm line (shoulder -> fist) points further down than STAFF_DOWN_MAX deg from "head up" (fist at the chest after the
# punch, wind-up below the shoulder) would aim the star head at the feet (seen 26/11 on every dir of the 8f attack): they use the thrust
# angle of their Dir8 instead: front views upright leaning outward on the staff side, side views forward, back views up-forward.
STAFF_DOWN_MAX = 100
STAFF_THRUST = {0: -20, 1: -15, 2: 80, 3: 40, 4: 10, 5: -40, 6: -80, 7: -25}


def skin_blobs(cell, anchor):
    a = np.asarray(cell).astype(int)
    head = hf.head_mask(cell)
    base = hf.skin_of(cell) & head
    if base.sum() < 30:
        return []
    med = np.median(a[base][:, :3], 0)
    d = np.abs(a[..., :3] - med)
    dist = d[..., 0] * 0.6 + d[..., 1] + d[..., 2] * 1.2
    m = (a[..., 3] > 128) & (dist < 72) & (a[..., 0] > a[..., 2] + 25) & (a[..., 1] > a[..., 2])
    m = ndimage.binary_fill_holes(ndimage.binary_closing(m, structure=hf.disc(2)))  # finger lines inside a fist
    m &= ~ndimage.binary_dilation(head, iterations=2)
    m = ndimage.binary_opening(m, structure=hf.disc(2))
    lab, n = ndimage.label(m)
    out = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        ys, xs = np.nonzero(lab == i)
        cy, cx = float(ys.mean()), float(xs.mean())
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if area < 40 or cy > HEM_Y or h > 2.2 * w:
            continue
        hx, chin = anchor["x"], anchor["y"] + 1.12 * anchor["w"]
        if abs(cx - hx) < 0.62 * anchor["w"] and cy < chin + 16:  # ears / neck / jaw under the head mask
            continue
        if cy > THIGH_Y and abs(cx - hx) < THIGH_DX:  # thighs between the coat flaps (front views)
            continue
        out.append((area, cx, cy))
    return sorted(out, reverse=True)[:2]


def assign(blobs, row, head_x, prev):
    """{R, L: [x, y, visible]} from up to 2 blobs (missing hand: previous frame's spot, hidden)."""
    pts = sorted([(x, y) for _, x, y in blobs])
    t = {}
    if len(pts) == 2:
        left, right = pts
        if row in (0, 1, 7):  # 7 = painted down-left (8-dir attack): the right hand is on screen-left like down / down-right
            t = {"R": left, "L": right}
        elif row == 6:  # painted left: the right hand is the FAR fist (further from the head column)
            t = {"R": max(pts, key=lambda p: abs(p[0] - head_x)), "L": min(pts, key=lambda p: abs(p[0] - head_x))}
        elif row == 2:  # side view: the near (right) fist is drawn over the body, nearer the centre column
            t = {"R": min(pts, key=lambda p: abs(p[0] - head_x)), "L": max(pts, key=lambda p: abs(p[0] - head_x))}
        else:
            t = {"R": right, "L": left}
        return {k: [round(v[0], 1), round(v[1], 1), True] for k, v in t.items()}
    out = {}
    if len(pts) == 1:
        x, y = pts[0]
        if row == 2:
            hand = "R"
        elif row == 6:
            hand = "L"
        elif row in (0, 1, 7):
            hand = "R" if x < head_x else "L"
        else:
            hand = "R" if x > head_x else "L"
        out[hand] = [round(x, 1), round(y, 1), True]
    for k in ("R", "L"):
        if k not in out:
            p = (prev or {}).get(k)
            out[k] = [p[0], p[1], False] if p else [float(head_x + (-40 if (k == "R") == (row < 3 or row == 7) else 40)), 175.0, False]
    return out


def skull_anchor(cell):
    """head_anchor of build-hero-layered-sheets, but a bare fist raised beside / over the head (skin like the face) is cut off the
    skull blob first (opening with a 9 px disc: the fist joins the head by a thin bridge), so the hair no longer slides toward it."""
    st = bc.sheets_tool
    sk = st.skull_mask(cell)
    op = ndimage.binary_opening(sk, structure=hf.disc(9))
    lab, n = ndimage.label(op)
    if n:
        big = 1 + int(np.argmax(ndimage.sum(op, lab, range(1, n + 1))))
        sk = sk & ndimage.binary_dilation(lab == big, iterations=9)
    al = ndimage.binary_dilation(sk, iterations=2)
    ys = np.nonzero(al.any(1))[0]
    top, h = ys[0], (ys[-1] - ys[0]) * 2.2
    best, bx = 0, CW / 2
    for y in range(top + int(h * 0.06), top + int(h * 0.3)):
        xs = np.nonzero(al[y])[0]
        if len(xs):
            for run in np.split(xs, np.nonzero(np.diff(xs) > 2)[0] + 1):
                if len(run) > best:
                    best, bx = len(run), (run[0] + run[-1]) / 2
    return round(float(bx), 1), int(top), best


def fix_heads(body):
    """Rewrite the head anchors of body.json (written by `write`) with skull_anchor: per frame x / top, row-median width."""
    path = bc.ROOT / "Assets/Resources/Data/hero-layered/ro-fantasy/cleric/body.json"
    bj = json.loads(path.read_text(encoding="utf-8"))
    anims, _, cells = body("cleric")
    heads, moved = [], 0
    for anim, n in anims:
        for r in range(len(cells[anim])):
            got = [skull_anchor(c) for c in cells[anim][r]]
            w = float(np.median([g[2] for g in got]))
            for f, (x, top, _) in enumerate(got):
                heads.append({"anim": anim, "dir": r, "f": f, "x": x, "y": top, "w": w, "angle": 0})
    old = {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}
    for h in heads:
        o = old.get((h["anim"], h["dir"], h["f"]))
        if o and abs(o["x"] - h["x"]) + abs(o["y"] - h["y"]) > 2:
            moved += 1
    bj["head"] = heads
    path.write_text(json.dumps(bj), encoding="utf-8")
    print(f"cleric head anchors: {moved} frames moved (fist cut off the skull)")


def table_of(body):
    """(table, anims, heads, cells) like ro_fantasy_grips.table_of, for the cleric."""
    fix_heads(body)
    anims, heads, cells = body("cleric")
    out = {}
    for anim, n in anims:
        rows = []
        for r in range(len(cells[anim])):
            row, prev = [], None
            for f in range(n):
                t = assign(skin_blobs(cells[anim][r][f], heads[(anim, r, f)]), r, heads[(anim, r, f)]["x"], prev)
                t.update({k: list(v) for k, v in FIX.get((anim, r, f), {}).items()})
                row.append({k: [round(float(v[0]), 1), round(float(v[1]), 1), bool(v[2])] for k, v in t.items()})
                prev = row[-1]
            if anim == "walk" and n == 8:
                walk_far_fists(row, r, heads, anim)
            rows.append(row)
        out[anim] = rows
    return out, anims, heads, cells


def walk_far_fists(row, r, heads, anim):
    """Walk: the left fist (the staff fist of the mirrored left dirs) = the right fist half a cycle later, mirrored about the head column
    (rows 0, 1, 3, 4); side view (row 2): the far fist is behind the body, = that fist a little further back, hidden. The skin detector misses
    many left fists (shaded sleeve cuff) and takes coat patches; a symmetric walk cycle gives a steadier spot. FIX still wins."""
    n = len(row)
    rs = [list(row[f]["R"]) for f in range(n)]
    for f in range(n):
        if "L" in FIX.get((anim, r, f), {}):
            continue
        rx, ry, _ = rs[(f + n // 2) % n]
        idle = FIX[("idle", r, 0)]
        hx = (idle["R"][0] + idle["L"][0]) / 2  # body column between the idle fists
        row[f]["L"] = [round(rx + 8, 1), round(ry, 1), False] if r == 2 else [round(2 * hx - rx, 1), round(ry, 1), True]


def staff_image():
    root = bc.ROOT
    img, gx, gy = poses.weapon_image(root, "cleric-staff", "blade")  # "blade": keep the grip of weapons.json (not the generic 74 %)
    return img, gx, gy


def grips(table, heads, cells):
    """(weapons, grips) for class.json: the star staff in the right hand, every anim x 8 dirs."""
    img, gx, gy = staff_image()
    weapons = [{"part": "weapon", "sheet": "Sprites/hero-layered/weapons/cleric-staff", "gx": gx, "gy": gy, "overHand": False}]
    out = []
    idle_head = {}  # d8 -> staff head point at idle (walk keeps it)
    for anim in ["idle"] + [a for a in table if a != "idle"]:
        rows = table[anim]
        n = len(rows[0])
        for f in range(n):
            for d8 in range(8):
                # 8-row sheet (attack painted for all 8 dirs, 26/11): row = d8, never mirrored, the staff in the painted right fist;
                # the staff art is still flipped facing left (same look as the left idle / walk). 5-row sheets: left dirs mirror rows 3-1.
                all8 = len(rows) == 8
                prow = poses.PAINTED[d8]
                row = d8 if all8 else prow
                mirror = d8 >= 5 and not all8
                flip = d8 >= 5
                h = heads[(anim, row, f)]
                cell = cells[anim][row][f]
                hand = "L" if mirror else "R"
                fx, fy, vis = table[anim][row][f][hand]
                if mirror:  # the far fist of the mirrored art holds the staff: rest pose of that fist, then mirrored
                    rest = poses.lead_angle("staff", "idle", 0, (fx, fy), h, prow, hand)
                else:
                    rest = poses.lead_angle("staff", "idle", 0, (fx, fy), h, prow, hand)
                x = CW - fx if mirror else fx
                if anim == "idle":
                    ang = -rest if mirror else rest
                elif anim == "walk":
                    hx, hy, ia = idle_head[d8]
                    ang = math.degrees(math.atan2(hx - x, fy - hy))
                    lim = 4 if row == 2 else WALK_SWAY_MAX  # side view: the staff moves with the fist, nearly the idle angle
                    ang = ia + max(-lim, min(lim, poses.wrap(ang - ia)))  # side view: no pole-vaulting staff
                else:
                    a = STAFF_ATTACK.get((row, f))
                    if f in (0, n - 1):
                        ang = idle_head[d8][2]  # guard frames (start / end) = the idle angle of that dir: no pop around the attack
                    elif a is not None:
                        ang = -a if mirror else a
                    elif all8 and d8 >= 5:  # painted left row: arm line on the mirrored geometry (right hand = left hand of the twin)
                        ang = -poses.arm_angle((CW - fx, fy), dict(h, x=CW - h["x"]), prow, "L")
                    else:
                        lf = table[anim][row][f]["R"]
                        a = poses.arm_angle((lf[0], lf[1]), h, prow, "R")
                        ang = -a if mirror else a
                    if (row, f) not in STAFF_ATTACK and 0 < f < n - 1 and abs(poses.wrap(ang)) > STAFF_DOWN_MAX:
                        ang = STAFF_THRUST[d8]  # arm hanging / fist pulled in: the star head would point at the feet -> thrust pose
                ang = poses.keep_tip_above_ground(ang, fy, gy, 1 if math.sin(math.radians(ang)) >= 0 else -1, ground=226)
                cover = hf.ring_cover(cell, fx, fy)
                behind = poses.behind_of(d8, "main", vis, cover)
                if not behind and anim != "walk":  # walk: clamped around the idle pose, idle depth (set below)
                    zone = poses.face_zone(cell, mirror)
                    hx0 = CW - h["x"] if mirror else h["x"]
                    a0 = ang
                    ang, behind, _ = poses.clear_face(img, gx, gy, x, fy, ang, flip, zone, hx0, 5 if prow == 2 else 3)
                    if behind and vis and prow in (0, 1, 2):  # a visible front fist: keep the staff in it, leaning outward (away from the face)
                        out_s = -1 if x < hx0 else 1
                        ang = min((a0 + out_s * k for k in range(0, 40, 3)),
                                  key=lambda a: poses.overlap(img, gx, gy, x, fy, a, flip, zone) + 0.5 * abs(a - a0))
                        behind = False
                if anim == "idle":
                    a = math.radians(ang)
                    idle_head[d8] = (x + gy * math.sin(a), fy - gy * math.cos(a), ang)
                if anim == "walk" and row == 2:  # side walk, fist swung back: the star head would sit on the face -> behind the head
                    hx0 = CW - h["x"] if mirror else h["x"]
                    behind = poses.overlap(img, gx, gy, x, fy, ang, mirror, poses.face_zone(cell, mirror)) > 4 * poses.FACE_PX
                out.append({"part": "weapon", "anim": anim, "dir": d8, "f": f, "x": round(x, 1), "y": round(fy, 1),
                            "angle": round(poses.wrap(ang), 1), "behind": bool(behind), "flip": bool(flip)})
    # walk keeps the idle depth of its direction (no flicker between in front / behind from step to step)
    idle_behind = {g["dir"]: g["behind"] for g in out if g["anim"] == "idle"}
    for g in out:
        if g["anim"] == "walk" and poses.PAINTED[g["dir"]] != 2:
            g["behind"] = idle_behind[g["dir"]]
        elif g["anim"] == "walk" and g["dir"] == 6:
            g["behind"] = True  # facing left the staff is in the far fist
    return weapons, out


def cmd_grips(body, hand_sheet, fists_json):
    table = json.loads(fists_json.read_text(encoding="utf-8"))["anims"]
    anims, heads, cells = body("cleric")
    hand_sheet(table, anims, cells, "cleric")
    path = bc.ROOT / "Assets/Resources/Data/hero-layered/ro-fantasy/cleric/class.json"
    cj = json.loads(path.read_text(encoding="utf-8"))
    cj["weapons"], cj["grips"] = grips(table, heads, cells)
    path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
    print("cleric V3:", len(cj["grips"]), "staff grips, no offhand")

"""Weapon grips of the layered heroes from the fist table (Tools/build-hero-layered-grips.py write). Holding rules: docs/sprite-rules.md.

Per class: main weapon in the RIGHT fist, offhand in the LEFT fist (WEAPONS). Angles are clockwise from upright on the y-down cell
(0 = blade / tip up, 90 = right, 180 = down), like HeroLayeredView. Computed on the 5 painted rows; the 3 left dirs mirror a painted row,
where the character's right hand is the FAR hand, so the main weapon goes on the mirrored LEFT fist (drawn behind the body unless that
fist lies visibly over the torso) and the offhand on the mirrored right fist; the art is flipped (`flip`) and attack angles are the mirrored
lead angles of the right hand, so the blade still leads the swing.
  idle / walk: blade (sword, dagger) low, pointing down-outward 35 deg (+ half the arm swing); staff upright, tilted 8 deg outward, tip
               above the head; pistol low, muzzle down-forward 25 deg; shield / book upright on the left fist.
  attack1 / attack3 frames 0-2: blade / staff / pistol along the arm (shoulder -> fist), so the tip leads the slash / the overhead smash;
               attack2 frames 0-2: points in the facing direction (thrust). Frame 3 (recover) = the idle pose. Shields / book stay upright.
No weapon in front of the face: a front weapon overlapping the head (+ hair margin) turns outward in 6 deg steps (max 48; staffs max 18,
they stay nearly upright), else goes behind the body (the staff head then shows beside the head).
"""
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

import hero_layered_fists as hf

CW, CH = 320, 256
CLASS_GENDER = {"warrior": "male", "wizard": "male", "mechanic": "male", "assassin": "female", "cleric": "female"}
# class -> ((main item, kind), (offhand item, kind))
WEAPONS = {"warrior": (("sword", "blade"), ("warrior-shield", "shield")),
           "wizard": (("wizard-staff", "staff"), ("wizard-book", "book")),
           "mechanic": (("pistol", "pistol"), ("pistol", "pistol")),
           "assassin": (("dagger", "blade"), ("dagger", "blade")),
           "cleric": (("cleric-staff", "staff"), ("cleric-shield", "shield"))}
STAFF_GRIP = 0.74  # staffs held at 74 % of their height from the top: the tip ends ~15 px above the skull at idle
BOOK_GRIP = 0.8    # the left fist holds the book's lower edge
FACING = [180, 135, 90, 45, 0]  # painted row -> facing angle
SHOULDER = [(-0.36, 0.36), (-0.28, 0.30), (-0.05, -0.05), (0.28, -0.28), (0.36, -0.36)]  # (R, L) shoulder x offset in skull widths
PAINTED = {0: 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 3, 6: 2, 7: 1}
FACE_PX = 20  # front-weapon pixels allowed over the head + hair margin


def wrap(a):
    return (a + 180) % 360 - 180


def clamp(v, lo, hi):
    return max(lo, min(hi, v))


def arm_angle(fist, h, row, hand):
    sx = h["x"] + SHOULDER[row][0 if hand == "R" else 1] * h["w"]
    sy = h["y"] + 1.08 * h["w"]
    return math.degrees(math.atan2(fist[0] - sx, -(fist[1] - sy)))


def rest_angle(kind, fist, h, row, arm, item=""):
    """Idle / walk / recover pose of a weapon in a fist (painted row geometry)."""
    o = 1 if row == 2 or fist[0] >= h["x"] else -1  # outward (side view: forward)
    sw = wrap(arm - 180)
    if kind == "blade":
        out = 58 if item == "sword" else 35  # the 150 px sword at 35 deg would stab ~25 px under the feet
        return 180 - o * out + clamp(0.5 * sw, -25, 25)
    if kind == "staff":
        return o * (20 if row == 2 else 8) + clamp(0.25 * sw, -10, 10)  # side view: leaning forward, ahead of the face
    if kind == "pistol":
        return 180 - o * 25 + clamp(0.4 * sw, -20, 20)
    return 0.0


def lead_angle(kind, anim, f, fist, h, row, hand, item=""):
    """Angle of a weapon in fist `hand` on a painted row (None = use the rest pose)."""
    arm = arm_angle(fist, h, row, hand)
    if kind in ("shield", "book") or f == 3 or anim in ("idle", "walk"):
        return rest_angle(kind, fist, h, row, arm, item)
    if anim == "attack2":
        if hand == "R":
            return FACING[row]
        return rest_angle(kind, fist, h, row, arm, item)
    if anim == "attack1" and hand == "L":
        return rest_angle(kind, fist, h, row, arm, item)
    return arm


def keep_tip_above_ground(ang, fy, length, side, ground=250):
    """Turn a downward blade / staff towards horizontal (same side) until its tip ends above the ground line (cell row `ground`)."""
    a = wrap(ang)
    lim = (fy - ground) / max(1.0, length)
    if math.cos(math.radians(a)) >= lim or lim <= -1:
        return a
    edge = math.degrees(math.acos(max(-1.0, min(1.0, lim))))
    if abs(a) > 175:  # straight down: turn to the fist's side
        return edge * side
    return edge if a >= 0 else -edge


def weapon_image(root, item, kind):
    img = Image.open(root / f"Assets/Resources/Sprites/hero-layered/weapons/{item}.png").convert("RGBA")
    gx, gy = json.loads((root / "Assets/Resources/Data/hero-layered/weapons.json").read_text(encoding="utf-8"))["grips"][item]
    if kind == "staff":
        gy = round(img.height * STAFF_GRIP, 1)
        gx = float(np.nonzero(np.asarray(img)[int(gy), :, 3] > 100)[0].mean())
    if kind == "book":
        gy = round(img.height * BOOK_GRIP, 1)
    return img, gx, gy


def placed(img, gx, gy, x, y, angle, flip, size=(CW, CH)):
    """The weapon as drawn by HeroLayeredView: pivot (gx, gy) on cell point (x, y), flipped about the pivot, turned clockwise by angle."""
    if flip:
        img, gx = img.transpose(Image.FLIP_LEFT_RIGHT), img.width - gx
    r = int(math.ceil(max(math.hypot(px - gx, py - gy) for px in (0, img.width) for py in (0, img.height)))) + 2
    pad = Image.new("RGBA", (2 * r, 2 * r))
    pad.alpha_composite(img, (int(round(r - gx)), int(round(r - gy))))
    rot = pad.rotate(-angle, resample=Image.BICUBIC, center=(r, r))
    out = Image.new("RGBA", (size[0] + 4 * r, size[1] + 4 * r))
    out.alpha_composite(rot, (int(round(x)) + r, int(round(y)) + r))
    return out.crop((2 * r, 2 * r, 2 * r + size[0], 2 * r + size[1]))


def face_zone(cell, mirror):
    m = ndimage.binary_dilation(hf.head_mask(cell), iterations=10)  # head + hair / hat margin
    return m[:, ::-1] if mirror else m


def overlap(img, gx, gy, x, y, angle, flip, zone):
    a = np.asarray(placed(img, gx, gy, x, y, angle, flip))[..., 3] > 80
    return int((a & zone).sum())


def clear_face(img, gx, gy, x, y, angle, flip, zone, head_x, steps=8):
    """(angle, behind_forced, px over the face) after turning the weapon away from the head."""
    n = overlap(img, gx, gy, x, y, angle, flip, zone)
    if n <= FACE_PX:
        return angle, False, n
    best = (n, angle)
    for step in range(1, steps + 1):
        for s in (1, -1):
            a = angle + s * 6 * step
            k = overlap(img, gx, gy, x, y, a, flip, zone)
            if k < best[0]:
                best = (k, a)
        if best[0] <= FACE_PX:
            return best[1], False, best[0]
    return angle, True, n


def behind_of(d8, role, vis, cover):
    if not vis:
        return True
    if role == "main":
        if d8 == 4:
            return True
        if d8 in (5, 6):
            return cover < 0.55  # far hand: behind, unless the fist is painted over the torso (else an empty fist + a blade from nowhere)
        return False
    if d8 in (2, 3, 4):
        return True
    if d8 == 5:
        return cover >= 0.5
    return False


def class_grips(root, cls, table, heads, cells):
    """(weapons entries, grips list) for class.json."""
    (mi, mk), (oi, ok) = WEAPONS[cls]
    items = {"weapon": mi, "offhand": oi}
    imgs = {"weapon": weapon_image(root, mi, mk), "offhand": weapon_image(root, oi, ok)}
    kinds = {"weapon": mk, "offhand": ok}
    weapons = [{"part": p, "sheet": f"Sprites/hero-layered/weapons/{it}", "gx": imgs[p][1], "gy": imgs[p][2],
                "overHand": kinds[p] == "shield"} for p, it in (("weapon", mi), ("offhand", oi))]
    grips = []
    for anim, rows in table.items():
        for f in range(len(rows[0])):
            for d8 in range(8):
                row = PAINTED[d8]
                mirror = d8 >= 5
                h = heads[(anim, row, f)]
                cell = cells[anim][row][f]
                zone = face_zone(cell, mirror)
                for part in ("weapon", "offhand"):
                    kind = kinds[part]
                    role = "main" if part == "weapon" else "off"
                    hand = ("R" if role == "main" else "L") if not mirror else ("L" if role == "main" else "R")
                    fx, fy, vis = table[anim][row][f][hand]
                    cover = hf.ring_cover(cell, fx, fy)
                    if not mirror:
                        ang = lead_angle(kind, anim, f, (fx, fy), h, row, hand, items[part])
                    else:
                        # painted geometry of the hand that does the motion: the right hand's lead for the main weapon (attacks),
                        # the rest pose of the fist actually holding it otherwise; then mirrored
                        lead_hand = "R" if role == "main" else "L"
                        rest = lead_angle(kind, "idle", 0, (fx, fy), h, row, hand, items[part])
                        if anim in ("idle", "walk") or f == 3 or kind in ("shield", "book"):
                            ang = rest
                        else:
                            lf = table[anim][row][f][lead_hand]
                            ang = lead_angle(kind, anim, f, (lf[0], lf[1]), h, row, lead_hand, items[part])
                            # held in the far fist: a lead angle that crosses the body (tip towards the head side) -> rest pose
                            if math.sin(math.radians(ang)) * (fx - h["x"]) < -0.5 * abs(fx - h["x"]) and abs(fx - h["x"]) > 8:
                                ang = rest
                        ang = -ang
                    x = CW - fx if mirror else fx
                    img, gx, gy = imgs[part]
                    if kind in ("blade", "staff"):
                        ang = keep_tip_above_ground(ang, fy, gy, 1 if x >= (CW - h["x"] if mirror else h["x"]) else -1)
                    behind = behind_of(d8, role, vis, cover)
                    lf, rf = table[anim][row][f]["L"], table[anim][row][f]["R"]
                    if kind in ("shield", "book") and anim == "attack3" and f < 3 and math.hypot(lf[0] - rf[0], lf[1] - rf[1]) < 45:
                        behind = True  # two-hand smash: the shield / book would sit on the joined fists over the head / the staff
                    if not behind:
                        hx = CW - h["x"] if mirror else h["x"]
                        ang, forced, _ = clear_face(img, gx, gy, x, fy, ang, mirror, zone, hx, (5 if PAINTED[d8] == 2 else 3) if kind == "staff" else 8)
                        behind = forced
                    grips.append({"part": part, "anim": anim, "dir": d8, "f": f, "x": round(x, 1), "y": round(fy, 1),
                                  "angle": round(wrap(ang), 1), "behind": bool(behind), "flip": bool(mirror)})
    return weapons, grips

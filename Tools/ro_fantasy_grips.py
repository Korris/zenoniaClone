"""RO Fantasy heroes: fists -> hand layer + weapon grips (reuses Tools/hero_layered_fists.py and Tools/hero_layered_grip_poses.py of the
common layered bodies; holding rules: docs/sprite-rules.md "Quy tắc tư thế cầm").

cmd_fists(cls): detect the fists on every painted frame (brown leather gloves; Assassin's dark gloves and Cleric's pale gloves are not
leather, so their fists come from OVERRIDES read on the marked sheets), write ArtLibrary/ro-fantasy-heroes/sheets/<class>-fists.json and
docs/reference/ro-fantasy-heroes-ingame/fists-<class>-<anim>.png (R red = right hand / weapon, L blue = left hand / offhand, hollow = hidden).
cmd_grips(cls): hand layer body/hand/<anim>.png (a disc around each visible fist, cut from the body) and grips of the class weapons.
"""
import json
import math
from importlib import import_module

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import hero_layered_fists as hf
import ro_fantasy_body_cut as bc
import ro_fantasy_motion_fists as mf

ROOT = bc.ROOT
SPR = ROOT / "Assets/Resources/Sprites/hero-layered/ro-fantasy"
DATA = ROOT / "Assets/Resources/Data/hero-layered/ro-fantasy"
FISTS = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/sheets"
REF = ROOT / "docs/reference/ro-fantasy-heroes-ingame"
CW, CH, PAINTED = bc.CW, bc.CH, bc.PAINTED
HAND_R = 9  # hand layer disc radius (cell px) around a fist

# (class, anim, painted row, frame) -> {hand: [x, y, visible]}, read on the marked sheets (2x)
T, F = True, False
OVERRIDES = {
    # idle: down / down-right R = screen-left fist; right: R = the near fist, the far one hidden at the hip; up-right / up: R = screen-right
    ("warrior", "idle", 0, 0): {"R": [112, 175, T], "L": [206, 175, T]},
    ("warrior", "idle", 1, 0): {"R": [125, 178, T], "L": [191, 175, T]},
    ("warrior", "idle", 2, 0): {"R": [151, 178, T], "L": [168, 172, F]},
    ("warrior", "idle", 3, 0): {"R": [189, 178, T], "L": [121, 178, T]},
    ("warrior", "idle", 4, 0): {"R": [202, 178, T], "L": [118, 178, T]},
    ("wizard", "idle", 0, 0): {"R": [112, 177, T], "L": [205, 177, T]},
    ("wizard", "idle", 1, 0): {"R": [122, 179, T], "L": [194, 175, T]},
    ("wizard", "idle", 2, 0): {"R": [150, 179, T], "L": [168, 174, F]},
    ("wizard", "idle", 3, 0): {"R": [196, 177, T], "L": [123, 178, T]},
    ("wizard", "idle", 4, 0): {"R": [206, 178, T], "L": [113, 179, T]},
    ("mechanic", "idle", 0, 0): {"R": [117, 180, T], "L": [205, 180, T]},
    ("mechanic", "idle", 1, 0): {"R": [122, 181, T], "L": [195, 180, T]},
    ("mechanic", "idle", 2, 0): {"R": [148, 183, T], "L": [166, 178, F]},
    ("mechanic", "idle", 3, 0): {"R": [195, 183, T], "L": [121, 184, T]},
    ("mechanic", "idle", 4, 0): {"R": [207, 183, T], "L": [113, 183, T]},
    ("assassin", "idle", 0, 0): {"R": [115, 177, T], "L": [200, 177, T]},
    ("assassin", "idle", 1, 0): {"R": [127, 180, T], "L": [192, 177, T]},
    ("assassin", "idle", 2, 0): {"R": [157, 180, T], "L": [172, 176, F]},
    ("assassin", "idle", 3, 0): {"R": [193, 174, T], "L": [128, 178, F]},  # the left fist hides under the scarf
    ("assassin", "idle", 4, 0): {"R": [200, 177, T], "L": [122, 176, F]},
    ("cleric", "idle", 0, 0): {"R": [113, 172, T], "L": [204, 172, T]},
    ("cleric", "idle", 1, 0): {"R": [122, 178, T], "L": [196, 174, T]},
    ("cleric", "idle", 2, 0): {"R": [151, 177, T], "L": [168, 174, F]},
    ("cleric", "idle", 3, 0): {"R": [193, 175, T], "L": [126, 174, F]},
    ("cleric", "idle", 4, 0): {"R": [205, 177, T], "L": [113, 177, T]},
}
OVERRIDES.update(mf.overrides())  # walk / attack fists of Warrior, Wizard, Mechanic
STAFF_SIDE_AHEAD = 22  # Wizard walk right / left: staff head this far ahead of its idle spot
CLERIC_STAFF_AHEAD, CLERIC_STAFF_LIFT = 36, -8  # Cleric walk right / left: staff head further ahead (and lower) than idle, wings clear of the chin
CLERIC_STAFF_ATTACK = {(0, 2): -110, (1, 1): -45}  # (painted row, frame): staff angle, read on the 8-dir sheet (mirrored on the left)
STAFF_HEAD_FLOOR = 226  # staff point (grip -> art top) floor: the crystal head is ~25 px wide, its rim then stays above the feet (244)
PUNCH_Y = 160  # Mechanic attack: a fist above this row is the shooting hand, its pistol points along the arm
SWORD_TIP_Y = 240  # Warrior idle / walk facing down / down-right: sword points down-outward, tip just above the feet line (244)


def body(cls):
    bj = json.loads((DATA / cls / "body.json").read_text(encoding="utf-8"))
    heads = {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}
    anims = [(a["name"], a["frames"]) for a in bj["anims"]]
    cells = {}
    for anim, n in anims:
        sh = Image.open(SPR / cls / "body" / f"{anim}.png").convert("RGBA")
        cells[anim] = [[sh.crop((f * CW, r * CH, (f + 1) * CW, (r + 1) * CH)) for f in range(n)] for r in range(PAINTED)]
    return anims, heads, cells


def table_of(cls):
    anims, heads, cells = body(cls)
    out = {}
    for anim, n in anims:
        rows = []
        for r in range(PAINTED):
            row, prev = [], None
            for f in range(n):
                t = hf.assign(hf.fists(cells[anim][r][f], None), r, heads[(anim, r, f)]["x"], prev)
                t.update({k: list(v) for k, v in OVERRIDES.get((cls, anim, r, f), {}).items()})
                row.append({k: [round(float(v[0]), 1), round(float(v[1]), 1), bool(v[2])] for k, v in t.items()})
                prev = t
            rows.append(row)
        out[anim] = rows
    return out, anims, heads, cells


def cmd_fists(cls):
    table, anims, _, cells = table_of(cls)
    FISTS.mkdir(parents=True, exist_ok=True)
    (FISTS / f"{cls}-fists.json").write_text(json.dumps({"note": "Fists per painted row / frame (Tools/ro_fantasy_grips.py): [x, y, visible], cell px.",
                                                          "anims": table}, indent=0), encoding="utf-8")
    REF.mkdir(parents=True, exist_ok=True)
    X0, X1, Y0, Y1, S = 70, 250, 90, 250, 4  # the arms' area, 4x, a numbered grid every 10 cell px (read fists on it, not on a shrunk view)
    cw, ch = (X1 - X0) * S, (Y1 - Y0) * S
    for anim, n in anims:
        img = Image.new("RGBA", (n * cw, PAINTED * ch), (200, 210, 190, 255))
        d = ImageDraw.Draw(img)
        for r in range(PAINTED):
            for f in range(n):
                ox, oy = f * cw, r * ch
                img.alpha_composite(cells[anim][r][f].crop((X0, Y0, X1, Y1)).resize((cw, ch), Image.NEAREST), (ox, oy))
                for g in range(X0, X1, 10):
                    d.line((ox + (g - X0) * S, oy, ox + (g - X0) * S, oy + ch), fill=(0, 0, 0, 160 if g % 50 == 0 else 60))
                    d.text((ox + (g - X0) * S + 2, oy + 2), str(g), fill=(200, 0, 0))
                for g in range(Y0, Y1, 10):
                    d.line((ox, oy + (g - Y0) * S, ox + cw, oy + (g - Y0) * S), fill=(0, 0, 0, 160 if g % 50 == 0 else 60))
                    d.text((ox + 2, oy + (g - Y0) * S + 2), str(g), fill=(0, 0, 200))
                for k, col in (("R", (230, 0, 0)), ("L", (0, 60, 255))):
                    x, y, vis = table[anim][r][f][k]
                    X, Y = ox + (x - X0) * S, oy + (y - Y0) * S
                    d.ellipse((X - 8, Y - 8, X + 8, Y + 8), outline=col, fill=col if vis else None, width=3)
                    d.text((X + 10, Y - 6 + (12 if k == "L" else 0)), f"{k}{int(x)},{int(y)}", fill=col)
                d.text((ox + 40, oy + 16), f"r{r} f{f}", fill=(0, 0, 0))
                d.rectangle((ox, oy, ox + cw - 1, oy + ch - 1), outline=(255, 0, 255), width=2)
        img.convert("RGB").save(REF / f"fists-{cls}-{anim}.png")
    print(cls, "fists:", {a: table[a][0][0] for a, _ in anims})


def hand_sheet(table, anims, cells, cls):
    """Hand layer: opaque pixels in a disc around every visible fist (the fist drawn again over the weapon handle)."""
    out_dir = SPR / cls / "body" / "hand"
    import_module("build-hero-layered-game-assets").mkdirs(out_dir)
    yy, xx = np.mgrid[0:CH, 0:CW]
    for anim, n in anims:
        sheet = np.zeros((CH * PAINTED, CW * n, 4), np.uint8)
        for r in range(PAINTED):
            for f in range(n):
                a = np.asarray(cells[anim][r][f])
                m = np.zeros((CH, CW), bool)
                for x, y, vis in table[anim][r][f].values():
                    if vis:
                        m |= (xx - x) ** 2 + (yy - y) ** 2 <= HAND_R * HAND_R
                m &= a[..., 3] > 0
                m = ndimage.binary_opening(m, iterations=1) | (m & (a[..., 3] > 200))
                cell = np.zeros_like(a)
                cell[m] = a[m]
                sheet[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW] = cell
        dst = out_dir / f"{anim}.png"
        Image.fromarray(sheet).save(dst)
        import_module("build-hero-layered-game-assets").meta(dst, "idle")


def cmd_grips(cls):
    poses = import_module("hero_layered_grip_poses")
    path = FISTS / f"{cls}-fists.json"
    table = json.loads(path.read_text(encoding="utf-8"))["anims"]
    anims, heads, cells = body(cls)
    hand_sheet(table, anims, cells, cls)
    cj_path = DATA / cls / "class.json"
    cj = json.loads(cj_path.read_text(encoding="utf-8"))
    cj["weapons"], cj["grips"] = poses.class_grips(ROOT, cls, table, heads, cells)
    if cls == "warrior":
        sword_down(cj, poses)
    if cls == "wizard":
        steady_staff(cj)
    if cls == "cleric":  # walk: same front / behind flip as the Wizard; side view head further ahead so the wings clear the chin
        steady_staff(cj, ahead=CLERIC_STAFF_AHEAD, lift=CLERIC_STAFF_LIFT)
        for g in cj["grips"]:  # attack frames whose arm-line angle points the crystal down at the feet / across the body
            a = CLERIC_STAFF_ATTACK.get((import_module("hero_layered_grip_poses").PAINTED[g["dir"]], g["f"]))
            if g["part"] == "weapon" and g["anim"] == "attack" and a is not None:
                if g["flip"] and g["f"] == 1:  # left dirs: the staff sits in the far fist, keep the wind-up pose of frame 0
                    f0 = next(h for h in cj["grips"] if (h["part"], h["anim"], h["dir"], h["f"]) == ("weapon", "attack", g["dir"], 0))
                    g["angle"], g["behind"] = f0["angle"], f0["behind"]
                else:
                    g["angle"] = -a if g["flip"] else a
    if cls == "mechanic":
        aim_raised(cj, table, heads, poses)
    cj_path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
    print(cls, len(cj["grips"]), "grips")


def sword_down(cj, poses):
    """Warrior idle / walk / attack recover, facing down / down-right / down-left: the generic rest pose leaves the 150 px sword nearly flat (fist low, tip kept above
    the ground), user 19/11: point it down-outward as far as the tip stays above the feet line."""
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")  # grip -> tip, art px (tip at the top of the art)
    for g in cj["grips"]:
        rest = g["anim"] in ("idle", "walk") or (g["anim"] == "attack" and g["f"] == 3)
        back = g["anim"] == "walk" and ((g["dir"] == 2 and g["x"] < 140) or (g["dir"] == 6 and g["x"] > CW - 140))
        if g["part"] == "weapon" and (rest and g["dir"] in (0, 1, 7) or back):  # back: side walk, sword arm swung back -> blade trails
            a = math.degrees(math.acos(max(-1.0, min(1.0, (g["y"] - SWORD_TIP_Y) / gy))))
            g["angle"] = round(-a if g["x"] < CW / 2 else a, 1)
        elif g["part"] == "weapon":  # every other frame: the tip may not cross the feet line either (generic limit is row 250)
            a = g["angle"]
            g["angle"] = round(poses.keep_tip_above_ground(a, g["y"], gy, 1 if a >= 0 else -1, ground=SWORD_TIP_Y + 4), 1)


def steady_staff(cj, ahead=STAFF_SIDE_AHEAD, lift=0):
    """Wizard walk: the staff head stays where it is at idle for that direction (in front of / beside the face, never over it) while
    the fist swings, and keeps the idle depth. The per-frame face check flipped the staff between in front and behind from one step
    to the next; the idle angle kept as is put the crystal over the face when the fist swings back (side view)."""
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")  # grip -> staff head, art px
    idle = {g["dir"]: g for g in cj["grips"] if g["part"] == "weapon" and g["anim"] == "idle"}
    for g in cj["grips"]:
        if g["part"] == "weapon" and g["anim"] == "walk":
            i = idle[g["dir"]]
            a = math.radians(i["angle"])
            hx, hy = i["x"] + gy * math.sin(a), i["y"] - gy * math.cos(a)
            if g["dir"] in (2, 6):  # side view: the fist swings behind the idle fist, push the head forward so the shaft clears the chin
                hx += ahead if g["dir"] == 2 else -ahead
                hy -= lift
            g["angle"] = round(math.degrees(math.atan2(hx - g["x"], g["y"] - hy)), 1)
            g["behind"] = i["behind"]
        elif g["part"] == "weapon":  # attack swings: the staff head may not sink under the feet line
            a = g["angle"]
            g["angle"] = round(keep_above(a, g["y"], gy, STAFF_HEAD_FLOOR), 1)


def keep_above(a, fy, length, floor):
    """Turn a weapon pointing down (towards horizontal, same side) until its tip ends above cell row `floor`."""
    return import_module("hero_layered_grip_poses").keep_tip_above_ground(a, fy, length, 1 if a >= 0 else -1, ground=floor)


def aim_raised(cj, table, heads, poses):
    """Mechanic attack frames 0-2: every pistol in a raised (shooting) fist points along its own arm, a pistol in a low fist rests
    (muzzle down-forward), also in the mirrored left dirs, where the generic pose gave the far-hand pistol the punching hand's aim
    (upright in front of the chin) while the near fist punched empty."""
    for g in cj["grips"]:
        if g["anim"] != "attack" or g["f"] >= 3:
            continue
        mirror = g["dir"] >= 5
        row = poses.PAINTED[g["dir"]]
        hand = ("R" if g["part"] == "weapon" else "L") if not mirror else ("L" if g["part"] == "weapon" else "R")
        fx, fy, vis = table["attack"][row][g["f"]][hand]
        h = heads[("attack", row, g["f"])]
        a = poses.arm_angle((fx, fy), h, row, hand)
        if fy >= PUNCH_Y:  # resting fist: pistol low, muzzle down-forward (the generic mirror copied the punching hand's aim)
            a = poses.rest_angle("pistol", (fx, fy), h, row, a)
        elif not vis:
            continue
        g["angle"] = round(-a if mirror else a, 1)

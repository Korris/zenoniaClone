"""Asteria V3 Rogue (Assassin, 2026-11-26, RO_FANTASY_VARIANT=assassin-v3): fist tables + dagger grips for the V3 art.

Hooked by Tools/ro_fantasy_grips.py when the variant is set: OVERRIDES replaces every old ("assassin", ...) fist override (old art),
ATTACK feeds ro_fantasy_attack8_grips.FISTS for the 8 painted attack dirs, apply() post-processes class.json grips.
R = character's right hand (main dagger), L = left hand (offhand dagger), [x, y, visible] in body-cell px (320 x 256), read by eye on
the 4x numbered grids docs/reference/character-v3/assassin-v3-test/check/fists-assassin-<anim>.png (the glove detector fails on the
dark brown fingerless gloves). Painted rows: 0 down, 1 down-right, 2 right, 3 up-right, 4 up (attack: + 5 up-left, 6 left, 7 down-left).
"""
import math

import hero_layered_fists as hf

T, F = True, False

# (class, anim, painted row, frame) -> {hand: [x, y, visible]}
OVERRIDES = {
    # idle: down / down-right R = screen-left fist; right: R = the near fist (the far one hidden behind the hip); up-right / up R = screen-right
    ("assassin", "idle", 0, 0): {"R": [117, 164, T], "L": [201, 164, T]},
    ("assassin", "idle", 1, 0): {"R": [116, 161, T], "L": [197, 157, T]},
    ("assassin", "idle", 2, 0): {"R": [156, 158, T], "L": [170, 148, F]},
    ("assassin", "idle", 3, 0): {"R": [197, 158, T], "L": [126, 158, T]},
    ("assassin", "idle", 4, 0): {"R": [202, 158, T], "L": [120, 158, T]},
    # walk (8 frames), read on the 3x grids (ro_fantasy_assassin_v3_fists.grid). Side (right): R = the near arm, drawn over the torso
    ("assassin", "walk", 0, 0): {"R": [129, 144, T], "L": [194, 137, T]},
    ("assassin", "walk", 0, 1): {"R": [117, 153, T], "L": [190, 150, T]},
    ("assassin", "walk", 0, 2): {"R": [117, 150, T], "L": [185, 144, T]},
    ("assassin", "walk", 0, 3): {"R": [126, 137, T], "L": [181, 144, T]},
    ("assassin", "walk", 0, 4): {"R": [129, 160, T], "L": [194, 139, T]},
    ("assassin", "walk", 0, 5): {"R": [128, 155, T], "L": [198, 141, T]},
    ("assassin", "walk", 0, 6): {"R": [131, 159, T], "L": [194, 141, T]},
    ("assassin", "walk", 0, 7): {"R": [129, 139, T], "L": [194, 144, T]},
    ("assassin", "walk", 1, 0): {"R": [131, 160, T], "L": [192, 148, T]},
    ("assassin", "walk", 1, 1): {"R": [128, 160, T], "L": [190, 144, T]},
    ("assassin", "walk", 1, 2): {"R": [128, 159, T], "L": [198, 148, T]},
    ("assassin", "walk", 1, 3): {"R": [138, 151, T], "L": [200, 133, T]},
    ("assassin", "walk", 1, 4): {"R": [151, 157, T], "L": [185, 133, T]},
    ("assassin", "walk", 1, 5): {"R": [129, 160, T], "L": [192, 135, T]},
    ("assassin", "walk", 1, 6): {"R": [131, 155, T], "L": [192, 146, T]},
    ("assassin", "walk", 1, 7): {"R": [138, 153, T], "L": [198, 137, T]},
    ("assassin", "walk", 2, 0): {"R": [196, 153, T], "L": [140, 164, T]},
    ("assassin", "walk", 2, 1): {"R": [135, 163, T], "L": [190, 155, F]},
    ("assassin", "walk", 2, 2): {"R": [198, 153, T], "L": [120, 153, T]},
    ("assassin", "walk", 2, 3): {"R": [203, 153, T], "L": [120, 153, T]},
    ("assassin", "walk", 2, 4): {"R": [200, 144, T], "L": [124, 141, T]},
    ("assassin", "walk", 2, 5): {"R": [190, 151, T], "L": [140, 150, F]},
    ("assassin", "walk", 2, 6): {"R": [203, 146, T], "L": [120, 146, T]},
    ("assassin", "walk", 2, 7): {"R": [207, 153, T], "L": [120, 144, T]},
    ("assassin", "walk", 3, 0): {"R": [189, 155, T], "L": [128, 160, T]},
    ("assassin", "walk", 3, 1): {"R": [201, 164, T], "L": [124, 157, T]},
    ("assassin", "walk", 3, 2): {"R": [203, 139, T], "L": [117, 144, T]},
    ("assassin", "walk", 3, 3): {"R": [207, 135, T], "L": [113, 144, T]},
    ("assassin", "walk", 3, 4): {"R": [203, 132, T], "L": [117, 151, T]},
    ("assassin", "walk", 3, 5): {"R": [194, 159, T], "L": [131, 153, T]},
    ("assassin", "walk", 3, 6): {"R": [203, 148, T], "L": [117, 151, T]},
    ("assassin", "walk", 3, 7): {"R": [207, 160, T], "L": [120, 153, T]},
    ("assassin", "walk", 4, 0): {"R": [198, 153, T], "L": [117, 155, T]},
    ("assassin", "walk", 4, 1): {"R": [201, 151, T], "L": [120, 159, T]},
    ("assassin", "walk", 4, 2): {"R": [203, 146, T], "L": [117, 151, T]},
    ("assassin", "walk", 4, 3): {"R": [207, 124, T], "L": [113, 146, T]},
    ("assassin", "walk", 4, 4): {"R": [198, 137, T], "L": [120, 157, T]},
    ("assassin", "walk", 4, 5): {"R": [200, 153, T], "L": [124, 155, T]},
    ("assassin", "walk", 4, 6): {"R": [200, 146, T], "L": [120, 151, T]},
    ("assassin", "walk", 4, 7): {"R": [198, 159, T], "L": [113, 141, T]},
}
# (class, d8, frame) -> {hand: [x, y, visible]}
ATTACK = {
    # attack d8 0: down, R = screen-left (both daggers are the same art: a fist pair per frame matters more than which hand ChatGPT meant)
    ("assassin", 0, 0): {"R": [145, 148, T], "L": [190, 157, T]},
    ("assassin", 0, 1): {"R": [120, 145, T], "L": [208, 135, T]},
    ("assassin", 0, 2): {"R": [150, 130, T], "L": [235, 163, T]},
    ("assassin", 0, 3): {"R": [135, 140, T], "L": [210, 133, T]},
    ("assassin", 0, 4): {"R": [132, 142, T], "L": [197, 135, T]},
    ("assassin", 0, 5): {"R": [128, 145, T], "L": [203, 132, T]},
    ("assassin", 0, 6): {"R": [128, 153, T], "L": [208, 157, T]},
    ("assassin", 0, 7): {"R": [125, 160, T], "L": [210, 158, T]},
    # attack d8 1: down-right, R = the striking fist f1-f2, L = the stabbing fist f3-f4
    ("assassin", 1, 0): {"R": [145, 145, T], "L": [205, 157, T]},
    ("assassin", 1, 1): {"R": [220, 177, T], "L": [212, 125, T]},
    ("assassin", 1, 2): {"R": [217, 177, T], "L": [138, 132, T]},
    ("assassin", 1, 3): {"R": [132, 143, T], "L": [238, 148, T]},
    ("assassin", 1, 4): {"R": [132, 145, T], "L": [253, 147, T]},
    ("assassin", 1, 5): {"R": [135, 148, T], "L": [220, 135, T]},
    ("assassin", 1, 6): {"R": [138, 157, T], "L": [210, 150, T]},
    ("assassin", 1, 7): {"R": [135, 157, T], "L": [215, 152, T]},
    # attack d8 2: right, R = the near arm (drawn over the torso), L = the far arm (stabs forward f3-f4)
    ("assassin", 2, 0): {"R": [135, 162, T], "L": [215, 145, T]},
    ("assassin", 2, 1): {"R": [210, 153, T], "L": [150, 140, F]},
    ("assassin", 2, 2): {"R": [225, 163, T], "L": [115, 133, T]},
    ("assassin", 2, 3): {"R": [113, 165, T], "L": [217, 132, T]},
    ("assassin", 2, 4): {"R": [112, 167, T], "L": [228, 127, T]},
    ("assassin", 2, 5): {"R": [125, 160, T], "L": [193, 133, T]},
    ("assassin", 2, 6): {"R": [135, 172, T], "L": [190, 155, T]},
    ("assassin", 2, 7): {"R": [138, 170, T], "L": [193, 162, T]},
    # attack d8 3: up-right, R = screen-right, L stabs f3-f5
    ("assassin", 3, 0): {"R": [223, 128, T], "L": [152, 167, T]},
    ("assassin", 3, 1): {"R": [268, 125, T], "L": [163, 128, T]},
    ("assassin", 3, 2): {"R": [267, 112, T], "L": [168, 112, T]},
    ("assassin", 3, 3): {"R": [173, 140, T], "L": [260, 100, T]},
    ("assassin", 3, 4): {"R": [163, 135, T], "L": [263, 90, T]},
    ("assassin", 3, 5): {"R": [160, 143, T], "L": [240, 105, T]},
    ("assassin", 3, 6): {"R": [220, 143, T], "L": [132, 138, T]},
    ("assassin", 3, 7): {"R": [207, 153, T], "L": [120, 147, T]},
    # attack d8 5: mirror of d8 3 (the left-dir attack art is the mirrored right art: identical daggers in both hands; R = mirrored L)
    ("assassin", 5, 0): {"R": [168, 167, T], "L": [97, 128, T]},
    ("assassin", 5, 1): {"R": [157, 128, T], "L": [52, 125, T]},
    ("assassin", 5, 2): {"R": [152, 112, T], "L": [53, 112, T]},
    ("assassin", 5, 3): {"R": [60, 100, T], "L": [147, 140, T]},
    ("assassin", 5, 4): {"R": [57, 90, T], "L": [157, 135, T]},
    ("assassin", 5, 5): {"R": [80, 105, T], "L": [160, 143, T]},
    ("assassin", 5, 6): {"R": [188, 138, T], "L": [100, 143, T]},
    ("assassin", 5, 7): {"R": [200, 147, T], "L": [113, 153, T]},
    # attack d8 6: mirror of d8 2 (the left-dir attack art is the mirrored right art: identical daggers in both hands; R = mirrored L)
    ("assassin", 6, 0): {"R": [105, 145, T], "L": [185, 162, T]},
    ("assassin", 6, 1): {"R": [170, 140, F], "L": [110, 153, T]},
    ("assassin", 6, 2): {"R": [205, 133, T], "L": [95, 163, T]},
    ("assassin", 6, 3): {"R": [103, 132, T], "L": [207, 165, T]},
    ("assassin", 6, 4): {"R": [92, 127, T], "L": [208, 167, T]},
    ("assassin", 6, 5): {"R": [127, 133, T], "L": [195, 160, T]},
    ("assassin", 6, 6): {"R": [130, 155, T], "L": [185, 172, T]},
    ("assassin", 6, 7): {"R": [127, 162, T], "L": [182, 170, T]},
    # attack d8 7: mirror of d8 1 (the left-dir attack art is the mirrored right art: identical daggers in both hands; R = mirrored L)
    ("assassin", 7, 0): {"R": [115, 157, T], "L": [175, 145, T]},
    ("assassin", 7, 1): {"R": [108, 125, T], "L": [100, 177, T]},
    ("assassin", 7, 2): {"R": [182, 132, T], "L": [103, 177, T]},
    ("assassin", 7, 3): {"R": [82, 148, T], "L": [188, 143, T]},
    ("assassin", 7, 4): {"R": [67, 147, T], "L": [188, 145, T]},
    ("assassin", 7, 5): {"R": [100, 135, T], "L": [185, 148, T]},
    ("assassin", 7, 6): {"R": [110, 150, T], "L": [182, 157, T]},
    ("assassin", 7, 7): {"R": [105, 152, T], "L": [185, 157, T]},
    # attack d8 4: up, R = screen-right
    ("assassin", 4, 0): {"R": [173, 162, T], "L": [108, 138, T]},
    ("assassin", 4, 1): {"R": [207, 92, T], "L": [120, 123, T]},
    ("assassin", 4, 2): {"R": [218, 105, T], "L": [120, 132, T]},
    ("assassin", 4, 3): {"R": [203, 95, T], "L": [117, 135, T]},
    ("assassin", 4, 4): {"R": [203, 80, T], "L": [117, 140, T]},
    ("assassin", 4, 5): {"R": [202, 105, T], "L": [125, 132, T]},
    ("assassin", 4, 6): {"R": [195, 132, T], "L": [115, 148, T]},
    ("assassin", 4, 7): {"R": [203, 155, T], "L": [118, 157, T]},
}


MIRROR_OF = [0, 1, 2, 3, 4, 3, 2, 1]
REST_FRAMES = (0, 5, 6)   # crouch / recover: daggers in the rest pose of their fist (f7 = the idle angle of that dir)
DAGGER = {}               # (d8, f, part): angle override, read on the 8-dir check sheet
BEHIND = {}               # (d8, f, part): bool override


def _arm(poses, fist, h, d8, hand):
    """Shoulder -> fist angle (0 = up, clockwise) on a painted row; left dirs are computed on the mirrored geometry."""
    if d8 < 5:
        return poses.arm_angle(fist, h, d8, hand)
    hm = dict(h, x=320 - h["x"])
    return -poses.arm_angle((320 - fist[0], fist[1]), hm, MIRROR_OF[d8], "L" if hand == "R" else "R")


WALK_SWAY, WALK_SWAY_MAX = 0.3, 12.0  # walk: dagger angle change (deg) per cell px the fist moves sideways from its idle spot, capped


def walk_steady(cj):
    """Walk: the generic rest pose added half the arm swing, so a fist swung up at chest height held its dagger nearly flat (sticking out
    like a wing on the back views). Here each dagger keeps the idle angle of its dir, tilted a little by the fist's sideways swing."""
    idle = {(g["part"], g["dir"]): g for g in cj["grips"] if g["anim"] == "idle" and g["f"] == 0}
    for g in cj["grips"]:
        i = idle.get((g["part"], g["dir"]))
        if g["anim"] != "walk" or i is None:
            continue
        side = 1 if i["angle"] >= 0 else -1  # outward = the side the idle blade already points to
        d = max(-WALK_SWAY_MAX, min(WALK_SWAY_MAX, WALK_SWAY * (g["x"] - i["x"]) * side))
        g["angle"] = round(i["angle"] - side * d, 1)


def apply(cj, table, heads, cells, poses):
    """V3 Rogue attack painted for all 8 dirs: main dagger on the R fist, offhand dagger on the L fist of each painted row (no mirroring).
    Strike frames: the blade along the arm (shoulder -> fist), so the point leads the slash / the stab; rest frames: rest pose; last frame
    = the idle angle of that dir (no pop when the attack ends). Depth: hidden fist -> behind; back views keep the generic depth rule."""
    walk_steady(cj)
    rows = table.get("attack")
    if not rows or len(rows) != 8:
        return
    gy = next(w["gy"] for w in cj["weapons"] if w["part"] == "weapon")
    idle = {(g["part"], g["dir"]): g for g in cj["grips"] if g["anim"] == "idle" and g["f"] == 0}
    grips = [g for g in cj["grips"] if g["anim"] != "attack"]
    for d8 in range(8):
        n = len(rows[d8])
        for f in range(n):
            h = heads[("attack", d8, f)]
            cell = cells["attack"][d8][f]
            for part, hand in (("weapon", "R"), ("offhand", "L")):
                fx, fy, vis = rows[d8][f][hand]
                if f == n - 1 and (part, d8) in idle:
                    a = idle[(part, d8)]["angle"]
                elif f in REST_FRAMES:
                    row = MIRROR_OF[d8]
                    mx, mh = (fx, h) if d8 < 5 else (320 - fx, dict(h, x=320 - h["x"]))
                    a = poses.rest_angle("blade", (mx, fy), mh, row, _arm(poses, (fx, fy), h, d8, hand) * (1 if d8 < 5 else -1))
                    a = a if d8 < 5 else -a
                else:
                    a = _arm(poses, (fx, fy), h, d8, hand)
                a = DAGGER.get((d8, f, part), a)
                a = poses.keep_tip_above_ground(a, fy, gy, 1 if math.sin(math.radians(a)) >= 0 else -1, ground=250)
                cover = hf.ring_cover(cell, fx, fy)
                behind = BEHIND.get((d8, f, part), poses.behind_of(d8, "main" if part == "weapon" else "off", vis, cover))
                grips.append({"part": part, "anim": "attack", "dir": d8, "f": f, "x": round(fx, 1), "y": round(fy, 1),
                              "angle": round(poses.wrap(a), 1), "behind": bool(behind), "flip": False})
    cj["grips"] = grips
    print("assassin V3 attack: 8 painted dirs,", sum(g["anim"] == "attack" for g in grips), "grips")


def grid(anim, row, out, x0=50, x1=270, y0=50, y1=250, s=2, frames=None):
    """Review image: the 8 (or n) cells of one painted row at s x, numbered grid every 10 cell px, current table fists marked
    (R red, L blue, hollow = hidden). Read fists on it: python -c "import ro_fantasy_assassin_v3_fists as a; a.grid('walk', 0, 'x.png')"."""
    import json
    from PIL import Image, ImageDraw
    import ro_fantasy_grips as g
    anims, heads, cells = g.body("assassin")
    try:
        table = json.loads((g.FISTS / "assassin-fists.json").read_text(encoding="utf-8"))["anims"].get(anim)
    except FileNotFoundError:
        table = None
    idx = list(frames) if frames is not None else list(range(len(cells[anim][row])))
    row_cells = [cells[anim][row][f] for f in idx]
    cw, ch = (x1 - x0) * s, (y1 - y0) * s
    img = Image.new("RGBA", (cw * len(row_cells), ch), (200, 210, 190, 255))
    d = ImageDraw.Draw(img)
    for k, (f, c) in enumerate(zip(idx, row_cells)):
        ox = k * cw
        img.alpha_composite(c.crop((x0, y0, x1, y1)).resize((cw, ch), Image.NEAREST), (ox, 0))
        for gx in range(x0, x1, 10):
            d.line((ox + (gx - x0) * s, 0, ox + (gx - x0) * s, ch), fill=(0, 0, 0, 150 if gx % 50 == 0 else 50))
            if gx % 20 == 0:
                for ty in range(1, ch, 60):
                    d.text((ox + (gx - x0) * s + 1, ty), str(gx), fill=(200, 0, 0))
        for gy in range(y0, y1, 10):
            d.line((ox, (gy - y0) * s, ox + cw, (gy - y0) * s), fill=(0, 0, 0, 150 if gy % 50 == 0 else 50))
            if gy % 20 == 0:
                for tx in range(1, cw, 90):
                    d.text((ox + tx, (gy - y0) * s + 1), str(gy), fill=(0, 0, 200))
        if table and row < len(table) and f < len(table[row]):
            for k, col in (("R", (230, 0, 0)), ("L", (0, 60, 255))):
                x, y, vis = table[row][f][k]
                X, Y = ox + (x - x0) * s, (y - y0) * s
                d.ellipse((X - 5, Y - 5, X + 5, Y + 5), outline=col, fill=col if vis else None, width=2)
        d.text((ox + 30, 12), f"r{row} f{f}", fill=(0, 0, 0))
        d.rectangle((ox, 0, ox + cw - 1, ch - 1), outline=(255, 0, 255), width=2)
    img.convert("RGB").save(out)


def attack_preview(d, out_prefix):
    """Before all 8 attack dirs exist: cut one painted dir exactly like ro_fantasy_body_cut.motion_rows does, and write two 3x numbered
    grids (frames 0-3, 4-7) to read its fists: python -c "import ro_fantasy_assassin_v3_fists as a; a.attack_preview('down', 'd:/temp/x')"."""
    import ro_fantasy_body_cut as bc
    idle, _ = bc.idle("assassin")
    figs = [c.crop(c.getbbox()) for c, _ in idle]
    src = bc.newest(f"body-assassin-attack-{d}-8f-chatgpt-*.png")
    cells = bc.attack8_cut.cut(src, bc.skull_w(figs[bc.MIRROR_OF[bc.DRAWN8.index(d)]]), bc.place, bc.skull_w, bc.despeck,
                               bc.bald_px("assassin"), f"assassin attack {d}", d)
    for h in range(4):
        _grid_cells(cells[h * 2:h * 2 + 2], [h * 2, h * 2 + 1], f"{out_prefix}-{h}.png")


def _grid_cells(row_cells, idx, out, x0=50, x1=270, y0=60, y1=230, s=3):
    from PIL import Image, ImageDraw
    cw, ch = (x1 - x0) * s, (y1 - y0) * s
    img = Image.new("RGBA", (cw * len(row_cells), ch), (200, 210, 190, 255))
    d = ImageDraw.Draw(img)
    for k, (f, c) in enumerate(zip(idx, row_cells)):
        ox = k * cw
        img.alpha_composite(c.crop((x0, y0, x1, y1)).resize((cw, ch), Image.NEAREST), (ox, 0))
        for gx in range(x0, x1, 10):
            d.line((ox + (gx - x0) * s, 0, ox + (gx - x0) * s, ch), fill=(0, 0, 0, 150 if gx % 50 == 0 else 50))
            if gx % 20 == 0:
                for ty in range(1, ch, 80):
                    d.text((ox + (gx - x0) * s + 1, ty), str(gx), fill=(200, 0, 0))
        for gy in range(y0, y1, 10):
            d.line((ox, (gy - y0) * s, ox + cw, (gy - y0) * s), fill=(0, 0, 0, 150 if gy % 50 == 0 else 50))
            if gy % 20 == 0:
                for tx in range(1, cw, 100):
                    d.text((ox + tx, (gy - y0) * s + 1), str(gy), fill=(0, 0, 200))
        d.text((ox + 30, 12), f"f{f}", fill=(0, 0, 0))
        d.rectangle((ox, 0, ox + cw - 1, ch - 1), outline=(255, 0, 255), width=2)
    img.convert("RGB").save(out)

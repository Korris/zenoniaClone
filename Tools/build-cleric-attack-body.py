"""Cleric attack1-4 body rows from ChatGPT (board docs/reference/cleric-basic-attack-board.png, 2026-11-08), staff + shield anchors.

One ChatGPT image per anim: 4 rows (facing down, up, left, right) x COUNT frames, empty fists, flat #FF00FF
(ArtLibrary/classes/cleric/attack-board/body/). Each row is fitted onto the 192 cell (Tools/cleric_attack_fit.py: body at idle's
neck-to-sole length, head shrunk to idle's width). Facing left is painted, not mirrored (rule 4: staff always in the RIGHT hand,
shield on the LEFT arm; facing left the staff hand is the far one).

Writes body/bare/<anim>.png, body/hand/<anim>.png (disc FIST_R round the staff fist when the staff is drawn in front; the shield hides
its own hand), body/armed/<anim>.png (bare + T0 gear, the baked fallback / frame count), staff + shield anchors of those anims in
held-cleric-melee.json, `.meta` for new sheets. Old sheets and json are backed up once in attack-board/old/.
Fists and staff angles: Tools/cleric_attack_fists.py (FISTS read on the `grid` images, auto-detected skin blobs marked in grey).
Then (head layer, after every write): python Tools/build-head-layer.py cleric anims-write attack1,attack2,attack3,attack4, then
anims-normscale with the same list, then `headcheck` of Tools/build-cleric-basic-attack.py and Tools/build-cleric-attack-hairstyles-check.py.

  python Tools/build-cleric-attack-body.py grid OUT_DIR [anim ...]   fitted frames at 4x on an 8 px grid, fists marked
  python Tools/build-cleric-attack-body.py preview OUT_DIR [anim ...] staff + shield T0 on the fitted rows, nothing written
  python Tools/build-cleric-attack-body.py armfix [anim ...]          after the head layer: raised forearms onto the fist layer, headless .meta
  python Tools/build-cleric-attack-body.py write [anim ...]          sheets + anchors (default: every anim with a source)
"""
import json
import os
import shutil
import sys
import uuid

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cleric_attack_compose as cc  # noqa: E402
import cleric_attack_fit as fit  # noqa: E402
import math  # noqa: E402
from cleric_attack_fists import FISTS, MIRRORS, PLANT, BUTT_PX, SOLE_Y, STAFF, SHIELD_TILT, SHIELD_OFF, staff_behind, shield_behind  # noqa: E402
from head_layer_classes import skin  # noqa: E402

FRAME = 192
ART = f"{cc.ROOT}/Assets/ArtLibrary/classes/cleric/attack-board"
SRC = {"attack1": f"{ART}/body/cleric-attack1-4dirs-chatgpt-1.png", "attack2": f"{ART}/body/cleric-attack2-4dirs-chatgpt-1.png",
       "attack3": f"{ART}/body/cleric-attack3-4dirs-chatgpt-1.png", "attack4": f"{ART}/body/cleric-attack4-4dirs-chatgpt-1.png"}
COUNT = {"attack1": 4, "attack2": 4, "attack3": 4, "attack4": 6}
MELEE = f"{cc.RES}/Data/classes/cleric/held-cleric-melee.json"
FIST_R = 8.5
STAFF_S, SHIELD_S = 0.52, 0.27


def rows_of(anim, log=print):
    """{dir: [192 cells]} of an anim."""
    parts = fit.split_rows(SRC[anim], COUNT[anim])
    out = {}
    for d in cc.DIRS:
        if (anim, d) in MIRRORS:
            sa, sd = MIRRORS[(anim, d)]
            src = fit.split_rows(SRC[sa], COUNT[sa])[sd] if sa != anim else parts[sd]
            out[d] = [fit.cell(p).transpose(Image.FLIP_LEFT_RIGHT) for p in fit.fit_row(src, sd, log)]
        else:
            out[d] = [fit.cell(p) for p in fit.fit_row(parts[d], d, log)]
    return out


def skin_blobs(c):
    """Centroids of skin blobs below the face (fist candidates), biggest first."""
    a = np.asarray(c)
    m = skin(a) & (a[..., 3] > 200)
    m[:118] = False  # face and neck
    m[168:] = False  # knees
    lab, n = ndimage.label(m)
    out = []
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(ys) >= 10:
            out.append((len(ys), round(float(xs.mean()), 1), round(float(ys.mean()), 1)))
    return [(x, y) for _, x, y in sorted(out, reverse=True)[:3]]


def grid(out, anims):
    os.makedirs(out, exist_ok=True)
    z = 4
    for anim in anims:
        for d, cells in rows_of(anim).items():
            w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
            dr = ImageDraw.Draw(w)
            for f, c in enumerate(cells):
                big = c.resize((FRAME * z, FRAME * z), Image.NEAREST)
                w.paste(big, (f * FRAME * z, 0), big)
                for v in range(0, FRAME, 8):
                    col = (255, 255, 0) if v % 32 == 0 else (95, 95, 95)
                    dr.line([(f * FRAME * z + v * z, 0), (f * FRAME * z + v * z, FRAME * z)], fill=col)
                    dr.line([(f * FRAME * z, v * z), ((f + 1) * FRAME * z, v * z)], fill=col)
                    if v % 32 == 0:
                        dr.text((f * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                        dr.text((f * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
                blobs = skin_blobs(c)
                fists = FISTS.get((anim, d))
                for x, y in blobs:
                    dr.ellipse(((f * FRAME + x - 3) * z, (y - 3) * z, (f * FRAME + x + 3) * z, (y + 3) * z), outline=(160, 160, 160), width=2)
                if fists:
                    for (x, y), col in zip(fists[f], ((255, 0, 0), (0, 160, 255))):  # red = staff (right) fist, blue = shield (left) fist
                        dr.ellipse(((f * FRAME + x - FIST_R) * z, (y - FIST_R) * z, (f * FRAME + x + FIST_R) * z, (y + FIST_R) * z), outline=col, width=3)
                print(anim, d, f, "skin blobs", blobs)
            w.save(f"{out}/{anim}-{d}-grid.png")


def backup():
    old = f"{ART}/old"
    os.makedirs(old, exist_ok=True)
    for layer in ("bare", "hand", "armed"):
        for anim in ("attack1", "attack2", "attack3"):
            src, dst = f"{cc.BODY}/{layer}/{anim}.png", f"{old}/{layer}-{anim}.png"
            if os.path.exists(src) and not os.path.exists(dst):
                shutil.copyfile(src, dst)
    if not os.path.exists(f"{old}/held-cleric-melee.json"):
        shutil.copyfile(MELEE, f"{old}/held-cleric-melee.json")


def ensure_meta(path, like):
    """Copy the .meta of a sheet of the same kind with a new guid (rule: new sheets get the Lv1 import settings, not Unity's defaults)."""
    if os.path.exists(path + ".meta"):
        return
    txt = open(like + ".meta", encoding="utf-8").read()
    old = next(l for l in txt.splitlines() if l.startswith("guid:"))
    open(path + ".meta", "w", encoding="utf-8").write(txt.replace(old, "guid: " + uuid.uuid4().hex))


def anchors_of(anim, d):
    """(staff frames, shield frames) of one row."""
    staff, shield = [], []
    for f, (rf, lf) in enumerate(FISTS[(anim, d)]):
        ang = STAFF[(anim, d)][f]
        gx, gy = rf
        if (anim, d, f) in PLANT:  # slide the grip up the shaft so the butt lands on the sole row (see cleric_attack_fists.PLANT)
            ux, uy = -math.sin(math.radians(ang)), math.cos(math.radians(ang))  # grip -> butt direction
            along = BUTT_PX - (SOLE_Y - rf[1]) / uy
            gx, gy = round(rf[0] - along * ux, 1), round(rf[1] - along * uy, 1)
        staff.append({"x": gx, "y": gy, "angle": ang, "s": STAFF_S, "sx": 1, "behind": staff_behind(anim, d, f), "hidden": False, "fallback": False})
        ox, oy = SHIELD_OFF[d]
        shield.append({"x": lf[0] + ox, "y": lf[1] + oy, "angle": SHIELD_TILT[d], "s": SHIELD_S, "sx": 1, "behind": shield_behind(anim, d, f), "hidden": False, "fallback": False})
    return staff, shield


def fist_layer(c, fist):
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    a = np.asarray(c).copy()
    a[..., 3] = np.where((xx - fist[0]) ** 2 + (yy - fist[1]) ** 2 <= FIST_R ** 2, a[..., 3], 0)
    return Image.fromarray(a)


def preview(out, anims, tier=0):
    """Rows = facings, columns = frames, composited with the staff + shield as the game will (painted head, no hairstyle layer), 3x."""
    os.makedirs(out, exist_ok=True)
    pad, z = 50, 3
    size = FRAME + 2 * pad
    tmp = f"{os.environ.get('TEMP', '/tmp')}/cleric-preview-bare.png"
    for anim in anims:
        rows = rows_of(anim, log=lambda *a: None)
        n = COUNT[anim]
        bare = Image.new("RGBA", (FRAME * n, FRAME * 4), (0, 0, 0, 0))
        _, loaded = cc.chf.load_gear("cleric")
        for d, cells in rows.items():
            for f, c in enumerate(cells):
                bare.paste(c, (f * FRAME, cc.ROWS[d] * FRAME))
            staff, shield = anchors_of(anim, d)
            loaded[("staff", anim, d)], loaded[("shield", anim, d)] = staff, shield
        bare.save(tmp)
        hand_tmp = tmp.replace("bare", "hand")
        hand = Image.new("RGBA", bare.size, (0, 0, 0, 0))
        for d, cells in rows.items():
            for f, c in enumerate(cells):
                if not loaded[("staff", anim, d)][f]["behind"]:
                    hand.paste(fist_layer(c, FISTS[(anim, d)][f][0]), (f * FRAME, cc.ROWS[d] * FRAME))
        hand.save(hand_tmp)
        sheet = Image.new("RGB", (size * n * z, size * 4 * z))
        for d in cc.DIRS:
            for f in range(n):
                im = cc.chf.compose("cleric", anim, d, f, bare_path=tmp, anchors=loaded, hand_path=hand_tmp, tier=tier, pad=pad)
                sheet.paste(cc.on_bg(im).resize((size * z, size * z), Image.NEAREST), (f * size * z, cc.ROWS[d] * size * z))
        sheet.save(f"{out}/{anim}-t{tier}-preview.png")
        print("->", f"{out}/{anim}-t{tier}-preview.png")


def write(anims):
    backup()
    data = json.load(open(MELEE, encoding="utf-8"))
    for anim in anims:
        rows = rows_of(anim)
        n = COUNT[anim]
        bare = Image.new("RGBA", (FRAME * n, FRAME * 4), (0, 0, 0, 0))
        hand = bare.copy()
        for d, cells in rows.items():
            r = cc.ROWS[d]
            staff, shield = anchors_of(anim, d)
            for f, c in enumerate(cells):
                bare.paste(c, (f * FRAME, r * FRAME))
                if not staff[f]["behind"]:
                    hand.paste(fist_layer(c, FISTS[(anim, d)][f][0]), (f * FRAME, r * FRAME))
            data["anchors"] = [a for a in data["anchors"] if not (a["anim"] == anim and a["dir"] == d)]
            data["anchors"] += [{"part": "staff", "anim": anim, "dir": d, "frames": staff}, {"part": "shield", "anim": anim, "dir": d, "frames": shield}]
        for layer, im in (("bare", bare), ("hand", hand)):
            path = f"{cc.BODY}/{layer}/{anim}.png"
            im.save(path)
            ensure_meta(path, f"{cc.BODY}/{layer}/attack1.png")
        # head layer: build-head-layer.py anims-write copies arms raised over the head onto the hand sheet, starting from this copy
        # (it makes one only when missing, so a stale copy would bring the old fists back): refresh it with every write
        pre = f"{cc.ROOT}/Assets/ArtLibrary/classes/cleric/body/hand/{anim}.pre-head.png"
        os.makedirs(os.path.dirname(pre), exist_ok=True)
        hand.save(pre)
        json.dump(data, open(MELEE, "w", encoding="utf-8"), indent=1)
        # armed = what the game shows without gear data and what sets the frame count (HeroRenderer.FrameCount): bare + T0 gear
        armed = Image.new("RGBA", (FRAME * n, FRAME * 4), (0, 0, 0, 0))
        for d in cc.DIRS:
            for f in range(n):
                armed.paste(cc.frame(anim, d, f, pad=0, heads={}), (f * FRAME, cc.ROWS[d] * FRAME))
        path = f"{cc.BODY}/armed/{anim}.png"
        armed.save(path)
        ensure_meta(path, f"{cc.BODY}/armed/attack1.png")
        print(anim, "written")


# Raised arms (fist above SHOULDER_Y): the head layer's mask takes the forearm beside / in front of the head with the painted head, and its
# arm-over-head pass misses the cream sleeve, so the hairstyle head is drawn with a floating fist next to it. armfix puts the erased bare
# pixels in a capsule fist -> shoulder (radius ARM_R, hair colour left out) back into the HEADLESS body, i.e. under the hairstyle head:
# a hairstyle that covers the spot hides that bit of arm (it reads as the arm passing behind the head), a smaller one shows it, nothing is
# left floating. Version 1 (2026-11-08) put them on the fist layer, drawn above the head: thin sleeve strips across the cheek (attack3 left /
# right) and specks of sleeve / old hair outline on long hairstyles (attack1 / attack2 up). The fist layer is reset to the painted fist discs
# (.pre-head copy), so anims-write's own arm-over-head bits (old hair outline taken for an arm) go too. Run after anims-write + anims-normscale.
SHOULDER = {"down": ((80, 118), (112, 118)), "up": ((112, 118), (80, 118)), "left": ((92, 118), (92, 118)), "right": ((100, 118), (100, 118))}
SHOULDER_Y, ARM_R, FIST_SKIN_R = 115, 7.0, 8.5


def armfix(anims):
    from head_layer_classes import CLASSES
    hair = CLASSES["cleric"]["hair"]
    yy, xx = np.mgrid[0:FRAME, 0:FRAME].astype(float)
    for anim in anims:
        bare = np.asarray(Image.open(f"{cc.BODY}/bare/{anim}.png").convert("RGBA"))
        hl = np.asarray(Image.open(f"{cc.BODY}/headless/{anim}.png").convert("RGBA")).copy()
        pre = f"{cc.ROOT}/Assets/ArtLibrary/classes/cleric/body/hand/{anim}.pre-head.png"
        hand = np.asarray(Image.open(pre).convert("RGBA")).copy()  # painted fist discs only, nothing drawn over the hairstyle head
        fixed = 0
        for d in cc.DIRS:
            r = cc.ROWS[d]
            for f, fists in enumerate(FISTS[(anim, d)]):
                sl = np.s_[r * FRAME:(r + 1) * FRAME, f * FRAME:(f + 1) * FRAME]
                b, h = bare[sl], hl[sl]
                m = np.zeros((FRAME, FRAME), bool)
                near_fist = np.zeros((FRAME, FRAME), bool)
                for (fx, fy), (sx, sy) in zip(fists, SHOULDER[d]):
                    if fy >= SHOULDER_Y:
                        continue
                    near_fist |= (xx - fx) ** 2 + (yy - fy) ** 2 <= FIST_SKIN_R ** 2
                    vx, vy = sx - fx, sy - fy
                    t = np.clip(((xx - fx) * vx + (yy - fy) * vy) / (vx * vx + vy * vy), 0, 1)
                    m |= (xx - fx - t * vx) ** 2 + (yy - fy - t * vy) ** 2 <= ARM_R ** 2
                m &= (b[..., 3] > 40) & (h[..., 3] < 40) & ~hair(b)
                # only sleeve (cream / white) and skin, plus their 1 px dark outline: old hair outline in the capsule stays out
                rgb = b[..., :3].astype(float)
                lum = rgb @ [0.3, 0.59, 0.11]
                sat = (rgb.max(-1) - rgb.min(-1)) / np.maximum(rgb.max(-1), 1)
                core = m & (((lum > 170) & (sat < 0.25)) | (skin(b) & near_fist))  # skin only at the fist: the cheek beside the arm stays head
                m = core | (m & (lum < 90) & ndimage.binary_dilation(core, iterations=1))
                if m.any():
                    h[m] = b[m]  # every facing: on the fist layer the filtered sleeve came out as broken dotted strips over the hair
                    fixed += 1
        Image.fromarray(hl).save(f"{cc.BODY}/headless/{anim}.png")
        Image.fromarray(hand).save(f"{cc.BODY}/hand/{anim}.png")
        print(anim, "raised-arm frames restored under the head:", fixed)


def metas(anims):
    """`.meta` of the head layer's new headless sheets (build-head-layer.py writes only the png)."""
    for anim in anims:
        ensure_meta(f"{cc.BODY}/headless/{anim}.png", f"{cc.BODY}/bare/attack1.png")  # headless/idle.png has no .meta yet


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "grid":
        grid(sys.argv[2], sys.argv[3:] or [a for a in SRC if os.path.exists(SRC[a])])
    elif cmd == "preview":
        preview(sys.argv[2], sys.argv[3:] or [a for a in SRC if os.path.exists(SRC[a])])
    elif cmd == "armfix":
        armfix(sys.argv[2:] or list(SRC))
        metas(sys.argv[2:] or list(SRC))
    elif cmd == "write":
        write(sys.argv[2:] or [a for a in SRC if os.path.exists(SRC[a])])
    else:
        print(__doc__)

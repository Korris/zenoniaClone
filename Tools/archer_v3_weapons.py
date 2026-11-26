"""Archer V3 weapons (26/11): bow (LEFT hand), quiver (back), flying arrow, cut from the ChatGPT sheet
src/archer-v3/keyed/weapons-archer-v3-chatgpt-N.png (#00FF00, left to right: bow upright with the string on the left, quiver upright,
arrow lying flat pointing right), sizes baked into the art like Knight V3 (cell px of the 216 px figure):
  bow 55 % of the figure (119 px), quiver 40 % (86 px), arrow 38 % (82 px)  (sizes asked in the design chat, 26/11)
Grips written into class.json (installed 26/11, HeroClass.Archer):
  part "weapon"   = the BOW (weapon slot "Bow") on the character's LEFT fist L on every Dir8 dir: the layered renderer draws a part on its
                    per-dir grips, so the hand is chosen here (data), never swapped on the left dirs (they have their own grips)
  part "offhand"  = the QUIVER (offhand slot "Quiver") on the back, placed from the head anchor of each frame
  "projectile"    = the flying arrow sprite (ArrowBoltRig, ComboConfigs.ArcherHits)
Bow pose: idle / walk upright, top tilted BOW_TILT away from the body, string toward the body (art flipped when the fist is left of
the skull); attack upright (string toward the archer, belly toward the target: flipped facing left). Depth: behind the body when the
left fist is hidden or is the far hand (facing down-right / right / up-right at rest; attack facing up-right / up / up-left).
"""
import json
import math

import numpy as np
from PIL import Image

BOW_PX, QUIVER_PX, ARROW_PX = 119, 86, 82
BOW_TILT = 6.0
SIDE_TILT = 20.0
CW = 320
DIR8 = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
PAINTED = [0, 1, 2, 3, 4, 3, 2, 1]
# quiver on the back per painted row: centre offset (dx, dy) in skull widths from the skull top centre, angle (deg, clockwise),
# behind the body?  Mouth (feathers) over the RIGHT shoulder: screen-left facing down, screen-right from the back (read on the
# approved armed idle docs/reference/character-v3/archer-v3/body-archer-idle-5dir-armed-review-v3-chatgpt-1.png)
QUIVER = {0: (-0.30, 1.55, -32, True), 1: (-0.42, 1.55, -30, True), 2: (-0.55, 1.55, -26, True),
          3: (0.05, 1.70, 33, False), 4: (0.08, 1.70, 30, False)}


def cut(bc, src):
    """[bow, quiver, arrow] RGBA figures (left to right) of the weapons sheet, keyed."""
    img = bc.pilot.key_green(Image.open(src))
    figs = [f for f, _, _ in bc.pilot.figures_with_pos(img, 3)]
    return [bc.despeck(f) for f in figs]


def scaled(fig, px, along="h"):
    s = px / (fig.height if along == "h" else fig.width)
    return fig.resize((max(1, round(fig.width * s)), max(1, round(fig.height * s))), Image.LANCZOS)


def bow_grip(bow):
    """Pivot = centre of the widest opaque run (the limb with its leather wrap, not the thin string) on the middle row."""
    a = np.asarray(bow)[..., 3] > 100
    y = bow.height // 2
    xs = np.nonzero(a[y])[0]
    runs = np.split(xs, np.nonzero(np.diff(xs) > 1)[0] + 1)
    run = max(runs, key=len)
    return float(run.mean()), float(y)


def grips(table, heads, anims):
    out = []
    for anim, n in anims:
        rows8 = len(table[anim]) == 8
        for d8 in range(8):
            row = d8 if rows8 else PAINTED[d8]
            mirror = (not rows8) and d8 >= 5
            for f in range(n):
                h = heads[(anim, row, f)]
                hx = CW - h["x"] if mirror else h["x"]
                if mirror:  # mirrored right row: the character's left hand is the mirrored R fist
                    x, y, vis = table[anim][row][f]["R"]
                    x = CW - x
                else:
                    x, y, vis = table[anim][row][f]["L"]
                left_of_skull = x < hx
                if anim == "attack":
                    face_left = d8 >= 5
                    flip = face_left if d8 not in (0, 4) else left_of_skull
                    angle = 0.0
                    behind = (not vis) or d8 in (3, 4, 5)
                else:
                    flip = left_of_skull if d8 not in (2, 6) else d8 == 6
                    angle = -BOW_TILT if left_of_skull else BOW_TILT
                    if d8 in (2, 6):  # side view: the far (right view) fist is behind the hip, tilt the top forward so the limb shows
                        angle = SIDE_TILT if d8 == 2 else -SIDE_TILT
                    behind = (not vis) or d8 in (1, 2, 3)
                out.append({"part": "weapon", "anim": anim, "dir": d8, "f": f, "x": round(x, 1), "y": round(y, 1),
                            "angle": angle, "behind": bool(behind), "flip": bool(flip)})
                dx, dy, qa, qb = QUIVER[PAINTED[d8]]
                s = -1 if d8 >= 5 else 1
                out.append({"part": "offhand", "anim": anim, "dir": d8, "f": f,
                            "x": round(hx + s * dx * h["w"], 1), "y": round(h["y"] + dy * h["w"], 1),
                            "angle": s * qa, "behind": qb, "flip": d8 >= 5})
    return out


def main(bc, ga, g, out_dir, game, docs):
    src = bc.newest("weapons-archer-v3-chatgpt-*.png")
    bow, quiver, arrow = cut(bc, src)
    bow, quiver, arrow = scaled(bow, BOW_PX), scaled(quiver, QUIVER_PX), scaled(arrow, ARROW_PX, "w")
    wdir = ga.SPR.parent / "weapons"  # Sprites/hero-layered/weapons/, beside the other classes' weapon art
    entries = []
    for name, img, part, (gx, gy) in (("archer-bow", bow, "weapon", bow_grip(bow)),
                                      ("archer-quiver", quiver, "offhand", (quiver.width / 2, quiver.height * 0.55)),
                                      ("archer-arrow", arrow, "projectile", (arrow.width / 2, arrow.height / 2))):
        img.save(wdir / f"{name}.png")
        ga.game_assets.meta(wdir / f"{name}.png", "idle")
        entries.append({"part": part, "sheet": ga.res(wdir / name), "gx": round(gx, 1), "gy": round(gy, 1)})
        print(f"{name}: {img.size}, pivot ({gx:.1f}, {gy:.1f})")
    tab = json.loads((out_dir / "sheets/archer-fists.json").read_text(encoding="utf-8"))["anims"]
    anims, heads, _ = g.body("archer")
    cj_path = ga.DATA / "archer/class.json"
    cj = json.loads(cj_path.read_text(encoding="utf-8"))
    cj["weapons"] = [dict(e, overHand=False) for e in entries if e["part"] in ("weapon", "offhand")]
    cj["projectile"] = next(e for e in entries if e["part"] == "projectile")
    cj["grips"] = grips(tab, heads, anims)
    cj["note"] = (cj.get("note", "").split(" ARCHER")[0] + " ARCHER: bow = part 'weapon' on the LEFT fist every dir, quiver = 'offhand' "
                  "on the back (head-anchored), flying arrow = 'projectile' (Tools/archer_v3_weapons.py).")
    cj_path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
    print("archer grips:", len(cj["grips"]))
    write_shared_grips(ga, entries)


LOCK = "D:/temp/hero-install.lock"


def write_shared_grips(ga, entries):
    """Data/hero-layered/weapons.json (shared by every class tool): add the archer grips under the install lock (mkdir = atomic)."""
    import os
    import time
    for _ in range(600):
        try:
            os.mkdir(LOCK)
            break
        except FileExistsError:
            time.sleep(0.5)
    else:
        raise SystemExit(f"{LOCK} held for 5 min: weapons.json not written")
    try:
        path = ga.DATA.parent / "weapons.json"
        wj = json.loads(path.read_text(encoding="utf-8"))
        for e in entries:
            wj["grips"][e["sheet"].rsplit("/", 1)[1]] = [e["gx"], e["gy"]]
        if "Archer V3" not in wj["note"]:
            wj["note"] += " Archer V3 26/11: archer-bow, archer-quiver, archer-arrow from Tools/archer_v3_weapons.py."
        path.write_text(json.dumps(wj, indent=1), encoding="utf-8")
    finally:
        os.rmdir(LOCK)

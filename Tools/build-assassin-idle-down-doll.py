"""Assassin idle facing down as a paper doll, like the Warrior's (build-warrior-idle-down-doll.py): the painted row 0 of body/armed/idle.png
(Tools/build-idle-down-set.py, daggers painted in) becomes body/bare/idle.png row 0 with the daggers erased, body/hand/idle.png row 0 gets the
two fists cut from the same art, and the idle/down anchors in held-assassin.json and held-assassin-melee.json lose their `fallback`, so the
worn daggers draw through the anchors at the melee size (2026-11-03: the daggers grew to 3/4 of the hero and the painted ones stayed small).

The painted daggers sit outside the body: from each fist they point down and out, past the boots, so they are cut by two rectangles per
side read off a 10x zoom of frame 0 (the four frames breathe with the hair only; the fists and daggers do not move). The guard stub that
stays under the fist is covered by the worn dagger, which draws in front of the body and under the fist overlay.

  python Tools/build-assassin-idle-down-doll.py          write bare row 0, hand row 0, anchors (originals kept in new-style/old/idle-down-row0/)
  python Tools/build-assassin-idle-down-doll.py preview  only the check sheet: scratch/idle-down-doll.png next to this file's OUT

Then: python Tools/build-assassin-new-style.py dagger-preview OUT iron-kunai (idle rows) and look at idle down in the game.
"""
import json
import os
import sys

import numpy as np
from PIL import Image

RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/assassin/body"
OLD = "Assets/ArtLibrary/classes/assassin/new-style/old/idle-down-row0"
GEAR, GEAR_MELEE = f"{RES}/Data/classes/assassin/held-assassin.json", f"{RES}/Data/classes/assassin/held-assassin-melee.json"
FRAME = 192
DAGGER_S = 0.243  # build-assassin-new-style.DAGGER_S: one dagger size on every frame

FISTS = {"main": (63, 148), "off": (129, 148)}  # frame px, centre of the left / right glove on screen
ANGLES = {"main": -115.0, "off": 115.0}         # clockwise from tip-up: down and out, the tip level with the boots
FIST_R = 9
# (x0, y0, x1, y1) inclusive, frame px: the painted dagger of each side, blade and guard
ERASE = {"main": [(0, 143, 58, 191), (0, 157, 63, 191)], "off": [(134, 143, 191, 191), (129, 157, 191, 191)]}


def frames(sheet):
    return [sheet[:FRAME, i * FRAME:(i + 1) * FRAME] for i in range(sheet.shape[1] // FRAME)]


def erase_mask():
    m = np.zeros((FRAME, FRAME), bool)
    for boxes in ERASE.values():
        for x0, y0, x1, y1 in boxes:
            m[y0:y1 + 1, x0:x1 + 1] = True
    return m


def fist_mask(arr):
    """Glove pixels inside a disc round each fist: not the gold guard, not the magenta blade, so nothing of the old dagger rides on top of the new one."""
    yy, xx = np.mgrid[:FRAME, :FRAME]
    m = np.zeros((FRAME, FRAME), bool)
    for cx, cy in FISTS.values():
        m |= (xx - cx) ** 2 + (yy - cy) ** 2 <= FIST_R ** 2
    r, g, b, a = [arr[..., i].astype(int) for i in range(4)]
    gold = (r > 150) & (g > 100) & (b < 120)
    blade = (b > 150) & (g < 0.7 * np.maximum(r, b)) & (r > 100)
    return m & (a > 0) & ~gold & ~blade


def build(write):
    armed = np.asarray(Image.open(f"{BODY}/armed/idle.png").convert("RGBA")).copy()
    bare = np.asarray(Image.open(f"{BODY}/bare/idle.png").convert("RGBA")).copy()
    hand = np.asarray(Image.open(f"{BODY}/hand/idle.png").convert("RGBA")).copy()
    n = armed.shape[1] // FRAME
    erase = erase_mask()
    check = Image.new("RGBA", (FRAME * n, FRAME * 3), (58, 62, 70, 255))
    for i in range(n):
        cell = armed[:FRAME, i * FRAME:(i + 1) * FRAME]
        body = cell.copy()
        body[erase] = 0
        fists = np.zeros_like(cell)
        fm = fist_mask(cell)
        fists[fm] = cell[fm]
        bare[:FRAME, i * FRAME:(i + 1) * FRAME] = body
        hand[:FRAME, i * FRAME:(i + 1) * FRAME] = fists
        for row, im in enumerate((cell, body, fists)):
            check.alpha_composite(Image.fromarray(im, "RGBA"), (i * FRAME, row * FRAME))
    if not write:
        return check
    os.makedirs(OLD, exist_ok=True)
    for name in ("armed", "bare", "hand"):
        backup = f"{OLD}/{name}-idle.png"
        if not os.path.exists(backup):
            Image.open(f"{BODY}/{name}/idle.png").save(backup)
    Image.fromarray(bare, "RGBA").save(f"{BODY}/bare/idle.png")
    Image.fromarray(hand, "RGBA").save(f"{BODY}/hand/idle.png")
    write_anchors(n)
    return check


def anchor(part):
    x, y = FISTS[part]
    return {"x": float(x), "y": float(y), "angle": ANGLES[part], "s": DAGGER_S, "sx": 1, "behind": False, "hidden": False, "fallback": False}


def write_anchors(n):
    """idle/down of both parts in both files: the melee file wins in the game (HeldGearData), the base file keeps the tools' view the same."""
    for path, kw in ((GEAR, {"separators": (",", ":")}), (GEAR_MELEE, {"indent": 1})):
        data = json.load(open(path, encoding="utf-8"))
        rest = [e for e in data["anchors"] if not (e["anim"] == "idle" and e["dir"] == "down")]
        for part in ("main", "off"):
            rest.append({"part": part, "anim": "idle", "dir": "down", "frames": [anchor(part) for _ in range(n)]})
        data["anchors"] = rest
        open(path, "w", encoding="utf-8").write(json.dumps(data, **kw))
    print("anchors written: idle/down main", FISTS["main"], ANGLES["main"], "off", FISTS["off"], ANGLES["off"])


if __name__ == "__main__":
    preview_only = sys.argv[1:2] == ["preview"]
    out = os.environ.get("OUT", "Assets/ArtLibrary/classes/assassin/new-style/old/idle-down-row0")
    os.makedirs(out, exist_ok=True)
    sheet = build(not preview_only)
    sheet.save(f"{out}/idle-down-doll-check.png")
    print("check sheet", f"{out}/idle-down-doll-check.png", "(rows: painted, bare, fists)")

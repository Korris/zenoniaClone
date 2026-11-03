"""Warrior combo hits 1-3 with clear swing directions, made from the existing body art (no new drawing):

  melee1lr  hit 1, slash LEFT -> RIGHT   body: melee1 as drawn
  melee2rl  hit 2, slash RIGHT -> LEFT   body: melee1 played backwards (the fist travels the other way)
  hit 3 (melee3up, rising slash) is drawn new and installed by Tools/build-warrior-attack.py; not made here any more.
  hit 4 stays melee4.

For each new anim it writes body/{bare,armed,hand}/<anim>.png (6 x 4 cells of 192 px; columns reversed when the body runs
backwards), a .meta copied from the source sheet with a fresh guid (same import settings: uncompressed, max size), and the
sword + shield anchors in Data/classes/warrior/held-gear-melee.json: position, size and behind flag follow the source frame (the fist),
the sword ANGLE is replaced by the sweep below. Angles: degrees clockwise on screen, 0 = blade up, 90 = right, 180 = down, -90 = left.
The old melee1-3 stay untouched (Earth Break borrows melee1). Re-run after Tools/build-melee-sheets.py, which rewrites the anchor file.

  python Tools/build-warrior-combo-directions.py          write sheets + anchors
"""
import json, os, re, shutil, uuid
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BODY = os.path.join(ROOT, "Assets", "Resources", "Sprites", "classes", "warrior", "body")
ANCHORS = os.path.join(ROOT, "Assets", "Resources", "Data", "classes", "warrior", "held-gear-melee.json")
CELL, COLS = 192, 6
DIRS = ["down", "up", "left", "right"]  # sheet rows

# new anim: (source anim, play backwards, sword angle per frame for each facing)
# The blade stays on the side the fist is on (the body art is fixed): it must never cut through the head or the torso.
# Facing down, melee1's fist is on screen-left for frames 0-3 and centre-right for 4-5; facing up it stays on the right;
# facing right it is behind the back for 0-3 and low in front for 4-5. The big jump between two frames is the fast part of the swing.
SWEEP_LR = {"down": [-50, -95, -130, -160, 135, 100], "up": [-20, 10, 40, 70, 110, 140], "right": [-110, -85, -55, -25, 100, 130]}
SWEEP_RL = {"down": [100, 135, -160, -130, -95, -50], "up": [140, 110, 70, 40, 10, -20], "right": [130, 100, -25, -55, -85, -110]}
# melee2 backwards: the fist rises from low (right / front) to high (left / behind).
SWEEP_UP = {"down": [160, 130, 100, -80, -45, -15], "up": [150, 135, 120, 70, 35, 10], "right": [150, 130, 105, -20, 10, -10]}
NEW = {
    "melee1lr": ("melee1", False, SWEEP_LR),
    "melee2rl": ("melee1", True, SWEEP_RL),
}


def angles(sweep, d):
    """Facing left mirrors facing right."""
    return sweep[d] if d in sweep else [-a for a in sweep["right"]]


def copy_meta(src_png, dst_png):
    meta = open(src_png + ".meta", encoding="utf-8").read()
    meta = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta, count=1)
    if os.path.exists(dst_png + ".meta"):  # keep the guid Unity already knows
        old = re.search(r"guid: ([0-9a-f]{32})", open(dst_png + ".meta", encoding="utf-8").read())
        if old: meta = re.sub(r"guid: [0-9a-f]{32}", "guid: " + old.group(1), meta, count=1)
    open(dst_png + ".meta", "w", encoding="utf-8", newline="\n").write(meta)


def write_sheets():
    for name, (src, backwards, _) in NEW.items():
        for folder in ("bare", "armed", "hand"):
            sp = os.path.join(BODY, folder, src + ".png")
            if not os.path.exists(sp):
                continue
            im = Image.open(sp).convert("RGBA")
            out = Image.new("RGBA", im.size, (0, 0, 0, 0))
            for c in range(COLS):
                sc = COLS - 1 - c if backwards else c
                out.paste(im.crop((sc * CELL, 0, (sc + 1) * CELL, im.height)), (c * CELL, 0))
            dp = os.path.join(BODY, folder, name + ".png")
            out.save(dp)
            copy_meta(sp, dp)
            print("sheet", os.path.relpath(dp, ROOT))


def write_anchors():
    data = json.load(open(ANCHORS, encoding="utf-8"))
    anchors = [a for a in data["anchors"] if a["anim"] not in NEW]  # only this tool's anims: melee3up belongs to build-warrior-attack.py
    added = []
    for name, (src, backwards, sweep) in NEW.items():
        for a in data["anchors"]:
            if a["anim"] != src:
                continue
            frames = [dict(f) for f in (reversed(a["frames"]) if backwards else a["frames"])]
            if a["part"] == "sword":
                for f, ang in zip(frames, angles(sweep, a["dir"])):
                    f["angle"] = float(ang)
            added.append({"part": a["part"], "anim": name, "dir": a["dir"], "frames": frames})
    data["anchors"] = anchors + added
    with open(ANCHORS, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(data, fh, indent=1)
    print("anchors", len(added), "entries ->", os.path.relpath(ANCHORS, ROOT))


if __name__ == "__main__":
    write_sheets()
    write_anchors()

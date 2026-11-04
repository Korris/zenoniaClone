"""Composites hero frames the way HeroRenderer draws them, to look at sprite work without opening Unity:
gear marked behind, body/bare, gear in front, body/hand on top; anchors from held-*-melee.json over held-*.json, grip as pivot.
Falls back to the armed frame when an anchor is missing or marked fallback, like the game.

Usage (from the project root; needs Pillow and numpy):
  python Tools/compose-hero-frame.py OUT.png [--anim idle] [--dir down] [--zoom 3] [--classes warrior,wizard,...] [--bg 58,62,70]
      one row per class, one column per frame.
Importable: compose(cls, anim, dir, frame, bare_path=None, anchors=None, hand_path=None, pad=0) returns a (192+2*pad) square RGBA image."""
import argparse, json, math, os
import numpy as np
from PIL import Image

FRAME = 192
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
RES = "Assets/Resources"
# part id, weapon folder, DrawScale, MinSx, in draw order: copied from HeroClassDef.Parts.
CLASSES = {
    "warrior": ("held-gear", [("shield", "shield", 0.8, 0.0), ("sword", "sword", 2.0, 0.85)]),
    "wizard": ("held-wizard", [("staff", "staff", 1.0, 0.0)]),
    "mechanic": ("held-mechanic", [("gun", "gun", 1.0, 0.0)]),
    "assassin": ("held-assassin", [("off", "dagger", 1.3, 0.0), ("main", "dagger", 1.3, 0.0)]),
    "cleric": ("held-cleric", [("shield", "shield", 0.8, 0.0), ("staff", "staff", 1.0, 0.0)]),
    "fighter": ("held-fighter", [("off", "glove", 1.0, 0.0), ("main", "glove", 1.0, 0.0)]),  # HeroClassDef.Fighter: two gloves, DrawScale 1.0
}


def load_gear(cls):
    base = f"{RES}/Data/classes/{cls}/{CLASSES[cls][0]}"
    file = json.load(open(base + ".json", encoding="utf-8"))
    anchors = {(a["part"], a["anim"], a["dir"]): a["frames"] for a in file["anchors"]}
    if os.path.exists(base + "-melee.json"):
        for a in json.load(open(base + "-melee.json", encoding="utf-8"))["anchors"]:
            anchors[(a["part"], a["anim"], a["dir"])] = a["frames"]
    return file, anchors


def place(canvas, art, grip, a, draw_scale, min_sx, pad=0):
    """Pastes art so its grip lands on (a.x, a.y), rotated clockwise by a.angle, as HeroRenderer.ApplyPart does."""
    k = a["s"] * draw_scale
    raw = a.get("sx", 1) or 1
    sx = math.copysign(max(abs(raw), min_sx), raw)
    th = math.radians(a["angle"])
    cos, sin = math.cos(th), math.sin(th)
    # frame px -> art px, the inverse of translate(a) * rotate(th) * scale(k*sx, k) * translate(-grip)
    ax, bx = cos / (k * sx), sin / (k * sx)
    ay, by = -sin / k, cos / k
    px, py = a["x"] + pad, a["y"] + pad
    cx = grip["gripX"] - ax * px - bx * py
    cy = grip["gripY"] - ay * px - by * py
    over = art.transform(canvas.size, Image.AFFINE, (ax, bx, cx, ay, by, cy), Image.BILINEAR)
    canvas.alpha_composite(over)


def cell(path, row, frame):
    x, y = frame * FRAME, row * FRAME
    return Image.open(path).convert("RGBA").crop((x, y, x + FRAME, y + FRAME))


def compose(cls, anim, direction, frame, bare_path=None, anchors=None, hand_path=None, tier=0, pad=0):
    file, loaded = load_gear(cls)
    anchors = anchors if anchors is not None else loaded
    parts = CLASSES[cls][1]
    row = ROWS[direction]
    frames = [(p, (anchors.get((p[0], anim, direction)) or [None] * 99)[frame]) for p in parts]
    canvas = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
    bare = bare_path or f"{RES}/Sprites/classes/{cls}/body/bare/{anim}.png"
    if not os.path.exists(bare) or any(a is None or a.get("fallback") for _, a in frames):
        canvas.alpha_composite(cell(f"{RES}/Sprites/classes/{cls}/body/armed/{anim}.png", row, frame), (pad, pad))
        return canvas

    def gear(behind):
        for (pid, folder, scale, min_sx), a in frames:
            if a.get("hidden") or bool(a.get("behind")) != behind:
                continue
            entry = (file["sword"] if pid == "sword" else file["shield"] if pid == "shield" else file["weapon"])[tier]
            art = Image.open(f"{RES}/Sprites/classes/{cls}/weapons/{folder}/{entry['name']}-base.png").convert("RGBA")
            place(canvas, art, entry["baseGrip"], a, scale, min_sx, pad)

    gear(True)
    canvas.alpha_composite(cell(bare, row, frame), (pad, pad))
    gear(False)
    hand = hand_path or f"{RES}/Sprites/classes/{cls}/body/hand/{anim}.png"
    if os.path.exists(hand):
        canvas.alpha_composite(cell(hand, row, frame), (pad, pad))
    return canvas


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--anim", default="idle")
    ap.add_argument("--dir", default="down")
    ap.add_argument("--zoom", type=int, default=3)
    ap.add_argument("--classes", default=",".join(CLASSES))
    ap.add_argument("--bg", default="58,62,70")
    ap.add_argument("--pad", type=int, default=50, help="room around the 192 px cell, because gear is drawn past its edge in the game")
    args = ap.parse_args()
    classes = args.classes.split(",")
    bg = tuple(int(v) for v in args.bg.split(",")) + (255,)
    size = FRAME + 2 * args.pad
    sheet = Image.new("RGBA", (4 * size, len(classes) * size), bg)
    for r, cls in enumerate(classes):
        for f in range(4):
            sheet.alpha_composite(compose(cls, args.anim, args.dir, f, pad=args.pad), (f * size, r * size))
    sheet = sheet.resize((sheet.width * args.zoom, sheet.height * args.zoom), Image.NEAREST)
    sheet.convert("RGB").save(args.out)
    print(args.out, sheet.size)


if __name__ == "__main__":
    main()

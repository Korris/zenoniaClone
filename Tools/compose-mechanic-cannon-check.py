"""Contact sheet of the Mechanic composed like HeroRenderer.Show/ApplyPart (cannon on the back + two pistols), per tier, no Unity needed.
Mirrors the game's art lookup: an anchor with `art` draws <folder>/<tier>/<art>.png, else the shared <folder>/<art>.png (GearOverlays.CenteredTier);
an anchor without `art` draws <folder>/<tier>-base.png with the manifest grip. Prints every texture path the game would ask for that does not exist.

Usage (project root; Pillow):
  python Tools/compose-mechanic-cannon-check.py OUT.png [--anim idle] [--dirs right,left,down,up] [--tiers scrap,dragon,frost] [--zoom 2] [--frame 0]
  one row per tier, one column per direction. Tier names = manifest order of held-mechanic-dual.json (scrap, brass, copper, plasma, ember, frost, volt, gatling, star, dragon)."""
import argparse, json, math, os
from PIL import Image

FRAME = 192
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
RES = "Assets/Resources"
SPR = RES + "/Sprites/classes/mechanic"
# HeroClassDef.Mechanic.Parts, in order: id, folder (draw scale 1, no min sx)
PARTS = [("cannon", "weapons/cannon"), ("gunL", "weapons/pistol"), ("gun", "weapons/pistol")]
MIRROR_LEFT = {"attack3", "attack4", "rocket", "dash", "hurt", "death"}
MISSING = set()


def load_art(path):
    if not os.path.exists(path):
        return None
    return Image.open(path).convert("RGBA")


def tier_art(folder, art, tier):
    """GearOverlays.CenteredTier: tier folder first, then the shared art. Reports only what the game would log as missing."""
    img = load_art(f"{SPR}/{folder}/{tier}/{art}.png")
    if img is None:
        img = load_art(f"{SPR}/{folder}/{art}.png")
        if img is None:
            MISSING.add(f"{folder}/{art}")
    return img


def place(canvas, art, grip, a, pad):
    k = a["s"]
    raw = a.get("sx", 1) or 1
    sx = math.copysign(abs(raw), raw)
    th = math.radians(a["angle"])
    cos, sin = math.cos(th), math.sin(th)
    ax, bx = cos / (k * sx), sin / (k * sx)
    ay, by = -sin / k, cos / k
    px, py = a["x"] + pad, a["y"] + pad
    cx = grip[0] - ax * px - bx * py
    cy = grip[1] - ay * px - by * py
    canvas.alpha_composite(art.transform(canvas.size, Image.AFFINE, (ax, bx, cx, ay, by, cy), Image.BILINEAR))


def cell(path, row, frame, mirror=False):
    x, y = frame * FRAME, row * FRAME
    im = Image.open(path).convert("RGBA").crop((x, y, x + FRAME, y + FRAME))
    return im.transpose(Image.FLIP_LEFT_RIGHT) if mirror else im


def compose(file, anchors, anim, direction, frame, tier_i, pad=50):
    tier = file["weapon"][tier_i]["name"]
    mirror = direction == "left" and anim in MIRROR_LEFT
    row = 3 if mirror else ROWS[direction]
    canvas = Image.new("RGBA", (FRAME + 2 * pad,) * 2, (0, 0, 0, 0))
    layers = []  # (order, image)
    for i, (pid, folder) in enumerate(PARTS):
        a = (anchors.get((pid, anim, direction)) or [None] * 99)[frame]
        if a is None or a.get("fallback"):
            return None
        if a.get("hidden"):
            continue
        if a.get("art"):
            art = tier_art(folder, a["art"], tier)
            grip = (art.width / 2, art.height / 2) if art else None
        else:
            entry = file["weapon"][tier_i]
            path = f"{SPR}/{folder}/{entry['name']}-base.png"
            art = load_art(path)
            if art is None:
                MISSING.add(path)
            g = entry["baseGrip"]
            grip = (g["gripX"], g["gripY"])
        if art is None:
            continue
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        place(layer, art, grip, a, pad)
        layers.append(((1 if a.get("behind") else 4) + min(i, 1), layer))
    body = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    body.alpha_composite(cell(f"{SPR}/body/bare/{anim}.png", row, frame, mirror), (pad, pad))
    layers.append((3, body))
    hp = f"{SPR}/body/hand/{anim}.png"
    if os.path.exists(hp):
        hand = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        hand.alpha_composite(cell(hp, row, frame, mirror), (pad, pad))
        layers.append((6, hand))
    for _, im in sorted(layers, key=lambda t: t[0]):
        canvas.alpha_composite(im)
    return canvas


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--anim", default="idle")
    ap.add_argument("--dirs", default="right,left,down,up")
    ap.add_argument("--tiers", default="scrap,dragon,frost")
    ap.add_argument("--zoom", type=int, default=2)
    ap.add_argument("--frame", type=int, default=0)
    ap.add_argument("--bg", default="88,120,76")
    args = ap.parse_args()
    file = json.load(open(f"{RES}/Data/classes/mechanic/held-mechanic-dual.json", encoding="utf-8"))
    anchors = {(s["part"], s["anim"], s["dir"]): s["frames"] for s in file["anchors"]}
    names = [e["name"] for e in file["weapon"]]
    dirs, tiers = args.dirs.split(","), args.tiers.split(",")
    size = FRAME + 100
    sheet = Image.new("RGBA", (len(dirs) * size, len(tiers) * size), tuple(int(v) for v in args.bg.split(",")) + (255,))
    for r, t in enumerate(tiers):
        for c, d in enumerate(dirs):
            img = compose(file, anchors, args.anim, d, args.frame, names.index(t))
            if img is not None:
                sheet.alpha_composite(img, (c * size, r * size))
    sheet = sheet.resize((sheet.width * args.zoom, sheet.height * args.zoom), Image.NEAREST)
    sheet.convert("RGB").save(args.out)
    print(args.out, sheet.size, "missing:", sorted(MISSING) or "none")


if __name__ == "__main__":
    main()

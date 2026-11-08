"""Hairstyle check of the Assassin basic attack (docs/sprite-rules.md "Hình mẫu nhân vật", step 4): every frame of melee1-4 in the 4 facings
with each of the 5 hairstyles (heads/<n>), composited the way the game draws it: daggers behind, headless body (body/headless/<anim>.png),
the hairstyle head placed by head-anchors.json, daggers in front (held-assassin-melee.json, tier 0), fist layer (body/hand). Column 0 of each
row block is idle frame 0 of that facing with the same style, to compare the head size.

  python Tools/build-assassin-attack-hairstyles-check.py [OUT.png]      default docs/reference/assassin-attack-hairstyles-check.png
  python Tools/build-assassin-attack-hairstyles-check.py OUT.png scale  head scale check instead: idle vs attack1-4 x 4 facings, style 0,
                                                                        full body as the game draws it (docs/reference/assassin-attack-head-scale-check.png)
"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

os.chdir("D:/zenoniaClone")
sys.path.insert(0, "Tools")
import head_layer_dirs as hd  # noqa: E402


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


chf = load("chf", "Tools/compose-hero-frame.py")
CLS, FRAME, PAD = "assassin", 192, 40
ANIMS = ("melee1", "melee2", "melee3", "melee4")
DIRS = ("down", "right", "up", "left")
BODY = f"Assets/Resources/Sprites/classes/{CLS}/body"


def frame(out, anchors_gear, anim, d, f, style, file):
    """One composited frame (PAD around the 192 cell)."""
    v = out[(anim, d)]
    head = hd.head_of(CLS, style, d)
    body = hd.composite(v["headless"][f], head, v["anchors"][f]) if head is not None else v["headless"][f]
    body = Image.fromarray(np.clip(body, 0, 255).astype(np.uint8), "RGBA")
    size = FRAME + 2 * PAD
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    entry = file["weapon"][0]
    art = Image.open(f"Assets/Resources/Sprites/classes/{CLS}/weapons/dagger/{entry['name']}-base.png").convert("RGBA")
    parts = [(anchors_gear.get((p, anim, d)) or [None] * 9)[f] for p in ("off", "main")]

    def gear(behind):
        for a in parts:
            if a and not a.get("hidden") and bool(a.get("behind")) == behind:
                chf.place(canvas, art, entry["baseGrip"], a, 1.3, 0.0, PAD)
    gear(True)
    canvas.alpha_composite(body, (PAD, PAD))
    gear(False)
    hand = f"{BODY}/hand/{anim}.png"
    if os.path.exists(hand):
        canvas.alpha_composite(chf.cell(hand, chf.ROWS[d], f), (PAD, PAD))
    return canvas


def hairstyles(path):
    out = hd.read_out(CLS)
    file, anchors = chf.load_gear(CLS)
    size = FRAME + 2 * PAD
    rows = [(a, d, s) for a in ANIMS for d in DIRS for s in range(5)]
    sheet = Image.new("RGBA", (size * 7, size * len(rows)), (58, 62, 70, 255))
    dr = ImageDraw.Draw(sheet)
    for r, (a, d, s) in enumerate(rows):
        sheet.alpha_composite(frame(out, anchors, "idle", d, 0, s, file), (0, r * size))
        for f in range(len(out[(a, d)]["anchors"])):
            sheet.alpha_composite(frame(out, anchors, a, d, f, s, file), ((f + 1) * size, r * size))
        dr.text((4, r * size + 4), f"{a} {d} style {s}", fill=(255, 255, 0))
    sheet = sheet.resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS)
    sheet.convert("RGB").save(path)
    print(path, sheet.size)


def scale_check(path):
    out = hd.read_out(CLS)
    file, anchors = chf.load_gear(CLS)
    size = FRAME + 2 * PAD
    sheet = Image.new("RGBA", (size * 5, size * 4), (58, 62, 70, 255))
    dr = ImageDraw.Draw(sheet)
    for r, d in enumerate(DIRS):
        sheet.alpha_composite(frame(out, anchors, "idle", d, 0, 0, file), (0, r * size))
        for c, a in enumerate(ANIMS):
            sheet.alpha_composite(frame(out, anchors, a, d, 2, 0, file), ((c + 1) * size, r * size))
        dr.text((4, r * size + 4), f"{d}: idle | attack1-4 frame 3", fill=(255, 255, 0))
    sheet.convert("RGB").save(path)
    print(path, sheet.size)


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else "docs/reference/assassin-attack-hairstyles-check.png"
    if len(sys.argv) > 2 and sys.argv[2] == "scale":
        scale_check(target)
    else:
        hairstyles(target)

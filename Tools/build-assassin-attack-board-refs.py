"""Reference images sent to ChatGPT for the Assassin basic-attack board and the body repaint (2026-11-08, like the Mechanic / Wizard boards).

  python Tools/build-assassin-attack-board-refs.py look      attack-board/ref/ref-assassin-ingame-look.png:
                                                              idle down / right / up / left with the tier-0 daggers as the game draws them,
                                                              plus the current combo (melee1-4 right, strike frame), 3x nearest, on #00FF00
  python Tools/build-assassin-attack-board-refs.py idle DIR  attack-board/ref/idle-DIR-4x.png: idle frame 0 of facing DIR, bare body (no daggers),
                                                              4x nearest on #00FF00, with the measured head ratio printed (rule 8 of the ChatGPT art rules)
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image, ImageDraw

os.chdir("D:/zenoniaClone")
spec = importlib.util.spec_from_file_location("chf", "Tools/compose-hero-frame.py")
chf = importlib.util.module_from_spec(spec)
spec.loader.exec_module(chf)

OUT = "Assets/ArtLibrary/classes/assassin/attack-board/ref"
BODY = "Assets/Resources/Sprites/classes/assassin/body"
GREEN = (0, 255, 0, 255)


def head_ratio(cell):
    """Hair width in the top quarter of the figure / figure height (hair top to sole): violet hair of the Assassin."""
    a = np.asarray(cell).astype(int)
    al = a[..., 3] > 100
    ys = np.nonzero(al)[0]
    top, bot = ys.min(), ys.max()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    hair = al & (b > g + 25) & (r > g + 10)  # violet / purple hair, not the red headband
    xs = np.flatnonzero(hair[top:top + int(0.3 * (bot - top + 1))].sum(0) > 1)
    w = xs.max() - xs.min() + 1
    return w, bot - top + 1, w / (bot - top + 1)


def look():
    pad, z = 40, 2
    shots = [("idle", d, 0) for d in ("down", "right", "up", "left")] + [("melee%d" % i, "right", 2) for i in (1, 2, 3, 4)]
    size = 192 + 2 * pad
    sheet = Image.new("RGBA", (4 * size, 2 * size), GREEN)  # row 1: idle in four facings, row 2: the current combo facing right
    for i, (a, d, f) in enumerate(shots):
        sheet.alpha_composite(chf.compose("assassin", a, d, f, pad=pad), ((i % 4) * size, (i // 4) * size))
    sheet = sheet.resize((sheet.width * z, sheet.height * z), Image.NEAREST)
    sheet.convert("RGB").save(f"{OUT}/ref-assassin-ingame-look.png")
    print(f"{OUT}/ref-assassin-ingame-look.png", sheet.size)


def idle(d):
    r = chf.ROWS[d]
    cell = Image.open(f"{BODY}/bare/idle.png").convert("RGBA").crop((0, r * 192, 192, r * 192 + 192))
    w, h, q = head_ratio(cell)
    canvas = Image.new("RGBA", (192, 192), GREEN)
    canvas.alpha_composite(cell)
    canvas = canvas.resize((768, 768), Image.NEAREST)
    canvas.convert("RGB").save(f"{OUT}/idle-{d}-4x.png")
    print(d, "hair width", w, "height", h, "ratio %.3f" % q)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "look":
        look()
    elif cmd == "idle":
        for d in sys.argv[2:] or ["down", "right", "up", "left"]:
            idle(d)
    else:
        print(__doc__)

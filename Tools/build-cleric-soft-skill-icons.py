"""Cleric skill icons, RO Soft Fantasy (20/11/2026): cut the ChatGPT sheet of 8 round icons and preview them like the RO Soft HUD draws them.

  py -3 Tools/build-cleric-soft-skill-icons.py cut <sheet.png>
      sheet = 4 columns x 2 rows of painted discs on flat #FF00FF (prompt: ArtLibrary/classes/cleric/skill-icons-src/prompt-soft-fantasy-icons.md).
      Background = near-magenta pixels connected to the image border (purple smoke inside a disc is kept), magenta spill removed at the rim.
      Writes Assets/Resources/Sprites/classes/cleric/skills/<slug>/icon.png (128 px, same files / .meta as before, order = SkillTable index 0-7)
      and the keyed 8-icon strip to ArtLibrary/classes/cleric/skill-icons-src/cleric-skill-icons-soft-fantasy-keyed.png.
  py -3 Tools/build-cleric-soft-skill-icons.py preview <out.png>
      the installed 128 px icons on a light panel, then cut round at 96 / 64 px over grass from the Willowbrook screenshot
      (quick art check only; the real HUD ring / mask is checked in game). Run before `cut` for the "before" image, after it for "after".
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SKILLS = ROOT / "Assets/Resources/Sprites/classes/cleric/skills"
SRC = ROOT / "Assets/ArtLibrary/classes/cleric/skill-icons-src"
BG_SHOT = ROOT / "docs/reference/cleric-fx-ro3/willowbrook-meadows-ingame-cleric.jpg"
SLUGS = ["heal", "spirit-blessing", "divine-aegis", "divine-light", "sanctuary", "guardian-spirit", "purification", "divine-ascension"]
KEY = np.array([255, 0, 255], dtype=np.float32)
SIZE = 128


def key_out(im):
    """RGBA with the border-connected magenta made transparent (soft 2-step edge) and magenta spill pulled out of the rim."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    dist = np.sqrt(((rgb - KEY) ** 2).sum(axis=2))
    near = dist < 150
    lab, _ = ndimage.label(near)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    alpha = np.where(bg, np.clip((dist - 60) / 90, 0, 1), 1.0)
    rim = ndimage.binary_dilation(bg, iterations=3) & ~bg
    # spill: magenta = high R and B over G; clamp R and B toward G on the rim and on semi-transparent pixels
    spill = rim | (alpha < 1)
    g = rgb[..., 1]
    for c in (0, 2):
        ch = rgb[..., c]
        rgb[..., c] = np.where(spill & (ch > g), g + (ch - g) * 0.35, ch)
    out = np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(out, "RGBA"), ~bg


def discs(fg):
    """Bounding boxes of the 8 biggest foreground blobs, row-major (2 rows of 4)."""
    lab, n = ndimage.label(ndimage.binary_opening(fg, iterations=2))
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    keep = np.argsort(sizes)[::-1][:8] + 1
    if len(keep) < 8:
        sys.exit("found %d icons, need 8" % len(keep))
    boxes = [s for i, s in enumerate(ndimage.find_objects(lab)) if i + 1 in keep]
    boxes = [(b[1].start, b[0].start, b[1].stop, b[0].stop) for b in boxes]
    mid = np.median([(b[1] + b[3]) / 2 for b in boxes])
    top = sorted([b for b in boxes if (b[1] + b[3]) / 2 < mid], key=lambda b: b[0])
    bot = sorted([b for b in boxes if (b[1] + b[3]) / 2 >= mid], key=lambda b: b[0])
    if len(top) != 4 or len(bot) != 4:
        sys.exit("rows are %d + %d, need 4 + 4" % (len(top), len(bot)))
    return top + bot


def square(im, box, pad=2):
    x0, y0, x1, y1 = box
    side = max(x1 - x0, y1 - y0) + 2 * pad
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    sq.paste(im.crop((int(cx - side / 2), int(cy - side / 2), int(cx - side / 2) + side, int(cy - side / 2) + side)), (0, 0))
    return sq.resize((SIZE, SIZE), Image.LANCZOS)


def cut(sheet):
    im = Image.open(sheet)
    keyed, fg = key_out(im)
    icons = [square(keyed, b) for b in discs(fg)]
    strip = Image.new("RGBA", (SIZE * 8 + 7 * 8, SIZE), (0, 0, 0, 0))
    for i, (slug, icon) in enumerate(zip(SLUGS, icons)):
        icon.save(SKILLS / slug / "icon.png")
        strip.alpha_composite(icon, (i * (SIZE + 8), 0))
        print("wrote", SKILLS / slug / "icon.png")
    strip.save(SRC / "cleric-skill-icons-soft-fantasy-keyed.png")


def round_crop(icon, size):
    """The icon cut to a circle at `size` px (approximation of the round HUD / skill-window button, without the ring)."""
    ic = icon.resize((size, size), Image.LANCZOS)
    m = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(m).ellipse((0, 0, size * 4 - 1, size * 4 - 1), fill=255)
    a = np.asarray(ic.getchannel("A"), dtype=np.float32) * np.asarray(m.resize((size, size), Image.LANCZOS), dtype=np.float32) / 255
    ic.putalpha(Image.fromarray(a.astype(np.uint8)))
    return ic


def preview(out):
    icons = [Image.open(SKILLS / s / "icon.png").convert("RGBA") for s in SLUGS]
    bg = Image.open(BG_SHOT).convert("RGBA")
    w = 8 * 136 + 16
    sheet = Image.new("RGBA", (w, 136 + 112 + 80), (0, 0, 0, 255))
    # row 1: bare icons on the HUD's light panel colour
    sheet.paste(Image.new("RGBA", (w, 136), (238, 243, 248, 255)), (0, 0))
    for i, ic in enumerate(icons):
        sheet.alpha_composite(ic, (8 + i * 136, 4))
    # rows 2-3: HUD buttons at 96 and 64 px over the grass / path of the in-game screenshot (same crop under every button)
    tile = bg.crop((380, 420, 380 + w // 2, 420 + 96)).resize((w, 192))
    sheet.paste(tile, (0, 136))
    for i, ic in enumerate(icons):
        sheet.alpha_composite(round_crop(ic, 96), (20 + i * 136, 144))
        sheet.alpha_composite(round_crop(ic, 64), (36 + i * 136, 252))
    sheet.convert("RGB").save(out)
    print("wrote", out)


if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == "cut":
        cut(sys.argv[2])
    elif len(sys.argv) == 3 and sys.argv[1] == "preview":
        preview(sys.argv[2])
    else:
        print(__doc__)

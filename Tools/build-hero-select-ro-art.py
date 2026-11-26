"""Hero select (RO Soft Adventure) art steps that are not plain sheet cuts. Sources are ChatGPT's (Assets/ArtLibrary/ui/hero-select-ro/src/),
outputs go to Assets/Resources/Sprites/ui/hero-select-ro/ (the UI pieces themselves: Tools/cut-ui-paper-sheet.py Tools/ui-ro-soft-sheets/hero-select-ro-*.json).

  py Tools/build-hero-select-ro-art.py portraits   six class portraits from the painted 2 x 3 sheet (#FF00FF gutters, Warrior .. Archer row by row),
                                                   cropped to the portrait window's aspect (centred, kept from the top) -> portrait-<class>.png
  py Tools/build-hero-select-ro-art.py bg          the courtyard softened as ChatGPT's review asked (blur ~2 px at 720p, saturation -10 %, contrast -8 %) -> bg.png
  py Tools/build-hero-select-ro-art.py greys       bar-fill / picked-ring turned to light grey ramps (bar-fill-white, picked-ring-white) so code tints them
                                                   (stat colours, warm gold ring) without muddying the painted colour
  py Tools/build-hero-select-ro-art.py ground      the preview's painted ground (hero-select/preview-ground) 12 % brighter -> preview-ground.png
  py Tools/build-hero-select-ro-art.py all
"""
import importlib, os, sys
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/ui/hero-select-ro/src"
OUT = f"{ROOT}/Assets/Resources/Sprites/ui/hero-select-ro"
CLASSES = ["warrior", "wizard", "mechanic", "assassin", "cleric", "archer"]
ASPECT = 152 / 182          # portrait window of HeroSelectScreen.Panels (w / h)
OUT_H, INSET = 368, 6

sys.path.insert(0, f"{ROOT}/Tools")
cut = importlib.import_module("cut-ui-paper-sheet")


def save(im, name):
    path = f"{OUT}/{name}.png"
    im.save(path)
    cut.write_ui_meta(path)
    print(path, im.size)


def runs(mask):
    """[start, end) runs of True in a 1-D mask, longer than 40 px."""
    out, start = [], None
    for i, v in enumerate(list(mask) + [False]):
        if v and start is None: start = i
        if not v and start is not None:
            if i - start > 40: out.append((start, i))
            start = None
    return out


def portraits():
    sheet = f"{SRC}/hero-select-ro-portraits-chatgpt-1.png"
    a = np.asarray(Image.open(sheet).convert("RGB")).astype(int)
    magenta = (a[..., 0] > 200) & (a[..., 1] < 80) & (a[..., 2] > 200)
    rows, cols = runs(magenta.mean(axis=1) < 0.5), runs(magenta.mean(axis=0) < 0.5)
    cells = [(x0, y0, x1, y1) for (y0, y1) in rows for (x0, x1) in cols]
    if len(cells) != len(CLASSES): raise SystemExit(f"found {len(cells)} cells ({len(rows)} rows x {len(cols)} cols), expected 6")
    im = Image.open(sheet).convert("RGBA")
    for name, (x0, y0, x1, y1) in zip(CLASSES, cells):
        x0, y0, x1, y1 = x0 + INSET, y0 + INSET, x1 - INSET, y1 - INSET
        w, h = x1 - x0, y1 - y0
        if w / h > ASPECT:
            nw = int(h * ASPECT); x0 += (w - nw) // 2; x1 = x0 + nw
        else:
            y1 = y0 + int(w / ASPECT)
        save(im.crop((x0, y0, x1, y1)).resize((int(OUT_H * ASPECT), OUT_H), Image.LANCZOS), f"portrait-{name}")


def bg():
    im = Image.open(f"{SRC}/hero-select-ro-bg-chatgpt-1.png").convert("RGB")
    im = im.filter(ImageFilter.GaussianBlur(2.6))          # ~2 px once the 1672 px picture covers a 1280 x 720 screen
    im = ImageEnhance.Color(im).enhance(0.9)
    im = ImageEnhance.Contrast(im).enhance(0.92)
    save(im, "bg")


def grey(name, out, lift):
    """Luminance of a cut piece, lifted towards white (alpha kept): multiplied by a colour in code it shows that colour with the painted shading."""
    im = Image.open(f"{OUT}/{name}.png").convert("RGBA")
    a = np.asarray(im).astype(np.float32)
    lum = a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114
    lum = lum / max(1.0, lum[a[..., 3] > 128].max() if (a[..., 3] > 128).any() else 255.0) * 255.0
    lum = lift + (255.0 - lift) * lum / 255.0
    o = a.copy()
    o[..., 0] = o[..., 1] = o[..., 2] = np.clip(lum, 0, 255)
    save(Image.fromarray(o.astype(np.uint8), "RGBA"), out)


def greys():
    grey("bar-fill", "bar-fill-white", 70)
    grey("picked-ring", "picked-ring-white", 120)


def ground():
    im = Image.open(f"{ROOT}/Assets/Resources/Sprites/ui/hero-select/preview-ground.png").convert("RGBA")
    rgb = ImageEnhance.Brightness(im.convert("RGB")).enhance(1.12)
    rgb.putalpha(im.getchannel("A"))
    save(rgb, "preview-ground")


def main():
    step = sys.argv[1] if len(sys.argv) > 1 else "all"
    for name, fn in (("portraits", portraits), ("bg", bg), ("greys", greys), ("ground", ground)):
        if step in (name, "all"): fn()


if __name__ == "__main__":
    main()

"""Character flow UI pieces (Character List / Customize and Name screens, 2026-11-12) cut from one ChatGPT sheet on flat magenta.

Input   Assets/ArtLibrary/custom/character-flow/character-flow-ui-parts-chatgpt-1.png  (prompt: prompt-02-ui-parts.txt)
Output  Assets/Resources/Sprites/ui/character-flow/<name>.png (+ .meta copied from ui/win-btn-gold.png with a new guid)
        pieces found as blobs, read row by row, left to right, and named by NAMES; swatch.png is drawn here (white disc, tinted in code)

  py Tools/build-character-flow-ui-parts.py check OUT.png   pieces on dark + light, no game files
  py Tools/build-character-flow-ui-parts.py write
"""
import os, re, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/custom/character-flow/character-flow-ui-parts-chatgpt-1.png"
OUT = f"{ROOT}/Assets/Resources/Sprites/ui/character-flow"
META = f"{ROOT}/Assets/Resources/Sprites/ui/win-btn-gold.png.meta"
NAMES = [["pedestal", "pedestal-glow", "create-slot"],
         ["btn-red", "btn-navy", "btn-gold", "dice", "icon-class", "icon-level", "icon-location"],
         ["swatch-ring", "slot", "slot-selected"]]


def keyed():
    """Magenta -> transparent. Soft glows (blue ring, gold slot) keep a partial alpha from their distance to magenta; the magenta
    spill on rims is pulled out of the colour."""
    a = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = np.clip((np.minimum(r, b) - g) / 255.0, 0, 1)             # 1 on pure magenta, 0 on anything with green in it
    alpha = np.clip((1 - mag) * 1.25 - 0.08, 0, 1)
    spill = np.clip(np.minimum(r, b) - g, 0, None) * (alpha < 0.999)
    out = np.dstack([r - spill * 0.9, g, b - spill * 0.9, alpha * 255])
    out[alpha < 0.06] = 0
    return np.clip(out, 0, 255).astype(np.uint8)


def pieces():
    img = keyed()
    solid = ndi.binary_closing(img[..., 3] > 40, iterations=6)
    lab, n = ndi.label(solid)
    boxes = [s for i, s in enumerate(ndi.find_objects(lab)) if (lab[s] == i + 1).sum() > 600]
    boxes.sort(key=lambda s: s[0].start)
    rows, cur = [], [boxes[0]]
    for s in boxes[1:]:                                              # a new row starts below the previous row's middle
        if s[0].start > (cur[0][0].start + cur[0][0].stop) / 2 and s[0].start > max(c[0].stop for c in cur) - 40: rows.append(cur); cur = [s]
        else: cur.append(s)
    rows.append(cur)
    out = {}
    for names, row in zip(NAMES, rows):
        row.sort(key=lambda s: s[1].start)
        if len(row) != len(names):
            sys.exit(f"row of {len(row)} pieces, expected {names}")
        for name, (ys, xs) in zip(names, row):
            y0, y1, x0, x1 = max(0, ys.start - 10), ys.stop + 10, max(0, xs.start - 10), xs.stop + 10   # room for the glow
            out[name] = Image.fromarray(img[y0:y1, x0:x1])
    d = Image.new("RGBA", (64, 64)); ImageDraw.Draw(d).ellipse((3, 3, 60, 60), fill=(255, 255, 255, 255), outline=(40, 40, 50, 255), width=3)
    out["swatch"] = d
    return out


def check(path):
    ps = pieces()
    w = sum(p.width + 20 for p in ps.values()); h = max(p.height for p in ps.values()) + 20
    sheet = Image.new("RGBA", (w, h * 2), (30, 34, 44, 255)); sheet.paste((225, 225, 215, 255), (0, h, w, h * 2))
    x = 10
    for name, p in ps.items():
        sheet.alpha_composite(p, (x, 10)); sheet.alpha_composite(p, (x, h + 10)); print(name, p.size); x += p.width + 20
    sheet.convert("RGB").save(path); print(path)


def write():
    os.makedirs(OUT, exist_ok=True)
    meta = open(META, encoding="utf-8").read()
    for name, p in pieces().items():
        png = f"{OUT}/{name}.png"; p.save(png)
        if not os.path.exists(png + ".meta"):                        # keep the guid Unity already knows
            open(png + ".meta", "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta, count=1))
    print("wrote", OUT)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "check": check(sys.argv[2])
    elif cmd == "write": write()
    else: print(__doc__)

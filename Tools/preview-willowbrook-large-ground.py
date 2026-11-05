"""Debug views of the large Willowbrook ground: stitched chunks -> full composite (plain / with rows overlay) + 1:1 crops.
Usage: python Tools/preview-willowbrook-large-ground.py  (writes to plans/261105-1052-willowbrook-large-hunting-map/visuals)"""
import os
import numpy as np
from PIL import Image, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLAN = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
OUT = os.path.join(PLAN, "visuals")
full = Image.new("RGB", (4608, 3072))
for r in range(2):
    for c in range(2):
        full.paste(Image.open(os.path.join(TILES, f"willowbrook-large-ground-r{r}c{c}.png")), (c * 2304, r * 1536))
rows = [l.rstrip("\n") for l in open(os.path.join(PLAN, "layout", "willowbrook-72x48-rows.txt")) if l.strip()]
col = {"T": (20, 40, 20, 120), "w": (40, 90, 255, 90), "B": (150, 90, 20, 140), "d": (255, 220, 120, 60)}

def save(im, name, q=88):
    p = os.path.join(OUT, name); im.convert("RGB").save(p.replace(".png", ".jpg") if False else p, optimize=True)
    print(name, os.path.getsize(p) // 1024, "KB")

def overlay(im, scale):
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov); t = 64 * scale
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in col: d.rectangle([x * t, y * t, (x + 1) * t - 1, (y + 1) * t - 1], fill=col[ch])
    for i in range(0, 73): d.line([i * t, 0, i * t, 48 * t], fill=(0, 0, 0, 50))
    for i in range(0, 49): d.line([0, i * t, 72 * t, i * t], fill=(0, 0, 0, 50))
    return Image.alpha_composite(im.convert("RGBA"), ov)

small = full.resize((1152, 768), Image.LANCZOS)
save(small, "ground-large-full.png")
save(overlay(small, 1 / 4), "ground-large-full-rows.png")
crops = {"crop-old-new-border-north": (1100, 560), "crop-stream-junction-east": (1900, 1700), "crop-new-bridge": (2200, 1700),
         "crop-road-bend": (2700, 1300), "crop-ruins": (3400, 150), "crop-farm-pond": (600, 2250), "crop-forest-edge": (2300, 700)}
for n, (x, y) in crops.items():
    save(full.crop((x, y, x + 1100, y + 700)), n + ".png")

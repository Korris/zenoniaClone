"""Classifies the 36x24 old section of Frost Hollow from the ChatGPT ground-only image (not from the old rows).
Auto pass (colour features per 42.667 px tile) + hand overrides in OVERRIDES; writes rows txt and a gridded overlay.
Usage: python classify-old-section-from-ground-only.py [--out DIR]
"""
import sys, numpy as np
from PIL import Image, ImageDraw, ImageFont
SRC = r"Assets/ArtLibrary/hd/map/frost-hollow/large/ground-only-chatgpt.png"
OUT = r"plans/261105-1240-frost-hollow-large-map"
im = Image.open(SRC).convert("RGB"); a = np.asarray(im).astype(float)
R, G, B = a[..., 0], a[..., 1], a[..., 2]
mx, mn = a.max(2), a.min(2); sat = (mx - mn) / np.maximum(mx, 1)
water = (B > R + 40) & (B > 120) & (sat > 0.35)
rock = (mx < 165) & (sat < 0.40) & (~water)            # dark grey-brown cliff faces / cliff shadows
path = (mx >= 150) & (mx < 200) & (sat < 0.12)         # lighter flat grey path stones
wood = (R > B + 30) & (R > 80) & (sat > 0.35) & (~water)
TS = 1536 / 36
rows = []
feat = {}
for ty in range(24):
    line = ""
    for tx in range(36):
        x0, y0 = int(tx * TS), int(ty * TS); x1, y1 = int((tx + 1) * TS), int((ty + 1) * TS)
        sl = (slice(y0, y1), slice(x0, x1))
        fw, fr, fp, fwood = water[sl].mean(), rock[sl].mean(), path[sl].mean(), wood[sl].mean()
        feat[(tx, ty)] = (fw, fr, fp, fwood)
        c = "s"
        if fw > 0.35: c = "w"
        elif fr > 0.22: c = "C"
        line += c
    rows.append(line)
# hand overrides: (tx, ty) -> char; filled after looking at the overlay
OVERRIDES = {}
exec(open(OUT + "/layout/old-section-overrides.py").read()) if __import__("os").path.exists(OUT + "/layout/old-section-overrides.py") else None
rows = [list(r) for r in rows]
for (tx, ty), c in OVERRIDES.items(): rows[ty][tx] = c
rows = ["".join(r) for r in rows]
open(OUT + "/layout/frost-hollow-ground-only-36x24-rows.txt", "w").write("\n".join(rows) + "\n")
# overlay at 2x
S = 2
big = im.resize((im.width * S, im.height * S), Image.NEAREST).convert("RGBA")
ov = Image.new("RGBA", big.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
ts = TS * S
col = {"C": (255, 0, 0, 90), "w": (0, 90, 255, 90), "B": (255, 200, 0, 110), "T": (255, 0, 255, 90)}
for ty in range(24):
    for tx in range(36):
        c = rows[ty][tx]
        if c in col: d.rectangle([tx * ts, ty * ts, (tx + 1) * ts, (ty + 1) * ts], fill=col[c])
f = ImageFont.load_default()
for i in range(37):
    d.line([i * ts, 0, i * ts, big.height], fill=(0, 0, 0, 140), width=1)
for i in range(25):
    d.line([0, i * ts, big.width, i * ts], fill=(0, 0, 0, 140), width=1)
for tx in range(36): d.text((tx * ts + 3, 2), str(tx), fill=(0, 0, 0, 255), font=f)
for ty in range(24): d.text((2, ty * ts + 3), str(ty), fill=(0, 0, 0, 255), font=f)
out = Image.alpha_composite(big, ov).convert("RGB")
out.save(OUT + "/visuals/frost-hollow-ground-only-rows-overlay.png")
for name, box in {"nw": (0, 0, 1536, 1024), "ne": (1536, 0, 3072, 1024), "sw": (0, 1024, 1536, 2048), "se": (1536, 1024, 3072, 2048)}.items():
    out.crop(box).save(OUT + "/visuals/frost-hollow-ground-only-rows-overlay-%s.png" % name)
for i, r in enumerate(rows): print("%2d %s" % (i, r))

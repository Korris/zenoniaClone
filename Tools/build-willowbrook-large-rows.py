"""Collision rows (72x48) of the painted Willowbrook Meadows, written to plans/.../layout/willowbrook-72x48-rows-painted.txt.
Usage: python Tools/build-willowbrook-large-rows.py [--overlay]   (needs the assembled ground chunks from build-willowbrook-large-painted.py)
 Water (w) is read from the painting (blue pixels, >= 30% of the tile); the old 36x24 section keeps its hand-classified rows from
 WillowbrookMeadowsMap.cs shifted +12 rows (its old border/HUD cells, now interior, come from the painting); the new ring is T:
 top 2 rows, bottom 3 rows (under the HUD), west col 0 (gate rows 23-24 open), east cols 70-71. Bridge B only exists in the old section."""
import os, re, sys
import numpy as np
from PIL import Image, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PLAN = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map")
OLD_ROWS = os.path.join(PLAN, "layout", "willowbrook-36x24-old-rows.txt")
OUT = os.path.join(PLAN, "layout", "willowbrook-72x48-rows-painted.txt")
C, R = 72, 48

def ground():
    full = Image.new("RGB", (4608, 3072))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"willowbrook-large-ground-r{r}c{c}.png")).convert("RGB"), (c * 2304, r * 1536))
    return full

def build(full):
    a = np.asarray(full).astype(np.int16)
    water = (((a[..., 0] - a[..., 2]) < 45) & (a[..., 2] > 60)).astype(np.float32)
    frac = water.reshape(R, 64, C, 64).mean((1, 3))
    rows = [["w" if frac[y, x] >= 0.30 else "g" for x in range(C)] for y in range(R)]
    frac[2:13, 52:68] = 0.0   # the paved ruin area at the north-east is grey like water but walkable
    rows = [["w" if frac[y, x] >= 0.35 else "g" for x in range(C)] for y in range(R)]
    old = [l.rstrip("\n") for l in open(OLD_ROWS) if l.strip()]
    assert len(old) == 24 and all(len(l) == 36 for l in old)
    for oy in range(24):
        for ox in range(36):
            border = oy == 0 or ox == 35 or oy >= 21
            if not border or ox == 0: rows[oy + 12][ox] = old[oy][ox]
    # cells the painting says are water inside the old section's opened border keep that (frac); old T cliffs/posts stay T
    for y in range(R):
        for x in range(C):
            if y < 2 or y >= 45 or x >= 70: rows[y][x] = "T"
            if x == 0 and y not in (23, 24): rows[y][x] = "T"
            if x == 0 and y in (23, 24): rows[y][x] = "g"
    # old gate neighbourhood: old row 12 col 1 'T' (palisade post) stays via old rows; opened ring row 12 (new 24) col 0 g
    return ["".join(r) for r in rows]

if __name__ == "__main__":
    full = ground(); rows = build(full)
    open(OUT, "w").write("\n".join(rows) + "\n")
    print("\n".join(rows))
    if "--overlay" in sys.argv:
        ov = Image.new("RGBA", full.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
        col = {"T": (10, 20, 10, 150), "w": (30, 80, 255, 110), "B": (150, 90, 20, 150)}
        for y, r in enumerate(rows):
            for x, ch in enumerate(r):
                if ch in col: d.rectangle([x * 64, y * 64, x * 64 + 63, y * 64 + 63], fill=col[ch])
        for i in range(73): d.line([i * 64, 0, i * 64, 3072], fill=(0, 0, 0, 45))
        for i in range(49): d.line([0, i * 64, 4608, i * 64], fill=(0, 0, 0, 45))
        Image.alpha_composite(full.convert("RGBA"), ov).convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(PLAN, "visuals", "rows-painted-overlay.jpg"), quality=88)

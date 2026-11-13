"""Collision rows (72x48) of the painted Willowbrook Meadows, written to plans/.../layout/willowbrook-72x48-rows-painted.txt.
Usage: python Tools/build-willowbrook-large-rows.py [--overlay]   (needs the assembled ground chunks from build-willowbrook-large-painted.py)
       python Tools/build-willowbrook-large-rows.py --redraw [--overlay]   (ground from build-large-ground-from-redraw-quarters.py, 13/11)
 --redraw: the redrawn ground keeps the layout but its stream is narrower and the pond sits a few tiles off. The rows of WillowbrookMeadowsMap.cs are the
 base (T and B stay); every w/g tile is re-read from the painting (saturated blue or waterfall foam >= 22% of the tile), then the hand edits of
 layout/willowbrook-72x48-redraw-overrides.txt apply ("x0 y0 x1 y1 char", inclusive, # comments, later lines win). Prints a flood fill from the start
 tile: with the bridge closed nothing east of the stream may be reachable (the stream has no gap), with it open everything walkable must be.
 Output: layout/willowbrook-72x48-rows-redraw.txt (paste into WillowbrookMeadowsMap.Rows) + visuals/rows-redraw-overlay*.jpg.
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

def build_redraw(full):
    a = np.asarray(full).astype(np.int16); r, g, b = a[..., 0], a[..., 1], a[..., 2]
    blue = (b > r + 40) & (r < 120) & (b > 95)                 # lilac flowers and the grey ruin paving fail r < 120 / b > r + 40
    foam = (b > 200) & (g > 190) & (b >= r) & (r > 150)        # waterfalls: white-blue; only counted next to blue water
    near = np.asarray(Image.fromarray((blue * 255).astype(np.uint8)).resize((C * 8, R * 8), Image.BOX).resize(full.size, Image.BILINEAR)) > 40
    frac = (blue | (foam & near)).astype(np.float32).reshape(R, 64, C, 64).mean((1, 3))
    cur = re.findall(r'"([TgwBd]{72})"', open(os.path.join(ROOT, "Assets", "Scripts", "Maps", "WillowbrookMeadowsMap.cs"), encoding="utf8").read())
    assert len(cur) == R
    rows = [[ch if ch in "TB" else ("w" if frac[y, x] >= 0.22 else "g") for x, ch in enumerate(cur[y])] for y in range(R)]
    ov = os.path.join(PLAN, "layout", "willowbrook-72x48-redraw-overrides.txt")
    if os.path.exists(ov):
        for ln in open(ov, encoding="utf8"):
            ln = ln.split("#")[0].split()
            if len(ln) != 5: continue
            x0, y0, x1, y1 = map(int, ln[:4])
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1): rows[y][x] = ln[4]
    return ["".join(r) for r in rows]


def flood(rows, walk):
    seen = {(3, 23)}; q = [(3, 23)]
    while q:
        x, y = q.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= n[0] < C and 0 <= n[1] < R and n not in seen and rows[n[1]][n[0]] in walk: seen.add(n); q.append(n)
    return seen


def redraw_main():
    full = ground(); rows = build_redraw(full)
    open(os.path.join(PLAN, "layout", "willowbrook-72x48-rows-redraw.txt"), "w").write("\n".join(rows) + "\n")
    print("\n".join("%2d %s" % (i, r) for i, r in enumerate(rows)))
    closed, opened = flood(rows, "g"), flood(rows, "gB")
    walk = [(x, y) for y in range(R) for x in range(C) if rows[y][x] in "gB"]
    print("bridge closed: reachable tiles east of the stream:", sum(1 for x, y in closed if x > 50), "(must be 0)")
    lost = [t for t in walk if t not in opened]
    print("bridge open: walkable %d, unreachable %d %s" % (len(walk), len(lost), lost[:30]))
    if "--overlay" in sys.argv:
        big = full.resize((C * 32, R * 32), Image.LANCZOS).convert("RGBA"); ov = Image.new("RGBA", big.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
        col = {"T": (255, 0, 0, 100), "w": (255, 0, 0, 100), "B": (255, 200, 0, 120)}
        for y in range(R):
            for x in range(C):
                ch = rows[y][x]
                if ch in col: d.rectangle([x * 32, y * 32, x * 32 + 31, y * 32 + 31], fill=col[ch])
                elif (x, y) not in opened: d.rectangle([x * 32, y * 32, x * 32 + 31, y * 32 + 31], fill=(255, 0, 255, 150))
        for i in range(C + 1): d.line([i * 32, 0, i * 32, big.height], fill=(0, 0, 0, 70))
        for i in range(R + 1): d.line([0, i * 32, big.width, i * 32], fill=(0, 0, 0, 70))
        for x in range(C): d.text((x * 32 + 2, 1), str(x), fill=(255, 255, 255, 255))
        for y in range(R): d.text((1, y * 32 + 2), str(y), fill=(255, 255, 255, 255))
        out = Image.alpha_composite(big, ov).convert("RGB")
        out.resize((1536, 1024), Image.LANCZOS).save(os.path.join(PLAN, "visuals", "rows-redraw-overlay.jpg"), quality=88)
        for n, (cx, cy) in {"nw": (0, 0), "ne": (1, 0), "sw": (0, 1), "se": (1, 1)}.items():
            out.crop((cx * 36 * 32, cy * 24 * 32, (cx + 1) * 36 * 32, (cy + 1) * 24 * 32)).save(os.path.join(PLAN, "visuals", "rows-redraw-overlay-%s.jpg" % n), quality=88)


if __name__ == "__main__":
    if "--redraw" in sys.argv: redraw_main(); sys.exit()
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

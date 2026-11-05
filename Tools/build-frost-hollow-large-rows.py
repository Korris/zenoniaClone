"""Collision rows (72x48) of the painted Frost Hollow -> plans/.../layout/frost-hollow-72x48-rows-painted.txt (phase 4).
Usage: python Tools/build-frost-hollow-large-rows.py [--overlay]   (reads the chunks written by build-frost-hollow-large-painted.py)
 Per 64 px tile (4608x3072 picture), same colour features as layout/classify-old-section-from-ground-only.py:
   w river water (cyan, saturated)  C dark grey-brown cliff face  s everything else (snow, paths, frozen lake = walkable ice, paved floors)
 then hand edits from layout/frost-hollow-72x48-overrides.txt (lines "x0 y0 x1 y1 char", inclusive tile rect, # comments; later lines win):
   B bridge decks, stairs, cave mouths, cliff islands, opened rims. The ring is forced next: row 0, rows 45-47 (under the HUD), cols 70-71, col 0 from row 22 down.
   Last, walkable tiles not reachable from the start tile (cliff-top plateau, specks) are sealed to C. The Den portal is the ice cave mouth (67-69,20).
 --overlay writes visuals/rows-painted-overlay*.png (gridded, numbered) and prints a flood fill from the start tile (4,21).
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "tiles")
PLAN = os.path.join(ROOT, "plans", "261105-1240-frost-hollow-large-map")
LAYOUT = os.path.join(PLAN, "layout")
OUT = os.path.join(LAYOUT, "frost-hollow-72x48-rows-painted.txt")
OVERRIDES = os.path.join(LAYOUT, "frost-hollow-72x48-overrides.txt")
OLD_ROWS = os.path.join(LAYOUT, "frost-hollow-ground-only-36x24-rows.txt")   # hand-fixed classification of the ground-only painting (old section)
C, R, TS = 72, 48, 64
START = (4, 21)


def ground():
    full = Image.new("RGB", (4608, 3072))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"frost-hollow-large-ground-r{r}c{c}.png")).convert("RGB"), (c * 2304, r * 1536))
    return full


def classify(full):
    a = np.asarray(full).astype(float)
    r, b = a[..., 0], a[..., 2]
    mx, mn = a.max(2), a.min(2); sat = (mx - mn) / np.maximum(mx, 1)
    water = (b > r + 40) & (b > 120) & (sat > 0.35)
    rock = (mx < 165) & (sat < 0.40) & (~water)
    fw = water.reshape(R, TS, C, TS).mean((1, 3)); fr = rock.reshape(R, TS, C, TS).mean((1, 3))
    # the only water is the river (cols < 27): blue snow shadows and the cave glow elsewhere count as walkable ground, not water
    return [["w" if fw[y, x] > 0.35 and x < 27 else "C" if fr[y, x] > 0.22 else "s" for x in range(C)] for y in range(R)]


def paste_old_section(rows):
    """Old 36x24 section from the hand-fixed ground-only rows, except its closed border (rows 22-23, col 35) where the painting now continues."""
    old = [l.rstrip() for l in open(OLD_ROWS) if l.strip()]
    assert len(old) == 24 and all(len(l) == 36 for l in old)
    for y in range(22):
        for x in range(35): rows[y][x] = old[y][x]


def apply_overrides(rows):
    if not os.path.exists(OVERRIDES): return
    for ln in open(OVERRIDES, encoding="utf8"):
        ln = ln.split("#")[0].split()
        if len(ln) != 5: continue
        x0, y0, x1, y1 = map(int, ln[:4])
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): rows[y][x] = ln[4]


def force_ring(rows):
    for y in range(R):
        for x in range(C):
            if y == 0 or y >= 45 or x >= 70 or (x == 0 and y >= 22): rows[y][x] = "C"


def flood(rows):
    seen = {START}; q = [START]
    while q:
        x, y = q.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < C and 0 <= n[1] < R and n not in seen and rows[n[1]][n[0]] in "sB": seen.add(n); q.append(n)
    return seen


def overlay(full, rows, seen):
    big = full.convert("RGBA").resize((C * 32, R * 32), Image.LANCZOS)
    ov = Image.new("RGBA", big.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov); t = 32
    col = {"C": (255, 0, 0, 95), "w": (0, 60, 255, 100), "B": (255, 200, 0, 120)}
    for y in range(R):
        for x in range(C):
            ch = rows[y][x]
            if ch in col: d.rectangle([x * t, y * t, (x + 1) * t, (y + 1) * t], fill=col[ch])
            elif (x, y) not in seen: d.rectangle([x * t, y * t, (x + 1) * t, (y + 1) * t], fill=(255, 0, 255, 140))   # walkable but unreachable
    f = ImageFont.load_default()
    for i in range(C + 1): d.line([i * t, 0, i * t, big.height], fill=(0, 0, 0, 90))
    for i in range(R + 1): d.line([0, i * t, big.width, i * t], fill=(0, 0, 0, 90))
    for x in range(C): d.text((x * t + 2, 1), str(x), fill=(0, 0, 0, 255), font=f)
    for y in range(R): d.text((1, y * t + 2), str(y), fill=(0, 0, 0, 255), font=f)
    out = Image.alpha_composite(big, ov).convert("RGB")
    if "--crop" in sys.argv:   # --crop x0 y0 x1 y1 [name]: tile rect at 1 tile = 48 px -> visuals/rows-crop-<name>.png
        i = sys.argv.index("--crop"); x0, y0, x1, y1 = map(int, sys.argv[i + 1:i + 5]); nm = sys.argv[i + 5] if len(sys.argv) > i + 5 else "x"
        out.crop((x0 * t, y0 * t, (x1 + 1) * t, (y1 + 1) * t)).resize(((x1 - x0 + 1) * 48, (y1 - y0 + 1) * 48), Image.LANCZOS).save(os.path.join(PLAN, "visuals", f"rows-crop-{nm}.png"))
    vis = os.path.join(PLAN, "visuals")
    out.resize((C * 16, R * 16), Image.LANCZOS).save(os.path.join(vis, "rows-painted-overlay.jpg"), quality=88)
    for n, (cx, cy) in {"nw": (0, 0), "ne": (1, 0), "sw": (0, 1), "se": (1, 1)}.items():
        out.crop((cx * 36 * t, cy * 24 * t, (cx + 1) * 36 * t, (cy + 1) * 24 * t)).save(os.path.join(vis, f"rows-painted-overlay-{n}.png"))


if __name__ == "__main__":
    full = ground(); rows = classify(full)
    paste_old_section(rows)
    apply_overrides(rows); force_ring(rows)
    seen = flood(rows)
    # walkable tiles nothing connects to (the NE cliff-top plateau with the stone circle, cliff-top specks): solid, so no spawn or prop lands there
    lost = [(x, y) for y in range(R) for x in range(C) if rows[y][x] in "sB" and (x, y) not in seen]
    for x, y in lost: rows[y][x] = "C"
    print("sealed %d unreachable walkable tiles" % len(lost))
    text = ["".join(r) for r in rows]
    open(OUT, "w").write("\n".join(text) + "\n")
    walk = sum(ch in "sB" for r in text for ch in r)
    print("\n".join("%2d %s" % (i, r) for i, r in enumerate(text)))
    print("walkable %d, reachable from %s: %d, unreachable %d" % (walk, START, len(seen), walk - len(seen)))
    if "--overlay" in sys.argv: overlay(full, rows, seen)

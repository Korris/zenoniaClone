"""Collision rows (48x32) of the painted Scorpion Gorge -> plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/scorpion-gorge-48x32-rows.txt.

Usage: python Tools/build-scorpion-gorge-rows.py [--overlay] [--no-flood]   (needs the ground chunks from build-scorpion-gorge-painted.py)
 1. Colour pass: w = dry-river pool (teal, solid unless bridged), C = canyon wall (dark, heavily grooved sandstone blocks, solid). Everything else (cracked
    earth fields, the stone road, the sand dune, the boss arena's sand swirl) is g (walkable); the road itself is not a separate letter, ROADS below lists
    its tile rects for the prop checker only.
 2. Frame: bottom 3 rows (under the HUD) and everything outside the map are C.
 3. Hand overrides from layout-scorpion-gorge/scorpion-gorge-rows-override.txt win over the colour pass: lines `x y ch` or `x0 y0 x1 y1 ch` (inclusive tile
    rect), # comments. The colour pass reads the bridge decks as river water and the temple steps as canyon wall; both live in that file (B bridge deck,
    s stair tread).
 4. Flood fill from the start tile: walkable tiles nobody can reach are turned into C and listed (rule 6 of docs/map-design-rules.md).
 Solid chars for the map class: "Cw". Walkable: g B s.
 --overlay writes per-quadrant pictures (grid + coordinates, solid red, water blue, stairs/bridge yellow, unreachable-turned-solid magenta) to
 layout-scorpion-gorge/visuals/rows-overlay-r{r}c{c}.png.
"""
import os, sys
from collections import deque
import numpy as np, cv2
from PIL import Image, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PLAN = os.path.join(ROOT, "plans", "261113-1923-scorpion-gorge", "layout-scorpion-gorge")
OVERRIDE = os.path.join(PLAN, "scorpion-gorge-rows-override.txt")
OUT = os.path.join(PLAN, "scorpion-gorge-48x32-rows.txt")
C, R, T = 48, 32, 64
START = (3, 14)    # arrival from Amber Dunes, on the main east-west road (west bank of the river)
SOLID = "Cw"


def ground():
    full = Image.new("RGB", (C * T, R * T))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"scorpion-gorge-ground-r{r}c{c}.png")).convert("RGB"), (c * 1536, r * 1024))
    return full


def classify(full):
    a = np.asarray(full).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    luma = 0.3 * r + 0.59 * g + 0.11 * b
    water = (b > r - 10) & (g > r - 25) & (luma < 190) & (luma > 40) & (b > 60)   # teal pools: B close to or above R, unlike the orange ground/cliff everywhere else
    mean = cv2.blur(luma, (17, 17)); sd = np.sqrt(np.maximum(cv2.blur(luma * luma, (17, 17)) - mean * mean, 0))
    redness = (r - b)   # cliff blocks are a much more saturated red-orange with deep dark grout lines than the muted cracked-earth fields
    rock = ~water & (redness > 95) & (sd > 30)
    f = lambda m: m.astype(np.float32).reshape(R, T, C, T).mean((1, 3))
    fw, fr = f(water), f(rock)
    rows = [["g"] * C for _ in range(R)]
    for y in range(R):
        for x in range(C):
            if fw[y, x] >= 0.30: rows[y][x] = "w"
            elif fr[y, x] >= 0.40: rows[y][x] = "C"
    return rows


def apply_overrides(rows):
    if not os.path.exists(OVERRIDE): return
    for line in open(OVERRIDE, encoding="utf8"):
        p = line.split("#")[0].split()
        if not p: continue
        if len(p) == 3: x0, y0, x1, y1, ch = int(p[0]), int(p[1]), int(p[0]), int(p[1]), p[2]
        else: x0, y0, x1, y1, ch = int(p[0]), int(p[1]), int(p[2]), int(p[3]), p[4]
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): rows[y][x] = ch


def flood(rows, closed=()):
    seen = set([START]); q = deque([START])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < C and 0 <= ny < R and (nx, ny) not in seen and (nx, ny) not in closed and rows[ny][nx] not in SOLID: seen.add((nx, ny)); q.append((nx, ny))
    return seen


def build(full):
    rows = classify(full)
    for x in range(C):
        for y in (R - 3, R - 2, R - 1): rows[y][x] = "C"
    apply_overrides(rows)
    assert rows[START[1]][START[0]] not in SOLID, "start tile is solid"
    if "--no-flood" in sys.argv: return ["".join(r) for r in rows], []   # while hand-fitting the overrides: show the colour pass as it is
    seen = flood(rows); cut = []
    for y in range(R):
        for x in range(C):
            if rows[y][x] not in SOLID and (x, y) not in seen: cut.append((x, y)); rows[y][x] = "C"
    # The dry riverbed must be a real barrier: without either bridge deck the east bank (crossroad, temple, boss arena) cannot be reached from the west gate.
    decks = {(x, y) for y in range(R) for x in range(C) if rows[y][x] == "B"}
    probe = (24, 10)
    assert probe in seen, "river probe %s is not reachable at all" % (probe,)
    if probe in flood(rows, decks): print("PROBLEM: the river leaks, %s is reachable without either bridge" % (probe,))
    # Either bridge alone must still connect both banks.
    west_deck = {d for d in decks if d[0] < 17}; east_deck = {d for d in decks if d[0] >= 17}
    for name, other in (("west bridge", east_deck), ("south bridge", west_deck)):
        if probe not in flood(rows, other): print("PROBLEM: the %s alone does not reach the east bank" % name)
    if cut: print("unreachable walkable tiles turned solid (%d):" % len(cut), cut if len(cut) < 40 else "...")
    return ["".join(r) for r in rows], cut


def overlay(full, rows, cut):
    ov = Image.new("RGBA", full.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    col = {"C": (255, 0, 0, 95), "w": (30, 60, 255, 120), "B": (255, 230, 0, 110), "s": (255, 230, 0, 110)}
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in col: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=col[ch])
    for x, y in cut: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=(255, 0, 255, 140))   # unreachable -> solid
    for i in range(C + 1): d.line([i * T, 0, i * T, R * T], fill=(0, 0, 0, 90))
    for i in range(R + 1): d.line([0, i * T, C * T, i * T], fill=(0, 0, 0, 90))
    for y in range(R):
        for x in range(C):
            if x % 2 == 0 and y % 2 == 0: d.text((x * T + 3, y * T + 2), "%d,%d" % (x, y), fill=(255, 255, 255, 255))
    return Image.alpha_composite(full.convert("RGBA"), ov).convert("RGB")


if __name__ == "__main__":
    full = ground(); rows, cut = build(full)
    os.makedirs(PLAN, exist_ok=True)
    open(OUT, "w").write("\n".join(rows) + "\n")
    print("\n".join(rows))
    if "--overlay" in sys.argv:
        vis = os.path.join(PLAN, "visuals"); os.makedirs(vis, exist_ok=True)
        ov = overlay(full, rows, cut)
        for r in range(2):
            for c in range(2): ov.crop((c * 1536, r * 1024, (c + 1) * 1536, (r + 1) * 1024)).save(os.path.join(vis, f"rows-overlay-r{r}c{c}.png"))
        ov.resize((1536, 1024), Image.LANCZOS).save(os.path.join(vis, "rows-overlay-full.png"))

#!/usr/bin/env python3
"""Taurus V4 GREYBOX: rows for Willowbrook Village (V2 100x100 since 25/11/2026, layout in Tools/tau_village_greybox_layout.py) and Clover Meadows (96x64).

  python Tools/build-tau-greybox-rows.py            # rows -> the two map classes, ground / border PNGs, preview + checks
  python Tools/build-tau-greybox-rows.py --check    # only checks (flood fill, road width, camps clear), writes nothing

Row chars (the first group is solid): T building / wall / fence / rune stone / door lintel   t tree or forest wall   w water   x (unused since phase 05)
                                       g grass   d road   p plaza   B bridge deck   (village V2: x = solid meadow outside the wall)
Meadows ground variants (all walkable, only the colour differs per zone, phase 05): h Rabbit Hills  m Mushroom Grove  b Bramble Thicket  e Elderwood Edge  r Ancient Roots  n Brambleback Nest.
The ground is a TEMPORARY stand-in: one flat colour per tile class, 16 px per tile, one chunk (the game scales it to the map). Final art = phase 04 (ChatGPT).
NPC / portal / camp coordinates live in the map classes and MapSpawnTable.cs; this script only reads them to draw the preview and run the checks.
"""
import math, os, re, sys
from collections import deque
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tau_village_greybox_layout as VL   # noqa: E402  (village V2 layout data)
from tau_village_greybox_layout import village   # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS = os.path.join(ROOT, "Assets", "Scripts", "Maps")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
SHOTS = os.path.join(ROOT, "docs", "reference", "world-design", "greybox-shots")
PX = 16
COL = {"g": (0x88, 0xA8, 0x5D), "d": (0xCB, 0xA6, 0x79), "p": (0xDD, 0xCB, 0xA0), "T": (0x8A, 0x84, 0x78), "t": (0x4F, 0x6B, 0x3A),
       "w": (0x5B, 0x8F, 0xB5), "B": (0xA0, 0x7A, 0x4A), "x": (0x5E, 0x6B, 0x52), "k": (0xB0, 0x6A, 0x3A),
       "h": (0x9C, 0xB4, 0x62), "m": (0x76, 0x9A, 0x58), "b": (0x8E, 0x8A, 0x58), "e": (0x5E, 0x84, 0x4A), "r": (0x7A, 0x88, 0x6E), "n": (0xA0, 0x84, 0x5C)}
SOLID = "Ttwx"


def grid(w, h, fill="g"):
    return [[fill] * w for _ in range(h)]


def rect(g, x0, y0, x1, y1, c):  # inclusive
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= y < len(g) and 0 <= x < len(g[0]): g[y][x] = c


# Phase 05 layout (96x64). South to north: Z1 Cloverfield rows 38-60 (C01 C02), Z2 Rabbit Hills rows 27-37 (C03, east) + Z3 Brookside (C04, west bank; the stream is rows 24-26 with ONE
# bridge on the road), then the north bank rows 1-23: Z4 Mushroom Grove (C05, east), Z5 Bramble Thicket in the north-west (C06 south, C07 north, Brambleback Nest at the far corner), Z6 Elderwood
# Edge (C08), Z7 Ancient Roots in the north-centre (C09 / C10 either side of the 4-wide corridor to the dungeon door), the closed Gemini gate at the north-east edge.
BRIDGE = (46, 49, 24, 26)                        # x0, x1, y0, y1
DOOR = (56, 59, 2)                               # corridor x0, x1 and the row of the door opening (phase 06 puts the portal here)
NEST = (3, 14, 1, 10)                            # x0, x1, y0, y1 of the Brambleback Nest floor; its mouth is rows 11, x 7-10
RUNESTONE = (38, 23)                             # quest touch point T-M03 (solid tile, approach from the north bank)
ANCIENT_ROOTS = (53, 9)                          # quest touch point T-M04
GEMINI = (88, 91, 6, 11)                         # closed gate block (solid), reached by the east spur
ZONES = (("m", 40, 91, 15, 23), ("b", 3, 39, 12, 23), ("e", 26, 52, 1, 14), ("r", 40, 91, 1, 14), ("h", 4, 91, 27, 37))   # x0, x1, y0, y1 per ground variant (painted in this order)


def wobble(y):
    return 4 + round(2 * math.sin(y * 0.5)), 91 - round(2 * math.sin(y * 0.37))


def meadows():
    W, H = 96, 64
    g = grid(W, H, "t")
    for y in range(1, 38):                         # north bank + south bank interiors
        l, r = wobble(y)
        for x in range(l, r + 1): g[y][x] = "g"
    for y in range(38, 61):                        # Z1, unchanged since phase 02
        f = 3 + round(3 * math.sin(y * 0.45))
        for x in range(6 + f, 90 - f): g[y][x] = "g"
    for ch, x0, x1, y0, y1 in ZONES:
        for y in range(y0, y1 + 1):
            l, r = wobble(y)
            for x in range(max(x0, l), min(x1, r) + 1): g[y][x] = ch
    rect(g, 3, 24, 92, 26, "w")                    # the stream
    rect(g, NEST[0], NEST[2], NEST[1], NEST[3], "n"); rect(g, 7, 11, 10, 11, "n")
    for y in range(27, 61):                        # the main road: Z1 wiggle, straight from row 38 up
        cx = road_cx(y)
        for x in range(cx - 2, cx + 2): g[y][x] = "d"
    rect(g, BRIDGE[0], BRIDGE[2], BRIDGE[1], BRIDGE[3], "B")
    rect(g, 46, 12, 49, 23, "d")                   # north bank road up to the junction
    rect(g, 5, 12, 59, 15, "d")                    # west-east trunk (to the thicket and the nest) and on to the corridor
    rect(g, DOOR[0], DOOR[2], DOOR[1], 11, "d")    # the corridor to the dungeon door
    rect(g, 60, 8, 86, 11, "d")                    # east spur to the closed Gemini gate
    rect(g, 54, 0, 61, 1, "T")                     # door lintel + arch (solid); the opening row DOOR[2] stays road
    rect(g, 54, 2, 55, 2, "T"); rect(g, 60, 2, 61, 2, "T")
    rect(g, GEMINI[0], GEMINI[2], GEMINI[1], GEMINI[3], "T")
    g[RUNESTONE[1]][RUNESTONE[0]] = "T"
    rect(g, ANCIENT_ROOTS[0], ANCIENT_ROOTS[1], ANCIENT_ROOTS[0] + 1, ANCIENT_ROOTS[1] + 1, "T")
    scatter_forest(g)
    rect(g, 0, 61, W - 1, H - 1, "t")
    return g


def scatter_forest(g):
    """Deterministic tree clumps for shape: only where nothing walks (>= 3 tiles from roads, camps, the nest, quest points and the door corridor)."""
    import random
    rng = random.Random(24112026)
    keep = list(camp_rects()) + [(NEST[0] - 1, NEST[1] + 1, NEST[2] - 1, NEST[3] + 2)]
    near = [(x, y) for y in range(len(g)) for x in range(len(g[0])) if g[y][x] in "dBpTw"]
    for _ in range(420):
        x, y = rng.randint(4, 91), rng.randint(1, 59)
        w, h = rng.choice(((1, 1), (2, 1), (1, 2), (2, 2)))
        if g[y][x] in "dBpTwt" or any(abs(x - k[0]) <= 3 or True for k in ()): continue
        if any(k[0] - 3 <= x + w and x <= k[1] + 3 and k[2] - 3 <= y + h and y <= k[3] + 3 for k in keep): continue
        if min(math.hypot(x - nx, y - ny) for nx, ny in near_sample(near, x, y)) < 3.2: continue
        if y >= 38 and y < 61: continue            # Z1 stays as phase 02 built it
        rect(g, x, y, x + w - 1, y + h - 1, "t")


def near_sample(near, x, y):
    return [(nx, ny) for nx, ny in near if abs(nx - x) <= 6 and abs(ny - y) <= 6] or [(-99, -99)]


def road_cx(y):
    return 48 + round(7 * math.sin((60 - min(max(y, 38), 60)) / 22.0 * math.pi))


def rows_of(g): return ["".join(r) for r in g]


def patch(path, rows):
    s = open(path, encoding="utf-8").read()
    a, b = s.index("// BEGIN ROWS"), s.index("// END ROWS")
    body = "// BEGIN ROWS (generated by Tools/build-tau-greybox-rows.py)\n        static readonly string[] Rows =\n        {\n" + "".join('            "%s",\n' % r for r in rows) + "        };\n        "
    open(path, "w", encoding="utf-8", newline="\n").write(s[:a] + body + s[b:])


def parse_map(cs):
    s = open(os.path.join(MAPS, cs), encoding="utf-8").read()
    npcs = [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'new MapNpc\("(\w+)", (\d+), (\d+)\)', s)]
    portals = [tuple(map(int, m[:2])) + (m[2],) + tuple(map(int, m[3:])) for m in re.findall(r"new MapPortal\((\d+), (\d+), (\w+), (\d+), (\d+)\)", s)]
    return npcs, portals


def camps():
    s = open(os.path.join(ROOT, "Assets", "Scripts", "MapSpawnTable.cs"), encoding="utf-8").read()
    a = s.index("TauCloverMeadows =")
    blk = s[a:s.index("};", a)]
    rx = r'new FrostHollowMap\.Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), \d+f(?:, (true|false))?\)'
    return [(m[0], int(m[1]), int(m[2]), int(m[3]), int(m[4]), int(m[5]), m[6] == "true") for m in re.findall(rx, blk)]


def camp_rects():
    """(x0, x1, y0, y1) of every distinct camp rect, read from MapSpawnTable.cs."""
    seen = []
    for typ, cnt, tx, ty, w, h, passive in camps():
        r = (tx, tx + w - 1, ty, ty + h - 1)
        if r not in seen: seen.append(r)
    return seen


def flood(rows, start):
    H, W = len(rows), len(rows[0]); seen = {start}; q = deque([start])
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in seen and rows[ny][nx] not in SOLID: seen.add((nx, ny)); q.append((nx, ny))
    return seen


def check(name, rows, start, npcs, portals, camp_list=()):
    bad = []
    H, W = len(rows), len(rows[0])
    free = {(x, y) for y in range(H) for x in range(W) if rows[y][x] not in SOLID}
    for npc, x, y in npcs:
        if rows[y][x] in SOLID: bad.append("NPC %s on a solid tile %d,%d" % (npc, x, y))
        free.discard((x, y))   # the NPC itself blocks its tile
    reach = flood(rows, start)
    for t in free - reach: bad.append("walkable tile not reachable from start: %s" % (t,))
    for p in portals:
        if rows[p[1]][p[0]] in SOLID: bad.append("portal on a solid tile %s" % (p,))
    for y in (H - 3, H - 2, H - 1):
        if any(c not in SOLID for c in rows[y]): bad.append("bottom row %d is not blocked (under the HUD)" % y)
    for npc, x, y in npcs:   # an NPC must not plug a 1-wide path: it needs a free neighbour set
        n = sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if rows[y + dy][x + dx] not in SOLID)
        if n < 2: bad.append("NPC %s is boxed in" % npc)
    total_by_rect = {}
    for typ, cnt, tx, ty, w, h, passive in camp_list: total_by_rect[(tx, ty, w, h)] = total_by_rect.get((tx, ty, w, h), 0) + cnt
    for typ, cnt, tx, ty, w, h, passive in camp_list:
        tiles = [(x, y) for y in range(ty, ty + h) for x in range(tx, tx + w)]
        solid = [t for t in tiles if rows[t[1]][t[0]] in SOLID]
        if solid: bad.append("camp %s rect has %d solid tiles" % (typ, len(solid)))
        if any(rows[y][x] in "dpB" for x, y in tiles): bad.append("camp %s rect lies on a road" % typ)
        if len(tiles) < 2 * total_by_rect[(tx, ty, w, h)]: bad.append("camp %s too small" % typ)
        for px, py, *_ in portals + [(start[0], start[1])]:
            d = min(math.hypot(x - px, y - py) for x, y in tiles)
            if d < 5: bad.append("camp %s only %.1f tiles from entrance %s" % (typ, d, (px, py)))
    print(name, "OK" if not bad else "PROBLEMS", "(%d walkable, %d reachable)" % (len(free), len(reach & free)))
    for b in bad: print("  PROBLEM", b)
    return not bad


def check_meadows(rows):
    """Phase 05 extras: the bridge is the only way north, the corridor / nest / quest points are open, 68 monsters in 10 camps."""
    bad = []
    H, W = len(rows), len(rows[0])
    cut = [list(r) for r in rows]
    for y in range(BRIDGE[2], BRIDGE[3] + 1):
        for x in range(BRIDGE[0], BRIDGE[1] + 1): cut[y][x] = "T"
    reach = flood(["".join(r) for r in cut], (48, 57))
    if (48, 20) in reach or (10, 5) in reach: bad.append("north bank reachable without the bridge")
    full = flood(rows, (48, 57))
    for name, (x, y) in (("door opening", (DOOR[0], DOOR[2])), ("nest centre", (8, 6)), ("gemini spur end", (GEMINI[0] - 1, 9)),
                         ("runestone approach", (RUNESTONE[0], RUNESTONE[1] - 1)), ("ancient roots approach", (ANCIENT_ROOTS[0] + 2, ANCIENT_ROOTS[1]))):
        if (x, y) not in full: bad.append(name + " not reachable")
    for x in range(DOOR[0], DOOR[1] + 1):
        for y in range(DOOR[2], 16):
            if rows[y][x] in SOLID: bad.append("door corridor blocked at %d,%d" % (x, y))
    for y in range(NEST[2], NEST[3] + 1):
        for x in range(NEST[0], NEST[1] + 1):
            if rows[y][x] in SOLID: bad.append("nest blocked at %d,%d" % (x, y))
    if sum(c[1] for c in camps()) != 68: bad.append("monster total %d, BIBLE says 68" % sum(c[1] for c in camps()))
    print("meadows extras", "OK" if not bad else "PROBLEMS")
    for b in bad: print("  PROBLEM", b)
    return not bad


def paint(rows, path, npcs, portals, camp_list, scale=PX, labels=False, districts=()):
    H, W = len(rows), len(rows[0])
    im = Image.new("RGB", (W * scale, H * scale))
    d = ImageDraw.Draw(im)
    for y in range(H):
        for x in range(W): d.rectangle([x * scale, y * scale, (x + 1) * scale - 1, (y + 1) * scale - 1], fill=COL[rows[y][x]])
    if labels:
        for typ, cnt, tx, ty, w, h, passive in camp_list:
            d.rectangle([tx * scale, ty * scale, (tx + w) * scale - 1, (ty + h) * scale - 1], outline=(255, 255, 0) if passive else (255, 140, 0), width=2)
        for npc, x, y in npcs: d.ellipse([x * scale + 2, y * scale + 2, (x + 1) * scale - 3, (y + 1) * scale - 3], fill=(255, 0, 255)); d.text((x * scale + scale, y * scale), npc, fill=(0, 0, 0))
        for p in portals: d.rectangle([p[0] * scale, p[1] * scale, (p[0] + 1) * scale - 1, (p[1] + 1) * scale - 1], outline=(0, 0, 255), width=2)
        for name, (x0, y0, x1, y1) in districts:   # village V2: district frames + names
            d.rectangle([x0 * scale, y0 * scale, (x1 + 1) * scale - 1, (y1 + 1) * scale - 1], outline=(40, 40, 40), width=1)
            d.text((x0 * scale + 3, y0 * scale + 2), name, fill=(0, 0, 0))
    im.save(path)


def main():
    only_check = "--check" in sys.argv
    V, M = village(), meadows()
    vr, mr = rows_of(V), rows_of(M)
    if not only_check:
        patch(os.path.join(MAPS, "TauWillowbrookVillageMap.cs"), vr); patch(os.path.join(MAPS, "TauCloverMeadowsMap.cs"), mr)
    vn, vp = parse_map("TauWillowbrookVillageMap.cs"); mn, mp = parse_map("TauCloverMeadowsMap.cs")
    cl = camps()
    ok = check("village", vr, VL.START, vn, [p for p in vp])
    ok &= check("meadows", mr, (48, 57), mn, mp, cl)
    ok &= check_meadows(mr)
    # a camp beside the road keeps at least 3 tiles of distance to any road tile (2 free tiles between)
    road = [(x, y) for y, r in enumerate(mr) for x, c in enumerate(r) if c == "d" and y < 60]
    for typ, cnt, tx, ty, w, h, passive in cl:
        gap = min(math.hypot(x - rx, y - ry) for y in range(ty, ty + h) for x in range(tx, tx + w) for rx, ry in road)
        print("  camp %s nearest road tile %.1f" % (typ, gap))
        if gap < 3: ok = False; print("  PROBLEM camp too close to the road")
    if only_check: return 0 if ok else 1
    os.makedirs(TILES, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    # phase 04: the ground art is Tools/build-tau-25d-ground.py (chunks + border) and the props Tools/place-tau-25d-props.py: re-run both after changing rows.
    paint(vr, os.path.join(SHOTS, "layout-willowbrook-village.png"), vn, vp, [], 8, True, VL.DISTRICTS)
    paint(mr, os.path.join(SHOTS, "layout-clover-meadows.png"), mn, mp, cl, 12, True)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

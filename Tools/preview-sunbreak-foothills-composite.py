"""Composite of the 48x32 Sunbreak Foothills: ground chunks + every prop at its game position (y-sorted) + collision overlay, and the layout checks of docs/map-design-rules.md.

Usage: python Tools/preview-sunbreak-foothills-composite.py [--minimap] [--crops]
 Reads SunbreakFoothillsProps.cs, SunbreakFoothillsPropDefs.cs, the rows + portals + arrival tiles of SunbreakFoothillsMap.cs and the SunbreakFoothills block of MapSpawnTable.cs.
 Writes layout-sunbreak/visuals/composite-full.png (props, no overlay) and composite-solid.png (solid tiles red, water blue, deck / treads yellow, spawn rects white, portals cyan, arrivals green,
 boss magenta); --crops adds the four quadrants of the overlay at 1:1; --minimap bakes the props into sunbreak-foothills-ground-minimap.png (288x192).
 Prints PROBLEM for: a prop on a road / bridge / stair tile or inside a spawn rect, a solid prop that cuts walkable tiles off, a spawn rect with fewer than 2 free tiles per monster,
 a spawn rect closer than 5 tiles to an arrival tile or a portal, a walkable tile in the bottom 3 rows, a solid portal / arrival / boss tile.
"""
import os, re, sys
from collections import deque
from PIL import Image, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS = os.path.join(ROOT, "Assets", "Scripts", "Maps")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PROPS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
VIS = os.path.join(ROOT, "plans", "261113-1010-world-map-new-maps", "layout-sunbreak", "visuals")
NAME = "sunbreak-foothills-ground"
C, R, T = 48, 32, 64
SOLID = "Cw"
# Real road tiles (the rows use 'd' for sand and a few snow patches too): the three roads to the exits, the west dead end, the stair approaches.
ROADS = [(20, 20, 22, 28), (2, 16, 13, 16), (3, 17, 4, 17), (10, 15, 13, 15), (15, 12, 18, 12), (18, 11, 21, 11), (20, 10, 22, 10), (22, 8, 24, 9), (23, 6, 24, 7),
         (16, 19, 19, 19), (17, 20, 20, 20), (20, 21, 25, 21), (27, 18, 29, 18), (28, 17, 30, 17), (30, 16, 36, 16), (34, 15, 47, 15), (38, 16, 47, 16), (35, 14, 37, 14), (36, 10, 37, 13)]


def rd(n): return open(os.path.join(MAPS, n), encoding="utf8").read()


def tables():
    src = re.sub(r"//.*", "", rd("SunbreakFoothillsProps.cs"))
    return [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'P\("(\w+)",\s*(\d+),\s*(\d+)\)', src)]


def defs():
    return {m[0]: (float(m[1]), float(m[2]), int(m[3]), int(m[4])) for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', rd("SunbreakFoothillsPropDefs.cs"))}


def map_data():
    src = rd("SunbreakFoothillsMap.cs")
    rows = re.findall(r'"([CwBsdg]{48})"', src)
    assert len(rows) == R, len(rows)
    portals = [(int(a), int(b)) for a, b in re.findall(r"new MapPortal\((\d+), (\d+),", src)]
    arrivals = [(int(a), int(b)) for a, b in re.findall(r"ArrivalTx = (\d+), \w+ArrivalTy = (\d+)", src)]
    return rows, portals, arrivals


def spawn():
    src = open(os.path.join(ROOT, "Assets", "Scripts", "MapSpawnTable.cs"), encoding="utf8").read()
    a = src.index("SunbreakFoothills ="); block = src[a:src.index("};", a)]
    zones = [(m[0], int(m[1]), int(m[2]), int(m[3]), int(m[4]), int(m[5])) for m in re.findall(r'new FrostHollowMap\.Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), \d+f\)', block)]
    boss = re.search(r"SunbreakFoothillsBossTile = new Vector2Int\((\d+), (\d+)\)", src)
    return zones, (int(boss.group(1)), int(boss.group(2)))


def ground():
    full = Image.new("RGBA", (C * T, R * T))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"{NAME}-r{r}c{c}.png")).convert("RGBA"), (c * 1536, r * 1024))
    return full


def paste(full, im, x, y):
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(full.width, x + im.width), min(full.height, y + im.height)
    if x1 <= x0 or y1 <= y0: return
    full.alpha_composite(im.crop((x0 - x, y0 - y, x1 - x, y1 - y)), (x0, y0))


def draw_props(full, plist, dd):
    # Like MapScenery: low walk-through decoration first (it lies on the ground), then everything else by its base row.
    flat = lambda t: dd[t][3] == 0 and dd[t][1] <= 3
    for t, tx, ty in sorted(plist, key=lambda p: (not flat(p[0]), p[2], p[1])):
        w, h, fw, fh = dd[t]
        im = Image.open(os.path.join(PROPS, t + ".png")).convert("RGBA")
        paste(full, im, int((tx + fw / 2.0) * T - im.width / 2), (ty + 1) * T - im.height)


def footprint(p, dd):
    t, tx, ty = p; w, h, fw, fh = dd[t]
    return [(x, y) for y in range(ty - fh + 1, ty + 1) for x in range(tx, tx + fw) if 0 <= x < C and 0 <= y < R]


def flood(solid, start):
    seen = {start}; q = deque([start])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < C and 0 <= n[1] < R and n not in seen and n not in solid: seen.add(n); q.append(n)
    return seen


def check(rows, plist, dd, portals, arrivals, zones, boss):
    bad = 0
    def problem(msg):
        nonlocal bad; bad += 1; print("PROBLEM", msg)
    base = {(x, y) for y in range(R) for x in range(C) if rows[y][x] in SOLID}
    road = {(x, y) for x0, y0, x1, y1 in ROADS for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)}
    road |= {(x, y) for y in range(R) for x in range(C) if rows[y][x] in "Bs"}
    for t in sorted(road):
        if rows[t[1]][t[0]] in SOLID: problem("road tile %s is solid in the rows" % (t,))
    in_zone = lambda x, y: any(z[2] <= x < z[2] + z[4] and z[3] <= y < z[3] + z[5] for z in zones)
    solid = set(base)
    for p in plist:
        t, tx, ty = p
        if t not in dd: problem("unknown prop %s" % t); continue
        if not os.path.exists(os.path.join(PROPS, t + ".png")): problem("missing art %s" % t)
        fw = dd[t][2]
        cells = footprint(p, dd) or [(x, ty) for x in range(tx, tx + fw)]   # walk-through props: their base tiles
        for c in cells:
            if c in road: problem("prop %s stands on road / bridge / stair tile %s" % (p, c))
            if in_zone(*c): problem("prop %s stands inside a spawn rect %s" % (p, c))
        solid |= set(footprint(p, dd))
    start = arrivals[0]
    seen = flood(solid, start)
    cut = [(x, y) for y in range(R) for x in range(C) if (x, y) not in solid and (x, y) not in seen]
    if cut: problem("walkable tiles cut off from the start: %s" % cut)
    for x in range(C):
        for y in range(R - 3, R):
            if (x, y) not in solid: problem("walkable tile under the HUD %s" % ((x, y),))
    for name, pts in (("portal", portals), ("arrival", arrivals), ("boss", [boss])):
        for p in pts:
            if p in solid: problem("%s tile %s is solid" % (name, p))
    total = 0
    for i, (t, n, zx, zy, zw, zh) in enumerate(zones):
        total += n
        free = [(x, y) for y in range(zy, zy + zh) for x in range(zx, zx + zw) if (x, y) not in solid]
        # a spawn spot is only used when it is 5 tiles from the start tile and the portals (MapSpawnTable.NearEntrance); arrivals are checked here against the whole rect
        near = min(((x - a) ** 2 + (y - b) ** 2) ** 0.5 for x, y in free for a, b in portals + arrivals)
        print("zone %d %-18s x%d  rect (%d,%d %dx%d)  free tiles %d  nearest arrival / portal %.1f tiles" % (i + 1, t, n, zx, zy, zw, zh, len(free), near))
        if len(free) < 2 * n: problem("zone %d has %d free tiles for %d monsters" % (i + 1, len(free), n))
        if near < 5: problem("zone %d is %.1f tiles from an arrival tile or portal" % (i + 1, near))
        if [c for c in free if c in road and rows[c[1]][c[0]] in "Bs"]: problem("zone %d covers a bridge or a stair" % (i + 1))
    print("monsters:", total, "+ boss at", boss, "| props:", len(plist), "| problems:", bad)
    return solid


def overlay(full, rows, solid, portals, arrivals, zones, boss):
    ov = Image.new("RGBA", full.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for y in range(R):
        for x in range(C):
            ch = rows[y][x]
            fill = (30, 60, 255, 110) if ch == "w" else (255, 0, 0, 95) if (x, y) in solid else (255, 230, 0, 100) if ch in "Bs" else None
            if fill: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=fill)
    for i in range(C + 1): d.line([i * T, 0, i * T, R * T], fill=(0, 0, 0, 70))
    for i in range(R + 1): d.line([0, i * T, C * T, i * T], fill=(0, 0, 0, 70))
    for y in range(0, R, 2):
        for x in range(0, C, 2): d.text((x * T + 3, y * T + 2), "%d,%d" % (x, y), fill=(255, 255, 255, 255))
    for t, n, zx, zy, zw, zh in zones:
        d.rectangle([zx * T, zy * T, (zx + zw) * T, (zy + zh) * T], outline=(255, 255, 255, 255), width=5)
        d.text((zx * T + 10, zy * T + 22), "%s x%d" % (t, n), fill=(255, 255, 255, 255), stroke_width=2, stroke_fill=(0, 0, 0, 255))
    for x, y in portals: d.rectangle([x * T + 4, y * T + 4, x * T + T - 5, y * T + T - 5], outline=(0, 255, 255, 255), width=6)
    for x, y in arrivals: d.ellipse([x * T + 14, y * T + 14, x * T + T - 14, y * T + T - 14], fill=(0, 255, 0, 255))
    d.ellipse([boss[0] * T + 8, boss[1] * T + 8, boss[0] * T + T - 8, boss[1] * T + T - 8], fill=(255, 0, 255, 255))
    return Image.alpha_composite(full, ov)


if __name__ == "__main__":
    plist, dd = tables(), defs()
    rows, portals, arrivals = map_data()
    zones, boss = spawn()
    full = ground()
    draw_props(full, plist, dd)
    os.makedirs(VIS, exist_ok=True)
    full.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-full.png"))
    if "--minimap" in sys.argv: full.convert("RGB").resize((288, 192), Image.LANCZOS).save(os.path.join(TILES, NAME + "-minimap.png"), optimize=True)
    solid = check(rows, plist, dd, portals, arrivals, zones, boss)
    ov = overlay(full, rows, solid, portals, arrivals, zones, boss).convert("RGB")
    ov.resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-solid.png"))
    if "--crops" in sys.argv:
        for r in range(2):
            for c in range(2): ov.crop((c * 1536, r * 1024, (c + 1) * 1536, (r + 1) * 1024)).save(os.path.join(VIS, f"composite-solid-r{r}c{c}.png"))

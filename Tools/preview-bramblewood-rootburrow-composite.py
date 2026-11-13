"""Preview + placement checks of Bramblewood Forest, Rootburrow and Thornheart Chamber, read from the real game data.

Usage: python Tools/preview-bramblewood-rootburrow-composite.py
Reads the C# rows of each map class, its portals and start tile, the props (BramblewoodForestProps / BramblewoodForestPropDefs) and the spawn zones + boss tiles
(MapSpawnTable.cs), then writes per map to plans/261113-1010-world-map-new-maps/visuals/:
  <map>-composite.png   ground + props drawn the way MapScenery places them (bottom-centre of the footprint, flat decoration first, then y-sorted)
  <map>-solids.png      the same with solid tiles tinted red (water blue), zones as yellow rects, portals cyan, arrival tiles green, start white, boss magenta
Prints PROBLEM lines for: a walkable tile that cannot be reached from the start (after props block), an open HUD row, a portal / arrival / start / boss on a solid tile,
a solid prop on a road or bridge tile or inside a zone rect, a zone with fewer than 2 free tiles per monster or closer than 5 tiles to the start, a portal or an arrival tile.
"""
import importlib.util, os, re, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("rows", os.path.join(HERE, "build-bramblewood-rootburrow-rows.py"))
rw = importlib.util.module_from_spec(spec); spec.loader.exec_module(rw)
VIS = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/visuals")
MAPS_CS = os.path.join(ROOT, "Assets/Scripts/Maps")
PROPS = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
# file id -> (class, spawn table name)
CLASSES = {"bramblewood-forest": ("BramblewoodForestMap", "BramblewoodForest"), "rootburrow": ("RootburrowMap", "Rootburrow"), "thornheart-chamber": ("ThornheartChamberMap", None)}
T = 64


def read(path):
    return open(path, encoding="utf-8").read()


def map_data(cls):
    src = read(os.path.join(MAPS_CS, cls + ".cs"))
    block = src[src.index("Rows ="):]
    rows = re.findall(r'^\s*"([A-Za-z]+)",\s*$', block[:block.index("};")], re.M)
    portals = [(int(a), int(b), t, int(c), int(d)) for a, b, t, c, d in re.findall(r"new MapPortal\((\d+), (\d+), (\w+)\.MapId, (\d+), (\d+)\)", src)]
    start = tuple(int(v) for v in re.search(r"PlayerStartTile = new Vector2Int\((\d+), (\d+)\)", src).groups())
    return rows, portals, start


def arrivals(cls):
    """Tiles where heroes land on this map: the targets of every portal that points at it (all map classes are scanned)."""
    out = []
    for name in os.listdir(MAPS_CS):
        if name.endswith(".cs"):
            out += [(int(c), int(d)) for c, d in re.findall(r"new MapPortal\(\d+, \d+, %s\.MapId, (\d+), (\d+)\)" % cls, read(os.path.join(MAPS_CS, name)))]
    return sorted(set(out))


def zones(table):
    if table is None: return []
    src = read(os.path.join(ROOT, "Assets/Scripts/MapSpawnTable.cs"))
    a = src.index("Zone[] %s =" % table)
    return [(m[0],) + tuple(int(v) for v in m[1:]) for m in re.findall(r'Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), \d+f\)', src[a:src.index("};", a)])]


def boss_tile(table):
    m = re.search(r"%sBossTile = new Vector2Int\((\d+), (\d+)\)" % (table or "ThornheartChamber"), read(os.path.join(ROOT, "Assets/Scripts/MapSpawnTable.cs")))
    return (int(m.group(1)), int(m.group(2))) if m else None


def prop_tables():
    defs = {n: (float(w), float(h), int(fw), int(fh)) for n, w, h, fw, fh in
            re.findall(r'\{ "(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\) \}', read(os.path.join(MAPS_CS, "BramblewoodForestPropDefs.cs")))}
    props = [(n, int(x), int(y)) for n, x, y in re.findall(r'P\("(\w+)", (\d+), (\d+)\)', read(os.path.join(MAPS_CS, "BramblewoodForestProps.cs")))]
    return defs, props


def draw_props(img, props, defs):
    """MapScenery order: walk-through decoration up to 3 tiles high lies under everything, the rest sorts by the base row."""
    flat = [p for p in props if defs[p[0]][3] == 0 and defs[p[0]][1] <= 3]
    tall = sorted([p for p in props if p not in flat], key=lambda p: p[2])
    for name, tx, ty in flat + tall:
        sp = Image.open(os.path.join(PROPS, name + ".png")).convert("RGBA")
        img.alpha_composite(sp, (int((tx + defs[name][2] * 0.5) * T - sp.width / 2), (ty + 1) * T - sp.height))


def main():
    defs, bw_props = prop_tables()
    font = ImageFont.truetype("arial.ttf", 18)
    bad = 0
    for map_id, (cls, table) in CLASSES.items():
        cols, nrows, _, tex, _ = rw.MAPS[map_id]
        rows, portals, start = map_data(cls)
        assert len(rows) == nrows and all(len(r) == cols for r in rows), map_id + ": rows are not %dx%d" % (cols, nrows)
        assert rows == rw.load_rows(map_id), map_id + ": the C# rows differ from the hand file (re-run build-bramblewood-rootburrow-rows.py and paste)"
        props = bw_props if map_id == "bramblewood-forest" else []
        img = Image.open(os.path.join(rw.TILES, tex + ".png")).convert("RGBA").resize((cols * T, nrows * T), Image.LANCZOS)
        draw_props(img, props, defs)
        img.convert("RGB").save(os.path.join(VIS, map_id + "-composite.png"))

        problems = []
        blocked = set()
        for name, tx, ty in props:
            _, _, fw, fh = defs[name]
            for y in range(ty - fh + 1, ty + 1):
                for x in range(tx, tx + fw):
                    blocked.add((x, y))
                    if rows[y][x] in "dB": problems.append("%s at %d,%d blocks road tile %d,%d" % (name, tx, ty, x, y))
        solid = lambda c, r: rows[r][c] in rw.SOLID or (c, r) in blocked
        seen = rw.flood(rows, start, blocked)
        lost = [(c, r) for r in range(nrows) for c in range(cols) if not solid(c, r) and (c, r) not in seen]
        if lost: problems.append("unreachable walkable tiles %s" % lost)
        if any(not solid(c, r) for r in range(nrows - 3, nrows) for c in range(cols)): problems.append("an open tile in the 3 HUD rows")
        land = arrivals(cls)
        boss = boss_tile(table) if map_id != "rootburrow" else None
        for what, tiles in (("start", [start]), ("portal", [(p[0], p[1]) for p in portals]), ("arrival", land), ("boss", [boss] if boss else [])):
            for c, r in tiles:
                if solid(c, r): problems.append("%s tile %d,%d is solid" % (what, c, r))
        zs = zones(table)
        keep_clear = [start] + [(p[0], p[1]) for p in portals] + land
        for kind, count, tx, ty, w, h in zs:
            free = sum(1 for y in range(ty, ty + h) for x in range(tx, tx + w) if not solid(x, y))
            same = sum(z[1] for z in zs if z[2:] == (tx, ty, w, h))
            if free < 2 * same: problems.append("zone %s at %d,%d: %d free tiles for %d monsters" % (kind, tx, ty, free, same))
            for c, r in keep_clear:
                dx, dy = max(tx - c, 0, c - (tx + w - 1)), max(ty - r, 0, r - (ty + h - 1))
                if (dx * dx + dy * dy) ** 0.5 < 5: problems.append("zone %s at %d,%d is %.1f tiles from %d,%d" % (kind, tx, ty, (dx * dx + dy * dy) ** 0.5, c, r))
            for (x, y) in blocked:
                if tx <= x < tx + w and ty <= y < ty + h: problems.append("a solid prop tile %d,%d inside zone %s at %d,%d" % (x, y, kind, tx, ty))
        ov = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
        box = lambda c, r: [c * T, r * T, (c + 1) * T - 1, (r + 1) * T - 1]
        for r in range(nrows):
            for c in range(cols):
                if rows[r][c] == "w": d.rectangle(box(c, r), fill=(0, 80, 255, 110))
                elif solid(c, r): d.rectangle(box(c, r), fill=(255, 0, 0, 110))
        for c, r in lost: d.ellipse(box(c, r), fill=(255, 0, 255, 230))
        for kind, count, tx, ty, w, h in zs:
            d.rectangle([tx * T + 3, ty * T + 3, (tx + w) * T - 4, (ty + h) * T - 4], outline=(255, 255, 0, 255), width=4)
        for c, r in land: d.rectangle(box(c, r), outline=(0, 255, 0, 255), width=5)
        for p in portals: d.rectangle(box(p[0], p[1]), outline=(0, 255, 255, 255), width=6)
        d.rectangle(box(*start), outline=(255, 255, 255, 255), width=3)
        if boss: d.ellipse(box(*boss), outline=(255, 0, 255, 255), width=6)
        out = Image.alpha_composite(img, ov); d = ImageDraw.Draw(out)
        for c in range(cols + 1): d.line([(c * T, 0), (c * T, nrows * T)], fill=(255, 255, 255, 70))
        for r in range(nrows + 1): d.line([(0, r * T), (cols * T, r * T)], fill=(255, 255, 255, 70))
        for c in range(cols): d.text((c * T + 4, 2), str(c), fill=(255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0))
        for r in range(nrows): d.text((3, r * T + 22), str(r), fill=(255, 255, 255), font=font, stroke_width=2, stroke_fill=(0, 0, 0))
        out.convert("RGB").save(os.path.join(VIS, map_id + "-solids.png"))
        print("%s: %d reachable tiles, %d props, zones %s (%d monsters), portals %s, arrivals %s" % (
            map_id, len(seen), len(props), [(z[0], z[1]) for z in zs], sum(z[1] for z in zs), [(p[0], p[1], p[2]) for p in portals], land))
        for p in problems: print("  PROBLEM:", p)
        bad += len(problems)
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()

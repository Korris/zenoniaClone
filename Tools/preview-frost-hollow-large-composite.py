"""Composite of the 72x48 Frost Hollow: ground chunks + every prop at its game position (y-sorted) + collision overlay + a tile grid for placing props.
Usage: python Tools/preview-frost-hollow-large-composite.py [--minimap] [--crops] [--nogrid]
 Reads the tables from Assets/Scripts/Maps/FrostHollow*Props.cs (rows `P("type", tx, ty)`), the PropDefs from VillagePropDefs.cs + FrostHollowPropDefs.cs and the rows from FrostHollowLayout.cs.
 Writes plans/261105-1240-frost-hollow-large-map/visuals/composite-full.jpg (props, no overlay), composite-solid.jpg (+ solid tiles red, portals/start/cluster rects),
 composite-grid-{r}{c}.jpg (1:1-ish 6 crops with tile coordinates), and with --minimap bakes the props into frost-hollow-large-ground-minimap.png (432x288, what Minimap.cs draws).
 Also runs the flood fill: every walkable tile must be reachable from the start after the prop footprints; prints the unreachable ones."""
import glob, os, re, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS = os.path.join(ROOT, "Assets", "Scripts", "Maps")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "tiles")
PROPS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
VIS = os.path.join(ROOT, "plans", "261105-1240-frost-hollow-large-map", "visuals")
T = 64
W, H = 72, 48

def rd(p): return open(p, encoding="utf8").read()

def tables():
    src = "".join(re.sub(r"//.*", "", rd(f)) for f in sorted(glob.glob(os.path.join(MAPS, "FrostHollow*Props.cs"))))
    return [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'P\("(\w+)",\s*(\d+),\s*(\d+)\)', src)]

def defs():
    src = rd(os.path.join(MAPS, "VillagePropDefs.cs")) + rd(os.path.join(MAPS, "FrostHollowPropDefs.cs"))
    return {m[0]: (float(m[1]), float(m[2]), int(m[3]), int(m[4])) for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', src)}

def rows():
    r = re.findall(r'^\s*"([CTwBs]{72})",?\s*$', rd(os.path.join(MAPS, "FrostHollowLayout.cs")), re.M)
    assert len(r) == 48, len(r)
    return r

def ground():
    full = Image.new("RGBA", (W * T, H * T))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"frost-hollow-large-ground-r{r}c{c}.png")).convert("RGBA"), (c * 2304, r * 1536))
    return full

def paste(full, im, x, y):
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(full.width, x + im.width), min(full.height, y + im.height)
    if x1 <= x0 or y1 <= y0: return
    full.alpha_composite(im.crop((x0 - x, y0 - y, x1 - x, y1 - y)), (x0, y0))

def draw_props(full, plist, dd):
    miss = set()
    for t, tx, ty in sorted(plist, key=lambda p: (p[2], p[1])):
        path = os.path.join(PROPS, t + ".png")
        if t not in dd or not os.path.exists(path): miss.add(t); continue
        fw = dd[t][2]
        im = Image.open(path).convert("RGBA")
        paste(full, im, int((tx + fw / 2.0) * T - im.width / 2), (ty + 1) * T - im.height)
    if miss: print("MISSING props:", sorted(miss))

def base_solid(rws):
    g = np.zeros((H, W), bool)
    for y, r in enumerate(rws):
        for x, ch in enumerate(r): g[y, x] = ch in "TwC"
    return g

def solid_grid(rws, plist, dd):
    g = base_solid(rws)
    for t, tx, ty in plist:
        if t not in dd: continue
        w, h, fw, fh = dd[t]
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if 0 <= x < W and 0 <= y < H: g[y, x] = True
    return g

def zones():
    zs = []
    for f in ("FrostHollowMap.cs", "MapSpawnTable.cs"):
        src = rd(os.path.join(ROOT, "Assets", "Scripts", f))
        if f == "MapSpawnTable.cs": src = src[src.index("FrostHollowOuter"):]
        else: src = src[:src.index("Zones") + 3000] if "Zones" in src else src
        for m in re.finditer(r'new (?:FrostHollowMap\.)?Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), \d+f\)', src):
            zs.append((m[1], int(m[2]), int(m[3]), int(m[4]), int(m[5]), int(m[6])))
    return zs

PORTALS = [(2, 21), (67, 20), (68, 20), (69, 20)]
START = (4, 21)

def overlay(full, g, zs):
    ov = Image.new("RGBA", full.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for y in range(H):
        for x in range(W):
            if g[y, x]: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=(255, 0, 0, 90))
    for z in zs:
        d.rectangle([z[2] * T, z[3] * T, (z[2] + z[4]) * T, (z[3] + z[5]) * T], outline=(255, 255, 255, 255), width=5)
    for x, y in PORTALS: d.rectangle([x * T, y * T, x * T + T, y * T + T], outline=(0, 255, 255, 255), width=5)
    d.rectangle([68 * T + 16, 22 * T + 16, 69 * T - 16, 23 * T - 16], fill=(0, 200, 255, 255))
    d.ellipse([START[0] * T + 16, START[1] * T + 16, START[0] * T + T - 16, START[1] * T + T - 16], fill=(0, 255, 0, 255))
    return Image.alpha_composite(full, ov)

def grid(img, x0, y0, tiles_w, tiles_h):
    """Crop of tile rect with tile lines every 1 tile (faint) and labels every 4."""
    im = img.crop((x0 * T, y0 * T, (x0 + tiles_w) * T, (y0 + tiles_h) * T)).convert("RGB"); d = ImageDraw.Draw(im)
    for i in range(tiles_w + 1):
        d.line([(i * T, 0), (i * T, im.height)], fill=(255, 255, 0) if (x0 + i) % 4 == 0 else (120, 120, 120), width=1)
    for j in range(tiles_h + 1):
        d.line([(0, j * T), (im.width, j * T)], fill=(255, 255, 0) if (y0 + j) % 4 == 0 else (120, 120, 120), width=1)
    for i in range(0, tiles_w, 4):
        for j in range(0, tiles_h, 4): d.text((i * T + 3, j * T + 3), f"{x0 + i},{y0 + j}", fill=(255, 0, 255))
    return im

# footprints may not touch these tiles: portals, arrival, start, gate nook, stairs, bridges, cave mouth
FORBID = [(1, 20, 5, 23), (66, 19, 70, 24), (5, 9, 7, 11), (3, 20, 5, 21), (4, 23, 6, 26), (29, 12, 31, 14), (32, 3, 34, 5), (44, 17, 47, 19), (47, 38, 50, 40), (15, 11, 22, 13), (15, 28, 24, 30)]

def validate(rws, plist, dd, zs):
    """Footprints on water/bridge tiles, on forbidden rects, overlapping each other; free tiles of each spawn rect (>= 2x count)."""
    seen, bad = {}, []
    for t, tx, ty in plist:
        if t not in dd: continue
        w, h, fw, fh = dd[t]
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if not (0 <= x < W and 0 <= y < H): bad.append((t, tx, ty, "off map")); continue
                if rws[y][x] in "wB": bad.append((t, tx, ty, "on " + rws[y][x]))
                if any(a <= x <= c and b <= y <= d for a, b, c, d in FORBID): bad.append((t, tx, ty, "forbidden"))
                if (x, y) in seen: bad.append((t, tx, ty, "overlaps " + seen[(x, y)]))
                seen[(x, y)] = t
    g = solid_grid(rws, plist, dd)
    for z in zs:
        free = int((~g[z[3]:z[3] + z[5], z[2]:z[2] + z[4]]).sum())
        if free < 2 * z[1]: bad.append((z[0], z[2], z[3], "cluster free %d < 2x%d" % (free, z[1])))
    return sorted(set(bad))

def flood(g):
    free = ~g
    lab, n = ndi.label(free)       # 4-connected
    sl = lab[START[1], START[0]]
    return free & (lab != sl)

if __name__ == "__main__":
    plist, dd, rws = tables(), defs(), rows()
    full = ground(); print("props:", len(plist))
    draw_props(full, plist, dd)
    full.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-full.jpg"), quality=88)
    if "--minimap" in sys.argv:
        full.convert("RGB").resize((432, 288), Image.LANCZOS).save(os.path.join(TILES, "frost-hollow-large-ground-minimap.png"), optimize=True)
    g = solid_grid(rws, plist, dd)
    for b in validate(rws, plist, dd, zones()): print("PROBLEM", b)
    lost = flood(g)
    print("walkable tiles not reachable from start:", int(lost.sum()), [(int(x), int(y)) for y, x in zip(*np.where(lost))][:20])
    ov = overlay(full, g, zones())
    ov.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-solid.jpg"), quality=88)
    if "--crops" in sys.argv:
        for r in range(2):
            for c in range(3):
                src = full if "--nogrid" in sys.argv else full
                grid(ov if "--solid" in sys.argv else full, c * 24, r * 24, 24, 24).save(os.path.join(VIS, f"composite-grid-{r}{c}.jpg"), quality=86)

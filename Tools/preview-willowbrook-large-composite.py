"""Composite of the 72x48 Willowbrook Meadows: ground chunks + every prop at its game position (y-sorted) + collision overlay.
Usage: python Tools/preview-willowbrook-large-composite.py [--minimap] [--crops]
 Reads the tables from Assets/Scripts/Maps/WillowbrookMeadows{Props,FrameProps,ScatterProps}.cs, the PropDefs from VillagePropDefs.cs and the rows from WillowbrookMeadowsMap.cs.
 Writes plans/.../visuals/composite-full.jpg (props, no overlay), composite-solid.jpg (+ solid tiles red, portals/start/cluster rects), optional 1:1 crops; --minimap bakes the
 props into willowbrook-large-ground-minimap.png (432x288, what Minimap.cs draws for a chunked map)."""
import os, re, sys, json
import numpy as np
from PIL import Image, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS = os.path.join(ROOT, "Assets", "Scripts", "Maps")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
PROPS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
VIS = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map", "visuals")
T = 64

def rd(n): return open(os.path.join(MAPS, n), encoding="utf8").read()

def tables():
    src = "".join(rd(n) for n in ("WillowbrookMeadowsProps.cs", "WillowbrookMeadowsFrameProps.cs", "WillowbrookMeadowsScatterProps.cs") if os.path.exists(os.path.join(MAPS, n)))
    src = re.sub(r"//.*", "", src)
    return [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'P\("(\w+)",\s*(\d+),\s*(\d+)\)', src)]

def defs():
    return {m[0]: (float(m[1]), float(m[2]), int(m[3]), int(m[4])) for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', rd("VillagePropDefs.cs"))}

def rows():
    r = re.findall(r'"([TgwBd]{72})"', rd("WillowbrookMeadowsMap.cs"))
    assert len(r) == 48, len(r)
    return r

def ground():
    full = Image.new("RGBA", (4608, 3072))
    for r in range(2):
        for c in range(2):
            full.paste(Image.open(os.path.join(TILES, f"willowbrook-large-ground-r{r}c{c}.png")).convert("RGBA"), (c * 2304, r * 1536))
    return full

def draw_props(full, plist, dd):
    miss = set()
    for t, tx, ty in sorted(plist, key=lambda p: (p[2], p[1])):
        if t not in dd: miss.add(t); continue
        w, h, fw, fh = dd[t]
        path = os.path.join(PROPS, t + ".png")
        if not os.path.exists(path): miss.add(t); continue
        im = Image.open(path).convert("RGBA")
        full.alpha_composite(im, (int((tx + fw / 2.0) * T - im.width / 2), (ty + 1) * T - im.height)) if False else paste(full, im, int((tx + fw / 2.0) * T - im.width / 2), (ty + 1) * T - im.height)
    if miss: print("MISSING props:", sorted(miss))

def paste(full, im, x, y):
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(full.width, x + im.width), min(full.height, y + im.height)
    if x1 <= x0 or y1 <= y0: return
    full.alpha_composite(im.crop((x0 - x, y0 - y, x1 - x, y1 - y)), (x0, y0))

def solid_grid(rws, plist, dd):
    g = np.zeros((48, 72), bool)
    for y, r in enumerate(rws):
        for x, ch in enumerate(r): g[y, x] = ch in "Tw"
    for t, tx, ty in plist:
        if t not in dd: continue
        w, h, fw, fh = dd[t]
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if 0 <= x < 72 and 0 <= y < 48: g[y, x] = True
    return g

def overlay(full, g, clusters):
    ov = Image.new("RGBA", full.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for y in range(48):
        for x in range(72):
            if g[y, x]: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=(255, 0, 0, 90))
    for c in clusters:
        d.rectangle([c[0] * T, c[1] * T, (c[0] + c[2]) * T, (c[1] + c[3]) * T], outline=(255, 255, 255, 255), width=6)
    d.rectangle([0, 23 * T, T, 25 * T], outline=(0, 255, 255, 255), width=6)       # west gate
    d.ellipse([3 * T + 16, 23 * T + 16, 4 * T - 16, 24 * T - 16], fill=(0, 255, 0, 255))   # start tile
    return Image.alpha_composite(full, ov)

def zones():
    m = re.findall(r'new FrostHollowMap\.Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), (\d+)f\)', open(os.path.join(ROOT, "Assets", "Scripts", "MapSpawnTable.cs"), encoding="utf8").read())
    return [(int(a[2]), int(a[3]), int(a[4]), int(a[5])) for a in m[-11:]] if len(m) >= 11 else []

if __name__ == "__main__":
    plist, dd, rws = tables(), defs(), rows()
    full = ground(); print("props:", len(plist))
    draw_props(full, plist, dd)
    full.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-full.jpg"), quality=88)
    if "--minimap" in sys.argv:
        full.convert("RGB").resize((432, 288), Image.LANCZOS).save(os.path.join(TILES, "willowbrook-large-ground-minimap.png"), optimize=True)
    g = solid_grid(rws, plist, dd)
    ov = overlay(full, g, zones())
    ov.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "composite-solid.jpg"), quality=88)
    if "--crops" in sys.argv:
        for i, (cx, cy) in enumerate([(0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1)]):
            ov.convert("RGB").crop((cx * 1536, cy * 1536, cx * 1536 + 1536, cy * 1536 + 1536)).resize((1024, 1024), Image.LANCZOS).save(os.path.join(VIS, f"composite-crop-{cy}{cx}.jpg"), quality=86)

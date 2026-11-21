"""Preview of a dark 2.5D dungeon: ground + props (as MapScenery draws them in 2D, pivot bottom-centre on the footprint), the solid tiles in red,
the lights as rings, the boss tile and portals; prints PROBLEM lines (first: Rootburrow, 21/11/2026).

    python Tools/preview-map-25d-dungeon.py rootburrow [OUT.jpg] [--minimap]    (--minimap also writes <ground>-minimap.png)

Checks: a blocking prop (FootH > 0) on a walkable tile (it would change collision: the server rows), a prop over a portal or the start, a tile the
start cannot reach (flood fill on the rows + prop footprints).
"""
import os, re, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
T = 64
def _floor(cls, key, start, boss, portals):
    return dict(cls=cls, size=(72, 48), ground="village/tiles/" + key + "-ground-r{r}c{c}.png", chunks=(2, 2), start=start, boss=boss, portals=portals,
                minimap="village/tiles/" + key + "-ground-minimap.png", out=key)
MAPS = {  # V4 three floors (21/11/2026)
    "rootburrow": _floor("Rootburrow", "rootburrow", (68, 23), None, [(71, 22), (71, 23), (71, 24), (4, 32), (5, 32), (6, 32)]),
    "rootburrow2": _floor("RootburrowB2", "rootburrow2", (66, 8), None, [(65, 5), (66, 5), (67, 5), (5, 5), (6, 5), (7, 5)]),
    "rootburrow3": _floor("RootburrowB3", "rootburrow3", (67, 36), (38, 21), [(66, 33), (67, 33), (68, 33), (1, 22), (1, 23), (1, 24)]),
}
LIGHTS = {"rbRuneOrb": ((255, 196, 122), 3.2), "rbRuneObelisk": ((115, 221, 229), 3.8), "rbPurpleCrystal": ((181, 231, 194), 2.0),
          "rbGlowShrooms": ((181, 231, 194), 1.4), "rbPinkCrystal": ((245, 189, 103), 2.4), "rbCrystalNest": ((245, 189, 103), 2.4), "rbRuneSeal": ((174, 144, 226), 3.0)}


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    cfg = MAPS[key]
    out = sys.argv[2] if len(sys.argv) > 2 and not sys.argv[2].startswith("--") else os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "rootburrow", "dark-25d", key + "-composite.jpg")
    w, h = cfg["size"]
    maps = os.path.join(ROOT, "Assets", "Scripts", "Maps")
    rows = re.findall(r'"([TgwBd]{%d})"' % w, open(os.path.join(maps, cfg["cls"] + "Map.cs"), encoding="utf8").read())
    props = [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'P\("(\w+)", (\d+), (\d+)\)', open(os.path.join(maps, cfg["cls"] + "Props.cs"), encoding="utf8").read())]
    defs = {m[0]: (float(m[1]), float(m[2]), int(m[3]), int(m[4])) for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', open(os.path.join(maps, "VillagePropDefs.cs"), encoding="utf8").read())}
    solid = np.array([[c in "TwB" for c in r] for r in rows])
    res = os.path.join(ROOT, "Assets", "Resources", "Sprites")
    nr, nc = cfg["chunks"]
    img = Image.new("RGBA", (w * T, h * T))
    for r in range(nr):
        for c in range(nc):
            part = Image.open(os.path.join(res, cfg["ground"].format(r=r, c=c))).convert("RGBA"); img.paste(part, (c * part.width, r * part.height))
    problems = []
    blocked = solid.copy()
    for t, x, y in sorted(props, key=lambda p: p[2]):
        if t not in defs: problems.append("unknown prop %s" % t); continue
        W, H, fw, fh = defs[t]
        if fh > 0:
            for yy in range(y - fh + 1, y + 1):
                for xx in range(x, x + fw):
                    if not solid[yy, xx]: problems.append("PROBLEM blocking %s at (%d,%d) on walkable (%d,%d)" % (t, x, y, xx, yy))
                    blocked[yy, xx] = True
        for px, py in cfg["portals"] + [cfg["start"]]:
            if x <= px < x + fw and y - max(fh, 1) + 1 <= py <= y: problems.append("PROBLEM %s on portal / start (%d,%d)" % (t, px, py))
        p = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props", t + ".png")
        if not os.path.exists(p): continue
        s = Image.open(p).convert("RGBA")
        cx, by = int((x + fw / 2.0) * T), (y + 1) * T
        img.alpha_composite(s, (cx - s.width // 2, by - s.height))
    if "--minimap" in sys.argv and "minimap" in cfg:                    # ground + props, 6 px per tile (the chunked map has no single ground image)
        mm = img.convert("RGB").resize((w * 6, h * 6), Image.LANCZOS); mm.save(os.path.join(res, cfg["minimap"])); print("minimap", cfg["minimap"])
    # flood fill from the start on the blocked grid
    seen = np.zeros_like(blocked); sx, sy = cfg["start"]; stack = [(sx, sy)]
    while stack:
        x, y = stack.pop()
        if not (0 <= x < w and 0 <= y < h) or seen[y, x] or blocked[y, x]: continue
        seen[y, x] = True; stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    lost = [(x, y) for y in range(h) for x in range(w) if not blocked[y, x] and not seen[y, x]]
    if lost: problems.append("PROBLEM %d walkable tiles cut off: %s" % (len(lost), lost[:8]))
    over = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(over)
    for y in range(h):
        for x in range(w):
            if blocked[y, x]: d.rectangle([x * T, y * T, x * T + T - 1, y * T + T - 1], fill=(255, 0, 0, 70))
    for t, x, y in props:
        if t in LIGHTS:
            c, r = LIGHTS[t]; fw = defs[t][2]; cx, cy = (x + fw / 2.0) * T, (y + 0.4) * T
            d.ellipse([cx - r * T, cy - r * T, cx + r * T, cy + r * T], outline=c + (255,), width=3)
    bx, by = cfg["boss"] or (-5, -5); d.rectangle([bx * T, by * T, bx * T + T, by * T + T], outline=(255, 220, 80, 255), width=6)
    for px, py in cfg["portals"]: d.rectangle([px * T, py * T, px * T + T, py * T + T], outline=(80, 255, 255, 255), width=5)
    img.alpha_composite(over)
    img.convert("RGB").resize((w * 32, h * 32), Image.LANCZOS).save(out, quality=88)
    print("\n".join(problems) or "no problems"); print(out)


if __name__ == "__main__":
    main()

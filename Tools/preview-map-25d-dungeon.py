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
# Snow / ice 2.5D maps (21/11/2026): rows + several prop tables; `generated` = the tables the 2.5D tools wrote (checked: they must not change the blocked set)
MAPS["frost-hollow"] = dict(cls="FrostHollow", rows_file="FrostHollowLayout.cs", row_chars="CTwBs", solid_chars="CTw", size=(72, 48),
                            ground="tiles/frost-hollow-large-ground-r{r}c{c}.png", chunks=(2, 2), start=(4, 21), boss=(7, 5),
                            portals=[(2, 21), (67, 20), (68, 20), (69, 20)], minimap="tiles/frost-hollow-large-ground-minimap.png", out="frost-hollow",
                            props_files=["FrostHollowProps.cs", "FrostHollowFrameProps.cs", "FrostHollowScatterProps.cs", "FrostHollowCliffProps.cs"],
                            generated=["FrostHollowCliffProps.cs"], lib="frost-hollow/snow-25d")
MAPS["frostfang-den"] = dict(cls="FrostfangDen", rows_file="FrostfangDenMap.cs", row_chars="TgdpwB", solid_chars="Tw", size=(36, 24),
                             ground="village/tiles/frostfang-den-ground.png", chunks=(1, 1), start=(3, 10), boss=(26, 11),
                             portals=[(0, 9), (0, 10), (0, 11), (34, 10)], out="frostfang-den",
                             props_files=["FrostfangDenMap.cs", "FrostfangDenProps.cs"], generated=["FrostfangDenProps.cs"], lib="frostfang-den/ice-25d")
LIGHTS = {"fdCrystalTall": ((121, 198, 217), 3.6), "fdCrystalTeal": ((88, 185, 181), 3.0), "fdCrystalWall": ((121, 198, 217), 2.4),
          "fdCrystalShards": ((186, 242, 239), 1.4),"rbRuneOrb": ((255, 196, 122), 3.2), "rbRuneObelisk": ((115, 221, 229), 3.8), "rbPurpleCrystal": ((181, 231, 194), 2.0),
          "rbGlowShrooms": ((181, 231, 194), 1.4), "rbPinkCrystal": ((245, 189, 103), 2.4), "rbCrystalNest": ((245, 189, 103), 2.4), "rbRuneSeal": ((174, 144, 226), 3.0)}


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    cfg = MAPS[key]
    out = sys.argv[2] if len(sys.argv) > 2 and not sys.argv[2].startswith("--") else os.path.join(ROOT, "Assets", "ArtLibrary", "maps", cfg.get("lib", "rootburrow/dark-25d"), key + "-composite.jpg")
    w, h = cfg["size"]
    maps = os.path.join(ROOT, "Assets", "Scripts", "Maps")
    rows = re.findall(r'"([%s]{%d})"' % (cfg.get("row_chars", "TgwBd"), w), open(os.path.join(maps, cfg.get("rows_file", cfg["cls"] + "Map.cs")), encoding="utf8").read())
    prop_re = r'(?:P|new MapProp)\("(\w+)", (\d+), (\d+)\)'
    props, gen = [], set()
    for f in cfg.get("props_files", [cfg["cls"] + "Props.cs"]):
        found = [(m[0], int(m[1]), int(m[2])) for m in re.findall(prop_re, open(os.path.join(maps, f), encoding="utf8").read())]
        props += found
        if f in cfg.get("generated", []): gen.update(found)
    defs = {}
    for f in ("VillagePropDefs.cs", "FrostHollowPropDefs.cs"):
        for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', open(os.path.join(maps, f), encoding="utf8").read()):
            defs.setdefault(m[0], (float(m[1]), float(m[2]), int(m[3]), int(m[4])))
    solid = np.array([[c in cfg.get("solid_chars", "TwB") for c in r] for r in rows])
    res = os.path.join(ROOT, "Assets", "Resources", "Sprites")
    nr, nc = cfg["chunks"]
    img = Image.new("RGBA", (w * T, h * T))
    for r in range(nr):
        for c in range(nc):
            part = Image.open(os.path.join(res, cfg["ground"].format(r=r, c=c))).convert("RGBA"); img.paste(part.resize((w * T // nc, h * T // nr)), (c * (w * T // nc), r * (h * T // nr)))
    problems = []
    blocked = solid.copy()
    hand = solid.copy()                                                  # rows + the hand-placed tables only (what the server had before 2.5D)
    for t, x, y in sorted(props, key=lambda p: p[2]):
        if t not in defs: problems.append("unknown prop %s" % t); continue
        W, H, fw, fh = defs[t]
        if fh > 0:
            for yy in range(y - fh + 1, y + 1):
                for xx in range(x, x + fw):
                    if not solid[yy, xx] and ((t, x, y) in gen or not gen): problems.append("PROBLEM blocking %s at (%d,%d) on walkable (%d,%d)" % (t, x, y, xx, yy))
                    blocked[yy, xx] = True
                    if (t, x, y) not in gen: hand[yy, xx] = True
        for px, py in cfg["portals"] + [cfg["start"]]:
            if x <= px < x + fw and y - max(fh, 1) + 1 <= py <= y: problems.append("PROBLEM %s on portal / start (%d,%d)" % (t, px, py))
        p = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props", t + ".png")
        if not os.path.exists(p): continue
        s = Image.open(p).convert("RGBA")
        cx, by = int((x + fw / 2.0) * T), (y + 1) * T
        img.alpha_composite(s, (cx - s.width // 2, by - s.height))
    # V5 chests + warp crystal (Assets/Scripts/Rpg/DungeonPoints.cs, drawn at run time by DungeonPointView): on a solid tile, next to a reachable floor tile
    if gen and (blocked != hand).any():
        problems.append("PROBLEM generated props change the blocked set (server rows): %s" % [(int(x), int(y)) for y, x in zip(*np.nonzero(blocked != hand))][:10])
    elif gen: print("blocked set unchanged by the generated 2.5D props (no server rebuild for collision)")
    dp = open(os.path.join(ROOT, "Assets", "Scripts", "Rpg", "DungeonPoints.cs"), encoding="utf8").read()
    chest_rows = re.findall(r'new ChestDef\("[\w-]+", "(\w+)", (\d+), (\d+), \d+, (true|false)\)', dp)
    points = [("rbChestRare" if r == "true" else "rbChest", int(x), int(y), None) for mid, x, y, r in chest_rows if mid == key]
    points += [("rbWarpCrystal", int(x), int(y), (int(ax), int(ay))) for mid, x, y, ax, ay in re.findall(r'new WaypointDef\("[\w-]+", "(\w+)", "[^"]*", (\d+), (\d+), (\d+), (\d+)\)', dp) if mid == key]
    for t, x, y, arrive in points:
        if not solid[y, x]: problems.append("PROBLEM %s at (%d,%d) is not on a solid tile" % (t, x, y))
        if not any(0 <= y + dy < h and 0 <= x + dx < w and not solid[y + dy, x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): problems.append("PROBLEM %s at (%d,%d) has no floor next to it" % (t, x, y))
        if arrive and blocked[arrive[1], arrive[0]]: problems.append("PROBLEM warp arrival (%d,%d) is blocked" % arrive)
        sp = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props", t + ".png")
        if os.path.exists(sp):
            sprite = Image.open(sp).convert("RGBA")
            img.alpha_composite(sprite, (int((x + 0.5) * T) - sprite.width // 2, (y + 1) * T - sprite.height))
    print("dungeon points", [(t, x, y) for t, x, y, _ in points])
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

"""Taurus V4 phase 04: props of the two 2.5D Taurus maps (Willowbrook Village V2 100x100, Clover Meadows 96x64): size, place, check, preview.

    python Tools/place-tau-25d-props.py village|meadows [--minimap]

Art: ChatGPT prop sheets (production chat, words of the design chat: docs/reference/taurus-v4-art/taurus-environment-production-brief-chatgpt.txt) cut by
Tools/cut-magenta-prop-sheet.py into Assets/ArtLibrary/maps/taurus/props/cut/<name>.png (sheet scale). This tool:
  1. SIZE: resizes every used cut PNG to its width in tiles (64 px / tile), bottom-centre anchor kept, into Assets/Resources/Sprites/village/props/<name>.png
     (+ a .meta copied from wfOakTree's import settings, new guid, only when missing) and writes Assets/Scripts/Maps/TauPropDefs.cs (W, H from the PNG).
  2. PLACE: village = hand list (each building on its greybox block); meadows = generated (tauprops_meadows below). Writes Assets/Scripts/Maps/Tau<Map>Props.cs.
  3. CHECK (prints PROBLEM): a blocking footprint must be inside the map and either already solid in the rows or plain grass off every road / plaza /
     bridge / camp rect / portal / NPC (+1 tile); flat decor never on a portal or NPC tile; every solid tile of the rows (border and HUD rows excepted) is
     covered by some prop sprite (else it is an invisible wall: docs/map-design-rules.md "Map 2.5D" 7).
  4. PREVIEW: top-down composite (ground + props, back to front) and the same with solid tiles in red, docs/reference/taurus-v4-art/<map>-composite*.jpg;
     --minimap also writes <ground>-minimap.png (6 px / tile) for the chunked ground.
Props with FootH > 0 on walkable tiles are blocked by the map class (BlockTiles over TauPropDefs), so collision = rows + those small posts.
"""
import os, re, sys, math, uuid
from PIL import Image, ImageDraw
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CUT = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "taurus", "props", "cut")
PROPS = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
MAPS = os.path.join(ROOT, "Assets", "Scripts", "Maps")
REF = os.path.join(ROOT, "docs", "reference", "taurus-v4-art")
T = 64
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tau_25d_props_layout import SIZE, meadows_props, camp_rects   # noqa: E402  (layout data, kept apart so this file stays a tool)
from tau_village_props_layout import village_props   # noqa: E402  (Willowbrook Village V2 160x160)

CFG = {
    "village": dict(cls="TauWillowbrookVillage", rows="TauWillowbrookVillageMap.cs", size=(100, 100), ground="tau-willowbrook-village-ground", chunks=(5, 5),
                    place=village_props, free_rows=(0, 97), camps=False),
    "meadows": dict(cls="TauCloverMeadows", rows="TauCloverMeadowsMap.cs", size=(96, 64), ground="tau-clover-meadows-ground", chunks=(4, 4),
                    place=meadows_props, free_rows=(0, 61), camps=True),
}
SOLID = "Ttwx"


def flat(name, fh, h): return fh == 0 and (h <= 3 or "Bridge" in name)   # MapScenery.LiesFlat


def flat_of(name, defs): return flat(name, defs[name][3], defs[name][1])


def rows_of(cfg):
    w, h = cfg["size"]
    s = open(os.path.join(MAPS, cfg["rows"]), encoding="utf-8").read()
    s2 = s[s.index("// BEGIN ROWS"):s.index("// END ROWS")]
    rows = re.findall(r'"([A-Za-z]{%d})"' % w, s2); assert len(rows) == h
    npcs = [(int(m[0]), int(m[1])) for m in re.findall(r'new MapNpc\("\w+", (\d+), (\d+)\)', s)]
    portals = [(int(m[0]), int(m[1])) for m in re.findall(r"new MapPortal\((\d+), (\d+),", s)]
    return rows, npcs, portals


def sized(name):
    """Resize the cut PNG to SIZE[name][0] tiles wide (bottom-centre anchor kept), 16 px padded; returns (W, H) in tiles."""
    wt, fw, fh = SIZE[name][:3]
    sy = SIZE[name][3] if len(SIZE[name]) > 3 else 1.0   # vertical stretch: a flat bridge drawn foreshortened for the 58 deg view, laid on the ground, must span its stream
    src = Image.open(os.path.join(CUT, name + ".png")).convert("RGBA")
    a = np.asarray(src)[..., 3]; cols = np.nonzero(a.max(0) > 40)[0]
    k = wt * T / float(cols[-1] - cols[0] + 1)
    im = src.resize((max(1, round(src.width * k)), max(1, round(src.height * k * sy))), Image.LANCZOS)
    W = int(math.ceil(im.width / 16.0) * 16); H = int(math.ceil(im.height / 16.0) * 16)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0)); canvas.alpha_composite(im, ((W - im.width) // 2, H - im.height))
    canvas.save(os.path.join(PROPS, name + ".png"), optimize=True)
    meta = os.path.join(PROPS, name + ".png.meta")
    if not os.path.exists(meta):
        tpl = open(os.path.join(PROPS, "wfOakTree.png.meta"), encoding="utf-8").read()
        open(meta, "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, tpl, count=1))
    return W / float(T), H / float(T)


def write_defs(defs):
    """TauPropDefs.cs holds every tau* prop of both maps: re-read the other map's entries so one run never drops them."""
    path = os.path.join(MAPS, "TauPropDefs.cs")
    old = {}
    if os.path.exists(path):
        for m in re.findall(r'\{ "(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\) \}', open(path, encoding="utf-8").read()):
            old[m[0]] = (float(m[1]), float(m[2]), int(m[3]), int(m[4]))
    old.update(defs)
    lines = "".join('            { "%s", new PropDef(%gf, %gf, %d, %d) },\n' % (n, w, h, fw, fh) for n, (w, h, fw, fh) in sorted(old.items()))
    open(path, "w", encoding="utf-8", newline="\n").write(
        "using System.Collections.Generic;\n\nnamespace ZenoniaProto\n{\n    /// <summary>\n"
        "    /// PropDefs of the Taurus maps (Willowbrook Village, Clover Meadows), GENERATED by Tools/place-tau-25d-props.py from the ChatGPT prop sheets\n"
        "    /// (art in Sprites/village/props/tau*.png, W x H = PNG px / 64). Trees block their trunk row, buildings their 2 bottom rows, walls / gate posts their tiles;\n"
        "    /// FootH 0 = walk-through (flat ground decor when H <= 3). VillagePropDefs.TryGet falls back to this table.\n    /// </summary>\n"
        "    public static class TauPropDefs\n    {\n        static readonly Dictionary<string, PropDef> Defs = new Dictionary<string, PropDef>\n        {\n"
        + lines + "        };\n\n        public static bool TryGet(string type, out PropDef def) { return Defs.TryGetValue(type, out def); }\n    }\n}\n")
    return old


def write_table(cfg, props):
    cls = cfg["cls"] + "Props"
    body = ""
    for i in range(0, len(props), 6):
        body += "            " + " ".join('P("%s", %d, %d),' % p for p in props[i:i + 6]) + "\n"
    open(os.path.join(MAPS, cls + ".cs"), "w", encoding="utf-8", newline="\n").write(
        "namespace ZenoniaProto\n{\n    /// <summary>\n    /// Props of " + cfg["cls"].replace("Tau", "") + " (Taurus V4 phase 04), GENERATED by Tools/place-tau-25d-props.py (layout: Tools/tau_25d_props_layout.py): [type, tx, ty] =\n"
        "    /// bottom-left footprint tile (MapScenery y-sorts by it; defs in TauPropDefs). Re-run the tool rather than editing by hand: it checks footprints against the rows.\n    /// </summary>\n"
        "    public static class " + cls + "\n    {\n        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }\n\n"
        "        public static readonly MapProp[] All =\n        {\n" + body + "        };\n    }\n}\n")


def check(cfg, rows, npcs, portals, props, defs):
    W, H = cfg["size"]; ok = True
    camps = camp_rects() if cfg["camps"] else []
    in_camp = lambda x, y: any(cx <= x < cx + cw and cy <= y < cy + ch for cx, cy, cw, ch in camps)
    near = lambda x, y, pts, d: any(abs(x - px) <= d and abs(y - py) <= d for px, py in pts)
    covered = np.zeros((H, W), bool)
    for name, tx, ty in props:
        w, h, fw, fh = defs[name]
        cx = tx + fw / 2.0
        x0, x1 = int(math.floor(cx - w / 2.0 + 0.35)), int(math.ceil(cx + w / 2.0 - 0.35))
        y0 = int(math.floor(ty + 1 - (h if flat(name, fh, h) else h / 0.85) + 0.5))
        covered[max(0, y0):ty + 1, max(0, x0):min(W, x1)] = True
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if not (0 <= x < W and 0 <= y < H): print("PROBLEM %s (%d,%d) outside" % (name, tx, ty)); ok = False; continue
                c = rows[y][x]
                if c in SOLID: continue
                if c in "dpBk" or in_camp(x, y) or near(x, y, portals, 2) or near(x, y, npcs, 1):
                    print("PROBLEM %s (%d,%d) blocks %s tile (%d,%d)" % (name, tx, ty, c, x, y)); ok = False
        if fh == 0 and (near(tx, ty, portals, 0) or near(tx, ty, npcs, 0)): print("PROBLEM %s on a portal / NPC tile" % name); ok = False
    a, b = cfg["free_rows"]
    bare = [(x, y) for y in range(a + 1, b) for x in range(1, W - 1) if rows[y][x] in "Tt" and not covered[y, x]]
    if bare: print("PROBLEM %d solid tiles with no prop over them, e.g. %s" % (len(bare), bare[:12])); ok = False
    print("props %d, problems: %s" % (len(props), "none" if ok else "see above"))
    return ok


def preview(key, cfg, rows, props, defs, minimap):
    W, H = cfg["size"]; s = 0.25
    g = Image.open(os.path.join(ROOT, "Assets", "ArtLibrary", "maps", "taurus", "ground", cfg["ground"] + "-full.jpg")).convert("RGBA")
    g = g.resize((int(W * T * s), int(H * T * s)), Image.LANCZOS)
    flat = [p for p in props if flat_of(p[0], defs)]
    stand = sorted([p for p in props if p not in flat], key=lambda p: p[2])
    cache = {}
    for name, tx, ty in flat + stand:
        w, h, fw, fh = defs[name]
        if name not in cache:
            im = Image.open(os.path.join(PROPS, name + ".png")); cache[name] = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)
        im = cache[name]
        g.alpha_composite(im, (int((tx + fw / 2.0) * T * s - im.width / 2), int((ty + 1) * T * s - im.height)))
    os.makedirs(REF, exist_ok=True)
    g.convert("RGB").save(os.path.join(REF, key + "-composite.jpg"), quality=88)
    red = Image.new("RGBA", g.size, (0, 0, 0, 0)); d = ImageDraw.Draw(red); k = T * s
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c in SOLID: d.rectangle([x * k, y * k, (x + 1) * k - 1, (y + 1) * k - 1], fill=(255, 30, 30, 95))
    for name, tx, ty in props:
        w, h, fw, fh = defs[name]
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if 0 <= y < H and 0 <= x < W and rows[y][x] not in SOLID: d.rectangle([x * k, y * k, (x + 1) * k - 1, (y + 1) * k - 1], fill=(255, 140, 0, 130))
    Image.alpha_composite(g, red).convert("RGB").save(os.path.join(REF, key + "-composite-solids.jpg"), quality=88)
    if minimap:
        g.convert("RGB").resize((W * 6, H * 6), Image.LANCZOS).save(os.path.join(TILES, cfg["ground"] + "-minimap.png"), optimize=True)
    print("preview:", os.path.join(REF, key + "-composite.jpg"))


def main():
    key = sys.argv[1]; cfg = CFG[key]
    rows, npcs, portals = rows_of(cfg)
    props = cfg["place"](rows)
    missing = sorted(set(n for n, _, _ in props if not os.path.exists(os.path.join(CUT, n + ".png"))))
    if missing: print("TODO no art yet (skipped):", ", ".join(missing))
    props = [p for p in props if p[0] not in missing]
    used = sorted(set(p[0] for p in props))
    defs = {}
    for n in used:
        w, h = sized(n); defs[n] = (w, h, SIZE[n][1], SIZE[n][2])  # SIZE may carry a 4th value (vertical stretch)
    write_defs(defs)
    write_table(cfg, props)
    ok = check(cfg, rows, npcs, portals, props, defs)
    preview(key, cfg, rows, props, defs, "--minimap" in sys.argv)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

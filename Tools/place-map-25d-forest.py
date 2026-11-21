"""Places the trees of a 2.5D forest map (first: Willowbrook Meadows, 21/11/2026) and writes them as a C# prop table + a forest-floor mask.

    python Tools/place-map-25d-forest.py willowbrook-meadows [--seed N]

Rules (docs/map-design-rules.md "Map 2.5D", ChatGPT art direction docs/reference/willowbrook-25d/willowbrook-forest-art-direction-chatgpt.md):
- Trees form belts around the open ground: a tile is "forest" when it is far enough from every walking lane, spawn-cluster rect, water tile, portal and
  the start tile (distance >= FOREST_DIST tiles). Trunks go on forest tiles with a minimum spacing (Poisson-like, seeded), plus a dense row on the
  solid border (north rows, east cols, the row under the HUD) whose trunks never change the collision.
- A trunk never lands on a lane, inside a cluster rect (+1), next to water, on another prop's footprint (+1) or near a gate / the start (rule 1, 5).
- After placing, a flood fill from the start tile: a tree that cuts off walkable tiles is removed (rule 6).
- Type by place: willow near water, pines + birches in the north-east ruins, big / ancient oaks on the borders, young broadleaf near the open ground.
Writes Assets/Scripts/Maps/<Class>ForestProps.cs (table `Forest`), Assets/ArtLibrary/maps/<map>/forest-25d/<map>-forest-mask.png (1 px per tile,
255 = deep forest, used by Tools/build-map-25d-ground.py for the darker forest floor) and prints a summary.
To convert another map: add an entry to MAPS (its rows file, prop tables, lane file, spawn block, start, portals, solid border lines).
"""
import os, re, sys, random
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS_DIR = os.path.join(ROOT, "Assets", "Scripts", "Maps")

MAPS = {
    "willowbrook-meadows": dict(
        cls="WillowbrookMeadows", rows_file="WillowbrookMeadowsMap.cs", size=(72, 48),
        tables=["WillowbrookMeadowsProps.cs", "WillowbrookMeadowsFrameProps.cs", "WillowbrookMeadowsScatterProps.cs"],
        lanes="plans/261105-1052-willowbrook-large-hunting-map/layout/willowbrook-72x48-lanes.txt",
        spawn_block="WillowbrookMeadows =", start=(3, 23), keep_clear=[(0, 23), (0, 24), (46, 44), (47, 44), (47, 42)],
        border_rows=[1, 45], border_cols=[0, 70], ruins=(42, 0, 72, 13),
        extra=[("wfOakTree", 1, 20)],  # hand-placed: covers the bare solid border tiles north of the west gate
    ),
}

FOREST_DIST = 2.5    # tiles from lanes / clusters / water / gates where the forest starts
EDGE_DIST = 1.5      # tiles: a lighter edge band (young trees, sparse)
SPACING = {"wfOakAncientTree": 3.4, "wfOakTree": 2.8, "wfOakTreeB": 2.8, "wfPineTree": 2.3, "wfBirchTree": 2.4, "wfWillowTree": 2.8, "wfOakYoungTree": 2.2}


def rd(n): return open(os.path.join(MAPS_DIR, n), encoding="utf8").read()


def load(cfg):
    w, h = cfg["size"]
    rows = re.findall(r'"([TgwBd]{%d})"' % w, rd(cfg["rows_file"]))
    assert len(rows) == h, len(rows)
    src = re.sub(r"//.*", "", "".join(rd(n) for n in cfg["tables"]))
    props = [(m[0], int(m[1]), int(m[2])) for m in re.findall(r'P\("(\w+)",\s*(\d+),\s*(\d+)\)', src)]
    defs = {m[0]: (float(m[1]), float(m[2]), int(m[3]), int(m[4])) for m in re.findall(r'"(\w+)", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\)', rd("VillagePropDefs.cs"))}
    lanes = [l.rstrip("\n") for l in open(os.path.join(ROOT, cfg["lanes"])) if l.strip()]
    spawn = open(os.path.join(ROOT, "Assets", "Scripts", "MapSpawnTable.cs"), encoding="utf8").read()
    a = spawn.index(cfg["spawn_block"]); spawn = spawn[a:spawn.index("};", a)]
    zones = [(int(m[2]), int(m[3]), int(m[4]), int(m[5])) for m in re.findall(r'Zone\("(\w+)", (\d+), (\d+), (\d+), (\d+), (\d+), (\d+)f\)', spawn)]
    return rows, props, defs, lanes, zones


def dist_field(seed_mask):
    """Euclidean distance (tiles) to the nearest True tile (brute force over seeds; maps are small)."""
    h, w = seed_mask.shape
    ys, xs = np.nonzero(seed_mask)
    gy, gx = np.mgrid[0:h, 0:w]
    d = np.full((h, w), 1e9)
    for y, x in zip(ys, xs): d = np.minimum(d, np.hypot(gx - x, gy - y))
    return d


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "willowbrook-meadows"
    seed = int(sys.argv[sys.argv.index("--seed") + 1]) if "--seed" in sys.argv else 21
    cfg = MAPS[key]; W, H = cfg["size"]
    rows, props, defs, lanes, zones = load(cfg)
    rng = random.Random(seed)
    walk = np.array([[c in "gBd" for c in r] for r in rows])
    water = np.array([[c == "w" for c in r] for r in rows])
    lane = np.array([[c == "r" for c in r[:W].ljust(W, ".")] for r in lanes[:H]])
    solid_prop = np.zeros((H, W), bool)
    for t, tx, ty in props:
        if t not in defs: continue
        _, _, fw, fh = defs[t]
        for y in range(ty - max(fh, 1) + 1, ty + 1):
            for x in range(tx, tx + fw):
                if 0 <= x < W and 0 <= y < H: solid_prop[y, x] = True
    zone_m = np.zeros((H, W), bool)
    for zx, zy, zw, zh in zones: zone_m[zy:zy + zh, zx:zx + zw] = True
    gates = np.zeros((H, W), bool)
    for x, y in cfg["keep_clear"] + [cfg["start"]]: gates[y, x] = True

    open_d = np.minimum(np.minimum(dist_field(lane), dist_field(zone_m)), dist_field(water) + 0.5)
    gate_d = dist_field(gates)
    prop_d = dist_field(solid_prop)
    forest = (open_d >= FOREST_DIST) & (gate_d >= 5)
    edge = (open_d >= EDGE_DIST) & ~forest & (gate_d >= 5)

    trunks = []
    def free(x, y, sp):
        return all((x - a) ** 2 + (y - b) ** 2 >= (max(sp, SPACING[t]) * 0.9) ** 2 for t, a, b in trunks)

    def pick(x, y, border):
        rx0, ry0, rx1, ry1 = cfg["ruins"]
        if open_d[y, x] < 99 and dist_field_cache["water"][y, x] <= 3.2 and rng.random() < 0.4: return "wfWillowTree"
        if rx0 <= x < rx1 and ry0 <= y < ry1: return rng.choice(["wfPineTree", "wfPineTree", "wfBirchTree", "wfOakYoungTree"])
        if border: return rng.choice(["wfOakTree", "wfOakTreeB", "wfOakAncientTree", "wfPineTree", "wfOakTree"])
        if edge[y, x]: return rng.choice(["wfOakYoungTree", "wfOakYoungTree", "wfBirchTree", "wfOakTree"])
        r = rng.random()
        return "wfOakTree" if r < 0.32 else "wfOakTreeB" if r < 0.55 else "wfOakAncientTree" if r < 0.65 else "wfPineTree" if r < 0.82 else "wfBirchTree" if r < 0.9 else "wfOakYoungTree"

    dist_field_cache = {"water": dist_field(water)}
    # 1) solid border: trunks on rows / cols that are solid anyway (collision unchanged)
    for y in cfg["border_rows"]:
        for x in range(1, W - 1, 3):
            if gate_d[min(y + 1, H - 1), x] < 4: continue
            t = pick(x, y, True)
            if free(x, y, SPACING[t]): trunks.append((t, x + rng.choice([0, 0, 1]), y))
    for x in cfg["border_cols"]:
        for y in range(3, H - 3, 3):
            if gate_d[y, x] < 4: continue
            t = pick(x, y, True)
            if free(x, y, SPACING[t]): trunks.append((t, x, y))
    # 2) forest tiles, deepest first so the belts fill from the inside; then a sparse edge band
    cand = [(x, y) for y in range(2, H - 3) for x in range(1, W - 2) if walk[y, x] and not solid_prop[y, x] and prop_d[y, x] >= 1.5 and (forest[y, x] or edge[y, x])]
    rng.shuffle(cand)
    cand.sort(key=lambda p: -open_d[p[1], p[0]])
    for x, y in cand:
        if edge[y, x] and rng.random() < 0.35: continue
        t = pick(x, y, False)
        if free(x, y, SPACING[t]): trunks.append((t, x, y))

    for t, x, y in cfg.get("extra", []): trunks.append((t, x, y))
    # 3) flood fill from the start: drop trees that cut off walkable tiles (rule 6)
    def reachable(tr):
        blocked = walk.copy() & ~solid_prop
        for t, x, y in tr:
            if walk[y, x]: blocked[y, x] = False
        seen = np.zeros_like(blocked); st = [cfg["start"]]
        while st:
            x, y = st.pop()
            if not (0 <= x < W and 0 <= y < H) or seen[y, x] or not blocked[y, x]: continue
            seen[y, x] = True; st += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
        return blocked & ~seen
    base_orphans = reachable([]).sum()
    while True:
        orphan = reachable(trunks)
        if orphan.sum() <= base_orphans: break
        oy, ox = np.argwhere(orphan)[0]
        trunks.sort(key=lambda p: (p[1] - ox) ** 2 + (p[2] - oy) ** 2)
        print("  drop", trunks[0], "cut off", (ox, oy)); trunks.pop(0)

    trunks.sort(key=lambda p: (p[2], p[1]))
    out = os.path.join(MAPS_DIR, cfg["cls"] + "ForestProps.cs")
    lines = []
    for i in range(0, len(trunks), 6):
        lines.append("            " + " ".join('P("%s", %d, %d),' % t for t in trunks[i:i + 6]))
    with open(out, "w", encoding="utf8", newline="\r\n") as f:
        f.write("namespace ZenoniaProto\n{\n    /// <summary>\n"
                "    /// Every tree of the 2.5D forest (21/11/2026), generated by Tools/place-map-25d-forest.py %s (seed %d): belts on the ground far from lanes, spawn rects,\n"
                "    /// water and gates, a dense row on the solid border, willows by the water, pines and birches round the ruins. Trees block their trunk tile only.\n"
                "    /// Re-run the tool rather than editing by hand (it checks lanes, cluster rects, gates and the flood fill), or edit and run the composite preview.\n"
                "    /// </summary>\n    public static partial class %sProps\n    {\n        static readonly MapProp[] Forest =\n        {\n%s\n        };\n    }\n}\n"
                % (key, seed, cfg["cls"], "\n".join(lines)))
    mask = np.clip((open_d - EDGE_DIST) / (FOREST_DIST + 2 - EDGE_DIST), 0, 1)
    mask[~walk & ~water] = 1.0  # solid border reads as deep forest
    mask[water] = 0
    lib = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "forest-25d"); os.makedirs(lib, exist_ok=True)
    Image.fromarray((mask * 255).astype(np.uint8)).save(os.path.join(lib, key + "-forest-mask.png"))
    from collections import Counter
    print(out, len(trunks), "trees", dict(Counter(t for t, _, _ in trunks)))


if __name__ == "__main__":
    main()

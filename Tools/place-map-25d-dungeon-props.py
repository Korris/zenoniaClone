"""Places the standing walls, crystals, lanterns and floor decor of a dark 2.5D dungeon (first: Rootburrow, 21/11/2026) and writes the C# prop table.

    python Tools/place-map-25d-dungeon-props.py rootburrow [--seed N]

Rules (docs/map-design-rules.md "Map 2.5D" + the dungeon rules, ChatGPT art direction
docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md section B):
- The rows never change: every BLOCKING prop stands on a tile that is already solid ('T'), so collision, the server and the flood fill stay as they were.
- Wall line = solid tiles touching the floor. NORTH of a floor tile (the floor runs south of it): tall pieces (root walls 3 wide, pillars, giant glow
  mushrooms, crystals, lanterns): they stand behind the hero. SOUTH of the floor (the floor is north of it): only low walk-through pieces lying flat
  (boulders, stones, small mushrooms), because anything tall there stands between the camera and the hero; tall pieces again from 3 rows south on.
  East / west sides: pillars, crystals, mushrooms, lanterns.
- Inside the solid mass (2+ tiles from the floor): sparse big roots / pillars / mushrooms so the cave reads as a mass of roots, not a flat floor.
- Lights (DungeonLights reads them by name): a lantern every ~10 tiles of wall, a cyan crystal every ~14, glow mushrooms every ~8.
- Floor decor (walk-through, FootH 0): small mushrooms, pebbles, short root logs along the walls, never on a portal (+2), the start (+2), the
  boss tile (+3) or a spawn rect's middle; the boss hall gets a rune floor seal in front of the boss.
Writes Assets/Scripts/Maps/<Class>Props.cs (table `All`) and prints a summary. Preview: Tools/preview-map-25d-dungeon.py.
"""
import os, re, sys, random
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAPS_DIR = os.path.join(ROOT, "Assets", "Scripts", "Maps")
MAPS = {  # V4 three-floor Rootburrow (21/11/2026): stairs mouths are fixed props on the solid tile north of a tunnel's end
    "rootburrow": dict(cls="Rootburrow", rows_file="RootburrowMap.cs", size=(72, 48), start=(68, 23), boss=None, hud_rows=45,
                       portals=[(71, 22), (71, 23), (71, 24), (4, 32), (5, 32), (6, 32)], fixed=[("rbStairsDown", 4, 31)],
                       reserved=[(57, 43), (38, 2), (15, 5), (68, 21)]),
    "rootburrow2": dict(cls="RootburrowB2", rows_file="RootburrowB2Map.cs", size=(72, 48), start=(66, 8), boss=None, hud_rows=45,
                        portals=[(65, 5), (66, 5), (67, 5), (5, 5), (6, 5), (7, 5)], fixed=[("rbStairsUp", 65, 4), ("rbStairsDown", 5, 4)],
                        reserved=[(55, 42), (33, 6), (23, 5), (68, 9)]),
    "rootburrow3": dict(cls="RootburrowB3", rows_file="RootburrowB3Map.cs", size=(72, 48), start=(67, 36), boss=(38, 21), hud_rows=45, arena=(12, 22),
                        portals=[(66, 33), (67, 33), (68, 33), (1, 22), (1, 23), (1, 24)], fixed=[("rbStairsUp", 66, 32)],
                        reserved=[(54, 40), (45, 6), (24, 40), (69, 37)]),
}
# reserved = the solid tiles of the V5 chests + warp crystal (Assets/Scripts/Rpg/DungeonPoints.cs, drawn at run time by DungeonPointView): no prop on or
# right next to them, no floor decor in front of them. Keep in step with DungeonPoints.
TALL_N = ["rbCaveWall", "rbCaveWallB", "rbStonePillar", "rbCaveWall", "rbHangingRoots", "rbCaveWallB"]
TALL_SIDE = ["rbStonePillar", "rbStonePillar", "rbCrystalNest", "rbCaveBoulder"]
LOW = ["rbCaveBoulder", "rbStoneCluster", "rbGlowShrooms", "rbStoneCluster", "rbPebbles"]
DEEP = ["rbStonePillar", "rbCaveWall", "rbCaveWallB", "rbCaveBoulder", "rbHangingRoots"]
FLOOR_DECOR = ["rbGlowShrooms", "rbPebbles", "rbPebbles", "rbStoneSlab", "rbGlowShrooms"]
FOOT_W = {"rbStairsDown": 3, "rbStairsUp": 3, "rbCaveWall": 3, "rbCaveWallB": 3, "rbHangingRoots": 2, "rbCaveBoulder": 2, "rbStoneSlab": 2, "rbRuneSeal": 3}


# Snow / ice maps (21/11/2026, docs/reference/frost-hollow-25d/snow-25d-art-direction-chatgpt.md): same rules, their own pieces. Frost Hollow keeps its
# hand-placed tables (OldSection / Frame / Scatter: forest, camp, towers...) and only gets the standing cliff line + sparse pines on the plateau mass,
# written as FrostHollowCliffProps (the `existing` tables are read so nothing lands on or next to their props).
MAPS["frost-hollow"] = dict(cls="FrostHollowCliff", rows_file="FrostHollowLayout.cs", size=(72, 48), start=(4, 21), boss=None, hud_rows=45,
                            row_chars="CTwBs", solid_chars="CTw", water_chars="w",
                            portals=[(2, 21), (67, 20), (68, 20), (69, 20)], fixed=[("fhIceCave", 67, 17)], reserved=[(7, 5)],
                            existing=["FrostHollowProps.cs", "FrostHollowFrameProps.cs", "FrostHollowScatterProps.cs"],
                            tall_n=["fhCliffA", "fhCliffB", "fhCliffWide", "fhCliffA", "fhCliffB", "fhCliffTall"], tall_n_fallback="fhCliffPillar",
                            tall_side=["fhCliffPillar", "fhCliffEndL", "fhCliffEndR", "fhPineSlim"],
                            low=["fhCliffLow", "fhRock", "fhRockSmall", "fhRock"], deep=["fhPineBig", "fhPineMid", "fhPineSlim", "fhDeadTree", "fhCliffPillar", "fhPineMid"],
                            runs=(["fhCliffWide", "fhCliffWide", "fhCliffTall"], ["fhCliffA", "fhCliffB"], "fhCliffEndL", "fhCliffEndR", "fhCliffPillar"),
                            floor_decor=[], lights=[], deep_rate=(0.22, 0.10), summary="Standing ice-rock cliffs and plateau pines of the 2.5D Frost Hollow")
# Frostfang Den (dim glacial cave): continuous standing ice walls north of the corridors, ice crystals as DungeonLights, the boss arena (Glacier Warden,
# MapSpawnTable FrostfangDenBossTile 26,11) kept clear of floor decor; the map's own hand-placed pillars / stalagmites / crystals / bones stay (existing).
MAPS["frostfang-den"] = dict(cls="FrostfangDen", rows_file="FrostfangDenMap.cs", size=(36, 24), start=(3, 10), boss=None, hud_rows=21, arena=(27, 12),
                             row_chars="TgdpwB", solid_chars="Tw", water_chars="w",
                             portals=[(0, 9), (0, 10), (0, 11), (34, 10)], fixed=[], reserved=[], existing=["FrostfangDenMap.cs"],
                             runs=(["fdIceWallA", "fdIceWallB", "fdCrystalWall"], ["fdIcicleWall"], "fdIceWallEnd", "fdIcePillar", "fdIcePillar"),
                             tall_n=["fdIcePillar"], tall_n_fallback="fdIcePillar", tall_side=["fdIcePillar", "fdCrystalTall", "fdIceWallEnd", "fdIcePillar"],
                             low=["fdIceBoulder", "fdRockCluster", "fdCrystalShards", "fdRockCluster"], deep=["fdIcePillar", "fdCrystalTall", "fdIceBoulder", "fdIceWallA", "fdCrystalTeal"],
                             floor_decor=["fdCrystalShards", "fdRockCluster", "fhRockSmall", "fhRockSlab", "fdRockCluster"],
                             lights=[("fdCrystalTall", 9, False), ("fdCrystalTeal", 9, False)], deep_rate=(0.35, 0.18), arena_seal=False,
                             summary="Standing ice walls, light crystals and floor decor of the dim 2.5D Frostfang Den")
FOOT_W.update({"fdIceWallA": 3, "fdIceWallB": 3, "fdCrystalWall": 3, "fdIcicleWall": 2, "fdIceBoulder": 2})
FOOT_W.update({"fhCliffA": 2, "fhCliffB": 2, "fhCliffWide": 3, "fhCliffTall": 3, "fhCliffLow": 3, "fhIceCave": 3})
ROOTBURROW_LIGHTS = [("rbRuneOrb", 9, True), ("rbRuneObelisk", 13, False), ("rbPurpleCrystal", 9, False), ("rbPinkCrystal", 11, False)]


def load(cfg):
    w, h = cfg["size"]
    rows = re.findall(r'"([%s]{%d})"' % (cfg.get("row_chars", "TgwBd"), w), open(os.path.join(MAPS_DIR, cfg["rows_file"]), encoding="utf8").read())
    assert len(rows) == h, len(rows)
    solid = np.array([[c in cfg.get("solid_chars", "TwB") for c in r] for r in rows])
    water = np.array([[c in cfg.get("water_chars", "wB") for c in r] for r in rows])
    return solid, water


def existing_props(cfg):
    """(type, x, y) of the map's hand-placed tables (Frost Hollow), so generated pieces keep clear of them."""
    out = []
    for f in cfg.get("existing", []):
        out += [(t, int(x), int(y)) for t, x, y in re.findall(r'(?:P|new MapProp)\("(\w+)", (\d+), (\d+)\)', open(os.path.join(MAPS_DIR, f), encoding="utf8").read())]
    return out


def dist_to_floor(solid):
    """Chebyshev distance (tiles) from every tile to the nearest floor tile (0 on the floor)."""
    h, w = solid.shape
    d = np.where(solid, 99, 0)
    for _ in range(8):
        p = np.pad(d, 1, constant_values=99)
        nb = np.min([p[y:y + h, x:x + w] for y in range(3) for x in range(3)], axis=0)
        d = np.where(solid, np.minimum(d, nb + 1), 0)
    return d


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    cfg = MAPS[key]
    seed = int(sys.argv[sys.argv.index("--seed") + 1]) if "--seed" in sys.argv else 21
    rng = random.Random(seed)
    solid, water = load(cfg)
    h, w = solid.shape
    d = dist_to_floor(solid)
    floor = ~solid
    used = np.zeros_like(solid)
    props = []
    for t, x, y in cfg.get("fixed", []):                               # stairs first: nothing else may stand on or next to them
        props.append((t, x, y)); used[max(0, y - 2):y + 2, max(0, x - 2):x + 5] = True
    for x, y in cfg.get("reserved", []):                              # V5 chests / warp crystal: their tile + one tile of air all round
        used[max(0, y - 1):y + 3, max(0, x - 1):x + 2] = True   # 2 rows south too: a tall piece there would stand in front of it
    hard = used.copy()                                                 # fixed / reserved only (the cliff line may touch the hand-placed props)
    for t, x, y in existing_props(cfg):                                # hand-placed tables: their tile + one tile of air
        used[max(0, y - 1):y + 2, max(0, x - 1):x + 3] = True
    light_rules = cfg.get("lights", ROOTBURROW_LIGHTS)
    lights = {n: [] for n, _, _ in light_rules}
    TALL_N_, TALL_SIDE_, LOW_, DEEP_ = cfg.get("tall_n", TALL_N), cfg.get("tall_side", TALL_SIDE), cfg.get("low", LOW), cfg.get("deep", DEEP)
    FLOOR_DECOR_ = cfg.get("floor_decor", FLOOR_DECOR)

    def free(x, y, fw):
        return x >= 0 and x + fw <= w and 0 <= y < h and not used[y, x:x + fw].any()

    def put(t, x, y, block=True):
        fw = FOOT_W.get(t, 1)
        if not free(x, y, fw): return False
        if block and not solid[y, x:x + fw].all(): return False      # a blocking prop only on solid tiles: rows unchanged
        if solid[y, x:x + fw].any() and water[y, x:x + fw].any(): return False
        if block: used[max(0, y - 1):y + 1, max(0, x - 1):x + fw + 1] = True   # a little air round the standing pieces
        used[y, x:x + fw] = True
        props.append((t, x, y))
        if t in lights: lights[t].append((x, y))
        return True

    def far(t, x, y, gap):
        return all((x - a) ** 2 + (y - b) ** 2 >= gap * gap for a, b in lights[t])

    near_floor_s = lambda x, y: y + 1 < h and floor[y + 1, x]           # floor just south: this is a north wall tile
    floor_north = lambda x, y, k: any(y - j >= 0 and floor[y - j, x] for j in range(1, k + 1))
    tiles = [(x, y) for y in range(h) for x in range(w) if solid[y, x] and y < cfg["hud_rows"]]
    rng.shuffle(tiles)
    def place_lights():
        # 1) light sources, on wall tiles that face the floor from the north or the sides
        for x, y in tiles:
            if not light_rules or d[y, x] != 1 or floor_north(x, y, 2): continue
            for name, gap, south in light_rules:                        # first rule that fits (same order as the Rootburrow if / elif chain)
                if far(name, x, y, gap) and (not south or near_floor_s(x, y)):
                    put(name, x, y); break
    if "runs" not in cfg: place_lights()                                 # Rootburrow: lights first; snow / ice maps: after the continuous wall line
    # 1b) continuous cliff line (snow maps, cfg "runs"): every solid tile with floor just south gets a standing piece, pieces side by side with
    # no air between them (a broken line of separate rocks does not read as a cliff), 3 / 2-wide pieces, a 1-wide end or pillar for the remainder
    if "runs" in cfg:
        occ = np.zeros_like(solid)
        for t, x, y in existing_props(cfg): occ[y, x] = True
        wall = lambda x, y: solid[y, x] and not water[y, x] and near_floor_s(x, y) and not occ[y, x] and not hard[y, x] and y < cfg["hud_rows"]
        r3, r2, r1l, r1r, r1 = cfg["runs"]
        for y in range(h):
            x = 0
            while x < w:
                if not wall(x, y): x += 1; continue
                x1 = x
                while x1 < w and wall(x1, y): x1 += 1
                pos = x
                while pos < x1:
                    left = x1 - pos
                    if left >= 3 and (left == 3 or rng.random() < 0.35): t, fw = rng.choice(r3), 3
                    elif left >= 2: t, fw = rng.choice(r2), 2
                    else: t, fw = (r1l if pos == x and x1 - x > 1 else r1r if x1 - x > 1 else r1), 1
                    props.append((t, pos, y)); used[y, pos:pos + fw] = True
                    if y > 0: used[y - 1, pos:pos + fw] = True
                    pos += fw
                x = x1
        place_lights()
    # 2) the wall line
    for y in range(h):
        for x in range(w):
            if not solid[y, x] or d[y, x] != 1 or y >= cfg["hud_rows"]: continue
            if floor_north(x, y, 2):                                    # south of the floor: low flat pieces only
                if rng.random() < 0.45: put(rng.choice(LOW_), x, y, block=False)
            elif near_floor_s(x, y):
                if rng.random() < 0.8: put(rng.choice(TALL_N_), x, y) or put(cfg.get("tall_n_fallback", "rbStonePillar"), x, y)
            elif rng.random() < 0.55: put(rng.choice(TALL_SIDE_), x, y)
    # 3) the solid mass between and around the halls
    for x, y in tiles:
        near_rate, deep_rate = cfg.get("deep_rate", (0.30, 0.12))
        if d[y, x] < 2 or floor_north(x, y, 3) or rng.random() > (near_rate if d[y, x] <= 4 else deep_rate): continue  # thinner deep in the mass
        put(rng.choice(DEEP_), x, y, block=rng.random() < 0.85)
    # 4) floor decor along the walls (walk-through), clear of portals / start / boss
    keep = [(px, py, 2) for px, py in cfg["portals"]] + [(cfg["start"][0], cfg["start"][1], 2)] + [(x, y, 2) for x, y in cfg.get("reserved", [])]
    if cfg["boss"]: keep.append((cfg["boss"][0], cfg["boss"][1], 3))
    if "arena" in cfg: keep.append((cfg["arena"][0], cfg["arena"][1], 6))   # the 40-player arena floor stays clear
    for y in range(h):
        for x in range(w):
            if not FLOOR_DECOR_ or not floor[y, x] or rng.random() > 0.10: continue
            if not solid[max(0, y - 1):y + 2, max(0, x - 1):x + 2].any(): continue
            if any(abs(x - a) <= r and abs(y - b) <= r for a, b, r in keep): continue
            put(rng.choice(FLOOR_DECOR_), x, y, block=False)
    if cfg["boss"]:
        bx, by = cfg["boss"]
        props.append(("rbRuneSeal", bx - 1, by + 2))                   # under the boss's reach, in front of it (flat, walk-through)
    if "arena" in cfg and cfg.get("arena_seal", True):                  # the Wanted arena: a rune seal under the event boss
        ax, ay = cfg["arena"]
        props.append(("rbRuneSeal", ax - 1, ay + 1))
    write(cfg, props)
    print("props", len(props), {k: len(v) for k, v in lights.items()})


def write(cfg, props):
    props.sort(key=lambda p: (p[2], p[1]))
    lines = []
    for i in range(0, len(props), 6):
        lines.append("            " + " ".join('P("%s", %d, %d),' % p for p in props[i:i + 6]))
    src = '''namespace ZenoniaProto
{
    /// <summary>
    /// %(summary)s (21/11/2026), generated by Tools/place-map-25d-dungeon-props.py %(key)s (seed 21).
    /// Every blocking prop stands on a tile the rows already block, so collision is unchanged; low pieces south of the floor and the floor decor are walk-through.
    /// The lanterns, crystals and glow mushrooms are the dungeon's lights (DungeonLights, by name). Re-run the tool rather than editing by hand.
    /// </summary>
    public static class %(cls)sProps
    {
        static MapProp P(string type, int tx, int ty) { return new MapProp(type, tx, ty); }

        public static readonly MapProp[] All =
        {
%(body)s
        };
    }
}
''' % dict(cls=cfg["cls"], summary=cfg.get("summary", "Standing walls, crystals, lanterns and floor decor of the dark 2.5D " + cfg["cls"]), key=[k for k, v in MAPS.items() if v is cfg][0], body="\n".join(lines))
    out = os.path.join(MAPS_DIR, cfg["cls"] + "Props.cs")
    open(out, "w", encoding="utf8", newline="\n").write(src)
    print("wrote", out)


if __name__ == "__main__":
    main()

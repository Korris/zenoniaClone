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
MAPS = {
    "rootburrow": dict(cls="Rootburrow", rows_file="RootburrowMap.cs", size=(60, 24), start=(56, 12), boss=(26, 6),
                       portals=[(59, 11), (59, 12), (59, 13), (4, 2), (5, 2)], hud_rows=21, arena=(9, 10)),  # V3 delve-deeper cave (21/11/2026)
}
TALL_N = ["rbCaveWall", "rbCaveWallB", "rbStonePillar", "rbCaveWall", "rbHangingRoots", "rbCaveWallB"]
TALL_SIDE = ["rbStonePillar", "rbStonePillar", "rbCrystalNest", "rbCaveBoulder"]
LOW = ["rbCaveBoulder", "rbStoneCluster", "rbGlowShrooms", "rbStoneCluster", "rbPebbles"]
DEEP = ["rbStonePillar", "rbCaveWall", "rbCaveWallB", "rbCaveBoulder", "rbHangingRoots"]
FLOOR_DECOR = ["rbGlowShrooms", "rbPebbles", "rbPebbles", "rbStoneSlab", "rbGlowShrooms"]
FOOT_W = {"rbCaveWall": 3, "rbCaveWallB": 3, "rbHangingRoots": 2, "rbCaveBoulder": 2, "rbStoneSlab": 2, "rbRuneSeal": 3}


def load(cfg):
    w, h = cfg["size"]
    rows = re.findall(r'"([TgwBd]{%d})"' % w, open(os.path.join(MAPS_DIR, cfg["rows_file"]), encoding="utf8").read())
    assert len(rows) == h, len(rows)
    solid = np.array([[c in "TwB" for c in r] for r in rows])
    water = np.array([[c in "wB" for c in r] for r in rows])
    return solid, water


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
    lights = dict(rbRuneOrb=[], rbRuneObelisk=[], rbPurpleCrystal=[], rbPinkCrystal=[])

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
    tiles = [(x, y) for y in range(h) for x in range(w) if solid[y, x] and y < cfg["hud_rows"] + 2]
    rng.shuffle(tiles)
    # 1) light sources first, on wall tiles that face the floor from the north or the sides
    for x, y in tiles:
        if d[y, x] != 1 or floor_north(x, y, 2): continue
        if far("rbRuneOrb", x, y, 9) and near_floor_s(x, y): put("rbRuneOrb", x, y)
        elif far("rbRuneObelisk", x, y, 13): put("rbRuneObelisk", x, y)
        elif far("rbPurpleCrystal", x, y, 9): put("rbPurpleCrystal", x, y)
        elif far("rbPinkCrystal", x, y, 11): put("rbPinkCrystal", x, y)
    # 2) the wall line
    for y in range(h):
        for x in range(w):
            if not solid[y, x] or d[y, x] != 1 or y >= cfg["hud_rows"] + 1: continue
            if floor_north(x, y, 2):                                    # south of the floor: low flat pieces only
                if rng.random() < 0.45: put(rng.choice(LOW), x, y, block=False)
            elif near_floor_s(x, y):
                if rng.random() < 0.8: put(rng.choice(TALL_N), x, y) or put("rbStonePillar", x, y)
            elif rng.random() < 0.55: put(rng.choice(TALL_SIDE), x, y)
    # 3) the solid mass between and around the halls
    for x, y in tiles:
        if d[y, x] < 2 or floor_north(x, y, 3) or rng.random() > 0.30: continue
        put(rng.choice(DEEP), x, y, block=rng.random() < 0.85)
    # 4) floor decor along the walls (walk-through), clear of portals / start / boss
    keep = [(px, py, 2) for px, py in cfg["portals"]] + [(cfg["start"][0], cfg["start"][1], 2), (cfg["boss"][0], cfg["boss"][1], 3)]
    if "arena" in cfg: keep.append((cfg["arena"][0], cfg["arena"][1], 6))   # the 40-player arena floor stays clear
    for y in range(h):
        for x in range(w):
            if not floor[y, x] or rng.random() > 0.10: continue
            if not solid[max(0, y - 1):y + 2, max(0, x - 1):x + 2].any(): continue
            if any(abs(x - a) <= r and abs(y - b) <= r for a, b, r in keep): continue
            put(rng.choice(FLOOR_DECOR), x, y, block=False)
    bx, by = cfg["boss"]
    props.append(("rbRuneSeal", bx - 1, by + 2))                       # under the boss's reach, in front of it (flat, walk-through)
    if "arena" in cfg:                                                   # the Wanted arena: a rune seal under the event boss
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
    /// Standing walls, crystals, lanterns and floor decor of the dark 2.5D %(cls)s (21/11/2026), generated by Tools/place-map-25d-dungeon-props.py %(key)s (seed 21).
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
''' % dict(cls=cfg["cls"], key=[k for k, v in MAPS.items() if v is cfg][0], body="\n".join(lines))
    out = os.path.join(MAPS_DIR, cfg["cls"] + "Props.cs")
    open(out, "w", encoding="utf8", newline="\n").write(src)
    print("wrote", out)


if __name__ == "__main__":
    main()

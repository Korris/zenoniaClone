"""Willowbrook Village V2 (100x100 since 25/11/2026 13:25): the social-hub town of Taurus, rows + named districts traced from ChatGPT's concept.

Design: ChatGPT chat "Ragnarok Và Đạo Chúa" (Tay). The user (25/11): no separate market, players gather and sell round a LAKE in the middle; then
"map đang bự quá, làm nhỏ lại": the concept-measured layout (160 units) is scaled by K = 0.625 to 100x100. Buildings keep their art size (only their centre moves).
Concept: Assets/ArtLibrary/maps/taurus/village-v2/willowbrook-v2-concept-lake-chatgpt-2.png (1254 px = 160 concept units): the values inside sc() / srect() /
sblock() were measured on it.
Used by Tools/build-tau-greybox-rows.py (rows, checks, labelled layout) and Tools/tau_village_props_layout.py (props on the same coordinates).
Coordinates are tiles, x right, y down (row 0 = north); every rect is inclusive (x0, y0, x1, y1).
Row chars: T building / wall / fence   t tree or forest   x open meadow outside the wall (solid)   w water   g grass   p plaza   B bridge deck
"""
import random

K = 0.625                       # concept units (160) -> tiles (100)
W = H = 100


def sc(v): return int(round(v * K))
def srect(r): return (sc(r[0]), sc(r[1]), sc(r[2]), sc(r[3]))
def spt(p): return (sc(p[0]), sc(p[1]))
def srad(r): return (r[0] * K, r[1] * K)


def sblock(r):
    """A building block measured on the concept: its size is the art's (unchanged), its bottom-centre moves with the scale."""
    w, h = r[2] - r[0] + 1, r[3] - r[1] + 1
    cx, by = sc((r[0] + r[2] + 1) / 2.0), sc(r[3])
    return (cx - w // 2, by - h + 1, cx - w // 2 + w - 1, by)


GATE_X = 47                     # gate opening x47-50 on the wall rows; portal row = WALL_ROWS[1]
WALL_ROWS = (88, 89)            # south of the wall: meadow (outside the town, never walked); rows 97-99 under the HUD
START = (50, 56)                # player start: the plaza south of the lake
ARRIVAL = (48, 86)              # where Clover Meadows' village gate lands a hero (three tiles in from the gate row)

RING_C, RING_R = spt((81, 66)), srad((40.0, 29.0))      # the cobbled ring round the lake: where players gather and sell
LAKE_C, LAKE_R = spt((81, 68)), srad((27.0, 17.0))
ISLAND_C, ISLAND_R = (49, 41), (5.8, 4.6)                # the old oak's island
OAK_TRUNK = (48, 40, 50, 42)                             # 3x3, the Great Oak's art
BRIDGE = (34, 38, 45, 40)                                # wooden bridge from the west shore to the island (3 rows like the art)
GAZEBO = sblock((91, 80, 95, 81))
GUILD_HALL = sblock((74, 28, 83, 32))
GUILD_TERRACE = srect((54, 33, 104, 40))
# Raised Guild terrace (user 26/11 "chốt 2.5D, làm khu guild hall nâng cao"): Guild Hall + its court on a plateau ~2 m up, reached ONLY by the
# grand stairs; retaining wall on rows 25-26 facing the lake ring, hedge-tree flanks down to the forest border. Rows stay 2D (the height is drawn).
PLATEAU = (34, 3, 63, 24)                 # the raised ground
TERRACE_WALL_ROWS = (25, 26)              # retaining wall (solid), seen from the south
TERRACE_STAIRS = (46, 51)                 # stair columns through the wall (open), centred on the hall
PLATEAU_FLANKS = ((32, 33), (64, 65))     # hedge trees closing the plateau's west / east sides
FORGE = sblock((123, 25, 129, 29))
FORGE_C, FORGE_R = spt((124, 30)), srad((16.0, 7.0))
POND_C, POND_R = spt((30, 32)), srad((11.0, 6.0))       # small pond garden (north-west)
POND_GARDEN_C, POND_GARDEN_R = spt((30, 32)), srad((15.0, 10.0))
PET_YARD_C, PET_YARD_R = spt((22, 68)), srad((13.0, 10.0))   # fenced grass yard (west): pets, sheep, kennel
EAST_GARDEN_C, EAST_GARDEN_R = spt((135, 66)), srad((15.0, 14.0))
HOMES = srect((10, 103, 62, 129))
CARD_PET = srect((100, 104, 152, 127))
MAIN_ROAD = (46, sc(94), 51, WALL_ROWS[0] - 1)
LINKS = [srect(r) for r in (                            # paving that joins every district to the ring
    (46, 42, 54, 48), (33, 63, 42, 71), (118, 58, 122, 74),   # (the terrace -> forge link went with the raised terrace, 26/11)
    (52, 94, 62, 104), (98, 94, 106, 105),
)]
# buildings (solid blocks; their prop stands on the lowest row, left column): name -> rect (concept-measured, sizes = art footprint)
BLOCKS = {name: sblock(r) for name, r in {
    "tauGuildHallHouse": (74, 28, 83, 32), "tauForgeHouse": (123, 25, 129, 29), "tauInnHouse": (24, 110, 31, 113),
    "tauCottageHouseB": (13, 119, 17, 121), "tauCottageHouseA": (54, 105, 58, 107), "tauCottageHouseD": (53, 119, 56, 121),
    "tauCottageHouseC": (102, 107, 105, 109), "tauCardShopHouse": (116, 115, 124, 118), "tauPetHouse": (135, 117, 142, 120),
    "tauLakeGazebo": (91, 80, 95, 81), "tauPondPavilion": (28, 21, 32, 22), "tauGardenMonument": (133, 68, 135, 69),
}.items()}
# single trees standing in the cobbled ring (their trunk tile is solid), between the lake shore and the ring's edge
RING_TREES = [spt(p) for p in ((46, 50), (114, 49), (118, 78), (108, 88), (52, 86), (43, 78), (116, 58), (45, 58))]  # (58, 41) / (102, 41) stood in the terrace wall
# district name, rect (for the layout labels and to keep the landscape trees out)
DISTRICTS = tuple((n, srect(r)) for n, r in (
    ("Central Lake & Plaza", (41, 37, 121, 95)), ("Guild Hall", (54, 18, 104, 40)), ("Forge", (105, 18, 145, 40)),
    ("Pond Garden", (14, 20, 46, 44)), ("Pet Yard", (7, 56, 40, 80)), ("East Garden", (117, 50, 152, 82)), ("Homes & Inn", (8, 100, 64, 131)),
    ("Card & Pet Shop", (96, 101, 154, 129)), ("Main Road / Gate", (70, 94, 88, 142)),
))
NPCS = tuple((n, sc(x), sc(y)) for n, x, y in (("elderRowan", 72, 35), ("cardMaster", 119, 121), ("tauBram", 120, 34), ("tauLumi", 26, 70), ("tauMira", 147, 122), ("tauFinn", 71, 138)))


def rect(g, r, c):
    x0, y0, x1, y1 = r
    for y in range(max(0, y0), min(len(g), y1 + 1)):
        for x in range(max(0, x0), min(len(g[0]), x1 + 1)): g[y][x] = c


def ellipse(g, c, r, ch):
    for y in range(len(g)):
        for x in range(len(g[0])):
            if ((x - c[0]) / r[0]) ** 2 + ((y - c[1]) / r[1]) ** 2 <= 1.0: g[y][x] = ch


def village():
    g = [["g"] * W for _ in range(H)]
    # forest border (3 tiles), the low south wall with the gate, meadow outside it, HUD rows
    rect(g, (0, 0, W - 1, 2), "t"); rect(g, (0, 0, 2, H - 1), "t"); rect(g, (W - 3, 0, W - 1, H - 1), "t")
    rect(g, (3, WALL_ROWS[0], W - 4, WALL_ROWS[1]), "T")
    rect(g, (3, WALL_ROWS[1] + 1, W - 4, H - 1), "x")
    rect(g, (0, H - 3, W - 1, H - 1), "T")
    # the ring plaza, the lake with its island and bridge, the districts and the paving links
    ellipse(g, RING_C, RING_R, "p")
    ellipse(g, LAKE_C, LAKE_R, "w"); ellipse(g, ISLAND_C, ISLAND_R, "g"); rect(g, OAK_TRUNK, "t"); rect(g, BRIDGE, "B")
    rect(g, GUILD_TERRACE, "p"); ellipse(g, FORGE_C, FORGE_R, "p")
    ellipse(g, POND_GARDEN_C, POND_GARDEN_R, "p"); ellipse(g, POND_C, POND_R, "w")
    ellipse(g, EAST_GARDEN_C, EAST_GARDEN_R, "p"); rect(g, HOMES, "p"); rect(g, CARD_PET, "p")
    for r in LINKS: rect(g, r, "p")
    rect(g, MAIN_ROAD, "p"); rect(g, (GATE_X, WALL_ROWS[0], GATE_X + 3, WALL_ROWS[1]), "p")   # cobbled like the concept
    ellipse(g, PET_YARD_C, PET_YARD_R, "g")        # grass yard (its fence is props round the rim, open to the ring link)
    for b in BLOCKS.values(): rect(g, b, "T")
    for x, y in RING_TREES: g[y][x] = "t"
    raised_terrace(g)
    scatter_trees(g)
    seal_pockets(g)
    return g


def raised_terrace(g):
    """Retaining wall across the terrace's south edge (stairs open), hedge trees up both flanks to the border: the plateau is reached by the stairs only."""
    x0, _, x1, _ = PLATEAU
    for y in TERRACE_WALL_ROWS:
        for x in range(x0, x1 + 1):
            g[y][x] = "p" if TERRACE_STAIRS[0] <= x <= TERRACE_STAIRS[1] else "T"
    for fx0, fx1 in PLATEAU_FLANKS: rect(g, (fx0, 3, fx1, TERRACE_WALL_ROWS[1]), "t")


def seal_pockets(g):
    """Grass the tree clumps closed off from the start becomes forest (map-design-rules: every walkable tile reaches the spawn)."""
    seen, todo = {START}, [START]
    while todo:
        x, y = todo.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in seen and g[ny][nx] not in "Ttwx": seen.add((nx, ny)); todo.append((nx, ny))
    for y in range(H):
        for x in range(W):
            if g[y][x] not in "Ttwx" and (x, y) not in seen: g[y][x] = "t"


def scatter_trees(g):
    """The concept's woods round the town: tree clumps outside every district (+1), >= 2 tiles from anything that is not grass, clear of the gate."""
    rng = random.Random(25112026)
    keep = [r for _, r in DISTRICTS] + [(40, 79, 58, WALL_ROWS[1])]
    busy = [(x, y) for y in range(H) for x in range(W) if g[y][x] not in "gtx"]
    for _ in range(1300):
        x, y = rng.randint(3, W - 5), rng.randint(3, WALL_ROWS[0] - 3)
        w, h = rng.choice(((1, 1), (2, 1), (1, 2), (2, 2)))
        if any(r[0] - 1 <= x + w and x <= r[2] + 1 and r[1] - 1 <= y + h and y <= r[3] + 1 for r in keep): continue
        if any(abs(x - bx) <= 2 + w and abs(y - by) <= 2 + h for bx, by in busy if abs(x - bx) <= 5 and abs(y - by) <= 5): continue
        rect(g, (x, y, x + w - 1, y + h - 1), "t")

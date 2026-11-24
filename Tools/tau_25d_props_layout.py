"""Layout data for Tools/place-tau-25d-props.py (Taurus V4 phase 04): prop sizes and where every prop of Willowbrook Village / Clover Meadows goes.

SIZE: name -> (sprite width in tiles, FootW, FootH[, vertical stretch for flat bridges]). Buildings block their 2 bottom rows on their greybox block, trees their trunk row, small posts 1 tile;
FootH 0 = walk-through. Names carry the StaticPropShadows.Classify keywords (house / forge / tower / tent / stall / tree / oak / bridge / fence...).
Placement rules (docs/map-design-rules.md + the design chat's brief, docs/reference/taurus-v4-art/taurus-environment-production-brief-chatgpt.txt):
roads, plaza, bridge, camps stay clear; decoration at road edges, wall feet, corners, around landmarks; blocking props only on greybox solid tiles or on grass off the lanes.
"""
import math, random

SIZE = {
    # WV-SHEET-01 major buildings
    "tauGuildHallHouse": (11.5, 10, 2), "tauForgeHouse": (8.0, 7, 2), "tauWindmillTower": (5.5, 4, 2), "tauCardTent": (5.0, 4, 2),
    "tauFruitStall": (2.8, 2, 2), "tauGoodsStall": (2.8, 2, 2),
    # WV-SHEET-02 great oak + plaza
    "tauGreatOakTree": (9.0, 3, 1), "tauOakPlanterRing": (5.4, 3, 0), "tauBenchA": (1.9, 2, 0), "tauBenchB": (1.9, 2, 0),
    "tauLampPost": (1.1, 1, 1), "tauFlowerBarrel": (1.0, 1, 0),
    # WV-SHEET-05 bridge, gate, wall
    "tauVillageBridge": (3.2, 2, 0, 1.45), "tauGateArchTower": (11.0, 4, 0), "tauGateTowerL": (3.78, 5, 0), "tauGateTowerR": (3.74, 3, 0), "tauGateArchBeam": (3.48, 4, 0), "tauWallLeft": (4.3, 4, 2), "tauWallRight": (4.3, 4, 2),
    "tauGatePillarL": (1.6, 1, 1), "tauGatePillarR": (1.6, 1, 1), "tauRiverRocks": (2.5, 2, 0),
    # WV-SHEET-03 service props
    "tauBulletinBoardSign": (2.0, 2, 1), "tauQuestBoardSign": (2.0, 2, 1), "tauAnvil": (1.2, 1, 0), "tauWeaponRack": (2.0, 2, 1),
    "tauWell": (2.0, 2, 1), "tauBarrels": (1.5, 1, 0), "tauCrates": (1.5, 1, 0), "tauSignpost": (1.0, 1, 1),
    # WV-SHEET-04 pet pen + social
    "tauPetPenFence": (5.6, 5, 1), "tauPetSign": (1.5, 1, 1), "tauFeedTrough": (1.5, 1, 0), "tauHayBales": (1.8, 2, 0),
    "tauFenceSmall": (1.5, 1, 0), "tauCardTable": (1.5, 1, 0),
    # CM-SHEET-01 trees
    "tauMeadowOakTree": (5.5, 1, 1), "tauMeadowOakMediumTree": (4.3, 1, 1), "tauElderwoodOakTree": (6.2, 1, 1), "tauYoungTree": (3.0, 1, 1),
    # mirrored, re-scaled copies of the same art (cut/*TreeB.png = ImageOps.mirror of the cut tree) so the forest edge does not repeat one silhouette (design review 24/11 P3)
    "tauMeadowOakTreeB": (5.0, 1, 1), "tauMeadowOakMediumTreeB": (4.7, 1, 1), "tauElderwoodOakTreeB": (5.7, 1, 1),
    "tauStumpRooted": (2.0, 1, 0), "tauBrokenTrunkLog": (2.5, 1, 0),
    # CM-SHEET-02 low flora (all walk-through)
    "tauDaisyFlowers": (1.5, 1, 0), "tauYellowFlowers": (1.5, 1, 0), "tauCloverPatch": (1.2, 1, 0), "tauTallGrass": (1.0, 1, 0), "tauLowBush": (2.0, 2, 0),
    "tauWideBush": (2.5, 2, 0), "tauFern": (1.2, 1, 0), "tauMushroomsSmall": (1.0, 1, 0), "tauReedsSmall": (1.0, 1, 0),
    # CM-SHEET-03 rocks + riverbank
    "tauMossyBoulder": (3.0, 2, 1), "tauRockCluster": (2.0, 1, 0), "tauPebbles": (1.2, 1, 0), "tauRiverbankRocks": (3.0, 2, 0), "tauReedsLarge": (1.3, 1, 0),
    "tauLilyBank": (1.5, 1, 0), "tauDriftLog": (2.2, 2, 0), "tauBrookMarkerStone": (1.5, 1, 1),
    # CM-SHEET-04 rabbit hills + brookside
    "tauBurrowMoundA": (2.0, 2, 0), "tauBurrowMoundB": (2.2, 2, 0), "tauBurrowEntrance": (1.5, 1, 0), "tauMeadowFence": (2.0, 2, 0), "tauMeadowSignpost": (1.0, 1, 1),
    "tauBrookBridge": (5.4, 4, 0, 1.3), "tauBankFlowers": (1.8, 2, 0),
    # CM-SHEET-05 mushroom grove
    "tauRedMushroomsBig": (2.0, 2, 0), "tauAmberMushrooms": (2.0, 2, 0), "tauMushroomRing": (2.5, 2, 0), "tauMushroomLog": (3.0, 2, 0), "tauMushroomStump": (2.0, 2, 0),
    "tauSporePuffs": (1.2, 1, 0), "tauRootArch": (2.5, 2, 1), "tauShadedBush": (2.0, 2, 0),
    # CM-SHEET-06 bramble thicket + nest
    "tauBrambleWallLarge": (4.0, 3, 1), "tauBrambleWallMedium": (3.0, 2, 1), "tauBrambleBushRound": (2.5, 2, 1), "tauBrambleRootHeap": (2.0, 2, 0),
    "tauNestMound": (4.0, 4, 0), "tauTuskedStone": (1.8, 1, 1), "tauThornyTrunkLog": (3.0, 2, 0),
    # CM-SHEET-07 ancient roots / shrine
    "tauBrookRunestone": (1.5, 1, 1), "tauAncientRootsStones": (2.4, 2, 2), "tauShrineEntrance": (10.0, 8, 0), "tauAncientSlab": (2.0, 1, 1),
    "tauRootColumn": (2.0, 1, 1), "tauSacredRoots": (3.0, 2, 0), "tauRuneStoneBroken": (2.0, 2, 0), "tauGeminiGateMarker": (4.6, 4, 2),
}


def P(name, tx, ty): return (name, tx, ty)


def village_props(rows):
    out = [
        # buildings on their greybox blocks (bottom-left footprint tile = left column, lowest row of the block)
        P("tauGuildHallHouse", 30, 6), P("tauForgeHouse", 3, 9), P("tauWindmillTower", 12, 6), P("tauCardTent", 38, 17),
        P("tauFruitStall", 30, 22), P("tauGoodsStall", 34, 22),
        # town square: the great oak on its 3x3 trunk block, the stone ring round it (flat), benches on the plaza rim, lamps on the grass beside it
        P("tauOakPlanterRing", 21, 15), P("tauGreatOakTree", 21, 14),
        P("tauBenchA", 17, 8), P("tauBenchB", 26, 8), P("tauBenchB", 17, 18), P("tauBenchA", 26, 18),
        P("tauLampPost", 16, 8), P("tauLampPost", 28, 8), P("tauLampPost", 16, 17), P("tauLampPost", 28, 18), P("tauLampPost", 19, 22), P("tauLampPost", 24, 22),
        P("tauFlowerBarrel", 32, 7), P("tauFlowerBarrel", 37, 7), P("tauFlowerBarrel", 29, 22), 
        # service props
        P("tauBulletinBoardSign", 28, 6), P("tauQuestBoardSign", 16, 24), P("tauAnvil", 5, 10), P("tauWeaponRack", 11, 8), P("tauWell", 29, 16),
        P("tauCrates", 2, 11), P("tauBarrels", 26, 21), P("tauSignpost", 25, 20),
        # pet pen (fence ring on its block, open west) + social corner
        P("tauPetPenFence", 38, 26), P("tauFenceSmall", 38, 20), P("tauFenceSmall", 40, 20), P("tauFenceSmall", 42, 20),   # the pen art is shallower than its 7-row block: low fence pieces close its north side
        P("tauFeedTrough", 40, 23), P("tauHayBales", 39, 25), P("tauCardTable", 33, 16),
        # stream: bridge over the B tiles, rocks on the banks
        P("tauVillageBridge", 9, 21), P("tauRiverRocks", 3, 17), P("tauRiverRocks", 13, 17), P("tauRiverRocks", 5, 21), P("tauRiverRocks", 14, 21),
        # south wall (rows 27-28) either side of the gate road x20-23, the arch over the road (walk-through), its two posts on the wall tiles
        # gate = tauGateArchTower split in 3 (cut/tauGateTower{L,R}, tauGateArchBeam: columns either side of the passage, same pixels): the towers stand on the
        # wall row 28, the beam sorts on row 26 so a hero in the gate rows 27-28 draws in front of it (design review 24/11: the arch hid the Warrior in the exit)
        P("tauGateTowerL", 16, 28), P("tauGateTowerR", 24, 28), P("tauGateArchBeam", 20, 26),
    ]
    out += [P("tauWallLeft", x, 28) for x in (0, 4, 8, 12)] + [P("tauWallRight", x, 28) for x in (28, 32, 36, 40)]   # clear of the gate towers (the arch now sorts on row 26)
    # corner tree clumps (2x2 't' blocks): one big oak each, trunk on the lower row
    out += [P("tauMeadowOakTree", x, y + 1) for x, y in ((2, 2), (41, 3), (41, 9), (2, 14), (2, 23), (14, 23))]
    # border rows / columns: a hedge of medium oaks and young trees so the solid edge reads as forest
    kinds = ("tauMeadowOakMediumTree", "tauYoungTree")
    out += [P(kinds[i % 2], x, 0) for i, x in enumerate(range(1, 43, 3))]
    out += [P(kinds[(i + 1) % 2], 0, y) for i, y in enumerate(range(3, 27, 3))] + [P(kinds[i % 2], 43, y) for i, y in enumerate(range(2, 20, 3))]
    return out


def camp_rects():
    """(tx, ty, w, h) of every distinct Clover Meadows camp rect, read from MapSpawnTable.cs."""
    import os, re
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    s = open(os.path.join(root, "Assets", "Scripts", "MapSpawnTable.cs"), encoding="utf-8").read()
    a = s.index("TauCloverMeadows ="); blk = s[a:s.index("};", a)]
    out = []
    for m in re.findall(r'Zone\("\w+", \d+, (\d+), (\d+), (\d+), (\d+)', blk):
        r = tuple(map(int, m))
        if r not in out: out.append(r)
    return out


# Clover Meadows: fixed landmarks on their greybox blocks (design brief CM-SHEET-04 / 06 / 07)
MEADOWS_FIXED = [
    P("tauBrookBridge", 46, 27),            # the only crossing, B tiles x46-49 rows 24-26 (flat, walk-through), its south end on the bank row 27
    P("tauShrineEntrance", 54, 2),          # dungeon door: lintel + arch x54-61 rows 0-2, the opening x56-59 row 2 stays road (walk-through art)
    P("tauBrookRunestone", 38, 23),         # quest touch point T-M03 (solid tile)
    P("tauAncientRootsStones", 53, 10),     # quest touch point T-M04, 2x2 solid
    P("tauGeminiGateMarker", 88, 11),       # closed Gemini gate block x88-91 rows 6-11
    P("tauNestMound", 3, 3), P("tauBrambleRootHeap", 12, 2), P("tauBrambleRootHeap", 4, 9),
    P("tauElderwoodOakTree", 88, 8), P("tauElderwoodOakTree", 91, 7),   # behind the Gemini gate art (its block is 6 rows deep)
    P("tauMeadowOakMediumTree", 1, 7), P("tauTuskedStone", 2, 6),        # west edge of the nest
]
# decoration by ground char (meadows 'g' = Cloverfield south of the stream and the open grass north of it)
DECOR = {
    "g": ["tauDaisyFlowers", "tauYellowFlowers", "tauCloverPatch", "tauTallGrass", "tauLowBush"],
    "h": ["tauBurrowMoundA", "tauBurrowMoundB", "tauBurrowEntrance", "tauCloverPatch", "tauLowBush", "tauBankFlowers"],
    "m": ["tauRedMushroomsBig", "tauAmberMushrooms", "tauMushroomRing", "tauMushroomLog", "tauMushroomStump", "tauSporePuffs", "tauShadedBush"],
    "b": ["tauBrambleRootHeap", "tauThornyTrunkLog", "tauShadedBush", "tauFern", "tauRockCluster"],
    "n": ["tauBrambleRootHeap", "tauThornyTrunkLog"],
    "e": ["tauFern", "tauStumpRooted", "tauRockCluster", "tauBrokenTrunkLog", "tauShadedBush"],
    "r": ["tauSacredRoots", "tauRuneStoneBroken", "tauFern", "tauRockCluster"],
}
BANK = ["tauReedsLarge", "tauRiverbankRocks", "tauLilyBank", "tauReedsSmall", "tauDriftLog"]


def meadows_props(rows):
    H, W = len(rows), len(rows[0])
    rng = random.Random(24112026)
    VAR = {"tauMeadowOakTree": "tauMeadowOakTreeB", "tauMeadowOakMediumTree": "tauMeadowOakMediumTreeB", "tauElderwoodOakTree": "tauElderwoodOakTreeB"}
    def P(name, x, y):   # every other big tree is the mirrored / re-scaled variant (no long run of one silhouette)
        return (VAR[name] if name in VAR and rng.random() < 0.5 else name, x, y)
    camps = camp_rects()
    out = list(MEADOWS_FIXED)
    taken = set()
    for n, x, y in out:
        for dy in range(-2, 3):
            for dx in range(-2, 5): taken.add((x + dx, y + dy))
    in_camp = lambda x, y, m=0: any(cx - m <= x < cx + cw + m and cy - m <= y < cy + ch + m for cx, cy, cw, ch in camps)
    road = [(x, y) for y in range(H) for x in range(W) if rows[y][x] in "dB"]
    rd = [[min((abs(x - rx) + abs(y - ry) for rx, ry in road if abs(x - rx) <= 3 and abs(y - ry) <= 3), default=99) for x in range(W)] for y in range(H)]

    def zone_of(x, y):
        if y <= 15 and x >= 26: return "old"          # Elderwood Edge + Ancient Roots
        if y <= 23 and x < 40: return "bramble"
        return "meadow"
    # 1) forest: trunks on solid 't' tiles with Poisson-like spacing, kind by area; boulders / bramble walls / root columns among them
    trunks = []
    cand = [(x, y) for y in range(H - 3) for x in range(W) if rows[y][x] == "t"]
    rng.shuffle(cand)
    for x, y in cand:
        z = zone_of(x, y)
        sp = 2.6 if z == "old" else 2.4
        if (x, y) in taken or any((x - a) ** 2 + (y - b) ** 2 < sp * sp for a, b in trunks): continue
        r = rng.random()
        if z == "old": name = "tauElderwoodOakTree" if r < 0.6 else ("tauMeadowOakTree" if r < 0.85 else "tauRootColumn")
        elif z == "bramble": name = "tauMeadowOakMediumTree" if r < 0.45 else ("tauBrambleWallMedium" if r < 0.7 else ("tauMeadowOakTree" if r < 0.9 else "tauMossyBoulder"))
        else: name = "tauMeadowOakTree" if r < 0.45 else ("tauMeadowOakMediumTree" if r < 0.8 else ("tauYoungTree" if r < 0.93 else "tauMossyBoulder"))
        fw = SIZE[name][1]
        if fw > 1 and not all(x + i < W and rows[y][x + i] == "t" for i in range(fw)): name = "tauMeadowOakMediumTree"   # wide solid props only where all their tiles are forest
        trunks.append((x, y))
        out.append(P(name, x, y))
    # 1a) any forest tile still bare (spacing skipped it): a young tree on it
    #     a tree covers its trunk row and ~3 rows above it, so a tile counts as covered by a trunk at most 1 column away and 0-3 rows BELOW it
    for x, y in sorted(cand, key=lambda t: -t[1]):
        if (x, y) not in taken and not any(abs(x - a) <= 1 and 0 <= b - y <= 3 for a, b in trunks):
            trunks.append((x, y)); out.append(P("tauYoungTree", x, y))
    # 1b) groves inside the forest zones (Mushroom Grove, Bramble Thicket, Elderwood, Ancient Roots): the rows only have a few 't' there, so single trunks
    #     on walkable ground (trunk tile blocked by the map class): >= 3 tiles from any road / bridge, >= 2 outside every camp rect, spacing 3.4 (never closes a lane)
    grove_p = {"m": 0.16, "b": 0.14, "e": 0.25, "r": 0.14}
    for y in range(2, 23):
        for x in range(2, W - 2):
            c = rows[y][x]
            if c not in grove_p or rd[y][x] < 3 or in_camp(x, y, 2) or (x, y) in taken: continue
            if any(rows[y + dy][x + dx] in "Tw" for dy in (-1, 0, 1, 2) for dx in (-1, 0, 1) if 0 <= y + dy < H): continue
            if rng.random() > grove_p[c] or any((x - a) ** 2 + (y - b) ** 2 < 3.4 ** 2 for a, b in trunks): continue
            trunks.append((x, y))
            r = rng.random()
            if c in "er": name = "tauElderwoodOakTree" if r < 0.55 else "tauMeadowOakTree"
            elif c == "m": name = "tauMeadowOakMediumTree" if r < 0.6 else "tauYoungTree"
            else: name = "tauMeadowOakMediumTree" if r < 0.5 else ("tauBrambleBushRound" if r < 0.8 else "tauYoungTree")
            if SIZE[name][1] > 1 and not all(rows[y][x + i] == c and rd[y][x + i] >= 3 for i in range(SIZE[name][1])): name = "tauYoungTree"
            out.append(P(name, x, y))
    # the HUD rows 61-63: a row of oaks on rows 61-62 (kinds and rows alternate) so the south edge reads as forest
    #     kept 4+ tiles clear of the village gate (portal x46-49 row 60): the gate road runs off the south edge there, bushes only
    for i, x in enumerate(range(1, W, 3)):
        if 41 <= x <= 54: out.append(P("tauWideBush" if i % 2 else "tauLowBush", x, 62)); continue
        out.append(P(rng.choice(("tauMeadowOakTree", "tauMeadowOakMediumTree", "tauYoungTree")), x, 61 + (i % 2)))
    # 2) flat decor on walkable ground: banks, road edges (1-2 tiles off), camp rims (never inside); low density (review: -30-40 % in combat areas)
    decor = []
    for y in range(1, H - 3):
        for x in range(1, W - 1):
            c = rows[y][x]
            if c not in DECOR or in_camp(x, y) or (x, y) in taken: continue
            bank = any(0 <= y + d < H and rows[y + d][x] == "w" for d in (-1, 1))
            near_road = 1 <= rd[y][x] <= 2
            p = 0.22 if bank else (0.07 if near_road else (0.05 if in_camp(x, y, 2) else 0.015))
            if c in "erm" and not near_road: p *= 2          # old forest floor + grove: more roots / stones / mushrooms (review)
            if rng.random() > p or any(abs(x - a) < 3 and abs(y - b) < 3 for a, b in decor): continue
            decor.append((x, y))
            out.append(P(rng.choice(BANK if bank else DECOR[c]), x, y))
    return out

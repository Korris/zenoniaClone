"""Layout data for Tools/place-tau-25d-props.py (Taurus V4 phase 04): prop sizes and where every prop of Clover Meadows goes (Willowbrook Village V2: Tools/tau_village_props_layout.py).

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
    "tauVillageBridge": (3.2, 2, 0, 1.45), "tauGateArchTower": (16.6, 4, 0),
    # village V2 gate (sheet 5, 25/11): one painted gate split in 3 (towers 4.36 / arch 2.77 / 4.34 of 11.5 at sheet scale, arch scaled to the 4-wide road); L fw 2 / R fw 6 keep the
    # sprites centred on x74 / x84 while the prop tiles stay where Map3DGateBuilder expects them (L block 73-76, R 81-84); the flower wall is one 8-tile segment
    "tauGateTowerL": (6.3, 2, 0), "tauGateTowerR": (6.3, 6, 0), "tauGateArchBeam": (4.0, 4, 0), "tauWallLeft": (8.6, 8, 2), "tauWallRight": (8.6, 8, 2),
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
    # Willowbrook Village V2 (25/11/2026): ChatGPT prop sheets 1-5 cut from Assets/ArtLibrary/maps/taurus/village-v2/wv2-sheet*.png (layout Tools/tau_village_props_layout.py)
    "tauInnHouse": (10.0, 8, 2), "tauCottageHouseA": (6.0, 5, 2), "tauCottageHouseB": (6.0, 5, 2), "tauCottageHouseC": (5.0, 4, 2), "tauCottageHouseD": (5.0, 4, 2),
    "tauStoreHouse": (5.0, 4, 2), "tauCardShopHouse": (11.0, 9, 2), "tauPetHouse": (9.5, 8, 2), "tauLakeGazebo": (6.0, 5, 2),
    "tauMarketStallA": (3.6, 3, 0), "tauMarketStallB": (3.6, 3, 0), "tauMarketTent": (6.0, 5, 0), "tauMarketBanner": (1.5, 1, 0), "tauFlowerPlanterBox": (2.4, 2, 0),
    "tauBuntingPosts": (4.0, 3, 0), "tauCratesSacks": (2.4, 2, 0), "tauOakPlanterBed": (3.9, 3, 0), "tauParkBench": (2.2, 2, 0), "tauBannerLampPost": (1.4, 1, 0),
    "tauLakeDock": (3.7, 3, 0), "tauFlowerArch": (4.5, 4, 0), "tauGatherPergola": (6.0, 5, 0), "tauLilyPads": (3.0, 2, 0), "tauFallsRocks": (5.0, 4, 2),
    "tauPetGoodsStall": (4.2, 3, 0), "tauCardPlayTable": (3.6, 3, 0), "tauPetKennel": (3.4, 3, 1), "tauArrowSignpost": (1.8, 1, 1),
    "tauWallEndPillar": (1.6, 1, 1), "tauYardFence": (6.4, 6, 1), "tauYardFenceSide": (2.2, 1, 1), "tauStoneStairs": (5.0, 4, 0),
    # raised Guild terrace (26/11): retaining wall 4 x 2 rows (tiles seamlessly), grand stairs 6 wide lying on the stair tiles, lantern pillars on the wall ends
    # round 2 (ChatGPT review "chưa liền mạch"): 3 wall variants with a visible coping top + a 2-tile filler; stairs v2 with a top landing and the wall
    # jambs hugging the opening
    "tauTerraceWall": (4.0, 4, 2), "tauTerraceWallB": (4.0, 4, 2), "tauTerraceWallC": (4.0, 4, 2), "tauTerraceWallShort": (2.0, 2, 2),
    "tauTerraceStairs": (10.0, 6, 0), "tauTerracePillar": (1.3, 1, 2),   # 10 m: its jamb walls meet the runs ending 2 tiles from the opening
    # sheets 6-7: trees / bushes / rocks (magenta) and garden pieces (green); the long bridge lies flat over the lake bridge tiles (squashed to their 3 rows)
    "tauRoundTreeA": (5.5, 1, 1), "tauRoundTreeB": (5.0, 1, 1), "tauPineTree": (3.4, 1, 1), "tauPineTreeSmall": (2.3, 1, 1), "tauRoundBush": (2.4, 2, 0),
    "tauFlowerBushSmall": (2.4, 2, 0), "tauMossRocks": (3.0, 2, 1), "tauCherryTree": (5.5, 1, 1), "tauCherryTreeSmall": (3.4, 1, 1),
    "tauLakeBridgeLong": (12.4, 12, 0, 0.8), "tauGardenMonument": (3.6, 3, 2), "tauParasolTable": (3.6, 3, 0), "tauSheep": (2.2, 2, 0), "tauLamb": (1.5, 1, 0),
    "tauFlowerBasket": (2.2, 2, 0),
}


def P(name, tx, ty): return (name, tx, ty)


def forest_kind(z, r):
    """Prop name for a forest trunk in zone z ("old", "bramble", "meadow", "village") from the roll r."""
    if z == "old": return "tauElderwoodOakTree" if r < 0.6 else ("tauMeadowOakTree" if r < 0.85 else "tauRootColumn")
    if z == "bramble": return "tauMeadowOakMediumTree" if r < 0.45 else ("tauBrambleWallMedium" if r < 0.7 else ("tauMeadowOakTree" if r < 0.9 else "tauMossyBoulder"))
    if z == "village": return "tauRoundTreeA" if r < 0.36 else ("tauRoundTreeB" if r < 0.66 else ("tauPineTree" if r < 0.84 else ("tauCherryTree" if r < 0.92 else "tauPineTreeSmall")))   # V2 sheet 6-7 trees, no boulders
    return "tauMeadowOakTree" if r < 0.45 else ("tauMeadowOakMediumTree" if r < 0.8 else ("tauYoungTree" if r < 0.93 else "tauMossyBoulder"))


def fill_forest(rows, rng, P, out, taken, zone_of, reach=3, filler="tauYoungTree"):
    """Trunks on every solid 't' tile (above the 3 HUD rows): Poisson-like spacing, then a young tree on any tile no trunk covers.
    Appends to out, returns the trunk list. Shared by Clover Meadows and Willowbrook Village V2."""
    H, W = len(rows), len(rows[0])
    trunks = []
    cand = [(x, y) for y in range(H - 3) for x in range(W) if rows[y][x] == "t"]
    rng.shuffle(cand)
    for x, y in cand:
        z = zone_of(x, y)
        sp = 2.6 if z == "old" else 2.4
        if (x, y) in taken or any((x - a) ** 2 + (y - b) ** 2 < sp * sp for a, b in trunks): continue
        name = forest_kind(z, rng.random())
        fw = SIZE[name][1]
        if fw > 1 and not all(x + i < W and rows[y][x + i] == "t" for i in range(fw)): name = "tauMeadowOakMediumTree"   # wide solid props only where all their tiles are forest
        trunks.append((x, y))
        out.append(P(name, x, y))
    # a tree covers its trunk row and ~3 rows above it, so a tile counts as covered by a trunk at most 1 column away and 0-3 rows BELOW it
    for x, y in sorted(cand, key=lambda t: -t[1]):
        if (x, y) not in taken and not any(abs(x - a) <= 1 and 0 <= b - y <= reach for a, b in trunks):
            trunks.append((x, y)); out.append(P(filler, x, y))
    return trunks

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
    trunks = fill_forest(rows, rng, P, out, taken, zone_of)
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

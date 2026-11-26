"""Props of Willowbrook Village V2 (100x100, 25/11/2026) for Tools/place-tau-25d-props.py, placed as in ChatGPT's lake concept
(Assets/ArtLibrary/maps/taurus/village-v2/willowbrook-v2-concept-lake-chatgpt-2.png). Art: ChatGPT prop sheets wv2-sheet1..7 (same folder), cut into
Assets/ArtLibrary/maps/taurus/props/cut/ by Tools/cut-magenta-prop-sheet.py; sizes in Tools/tau_25d_props_layout.py SIZE.

P(name, tx, ty) = bottom-left footprint tile in map tiles (things derived from Tools/tau_village_greybox_layout.py, already scaled);
C(name, x, y) = the same measured on the concept (160 units), scaled by VL.K. Rails / walls are laid in map tiles so their segments never overlap.
Blocking props (FootH > 0) stand on their solid blocks or on grass off the paving; stalls, lamps, banners on the ring are walk-through (FootH 0).
"""
import math
import random
import tau_village_greybox_layout as VL
from tau_25d_props_layout import DECOR, SIZE, fill_forest


def P(name, tx, ty): return (name, tx, ty)
def C(name, x, y): return (name, VL.sc(x), VL.sc(y))


def on_ellipse(c, r, n, start=0.0, skip=()):
    """n points (tile) round an ellipse, skipping angles (deg) inside any (a0, a1) of skip."""
    out = []
    for i in range(n):
        a = start + 360.0 * i / n
        if any(a0 <= a % 360 <= a1 for a0, a1 in skip): continue
        out.append((int(round(c[0] + r[0] * math.cos(math.radians(a)))), int(round(c[1] + r[1] * math.sin(math.radians(a))))))
    return out


def rail(name, x0, x1, y):
    """Horizontal rail fence segments (SIZE footprint width apart) from x0 to x1 on row y."""
    fw = SIZE[name][1]
    return [P(name, x, y) for x in range(x0, x1 - fw + 2, fw)]


def buildings():
    b = VL.BLOCKS
    out = [P(name, r[0], r[3]) for name, r in b.items() if name != "tauPondPavilion"]
    pav = b["tauPondPavilion"]
    return out + [P("tauLakeGazebo", pav[0], pav[3])]   # the pond garden's pavilion = the gazebo art


def lake():
    o = VL.OAK_TRUNK
    out = [P("tauGreatOakTree", o[0], o[3]), P("tauOakPlanterBed", o[0], o[3] + 1), P("tauLakeBridgeLong", VL.BRIDGE[0], VL.BRIDGE[3] + 1)]
    out += [C("tauFallsRocks", 92, 57), C("tauLakeDock", 103, 71), C("tauLakeDock", 61, 80)]
    out += [C("tauLilyPads", x, y) for x, y in ((70, 75), (88, 62), (66, 57), (96, 72))]
    # lamps round the shore (the bridge head and the gazebo left free), planters on the ring, a few stalls like the concept
    out += [P("tauBannerLampPost", x, y) for x, y in on_ellipse(VL.LAKE_C, (VL.LAKE_R[0] + 2.5, VL.LAKE_R[1] + 2.5), 12, 15, skip=((165, 200), (45, 75)))]
    # no benches (user 25/11 23:11 "thôi dẹp hết đống ghế đi")
    out += [C("tauFlowerPlanterBox", x, y) for x, y in ((52, 57), (108, 58), (58, 86), (102, 85), (79, 92))]
    out += [C("tauMarketStallA", 57, 45), C("tauMarketStallB", 47, 54), C("tauMarketStallA", 46, 77), C("tauMarketStallB", 109, 50), C("tauMarketStallA", 111, 78)]
    out += [C("tauBuntingPosts", 52, 49)]
    # trees standing in the ring (their 't' tiles): cherries and round trees with a bush / flower basket at the foot
    for i, (x, y) in enumerate(VL.RING_TREES):
        out += [P(("tauCherryTree", "tauRoundTreeB", "tauCherryTreeSmall")[i % 3], x, y), P(("tauFlowerBasket", "tauFlowerBushSmall")[i % 2], x + 1, y + 1)]
    return out


def districts():
    out = []
    # Guild terrace: banners at the steps, the Wanted board on the grass west of it
    g = VL.GUILD_HALL
    out += [P("tauMarketBanner", x, g[3] + 2) for x in (g[0] - 3, g[0] - 1, g[2] + 1, g[2] + 3)] + [P("tauStoneStairs", g[0] + 3, g[3] + 2)]
    out += [P("tauBulletinBoardSign", VL.GUILD_TERRACE[0] - 3, VL.GUILD_TERRACE[1] + 2)]
    # forge yard
    f = VL.FORGE
    out += [P("tauAnvil", f[2] + 1, f[3] + 2), P("tauCratesSacks", f[0] - 4, f[3] + 2), P("tauBarrels", f[2] + 4, f[3] + 3)]
    # pond garden (north-west): falls on the pond, flower arch, lilies, lamp
    out += [C("tauFallsRocks", 22, 30), C("tauFlowerArch", 17, 40), C("tauLilyPads", 31, 33), C("tauBannerLampPost", 42, 37)]
    # pet yard (west): rail fence round the rim, open to the ring on the east; pergola, kennel, sheep
    cx, cy = VL.PET_YARD_C; rx, ry = VL.PET_YARD_R
    top, bot, west, east = int(cy - ry) + 1, int(cy + ry) - 1, int(cx - rx) + 1, int(cx + rx) - 1
    out += rail("tauYardFence", west + 1, east - 1, top) + rail("tauYardFence", west + 1, east - 1, bot)
    out += [P("tauYardFenceSide", west, y) for y in range(top + 2, bot - 1, 2)] + [P("tauYardFenceSide", east, top + 2), P("tauYardFenceSide", east, bot - 1)]
    out += [C("tauGatherPergola", 15, 65), C("tauPetKennel", 17, 74), C("tauSheep", 25, 66), C("tauSheep", 20, 70), C("tauLamb", 28, 71), C("tauLamb", 13, 68)]
    # east garden: flower arch at the north steps, planters (the monument is a block)
    out += [C("tauFlowerArch", 133, 55),
            C("tauFlowerPlanterBox", 128, 66), C("tauFlowerPlanterBox", 141, 66)]
    # homes + inn: parasol tables, planters, lamps, a rail fence along the south edge
    hm = VL.HOMES
    out += [C("tauParasolTable", 37, 124), C("tauParasolTable", 45, 116), C("tauCratesSacks", 20, 116), C("tauFlowerPlanterBox", 35, 113), C("tauFlowerPlanterBox", 48, 110)]
    out += [P("tauBannerLampPost", x, y) for x in (hm[0] + 1, hm[2] - 1) for y in (hm[1] + 1, hm[3] - 1)]
    out += rail("tauYardFence", hm[0], hm[2], hm[3] + 1)
    # card & pet square: stalls, card table, planters, lamps, rail fence along the south edge
    cp = VL.CARD_PET
    out += [C("tauPetGoodsStall", 146, 124), C("tauMarketStallB", 104, 121), C("tauCardPlayTable", 109, 124), C("tauParasolTable", 128, 124),
            C("tauFlowerPlanterBox", 113, 111), C("tauFlowerPlanterBox", 131, 112)]
    out += [P("tauBannerLampPost", x, y) for x in (cp[0] + 1, cp[2] - 1) for y in (cp[1] + 1, cp[3] - 1)]
    out += rail("tauYardFence", cp[0], cp[2], cp[3] + 1)
    out += [P("tauArrowSignpost", VL.MAIN_ROAD[2] + 2, VL.MAIN_ROAD[1] + 1), P("tauArrowSignpost", VL.MAIN_ROAD[0] - 2, VL.MAIN_ROAD[1] + 1)]
    # main road: lamps on the grass either side, the quest board near the gate
    mr = VL.MAIN_ROAD
    out += [P("tauLampPost", x, y) for y in range(mr[1] + 4, mr[3] - 2, 7) for x in (mr[0] - 2, mr[2] + 2)] + [P("tauQuestBoardSign", mr[0] - 5, mr[3] - 5)]
    # raised Guild terrace: retaining wall segments on the wall rows, the grand stairs lying on the stair tiles, a lantern pillar at each stair side
    wy = VL.TERRACE_WALL_ROWS[1]; s0, s1 = VL.TERRACE_STAIRS
    # round 2: the stairs art carries ~1.6 m of wall on each side (jambs), so the wall runs stop 2 tiles short of the opening; variants A / C / B / C...
    # so no flower clump repeats side by side, a 2-tile filler where a run does not divide by 4
    x0, x1 = VL.PLATEAU[0], VL.PLATEAU[2]
    west, east = (x0, s0 - 3), (s1 + 3, x1)          # 34-43 / 54-63
    kinds = ["tauTerraceWall", "tauTerraceWallC", "tauTerraceWallB"]
    def run(a, b, k0, short_first):
        n = b - a + 1; segs = []; x = a
        if short_first and n % 4: segs.append(("tauTerraceWallShort", x)); x += 2
        i = k0
        while x + 3 <= b: segs.append((kinds[i % 3], x)); x += 4; i += 1
        if x <= b: segs.append(("tauTerraceWallShort", x))
        return [P(k, sx, wy) for k, sx in segs]
    out += run(*west, 0, False) + run(*east, 2, True)
    # stairs (foot one row onto the plaza: it leans up to the wall top), centred on the opening; lantern pillars at the outer ends of its jamb walls
    out += [P("tauTerraceStairs", s0, wy + 1), P("tauTerracePillar", s0 - 3, wy), P("tauTerracePillar", s1 + 3, wy)]
    # gate (towers / arch split from the painted gate) and the flower wall
    gx, wr = VL.GATE_X, VL.WALL_ROWS[1]
    out += [P("tauGateTowerL", gx - 4, wr), P("tauGateTowerR", gx + 4, wr), P("tauGateArchBeam", gx, wr - 2)]
    out += [P("tauWallLeft", x, wr) for x in range(gx - 12, -7, -8) if x >= 1] + [P("tauWallRight", x, wr) for x in range(gx + 10, VL.W - 3, 8)]
    return out


def village_props(rows):
    H, W = len(rows), len(rows[0])
    rng = random.Random(25112026)
    def PV(name, x, y): return (name, x, y)
    out = buildings() + lake() + districts()
    o = VL.OAK_TRUNK
    taken = {(x, y) for y in range(o[1], o[3] + 1) for x in range(o[0], o[2] + 1)}   # the great oak's trunk block is 't' but has its own tree
    taken |= set(VL.RING_TREES)                      # placed by lake() with their own kind
    # the retaining wall's two ends run into the hedge flanks: a cherry + a round tree on the flank tiles right at each end hide where the wall stops
    # (ChatGPT round 2 review 8.6/10: "hai đầu tường còn hơi cụt")
    wy = VL.TERRACE_WALL_ROWS[1]
    for fx in (VL.PLATEAU_FLANKS[0][1], VL.PLATEAU_FLANKS[1][0]):
        out += [P("tauCherryTreeSmall", fx, wy), P("tauRoundTreeB", fx, wy - 2)]
        taken |= {(fx, wy), (fx, wy - 2)}
    fill_forest(rows, rng, PV, out, taken, lambda x, y: "village", reach=2, filler="tauPineTreeSmall")
    # sparse flowers / bushes on the open grass (never next to paving) and on the meadow outside the wall
    decor = []
    for y in range(4, H - 4):
        for x in range(4, W - 4):
            c = rows[y][x]
            if c not in "gx" or any(rows[y + dy][x + dx] not in "gx" for dy in (-2, 0, 2) for dx in (-2, 0, 2)): continue
            if rng.random() > (0.03 if c == "x" else 0.015) or any(abs(x - a) < 4 and abs(y - b) < 4 for a, b in decor): continue
            decor.append((x, y)); out.append(P(rng.choice(DECOR["g"] + ["tauRoundBush", "tauFlowerBushSmall", "tauMossRocks"]), x, y))
    return [p for p in out if p[0] in SIZE]   # a name without a SIZE entry yet is left out

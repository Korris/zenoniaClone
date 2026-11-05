"""One-shot design script for Willowbrook 72x48 (phase 2). Writes rows, clusters, layout json and the preview png.
Legend: T solid (forest/cliff/ruin wall/rock)  g open  w water (solid)  B bridge (walkable)  d dirt road (walkable, art mask only).
Run: python build-willowbrook-72x48-layout.py  (reads the OLD 36x24 rows from Assets/Scripts/Maps/WillowbrookMeadowsMap.cs; once phase 4
rewrites that file, set OLD_ROWS_FROM to a saved copy or just use the generated rows.txt)."""
import re, json, os
from collections import deque
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
VIS = os.path.join(HERE, '..', 'visuals')
W, H = 72, 48
OX, OY = 0, 12                       # old section offset (cols 0-35, rows 12-35)

src = open(os.path.join(ROOT, 'Assets/Scripts/Maps/WillowbrookMeadowsMap.cs'), encoding='utf-8').read()
old = re.findall(r'^\s*"([Tgw B]{36})",\s*$', src, re.M)[:24]
assert len(old) == 24, len(old)

G = [['g'] * W for _ in range(H)]
def put(x, y, ch):
    if 0 <= x < W and 0 <= y < H: G[y][x] = ch
def rect(x, y, w, h, ch):
    for yy in range(y, y + h):
        for xx in range(x, x + w): put(xx, yy, ch)
def in_old(x, y): return OX <= x < OX + 36 and OY <= y < OY + 24

# border: rows 0-2 forest, rows 45-47 solid (HUD), col 0 and col 71
rect(0, 0, W, 3, 'T'); rect(0, 45, W, 3, 'T'); rect(0, 0, 1, H, 'T'); rect(71, 0, 1, H, 'T')

# paste old section; open its former outer edge (row 0, col 35, old HUD rows 21-23)
for r in range(24):
    for c in range(36):
        G[r + OY][c + OX] = old[r][c]
for c in range(1, 36): G[OY][c] = 'g'
for r in range(1, 24):
    if old[r][35] == 'T': G[r + OY][35] = 'g'
for r in (21, 22, 23):
    for c in range(1, 36): G[r + OY][c] = 'g'

# north stream continues from the old top (cols 19-23 at row 12) up to the waterfall at row 3
for y in range(3, 12):
    x0 = 19 + (1 if y % 4 in (1, 2) else 0)
    for x in range(x0, x0 + 5): put(x, y, 'w')
for x in range(19, 24): put(x, 12, 'w')

def line_pts(pts, r, ch, only_new=True, bridge_over=False):
    """Stamp a thick polyline of ch on open ground; with bridge_over water tiles become B (never inside the old section)."""
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        n = int(max(abs(x2 - x1), abs(y2 - y1)) * 2) + 1
        for i in range(n + 1):
            cx, cy = x1 + (x2 - x1) * i / n, y1 + (y2 - y1) * i / n
            for yy in range(int(cy - r - 1), int(cy + r + 2)):
                for xx in range(int(cx - r - 1), int(cx + r + 2)):
                    if (xx + .5 - cx) ** 2 + (yy + .5 - cy) ** 2 > (r + .35) ** 2: continue
                    if not (3 <= yy < 45 and 1 <= xx < 71): continue
                    if only_new and in_old(xx, yy): continue
                    cur = G[yy][xx]
                    if bridge_over and cur == 'w': G[yy][xx] = 'B'
                    elif cur in ('g', 'd'): G[yy][xx] = ch

# stream from the old east exit (col 35, rows 27-31) to the pond, and the pond
line_pts([(34, 28.5), (36.5, 30), (39, 33), (41, 35.5)], 2.0, 'w', only_new=False)
for y in range(34, 45):
    for x in range(34, 56):
        if ((x + .5 - 45) / 6.6) ** 2 + ((y + .5 - 38) / 3.0) ** 2 <= 1: put(x, y, 'w')

# tree strips (2 tiles wide) with gaps: cols 37-38 rows 3-19 (gap rows 9-11), cols 57-58 rows 12-20 and 27-38
for y in range(3, 20):
    if not (9 <= y <= 11): rect(37, y, 2, 1, 'T')
for y in list(range(12, 21)) + list(range(27, 39)): rect(57, y, 2, 1, 'T')
rect(37, 42, 2, 3, 'T')
# ruins NE (cols 54-63, rows 4-10): broken walls, 3-wide gaps, pillars
rect(54, 4, 10, 1, 'T'); rect(54, 4, 1, 7, 'T'); rect(63, 4, 1, 7, 'T'); rect(54, 10, 10, 1, 'T')
rect(58, 4, 3, 1, 'g'); rect(54, 6, 1, 3, 'g'); rect(63, 6, 1, 3, 'g'); rect(57, 10, 4, 1, 'g')
for p in ((57, 7), (60, 7), (57, 6)): put(p[0], p[1], 'T')
for p in ((55, 4), (62, 4), (54, 5), (63, 5), (55, 10), (62, 10)): put(p[0], p[1], 'g')
# rocks 2x2 and lone trees in the new zones
for (x, y) in ((6, 4), (28, 38), (61, 42), (33, 6), (47, 9), (68, 17), (67, 33), (24, 41)): rect(x, y, 2, 2, 'T')
for (x, y) in ((7, 43), (14, 6), (21, 8), (45, 14), (51, 7), (52, 28), (56, 33), (35, 40), (66, 10), (68, 40), (4, 9), (43, 30)): put(x, y, 'T')
# abandoned farm (cols 9-17, rows 36-43): shed 3x2 + haystack 2x1 collide; the fields are art only
rect(14, 38, 3, 2, 'T'); rect(10, 41, 2, 1, 'T')
# east dead end: barrier X (cols 69-71, rows 20-24)
rect(69, 20, 3, 5, 'T')

# roads (dirt) over open ground; bridges B where they cross water (new area only). Stamped after obstacles,
# but never over T, so the roads route around them.
roads = [
    [(36, 23), (44, 22.5), (52, 23.5), (60, 22.5), (68, 22.5)],             # main road east to the barrier
    [(46, 24), (46, 30), (47, 34)],                                          # main -> pond east bank
    [(47, 34), (53, 34), (60, 38), (63, 43)],                                # bank -> south-east
    [(62, 24), (63, 31), (64, 38)],                                          # main -> east south
    [(46, 22), (50, 17), (55, 13), (58, 11)],                                # main -> ruins
    [(10, 12), (10, 8), (8, 5)], [(27, 12), (29, 8), (31, 5)],               # north band from the old section
    [(5, 35), (7, 37), (11, 38)],                                            # old SW branch -> farm gate
    [(17, 38), (27, 37), (34, 36), (37, 31.5), (41, 28.5), (46, 28)],        # farm -> bridge over the stream -> bank road
    [(17, 41), (28, 43), (50, 43), (60, 40)],                                # farm -> south shore
    [(58, 11), (64, 8)],                                                     # inside the ruins
]
for i, pts in enumerate(roads): line_pts(pts, 1.5, 'd', bridge_over=(i == 8))  # only the farm -> bank road bridges the stream
# road tiles keep the y22-24 main line fully open at the barrier approach
rows = [''.join(r) for r in G]
assert len(rows) == 48 and all(len(r) == 72 for r in rows)
open(os.path.join(HERE, 'willowbrook-72x48-rows.txt'), 'w', newline='\n').write('\n'.join(rows) + '\n')

WALK = set('gBd')
def walk(x, y): return 0 <= x < W and 0 <= y < H and G[y][x] in WALK
START = (3, 23); PORTALS = [(0, 23), (0, 24)]
def bfs(srcs):
    d = {s: 0 for s in srcs}; q = deque(srcs)
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in d and walk(*n): d[n] = d[(x, y)] + 1; q.append(n)
    return d
dist = bfs([START])
allw = [(x, y) for y in range(H) for x in range(W) if walk(x, y)]
unreach = [p for p in allw if p not in dist]
print('walkable', len(allw), 'unreachable', len(unreach), unreach[:30])
for p in PORTALS + [START]: assert walk(*p) and p in dist, p

# clusters: id, area, count, rect (tx,ty,w,h), respawn ms
C = [
    ('1', 'Tay bac (cu)', 6, (4, 15, 9, 5), 6000), ('2', 'Tay nam (cu)', 6, (12, 27, 12, 5), 6500),
    ('3', 'Tay, ben kia suoi (cu)', 5, (25, 15, 8, 6), 7000), ('4', 'Dai bac tay', 6, (4, 3, 12, 6), 7500),
    ('5', 'Dai bac, dong suoi', 5, (28, 3, 7, 6), 6000), ('6', 'Giua bac', 7, (40, 4, 12, 7), 8000),
    ('7', 'Giua, bac duong', 6, (39, 14, 14, 6), 8500), ('8', 'Bo dong ao', 6, (45, 30, 9, 7), 9000),
    ('9', 'Phe tich dong bac', 8, (57, 3, 11, 8), 9500), ('A', 'Dong co dong', 8, (57, 15, 11, 8), 9000),
    ('B', 'Dong nam', 7, (60, 30, 10, 9), 10000),
]
out = []
for cid, area, cnt, (tx, ty, w, h), resp in C:
    tiles = [(x, y) for y in range(ty, ty + h) for x in range(tx, tx + w) if walk(x, y)]
    assert tx + w <= W and ty + h <= H
    reach = [t for t in tiles if t in dist]
    dmin = min(dist[t] for t in reach); dcen = sorted(dist[t] for t in reach)[len(reach) // 2]
    out.append(dict(id=cid, area=area, count=cnt, rect=dict(tx=tx, ty=ty, w=w, h=h), respawnMs=resp, walkable=len(tiles), pathDistMin=dmin, pathDistMedian=dcen))
    print(cid, cnt, 'walkable', len(tiles), 'need', 2 * cnt, 'dist min/med', dmin, dcen)
    assert len(tiles) >= 2 * cnt, cid
order = sorted(out, key=lambda c: c['pathDistMedian'])
# tiers by path distance from the entrance
tiers = [(1, 'bogSlime', 'frostSlime'), (1, 'bogSlime', 'frostSlime'), (2, 'hornRabbit', 'iceGoblin'), (2, 'hornRabbit', 'iceGoblin'), (2, 'hornRabbit', 'iceGoblin'),
         (3, 'thornBoar', 'iceGoblin'), (3, 'thornBoar', 'spectralWolf'), (4, 'thornBoar', 'spectralWolf'), (5, 'meadowMantis', 'spectralWolf'),
         (5, 'meadowMantis', 'spectralWolf'), (6, 'brambleTreant', 'spectralWolf')]
for c, (lv, t, fb) in zip(order, tiers): c.update(level=lv, type=t, fallbackType=fb)
print('total', sum(c['count'] for c in out), [(c['id'], c['level']) for c in order])
json.dump(dict(map='willowbrookMeadows', size=[W, H], clusters=out), open(os.path.join(HERE, 'willowbrook-72x48-clusters.json'), 'w'), indent=1)
json.dump(dict(map='willowbrookMeadows', size=[W, H], oldSection=dict(tx=OX, ty=OY, w=36, h=24), start=dict(tx=3, ty=23),
    portals=[dict(tx=0, ty=23, to='thatchVillage', toTx=33, toTy=23), dict(tx=0, ty=24, to='thatchVillage', toTx=33, toTy=24)],
    areas=dict(oldBridge=dict(tx=23, ty=22, w=5, h=1), pond=dict(cx=45, cy=38, rx=6.6, ry=3.0), ruins=dict(tx=54, ty=4, w=10, h=7),
    farm=dict(tx=9, ty=36, w=9, h=8, shed=dict(tx=14, ty=38, w=3, h=2), haystack=dict(tx=10, ty=41, w=2, h=1)),
    barrierEast=dict(tx=69, ty=20, w=3, h=5), treeStripWest=dict(tx=37, ty=3, w=2, h=17, gapRows='9-11'),
    treeStripEast=dict(tx=57, w=2, rows='12-20 and 27-38'), mainRoad='rows 22-24 from the gate to col 68')),
    open(os.path.join(HERE, 'willowbrook-72x48-layout.json'), 'w'), indent=1)

# preview, 16 px per tile
S = 16
COL = {'T': (28, 70, 34), 'g': (150, 190, 80), 'w': (60, 130, 200), 'B': (140, 90, 40), 'd': (205, 175, 120)}
im = Image.new('RGB', (W * S, H * S)); d = ImageDraw.Draw(im)
for y in range(H):
    for x in range(W):
        c = COL[G[y][x]]
        if G[y][x] == 'g' and not in_old(x, y): c = (165, 200, 95)
        if y >= 45: c = (20, 30, 20)
        if (x, y) in unreach: c = (255, 0, 255)
        d.rectangle([x * S, y * S, x * S + S - 1, y * S + S - 1], fill=c)
d.rectangle([OX * S, OY * S, (OX + 36) * S - 1, (OY + 24) * S - 1], outline=(255, 255, 255), width=2)
try: fs = ImageFont.truetype('arial.ttf', 13)
except Exception: fs = ImageFont.load_default()
for c in out:
    r = c['rect']
    d.rectangle([r['tx'] * S, r['ty'] * S, (r['tx'] + r['w']) * S - 1, (r['ty'] + r['h']) * S - 1], outline=(230, 30, 30), width=2)
    d.text((r['tx'] * S + 4, r['ty'] * S + 2), '%s Lv%d x%d' % (c['id'], c['level'], c['count']), fill=(255, 255, 255), font=fs, stroke_width=2, stroke_fill=(0, 0, 0))
for p in PORTALS: d.rectangle([p[0] * S, p[1] * S, p[0] * S + S - 1, p[1] * S + S - 1], fill=(255, 0, 0))
d.rectangle([START[0] * S, START[1] * S, START[0] * S + S - 1, START[1] * S + S - 1], fill=(255, 255, 0))
d.text((4 * S, 25 * S), 'START / GATE', fill=(255, 255, 255), font=fs, stroke_width=2, stroke_fill=(0, 0, 0))
os.makedirs(VIS, exist_ok=True)
im.save(os.path.join(VIS, 'willowbrook-72x48-layout-preview.png'))

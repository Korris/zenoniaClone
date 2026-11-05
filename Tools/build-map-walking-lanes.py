"""Walking lanes of the two 72x48 hunting maps (Willowbrook Meadows, Frost Hollow): the tiles the roads, paths, bridges, stairs and plazas run over.
Usage: python Tools/build-map-walking-lanes.py [--preview]
 The painted ground has no road tiles in its rows, so the lanes are centre lines read off the painting (tile coordinates, y down), widened to a radius (main road 1.75 tiles = 3-4 tiles wide, trails 1.25-1.5).
 Writes plans/<plan>/layout/{willowbrook,frost-hollow}-72x48-lanes.txt ('r' = lane tile, '.' = other; 48 rows of 72). WillowbrookLargeMapTests / FrostHollowLargeMapTests read those files and assert that no
 solid prop footprint stands on a lane tile; Tools/preview-*-large-composite.py report offenders. --preview draws lanes-<map>.jpg into the plan's visuals/ to check the lines against the painting.
 Design rule (user feedback 2026-11-05): nothing solid on a road, path, bridge or stair; decoration goes beside the lane, at cliff bases, corners and map borders."""
import os, sys
import numpy as np
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 72, 48
WB_PLAN = os.path.join(ROOT, "plans", "261105-1052-willowbrook-large-hunting-map")
FH_PLAN = os.path.join(ROOT, "plans", "261105-1240-frost-hollow-large-map")

# (radius, [(x, y), ...]) centre lines, tile units (x right, y down); a tile centre is (tx + 0.5, ty + 0.5)
WILLOWBROOK = [
    (1.75, [(0, 24), (7, 24.3), (12, 23.3), (15.5, 24.5), (19.7, 24.2), (23.4, 22.8), (28, 22.2), (33, 22.8), (37.5, 21.8), (42, 22), (47, 23.2), (51.5, 23.6), (56, 23.8), (61, 24.5), (64, 24)]),
    (1.25, [(19, 0), (18, 2.3), (15, 3.3), (13.6, 5), (15.5, 7.5), (17.3, 8.4), (16, 10.3), (15, 11.5)]),                       # north-west trail
    (1.25, [(13.6, 23), (13.6, 19.7), (14, 18), (16, 16.5), (18, 16), (19.5, 15.2)]),                                            # trail north from the road, old section
    (1.25, [(14, 24.5), (12.6, 26.7), (10.8, 28.1), (9.8, 30), (8.6, 31.5), (7.5, 32.5), (8.5, 35), (7.5, 37), (7, 39)]),         # trail south-west to the farm
    (1.25, [(48.3, 22.5), (48.5, 20.5), (51, 19.2), (53.5, 18), (54.5, 16.6), (56.2, 16.2)]),                                     # branch north-east
    (1.25, [(51.6, 24), (51, 26), (53, 27), (55.3, 28), (56, 29.2)]),                                                              # branch south-east
    (1.25, [(30.5, 10.8), (32.5, 11.5), (34.7, 12.4)]),
    (1.25, [(50.9, 35.6), (48.7, 37.5), (48.7, 40.3), (47.8, 43.6), (46.4, 45)]),                                                 # south trail to the pond
    (1.25, [(56, 6), (53.5, 8), (51.5, 10), (53, 11.7)]),                                                                         # trail on the ruined paving
    (1.25, [(24.4, 17.6), (25.5, 19.5), (26.7, 21)]),                                                                             # trail down to the bridge
    (1.5, [(23.5, 21), (23.5, 24)]),                                                                                             # bridge
]

FROST_HOLLOW = [
    (1.5, [(2, 21.5), (4.5, 21.5), (5.5, 22.5), (5.5, 25.5), (7, 26), (9.4, 27.7), (12.2, 29.3), (15.5, 29.5)]),                 # gate -> south-west rim stairs -> bridge 2
    (1.5, [(4.5, 21.5), (6, 19), (6.5, 15), (6.5, 12.5), (9, 12.7), (13, 12.5), (16, 12.3), (18.7, 12.3)]),                      # start -> north -> bridge 1
    (1.25, [(5.5, 8), (5.5, 11)]),                                                                                               # west valley stairs
    (1.5, [(21, 12.3), (24, 11.5), (25.5, 10.5), (26.3, 9.4), (28, 8.5), (31, 6.5), (33, 4.5), (33, 2.5)]),                      # bridge 1 -> north stairs
    (1.25, [(29.5, 11.2), (29.5, 14.2)]),                                                                                        # river-east stairs
    (1.5, [(33, 2.5), (35.6, 7.5), (39, 8.2), (42, 9.4), (45, 10), (48.3, 10), (51.6, 9.6), (53.9, 8.7), (56.7, 8.7), (59, 8.9), (61.4, 9.8),
           (64.7, 9.6), (67, 10.8), (67.5, 12.7), (65.6, 14), (63.3, 15), (62.3, 16.9), (62.8, 18.8), (64.7, 21.1), (66.1, 22.3), (68.5, 22.5)]),  # long NE road to the ice cave
    (1.5, [(49.5, 2), (49.5, 8.5)]),                                                                                             # watchtower stairs + stone circle approach
    (1.5, [(45.5, 16), (45.5, 25)]),                                                                                             # E1 stairs + cobble pass to the lake
    (1.25, [(37.5, 26), (37, 29), (37.5, 32), (40, 34.6), (44, 36.5), (47, 37.5), (49.5, 38.5), (49.5, 40), (53.4, 42.2), (56.2, 43.6)]),  # lake-shore trail, SE ledge stairs
    (1.25, [(31, 38), (33, 37), (34.7, 36)]),
    (1.5, [(9.8, 36), (9, 38), (6, 40.3), (5, 41.5), (8, 43), (12, 43.5)]),                                                      # south-west camp approach and plaza
]

def lane(lines):
    m = np.zeros((H, W), bool)
    yy, xx = np.mgrid[0:H, 0:W]
    cx, cy = xx + 0.5, yy + 0.5
    for rad, pts in lines:
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            dx, dy = x1 - x0, y1 - y0
            L2 = dx * dx + dy * dy or 1e-9
            t = np.clip(((cx - x0) * dx + (cy - y0) * dy) / L2, 0, 1)
            d = np.hypot(cx - (x0 + t * dx), cy - (y0 + t * dy))
            m |= d <= rad
    return m

def write(m, plan, name):
    os.makedirs(os.path.join(plan, "layout"), exist_ok=True)
    open(os.path.join(plan, "layout", name), "w").write("\n".join("".join("r" if v else "." for v in row) for row in m) + "\n")

def maps():
    return [("willowbrook", WB_PLAN, lane(WILLOWBROOK), os.path.join(ROOT, "Assets/Resources/Sprites/village/tiles/willowbrook-large-ground-r%dc%d.png")),
            ("frost-hollow", FH_PLAN, lane(FROST_HOLLOW), os.path.join(ROOT, "Assets/Resources/Sprites/tiles/frost-hollow-large-ground-r%dc%d.png"))]

if __name__ == "__main__":
    from PIL import Image
    for name, plan, m, tex in maps():
        write(m, plan, name + "-72x48-lanes.txt")
        print(name, "lane tiles", int(m.sum()))
        if "--preview" in sys.argv:
            full = Image.new("RGB", (W * 64, H * 64))
            for r in range(2):
                for c in range(2): full.paste(Image.open(tex % (r, c)).convert("RGB"), (c * 2304, r * 1536))
            a = np.asarray(full).copy()
            for y in range(H):
                for x in range(W):
                    if m[y, x]: a[y * 64:(y + 1) * 64, x * 64:(x + 1) * 64] = (a[y * 64:(y + 1) * 64, x * 64:(x + 1) * 64] * 0.55 + np.array([255, 230, 0]) * 0.45).astype(np.uint8)
            os.makedirs(os.path.join(plan, "visuals"), exist_ok=True)
            Image.fromarray(a).resize((1536, 1024), Image.LANCZOS).save(os.path.join(plan, "visuals", "lanes-" + name + ".jpg"), quality=86)

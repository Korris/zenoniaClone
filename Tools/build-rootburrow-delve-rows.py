"""Rows of Rootburrow V3, the "delve deeper" dark cave (21/11/2026; user: "map dungeon thì làm kiểu map đi vào sâu ấy chứ không dàn trải ra như map
thường"; ChatGPT layout V3 in docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md): 60 x 24 tiles, the way in on the
EAST edge (from Bramblewood), then east to west: entrance -> narrow tunnel -> monster cave I -> winding tunnel -> monster cave II -> deep tunnel ->
Ambercap Warden's cave (map boss) -> last tunnel -> the Wanted arena (17 x 17, the deepest cave) with the arch to Thornheart Chamber at its north-west.
Side alcoves for decor. Rows 0-1 and 21-23 are solid.

    python Tools/build-rootburrow-delve-rows.py            writes the rows into Assets/Scripts/Maps/RootburrowMap.cs (between the ROWS markers) + prints them

Shapes are rounded rectangles / ellipses for the caves and polylines of a given width for the tunnels, all in tile units (x east, y down).
After it: Tools/place-map-25d-dungeon-props.py rootburrow, Tools/build-map-25d-dungeon-ground.py rootburrow, Tools/preview-map-25d-dungeon.py rootburrow.
"""
import os, re
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 60, 24
CAVES = [  # ellipse (cx, cy, rx, ry) in tile centres
    ("arena", (9.0, 11.5, 8.2, 8.6)),
    ("ambercap", (26.5, 8.0, 4.6, 4.2)),
    ("caveII", (39.0, 15.0, 4.2, 3.9)),
    ("caveI", (49.5, 6.0, 4.2, 3.5)),
    ("entrance", (57.5, 12.0, 2.4, 2.4)),
]
TUNNELS = [  # polyline points, width in tiles (2-3 tiles: the walls hug the path)
    ([(57, 12), (55, 12), (55, 8), (53, 8)], 2.4),            # entrance -> cave I (one turn north)
    ([(46, 7.5), (44, 7.5), (44, 13), (42.5, 13)], 2.4),      # cave I -> cave II (turns south)
    ([(35.5, 16), (33, 16), (33, 10), (30.5, 10)], 2.4),      # cave II -> Ambercap (turns north)
    ([(22.5, 10), (19.5, 10), (19.5, 13), (16.5, 13)], 2.6),  # Ambercap -> arena (a last bend down)
    ([(4.5, 4), (4.5, 2)], 2.2),                              # arena -> Thornheart arch (portal tiles 4,2 / 5,2)
]
ALCOVES = [(50, 10, 2, 2), (38, 10, 2, 2), (25, 12, 3, 2), (56, 15, 2, 2)]  # x, y, w, h (decor / chest nooks)


def seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(dx * dx + dy * dy, 1e-6), 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def build():
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    floor = np.zeros((H, W), bool)
    for _, (cx, cy, rx, ry) in CAVES:
        floor |= ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1.0
    for pts, wid in TUNNELS:
        for a, b in zip(pts, pts[1:]):
            floor |= seg_dist(xx, yy, a, b) <= wid / 2.0
    for x, y, w, h in ALCOVES:
        floor[y:y + h, x:x + w] = True
    floor[:2] = False; floor[21:] = False
    floor[2, 4:6] = True                                       # the arch to Thornheart stays open up to its portal row
    floor[:, 0] = False; floor[11:14, 59] = True; floor[:11, 59] = False; floor[14:, 59] = False  # only the east gate reaches the edge
    return ["".join("d" if floor[y, x] else "T" for x in range(W)) for y in range(H)]


def main():
    rows = build()
    print("\n".join(rows))
    p = os.path.join(ROOT, "Assets", "Scripts", "Maps", "RootburrowMap.cs")
    s = open(p, encoding="utf8").read()
    body = "\n".join('            "%s",' % r for r in rows)
    end = chr(10) + "            // ROWS END"
    a = s.find("// ROWS BEGIN"); b = s.find("// ROWS END")
    if a < 0 or b < 0: raise SystemExit("ROWS markers not found in RootburrowMap.cs")
    a = s.index("\n", a) + 1
    s2 = s[:a] + body + end + s[b + len("// ROWS END"):]
    open(p, "w", encoding="utf8", newline="\n").write(s2)
    print("wrote", p)


if __name__ == "__main__":
    main()

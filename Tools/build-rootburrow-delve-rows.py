"""Rows of Rootburrow V4, a three-floor "delve deeper" dark cave (21/11/2026; user after playing V3: "Rootburrow vẫn chưa đủ độ sâu, đi có tí là hết";
ChatGPT layout V4 in docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md). Every floor is the largest map size (72 x 48):
a serpentine main tunnel (3 tiles) through 5-6 caves, side branches (2 tiles) to dead-end alcoves, the stairs at the end of the floor.
  1F rootburrow   east gate (from Bramblewood) -> ... -> stairs down at the north-west
  2F rootburrow2  stairs up (north-east) -> ... -> stairs down (north-west)
  3F rootburrow3  stairs up (east) -> ... -> Ambercap Warden's chamber -> the Wanted arena (17 x 17, west) -> arch to Thornheart Chamber (west edge)
Rows 0, 45-47 and columns 0 / 71 stay solid except the gate / arch tiles. A stairs mouth is a prop on the solid tile just NORTH of the tunnel's end
(it faces the camera), its portal tiles are the floor row right under it.

    python Tools/build-rootburrow-delve-rows.py            writes the rows of the three map classes (between the ROWS markers) + prints them

Shapes in tile units (x east, y down, tile centres on integers): caves are ellipses, tunnels polylines of a width (round ends).
After it: Tools/place-map-25d-dungeon-props.py <floor>, Tools/build-map-25d-dungeon-ground.py <floor>, Tools/preview-map-25d-dungeon.py <floor> --minimap.
"""
import os
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 72, 48
FLOORS = {
    "RootburrowMap.cs": dict(
        main=[(71, 23), (63, 23), (63, 8), (53, 8), (53, 34), (43, 34), (43, 12), (33, 12), (33, 38), (22, 38), (22, 18), (12, 18), (12, 39), (5, 39), (5, 33)],
        caves=[(63, 23, 4.2, 5), (53, 20, 4.2, 5), (43, 34, 4.5, 4.5), (33, 25, 4.5, 6), (22, 18, 4.5, 4.5)],
        branches=[[(53, 30), (57, 30), (57, 40)], [(38, 12), (38, 5)], [(15, 18), (15, 8)]],
        nooks=[(57, 40, 2.5, 2.2), (38, 5, 2.6, 2.2), (15, 8, 3, 2.6)],
        edge=[(71, y) for y in (22, 23, 24)]),
    "RootburrowB2Map.cs": dict(
        main=[(66, 6), (66, 9), (62, 9), (62, 35), (52, 35), (52, 11), (43, 11), (43, 39), (33, 39), (33, 14), (24, 14), (24, 37), (15, 37), (15, 9), (6, 9), (6, 6)],
        caves=[(62, 22, 3.5, 5), (52, 25, 3.5, 5), (43, 26, 3.5, 6), (33, 25, 3.5, 5), (24, 25, 3.5, 5), (15, 21, 3.5, 5)],
        branches=[[(58, 35), (58, 42)], [(43, 11), (43, 6), (38, 6)], [(15, 9), (15, 5), (20, 5)]],
        nooks=[(58, 42, 2.5, 2.2), (37, 6, 3, 2.5), (20, 5, 2.2, 2)],
        edge=[]),
    "RootburrowB3Map.cs": dict(
        main=[(67, 34), (67, 37), (61, 37), (61, 9), (53, 9), (53, 33), (46, 33), (46, 15), (38, 15), (38, 23), (30, 23), (30, 37), (23, 37), (23, 23), (19, 23)],
        caves=[(61, 23, 3.5, 5), (53, 25, 3.5, 5), (38, 23, 5.5, 6), (30, 37, 4, 4)],
        branches=[[(61, 37), (61, 40), (57, 40)], [(53, 9), (53, 6), (48, 6)], [(30, 37), (30, 40), (27, 40)], [(4, 23), (1, 23)]],
        nooks=[(57, 40, 2.5, 2.2), (48, 6, 2.2, 2), (27, 40, 2.4, 2.2)],
        arena=(4, 15, 17, 17),
        edge=[(1, y) for y in (22, 23, 24)]),
}


def seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(dx * dx + dy * dy, 1e-6), 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def build(cfg):
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    floor = np.zeros((H, W), bool)
    for pts, wid in [(cfg["main"], 3.0)] + [(b, 2.0) for b in cfg["branches"]]:
        for a, b in zip(pts, pts[1:]):
            floor |= seg_dist(xx, yy, a, b) <= wid / 2.0
    for cx, cy, rx, ry in cfg["caves"] + cfg["nooks"]:
        floor |= ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1.0
    if "arena" in cfg:                                         # a rounded square: the 40-player Wanted arena
        x0, y0, w, h = cfg["arena"]
        cx, cy = x0 + (w - 1) / 2.0, y0 + (h - 1) / 2.0
        sq = (np.abs(xx - cx) <= w / 2.0 - 0.5) & (np.abs(yy - cy) <= h / 2.0 - 0.5)
        floor |= sq & (np.hypot(np.maximum(np.abs(xx - cx) - (w / 2.0 - 3.5), 0), np.maximum(np.abs(yy - cy) - (h / 2.0 - 3.5), 0)) <= 3.0)
    floor[0] = False; floor[45:] = False; floor[:, 0] = False; floor[:, W - 1] = False
    for x, y in cfg["edge"]: floor[y, x] = True                # the gate / arch reaches the edge column
    return ["".join("d" if floor[y, x] else "T" for x in range(W)) for y in range(H)]


def write(name, rows):
    p = os.path.join(ROOT, "Assets", "Scripts", "Maps", name)
    s = open(p, encoding="utf8").read()
    a = s.find("// ROWS BEGIN"); b = s.find("// ROWS END")
    if a < 0 or b < 0: raise SystemExit("ROWS markers not found in " + name)
    a = s.index("\n", a) + 1
    body = "\n".join('            "%s",' % r for r in rows)
    s2 = s[:a] + body + chr(10) + "            // ROWS END" + s[b + len("// ROWS END"):]
    open(p, "w", encoding="utf8", newline="\n").write(s2)
    print("wrote", p)


def main():
    for name, cfg in FLOORS.items():
        rows = build(cfg)
        print(name); print("\n".join(rows))
        write(name, rows)


if __name__ == "__main__":
    main()

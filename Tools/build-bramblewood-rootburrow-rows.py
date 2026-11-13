"""Collision rows + ground textures of Bramblewood Forest, Rootburrow and Thornheart Chamber (World Map 2.1, 13/11/2026).

Usage (needs Pillow):
  python Tools/build-bramblewood-rootburrow-rows.py            expand the hand files to rows, flood-fill, write <map>-rows.txt (C# string rows)
  python Tools/build-bramblewood-rootburrow-rows.py --grounds  also write the ground textures to Assets/Resources/Sprites/village/tiles/
Hand files (the source of truth, edit these): plans/261113-1010-world-map-new-maps/layout/<map>-rows-hand.txt, one line per row
"<row>: g5-16 d17-18 w25-26" (char + column range); unlisted tiles are T. Solid chars: T w. The three paintings are not colour-classified:
root walls, dirt and trunks share the same browns, so a colour pass mislabels more than it saves.
Grounds: the 36x24 paintings (1536x1024) are resized to 2304x1536 like every other 36x24 map; the chamber is already 64 px per tile (24x16).
Tools/preview-bramblewood-rootburrow-composite.py draws the red overlay + props + zones and runs the placement checks.
"""
import os, sys
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
LAYOUT = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/layout")
TILES = os.path.join(ROOT, "Assets/Resources/Sprites/village/tiles")
ART = os.path.join(ROOT, "Assets/ArtLibrary/maps")
SOLID = "Tw"
# id: (cols, rows, source painting, ground texture name, start tile used for the flood fill)
MAPS = {
    "bramblewood-forest": (36, 24, "bramblewood-forest/bramblewood-forest-ground-only-chatgpt-1.png", "bramblewood-forest-ground", (17, 2)),
    "rootburrow": (36, 24, "rootburrow/rootburrow-chatgpt-1.png", "rootburrow-ground", (32, 13)),
    "thornheart-chamber": (24, 16, "rootburrow/thornheart-chamber-chatgpt-1.png", "thornheart-chamber-ground", (12, 10)),
}


def load_rows(map_id):
    cols, rows = MAPS[map_id][0], MAPS[map_id][1]
    grid = [["T"] * cols for _ in range(rows)]
    for line in open(os.path.join(LAYOUT, map_id + "-rows-hand.txt"), encoding="utf-8"):
        line = line.split("#")[0].strip()
        if not line: continue
        r, spans = line.split(":")
        for span in spans.split():
            a, _, b = span[1:].partition("-")
            for c in range(int(a), int(b or a) + 1):
                if grid[int(r)][c] != "T": sys.exit("%s row %s col %d listed twice" % (map_id, r, c))
                grid[int(r)][c] = span[0]
    return ["".join(g) for g in grid]


def flood(rows, start, blocked=()):
    """Tiles reachable from start over non-solid tiles (blocked = extra solid tiles, e.g. prop footprints)."""
    h, w = len(rows), len(rows[0])
    seen, stack = set(), [start]
    while stack:
        c, r = stack.pop()
        if (c, r) in seen or not (0 <= c < w and 0 <= r < h) or rows[r][c] in SOLID or (c, r) in blocked: continue
        seen.add((c, r))
        stack += [(c + 1, r), (c - 1, r), (c, r + 1), (c, r - 1)]
    return seen


def main():
    for map_id, (cols, nrows, src, tex, start) in MAPS.items():
        rows = load_rows(map_id)
        seen = flood(rows, start)
        lost = [(c, r) for r in range(nrows) for c in range(cols) if rows[r][c] not in SOLID and (c, r) not in seen]
        hud = [r for r in range(nrows - 3, nrows) if set(rows[r]) - set(SOLID)]
        with open(os.path.join(LAYOUT, map_id + "-rows.txt"), "w", encoding="utf-8") as f:
            f.write("".join('            "%s",\n' % r for r in rows))
        print("%s: %dx%d, %d walkable, unreachable %s, open HUD rows %s" % (map_id, cols, nrows, len(seen), lost or "none", hud or "none"))
        if "--grounds" in sys.argv:
            im = Image.open(os.path.join(ART, src)).convert("RGB")
            if cols == 36: im = im.resize((2304, 1536), Image.LANCZOS)
            im.save(os.path.join(TILES, tex + ".png"))
            print("  wrote", tex + ".png", im.size)


if __name__ == "__main__":
    main()

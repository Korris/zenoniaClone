"""Slices the painted skill rosters (ArtLibrary/custom/skill-icons-<class>-roster.png, 3 rows x 5 icons) into the 128px touch-button icons.
Run from the project root (Python 3 + Pillow). Cells are found from the alpha ink (gutters between icons), like the web painted-combat-assets.js.
Output: Assets/Resources/Sprites/skill-icons/<class>-<index>.png, index = row * 5 + column (the web SKILL_ORDER slot of that class).
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets", "ArtLibrary", "custom")
OUT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "skill-icons")
SIZE = 128

def bands(counts, min_ink, n):
    """Runs of indexes whose ink count exceeds min_ink, merged / trimmed to the n widest."""
    runs, start = [], None
    for i, c in enumerate(counts + [0]):
        if c > min_ink and start is None: start = i
        elif c <= min_ink and start is not None: runs.append((start, i)); start = None
    runs.sort(key=lambda r: r[1] - r[0], reverse=True)
    return sorted(runs[:n])

def slice_roster(cls):
    im = Image.open(os.path.join(SRC, "skill-icons-%s-roster.png" % cls)).convert("RGBA")
    w, h = im.size
    a = im.getchannel("A").load()
    rows = bands([sum(1 for x in range(0, w, 2) if a[x, y] > 80) for y in range(h)], w * 0.1, 3)
    if len(rows) != 3: raise SystemExit("%s: found %d rows" % (cls, len(rows)))
    for r, (y0, y1) in enumerate(rows):
        cols = bands([sum(1 for y in range(y0, y1, 2) if a[x, y] > 80) for x in range(w)], (y1 - y0) * 0.1, 5)
        if len(cols) != 5: raise SystemExit("%s row %d: found %d columns" % (cls, r, len(cols)))
        for c, (x0, x1) in enumerate(cols):
            cell = im.crop((x0, y0, x1, y1))
            side = max(cell.size)
            sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
            sq.paste(cell, ((side - cell.size[0]) // 2, (side - cell.size[1]) // 2))
            sq.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, "%s-%d.png" % (cls, r * 5 + c)))

os.makedirs(OUT, exist_ok=True)
for cls in ["warrior", "wizard", "mechanic", "cleric", "assassin"]: slice_roster(cls)
print("skill icons written to", OUT, len(os.listdir(OUT)))

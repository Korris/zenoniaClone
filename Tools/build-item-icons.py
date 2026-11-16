"""Builds the 96x96 item icons used by the Unity inventory / ground loot (run from the project root, Python 3 + Pillow + numpy).

 - armor / gloves / boots (shared fallback; class sets come from build-class-gear-icons.py): sliced from the web's painted 5x2 atlases (transparent gutters), tier 0..9
 - weapon-<class>-<tier>, offhand-<class>-<tier>: no longer here (removed 16/11 with the single-gun Mechanic and the Cleric companion art);
   each class's tier tool writes its own (build-warrior-sword / -shield, build-wizard-tier-staff-book, build-mechanic-pistol-tiers / -cannon-tiers,
   build-assassin-tier-daggers, build-cleric-tier-staff-shield)
Output: Assets/Resources/Sprites/icons/<name>.png
"""
import os, sys
import numpy as np
from PIL import Image

WEB = os.environ.get("ZENONIA_WEB", r"C:\Project\zenoniaClone\Assets\custom")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPR = os.path.join(ROOT, "Assets", "Resources", "Sprites")
OUT = os.path.join(SPR, "icons")
SIZE = 96
os.makedirs(OUT, exist_ok=True)

def fit(im):
    """Trim to content, scale to fit SIZE-8, centre on a transparent SIZE x SIZE canvas."""
    bb = im.getbbox()
    if bb: im = im.crop(bb)
    k = (SIZE - 8) / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    out.paste(im, ((SIZE - im.width) // 2, (SIZE - im.height) // 2), im)
    return out

def bands(mask):
    """[(start, end)] runs of True in a 1-D boolean array."""
    runs, start = [], None
    for i, v in enumerate(mask):
        if v and start is None: start = i
        if not v and start is not None: runs.append((start, i)); start = None
    if start is not None: runs.append((start, len(mask)))
    return [r for r in runs if r[1] - r[0] > 8]

def slice_atlas(slot, file):
    im = Image.open(os.path.join(WEB, file)).convert("RGBA")
    a = np.array(im)[:, :, 3] > 24
    rows = bands(a.any(axis=1))
    cells = []
    for r0, r1 in rows:
        cols = bands(a[r0:r1].any(axis=0))
        if len(cols) == 4:  # two painted icons touch: cut the widest band at its thinnest column
            widest = max(range(4), key=lambda i: cols[i][1] - cols[i][0])
            c0, c1 = cols[widest]
            span = a[r0:r1, c0:c1].sum(axis=0)
            lo, hi = int((c1 - c0) * 0.35), int((c1 - c0) * 0.65)
            cut = c0 + lo + int(np.argmin(span[lo:hi]))
            cols[widest:widest + 1] = [(c0, cut), (cut, c1)]
        for c0, c1 in cols:
            cells.append(im.crop((c0, r0, c1, r1)))
    if len(cells) != 10: sys.exit("%s: expected 10 icons, found %d" % (slot, len(cells)))
    for tier, c in enumerate(cells): fit(c).save(os.path.join(OUT, "%s-%d.png" % (slot, tier)))

# helmet-N is now the shared NECKLACE set (Tools/build-class-gear-icons.py write shared, designer 08/11): not sliced from the old hat atlas any more
for slot, f in {"armor": "armor-icons-chibi.png", "gloves": "gloves-icons-chibi.png", "boots": "boots-icons-chibi.png"}.items():
    slice_atlas(slot, f)

print("icons written to", OUT, len(os.listdir(OUT)))

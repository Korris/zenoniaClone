"""Builds the 96x96 item icons used by the Unity inventory / ground loot (run from the project root, Python 3 + Pillow + numpy).

 - helmet / armor / gloves / boots: sliced from the web's painted 5x2 atlases (transparent gutters), tier 0..9
 - weapon-<class>-<tier>, offhand-<class>-<tier>: the cut held-gear overlay PNGs (Resources/Sprites), tilted like the web gear icons
Output: Assets/Resources/Sprites/icons/<name>.png
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw

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

def load_names(data, key):
    return [e["name"] for e in json.load(open(os.path.join(SPR, "..", "Data", data + ".json")))[key]]

def overlay(folder, name, tilt):
    im = Image.open(os.path.join(SPR, folder, name + "-base.png")).convert("RGBA")
    if tilt: im = im.rotate(tilt, expand=True, resample=Image.BICUBIC)
    return fit(im)

def spellbook():
    im = Image.new("RGBA", (96, 96), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((20, 12, 76, 84), 8, fill=(84, 52, 150, 255), outline=(30, 20, 70, 255), width=3)
    d.rounded_rectangle((28, 12, 76, 84), 8, fill=(120, 80, 200, 255), outline=(30, 20, 70, 255), width=3)
    d.ellipse((42, 36, 66, 60), fill=(255, 224, 110, 255), outline=(30, 20, 70, 255), width=2)
    d.rectangle((22, 18, 27, 78), fill=(245, 235, 210, 255))
    return im

for slot, f in {"helmet": "helmet-icons-chibi.png", "armor": "armor-icons-chibi.png", "gloves": "gloves-icons-chibi.png", "boots": "boots-icons-chibi.png"}.items():
    slice_atlas(slot, f)

specs = [  # class, weapon (manifest file, key, folder, tilt), offhand (...)
    ("warrior", ("held-gear", "sword", "sword", -45), ("held-gear", "shield", "shield", 0)),
    ("wizard", ("held-wizard", "weapon", "wizard-staff", -45), None),
    ("mechanic", ("held-mechanic", "weapon", "mechanic-gun", 45), ("held-mechanic", "companion", "mechanic-robot", 0)),
    ("assassin", ("held-assassin", "weapon", "assassin-dagger", -45), ("held-assassin", "weapon", "assassin-dagger", 45)),
    ("cleric", ("held-cleric", "weapon", "cleric-staff", -45), ("held-cleric", "companion", "cleric-spirit", 0)),
]
for cls, w, o in specs:
    for tier, name in enumerate(load_names(w[0], w[1])): overlay(w[2], name, w[3]).save(os.path.join(OUT, "weapon-%s-%d.png" % (cls, tier)))
    for tier in range(10):
        img = overlay(o[2], load_names(o[0], o[1])[tier], o[3]) if o else fit(spellbook())
        img.save(os.path.join(OUT, "offhand-%s-%d.png" % (cls, tier)))
print("icons written to", OUT, len(os.listdir(OUT)))

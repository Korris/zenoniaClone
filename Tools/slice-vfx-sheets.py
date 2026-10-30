"""Slice the hand-painted ranged-attack VFX sheets (3 rows x 4 cols, RGBA) by transparent gutters (not a fixed grid).
Writes Assets/Resources/Sprites/fx/vfx/<class>/<row>_<n>.png (trimmed) and Assets/Resources/Data/vfx-<class>.json
(per-frame size + pivot in pixels from the bottom-left of the trimmed frame). Rows: cast, proj, impact.
Usage: python Tools/slice-vfx-sheets.py   (run from the project root; helpers in Tools/vfx_slice_common.py)"""
import os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vfx_slice_common import clean_mask, spans, trim_box, head_pivot, write_frame, write_json

SRC = "Assets/ArtLibrary/vfx"
OUT = "Assets/Resources/Sprites/fx/vfx"
DATA = "Assets/Resources/Data"
# (json/folder id, source sheet) ; the two mechanic sheets are separate colour variants
SHEETS = [("cleric", "cleric/cleric-attack-vfx.png"), ("wizard", "wizard/wizard-attack-vfx.png"),
          ("mechanic-blue", "mechanic/mechanic-attack-vfx-blue.png"), ("mechanic-orange", "mechanic/mechanic-attack-vfx-orange.png")]
ROWS = ["cast", "proj", "impact"]
def process(cid, rel):
    img = np.array(Image.open(os.path.join(SRC, rel)).convert("RGBA"))
    mask = clean_mask(img[..., 3])
    img[..., 3] = np.where(mask, img[..., 3], 0)
    # color bleed from removed specks is irrelevant (alpha 0)
    rows = spans(mask.sum(axis=1), 3)
    cls = cid.split("-")[0]
    folder = os.path.join(OUT, cid); os.makedirs(folder, exist_ok=True)
    meta = {"frames": []}
    for rname, (y0, y1) in zip(ROWS, rows):
        for c, (x0, x1) in enumerate(spans(mask[y0:y1].sum(axis=0), 4)):
            sub = img[y0:y1, x0:x1]
            m = mask[y0:y1, x0:x1]
            ty0, ty1, tx0, tx1 = trim_box(m)
            fm = m[ty0:ty1, tx0:tx1]
            fr = sub[ty0:ty1, tx0:tx1]
            h, w = fr.shape[:2]
            if rname == "proj": px, py = head_pivot(fr, fm, 0 if cls == "wizard" else 75)   # wizard: orb centre
            elif rname == "impact" and cls == "cleric": px, py = w / 2.0, h - 1.0       # pillar anchored upright at the bottom-centre
            elif rname == "cast" and cls == "mechanic": px, py = w * 0.2, h / 2.0     # barrel end of the muzzle blast
            else: px, py = w / 2.0, h / 2.0
            meta["frames"].append(write_frame(folder, "%s_%d" % (rname, c), fr, px, py))  # py from the bottom
    return meta

for cid, rel in SHEETS:
    meta = process(cid, rel)
    write_json(os.path.join(DATA, "vfx-%s.json" % cid), meta)
    print(cid, len(meta["frames"]), "frames")

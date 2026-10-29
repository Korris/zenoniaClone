"""Slice the hand-painted ranged-attack VFX sheets (3 rows x 4 cols, RGBA) by transparent gutters (not a fixed grid).
Writes Assets/Resources/Sprites/fx/vfx/<class>/<row>_<n>.png (trimmed) and Assets/Resources/Data/vfx-<class>.json
(per-frame size + pivot in pixels from the bottom-left of the trimmed frame). Rows: cast, proj, impact.
Usage: python Tools/slice-vfx-sheets.py   (run from the project root; needs numpy, opencv-python, Pillow)"""
import json, os
import numpy as np, cv2
from PIL import Image

SRC = "Assets/ArtLibrary/vfx"
OUT = "Assets/Resources/Sprites/fx/vfx"
DATA = "Assets/Resources/Data"
# (json/folder id, source sheet) ; the two mechanic sheets are separate colour variants
SHEETS = [("cleric", "cleric/cleric-attack-vfx.png"), ("wizard", "wizard/wizard-attack-vfx.png"),
          ("mechanic-blue", "mechanic/mechanic-attack-vfx-blue.png"), ("mechanic-orange", "mechanic/mechanic-attack-vfx-orange.png")]
ROWS = ["cast", "proj", "impact"]
ALPHA_T = 10        # alpha counted as "ink" when looking for gutters / trimming
SPECK_PX = 12       # connected components smaller than this are stray specks
GUTTER_MIN = 6      # px of empty projection that counts as a gutter

def clean_mask(a):
    m = (a > ALPHA_T).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    keep = np.zeros(n, bool)
    keep[1:] = st[1:, cv2.CC_STAT_AREA] >= SPECK_PX
    return keep[lab]

def spans(proj, count):
    """Ink spans of a 1-D projection, merged across gaps < GUTTER_MIN; keep the `count` largest, in order."""
    idx = np.flatnonzero(proj > 0)
    out, s, p = [], idx[0], idx[0]
    for i in idx[1:]:
        if i - p > GUTTER_MIN: out.append((s, p + 1)); s = i
        p = i
    out.append((s, p + 1))
    if len(out) != count:
        out = sorted(sorted(out, key=lambda r: -proj[r[0]:r[1]].sum())[:count])
    return out

def head_pivot(rgba, mask, pct=75):
    """Projectile pivot = centroid of the white-hot pixels in the right half of the bright core (the head)."""
    rgb = rgba[..., :3].astype(int)
    hot = mask & (rgb.min(axis=2) > 215) & (rgba[..., 3] > 200)
    ys, xs = np.nonzero(hot)
    if len(xs) < 8:
        ys, xs = np.nonzero(mask & (rgba[..., 3] > 200))
    xr = np.percentile(xs, pct)           # head sits at the leading (right) end of the bright core
    sel = xs >= xr
    return float(xs[sel].mean()), float(ys[sel].mean())

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
            ys, xs = np.nonzero(m)
            ty0, ty1, tx0, tx1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            fr, fm = sub[ty0:ty1], m[ty0:ty1, tx0:tx1]
            fr = sub[ty0:ty1, tx0:tx1]
            h, w = fr.shape[:2]
            if rname == "proj": px, py = head_pivot(fr, fm, 0 if cls == "wizard" else 75)   # wizard: orb centre
            elif rname == "impact" and cls == "cleric": px, py = w / 2.0, h - 1.0       # pillar anchored upright at the bottom-centre
            elif rname == "cast" and cls == "mechanic": px, py = w * 0.2, h / 2.0     # barrel end of the muzzle blast
            else: px, py = w / 2.0, h / 2.0
            name = "%s_%d" % (rname, c)
            Image.fromarray(fr).save(os.path.join(folder, name + ".png"))
            meta["frames"].append({"name": name, "w": int(w), "h": int(h), "px": round(px, 1), "py": round(h - py, 1)})  # py from the bottom
    return meta

for cid, rel in SHEETS:
    meta = process(cid, rel)
    os.makedirs(DATA, exist_ok=True)
    with open(os.path.join(DATA, "vfx-%s.json" % cid), "w") as f: json.dump(meta, f, indent=1)
    print(cid, len(meta["frames"]), "frames")

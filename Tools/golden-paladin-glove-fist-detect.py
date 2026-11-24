"""Golden Paladin TEST look (24/11): find the dark-brown gloves (fists) on every cut body cell and write the fist table json the variant
fist module reads (Tools/ro_fantasy_golden_paladin_fists.py). The generic detector (hero_layered_fists) takes the belt / boots on this
gold armour; here a glove = a compact dark-brown blob (r > g > b, dark, saturated) above the boots, the belt row band excluded by shape.

  RO_FANTASY_VARIANT=golden-paladin python Tools/golden-paladin-glove-fist-detect.py   (after `build-ro-fantasy-hero-game-assets.py write warrior`)

Hands: R = right hand (sword), L = left hand (shield). Painted rows: 0 down, 1 down-right, 2 right, 3 up-right, 4 up (+ attack 5 up-left,
6 left, 7 down-left). Frame 0 of each row: down / down-right / down-left R = screen-left blob; up / up-right / up-left R = screen-right;
right: R = the near blob (one blob: L hidden); left: L = the near blob. Later frames: each hand follows its nearest blob of the previous
frame (tracking), a hand without a blob keeps its last spot hidden. Review: the marked sheets of `fists`, then fix in FIX of the module.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ro_fantasy_body_cut as bc  # noqa: E402

CW, CH = bc.CW, bc.CH
SPR = bc.ROOT / "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body"
OUT = bc.variant.SHEETS / "warrior-glove-fists.json"


def gloves(cell):
    """[(x, y, area)] of glove blobs in one cell."""
    a = np.asarray(cell).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    mx = np.maximum(np.maximum(r, g), b)
    sat = (mx - np.minimum(np.minimum(r, g), b)) / np.maximum(mx, 1)
    m = (al > 200) & (r > g) & (g >= b) & (mx < 150) & (mx > 45) & (sat > 0.35) & (r - b > 35)
    m[196:] = False  # boots (soles + cuffs are dark brown too)
    m[:30] = False  # above any raised fist
    m = ndimage.binary_opening(m, iterations=1)
    lab, n = ndimage.label(m)
    out = []
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        ys, xs = np.nonzero(lab[sl] == i)
        area = len(ys)
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if area < 55 or w > 2.2 * h + 6 or w > 30:  # belt strips are wide and thin
            continue
        out.append((float(xs.mean() + sl[1].start), float(ys.mean() + sl[0].start), area))
    return out


def first(blobs, row):
    blobs = sorted(blobs, key=lambda b: -b[2])[:2]
    if not blobs:
        return {"R": [160, 160, False], "L": [160, 160, False]}
    if len(blobs) == 1:
        x, y, _ = blobs[0]
        near = "L" if row == 6 else "R"
        far = "R" if near == "L" else "L"
        return {near: [x, y, True], far: [x, y, False]}
    a, b = sorted(blobs, key=lambda t: t[0])
    if row in (0, 1, 7, 2, 6):  # front-ish (right / left two blobs: the near one is in front, same order as the front views)
        return {"R": [a[0], a[1], True], "L": [b[0], b[1], True]}
    return {"R": [b[0], b[1], True], "L": [a[0], a[1], True]}


def track(prev, blobs):
    out = {}
    free = sorted(blobs, key=lambda b: -b[2])[:2]
    pairs = sorted(((np.hypot(prev[h][0] - x, prev[h][1] - y), h, k) for h in ("R", "L") for k, (x, y, _) in enumerate(free)))
    used_h, used_k = set(), set()
    for d, h, k in pairs:
        if h in used_h or k in used_k or d > 70:
            continue
        out[h] = [free[k][0], free[k][1], True]
        used_h.add(h)
        used_k.add(k)
    for h in ("R", "L"):
        out.setdefault(h, [prev[h][0], prev[h][1], False])
    return out


def main():
    bj = json.loads((bc.ROOT / "Assets/Resources/Data/hero-layered/ro-fantasy/warrior/body.json").read_text(encoding="utf-8"))
    table = {}
    for a in bj["anims"]:
        anim, n = a["name"], a["frames"]
        sh = Image.open(SPR / f"{anim}.png").convert("RGBA")
        rows = sh.height // CH
        table[anim] = {}
        for r in range(rows):
            prev = None
            for f in range(n):
                cell = sh.crop((f * CW, r * CH, (f + 1) * CW, (r + 1) * CH))
                bl = gloves(cell)
                t = first(bl, r) if prev is None else track(prev, bl)
                t = {k: [round(v[0], 1), round(v[1], 1), bool(v[2])] for k, v in t.items()}
                table[anim][f"{r},{f}"] = t
                prev = t
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(table, indent=0), encoding="utf-8")
    print("wrote", OUT, {k: len(v) for k, v in table.items()})


if __name__ == "__main__":
    main()

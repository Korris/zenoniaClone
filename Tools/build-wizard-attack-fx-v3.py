"""Wizard basic-attack VFX v3, after the board docs/reference/wizard-basic-attack-board.png (2026-11-08), from one ChatGPT sheet on flat #00FF00:
Assets/ArtLibrary/classes/wizard/vfx/attack-v3/wizard-attack-fx-v3-chatgpt-1.png (6 rows x 4 frames, 256 px cells; chat
https://chatgpt.com/c/6af01b4d-2274-83ec-b9a8-43cf2efa3cdd, sent with ref-board-fx-rows.png). Rows of the sheet -> rows of the set "wizard-v3":
  Colours: violet turned to blue (to_blue): the class colour of the Wizard is blue / ice.
  1 Staff Cast Flash -> cast   (every hit's windup, VfxAnim.Cast; 1.35x on the finisher)
  2 Arcane Bolt      -> proj   (hits 1 and 3, pointing right, pivot on the crystal head)
  3 Frost Shard      -> shard  (hit 2, the book's ice spear; BoltDef sprite "frostShard")
  4 Arcane Orb       -> bolt   (hit 4's orb, round, pivot at its centre)
  5 Orb Burst        -> blast  (hit 4's burst, VfxAnim.Blast sizes it to the blast radius)
  6 Impact           -> impact (hits 1-3; pivot 70 % down: the ice chips spray up from the hit point)
Output read by VfxSprites ("wizard-v3"): Assets/Resources/Sprites/classes/wizard/fx/attack-v3/<row>_<n>.png (+ .meta cloned from attack-v2)
and Assets/Resources/Data/classes/wizard/vfx-attack-v3.json (w, h, pivot px from the bottom-left). One output px = VfxSprites.UnitsPerPx (0.058) units.

  python Tools/build-wizard-attack-fx-v3.py cut       frames, json, metas
  python Tools/build-wizard-attack-fx-v3.py preview   OUT.png: every frame at game size on snow and on dark, next to the hero
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
sys.path.insert(0, "Tools")
from wizard_attack_fx_common import clone_meta  # noqa: E402

SRC = "Assets/ArtLibrary/classes/wizard/vfx/attack-v3/wizard-attack-fx-v3-chatgpt-1.png"
OUT = "Assets/Resources/Sprites/classes/wizard/fx/attack-v3"
LIKE = "Assets/Resources/Sprites/classes/wizard/fx/attack-v2"
DATA = "Assets/Resources/Data/classes/wizard"
SHEET_ROWS = ["cast", "proj", "shard", "bolt", "blast", "impact"]
# source px -> output px (0.058 units per output px): cast ~13, dart ~16, orb ~13 (x1.35 finisher = ~18), impact ~16 units
ROW_SCALE = {"cast": 1.15, "proj": 1.3, "shard": 1.3, "bolt": 1.1, "blast": 1.0, "impact": 1.25}
UNITS_PER_PX = 0.058
CELL = 256


def key_green(path):
    """Straight RGBA: alpha from the distance to #00FF00, green spill clamped on the soft edge (the art has no green)."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.maximum(np.maximum(r, b), 255 - g)
    alpha = np.clip((d - 35) / 80, 0, 1)
    g2 = np.minimum(g, np.maximum(r, b) + 10)  # no green anywhere in this art: clamp it everywhere
    return np.dstack([r, g2, b, alpha * 255]).clip(0, 255).astype(np.uint8)


# Class colour (user 2026-11-08, docs/sprite-rules.md "Màu chủ đạo theo class"): Wizard = blue / ice, violet belongs to the Assassin. The sheet
# was painted blue + violet; every violet / purple pixel (hue 245-330 deg) is turned to blue, keeping its saturation and brightness, so the
# dark-violet outlines become navy, the violet glows royal blue, the white cores and ice-cyan stay.
BLUE_HUE = 218.0
VIOLET = (245.0, 330.0)


def to_blue(px):
    """RGBA uint8 -> same with violet hues rotated to BLUE_HUE (HSV, vectorised)."""
    rgb = px[..., :3].astype(np.float64) / 255
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.zeros_like(mx)
    m = d > 1e-6
    rm, gm, bm = m & (mx == r), m & (mx == g) & (mx != r), m & (mx == b) & (mx != r) & (mx != g)
    h[rm] = (60 * ((g - b)[rm] / d[rm]) + 360) % 360
    h[gm] = 60 * ((b - r)[gm] / d[gm]) + 120
    h[bm] = 60 * ((r - g)[bm] / d[bm]) + 240
    sat = np.where(mx > 0, d / np.maximum(mx, 1e-6), 0)
    sel = m & (h >= VIOLET[0]) & (h <= VIOLET[1]) & (sat > 0.15)
    h = np.where(sel, BLUE_HUE, h)
    c = mx * sat
    x = c * (1 - np.abs((h / 60) % 2 - 1))
    z = np.zeros_like(c)
    k = (h // 60).astype(int) % 6
    rgb2 = np.select([k[..., None] == i for i in range(6)],
                     [np.stack(t, -1) for t in ((c, x, z), (x, c, z), (z, c, x), (z, x, c), (x, z, c), (c, z, x))])
    rgb2 = rgb2 + (mx - c)[..., None]
    out = px.copy()
    out[..., :3] = np.where(sel[..., None], (rgb2 * 255).round().clip(0, 255), px[..., :3]).astype(np.uint8)
    return out


def bands(al, axis, count):
    """`count` ink bands along an axis (rows: axis 1 summed), split at the emptiest lines between them (frames do not sit on the 256 px grid)."""
    prof = al.sum(axis=axis).astype(float)
    size = len(prof) / count
    cuts = [0]
    for k in range(1, count):
        lo, hi = int(size * k - size * 0.35), int(size * k + size * 0.35)
        cuts.append(lo + int(np.argmin(prof[lo:hi])))
    cuts.append(len(prof))
    return cuts


def frames():
    """{(row name, n): RGBA cropped to its ink}: blobs assigned to the band of their centroid (bands split at the emptiest lines)."""
    px = to_blue(key_green(SRC))
    al = px[..., 3] > 40
    rows = bands(al, 1, len(SHEET_ROWS))
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=2))
    cells = {}
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(xs) < 12:
            continue
        r = int(np.searchsorted(rows, ys.mean(), side="right") - 1)
        cells.setdefault(r, []).append((xs.mean(), lab == i))
    out = {}
    for r, blobs in cells.items():
        row_al = np.zeros(al.shape, bool)
        for _, m in blobs:
            row_al |= m
        cols = bands(row_al, 0, 4)
        for cx, m in blobs:
            c = int(np.searchsorted(cols, cx, side="right") - 1)
            key = (SHEET_ROWS[r], c)
            out.setdefault(key, np.zeros(al.shape, bool))
            out[key] |= m
    res = {}
    for key, m in out.items():
        a = px.copy()
        a[..., 3] = np.where(m, a[..., 3], 0)
        im = Image.fromarray(a, "RGBA")
        res[key] = im.crop(im.getbbox())
    missing = [(rn, c) for rn in SHEET_ROWS for c in range(4) if (rn, c) not in res]
    if missing:
        raise SystemExit(f"missing cells {missing}")
    return res


def pivot(name, im):
    """Pivot (x, y from the top) of a frame: dart head for the projectiles, centre for round art, 70 % down for the impact spray."""
    if name in ("proj", "shard"):
        a = np.asarray(im)[..., 3] > 128
        ys, xs = np.nonzero(a)
        head = xs > xs.max() - 0.3 * (xs.max() - xs.min())  # the crystal head: the right 30 %
        return float(xs[head].mean()), float(ys[head].mean())
    if name == "impact":
        return im.width / 2.0, im.height * 0.7
    return im.width / 2.0, im.height / 2.0


def build():
    out = {}
    for (name, n), im in frames().items():
        s = ROW_SCALE[name]
        big = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
        px, py = pivot(name, big)
        out[(name, n)] = (big, (px, py))
    return out


def cut():
    os.makedirs(OUT, exist_ok=True)
    meta = {"frames": []}
    for (name, n), (img, (px, py)) in sorted(build().items()):
        fn = f"{name}_{n}"
        img.save(f"{OUT}/{fn}.png")
        like = f"{LIKE}/{'cast' if name not in ('cast', 'proj', 'impact') else name}_{n}.png.meta"
        clone_meta(like, f"{OUT}/{fn}.png.meta")
        meta["frames"].append({"name": fn, "w": img.width, "h": img.height, "px": round(px, 1), "py": round(img.height - py, 1)})
        print(f"{fn:9s} {img.width:4d} x {img.height:<4d} pivot ({px:.1f}, {py:.1f})  {img.width * UNITS_PER_PX:.1f} units wide")
    with open(f"{DATA}/vfx-attack-v3.json", "w", newline="\n") as f:
        json.dump(meta, f, indent=1)
    clone_meta(f"{DATA}/vfx-attack-v2.json.meta", f"{DATA}/vfx-attack-v3.json.meta")
    clone_meta(LIKE + ".meta", OUT + ".meta")
    print("wrote", OUT, "and", f"{DATA}/vfx-attack-v3.json")


def preview(out):
    """Every frame at game size (8 px per unit, the hero ~160 px tall) on snow and on dark, with the idle hero for scale."""
    sys.path.insert(0, "Tools")
    import wizard_attack_compose as wc
    ppu = 8.0
    fr = build()
    hero = wc.compose("idle", "right", 0)
    hero = hero.resize((round(hero.width * ppu / 8), round(hero.height * ppu / 8)), Image.NEAREST)
    slot, rowh = int(26 * ppu), int(30 * ppu)
    W = slot * 5
    panels = []
    for bg in ((38, 44, 58, 255), (226, 234, 242, 255)):
        cv = Image.new("RGBA", (W, rowh * len(SHEET_ROWS)), bg)
        d = ImageDraw.Draw(cv)
        for r, name in enumerate(SHEET_ROWS):
            y = r * rowh + rowh // 2
            cv.alpha_composite(hero, (4, int(y - hero.height * 0.62)))
            d.text((4, r * rowh + 4), name, fill=(255, 255, 0, 255) if bg[0] < 128 else (120, 0, 120, 255))
            k = 1.35 if name == "bolt" else 1.0
            for n in range(4):
                img, (px, py) = fr[(name, n)]
                s = ppu * UNITS_PER_PX * k
                im = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
                cv.alpha_composite(im, (int(slot * (n + 1) + slot / 2 - px * s), int(y - py * s)))
        panels.append(cv)
    sheet = Image.new("RGBA", (W * 2, rowh * len(SHEET_ROWS)))
    for i, p in enumerate(panels):
        sheet.paste(p, (i * W, 0))
    sheet.convert("RGB").save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cut":
        cut()
    elif cmd == "preview":
        preview(sys.argv[2])
    else:
        print(__doc__)

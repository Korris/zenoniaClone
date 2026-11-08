"""Cleric basic-attack VFX v2, cut from the ChatGPT sheet drawn after the board docs/reference/cleric-basic-attack-board.png (2026-11-08).
Source: Assets/ArtLibrary/classes/cleric/vfx/attack-v2/cleric-attack-fx-v2-green-chatgpt-1.png (jade green + white, the class colour), 6 rows x 4 frames on flat #FF00FF
(chat Tay https://chatgpt.com/c/6af01e01-4b3c-83ec-8981-2622e33cf5e3). Rows of the sheet -> rows of VfxSprites set "cleric-v2":
  0 staff cast flash -> cast_N    (centred; played at the staff tip as the windup starts, every hit)
  1 light arrow      -> proj_N    (hits 1-2, pivot = the head, drawn flying right)
  2 halo lance       -> lance_N   (hit 3, BoltDef sprite "haloLance")
  3 holy orb         -> bolt_N    (hit 4 projectile, played at VfxAnim.FinisherScale)
  4 judgment burst   -> blast_N   (hit 4 burst, centred, sized by VfxAnim.Blast to the r26 blast)
  5 impact           -> impact_N  (pivot = bottom centre: the cleric impact stands on the ground, ComboProjectile.PaintedHitFx)
Output: Assets/Resources/Sprites/classes/cleric/fx/attack-v2/<row>_N.png (+ .meta cloned from fx/attack) and
Assets/Resources/Data/classes/cleric/vfx-attack-v2.json. One output px = VfxSprites.UnitsPerPx (0.058) units. Timing is the code's
(VfxAnim fps, ComboConfigs.ClericHits windup / active), nothing here.
  python Tools/build-cleric-attack-fx.py cut       write frames, json, metas
  python Tools/build-cleric-attack-fx.py preview   OUT.png: every row at game size on dark and snow-light, beside the hero and the old set
"""
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

os.chdir("D:/zenoniaClone")
sys.path.insert(0, "Tools")
from wizard_attack_fx_common import to_image, spans, resize_rgba, trim, clone_meta  # noqa: E402

spec = importlib.util.spec_from_file_location("mfx", "Tools/build-mechanic-attack-fx-v2.py")
mfx = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mfx)  # key_magenta, head_pivot, draw_fx

SRC = "Assets/ArtLibrary/classes/cleric/vfx/attack-v2/cleric-attack-fx-v2-green-chatgpt-1.png"  # gold first version: *-gold-rejected-* (class colour fixed 08/11: Cleric green + white)
OUT = "Assets/Resources/Sprites/classes/cleric/fx/attack-v2"
OLD = "Assets/Resources/Sprites/classes/cleric/fx/attack"
DATA = "Assets/Resources/Data/classes/cleric"
SHEET_ROWS = ["cast", "proj", "lance", "bolt", "blast", "impact"]
# source px -> output px; game lengths (units): flash ~9 (1.5x = 16 covered her face and chest for 0.15 s in game), arrow ~20, lance ~24, orb ~15 (x1.35 in code), impact ~20, blast scaled by code
ROW_SCALE = {"cast": 0.8, "proj": 1.9, "lance": 1.5, "bolt": 1.4, "blast": 1.0, "impact": 1.6}
UNITS_PER_PX = 0.058


# row bands of the 1536 x 1024 sheet (sparkles bridge the gutters, so rows are cut at fixed rows read on the image, not by gutters)
BANDS = [0, 195, 341, 497, 643, 835, 1024]


def cells(pm, cols=4):
    """Boxes (y0, y1, x0, x1) row by row: ink spans of each band assigned to the equal column their centre falls in."""
    ink = pm[..., 3] > 128
    w = ink.shape[1]
    out = []
    for y0, y1 in zip(BANDS[:-1], BANDS[1:]):
        boxes = {}
        for x0, x1 in spans(ink[y0:y1].sum(0)):
            c = min(cols - 1, int(((x0 + x1) / 2) / (w / cols)))
            b = boxes.get(c)
            boxes[c] = (min(b[0], x0), max(b[1], x1)) if b else (x0, x1)
        if len(boxes) != cols:
            raise SystemExit("band %d-%d: %d frames, want %d" % (y0, y1, len(boxes), cols))
        for c in range(cols):
            x0, x1 = boxes[c]
            ys = np.where(ink[y0:y1, x0:x1].any(1))[0]
            out.append((y0 + ys[0], y0 + ys[-1] + 1, x0, x1))
    return out


def build():
    """{(row, n): (PIL RGBA, (px, py from top))}"""
    pm = mfx.key_magenta(SRC)
    # green + white art: thin light lines keep a lilac cast from the #FF00FF key (seen in the burst rays); no pixel should be magenta-ish
    rgb = pm[..., :3]
    lilac = np.minimum(rgb[..., 0], rgb[..., 2]) > rgb[..., 1]
    for c in (0, 2):
        rgb[..., c] = np.where(lilac, np.minimum(rgb[..., c], rgb[..., 1]), rgb[..., c])
    boxes = cells(pm)
    out = {}
    for i, (y0, y1, x0, x1) in enumerate(boxes):
        r, n = divmod(i, 4)
        row = SHEET_ROWS[r]
        img, _ = trim(resize_rgba(to_image(pm[y0:y1, x0:x1]), ROW_SCALE[row]), pad=2)
        if row in ("proj", "lance", "bolt"):
            piv = mfx.head_pivot(img)
        elif row == "impact":
            piv = (img.width / 2.0, img.height - 3.0)
        else:
            piv = (img.width / 2.0, img.height / 2.0)
        out[(row, n)] = (img, piv)
    return out


def cut():
    os.makedirs(OUT, exist_ok=True)
    meta = {"frames": []}
    for (row, n), (img, (px, py)) in sorted(build().items()):
        name = "%s_%d" % (row, n)
        img.save(os.path.join(OUT, name + ".png"))
        clone_meta(os.path.join(OLD, "cast_0.png.meta"), os.path.join(OUT, name + ".png.meta"))
        meta["frames"].append({"name": name, "w": img.width, "h": img.height, "px": round(px, 1), "py": round(img.height - py, 1)})
        print("%-9s %4d x %-4d  %.1f x %.1f units" % (name, img.width, img.height, img.width * UNITS_PER_PX, img.height * UNITS_PER_PX))
    with open(os.path.join(DATA, "vfx-attack-v2.json"), "w", newline="\n") as f:
        json.dump(meta, f, indent=1)
    clone_meta(os.path.join(DATA, "vfx-attack.json.meta"), os.path.join(DATA, "vfx-attack-v2.json.meta"))
    clone_meta(OLD + ".meta", OUT + ".meta")
    print("wrote", OUT, "and", os.path.join(DATA, "vfx-attack-v2.json"))


def preview(path):
    new = build()
    hero = Image.open("Assets/Resources/Sprites/classes/cleric/body/armed/attack1.png").convert("RGBA").crop((2 * 192, 3 * 192, 3 * 192, 4 * 192))
    old = json.load(open(os.path.join(DATA, "vfx-attack.json")))
    oldf = {f["name"]: (Image.open(os.path.join(OLD, f["name"] + ".png")).convert("RGBA"), (f["px"], f["h"] - f["py"])) for f in old["frames"]}
    ppu = mfx.PPU
    slot, rowh = int(26 * ppu), int(34 * ppu)
    W = slot * 9
    panels = []
    for bg in [(38, 44, 58, 255), (226, 234, 242, 255)]:
        c = Image.new("RGBA", (W, rowh * 7), bg)
        d = ImageDraw.Draw(c)
        ink = (255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255)
        for r, row in enumerate(SHEET_ROWS):
            y = r * rowh + rowh // 2
            c.alpha_composite(hero, (0, int(y - 150)))
            sc = 2.4 if row == "blast" else 1.35 if row == "bolt" else 1.0
            d.text((4, r * rowh + 4), f"{row} x{sc}", fill=ink)
            for n in range(4):
                img, pv = new[(row, n)]
                mfx.draw_fx(c, img, pv, (slot * (1.6 + n * (1.9 if row == "blast" else 1.4)), y), sc)
        y = 6 * rowh + rowh // 2
        d.text((4, 6 * rowh + 4), "old cleric set: cast_2, proj_0, impact_2", fill=ink)
        for i, nm in enumerate(["cast_2", "proj_0", "impact_2"]):
            img, pv = oldf[nm]
            mfx.draw_fx(c, img, pv, (slot * (1.6 + i * 2), y))
        panels.append(c)
    out = Image.new("RGBA", (W * 2, rowh * 7))
    for i, p in enumerate(panels):
        out.paste(p, (i * W, 0))
    out.convert("RGB").save(path)
    print("wrote", path, out.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cut":
        cut()
    elif cmd == "preview":
        preview(sys.argv[2])
    else:
        print(__doc__)

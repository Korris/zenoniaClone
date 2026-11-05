"""Wizard basic-attack VFX v2 (spinning arcane rune disc) from the ChatGPT sheet on flat #00FF00.
Source: Assets/ArtLibrary/classes/wizard/vfx/attack-v2/wizard-attack-v2-sheet-1.png (3 rows x 4 cols: cast, proj, impact;
chat https://chatgpt.com/c/6aec7342-4068-83ec-8844-ac7846da159a). Output read by VfxSprites ("wizard-v2"):
  Assets/Resources/Sprites/classes/wizard/fx/attack-v2/{cast,proj,impact}_N.png  + .meta cloned from fx/attack/
  Assets/Resources/Data/classes/wizard/vfx-attack-v2.json (w, h, pivot px from the bottom-left)
One source px = VfxSprites.UnitsPerPx (0.058) world units, so ROW_SCALE sets the on-screen size of each row.
Projectile: the 4 painted frames spin unevenly, so all 4 are built from frame 0 with the ring + interior rotated 15 deg per frame.
Usage (project root):  python Tools/build-wizard-attack-fx.py cut      # write frames, json, metas
                       python Tools/build-wizard-attack-fx.py preview  # Tools/out/wizard-attack-fx-preview.png (dark + light, next to the current set and Ice Storm)
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wizard_attack_fx_common import key_green, to_image, cells, fill_enclosed, outline, spin_frames, resize_rgba, trim, clone_meta

SRC = "Assets/ArtLibrary/classes/wizard/vfx/attack-v2/wizard-attack-v2-sheet-1.png"
OUT = "Assets/Resources/Sprites/classes/wizard/fx/attack-v2"
OLD = "Assets/Resources/Sprites/classes/wizard/fx/attack"
DATA = "Assets/Resources/Data/classes/wizard"
ROWS = ["cast", "proj", "impact"]
ROW_SCALE = {"cast": 0.9, "proj": 0.95, "impact": 1.05}  # source px -> output px; 0.058 units per output px in game
SOLID = {(0, 2), (1, 0), (1, 1), (1, 2), (1, 3), (2, 0)}  # cells whose disc interior must be opaque (translucent over green in the art)
OUTLINE = {(0, 0): 4, (0, 1): 4, (0, 3): 4, (2, 2): 4, (2, 3): 4}  # thin white-line frames get a violet rim (px) to stay visible on snow
SPIN_STEP = 15.0  # degrees per projectile frame (16 fps loop: 240 deg/s; 4 frames = 60 deg = one hexagram symmetry)
UNITS_PER_PX = 0.058  # VfxSprites.UnitsPerPx


def build():
    """Keyed, filled, scaled frames: {(row, col): (PIL RGBA, pivot (px, py from top))}."""
    pm = key_green(SRC)
    boxes = cells(pm)
    frames = {}
    for i, (y0, y1, x0, x1) in enumerate(boxes):
        r, c = divmod(i, 4)
        cell = pm[y0:y1, x0:x1]
        if (r, c) in SOLID: cell = fill_enclosed(cell)
        if (r, c) in OUTLINE: cell = outline(cell, OUTLINE[(r, c)])
        frames[(r, c)] = cell
    out = {}
    # projectile: spin frame 0, pivot = disc centre
    spun, (cx, cy, R) = spin_frames(frames[(1, 0)], 4, SPIN_STEP)
    print("proj disc centre (%.1f, %.1f) ring radius %.1f px -> %.1f units" % (cx, cy, R, 2 * R * ROW_SCALE["proj"] * UNITS_PER_PX))
    for c, img in enumerate(spun):
        s = ROW_SCALE["proj"]
        img, (ox, oy) = trim(resize_rgba(img, s))
        out[(1, c)] = (img, (cx * s - ox, cy * s - oy))
    for r in (0, 2):
        for c in range(4):
            s = ROW_SCALE[ROWS[r]]
            img, _ = trim(resize_rgba(to_image(frames[(r, c)]), s))
            out[(r, c)] = (img, (img.width / 2.0, img.height / 2.0))  # round flashes: centre of the ink box
    return out


def cut():
    os.makedirs(OUT, exist_ok=True)
    meta = {"frames": []}
    for (r, c), (img, (px, py)) in sorted(build().items()):
        name = "%s_%d" % (ROWS[r], c)
        img.save(os.path.join(OUT, name + ".png"))
        clone_meta(os.path.join(OLD, name + ".png.meta"), os.path.join(OUT, name + ".png.meta"))
        meta["frames"].append({"name": name, "w": img.width, "h": img.height, "px": round(px, 1), "py": round(img.height - py, 1)})
        print("%-9s %4d x %-4d pivot (%.1f, %.1f)  %.1f units wide" % (name, img.width, img.height, px, py, img.width * UNITS_PER_PX))
    with open(os.path.join(DATA, "vfx-attack-v2.json"), "w", newline="\n") as f: json.dump(meta, f, indent=1)
    clone_meta(os.path.join(DATA, "vfx-attack.json.meta"), os.path.join(DATA, "vfx-attack-v2.json.meta"))
    clone_meta(OLD + ".meta", OUT + ".meta")
    print("wrote", OUT, "and", os.path.join(DATA, "vfx-attack-v2.json"))


# ---- preview -----------------------------------------------------------------------------------------------------------
PX_PER_UNIT = 8.0   # 2x the 640 px-tall game window (160 units tall)
GLOW = 0.22         # FeelConfig.PaintedGlowAlpha: additive copy of every painted FX sprite


def draw_fx(canvas, img, pivot, at, ppu, glow=GLOW):
    """Normal blend plus a faint additive copy, like VfxAnim / the painted projectile."""
    s = ppu * UNITS_PER_PX
    im = resize_rgba(img, s)
    x, y = int(round(at[0] - pivot[0] * s)), int(round(at[1] - pivot[1] * s))
    canvas.alpha_composite(im, (x, y))
    a = np.asarray(im).astype(np.float64)
    region = np.asarray(canvas.crop((x, y, x + im.width, y + im.height))).astype(np.float64)
    region[..., :3] = np.clip(region[..., :3] + a[..., :3] * a[..., 3:4] / 255 * glow, 0, 255)
    canvas.paste(Image.fromarray(region.round().astype(np.uint8), "RGBA"), (x, y))


def old_set():
    meta = json.load(open(os.path.join(DATA, "vfx-attack.json")))
    return {f["name"]: (Image.open(os.path.join(OLD, f["name"] + ".png")).convert("RGBA"), (f["px"], f["h"] - f["py"])) for f in meta["frames"]}


def ice_storm_hits():
    """Ice Storm impact row (row 2 of the 192 px sheet), 34 units per cell at spear size 1 (IceStormUltimate.HitPpu), anchor 0.84 from the top."""
    sheet = Image.open("Assets/Resources/Sprites/classes/wizard/fx/combat/ice-storm.png").convert("RGBA")
    return [(sheet.crop((c * 192, 2 * 192, c * 192 + 192, 3 * 192)), (96, 192 * 0.84)) for c in range(6)]


def hero():
    """Wizard attack1 facing right, frame 2 (192 px = 24 units, feet 4 units above the cell bottom)."""
    sheet = Image.open("Assets/Resources/Sprites/classes/wizard/body/armed/attack1.png").convert("RGBA")
    return sheet.crop((2 * 192, 3 * 192, 3 * 192, 4 * 192))


def preview():
    new, old, ice = build(), old_set(), ice_storm_hits()
    ppu = PX_PER_UNIT
    slot, rowh = int(24 * ppu), int(30 * ppu)
    W = int(slot * 12 + 40 * ppu)
    panels = []
    for bg in [(38, 44, 58, 255), (226, 234, 242, 255)]:
        canvas = Image.new("RGBA", (W, rowh * 4), bg)
        d = ImageDraw.Draw(canvas)
        hs = ppu / 8.0  # hero: 8 source px per unit
        h = resize_rgba(hero(), hs)
        for row, (label, frames) in enumerate([("current fx/attack", old), ("new fx/attack-v2", {"%s_%d" % (ROWS[r], c): v for (r, c), v in new.items()})]):
            y = row * rowh + rowh // 2
            canvas.alpha_composite(h, (int(4 * ppu), int(y - h.height * 0.55)))
            d.text((6, row * rowh + 4), label, fill=(255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255))
            for i, name in enumerate(["cast_0", "cast_1", "cast_2", "cast_3", "proj_0", "proj_1", "proj_2", "proj_3", "impact_0", "impact_1", "impact_2", "impact_3"]):
                img, pivot = frames[name]
                draw_fx(canvas, img, pivot, (int(32 * ppu) + i * slot + slot // 2, y), ppu)
        y = 2 * rowh + rowh // 2
        d.text((6, 2 * rowh + 4), "new: finisher 1.35x (proj, impact)", fill=(255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255))
        for i, key in enumerate([(1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (2, 3)]):
            img, pivot = new[key]
            draw_fx(canvas, img, pivot, (int(32 * ppu) + i * int(slot * 1.4) + slot // 2, y), ppu * 1.35)
        y = 3 * rowh + rowh // 2
        d.text((6, 3 * rowh + 4), "Ice Storm impact row at game size (spear size 25)", fill=(255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255))
        for i, (img, pivot) in enumerate(ice):
            s = 34.0 / 192 * (25 * 1.1 / 34) * ppu  # HitPpu then localScale hs
            im = resize_rgba(img, s)
            x, yy = int(32 * ppu) + i * int(slot * 1.5), int(y + 6 * ppu)
            canvas.alpha_composite(im, (int(x + slot * 0.75 - pivot[0] * s), int(yy - pivot[1] * s)))
        panels.append(canvas)
    out = Image.new("RGBA", (W, rowh * 8))
    for i, p in enumerate(panels): out.paste(p, (0, i * rowh * 4))
    os.makedirs("Tools/out", exist_ok=True)
    out.convert("RGB").save("Tools/out/wizard-attack-fx-preview.png")
    print("wrote Tools/out/wizard-attack-fx-preview.png", out.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "preview"
    {"cut": cut, "preview": preview}[cmd]()

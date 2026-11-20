"""Warrior lock-target basic attack: slash trail VFX (one heavy slash on one monster), 4 frames drawn by ChatGPT (Tan Pham,
chat https://chatgpt.com/c/6affde79-fbb0-83ec-9f29-83403e41d605, 2026-11-20) as a 2 x 2 grid on flat #00FF00.
Every frame is a crescent whose convex back faces RIGHT (= the swing direction), drawn top to bottom.

Source: Assets/ArtLibrary/classes/warrior/vfx/lock-slash/warrior-lock-slash-chatgpt-1.png
Writes:  Assets/Resources/Sprites/classes/warrior/fx/attack/lock-slash.png   1 x 4 cells of CELL px. The frames keep the place they have in
         their source cell (the thin first streak sits behind the full crescent, so the slash travels forward); one crop box and one scale for
         all four, pivot = cell centre. WarriorLockSlashFx turns the strip toward the locked monster.
         docs/reference/warrior-lock-slash/preview.png   frames on dark + light ground beside the old warrior slash (sprite rules, ChatGPT art 5)

  python Tools/build-warrior-lock-slash-vfx.py [cut|preview]   (no argument = both)
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/classes/warrior/vfx/lock-slash/warrior-lock-slash-chatgpt-1.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/fx/attack/lock-slash.png")
REF_META = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/fx/warrior-cleave-vfx.png.meta")
OLD_SLASH = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/fx/warrior-slash-vfx.png")
PREVIEW = os.path.join(ROOT, "docs/reference/warrior-lock-slash/preview.png")
CELL, N, COLS, ROWS = 384, 4, 2, 2
MARGIN = 0.06  # empty border kept inside each cell (fraction of CELL): no frame touches the cell edge


def unkey_green(rgb):
    """Flat #00FF00 render -> straight-alpha RGBA. Alpha from how much green dominates; green spill pulled back to max(r, b)."""
    rgb = rgb.astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    dom = g - np.maximum(r, b)
    a = 1.0 - np.clip((dom - 40.0) / 150.0, 0, 1)
    g2 = np.minimum(g, np.maximum(r, b))
    out = np.dstack([r, g2, b, a * 255.0])
    out[a < 0.04] = 0
    return np.clip(out, 0, 255).astype(np.uint8)


def cells(rgba):
    h, w = rgba.shape[:2]
    cw, ch = w // COLS, h // ROWS
    return [rgba[(i // COLS) * ch:(i // COLS + 1) * ch, (i % COLS) * cw:(i % COLS + 1) * cw] for i in range(N)]


def cut():
    rgba = unkey_green(np.asarray(Image.open(SRC).convert("RGB")))
    parts = cells(rgba)
    for p in parts:
        p[:4], p[-4:], p[:, :4], p[:, -4:] = 0, 0, 0, 0
    boxes = []
    for p in parts:
        ys, xs = np.nonzero(p[..., 3] > 20)
        boxes.append((xs.min(), xs.max(), ys.min(), ys.max()))
    # One crop box for all frames, in source-cell coordinates: the frames keep their drawn offsets.
    x0 = min(b[0] for b in boxes); x1 = max(b[1] for b in boxes)
    y0 = min(b[2] for b in boxes); y1 = max(b[3] for b in boxes)
    k = CELL * (1 - 2 * MARGIN) / max(x1 - x0 + 1, y1 - y0 + 1)
    sheet = Image.new("RGBA", (CELL * N, CELL))
    for i, p in enumerate(parts):
        crop = Image.fromarray(p[y0:y1 + 1, x0:x1 + 1])
        im = crop.resize((max(1, round(crop.width * k)), max(1, round(crop.height * k))), Image.LANCZOS)
        sheet.alpha_composite(im, (i * CELL + (CELL - im.width) // 2, (CELL - im.height) // 2))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT)
    if not os.path.exists(OUT + ".meta") and os.path.exists(REF_META):
        import re, uuid
        meta = open(REF_META, encoding="utf-8").read()
        open(OUT + ".meta", "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta, count=1))
    # Front of the arc (rightmost opaque px) of the full crescent (frame 2), as a fraction of the cell from its centre: code places it.
    a = np.asarray(sheet)[..., 3]
    fronts = []
    for i in range(N):
        xs = np.nonzero(a[:, i * CELL:(i + 1) * CELL].max(0) > 40)[0]
        fronts.append(round((xs.max() - CELL / 2) / CELL, 3))
    print("wrote", os.path.relpath(OUT, ROOT), sheet.size, "scale", round(k, 3), "boxes", boxes, "front/cell from centre", fronts)


def preview():
    strip = Image.open(OUT)
    old = Image.open(OLD_SLASH) if os.path.exists(OLD_SLASH) else None
    W = CELL * (N + 1) + 40
    img = Image.new("RGBA", (W, CELL * 2 + 60), (0, 0, 0, 255))
    d = ImageDraw.Draw(img)
    for row, bg in enumerate([(34, 38, 52, 255), (226, 232, 214, 255)]):
        y = 30 + row * (CELL + 20)
        d.rectangle([0, y - 10, W, y + CELL + 10], fill=bg)
        for i in range(N):
            img.alpha_composite(strip.crop((i * CELL, 0, (i + 1) * CELL, CELL)), (i * CELL, y))
        if old is not None:
            cw = old.width // 4
            o = old.crop((cw, 0, 2 * cw, old.height // 3)).resize((CELL, CELL * (old.height // 3) // cw))
            img.alpha_composite(o, (N * CELL + 40, y))
    d.text((6, 4), "lock-slash f0-f3 (new, ChatGPT) | old warrior-slash-vfx row 0 f1", fill=(255, 255, 255, 255))
    os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
    img.convert("RGB").save(PREVIEW)
    print("wrote", os.path.relpath(PREVIEW, ROOT))


HERO_8DIR = os.path.join(ROOT, "docs/reference/ro-fantasy-heroes-ingame/warrior-attack-8dir.png")  # 8 rows x 4 frames, as the game draws the hero
CHECK = os.path.join(ROOT, "docs/reference/warrior-lock-slash/offline-8dir-check.png")
# Same numbers as WarriorLockSlashFx.cs (keep in sync): cell units, front of the full crescent, lift, front past the target, min ahead.
CELL_UNITS, FRONT_OF_CELL, CHEST, FRONT_PAST, MIN_AHEAD, FIGURE = 20.0, 0.41, 10.0, 3.0, 9.0, 1.15
ROW_ANGLE = [-90, -45, 0, 45, 90, 135, 180, -135]  # rows of the 8-dir sheet: down, down-right, right, up-right, up, up-left, left, down-left
PX_PER_UNIT = 200 / 20.5 / FIGURE  # hero body ~200 px of the check cell = 20.5 units x figure scale (soft-sheet bodyPx / 20.5)


def check(target_dist=13.0):  # chest to the body edge where AttackChase stops (16 - 3)
    """Hero attack frames f1, f2 of each of the 8 directions with the slash frame 1 / 2 composited the way WarriorLockSlashFx places it
    (target `target_dist` units away along the facing; mirrored on the left half; behind the hero when slashing up the screen)."""
    import math
    hero = Image.open(HERO_8DIR).convert("RGBA")
    strip = Image.open(OUT)
    cw, ch = hero.width // 4, hero.height // 8
    P = 150  # padding around each hero cell so slashes up / sideways stay in view
    out = Image.new("RGBA", ((cw + 2 * P) * 2, (ch + 2 * P) * 8), (200, 210, 190, 255))
    k = FIGURE
    size_px = round(CELL_UNITS * k * PX_PER_UNIT)
    for row, ang in enumerate(ROW_ANGLE):
        rad = math.radians(ang)
        d = (math.cos(rad), math.sin(rad))
        ahead = max(MIN_AHEAD * k, min(target_dist + FRONT_PAST, 30.0) - FRONT_OF_CELL * CELL_UNITS * k)
        for col, f in enumerate((1, 2)):
            cell = Image.new("RGBA", (cw + 2 * P, ch + 2 * P))
            cell.alpha_composite(hero.crop((f * cw, row * ch, (f + 1) * cw, (row + 1) * ch)), (P, P))
            feet = (P + cw / 2, P + ch - 12)
            cx = feet[0] + d[0] * ahead * PX_PER_UNIT
            cy = feet[1] - (CHEST * k + d[1] * ahead) * PX_PER_UNIT
            fr = strip.crop((f * CELL, 0, (f + 1) * CELL, CELL)).resize((size_px, size_px), Image.LANCZOS)
            if math.cos(rad) < -0.01: fr = fr.transpose(Image.FLIP_TOP_BOTTOM)
            fr = fr.rotate(ang, resample=Image.BICUBIC, expand=True)
            layer = Image.new("RGBA", cell.size)
            layer.alpha_composite(fr, (round(cx - fr.width / 2), round(cy - fr.height / 2)))
            base = Image.new("RGBA", cell.size, (200, 210, 190, 255))
            if d[1] > 0.35:  # behind the hero
                base.alpha_composite(layer); base.alpha_composite(cell)
            else:
                base.alpha_composite(cell); base.alpha_composite(layer)
            ImageDraw.Draw(base).rectangle([0, 0, base.width - 1, base.height - 1], outline=(150, 60, 60, 255))
            out.alpha_composite(base, (col * (cw + 2 * P), row * (ch + 2 * P)))
    os.makedirs(os.path.dirname(CHECK), exist_ok=True)
    out.convert("RGB").save(CHECK)
    print("wrote", os.path.relpath(CHECK, ROOT))


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    if cmd in ("cut", "all"): cut()
    if cmd in ("preview", "all"): preview()
    if cmd in ("check", "all"): check()

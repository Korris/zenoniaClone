"""RO Soft Fantasy 8-direction pilot, default outfit only (2026-11-19): cut the ChatGPT turnaround sheets of each class
and compose a crowd mock around the Wanted Boss.

Sources (ArtLibrary/classes/<class>/ro-soft-8dir/src/, flat #00FF00, figures in one row), <c> = class:
  <c>-ro-turnaround-5dir-chatgpt-1.png        idle: down, down-right, right, up-right, up
  <c>-ro-turnaround-3dir-left-chatgpt-1.png   idle: down-left, left, up-left (drawn, not mirrored: the weapon stays in the right hand)
  <c>-ro-attack-5dir-chatgpt-1.png / <c>-ro-attack-3dir-left-chatgpt-1.png   attack pose, same orders
  OVERRIDES: single redrawn figures that replace a wrong one
Commands:
  cut [class|all]   key the green, split figures, scale each to HERO_PX (attack ATTACK_H x HERO_PX), write cut/<pose>-<dir>.png
  sheet             review sheet: class x pose rows, 8 directions -> docs/reference/ro-soft-8dir-5-classes-sheet.png
  compose           offline mock at game scale: Sunken Reliquary ground + Mire Hydra Queen + a ring of heroes (every class
                    with all 8 idle cuts) facing the boss, 8 directions vs today's 4 -> docs/reference/ro-soft-8dir-crowd-mock.png
Not the game renderer: weapons are drawn into these figures and the ground has no props; it only judges style + facing.
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
CLASSES = ["warrior", "wizard", "mechanic", "assassin", "cleric"]
RIGHT5, LEFT3 = ["down", "down-right", "right", "up-right", "up"], ["down-left", "left", "up-left"]
SHEETS = {  # file stem after "<class>-" -> (pose, directions left to right)
    "ro-turnaround-5dir-chatgpt-1": ("idle", RIGHT5),
    "ro-turnaround-3dir-left-chatgpt-1": ("idle", LEFT3),
    "ro-attack-5dir-chatgpt-1": ("attack", RIGHT5),
    "ro-attack-3dir-left-chatgpt-1": ("attack", LEFT3),
}
OVERRIDES = {  # class -> {stem: (pose, dirs)}: read after SHEETS, so it replaces the same pose-dir
    "warrior": {"ro-attack-up-left-chatgpt-2": ("attack", ["up-left"])},  # the sheet's up-left faced left with the eye visible
}
ATTACK_H = 0.84  # fallback attack/idle height (Warrior 5-direction sheets: 386-395 px vs 461-465 px)
HERO_PX = 147  # hero height on a 1604x1069 game capture (camera 160 units tall, hero ~22 units incl. hair)
GROUND = ROOT / "Assets/Resources/Sprites/village/tiles/sunken-reliquary-ground.png"  # 64 px per 16-unit tile
BOSS = ROOT / "Assets/Resources/Sprites/monsters/mireHydraQueen.png"  # 4 x 4 cells of 384, row 0 = facing down
GROUND_SCALE = 1.67  # 64 px/tile -> 107 px/tile of the capture (6.68 px per unit)
BOSS_PX = 340  # boss figure height on the capture (about 2.3 heroes, read off ingame-40-players-*.png)


def art(cls):
    return ROOT / f"Assets/ArtLibrary/classes/{cls}/ro-soft-8dir"


def key_green(img):
    """Alpha from the distance to pure #00FF00 (keeps green-ish art intact), despill the soft edge."""
    a = np.asarray(img.convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum.reduce([r, b, 255 - g]) - 40) / 80, 0, 1)
    g2 = np.where(alpha < 1, np.minimum(g, np.maximum(r, b) + 10), g)
    return Image.fromarray(np.dstack([r, g2, b, alpha * 255]).astype(np.uint8), "RGBA")


def figures(rgba, count):
    """`count` figures left to right: the biggest blobs are the bodies, every other blob >= 30 px joins the body whose
    pixels are nearest (a sword tip may touch the next figure, so blobs are never grown into each other)."""
    arr = np.asarray(rgba)
    al = arr[..., 3] > 128
    lab, n = ndimage.label(al)
    sizes = ndimage.sum(al, lab, range(1, n + 1))
    seeds = list(np.argsort(sizes)[::-1][:count] + 1)
    dist_to = [ndimage.distance_transform_edt(lab != k) for k in seeds]
    owner = np.zeros_like(lab)
    for k in range(1, n + 1):
        if sizes[k - 1] >= 30:
            m = lab == k
            owner[m] = seeds[int(np.argmin([d[m].min() for d in dist_to]))]
    boxes = []
    for k in seeds:
        ys, xs = np.nonzero(owner == k)
        boxes.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1, k))
    figs = []
    for x0, y0, x1, y1, k in sorted(boxes):
        crop = arr[y0:y1, x0:x1].copy()
        crop[..., 3] = np.where(owner[y0:y1, x0:x1] == k, crop[..., 3], 0)
        figs.append(Image.fromarray(crop, "RGBA"))
    return figs


def head_ratio(fig):
    """Widest solid run in the top 40 % over figure height (a weapon beside the head inflates it: read with the image)."""
    al = np.asarray(fig)[..., 3] > 128
    h, best = al.shape[0], 0
    for y in range(int(h * 0.05), int(h * 0.4)):
        xs = np.nonzero(al[y])[0]
        if len(xs):
            best = max(best, max(len(r) for r in np.split(xs, np.nonzero(np.diff(xs) > 3)[0] + 1)))
    return best / h


def cut_class(cls):
    cut = art(cls) / "cut"
    cut.mkdir(parents=True, exist_ok=True)
    for stem, (pose, dirs) in list(SHEETS.items()) + list(OVERRIDES.get(cls, {}).items()):
        src = art(cls) / "src" / f"{cls}-{stem}.png"
        if not src.exists():
            print("missing", src.name)
            continue
        for d, f in zip(dirs, figures(key_green(Image.open(src)), len(dirs))):
            print(f"{cls} {pose}-{d}: {f.size} head/height {head_ratio(f):.3f}")
            f.save(cut / f"{pose}-{d}-raw.png")
    # ChatGPT draws every image at its own scale, so each figure is scaled by its own height; the attack/idle height ratio
    # comes from the two 5-direction sheets (same layout, so the same drawing scale), ATTACK_H when one is missing
    def mean_h(pose):
        hs = [Image.open(cut / f"{pose}-{d}-raw.png").height for d in RIGHT5 if (cut / f"{pose}-{d}-raw.png").exists()]
        return sum(hs) / len(hs) if len(hs) == 5 else None
    ratio = mean_h("attack") / mean_h("idle") if mean_h("attack") and mean_h("idle") else ATTACK_H
    print(f"{cls} attack/idle height {ratio:.3f}")
    for p in sorted(cut.glob("*-raw.png")):
        f = Image.open(p)
        s = (HERO_PX if p.name.startswith("idle") else HERO_PX * ratio) / f.height
        f.resize((max(1, round(f.width * s)), max(1, round(f.height * s))), Image.LANCZOS).save(cut / p.name.replace("-raw", ""))


def cmd_cut(which="all"):
    for cls in CLASSES if which == "all" else [which]:
        cut_class(cls)


DIR8 = ["right", "down-right", "down", "down-left", "left", "up-left", "up", "up-right"]  # by angle, y pointing down on screen
DIR4 = ["right", "down", "left", "up"]


def facing(dx, dy, dirs):
    step = 360 / len(dirs)
    return dirs[int(((math.degrees(math.atan2(dy, dx)) % 360) + step / 2) // step) % len(dirs)]


def scene(dirs, label):
    g = Image.open(GROUND).convert("RGBA")
    cx, cy = 1952, 928  # boss tile (30.5, 9.5 from the bottom) of the 24-row map
    w, h = round(1604 / GROUND_SCALE), round(1069 / GROUND_SCALE)
    x0, y0 = min(max(cx - w // 2, 0), g.width - w), min(max(cy - h // 2, 0), g.height - h)
    bg = g.crop((x0, y0, x0 + w, y0 + h)).resize((1604, 1069), Image.LANCZOS)
    bx, by = (cx - x0) * GROUND_SCALE, (cy - y0) * GROUND_SCALE + 60
    boss = Image.open(BOSS).convert("RGBA").crop((0, 0, 384, 384))
    boss = boss.crop(boss.getbbox())
    boss = boss.resize((round(boss.width * BOSS_PX / boss.height), BOSS_PX), Image.LANCZOS)
    sprites = [(by, boss, bx - boss.width / 2, by - boss.height)]
    ready = [c for c in CLASSES if all((art(c) / "cut" / f"idle-{d}.png").exists() for d in DIR8)]
    rng = np.random.default_rng(7)
    n = 20
    for i in range(n):
        t = 2 * math.pi * i / n + rng.uniform(-0.12, 0.12)
        rx, ry = 330 + rng.uniform(-30, 50), 230 + rng.uniform(-25, 40)
        fx, fy = bx + rx * math.cos(t), by - 40 + ry * math.sin(t)
        d = facing(bx - fx, (by - 40) - fy, dirs)
        cls = ready[i % len(ready)]
        p = art(cls) / "cut" / f"{'attack' if i % 3 != 2 else 'idle'}-{d}.png"
        f = Image.open(p if p.exists() else art(cls) / "cut" / f"idle-{d}.png")
        sprites.append((fy, f, fx - f.width / 2, fy - f.height))
    shadow = Image.new("RGBA", bg.size)
    dr = ImageDraw.Draw(shadow)
    for y, f, x, _ in sprites[1:]:
        dr.ellipse((x + f.width / 2 - 26, y - 8, x + f.width / 2 + 26, y + 8), fill=(0, 0, 0, 70))
    bg = Image.alpha_composite(bg, shadow)
    for y, f, x, top in sorted(sprites, key=lambda s: s[0]):
        bg.alpha_composite(f, (round(x), round(top)))
    ImageDraw.Draw(bg).text((16, 12), label, fill=(255, 255, 255, 255))
    return bg


def cmd_compose():
    a, b = scene(DIR8, "8 directions (RO Soft pilot)"), scene(DIR4, "4 directions (today)")
    out = Image.new("RGBA", (a.width, a.height * 2 + 8), (20, 20, 20, 255))
    out.paste(a, (0, 0))
    out.paste(b, (0, a.height + 8))
    dst = ROOT / "docs/reference/ro-soft-8dir-crowd-mock.png"
    out.convert("RGB").save(dst)
    print("wrote", dst)


def cmd_sheet():
    """Review sheet: one row per class and pose, the 8 directions in DIR8 order, on a light floor colour."""
    order = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
    cell = 200
    rows = [(c, p) for c in CLASSES for p in ("idle", "attack")]
    out = Image.new("RGBA", (cell * len(order), cell * len(rows)), (222, 214, 196, 255))
    for r, (c, p) in enumerate(rows):
        for k, d in enumerate(order):
            f = art(c) / "cut" / f"{p}-{d}.png"
            if f.exists():
                im = Image.open(f)
                im.thumbnail((cell - 8, cell - 8))
                out.alpha_composite(im, (k * cell + (cell - im.width) // 2, r * cell + cell - 4 - im.height))
    dst = ROOT / "docs/reference/ro-soft-8dir-5-classes-sheet.png"
    out.convert("RGB").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    {"cut": cmd_cut, "compose": cmd_compose, "sheet": cmd_sheet}[sys.argv[1]](*sys.argv[2:])

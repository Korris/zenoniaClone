"""Idle facing down for every class from one painted line-up, so all share proportions and line weight: Assets/ArtLibrary/custom/idle-down-set/six-heroes-src.png.
Writes row 0 of each class's body/armed/idle.png, and marks idle/down `fallback` in its held-*-melee.json so the game draws that row whole: weapons are painted in and do not follow the equipment at this pose.
The Warrior is repainted alone with a long sword (warrior-long-sword-src.png). The sword passes the 192 px cell, so its frames go to body/armed/idle-down-big.png (288 x 240 cells, HeroClassDef.BigIdleDown), drawn only when the hero has no gear data. Its armed row and idle/down anchors are NOT written here: Tools/build-warrior-idle-down-doll.py owns them, so running this cannot undo the paper doll.
The Mechanic is repainted alone with one gun, an empty hand and the turret beside him (mechanic-turret-src.png; the turret is cut away for the sprite).
The Cleric is repainted alone without wings, with her shield and no spirit (cleric-no-wings-src.png, the same picture as hero-select/cleric.png): wings and pets are items for every class. The shield is painted into this row, so here it does not follow the equipment.
The robot and spirit in the painting are erased: HeroCompanion draws its own. The painted ground shadow too: HeroRenderer draws its own. Boxing has no class yet: it only goes to idle-down-set/boxing.png.
Old rows are kept in idle-down-set/old-<class>-armed-idle.png. Needs Pillow, numpy and scipy; run from the project root.
"""
import importlib.util
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SET = ROOT / "Assets/ArtLibrary/custom/idle-down-set"
CLASSES = ROOT / "Assets/Resources/Sprites/classes"
DATA = ROOT / "Assets/Resources/Data/classes"
CELL = 192
SCALE = 0.434          # a hero's 378 source px, head to sole, become the 164 px the classes stand at
SOURCE_SOLE = 592
SHADOW_FROM = 575      # source rows from here down may hold the painted ground shadow
BREATH = (0, 1, 2, 1)  # rows the figure is shorter by, per frame, soles planted
# source x range, x between the boots, sole row in the frame (the class's other rows), outline of its companion, to erase
HEROES = {
    "warrior": dict(x=(30, 370), feet=204, sole=186, gear="held-gear", alone="warrior-long-sword-src.png", alone_scale=0.1745, alone_sole=1033, alone_feet=680),
    "wizard": dict(x=(378, 720), feet=580, sole=191, gear="held-wizard"),
    "mechanic": dict(x=(730, 1085), feet=908, sole=188, gear="held-mechanic", solo="mechanic-turret-src.png", solo_scale=0.1703, solo_sole=1087, solo_feet=622, largest=True),
    "assassin": dict(x=(1092, 1420), feet=1281, sole=191, gear="held-assassin"),
    "cleric": dict(x=(1420, 1800), feet=1615, sole=191, gear="held-cleric", solo="cleric-no-wings-src.png", solo_scale=0.162, solo_sole=1137, solo_feet=685),
    "boxing": dict(x=(1815, 2135), feet=1982, sole=191),
}

spec = importlib.util.spec_from_file_location("hs", ROOT / "Tools/build-hero-select-art.py")
hs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hs)


def solo_art(cfg, with_companion=False):
    rgba = hs.key_out(SET / (cfg.get("reference", cfg["solo"]) if with_companion else cfg["solo"]), hs.GREEN)
    if cfg.get("largest") and not with_companion:  # in the game the robot beside him is drawn by HeroCompanion
        labels, count = ndimage.label(rgba[..., 3] > 128)  # solid pixels only: the faint key halo joins everything
        keep = 1 + int(np.argmax(ndimage.sum(np.ones(labels.shape), labels, range(1, count + 1))))
        rgba[~ndimage.binary_dilation(labels == keep, iterations=3)] = 0
    return hs.to_image(rgba)


def cutout():
    rgba = hs.key_out(SET / "six-heroes-src.png", hs.GREEN)
    below = np.zeros(rgba.shape[:2], bool)
    below[SHADOW_FROM:] = True
    r, g, b = rgba[..., 0], rgba[..., 1], rgba[..., 2]
    greenish = (g > r * 1.15) & (g > b * 1.15)
    dark = (r < 70) & (g < 90) & (b < 70)
    rgba[below & (greenish | dark) & (np.arange(rgba.shape[0])[:, None] > SOURCE_SOLE - 6)] = 0
    rgba[SOURCE_SOLE + 16:] = 0
    return rgba


def split(rgba):
    """Whole connected figures, not x ranges: an assassin blade and a cleric staff share columns."""
    labels, count = ndimage.label(rgba[..., 3] > 20)
    sizes = ndimage.sum(np.ones(labels.shape), labels, range(1, count + 1))
    mains = sorted(np.argsort(sizes)[-len(HEROES):] + 1, key=lambda i: ndimage.center_of_mass(labels == i)[1])
    return labels, dict(zip(HEROES, mains)), set(mains)


def figure(rgba, name, cfg, labels, mains, all_mains):
    keep = labels == mains[name]
    for i in range(1, labels.max() + 1):  # small sparks and flame tips in the hero's columns
        if i in all_mains:
            continue
        ys, xs = np.nonzero(labels == i)
        if len(xs) and cfg["x"][0] <= xs.mean() < cfg["x"][1]:
            keep |= labels == i
    part = rgba.copy()
    part[~keep] = 0
    if "companion" in cfg:  # traced by hand along the outline: it overlaps hair and hat
        mask = Image.new("L", (part.shape[1], part.shape[0]), 0)
        ImageDraw.Draw(mask).polygon(cfg["companion"], fill=255)
        part[np.asarray(mask) > 0] = 0
        rest, count = ndimage.label(part[..., 3] > 20)  # crumbs of its outline
        sizes = ndimage.sum(np.ones(rest.shape), rest, range(1, count + 1))
        for i, size in enumerate(sizes):
            if size < 400:
                part[rest == i + 1] = 0
    return hs.to_image(part)


BIG_W, BIG_H, BIG_BELOW = 288, 240, 48  # the 192 cell's bottom sits 48 rows above this cell's


def warrior_frames(cfg):
    src = hs.to_image(hs.key_out(SET / cfg["alone"], hs.GREEN))
    scale, sole = cfg["alone_scale"], cfg["alone_sole"]
    w = round(src.width * scale)
    x0 = round(BIG_W / 2 - cfg["alone_feet"] * scale)
    sole_row = BIG_H - BIG_BELOW - (CELL - cfg["sole"])
    out = []
    for less in BREATH:
        cell = Image.new("RGBA", (BIG_W, BIG_H), (0, 0, 0, 0))
        tall = round(sole * scale) - less
        upper = src.crop((0, 0, src.width, sole)).resize((w, tall), Image.LANCZOS)
        cell.paste(upper, (x0, sole_row - tall), upper)
        low_h = round((src.height - sole) * scale)
        lower = src.crop((0, sole, src.width, src.height)).resize((w, low_h), Image.LANCZOS)
        cell.paste(lower, (x0, sole_row), lower)
        out.append(cell)
    return out


def frames(src, cfg):
    scale, source_sole, feet = (cfg["solo_scale"], cfg["solo_sole"], cfg["solo_feet"]) if "solo" in cfg else (SCALE, SOURCE_SOLE, cfg["feet"])
    x0 = round(CELL / 2 - feet * scale)
    tall = round(source_sole * scale)
    below = round((src.height - source_sole) * scale)
    out = []
    for less in BREATH:
        cell = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        upper = src.crop((0, 0, src.width, source_sole)).resize((round(src.width * scale), tall - less), Image.LANCZOS)
        cell.alpha_composite(Image.new("RGBA", cell.size, (0, 0, 0, 0)))
        cell.paste(upper, (x0, cfg["sole"] - (tall - less)), upper)
        if below > 0:  # the sword tip under the soles, not squeezed by the breath
            lower = src.crop((0, source_sole, src.width, src.height)).resize((round(src.width * scale), below), Image.LANCZOS)
            cell.paste(lower, (x0, cfg["sole"]), lower)
        out.append(cell)
    return out


def mark_fallback(cls, gear):
    main = json.loads((DATA / cls / f"{gear}.json").read_text(encoding="utf-8"))
    path = DATA / cls / f"{gear}-melee.json"
    data = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {"anchors": []}
    sets = [a for a in main["anchors"] if a["anim"] == "idle" and a["dir"] == "down"]
    for a in sets:
        found = next((e for e in data["anchors"] if e["part"] == a["part"] and e["anim"] == "idle" and e["dir"] == "down"), None)
        if found is None:
            found = dict(part=a["part"], anim="idle", dir="down", frames=[dict(f) for f in a["frames"]])
            data["anchors"].append(found)
        for f in found["frames"]:
            f["fallback"] = True
    path.write_text(json.dumps(data, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")


def main():
    rgba = cutout()
    labels, mains, all_mains = split(rgba)
    write_reference(rgba, labels, mains, all_mains)
    only = sys.argv[1:]  # class names; none = all
    for name, cfg in HEROES.items():
        if only and name not in only:
            continue
        art = solo_art(cfg) if "solo" in cfg else figure(rgba, name, cfg, labels, mains, all_mains)
        cells = frames(art, cfg)
        if "alone" in cfg:
            big = warrior_frames(cfg)
            sheet = Image.new("RGBA", (BIG_W * len(big), BIG_H), (0, 0, 0, 0))
            for i, cell in enumerate(big):
                sheet.paste(cell, (i * BIG_W, 0))
            sheet.save(CLASSES / name / "body/armed/idle-down-big.png")
            # The 192 cell of the same pose, clipped: what the bag portrait shows.
            cells = [c.crop(((BIG_W - CELL) // 2, BIG_H - BIG_BELOW - CELL, (BIG_W + CELL) // 2, BIG_H - BIG_BELOW)) for c in big]
        if "gear" not in cfg:
            cells[0].save(SET / f"{name}.png")
            continue
        if name == "warrior":  # row 0 and its anchors belong to Tools/build-warrior-idle-down-doll.py now: sword and shield are separate there
            cells[0].save(SET / f"{name}.png")
            continue
        path = CLASSES / name / "body/armed/idle.png"
        backup = SET / f"old-{name}-armed-idle.png"
        if not backup.exists():
            shutil.copy(path, backup)
        sheet = Image.open(path).convert("RGBA")
        sheet.paste((0, 0, 0, 0), (0, 0, sheet.width, CELL))
        for i, cell in enumerate(cells):
            sheet.paste(cell, (i * CELL, 0))
        sheet.save(path)
        cells[0].save(SET / f"{name}.png")
        mark_fallback(name, cfg["gear"])


def write_reference(rgba, labels, mains, all_mains):
    """Style reference for later sprite work: the five playable classes at source resolution, soles on one line, on a flat background."""
    gap, top_pad, bottom_pad = 40, 60, 90
    pieces = []
    for name, cfg in HEROES.items():
        if "gear" not in cfg:
            continue
        if "alone" in cfg:
            art = hs.to_image(hs.key_out(SET / cfg["alone"], hs.GREEN))
            k = (SOURCE_SOLE - 216) / (cfg["alone_sole"] - 93)  # the line-up Warrior's 376 px, head to sole
            art = art.resize((round(art.width * k), round(art.height * k)), Image.LANCZOS)
            sole = round(cfg["alone_sole"] * k)
        elif "solo" in cfg:
            art = solo_art(cfg, with_companion=True)
            k = cfg["solo_scale"] / SCALE
            art = art.resize((round(art.width * k), round(art.height * k)), Image.LANCZOS)
            sole = round(cfg["solo_sole"] * k)
        else:
            art = figure(rgba, name, cfg, labels, mains, all_mains)
            sole = SOURCE_SOLE
        box = art.getbbox()
        pieces.append((art.crop(box), sole - box[1]))
    above = max(p[1] for p in pieces)
    below = max(p[0].height - p[1] for p in pieces)
    width = sum(p[0].width for p in pieces) + gap * (len(pieces) + 1)
    sheet = Image.new("RGBA", (width, top_pad + above + below + bottom_pad), (58, 66, 78, 255))
    x = gap
    for art, sole in pieces:
        sheet.alpha_composite(art, (x, top_pad + above - sole))
        x += art.width + gap
    out = ROOT / "docs/reference/hero-idle-down-5-classes.png"
    sheet.convert("RGB").save(out)


if __name__ == "__main__":
    main()

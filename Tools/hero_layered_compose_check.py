"""Offline check of the layered hero exactly as the game layers it (HeroLayeredView.cs + HeroRecolor.shader ramp in hero_recolor_core):
body grey sheet + mask recoloured with the outfit colour, hair cell recoloured with _HairAll, scaled by w / refW around its anchor and put
on the frame's head anchor, drawn over the body; left dirs = mirrored right rows (anchor x mirrored). Writes
docs/reference/hero-layered-real/<class>-<anim>.png (rows = 8 Dir8 dirs, colour combo changes per row) and prints magenta leftovers.
Run: python Tools/build-hero-layered-game-assets.py check
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import hero_recolor_core as core

DIR8 = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
PAINTED_ROW = [0, 1, 2, 3, 4, 3, 2, 1]
CROP = (50, 10, 270, 250)  # figure area of a 320 x 256 cell (hair and ponytail included)
CLASSES = ["warrior", "wizard", "mechanic", "assassin", "cleric"]


def load(root, cls):
    data = root / "Assets/Resources"
    cj = json.loads((data / f"Data/hero-layered/{cls}/class.json").read_text(encoding="utf-8"))
    bj = json.loads((data / (cj["body"] + ".json")).read_text(encoding="utf-8"))
    he = cj["hair"][0]
    hair = np.asarray(Image.open(data / (he["sheet"] + ".png")).convert("RGBA"))
    heads = {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}
    return cj, bj, he, hair, heads


def place(out, strip, e, h, row, mirror, drop_w, cw, ch):
    """A 5-cell layer strip (hair or hat entry e) on head anchor h, scaled w / refW, seat at skull top + drop * w, as HeroLayeredView."""
    hw, hh = e["cellW"], e["cellH"]
    hc = Image.fromarray(strip[:, row * hw:(row + 1) * hw])
    k = h["w"] / e["refW"]
    hc = hc.resize((max(1, round(hw * k)), max(1, round(hh * k))), Image.LANCZOS)
    ax, ay = e["ax"] * k, e["ay"] * k
    if mirror:
        hc = hc.transpose(Image.FLIP_LEFT_RIGHT)
        ax = hc.width - ax
    x = cw - h["x"] if mirror else h["x"]
    layer = Image.new("RGBA", (cw + 2 * hc.width, ch + 2 * hc.height))
    layer.alpha_composite(hc, (int(round(x - ax)) + hc.width, int(round(h["y"] + drop_w - ay)) + hc.height))
    out.alpha_composite(layer.crop((hc.width, hc.height, hc.width + cw, hc.height + ch)))


def frame(root, bj, he, hair, heads, anim, d8, f, hair_hex, outfit_hex, cache, hat=None):
    if anim not in cache:
        base = root / "Assets/Resources" / bj["sheets"]
        cache[anim] = (np.asarray(Image.open(base / f"{anim}.png").convert("RGBA")), np.asarray(Image.open(base / f"{anim}-mask.png").convert("RGBA")))
    sheet, mask = cache[anim]
    cw, ch = bj["cellW"], bj["cellH"]
    row, mirror = PAINTED_ROW[d8], d8 >= 5
    sl = (slice(row * ch, (row + 1) * ch), slice(f * cw, (f + 1) * cw))
    body = Image.fromarray(core.recolour(sheet[sl], mask[sl], None, outfit_hex))
    h = heads[(anim, row, f)]
    if mirror:
        body = body.transpose(Image.FLIP_LEFT_RIGHT)
    out = Image.new("RGBA", (cw, ch))
    out.alpha_composite(body)
    if hat is None or not hat[0].get("hidesHair"):
        place(out, core.recolour(hair, None, hair_hex, None, hair_all=True), he, h, row, mirror, 0.0, cw, ch)
    if hat is not None:
        place(out, hat[1], hat[0], h, row, mirror, hat[0]["drop"] * h["w"], cw, ch)
    return out


def pink_left(sheet):
    """Pink / magenta pixels still coloured in a game sheet (looser than the outfit test: any hue 265-355 with some saturation)."""
    h, s, v = core.hsv(sheet[..., :3] / 255.0)
    return int(((sheet[..., 3] > 8) & (h >= 265) & (h <= 355) & (s > 0.25) & (v > 0.2)).sum())


def main(root):
    pal = json.loads((root / "Assets/Resources/Data/hero-layered/palettes.json").read_text(encoding="utf-8"))
    out_dir = root / "docs/reference/hero-layered-real"
    out_dir.mkdir(parents=True, exist_ok=True)
    cw, chh = CROP[2] - CROP[0], CROP[3] - CROP[1]
    for ci, cls in enumerate(CLASSES):
        cj, bj, he, hair, heads = load(root, cls)
        cache, magenta = {}, 0
        combos = [(pal["hair"][(ci + j * 3) % 8], pal["outfit"][(ci + j * 2) % 6]) for j in range(3)]
        for a in bj["anims"]:
            anim, n = a["name"], a["frames"]
            cols = n if n > 1 else 3  # idle: the 3 colour combos side by side
            img = Image.new("RGBA", (cw * cols, chh * 8), (196, 204, 188, 255))
            dr = ImageDraw.Draw(img)
            for d8 in range(8):
                for c in range(cols):
                    hx, ox = combos[c] if n == 1 else combos[d8 % 3]
                    fr = frame(root, bj, he, hair, heads, anim, d8, c if n > 1 else 0, hx, ox, cache)
                    img.alpha_composite(fr.crop(CROP), (c * cw, d8 * chh))
                dr.text((3, d8 * chh + 3), DIR8[d8], fill=(30, 30, 30, 255))
            img.convert("RGB").save(out_dir / f"{cls}-{anim}.png")
            magenta += pink_left(cache[anim][0])
        print(f"{cls}: magenta pixels left in the body sheets: {magenta}")
        hats_sheet(root, cls, ci, bj, he, hair, heads, pal, out_dir)
    print("wrote", out_dir)


def hats_sheet(root, cls, ci, bj, he, hair, heads, pal, out_dir):
    """<class>-hats.png: 4 hats (starter of the class first, a hood that hides the hair among them) x 8 dirs, idle + walk frame 1."""
    hats = json.loads((root / "Assets/Resources/Data/hero-layered/headgear.json").read_text(encoding="utf-8"))["hats"]
    starter = [h for h in hats if h["classId"] == cls]
    pick = starter + [hats[(ci * 4 + j * 5) % len(hats)] for j in range(3)]
    if not any(h.get("hidesHair") for h in pick):
        pick[-1] = hats[4 if ci % 2 else 8]  # nun veil / aviator cap
    cw, chh = CROP[2] - CROP[0], CROP[3] - CROP[1]
    img = Image.new("RGBA", (cw * 8, chh * len(pick) * 2), (196, 204, 188, 255))
    dr = ImageDraw.Draw(img)
    cache = {}
    for r, hat in enumerate(pick):
        strip = np.asarray(Image.open(root / "Assets/Resources" / (hat["sheet"] + ".png")).convert("RGBA"))
        for k, (anim, f) in enumerate((("idle", 0), ("walk", 1))):
            for d8 in range(8):
                fr = frame(root, bj, he, hair, heads, anim, d8, f, pal["hair"][(ci + r + d8) % 8], pal["outfit"][(ci + r) % 6], cache, (hat, strip))
                img.alpha_composite(fr.crop(CROP), (d8 * cw, (r * 2 + k) * chh))
            dr.text((3, (r * 2 + k) * chh + 3), hat["key"] + " " + anim, fill=(30, 30, 30, 255))
    img.convert("RGB").save(out_dir / f"{cls}-hats.png")

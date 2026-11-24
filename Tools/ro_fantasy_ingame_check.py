"""Offline check of the RO Fantasy heroes composed exactly like the game (HeroLayeredView order, HeroRecolor ramp of hero_recolor_core):
weapon / offhand behind, body (fixed colours), hair dyed with _HairAll, Mechanic goggles (bare head only), hat, weapons in front, hand layer,
shield over the hand. Per class one sheet per painted anim: rows = 3 hairstyles x 3 hair colours (+ 2 rows with the class starter hat /
straw hat), columns = the 8 Dir8 directions (motion anims: frames of the down-right and left directions).
Writes docs/reference/ro-fantasy-heroes-ingame/<class>-<anim>.png. Run: python Tools/build-ro-fantasy-hero-game-assets.py check
"""
import json

import numpy as np
from PIL import Image, ImageDraw

import hero_layered_weapons_compose as wf
import ro_fantasy_body_cut as bc

DATA = bc.ROOT / "Assets/Resources"
OUT = bc.variant.review_dir(bc.ROOT / "docs/reference/ro-fantasy-heroes-ingame")
CROP = (0, 0, 320, 256)  # the whole cell: the sword tip pointing down-outward reaches x ~15
HAIR_PICKS = [0, 3, 6]  # soft chestnut, silver, lavender
DIR8 = wf.DIR8


def load(path):
    return json.loads((DATA / path).read_text(encoding="utf-8"))


def ctx_of(cj, bj, style):
    he = cj["hair"][style]
    hair = np.asarray(Image.open(DATA / (he["sheet"] + ".png")).convert("RGBA"))
    acc = np.asarray(Image.open(DATA / (he["accSheet"] + ".png")).convert("RGBA")) if he.get("accSheet") else None
    heads = {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}
    grips = {(g["part"], g["anim"], g["dir"], g["f"]): g for g in cj["grips"]}
    weps = {w["part"]: (Image.open(DATA / (w["sheet"] + ".png")).convert("RGBA"), w["gx"], w["gy"], w.get("overHand", False)) for w in cj["weapons"]}
    return (bj, he, hair, heads, grips, weps, {}, acc)


def hat(key):
    hats = load("Data/hero-layered/headgear.json")["hats"]
    h = next(x for x in hats if x["key"] == key or x.get("classId") == key)
    return h, np.asarray(Image.open(DATA / (h["sheet"] + ".png")).convert("RGBA"))


def main(classes):
    OUT.mkdir(parents=True, exist_ok=True)
    pal = load("Data/hero-layered/palettes.json")
    cw, ch = CROP[2] - CROP[0], CROP[3] - CROP[1]
    for cls in classes:
        cj = load(f"Data/hero-layered/ro-fantasy/{cls}/class.json")
        bj = load(cj["body"] + ".json")
        ctxs = [ctx_of(cj, bj, s) for s in range(len(cj["hair"]))]
        hats = [hat(cls), hat("straw-hat")]
        for a in bj["anims"]:
            anim, n = a["name"], a["frames"]
            cols = [(d, 0) for d in range(8)] if n == 1 else [(d, f) for d in (1, 6) for f in range(n)]
            rows = [(s, HAIR_PICKS[(s + j) % 3], None) for s in range(len(ctxs)) for j in range(3)] + [(0, 0, hats[0]), (1, 4, hats[1])]
            img = Image.new("RGBA", (cw * len(cols), ch * len(rows)), (196, 204, 188, 255))
            dr = ImageDraw.Draw(img)
            for r, (s, hi, hh) in enumerate(rows):
                for c, (d8, f) in enumerate(cols):
                    fr = wf.frame(bc.ROOT, ctxs[s], anim, d8, f, pal["hair"][hi], None, hh)
                    img.alpha_composite(fr.crop(CROP), (c * cw, r * ch))
                    if r == 0:
                        dr.text((c * cw + 3, 3), f"{DIR8[d8]} f{f}", fill=(30, 30, 30, 255))
                dr.text((3, r * ch + 16), f"style {s} {pal['hairNames'][hi]}{' + ' + hh[0]['key'] if hh else ''}", fill=(30, 30, 30, 255))
            dst = OUT / f"{cls}-{anim}.png"
            img.convert("RGB").save(dst)
            print("wrote", dst)
            if n > 1:
                every_dir(cls, anim, n, ctxs, pal)


def every_dir(cls, anim, n, ctxs, pal):
    """Motion anims: every frame x the 8 directions (rows), hairstyle k on row k % styles; marks the feet line (244)."""
    cw, ch = CROP[2] - CROP[0], CROP[3] - CROP[1]
    img = Image.new("RGBA", (cw * n, ch * 8), (196, 204, 188, 255))
    dr = ImageDraw.Draw(img)
    for d8 in range(8):
        s = d8 % len(ctxs)
        for f in range(n):
            img.alpha_composite(wf.frame(bc.ROOT, ctxs[s], anim, d8, f, pal["hair"][HAIR_PICKS[s % len(HAIR_PICKS)]], None, None).crop(CROP), (f * cw, d8 * ch))
            dr.text((f * cw + 3, d8 * ch + 3), f"{DIR8[d8]} f{f}", fill=(30, 30, 30, 255))
        dr.line((0, d8 * ch + bc.FEET - CROP[1], cw * n, d8 * ch + bc.FEET - CROP[1]), fill=(200, 60, 60, 255))
    dst = OUT / f"{cls}-{anim}-8dir.png"
    img.convert("RGB").save(dst)
    print("wrote", dst)

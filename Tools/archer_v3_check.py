"""Archer V3 offline review sheets, composed in the game's layer order (HeroLayeredView.Show):
bow ("weapon") behind, quiver ("offhand") behind, body, hair (dyed like the HeroRecolor shader), bow front, quiver front, hand layer (always).
  archer-idle.png            rows = 2 hairstyles x 3 dyes, columns = the 8 Dir8 directions
  archer-<anim>-8dir.png     motion anims: every frame (columns) x the 8 directions (rows), feet line in red
Output: docs/reference/character-v3/archer-v3/check/.  Reads the game-ready folder (not Assets/Resources), palettes from Resources.
"""
import json

import numpy as np
from PIL import Image, ImageDraw

import hero_recolor_core as core
import hero_layered_compose_check as cc
import hero_layered_grip_poses as gp

HAIR_PICKS = [0, 3, 6]
DIR8 = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]


def ctx(game, style):
    cj = json.loads((game / "Data/hero-layered/ro-fantasy/archer/class.json").read_text(encoding="utf-8"))
    bj = json.loads((game / (cj["body"] + ".json")).read_text(encoding="utf-8"))
    he = cj["hair"][style]
    hair = np.asarray(Image.open(game / (he["sheet"] + ".png")).convert("RGBA"))
    heads = {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}
    grips = {(g["part"], g["anim"], g["dir"], g["f"]): g for g in cj.get("grips", [])}
    weps = {w["part"]: (Image.open(game / (w["sheet"] + ".png")).convert("RGBA"), w["gx"], w["gy"]) for w in cj.get("weapons", [])}
    return {"cj": cj, "bj": bj, "he": he, "hair": hair, "heads": heads, "grips": grips, "weps": weps, "cache": {}, "game": game}


def frame(c, anim, d8, f, hair_hex, weapons=True):
    bj = c["bj"]
    if anim not in c["cache"]:
        base = c["game"] / bj["sheets"]
        c["cache"][anim] = tuple(np.asarray(Image.open(p).convert("RGBA")) for p in (base / f"{anim}.png", base / "hand" / f"{anim}.png"))
    sheet, hands = c["cache"][anim]
    cw, ch = bj["cellW"], bj["cellH"]
    all_dirs = sheet.shape[0] >= 8 * ch
    row, mirror = (d8, False) if all_dirs else (cc.PAINTED_ROW[d8], d8 >= 5)
    sl = (slice(row * ch, (row + 1) * ch), slice(f * cw, (f + 1) * cw))
    body, hand = Image.fromarray(sheet[sl]), Image.fromarray(hands[sl]) if hands.shape[0] > row * ch else None
    if mirror:
        body = body.transpose(Image.FLIP_LEFT_RIGHT)
        hand = hand.transpose(Image.FLIP_LEFT_RIGHT) if hand else None
    h = c["heads"][(anim, row, f)]
    hrow, hflip = cc.PAINTED_ROW[d8], d8 >= 5
    if all_dirs and hflip:
        h = dict(h, x=cw - h["x"])
    layers = {}
    if weapons:
        for part in ("weapon", "offhand"):
            g = c["grips"].get((part, anim, d8, f))
            if g is None or part not in c["weps"]:
                continue
            img, gx, gy = c["weps"][part]
            layers[part] = (gp.placed(img, gx, gy, g["x"], g["y"], g["angle"], g.get("flip", False)), g["behind"])
    out = Image.new("RGBA", (cw, ch))
    for part in ("weapon", "offhand"):
        if part in layers and layers[part][1]:
            out.alpha_composite(layers[part][0])
    out.alpha_composite(body)
    cc.place(out, core.recolour(c["hair"], None, hair_hex, None, hair_all=True), c["he"], h, hrow, hflip, 0.0, cw, ch)
    front = [p for p in ("weapon", "offhand") if p in layers and not layers[p][1]]
    for part in front:
        out.alpha_composite(layers[part][0])
    if hand is not None:
        out.alpha_composite(hand)
    return out


def main(bc, ga, game, docs):
    out_dir = docs / "check"
    out_dir.mkdir(parents=True, exist_ok=True)
    pal = json.loads((bc.ROOT / "Assets/Resources/Data/hero-layered/palettes.json").read_text(encoding="utf-8"))
    cj = json.loads((game / "Data/hero-layered/ro-fantasy/archer/class.json").read_text(encoding="utf-8"))
    ctxs = [ctx(game, s) for s in range(len(cj["hair"]))]
    bj = ctxs[0]["bj"]
    cw, ch = bj["cellW"], bj["cellH"]
    for a in bj["anims"]:
        anim, n = a["name"], a["frames"]
        if n == 1:
            rows = [(s, HAIR_PICKS[j]) for s in range(len(ctxs)) for j in range(3)]
            img = Image.new("RGBA", (cw * 8, ch * len(rows)), (196, 204, 188, 255))
            dr = ImageDraw.Draw(img)
            for r, (s, hi) in enumerate(rows):
                for d8 in range(8):
                    img.alpha_composite(frame(ctxs[s], anim, d8, 0, pal["hair"][hi]), (d8 * cw, r * ch))
                    if r == 0:
                        dr.text((d8 * cw + 3, 3), DIR8[d8], fill=(30, 30, 30, 255))
                dr.text((3, r * ch + 16), f"style {s} {pal['hairNames'][hi]}", fill=(30, 30, 30, 255))
            dst = out_dir / f"archer-{anim}.png"
        else:
            img = Image.new("RGBA", (cw * n, ch * 8), (196, 204, 188, 255))
            dr = ImageDraw.Draw(img)
            for d8 in range(8):
                s = d8 % len(ctxs)
                for f in range(n):
                    img.alpha_composite(frame(ctxs[s], anim, d8, f, pal["hair"][HAIR_PICKS[s]]), (f * cw, d8 * ch))
                    dr.text((f * cw + 3, d8 * ch + 3), f"{DIR8[d8]} f{f}", fill=(30, 30, 30, 255))
                dr.line((0, d8 * ch + bc.FEET, cw * n, d8 * ch + bc.FEET), fill=(200, 60, 60, 255))
            dst = out_dir / f"archer-{anim}-8dir.png"
        img.convert("RGB").save(dst)
        print("wrote", dst)

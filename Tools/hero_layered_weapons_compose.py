"""Contact sheets of the layered heroes WITH weapons, composed in the game's order (HeroLayeredView): weapon behind, offhand behind,
body (recoloured), hair (hidden by a hood), hair accessory (Mechanic goggles, only without a hat), hat, weapon front, offhand front,
hand layer, offhand front drawn over the hand (shields). Rows = 8 Dir8 dirs, columns = frames (idle: bare head / class starter hat /
straw hat). Writes docs/reference/hero-layered-weapons/<class>-<anim>.png and prints the rule check (front weapon over the face, blade
pointing inwards at rest). Run: python Tools/build-hero-layered-grips.py check
"""
import json

import numpy as np
from PIL import Image, ImageDraw

import hero_recolor_core as core
import hero_layered_compose_check as cc
import hero_layered_grip_poses as gp

DIR8 = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
CROP = (0, 0, 320, 256)


def frame(root, ctx, anim, d8, f, hair_hex, outfit_hex, hat=None):
    bj, he, hair, heads, grips, weps, cache, acc = ctx
    if anim not in cache:
        base = root / "Assets/Resources" / bj["sheets"]
        hand = base / "hand" / f"{anim}.png"
        cache[anim] = tuple(np.asarray(Image.open(p).convert("RGBA")) for p in (base / f"{anim}.png", base / f"{anim}-mask.png", hand))
    sheet, mask, hands = cache[anim]
    cw, ch = bj["cellW"], bj["cellH"]
    all_dirs = sheet.shape[0] >= 8 * ch  # anim painted for all 8 dirs (HeroLayeredSet.AllDirs): row = d8, never mirrored
    row, mirror = (d8, False) if all_dirs else (cc.PAINTED_ROW[d8], d8 >= 5)
    sl = (slice(row * ch, (row + 1) * ch), slice(f * cw, (f + 1) * cw))
    body = Image.fromarray(core.recolour(sheet[sl], mask[sl], None, outfit_hex))
    hand = Image.fromarray(hands[sl])
    if mirror:
        body, hand = body.transpose(Image.FLIP_LEFT_RIGHT), hand.transpose(Image.FLIP_LEFT_RIGHT)
    h = heads[(anim, row, f)]
    hrow, hflip = cc.PAINTED_ROW[d8], d8 >= 5  # hair / hat strips: 5 cells, left dirs mirrored, seated on this row's anchor
    if all_dirs and hflip:
        h = dict(h, x=cw - h["x"])  # cc.place mirrors the anchor back: lands on the painted (unmirrored) skull
    layers = {}
    for part in ("weapon", "offhand"):
        g = grips.get((part, anim, d8, f))
        if g is None:
            continue
        img, gx, gy, over = weps[part]
        layers[part] = (gp.placed(img, gx, gy, g["x"], g["y"], g["angle"], g.get("flip", False)), g["behind"], over)
    out = Image.new("RGBA", (cw, ch))
    for part in ("weapon", "offhand"):
        if part in layers and layers[part][1]:
            out.alpha_composite(layers[part][0])
    out.alpha_composite(body)
    if hat is None or not hat[0].get("hidesHair"):
        cc.place(out, core.recolour(hair, None, hair_hex, None, hair_all=True), he, h, hrow, hflip, 0.0, cw, ch)
    if acc is not None and hat is None:
        cc.place(out, acc, he, h, hrow, hflip, 0.0, cw, ch)
    if hat is not None:
        cc.place(out, hat[1], hat[0], h, hrow, hflip, hat[0]["drop"] * h["w"], cw, ch)
    for part in ("weapon", "offhand"):
        if part in layers and not layers[part][1] and not layers[part][2]:
            out.alpha_composite(layers[part][0])
    out.alpha_composite(hand)
    for part in ("weapon", "offhand"):
        if part in layers and not layers[part][1] and layers[part][2]:
            out.alpha_composite(layers[part][0])
    return out


def context(root, cls):
    cj, bj, he, hair, heads = cc.load(root, cls)
    grips = {(g["part"], g["anim"], g["dir"], g["f"]): g for g in cj["grips"]}
    weps = {}
    for w in cj["weapons"]:
        img = Image.open(root / "Assets/Resources" / (w["sheet"] + ".png")).convert("RGBA")
        weps[w["part"]] = (img, w["gx"], w["gy"], w.get("overHand", False))
    acc = None
    if he.get("accSheet"):
        acc = np.asarray(Image.open(root / "Assets/Resources" / (he["accSheet"] + ".png")).convert("RGBA"))
    return cj, (bj, he, hair, heads, grips, weps, {}, acc)


def rule_check(root, cls, cj, bj, heads):
    """Front weapons still over the head zone; rest-pose blades pointing inwards (tip on the head side of the fist)."""
    face, inward = [], []
    cells = {}
    base = root / "Assets/Resources" / bj["sheets"]
    weps = {w["part"]: w for w in cj["weapons"]}
    kinds = dict(zip(("weapon", "offhand"), (k for _, k in gp.WEAPONS[cls])))
    imgs = {p: (Image.open(root / "Assets/Resources" / (w["sheet"] + ".png")).convert("RGBA"), w["gx"], w["gy"]) for p, w in weps.items()}
    for g in cj["grips"]:
        anim, d8, f = g["anim"], g["dir"], g["f"]
        row, mirror = cc.PAINTED_ROW[d8], d8 >= 5
        if anim not in cells:
            cells[anim] = Image.open(base / f"{anim}.png").convert("RGBA")
        cell = cells[anim].crop((f * 320, row * 256, f * 320 + 320, row * 256 + 256))
        if not g["behind"]:
            img, gx, gy = imgs[g["part"]]
            n = gp.overlap(img, gx, gy, g["x"], g["y"], g["angle"], g["flip"], gp.face_zone(cell, mirror))
            if n > gp.FACE_PX:
                face.append(f"{anim} {DIR8[d8]} f{f} {g['part']} {n}px")
        if kinds[g["part"]] == "blade" and (anim in ("idle", "walk") or f == 3) and row not in (2,):
            hx = 320 - heads[(anim, row, f)]["x"] if mirror else heads[(anim, row, f)]["x"]
            tip_dx = np.sin(np.radians(g["angle"]))
            if (g["x"] - hx) * tip_dx < -1e-6:
                inward.append(f"{anim} {DIR8[d8]} f{f} {g['part']}")
    print(f"{cls}: front weapon over the face: {len(face)} {face[:12]}; rest blade inward: {len(inward)} {inward[:12]}")


def main(root):
    pal = json.loads((root / "Assets/Resources/Data/hero-layered/palettes.json").read_text(encoding="utf-8"))
    hats = json.loads((root / "Assets/Resources/Data/hero-layered/headgear.json").read_text(encoding="utf-8"))["hats"]
    out_dir = root / "docs/reference/hero-layered-weapons"
    out_dir.mkdir(parents=True, exist_ok=True)
    cw, chh = CROP[2] - CROP[0], CROP[3] - CROP[1]
    for ci, cls in enumerate(gp.CLASS_GENDER):
        cj, ctx = context(root, cls)
        bj, heads = ctx[0], ctx[3]
        starter = next(h for h in hats if h["classId"] == cls)
        straw = next(h for h in hats if h["key"] == "straw-hat")
        hat_of = {k["key"]: (k, np.asarray(Image.open(root / "Assets/Resources" / (k["sheet"] + ".png")).convert("RGBA"))) for k in (starter, straw)}
        for a in bj["anims"]:
            anim, n = a["name"], a["frames"]
            cols = n if n > 1 else 3
            img = Image.new("RGBA", (cw * cols, chh * 8), (196, 204, 188, 255))
            dr = ImageDraw.Draw(img)
            for d8 in range(8):
                hx, ox = pal["hair"][(ci + d8) % 8], pal["outfit"][(ci + d8 * 2) % 6]
                for c in range(cols):
                    hat = None if n > 1 or c == 0 else hat_of[(starter if c == 1 else straw)["key"]]
                    fr = frame(root, ctx, anim, d8, c if n > 1 else 0, hx, ox, hat)
                    img.alpha_composite(fr.crop(CROP), (c * cw, d8 * chh))
                dr.text((3, d8 * chh + 3), DIR8[d8], fill=(30, 30, 30, 255))
            img.convert("RGB").save(out_dir / f"{cls}-{anim}.png")
        rule_check(root, cls, cj, bj, heads)
    print("wrote", out_dir)

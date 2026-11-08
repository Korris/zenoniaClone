"""Mechanic attack1-4 with every hairstyle, composited the way HeroRenderer + HeroHeadLayer draw them (no Unity): gear behind, body/headless,
head of style n placed by head-anchors.json, gear in front, body/hand on top. Left rows of HeroClassDef.MirrorLeft anims (attack3, attack4)
are the right frame mirrored, head included (HeroHeadLayer.Apply uses the right anchors and head for them).
A frame without an anchor or headless row draws the whole bare body, like the game (marked with a red corner).

Usage (project root; Pillow, numpy, scipy):
  python Tools/compose-mechanic-attack-hairstyles-check.py OUT.png [--anims attack1,attack2,attack3,attack4] [--dirs down,up,right,left]
         [--styles 0,1,2,3,4] [--tier scrap] [--zoom 1] [--rows-dir DIR]
  rows = anim x facing, columns = style x frame; --rows-dir also saves each row alone at 3x (to look at frame by frame)."""
import argparse, importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import head_layer_core as core  # noqa: E402
import head_layer_dirs as hd  # noqa: E402


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


cc = _load("cc", os.path.join(os.path.dirname(os.path.abspath(__file__)), "compose-mechanic-cannon-check.py"))
FRAME, SPR, RES = cc.FRAME, cc.SPR, cc.RES
PAD = 50
CROP = (PAD - 34, PAD - 10, PAD + FRAME + 34, PAD + FRAME + 10)  # around the cell; cannon tips may poke out a little


def head_anchors():
    with open(f"{RES}/Data/classes/mechanic/head-anchors.json", encoding="utf-8") as fh:
        return {(x["anim"], x["dir"]): x["frames"] for x in json.load(fh)["anims"]}


def compose(file, anchors, hanc, anim, d, f, tier_i, style):
    """One frame: returns (RGBA canvas FRAME + 2 PAD square, used head layer)."""
    mirror = d == "left" and anim in cc.MIRROR_LEFT
    row = 3 if mirror else cc.ROWS[d]
    hdir = "right" if mirror else d
    canvas = Image.new("RGBA", (FRAME + 2 * PAD,) * 2, (0, 0, 0, 0))
    layers = []
    tier = file["weapon"][tier_i]
    for i, (pid, folder) in enumerate(cc.PARTS):
        a = (anchors.get((pid, anim, d)) or [None] * 99)[f]
        if a is None or a.get("fallback") or a.get("hidden"):
            continue
        if a.get("art"):
            art = cc.tier_art(folder, a["art"], tier["name"])
            grip = (art.width / 2, art.height / 2) if art else None
        else:
            art = cc.load_art(f"{SPR}/{folder}/{tier['name']}-base.png")
            grip = (tier["baseGrip"]["gripX"], tier["baseGrip"]["gripY"])
        if art is None:
            continue
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        cc.place(layer, art, grip, a, PAD)
        layers.append(((1 if a.get("behind") else 4) + min(i, 1), layer))
    hl_path = f"{SPR}/body/headless/{anim}.png"
    fa = (hanc.get((anim, hdir)) or [None] * 99)[f]
    head = hd.head_of("mechanic", style, hdir)
    used = fa is not None and head is not None and os.path.exists(hl_path)
    body = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    if used:
        cellim = Image.open(hl_path).convert("RGBA").crop((f * FRAME, row * FRAME, (f + 1) * FRAME, (row + 1) * FRAME))
        hc = core.u8(core.straight(hd.place(head, fa)))
        cellim.alpha_composite(Image.fromarray(hc))  # head: same order as the body, just in front of it
        if mirror:
            cellim = cellim.transpose(Image.FLIP_LEFT_RIGHT)
    else:
        cellim = cc.cell(f"{SPR}/body/bare/{anim}.png", row, f, mirror)
    body.alpha_composite(cellim, (PAD, PAD))
    layers.append((3, body))
    hp = f"{SPR}/body/hand/{anim}.png"
    if os.path.exists(hp):
        hand = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        hand.alpha_composite(cc.cell(hp, row, f, mirror), (PAD, PAD))
        layers.append((6, hand))
    for _, im in sorted(layers, key=lambda t: t[0]):
        canvas.alpha_composite(im)
    return canvas, used


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out")
    ap.add_argument("--anims", default="attack1,attack2,attack3,attack4")
    ap.add_argument("--dirs", default="down,up,right,left")
    ap.add_argument("--styles", default="0,1,2,3,4")
    ap.add_argument("--tier", default="scrap")
    ap.add_argument("--zoom", type=int, default=1)
    ap.add_argument("--bg", default="58,62,70")
    ap.add_argument("--rows-dir", default=None)
    args = ap.parse_args()
    file = json.load(open(f"{RES}/Data/classes/mechanic/held-mechanic-dual.json", encoding="utf-8"))
    anchors = {(s["part"], s["anim"], s["dir"]): s["frames"] for s in file["anchors"]}
    tier_i = [e["name"] for e in file["weapon"]].index(args.tier)
    hanc = head_anchors()
    styles = [int(s) for s in args.styles.split(",")]
    bg = tuple(int(v) for v in args.bg.split(",")) + (255,)
    cw, ch = CROP[2] - CROP[0], CROP[3] - CROP[1]
    rows = [(a, d) for a in args.anims.split(",") for d in args.dirs.split(",")]
    nf = {a: Image.open(f"{SPR}/body/bare/{a}.png").width // FRAME for a in args.anims.split(",")}
    cols = max(nf.values())
    gap, label = 8, 18
    sheet = Image.new("RGBA", (len(styles) * (cols * cw + gap), len(rows) * (ch + label)), bg)
    dr = ImageDraw.Draw(sheet)
    for r, (anim, d) in enumerate(rows):
        y = r * (ch + label)
        dr.text((4, y + 3), f"{anim} {d}", fill=(255, 255, 0, 255))
        strip = Image.new("RGBA", (len(styles) * (nf[anim] * cw + gap), ch), bg)
        for si, s in enumerate(styles):
            for f in range(nf[anim]):
                im, used = compose(file, anchors, hanc, anim, d, f, tier_i, s)
                tile = im.crop(CROP)
                x = si * (cols * cw + gap) + f * cw
                sheet.alpha_composite(tile, (x, y + label))
                strip.alpha_composite(tile, (si * (nf[anim] * cw + gap) + f * cw, 0))
                if not used:
                    dr.polygon([(x, y + label), (x + 12, y + label), (x, y + label + 12)], fill=(255, 0, 0, 255))
            if r == 0:
                dr.text((si * (cols * cw + gap) + 120, 3), f"style {s}", fill=(255, 255, 255, 255))
        if args.rows_dir:
            os.makedirs(args.rows_dir, exist_ok=True)
            strip.resize((strip.width * 3, strip.height * 3), Image.NEAREST).convert("RGB").save(f"{args.rows_dir}/{anim}-{d}.png")
    if args.zoom > 1:
        sheet = sheet.resize((sheet.width * args.zoom, sheet.height * args.zoom), Image.NEAREST)
    sheet.convert("RGB").save(args.out)
    print(args.out, sheet.size, "missing art:", sorted(cc.MISSING) or "none")


if __name__ == "__main__":
    main()

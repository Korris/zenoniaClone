"""Hairstyle check of the Cleric attack anims (docs/sprite-rules.md, "Hình mẫu nhân vật" step 4): rows = attack1-4 x 4 facings, columns =
the 5 hairstyles (heads/<n>) x every frame, each cell composited the way the game draws it (headless body + hairstyle head by
head-anchors.json + staff + shield T0 + fist layer, Tools/cleric_attack_compose.py). A frame without a head anchor shows the painted
head, marked with a red corner.

  python Tools/build-cleric-attack-hairstyles-check.py OUT.png [anim ...]
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cleric_attack_compose as cc  # noqa: E402

STYLES = range(5)


def main(out, anims):
    heads = cc.head_anchors()
    pad, top, h = 30, 20, 190  # crop: from 20 px above the cell top (raised staff) down to the soles
    w = cc.FRAME + 2 * pad
    rows = [(a, d) for a in anims for d in cc.DIRS]
    ncol = max(cc.frame_count(a) for a in anims) * len(STYLES)
    sheet = Image.new("RGB", (w * ncol + 70, h * len(rows)), (40, 44, 52))
    dr = ImageDraw.Draw(sheet)
    for r, (a, d) in enumerate(rows):
        dr.text((4, r * h + 4), f"{a}\n{d}", fill=(230, 230, 230))
        n = cc.frame_count(a)
        for s in STYLES:
            for f in range(n):
                im = cc.frame(a, d, f, style=s, pad=pad, heads=heads)
                tile = cc.on_bg(im).crop((0, pad + top - 20, w, pad + top - 20 + h))
                x = 70 + (s * n + f) * w
                sheet.paste(tile, (x, r * h))
                if (a, d) not in heads or f >= len(heads[(a, d)]):
                    dr.rectangle((x, r * h, x + 10, r * h + 10), fill=(255, 0, 0))
            dr.line((70 + s * n * w, r * h, 70 + s * n * w, r * h + h), fill=(255, 255, 0))
    sheet.save(out)
    print("->", out, sheet.size)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2:] or ["attack1", "attack2", "attack3", "attack4"])

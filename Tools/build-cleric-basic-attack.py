"""Cleric basic attack (4-hit combo) redone from a ChatGPT board, the way the Mechanic's was (2026-11-08).

Board: docs/reference/cleric-basic-attack-board.png (= ArtLibrary/classes/cleric/attack-board/cleric-basic-attack-board-chatgpt-*.png).
Combo timing / range (ComboConfigs.ClericHits): hits 1-3 70 + 60 + 130 ms = 0.26 s, bolt range 60 (~4 tiles); hit 4 130 + 80 + 250 ms =
0.46 s, range 55, burst r26. Hit N draws body anim attackN (HeroClassDef.Cleric.ComboAnims).

  python Tools/build-cleric-basic-attack.py ref                 ChatGPT inputs in attack-board/ref/: in-game idle 4 facings (head layer,
                                                                 staff + shield T0), per facing idle x4 crops, head-width ratios printed
  python Tools/build-cleric-basic-attack.py headcheck OUT.png   idle vs attack1-4 x 4 facings, body + head only, head/height ratios printed
Body rows and anchors: Tools/build-cleric-attack-body.py. FX: Tools/build-cleric-attack-fx.py.
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cleric_attack_compose as cc  # noqa: E402

ROOT = cc.ROOT
REF = f"{ROOT}/Assets/ArtLibrary/classes/cleric/attack-board/ref"


def ref():
    os.makedirs(REF, exist_ok=True)
    pad, z = 40, 3
    size = cc.FRAME + 2 * pad
    strip = Image.new("RGB", (size * 4 * z, size * z), (200, 210, 225))
    for i, d in enumerate(cc.DIRS):
        im = cc.frame("idle", d, 0, pad=pad)
        big = cc.on_bg(im, (200, 210, 225)).resize((size * z, size * z), Image.NEAREST)
        strip.paste(big, (i * size * z, 0))
        body = cc.frame("idle", d, 0, pad=0, gear=False)
        top, bot, h, w = cc.measure(body)
        print(f"idle {d}: hair top {top} sole {bot} height {h} head width {w} -> head {100 * w / h:.0f}% of height")
        crop = cc.on_bg(im.crop((pad - 20, pad + top - 30, pad + cc.FRAME + 20, pad + bot + 10)), (0, 255, 0))
        crop.resize((crop.width * 4, crop.height * 4), Image.NEAREST).save(f"{REF}/idle-{d}-x4.png")
    strip.save(f"{REF}/cleric-ingame-idle-4dirs.png")
    # empty-hand body + head (no staff, no shield): the pose / proportion reference for the attack rows, facings labelled
    bare = Image.new("RGB", (size * 4 * z, (size + 20) * z), (200, 210, 225))
    dr = ImageDraw.Draw(bare)
    for i, d in enumerate(cc.DIRS):
        im = cc.on_bg(cc.frame("idle", d, 0, pad=pad, gear=False), (200, 210, 225)).resize((size * z, size * z), Image.NEAREST)
        bare.paste(im, (i * size * z, 20 * z))
        dr.text((i * size * z + 20, 10), f"facing {d.upper()}", fill=(20, 20, 20))
    bare.save(f"{REF}/cleric-ingame-idle-4dirs-empty-hands.png")
    print("->", REF)


def headcheck(out):
    """Rows = facings, columns = idle f0, then every frame of attack1-4 (body + hairstyle 0 head, no gear). Under each: the drawn head's width
    and height against idle's (rule 8: within 5 %), and the body height (hair top to sole; the hit-4 crouch is lower on purpose)."""
    import numpy as np
    anims = ["attack1", "attack2", "attack3", "attack4"]
    cols = [("idle", 0)] + [(a, f) for a in anims if os.path.exists(f"{cc.BODY}/bare/{a}.png") for f in range(cc.frame_count(a))]
    z, cw, ch = 2, cc.FRAME, cc.FRAME + 26
    sheet = Image.new("RGB", (cw * len(cols) * z, ch * 4 * z), (40, 44, 52))
    heads = cc.head_anchors()
    worst = 0.0
    for r, d in enumerate(cc.DIRS):
        ref = None
        for c, (a, f) in enumerate(cols):
            body = cc.frame(a, d, f, pad=0, gear=False, heads=heads)
            head = cc.head_only(a, d, f, heads=heads)
            top, bot, h, _ = cc.measure(body)
            tile = Image.new("RGB", (cw, ch), (40, 44, 52))
            tile.paste(cc.on_bg(body), (0, 0))
            dr = ImageDraw.Draw(tile)
            if head is None:
                dr.text((4, cc.FRAME), f"{a[0]}{a[-1]}f{f} painted head", fill=(255, 80, 80))
            else:
                al = np.asarray(head)[..., 3] > 100
                hw, hh = int(np.ptp(np.nonzero(al.any(0))[0])) + 1, int(np.ptp(np.nonzero(al.any(1))[0])) + 1
                ref = ref or (hw, hh)
                dev = 100 * max(abs(hw / ref[0] - 1), abs(hh / ref[1] - 1))
                worst = max(worst, dev)
                dr.text((4, cc.FRAME), f"{a[0]}{a[-1]}f{f} head {hw}x{hh} {dev:.0f}%", fill=(255, 80, 80) if dev > 5 else (220, 220, 220))
            dr.text((4, cc.FRAME + 12), f"body h {h}", fill=(180, 180, 180))
            sheet.paste(tile.resize((cw * z, ch * z), Image.NEAREST), (c * cw * z, r * ch * z))
    sheet.save(out)
    print("->", out, f"largest head size deviation from idle: {worst:.1f}%")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "ref":
        ref()
    elif cmd == "headcheck":
        headcheck(sys.argv[2])
    else:
        print(__doc__)

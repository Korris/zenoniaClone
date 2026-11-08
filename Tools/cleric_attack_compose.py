"""Cleric frames composited the way the game draws them, head layer included (Tools/build-cleric-basic-attack.py).

Game order (HeroRenderer + HeroHeadLayer): gear marked behind, body (body/headless/<anim> when head-anchors.json has the row, else
body/bare/<anim>), the hairstyle head placed by its anchor, gear in front, the fist layer body/hand/<anim> on top.
Rows without a head anchor keep the whole bare body (its painted head), like the game.
"""
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image

ROOT = "D:/zenoniaClone"
sys.path.insert(0, f"{ROOT}/Tools")
import head_layer_core as core  # noqa: E402
import head_layer_dirs as hd  # noqa: E402

FRAME = 192
RES = f"{ROOT}/Assets/Resources"
BODY = f"{RES}/Sprites/classes/cleric/body"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
DIRS = ("down", "up", "left", "right")


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


chf = _load("chf", f"{ROOT}/Tools/compose-hero-frame.py")


def head_anchors():
    data = json.load(open(f"{RES}/Data/classes/cleric/head-anchors.json", encoding="utf-8"))
    return {(e["anim"], e["dir"]): e["frames"] for e in data["anims"]}


def _cell(path, row, f):
    return Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))


def frame(anim, d, f, style=0, tier=0, pad=60, anchors=None, gear=True, heads=None):
    """One composited frame, (192 + 2 pad) square RGBA. gear=False: body + head only (for head measures)."""
    heads = heads if heads is not None else head_anchors()
    row = ROWS[d]
    hl_path = f"{BODY}/headless/{anim}.png"
    ha = heads.get((anim, d))
    use_head = ha is not None and f < len(ha) and os.path.exists(hl_path)
    if use_head:
        body = core.u8(hd.composite(np.asarray(_cell(hl_path, row, f)).astype(np.float64), hd.head_of("cleric", style, d), ha[f]))
        body = Image.fromarray(body)
    else:
        body = _cell(f"{BODY}/bare/{anim}.png", row, f)
    if not gear:
        c = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
        c.alpha_composite(body, (pad, pad))
        return c
    # compose-hero-frame draws behind gear, the bare cell, front gear, hand: give it our body (head layer included) as the "bare" sheet
    tmp = f"{os.environ.get('TEMP', '/tmp')}/cleric-compose-body.png"
    sheet = Image.new("RGBA", (FRAME * (f + 1), FRAME * 4), (0, 0, 0, 0))
    sheet.paste(body, (f * FRAME, row * FRAME))
    sheet.save(tmp)
    hand = f"{BODY}/hand/{anim}.png"
    return chf.compose("cleric", anim, d, f, bare_path=tmp, anchors=anchors, hand_path=hand if os.path.exists(hand) else "-", tier=tier, pad=pad)


def frame_count(anim):
    return Image.open(f"{BODY}/bare/{anim}.png").width // FRAME


def on_bg(im, bg=(70, 84, 110)):
    b = Image.new("RGBA", im.size, bg + (255,))
    b.alpha_composite(im)
    return b.convert("RGB")


def measure(im):
    """(hair top, sole, figure height, head width) of a body-only composite: head width = widest row in the top 40 % of the figure."""
    a = np.asarray(im)[..., 3] > 100
    ys = np.nonzero(a.any(1))[0]
    top, bot = int(ys.min()), int(ys.max())
    h = bot - top + 1
    w = max(int(np.ptp(np.nonzero(a[y])[0])) + 1 for y in range(top, top + int(0.4 * h)) if a[y].any())
    return top, bot, h, w


def head_only(anim, d, f, style=0, heads=None):
    """The hairstyle head alone as the game places it on that frame (RGBA 192 cell), None when the frame keeps the painted head."""
    heads = heads if heads is not None else head_anchors()
    ha = heads.get((anim, d))
    if ha is None or f >= len(ha):
        return None
    return Image.fromarray(core.u8(core.straight(hd.place(hd.head_of("cleric", style, d), ha[f]))))

"""Wizard frames composited the way HeroRenderer + HeroHeadLayer draw them (no Unity): staff / book behind, the headless Lv1 body,
the hairstyle head placed by head-anchors.json, staff / book in front, the fist layer on top. A frame without head anchors (or without a
headless sheet) draws the whole bare body, like the game. Used by Tools/build-wizard-attack-board-repaint.py (refs, grid, check).

  compose(anim, d, f, style=0, tier=0, pad=0, bare=None, hand=None, anchors=None)  -> RGBA image (192 + 2 pad square)
"""
import importlib.util
import json
import os

import numpy as np
from PIL import Image

ROOT = "D:/zenoniaClone"
RES = f"{ROOT}/Assets/Resources"
BODY = f"{RES}/Sprites/classes/wizard/body"
HEADS = f"{RES}/Sprites/classes/wizard/heads"
HEAD_ANCHORS = f"{RES}/Data/classes/wizard/head-anchors.json"
FRAME = 192
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


chf = _load("chf", f"{ROOT}/Tools/compose-hero-frame.py")


def head_anchors():
    with open(HEAD_ANCHORS, encoding="utf-8") as fh:
        return {(x["anim"], x["dir"]): x["frames"] for x in json.load(fh)["anims"]}


def head_art(style, d):
    """Head cell of a style + facing as the game picks it (left = right mirrored)."""
    if d == "left":
        h = head_art(style, "right")
        return None if h is None else h.transpose(Image.FLIP_LEFT_RIGHT)
    p = f"{HEADS}/{style}/{d}.png"
    return Image.open(p).convert("RGBA") if os.path.exists(p) else None


def placed_head(style, d, a):
    """Head cell moved by an anchor {x, y, s, sy, angle} (scale about the cell bottom centre, rotate clockwise, move), as HeroHeadLayer."""
    import math
    h = head_art(style, d)
    s = a.get("s", 1.0) or 1.0
    sy = a.get("sy", 1.0) or 1.0
    th = math.radians(a.get("angle", 0.0))
    c, n = math.cos(th), math.sin(th)
    px, py = FRAME / 2, float(FRAME)
    # output -> source: undo move, rotate back, unscale (pixel centres)
    # x_s = (c*(xo-px-ax) + n*(yo-py-ay)) / s + px ; y_s = (-n*(xo-px-ax) + c*(yo-py-ay)) / (s*sy) + py
    ax, ay = a["x"], a["y"]
    A = c / s
    B = n / s
    C = px - A * (px + ax) - B * (py + ay)
    D = -n / (s * sy)
    E = c / (s * sy)
    F = py - D * (px + ax) - E * (py + ay)
    return h.transform((FRAME, FRAME), Image.AFFINE, (A, B, C, D, E, F), Image.BILINEAR)


def no_gear():
    """Anchor table with every staff / book frame hidden: the body alone (ChatGPT references of empty fists)."""
    _, loaded = chf.load_gear("wizard")
    return {k: [dict(fr, hidden=True) for fr in v] for k, v in loaded.items()}


def compose(anim, d, f, style=0, tier=0, pad=0, bare=None, hand=None, anchors=None, heads=True):
    """One frame as the game draws it. bare / hand: sheet paths overriding body/bare|hand/<anim>.png (previews of a row not yet written)."""
    ha = head_anchors()
    a = (ha.get((anim, d)) or [None] * 99)[f] if heads else None
    headless = f"{BODY}/headless/{anim}.png"
    if a is not None and os.path.exists(headless) and bare is None:
        return chf.compose("wizard", anim, d, f, bare_path=headless, hand_path=hand, anchors=anchors, tier=tier, pad=pad,
                           over=placed_head(style, d, a))
    return chf.compose("wizard", anim, d, f, bare_path=bare, hand_path=hand, anchors=anchors, tier=tier, pad=pad)


def on_bg(im, bg=(58, 62, 70)):
    out = Image.new("RGBA", im.size, bg + (255,))
    out.alpha_composite(im)
    return out.convert("RGB")

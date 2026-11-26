"""Rest layout, bones and draw order of the Warrior facing-camera TEST rig (cell 320 x 256, feet y 244, like body/idle.png).

Part scale: the ChatGPT parts sheet draws a chunkier body than the approved idle, so body parts are scaled by K * SCALE
(K = head ear-to-ear 78 px). The head is NOT the ChatGPT head: it is cut from the approved idle-down cell (exact face, and the
hair anchor x 158.5, y 27, w 70 stays valid). Positions = (part centre x, part top y) in cell px at rest.
"""
import numpy as np
from PIL import Image

IDLE = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/idle.png"
CELL_W, CELL_H, FEET_Y, BODY_PX = 320, 256, 244, 200

SCALE = {  # factor on K per part
    "torso": 0.8, "hips": 0.8, "padL": 0.8, "padR": 0.8, "upperArmL": 0.8, "upperArmR": 0.8,
    "forearmL": 0.65, "forearmR": 0.65, "thighL": 0.8, "thighR": 0.8, "shinL": 0.8, "shinR": 0.8,
    "capeL": 1.0, "capeM": 1.0, "capeR": 1.0,
}

# part -> (centre x, top y) at rest
PLACE = {
    "capeM": (160, 112), "capeL": (124, 118), "capeR": (196, 118),
    "thighL": (147, 138), "thighR": (173, 138),
    "torso": (160, 92),
    "hips": (160, 132),
    "shinL": (139, 167), "shinR": (181, 167),
    "upperArmL": (136, 111), "upperArmR": (184, 111),
    "padL": (135, 106), "padR": (185, 106),
    "head": (160, 0),            # idle cut: placed at its own cell position (top y unused)
    "forearmL": (120, 122), "forearmR": (200, 122),
}

# back to front (z index); forearms last = over the body like the frame art's hand layer
ORDER = ["capeM", "capeL", "capeR", "thighL", "thighR", "torso", "hips", "shinL", "shinR",
         "upperArmL", "upperArmR", "padL", "padR", "head", "forearmL", "forearmR"]

# bones: name, parent, joint (cell px at rest), lag (fraction of the cycle: follow-through)
BONES = [
    ("root", "", (160, 146), 0.0),
    ("torso", "root", (160, 140), 0.0),
    ("head", "torso", (160, 100), 0.04),
    ("padL", "torso", (138, 108), 0.0), ("padR", "torso", (182, 108), 0.0),
    ("upperArmL", "torso", (135, 114), 0.0), ("upperArmR", "torso", (185, 114), 0.0),
    ("forearmL", "upperArmL", (127, 136), 0.0), ("forearmR", "upperArmR", (193, 136), 0.0),
    ("thighL", "root", (148, 146), 0.0), ("thighR", "root", (172, 146), 0.0),
    ("shinL", "thighL", (142, 186), 0.0), ("shinR", "thighR", (178, 186), 0.0),
    ("capeM", "torso", (160, 110), 0.10), ("capeL", "torso", (132, 112), 0.08), ("capeR", "torso", (188, 112), 0.08),
]
PART_BONE = {n: n for n in PLACE}
PART_BONE["hips"] = "root"
HAND_PARTS = {"forearmL"}  # drawn on the fist order (over the sword handle) when the sword is in front

HAIR = {"bone": "head", "x": 158.5, "y": 27, "w": 70.0, "angle": 0.0}
WEAPON = {"bone": "forearmL", "x": 116.0, "y": 162.0, "w": 0.0, "angle": -131.4}   # idle-down grip of the frame art
OFFHAND = {"bone": "forearmR", "x": 201.0, "y": 162.0, "w": 0.0, "angle": 0.0}


def idle_head():
    """Head (bald, face) cut from the approved idle-down cell: rows above the chin, scarf navy removed. Returns (rgba, left, top)."""
    a = np.array(Image.open(IDLE).convert("RGBA").crop((0, 0, CELL_W, CELL_H))).copy()
    a[103:] = 0
    rgb = a[..., :3].astype(int)
    navy = (rgb[..., 2] > rgb[..., 0] + 10) & (rgb[..., 2] < 170)
    rows = np.zeros(CELL_H, bool)
    rows[93:103] = True
    a[navy & rows[:, None]] = 0
    ys, xs = np.where(a[..., 3] > 0)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    return a[y0:y1, x0:x1], int(x0), int(y0)


def placed(parts):
    """name -> (rgba, left, top) at rest; parts from warrior_rig_down_parts.all_parts(scale=SCALE)."""
    out = {}
    for n, (cx, top) in PLACE.items():
        if n == "head":
            out[n] = idle_head()
            continue
        p = parts[n]
        out[n] = (p, int(round(cx - p.shape[1] / 2)), int(top))
    return out

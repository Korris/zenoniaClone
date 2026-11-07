"""Neck cuts of the head layer for the directions other than idle-down (Tools/head_layer_dirs.py), per class and facing.

Coordinates are cell px of frame 0 of `idle` in that facing (row 1 = up, row 3 = right); left uses the right head mirrored, so it has no
cut of its own. Every other frame (idle breathing, walk) gets the same cut moved with the head's anchor.
cut:   polyline [(x, y), ...]: solid pixels above it are head.
chin:  optional (x0, x1, y0, y1) box below the cut whose pixels `chin_head` accepts are head too (the chin, a face mask).
hair_to: optional row: class-hair-coloured pixels below the cut, joined to the head, down to this row are head too.
keep:  (x0, x1): opaque non-hair head pixels within 2 px of the body stay on the body too, so a sub-pixel move never opens the neck.
graft: first row of the face that a new hairstyle takes from style 0 (side view only; eyes and below), `face_x` the face columns.
Each cut was checked on the zoomed mask preview (`dirs split`).
"""
from head_layer_classes import skin, not_red

DIRS = {
    "warrior": {
        # walk down (idle down is the prototype's colour cut): the fur collar rises beside the chin
        "down": {"cut": [(0, 96), (82, 96), (86, 104), (110, 104), (114, 96), (192, 96)], "chin": (84, 112, 100, 108), "chin_head": skin, "keep": (84, 112)},
        "up": {"cut": [(0, 90), (192, 90)], "keep": (70, 120)},
        "right": {"cut": [(0, 90), (98, 90), (104, 97), (192, 97)], "chin": (98, 130, 97, 104), "chin_head": skin, "keep": (98, 126),
                  "graft": 80, "face_x": (98, 132)},
    },
    # wizard (2026-11-07: hoods dropped, every style is drawn bare-headed): the cut takes the hood with the head, the capelet stays body
    "wizard": {
        "up": {"cut": [(0, 87), (192, 87)], "keep": (80, 112)},
        "right": {"cut": [(0, 97), (192, 97)], "chin": (98, 132, 95, 103), "chin_head": skin, "keep": (98, 128),
                  "graft": 82, "face_x": (100, 140)},
    },
    "mechanic": {
        # walk down: the scarf tail flies out left at neck height; the cut climbs left of the hair so it stays body
        "down": {"cut": [(0, 76), (58, 76), (64, 88), (70, 93), (76, 97), (192, 97)], "chin": (84, 108, 97, 103), "chin_head": skin, "keep": (84, 108)},
        "up": {"cut": [(0, 92), (192, 92)], "keep": (80, 112)},
        "right": {"cut": [(0, 94), (192, 94)], "chin": (96, 128, 92, 102), "chin_head": skin, "keep": (96, 124),
                  "graft": 78, "face_x": (96, 130)},
    },
    "assassin": {
        # walk down: the prototype's cut drops to row 132 left of the face for the ponytail and would take the swinging fist; cut flat, take the hair by colour
        "down": {"cut": [(0, 108), (192, 108)], "chin": (78, 119, 108, 116), "chin_head": not_red, "keep": (80, 116), "hair_to": 134},
        "up": {"cut": [(0, 104), (192, 104)], "keep": (80, 116), "hair_to": 124},
        "right": {"cut": [(0, 104), (192, 104)], "hair_to": 132, "chin": (96, 128, 100, 112), "chin_head": not_red, "keep": (96, 124),
                  "graft": 78, "face_x": (96, 130)},
    },
    "cleric": {
        "up": {"cut": [(0, 112), (192, 112)], "keep": (80, 112)},
        "right": {"cut": [(0, 110), (192, 110)], "chin": (92, 124, 104, 114), "chin_head": skin, "keep": (92, 122),
                  "graft": 86, "face_x": (90, 126)},
    },
}

"""Per-class settings of the head layer tool (Tools/build-head-layer.py), idle facing down, row 0 of body/bare/idle.png.

mask:    "warrior" = the prototype's colour-component cut (head_layer_warrior.py), "polyline" = everything solid above `cut` is head,
         plus the pixels in the `chin` box (x0, x1, y0, y1) that `chin_head` calls head (skin, a face mask), dark outlines going
         with their nearest light pixel. Every cut was checked on a zoomed mask preview (`split`).
breath:  "lift" = the Warrior's smoothstep lift (build-warrior-move.py), "scale" = a vertical scale about the sole edge, measured per frame.
crop:    head crop (x0, y0, x1, y1, cell px) sent to ChatGPT at 4x on #00FF00.
keep:    (x0, x1): face pixels of the head this close (KEEP px) to the body stay on the body as well, so a sub-pixel move never opens the chin.
graft:   first row of the face that every new hairstyle takes from style 0 (eyes and below), `hair` = style 0 hair/accessory colours in
         those rows that are not face (never grafted); `eyes` boxes (x0, y0, x1, y1) are always face (the Warrior's red eyes share the hair colour; elsewhere empty, or the old bangs crossing the eyes would be grafted);
         `face_x` = columns the face lies in (keeps hood and hair shadows beside the cheeks out of the graft);
         `shadow` (optional) = (colour test, row): pixels above that row matching it are old-hair shadow on the skin, not face;
         `behind_x` (optional) = columns where new hair below style 0's head is back hair: erased, the body hides it.
"""
import numpy as np


def _rgb(c):
    return [c[..., i].astype(np.float64) for i in range(3)]


def skin(c):
    """Peach / pale skin of every class (also the light pink of the mouth)."""
    r, g, b = _rgb(c)
    return (r > 175) & (r >= g) & (g > b) & (r - b > 18) & (r - b < 120) & (g - b > 6)


def not_red(c):
    r, g, b = _rgb(c)
    return ~((r > 120) & (r > g * 1.8) & (r > b * 1.8))


CLASSES = {
    "warrior": {
        "female": False, "mask": "warrior", "breath": "lift", "crop": (36, 10, 156, 112), "keep_band": 2, "graft": 73,
        "hair": lambda c: (lambda r, g, b: (r > g + 60))(*_rgb(c)),
        "eyes": [(79, 82, 92, 91), (105, 82, 118, 91)], "face_x": (70, 126),
    },
    "wizard": {
        "female": False, "mask": "polyline", "breath": "scale", "crop": (40, 22, 152, 116), "keep": (84, 110), "graft": 89,
        "cut": [(0, 100), (82, 100), (84, 101), (106, 101), (108, 103), (130, 103), (132, 101), (192, 101)], "chin": (84, 112, 101, 108), "chin_head": skin,
        # lavender-white hair, dark blue hood, gold trim are not face (the purple eyes are)
        "hair": lambda c: (lambda r, g, b: ((r > 190) & (g > 180) & (b > 215) & (b >= r)) | ((b > g + 10) & (r > 140) & (g > 110) & (b > 170)) | ((b > r + 30) & (r < 110)) | ((r > 150) & (g > 120) & (b < 90)))(*_rgb(c)),
        "eyes": [], "shadow": (lambda c: (lambda r, g, b: (r > 195) & (g > 85) & (g < 160) & (b < 150) & (r - g > 60))(*_rgb(c)), 90), "face_x": (75, 119),
    },
    "mechanic": {
        "female": False, "mask": "polyline", "breath": "scale", "crop": (44, 18, 148, 110), "keep": (84, 108), "graft": 80,
        "cut": [(0, 97), (192, 97)], "chin": (84, 108, 97, 103), "chin_head": skin,
        # yellow hair, brass goggles (the cyan eyes are face)
        "hair": lambda c: (lambda r, g, b: ((r > 150) & (g > 100) & (r - b > 120)) | ((r < 200) & (r - b > 90)))(*_rgb(c)),
        "eyes": [], "face_x": (74, 118),
    },
    "assassin": {
        "female": True, "mask": "polyline", "breath": "scale", "crop": (12, 10, 150, 128), "keep": (80, 116), "graft": 76,
        "cut": [(0, 132), (63, 132), (67, 112), (77, 112), (78, 108), (118, 108), (119, 112), (192, 112)], "chin": (78, 119, 108, 116), "chin_head": not_red,
        # purple hair; graft from row 76 so the red headband is style 0's too (ChatGPT drew it lower, over the brows); red eyes, black face mask are face
        "hair": lambda c: (lambda r, g, b: (b > g + 20) & (r > g + 10))(*_rgb(c)),
        "purge": lambda c: (lambda r, g, b: (r > 160) & (g < 90) & (b < 100))(*_rgb(c)),  # headband red in the new art
        "eyes": [], "face_x": (76, 121),
    },
    "cleric": {
        "female": True, "mask": "polyline", "breath": "scale", "crop": (44, 18, 148, 120), "keep": (82, 110), "graft": 88,
        "cut": [(0, 115), (79, 115), (81, 111), (106, 111), (109, 114), (112, 116), (192, 116)], "chin": (82, 110, 111, 116), "chin_head": skin,
        # blonde hair and its orange tips (the head piece is above the graft rows; the grey-blue eyes are face)
        "hair": lambda c: (lambda r, g, b: ((g - b > 45) & (r > 200)) | ((r - b > 110) & (g < 140)))(*_rgb(c)),
        "eyes": [], "behind_x": (76, 116), "face_x": (70, 118),
        "eye_fill": ((0, 88, 52), (84, 102)),  # the doll's keyed-out irises: dark green of the side-view eye
    },
}

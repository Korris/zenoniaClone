"""Wizard V3 (Asteria Mage, 26/11/2026) fist tables, read by eye on the 4x numbered sheets
docs/reference/character-v3/wizard-v3-test/check/fists-wizard-<anim>.png (written by `build-wizard-v3-game-assets.py fists`).
Replace the 19/11 Wizard entries of ro_fantasy_grips.OVERRIDES / ro_fantasy_motion_fists (old art) at run time.

R = character's right hand (staff), L = left hand (book / empty), [x, y, visible] in body-cell px (320 x 256).
Read on the 188 px body, then scaled x 216/188 about the feet (160, 244) when the Wizard got the Knight V3 height (BALD_PX_OF 216, 26/11).
Painted rows: 0 down, 1 down-right, 2 right, 3 up-right, 4 up (attack: + 5 up-left, 6 left, 7 down-left).
down / down-right / down-left: R = screen-left fist; right: R = the NEAR fist; left: R = the FAR fist; up / up-right / up-left: R = screen-right.
"""
T, F = True, False

IDLE = {   # row: {hand: [x, y, vis]}  (idle chatgpt-2, read 26/11)
    0: {"R": [115, 168, T], "L": [208, 167, T]},
    1: {"R": [120, 169, T], "L": [201, 164, T]},
    2: {"R": [160, 167, T], "L": [178, 161, F]},  # the far fist hides behind the hip
    3: {"R": [191, 170, T], "L": [120, 168, T]},
    4: {"R": [208, 170, T], "L": [114, 169, T]},
}
WALK = {   # (row, frame): {...}  (walk 8f chatgpt-1 of each dir, read 26/11 on 3x grids)
    (0, 0): {"R": [120, 167, T], "L": [204, 165, T]}, (0, 1): {"R": [116, 162, T], "L": [203, 162, T]},
    (0, 2): {"R": [123, 165, T], "L": [208, 161, T]}, (0, 3): {"R": [126, 159, T], "L": [198, 161, T]},
    (0, 4): {"R": [120, 159, T], "L": [192, 156, T]}, (0, 5): {"R": [116, 161, T], "L": [192, 156, T]},
    (0, 6): {"R": [120, 159, T], "L": [192, 159, T]}, (0, 7): {"R": [116, 153, T], "L": [191, 153, T]},
    (1, 0): {"R": [116, 167, T], "L": [191, 167, T]}, (1, 1): {"R": [117, 165, T], "L": [191, 167, T]},
    (1, 2): {"R": [117, 167, T], "L": [191, 165, T]}, (1, 3): {"R": [116, 162, T], "L": [191, 162, T]},
    (1, 4): {"R": [116, 165, T], "L": [192, 168, T]}, (1, 5): {"R": [117, 168, T], "L": [192, 168, T]},
    (1, 6): {"R": [116, 161, T], "L": [192, 165, T]}, (1, 7): {"R": [116, 161, T], "L": [192, 165, T]},
    # right: R = near fist (hip -> forward -> hip); L = far fist, seen behind the back on f1-f3 only
    (2, 0): {"R": [158, 170, T], "L": [137, 161, F]}, (2, 1): {"R": [169, 168, T], "L": [122, 161, T]},
    (2, 2): {"R": [181, 165, T], "L": [126, 161, T]}, (2, 3): {"R": [186, 161, T], "L": [117, 157, T]},
    (2, 4): {"R": [177, 167, T], "L": [137, 161, F]}, (2, 5): {"R": [171, 168, T], "L": [137, 161, F]},
    (2, 6): {"R": [180, 162, T], "L": [137, 161, F]}, (2, 7): {"R": [163, 173, T], "L": [137, 161, F]},
    # up-right: R = screen-right, hidden in front of the body when it swings forward (f4, f7)
    (3, 0): {"R": [194, 170, T], "L": [116, 167, T]}, (3, 1): {"R": [198, 165, T], "L": [116, 165, T]},
    (3, 2): {"R": [198, 157, T], "L": [116, 157, T]}, (3, 3): {"R": [204, 151, T], "L": [114, 161, T]},
    (3, 4): {"R": [206, 145, F], "L": [111, 160, T]}, (3, 5): {"R": [200, 161, T], "L": [116, 161, T]},
    (3, 6): {"R": [192, 165, T], "L": [124, 165, T]}, (3, 7): {"R": [203, 147, F], "L": [120, 159, T]},
    (4, 0): {"R": [209, 170, T], "L": [114, 170, T]}, (4, 1): {"R": [203, 167, T], "L": [114, 168, T]},
    (4, 2): {"R": [200, 161, T], "L": [114, 161, T]}, (4, 3): {"R": [200, 156, T], "L": [114, 151, T]},
    (4, 4): {"R": [203, 157, T], "L": [120, 156, T]}, (4, 5): {"R": [203, 161, T], "L": [117, 161, T]},
    (4, 6): {"R": [206, 165, T], "L": [116, 162, T]}, (4, 7): {"R": [206, 168, T], "L": [116, 167, T]},
}
ATTACK = {  # (d8, frame): {...}  (8-row attack, ro_fantasy_attack8_grips.FISTS), read 26/11 on fist-cands-attack-r<d8>.png (3x grids)
    # down: R = screen-left; f1 raised beside the right shoulder, f2-f4 thrust, L at the chest
    (0, 0): {"R": [129, 122, T], "L": [171, 143, T]}, (0, 1): {"R": [119, 95, T], "L": [177, 137, T]},
    (0, 2): {"R": [126, 106, T], "L": [188, 131, T]}, (0, 3): {"R": [135, 123, T], "L": [197, 136, T]},
    (0, 4): {"R": [132, 130, T], "L": [194, 137, T]}, (0, 5): {"R": [129, 146, T], "L": [183, 151, T]},
    (0, 6): {"R": [126, 162, T], "L": [184, 146, T]}, (0, 7): {"R": [116, 168, T], "L": [201, 167, T]},
    # down-right: R raised screen-left (f1), then thrust across the chest to screen-right (f2-f4); L the other fist
    (1, 0): {"R": [136, 138, T], "L": [189, 150, T]}, (1, 1): {"R": [122, 89, T], "L": [196, 135, T]},
    (1, 2): {"R": [191, 130, T], "L": [111, 141, T]}, (1, 3): {"R": [183, 126, T], "L": [95, 134, T]},
    (1, 4): {"R": [193, 137, T], "L": [117, 135, T]}, (1, 5): {"R": [140, 129, T], "L": [198, 135, T]},
    (1, 6): {"R": [138, 159, T], "L": [204, 146, T]}, (1, 7): {"R": [126, 168, T], "L": [201, 165, T]},
    # right: R = the near fist (thrusts), L = far fist in front of the chest
    (2, 0): {"R": [139, 129, T], "L": [182, 137, T]}, (2, 1): {"R": [126, 93, T], "L": [193, 133, T]},
    (2, 2): {"R": [206, 113, T], "L": [182, 144, T]}, (2, 3): {"R": [224, 113, T], "L": [182, 144, T]},
    (2, 4): {"R": [216, 120, T], "L": [183, 144, T]}, (2, 5): {"R": [163, 127, T], "L": [193, 136, T]},
    (2, 6): {"R": [151, 144, T], "L": [184, 142, T]}, (2, 7): {"R": [152, 167, T], "L": [178, 161, F]},
    # up-right: R = screen-right (thrusts up-right), L hidden in front of the body until the guard
    (3, 0): {"R": [194, 116, T], "L": [149, 130, F]}, (3, 1): {"R": [200, 90, T], "L": [149, 130, F]},
    (3, 2): {"R": [230, 97, T], "L": [149, 130, F]}, (3, 3): {"R": [237, 97, T], "L": [149, 130, F]},
    (3, 4): {"R": [234, 107, T], "L": [149, 130, F]}, (3, 5): {"R": [212, 115, T], "L": [149, 130, F]},
    (3, 6): {"R": [194, 145, T], "L": [149, 136, F]}, (3, 7): {"R": [193, 167, T], "L": [122, 151, T]},
    # up: R = screen-right; f0 both fists hidden in front of the body (painted so)
    (4, 0): {"R": [190, 134, F], "L": [128, 136, F]}, (4, 1): {"R": [205, 92, T], "L": [131, 136, F]},
    (4, 2): {"R": [208, 96, T], "L": [131, 136, F]}, (4, 3): {"R": [209, 96, T], "L": [131, 136, F]},
    (4, 4): {"R": [212, 108, T], "L": [131, 136, F]}, (4, 5): {"R": [199, 115, T], "L": [131, 136, F]},
    (4, 6): {"R": [190, 156, T], "L": [122, 150, T]}, (4, 7): {"R": [198, 165, T], "L": [121, 165, T]},
    # up-left: R = screen-right = the FAR arm; it reaches over the head to the up-left (f2-f4), L = the near fist (screen-left)
    (5, 0): {"R": [184, 114, T], "L": [127, 133, T]}, (5, 1): {"R": [194, 76, T], "L": [117, 126, T]},
    (5, 2): {"R": [85, 97, T], "L": [121, 131, T]}, (5, 3): {"R": [80, 95, T], "L": [123, 136, T]},
    (5, 4): {"R": [80, 106, T], "L": [126, 141, T]}, (5, 5): {"R": [107, 115, T], "L": [114, 138, T]},
    (5, 6): {"R": [197, 149, T], "L": [122, 131, T]}, (5, 7): {"R": [189, 161, F], "L": [130, 168, T]},
    # left: R = the FAR fist (thrusts), L = the near fist at the chest
    (6, 0): {"R": [160, 125, F], "L": [123, 136, T]}, (6, 1): {"R": [192, 87, T], "L": [124, 134, T]},
    (6, 2): {"R": [89, 106, T], "L": [134, 136, T]}, (6, 3): {"R": [76, 110, T], "L": [132, 137, T]},
    (6, 4): {"R": [86, 116, T], "L": [134, 135, T]}, (6, 5): {"R": [107, 113, T], "L": [130, 135, T]},
    (6, 6): {"R": [166, 153, F], "L": [134, 134, T]}, (6, 7): {"R": [142, 161, F], "L": [157, 175, T]},
    # down-left: R = screen-left (thrusts to screen-left), L = screen-right
    (7, 0): {"R": [127, 144, T], "L": [162, 141, T]}, (7, 1): {"R": [104, 90, T], "L": [161, 138, T]},
    (7, 2): {"R": [100, 131, T], "L": [181, 144, T]}, (7, 3): {"R": [101, 141, T], "L": [189, 141, T]},
    (7, 4): {"R": [105, 153, T], "L": [186, 138, T]}, (7, 5): {"R": [107, 131, T], "L": [162, 139, T]},
    (7, 6): {"R": [119, 142, T], "L": [161, 138, T]}, (7, 7): {"R": [122, 168, T], "L": [194, 175, T]},
}


def overrides():
    out = {("wizard", "idle", r, 0): v for r, v in IDLE.items()}
    out.update({("wizard", "walk", r, f): v for (r, f), v in WALK.items()})
    return out

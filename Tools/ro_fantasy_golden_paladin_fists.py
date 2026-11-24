"""Golden Paladin TEST look (RO_FANTASY_VARIANT=golden-paladin, 2026-11-24): warrior fists + attack sword table for its own ChatGPT art
(src/golden-paladin/). Same layout as ro_fantasy_motion_fists.WARRIOR_V2 / WARRIOR_V2_ATTACK and ro_fantasy_attack8_grips.SWORD:
R = character's right hand (sword), L = left hand (shield), [x, y, visible] in body-cell px (320 x 256).
Painted rows: 0 down, 1 down-right, 2 right, 3 up-right, 4 up (attack: + 5 up-left, 6 left, 7 down-left).
Base = the glove detector (Tools/golden-paladin-glove-fist-detect.py -> sheets/golden-paladin/warrior-glove-fists.json), then FIX read
by eye on docs/reference/character-v3/golden-paladin-test/check/fists-warrior-<anim>.png.
"""
import json
from pathlib import Path

T, F = True, False
_JSON = Path(__file__).resolve().parent.parent / "Assets/ArtLibrary/ro-fantasy-heroes/sheets/golden-paladin/warrior-glove-fists.json"
_DET = json.loads(_JSON.read_text(encoding="utf-8")) if _JSON.exists() else {}

# (anim, row, frame): {hand: [x, y, visible]} manual fixes over the detector
FIX = {
    # idle: right view = one glove (the left fist hides behind the hip; the detector took a belt bit); up-right: left fist behind the cape
    # idle (face redraw chatgpt-4): gloves wider than the detector's limit on down / right
    ("idle", 0, 0): {"R": [118, 168, T], "L": [202, 168, T]}, ("idle", 1, 0): {"R": [132, 169, T], "L": [199, 165, T]},
    ("idle", 2, 0): {"R": [155, 167, T], "L": [158, 160, F]}, ("idle", 3, 0): {"L": [118, 160, F]},
    # walk down-right f3: the detector took the bracer gem for the left fist (swinging back behind the hip)
    ("walk", 1, 3): {"L": [188, 156, T]},
    # walk right: R = the NEAR arm (pauldron + gem gauntlet drawn over the torso), hip -> forward -> hip; L = the far fist, back over
    # the cape on f1-f5, hidden f0 / f6 / f7 (the detector's first frame had one blob, so it swapped the hands)
    ("walk", 2, 0): {"R": [157, 175, T], "L": [122, 168, F]}, ("walk", 2, 1): {"R": [166, 175, T], "L": [122, 165, T]},
    ("walk", 2, 2): {"R": [182, 160, T], "L": [117, 164, T]}, ("walk", 2, 3): {"R": [189, 151, T], "L": [116, 163, T]},
    ("walk", 2, 4): {"R": [191, 160, T], "L": [116, 161, T]}, ("walk", 2, 5): {"R": [176, 167, T], "L": [121, 165, T]},
    ("walk", 2, 6): {"R": [143, 176, T], "L": [122, 166, F]}, ("walk", 2, 7): {"R": [157, 173, T], "L": [122, 168, F]},
    # attack, 8 Dir8 rows x 8 frames: every fist read by eye on the cut cells (blob detector + numbered sheet). down / down-right /
    # down-left: R = screen-left; right: R = near arm; left: R = far arm (raised behind the head, then chops forward); up / up-right:
    # R = screen-right; up-left: R = screen-right, hidden f4-f7 (ChatGPT swings the near shield arm forward: the sword chops into the screen)
    ("attack", 0, 0): {"R": [109, 172, T], "L": [196, 176, T]}, ("attack", 0, 1): {"R": [96, 132, T], "L": [194, 170, T]},
    ("attack", 0, 2): {"R": [113, 75, T], "L": [199, 164, T]}, ("attack", 0, 3): {"R": [163, 169, T], "L": [205, 169, T]},
    ("attack", 0, 4): {"R": [184, 192, T], "L": [220, 164, T]}, ("attack", 0, 5): {"R": [168, 198, T], "L": [212, 164, T]},
    ("attack", 0, 6): {"R": [108, 163, T], "L": [200, 165, T]}, ("attack", 0, 7): {"R": [103, 171, T], "L": [204, 167, T]},
    ("attack", 1, 0): {"R": [128, 168, T], "L": [201, 170, T]}, ("attack", 1, 1): {"R": [116, 113, T], "L": [201, 157, T]},
    ("attack", 1, 2): {"R": [134, 70, T], "L": [212, 143, T]}, ("attack", 1, 3): {"R": [191, 173, T], "L": [218, 156, T]},
    ("attack", 1, 4): {"R": [235, 189, T], "L": [235, 150, T]}, ("attack", 1, 5): {"R": [196, 190, T], "L": [223, 158, T]},
    ("attack", 1, 6): {"R": [142, 161, T], "L": [210, 158, T]}, ("attack", 1, 7): {"R": [121, 162, T], "L": [201, 162, T]},
    ("attack", 2, 0): {"R": [186, 155, T], "L": [165, 160, F]}, ("attack", 2, 1): {"R": [108, 117, T], "L": [187, 155, T]},
    ("attack", 2, 2): {"R": [119, 68, T], "L": [191, 148, T]}, ("attack", 2, 3): {"R": [234, 155, T], "L": [175, 155, F]},
    ("attack", 2, 4): {"R": [235, 180, T], "L": [170, 158, F]}, ("attack", 2, 5): {"R": [200, 187, T], "L": [208, 157, T]},
    ("attack", 2, 6): {"R": [156, 170, T], "L": [194, 156, T]}, ("attack", 2, 7): {"R": [152, 171, T], "L": [188, 159, T]},
    ("attack", 3, 0): {"R": [195, 176, T], "L": [104, 157, T]}, ("attack", 3, 1): {"R": [204, 108, T], "L": [98, 147, T]},
    ("attack", 3, 2): {"R": [204, 66, T], "L": [93, 150, T]}, ("attack", 3, 3): {"R": [218, 160, T], "L": [85, 147, T]},
    ("attack", 3, 4): {"R": [212, 178, T], "L": [88, 138, T]}, ("attack", 3, 5): {"R": [196, 189, T], "L": [87, 142, T]},
    ("attack", 3, 6): {"R": [195, 174, T], "L": [99, 150, T]}, ("attack", 3, 7): {"R": [194, 174, T], "L": [106, 157, T]},
    ("attack", 4, 0): {"R": [207, 166, T], "L": [110, 166, T]}, ("attack", 4, 1): {"R": [218, 102, T], "L": [104, 140, T]},
    ("attack", 4, 2): {"R": [210, 66, T], "L": [98, 153, T]}, ("attack", 4, 3): {"R": [236, 149, T], "L": [105, 153, T]},
    ("attack", 4, 4): {"R": [244, 157, T], "L": [122, 150, T]}, ("attack", 4, 5): {"R": [226, 167, T], "L": [134, 148, T]},
    ("attack", 4, 6): {"R": [220, 158, T], "L": [109, 161, T]}, ("attack", 4, 7): {"R": [208, 166, T], "L": [110, 165, T]},
    ("attack", 5, 0): {"R": [212, 161, T], "L": [118, 163, T]}, ("attack", 5, 1): {"R": [201, 89, T], "L": [114, 151, T]},
    ("attack", 5, 2): {"R": [204, 59, T], "L": [106, 152, T]}, ("attack", 5, 3): {"R": [228, 118, T], "L": [111, 152, T]},
    ("attack", 5, 4): {"R": [205, 140, F], "L": [104, 158, T]}, ("attack", 5, 5): {"R": [205, 145, F], "L": [114, 154, T]},
    ("attack", 5, 6): {"R": [203, 152, F], "L": [118, 154, T]}, ("attack", 5, 7): {"R": [202, 160, F], "L": [116, 160, T]},
    ("attack", 6, 0): {"R": [150, 160, F], "L": [123, 161, T]}, ("attack", 6, 1): {"R": [200, 99, T], "L": [125, 159, T]},
    ("attack", 6, 2): {"R": [201, 74, T], "L": [114, 157, T]}, ("attack", 6, 3): {"R": [192, 100, T], "L": [113, 171, T]},
    ("attack", 6, 4): {"R": [85, 145, T], "L": [123, 169, T]}, ("attack", 6, 5): {"R": [88, 162, T], "L": [134, 170, T]},
    ("attack", 6, 6): {"R": [150, 160, F], "L": [115, 161, T]}, ("attack", 6, 7): {"R": [150, 160, F], "L": [122, 161, T]},
    ("attack", 7, 0): {"R": [109, 161, T], "L": [190, 156, T]}, ("attack", 7, 1): {"R": [91, 115, T], "L": [212, 142, T]},
    ("attack", 7, 2): {"R": [96, 75, T], "L": [190, 151, T]}, ("attack", 7, 3): {"R": [80, 158, T], "L": [196, 148, T]},
    ("attack", 7, 4): {"R": [76, 174, T], "L": [198, 144, T]}, ("attack", 7, 5): {"R": [100, 186, T], "L": [194, 150, T]},
    ("attack", 7, 6): {"R": [107, 156, T], "L": [198, 156, T]}, ("attack", 7, 7): {"R": [114, 158, T], "L": [190, 152, T]},
}


def _table(anim):
    out = {}
    for key, v in _DET.get(anim, {}).items():
        r, f = (int(s) for s in key.split(","))
        out[(r, f)] = {h: list(p) for h, p in v.items()}
    for (a, r, f), v in FIX.items():
        if a == anim:
            out.setdefault((r, f), {}).update(v)
    return out


WARRIOR = {("warrior", "idle"): _table("idle"), ("warrior", "walk"): _table("walk")}
ATTACK = _table("attack")

# attack sword angle / depth OVERRIDES per (d8, f) (0 = tip up, clockwise) on top of the Knight V3 table of ro_fantasy_attack8_grips
# (same 8-frame chop timing), tuned on the golden 8-dir check sheet
SWORD = {
    (2, 0): 118, (2, 1): -30,  # right: f0 the sword fist rests at the hip like idle (Knight crouched with it pulled back) -> idle angle, in front
    (6, 3): -20,               # left f3: the far fist is still raised over the head (Knight's was already forward) -> blade up-left, not across the face
}
SWORD_BEHIND = {(2, 0): False}
SHIELD_MIN_Y = {0: 168, 1: 172, 7: 168}  # tower shield 108 px (Knight 91): its centre lower so it never covers the face

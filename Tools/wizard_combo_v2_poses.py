"""Frame tables of the Wizard combo v2 (Tools/build-wizard-combo-v2-attacks.py): board 10/11/2026
Assets/ArtLibrary/classes/wizard/attack-board/northwind-combo-v2/wizard-combo-v2-board-chatgpt-1.png.

1 Crystal Thrust (staff jab down at the viewer), 2 Frost Sweep (two-handed horizontal swing), 3 Glacier Slam (two-handed overhead smash
into the ground), 4 Blizzard Nova (staff planted, arms thrown wide). The book is hidden in the two-handed hits.

Staff angle: clockwise degrees on the y-down frame, 0 = crystal up (as Tools/wizard_attack_repaint_poses.py). `at` = the staff's grip point
in frame px instead of the right fist (planted staff while the hands are off it). Angles below are read by `angles` (the staff row of the
ChatGPT image) and rounded; fix a frame here when the game preview disagrees with the painted staff.
FIST_FIX: (anim, dir) -> {painted frame: ((right fist x, y), (left fist x, y))}, frame px after `fit`, read on the `grid` images.
"""

COUNT = {"attack1": 6, "attack2": 6, "attack3": 6, "attack4": 8}

# painted frame shown at each game frame: every strip plays straight through (one sheet frame per painted frame). Hits 1-3: 6 frames over
# 260 ms, the strike (painted 2) shows from 87 ms, the bolt leaves at 70 ms (end of the step frame); hit 4: 8 frames over 470 ms, the burst
# (painted 2) from 118 ms, release at 130 ms.
ORDER = {"attack1": list(range(6)), "attack2": list(range(6)), "attack3": list(range(6)), "attack4": list(range(8))}

CAST_FRAME = 2   # game frame of the release (ORDER): the cast flash sits at the staff crystal of that frame


def _p(staff, book=8, book_hidden=False, at=None, staff_behind=False, slide=0):
    """slide: px the grip moves along the shaft toward the crystal (the T0 staff has 74 px of shaft below the grip: slid 70 the butt sits at the
    fist, so a staff pointed at the viewer does not stick up over the head)."""
    return {"staff": {"angle": staff, "behind": staff_behind, "at": at, "slide": slide}, "book": {"angle": book, "sx": 1, "behind": False, "hidden": book_hidden}}


# filled from `angles` output (see the tool); book: hit 1 keeps it in the raised left fist, hits 2-4 hide it (both hands on the staff)
POSES = {
    ("attack1", "down"): [_p(-5), _p(-14), _p(172, slide=70), _p(172, slide=70), _p(-6), _p(-5)],
    ("attack2", "down"): [_p(-84, book_hidden=True), _p(-85, book_hidden=True), _p(92, book_hidden=True), _p(72, book_hidden=True),
                          _p(-87, book_hidden=True), _p(-4, book_hidden=True)],
    # slam frames: the grip point slides down the shaft (`at`) so the butt stops at the chin and the crystal hits the ground in front of
    # the feet, both fists on the shaft (the art is as tall as the hero; gripped at a fist the butt covered the face)
    ("attack3", "down"): [_p(-2, book_hidden=True), _p(-1, book_hidden=True), _p(180, book_hidden=True, at=(96, 166)),
                          _p(180, book_hidden=True, at=(96, 170)), _p(-1, book_hidden=True), _p(-2, book_hidden=True)],
    # 0, 1, 6: both fists on the staff in front of the chest, tilted out so the crystal clears the face (the art is as tall as the hero);
    # 2-5: the staff planted between the feet, leaning to screen-left, hands off it (`at`); 7: upright in the right hand as idle
    ("attack4", "down"): [_p(-35, book_hidden=True), _p(-35, book_hidden=True)] + [_p(-25, book_hidden=True, at=(80, 120))] * 4
                         + [_p(-35, book_hidden=True), _p(-5, book_hidden=True)],
}

FIST_FIX = {
    # both fists stacked on the vertical staff (one skin blob): R = lower fist, L = upper (`stacked` of the tool)
    ("attack3", "down"): {0: ((96.4, 84.5), (96.0, 62.8)), 1: ((92.6, 125.1), (92.6, 125.1)), 2: ((96.4, 149.6), (97.1, 142.0)),
                          3: ((96.3, 154.5), (92.5, 137.0)), 4: ((95.0, 120.7), (90.0, 101.8))},
    ("attack4", "down"): {0: ((99.9, 131.1), (98.2, 117.0)), 1: ((96.0, 139.7), (97.3, 133.8)), 6: ((99.4, 131.1), (97.9, 117.0))},
}

"""Staff / book poses and fist fixes of the Wizard attack rows (Tools/build-wizard-attack-board-repaint.py).

Angles are clockwise degrees on the y-down frame, 0 = the art upright (staff crystal up, book as at idle facing down: open, spine on the left).
The staff's grip sits on the RIGHT fist, the book's grip (lower inner edge of the cover) on the LEFT fist. behind = drawn under the body
(an item in a far hand that the body hides); items in front get the fist disc of the hand layer on top.
FIST_FIX: (anim, dir) -> {frame: ((right fist x, y), (left fist x, y))}, read on the `grid` images where the skin detection is wrong or
cannot tell near / far (side views).
"""

STAFF_S = 0.447  # every staff anchor of the class (held-wizard-melee.json), so the attack staff is the idle staff
BOOK_S = 0.25    # the idle-down book (Tools/build-wizard-tier-staff-book.py BOOK_W): one book size for every frame and tier


def _p(staff, book, staff_behind=False, book_behind=False, book_sx=1):
    return {"staff": {"angle": staff, "behind": staff_behind}, "book": {"angle": book, "sx": book_sx, "behind": book_behind}}


POSES = {
    # staff thrust: raised, swinging, pointed down at the viewer (crystal low), back
    ("attack1", "down"): [_p(-20, 8), _p(-70, 8), _p(165, 8), _p(-80, 8)],  # frame 2: crystal down toward the viewer, the butt up-left, away from the face
    # near (right) arm: pulled back at the hip, rising, extended (staff level, crystal forward), back; book in the far fist behind the body
    ("attack1", "right"): [_p(100, 20, False, True), _p(60, 20, False, True), _p(90, 20, False, True), _p(70, 20, False, True)],
    # mirrored rows (MIRRORS): the staff fist is the source's book fist
    ("attack2", "down"): [_p(-35, 8)] * 4,  # book thrust forward-down on screen-right, staff held upright at the chest
    ("attack2", "left"): [_p(-60, -20, True, False, -1)] * 4,  # = attack1 right mirrored: near (left) arm thrusts the book, staff in the far fist behind
    # back view: the right fist (screen-right) swings out and up; the staff stays in front of the fist (outside the body), the book at the hidden chest fist
    ("attack1", "up"): [_p(15, -8, False, True), _p(30, -8, False, True), _p(35, -8, False, True), _p(25, -8, False, True)],
    ("attack2", "up"): [_p(10, -20, True, False, -1), _p(10, -30, True, False, -1), _p(10, -35, True, False, -1), _p(10, -25, True, False, -1)],
    # far (left) arm thrusts the book forward (outside the body from frame 1: in front), the near fist keeps the staff low at the hip, crystal forward-down
    ("attack2", "right"): [_p(100, 20, False, True), _p(100, 15, False, False), _p(100, 10, False, False), _p(100, 15, False, False)],
    # = attack2 right mirrored: the far (right) arm thrusts the staff forward (crystal left), the near fist holds the book low at the hip
    ("attack1", "left"): [_p(-30, -20, True, False, -1), _p(-80, -20, False, False, -1), _p(-90, -20, False, False, -1), _p(-75, -20, False, False, -1)],
    # staff raised high beside the head (tilted out so the crystal never crosses the face), book pushed forward at the chest
    ("attack3", "down"): [_p(-40, 8), _p(-20, 8), _p(-10, 8), _p(-10, 8)],
    # near (right) fist rises behind the head: the staff goes behind the body and head, tilted back; the far fist pushes the book forward
    ("attack3", "right"): [_p(60, 20, False, True), _p(-40, 15, True, False), _p(-30, 15, True, False), _p(-30, 15, True, False)],
    # back view: the right fist rises out to the screen-right beside the head (staff in front of it), the book fist pushes forward, hidden
    ("attack3", "up"): [_p(10, -8, True, True), _p(20, -8, False, True), _p(25, -8, False, True), _p(20, -8, False, True)],
    # facing left: the far (right) fist rises behind the head, staff behind the body tilted back; the near fist pushes the book forward
    ("attack3", "left"): [_p(-60, -20, True, False, -1), _p(40, -15, True, False, -1), _p(30, -15, True, False, -1), _p(30, -15, True, False, -1)],
    # both fists gather at the chest (staff tilted out, clear of the face), then push together: staff held across the belly (crystal out to screen-left, the butt never crosses the face)
    ("attack4", "down"): [_p(-45, 8), _p(-35, 8), _p(-100, 0), _p(-95, 0), _p(-100, 0), _p(-60, 8)],
    # near (right) fist: gather, pull back, then both fists push forward together (staff level, crystal forward); book in the far fist behind the body
    ("attack4", "right"): [_p(40, 20, False, True), _p(80, 20, False, True), _p(85, 15, False, True), _p(85, 15, False, True), _p(85, 15, False, True),
                           _p(50, 20, False, True)],
    # = attack4 right mirrored: the staff in the far (right) fist behind the body, the book in the near fist in front
    ("attack4", "left"): [_p(-40, -20, True, False, -1), _p(-80, -20, True, False, -1), _p(-85, -15, True, False, -1), _p(-85, -15, True, False, -1),
                          _p(-85, -15, True, False, -1), _p(-50, -20, True, False, -1)],
    # back view: elbows out, then both fists push up beside the neck (staff out on the screen-right, book opening out to the screen-left, clear of the hair)
    ("attack4", "up"): [_p(10, -8, True, True), _p(10, -8, True, True), _p(15, -15, False, False, -1), _p(15, -15, False, False, -1),
                        _p(15, -15, False, False, -1), _p(10, -8, True, True)],
}

FIST_FIX = {
    ("attack4", "up"): {0: ((117.1, 114.2), (68.8, 115.5)), 1: ((113.3, 123.7), (73.0, 124.1)), 5: ((117.7, 115.9), (66.0, 115.2))},
    ("attack4", "right"): {0: ((101.0, 128.6), (125.5, 122.1)), 1: ((76.0, 135.3), (126.9, 126.0)), 2: ((145.0, 113.0), (141.0, 107.0)),
                           3: ((148.0, 112.0), (144.0, 106.0)), 4: ((144.0, 121.0), (148.0, 106.7)), 5: ((101.2, 124.0), (125.5, 116.8))},
    ("attack3", "up"): {0: ((118.0, 118.0), (72.0, 118.0)), 1: ((131.1, 98.6), (70.0, 120.0)), 3: ((134.5, 90.6), (66.0, 122.0))},
    ("attack3", "left"): {0: ((96.0, 120.0), (78.8, 124.0)), 1: ((125.0, 99.6), (58.7, 118.3)), 2: ((136.5, 82.3), (52.2, 113.9)),
                          3: ((135.2, 87.2), (51.9, 114.0))},
    ("attack3", "right"): {0: ((107.9, 120.6), (124.9, 121.1)), 1: ((84.3, 103.8), (137.3, 116.9)), 2: ((63.0, 84.5), (148.2, 113.0)),
                           3: ((67.0, 92.2), (149.2, 112.9))},
    ("attack2", "right"): {1: ((91.5, 145.5), (136.0, 108.1)), 2: ((90.9, 145.5), (151.1, 106.9)), 3: ((91.5, 145.6), (133.1, 109.9))},
    # back view: the left fist is at the chest, hidden; detection picked the right wrist
    ("attack1", "up"): {1: ((131.1, 113.8), (68.0, 124.0)), 2: ((135.9, 95.7), (68.0, 124.0))},
    # side views: the skin detection cannot tell the near (right) fist from the far (left) one
    ("attack1", "right"): {0: ((80.8, 144.4), (128.0, 125.1)), 1: ((129.7, 112.3), (129.8, 126.3)), 2: ((155.4, 107.0), (125.8, 125.4)),
                           3: ((108.9, 129.1), (132.6, 126.5))},
}

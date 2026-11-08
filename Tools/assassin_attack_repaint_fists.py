"""Fist table of the Assassin basic attack repaint (Tools/build-assassin-attack-repaint.py), read on its `grid` images (cell px, 192 cell).
FISTS[(anim, dir)] = 6 frames of ((main x, y, angle, behind), (off x, y, angle, behind)).
main = the dagger in her RIGHT hand, off = the LEFT hand. Facing down her right hand is on screen-left; facing up on screen-right;
facing right the near arm (drawn over the body) is the right hand. Facing left is the right row mirrored (the tool swaps main / off).
angle: dagger tip direction, degrees clockwise from straight up (90 = tip to the right, 180 = tip down), as in held-assassin-melee.json.
hidden: optional 5th item True = no dagger in that hand this frame (fist not painted).
behind: drawn behind the body (the far hand when it is behind the torso); a front fist gets a fist-layer disc over the hilt.
"""
FISTS = {
    # melee1 Crescent Cut: right fist back (wind-up), sweeps across, out to the far side, back to ready; left fist low guard
    ("melee1", "down"): [((62, 143.5, -40, False), (131, 145, 115, False)), ((104.5, 128, 130, False), (133, 145, 115, False)),
                         ((143, 119, 100, False), (131, 146, 115, False)), ((106.5, 133, 120, False), (131, 148, 115, False)),
                         ((101.5, 136, 150, False), (130, 146, 115, False)), ((71, 143.5, -115, False), (124, 143.5, 115, False))],
    ("melee1", "right"): [((82, 140.5, -80, False), (148, 148, 122, True)), ((150.5, 122, 90, False), (132, 146, 122, True)),
                          ((182, 125.5, 100, False), (143, 145, 122, True)), ((164, 133.5, 120, False), (134, 143.5, 122, True)),
                          ((109, 143.5, 160, False), (129, 145, 122, True)), ((89.5, 143.5, -107.5, False), (130, 143.5, 122, True))],
    # up (v3 strip, 2026-11-08): only the right arm (picture-right) moves, an outward horizontal slash; v1 and v2 swung the left arm
    ("melee1", "up"): [((122, 133, 150, True), (65, 144.5, -115, False)), ((145, 112, 60, False), (66, 145, -115, False)),
                       ((157.5, 118, 80, False), (61, 144, -115, False)), ((153, 133, 100, False), (62, 144, -115, False)),
                       ((132, 144, 127.5, False), (64, 144, -115, False)), ((130, 144, 127.5, False), (64, 144, -115, False))],
    # melee2 Rising Fang: left fist from the knee up high beside the head; right fist guards at the chest
    ("melee2", "down"): [((69, 145, -100, False), (131, 175.5, 105, False)), ((81.5, 128, -80, False), (143, 142, 110, False)),
                         ((74, 105, -70, False), (140, 64.5, 30, False)), ((73.5, 113.5, -75, False), (148, 74.5, 20, False)),
                         ((80, 131, -120, False), (113, 131, 120, False)), ((65, 141, -115, False), (127, 141, 115, False))],
    ("melee2", "right"): [((118, 146, -110, False, True), (154, 165.5, 120, False)), ((109, 148, -110, False), (153.5, 134, 80, False)),
                          ((115, 115, -130, False), (158.5, 69, 20, False)), ((106.5, 115, -130, False), (160, 72, 10, False)),
                          ((94.5, 142, -110, False), (150.5, 119, 70, False)), ((91, 145, -107.5, False), (131, 138, 122, True))],
    ("melee2", "up"): [((131, 154, 127.5, False), (52, 162.5, -130, False)), ((130.5, 144, 127.5, False), (42.5, 129, -95, False)),
                       ((125.5, 128, 127.5, False), (43.5, 67.5, -20, False)), ((122, 134, 127.5, False), (52, 67.5, -15, False)),
                       ((132, 144, 127.5, False), (47, 109, -70, False)), ((131, 142, 127.5, False), (56.5, 144, -115, False))],
    # melee3 Cross Rend: both fists up beside the head, chop in, crossed X low, flung out wide, back to ready
    ("melee3", "down"): [((55, 89, -20, False), (144, 92, 20, False)), ((84.5, 141, 150, False), (106, 141, -150, False)),
                         ((82, 158, 145, False), (108, 158, -145, False)), ((50, 152.5, -100, False), (142.5, 152.5, 100, False)),
                         ((64, 141, -120, False), (130.5, 141, 120, False)), ((65, 142.5, -115, False), (127, 142.5, 115, False))],
    ("melee3", "right"): [((69, 96, -100, False), (86, 79, -60, True)), ((153.5, 134, 140, False), (148, 136, 130, True)),
                          ((140, 161, 160, False), (115.5, 161, 120, False)), ((157, 167, 110, False), (142.5, 167, 120, False)),
                          ((88.5, 145, -150, False), (132, 147, 122, True)), ((88, 144, -107.5, False), (128.5, 144, 122, True))],
    ("melee3", "up"): [((148, 95, 20, False), (46, 95, -20, False)), ((127.5, 131, -150, False), (61, 119, 150, False)),
                       ((111, 145, -145, True), (62, 144, 145, True)), ((145.5, 155, 100, False), (47.5, 155, -100, False)),
                       ((129, 135, 127.5, False), (65.5, 135, -115, False)), ((133, 144, 127.5, False), (55, 144, -115, False))],
    # melee4 Shadow Lunge: crouch, right stab, left stab, both together, recover (facing down the stabs point at the viewer: tips down)
    ("melee4", "down"): [((65, 139.5, -100, False), (132.5, 139.5, 100, False)), ((91.5, 136.5, 180, False), (145, 138, 115, False)),
                         ((48, 129.5, -115, False), (91, 134, 180, False)), ((79, 142.5, 172, False), (121, 142.5, -172, False)),
                         ((64, 141, -120, False), (129, 141, 120, False)), ((63.5, 142.5, -115, False), (130, 142.5, 115, False))],
    # right (v2 strip, 2026-11-08): near arm stabs (f2), far arm stabs (f3: the side view shows the far arm in front of the chest, so its
    # dagger is drawn in front), both together (f4: far fist higher, drawn behind its own painted fist)
    ("melee4", "right"): [((128, 147, -110, False), (118, 145, -100, True)), ((182, 122, 90, False), (86, 144, -110, False)),
                          ((85, 144, -120, False), (182, 122, 90, False)), ((174, 135, 95, False), (184, 116.5, 85, True)),
                          ((128, 147, 160, False), (89, 150, -130, True)), ((90, 144, -107.5, False), (129, 144, 122, True))],
    # up: stabbing away from the viewer, the tips point up and outward so they clear the hair
    ("melee4", "up"): [((129.5, 142.5, 127.5, False), (59, 144, -115, False)), ((165, 109, 25, False), (81.5, 141, -115, False)),
                       ((170, 141, 127.5, False), (73.5, 109, -25, False)), ((164, 109, 20, False), (73.5, 110.5, -20, False)),
                       ((130.5, 145, 127.5, False), (63, 145, -115, False)), ((134, 144, 127.5, False), (55, 144, -115, False))],
}

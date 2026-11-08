"""Fist centres per frame of the repainted Mechanic attack rows (Tools/build-mechanic-attack-down-repaint.py), read on its `grid` output
(8 px grid, about 2 px accurate), in the fitted 192 cell.
attack1-3: FISTS[(anim, dir)][frame] = (gun fist, gunL fist): gun = the hero's RIGHT hand (screen left facing down, screen right facing
up, the near hand facing right), gunL = his left hand; None = not visible.
attack4: FISTS[(anim, dir)][frame] = (rear fist, front fist): the cannon barrel runs through both, CANNON_LINE says where along it.
Mirrored rows (left, up attack2) are not listed: the tool mirrors their source row.
"""
# Rows in NORMALIZE of the tool (head shrunk to idle's size, body scaled to idle's length, 2026-11-08) were re-read through that fit.
FISTS = {
    ("attack1", "down"): [[(74, 119), (128, 139)], [(78, 108), (127, 136)], [(74, 98), (128, 136)], [(68, 135), (124, 136)]],
    ("attack2", "down"): [[(66, 139), (114, 122)], [(64, 139), (112, 107)], [(67, 137), (112, 99)], [(62, 139), (114, 137)]],
    # v2 (fists at chest height, below the chin; v1 at face height made the two pistols cover his face)
    ("attack3", "down"): [[(72, 124), (110, 125)], [(72, 120), (109, 120)], [(72, 120), (110, 120)], [(72, 119), (106, 119)], [(72, 120), (110, 120)], [(64, 138), (114, 138)]],
    # facing right: attack1 = the near (right) arm from the gem shoulder, in front of the chest (repainted 2026-11-08: the old row stretched the far
    # arm like attack2, the user saw both hits use the same hand); the far hand is not drawn, its pistol hidden (behind the body at the hip it peeked out between the legs like a second shooting gun)
    ("attack1", "right"): [[(143, 90), None], [(167, 91), None], [(145, 84), None], [(165, 92), None]],
    # attack2 = the far (left) arm stretched out, the near fist at the hip
    ("attack2", "right"): [[(68, 126), (146, 91)], [(68, 127), (164, 91)], [(67, 127), (144, 86)], [(67, 127), (163, 90)]],
    # facing up (back view): attack1 = the right arm (screen right) raised, the left fist at the hip
    ("attack1", "up"): [[(147, 84), (59, 125)], [(142, 76), (57, 125)], [(142, 72), (59, 125)], [(140, 79), (59, 125)]],
    # attack3 up: both fists raised beside the head (screen left = the left hand)
    ("attack3", "up"): [[(136, 102), (49, 102)], [(137, 81), (48, 81)], [(137, 81), (51, 81)], [(136, 71), (41, 71)], [(137, 81), (48, 81)], [(136, 121), (46, 121)]],
    # attack4 (rear fist, front fist). Down: shoulder fist (screen right, high), front fist lower, toward the viewer.
    ("attack4", "down"): [[(140, 98), (109, 129)], [(135, 88), (129, 125)], [(130, 77), (136, 108)], [(136, 88), (127, 125)], [(135, 88), (127, 125)], [(131, 112), (141, 136)]],
    # Right: near fist behind, far fist forward, both at chest height (the barrel level, recoil tips it up)
    ("attack4", "right"): [[(132, 114), (152, 114)], [(123, 92), (149, 92)], [(89, 96), (119, 84)], [(124, 92), (149, 92)], [(127, 93), (151, 93)], [(101, 118), (138, 114)]],
    # old row, head-normalised: pistol anchors moved with it
    ("attack3", "right"): [[(143, 108), (147, 102)], [(142, 108), (147, 102)], [(144, 109), (148, 104)], [(151, 92), (153, 86)], [(129, 109), (134, 104)], [(133, 108), (138, 102)]],
}
# Facing right (left = mirrored by the tool): the shooting hand holds the side art pointing right (angle 0), the other hangs forward-down.
_AIM = dict(angle=0.0, sx=1.0, behind=False)
SIDE_POSE = {
    "attack1": {"right": {"gun": _AIM, "gunL": dict(angle=60.0, sx=1.0, behind=True)}},
    "attack2": {"right": {"gunL": _AIM, "gun": dict(angle=60.0, sx=1.0, behind=False)}},
}
# attack4 cannon along the fist line: art, rear fist at `frac` of the barrel from the tail, drawn behind the body or not, length (foreshortening)
CANNON_LINE = {
    "down": dict(art="front", frac=[0.5, 0.15, 0.15, 0.15, 0.15, 0.3], behind=[True, False, False, False, False, False], length=0.8),
    "right": dict(art="side", frac=[0.4, 0.25, 0.25, 0.25, 0.25, 0.3], behind=False),
}

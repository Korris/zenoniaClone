# Hand fixes on top of the colour classifier, found by eye on the gridded ground-only image (see visuals/*-overlay*.png).
OVERRIDES = {}
for tx in range(36):
    OVERRIDES[(tx, 0)] = "C"                                   # top border (the river stays w below)
for tx in (17, 18, 19): OVERRIDES[(tx, 0)] = "w"
for ty in range(24): OVERRIDES[(0, ty)] = "C"                  # west border column
OVERRIDES[(1, 12)] = "s"                                       # blue shadow, not water
for tx in range(17, 22): OVERRIDES[(tx, 12)] = "B"             # bridge deck (as the old rows: one row, cols 17-21)
for ty in (9, 10, 11):                                         # west stairs down the cliff
    for tx in (5, 6): OVERRIDES[(tx, ty)] = "s"
for tx in (29, 30):                                            # east stairs up the cliff (SE field <-> NE plateau)
    for ty in (12, 13): OVERRIDES[(tx, ty)] = "s"
for tx in (32, 33):                                            # north-east stairs and the path above them
    for ty in (2, 3, 4): OVERRIDES[(tx, ty)] = "s"
for tx in (13, 14, 15):                                        # enclosed cliff-top island: unreachable, keep it solid so nothing spawns there
    for ty in (14, 15): OVERRIDES[(tx, ty)] = "C"

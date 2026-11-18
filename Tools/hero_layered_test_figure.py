"""Synthetic layered hero drawn by the spec's colour rules (no real art exists yet): a bald chibi body on #00FF00 with a magenta outfit,
a grey hair layer, a hat, a sword and a shield. Used by build-hero-recolor-mask.py selftest / test-sheets to try the ramp and the layered
renderer before ChatGPT art arrives. Simple shapes on purpose; the colours are slightly OFF the exact hexes, like ChatGPT art.

Cell 320 x 256, feet on row 244, figure ~200 px with hair. Painted dirs (sheet rows): 0 down, 1 down-right, 2 right, 3 up-right, 4 up.
"""
from PIL import Image, ImageDraw

CW, CH, FEET = 320, 256, 244
GREEN = (0, 255, 0, 255)
OUTLINE = (58, 36, 24, 255)        # #3A2418
SKIN, SKIN_SH = (255, 226, 200, 255), (242, 188, 154, 255)
BLUSH = (246, 168, 120, 255)       # orange-ish, must stay out of the outfit mask
CREAM = (244, 238, 220, 255)
LEATHER, LEATHER_D = (138, 90, 52, 255), (94, 58, 30, 255)
PANTS, PANTS_D = (107, 91, 78, 255), (74, 62, 53, 255)
EYE = (60, 34, 22, 255)
MAG_L, MAG_M, MAG_D = (250, 146, 248, 255), (238, 14, 236, 255), (156, 6, 162, 255)   # near #FF8CFF / #FF00FF / #A000A0
HAIR = [(36, 36, 36, 255), (85, 85, 85, 255), (153, 153, 153, 255), (224, 224, 224, 255)]
HEAD_RX, HEAD_RY = 44, 42
HAIR_CELL, HAIR_AX, HAIR_AY = (160, 128), 80, 44
# per painted dir: body half width, head x shift, eyes, right-hand x / left-hand x offsets from the centre, which hand is drawn behind
DIRS = [
    dict(bw=32, hx=0, eyes=(-16, 16), rh=-44, lh=44, behind=None),
    dict(bw=29, hx=6, eyes=(-4, 22), rh=-34, lh=36, behind='l'),
    dict(bw=24, hx=8, eyes=(26,), rh=10, lh=-12, behind='l'),
    dict(bw=29, hx=4, eyes=(), rh=36, lh=-34, behind='r'),
    dict(bw=32, hx=0, eyes=(), rh=44, lh=-44, behind='rl'),
]
WALK_LEG = [(-7, 5), (0, 0), (5, -7), (0, 0)]   # (left leg dy, right leg dy) per walk frame
WALK_BOB = [0, -3, 0, -3]
SWING = [(-28, -34), (8, -6), (34, 4), (10, 0)]  # attack1 right-hand offset (dx toward the facing, dy) per frame
SWING_ANGLE = [-70, 20, 90, 35]                  # sword angle per attack frame (clockwise, 0 = up), facing right


def new_cell():
    return Image.new('RGBA', (CW, CH), GREEN)


def body_frame(d, anim, f):
    """(body image, hand image, head anchor dict, hands dict) for painted dir d."""
    p = DIRS[d]
    img, hand = new_cell(), Image.new('RGBA', (CW, CH), (0, 0, 0, 0))
    g = ImageDraw.Draw(img)
    bob = WALK_BOB[f] if anim == 'walk' else (2 if anim == 'attack1' and f == 2 else 0)
    cx = 160
    ll, rl = WALK_LEG[f] if anim == 'walk' else (0, 0)
    face = 1 if d in (1, 2, 3) else 0
    # hands: right hand moves on attack1 toward the facing
    rhx, rhy = cx + p['rh'], 182 + bob
    if anim == 'attack1':
        sx, sy = SWING[f]
        rhx += sx * (face if face else -1 if d == 0 else 1)
        rhy += sy
    lhx, lhy = cx + p['lh'], 182 + bob
    hands = {'r': (rhx, rhy), 'l': (lhx, lhy)}
    behind = p['behind'] or ''
    for side in 'rl':
        if side in behind:
            arm(g, cx, 142 + bob, hands[side], dark=True)
    # legs + boots
    for i, (lx, dy) in enumerate(((cx - 13, ll), (cx + 13, rl))):
        lx += 4 * face
        g.rectangle([lx - 9, 194 + bob, lx + 9, 228 + dy * 0], fill=PANTS if i == 0 else PANTS_D, outline=OUTLINE, width=3)
        g.rounded_rectangle([lx - 11 + 3 * face, 222 + dy, lx + 11 + 5 * face, FEET], 5, fill=LEATHER, outline=OUTLINE, width=3)
    # tunic: trapezoid, light left third, mid, dark right third + hem
    bw = p['bw']
    top, bot = 134 + bob, 200 + bob
    poly = [(cx - bw + 6, top), (cx + bw - 6, top), (cx + bw + 4, bot), (cx - bw - 4, bot)]
    g.polygon(poly, fill=MAG_M)
    g.polygon([(cx - bw + 6, top), (cx - bw // 3, top), (cx - bw // 3 - 4, bot), (cx - bw - 4, bot)], fill=MAG_L)
    g.polygon([(cx + bw // 3, top), (cx + bw - 6, top), (cx + bw + 4, bot), (cx + bw // 3 + 4, bot)], fill=MAG_D)
    g.rectangle([cx - bw - 2, bot - 8, cx + bw + 2, bot - 1], fill=MAG_D)
    g.polygon(poly, outline=OUTLINE, width=3)
    if d <= 1:
        g.polygon([(cx - 10 + p['hx'], top), (cx + 10 + p['hx'], top), (cx + p['hx'], top + 16)], fill=CREAM, outline=OUTLINE)
    g.rectangle([cx - bw - 1, 182 + bob, cx + bw + 1, 190 + bob], fill=LEATHER, outline=OUTLINE, width=2)
    for side in 'rl':
        if side not in behind:
            arm(g, cx, 142 + bob, hands[side], dark=False)
    # head (bald): skull, ears, eyes, blush
    hx, hy = cx + p['hx'], 98 + bob
    if d in (0, 1, 2):
        g.ellipse([hx - HEAD_RX - 6 + (14 if d == 2 else 0), hy - 6, hx - HEAD_RX + 8 + (14 if d == 2 else 0), hy + 10], fill=SKIN_SH, outline=OUTLINE, width=2)
    g.ellipse([hx - HEAD_RX, hy - HEAD_RY, hx + HEAD_RX, hy + HEAD_RY], fill=SKIN, outline=OUTLINE, width=3)
    for ex in p['eyes']:
        g.ellipse([hx + ex - 5, hy + 2, hx + ex + 5, hy + 16], fill=EYE)
        g.ellipse([hx + ex - 2, hy + 4, hx + ex + 1, hy + 8], fill=(255, 255, 255, 255))
    for ex in p['eyes']:
        g.ellipse([hx + ex - 7, hy + 18, hx + ex + 7, hy + 24], fill=BLUSH)
    if d >= 3:
        g.arc([hx - 20, hy + 20, hx + 20, hy + 40], 20, 160, fill=SKIN_SH, width=3)  # nape line
    # fist overlay of the weapon hand (drawn in front of the sword): the right hand when it is not behind the body
    if 'r' not in behind:
        fist(ImageDraw.Draw(hand), hands['r'])
    head = dict(x=hx, y=hy - HEAD_RY, w=2 * HEAD_RX, angle=0)
    return img, hand, head, hands


def arm(g, cx, sy, hand, dark):
    hx, hy = hand
    sx = cx + (22 if hx > cx else -22)
    g.line([(sx, sy), (hx, hy)], fill=OUTLINE, width=17)
    g.line([(sx, sy), (hx, hy)], fill=MAG_D if dark else MAG_M, width=11)
    fist(g, hand, dark)


def fist(g, hand, dark=False):
    hx, hy = hand
    g.ellipse([hx - 9, hy - 9, hx + 9, hy + 9], fill=SKIN_SH if dark else SKIN, outline=OUTLINE, width=3)


def hair_cell(d):
    """Grey hair layer, painted dir d, anchor (HAIR_AX, HAIR_AY) = skull top centre, drawn for a skull 88 px wide."""
    img = Image.new('RGBA', HAIR_CELL, GREEN)
    g = ImageDraw.Draw(img)
    ax, ay, w = HAIR_AX, HAIR_AY, 2 * HEAD_RX
    o, sh, base, hi = HAIR
    shift = [0, -6, -12, -4, 0][d]
    if d >= 3:  # back of the head: hair down to the nape
        g.ellipse([ax - w // 2 - 5, ay - 10, ax + w // 2 + 5, ay + 76], fill=base, outline=o, width=3)
        g.ellipse([ax - w // 2 + 4, ay + 34, ax + w // 2 - 4, ay + 74], fill=sh)
    else:
        g.chord([ax - w // 2 - 6 + shift, ay - 12, ax + w // 2 + 6 + shift, ay + 64], 180, 360, fill=base, outline=o, width=3)
        spikes = [(ax - w // 2 - 4 + shift, ay + 24)]
        for i in range(7):
            x = ax - w // 2 + shift + i * (w + 8) // 7
            spikes += [(x + 6, ay + 34 + (i % 2) * 8), (x + 12, ay + 24)]
        g.polygon(spikes + [(ax + w // 2 + 6 + shift, ay + 20), (ax + shift, ay + 6)], fill=base, outline=o)
        if d == 2:
            g.polygon([(ax - w // 2 - 6, ay + 20), (ax - 10, ay + 22), (ax - 30, ay + 60), (ax - w // 2 - 2, ay + 56)], fill=sh, outline=o)
    for t in range(3):
        g.line([(ax - 30 + shift + t * 14, ay + 2), (ax - 22 + shift + t * 14, ay - 6)], fill=o, width=3)
    g.arc([ax - 30 + shift, ay - 4, ax + 10 + shift, ay + 22], 200, 300, fill=hi, width=5)
    g.arc([ax - 10 + shift, ay + 2, ax + 34 + shift, ay + 30], 300, 360, fill=sh, width=4)
    return img


def hat_cell(d):
    """Test headgear (real colours, not recoloured): a blue pointed hat; seat = bottom centre (80, 112) of a 160 x 128 cell."""
    img = Image.new('RGBA', HAIR_CELL, GREEN)
    g = ImageDraw.Draw(img)
    tip = [80, 74, 64, 92, 80][d]
    g.polygon([(34, 104), (126, 104), (tip + 8, 6), (tip - 2, 8)], fill=(52, 92, 176, 255), outline=OUTLINE)
    g.polygon([(56, 104), (126, 104), (tip + 8, 6)], fill=(36, 68, 140, 255))
    g.rectangle([40, 88, 120, 100], fill=(232, 186, 64, 255), outline=OUTLINE, width=2)
    g.ellipse([14, 98, 146, 120], fill=(44, 80, 160, 255), outline=OUTLINE, width=3)
    return img


def sword():
    """40 x 170 upright sword, grip point (20, 146)."""
    img = Image.new('RGBA', (40, 170), (0, 0, 0, 0))
    g = ImageDraw.Draw(img)
    g.polygon([(20, 2), (28, 16), (28, 128), (12, 128), (12, 16)], fill=(222, 228, 238, 255), outline=OUTLINE)
    g.line([(20, 10), (20, 124)], fill=(150, 160, 178, 255), width=3)
    g.rectangle([2, 128, 38, 136], fill=(230, 180, 60, 255), outline=OUTLINE, width=2)
    g.rectangle([15, 136, 25, 160], fill=(110, 64, 36, 255), outline=OUTLINE, width=2)
    g.ellipse([12, 158, 28, 169], fill=(206, 48, 48, 255), outline=OUTLINE, width=2)
    return img


def shield():
    """64 x 76 round shield, grip at its centre (32, 38)."""
    img = Image.new('RGBA', (64, 76), (0, 0, 0, 0))
    g = ImageDraw.Draw(img)
    g.ellipse([1, 1, 62, 74], fill=(232, 186, 64, 255), outline=OUTLINE, width=3)
    g.ellipse([8, 8, 55, 67], fill=(186, 40, 44, 255))
    g.polygon([(32, 18), (40, 38), (32, 58), (24, 38)], fill=(246, 240, 226, 255))
    return img

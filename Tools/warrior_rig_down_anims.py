"""Keyframes of the Warrior facing-camera TEST rig, from the design chat's "Starward Knight V3 - Spine Rig Animation Spec V1.0"
(ref/rig-down/animation-spec-chatgpt.md, Tay, 25/11). Units: angle = screen degrees clockwise, x / y = cell px (y DOWN; the spec's
"hip Y -2.4" = lower = +2.4 here), the rig is 216 px tall like the spec's H. Bone names: L = SCREEN left = the Knight's RIGHT (sword) side.

Adaptations (front view has no depth axis; round-2 changes are noted at WALK and SLASH):
- leg "forward angle" f (spec thigh angle, + = forward) -> foot lower + limb a little longer (toward camera); knee bend k -> foot lift
  (1 - cos k) * 55 px and a slightly shorter shin. The spec's knee column is swapped at passing / up (it bends the stance leg); here the
  SWING leg gets the big knee (25 deg at passing).
- slash: spec timing / easing / body twist / shield / cape kept (0, 45, 85, 120 impact, 175, 240, 330, 400 ms); the arm path follows the
  approved frame slash instead of the spec's signs: raised on the sword side (screen top-left), chop down across to the lower right; the
  sword turns one continuous clockwise circle (cocked up-left -> 170 deg smear in 35 ms -> follow-through -> back to the idle angle).
Key easing `e`: 0 Catmull-Rom, 1 linear, 2 ease-in, 3 ease-out, 4 hold.
"""
import math

SWORD_IDLE = -131.4  # grip angle of the idle-down frame art


def bk(n, a=0.0, x=0.0, y=0.0, sx=1.0, sy=1.0):
    return {"n": n, "a": round(a, 3), "x": round(x, 3), "y": round(y, 3), "sx": round(sx, 4), "sy": round(sy, 4)}


def idle():
    """1600 ms breath: chest up at 25 %, down at 75 %; head / pads / arms / cape later in phase (lags)."""
    keys = []
    for t in (0.0, 0.25, 0.5, 0.75):
        s = math.sin(2 * math.pi * t)  # 0, 1, 0, -1
        keys.append({"t": t, "e": 0, "wb": False, "wa": 0.5 * s, "b": [
            bk("torso", y=-1.1 * s, sy=1 + 0.005 * s),
            bk("head", a=0.4 * s, y=-0.7 * s * 0.4),            # head rides the chest; only a little extra
            bk("padL", a=-0.6 * s, y=-0.5 * s), bk("padR", a=0.6 * s, y=-0.5 * s),
            bk("upperArmL", a=0.4 * s), bk("upperArmR", a=-0.4 * s),
            bk("forearmL", a=0.7 * s, x=-0.4 * s), bk("forearmR", a=-0.7 * s, x=0.4 * s),
            bk("capeM", a=0.8 * s), bk("capeL", a=1.3 * s), bk("capeR", a=-1.3 * s),
        ]})
    lags = [{"n": "head", "v": 100 / 1600}, {"n": "padL", "v": 60 / 1600}, {"n": "padR", "v": 60 / 1600},
            {"n": "forearmL", "v": 120 / 1600}, {"n": "forearmR", "v": 120 / 1600},
            {"n": "capeM", "v": 180 / 1600}, {"n": "capeL", "v": 220 / 1600}, {"n": "capeR", "v": 220 / 1600}]
    return {"name": "idle", "ms": 1600, "loop": True, "keys": keys, "lags": lags}


# walk: (t, legL f, legL k, legR f, legR k, hip y spec)  legL = Knight's right leg.
# Round 2 (design chat 25/11, "bước có trọng lượng hơn"): thigh +-16 -> +-21 deg, swing knee 25 -> 32, hip down -2.4 -> -3.0, up +2 -> +2.5.
WALK_F = 19.0 / 16.0   # round 3 (final): +-21 -> +-19 deg, swing knee stays 32
WALK_SWING_K = 32.0 / 25.0
WALK_RAW = [  # round-1 table (spec V1.0, swing knee column fixed); `s` marks the swing leg of each key
    (0.000, 16, 5, -16, 10, 0.0, "R"),
    (0.125, 9, 17, -12, 20, -2.4, "R"),
    (0.250, -3, 7, 3, 25, 0.5, "R"),
    (0.375, -13, 4, 13, 12, 2.0, "R"),
    (0.500, -16, 10, 16, 5, 0.0, "L"),
    (0.625, -12, 20, 9, 17, -2.4, "L"),
    (0.750, 3, 25, -3, 7, 0.5, "L"),
    (0.875, 13, 12, -13, 4, 2.0, "L"),
]
HIP_R2 = {-2.4: -3.0, 2.0: 2.5}
WALK = [(t, fl * WALK_F, kl * (WALK_SWING_K if sw == "L" else 1.0), fr * WALK_F, kr * (WALK_SWING_K if sw == "R" else 1.0), HIP_R2.get(h, h))
        for t, fl, kl, fr, kr, h, sw in WALK_RAW]
TORSO_TILT = 2.5   # round 2: +-1.5 -> +-2.5 deg


def leg(side, f, k, stance=False, root_x=0.0):
    """Front view: the swing knee lifts the boot (32 deg = 18 px; the frame walk lifts ~12-15 px) and foreshortens the thigh
    (it comes toward the camera); forward f puts the foot lower (0.45 px / deg) with a slightly longer limb; the lifted shin turns out a little.
    Round 3: the stance foot is held on the ground (its forward / back slide cut to 35 %, the hip side shift cancelled)."""
    out = 1.0 if side == "L" else -1.0   # + angle = foot outward for the screen-left leg
    lift = max(0.0, k - 12) * 0.9          # stance knees (<= 17 deg) only absorb the hip drop; the swing knee lifts the boot
    fy = 0.45 * f * (0.35 if stance else 1.0)
    return [bk("thigh" + side, a=out * k * 0.08, sy=1 + 0.004 * f - 0.006 * max(0.0, k - 10)),
            bk("shin" + side, a=out * k * 0.12, x=-root_x if stance else 0.0, y=fy - lift, sy=1 + 0.003 * f - 0.002 * k)]


def walk():
    keys = []
    for t, fl, kl, fr, kr, hip in WALK:
        c = math.cos(2 * math.pi * t)      # +1 when the Knight's right leg is forward
        s = math.sin(2 * math.pi * t)
        rx = -2.0 * s                      # round 3: hip side shift +-2 px over the stance leg
        swing_r = t < 0.5                  # first half: the Knight's left leg (screen right) swings
        b = [bk("root", x=rx, y=-hip)] + leg("L", fl, kl, stance=swing_r, root_x=rx) + leg("R", fr, kr, stance=not swing_r, root_x=rx)
        b += [bk("torso", a=TORSO_TILT * s, x=1.5 * c, y=0.0),
              bk("head", a=-0.8 * s - TORSO_TILT * s * 0.5),                 # counter the torso tilt
              # sword arm (screen left): back (outward, +) when its leg is forward; forward = inward + elbow bend
              bk("upperArmL", a=9 * c, y=-1.0 * max(0, -c)), bk("forearmL", a=-6 * max(0, -c)),
              # shield arm: forward (inward, +) when the Knight's right leg is forward
              bk("upperArmR", a=6 * c), bk("forearmR", a=4 * max(0, c)),
              bk("padL", a=1.0 * c), bk("padR", a=1.0 * c),
              bk("capeM", a=3 * s), bk("capeL", a=5.5 * s), bk("capeR", a=5.5 * s)]
        keys.append({"t": t, "e": 0, "wb": False, "wa": 4 * c, "b": b})
    lags = [{"n": "capeM", "v": 0.12}, {"n": "capeL", "v": 0.13}, {"n": "capeR", "v": 0.13}, {"n": "head", "v": 0.04}]
    return {"name": "walk", "ms": 600, "loop": True, "keys": keys, "lags": lags}


# slash: (ms, e, upperArmL, forearmL, wrist (sword turn on top of the arm), torso, root x, root y (crouch), shield upper arm,
# shield forearm, capeM, capeL, capeR, knees out).
# Round 2 (design chat 25/11): raise peaks at 90 ms then a 30 ms chop to the 120 ms blow; torso +16 at impact, +20 follow-through;
# crouch stays 6 px; NO full sword turn: the blade follows the arm, wrist only -10..+15 deg (visible arc ~135 deg); cape peaks 50 ms after
# the impact (sides +-8, middle +-4).
SLASH = [
    (0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
    (45, 3, 50, 25, -5, -7, -1, 4, 5, 3, 0, 0, 0, 6),
    (90, 2, 100, 30, -10, -11, -2, 3, -8, -4, -3, -5, 3, 5),
    (120, 3, -18, -10, 15, 16, 3, 6, -22, -14, -1, -2, 2, 9),
    (170, 4, -32, -14, 15, 20, 4, 6, -16, -10, 4, 8, -8, 9),     # round 3: follow-through held 35 ms more (hold key)
    (205, 0, -32, -14, 15, 20, 4, 6, -16, -10, 4, 7, -7, 9),
    (275, 0, -24, -10, 11, 15, 3, 4, -10, -6, 3, 5, -5, 6),      # strong return from 275 ms
    (340, 3, -6, -2, 3, 3, 1, 1, -2, -1, 1, 1, -1, 1),
    (400, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
]
# round 3: torso leans 5 deg toward the camera at the blow (shorter, lower chest) and the sword shoulder moves 4 px along the cut,
# both back within the next 120 ms
LEAN = {120: (5.0, 4.0), 170: (3.5, 3.0), 205: (2.0, 1.5), 240: (0.0, 0.0)}


def slash():
    keys = []
    for ms, e, ua, fa, wa, torso, rx, ry, sh, shf, cm, cl, cr, knee in SLASH:
        b = [bk("root", x=rx, y=ry),
             bk("torso", a=torso, y=LEAN.get(ms, (0, 0))[0] * 0.3, sy=1 - 0.004 * ry - LEAN.get(ms, (0, 0))[0] * 0.004), bk("head", a=-torso * 0.35),
             bk("upperArmL", a=ua, x=LEAN.get(ms, (0, 0))[1] * 0.7, y=LEAN.get(ms, (0, 0))[1] * 0.7), bk("forearmL", a=fa),
             bk("padL", a=max(-12, min(12, ua * 0.08))),
             bk("upperArmR", a=sh), bk("forearmR", a=shf),
             # squat: thighs splay out (knees out), shins turn back in and rise by the crouch so the boots stay on the floor
             bk("thighL", a=knee, sy=1 - 0.008 * ry), bk("thighR", a=-knee, sy=1 - 0.008 * ry),
             bk("shinL", a=-knee * 1.6, y=-ry * 0.55), bk("shinR", a=knee * 1.6, y=-ry * 0.55),
             bk("capeM", a=cm), bk("capeL", a=cl), bk("capeR", a=cr)]
        keys.append({"t": ms / 400.0, "e": e, "wb": False, "wa": round(wa, 3), "b": b})
    return {"name": "attack", "ms": 400, "loop": False, "keys": keys, "lags": []}


def all_anims():
    return [idle(), walk(), slash()]

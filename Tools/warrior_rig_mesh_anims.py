"""Keyframes of the Warrior facing-camera MESH rig (TEST 25/11, Phase 2 of plans/261125-0045-warrior-skeletal-rig-prototype).

Timing / key moments come from the design chat's "Starward Knight V3 - Spine Rig Animation Spec V1.0" + its 3 review rounds
(Assets/ArtLibrary/ro-fantasy-heroes/ref/rig-down/animation-spec-and-reviews-chatgpt.txt): idle 1600 ms (keys 0/400/800/1200,
head +100, pads +60, forearms +120, cape +180/+220 ms), walk 600 ms in 8 keys (contact, down, passing, up x 2; hip -3.0 / +2.5 px,
hip side +-2 px, stance foot locked), slash 400 ms with the blow at 120 ms (0 / 45 wind-up / 90 raise peak / 120 IMPACT / follow-through
held 35 ms more / strong return from 275 / 330 guard / 400; easing ease-out wind-up, hard ease-in into the blow, ease-out after;
cape peaks 40-60 ms after the impact; the frame art's raised fist up-left and the fist thrust toward the camera at the blow).
What changed against the cut-out rig (the 4 "puppet" causes):
- facing camera, limbs swing TOWARD / AWAY from the camera: `sy` (length along the bone) shortens a limb coming forward, a few deg only;
  a lifted knee = shorter thigh + shin and the foot higher; the stance foot is solved to stay on the floor (plant()).
- weight: contact key + a 24 ms contact hold (the hips keep sinking while the feet stay), ease-in into every foot strike, ease-out out of it;
  slash wind-up ease-out, raise peak, ease-in chop (2 segments), back-out overshoot after the blow, hold, settle.
- cape / hair: springs in the game (HeroMeshRigPose) on top of the keys, so they lag and overshoot by themselves.
- the forearm thrust toward the camera: forearm sy 0.66 + sx 1.12 (shorter, bigger fist) and a sprite swap at the blow (`sw`).
Units: angle = screen degrees CLOCKWISE (+ on a downward arm = hand swings to screen-left), x / y = px in the parent's turned frame (y DOWN),
sx / sy = scale across / along the bone. L = screen left = the Knight's right (sword) side.
Key fields: t (0..1), e easing of the segment starting here (0 Catmull-Rom, 1 linear, 2 ease-in, 3 ease-out, 4 hold, 5 in-out, 6 back-out),
wb sword behind the body, wa wrist (sword turn on the forearm), lb / rb arm drawn behind the body, sw mesh swaps ("armL=thrust").
"""
import math

THIGH_DY, SHIN_DY = 42.0, 48.0


def bk(n, a=0.0, x=0.0, y=0.0, sx=1.0, sy=1.0):
    return {"n": n, "a": round(a, 3), "x": round(x, 3), "y": round(y, 3), "sx": round(sx, 4), "sy": round(sy, 4)}


def key(t, e, b, wb=False, wa=0.0, lb=False, rb=False, sw=""):
    return {"t": round(t, 5), "e": e, "wb": wb, "wa": round(wa, 3), "lb": lb, "rb": rb, "sw": sw, "b": b}


def plant(side, st, foot, root_dy, root_dx=0.0, out=0.0, stance=False, root_a=0.0, near=0.0):
    """Leg keys so the boot lands `foot` px below its rest place (- = lifted): thigh length st, shin length solved.
    out = knee splay (deg): the shin turns back by 1 + 42/48 of it so the boot stays where it is (knees open, feet planted);
    root_a = pelvis tilt, cancelled on the thigh so the legs stay vertical; near = 0..1 how much the limb comes toward the camera (wider)."""
    s = 1.0 + (foot - root_dy - THIGH_DY * (st - 1.0)) / SHIN_DY
    o = 1.0 if side == "L" else -1.0
    return [bk("thigh" + side, a=o * out - root_a, x=-root_dx if stance else 0.0, sy=st, sx=1.0 + 0.5 * (1.0 - st) + 0.06 * near),
            bk("shin" + side, a=-o * out * (1.0 + THIGH_DY / SHIN_DY), sy=s, sx=1.0 + 0.4 * max(0.0, 1.0 - s) + 0.05 * near)]


def idle():
    keys = []
    for t in (0.0, 0.25, 0.5, 0.75):
        s = math.sin(2 * math.pi * t)
        keys.append(key(t, 0, [
            bk("torso", y=-1.1 * s, sy=1 + 0.005 * s), bk("root", y=-0.3 * s),
            bk("head", a=0.4 * s, y=-0.3 * s),
            bk("upperArmL", a=0.5 * s, sy=1 - 0.004 * s), bk("upperArmR", a=-0.5 * s, sy=1 - 0.004 * s),
            bk("forearmL", a=0.7 * s), bk("forearmR", a=-0.7 * s),
            bk("thighL", sy=1 + 0.3 / THIGH_DY * s), bk("thighR", sy=1 + 0.3 / THIGH_DY * s),   # hips breathe, feet stay
            bk("capeM", a=0.8 * s), bk("capeL", a=1.3 * s), bk("capeR", a=-1.3 * s)], wa=0.5 * s))
    lags = {"head": 100, "upperArmL": 60, "upperArmR": 60, "forearmL": 120, "forearmR": 120, "capeM": 180, "capeL": 220, "capeR": 220}
    return {"name": "idle", "ms": 1600, "loop": True, "keys": keys, "lags": [{"n": n, "v": v / 1600} for n, v in lags.items()]}


# Targets MEASURED on the approved frame art (Tools/measure-warrior-down-motion-amplitude.py, cell px): walk boot lift 12 / 9 px (only
# at passing, both boots down at contact), head bob 5 px, head x 0.7 px (the head stays steady), chest x 2.4 px, gloves up to 13-19 px
# when an arm swings forward (fist toward the camera = higher). Attack: whole figure crouched 9-24 px (head top 37-52 vs 28 idle), head x
# range 15 px (left at the blow, right after it), sword-side boot steps up ~7 px at the blow and lands after, gloves move 15 px.
# Round 1 mesh had walk gloves 6 px, attack crouch 8 px and the head going 20 px RIGHT at the blow (opposite of the art).

# one half-cycle (screen-left leg forward at contact); the second half mirrors it.
# (t, e, front st, front foot, back st, back foot, root y, contact squash of the front leg)   root y + = hips lower.
HALF = [
    (0.000, 3, 1.02, 2.0, 0.97, -2.0, 0.5, 0.0),    # contact: front boot strikes (a little lower = nearer), back heel up
    (0.040, 0, 0.97, 2.0, 0.95, -3.0, 2.6, 1.0),    # contact hold: boot stays, hips keep sinking, landing leg compresses
    (0.125, 0, 0.95, 1.5, 0.90, -7.0, 4.0, 0.4),    # down: lowest hips right after the strike
    (0.250, 0, 1.00, 0.5, 0.82, -13.0, -0.5, 0.0),  # passing: swing knee up toward the camera (short wide thigh + shin), hips rising
    (0.375, 2, 1.02, 0.0, 0.93, -5.0, -2.8, 0.0),   # up: highest hips; ease-in into the next strike
]
PELVIS_TILT, HIP_SHIFT = 2.2, 2.6


def arm(side, fwd, gain):
    """fwd + = swings toward the camera: shorter upper arm + much shorter forearm (fist rises ~13 px, as the frame art) and the elbow
    bends inward; fwd - = back: a bit longer, outward."""
    inward = -1.0 if side == "L" else 1.0
    f, b = max(0.0, fwd) * gain, max(0.0, -fwd) * gain
    return [bk("upperArm" + side, a=inward * (5.0 * f - 4.0 * b), y=-1.5 * f, sy=1.0 - 0.20 * f + 0.02 * b, sx=1.0 + 0.05 * f),
            bk("forearm" + side, a=inward * 12.0 * f, sy=1.0 - 0.45 * f + 0.02 * b, sx=1.0 + 0.15 * f)]


def walk():
    keys = []
    for half, (front, back) in enumerate((("L", "R"), ("R", "L"))):
        for t, e, fst, ffoot, bst, bfoot, ry, squash in HALF:
            tt = t + 0.5 * half
            sn = math.sin(2 * math.pi * tt)              # + while the screen-left leg is the stance leg
            c = math.cos(2 * math.pi * tt)               # + : screen-left leg forward
            rx = -HIP_SHIFT * sn                         # weight over the stance leg
            ra = -PELVIS_TILT * sn                       # pelvis drops on the swing side
            lift = max(0.0, (0.95 - bst) / 0.13)         # 0..1 swing knee height
            b = [bk("root", a=ra, x=rx, y=ry)]
            fr = plant(front, fst, ffoot, ry, rx, out=0.0, stance=True, root_a=ra)
            for leg in fr:                               # contact squash on the landing leg
                leg["sx"] = round(leg["sx"] + 0.05 * squash, 4)
            b += fr + plant(back, bst, bfoot, ry, rx, out=2.5 * lift, root_a=ra, near=lift)
            b += [bk("torso", a=-ra + 0.6 * sn, x=0.8 * c, sx=1.0 - 0.025 * abs(c)),   # counter the pelvis, shoulders turn
                  bk("head", a=-0.6 * sn, x=0.5 * HIP_SHIFT * sn)]                    # head stays over the feet
            b += arm("L", -c, 1.0) + arm("R", c, 1.0)
            b += [bk("capeM", a=1.5 * sn), bk("capeL", a=3.0 * sn), bk("capeR", a=3.0 * sn)]
            keys.append(key(tt, e, b, wa=4.0 * c))
    lags = [{"n": "head", "v": 0.06}, {"n": "upperArmL", "v": 0.03}, {"n": "upperArmR", "v": 0.03},
            {"n": "forearmL", "v": 0.04}, {"n": "forearmR", "v": 0.04},
            {"n": "capeM", "v": 0.10}, {"n": "capeL", "v": 0.12}, {"n": "capeR", "v": 0.12}]
    return {"name": "walk", "ms": 600, "loop": True, "keys": keys, "lags": lags}


# slash keys: crouch (root y), torso twist / lean toward the sword side, squash at the blow then stretch, sword-side step.
def sk(ms, e, ua, uasy, fa, fasy, fasx, wa, ta, tx, tsx, tsy, rx, ry, ha, sh, shf, knee, foot, sw=""):
    return dict(ms=ms, e=e, ua=ua, uasy=uasy, fa=fa, fasy=fasy, fasx=fasx, wa=wa, ta=ta, tx=tx, tsx=tsx, tsy=tsy, rx=rx, ry=ry, ha=ha,
                sh=sh, shf=shf, knee=knee, foot=foot, sw=sw)


#        ms   e  upperArmL  forearmL(a,sy,sx)  wrist torso(a,x,sx,sy)       root(x,y) head shield(ua,fa) knee boot
SLASH = [
    sk(0, 3, 0, 1.00, 0, 1.00, 1.00, 0, 0, 0, 1.00, 1.00, 0, 0, 0, 0, 0, 0, 0),
    sk(45, 3, 70, 0.95, 40, 0.97, 1.00, -20, -4, -2, 0.97, 0.96, -2, 12, 2, 8, 5, 9, 0),                # wind-up: crouch, lean to the sword side
    sk(90, 2, 135, 0.90, 35, 0.95, 1.02, -38, -5, -2.5, 0.96, 0.96, -2, 12, 2, -6, -3, 9, -4),         # raise peak, sword-side boot lifts
    sk(101, 1, 118, 0.92, 26, 0.90, 1.04, -30, -5, -3, 0.98, 0.95, -2, 14, 2, -10, -6, 10, -6),        # chop (2nd ease-in segment)
    sk(120, 6, -28, 0.80, -30, 0.66, 1.12, 90, -5, -4, 1.05, 0.92, -2, 18, 2, -22, -14, 12, -5, "armL=thrust"),   # IMPACT: lowest, squash, head left
    sk(150, 0, -34, 0.79, -34, 0.65, 1.13, 96, 1, 0, 1.01, 0.97, 0, 17, 0, -18, -11, 12, 1.5, "armL=thrust"),     # boot lands (nearer)
    sk(170, 4, -36, 0.78, -36, 0.64, 1.13, 98, 3, 2, 0.99, 1.02, 1, 15, -1, -16, -10, 11, 0, "armL=thrust"),      # stretch, head right
    sk(205, 0, -36, 0.78, -36, 0.64, 1.13, 98, 3, 2, 0.99, 1.02, 1, 15, -1, -16, -10, 11, 0, "armL=thrust"),      # held 35 ms
    sk(250, 0, -26, 0.88, -22, 0.84, 1.06, 60, 2, 1.5, 1.00, 1.00, 1, 11, -1, -10, -6, 9, 0),          # strong return
    sk(330, 3, -5, 0.99, -3, 0.99, 1.00, 6, 0.5, 0.5, 1.00, 1.00, 0.5, 6, 0, -2, -1, 5, 0),            # guard, still a little low
    sk(400, 3, 0, 1.00, 0, 1.00, 1.00, 0, 0, 0, 1.00, 1.00, 0, 0, 0, 0, 0, 0, 0),
]


def slash():
    keys = []
    for k in SLASH:
        ry = k["ry"]
        b = [bk("root", x=k["rx"], y=ry), bk("torso", a=k["ta"], x=k["tx"], sx=k["tsx"], sy=k["tsy"]), bk("head", a=k["ha"]),
             bk("upperArmL", a=k["ua"], sy=k["uasy"]), bk("forearmL", a=k["fa"], sy=k["fasy"], sx=k["fasx"]),
             bk("upperArmR", a=k["sh"]), bk("forearmR", a=k["shf"])]
        st = 1.0 - 0.013 * ry                       # the crouch shortens the thighs (knees come toward the camera), shins take the rest
        b += plant("L", st, k["foot"], ry, k["rx"], out=k["knee"], stance=True, near=max(0.0, -k["foot"]) / 6.0)
        b += plant("R", st, 0.0, ry, k["rx"], out=k["knee"], stance=True)
        keys.append(key(k["ms"] / 400.0, k["e"], b, wb=90 <= k["ms"] < 101, wa=k["wa"], sw=k["sw"]))
    return {"name": "attack", "ms": 400, "loop": False, "keys": keys, "lags": [{"n": "head", "v": 0.02}]}


def all_anims():
    return [idle(), walk(), slash()]

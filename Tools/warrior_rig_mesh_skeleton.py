"""Skeleton + bone-weight rules of the Warrior facing-camera MESH rig (TEST 25/11, Phase 2).

Weights are written against BONE NAMES and cell positions, never against one picture: any parts set painted in the same
320 x 256 cell layout (feet y 244, joints at the places below) is meshed and weighted by the same rules, so new armour / another
outfit only needs new part images (warrior_rig_mesh_regions) and a re-run of `write`. Units: cell px, y DOWN.

Bone = (name, parent, joint, end, spring). The bone axis (joint -> end) is the axis a key's `sy` scales (foreshortening along the
limb); `sx` scales across it. Scale is NOT inherited (a child joint is carried by the parent's full affine, so a shortened thigh lifts
the knee, but the shin keeps its own width / length). spring: secondary motion in the game (cape, hair) on top of the keys.
"""

BONES = [
    ("root", "", (160, 150), (160, 160), 0),
    ("torso", "root", (160, 146), (160, 104), 0),
    ("head", "torso", (160, 101), (160, 30), 0),
    ("upperArmL", "torso", (131, 124), (124, 142), 0), ("forearmL", "upperArmL", (124, 142), (117, 163), 0),
    ("upperArmR", "torso", (188, 124), (195, 142), 0), ("forearmR", "upperArmR", (195, 142), (202, 163), 0),
    ("thighL", "root", (149, 152), (141, 194), 0), ("shinL", "thighL", (141, 194), (137, 242), 0),
    ("thighR", "root", (170, 152), (178, 194), 0), ("shinR", "thighR", (178, 194), (182, 242), 0),
    ("capeL", "torso", (133, 150), (108, 220), 1), ("capeM", "root", (160, 160), (160, 214), 1),
    ("capeR", "torso", (186, 150), (211, 220), 1),
    ("hair", "head", (160, 30), (160, 0), 1),          # carries the hair layer only (no mesh weight): spring bounce
]
BONE_INDEX = {b[0]: i for i, b in enumerate(BONES)}


def ss(e0, e1, v):
    """smoothstep from e0 (0) to e1 (1); e0 > e1 allowed (falls)."""
    if e0 == e1:
        return 1.0 if v >= e1 else 0.0
    t = min(1.0, max(0.0, (v - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def body_weights(x, y):
    """Head rigid above the chin (no face warping), torso, pelvis, legs (thigh -> shin around the knee), the cloth between
    the knees on capeM, shoulder pads half on the upper arm."""
    w = {}
    head = ss(105, 97, y)
    w["head"] = head
    rest = 1.0 - head
    torso = ss(153, 141, y) * rest
    side = abs(x - 160)
    pad = 0.45 * ss(17, 25, side) * ss(131, 122, y) * torso
    w["upperArmL" if x < 160 else "upperArmR"] = pad
    w["torso"] = torso - pad
    low = rest - torso
    s = "L" if x < 160 else "R"
    mid = ss(186, 196, y) * (1.0 - ss(3, 6, side))         # cloth strip between the knees
    leg = ss(156, 184, y) * ss(1, 4, side) * (1.0 - mid)
    shin = ss(188, 201, y)
    w["capeM"] = low * mid
    w["thigh" + s] = low * leg * (1 - shin)
    w["shin" + s] = low * leg * shin
    w["root"] = low * (1 - leg - mid)
    return w


def arm_weights(side):
    def f(x, y):
        fore = ss(135, 147, y)
        top = 0.5 * ss(130, 124, y)
        return {"torso": top, "upperArm" + side: (1 - top) * (1 - fore), "forearm" + side: (1 - top) * fore}
    return f


def cape_weights(x, y):
    """Wings swing on capeL / capeR (springs), pinned to the torso at the top. The filled backdrop behind hips / legs rides the
    pelvis only (root): it must stay hidden behind the body and only show where a leg lifts or an arm swings away (round 1 in the
    game: on the spring bones it slid out under the boots as a dark navy block)."""
    pin = ss(170, 152, y)
    wing_l, wing_r = ss(131, 121, x), ss(188, 198, x)       # 1 on the wing, 0 on the backdrop (blend over 10 px)
    wing = max(wing_l, wing_r)
    bone = "capeL" if x < 160 else "capeR"
    return {"torso": pin * 0.6 * wing, "root": pin * 0.4 * wing + (1 - wing), bone: (1 - pin) * wing}


RULES = {"cape": cape_weights, "body": body_weights, "armL": arm_weights("L"), "armR": arm_weights("R")}

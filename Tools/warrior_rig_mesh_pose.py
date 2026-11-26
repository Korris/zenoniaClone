"""Pose evaluation of the Warrior MESH rig, offline mirror of HeroMeshRigPose.cs (keep the two in step).

Per bone: sample a, x, y, sx, sy at phase - lag; FK: turn = parent turn + a; joint = parent affine (rest joint) + parent turn (x, y);
affine M(p) = joint + R(rest axis + turn) * S(sx, sy) * R(-rest axis) * (p - rest joint)   (scale along / across the bone, not inherited).
Springs (cape / hair): the turn and the length of a spring bone chase their keyed target with a damped spring; the target leans
against the joint's own velocity (cloth trails the motion, shortens when the body drops).
"""
import math

import warrior_rig_mesh_skeleton as K

SPRING_HZ, SPRING_DAMP = 2.6, 0.38
TRAIL_DEG = 0.05       # deg per px/s of joint velocity (x)
TRAIL_LEN = 0.0012     # length change per px/s (y)
TRAIL_MAX = 12.0


def ease(e, u, pm, p0, p1, pn):
    if e == 1:
        return p0 + (p1 - p0) * u
    if e == 2:
        return p0 + (p1 - p0) * u * u
    if e == 3:
        return p0 + (p1 - p0) * (1 - (1 - u) ** 2)
    if e == 4:
        return p0
    if e == 5:
        return p0 + (p1 - p0) * u * u * (3 - 2 * u)
    if e == 6:
        c = 1.70158
        v = u - 1
        return p0 + (p1 - p0) * (1 + (c + 1) * v ** 3 + c * v ** 2)
    u2, u3 = u * u, u * u * u
    return 0.5 * (2 * p0 + (-pm + p1) * u + (2 * pm - 5 * p0 + 4 * p1 - pn) * u2 + (-pm + 3 * p0 - 3 * p1 + pn) * u3)


def rot(v, deg):
    r = math.radians(deg)
    c, s = math.cos(r), math.sin(r)
    return (v[0] * c - v[1] * s, v[0] * s + v[1] * c)


def rest_axis(b):
    _n, _p, j, e, _s = b
    return math.degrees(math.atan2(-(e[0] - j[0]), e[1] - j[1]))


AXIS = [rest_axis(b) for b in K.BONES]


def segment(keys, p):
    i = 0
    while i + 1 < len(keys) and keys[i + 1]["t"] <= p:
        i += 1
    return i


def neighbour(anim, i):
    k = anim["keys"]
    return k[i % len(k)] if anim["loop"] else k[max(0, min(len(k) - 1, i))]


def chan(key, bone, c):
    for b in key["b"]:
        if b["n"] == bone:
            return b[c]
    return 1.0 if c in ("sx", "sy") else 0.0


def sample(anim, p, getter):
    k = anim["keys"]
    i = segment(k, p)
    j = i + 1 if i + 1 < len(k) else (0 if anim["loop"] else i)
    if i == j:
        u = 0.0
    else:
        t0, t1 = k[i]["t"], k[j]["t"] + (1.0 if j < i else 0.0)
        pp = p + (1.0 if p < t0 else 0.0)
        u = min(1.0, max(0.0, (pp - t0) / (t1 - t0))) if t1 > t0 else 0.0
    return ease(k[i]["e"], u, getter(neighbour(anim, i - 1)), getter(k[i]), getter(k[j]), getter(neighbour(anim, j + 1)))


class Pose:
    def __init__(self):
        n = len(K.BONES)
        self.pos = [(0.0, 0.0)] * n
        self.turn = [0.0] * n
        self.scale = [(1.0, 1.0)] * n
        self.spring = None            # per bone [turn, turn vel, len, len vel, last joint]
        self.flags = {}

    def reset(self):
        self.spring = None

    def affine(self, b, p):
        """rest cell point p -> posed cell point by bone b."""
        _n, _par, j, _e, _s = K.BONES[b]
        q = rot((p[0] - j[0], p[1] - j[1]), -AXIS[b])
        q = (q[0] * self.scale[b][0], q[1] * self.scale[b][1])
        q = rot(q, AXIS[b] + self.turn[b])
        return (self.pos[b][0] + q[0], self.pos[b][1] + q[1])

    def evaluate(self, anim, phase, dt):
        lags = {l["n"]: l["v"] for l in anim.get("lags", [])}
        first = self.spring is None
        if first:
            self.spring = [None] * len(K.BONES)
        for i, (n, par, j, _e, spring) in enumerate(K.BONES):
            p = phase - lags.get(n, 0.0)
            p = p - math.floor(p) if anim["loop"] else min(1.0, max(0.0, p))
            v = {c: sample(anim, p, lambda k, c=c: chan(k, n, c)) for c in ("a", "x", "y", "sx", "sy")}
            if par:
                pi = K.BONE_INDEX[par]
                base = self.affine(pi, j)
                d = rot((v["x"], v["y"]), self.turn[pi])
                self.pos[i] = (base[0] + d[0], base[1] + d[1])
                turn = self.turn[pi] + v["a"]
            else:
                self.pos[i] = (j[0] + v["x"], j[1] + v["y"])
                turn = v["a"]
            sy = v["sy"]
            if spring:
                turn, sy = self.spring_step(i, turn, sy, dt, first)
            self.turn[i] = turn
            self.scale[i] = (v["sx"], sy)
        q = phase - math.floor(phase) if anim["loop"] else min(1.0, max(0.0, phase))
        k0 = anim["keys"][segment(anim["keys"], q)]
        self.flags = {"wb": k0["wb"], "lb": k0["lb"], "rb": k0["rb"], "sw": k0["sw"], "wa": sample(anim, q, lambda k: k["wa"])}

    def spring_step(self, i, turn_t, len_t, dt, first):
        s = self.spring[i]
        if first or s is None or dt <= 0:
            self.spring[i] = [turn_t, 0.0, len_t, 0.0, self.pos[i]]
            return turn_t, len_t
        vx = (self.pos[i][0] - s[4][0]) / dt
        vy = (self.pos[i][1] - s[4][1]) / dt
        turn_t += max(-TRAIL_MAX, min(TRAIL_MAX, TRAIL_DEG * vx))
        len_t *= max(0.88, min(1.08, 1.0 - TRAIL_LEN * vy))
        w = 2 * math.pi * SPRING_HZ
        steps = max(1, int(math.ceil(dt / (1 / 120))))
        h = dt / steps
        for _ in range(steps):
            s[1] += h * (w * w * (turn_t - s[0]) - 2 * SPRING_DAMP * w * s[1])
            s[0] += h * s[1]
            s[3] += h * (w * w * (len_t - s[2]) - 2 * SPRING_DAMP * w * s[3])
            s[2] += h * s[3]
        s[4] = self.pos[i]
        return s[0], s[2]

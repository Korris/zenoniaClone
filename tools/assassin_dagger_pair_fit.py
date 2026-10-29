"""Fit the TWO daggers of an armed assassin frame (shared by tools/estimate-assassin-dagger-anchors.py).

Region = the bright purple blade pixels of the armed frame that the weaponless frame lacks (closed to a filled blade; the
blades are the only bright violet in the art, so this also catches the part that crosses the body). Daggers are
fitted one at a time with tools/held_gear_iou_fit.py: if the region falls apart into 2+ pieces each of the two biggest gets
its own fit, otherwise the best fit is stamped out of the region and the remainder is fitted for the second dagger.
Art is the tier-2 plain dagger, vertical blade UP, grip = handle wrap: angle 0 = blade up, clockwise +, so a reverse grip
(blade down / outward) is ~110..250 deg.
"""
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

import held_gear_fit as fit
import held_gear_iou_fit as ifit

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent

SCALES = tuple(round(float(x), 3) for x in np.arange(0.14, 0.355, 0.03))
SXS = (0.7, 0.85, 1.0)
ANGLES = np.arange(-180, 180, 15)
MIN_PIECE, MIN_FIT_IOU, HAND_REACH = 60, 0.12, 20
FIT_TIER = 2  # violet edge: curved blade closest to the painted armed daggers (grip / length are shared by all tiers)


def load_overlay(tier=FIT_TIER, variant='base'):
    text = (ROOT / 'src' / 'data' / 'assassin-dagger-overlay-manifest-generated.js').read_text(encoding='utf-8')
    e = json.loads(text[text.index('Object.freeze(') + len('Object.freeze('):text.rindex(')')])[tier][variant]
    return Image.open(ROOT / e['src']).convert('RGBA'), e['gripX'], e['gripY']


def purple(img):
    """Bright violet pixels (the glowing blades); body parts are dark / red / gold."""
    a = np.asarray(img).astype(int)
    return (a[..., 3] > 128) & (a[..., 2] >= a[..., 0]) & (a[..., 2] > a[..., 1] * 1.5) & (a[..., 2] > 120)


def close(mask, n=7):
    return cv2.morphologyEx(mask.astype(np.uint8), cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (n, n))) > 0


def blade_region(img, wl):
    """Filled blade pixels of `img` that the body `wl` does not already have."""
    return close(purple(img) & ~(cv2.dilate(purple(wl).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0))


def footprint(pose, mask, grip, shape=(192, 192)):
    """Boolean frame-size mask of the placed template."""
    t, off = fit.make_template(mask, grip, pose['s'], pose['sx'], pose['angle'])
    out = np.zeros(shape, bool)
    x0, y0 = int(round(pose['x'])) - off[0], int(round(pose['y'])) - off[1]
    ys, xs = np.nonzero(t > 0)
    fx, fy = xs + x0, ys + y0
    ok = (fx >= 0) & (fx < shape[1]) & (fy >= 0) & (fy < shape[0])
    out[fy[ok], fx[ok]] = True
    return out


def fit_region(reg, body_op, mask, grip, scales=SCALES, sxs=SXS, angles=ANGLES, near=None, pinned=False):
    """Coarse then fine IoU fit of one dagger to `reg`. Returns (score, pose) or (score, None)."""
    score, pose = ifit.fit_iou(reg, body_op, mask, grip, scales, sxs, angles, near=near)
    if pose is None or score < MIN_FIT_IOU:
        return score, None
    ss = (pose['s'],) if pinned else tuple(sorted({round(max(0.1, pose['s'] + k), 3) for k in (-0.03, -0.015, 0, 0.015, 0.03)}))
    sxs2 = tuple(sorted({round(min(1.0, max(0.5, pose['sx'] + k)), 3) for k in (-0.12, 0, 0.12)}))
    score, pose = ifit.fit_iou(reg, body_op, mask, grip, ss, sxs2, np.arange(pose['angle'] - 10, pose['angle'] + 11, 2.5), near=near)
    pose['angle'] = ((pose['angle'] + 180) % 360) - 180
    return score, pose


def base_end(piece, centre):
    """Blade end nearest the body centre (where the fist holds it): mean of the 10% of piece pixels closest to `centre`."""
    ys, xs = np.nonzero(piece)
    d = np.hypot(xs - centre[0], ys - centre[1])
    k = d <= np.quantile(d, 0.10)
    return float(xs[k].mean()), float(ys[k].mean())


def pieces(reg):
    n, lab, st, _ = cv2.connectedComponentsWithStats(reg.astype(np.uint8))
    order = [i for i in np.argsort(-st[1:, cv2.CC_STAT_AREA]) + 1 if st[i, cv2.CC_STAT_AREA] >= MIN_PIECE]
    return [lab == i for i in order]


def fit_pair(armed, wl, scales=SCALES, pinned=False):
    """-> (region, body_op, [(score, pose)...] up to 2 fitted daggers, unordered)."""
    art, gx, gy = load_overlay()
    mask = close(purple(art))  # blade only: the handle is held in the fist and has no violet
    reg, body_op = blade_region(armed, wl), np.zeros((192, 192), bool)
    parts = pieces(reg)
    wl_ys, wl_xs = np.nonzero(np.asarray(wl)[..., 3] > 48)
    centre = (wl_xs.mean(), wl_ys.mean())
    held = lambda piece: (*base_end(piece, centre), HAND_REACH)  # the fist sits at the blade's inner end: stops 180-degree flips
    found = []
    if len(parts) >= 2:
        for p in parts[:2]:
            score, pose = fit_region(p, body_op, mask, (gx, gy), scales, near=held(p), pinned=pinned)
            if pose:
                found.append((score, pose))
    elif parts:
        score, pose = fit_region(parts[0], body_op, mask, (gx, gy), scales, near=held(parts[0]), pinned=pinned)
        if pose:
            found.append((score, pose))
            rest = reg & ~(cv2.dilate(footprint(pose, mask, (gx, gy)).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0)
            for p in pieces(rest)[:1]:
                s2, p2 = fit_region(p, body_op, mask, (gx, gy), scales, near=held(p), pinned=pinned)
                if p2:
                    found.append((s2, p2))
    return reg, body_op, found, (mask, gx, gy)

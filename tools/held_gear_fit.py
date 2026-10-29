"""Generic overlay fit for held gear (warrior sword + shield; generalizes the wizard staff fit in
estimate-wizard-staff-anchors.py). The overlay's own silhouette (upright art, grip pixel known) is the template:
scaled by s, squeezed horizontally by sx (side views foreshorten the shield), rotated by `angle` (deg, clockwise,
0 = art upright) about the grip, then slid over a weight map with cv2.matchTemplate.

Weight map (see weights()): +1 where the gear is visible (armed pixel that the variant body lacks / paints differently),
-0.5 outside the armed silhouette (gear cannot be there), -0.2 over unchanged body (may be partly hidden behind it, but must not bloat).
"""
import math

import cv2
import numpy as np

FRAME = 192
BODY_PENALTY = 0.2  # mild cost for covering unchanged body (stops the template bloating over it)
VISIBLE_DIFF = 90  # summed |dRGB| above which two opaque pixels count as "painted differently"


def weights(armed, body, remove=None):
    """armed/body: RGBA PIL frames. `remove` = body alpha used for the 'not body' test (defaults to body).
    Returns (weight map float32, differs mask bool, body-opaque mask bool)."""
    a, b = np.asarray(armed).astype(int), np.asarray(body).astype(int)
    a_op, b_op = a[..., 3] > 128, b[..., 3] > 48
    near_body = cv2.dilate(b_op.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))) > 0
    differs = a_op & b_op & (np.abs(a[..., :3] - b[..., :3]).sum(axis=2) > VISIBLE_DIFF)
    cand = (a_op & ~near_body) | differs
    wt = np.where(a_op, -BODY_PENALTY, -0.5)
    wt = np.where(cand, 1.0, wt)
    return wt.astype(np.float32), differs, b_op


def make_template(mask, grip, s, sx, angle):
    """Binary template of `mask` (art-space bool) -> (float32 crop, grip offset inside the crop)."""
    gx, gy = grip
    h, w = mask.shape
    m = cv2.resize(mask.astype(np.uint8), (max(1, round(w * s * sx)), max(1, round(h * s))), interpolation=cv2.INTER_AREA)
    gpx, gpy = gx * s * sx, gy * s
    size = int(max(m.shape) * 2 + 8)
    canvas = np.zeros((size, size), np.uint8)
    c = size // 2
    x0, y0 = int(round(c - gpx)), int(round(c - gpy))
    canvas[y0:y0 + m.shape[0], x0:x0 + m.shape[1]] = m
    rot = cv2.getRotationMatrix2D((c, c), -angle, 1.0)  # cv2 positive = CCW; ours = clockwise
    canvas = cv2.warpAffine(canvas, rot, (size, size), flags=cv2.INTER_LINEAR)
    ys, xs = np.nonzero(canvas > 0)
    ox, oy = xs.min(), ys.min()
    return (canvas[oy:ys.max() + 1, ox:xs.max() + 1] > 0).astype(np.float32), (c - ox, c - oy)


def best_pose(wt, mask, grip, scales, sxs, angles, pad=90):
    """Exhaustive search; returns (score, pose dict). Grip position is the template grip placed on the map."""
    padded = cv2.copyMakeBorder(wt, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=-0.5)
    best = (-1e9, None)
    for s in scales:
        for sx in sxs:
            for angle in angles:
                t, off = make_template(mask, grip, s, sx, angle)
                if t.shape[0] > padded.shape[0] or t.shape[1] > padded.shape[1]:
                    continue
                res = cv2.matchTemplate(padded, t, cv2.TM_CCORR)
                _, mx, _, loc = cv2.minMaxLoc(res)
                if mx > best[0]:
                    best = (mx, {'x': int(loc[0] + off[0] - pad), 'y': int(loc[1] + off[1] - pad), 'angle': float(angle), 's': float(s), 'sx': float(sx)})
    return best


def fit(wt, mask, grip, s_range, sxs, angle_range=(-180, 180)):
    """Coarse (15 deg) then fine (+-9 deg by 3, scale +-0.04) search."""
    lo, hi = angle_range
    coarse = np.arange(lo, hi, 15)
    score, pose = best_pose(wt, mask, grip, s_range, sxs, coarse)
    fine_s = sorted({round(max(0.1, pose['s'] + d), 3) for d in (-0.04, -0.02, 0, 0.02, 0.04)})
    fine_sx = sorted({round(min(1.0, max(0.3, pose['sx'] + d)), 3) for d in (-0.15, 0, 0.15)})
    score, pose = best_pose(wt, mask, grip, fine_s, fine_sx, np.arange(pose['angle'] - 9, pose['angle'] + 10, 3))
    pose['angle'] = ((pose['angle'] + 180) % 360) - 180
    return score, pose


def occlusion(pose, mask, grip, differs, body_op):
    """(visible fraction, overlap share): of the template pixels lying over the body, how many are painted
    differently in the armed frame. Low visible fraction => the body covers the gear => draw it BEHIND the body."""
    t, off = make_template(mask, grip, pose['s'], pose['sx'], pose['angle'])
    ys, xs = np.nonzero(t > 0)
    fx, fy = xs + round(pose['x']) - off[0], ys + round(pose['y']) - off[1]
    ok = (fx >= 0) & (fx < FRAME) & (fy >= 0) & (fy < FRAME)
    fx, fy = fx[ok], fy[ok]
    over = body_op[fy, fx]
    if not len(over) or over.sum() == 0:
        return 1.0, 0.0
    return float(differs[fy[over], fx[over]].mean()), float(over.mean())


def to_js_value(pose, score, behind):
    return {'x': round(pose['x'], 1), 'y': round(pose['y'], 1), 'angle': round(pose['angle'], 1), 's': round(pose['s'], 3),
            'sx': round(pose['sx'], 3), 'behind': bool(behind), 'score': round(float(score))}


def rotation_dist(a, b):
    return abs(((a - b + 180) % 360) - 180)


__all__ = ['weights', 'fit', 'occlusion', 'to_js_value', 'rotation_dist', 'math']

"""IoU-based constrained fit of an overlay silhouette to a target gear region (visible gear pixels in the armed frame).
score = intersection / (|region| + penalty); penalty = template pixels outside the region, counted at half weight when they
lie over the (unchanged) body (they may be hidden behind it). Angle is constrained near the region's principal axis
(swords) or a small tilt (shields); scale is clamped by the caller.
"""
import cv2
import numpy as np

from held_gear_fit import make_template

OVER_BODY = 0.5


def region_mask(top, body, weights_fn, min_area=60, open_px=3, rel_keep=0.0, min_elong=1.6):
    """Visible gear region: weights>0.5, opened (open_px) and stripped of specks / components smaller than
    rel_keep x the largest. Pose-difference noise is thin, so a wide opening isolates a blade. Returns (mask, body_op)."""
    wt, _, body_op = weights_fn(top, body)
    raw = (wt > 0.5).astype(np.uint8)
    kernel = lambda n: cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (n, n))
    fine = cv2.morphologyEx(raw, cv2.MORPH_OPEN, kernel(3))
    m = fine
    if open_px > 3:
        # Wide mode (blades): the core is the part of the gear OUTSIDE the body silhouette (free of pose-difference noise),
        # grown 12 px back over body pixels that are painted differently (blade root crossing the cape / arm).
        outside = raw & ~cv2.dilate(body_op.astype(np.uint8), kernel(5))
        m = cv2.morphologyEx(outside, cv2.MORPH_OPEN, kernel(open_px))
        n0, lab0, st0, _ = cv2.connectedComponentsWithStats(m)
        a0 = st0[:, cv2.CC_STAT_AREA].astype(float)
        a0[0] = 0
        for c in range(1, n0):  # blades are elongated; squat blobs (a foot that moved between the sheets) are not
            ys, xs = np.nonzero(lab0 == c)
            if len(ys) > 8:
                w, _ = np.linalg.eigh(np.cov(np.stack([xs, ys])))
                if np.sqrt(w[1] / max(w[0], 1e-6)) < min_elong:
                    a0[c] = 0
        keep0 = a0 >= max(min_area // 2, rel_keep * a0.max())
        keep0[0] = False
        m = keep0[lab0].astype(np.uint8)
        for _ in range(12):
            m = cv2.dilate(m, kernel(3)) & fine
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    areas = st[:, cv2.CC_STAT_AREA].astype(float)
    areas[0] = 0
    keep = areas >= max(min_area, rel_keep * areas.max())
    keep[0] = False
    return keep[lab], body_op


def principal_axis(mask, away_from):
    """(angle deg of the tip direction in our convention [0 = up, clockwise +], elongation). The tip is the axis end
    farthest from `away_from` (the body centre)."""
    ys, xs = np.nonzero(mask)
    pts = np.stack([xs, ys], 1).astype(float)
    c = pts.mean(0)
    w, v = np.linalg.eigh(np.cov((pts - c).T))
    ax = v[:, 1]
    proj = (pts - c) @ ax
    ends = [c + ax * proj.max(), c + ax * proj.min()]
    tip_dir = ax if np.linalg.norm(ends[0] - away_from) >= np.linalg.norm(ends[1] - away_from) else -ax
    return float(np.degrees(np.arctan2(tip_dir[0], -tip_dir[1]))), float(np.sqrt(w[1] / max(w[0], 1e-6)))


def fit_iou(region, body_op, mask, grip, scales, sxs, angles, pad=60, near=None, over_body=OVER_BODY):
    """Exhaustive IoU search over (scale, sx, angle) x frame position. Returns (iou, pose dict)."""
    r = region.astype(np.float32)
    area = float(r.sum())
    over = (body_op & ~region).astype(np.float32)

    def padded(a, value=0.0):
        return cv2.copyMakeBorder(a, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=value)

    pr, po = padded(r), padded(over)
    # outside the frame is free only when the armed art is clipped by the sheet edge, else it counts as 'not region'
    clipped = bool(region[:2].any() or region[-2:].any() or region[:, :2].any() or region[:, -2:].any())
    pn = padded(1.0 - r, 0.0 if clipped else 1.0)
    best = (-1.0, None)
    # near = (x, y, radius): only grip positions within `radius` px of (x, y) are allowed
    for s in scales:
        for sx in sxs:
            for angle in angles:
                t, off = make_template(mask, grip, s, sx, angle)
                if t.shape[0] > pr.shape[0] or t.shape[1] > pr.shape[1]:
                    continue
                inter = cv2.matchTemplate(pr, t, cv2.TM_CCORR)
                outside = cv2.matchTemplate(pn, t, cv2.TM_CCORR)
                hidden = cv2.matchTemplate(po, t, cv2.TM_CCORR)
                score = inter / (area + outside - over_body * hidden + 1e-3)
                if near is not None:
                    h, w = score.shape
                    gy, gx = np.mgrid[0:h, 0:w]
                    far = (gx + off[0] - pad - near[0]) ** 2 + (gy + off[1] - pad - near[1]) ** 2 > near[2] ** 2
                    score = np.where(far, -1.0, score)
                _, mx, _, loc = cv2.minMaxLoc(score)
                if mx > best[0]:
                    best = (mx, {'x': int(loc[0] + off[0] - pad), 'y': int(loc[1] + off[1] - pad), 'angle': float(angle), 's': float(s), 'sx': float(sx)})
    return best

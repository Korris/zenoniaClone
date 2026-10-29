"""Dagger anchors for the animations that have NO armed sheet (walk / dash / hurt) (used by estimate-assassin-dagger-anchors.py).

Reference = per hand and facing, the idle anchors fitted from the armed art (median angle / scale / squeeze). The fist patch
(34 px around the idle grip, cut from the weaponless idle frames) is searched in each target frame by normalised
cross-correlation near the idle grip position; the reference pose is moved to the best match. Weak matches (NCC < MIN_NCC) get
{"fallback": true} (= bare body, no daggers); fix single frames with tools/assets/assassin-held-gear-anchor-overrides.json.
"""
import cv2
import numpy as np

PATCH, REACH, MIN_NCC, MIN_HAND_GAP = 34, 28, 0.40, 18


def _rgb(img):
    """RGBA PIL frame -> float32 RGB composited on mid grey (appearance only, alpha-independent comparison)."""
    a = np.asarray(img).astype(np.float32)
    al = a[..., 3:4] / 255.0
    return a[..., :3] * al + 128.0 * (1 - al)


def _circ_median(angles):
    ref = angles[0]
    return float(((np.median([((a - ref + 180) % 360) - 180 for a in angles]) + ref + 180) % 360) - 180)


def _reference(poses):
    good = [p for p in poses if not p.get('fallback')]
    if not good:
        return None
    return {'x': float(np.median([p['x'] for p in good])), 'y': float(np.median([p['y'] for p in good])),
            'angle': _circ_median([p['angle'] for p in good]), 's': float(np.median([p['s'] for p in good])),
            'sx': float(np.median([p['sx'] for p in good])), 'behind': sum(p['behind'] for p in good) * 2 > len(good)}


def _patches(poses, d, frame):
    out = []
    for i, p in enumerate(poses):
        if p.get('fallback'):
            continue
        img = _rgb(frame('weaponless', 'idle', d, i))
        x, y = int(round(p['x'])), int(round(p['y']))
        h = PATCH // 2
        pad = cv2.copyMakeBorder(img, h, h, h, h, cv2.BORDER_REPLICATE)
        out.append(pad[y:y + PATCH, x:x + PATCH].copy())
    return out


def _match(target, patches, ref):
    """(score, x, y) of the best patch match whose centre lies within REACH px of the reference grip."""
    h = PATCH // 2
    pad = cv2.copyMakeBorder(target, h, h, h, h, cv2.BORDER_REPLICATE)
    best = (-1.0, ref['x'], ref['y'])
    for patch in patches:
        res = cv2.matchTemplate(pad, patch, cv2.TM_CCOEFF_NORMED)
        ys, xs = np.mgrid[0:res.shape[0], 0:res.shape[1]]
        res = np.where((xs - ref['x']) ** 2 + (ys - ref['y']) ** 2 > REACH ** 2, -1.0, res)
        _, mx, _, loc = cv2.minMaxLoc(res)
        if mx > best[0]:
            best = (float(mx), float(loc[0]), float(loc[1]))  # patch top-left in padded = centre in the frame
    return best


def track_fists(data, free, dirs, scale, overrides, frame):
    """Fills data[hand][anim][dir] for each anim of `free` ({anim: frame count})."""
    for d in dirs:
        refs = {h: _reference(data[h]['idle'][d]) for h in ('main', 'off')}
        patches = {h: _patches(data[h]['idle'][d], d, frame) for h in ('main', 'off')}
        for anim, n in free.items():
            for i in range(n):
                target = _rgb(frame('weaponless', anim, d, i))
                found = {}
                for h in ('main', 'off'):
                    if refs[h] and patches[h]:
                        found[h] = _match(target, patches[h], refs[h])
                gap = np.hypot(*(np.array(found['main'][1:]) - np.array(found['off'][1:]))) if len(found) == 2 else 0
                for h in ('main', 'off'):
                    ref, key = refs[h], f'{h}.{anim}.{d}.{i}'
                    if h in found and found[h][0] >= MIN_NCC and gap >= MIN_HAND_GAP:
                        score, x, y = found[h]
                        e = {'x': round(x, 1), 'y': round(y, 1), 'angle': round(ref['angle'], 1), 's': round(ref['s'], 3),
                             'sx': round(ref['sx'], 3), 'behind': ref['behind'], 'score': round(score * 1000)}
                    else:
                        e = {'fallback': True, 'score': round(found[h][0] * 1000) if h in found else 0}
                    if key in overrides:
                        e.pop('fallback', None)
                        e.update(overrides[key])
                    data[h][anim][d].append(e)

"""Estimate the per-frame holy-staff anchor for the cleric from the aligned frame sets.

  staff = armed - weaponless (what the armed frame paints that the staff-less body lacks), IoU fit
  (tools/held_gear_iou_fit.py, same method as tools/estimate-warrior-held-gear-anchors.py).
Per displayed frame: anchor x,y = grip px in the 192px frame, angle deg (0 = art upright, clockwise +), s = frame px per
art px, sx = horizontal squeeze, behind = body drawn over the staff. A frame whose fitted overlay does not reproduce the
armed staff region (QA IoU < MIN_QA_IOU, or hardly any staff shows outside the body; e.g. the weaponless arms drifted) gets {"fallback": true}: the renderer draws
the armed baked frame there instead of a floating staff. Death is not covered (armed death has no staff).
Overrides: tools/assets/cleric-held-gear-anchor-overrides.json {"staff.attack2.left.1": {"angle": 40}} (un-hides too).
Outputs src/data/cleric-held-anchors-generated.js, assets/hd/cleric/anchors.json, QA sheets assets/hd/cleric/qa/<anim>.png
([armed | weaponless + overlay | diff green = both, red = armed only, blue = overlay only]).
Usage: python tools/estimate-cleric-staff-anchors.py
"""
import json
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image

import held_gear_fit as fit
import held_gear_iou_fit as ifit
from held_gear_preview import compose
import cv2
from warrior_gear_qa import comp_region, iou

ROOT = Path(__file__).resolve().parents[1]
CL = ROOT / 'assets' / 'hd' / 'cleric'
OVERRIDES = ROOT / 'tools' / 'assets' / 'cleric-held-gear-anchor-overrides.json'
OUT_JS = ROOT / 'src' / 'data' / 'cleric-held-anchors-generated.js'
FRAME = 192
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2}
DIRS = ['down', 'up', 'left', 'right']
SCALES = tuple(round(float(x), 3) for x in np.arange(0.36, 0.53, 0.02))
SXS = (0.7, 0.85, 1.0)
GAP, MIN_AREA, MIN_FIT_IOU, MIN_QA_IOU, SCALE_BAND, BEHIND_BELOW = 7, 70, 0.10, 0.35, 0.06, 0.35
MIN_VISIBLE = 250  # px of staff region outside the body: less => the staff is mostly in front of the body, the fit cannot be trusted
SHEETS = {}


def sheet(name, anim):
    if (name, anim) not in SHEETS:
        path = CL / ('normalized-hd' if name == 'armed' else 'weaponless/normalized-hd') / f'{anim}.png'
        SHEETS[name, anim] = Image.open(path).convert('RGBA')
    return SHEETS[name, anim]


def frame(name, anim, d, i):
    r = DIRS.index(d)
    return sheet(name, anim).crop((i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME))


def load_overlay(tier=0, variant='base'):
    text = (ROOT / 'src' / 'data' / 'cleric-staff-overlay-manifest-generated.js').read_text(encoding='utf-8')
    e = json.loads(text[text.index('Object.freeze(') + len('Object.freeze('):text.rindex(')')])[tier][variant]
    return Image.open(ROOT / e['src']).convert('RGBA'), e['gripX'], e['gripY']


def staff_region(armed, wl):
    """Armed pixels outside the (dilated) weaponless silhouette = the staff head / tail sticking out of the body.
    The weaponless sheet is a redraw, so colour differences over the body are noise and are not used.
    Returns (region mask, weaponless-opaque mask, body halo mask)."""
    a_op, b_op = np.asarray(armed)[..., 3] > 128, np.asarray(wl)[..., 3] > 48
    halo = cv2.dilate(b_op.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (GAP, GAP))) > 0
    m = cv2.morphologyEx((a_op & ~halo).astype(np.uint8), cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    areas = st[:, cv2.CC_STAT_AREA].astype(float)
    areas[0] = 0
    keep = areas >= max(MIN_AREA // 2, 0.15 * areas.max())
    keep[0] = False
    return keep[lab], b_op, halo


def angle_candidates(anim, reg, body_mask):
    """Staff head points away from the body: idle / walk hold it upright (+-30 deg); every other animation searches
    +-36 deg around the direction body centre -> visible staff head (0 = up, clockwise +). A head-only region has no
    reliable principal axis, so an unconstrained search flips the staff upside down."""
    if anim in ('idle', 'walk'):
        return np.arange(-30, 31, 6)
    (by, bx), (ry, rx) = (np.argwhere(body_mask).mean(0), np.argwhere(reg).mean(0))
    phi = float(np.degrees(np.arctan2(rx - bx, -(ry - by))))
    return np.array([((phi + k + 180) % 360) - 180 for k in range(-36, 37, 6)])


def estimate(job):
    """job = (anim, d, i, scale band or None, prior pose or None) -> (job, anchor dict)."""
    anim, d, i, band, prior = job
    scales = (tuple(x for x in SCALES if band[0] <= x <= band[1]) or (round(sum(band) / 2, 3),)) if band else SCALES
    art, gx, gy = load_overlay()
    mask = np.asarray(art)[..., 3] > 128
    armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
    reg, body_op, _ = staff_region(armed, wl)
    if reg.sum() < MIN_AREA:
        return job, {'hidden': True, 'score': 0}
    angles, near = angle_candidates(anim, reg, body_op), None
    if prior:  # temporal consistency: stay near the row's median pose
        angles = np.array([((prior['angle'] + k + 180) % 360) - 180 for k in range(-24, 25, 6)])
        near = (prior['x'], prior['y'], 22)
    score, pose = ifit.fit_iou(reg, body_op, mask, (gx, gy), scales, SXS, angles, near=near)
    if pose is None or score < MIN_FIT_IOU:
        return job, {'hidden': True, 'score': round(float(score) * 1000)}
    score, pose = ifit.fit_iou(reg, body_op, mask, (gx, gy), (pose['s'],), (pose['sx'],), np.arange(pose['angle'] - 5, pose['angle'] + 6, 1.0), near=near)
    pose['angle'] = ((pose['angle'] + 180) % 360) - 180
    _, differs, _ = fit.weights(armed, wl)
    vis, share = fit.occlusion(pose, mask, (gx, gy), differs, body_op)
    return job, fit.to_js_value(pose, score * 1000, share > 0.2 and vis < BEHIND_BELOW)


def circ_median(angles):
    ref = angles[0]
    return float(((np.median([((a - ref + 180) % 360) - 180 for a in angles]) + ref + 180) % 360) - 180)


def fit_all():
    jobs = [(a, d, i, None, None) for a, n in COUNTS.items() for d in DIRS for i in range(n)]
    with ProcessPoolExecutor() as pool:
        first = list(pool.map(estimate, jobs, chunksize=4))
        med = float(np.median([e['s'] for _, e in first if not e.get('hidden') and e['score'] > 300]))
        band = (med * (1 - SCALE_BAND), med * (1 + SCALE_BAND))
        print('scale band', band)
        results = {(a, d, i): e for (a, d, i, *_), e in pool.map(estimate, [(a, d, i, band, None) for a, d, i, *_ in jobs], chunksize=4)}
        redo = []
        for (a, d, i), e in results.items():  # steady rows: re-fit outliers near the row median
            row = [results[a, d, j] for j in range(COUNTS[a])]
            good = [r for r in row if not r.get('hidden')]
            if a.startswith('attack') or len(good) < 2:
                continue
            prior = {'angle': circ_median([r['angle'] for r in good]), 'x': float(np.median([r['x'] for r in good])), 'y': float(np.median([r['y'] for r in good]))}
            if e.get('hidden') or fit.rotation_dist(e['angle'], prior['angle']) > 30 or np.hypot(e['x'] - prior['x'], e['y'] - prior['y']) > 24:
                redo.append((a, d, i, band, prior))
        for (a, d, i, *_), e in pool.map(estimate, redo, chunksize=2):
            if not e.get('hidden'):
                results[a, d, i] = e
    return results


def qa_and_fallback(data, art):
    """Per frame IoU(armed staff region, composed overlay region); writes QA sheets; marks poor frames as fallback."""
    rows = {}
    for anim, n in COUNTS.items():
        qa = Image.new('RGB', (n * FRAME * 3, FRAME * 4), (20, 20, 30))
        for r, d in enumerate(DIRS):
            for i in range(n):
                armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
                a = data[anim][d][i]
                comp = wl if a.get('fallback') else compose(wl, [(a, *art)])
                a_reg, _, halo = staff_region(armed, wl)
                c_reg = comp_region(comp, wl) & ~halo
                key = f'{anim}.{d}.{i}'
                rows[key] = 0.0 if a.get('fallback') else iou(a_reg, c_reg)
                if rows[key] < MIN_QA_IOU or a_reg.sum() < MIN_VISIBLE:
                    rows[key] = min(rows[key], MIN_QA_IOU - 0.001)
                    data[anim][d][i] = {'fallback': True, 'score': a.get('score', 0)}
                    comp = armed  # what the game draws for a fallback frame
                diff = np.zeros((FRAME, FRAME, 3), np.uint8)
                diff[a_reg & c_reg], diff[a_reg & ~c_reg], diff[~a_reg & c_reg] = (60, 200, 60), (230, 50, 50), (60, 100, 255)
                for c, img in enumerate((armed, comp)):
                    tile = Image.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                    tile.alpha_composite(img)
                    qa.paste(tile.convert('RGB'), ((i * 3 + c) * FRAME, r * FRAME))
                qa.paste(Image.fromarray(diff), ((i * 3 + 2) * FRAME, r * FRAME))
        (CL / 'qa').mkdir(exist_ok=True)
        qa.save(CL / 'qa' / f'{anim}.png')
    return rows


def main():
    overrides = json.loads(OVERRIDES.read_text()) if OVERRIDES.exists() else {}
    results = fit_all()
    data = {a: {d: [] for d in DIRS} for a in COUNTS}
    for (a, d, i), e in sorted(results.items()):
        if f'staff.{a}.{d}.{i}' in overrides:  # a manual pose also un-hides the frame
            e.pop('hidden', None)
            e.update(overrides[f'staff.{a}.{d}.{i}'])
        data[a][d].append({'fallback': True, 'score': e['score']} if e.get('hidden') else e)
    rows = qa_and_fallback(data, load_overlay())
    fallbacks = [k for k, v in rows.items() if v < MIN_QA_IOU]
    vals = np.array(list(rows.values()))
    print(f'frames {len(vals)}: QA IoU mean {vals.mean():.3f} min {vals.min():.3f}; fallback to armed baked frame: {len(fallbacks)}')
    for k in sorted(fallbacks):
        print('  fallback', k, f'{rows[k]:.2f}')
    (CL / 'anchors.json').write_text(json.dumps({'anchors': data, 'qaIou': rows}, indent=1))
    OUT_JS.write_text('// GENERATED by tools/estimate-cleric-staff-anchors.py -- do not edit by hand (use tools/assets/cleric-held-gear-anchor-overrides.json).\n'
                      '// Holy-staff grip anchor per displayed cleric frame [anim][dir][i]: x,y = grip px in the 192px frame, angle deg (0 = art upright,\n'
                      '// clockwise +), s = frame px per art px, sx = horizontal squeeze, behind = body drawn over the staff,\n'
                      '// fallback = overlay does not match the armed art here: draw the armed baked frame instead.\n'
                      f'export const CLERIC_STAFF_ANCHORS = Object.freeze({json.dumps(data, separators=(",", ":"))});\n', encoding='utf-8')
    print('wrote', OUT_JS.name)


if __name__ == '__main__':
    main()

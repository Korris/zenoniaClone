"""Estimate per-frame sword + shield anchors for the new warrior from the aligned frame sets.

  sword  = armed - weaponless (what the armed frame paints that the sword-less body lacks)
  shield = weaponless - bare
Per displayed frame (anim x dir x index; no mirroring) the tier-0 overlay silhouette is fitted by template matching
(tools/held_gear_fit.py): anchor x,y = grip pixel in the 192px frame, angle deg (0 = art upright, clockwise +),
s = frame px per art px, sx = horizontal squeeze (foreshortening), behind = body drawn over the gear.
Frames where the gear is not visible are filled from the nearest visible frame (drawn behind the body).
Manual fixes: tools/assets/warrior-held-gear-anchor-overrides.json {"sword.attack2.left.1": {"angle": 40}}.
Outputs src/data/warrior-held-anchors-generated.js + assets/hd/warrior-v2/anchors.json + anchor-preview-*.png.
Usage: python tools/estimate-warrior-held-gear-anchors.py
"""
import json
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image

import held_gear_fit as fit
import held_gear_iou_fit as ifit
from held_gear_preview import load_overlay, write_previews

ROOT = Path(__file__).resolve().parents[1]
W2 = ROOT / 'assets' / 'hd' / 'warrior-v2'
OVERRIDES = ROOT / 'tools' / 'assets' / 'warrior-held-gear-anchor-overrides.json'
OUT_JS = ROOT / 'src' / 'data' / 'warrior-held-anchors-generated.js'
FRAME = 192
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2}
DIRS = ['down', 'up', 'left', 'right']
KINDS = {  # kind: (scale grid, sx grid, min region px)
    'sword': (tuple(round(float(x), 3) for x in np.arange(0.34, 0.43, 0.02)), (0.7, 0.85, 1.0), 120),
    'shield': (tuple(round(float(x), 3) for x in np.arange(0.14, 0.34, 0.02)), (0.45, 0.6, 0.75, 0.9, 1.0), 120),
}
MIN_IOU = 0.12
SCALE_BAND = 0.06  # pass 2: scale clamped to +-12% of the median of well-fitted frames
BEHIND_BELOW = 0.35  # visible fraction over the body under which the gear is hidden by the body
SHEETS = {}


def sheet(name, anim):
    key = (name, anim)
    if key not in SHEETS:
        path = W2 / 'normalized-hd' / f'{anim}.png' if name == 'armed' else W2 / name / 'normalized-hd' / f'{anim}.png'
        SHEETS[key] = Image.open(path).convert('RGBA')
    return SHEETS[key]


def frame(name, anim, d, i):
    r = DIRS.index(d)
    return sheet(name, anim).crop((i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME))


def angle_candidates(kind, anim, d, region, body_mask):
    if kind == 'shield':
        return np.arange(-36, 37, 6)
    if d == 'up' and not anim.startswith('attack'):  # armed art: blade held low at the right hip, tip up-right, hilt hidden by the cape
        return np.arange(20, 81, 6)
    ys, xs = np.nonzero(body_mask)
    centre = np.array([xs.mean(), ys.mean()])
    axis, elong = ifit.principal_axis(region, centre)
    touches = region[:2].any() or region[-2:].any() or region[:, :2].any() or region[:, -2:].any()
    if elong < 1.8 or touches:
        return np.arange(-180, 180, 8)
    return np.array([((axis + d + 180) % 360) - 180 for d in range(-24, 25, 6)])


def circ_median(angles):
    ref = angles[0]
    return float(((np.median([((a - ref + 180) % 360) - 180 for a in angles]) + ref + 180) % 360) - 180)


def estimate(job):
    """job = (kind, anim, d, i, scale band or None). Returns (job, anchor dict)."""
    kind, anim, d, i, band, prior = job
    scales, sxs, min_area = KINDS[kind]
    if kind == 'sword' and d == 'up':
        min_area = 40  # only a short blade stub shows past the cape
    if band:
        scales = tuple(x for x in scales if band[0] <= x <= band[1]) or (round(sum(band) / 2, 3),)
    art, gx, gy = load_overlay(kind)
    mask = np.asarray(art)[..., 3] > 128
    top, body = (('armed', 'weaponless') if kind == 'sword' else ('weaponless', 'bare'))
    region, body_op = ifit.region_mask(frame(top, anim, d, i), frame(body, anim, d, i), fit.weights, min_area, *((5, 0.4, 1.1 if d == 'up' else 1.6) if kind == 'sword' else (3, 0.0)))
    if region.sum() < min_area:
        return job, {'hidden': True, 'score': 0}
    angles = angle_candidates(kind, anim, d, region, np.asarray(frame('weaponless', anim, d, i))[..., 3] > 128)
    hide = 1.0 if (kind == 'sword' and d == 'up' and not anim.startswith('attack')) else ifit.OVER_BODY  # up view: body covers the blade root, so no cost over it
    near = None
    if prior:  # temporal consistency: stay near the row's median pose
        angles = np.array([((prior['angle'] + k + 180) % 360) - 180 for k in range(-24, 25, 6)])
        near = (prior['x'], prior['y'], 22)
    score, pose = ifit.fit_iou(region, body_op, mask, (gx, gy), scales, sxs, angles, near=near, over_body=hide)
    if pose is None or score < (0.08 if min_area < 100 else MIN_IOU):
        return job, {'hidden': True, 'score': round(float(score) * 1000)}
    score, pose = ifit.fit_iou(region, body_op, mask, (gx, gy), (pose['s'],), (pose['sx'],), np.arange(pose['angle'] - 5, pose['angle'] + 6, 1.0), near=near, over_body=hide)
    pose['angle'] = ((pose['angle'] + 180) % 360) - 180
    _, differs, _ = fit.weights(frame(top, anim, d, i), frame(body, anim, d, i))
    vis, share = fit.occlusion(pose, mask, (gx, gy), differs, body_op)
    return job, fit.to_js_value(pose, score * 1000, (kind == 'sword' and d == 'up' and not anim.startswith('attack')) or (share > 0.2 and vis < BEHIND_BELOW))


def fill_hidden(row):
    """Gear invisible in a frame would blink: reuse the nearest visible frame's pose behind the body."""
    if all(e.get('hidden') for e in row):
        return row
    out = []
    for i, e in enumerate(row):
        if e.get('hidden'):
            near = min((j for j, o in enumerate(row) if not o.get('hidden')), key=lambda j: abs(j - i))
            e = {**row[near], 'behind': True, 'filled': True}
        out.append(e)
    return out


def main():
    overrides = json.loads(OVERRIDES.read_text()) if OVERRIDES.exists() else {}
    jobs = [(k, a, d, i, None, None) for k in KINDS for a, n in COUNTS.items() for d in DIRS for i in range(n)]
    with ProcessPoolExecutor() as pool:
        first = list(pool.map(estimate, jobs, chunksize=4))
        bands = {}
        for k in KINDS:
            good = [e['s'] for (kk, *_), e in first if kk == k and not e.get('hidden') and e['score'] > 350]
            med = float(np.median(good))
            bands[k] = (med * (1 - SCALE_BAND), med * (1 + SCALE_BAND))
        print('scale bands', bands)
        second = pool.map(estimate, [(k, a, d, i, bands[k], None) for k, a, d, i, _, _ in jobs], chunksize=4)
        results = {(k, a, d, i): e for (k, a, d, i, *_), e in second}
        redo = []
        for (k, a, d, i), e in results.items():  # steady rows (not attacks): re-fit outliers near the row median
            if a.startswith('attack'):
                continue
            row = [results[(k, a, d, j)] for j in range(COUNTS[a])]
            good = [r for r in row if not r.get('hidden')]
            if len(good) < 2:
                continue
            prior = {'angle': circ_median([r['angle'] for r in good]), 'x': float(np.median([r['x'] for r in good])), 'y': float(np.median([r['y'] for r in good]))}
            if e.get('hidden') or fit.rotation_dist(e['angle'], prior['angle']) > 30 or np.hypot(e['x'] - prior['x'], e['y'] - prior['y']) > 24:
                redo.append((k, a, d, i, bands[k], prior))
        for (k, a, d, i, *_), e in pool.map(estimate, redo, chunksize=2):
            if not e.get('hidden'):
                results[(k, a, d, i)] = e
        print('re-fitted outliers:', len(redo))
    data = {k: {a: {d: [] for d in DIRS} for a in COUNTS} for k in KINDS}
    low = []
    for (kind, anim, d, i), e in results.items():
        if e.get('hidden'):
            low.append(f'{kind}.{anim}.{d}.{i}: not visible (score {e["score"]})')
        if f'{kind}.{anim}.{d}.{i}' in overrides:  # a manual pose also un-hides the frame
            e.pop('hidden', None)
            e.update(overrides[f'{kind}.{anim}.{d}.{i}'])
        data[kind][anim][d].append((i, e))
    for kind in KINDS:
        for anim in COUNTS:
            for d in DIRS:
                row = [e for _, e in sorted(data[kind][anim][d], key=lambda t: t[0])]
                data[kind][anim][d] = fill_hidden(row)
    low += [f'{k}.{a}.{d}.{i}: weak score {e["score"]}' for k in KINDS for a, ds in data[k].items() for d, r in ds.items()
            for i, e in enumerate(r) if e.get('score', 999) < KINDS[k][2] * 1.4 and not e.get('filled')]
    (W2 / 'anchors.json').write_text(json.dumps(data, indent=1))
    OUT_JS.write_text('// GENERATED by tools/estimate-warrior-held-gear-anchors.py -- do not edit by hand (use tools/assets/warrior-held-gear-anchor-overrides.json).\n'
                      '// Per displayed warrior frame {sword, shield}[anim][dir][i]: x,y = grip px in the 192px frame, angle deg (0 = art upright, clockwise +),\n'
                      '// s = frame px per art px, sx = horizontal squeeze, behind = body drawn over the gear, hidden = not drawn.\n'
                      f'export const WARRIOR_HELD_ANCHORS = Object.freeze({json.dumps(data, separators=(",", ":"))});\n', encoding='utf-8')
    print(f'wrote {OUT_JS.name}; {len(low)} notes')
    for line in low[:80]:
        print('  low:', line)
    sheets = {'armed': {a: sheet('armed', a) for a in COUNTS}, 'bare': {a: sheet('bare', a) for a in COUNTS}}
    write_previews(data, COUNTS, DIRS, sheets, W2)


if __name__ == '__main__':
    main()

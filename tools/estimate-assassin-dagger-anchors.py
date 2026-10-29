"""Estimate the per-frame anchors of BOTH assassin daggers (main = weapon slot, off = offhand slot).

 armed frames (idle, attack1-3): dagger pair = armed - weaponless, IoU fit per dagger (tools/assassin_dagger_pair_fit.py), the
   scale is pinned to DAGGER_S (+-8%), the pair is split into main / off (rule in assign_hands).
   A frame whose composed pair does not reproduce the armed dagger region (QA IoU < MIN_QA_IOU) gets {"fallback": true} on BOTH
   hands: the renderer then draws the armed baked frame (whole-frame fallback - both daggers or neither, never half a pair).
 walk / dash / hurt (no armed sheet exists): tools/assassin_fist_tracker.py moves the idle reference pose with the fist.
Anchor fields: x,y = grip px in the 192px frame, angle deg (0 = blade up, clockwise +), s = frame px per art px, sx = horizontal
squeeze, behind = body drawn over the dagger. Overrides: tools/assets/assassin-held-gear-anchor-overrides.json
{"main.attack2.left.1": {"angle": 40}} (un-hides too). Outputs src/data/assassin-held-anchors-generated.js,
assets/hd/assassin/anchors.json, QA sheets assets/hd/assassin/qa/<anim>.png ([armed | weaponless + daggers | diff]).
Usage: python tools/estimate-assassin-dagger-anchors.py
"""
import json
from concurrent.futures import ProcessPoolExecutor

import numpy as np
from PIL import Image

import held_gear_fit as fit
import assassin_dagger_pair_fit as pf
from assassin_fist_tracker import track_fists
from held_gear_preview import compose
from warrior_gear_qa import iou

ROOT, AS = pf.ROOT, pf.ROOT / 'assets' / 'hd' / 'assassin'
OVERRIDES = ROOT / 'tools' / 'assets' / 'assassin-held-gear-anchor-overrides.json'
OUT_JS = ROOT / 'src' / 'data' / 'assassin-held-anchors-generated.js'
FRAME = 192
ARMED = {'idle': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6}
FREE = {'walk': 4, 'dash': 3, 'hurt': 2}
DIRS = ['down', 'up', 'left', 'right']
MIN_QA_IOU, MIN_VISIBLE, BEHIND_BELOW, DAGGER_S = 0.35, 120, 0.35, 0.23
SHEETS = {}


def sheet(name, anim):
    if (name, anim) not in SHEETS:
        SHEETS[name, anim] = Image.open(AS / ('normalized-hd' if name == 'armed' else 'weaponless/normalized-hd') / f'{anim}.png').convert('RGBA')
    return SHEETS[name, anim]


def frame(name, anim, d, i):
    r = DIRS.index(d)
    return sheet(name, anim).crop((i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME))


def assign_hands(d, poses):
    """[pose] x2 -> (main, off). The hero's right hand is the main hand: facing the camera (down) it is screen-left, facing
    away (up) screen-right; in profile it is the near hand facing right (screen-lower) and the far hand facing left. Poses
    within 6 px in y fall back to 'the forward dagger leads' (right: larger x, left: smaller x)."""
    a, b = sorted(poses, key=lambda p: p['x'])  # a = screen-left, b = screen-right
    if d == 'down':
        return a, b
    if d == 'up':
        return b, a
    low, high = sorted(poses, key=lambda p: p['y'], reverse=True)  # low = larger y = nearer the camera
    if abs(low['y'] - high['y']) > 6:
        return (low, high) if d == 'right' else (high, low)
    return (b, a) if d == 'right' else (a, b)


def estimate(job):
    anim, d, i, scales, pinned = job
    armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
    reg, body_op, found, (mask, gx, gy) = pf.fit_pair(armed, wl, scales, pinned)
    if len(found) < 2 or reg.sum() < MIN_VISIBLE:
        return job, None
    _, differs, wl_op = fit.weights(armed, wl)
    out = []
    for score, pose in found:
        vis, share = fit.occlusion(pose, mask, (gx, gy), differs, wl_op)
        out.append(fit.to_js_value(pose, score * 1000, share > 0.2 and vis < BEHIND_BELOW))
    return job, out


def fit_armed():
    """Scale is pinned to DAGGER_S: free fits shrink the template (the blade root is hidden by the arm, so a small template
    pays no over-body penalty) and give daggers half the painted length. DAGGER_S = painted blade length / art length."""
    band = tuple(round(DAGGER_S * (1 + k), 3) for k in (-0.08, -0.04, 0, 0.04, 0.08))
    jobs = [(a, d, i, band, True) for a, n in ARMED.items() for d in DIRS for i in range(n)]
    with ProcessPoolExecutor() as pool:
        results = dict(pool.map(estimate, jobs, chunksize=2))
    return {k[:3]: v for k, v in results.items()}, DAGGER_S


def qa_and_fallback(data, art):
    """Per frame IoU(armed dagger region, composed pair region); poor frames => {fallback} on both hands. Writes QA sheets."""
    rows = {}
    (AS / 'qa').mkdir(exist_ok=True)
    for anim, n in ARMED.items():
        qa = Image.new('RGB', (n * FRAME * 3, FRAME * 4), (20, 20, 30))
        for r, d in enumerate(DIRS):
            for i in range(n):
                armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
                pair = [data[h][anim][d][i] for h in ('main', 'off')]
                comp = wl if pair[0].get('fallback') else compose(wl, [(a, *art) for a in pair])
                a_reg, c_reg = pf.blade_region(armed, wl), pf.blade_region(comp, wl)
                key = f'{anim}.{d}.{i}'
                rows[key] = 0.0 if pair[0].get('fallback') else iou(a_reg, c_reg)
                if rows[key] < MIN_QA_IOU or a_reg.sum() < MIN_VISIBLE:
                    rows[key] = min(rows[key], MIN_QA_IOU - 0.001)
                    for h in ('main', 'off'):
                        data[h][anim][d][i] = {'fallback': True, 'score': 0}
                    comp = armed  # what the game draws for a fallback frame
                diff = np.zeros((FRAME, FRAME, 3), np.uint8)
                diff[a_reg & c_reg], diff[a_reg & ~c_reg], diff[~a_reg & c_reg] = (60, 200, 60), (230, 50, 50), (60, 100, 255)
                for c, img in enumerate((armed, comp)):
                    tile = Image.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                    tile.alpha_composite(img)
                    qa.paste(tile.convert('RGB'), ((i * 3 + c) * FRAME, r * FRAME))
                qa.paste(Image.fromarray(diff), ((i * 3 + 2) * FRAME, r * FRAME))
        qa.save(AS / 'qa' / f'{anim}.png')
    return rows


def main():
    overrides = json.loads(OVERRIDES.read_text()) if OVERRIDES.exists() else {}
    results, med = fit_armed()
    data = {h: {a: {d: [] for d in DIRS} for a in {**ARMED, **FREE}} for h in ('main', 'off')}
    for (a, d, i), pair in sorted(results.items()):
        main_a, off_a = assign_hands(d, pair) if pair else ({'fallback': True, 'score': 0},) * 2
        for h, e in (('main', main_a), ('off', off_a)):
            e = dict(e)
            if f'{h}.{a}.{d}.{i}' in overrides:
                e.pop('fallback', None)
                e.update(overrides[f'{h}.{a}.{d}.{i}'])
            data[h][a][d].append(e)
    art = pf.load_overlay()
    rows = qa_and_fallback(data, art)
    fallbacks = [k for k, v in rows.items() if v < MIN_QA_IOU]
    vals = np.array(list(rows.values()))
    print(f'armed frames {len(vals)}: QA IoU mean {vals.mean():.3f} min {vals.min():.3f}; fallback to armed baked frame: {len(fallbacks)}')
    for k in sorted(fallbacks):
        print('  fallback', k, f'{rows[k]:.2f}')
    track_fists(data, FREE, DIRS, med, overrides, frame)
    (AS / 'anchors.json').write_text(json.dumps({'anchors': data, 'qaIou': rows}, indent=1))
    OUT_JS.write_text('// GENERATED by tools/estimate-assassin-dagger-anchors.py -- do not edit by hand (use tools/assets/assassin-held-gear-anchor-overrides.json).\n'
                      '// Dagger grip anchors per displayed assassin frame [main|off][anim][dir][i]: x,y = grip px in the 192px frame, angle deg (0 = blade up,\n'
                      '// clockwise +), s = frame px per art px, sx = horizontal squeeze, behind = body drawn over the dagger, fallback = overlay does not\n'
                      '// match the armed art here: draw the armed baked frame instead (both hands). main = weapon slot (hero right hand), off = offhand slot.\n'
                      f'export const ASSASSIN_HELD_ANCHORS = Object.freeze({json.dumps(data, separators=(",", ":"))});\n', encoding='utf-8')
    print('wrote', OUT_JS.name)


if __name__ == '__main__':
    main()

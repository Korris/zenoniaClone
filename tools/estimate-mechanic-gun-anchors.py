"""Estimate the per-frame held-gun anchor for the mechanic from the aligned frame sets (same method as
tools/estimate-cleric-staff-anchors.py: gun = armed pixels outside the weaponless silhouette, IoU fit by
tools/held_gear_iou_fit.py, QA IoU per frame, poor frames => {"fallback": true} = draw the armed baked frame).
Differences from the staff: the gun art points RIGHT at angle 0 (any angle is searched) and may be mirrored: a negative
`sx` flips the art horizontally about the grip (left-facing guns), |sx| = horizontal squeeze. Anims without weaponless
frames (attack2, death) are not listed -> the renderer keeps the baked armed frames.
Mirrored anims (attack3 / dash / hurt: displayed left = right row flipped, see asset-atlas-hd-mechanic.js) are fitted on the flipped frames.
Overrides: tools/assets/mechanic-held-gear-anchor-overrides.json {"gun.attack3.left.1": {"angle": 40}} (un-hides too).
Outputs src/data/mechanic-held-anchors-generated.js, assets/hd/mechanic/anchors.json, QA sheets assets/hd/mechanic/qa/<anim>.png.
Usage: python tools/estimate-mechanic-gun-anchors.py
"""
import importlib.util
import json
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps

import held_gear_fit as fit
import held_gear_iou_fit as ifit
from held_gear_preview import compose
from warrior_gear_qa import comp_region, iou

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location('cleric_anchor_estimator', HERE / 'estimate-cleric-staff-anchors.py')
_cleric = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_cleric)
staff_region = _cleric.staff_region  # armed pixels outside the dilated weaponless silhouette (gear sticking out of the body)

ROOT = HERE.parent
MC = ROOT / 'assets' / 'hd' / 'mechanic'
OVERRIDES = ROOT / 'tools' / 'assets' / 'mechanic-held-gear-anchor-overrides.json'
OUT_JS = ROOT / 'src' / 'data' / 'mechanic-held-anchors-generated.js'
FRAME = 192
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack3': 6, 'dash': 3, 'hurt': 2}
MIRRORED = {'attack3', 'dash', 'hurt'}
DIRS = ['down', 'up', 'left', 'right']
COARSE_S, COARSE_SX, COARSE_ANGLES = (0.14, 0.2, 0.26), (0.85, 1.0), np.arange(-180, 180, 20)
MIN_AREA, MIN_FIT_IOU, MIN_QA_IOU, BEHIND_BELOW, MIN_VISIBLE = 70, 0.10, 0.35, 0.35, 120
SHEETS = {}


def sheet(name, anim):
    if (name, anim) not in SHEETS:
        folder = 'normalized-hd' if name == 'armed' else 'weaponless/normalized-hd'
        SHEETS[name, anim] = Image.open(MC / folder / f'{anim}.png').convert('RGBA')
    return SHEETS[name, anim]


def frame(name, anim, d, i):
    """Displayed frame (left of a mirrored anim = right row flipped)."""
    mirror = d == 'left' and anim in MIRRORED
    r = DIRS.index('right' if mirror else d)
    img = sheet(name, anim).crop((i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME))
    return ImageOps.mirror(img) if mirror else img


def load_overlay(tier=0, variant='base', flip=False):
    text = (ROOT / 'src' / 'data' / 'mechanic-gun-overlay-manifest-generated.js').read_text(encoding='utf-8')
    e = json.loads(text[text.index('Object.freeze(') + len('Object.freeze('):text.rindex(')')])[tier][variant]
    img = Image.open(ROOT / e['src']).convert('RGBA')
    gx = e['gripX']
    if flip:
        img, gx = ImageOps.mirror(img), img.width - 1 - gx
    return img, gx, e['gripY']


def fit_one(reg, body_op, flip, scales, sxs, angles):
    art, gx, gy = load_overlay(flip=flip)
    return ifit.fit_iou(reg, body_op, np.asarray(art)[..., 3] > 128, (gx, gy), scales, sxs, angles)


def estimate(job):
    anim, d, i = job
    armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
    reg, body_op, _ = staff_region(armed, wl)
    if reg.sum() < MIN_AREA:
        return job, {'hidden': True, 'score': 0}
    best = (-1.0, None, False)
    for flip in (False, True):
        score, pose = fit_one(reg, body_op, flip, COARSE_S, COARSE_SX, COARSE_ANGLES)
        if pose and score > best[0]:
            best = (score, pose, flip)
    score, pose, flip = best
    if pose is None or score < MIN_FIT_IOU:
        return job, {'hidden': True, 'score': round(float(score) * 1000)}
    ss = tuple(sorted({round(max(0.1, pose['s'] + k), 3) for k in (-0.03, -0.015, 0, 0.015, 0.03)}))
    sxs = tuple(sorted({round(min(1.0, max(0.5, pose['sx'] + k)), 3) for k in (-0.15, 0, 0.15)}))
    score, pose = fit_one(reg, body_op, flip, ss, sxs, np.arange(pose['angle'] - 12, pose['angle'] + 13, 3.0))
    pose['angle'] = ((pose['angle'] + 180) % 360) - 180
    art, gx, gy = load_overlay(flip=flip)
    _, differs, _ = fit.weights(armed, wl)
    vis, share = fit.occlusion(pose, np.asarray(art)[..., 3] > 128, (gx, gy), differs, body_op)
    out = fit.to_js_value(pose, score * 1000, share > 0.2 and vis < BEHIND_BELOW)
    out['sx'] = -out['sx'] if flip else out['sx']  # negative = art mirrored about the grip
    return job, out


def compose_frame(wl, a):
    """Composite for anchor `a` (negative sx => mirrored art, drawn with the positive squeeze)."""
    art, gx, gy = load_overlay(flip=a['sx'] < 0)
    return compose(wl, [({**a, 'sx': abs(a['sx'])}, art, gx, gy)])


def qa_and_fallback(data):
    rows = {}
    (MC / 'qa').mkdir(exist_ok=True)
    for anim, n in COUNTS.items():
        qa = Image.new('RGB', (n * FRAME * 3, FRAME * 4), (20, 20, 30))
        for r, d in enumerate(DIRS):
            for i in range(n):
                armed, wl = frame('armed', anim, d, i), frame('weaponless', anim, d, i)
                a = data[anim][d][i]
                comp = wl if a.get('fallback') else compose_frame(wl, a)
                a_reg, _, halo = staff_region(armed, wl)
                c_reg = comp_region(comp, wl) & ~halo
                key = f'{anim}.{d}.{i}'
                rows[key] = 0.0 if a.get('fallback') else iou(a_reg, c_reg)
                if rows[key] < MIN_QA_IOU or a_reg.sum() < MIN_VISIBLE:
                    rows[key] = min(rows[key], MIN_QA_IOU - 0.001)
                    data[anim][d][i] = {'fallback': True, 'score': a.get('score', 0)}
                    comp = armed
                diff = np.zeros((FRAME, FRAME, 3), np.uint8)
                diff[a_reg & c_reg], diff[a_reg & ~c_reg], diff[~a_reg & c_reg] = (60, 200, 60), (230, 50, 50), (60, 100, 255)
                for c, img in enumerate((armed, comp)):
                    tile = Image.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                    tile.alpha_composite(img)
                    qa.paste(tile.convert('RGB'), ((i * 3 + c) * FRAME, r * FRAME))
                qa.paste(Image.fromarray(diff), ((i * 3 + 2) * FRAME, r * FRAME))
        qa.save(MC / 'qa' / f'{anim}.png')
    return rows


def main():
    overrides = json.loads(OVERRIDES.read_text()) if OVERRIDES.exists() else {}
    jobs = [(a, d, i) for a, n in COUNTS.items() for d in DIRS for i in range(n)]
    with ProcessPoolExecutor() as pool:
        results = dict(pool.map(estimate, jobs, chunksize=2))
    data = {a: {d: [] for d in DIRS} for a in COUNTS}
    for (a, d, i), e in sorted(results.items()):
        if f'gun.{a}.{d}.{i}' in overrides:
            e.pop('hidden', None)
            e.update(overrides[f'gun.{a}.{d}.{i}'])
        data[a][d].append({'fallback': True, 'score': e['score']} if e.get('hidden') else e)
    rows = qa_and_fallback(data)
    fallbacks = [k for k, v in rows.items() if v < MIN_QA_IOU]
    vals = np.array(list(rows.values()))
    print(f'frames {len(vals)}: QA IoU mean {vals.mean():.3f} min {vals.min():.3f}; fallback to armed baked frame: {len(fallbacks)}')
    for k in sorted(fallbacks):
        print('  fallback', k, f'{rows[k]:.2f}')
    (MC / 'anchors.json').write_text(json.dumps({'anchors': data, 'qaIou': rows}, indent=1))
    OUT_JS.write_text('// GENERATED by tools/estimate-mechanic-gun-anchors.py -- do not edit by hand (use tools/assets/mechanic-held-gear-anchor-overrides.json).\n'
                      '// Gun grip anchor per displayed mechanic frame [anim][dir][i]: x,y = grip px in the 192px frame, angle deg (0 = barrel points right,\n'
                      '// clockwise +), s = frame px per art px, sx = horizontal squeeze (negative = art mirrored about the grip), behind = body drawn over\n'
                      '// the gun, fallback = overlay does not match the armed art here: draw the armed baked frame instead. attack2 / death are not listed.\n'
                      f'export const MECHANIC_GUN_ANCHORS = Object.freeze({json.dumps(data, separators=(",", ":"))});\n', encoding='utf-8')
    print('wrote', OUT_JS.name)


if __name__ == '__main__':
    main()

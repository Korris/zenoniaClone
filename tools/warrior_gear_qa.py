"""QA for warrior held gear fit: per frame IoU of the gear region (armed - body) vs the composed bare+overlays region.
Writes assets/hd/warrior-v2/qa/<anim>.png ([armed | bare+overlays | diff] per frame; diff: green = both, red = armed only,
blue = overlay only) and prints a table. Usage: python tools/warrior_gear_qa.py [--list N]"""
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

import held_gear_fit as fit
import held_gear_iou_fit as ifit
from held_gear_preview import compose, load_overlay

ROOT = Path(__file__).resolve().parents[1]
W2 = ROOT / 'assets' / 'hd' / 'warrior-v2'
FRAME = 192
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2}
DIRS = ['down', 'up', 'left', 'right']


def region(top, body, sword=False, d=''):
    """Gear region mask: pixels `top` paints that `body` does not (same definition as the fit; swords use a wide
    opening so thin pose-difference noise is dropped)."""
    return ifit.region_mask(top, body, fit.weights, 60, *((5, 0.4, 1.1 if d == 'up' else 1.6) if sword else (3, 0.0)))[0]


def comp_region(comp, body):
    """Pixels the composite changes vs the bare body (alpha or colour), opened, specks removed."""
    a, b = np.asarray(comp).astype(int), np.asarray(body).astype(int)
    m = ((np.abs(a[..., :3] - b[..., :3]).sum(axis=2) > 24) | ((a[..., 3] > 128) != (b[..., 3] > 128))) & (a[..., 3] > 128)
    m = cv2.morphologyEx(m.astype(np.uint8), cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    keep = np.zeros(n, bool)
    keep[1:] = st[1:, cv2.CC_STAT_AREA] >= 60
    return keep[lab]


def iou(a, b):
    u = (a | b).sum()
    return float((a & b).sum() / u) if u else 1.0


def load(name, anim):
    p = W2 / 'normalized-hd' / f'{anim}.png' if name == 'armed' else W2 / name / 'normalized-hd' / f'{anim}.png'
    return Image.open(p).convert('RGBA')


def evaluate(data, write=True):
    art = {k: load_overlay(k) for k in ('shield', 'sword')}
    rows = {}
    for anim, n in COUNTS.items():
        sh = {k: load(k, anim) for k in ('armed', 'bare', 'weaponless')}
        qa = Image.new('RGB', (n * FRAME * 3, FRAME * 4), (20, 20, 30))
        for r, d in enumerate(DIRS):
            for i in range(n):
                box = (i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME)
                armed, bare, wl = (sh[k].crop(box) for k in ('armed', 'bare', 'weaponless'))
                parts = [(data[k][anim][d][i], *art[k]) for k in ('shield', 'sword')]
                comp = compose(bare, parts)
                a_sw, c_sw = region(armed, wl, True, d), comp_region(compose(bare, [parts[1]]), bare)
                a_sh, c_sh = region(wl, bare), comp_region(compose(bare, [parts[0]]), bare)
                a_all, c_all = a_sw | a_sh, c_sw | c_sh
                rows[f'{anim}.{d}.{i}'] = {'all': iou(a_all, c_all), 'sword': iou(a_sw, c_sw), 'shield': iou(a_sh, c_sh)}
                if write:
                    diff = np.zeros((FRAME, FRAME, 3), np.uint8)
                    diff[a_all & c_all] = (60, 200, 60)
                    diff[a_all & ~c_all] = (230, 50, 50)
                    diff[~a_all & c_all] = (60, 100, 255)
                    for c, img in enumerate((armed, comp)):
                        tile = Image.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                        tile.alpha_composite(img)
                        qa.paste(tile.convert('RGB'), ((i * 3 + c) * FRAME, r * FRAME))
                    qa.paste(Image.fromarray(diff), ((i * 3 + 2) * FRAME, r * FRAME))
        if write:
            (W2 / 'qa').mkdir(exist_ok=True)
            qa.save(W2 / 'qa' / f'{anim}.png')
    return rows


if __name__ == '__main__':
    data = json.loads((W2 / 'anchors.json').read_text())
    rows = evaluate(data)
    allv = np.array([v['all'] for v in rows.values()])
    swv = np.array([v['sword'] for v in rows.values()])
    shv = np.array([v['shield'] for v in rows.values()])
    print(f'frames {len(rows)}: IoU all mean {allv.mean():.3f} min {allv.min():.3f} | sword mean {swv.mean():.3f} min {swv.min():.3f} | shield mean {shv.mean():.3f} min {shv.min():.3f}')
    for dname in DIRS:
        v = [x['all'] for k, x in rows.items() if f'.{dname}.' in k]
        s = [x['sword'] for k, x in rows.items() if f'.{dname}.' in k]
        print(f'  {dname}: all {np.mean(v):.3f} sword {np.mean(s):.3f}')
    n = int(sys.argv[sys.argv.index('--list') + 1]) if '--list' in sys.argv else 15
    for k, x in sorted(rows.items(), key=lambda t: t[1]['all'])[:n]:
        print(f'  worst {k}: all {x["all"]:.3f} sword {x["sword"]:.3f} shield {x["shield"]:.3f}')
    json.dump(rows, open(W2 / 'qa' / 'iou.json', 'w'), indent=0)

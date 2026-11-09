"""Fit five generated Wizard head turnarounds; keep Northwind style0.

Outputs full 192px registered down/up/right textures for styles1..5. The
runtime mirrors right for left and discovers the six styles automatically.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import uuid

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261109-exact'
HEADS = ROOT / 'Assets/Resources/Sprites/classes/wizard/heads'
OUT = ROOT / 'Build/wizard-current-reference'
S = 192
spec = importlib.util.spec_from_file_location('exact_hair_fit', ROOT / 'Tools/install-wizard-current-reference.py')
fit = importlib.util.module_from_spec(spec); spec.loader.exec_module(fit)


def components(image):
    a = np.asarray(image)
    labels, _ = ndimage.label(a[..., 3] > 100, np.ones((3, 3)))
    counts = np.bincount(labels.ravel()); counts[0] = 0
    ids = sorted(range(1, len(counts)), key=lambda i: counts[i], reverse=True)[:3]
    assert len(ids) == 3 and min(counts[i] for i in ids) > 1000
    core = np.where(np.isin(labels, ids), labels, 0)
    distance, nearest = ndimage.distance_transform_edt(core == 0, return_indices=True)
    owner = core[tuple(nearest)]
    ids.sort(key=lambda i: np.where(labels == i)[1].mean())
    result = []
    for i in ids:
        part = a.copy(); part[(owner != i) | (distance > 8)] = 0
        im = Image.fromarray(part)
        result.append(im.crop(fit.solid_box(im)))
    return result


def eyes(image):
    a = np.asarray(image).astype(float)
    r, g, b = [a[..., i] for i in range(3)]
    yy, xx = np.indices(a.shape[:2])
    box = fit.solid_box(image)
    mask = ((b > r*1.35) & (b > g*1.08) & (g > 90) & (b > 130)
            & (a[..., 3] > 120) & (yy > box[1]+.60*(box[3]-box[1])))
    labels, n = ndimage.label(mask, np.ones((3, 3)))
    points = []
    for label in range(1, n+1):
        ys, xs = np.where(labels == label)
        if len(xs) >= 3:
            points.append((xs.mean(), ys.mean(), len(xs)))
    pairs = []
    for i, left in enumerate(points):
        for right in points[i+1:]:
            gap = abs(left[0]-right[0])
            if gap > (box[2]-box[0])*.14 and abs(left[1]-right[1]) < (box[3]-box[1])*.12:
                pairs.append(((left[1]+right[1])/2, gap, (left[0]+right[0])/2))
    assert pairs, ('Cannot locate blue eye pair', points)
    y, gap, x = max(pairs)
    return x, y, gap


def meta(path, template):
    path = Path(str(path)+'.meta')
    if path.exists():
        return
    if template:
        import re
        source = Path(str(template)+'.meta')
        if not source.exists():
            source = ROOT / 'Assets/Resources/Sprites/ui/hero-select/hero-wizard.png.meta'
        text = source.read_text(encoding='utf-8')
        text = re.sub(r'guid: [0-9a-f]+', 'guid: '+uuid.uuid4().hex, text, count=1)
    else:
        text = 'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    path.write_text(text, encoding='utf-8')


def install(preview_only=False):
    count = 2 if preview_only else 5
    for style in range(1, count+1):
        assert (ART / 'src' / f'hair{style}-turnaround.png').exists(), f'Missing hairstyle {style}'
    template = {d: Image.open(HEADS / '0' / f'{d}.png').convert('RGBA') for d in ('down', 'up', 'right')}
    tx, ty, gap = eyes(template['down'])
    report = []
    board = Image.new('RGBA', (S*3*3, S*3*count), (36, 43, 55, 255))
    for style in range(1, count+1):
        front, back, right = components(Image.open(ART / 'src' / f'hair{style}-turnaround.png').convert('RGBA'))
        sx, sy, source_gap = eyes(front)
        k = gap/source_gap
        face = fit.largest(fit.skin(np.asarray(front)))
        chin = np.where(face)[0].max()+1
        for col, (direction, im) in enumerate(zip(('down', 'up', 'right'), (front, back, right))):
            refbox = fit.solid_box(template[direction])
            scale = k
            if direction == 'down':
                dx, dy = tx-sx*scale, 107-chin*scale
            else:
                frontbox = fit.solid_box(template['down'])
                scale *= (refbox[3]-refbox[1])/(frontbox[3]-frontbox[1])
                dx = (refbox[0]+refbox[2])/2-im.width*scale/2
                dy = refbox[3]-im.height*scale
            canvas = im.transform((S, S), Image.Transform.AFFINE,
                                  (1/scale, 0, -dx/scale, 0, 1/scale, -dy/scale), Image.Resampling.BICUBIC)
            box = fit.solid_box(canvas)
            assert box and box[0] > 1 and box[1] > 1 and box[2] < 191 and box[3] < 150, (style, direction, box)
            if direction == 'down':
                actual = eyes(canvas)[2]
                assert abs(actual/gap-1) < .05, (style, gap, actual)
            if not preview_only:
                path = HEADS / str(style) / f'{direction}.png'
                fit.save_image(path, canvas)
                meta(path, HEADS / '0' / f'{direction}.png')
                meta(path.parent, None)
            board.alpha_composite(canvas.resize((S*3, S*3), Image.Resampling.NEAREST), (col*S*3, (style-1)*S*3))
            report.append(dict(style=style, direction=direction, bounds=box, uniformScale=scale))
        ImageDraw.Draw(board).text((8, (style-1)*S*3+8), f'STYLE {style}', fill='white')
    OUT.mkdir(parents=True, exist_ok=True)
    board.save(OUT / 'new-hairstyles-turnaround.png')
    (OUT / 'new-hair-verification.json').write_text(json.dumps(report, indent=2))
    print('PASS:', count, 'new hairstyles x 3 directions; down eye spacing matches default within5%; style0 kept')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--preview-only', action='store_true')
    args = parser.parse_args()
    install(args.preview_only)

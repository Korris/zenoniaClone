"""Wizard idle/down repaint: natural sleeves and planted stance; never warp fists.

python Tools/build-wizard-stance-v3.py check [tiers...]
python Tools/build-wizard-stance-v3.py write
python Tools/build-wizard-stance-v3.py verify
"""
import importlib.util
from functools import lru_cache
import json
from pathlib import Path
import shutil
import sys
import uuid

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/stance-v3'
BODY = ROOT / 'Assets/Resources/Sprites/classes/wizard/body'
OLD = ART / 'before-install'
S = 192
WIDTH = [110, 123, 134, 140, 146, 158, 162, 171, 179, 183]
# Costume silhouette may grow sideways; the person must not grow with its tier.
# These landmarks match heads/0/down.png and the unchanged movement rows.
CHIN = 107
SOLE = 189


def load(file):
    spec = importlib.util.spec_from_file_location('stance_' + file.replace('-', '_'), ROOT / 'Tools' / file)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


ref = load('build-wizard-reference-down.py')
gear = ref.gear
outfits = ref.outfits


def folder(t):
    return BODY / f'set-t{t}' if t else BODY


def backup(path):
    dst = OLD / path.relative_to(ROOT)
    if path.exists() and not dst.exists():
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, dst)


def save_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    backup(path)
    path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    meta = Path(str(path)+'.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')


def skin(a):
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    return (r > 170) & (g > 150) & (b > 120) & (r-g > 12) & (g-b > 6) & (r-b < 125) & (a[..., 3] > 150)


def face_mask(a):
    m = skin(a)
    m[round(a.shape[0]*0.58):] = False
    lab, _ = ndimage.label(m, np.ones((3, 3)))
    counts = np.bincount(lab.ravel()); counts[0] = 0
    return lab == counts.argmax()


def fitted(t):
    source = Image.open(ART / f't{t}-body.png').convert('RGBA')
    assert source.getchannel('A').getextrema() == (0, 255), 'Source must have genuine alpha'
    source = source.crop(source.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox())
    a = np.asarray(source).copy()
    face = face_mask(a)
    ys, xs = np.where(face)
    chin, cx = ys.max()+1, (xs.min()+xs.max())/2
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    hair = (r > g*0.95) & (b > r*0.90) & (g > b*0.50) & (b > 80) & (a[..., 3] > 30)
    hair[chin:] = False
    lab, n = ndimage.label(hair, np.ones((3, 3)))
    sizes = np.bincount(lab.ravel()); sizes[0] = 0
    hair = lab == sizes.argmax()
    ring = max(2, round(source.height*0.010))
    cut = ndimage.binary_fill_holes(ndimage.binary_closing(hair | face, iterations=ring))
    cut = ndimage.binary_dilation(cut, iterations=ring)
    cut[chin+2:] = False
    a[cut] = 0
    body = Image.fromarray(a)
    # Fit the anatomy below the chin, not the full painted bounding box: hair
    # volume and tall shoulder ornaments differ between independently drawn sets.
    ky = (SOLE-CHIN)/(source.height-chin)
    box = body.getbbox()
    kx = WIDTH[t]/(box[2]-box[0])
    size = (round(source.width*kx), round(source.height*ky))
    body = body.resize(size, Image.Resampling.LANCZOS)
    x, y = round(98-cx*kx), SOLE-size[1]
    box = body.getbbox()
    x += max(0, 4-(x+box[0]))
    x -= max(0, x+box[2]-188)
    canvas = Image.new('RGBA', (S, S))
    canvas.alpha_composite(body, (x, y))
    arr = np.asarray(canvas).copy()
    labels, count = ndimage.label(arr[..., 3] > 15, np.ones((3, 3)))
    for label in range(1, count+1):
        by, bx = np.where(labels == label)
        if len(bx) < 25 and by.max() < y+chin*ky+4:
            arr[labels == label] = 0
    return Image.fromarray(arr), round(y+chin*ky-CHIN)


def fist(a, side):
    m = skin(a)
    yy, xx = np.indices(m.shape)
    m &= (xx < 84) if side == 0 else (xx > 113)
    m &= (yy > 100) & (yy < 160)
    lab, n = ndimage.label(m, np.ones((3, 3)))
    candidates = []
    for label in range(1, n+1):
        ys, xs = np.where(lab == label)
        if 5 <= len(xs) < 220:
            target = (50, 125) if side == 0 else (143, 133)
            distance = (xs.mean()-target[0])**2+(ys.mean()-target[1])**2
            candidates.append((distance, label))
    if not candidates:
        raise ValueError(f'No fist found, side {side}')
    m = lab == min(candidates)[1]
    ys, xs = np.where(m)
    mask = ndimage.binary_dilation(ndimage.binary_fill_holes(m), iterations=1) & (a[..., 3] > 20)
    return mask, (round(xs.mean(), 2), round(ys.mean(), 2))


@lru_cache(maxsize=10)
def frames(t):
    body, lift = fitted(t)
    result = []
    for less in (0, 1, 2, 1):
        sy = (189-less)/189
        im = body.resize((S, round(S*sy)), Image.Resampling.LANCZOS)
        c = Image.new('RGBA', (S, S))
        c.alpha_composite(im, (0, round(189*(1-sy))))
        a = np.asarray(c)
        left, lp = fist(a, 0); right, rp = fist(a, 1)
        hand = a.copy(); hand[~(left | right)] = 0
        result.append((c, Image.fromarray(hand), lp, rp, sy, lift*sy))
    return result


def data(t, fr):
    held = []
    for part, index, scale, angle in (('staff', 2, 0.447, 0), ('book', 3, 0.25, 8)):
        held.append(dict(part=part, anim='idle', dir='down', frames=[dict(
            x=f[index][0], y=f[index][1], s=scale, angle=angle, sx=1,
            behind=False, hidden=False, fallback=False, art='idle-stance') for f in fr]))
        if part == 'staff':
            canonical_y = frames(0)[0][2][1]
            for a in held[-1]['frames']:
                a['artY'] = round(canonical_y-a['y'], 2)
    head = dict(anims=[dict(anim='idle', dir='down', frames=[dict(x=0, y=f[5], sy=f[4], angle=0) for f in fr])])
    return dict(anchors=held), head


def idle_art():
    manifest = json.loads((ROOT / gear.GEAR).read_text(encoding='utf-8'))
    main_y = frames(0)[0][2][1]
    for key, root in (('weapon', gear.STAFF_LIVE), ('book', gear.BOOK_LIVE)):
        for entry in manifest[key]:
            source = ROOT / root / (entry['name']+'-base.png')
            im = Image.open(source).convert('RGBA')
            if key == 'weapon':
                gx = entry['baseGrip']['gripX']
                gy = entry['baseGrip']['gripY']+(main_y-114)/0.447
            else:
                alpha = np.asarray(im)[..., 3] > 80
                ys, xs = np.where(alpha)
                gx = round(xs.min()+0.18*(xs.max()-xs.min()))
                gy = np.where(alpha[:, gx])[0].max()
            w = 2*int(np.ceil(max(gx, im.width-gx)))+6
            h = 2*int(np.ceil(max(gy, im.height-gy)))+6
            canvas = Image.new('RGBA', (w, h))
            canvas.alpha_composite(im, (round(w/2-gx), round(h/2-gy)))
            path = ROOT / root / entry['name'] / 'idle-stance.png'
            path.parent.mkdir(parents=True, exist_ok=True)
            canvas.save(path)
            outfits.folder_meta(str(path.parent))
            if Path(str(source)+'.meta').exists():
                outfits.meta_copy(str(source), str(path))
    return manifest


def write(tiers=range(10)):
    tiers = list(tiers)
    for t in tiers:
        fr = frames(t)
        held, head = data(t, fr)
        sheets = {}
        for part in ('bare', 'headless', 'hand'):
            path = folder(t) / part / 'idle.png'
            backup(path)
            sheets[part] = Image.open(path).convert('RGBA')
        for f, (body, hand, lp, rp, sy, lift) in enumerate(fr):
            full = body.copy()
            h = Image.open(outfits.HEAD0).convert('RGBA')
            h = h.resize((S, round(S*sy)), Image.Resampling.LANCZOS)
            full.alpha_composite(h, (0, S-h.height+round(lift)))
            for part, im in (('bare', full), ('headless', body), ('hand', hand)):
                sheets[part].paste(im, (f*S, 0))
        for part, im in sheets.items():
            im.save(folder(t) / part / 'idle.png')
        save_json(folder(t) / 'held-anchors.json', held)
        if t:
            save_json(folder(t) / 'head-anchors.json', head)
        else:
            path = ROOT / 'Assets/Resources/Data/classes/wizard/head-anchors.json'
            original = json.loads(path.read_text(encoding='utf-8'))
            original['anims'] = [head['anims'][0] if (a['anim'], a['dir']) == ('idle', 'down') else a for a in original['anims']]
            save_json(path, original)
        print('stance', t, 'hands', fr[0][2:4], 'head offset', fr[0][5])
    idle_art()
    check(tiers, live=True)
    breathing = Image.new('RGBA', (320*4, 320*10), (35, 40, 51, 255))
    hairstyles = Image.new('RGBA', (320*5, 320*10), (35, 40, 51, 255))
    for t in tiers:
        for f in range(4):
            breathing.alpha_composite(compose(t, f=f), (320*f, 320*t))
        for style in range(5):
            hairstyles.alpha_composite(compose(t, style=style), (320*style, 320*t))
    breathing.save(ART / 'breathing-all-tiers.png')
    hairstyles.save(ART / 'hairstyles-all-tiers.png')


def compose(t, f=0, style=0):
    fr = frames(t); held, head = data(t, fr)
    c = Image.new('RGBA', (320, 320))
    c.alpha_composite(fr[f][0], (64, 64))
    h = Image.open(BODY.parent / 'heads' / str(style) / 'down.png').convert('RGBA')
    sy, lift = fr[f][4:6]
    h = h.resize((S, round(S*sy)), Image.Resampling.LANCZOS)
    c.alpha_composite(h, (64, 64+S-h.height+round(lift)))
    manifest = json.loads((ROOT / gear.GEAR).read_text(encoding='utf-8'))
    for item, key, root in ((held['anchors'][0], 'weapon', gear.STAFF_LIVE), (held['anchors'][1], 'book', gear.BOOK_LIVE)):
        name = manifest[key][t]['name']
        path = ROOT / root / name / 'idle-stance.png'
        if path.exists():
            art = Image.open(path).convert('RGBA')
            grip = dict(gripX=art.width/2, gripY=art.height/2)
            anchor = dict(item['frames'][f])
            anchor['y'] += anchor.get('artY', 0)
            gear.chf.place(c, art, grip, anchor, 1, 0, 64)
    c.alpha_composite(fr[f][1], (64, 64))
    return c


def check(tiers, live=False):
    cols = min(5, max(1, len(tiers)))
    out = Image.new('RGBA', (320*cols, 320*((len(tiers)+cols-1)//cols)), (35, 40, 51, 255))
    draw = ImageDraw.Draw(out)
    for i, t in enumerate(tiers):
        fr = frames(t)
        c = compose(t) if live else fr[0][0].copy()
        if not live:
            h = Image.open(outfits.HEAD0).convert('RGBA')
            c.alpha_composite(h, (0, round(fr[0][5])))
            draw2 = ImageDraw.Draw(c)
            for x, y in fr[0][2:4]:
                draw2.ellipse((x-5, y-5, x+5, y+5), outline='red', width=1)
            out.alpha_composite(c, ((i%cols)*320+64, (i//cols)*320+64))
        else:
            out.alpha_composite(c, ((i%cols)*320, (i//cols)*320))
        draw.text(((i%cols)*320+5, (i//cols)*320+5), f'T{t}', fill='white')
    out.save(ART / ('installed.png' if live else 'fist-grid.png'))
    print('check', tiers)


def verify():
    count = 0
    for old in OLD.rglob('idle.png'):
        live = ROOT / old.relative_to(OLD)
        assert np.array_equal(np.asarray(Image.open(old))[S:], np.asarray(Image.open(live))[S:]), live
        count += 1
    for t in range(10):
        fr = frames(t)
        assert abs(fr[0][5]) <= 1, f'T{t}: chin moved away from movement height'
        assert abs(fr[0][0].getbbox()[3]-SOLE) <= 1, f'T{t}: feet moved off baseline'
        installed = Image.open(folder(t) / 'bare/idle.png').convert('RGBA')
        for f in range(4):
            cell = installed.crop((f*S, 0, (f+1)*S, S))
            # Read the actual installed sheet, not just the generated fit.
            bounds = cell.getchannel('A').point(lambda v: 255 if v > 80 else 0).getbbox()
            base = Image.open(BODY / 'bare/idle.png').convert('RGBA').crop((f*S, 0, (f+1)*S, S))
            expected = base.getchannel('A').point(lambda v: 255 if v > 80 else 0).getbbox()
            assert abs(bounds[1]-expected[1]) <= 1, f'T{t}/f{f}: hair height differs from T0'
            assert abs(bounds[3]-expected[3]) <= 1, f'T{t}/f{f}: sole height differs from T0'
        assert fr[0][3][0]-fr[0][2][0] > 65, f'T{t}: hands still tucked against torso'
        head = np.asarray(fr[0][0])
        assert not (head[:, :2, 3] > 100).any() and not (head[:, -2:, 3] > 100).any(), f'T{t}: clipped cloth'
        file = json.loads((folder(t)/'held-anchors.json').read_text(encoding='utf-8'))
        assert len(file['anchors']) == 2
        for a in file['anchors']:
            assert a['anim'] == 'idle' and a['dir'] == 'down' and len(a['frames']) == 4
    path = ROOT / 'Assets/Resources/Data/classes/wizard/head-anchors.json'
    original = json.loads((OLD/path.relative_to(ROOT)).read_text(encoding='utf-8'))
    current = json.loads(path.read_text(encoding='utf-8'))
    other = lambda data: [a for a in data['anims'] if (a['anim'], a['dir']) != ('idle', 'down')]
    assert other(original) == other(current), 'Head anchors for other animations changed'
    print('PASS:', count, 'sheets retain other facings; 40 natural fist pairs, unclipped cloth, per-outfit anchors')


if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'write': write([int(t) for t in sys.argv[2:]] or range(10))
    elif cmd == 'verify': verify()
    elif cmd == 'check': check([int(t) for t in sys.argv[2:]] or [0])

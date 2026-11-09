"""Install the user's ten-tier board into current Wizard idle/down Resources.

Uses faithful per-tier redraws from 4x enlarged user reference crops, with
separated reference-20261108 equipment. Never the Astral V7/V8 redesigns.
Run from any directory: python Tools/install-wizard-current-reference.py
Backups, verification reports and previews go to Build/wizard-current-reference.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage
from scipy.spatial import ConvexHull

ROOT = Path(__file__).resolve().parents[1]
INSTALL_ROOT = ROOT
SRC = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261108'
BODY_SRC = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261109-exact/src'
OUT = ROOT / 'Build/wizard-current-reference'
RES = ROOT / 'Assets/Resources'
BODY = RES / 'Sprites/classes/wizard/body'
DATA = RES / 'Data/classes/wizard'
S, CHIN, SOLE = 192, 94, 189  # Northwind's corrected neck/sole landmarks
HEAD_SCALE = 65/78
STAFF_S, BOOK_S = .447, .25
STAFF_HEIGHTS = [160 + 9*t for t in range(10)]
MAX_SHAFT = 6.0  # frame pixels, narrower than the painted fist at every tier
originals = {}
fits = {}


def remember(path):
    if path.exists() and path not in originals:
        originals[path] = path.read_bytes()
        backup = OUT / 'before-install' / path.relative_to(INSTALL_ROOT)
        if not backup.exists():
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, backup)


def save_image(path, image):
    remember(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def save_json(path, data):
    remember(path)
    path.write_text(json.dumps(data, indent=2), encoding='utf-8')


def solid_box(image):
    return image.getchannel('A').point(lambda v: 255 if v > 80 else 0).getbbox()


def skin(a):
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    return ((r > 170) & (g > 140) & (b > 110) & (r-g > 12)
            & (g-b > 6) & (r-b < 125) & (a[..., 3] > 150))


def largest(mask):
    labels, _ = ndimage.label(mask, np.ones((3, 3)))
    counts = np.bincount(labels.ravel()); counts[0] = 0
    assert counts.max() > 0, 'Missing connected component'
    return labels == counts.argmax()


def costume(t):
    image = Image.open(BODY_SRC / f't{t}-body.png').convert('RGBA')
    raw = np.asarray(image).copy()
    opaque = largest(raw[..., 3] > 180)
    raw[~ndimage.binary_dilation(opaque, iterations=2)] = 0
    image = Image.fromarray(raw)
    image = image.crop(solid_box(image))
    a = np.asarray(image).copy()
    face = skin(a); face[round(image.height*.58):] = False
    face = largest(face)
    ys, xs = np.where(face)
    chin, cx = ys.max()+1, (xs.min()+xs.max())/2
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    hair = (r > g*.95) & (b > r*.9) & (g > b*.5) & (b > 80) & (a[..., 3] > 30)
    hair[chin:] = False
    labels, n = ndimage.label(hair, np.ones((3, 3)))
    counts = np.bincount(labels.ravel()); counts[0] = 0
    selected = []
    for label in range(1, n+1):
        if counts[label] < max(20, counts.max()*.005):
            continue
        hy, hx = np.where(labels == label)
        if hy.mean() < chin-image.height*.12 and abs(hx.mean()-cx) < image.width*.36:
            selected.append(label)
    assert selected, f'T{t}: no hair components found'
    hair = np.isin(labels, selected)
    head_skin = skin(a); head_skin[chin:] = False
    yy, xx = np.indices(head_skin.shape)
    head_skin &= abs(xx-cx) < image.width*.24
    hy, hx = np.where(hair | head_skin)
    points = np.column_stack((hx, hy))
    hull = ConvexHull(points)
    silhouette = Image.new('1', image.size)
    ImageDraw.Draw(silhouette).polygon([tuple(map(int, p)) for p in points[hull.vertices]], fill=1)
    cut = np.asarray(silhouette, dtype=bool)
    cut = ndimage.binary_dilation(cut, iterations=8); cut[chin+2:] = False
    if t == 0:
        # The blue T0 has no lavender costume material; remove a detached
        # lavender nape lock that can otherwise show beside the shorter cuts.
        nape = (r > g*1.02) & (b > g*1.08) & (a[..., 3] > 20)
        nape &= (yy < chin+2) & (abs(xx-cx) < image.width*.35)
        nape = ndimage.binary_dilation(nape, iterations=3); nape[chin+2:] = False
        cut |= nape
    a[cut] = 0
    # Smaller Northwind heads expose old keyed hair outlines. Keep the costume
    # component and its antialiased edge, not detached head/key-color fragments.
    keep = ndimage.binary_dilation(largest(a[..., 3] > 80), iterations=2)
    a[~keep] = 0
    # One scale for both axes: keep the painted cloth/arm/leg proportions.
    k = (SOLE-CHIN)/(image.height-chin)
    body = Image.fromarray(a).resize((round(image.width*k), round(image.height*k)), Image.Resampling.LANCZOS)
    fits[t] = dict(scaleX=body.width/image.width, scaleY=body.height/image.height,
                   neckRow=CHIN, soleRow=SOLE)
    assert abs(fits[t]['scaleX']/fits[t]['scaleY']-1) < .012, fits[t]
    x, y = round(98-cx*k), SOLE-body.height
    box = solid_box(body)
    assert 2 <= x+box[0] and x+box[2] <= 190, f'T{t}: costume clipped'
    frame = Image.new('RGBA', (S, S)); frame.alpha_composite(body, (x, y))
    return frame


def install_northwind_head():
    # Read the original Northwind source, never a previously resized live head.
    spec = importlib.util.spec_from_file_location('northwind_parts', ROOT / 'Tools/install-wizard-northwind-v7.py')
    northwind = importlib.util.module_from_spec(spec); spec.loader.exec_module(northwind)
    sheet = Image.open(ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/northwind-v7/body-sheet.png').convert('RGBA')
    figure = northwind.pieces(sheet, 10)[0]
    a = np.asarray(figure).copy()
    face = skin(a); face[round(figure.height*.58):] = False
    face = largest(face)
    ys, xs = np.where(face)
    chin, cx = ys.max()+1, (xs.min()+xs.max())/2
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    hair = (((r > g*.95) & (b > r*.9) & (g > b*.5) & (b > 80))
            | ((b > r*1.4) & (g > r*1.3) & (b > 100))) & (a[..., 3] > 30)
    hair[chin:] = False
    mask = largest(hair) | face
    mask = ndimage.binary_fill_holes(ndimage.binary_closing(mask, iterations=2))
    mask = ndimage.binary_dilation(mask, iterations=2); mask[chin:] = False
    a[~mask] = 0
    head = Image.fromarray(a)
    box = solid_box(head); head = head.crop(box)
    # Canonical texture keeps the old 78px head/neck107 registration so walk
    # and attack anchors remain valid. Idle alone uses uniform HEAD_SCALE.
    k = 78/head.height
    head = head.resize((round(head.width*k), 78), Image.Resampling.LANCZOS)
    frame = Image.new('RGBA', (S, S))
    frame.alpha_composite(head, (round(98-(cx-box[0])*k), 107-head.height))
    save_image(RES / 'Sprites/classes/wizard/heads/0/down.png', frame)


def head_anchor(sy):
    return dict(x=round(2*(1-HEAD_SCALE), 5),
                y=round((CHIN-S+(S-107)*HEAD_SCALE)*sy, 5),
                s=HEAD_SCALE, sy=sy, angle=0)


def fitted_head(image, sy):
    a = head_anchor(sy); sx, sy = a['s'], a['s']*a['sy']
    return image.transform((S, S), Image.Transform.AFFINE,
                           (1/sx, 0, S/2-(S/2+a['x'])/sx,
                            0, 1/sy, S-(S+a['y'])/sy), Image.Resampling.BICUBIC)


def fist(a, side):
    yy, xx = np.indices(a.shape[:2])
    mask = skin(a) & (yy > 100) & (yy < 155)
    mask &= (xx < 84) if side == 0 else (xx > 110)
    labels, n = ndimage.label(mask, np.ones((3, 3)))
    target = (55, 115) if side == 0 else (135, 130)
    choices = []
    for label in range(1, n+1):
        ys, xs = np.where(labels == label)
        if 28 <= len(xs) <= 300:
            # Pearls and golden trim can share skin tones. Prefer a full fist
            # over a tiny ornament nearer the previous pose's search center.
            choices.append(((xs.mean()-target[0])**2+(ys.mean()-target[1])**2-1.5*len(xs), label))
    assert choices, f'Missing fist {side}'
    mask = labels == min(choices)[1]
    # Ink between fingers can split one fist into several skin components.
    # Include adjacent finger segments before measuring its grip/overlay.
    mask = ndimage.binary_dilation(mask, iterations=3) & skin(a)
    ys, xs = np.where(mask)
    hand = ndimage.binary_dilation(ndimage.binary_fill_holes(mask), iterations=1) & (a[..., 3] > 20)
    return hand, (round(xs.mean(), 2), round(ys.mean(), 2)), int(xs.max()-xs.min()+1)


def slim_shaft(image, grip_y):
    """Narrow the shaft only; preserve the head, end crystal and total length."""
    result = image.copy()
    a = np.asarray(image)
    # The head ornaments occupy the upper part of these upright source staves.
    top, bottom = round(image.height*.45), round(image.height*.87)
    assert top+8 < grip_y < bottom-8, 'Grip not inside shaft'
    cap = MAX_SHAFT/STAFF_S
    for y in range(top, bottom):
        xs = np.where(a[y, :, 3] > 80)[0]
        if not len(xs):
            continue
        width = xs[-1]-xs[0]+1
        blend = min(1, (y-top)/8, (bottom-y)/8)
        k = 1-(1-min(1, cap/width))*blend
        center = (xs[0]+xs[-1])/2
        row = image.crop((0, y, image.width, y+1))
        row = row.transform(row.size, Image.Transform.AFFINE,
                            (1/k, 0, center*(1-1/k), 0, 1, 0), Image.Resampling.BICUBIC)
        result.paste(row, (0, y))
    assert np.array_equal(np.asarray(result)[:top], a[:top])
    assert np.array_equal(np.asarray(result)[bottom:], a[bottom:])
    return result


def icon(image):
    image = image.crop(solid_box(image))
    k = min(56/image.width, 56/image.height)
    image = image.resize((max(1, round(image.width*k)), max(1, round(image.height*k))), Image.Resampling.LANCZOS)
    out = Image.new('RGBA', (64, 64))
    out.alpha_composite(image, ((64-image.width)//2, (64-image.height)//2))
    return out


def centered_art(image, grip):
    gx, gy = grip['gripX'], grip['gripY']
    w = 2*int(np.ceil(max(gx, image.width-gx)))+6
    h = 2*int(np.ceil(max(gy, image.height-gy)))+6
    out = Image.new('RGBA', (w, h))
    out.alpha_composite(image, (w//2-gx, h//2-gy))
    return out


def install():
    OUT.mkdir(parents=True, exist_ok=True)
    assert all((BODY_SRC / f't{t}-body.png').exists() for t in range(10)), 'All10 faithful redraw sources required'
    install_northwind_head()
    manifest_path = DATA / 'held-wizard.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    old_melee = (DATA / 'held-wizard-melee.json').read_bytes()
    head_path = DATA / 'head-anchors.json'
    head_data = json.loads(head_path.read_text(encoding='utf-8'))
    other_heads = [a for a in head_data['anims'] if (a['anim'], a['dir']) != ('idle', 'down')]
    head_image = Image.open(RES / 'Sprites/classes/wizard/heads/0/down.png').convert('RGBA')
    frames, anchors, widths = {}, {}, {}
    for t in range(10):
        folder = BODY / f'set-t{t}' if t else BODY
        body = costume(t)
        frames[t], widths[t] = [], []
        held = [dict(part=p, anim='idle', dir='down', frames=[]) for p in ('staff', 'book')]
        head = dict(anim='idle', dir='down', frames=[])
        sheets = {p: Image.open(folder / p / 'idle.png').convert('RGBA') for p in ('bare', 'headless', 'hand')}
        for p in sheets:
            remember(folder / p / 'idle.png')
        for f, less in enumerate((0, 1, 2, 1)):
            sy = (SOLE-less)/SOLE
            offset = S-round(S*sy)
            frame = Image.new('RGBA', (S, S))
            frame.alpha_composite(body.resize((S, round(S*sy)), Image.Resampling.LANCZOS), (0, offset))
            a = np.asarray(frame)
            left, lp, lw = fist(a, 0); right, rp, _ = fist(a, 1)
            hand = a.copy(); hand[~(left | right)] = 0
            full = frame.copy()
            full.alpha_composite(fitted_head(head_image, sy))
            for p, im in (('bare', full), ('headless', frame), ('hand', Image.fromarray(hand))):
                sheets[p].paste(im, (f*S, 0))
            for entry, pos, scale, angle in ((held[0], lp, STAFF_S, 0), (held[1], rp, BOOK_S, 8)):
                entry['frames'].append(dict(x=pos[0], y=pos[1], s=scale, angle=angle, sx=1,
                                            behind=False, hidden=False, fallback=False, art='idle-stance'))
            head['frames'].append(head_anchor(sy))
            frames[t].append((full, Image.fromarray(hand)))
            widths[t].append(lw)
        for p, sheet in sheets.items():
            save_image(folder / p / 'idle.png', sheet)
        save_json(folder / 'held-anchors.json', dict(anchors=held))
        if t:
            save_json(folder / 'head-anchors.json', dict(anims=[head]))
        else:
            head_data['anims'] = [head if (a['anim'], a['dir']) == ('idle', 'down') else a for a in head_data['anims']]
            save_json(head_path, head_data)
        anchors[t] = held

    report = []
    for t in range(10):
        row = dict(tier=t, characterHeight=160, costumeFit=fits[t])
        for part, key, scale in (('staff', 'weapon', STAFF_S), ('book', 'book', BOOK_S)):
            source = Image.open(SRC / f't{t}-{part}.png').convert('RGBA')
            source = source.crop(solid_box(source))
            k = STAFF_HEIGHTS[t]/scale/source.height if part == 'staff' else (40+1.5*t)/scale/source.width
            art = source.resize((max(1, round(source.width*k)), max(1, round(source.height*k))), Image.Resampling.LANCZOS)
            if part == 'staff':
                gy = round(art.height-(SOLE-114)/scale)
                art = slim_shaft(art, gy)
                xs = np.where(np.asarray(art)[gy, :, 3] > 80)[0]
                gx = round((xs[0]+xs[-1])/2)
                row['shaftWidth'] = round((xs[-1]-xs[0]+1)*scale, 2)
                row['fistWidths'] = widths[t]
                assert row['shaftWidth'] <= MAX_SHAFT+.5, row
                assert row['shaftWidth'] < min(widths[t]), row
                row['staffHeight'] = round((solid_box(art)[3]-solid_box(art)[1])*scale, 2)
            else:
                gx = round(art.width*.18)
                gy = int(np.where(np.asarray(art)[:, gx, 3] > 80)[0].max())
            canvas = Image.new('RGBA', (art.width+6, art.height+6)); canvas.alpha_composite(art, (3, 3))
            grip = dict(w=canvas.width, h=canvas.height, gripX=gx+3, gripY=gy+3)
            entry = manifest[key][t]
            entry.update(baseGrip=grip, enhancedGrip=dict(grip)); entry.pop('bobY', None)
            folder = RES / f'Sprites/classes/wizard/weapons/{part}'
            for variant in ('base', 'enhanced'):
                save_image(folder / f'{entry["name"]}-{variant}.png', canvas)
            # Runtime selects this centered art for outfit idle poses. Keep its
            # pivot on the same measured grip as base/enhanced and attack art.
            save_image(folder / entry['name'] / 'idle-stance.png', centered_art(canvas, grip))
            save_image(RES / f'Sprites/icons/{"weapon" if part == "staff" else "offhand"}-wizard-{t}.png', icon(canvas))
        report.append(row)
        torso = Image.open((BODY / f'set-t{t}' if t else BODY) / 'headless/idle.png').crop((38, 106, 158, 188))
        save_image(RES / f'Sprites/icons/armor-wizard-{t}.png', icon(torso))
    save_json(manifest_path, manifest)
    assert all(b['staffHeight'] > a['staffHeight'] for a, b in zip(report, report[1:])), report
    assert (DATA / 'held-wizard-melee.json').read_bytes() == old_melee
    now = json.loads(head_path.read_text(encoding='utf-8'))
    assert [a for a in now['anims'] if (a['anim'], a['dir']) != ('idle', 'down')] == other_heads
    sheet_count = 0
    for path, old in originals.items():
        if path.name == 'idle.png':
            import io
            before, after = Image.open(io.BytesIO(old)), Image.open(path)
            assert np.array_equal(np.asarray(before)[S:], np.asarray(after)[S:]), path
            sheet_count += 1
    (OUT / 'verification.json').write_text(json.dumps(dict(tiers=report, unchangedOtherFacingSheets=sheet_count,
                                                       nonIdleGearAnchorsUnchanged=True), indent=2))
    spec = importlib.util.spec_from_file_location('wizard_reference_compose', ROOT / 'Tools/compose-hero-frame.py')
    compose = importlib.util.module_from_spec(spec); spec.loader.exec_module(compose)
    board = Image.new('RGBA', (1600, 640), (36, 43, 55, 255)); draw = ImageDraw.Draw(board)
    breathing = Image.new('RGBA', (1280, 3200), (36, 43, 55, 255))
    style_count = 0
    while (RES / f'Sprites/classes/wizard/heads/{style_count}/down.png').exists():
        style_count += 1
    styles = Image.new('RGBA', (320*style_count, 3200), (36, 43, 55, 255))
    def preview(t, f=0, style=0):
        c = Image.new('RGBA', (320, 320))
        folder = BODY / f'set-t{t}' if t else BODY
        body = Image.open(folder / 'headless/idle.png').crop((S*f, 0, S*(f+1), S))
        c.alpha_composite(body, (64, 100))
        sy = (SOLE-(0, 1, 2, 1)[f])/SOLE
        h = Image.open(RES / f'Sprites/classes/wizard/heads/{style}/down.png').convert('RGBA')
        c.alpha_composite(fitted_head(h, sy), (64, 100))
        for index, key, part in ((0, 'weapon', 'staff'), (1, 'book', 'book')):
            entry = manifest[key][t]
            art = Image.open(RES / f'Sprites/classes/wizard/weapons/{part}/{entry["name"]}/idle-stance.png').convert('RGBA')
            anchor = dict(anchors[t][index]['frames'][f]); anchor['y'] += 36
            compose.place(c, art, dict(gripX=art.width/2, gripY=art.height/2), anchor, 1, 0, 64)
        c.alpha_composite(frames[t][f][1], (64, 100))
        return c
    for t in range(10):
        x, y = (t%5)*320, (t//5)*320
        board.alpha_composite(preview(t), (x, y)); draw.text((x+8, y+8), f'T{t}', fill='white')
        for f in range(4):
            breathing.alpha_composite(preview(t, f), (f*320, t*320))
        for style in range(style_count):
            styles.alpha_composite(preview(t, style=style), (style*320, t*320))
    board.save(OUT / 'all-tiers.png')
    breathing.save(OUT / 'breathing.png'); styles.save(OUT / 'hairstyles.png')
    (OUT / 'installed-sha256.json').write_text(json.dumps({str(p.relative_to(INSTALL_ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                                                        for p in originals}, indent=2))
    print('PASS: 10 tiers / 40 breathing frames;', sheet_count, 'sheets preserve other facings; non-idle anchors unchanged')
    print('Staff heights:', [r['staffHeight'] for r in report])
    print('Shaft widths:', [r['shaftWidth'] for r in report])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target', type=Path, default=ROOT)
    parser.add_argument('--body-source', type=Path, default=BODY_SRC)
    parser.add_argument('--output', type=Path, default=OUT)
    args = parser.parse_args()
    INSTALL_ROOT = args.target.resolve()
    assert (INSTALL_ROOT / 'ProjectSettings/ProjectVersion.txt').exists(), 'Expected Unity project/copy'
    if INSTALL_ROOT != ROOT:
        assert (INSTALL_ROOT / 'Assets').resolve() != (ROOT / 'Assets').resolve(), 'Copy must not alias live Assets'
    BODY_SRC, OUT = args.body_source.resolve(), args.output.resolve()
    RES = INSTALL_ROOT / 'Assets/Resources'
    BODY, DATA = RES / 'Sprites/classes/wizard/body', RES / 'Data/classes/wizard'
    spec = importlib.util.spec_from_file_location('wizard_main_hashes', ROOT / 'Tools/install-wizard-northwind-v7.py')
    hashes = importlib.util.module_from_spec(spec); spec.loader.exec_module(hashes)
    before = hashes.digest_live() if INSTALL_ROOT != ROOT else None
    install()
    if before is not None:
        assert hashes.digest_live() == before, 'Main Wizard assets changed during copy installation'
        print('PASS: main Wizard assets unchanged')

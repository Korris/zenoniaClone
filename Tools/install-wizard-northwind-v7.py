"""Export Northwind V7 plus ultimate T9 into a build copy; preserve live assets."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import uuid

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/northwind-v7'
PARTS = ROOT / 'Build/wizard-northwind/extracted'
BUILD = ROOT / 'Build/wizard-northwind'
VARIANT = 'northwind'
STAFF_HEIGHTS = [160 + 9*tier for tier in range(10)]


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'Tools' / file)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def pieces(image, count):
    a = np.asarray(image).copy()
    labels, _ = ndimage.label(a[..., 3] > 150, np.ones((3, 3)))
    sizes = np.bincount(labels.ravel()); sizes[0] = 0
    ids = sorted(range(1, len(sizes)), key=lambda i: sizes[i], reverse=True)[:count]
    assert len(ids) == count and min(sizes[ids]) > 2000
    core = np.where(np.isin(labels, ids), labels, 0)
    distance, nearest = ndimage.distance_transform_edt(core == 0, return_indices=True)
    owner = core[tuple(nearest)]
    ids.sort(key=lambda i: (int(np.where(labels == i)[0].mean() > image.height/2),
                            np.where(labels == i)[1].mean()))
    result = []
    for i in ids:
        part = a.copy(); part[(owner != i) | (distance > 3)] = 0
        part[..., 3][part[..., 3] >= 245] = 255
        im = Image.fromarray(part)
        result.append(im.crop(im.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox()))
    return result


def split():
    PARTS.mkdir(parents=True, exist_ok=True)
    bodies = pieces(Image.open(ART / 'body-sheet.png').convert('RGBA'), 10)
    rgb = np.asarray(Image.open(ART / 'equipment-keyed.png').convert('RGB')).astype(float)
    key = np.array([255, 0, 255])
    distance = np.max(abs(rgb-key), axis=2)
    alpha = np.clip((distance-10)/30, 0, 1)
    unmix = np.clip((rgb-(1-alpha[..., None])*key)/np.maximum(alpha[..., None], .02), 0, 255)
    # Remove magenta matte spill only along the keyed silhouette. Interior
    # amethyst/purple enamel is protected, unlike a global color replacement.
    edge = ndimage.binary_dilation(alpha < .99, iterations=3) & (alpha > 0)
    spill = np.maximum(0, np.minimum(unmix[..., 0], unmix[..., 2])-unmix[..., 1]-5)
    spill *= edge
    unmix[..., 0] -= spill
    unmix[..., 2] -= spill
    rgba = np.dstack((unmix, alpha*255)).astype(np.uint8)
    equipment = pieces(Image.fromarray(rgba), 20)
    for t, body in enumerate(bodies):
        body.save(PARTS / f't{t}-body.png')
        pair = equipment[t*2:t*2+2]
        pair.sort(key=lambda im: im.height/im.width, reverse=True)
        assert pair[0].height/pair[0].width > 1.4 and pair[1].height/pair[1].width < 1.6
        pair[0].save(PARTS / f't{t}-staff.png')
        pair[1].save(PARTS / f't{t}-book.png')
    if VARIANT == 'northwind':
        source = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/northwind-v8/t9-separated.png'
        parts = pieces(Image.open(source).convert('RGBA'), 3)
        staff = max(parts, key=lambda im: im.height/im.width)
        book = min(parts, key=lambda im: im.height/im.width)
        body = next(im for im in parts if im is not staff and im is not book)
        for name, im in (('staff', staff), ('book', book), ('body', body)):
            im.save(PARTS / f't9-{name}.png')


def digest_live():
    paths = list((ROOT / 'Assets/Resources/Sprites/classes/wizard').rglob('*'))
    paths += list((ROOT / 'Assets/Resources/Data/classes/wizard').rglob('*'))
    paths += list((ROOT / 'Assets/Resources/Sprites/icons').glob('*wizard*'))
    return {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in paths if p.is_file()}


def slim_shaft(image, grip_y):
    """Fit the shaft to the fist; leave the large staff head and pommel intact."""
    a = np.asarray(image)
    widths = np.zeros(image.height)
    centers = np.zeros(image.height)
    for y in range(image.height):
        xs = np.where(a[y, :, 3] > 100)[0]
        if len(xs):
            widths[y] = xs[-1]-xs[0]+1
            centers[y] = (xs[0]+xs[-1])/2
    width = widths[grip_y]
    assert width > 0
    limit = max(width*2.2, widths.max()*.28)
    top = grip_y
    bottom = grip_y
    while top > 0 and 0 < widths[top-1] <= limit:
        top -= 1
    while bottom < image.height-1 and 0 < widths[bottom+1] <= limit:
        bottom += 1
    top = max(top, round(image.height*.40))
    bottom = min(bottom, round(image.height*.84))
    factor = min(.62, 18/width)
    fade = max(5, round(image.height*.025))
    result = image.copy()
    for y in range(top, bottom+1):
        blend = min(1, (y-top)/fade, (bottom-y)/fade)
        scale = 1-(1-factor)*max(0, blend)
        row = image.crop((0, y, image.width, y+1))
        row = row.transform((image.width, 1), Image.Transform.AFFINE,
            (1/scale, 0, centers[y]*(1-1/scale), 0, 1, 0), Image.Resampling.BICUBIC)
        result.paste(row, (0, y))
    assert np.array_equal(np.asarray(result)[:top], a[:top])
    assert np.array_equal(np.asarray(result)[bottom+1:], a[bottom+1:])
    return result, dict(beforePx=round(width*.447, 2), afterPx=round(width*factor*.447, 2), rows=[top, bottom])


def install(target):
    target = target.resolve()
    assert target.is_relative_to(ROOT / 'Build') and target != ROOT / 'Build', 'Build copy only'
    assert (target / 'ProjectSettings/ProjectVersion.txt').exists(), 'Not a Unity copy'
    split()
    original = digest_live()
    s = load('northwind_stance', 'build-wizard-stance-v3.py')
    s.ROOT = target
    s.ART = BUILD / 'previews'
    s.ART.mkdir(parents=True, exist_ok=True)
    s.OLD = BUILD / 'before-install'
    s.BODY = target / 'Assets/Resources/Sprites/classes/wizard/body'
    # Keep the source's head/body balance inside the same 160px standing height.
    # An oversized 78px head previously left only 82px for these longer coats,
    # flattening them vertically by 18-23% against the horizontal art scale.
    s.CHIN = 94
    s.outfits.HEAD0 = str(s.BODY.parent / 'heads/0/down.png')
    g = s.gear
    for name in ('GEAR', 'STAFF_LIVE', 'BOOK_LIVE', 'ICONS'):
        setattr(g, name, str(target / getattr(g, name)))

    def skin(a):
        r, green, b = [a[..., i].astype(float) for i in range(3)]
        return ((r > 180) & (green > 145) & (b > 125) & (r-green > 25)
                & (green-b > 12) & (r-b < 120) & (a[..., 3] > 150))
    if VARIANT == 'northwind':
        s.skin = skin

    def fist(a, side):
        r, green, b = [a[..., i].astype(float) for i in range(3)]
        yy, xx = np.indices(a.shape[:2])
        mask = ((r > 170) & (green > 150) & (b > 120) & (r-green > 12)
                & (green-b > 6) & (r-b < 125) & (a[..., 3] > 150))
        mask &= ((xx < 84) if side == 0 else (xx > 113)) & (yy > 99) & (yy < 150)
        labels, _ = ndimage.label(mask, np.ones((3, 3)))
        choices = []
        tx, ty = (54, 112) if side == 0 else (128, 128)
        if VARIANT == 'astral' and side == 0:
            tx, ty = 48, 107
        for label in range(1, labels.max()+1):
            ys, xs = np.where(labels == label)
            if 8 <= len(xs) < 220:
                choices.append(((xs.mean()-tx)**2+(ys.mean()-ty)**2, label))
        assert choices, f'Missing fist {side}'
        mask = labels == min(choices)[1]
        ys, xs = np.where(mask)
        hand = ndimage.binary_dilation(ndimage.binary_fill_holes(mask), iterations=1) & (a[..., 3] > 20)
        return hand, (round(xs.mean(), 2), round(ys.mean(), 2))
    s.fist = fist

    def extract(t):
        im = Image.open(PARTS / f't{t}-body.png').convert('RGBA')
        im = im.crop(im.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox())
        a = np.asarray(im).copy()
        face = s.face_mask(a)
        chin = np.where(face)[0].max()+1
        r, green, b = [a[..., i].astype(float) for i in range(3)]
        white = (r > green*.95) & (b > r*.9) & (green > b*.5) & (b > 80)
        cyan = (b > r*1.4) & (green > r*1.3) & (b > 100)
        hair = (white | cyan) & (a[..., 3] > 30)
        hair[chin:] = False
        labels, _ = ndimage.label(hair, np.ones((3, 3)))
        sizes = np.bincount(labels.ravel()); sizes[0] = 0
        hair = labels == sizes.argmax()
        ring = max(2, round(im.height*.008))
        mask = ndimage.binary_fill_holes(ndimage.binary_closing(hair | face, iterations=ring))
        mask = ndimage.binary_dilation(mask, iterations=ring); mask[chin+2:] = False
        head = a.copy(); head[~mask] = 0
        body = a.copy(); body[mask] = 0
        ys, xs = np.where(face)
        return Image.fromarray(body), Image.fromarray(head), chin, (xs.min()+xs.max())/2

    def fitted(t):
        body, head, chin, cx = extract(t)
        ky = (189-s.CHIN)/(body.height-chin)
        kx = ky  # preserve the painted armour/cloth aspect ratio exactly
        box = body.getbbox()
        assert (box[2]-box[0])*kx <= 180, f'T{t}: uniform-scale costume needs more frame room'
        size = round(body.width*kx), round(body.height*ky)
        body = body.resize(size, Image.Resampling.LANCZOS)
        x, y = round(98-cx*kx), 189-size[1]
        box = body.getbbox()
        x += max(0, 4-(x+box[0])); x -= max(0, x+box[2]-188)
        frame = Image.new('RGBA', (192, 192)); frame.alpha_composite(body, (x, y))
        return frame, round(y+chin*ky-s.CHIN)

    s.fitted = fitted
    body0, head, chin0, cx0 = extract(0)
    box = head.getbbox()
    if VARIANT == 'astral':
        # Preserve the unmodified design's whole-character ratio. Derive the
        # head/neck landmarks from T0 instead of forcing a preselected head size.
        k0 = 160/body0.height
        s.CHIN = 189-round((body0.height-chin0)*k0)
        head = head.crop(box).resize((round((box[2]-box[0])*k0), round((box[3]-box[1])*k0)), Image.Resampling.LANCZOS)
        at = (round(98-(cx0-box[0])*k0), 29+round(box[1]*k0))
    else:
        head = head.crop(box).resize((79, 65), Image.Resampling.LANCZOS)
        at = (59, 29)
    frame = Image.new('RGBA', (192, 192)); frame.alpha_composite(head, at)
    if VARIANT == 'northwind':
        # User selected Astral's white default hair for Northwind.
        source = Image.open(ROOT / 'Assets/Resources/Sprites/classes/wizard/heads/0/down.png').convert('RGBA')
        k = 65/78
        frame = source.transform((192, 192), Image.Transform.AFFINE,
            (1/k, 0, 98-98/k, 0, 1/k, 107-s.CHIN/k), Image.Resampling.BICUBIC)
    s.backup(Path(s.outfits.HEAD0)); frame.save(s.outfits.HEAD0)
    for style in range(1, 5):
        source = Image.open(ROOT / f'Assets/Resources/Sprites/classes/wizard/heads/{style}/down.png').convert('RGBA')
        sx, sy = 79/83, 65/78
        if VARIANT == 'astral':
            sx = sy = (s.CHIN-29)/78
        head = source.transform((192, 192), Image.Transform.AFFINE,
            (1/sx, 0, 98-98/sx, 0, 1/sy, 107-s.CHIN/sy), Image.Resampling.BICUBIC)
        path = s.BODY.parent / f'heads/{style}/down.png'
        s.backup(path); head.save(path)
    if VARIANT == 'northwind':
        # The third selection (index2) was orange and unrelated to this set.
        for direction in ('down', 'up', 'right'):
            source = s.BODY.parent / f'heads/0/{direction}.png'
            path = s.BODY.parent / f'heads/2/{direction}.png'
            s.backup(path); Image.open(source).save(path)
    manifest_path = Path(g.GEAR)
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    glow = ['#429aff', '#cfa674', '#3db54c', '#f78837', '#9aa7b5',
            '#be3349', '#26c9c3', '#efbf34', '#a85ce0', '#dbeeff']
    if VARIANT == 'astral':
        glow = ['#429aff', '#ead8c9', '#ec85ba', '#33b5b0', '#eb8b33',
                '#716b80', '#efbe38', '#2fb550', '#a365df', '#df3540']

    shaft_report = []
    def export(kind, tier):
        part, scale = ('staff', .447) if kind == 'weapon' else ('book', .25)
        source = Image.open(PARTS / f't{tier}-{part}.png').convert('RGBA')
        staff_height = STAFF_HEIGHTS[tier] if STAFF_HEIGHTS is not None else 160*(1+.018*tier)
        k = (staff_height/scale/source.height) if part == 'staff' else (40/scale/source.width)
        im = source.resize((max(1, round(source.width*k)), max(1, round(source.height*k))), Image.Resampling.LANCZOS)
        if part == 'staff' and VARIANT == 'northwind':
            im, report = slim_shaft(im, round(im.height-(189-114)/scale))
            shaft_report.append(dict(tier=tier, **report))
        canvas = Image.new('RGBA', (im.width+6, im.height+6)); canvas.alpha_composite(im, (3, 3))
        mask = np.asarray(canvas)[..., 3] > 100
        if part == 'staff':
            gy = round(canvas.height-(189-114)/scale)
            xs = np.where(mask[gy])[0]
            gx = round((xs.min()+xs.max())/2)
        else:
            gx = round(canvas.width*.18)
            gy = np.where(mask[:, gx])[0].max()
        grip = dict(w=canvas.width, h=canvas.height, gripX=int(gx), gripY=int(gy))
        entry = manifest[kind][tier]
        entry.update(glow=glow[tier], baseGrip=grip, enhancedGrip=dict(grip))
        entry.pop('bobY', None)
        root = Path(g.STAFF_LIVE if part == 'staff' else g.BOOK_LIVE)
        for variant in ('base', 'enhanced'):
            canvas.save(root / f'{entry["name"]}-{variant}.png')
        g.icon(np.asarray(canvas).astype(float), str(Path(g.ICONS) / f'{"weapon" if part == "staff" else "offhand"}-wizard-{tier}.png'), part == 'staff')

    for tier in range(10):
        export('weapon', tier); export('book', tier)
    if shaft_report:
        (BUILD / 'shaft-width-report.json').write_text(json.dumps(shaft_report, indent=2))
    manifest_path.write_text(json.dumps(manifest, separators=(',', ':')), encoding='utf-8')
    s.write()
    s.check(list(range(10)))
    s.verify()
    if STAFF_HEIGHTS is not None:
        heights = []
        for entry in manifest['weapon']:
            image = Image.open(Path(g.STAFF_LIVE) / f'{entry["name"]}-base.png')
            box = image.getchannel('A').point(lambda v: 255 if v > 80 else 0).getbbox()
            heights.append(round((box[3]-box[1])*.447, 2))
        assert all(b > a for a, b in zip(heights, heights[1:])), heights
        (BUILD / 'staff-height-report.json').write_text(json.dumps(dict(characterHeight=160, staffHeights=heights), indent=2))
        print('PASS: staff heights strictly increase T0-T9:', heights)
    for tier in range(10):
        folder = s.folder(tier)
        torso = Image.open(folder / 'bare/idle.png').crop((38, 106, 158, 188))
        g.icon(np.asarray(torso).astype(float), str(Path(g.ICONS) / f'armor-wizard-{tier}.png'), False)
    assert digest_live() == original, 'Main Wizard assets changed!'
    (BUILD / 'main-asset-hashes.json').write_text(json.dumps(original, indent=2))
    print(f'PASS: {VARIANT} installed into build copy only; main Wizard hashes unchanged')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('command', choices=('split', 'install'))
    parser.add_argument('--target', type=Path)
    args = parser.parse_args()
    if args.command == 'split': split()
    else:
        assert args.target is not None
        install(args.target)

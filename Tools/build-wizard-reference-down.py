"""Install the user's 2026-11-08 two-row Wizard ladder, idle/down first.

python Tools/build-wizard-reference-down.py split [tiers...]
python Tools/build-wizard-reference-down.py write
python Tools/build-wizard-reference-down.py preview

Sources and original files are kept alongside the enlarged reference. Only
idle row zero is replaced; the existing head styles and other body rows stay.
"""
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Tools'))
ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261108'
BODY = ROOT / 'Assets/Resources/Sprites/classes/wizard/body'
BACKUP = ART / 'before-install'
SIZE = 192
# Preserve the ladder's growing silhouette instead of fitting every costume to
# the same hair-to-sole box. Widths are frame pixels; hairstyles keep their size.
COSTUME_WIDTH = [125, 140, 150, 153, 155, 168, 164, 171, 177, 180]
HEAD_LIFT = [0, 1, 3, 4, 5, 9, 9, 11, 13, 15]
BOOK_WIDTH = [0.25, 0.265, 0.28, 0.29, 0.30, 0.32, 0.33, 0.345, 0.36, 0.38]
POSE_HEAD = ART / 'pose-head-before-user-design.png'


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'Tools' / file)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


outfits = load('reference_outfits', 'build-wizard-outfit-idle-down.py')
gear = load('reference_gear', 'build-wizard-tier-staff-book.py')


def backup(path):
    path = Path(path)
    dst = BACKUP / path.relative_to(ROOT)
    if path.exists() and not dst.exists():
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, dst)


def split(t):
    src = ART / f't{t}-equipment-sheet.png'
    rgba = gear.key_out(str(src), 'green').clip(0, 255).astype(np.uint8)
    labels, n = ndimage.label(rgba[..., 3] > 150, np.ones((3, 3)))
    sizes = np.bincount(labels.ravel())
    biggest = sorted(np.argsort(sizes[1:])[-3:] + 1,
                     key=lambda i: np.where(labels == i)[1].mean())
    boxes = []
    for i in biggest:
        y, x = np.where(labels == i)
        boxes.append((x.min(), y.min(), x.max() + 1, y.max() + 1))
    if any(b[2] >= c[0] for b, c in zip(boxes, boxes[1:])):
        raise ValueError(f'T{t}: equipment overlaps, inspect source before splitting')
    boundaries = [0] + [(boxes[i][2] + boxes[i+1][0]) // 2 for i in range(2)] + [rgba.shape[1]]
    for i, kind in enumerate(('body', 'staff', 'book')):
        a = rgba[:, boundaries[i]:boundaries[i+1]].copy()
        lab, _ = ndimage.label(a[..., 3] > 100, np.ones((3, 3)))
        counts = np.bincount(lab.ravel())
        keep = counts >= 6
        keep[0] = False
        a[~ndimage.binary_dilation(keep[lab], iterations=2)] = 0
        im = Image.fromarray(a)
        im = im.crop(im.getbbox())
        im.save(ART / f't{t}-{kind}.png')
    print('split', t, boxes)


def base_frame():
    # Use the original pose on repeat runs; installed T0 must not become a new fit reference.
    old = BACKUP / BODY.relative_to(ROOT)
    body = old if (old / 'headless/idle.png').exists() else BODY
    im = outfits.cell(str(body / 'headless/idle.png'))
    im.alpha_composite(Image.open(POSE_HEAD if POSE_HEAD.exists() else outfits.HEAD0).convert('RGBA'))
    return im


def skin_mask(a, cx, cy):
    a = a.astype(float)
    yy, xx = np.indices(a.shape[:2])
    skin = gear.fist._skin(a[..., :3]) & (a[..., 3] > 150)
    skin &= ((xx-cx)**2 + (yy-cy)**2) < 15**2
    lab, n = ndimage.label(skin, np.ones((3, 3)))
    candidates = []
    for i in range(1, n+1):
        y, x = np.where(lab == i)
        if len(x) >= 5:
            candidates.append(((x.mean()-cx)**2 + (y.mean()-cy)**2, i))
    if not candidates:
        raise ValueError(f'No skin found near fist {cx},{cy}')
    return lab == min(candidates)[1]


def fitted(t):
    fig = Image.open(ART / f't{t}-body.png').convert('RGBA')
    ref = base_frame()
    top, bot, hx = outfits.metrics(ref)
    ft, fb, _ = outfits.metrics(fig)
    k = (bot-top+1)/(fb-ft+1)
    fig = fig.resize((round(fig.width*k), round(fig.height*k)), Image.Resampling.LANCZOS)
    ft, fb, fhx = outfits.metrics(fig)
    fx, rx = outfits.face_x(fig, ft), outfits.face_x(ref, top)
    dx = round((rx-fx) if fx is not None and rx is not None else hx-fhx)
    canvas = Image.new('RGBA', (SIZE, SIZE))
    canvas.alpha_composite(fig, (dx, bot-fb))
    a = np.asarray(canvas).copy()
    # Move each complete fist and cuff smoothly onto the existing gear anchors.
    yy, xx = np.indices((SIZE, SIZE), dtype=float)
    for tx, ty in ((60, 114), (124, 132)):
        m = skin_mask(a, tx, ty)
        y, x = np.where(m)
        sx, sy = x.mean(), y.mean()
        dx, dy = tx-sx, ty-sy
        radius = np.sqrt((xx-tx)**2+(yy-ty)**2)
        weight = np.clip((23-radius)/14, 0, 1)
        weight = weight*weight*(3-2*weight)
        a = np.stack([ndimage.map_coordinates(a[..., c], [yy-dy*weight, xx-dx*weight],
                    order=1, mode='constant', cval=0) for c in range(4)], axis=-1).astype(np.uint8)
    return Image.fromarray(a)


def tier_dir(t):
    return BODY / f'set-t{t}' if t else BODY


def install_head():
    source = Image.open(ART / 'default-head-user-design.png').convert('RGBA')
    assert source.getchannel('A').getextrema() == (0, 255), 'Head needs genuine transparency'
    bbox = source.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    source = source.crop(bbox)
    k = min(83/source.width, 80/source.height)
    source = source.resize((round(source.width*k), round(source.height*k)), Image.Resampling.LANCZOS)
    frame = Image.new('RGBA', (SIZE, SIZE))
    frame.alpha_composite(source, (round(98.5-source.width/2), 107-source.height))
    path = Path(outfits.HEAD0)
    backup(ROOT / path)
    frame.save(path)
    print('default head from user design', source.size, frame.getbbox())


def write_bodies():
    # Head-mask extraction needs the original base sheets until every outfit is cut.
    for part in ('bare', 'hand', 'headless'):
        path = BODY / part / 'idle.png'
        original = BACKUP / path.relative_to(ROOT)
        if original.exists():
            current = Image.open(path).convert('RGBA')
            current.paste(Image.open(original).convert('RGBA').crop((0, 0, 768, 192)), (0, 0))
            current.save(path)
    for t in range(10):
        dst = BODY / f'set-t{t}'
        frame = fitted(t)
        _, sole, _ = outfits.metrics(frame)
        sheets = {}
        for part in ('bare', 'hand'):
            source = tier_dir(t) / part / 'idle.png'
            backup(source)
            if not source.exists():
                source = BODY / part / 'idle.png'
            sheets[part] = Image.open(source).convert('RGBA')
        for f, less in enumerate(outfits.BREATH):
            # Full-cell compression anchored at the sole, same breathing as the base pose.
            fr = Image.new('RGBA', (SIZE, SIZE))
            s = (sole+1-less)/(sole+1)
            small = frame.resize((SIZE, round(SIZE*s)), Image.Resampling.LANCZOS)
            fr.alpha_composite(small, (0, round((sole+1)*(1-s))))
            sheets['bare'].paste(fr, (f*SIZE, 0))
            a = np.asarray(fr).copy()
            mask = skin_mask(a, 60, 114) | skin_mask(a, 124, 132)
            mask = ndimage.binary_dilation(mask, iterations=1) & (a[..., 3] > 30)
            a[~mask] = 0
            sheets['hand'].paste(Image.fromarray(a), (f*SIZE, 0))
        for part, sh in sheets.items():
            path = dst / part / 'idle.png'
            path.parent.mkdir(parents=True, exist_ok=True)
            backup(path)
            sh.save(path)
            outfits.meta_copy(str(BODY / part / 'idle.png'), str(path))
            outfits.folder_meta(str(path.parent))
        outfits.folder_meta(str(dst))
        backup(dst / 'headless/idle.png')
        subprocess.run([sys.executable, str(ROOT / 'Tools/build-head-layer.py'),
                        'wizard', 'outfit-headless', f'set-t{t}'], check=True)
        path = dst / 'headless/idle.png'
        # The generic ten-pixel hair cleanup erases pale crystal pauldrons and
        # gold dragon wings next to the head. Cut against the actual head art,
        # with only a narrow fringe cleanup, so those costume shapes survive.
        hl = Image.open(path).convert('RGBA')
        reference_head = np.asarray(Image.open(POSE_HEAD if POSE_HEAD.exists() else outfits.HEAD0).convert('RGBA'))[..., 3] > 20
        for f in range(4):
            a = np.asarray(sheets['bare'].crop((f*SIZE, 0, (f+1)*SIZE, SIZE))).copy()
            r, g, b = (a[..., i].astype(float) for i in range(3))
            pale_hair = (b > r*0.94) & (g > b*0.67) & (r > 130)
            dark = a[..., :3].mean(-1) < 75
            near = ndimage.binary_dilation(reference_head, iterations=3)
            cut = reference_head | (near & (pale_hair | dark))
            cut[109:] = False
            a[cut] = 0
            hl.paste(Image.fromarray(a), (f*SIZE, 0))
        hl.save(path)
        outfits.meta_copy(str(BODY / 'bare/idle.png'), str(path))
        outfits.folder_meta(str(path.parent))
    for part in ('bare', 'hand', 'headless'):
        path = BODY / part / 'idle.png'
        backup(path)
        sh = Image.open(path).convert('RGBA')
        new = Image.open(BODY / 'set-t0' / part / 'idle.png').convert('RGBA')
        sh.paste(new.crop((0, 0, SIZE*4, SIZE)), (0, 0))
        sh.save(path)
        outfits.meta_copy(str(BODY / 'bare/idle.png'), str(path))


def grow_costumes():
    """Expand the costume below a fixed-size head, with the soles planted.

    Work on the headless layer: scaling a whole character would enlarge hair and
    erase the progression again. Empty fists are moved back to the grip anchors.
    """
    yy, xx = np.indices((SIZE, SIZE), dtype=float)
    head = Image.open(outfits.HEAD0).convert('RGBA')
    anchors = json.loads((ROOT / 'Assets/Resources/Data/classes/wizard/head-anchors.json').read_text(encoding='utf-8'))
    base_anchors = next(a for a in anchors['anims'] if a['anim'] == 'idle' and a['dir'] == 'down')
    for t in range(10):
        folder = tier_dir(t)
        sheets = {p: Image.open(folder / p / 'idle.png').convert('RGBA')
                  for p in ('bare', 'headless', 'hand')}
        bbox = outfits.cell(str(folder / 'headless/idle.png')).getbbox()
        sx = COSTUME_WIDTH[t] / (bbox[2]-bbox[0])
        sy = (82+HEAD_LIFT[t])/82
        left = 98+(bbox[0]-98)*sx
        right = 98+(bbox[2]-98)*sx
        shift = max(0, 6-left) - max(0, right+max(0, 6-left)-186)
        columns = np.arange(SIZE, dtype=float)
        distance = columns-98
        expansion = np.clip((np.abs(distance)-18)/35, 0, 1)
        # Keep the chest, neck and boots at their natural widths; the extra
        # breadth belongs to pauldrons, cape panels and outer robe edges.
        expanded_columns = 98+distance*(1+(sx-1)*expansion)+shift*expansion
        for f, less in enumerate(outfits.BREATH):
            src = np.asarray(outfits.cell(str(folder / 'headless/idle.png'), f=f))
            ys = 189+(yy-189)/sy
            xs = np.interp(xx, expanded_columns, columns, left=-100, right=300)
            a = np.stack([ndimage.map_coordinates(src[..., c], [ys, xs], order=1,
                          mode='constant', cval=0) for c in range(4)], axis=-1).astype(np.uint8)
            # Find fists in the expanded costume, then move each fist + cuff as
            # one local region. Equipment anchors therefore remain compatible.
            for ox, oy, tx, ty in ((60, 114, 60, 114), (124, 132, 124, 132)):
                px, py = np.interp(ox, columns, expanded_columns), 189+(oy-189)*sy
                m = skin_mask(a, px, py)
                y, x = np.where(m)
                dx, dy = tx-x.mean(), ty-y.mean()
                radius = np.sqrt((xx-tx)**2+(yy-ty)**2)
                weight = np.clip((32-radius)/20, 0, 1)
                weight = weight*weight*(3-2*weight)
                a = np.stack([ndimage.map_coordinates(a[..., c], [yy-dy*weight, xx-dx*weight],
                              order=1, mode='constant', cval=0) for c in range(4)], axis=-1).astype(np.uint8)
            costume = Image.fromarray(a)
            labels, count = ndimage.label(a[..., 3] > 20, np.ones((3, 3)))
            for label in range(1, count+1):
                ry, rx = np.where(labels == label)
                if len(rx) < 35 and ry.max() < 113:
                    a[labels == label] = 0
            costume = Image.fromarray(a)
            sheets['headless'].paste(costume, (SIZE*f, 0))
            full = costume.copy()
            sy_head = base_anchors['frames'][f].get('sy', 1)
            h = head.resize((SIZE, round(SIZE*sy_head)), Image.Resampling.LANCZOS)
            full.alpha_composite(h, (0, SIZE-h.height-round(HEAD_LIFT[t]*sy_head)))
            sheets['bare'].paste(full, (SIZE*f, 0))
            hands = a.copy()
            mask = skin_mask(a, 60, 114) | skin_mask(a, 124, 132)
            mask = ndimage.binary_dilation(mask, iterations=1) & (a[..., 3] > 30)
            hands[~mask] = 0
            sheets['hand'].paste(Image.fromarray(hands), (SIZE*f, 0))
        for p, sh in sheets.items():
            sh.save(folder / p / 'idle.png')
        if t:
            frames = [dict(a, y=a.get('y', 0)-HEAD_LIFT[t]*a.get('sy', 1))
                      for a in base_anchors['frames']]
            path = folder / 'head-anchors.json'
            path.write_text(json.dumps({'anims': [dict(anim='idle', dir='down', frames=frames)]}, indent=2), encoding='utf-8')
            meta = Path(str(path)+'.meta')
            if not meta.exists():
                import uuid
                meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
        print('costume', t, 'width', COSTUME_WIDTH[t], 'head lift', HEAD_LIFT[t])


def configure_gear():
    def figures(kind):
        part = 'staff' if kind == 'staffs' else 'book'
        return [np.asarray(Image.open(ART / f't{t}-{part}.png').convert('RGBA')).astype(np.float32)
                for t in range(10)]
    gear.figures = figures
    gear.STAFF_FIT = [(0.03, 1.0), (0.01, 1.0), (-0.04, 1.0), (-0.06, 1.0),
                      (-0.07, 1.0), (-0.14, 1.01), (-0.16, 1.01), (-0.20, 1.02),
                      (-0.24, 1.03), (-0.29, 1.04)]
    gear.STAFF_ANGLE = 0.0
    gear.GLOW = ['#4f9dff', '#d9a74c', '#ff5daf', '#45dfe6', '#ffbd26',
                 '#a35cff', '#ffe14b', '#40cf63', '#a365ff', '#ff472e']
    gear.fist.FIST_BOX = [(113, 119, 138, 147)] * 10
    def books():
        result = []
        for t, f in enumerate(figures('books')):
            w = BOOK_WIDTH[t]*gear.H_GAME/gear.BOOK_S
            small = gear.hs.resize(f, (round(w), round(f.shape[0]*w/f.shape[1])))
            canvas = np.pad(small, ((3, 3), (3, 3), (0, 0)))
            masks = gear.fist.fist_masks(str(BODY), t)
            gx, gy = gear.fist.book_grip(canvas, gear.BOOK_FIST, gear.BOOK_S, masks[0])
            result.append((canvas, dict(w=canvas.shape[1], h=canvas.shape[0],
                           gripX=round(gx), gripY=round(gy)), gear.fist.bob_y(masks)))
        return result
    gear.books = books
    gear.compose_tier = compose_live


def compose_live(t, staff, book, pad=32, f=0, style=0):
    c = Image.new('RGBA', (SIZE+pad*2, SIZE+pad*2))
    c.alpha_composite(outfits.cell(str(tier_dir(t) / 'headless/idle.png'), f=f), (pad, pad))
    head = Image.open(BODY.parent / 'heads' / str(style) / 'down.png').convert('RGBA')
    sy = (1, 0.9935, 0.9875, 0.9935)[f]
    head = head.resize((SIZE, round(SIZE*sy)), Image.Resampling.LANCZOS)
    c.alpha_composite(head, (pad, pad+SIZE-head.height-round(HEAD_LIFT[t]*sy)))
    for art, grip, anchor, scale, angle, bob in (
            (staff[0], staff[1], gear.STAFF_ANCHOR, gear.wns.S, gear.STAFF_ANGLE, 0),
            (book[0], book[1], gear.BOOK_FIST, gear.BOOK_S, gear.BOOK_ANGLE, book[2][f])):
        gear.chf.place(c, gear.hs.to_image(art), grip,
                       dict(x=anchor[0], y=anchor[1]+bob, s=scale, sx=1, angle=angle), 1, 0, pad)
    c.alpha_composite(outfits.cell(str(tier_dir(t) / 'hand/idle.png'), f=f), (pad, pad))
    return c


def write_gear():
    configure_gear()
    for file in (gear.GEAR, gear.MELEE):
        backup(ROOT / file)
    for folder in (gear.STAFF_LIVE, gear.BOOK_LIVE):
        for p in (ROOT / folder).glob('*.png'):
            backup(p)
    for kind in ('weapon', 'offhand', 'armor'):
        for t in range(10):
            backup(ROOT / gear.ICONS / f'{kind}-wizard-{t}.png')
    gear.write_staff()
    gear.write_book()
    # The older book installer predates attack-book anchors. Preserve every
    # non-idle/down entry, especially the four current attack animations.
    path = ROOT / gear.MELEE
    original = json.loads((BACKUP / path.relative_to(ROOT)).read_text(encoding='utf-8'))
    current = json.loads(path.read_text(encoding='utf-8'))
    key = lambda e: (e['part'], e['anim'], e['dir'])
    replacements = {key(e): e for e in current['anchors']
                    if (e['anim'], e['dir']) == ('idle', 'down')}
    current['anchors'] = [replacements.get(key(e), e) for e in original['anchors']]
    known = {key(e) for e in original['anchors']}
    current['anchors'] += [e for k, e in replacements.items() if k not in known]
    path.write_text(json.dumps(current, indent=1), encoding='utf-8')
    for t in range(10):
        fr = outfits.cell(str(tier_dir(t) / 'bare/idle.png'))
        torso = fr.crop((38, 106, 158, 188))
        gear.icon(np.asarray(torso).astype(float), str(ROOT / gear.ICONS / f'armor-wizard-{t}.png'), False)


def preview():
    configure_gear()
    data = json.loads((ROOT / gear.GEAR).read_text(encoding='utf-8'))
    cellw, cellh = 500, 500
    sheet = Image.new('RGB', (cellw*5, cellh*4), (35, 40, 51))
    draw = ImageDraw.Draw(sheet)
    breathing = Image.new('RGBA', (256*4, 256*10), (35, 40, 51, 255))
    for t in range(10):
        parts = []
        for key, folder in (('weapon', gear.STAFF_LIVE), ('book', gear.BOOK_LIVE)):
            entry = data[key][t]
            arr = np.asarray(Image.open(ROOT / folder / f"{entry['name']}-base.png").convert('RGBA')).astype(float)
            parts.append((arr, entry['baseGrip'], entry.get('bobY', [0]*4)))
        game = gear.compose_tier(t, *parts, pad=32)
        for f in range(4):
            breathing.alpha_composite(gear.compose_tier(t, *parts, pad=32, f=f), (f*256, t*256))
        source = Image.open(ART / f't{t}-design-large.png').convert('RGB')
        source = source.resize((round(source.width*0.30), round(source.height*0.30)), Image.Resampling.NEAREST)
        game = game.crop(game.getbbox())
        game = game.resize((game.width*2, game.height*2), Image.Resampling.NEAREST)
        col, row = t%5, (t//5)*2
        sheet.paste(source, (col*cellw+(cellw-source.width)//2, (row+1)*cellh-20-source.height))
        bg = Image.new('RGBA', game.size, (222, 231, 237, 255))
        bg.alpha_composite(game)
        sheet.paste(bg.convert('RGB'), (col*cellw+(cellw-game.width)//2, (row+2)*cellh-20-game.height))
        draw.text((col*cellw+8, row*cellh+8), f'T{t} REFERENCE', fill='white')
        draw.text((col*cellw+8, (row+1)*cellh+8), f'T{t} IDLE DOWN', fill='white')
    sheet.save(ROOT / 'docs/reference/wizard-reference-20261108-vs-sprites.png')
    breathing.save(ART / 'breathing-all-tiers.png')
    # All existing hair styles on the installed costume, staff/book and fist layers.
    styles = Image.new('RGBA', (256*5, 256*10), (35, 40, 51, 255))
    for t in range(10):
        for style in range(5):
            c = Image.new('RGBA', (256, 256))
            c.alpha_composite(outfits.cell(str(tier_dir(t) / 'headless/idle.png')), (32, 32))
            c.alpha_composite(Image.open(BODY.parent / 'heads' / str(style) / 'down.png').convert('RGBA'), (32, 32-HEAD_LIFT[t]))
            for key, folder, anchor, scale, angle in (
                    ('weapon', gear.STAFF_LIVE, gear.STAFF_ANCHOR, gear.wns.S, gear.STAFF_ANGLE),
                    ('book', gear.BOOK_LIVE, gear.BOOK_FIST, gear.BOOK_S, gear.BOOK_ANGLE)):
                entry = data[key][t]
                gear.chf.place(c, Image.open(ROOT / folder / f"{entry['name']}-base.png").convert('RGBA'),
                               entry['baseGrip'], dict(x=anchor[0], y=anchor[1], s=scale, sx=1, angle=angle), 1, 0, 32)
            c.alpha_composite(outfits.cell(str(tier_dir(t) / 'hand/idle.png')), (32, 32))
            styles.alpha_composite(c, (style*256, t*256))
    styles.save(ART / 'hairstyles-all-tiers.png')
    print('preview written')


def verify():
    unchanged = 0
    for path in (BACKUP / BODY.relative_to(ROOT)).rglob('idle.png'):
        live = ROOT / path.relative_to(BACKUP)
        old, new = np.asarray(Image.open(path)), np.asarray(Image.open(live))
        if not np.array_equal(old[SIZE:], new[SIZE:]):
            raise AssertionError(f'Other facing rows changed: {live}')
        unchanged += 1
    path = ROOT / gear.MELEE
    old = json.loads((BACKUP / path.relative_to(ROOT)).read_text(encoding='utf-8'))
    new = json.loads(path.read_text(encoding='utf-8'))
    outside = lambda data: [e for e in data['anchors'] if (e['anim'], e['dir']) != ('idle', 'down')]
    assert outside(old) == outside(new), 'Other animation anchors changed'
    configure_gear()
    for t in range(10):
        for m in gear.fist.fist_masks(str(BODY), t):
            assert m.sum() >= 10, f'T{t}: missing fist overlay'
        for sub in ('bare', 'headless', 'hand'):
            p = tier_dir(t) / sub / 'idle.png'
            assert Image.open(p).size == (768, 768)
            assert Path(str(p)+'.meta').exists()
        if t:
            a = json.loads((tier_dir(t) / 'head-anchors.json').read_text(encoding='utf-8'))['anims'][0]
            assert a['anim'] == 'idle' and a['dir'] == 'down'
            assert a['frames'][0]['y'] == -HEAD_LIFT[t]
    widths = []
    for t in (0, 9):
        box = outfits.cell(str(tier_dir(t) / 'headless/idle.png')).getbbox()
        widths.append(box[2]-box[0])
    assert widths[1] >= widths[0]*1.3, f'Progression lost: T0/T9 widths {widths}'
    print(f'PASS: {unchanged} sheets retain other facing rows; all non-idle/down anchors retained; '
          f'10 tiers x 4 fist overlays, sheet sizes and import metadata valid; T0/T9 widths {widths}.')


if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'split':
        for t in map(int, sys.argv[2:] or range(10)):
            split(t)
    elif cmd == 'write':
        write_bodies()
        grow_costumes()
        write_gear()
        preview()
    elif cmd == 'preview':
        preview()
    elif cmd == 'gear':
        write_gear()
        preview()
    elif cmd == 'verify':
        verify()
    elif cmd == 'head':
        install_head()

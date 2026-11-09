"""Battle-mage idle/down. Reuse the verified sprite/anchor writer, new art only.

python Tools/build-wizard-stance-v4.py write [tiers...]
python Tools/build-wizard-stance-v4.py verify
"""
import importlib.util
from pathlib import Path
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('stance_v3', ROOT / 'Tools/build-wizard-stance-v3.py')
stance = importlib.util.module_from_spec(spec)
spec.loader.exec_module(stance)
stance.ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/stance-v4'
stance.OLD = stance.ART / 'before-install'


def anatomy_widths():
    for t in range(10):
        source = Image.open(stance.ART / f't{t}-body.png').convert('RGBA')
        source = source.crop(source.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox())
        a = np.asarray(source)
        face = stance.face_mask(a)
        chin = np.where(face)[0].max()+1
        r, g, b = [a[..., i].astype(float) for i in range(3)]
        hair = (r > g*.95) & (b > r*.90) & (g > b*.50) & (b > 80) & (a[..., 3] > 100)
        hair[chin:] = False
        labels, _ = ndimage.label(hair, np.ones((3, 3)))
        counts = np.bincount(labels.ravel()); counts[0] = 0
        ys, xs = np.where(labels == counts.argmax())
        # Scale anatomy relative to the head, not an arbitrary tier width.
        # Higher-tier capes stay broader when the artist drew them broader.
        body = a.copy(); body[:chin+2] = 0
        box = Image.fromarray(body).getbbox()
        stance.WIDTH[t] = round((box[2]-box[0])*83/(xs.max()-xs.min()+1))


def fist(a, side):
    # Both fists are now lowered outside the hips. Upper metal/pearl trims can
    # match skin color; do not select those instead of the real hand.
    m = stance.skin(a)
    yy, xx = np.indices(m.shape)
    m &= (xx < 84) if side == 0 else (xx > 113)
    m &= (yy > 136) & (yy < 160)
    labels, _ = ndimage.label(m, np.ones((3, 3)))
    candidates = []
    for label in range(1, labels.max()+1):
        ys, xs = np.where(labels == label)
        if 5 <= len(xs) < 220:
            target_x = 55 if side == 0 else 140
            candidates.append(((xs.mean()-target_x)**2+(ys.mean()-144)**2, label))
    if not candidates:
        raise ValueError(f'No lowered hand, side {side}')
    m = labels == min(candidates)[1]
    ys, xs = np.where(m)
    mask = ndimage.binary_dilation(ndimage.binary_fill_holes(m), iterations=1) & (a[..., 3] > 20)
    return mask, (round(xs.mean(), 2), round(ys.mean(), 2))


def split_sheet():
    sheet = Image.open(stance.ART / 'tier-sheet.png').convert('RGBA')
    raw = np.asarray(sheet)
    labels, _ = ndimage.label(raw[..., 3] > 245)
    counts = np.bincount(labels.ravel()); counts[0] = 0
    bodies = [i for i in range(1, len(counts)) if counts[i] > 10000]
    assert len(bodies) == 10, f'Expected ten separate bodies, got {len(bodies)}'
    # Mantle tips cross nominal grid boundaries: assign connected figures,
    # rather than cutting off the previous tier's cape at a fixed column.
    bodies.sort(key=lambda i: (int(np.where(labels == i)[0].mean() > sheet.height/2),
                               np.where(labels == i)[1].mean()))
    core = np.where(np.isin(labels, bodies), labels, 0)
    distance, nearest = ndimage.distance_transform_edt(core == 0, return_indices=True)
    owner = core[tuple(nearest)]
    for t in range(1, 10):
        a = raw.copy()
        a[(owner != bodies[t]) | (distance > 3)] = 0
        # The generator exported solid art at alpha 252-254, not 255.
        # Normalize that export rounding; keep genuine antialiased edge alpha.
        a[..., 3][a[..., 3] >= 245] = 255
        mask = a[..., 3] > 100
        assert mask.any(), f'T{t}: empty cell'
        assert not mask[:, (0, -1)].any(), f'T{t}: art crosses image boundary'
        cell = Image.fromarray(a)
        cell = cell.crop(cell.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox())
        cell.save(stance.ART / f't{t}-body.png')


def install_head():
    source = Image.open(stance.ART / 't0-body.png').convert('RGBA')
    source = source.crop(source.getchannel('A').point(lambda v: 255 if v > 12 else 0).getbbox())
    a = np.asarray(source)
    face = stance.face_mask(a)
    chin = np.where(face)[0].max()+1
    r, g, b = [a[..., i].astype(float) for i in range(3)]
    hair = (r > g*.95) & (b > r*.90) & (g > b*.50) & (b > 80) & (a[..., 3] > 30)
    hair[chin:] = False
    labels, _ = ndimage.label(hair, np.ones((3, 3)))
    counts = np.bincount(labels.ravel()); counts[0] = 0
    hair = labels == counts.argmax()
    radius = max(2, round(source.height*.010))
    mask = ndimage.binary_fill_holes(ndimage.binary_closing(hair | face, iterations=radius))
    mask = ndimage.binary_dilation(mask, iterations=radius)
    mask[chin+2:] = False
    head = a.copy(); head[~mask] = 0
    head = Image.fromarray(head)
    head = head.crop(head.getbbox()).resize((83, 78), Image.Resampling.LANCZOS)
    frame = Image.new('RGBA', (192, 192))
    frame.alpha_composite(head, (57, 29))
    path = stance.BODY.parent / 'heads/0/down.png'
    stance.backup(path)
    frame.save(path)


def data(t, fr):
    held, head = old_data(t, fr)
    for anchor in held['anchors']:
        if anchor['part'] == 'staff':
            for frame in anchor['frames']:
                frame['angle'] = -10
    return held, head


def verify():
    stance.verify()
    for t in range(10):
        for frame in stance.frames(t):
            assert frame[2][1] > 137 and frame[3][1] > 137, f'T{t}: hand raised above hip'
        held, _ = stance.data(t, stance.frames(t))
        assert all(a['angle'] == -10 for a in held['anchors'][0]['frames'])
    print('PASS: 40 low hand pairs and outward staff angles; anatomy width follows head scale')


old_data = stance.data
stance.data = data
stance.fist = fist

if __name__ == '__main__':
    command = sys.argv[1]
    if command == 'write':
        anatomy_widths()
        install_head()
        stance.write([int(t) for t in sys.argv[2:]] or range(10))
    elif command == 'split':
        split_sheet()
    elif command == 'check':
        anatomy_widths()
        stance.check([int(t) for t in sys.argv[2:]] or range(10))
    elif command == 'verify':
        anatomy_widths()
        verify()
    else:
        raise SystemExit('Expected write, check or verify')

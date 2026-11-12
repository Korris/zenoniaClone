"""Wizard layered head: one shared bald face + gray hair layers per style, from ChatGPT sheets (3 heads on #00FF00: down | right | up).

Inputs  Assets/ArtLibrary/classes/wizard/heads/layered/src/  (bald template + hair-<n> sheets, names below)
Output  Sprites/classes/wizard/heads/face/<dir>.png, heads/hair/<n>/<dir>.png  (192 cells in the old full-head frame space,
        so Data/classes/wizard/head-anchors.json is unchanged); HeroHairLayers draws face + hair, HairDye colours the gray hair.

Fit: the bald sheet's down face (skin) is scaled to the width of the reference face (old heads/4, the one style with a clear face),
chin and centre aligned per facing; the same scale goes to all three heads. Each hair sheet is aligned head by head onto the
bald template by its own face (skin bbox), then only hair is kept: gray body, coloured accents touching it, dark outline touching both.

usage: python Tools/build-wizard-hair-layers.py fit  [--bald FILE] [--hair 0=FILE,1=FILE,...]   -> staging + preview, no game files
       python Tools/build-wizard-hair-layers.py install                                       -> copy staging into Resources
"""
import argparse, json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = f'{REPO}/Assets/Resources'
HEADS = f'{RES}/Sprites/classes/wizard/heads'
SRC = f'{REPO}/Assets/ArtLibrary/classes/wizard/heads/layered/src'
STAGE = f'{REPO}/Assets/ArtLibrary/classes/wizard/heads/layered/staging'
DIRS = ['down', 'right', 'up']
REF_FACE = 4          # old full head with a clear forehead and both eyes: size/position reference for the face
REF_UP = 1            # old up head (neck/ears) reference
F = 192
NECK_GAP = 24         # facing down: half width of the band under the chin cleared of hair (hair behind the neck would cover the collar;
                      # the hair layer is drawn on the body), the side locks outside it fall over the shoulders as in the Northwind art


def rgba(p):
    return np.array(Image.open(p).convert('RGBA')).astype(np.int32)


def skin(a):
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    return (al > 128) & (r - b >= 28) & (r >= g) & (r > 150)


def key_green(a):
    """#00FF00 background -> transparent, with green spill on the antialiased rim pulled back."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # near #00FF00 only: a bright teal lock (e.g. 50,220,160) must survive, so blue has to stay low too
    bg = (g > 150) & (r < 120) & (b < 110) & (g - np.maximum(r, b) > 90)
    out = a.copy(); out[..., 3] = np.where(bg, 0, 255)
    rim = ndi.binary_dilation(bg, iterations=2) & ~bg
    out[..., 1] = np.where(rim, np.minimum(g, np.maximum(r, b) + 10), g)
    return out


def heads_of(sheet):
    """The three largest opaque blobs, left to right (down, right, up): list of (y0, y1, x0, x1)."""
    lab, n = ndi.label(ndi.binary_closing(sheet[..., 3] > 0, iterations=3))
    if n < 3:
        sys.exit('expected 3 heads on the sheet')
    sizes = ndi.sum(np.ones_like(lab), lab, range(1, n + 1))
    objs = ndi.find_objects(lab)
    big = sorted((int(i) for i in np.argsort(sizes)[-3:]), key=lambda i: objs[i][1].start)
    return [(objs[i][0].start, objs[i][0].stop, objs[i][1].start, objs[i][1].stop) for i in big]


def face_box(a):
    """Skin bbox (y0, y1, x0, x1) and its widest row in the lower 60% (cheek width)."""
    ys, xs = np.nonzero(skin(a))
    y0, y1 = ys.min(), ys.max()
    lo = skin(a)[int(y0 + 0.4 * (y1 - y0)):y1 + 1]
    return (y0, y1, xs.min(), xs.max()), lo.sum(1).max()


def place(img, scale, dx, dy):
    """Scale an RGBA crop by `scale` and paste with its top-left at (dx, dy) into an empty 192 cell."""
    im = Image.fromarray(img.astype(np.uint8), 'RGBA')
    im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.LANCZOS)
    cell = Image.new('RGBA', (F, F)); cell.alpha_composite(im, (round(dx), round(dy)))
    a = np.array(cell).astype(np.int32); a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)  # hard pixel-art alpha
    return a


def anchor_point(a, d):
    """Point used to line heads up: chin bottom + face centre (down/right), skin bottom + centre for up (neck)."""
    (y0, y1, x0, x1), _ = face_box(a)
    ys, xs = np.nonzero(skin(a)[y1 - 12:y1 + 1]) if d != 'up' else np.nonzero(skin(a))
    return float(xs.mean()), float(y1)


def fit_bald(path):
    sheet = key_green(rgba(path)); boxes = heads_of(sheet)
    crops = [sheet[y0:y1, x0:x1] for y0, y1, x0, x1 in boxes]
    _, ref_w = face_box(rgba(f'{HEADS}/{REF_FACE}/down.png'))
    _, w = face_box(crops[0])
    s = ref_w / w
    out = {}
    for d, c in zip(DIRS, crops):
        ref = rgba(f'{HEADS}/{REF_UP if d == "up" else REF_FACE}/{d}.png')
        cell = place(c, s, 0, 0)
        (rx, ry), (cx, cy) = anchor_point(ref, d), anchor_point(cell, d)
        out[d] = place(c, s, rx - cx, ry - cy)
    return out, s


def features(bald, d, grow=4):
    """Eyes, brows and mouth of the bald template, grown a little: the hair sheet's own eyes sit a few px off and their whites read
    as light gray hair, so nothing of the hair sheet may stay there (else the eyes double). Only dark or coloured marks in the lower
    half of the face count (the scalp's light shine is not a feature); the back view has none."""
    if d == 'up':
        return np.zeros(bald.shape[:2], bool)
    sk = skin(bald)
    ys, _ = np.nonzero(sk)
    rgb = bald[..., :3]
    mark = (bald[..., 3] > 0) & ~sk & ((rgb.mean(-1) < 150) | (rgb.max(-1) - rgb.min(-1) > 60))
    face = ndi.binary_fill_holes(ndi.binary_closing(sk, iterations=3))
    face[:int(ys.min() + 0.45 * (ys.max() - ys.min()))] = False  # brows sit just below the middle of the skull+face
    lab, n = ndi.label(face & mark)
    if n:  # the head's own outline touches the face rim: keep only marks that do not reach outside the face
        rim = ndi.binary_dilation(~ndi.binary_fill_holes(ndi.binary_closing(sk, iterations=3)), iterations=2)
        mark = np.isin(lab, [i for i in range(1, n + 1) if not (rim & (lab == i)).any()])
    else:
        mark = face & mark
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    brow = mark & (b > r + 40) & (g > r + 20) & (rgb.mean(-1) > 110)   # bright cyan brows: bangs may fall over them (10/11: clearing
    lab, n = ndi.label(mark)                                            # them cut every fringe flat at the brow line, a bowl cut)
    mark = np.isin(lab, [i for i in range(1, n + 1) if (brow & (lab == i)).sum() < 0.5 * (lab == i).sum()]) & mark
    iris = (b > r + 60) & (b > 150)                                     # eyes: only the eye itself + 2 px, so locks may fall between
    lab, n = ndi.label(mark)                                            # and across the eyes as in the Northwind art
    eyes = np.isin(lab, [i for i in range(1, n + 1) if (iris & (lab == i)).any()])
    if d == 'right':                                                    # profile: only marks in front of the eye (mouth); the ear lines
        ex = np.nonzero(eyes)[1].min() if eyes.any() else 0             # stay free so hair may cover the ear (10/11: bald ear looked bad)
        cols = np.arange(mark.shape[1])[None, :]
        mark = mark & (cols >= ex)
    return ndi.binary_dilation(mark & ~eyes, iterations=grow) | ndi.binary_dilation(eyes, iterations=2)


def brows(bald, d):
    """Bright cyan brow strokes of the bald template (front / side), grown 3 px: the hair sheet's own brows sit there."""
    if d == 'up':
        return np.zeros(bald.shape[:2], bool)
    r, g, b = (bald[..., i].astype(int) for i in range(3))
    m = (bald[..., 3] > 0) & (b > r + 40) & (g > r + 20) & (bald[..., :3].mean(-1) > 110) & ~skin(bald)
    m[int(np.nonzero(skin(bald))[0].max() * 0.75):] = False         # not the eyes' blue further down
    return ndi.binary_dilation(m, iterations=3)


def hair_only(a, bald, d):
    """Keep the hair of an aligned full head painted in grays: the light gray body, coloured accents (lock, ribbon) that share a long
    edge with it, and the dark outline next to either. Skin and the face outline drop out; the template's eye/mouth zone is cleared."""
    a = a.copy(); a[features(bald, d)] = 0
    al = a[..., 3] > 0
    mx, mn = a[..., :3].max(-1), a[..., :3].min(-1)
    light = a[..., :3].mean(-1) / 255
    gray = al & (mx - mn <= 34) & ~skin(a)
    body = gray & (light > 0.25)
    near_body = ndi.binary_dilation(body, iterations=3)  # a lock is ringed by its dark outline, so look 3 px out
    accent = al & ~gray & ~skin(a)
    lab, n = ndi.label(accent)
    keep_acc = np.zeros_like(accent)
    bz = brows(bald, d)
    for i in range(1, n + 1):
        comp = lab == i
        if (comp & bz).sum() > 0.5 * comp.sum():                   # the hair sheet's own brow: the face layer has it
            continue
        if (comp & near_body).sum() >= max(3, 0.15 * comp.sum()):
            keep_acc |= comp
    dark = gray & (light <= 0.25) & ndi.binary_dilation(body | keep_acc)
    keep = body | keep_acc | dark
    lab, n = ndi.label(keep)
    if n > 1:  # stray specks (eye glints, key residue) apart from the hair mass
        sizes = ndi.sum(keep, lab, range(1, n + 1))
        keep = np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz >= 30])
    inface = ndi.binary_erosion(skin(bald), iterations=1)              # crumbs of the sheet's own eyes left on the cheek
    lab, n = ndi.label(keep & inface)
    for i in range(1, n + 1):
        if (lab == i).sum() < 30:
            keep &= lab != i
    out = a.copy(); out[~keep] = 0
    return out


def face_scale(sheet, boxes, bald):
    y0, y1, x0, x1 = boxes[0]
    _, w = face_box(sheet[y0:y1, x0:x1]); _, bw = face_box(bald['down'])
    return bw / w


UNDER_OVERLAP = 3     # rows the hair-under layer repeats above the cut: the two layers' soft edges left a 1 px see-through line at the chin row


def split_under(hair, bald):
    """Long hair below the bald chin -> the hair-under layer (drawn behind the body, so the collar and robe cover it as in the
    Northwind art); the rest stays in front. Returns (front, under)."""
    chin = np.nonzero(bald[..., 3] > 128)[0].max()
    front, under = hair.copy(), hair.copy()
    front[chin + 1:] = 0; under[:chin + 1 - UNDER_OVERLAP] = 0
    return front, under


def fill_pinholes(hair, max_px=25):
    """Small holes fully inside the hair (the bald scalp peeking through, 10/11) take the mean colour of the hair around them."""
    al = hair[..., 3] > 0
    holes = ndi.binary_fill_holes(al) & ~al
    lab, n = ndi.label(holes)
    out = hair.copy()
    for i in range(1, n + 1):
        m = lab == i
        if m.sum() > max_px:
            continue
        ring = ndi.binary_dilation(m, iterations=1) & al
        if ring.any():
            out[m, :3] = hair[ring, :3].mean(0); out[m, 3] = 255
    return out


def iris_centre(a):
    """Centre of the blue iris pixels in the lower half of the face, or None."""
    iris = (a[..., 3] > 0) & (a[..., 2] > a[..., 0] + 60) & (a[..., 2] > 150) & (a[..., 1] < a[..., 2] - 25)
    sk = skin(a)
    if not sk.any() or not iris.any():
        return None
    ys = np.nonzero(sk)[0]
    iris[:int(ys.min() + 0.45 * (ys.max() - ys.min()))] = False      # not the cyan forelock / brows
    if iris.sum() < 4:
        return None
    yy, xx = np.nonzero(iris)
    return float(xx.mean()), float(yy.mean())


def fit_hair(path, bald, like=None, under=False):
    """like: an earlier sheet of the same hairstyle whose cheeks show; the scale is its face scale times the ratio of the two
    front hair widths (10/11: long bangs and side locks hide the cheeks and the eyes are redrawn bigger, both scales lied)."""
    sheet = key_green(rgba(path)); boxes = heads_of(sheet)
    if like:
        ls = key_green(rgba(like)); lb = heads_of(ls)
        s_like = face_scale(ls, lb, bald) * (lb[0][3] - lb[0][2]) / (boxes[0][3] - boxes[0][2])
    out = {}
    for d, (y0, y1, x0, x1) in zip(DIRS, boxes):
        c = sheet[y0:y1, x0:x1]
        if d == 'down':  # one drawing scale per sheet, from the front face (hair hides much of the side/back face, so those widths lie)
            s = s_like if like else face_scale(sheet, boxes, bald)
        cell = place(c, s, 0, 0)
        (bx, by), (cx, cy) = anchor_point(bald[d], d), anchor_point(cell, d)
        if d == 'right' and iris_centre(cell) and iris_centre(bald[d]):  # profile: line the eyes up (hair covers ear and cheek, the face
            (bx, by), (cx, cy) = iris_centre(bald[d]), iris_centre(cell)  # outline differs; a chin fit left the sheet's eye beside the game's)
        out[d] = hair_only(place(c, s, bx - cx, by - cy), bald[d], d)
        if d == 'down' and not under:
            out[d] = clear_neck(out[d], bald[d])
        if d == 'right':
            out[d] = clear_jaw(out[d])
        out[d] = fill_pinholes(out[d])
        if under and d != 'up':                                          # the back view keeps its hair over the back
            out[d], out['under-' + d] = split_under(out[d], bald[d])
    return out


JAW = (104, 92, 0.3)  # right profile: no hair in front of the line from under the ear (x 104, y 92) slanting back 0.3 px per row (a beard look otherwise)


def clear_jaw(hair):
    x0, y0, k = JAW
    xx = np.arange(F)[None, :]; yy = np.arange(F)[:, None]
    out = hair.copy()
    out[(yy > y0) & (xx > x0 - (yy - y0) * k)] = 0
    lab, n = ndi.label(out[..., 3] > 0)
    for i in range(1, n + 1):
        if (lab == i).sum() < 12:
            out[lab == i] = 0
    return out


def clear_neck(hair, bald):
    """Remove hair below the bald chin within NECK_GAP of the face centre (front view only)."""
    fa = bald[..., 3] > 128
    ys, xs = np.where(fa)
    chin, cx = ys.max(), (xs.min() + xs.max()) / 2
    out = hair.copy()
    xx = np.arange(F)[None, :]; yy = np.arange(F)[:, None]
    out[(yy > chin) & (np.abs(xx - cx) < NECK_GAP)] = 0
    lab, n = ndi.label(out[..., 3] > 0)                          # crumbs left at the band edge
    for i in range(1, n + 1):
        if (lab == i).sum() < 12:
            out[lab == i] = 0
    return out


def save(a, p):
    os.makedirs(os.path.dirname(p), exist_ok=True); Image.fromarray(a.astype(np.uint8), 'RGBA').save(p)


def preview(bald, hairs):
    tiles = []
    for d in DIRS:
        row = []
        for n in sorted(hairs):
            im = Image.new('RGBA', (F, F), (70, 70, 80, 255))
            im.alpha_composite(Image.fromarray(bald[d].astype(np.uint8)))
            im.alpha_composite(Image.fromarray(hairs[n][d].astype(np.uint8)))
            row.append(np.array(im.crop((40, 20, 152, 124))))
        tiles.append(np.hstack(row))
    sheet = Image.fromarray(np.vstack(tiles))
    sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(f'{STAGE}/preview-face-plus-hair.png')


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('cmd', choices=['fit', 'install'])
    ap.add_argument('--bald', default=f'{SRC}/wizard-bald-template.png')
    ap.add_argument('--hair', default='', help='n=FILE,... (default src/wizard-hair-<n>-gray.png for n = 0..9 that exist)')
    ap.add_argument('--under', default='', help='n,m: styles whose hair below the chin goes to hair-under/ (behind the body)')
    ap.add_argument('--like', default='', help='n=FILE: earlier sheet of style n with visible cheeks, for the scale (see fit_hair)')
    a = ap.parse_args()
    if a.cmd == 'install':
        for sub in ('face', 'hair', 'hair-under'):
            if os.path.isdir(f'{STAGE}/{sub}'):
                shutil.copytree(f'{STAGE}/{sub}', f'{HEADS}/{sub}', dirs_exist_ok=True)
        print('installed', HEADS); return
    hair_files = dict(kv.split('=', 1) for kv in a.hair.split(',') if kv) if a.hair else \
        {str(n): f'{SRC}/wizard-hair-{n}-gray.png' for n in range(10) if os.path.exists(f'{SRC}/wizard-hair-{n}-gray.png')}
    likes = dict(kv.split('=', 1) for kv in a.like.split(',') if kv)
    bald, s = fit_bald(a.bald)
    for d in DIRS:
        save(bald[d], f'{STAGE}/face/{d}.png')
    hairs = {}
    for n, p in sorted(hair_files.items(), key=lambda kv: int(kv[0])):
        hairs[int(n)] = fit_hair(p, bald, likes.get(n), n in a.under.split(','))
        for d in DIRS:
            save(hairs[int(n)][d], f'{STAGE}/hair/{n}/{d}.png')
            if 'under-' + d in hairs[int(n)]:
                save(hairs[int(n)]['under-' + d], f'{STAGE}/hair-under/{n}/{d}.png')
    if hairs:
        preview(bald, hairs)
    print(json.dumps({'bald_scale': round(s, 4), 'styles': sorted(hairs), 'staging': STAGE}))


main()

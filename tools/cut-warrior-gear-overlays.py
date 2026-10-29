"""Cut warrior-sword-overlay-sheet-raw.png / warrior-shield-overlay-sheet-raw.png (5 cols x 4 rows: items 1-5 base,
1-5 +10, 6-10 base, 6-10 +10) into 20 upright PNGs each, a manifest.json, src/data/warrior-<kind>-overlay-manifest-generated.js
(array by tier 0..9) and a contact sheet next to the idle warrior at in-game scale.

The +10 effects bleed into the row above, so rows/cols are cut on a FIXED grid (boundary = emptiest solid-pixel
line near k*H/4); fragments farther than MARGIN px from the main body are dropped. The enhanced art is registered to
the base art by template matching (the glow cloud would otherwise shift the grip).
Usage: python tools/cut-warrior-gear-overlays.py [sword|shield|all]
"""
import colorsys
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from weapon_overlay_geometry import detect_grip, label_components, trim

ROOT = Path(__file__).resolve().parents[1]
NAMES = ['iron', 'bronze', 'crimson', 'flame', 'frost', 'thunder', 'verdant', 'shadow', 'holy', 'dragon']
HAZE, SOLID, MIN_COMP, MARGIN, PAD, WINDOW = 48, 200, 12, 36, 4, 60
MIN_BODY_SCALE, HAZE_KEEP = 0.94, 0.6  # enhanced art: max shrink of the item body, share of glow-cloud opacity kept
KINDS = {  # target = base art height px; grip rule per kind
    'sword': {'sheet': 'warrior-sword-overlay-sheet-raw.png', 'target': 256, 'export': 'WARRIOR_SWORD_OVERLAYS'},
    'shield': {'sheet': 'warrior-shield-overlay-sheet-raw.png', 'target': 224, 'export': 'WARRIOR_SHIELD_OVERLAYS'},
}


def valley(profile, center):
    lo, hi = max(1, center - WINDOW), min(len(profile) - 1, center + WINDOW)
    seg = profile[lo:hi]
    return lo + int(np.argmin(seg))  # first emptiest line


def cell_edges(arr, rows=4, cols=5):
    solid = arr[..., 3] >= SOLID
    h, w = solid.shape
    ys = [0] + [valley(solid.sum(axis=1), round(k * h / rows)) for k in range(1, rows)] + [h]
    xs = [0] + [valley(solid.sum(axis=0), round(k * w / cols)) for k in range(1, cols)] + [w]
    return ys, xs


def cut_cell(arr, y0, y1, x0, x1):
    """Cell crop keeping the main component and fragments (sparkles) within MARGIN of its bbox."""
    sub = arr[y0:y1, x0:x1].copy()
    comps = [c for c in label_components(sub[..., 3] > 0) if len(c[0]) >= MIN_COMP]
    ys, xs = max(comps, key=lambda c: len(c[0]))
    bx0, bx1, by0, by1 = xs.min() - MARGIN, xs.max() + MARGIN, ys.min() - MARGIN, ys.max() + MARGIN
    keep = np.zeros(sub.shape[:2], bool)
    for cy, cx in comps:
        if bx0 <= cx.mean() <= bx1 and by0 <= cy.mean() <= by1:
            keep[cy, cx] = True
    sub[~keep] = 0
    return Image.fromarray(sub, 'RGBA')


def clean(img):
    a = np.array(img)
    a[..., 3] = np.where(a[..., 3] >= HAZE, a[..., 3], 0)
    return Image.fromarray(a, 'RGBA')


def resize_img(im, factor):
    return clean(im.resize((max(1, round(im.width * factor)), max(1, round(im.height * factor))), Image.Resampling.LANCZOS))


def solid_height(img):
    rows = np.nonzero((np.array(img)[..., 3] >= SOLID).any(axis=1))[0]
    return rows[-1] - rows[0] + 1


def axis_extent(im, band=14):
    """(cx, top, bottom) of the item core: solid pixels within `band` px of the median x of the mid-height rows."""
    solid = np.array(im)[..., 3] >= 235
    h = solid.shape[0]
    cx = int(np.median(np.nonzero(solid[int(h * 0.4):int(h * 0.6)])[1]))
    ys = np.nonzero(solid[:, max(0, cx - band):cx + band + 1].any(axis=1))[0]
    return cx, int(ys.min()), int(ys.max())


def register(base, enh):
    """Offset (dx, dy) of base inside enh from the aligned core axis/top/bottom; last value = height ratio (~1)."""
    (bx, bt, bb), (ex, et, eb) = axis_extent(base), axis_extent(enh)
    return ex - bx, round(((et - bt) + (eb - bb)) / 2), (eb - et) / max(1, bb - bt)


def body_cap(base, enh):
    """Scale (<=1, >= MIN_BODY_SCALE) that brings the enhanced item body (solid pixels) back to the base item's area, so
    +N art is not drawn fatter than the base item (its effect is kept, only its drawn size is capped)."""
    area = lambda im: int((np.array(im)[..., 3] >= SOLID).sum())
    return max(MIN_BODY_SCALE, min(1.0, (area(base) / max(1, area(enh))) ** 0.5))


def soften_haze(im):
    """Enhanced art carries a large translucent glow cloud (alpha < SOLID): thin it so it does not swallow the hero."""
    a = np.array(im)
    haze = (a[..., 3] > 0) & (a[..., 3] < SOLID)
    a[..., 3] = np.where(haze, (a[..., 3] * HAZE_KEEP).astype(np.uint8), a[..., 3])
    a[..., 3] = np.where(a[..., 3] >= HAZE, a[..., 3], 0)
    return Image.fromarray(a, 'RGBA')


def glow_hex(enh, base, dx, dy):
    """Dominant hue of bright saturated pixels the enhanced art adds around the (registered) base."""
    e, b = np.array(enh), np.array(base)
    mask = e[..., 3] > 0
    ys, xs = np.nonzero(b[..., 3] > 0)
    mask[np.clip(ys + dy, 0, e.shape[0] - 1), np.clip(xs + dx, 0, e.shape[1] - 1)] = False
    hsv = np.array([colorsys.rgb_to_hsv(*(p / 255.0)) for p in e[mask][:, :3]])
    hsv = hsv[(hsv[:, 1] > 0.25) & (hsv[:, 2] > 0.6)]
    if not len(hsv):
        return '#ffffff'
    hist = np.bincount((hsv[:, 0] * 360).astype(int) % 360, minlength=360).astype(float)
    smooth = sum(np.roll(hist, k) for k in range(-12, 13))
    r, g, b2 = colorsys.hsv_to_rgb(int(np.argmax(smooth)) / 360, 0.65, 1.0)
    return '#%02x%02x%02x' % (round(r * 255), round(g * 255), round(b2 * 255))


def grip_of(kind, base, cfg=None):
    if cfg and cfg.get('grip'):
        return cfg['grip'](base)  # custom rule (cut-cleric-gear-overlays.py)
    if kind == 'sword':
        return detect_grip(base)  # centre of the hilt handle (guard .. pommel midpoint)
    box = base.getbbox()  # shield: centre of the shield = where the arm straps sit behind it
    return (box[0] + box[2]) // 2, (box[1] + box[3]) // 2


def process(kind, i, base, enh, out_dir, cfg):
    base, enh = trim(base, PAD), trim(enh, PAD)
    s = cfg['target'] / solid_height(base)
    base, enh = resize_img(base, s), resize_img(enh, s)
    gx, gy = grip_of(kind, base, cfg)
    ratio = register(base, enh)[2]  # enhanced core is drawn up to ~10% larger: normalize so the item keeps its size
    enh = resize_img(enh, 1 / min(1.2, max(1.0, ratio)))
    enh = resize_img(enh, body_cap(base, enh))
    enh = soften_haze(enh)
    dx, dy, score = register(base, enh)
    # pad the enhanced canvas when the registered base would stick out of it (never crop art)
    names, dirname = cfg.get('names', NAMES), cfg.get('dir', f'warrior-{kind}')
    entry = {'name': names[i]}
    for tag, im, g in (('base', base, (gx, gy)), ('enhanced', enh, (gx + dx, gy + dy))):
        if not (0 <= g[0] < im.width and 0 <= g[1] < im.height):
            raise SystemExit(f'{names[i]} {tag}: grip {g} outside {im.size}')
        im.save(out_dir / f'{names[i]}-{tag}.png')
        entry[tag] = {'src': f'assets/custom/{dirname}-overlays/{names[i]}-{tag}.png', 'w': im.width, 'h': im.height,
                      'gripX': int(g[0]), 'gripY': int(g[1])}
    entry['glow'] = glow_hex(enh, base, dx, dy)
    print(f"{i + 1:2d} {names[i]:9s} base {base.width}x{base.height} grip({gx},{gy}) enh {enh.width}x{enh.height} "
          f"reg({dx},{dy}) hratio {score:.2f} glow {entry['glow']}")
    return entry, base, enh


def contact_sheet(kind, manifest, out_dir, scale):
    """Idle front warrior + each tier base | enhanced at roughly in-game scale (`scale` = frame px per art px)."""
    hero = Image.open(ROOT / 'assets/hd/warrior-v2/normalized-hd/idle.png').convert('RGBA').crop((0, 0, 192, 192))
    cell = 220
    sheet = Image.new('RGBA', (cell * 2 * 5 + 200, cell * 2 + 40), (52, 56, 84, 255))
    sheet.alpha_composite(hero, (4, 20))
    d = ImageDraw.Draw(sheet)
    for t, e in enumerate(manifest):
        for v, tag in enumerate(('base', 'enhanced')):
            im = Image.open(ROOT / e[tag]['src']).convert('RGBA')
            im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.Resampling.LANCZOS)
            x, y = 200 + (t % 5) * cell * 2 + v * cell, (t // 5) * cell + 20
            sheet.alpha_composite(im, (x + 10, y + 10))
        d.text((200 + (t % 5) * cell * 2, (t // 5) * cell + 4), f'{t} {e["name"]}  base | +10', fill=(255, 255, 0))
    sheet.crop((0, 0, sheet.width, cell * 2 + 20)).save(out_dir / 'contact-sheet.png')


def run(kind):
    cfg = KINDS[kind]
    out_dir = ROOT / f'assets/custom/{cfg.get("dir", f"warrior-{kind}")}-overlays'
    out_dir.mkdir(parents=True, exist_ok=True)
    arr = np.array(Image.open(ROOT / 'assets/custom' / cfg['sheet']).convert('RGBA'))
    arr[..., 3] = np.where(arr[..., 3] >= HAZE, arr[..., 3], 0)
    ys, xs = cell_edges(arr)
    manifest = []
    pairs = [None] * 10
    for r in range(4):
        for c in range(5):
            cell = cut_cell(arr, ys[r], ys[r + 1], xs[c], xs[c + 1])
            pairs.__setitem__((r // 2) * 5 + c, (pairs[(r // 2) * 5 + c] or []) + [cell])
    for i, (base, enh) in enumerate(pairs):
        manifest.append(process(kind, i, base, enh, out_dir, cfg)[0])
    (out_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    js = (f'// GENERATED by tools/{cfg.get("tool", "cut-warrior-gear-overlays.py")} ({kind}) -- do not edit by hand.\n'
          f'// Index = equipmentTier(itemLevel) 0..9. Grip = pixel in each PNG that sits at the warrior hand/arm anchor.\n'
          f'export const {cfg["export"]} = Object.freeze({json.dumps(manifest, indent=2)});\n')
    (ROOT / f'src/data/{cfg.get("dir", f"warrior-{kind}")}-overlay-manifest-generated.js').write_text(js, encoding='utf-8')
    contact_sheet(kind, manifest, out_dir, 0.3)
    print(f'{kind}: wrote 20 overlays + manifest + contact sheet')


if __name__ == '__main__':
    arg = sys.argv[1] if len(sys.argv) > 1 else 'all'
    for k in (KINDS if arg == 'all' else [arg]):
        run(k)

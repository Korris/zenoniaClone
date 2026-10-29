"""Cut wizard-staff-overlay-sheet-raw.png (5 cols x 4 rows: staffs 1-5 base/enh, 6-10 base/enh) plus the
arcane replacement (wizard-staff-arcane-fix-raw.png) into 20 upright PNGs of equal staff length,
manifest.json, src/data/wizard-staff-overlay-manifest-generated.js (array by tier 0..9) and a contact sheet."""
from pathlib import Path
import colorsys
import json
import sys
import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from weapon_overlay_geometry import label_components, split_columns, trim
from wizard_staff_contact_sheet import build

root = Path(__file__).resolve().parents[1]
out_dir = root / 'assets/custom/wizard-staff-overlays'
NAMES = ['apprentice', 'wood', 'crystal', 'moon', 'arcane', 'flame', 'ice', 'nature', 'holy', 'void']
ARCANE = 4
TARGET_LEN, PAD, HAZE, MIN_COMP, GRIP_FRAC = 256, 4, 48, 12, 0.55  # base staff length px; grip = 55% down from top


def load_clean(path):
    a = np.array(Image.open(path).convert('RGBA'))
    a[..., 3] = np.where(a[..., 3] >= HAZE, a[..., 3], 0)
    return a


def clean(img):
    a = np.array(img)
    a[..., 3] = np.where(a[..., 3] >= HAZE, a[..., 3], 0)
    return Image.fromarray(a, 'RGBA')


def cut(arr, y0, y1, x0, x1):
    """Cell crop; keep components >= MIN_COMP px (sparkles/swirls) and drop the rest."""
    sub = arr[y0:y1, x0:x1].copy()
    keep = np.zeros(sub.shape[:2], bool)
    for ys, xs in label_components(sub[..., 3] > 0):
        if len(ys) >= MIN_COMP:
            keep[ys, xs] = True
    sub[~keep] = 0
    return Image.fromarray(sub, 'RGBA')


def sheet_pairs(sheet):
    """Return [(base, enh)] x 10 from the 5x4 sheet (rows 1-2 = staffs 1-5, rows 3-4 = staffs 6-10)."""
    pairs = [[None, None] for _ in range(10)]
    for c, (x0, x1) in enumerate(split_columns(sheet[..., 3], 5)):
        rows = np.nonzero((sheet[:, x0:x1, 3] > 0).any(axis=1))[0]
        cuts = sorted(sorted(range(len(rows) - 1), key=lambda i: rows[i] - rows[i + 1])[:3])  # 3 widest row gutters
        starts = [rows[0]] + [rows[i + 1] for i in cuts]
        ends = [rows[i] + 1 for i in cuts] + [rows[-1] + 1]
        for r in range(4):
            pairs[(r // 2) * 5 + c][r % 2] = cut(sheet, starts[r], ends[r], x0, x1)
    return pairs


def fix_pair(fix):
    (a0, a1), (b0, b1) = split_columns(fix[..., 3], 2)
    return [cut(fix, 0, fix.shape[0], a0, a1), cut(fix, 0, fix.shape[0], b0, b1)]


def main_body(img):
    a = np.array(img)[..., 3] > 0
    ys, xs = max(label_components(a), key=lambda c: len(c[0]))
    return a, ys, xs


def tip(img):
    """(x, y) of the staff bottom tip: bottom of the biggest component, x = centre of its lowest rows."""
    _, ys, xs = main_body(img)
    y = int(ys.max())
    return int(round(xs[ys >= y - 4].mean())), y


def grip_on_shaft(img, y):
    """(x, y) of the biggest component's median x on the nearest row to y that has pixels."""
    a, ys, xs = main_body(img)
    body = np.zeros_like(a)
    body[ys, xs] = True
    for d in range(a.shape[0]):
        for yy in (y - d, y + d):
            if 0 <= yy < a.shape[0] and body[yy].any():
                return int(np.median(np.nonzero(body[max(yy - 2, 0):yy + 3])[1])), yy
    raise ValueError('empty staff')


def glow_hex(enh, base, dx, dy):
    """Dominant hue of bright saturated pixels the enhanced art adds over the (aligned) base."""
    e, b = np.array(enh), np.array(base)
    mask = e[..., 3] > 0
    ys, xs = np.nonzero(b[..., 3] > 0)
    mask[np.clip(ys + dy, 0, e.shape[0] - 1), np.clip(xs + dx, 0, e.shape[1] - 1)] = False
    hsv = np.array([colorsys.rgb_to_hsv(*(p / 255.0)) for p in e[mask][:, :3]])
    hsv = hsv[(hsv[:, 1] > 0.25) & (hsv[:, 2] > 0.6)]
    hist = np.bincount((hsv[:, 0] * 360).astype(int) % 360, minlength=360).astype(float)
    smooth = sum(np.roll(hist, k) for k in range(-12, 13))
    r, g, b2 = colorsys.hsv_to_rgb(int(np.argmax(smooth)) / 360, 0.65, 1.0)
    return '#%02x%02x%02x' % (round(r * 255), round(g * 255), round(b2 * 255))


def process(i, base, enh):
    base, enh = trim(base, PAD), trim(enh, PAD)
    rows = np.nonzero((np.array(base)[..., 3] > 0).any(axis=1))[0]
    s = TARGET_LEN / (rows[-1] - rows[0] + 1)  # one factor per pair: base staff length -> TARGET_LEN
    base, enh = (clean(im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.Resampling.LANCZOS))
                 for im in (base, enh))
    rows = np.nonzero((np.array(base)[..., 3] > 0).any(axis=1))[0]
    length = int(rows[-1] - rows[0] + 1)
    gx, gy = grip_on_shaft(base, int(round(rows[0] + GRIP_FRAC * length)))
    (tbx, tby), (tex, tey) = tip(base), tip(enh)  # enhanced art sits on the same staff: align by bottom tip
    dx, dy = tex - tbx, tey - tby
    if not (0 <= gx + dx < enh.width and 0 <= gy + dy < enh.height):
        raise SystemExit(f'{NAMES[i]}: enhanced grip out of bounds')
    entry = {'name': NAMES[i]}
    for tag, im, g in (('base', base, (gx, gy)), ('enhanced', enh, (gx + dx, gy + dy))):
        im.save(out_dir / f'{NAMES[i]}-{tag}.png')
        entry[tag] = {'src': f'assets/custom/wizard-staff-overlays/{NAMES[i]}-{tag}.png',
                      'w': im.width, 'h': im.height, 'gripX': int(g[0]), 'gripY': int(g[1])}
    entry['glow'] = glow_hex(enh, base, dx, dy)
    return entry, {(i, 'base'): base, (i, 'enhanced'): enh}, length


def main():
    pairs = sheet_pairs(load_clean(root / 'assets/custom/wizard-staff-overlay-sheet-raw.png'))
    pairs[ARCANE] = fix_pair(load_clean(root / 'assets/custom/wizard-staff-arcane-fix-raw.png'))
    out_dir.mkdir(parents=True, exist_ok=True)
    manifest, images, length = [], {}, TARGET_LEN
    for i, (base, enh) in enumerate(pairs):
        entry, ims, length = process(i, base, enh)
        manifest.append(entry)
        images.update(ims)
        b, e = entry['base'], entry['enhanced']
        print(f"{i + 1:2d} {entry['name']:10s} base {b['w']}x{b['h']} grip({b['gripX']},{b['gripY']}) "
              f"enh {e['w']}x{e['h']} grip({e['gripX']},{e['gripY']}) glow {entry['glow']} len {length}")
    (out_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    js = ('// GENERATED by tools/cut-wizard-staff-overlays.py -- do not edit by hand.\n'
          '// Index = equipmentTier(itemLevel) 0..9. Grip = pixel in each PNG that sits in the wizard hand (~55% down the staff).\n'
          f'export const WIZARD_STAFF_OVERLAYS = Object.freeze({json.dumps(manifest, indent=2)});\n')
    (root / 'src/data/wizard-staff-overlay-manifest-generated.js').write_text(js, encoding='utf-8')
    build(root, out_dir, manifest, images, length)
    print('Wrote 20 overlays, manifest.json, generated module, contact-sheet.png')


main()

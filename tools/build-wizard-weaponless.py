"""Normalize the weaponless wizard sheets so each frame lines up with the baked normalized frame.

Raw: assets/hd/wizard/weaponless/raw/<anim>.png (ChatGPT edits of the baked sheets, same grid/size).
Out: assets/hd/wizard/weaponless/normalized-hd/<anim>.png (192px frames) + scale.json + compare-contact-sheet.png

Why not build-hd-render place(): it centers the bbox, and removing the staff changes the bbox, so the
body would shift vs. the baked frame. Here the scale is copied from the baked scale.json (the raw
sheets keep the original pixel size) and each frame is positioned by a best-overlap shift search
against the matching baked normalized frame (weaponless alpha must sit inside the baked alpha).
Usage: python tools/build-wizard-weaponless.py
"""
import importlib.util
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
WIZ = ROOT / 'assets' / 'hd' / 'wizard'
RAW = WIZ / 'weaponless' / 'raw'
OUT = WIZ / 'weaponless' / 'normalized-hd'
BAKED = WIZ / 'normalized-hd'
FRAME = 192
SEARCH = 36  # +-px shift search window around the bbox-centered, bottom-aligned guess
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2, 'death': 5}
ROWS = ['down', 'up', 'left', 'right']


def load_builder():
    sys.argv = ['build-hd-hero.py', str(RAW), str(OUT)]
    spec = importlib.util.spec_from_file_location('hd_wizard_weaponless', ROOT / 'tools' / 'build-hd-hero.py')
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def best_shift(body, baked_alpha):
    """body: downscaled RGBA PIL image (tight bbox). Returns (x, y) top-left in a 192 frame."""
    w, h = body.size
    pad = SEARCH
    canvas = np.zeros((FRAME + 2 * pad + h, FRAME + 2 * pad + w), np.float32)
    canvas[pad:pad + FRAME, pad:pad + FRAME] = baked_alpha
    tmpl = np.asarray(body.split()[3], np.float32) / 255.0
    corr = cv2.matchTemplate(canvas, tmpl, cv2.TM_CCORR)  # sum(W*B) at each top-left offset
    gx, gy = (FRAME - w) // 2, FRAME - h
    ys, xs = np.mgrid[0:corr.shape[0], 0:corr.shape[1]]
    px, py = xs - pad, ys - pad  # candidate top-left in frame coordinates
    far = np.maximum(np.abs(px - gx), np.abs(py - gy)) > SEARCH
    # 2*corr - sum(W): rewards W inside B, penalizes W spilling outside B (staff only adds to B, so
    # it cannot pull the body). Tiny prior toward the feet-anchored guess breaks remaining ties.
    score = 2 * corr - tmpl.sum() - 0.02 * (np.abs(px - gx) + np.abs(py - gy)) * tmpl.sum() / 100.0
    score[far] = -1e9
    iy, ix = np.unravel_index(np.argmax(score), score.shape)
    return int(px[iy, ix]), int(py[iy, ix]), float(score[iy, ix])


def build_sheet(builder, anim, cols, baked_scale):
    path = RAW / f'{anim}.png'
    if not path.exists():
        print(f'WARN missing raw {anim}')
        return None, None, None
    cells = builder.split_sheet(Image.open(path).convert('RGBA'), cols)
    baked = np.asarray(Image.open(BAKED / f'{anim}.png').convert('RGBA'))
    sheet = Image.new('RGBA', (cols * FRAME, 4 * FRAME), (0, 0, 0, 0))
    crops, alphas = {}, {}
    for r in range(4):
        for c in range(cols):
            crop = builder.tight_bbox(cells[r][c])
            if crop is None:
                print(f'WARN {anim} {ROWS[r]}[{c}] empty')
                continue
            crops[r, c] = Image.fromarray(crop, 'RGBA')
            alphas[r, c] = baked[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME, 3].astype(np.float32) / 255.0

    def scaled(key, factor):
        body = builder.downscale(crops[key], baked_scale * factor)
        return body.crop(body.getbbox())

    # ChatGPT redraws the body a few percent larger/smaller: pick ONE factor per sheet (median of the
    # per-frame best) so the body size matches the baked sheet without frame-to-frame size jitter.
    best = []
    for key in crops:
        trials = [(best_shift(scaled(key, f), alphas[key])[2], f) for f in np.arange(0.66, 1.11, 0.02)]
        best.append(max(trials)[1])
    factor = float(np.median(best)) if best else 1.0
    print(f'  {anim}: body factor vs baked scale {factor:.2f} (per-frame {min(best):.2f}..{max(best):.2f})')
    deltas = []
    for key in crops:
        body = scaled(key, factor)
        x, y, _ = best_shift(body, alphas[key])
        deltas.append((x - (FRAME - body.width) // 2, y - (FRAME - body.height)))
        layer = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
        layer.paste(body, (x, y))  # PIL clips parts outside the frame
        sheet.paste(layer, (key[1] * FRAME, key[0] * FRAME))
    return sheet, deltas, baked_scale * factor


def diff_tile(b, w):
    """baked only = red (the staff), weaponless only = cyan (drift), both = light grey."""
    ba, wa = np.asarray(b)[..., 3] > 100, np.asarray(w)[..., 3] > 100
    img = np.full((FRAME, FRAME, 3), 40, np.uint8)
    img[ba & ~wa] = (255, 60, 60)
    img[wa & ~ba] = (60, 220, 255)
    img[ba & wa] = (235, 235, 235)
    return Image.fromarray(img)


def compare_sheet(sheets):
    rows = []
    for anim, sheet in sheets.items():
        baked = Image.open(BAKED / f'{anim}.png').convert('RGBA')
        cols = sheet.width // FRAME
        for r in range(4):
            strip = Image.new('RGB', (cols * FRAME * 3, FRAME), (40, 40, 56))
            for c in range(cols):
                box = (c * FRAME, r * FRAME, (c + 1) * FRAME, (r + 1) * FRAME)
                b, w = baked.crop(box), sheet.crop(box)
                for k, tile in enumerate([b, w]):
                    bg = Image.new('RGBA', (FRAME, FRAME), (40, 40, 56, 255))
                    bg.alpha_composite(tile)
                    strip.paste(bg.convert('RGB'), ((c * 3 + k) * FRAME, 0))
                strip.paste(diff_tile(b, w), ((c * 3 + 2) * FRAME, 0))
            rows.append((f'{anim} {ROWS[r]}', strip))
    width = max(s.width for _, s in rows)
    out = Image.new('RGB', (width, sum(s.height + 14 for _, s in rows)), (20, 20, 30))
    d, y = ImageDraw.Draw(out), 0
    for label, strip in rows:
        d.text((2, y), label + '  (baked | weaponless | diff)', fill=(255, 255, 0))
        out.paste(strip, (0, y + 12))
        y += strip.height + 14
    out.save(OUT / 'compare-contact-sheet.png')


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    builder = load_builder()
    baked_scale = json.loads((BAKED / 'scale.json').read_text())
    sheets, records = {}, {}
    for anim, cols in COUNTS.items():
        scale = baked_scale[anim]['scale']
        sheet, deltas, scale = build_sheet(builder, anim, cols, scale)
        if sheet is None:
            continue
        arr = np.array(sheet)
        arr[..., 3][arr[..., 3] <= 48] = 0  # no haze / colored fringe at low alpha
        sheet = Image.fromarray(arr, 'RGBA')
        sheet.save(OUT / f'{anim}.png')
        sheets[anim] = sheet
        records[anim] = {'scale': scale, 'frame': FRAME}
        mx = max((max(abs(dx), abs(dy)) for dx, dy in deltas), default=0)
        print(f'{anim}: scale {scale:.4f}, max shift from centered guess {mx}px')
    (OUT / 'scale.json').write_text(json.dumps(records, indent=2))
    compare_sheet(sheets)
    print('wrote', OUT)


if __name__ == '__main__':
    main()

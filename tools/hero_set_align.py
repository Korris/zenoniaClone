"""Shared helpers to normalize a hero sheet set into 192px frames and to align a variant set (weaponless /
bare) onto the armed reference frame by frame. Generalizes tools/build-wizard-weaponless.py (best-overlap
shift search + ONE scale factor per sheet) for the warrior-v2 sets (see tools/build-warrior-v2-sheets.py)."""
import importlib.util
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FRAME = 192
ROWS = ['down', 'up', 'left', 'right']


def load_builder(src_dir, out_dir):
    """tools/build-hd-hero.py as a module configured for 192px frames (same recipe as build-hd-render.py)."""
    sys.argv = ['build-hd-hero.py', str(src_dir), str(out_dir)]
    spec = importlib.util.spec_from_file_location('hd_hero_builder_' + Path(src_dir).parent.name, ROOT / 'tools' / 'build-hd-hero.py')
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    mod.FRAME = FRAME
    mod.TARGET_BODY_HEIGHT = 164

    def place(img):
        canvas = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
        bounds = img.getbbox()
        if bounds:
            cropped = img.crop(bounds)
            if cropped.width > FRAME or cropped.height > FRAME:
                cropped = cropped.crop((max(0, cropped.width - FRAME), max(0, cropped.height - FRAME), cropped.width, cropped.height))
            canvas.alpha_composite(cropped, ((FRAME - cropped.width) // 2, FRAME - cropped.height))
        return canvas

    mod.place_in_frame = place
    return mod


def best_shift(body, ref_alpha, search):
    """body: tight-bbox RGBA PIL image at final scale. Returns (x, y, score) = top-left in the 192 frame that
    puts the body's alpha best inside ref_alpha (2*corr - sum(W): rewards inside, penalizes spill)."""
    w, h = body.size
    pad = search
    canvas = np.zeros((FRAME + 2 * pad + h, FRAME + 2 * pad + w), np.float32)
    canvas[pad:pad + FRAME, pad:pad + FRAME] = ref_alpha
    tmpl = np.asarray(body.split()[3], np.float32) / 255.0
    corr = cv2.matchTemplate(canvas, tmpl, cv2.TM_CCORR)
    gx, gy = (FRAME - w) // 2, FRAME - h
    ys, xs = np.mgrid[0:corr.shape[0], 0:corr.shape[1]]
    px, py = xs - pad, ys - pad
    far = np.maximum(np.abs(px - gx), np.abs(py - gy)) > search
    score = 2 * corr - tmpl.sum() - 0.02 * (np.abs(px - gx) + np.abs(py - gy)) * tmpl.sum() / 100.0
    score[far] = -1e9
    iy, ix = np.unravel_index(np.argmax(score), score.shape)
    return int(px[iy, ix]), int(py[iy, ix]), float(score[iy, ix])


def align_sheet(builder, raw_path, ref_sheet, cols, ref_scale, search=60):
    """Normalize raw_path onto the armed sheet `ref_sheet` (PIL RGBA). Returns (sheet, scale) or (None, None)."""
    if not raw_path.exists():
        return None, None
    cells = builder.split_sheet(Image.open(raw_path).convert('RGBA'), cols)
    ref = np.asarray(ref_sheet)
    crops, alphas = {}, {}
    for r in range(4):
        for c in range(cols):
            crop = builder.tight_bbox(cells[r][c])
            if crop is None:
                print(f'WARN {raw_path.name} {ROWS[r]}[{c}] empty')
                continue
            crops[r, c] = Image.fromarray(crop, 'RGBA')
            alphas[r, c] = ref[r * FRAME:(r + 1) * FRAME, c * FRAME:(c + 1) * FRAME, 3].astype(np.float32) / 255.0

    def scaled(key, factor):
        body = builder.downscale(crops[key], ref_scale * factor)
        return body.crop(body.getbbox())

    best = [max((best_shift(scaled(k, f), alphas[k], search)[2], f) for f in np.arange(0.70, 1.31, 0.02))[1] for k in crops]
    factor = float(np.median(best))
    print(f'  {raw_path.parent.parent.name}/{raw_path.stem}: factor {factor:.2f} (per-frame {min(best):.2f}..{max(best):.2f})')
    sheet = Image.new('RGBA', (cols * FRAME, 4 * FRAME), (0, 0, 0, 0))
    for key in crops:
        body = scaled(key, factor)
        x, y, _ = best_shift(body, alphas[key], search)
        layer = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
        layer.paste(body, (x, y))
        sheet.paste(layer, (key[1] * FRAME, key[0] * FRAME))
    arr = np.array(sheet)
    arr[..., 3][arr[..., 3] <= 48] = 0
    return Image.fromarray(arr, 'RGBA'), ref_scale * factor


def compare_sheet(pairs, path, label):
    """pairs: {anim: (ref_sheet, aligned_sheet)} -> rows of ref | aligned | diff (red = ref only, cyan = aligned only)."""
    rows = []
    for anim, (ref, ali) in pairs.items():
        cols = ali.width // FRAME
        for r in range(4):
            strip = Image.new('RGB', (cols * FRAME * 3, FRAME), (40, 40, 56))
            for c in range(cols):
                box = (c * FRAME, r * FRAME, (c + 1) * FRAME, (r + 1) * FRAME)
                b, w = ref.crop(box), ali.crop(box)
                for k, tile in enumerate([b, w]):
                    bg = Image.new('RGBA', (FRAME, FRAME), (40, 40, 56, 255))
                    bg.alpha_composite(tile)
                    strip.paste(bg.convert('RGB'), ((c * 3 + k) * FRAME, 0))
                ba, wa = np.asarray(b)[..., 3] > 100, np.asarray(w)[..., 3] > 100
                img = np.full((FRAME, FRAME, 3), 40, np.uint8)
                img[ba & ~wa] = (255, 60, 60)
                img[wa & ~ba] = (60, 220, 255)
                img[ba & wa] = (235, 235, 235)
                strip.paste(Image.fromarray(img), ((c * 3 + 2) * FRAME, 0))
            rows.append((f'{anim} {ROWS[r]}', strip))
    out = Image.new('RGB', (max(s.width for _, s in rows), sum(s.height + 14 for _, s in rows)), (20, 20, 30))
    d, y = ImageDraw.Draw(out), 0
    for name, strip in rows:
        d.text((2, y), f'{name}  (armed | {label} | diff)', fill=(255, 255, 0))
        out.paste(strip, (0, y + 12))
        y += strip.height + 14
    out.save(path)

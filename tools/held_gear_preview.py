"""Preview helpers for estimate-warrior-held-gear-anchors.py. Mirrors the runtime transform in
src/graphics/held-weapon-renderer.js: grip -> (x, y), rotate `angle` (clockwise), scale s (x additionally by sx),
behind = drawn under the body."""
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FRAME = 192


def load_overlay(kind, tier=0, variant='base'):
    text = (ROOT / 'src' / 'data' / f'warrior-{kind}-overlay-manifest-generated.js').read_text(encoding='utf-8')
    entries = json.loads(text[text.index('Object.freeze(') + len('Object.freeze('):text.rindex(')')])
    e = entries[tier][variant]
    return Image.open(ROOT / e['src']).convert('RGBA'), e['gripX'], e['gripY']


def gear_layer(anchor, overlay, gx, gy):
    sx, sy = anchor['s'] * anchor.get('sx', 1.0), anchor['s']
    img = overlay.resize((max(1, round(overlay.width * sx)), max(1, round(overlay.height * sy))), Image.LANCZOS)
    cx, cy = gx * sx, gy * sy
    pad = int(max(img.size) * 1.5)
    big = Image.new('RGBA', (img.width + 2 * pad, img.height + 2 * pad), (0, 0, 0, 0))
    big.paste(img, (pad, pad))
    rot = big.rotate(-anchor['angle'], resample=Image.BICUBIC, center=(pad + cx, pad + cy))
    layer = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    layer.paste(rot, (round(anchor['x'] - pad - cx), round(anchor['y'] - pad - cy)), rot)
    return layer


def compose(body, parts):
    """parts = [(anchor, overlay, gx, gy)] in draw order (shield, sword). Behind layers first, then body, then front."""
    out = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    layers = [(a.get('behind'), gear_layer(a, *rest)) for a, *rest in parts if not a.get('hidden')]
    for behind, layer in layers:
        if behind:
            out.alpha_composite(layer)
    out.alpha_composite(body)
    for behind, layer in layers:
        if not behind:
            out.alpha_composite(layer)
    return out


def write_previews(data, counts, dirs, sheets, out_dir):
    """sheets = {'armed':{anim:img}, 'bare':{...}}; one image per anim: armed | bare + shield + sword."""
    parts_art = {k: load_overlay(k) for k in ('shield', 'sword')}
    for anim, n in counts.items():
        sheet = Image.new('RGB', (n * FRAME * 2, FRAME * 4), (20, 20, 30))
        for r, d in enumerate(dirs):
            for i in range(n):
                box = (i * FRAME, r * FRAME, (i + 1) * FRAME, (r + 1) * FRAME)
                armed, bare = sheets['armed'][anim].crop(box), sheets['bare'][anim].crop(box)
                parts = [(data[k][anim][d][i], *parts_art[k]) for k in ('shield', 'sword')]
                for c, img in enumerate((armed, compose(bare, parts))):
                    tile = Image.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                    tile.alpha_composite(img)
                    if c:
                        dr = ImageDraw.Draw(tile)
                        for k, col in (('sword', (255, 255, 0)), ('shield', (0, 255, 255))):
                            a = data[k][anim][d][i]
                            if 'x' in a:
                                dr.ellipse([a['x'] - 2, a['y'] - 2, a['x'] + 2, a['y'] + 2], outline=col)
                    sheet.paste(tile.convert('RGB'), ((i * 2 + c) * FRAME, r * FRAME))
        sheet.save(out_dir / f'anchor-preview-{anim}.png')
    print('wrote anchor-preview-*.png in', out_dir)

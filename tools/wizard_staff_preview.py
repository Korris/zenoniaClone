"""Preview sheet for estimate-wizard-staff-anchors.py: baked frame | weaponless + overlay at the anchor.
Mirrors the runtime transform (src/graphics/held-weapon-renderer.js): grip -> (x,y), rotate angle (clockwise),
scale s, behind = drawn under the body."""
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
FRAME = 192


def load_overlay(tier=0, variant='base'):
    text = (ROOT / 'src' / 'data' / 'wizard-staff-overlay-manifest-generated.js').read_text(encoding='utf-8')
    entries = json.loads(text[text.index('Object.freeze(') + len('Object.freeze('):text.rindex(')')])
    e = entries[tier][variant]
    return Image.open(ROOT / e['src']).convert('RGBA'), e['gripX'], e['gripY']


def staff_layer(anchor, overlay, gx, gy):
    """Overlay transformed into a 192px frame layer."""
    s = anchor['s']
    img = overlay.resize((max(1, round(overlay.width * s)), max(1, round(overlay.height * s))), Image.LANCZOS)
    cx, cy = gx * s, gy * s
    pad = int(max(img.size) * 1.5)
    big = Image.new('RGBA', (img.width + 2 * pad, img.height + 2 * pad), (0, 0, 0, 0))
    big.paste(img, (pad, pad))
    # rotate around the grip: shift so the grip is the canvas center, rotate, shift back
    cxp, cyp = pad + cx, pad + cy
    rot = big.rotate(-anchor['angle'], resample=Image.BICUBIC, center=(cxp, cyp))
    layer = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    layer.paste(rot, (round(anchor['x'] - cxp), round(anchor['y'] - cyp)), rot)
    return layer


def compose(bare, anchor, overlay, gx, gy):
    if anchor.get('hidden'):
        return bare
    staff = staff_layer(anchor, overlay, gx, gy)
    out = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    if anchor.get('behind'):
        out.alpha_composite(staff)
        out.alpha_composite(bare)
    else:
        out.alpha_composite(bare)
        out.alpha_composite(staff)
    return out


def write_preview(data, counts, dirs, frame_of, tiers=((0, 'base'), (2, 'enhanced'))):
    from PIL import Image as I
    wiz = ROOT / 'assets' / 'hd' / 'wizard'
    overlays = [load_overlay(t, v) for t, v in tiers]
    cols = 4  # tiles per line = 1 baked + overlays per frame; wrap lines of 12 frames-groups
    rows = []
    for anim, n in counts.items():
        baked_sheet = I.open(wiz / 'normalized-hd' / f'{anim}.png').convert('RGBA')
        bare_sheet = I.open(wiz / 'weaponless' / 'normalized-hd' / f'{anim}.png').convert('RGBA')
        for d in dirs:
            strip = I.new('RGB', (n * FRAME * 2, FRAME), (40, 44, 64))
            for i in range(n):
                baked, bare = frame_of(baked_sheet, anim, d, i), frame_of(bare_sheet, anim, d, i)
                a = data[anim][d][i]
                tile = I.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                tile.alpha_composite(baked)
                strip.paste(tile.convert('RGB'), (i * FRAME * 2, 0))
                tile = I.new('RGBA', (FRAME, FRAME), (40, 44, 64, 255))
                tile.alpha_composite(compose(bare, a, *overlays[0]))
                dr = ImageDraw.Draw(tile)
                if 'x' in a:
                    dr.ellipse([a['x'] - 2, a['y'] - 2, a['x'] + 2, a['y'] + 2], outline=(255, 43, 214))
                strip.paste(tile.convert('RGB'), (i * FRAME * 2 + FRAME, 0))
            rows.append((f'{anim} {d}', strip))
    out_dir = wiz / 'weaponless'
    # one sheet per animation group to keep each image a viewable size
    groups = {}
    for label, strip in rows:
        groups.setdefault(label.split()[0], []).append((label, strip))
    for anim, items in groups.items():
        w = max(s.width for _, s in items)
        sheet = I.new('RGB', (w, sum(s.height + 12 for _, s in items)), (20, 20, 30))
        dr, y = ImageDraw.Draw(sheet), 0
        for label, strip in items:
            dr.text((2, y), f'{label}  (baked | weaponless + apprentice staff @anchor)', fill=(255, 255, 0))
            sheet.paste(strip, (0, y + 11))
            y += strip.height + 12
        sheet.save(out_dir / f'anchor-preview-{anim}.png')
    print('wrote anchor-preview-*.png in', out_dir)

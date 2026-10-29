"""Normalize the new red-haired warrior (assets/hd/warrior-v2) into 192px frames.

  raw/*.png            armed (sword + shield)  -> normalized-hd/                = the baked warrior sprite set (reference)
  weaponless/raw/*.png sword removed           -> weaponless/normalized-hd/     aligned per frame onto the armed body
  bare/raw/*.png       sword + shield removed  -> bare/normalized-hd/           aligned per frame onto the armed body
  raw/idle.png row0/col0 head                  -> normalized/hero_face.png      (HUD portrait)
Aligned sets skip `death` (armed death has no sword; overlays are hidden then).
Usage: python tools/build-warrior-v2-sheets.py
"""
import json

import numpy as np
from PIL import Image

from hero_set_align import FRAME, ROOT, align_sheet, compare_sheet, load_builder

W2 = ROOT / 'assets' / 'hd' / 'warrior-v2'
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2, 'death': 5}
FACE = 152


def build_armed(builder):
    out = W2 / 'normalized-hd'
    out.mkdir(exist_ok=True)
    builder.FRAME_COUNTS.update(COUNTS)  # walk = 4 key poses
    sheets, records = {}, {}
    for anim, cols in COUNTS.items():  # dict order: idle first => head-scale reference
        sheet, record = builder.build_sheet(anim, cols)
        arr = np.array(sheet)
        arr[..., 3][arr[..., 3] <= 48] = 0
        sheet = Image.fromarray(arr, 'RGBA')
        sheet.save(out / f'{anim}.png')
        sheets[anim], records[anim] = sheet, record
    failures = builder.validate(sheets)
    if failures:
        raise SystemExit('\n'.join(failures))
    (out / 'scale.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    return sheets, records


def build_variant(builder, name, armed, records):
    out = W2 / name / 'normalized-hd'
    out.mkdir(parents=True, exist_ok=True)
    pairs, scales = {}, {}
    for anim, cols in COUNTS.items():
        if anim == 'death':
            continue
        sheet, scale = align_sheet(builder, W2 / name / 'raw' / f'{anim}.png', armed[anim], cols, records[anim]['scale'])
        if sheet is None:
            print(f'WARN {name}/{anim} missing')
            continue
        sheet.save(out / f'{anim}.png')
        pairs[anim], scales[anim] = (armed[anim], sheet), {'scale': scale, 'frame': FRAME}
    (out / 'scale.json').write_text(json.dumps(scales, indent=2), encoding='utf-8')
    compare_sheet(pairs, out / 'compare-contact-sheet.png', name)


def build_face(builder):
    """Head of the idle front frame: widest span of the top rows, squared from the top, resized to FACE px."""
    cells = builder.split_sheet(Image.open(W2 / 'raw' / 'idle.png').convert('RGBA'), COUNTS['idle'])
    body = builder.tight_bbox(cells[0][0])
    h = body.shape[0]
    cols = np.nonzero((body[: int(h * 0.30), :, 3] > 0).any(axis=0))[0]
    x0, x1 = int(cols.min()), int(cols.max()) + 1
    side = min(int((x1 - x0) * 1.08), h)  # a little taller than wide: hair top .. fur collar
    cx = (x0 + x1) // 2
    left = max(0, min(body.shape[1] - side, cx - side // 2))
    crop = Image.fromarray(body[:side, left:left + side], 'RGBA')
    small = builder.downscale(crop, (FACE - 4) / side)
    face = Image.new('RGBA', (FACE, FACE), (0, 0, 0, 0))
    face.paste(small, ((FACE - small.width) // 2, (FACE - small.height) // 2), small)
    out = W2 / 'normalized'
    out.mkdir(exist_ok=True)
    face.save(out / 'hero_face.png')


def main():
    builder = load_builder(W2 / 'raw', W2 / 'normalized-hd')
    armed, records = build_armed(builder)
    for name in ('weaponless', 'bare'):
        build_variant(builder, name, armed, records)
    build_face(builder)
    print('wrote', W2)


if __name__ == '__main__':
    main()

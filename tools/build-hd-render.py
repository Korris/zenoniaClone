"""Rebuild 192px hero frames from original artwork, preserving 96px sheets for comparison.

Usage: python tools/build-hd-render.py [hero wizard mechanic]
Output: assets/hd/<class>/normalized-hd/*.png (24 world units at 8px/unit).
"""
import importlib.util
import json
from pathlib import Path
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
classes = sys.argv[1:] or ['hero', 'wizard', 'mechanic']
for cls in classes:
    if cls not in ('hero', 'wizard', 'mechanic'):
        raise SystemExit(f'Unknown class: {cls}')
    source = ROOT / 'assets' / 'hd' / cls
    output = source / 'normalized-hd'
    output.mkdir(exist_ok=True)
    sys.argv = ['build-hd-hero.py', str(source), str(output)]
    spec = importlib.util.spec_from_file_location(f'hd_render_{cls}', ROOT / 'tools' / 'build-hd-hero.py')
    builder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(builder)
    builder.FRAME = 192
    builder.TARGET_BODY_HEIGHT = 164

    def place(img):
        # Preserve soft edge alpha once; mask-paste would multiply it a second time.
        canvas = Image.new('RGBA', (192, 192), (0, 0, 0, 0))
        bounds = img.getbbox()
        if bounds:
            cropped = img.crop(bounds)
            if cropped.width > 192 or cropped.height > 192:
                cropped = cropped.crop((max(0, cropped.width - 192), max(0, cropped.height - 192), cropped.width, cropped.height))
            canvas.alpha_composite(cropped, ((192 - cropped.width) // 2, 192 - cropped.height))
        return canvas

    builder.place_in_frame = place
    sheets, records = {}, {}
    for anim, count in builder.FRAME_COUNTS.items():
        source_anim = 'walk-v2' if anim == 'walk' else anim
        sheet, record = builder.build_sheet(source_anim, count)
        if sheet is None:
            raise SystemExit(f'Missing source for {cls}/{anim}')
        sheets[anim] = sheet
        records[anim] = record
        sheet.save(output / f'{anim}.png')
    failures = builder.validate(sheets)
    if failures:
        raise SystemExit('\n'.join(failures))
    (output / 'scale.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    print(f'{cls}: {len(sheets)} animations rebuilt at 192px OK')

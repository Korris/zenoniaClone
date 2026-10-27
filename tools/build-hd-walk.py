"""Normalize only the revised walk sheets, reusing the existing hero pipeline.

Usage: python tools/build-hd-walk.py [hero wizard mechanic]
Keeps the original sheets and every other animation unchanged.
"""
import importlib.util
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
classes = sys.argv[1:] or ['hero', 'wizard', 'mechanic']
for cls in classes:
    if cls not in ('hero', 'wizard', 'mechanic'):
        raise SystemExit(f'Unknown class: {cls}')
    source = ROOT / 'assets' / 'hd' / cls
    output = source / 'normalized'
    sys.argv = ['build-hd-hero.py', str(source), str(output)]
    spec = importlib.util.spec_from_file_location(f'walk_builder_{cls}', ROOT / 'tools' / 'build-hd-hero.py')
    builder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(builder)
    # Establish head-size reference without writing or changing idle artwork.
    idle, _ = builder.build_sheet('idle', 4)
    if idle is None:
        raise SystemExit(f'Missing idle reference: {cls}')
    walk, record = builder.build_sheet('walk-v2', 4)
    if walk is None:
        raise SystemExit(f'Missing revised walk sheet: {cls}')
    failures = builder.validate({'walk': walk})
    if failures:
        raise SystemExit('\n'.join(failures))
    walk.save(output / 'walk-v2.png')
    (output / 'walk-v2.scale.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'{cls}: walk-v2.png {walk.width}x{walk.height} OK')

"""Align the gun-less mechanic (assets/hd/mechanic/weaponless/raw) onto the baked armed set, frame by frame.

  assets/hd/mechanic/normalized-hd/*.png  = baked armed set (built by tools/build-hd-render.py, KEPT as is) = reference
  weaponless/raw/*.png                    -> weaponless/normalized-hd/  (same 192px grid, shifted/scaled onto the armed body)
Skipped: attack2 (weaponless sheet has different poses than the armed one: the game draws the baked armed attack2 frames,
i.e. the per-animation fallback) and death (armed death carries no gun overlay).
dash / hurt weaponless sheets were painted at a larger resolution than their armed sources (same aspect): they are
resampled to the armed source size first so the armed scale.json factor applies.
Usage: python tools/build-mechanic-sheets.py
"""
import json
import tempfile
from pathlib import Path

from PIL import Image

from hero_set_align import FRAME, ROOT, align_sheet, compare_sheet, load_builder

MC = ROOT / 'assets' / 'hd' / 'mechanic'
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack3': 6, 'dash': 3, 'hurt': 2}
ARMED_SOURCE = {'walk': 'walk-v2'}  # armed walk = walk-v2.png (build-hd-render.py)


def matched_raw(anim, tmp):
    """Weaponless raw sheet resampled to the armed source resolution (no-op when equal)."""
    img = Image.open(MC / 'weaponless' / 'raw' / f'{anim}.png').convert('RGBA')
    size = Image.open(MC / f'{ARMED_SOURCE.get(anim, anim)}.png').size
    if img.size != size:
        print(f'  {anim}: resample weaponless {img.size} -> armed source {size}')
        img = img.resize(size, Image.Resampling.LANCZOS)
    path = Path(tmp) / f'{anim}.png'
    img.save(path)
    return path


def main():
    records = json.loads((MC / 'normalized-hd' / 'scale.json').read_text())
    out = MC / 'weaponless' / 'normalized-hd'
    out.mkdir(parents=True, exist_ok=True)
    pairs, scales = {}, {}
    with tempfile.TemporaryDirectory() as tmp:
        builder = load_builder(tmp, out)
        for anim, cols in COUNTS.items():
            armed = Image.open(MC / 'normalized-hd' / f'{anim}.png').convert('RGBA')
            sheet, scale = align_sheet(builder, matched_raw(anim, tmp), armed, cols, records[anim]['scale'])
            if sheet is None:
                print(f'WARN weaponless/{anim} missing')
                continue
            sheet.save(out / f'{anim}.png')
            pairs[anim], scales[anim] = (armed, sheet), {'scale': scale, 'frame': FRAME}
    (out / 'scale.json').write_text(json.dumps(scales, indent=2), encoding='utf-8')
    compare_sheet(pairs, out / 'compare-contact-sheet.png', 'weaponless')
    print('wrote', out)


if __name__ == '__main__':
    main()

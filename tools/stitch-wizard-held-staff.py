"""Stitch tools/screenshots/wizard-held-staff/raw clips (from check-wizard-held-staff.mjs) into one sheet per config:
rows = down/up/left/right, columns = poses. Usage: python tools/stitch-wizard-held-staff.py [screenshots dir]
(also used for tools/screenshots/warrior-held-gear from check-warrior-held-gear.mjs)"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent / 'screenshots' / 'wizard-held-staff'
DIRS = ['down', 'up', 'left', 'right']
SCALE = 1.0


def main():
    raw = OUT / 'raw'
    labels = sorted({p.name.rsplit('-', 2)[0] for p in raw.glob('*.png')})
    for label in labels:
        poses = []
        for p in sorted(raw.glob(f'{label}-*-down.png')):
            poses.append(p.name[len(label) + 1:-len('-down.png')])
        order = ['idle', 'walk0', 'walk1', 'walk2', 'walk3', 'atk1a', 'atk1b', 'atk2', 'atk3a', 'atk3b', 'dash']
        poses.sort(key=order.index)
        first = Image.open(raw / f'{label}-{poses[0]}-down.png')
        w, h = int(first.width * SCALE), int(first.height * SCALE)
        sheet = Image.new('RGB', (w * len(poses), (h + 14) * 4), (20, 20, 30))
        d = ImageDraw.Draw(sheet)
        for r, direction in enumerate(DIRS):
            for c, pose in enumerate(poses):
                im = Image.open(raw / f'{label}-{pose}-{direction}.png').convert('RGB').resize((w, h), Image.LANCZOS)
                sheet.paste(im, (c * w, r * (h + 14) + 14))
                d.text((c * w + 4, r * (h + 14) + 1), f'{label} {pose} {direction}', fill=(255, 255, 0))
        sheet.save(OUT / f'sheet-{label}.png')
        print('wrote', f'sheet-{label}.png')


if __name__ == '__main__':
    main()

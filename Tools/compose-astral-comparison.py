"""Compare separately built Wizard variants at the identical in-game camera."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
CURRENT = ROOT / 'Build/wizard-reference/captures-battle-mage'
NORTHWIND = ROOT / 'Build/wizard-astral/captures'
OUT = ROOT / 'docs/reference'
BOX = (200, 140, 700, 620)


def shot(folder, tier):
    return Image.open(folder / f't{tier}_1.png').convert('RGB').crop(BOX)


comparison = Image.new('RGB', (1000, 1512), (35, 40, 51))
d = ImageDraw.Draw(comparison)
for row, tier in enumerate((0, 5, 9)):
    for col, (folder, label) in enumerate(((CURRENT, 'Current Wizard'), (NORTHWIND, 'Astral Scholar V7'))):
        x, y = col*500, row*504
        d.text((x+8, y+5), f'T{tier} - {label}', fill='white')
        comparison.paste(shot(folder, tier), (x, y+24))
comparison.save(OUT / 'wizard-astral-vs-current-ingame.png')

board = Image.new('RGB', (1500, 616), (35, 40, 51))
d = ImageDraw.Draw(board)
for tier in range(10):
    x, y = tier%5*300, tier//5*308
    d.text((x+8, y+4), f'T{tier}', fill='white')
    board.paste(shot(NORTHWIND, tier).resize((300, 288), Image.Resampling.LANCZOS), (x, y+20))
board.save(OUT / 'wizard-astral-all-tiers-ingame.png')

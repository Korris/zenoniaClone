"""Review screenshots at a shared camera scale; never rescale heroes separately."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
CAPTURE = ROOT / 'Build/wizard-reference/captures-battle-mage'
BEFORE = ROOT / 'Build/wizard-reference/captures-height-fixed'
OUT = ROOT / 'docs/reference'
BOX = (200, 140, 700, 620)


def shot(folder, name):
    return Image.open(folder / f'{name}_1.png').convert('RGB').crop(BOX)


comparison = Image.new('RGB', (1000, 1008), (35, 40, 51))
d = ImageDraw.Draw(comparison)
for row, tier in enumerate((0, 9)):
    for col, (folder, label) in enumerate(((BEFORE, 'Before'), (CAPTURE, 'Battle mage'))):
        x, y = col*500, row*504
        d.text((x+8, y+5), f'T{tier} - {label}', fill='white')
        comparison.paste(shot(folder, f't{tier}'), (x, y+24))
comparison.save(OUT / 'wizard-battle-mage-before-after.png')

party = Image.new('RGB', (1500, 504), (35, 40, 51))
d = ImageDraw.Draw(party)
for col, (name, label) in enumerate((('warrior', 'Warrior'), ('mechanic', 'Mechanic'), ('t0', 'Wizard - battle mage'))):
    d.text((col*500+8, 5), label, fill='white')
    party.paste(shot(CAPTURE, name), (col*500, 24))
party.save(OUT / 'wizard-battle-mage-vs-party.png')

board = Image.new('RGB', (1500, 616), (35, 40, 51))
d = ImageDraw.Draw(board)
for tier in range(10):
    x, y = tier%5*300, tier//5*308
    d.text((x+8, y+4), f'T{tier}', fill='white')
    board.paste(shot(CAPTURE, f't{tier}').resize((300, 288), Image.Resampling.LANCZOS), (x, y+20))
board.save(OUT / 'wizard-battle-mage-all-tiers-ingame.png')

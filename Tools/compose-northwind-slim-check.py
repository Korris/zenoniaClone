"""Compare the grip correction at the same in-game camera."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'Build/wizard-northwind'
BOX = (250, 130, 650, 570)
def shot(folder, tier):
    return Image.open(BASE / folder / f't{tier}_1.png').convert('RGB').crop(BOX)
comparison = Image.new('RGB', (800, 1392), (35, 40, 51))
draw = ImageDraw.Draw(comparison)
for row, tier in enumerate((0, 5, 9)):
    for col, (folder, label) in enumerate((('captures-v8', 'Before'), ('captures-slim', 'Slim shaft + white hair'))):
        x, y = col*400, row*464
        draw.text((x+8, y+4), f'T{tier} - {label}', fill='white')
        comparison.paste(shot(folder, tier), (x, y+24))
comparison.save(ROOT / 'docs/reference/wizard-northwind-slim-before-after.png')
board = Image.new('RGB', (1500, 700), (35, 40, 51))
draw = ImageDraw.Draw(board)
for tier in range(10):
    x, y = tier%5*300, tier//5*350
    draw.text((x+8, y+4), f'T{tier}', fill='white')
    board.paste(shot('captures-slim', tier).resize((300, 330), Image.Resampling.LANCZOS), (x, y+20))
board.save(ROOT / 'docs/reference/wizard-northwind-slim-all-tiers-ingame.png')

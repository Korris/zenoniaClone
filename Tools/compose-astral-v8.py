"""Show both Wizard V8 variants at an identical camera, including tall staves."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT = Path(__file__).resolve().parents[1]
for variant in ('astral', 'northwind'):
    folder = ROOT / f'Build/wizard-{variant}/captures-v8'
    if not all((folder / f't{tier}_1.png').exists() for tier in range(10)):
        continue
    board = Image.new('RGB', (1500, 700), (35, 40, 51))
    draw = ImageDraw.Draw(board)
    for tier in range(10):
        x, y = tier % 5 * 300, tier // 5 * 350
        shot = Image.open(folder / f't{tier}_1.png')
        board.paste(shot.crop((250, 130, 650, 570)).resize((300, 330), Image.Resampling.LANCZOS), (x, y+20))
        draw.text((x+8, y+4), f'T{tier} - {variant.title()} V8', fill='white')
    board.save(ROOT / f'docs/reference/wizard-{variant}-v8-all-tiers-ingame.png')

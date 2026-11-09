from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Build/wizard-grandeur'


def panel(path, title):
    image = Image.open(path).convert('RGB').crop((250, 130, 650, 570))
    result = Image.new('RGB', (400, 470), '#18202b')
    result.paste(image, (0, 30))
    ImageDraw.Draw(result).text((12, 9), title, fill='white')
    return result


board = Image.new('RGB', (2000, 940), '#18202b')
for tier in range(10):
    board.paste(panel(OUT / f'captures/t{tier}_1.png', f'T{tier} - Wizard grandeur'),
                ((tier % 5) * 400, (tier // 5) * 470))
board.save(OUT / 'all-tiers-ingame.png')

comparison = Image.new('RGB', (1200, 470), '#18202b')
for index, (path, title) in enumerate([
    (ROOT / 'Build/wizard-current-reference/captures/t9_1.png', 'Current Wizard - T9'),
    (OUT / 'captures/t9_1.png', 'Wizard grandeur - T9'),
    (ROOT / 'Build/wizard-northwind/verify-t9_1.png', 'Northwind - T9'),
]):
    comparison.paste(panel(path, title), (index * 400, 0))
comparison.save(OUT / 'current-grandeur-northwind-t9.png')

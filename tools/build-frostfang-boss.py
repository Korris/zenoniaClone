"""Normalize the generated Frostfang King 4x4 sheet into the existing HD monster format."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
source = Image.open(ROOT / 'assets/hd/monsters/raw/frostfangKing.png').convert('RGBA')
cells = []
for row in range(4):
    for col in range(4):
        cell = source.crop((col * source.width // 4, row * source.height // 4,
                            (col + 1) * source.width // 4, (row + 1) * source.height // 4))
        alpha = cell.getchannel('A')
        bounds = alpha.point(lambda a: 255 if a > 40 else 0).getbbox()
        if not bounds:
            raise ValueError(f'Empty boss frame {row}/{col}')
        cells.append(cell.crop(bounds))
scale = min(170 / max(c.height for c in cells), 180 / max(c.width for c in cells))
sheet = Image.new('RGBA', (768, 768))
for i, cell in enumerate(cells):
    cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.Resampling.LANCZOS)
    sheet.alpha_composite(cell, ((i % 4) * 192 + (192 - cell.width) // 2,
                                (i // 4) * 192 + 192 - cell.height))
sheet.save(ROOT / 'assets/hd/monsters/normalized-hd/frostfangKing.png')
print('Frostfang King: 16 frames, 192px, down/up/left/right')

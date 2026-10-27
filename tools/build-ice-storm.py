"""Pack the generated 6x4 transparent VFX atlas into fixed 192px runtime cells."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1] / 'assets/hd/wizard/fx'
image = Image.open(root / 'ice-storm-clean.png').convert('RGBA')
sheet = Image.new('RGBA', (1152, 768))
for row in range(4):
    for col in range(6):
        cell = image.crop((col * image.width // 6, row * image.height // 4,
                           (col + 1) * image.width // 6, (row + 1) * image.height // 4))
        sheet.alpha_composite(cell.resize((192, 192), Image.Resampling.LANCZOS), (col * 192, row * 192))
sheet.save(root / 'ice-storm.png')
print('Ice Storm: 24 frames, 192px, transparent atlas 1152x768')

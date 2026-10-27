"""Pack generated directional attacks without shrinking the monsters' body size.

Sources: raw/<type>-attack.png, 4 columns x 4 rows down/up/left/right.
Output: 256px cells, feet at y224, eight source pixels per world unit.
"""
from pathlib import Path
import importlib.util
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('hero_helpers', ROOT / 'tools/build-hd-hero.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
RAW = ROOT / 'assets/hd/monsters/raw'
OUT = ROOT / 'assets/hd/monsters/normalized-hd'

for kind in ['frostSlime', 'iceGoblin', 'spectralWolf', 'frostfangKing']:
    arr = np.array(Image.open(RAW / f'{kind}-attack.png').convert('RGBA'))
    arr[..., 3] = np.where(arr[..., 3] > 24, arr[..., 3], 0)
    row_cuts = helper.grid_lines(arr[..., 3] > 40, 0, 4)
    cells = []
    for row in range(4):
        band = arr[row_cuts[row]:row_cuts[row + 1]]
        col_cuts = helper.grid_lines(band[..., 3] > 40, 1, 4)
        for col in range(4):
            cell = band[:, col_cuts[col]:col_cuts[col + 1]].copy()
            labels, sizes = helper.label_components(cell[..., 3] > 24)
            if not sizes:
                raise ValueError(f'Empty frame: {kind} {row}/{col}')
            largest = max(sizes, key=sizes.get)
            # Remove detached dust / neighboring frame scraps; keep the whole connected body.
            cell[..., 3] = np.where(labels == largest, cell[..., 3], 0)
            image = Image.fromarray(cell, 'RGBA')
            cells.append(image.crop(image.getbbox()))

    ref = Image.open(OUT / f'{kind}.png').convert('RGBA')
    reference_heights = []
    for row in range(4):
        frame = ref.crop((0, row * 192, 192, (row + 1) * 192))
        box = frame.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox()
        reference_heights.append(box[3] - box[1])
    # Use one scale for the entire sheet: squash/stretch remains actual motion.
    source_heights = [cells[row * 4].height for row in range(4)]
    scale = float(np.median(reference_heights) / np.median(source_heights))
    scale = min(scale, 240 / max(c.width for c in cells), 216 / max(c.height for c in cells))
    sheet = Image.new('RGBA', (1024, 1024))
    for i, cell in enumerate(cells):
        cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.Resampling.LANCZOS)
        sheet.alpha_composite(cell, ((i % 4) * 256 + (256 - cell.width) // 2,
                                    (i // 4) * 256 + 224 - cell.height))
    sheet.save(OUT / f'{kind}-attack.png')
    print(f'{kind}: 16 attack frames, scale {scale:.3f}, 1024x1024')

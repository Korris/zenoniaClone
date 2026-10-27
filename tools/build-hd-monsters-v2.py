"""Build four-direction 192px Frost Hollow sheets, adding real rear views and a fuller wolf.
Usage: python tools/build-hd-monsters-v2.py
"""
import importlib.util
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'assets/hd/monsters/raw'
OUT = ROOT / 'assets/hd/monsters/normalized-hd'
OUT.mkdir(exist_ok=True)
spec = importlib.util.spec_from_file_location('monster_helpers', ROOT / 'tools/build-hd-hero.py')
hero = importlib.util.module_from_spec(spec)
spec.loader.exec_module(hero)

def split(path, rows):
    arr = hero.normalize_alpha(Image.open(path).convert('RGBA'))
    rb = hero.grid_lines(arr[..., 3] > 0, 0, rows)
    result = []
    for r in range(rows):
        band = arr[rb[r]:rb[r + 1]]
        cb = hero.grid_lines(band[..., 3] > 0, 1, 4)
        cells = []
        for c in range(4):
            cell = band[:, cb[c]:cb[c + 1]].copy()
            labels, sizes = hero.label_components(cell[..., 3] > 0)
            if not sizes:
                raise RuntimeError(f'Empty frame {path.name}/{r}/{c}')
            main = max(sizes, key=sizes.get)
            cell[..., 3] = np.where(labels == main, cell[..., 3], 0)
            cells.append(hero.tight_bbox(cell))
        result.append(cells)
    return result

backs = split(RAW / 'rear-walk.png', 3)
for i, (kind, target) in enumerate([('frostSlime', 104), ('iceGoblin', 152), ('spectralWolf', 156)]):
    if kind == 'spectralWolf':
        rows = split(RAW / 'spectralWolf-v2.png', 4)
    else:
        original = split(RAW / f'{kind}.png', 3)
        if kind == 'iceGoblin':
            original[1] = [cell[:, ::-1] for cell in original[2]]
        rear = backs[i]
        if kind == 'iceGoblin':
            rear[2] = rear[0]  # generated frame 2 swaps the dagger hand; keep the consistent pose
        rows = [original[0], rear, original[1], original[2]]
    sheet = Image.new('RGBA', (768, 768), (0, 0, 0, 0))
    for row, cells in enumerate(rows):
        scale = target / np.median([cell.shape[0] for cell in cells])
        # Profile wolves are long rather than tall; preserve the full tail and paws.
        scale = min(scale, 184 / max(cell.shape[1] for cell in cells), 184 / max(cell.shape[0] for cell in cells))
        for col, cell in enumerate(cells):
            image = hero.downscale(Image.fromarray(cell, 'RGBA'), scale)
            image = image.crop(image.getbbox())
            sheet.alpha_composite(image, (col * 192 + (192 - image.width) // 2, row * 192 + 192 - image.height))
    sheet.save(OUT / f'{kind}.png')
    print(f'{kind}: 768x768, down/up/left/right, 192px frames OK')

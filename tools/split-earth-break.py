"""Export Earth Break as 24 padded PNGs with fixed stage anchors, not runtime atlas cuts."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
source = Image.open(root / 'assets/custom/earth-break-fx.png').convert('RGBA')
pixels = np.array(source)
# Discard the generated background's almost-transparent haze, preserving actual soft edges.
pixels[..., 3] = np.where(pixels[..., 3] > 48, pixels[..., 3], 0)
alpha = pixels[..., 3]
output = root / 'assets/custom/earth-break-frames'
output.mkdir(exist_ok=True)
stages = [('charge', 0, 270, 205, 320), ('sweep', 270, 475, 375, 192),
          ('slam', 475, 795, 760, 340), ('fissures', 795, 1024, 900, 192)]
manifest = {}
preview = Image.new('RGB', (1152, 896), '#1c3143')
draw = ImageDraw.Draw(preview)
sweep_source = np.array(Image.open(root / 'assets/custom/earth-break-sweep-source.png').convert('RGBA'))
sweep_source[..., 3] = np.where(sweep_source[..., 3] > 48, sweep_source[..., 3], 0)
sweeps = []
for i in range(6):
    sw, sh = sweep_source.shape[1] // 3, sweep_source.shape[0] // 2
    part = Image.fromarray(sweep_source[(i // 3) * sh:(i // 3 + 1) * sh, (i % 3) * sw:(i % 3 + 1) * sw], 'RGBA')
    sweeps.append(part.crop(part.getbbox()))
sweep_scale = min(280 / max(im.width for im in sweeps), 260 / max(im.height for im in sweeps))
for row, (name, top, bottom, ground, anchor_y) in enumerate(stages):
    profile = (alpha[top:bottom] > 0).sum(axis=0)
    edges = [0]
    for col in range(1, 6):
        expected = col * 256
        candidates = range(expected - 22, expected + 23)
        edges.append(min(candidates, key=lambda x: (int(profile[x-1:x+2].sum()), abs(x-expected))))
    edges.append(source.width)
    manifest[name] = []
    for col in range(6):
        left, right = edges[col:col+2]
        # The sweep/slam gutter differs slightly in each column; never include the next blade tip.
        split = min(range(465, 486), key=lambda y: (int((alpha[y, left:right] > 0).sum()), abs(y-475)))
        cell_top = split if name == 'slam' else top
        cell_bottom = split if name == 'sweep' else bottom
        crop = Image.fromarray(pixels[cell_top:cell_bottom, left:right], 'RGBA')
        frame = Image.new('RGBA', (384, 384))
        frame.alpha_composite(crop, (192 + left - (col * 256 + 128), anchor_y + cell_top - ground))
        if name == 'sweep':
            # This row was regenerated with isolated cells because the original crescents overlap.
            art = sweeps[col].resize((round(sweeps[col].width * sweep_scale), round(sweeps[col].height * sweep_scale)), Image.Resampling.LANCZOS)
            frame = Image.new('RGBA', (384, 384))
            frame.alpha_composite(art, ((384 - art.width) // 2, 192 - art.height // 2))
        filename = f'{name}-{col + 1}.png'
        frame.save(output / filename)
        source_rect = [(col % 3) * sw, (col // 3) * sh, (col % 3 + 1) * sw, (col // 3 + 1) * sh] if name == 'sweep' else [left, cell_top, right, cell_bottom]
        manifest[name].append({'file': filename, 'anchorX': 192, 'anchorY': anchor_y, 'referenceWidth': 256,
                               'sourceFile': 'earth-break-sweep-source.png' if name == 'sweep' else 'earth-break-fx.png',
                               'sourceRect': source_rect})
        sample = frame.resize((184, 184), Image.Resampling.LANCZOS)
        preview.paste(sample, (col * 192 + 4, row * 224 + 8), sample)
        draw.text((col * 192 + 8, row * 224 + 198), f'{name} {col + 1}', fill='#f0e3bf')
(output / 'frames.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
preview.save(output / 'contact-sheet.png')
print('Exported 24 standalone 384px frames with stable centers/ground anchors.')

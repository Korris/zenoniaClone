"""Cut the cleric holy-staff and spirit-companion art with the warrior cutter (tools/cut-warrior-gear-overlays.py):
  assets/custom/cleric-staff-overlay-sheet-raw.png  (5 cols x 4 rows: 1-5 base, 1-5 +10, 6-10 base, 6-10 +10)
      -> assets/custom/cleric-staff-overlays/, src/data/cleric-staff-overlay-manifest-generated.js  (grip = mid-lower shaft)
  assets/custom/cleric-spirit-overlay-sheet-raw.png (same layout)
      -> assets/custom/cleric-spirit-overlays/, src/data/cleric-spirit-overlay-manifest-generated.js (grip = body centre)
  assets/custom/cleric-spirit-float-sheet-raw.png   (4 cols x 4 rows: down/up/left/right x 4 float frames, tier 1 art)
      -> assets/custom/cleric-spirit-float/, src/data/cleric-spirit-float-manifest-generated.js
and contact sheets next to the idle cleric at in-game scale.
Usage: python tools/cut-cleric-gear-overlays.py [staff|spirit|float|all]
"""
import importlib.util
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
spec = importlib.util.spec_from_file_location('warrior_cutter', HERE / 'cut-warrior-gear-overlays.py')
cutter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cutter)
from weapon_overlay_geometry import detect_grip  # noqa: E402

ROOT = cutter.ROOT
STAFF_NAMES = ['acolyte', 'blossom', 'dawn', 'seraph', 'moonlit', 'radiant', 'halo', 'verdant', 'celestial', 'archangel']
SPIRIT_FRAME_PX = 60  # spirit drawn ~60 of the 192 frame px tall (= SPIRIT_ART_PX art px -> 60)
SPIRIT_ART_PX = 200


def spirit_grip(img):
    box = img.getbbox()
    return (box[0] + box[2]) // 2, (box[1] + box[3]) // 2


cutter.KINDS = {
    'staff': {'sheet': 'cleric-staff-overlay-sheet-raw.png', 'target': 256, 'export': 'CLERIC_STAFF_OVERLAYS',
              'names': STAFF_NAMES, 'dir': 'cleric-staff', 'tool': 'cut-cleric-gear-overlays.py', 'grip': lambda im: detect_grip(im, 0.34)},
    'spirit': {'sheet': 'cleric-spirit-overlay-sheet-raw.png', 'target': SPIRIT_ART_PX, 'export': 'CLERIC_SPIRIT_OVERLAYS',
               'names': STAFF_NAMES, 'dir': 'cleric-spirit', 'tool': 'cut-cleric-gear-overlays.py', 'grip': spirit_grip},
}


def contact_sheet(kind, manifest, out_dir, scale):
    """Idle front cleric + each tier base | enhanced at in-game scale (staff ~0.45 frame px / art px, spirit 0.3)."""
    hero = Image.open(ROOT / 'assets/hd/cleric/normalized-hd/idle.png').convert('RGBA').crop((0, 0, 192, 192))
    k = 0.45 if kind == 'staff' else SPIRIT_FRAME_PX / SPIRIT_ART_PX
    cell = 230
    sheet = Image.new('RGBA', (200 + cell * 10, cell * 2 + 20), (52, 56, 84, 255))
    sheet.alpha_composite(hero, (4, 20))
    d = ImageDraw.Draw(sheet)
    for t, e in enumerate(manifest):
        x0, y0 = 200 + (t % 5) * cell * 2, (t // 5) * cell + 20
        for v, tag in enumerate(('base', 'enhanced')):
            im = Image.open(ROOT / e[tag]['src']).convert('RGBA')
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.Resampling.LANCZOS)
            sheet.alpha_composite(im, (x0 + v * cell + 10, y0 + 10))
        d.text((x0, y0 - 14), f'{t} {e["name"]}  base | +10', fill=(255, 255, 0))
    sheet.save(out_dir / 'contact-sheet.png')


cutter.contact_sheet = contact_sheet


CLERIC_FLOAT = {'sheet': 'cleric-spirit-float-sheet-raw.png', 'dir': 'cleric-spirit-float', 'js': 'cleric-spirit-float-manifest-generated.js',
                'export': 'CLERIC_SPIRIT_FLOAT', 'art_px': SPIRIT_ART_PX, 'tool': 'cut-cleric-gear-overlays.py (float)',
                'note': 'Tier-1 spirit float loop: 4 facings x 4 frames, art scale = CLERIC_SPIRIT_OVERLAYS[0].base. Grip = spirit body centre.'}


def cut_float(cfg=CLERIC_FLOAT):
    """4x4 float sheet -> 16 PNGs registered in one canvas per direction row (relative bob motion kept).
    cfg is reused by tools/cut-mechanic-gear-overlays.py for the robot companion."""
    out_dir = ROOT / 'assets/custom' / cfg['dir']
    out_dir.mkdir(parents=True, exist_ok=True)
    arr = np.array(Image.open(ROOT / 'assets/custom' / cfg['sheet']).convert('RGBA'))
    arr[..., 3] = np.where(arr[..., 3] >= cutter.HAZE, arr[..., 3], 0)
    ys, xs = cutter.cell_edges(arr, 4, 4)
    first = cutter.cut_cell(arr, ys[0], ys[1], xs[0], xs[1])
    s = cfg['art_px'] / (first.getbbox()[3] - first.getbbox()[1])  # same art scale as the tier-1 static spirit
    manifest = {}
    for r, dirname in enumerate(['down', 'up', 'left', 'right']):
        cells = [cutter.cut_cell(arr, ys[r], ys[r + 1], xs[c], xs[c + 1]) for c in range(4)]
        scaled = [cutter.resize_img(c, s) for c in cells]
        boxes = [c.getbbox() for c in scaled]
        # shared canvas: the union of all 4 frame boxes, each frame keeps its own position relative to its cell centre
        cx = [(c.width) // 2 for c in scaled]
        left = max(cx[i] - boxes[i][0] for i in range(4)) + 4
        right = max(boxes[i][2] - cx[i] for i in range(4)) + 4
        height = max(c.height for c in scaled)
        manifest[dirname] = []
        for i, im in enumerate(scaled):
            canvas = Image.new('RGBA', (left + right, height), (0, 0, 0, 0))
            canvas.alpha_composite(im, (left - cx[i], 0))
            canvas.save(out_dir / f'{dirname}-{i}.png')
            manifest[dirname].append({'src': f'assets/custom/{cfg["dir"]}/{dirname}-{i}.png', 'w': canvas.width, 'h': canvas.height,
                                      'gripX': left, 'gripY': height // 2})
    header = f'// GENERATED by tools/{cfg["tool"]} -- do not edit by hand.\n// {cfg["note"]}\n'
    js = header + f'export const {cfg["export"]} = Object.freeze({json.dumps(manifest, indent=2)});\n'
    (ROOT / 'src/data' / cfg['js']).write_text(js, encoding='utf-8')
    print('float: wrote 16 frames + manifest')


if __name__ == '__main__':
    arg = sys.argv[1] if len(sys.argv) > 1 else 'all'
    for k in ('staff', 'spirit'):
        if arg in ('all', k):
            cutter.run(k)
    if arg in ('all', 'float'):
        cut_float()

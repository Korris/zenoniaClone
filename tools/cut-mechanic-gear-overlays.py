"""Cut the mechanic gun and robot-companion art with the warrior cutter (tools/cut-warrior-gear-overlays.py):
  assets/custom/mechanic-gun-overlay-sheet-raw.png   (5 cols x 4 rows: 1-5 base, 1-5 +10, 6-10 base, 6-10 +10; barrel -> RIGHT)
      -> assets/custom/mechanic-gun-overlays/, src/data/mechanic-gun-overlay-manifest-generated.js  (grip = handle centre)
  assets/custom/mechanic-robot-overlay-sheet-raw.png (same layout)
      -> assets/custom/mechanic-robot-overlays/, src/data/mechanic-robot-overlay-manifest-generated.js (grip = body centre)
  assets/custom/mechanic-robot-float-sheet-raw.png   (4x4: down/up/left/right x 4 float frames, tier 1 art)
      -> assets/custom/mechanic-robot-float/, src/data/mechanic-robot-float-manifest-generated.js
Guns/robots are horizontal / round, so art is scaled by solid WIDTH and the +10 art is registered to the base by template
matching (the warrior cutter registers along a vertical item axis). Gun art keeps its cut orientation: angle 0 = barrel points right.
Usage: python tools/cut-mechanic-gear-overlays.py [gun|robot|float|all]
"""
import importlib.util
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, HERE / file)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


cutter = load('warrior_cutter', 'cut-warrior-gear-overlays.py')
cleric = load('cleric_cutter', 'cut-cleric-gear-overlays.py')  # for cut_float (generic through cfg)
ROOT = cutter.ROOT
GUN_NAMES = ['scrap', 'brass', 'copper', 'plasma', 'ember', 'frost', 'volt', 'gatling', 'star', 'dragon']
GUN_LENGTH_PX, ROBOT_WIDTH_PX = 256, 200
ROBOT_FRAME_PX = 52  # robot drawn ~52 of the 192 frame px wide


def solid_width(img):
    cols = np.nonzero((np.array(img)[..., 3] >= cutter.SOLID).any(axis=0))[0]
    return cols[-1] - cols[0] + 1


def gun_grip(img):
    """Handle centre: centroid of solid pixels in the lower-left of the body (grip sits bottom-left, barrel -> right)."""
    a = np.array(img)[..., 3] >= cutter.SOLID
    ys, xs = np.nonzero(a)
    x0, y0, w, h = xs.min(), ys.min(), xs.max() - xs.min(), ys.max() - ys.min()
    sel = (xs > x0 + 0.12 * w) & (xs < x0 + 0.38 * w) & (ys > y0 + 0.6 * h) & (ys < y0 + 0.95 * h)
    return int(xs[sel].mean()), int(ys[sel].mean())


def body_centre(img):
    box = img.getbbox()
    return (box[0] + box[2]) // 2, (box[1] + box[3]) // 2


def register(base, enh):
    """(dx, dy, scale): placement of the base art inside the enhanced art + the scale that makes it match. Template
    matching of the solid (opaque) masks over a small scale range; the sparks/flames around add little to the solid core."""
    b, e = [(np.array(im)[..., 3] >= 235).astype(np.float32) for im in (base, enh)]
    best = (-1.0, 1.0, (0, 0))
    for s in np.arange(0.86, 1.2, 0.02):
        t = cv2.resize(b, (max(1, round(b.shape[1] * s)), max(1, round(b.shape[0] * s))), interpolation=cv2.INTER_AREA)
        pad = 24
        canvas = cv2.copyMakeBorder(e, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=0)
        if t.shape[0] > canvas.shape[0] or t.shape[1] > canvas.shape[1]:
            continue
        res = cv2.matchTemplate(canvas, t, cv2.TM_CCORR) / (np.sqrt(t.sum() * e.sum()) + 1e-6)
        _, mx, _, loc = cv2.minMaxLoc(res)
        if mx > best[0]:
            best = (mx, float(s), (loc[0] - pad, loc[1] - pad))
    score, s, (dx, dy) = best
    return dx, dy, s if abs(s - 1) > 0.02 else 1.0  # ratio (enh vs base) in process(); the 2nd call only prints it


cutter.solid_height = solid_width
cutter.register = register
cutter.KINDS = {
    'gun': {'sheet': 'mechanic-gun-overlay-sheet-raw.png', 'target': GUN_LENGTH_PX, 'export': 'MECHANIC_GUN_OVERLAYS', 'names': GUN_NAMES,
            'dir': 'mechanic-gun', 'tool': 'cut-mechanic-gear-overlays.py', 'grip': gun_grip},
    'robot': {'sheet': 'mechanic-robot-overlay-sheet-raw.png', 'target': ROBOT_WIDTH_PX, 'export': 'MECHANIC_ROBOT_OVERLAYS', 'names': GUN_NAMES,
              'dir': 'mechanic-robot', 'tool': 'cut-mechanic-gear-overlays.py', 'grip': body_centre},
}
FLOAT = {'sheet': 'mechanic-robot-float-sheet-raw.png', 'dir': 'mechanic-robot-float', 'js': 'mechanic-robot-float-manifest-generated.js',
         'export': 'MECHANIC_ROBOT_FLOAT', 'art_px': ROBOT_WIDTH_PX, 'tool': 'cut-mechanic-gear-overlays.py (float)',
         'note': 'Tier-1 robot float loop: 4 facings x 4 frames, art scale = MECHANIC_ROBOT_OVERLAYS[0].base. Grip = robot body centre.'}


def contact_sheet(kind, manifest, out_dir, scale):
    """Idle front mechanic + each tier base | enhanced at in-game scale (gun ~0.2 frame px / art px, robot 0.26)."""
    hero = Image.open(ROOT / 'assets/hd/mechanic/weaponless/normalized-hd/idle.png').convert('RGBA').crop((0, 0, 192, 192))
    k = 0.2 if kind == 'gun' else ROBOT_FRAME_PX / ROBOT_WIDTH_PX
    cell = 200
    sheet = Image.new('RGBA', (200 + cell * 10, cell * 2 + 20), (52, 56, 84, 255))
    sheet.alpha_composite(hero, (4, 20))
    d = ImageDraw.Draw(sheet)
    for t, e in enumerate(manifest):
        x0, y0 = 200 + (t % 5) * cell * 2, (t // 5) * cell + 20
        for v, tag in enumerate(('base', 'enhanced')):
            im = Image.open(ROOT / e[tag]['src']).convert('RGBA')
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.Resampling.LANCZOS)
            sheet.alpha_composite(im, (x0 + v * cell + 10, y0 + 40))
        d.text((x0, y0 - 14), f'{t} {e["name"]}  base | +10', fill=(255, 255, 0))
    sheet.save(out_dir / 'contact-sheet.png')


cutter.contact_sheet = contact_sheet

if __name__ == '__main__':
    arg = sys.argv[1] if len(sys.argv) > 1 else 'all'
    for k in ('gun', 'robot'):
        if arg in ('all', k):
            cutter.run(k)
    if arg in ('all', 'float'):
        cleric.cut_float(FLOAT)

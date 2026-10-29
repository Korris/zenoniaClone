"""Cut the assassin dagger art with the warrior cutter (tools/cut-warrior-gear-overlays.py):
  assets/custom/assassin-dagger-overlay-sheet-raw.png (5 cols x 4 rows: 1-5 base, 1-5 +10, 6-10 base, 6-10 +10; blade UP)
      -> assets/custom/assassin-dagger-overlays/, src/data/assassin-dagger-overlay-manifest-generated.js (grip = handle wrap)
and a contact sheet next to the idle assassin at in-game scale. The SAME art serves the main hand (weapon slot) and the off
hand (offhand slot); the renderer mirrors nothing, the anchors carry angle / sx per hand.
Usage: python tools/cut-assassin-gear-overlays.py
"""
import importlib.util
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
spec = importlib.util.spec_from_file_location('warrior_cutter', HERE / 'cut-warrior-gear-overlays.py')
cutter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cutter)
from weapon_overlay_geometry import detect_grip  # noqa: E402

ROOT = cutter.ROOT
NAMES = ['iron-kunai', 'bronze-fang', 'violet-edge', 'venom', 'frost-shard', 'ember', 'storm', 'shadow', 'moonlight', 'void-reaper']
DAGGER_PX = 256  # blade tip .. pommel ring
IN_GAME_K = 0.14  # frame px per art px (dagger ~36 of the 192 frame px)

cutter.KINDS = {
    'dagger': {'sheet': 'assassin-dagger-overlay-sheet-raw.png', 'target': DAGGER_PX, 'export': 'ASSASSIN_DAGGER_OVERLAYS',
               'names': NAMES, 'dir': 'assassin-dagger', 'tool': 'cut-assassin-gear-overlays.py', 'grip': lambda im: detect_grip(im, 0.22)},
}


def contact_sheet(kind, manifest, out_dir, scale):
    """Idle front assassin + each tier base | enhanced at in-game scale."""
    hero = Image.open(ROOT / 'assets/hd/assassin/weaponless/normalized-hd/idle.png').convert('RGBA').crop((0, 0, 192, 192))
    cell = 100
    sheet = Image.new('RGBA', (200 + cell * 10, cell * 2 + 20), (52, 56, 84, 255))
    sheet.alpha_composite(hero, (4, 20))
    d = ImageDraw.Draw(sheet)
    for t, e in enumerate(manifest):
        x0, y0 = 200 + (t % 5) * cell * 2, (t // 5) * cell + 20
        for v, tag in enumerate(('base', 'enhanced')):
            im = Image.open(ROOT / e[tag]['src']).convert('RGBA')
            im = im.resize((max(1, round(im.width * IN_GAME_K)), max(1, round(im.height * IN_GAME_K))), Image.Resampling.LANCZOS)
            sheet.alpha_composite(im, (x0 + v * cell + 20, y0 + 10))
        d.text((x0, y0 - 14), f'{t} {e["name"]}  base | +10', fill=(255, 255, 0))
    sheet.save(out_dir / 'contact-sheet.png')


cutter.contact_sheet = contact_sheet

if __name__ == '__main__':
    cutter.run('dagger')

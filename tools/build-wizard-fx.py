"""Builds the wizard's HD spell effects from the ChatGPT sheet assets/hd/wizard/fx.png.

The sheet holds 6 effects in reading order (FX_ORDER): 3 projectiles painted flying to screen-right
(the game rotates them to the cast direction) and 3 round bursts. Each is cut by
tools/sprite_sheet_cutter.py and saved to assets/hd/wizard/fx/<name>.png at FX_UNITS world units
x 4 px per unit. FX_UNITS must match HD_WIZARD_FX in src/graphics/asset-atlas-hd-wizard.js.
Usage: python tools/build-wizard-fx.py
"""
from pathlib import Path

from sprite_sheet_cutter import cut_sheet

SRC = Path('assets/hd/wizard/fx.png')
OUT = Path('assets/hd/wizard/fx')
PX_PER_UNIT = 4

FX_ORDER = ['magicBolt', 'fireball', 'iceShard', 'fireBurst', 'frostNova', 'arcaneBurst']
# World size (w, h) per effect, before the in-game draw scale.
FX_UNITS = {
    'magicBolt': (28, 12), 'fireball': (28, 13), 'iceShard': (28, 14),
    'fireBurst': (40, 40), 'frostNova': (40, 40), 'arcaneBurst': (40, 40),
}

if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    sizes = {n: (w * PX_PER_UNIT, h * PX_PER_UNIT) for n, (w, h) in FX_UNITS.items()}
    cut_sheet(SRC, FX_ORDER, sizes, OUT)

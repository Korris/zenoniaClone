"""Builds the Mechanic's HD gun effects from the ChatGPT sheet assets/hd/mechanic/fx.png.

The sheet holds 6 effects in reading order (FX_ORDER): 3 projectiles painted flying to screen-right
(the game rotates them to the shot direction), a muzzle flash and 2 round bursts. Each is cut by
tools/sprite_sheet_cutter.py and saved to assets/hd/mechanic/fx/<name>.png at FX_UNITS world units
x 4 px per unit. Names must match HD_MECHANIC_FX in src/graphics/asset-atlas-hd-mechanic.js.
Usage: python tools/build-mechanic-fx.py
"""
from pathlib import Path

from sprite_sheet_cutter import cut_sheet

SRC = Path('assets/hd/mechanic/fx.png')
OUT = Path('assets/hd/mechanic/fx')
PX_PER_UNIT = 4

FX_ORDER = ['bullet', 'plasmaShot', 'rocket', 'muzzleFlash', 'explosion', 'shockPulse']
# World size (w, h) per effect, before the in-game draw scale.
FX_UNITS = {
    'bullet': (20, 8), 'plasmaShot': (28, 12), 'rocket': (28, 13),
    'muzzleFlash': (20, 20), 'explosion': (40, 40), 'shockPulse': (40, 40),
}

if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    sizes = {n: (w * PX_PER_UNIT, h * PX_PER_UNIT) for n, (w, h) in FX_UNITS.items()}
    cut_sheet(SRC, FX_ORDER, sizes, OUT)

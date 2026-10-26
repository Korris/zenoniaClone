"""Builds Oakhaven Village v6 art from the raw ChatGPT sheets in assets/hd/map/oakhaven-v6/.

Inputs (raw, as downloaded from ChatGPT):
  ground-raw-v2.png  1536x1024 opaque painted ground (grass, roads, plaza, river, bridge, dock, farm,
                     forest edge) -- the whole map backdrop, 36x24 tiles.
  houses-raw-v1.png  5 buildings on transparent alpha (faint dark halo), reading order:
                     elder manor, Mia's cottage, hunter lodge / healer cottage, carpenter workshop.
  props-raw-v1.png   15 props on transparent alpha (faint halo), 5x3 reading order (PROP_ORDER).
  ground-hd-{nw,ne,sw,se}.png  optional: ChatGPT's high-res redraws of 4 overlapping quadrants of
                     ground-raw-v2.png; when all 4 exist they are stitched into the backdrop
                     (tools/oakhaven_ground_stitch.py), otherwise the painting is upscaled.

Outputs:
  assets/hd/map/oakhaven-v6/ground.webp   backdrop at 4 px per world unit (36*16*4 x 24*16*4); lossy
                                          WebP q92 (~1.4 MB vs ~6.7 MB PNG) since it loads at every boot
  assets/hd/props/normalized/<type>.png   one canvas per prop type, sized exactly
                                          PROP_TILES[type] * 16 * 4 px (bottom-centre fit)

Sheets are cut by tools/sprite_sheet_cutter.py (halo removal, blob labeling, reading order,
premultiplied resize).
PROP_TILES MUST match width/height in src/world/prop-definitions.js. Safe to rerun.
Usage: python tools/build-oakhaven-village-art.py
"""
from pathlib import Path

from PIL import Image, ImageFilter

from oakhaven_ground_stitch import QUADRANTS, stitch_ground
from sprite_sheet_cutter import cut_sheet

SRC = Path('assets/hd/map/oakhaven-v6')
PROP_OUT = Path('assets/hd/props/normalized')
PX_PER_UNIT = 4
TILE = 16
MAP_TILES = (36, 24)

HOUSE_ORDER = ['elderHouse', 'cottageRed', 'hunterLodge', 'healerCottage', 'carpenterWorkshop']
PROP_ORDER = [
    'well', 'lanternPost', 'marketStall', 'scarecrow', 'woodpile',
    'campfire', 'barrel', 'crate', 'rowboat', 'villageFence',
    'villageOak', 'villagePine', 'villageBush', 'villageFlowerBush', 'villageSignpost',
]
# World size in tiles (w, h) per prop -- keep in sync with src/world/prop-definitions.js.
PROP_TILES = {
    'elderHouse': (5.75, 5.0), 'cottageRed': (3.5, 3.75), 'hunterLodge': (4.25, 3.75),
    'healerCottage': (4.75, 3.75), 'carpenterWorkshop': (6.0, 3.5),
    'well': (2.25, 2.5), 'lanternPost': (1.25, 2.5), 'marketStall': (2.75, 2.5), 'scarecrow': (1.75, 2.0),
    'woodpile': (2.25, 1.875), 'campfire': (1.75, 1.375), 'barrel': (1.0, 1.25), 'crate': (1.25, 1.125),
    'rowboat': (1.25, 2.25), 'villageFence': (2.0, 1.0),
    'villageOak': (3.5, 3.75), 'villagePine': (2.25, 3.75), 'villageBush': (1.75, 1.75),
    'villageFlowerBush': (2.0, 1.75), 'villageSignpost': (1.5, 2.25),
}


def cut_props(path, names):
    """Cut a raw sheet into normalized prop canvases sized PROP_TILES[name] tiles at 4 px/unit."""
    sizes = {n: (round(PROP_TILES[n][0] * TILE * PX_PER_UNIT), round(PROP_TILES[n][1] * TILE * PX_PER_UNIT)) for n in names}
    cut_sheet(path, names, sizes, PROP_OUT)


def build_ground():
    size = (MAP_TILES[0] * TILE * PX_PER_UNIT, MAP_TILES[1] * TILE * PX_PER_UNIT)
    raw = Image.open(SRC / 'ground-raw-v2.png').convert('RGB')
    quads = {n: SRC / f'ground-hd-{n}.png' for n in QUADRANTS}
    if all(p.exists() for p in quads.values()):
        # Preferred: stitch ChatGPT's 4 high-res redraws (see oakhaven_ground_stitch.py) -- real detail.
        ground = stitch_ground(raw, {n: Image.open(p) for n, p in quads.items()}, size)
    else:
        # Fallback: ChatGPT paints at most 1536x1024, so the single painting is UPSCALED 1.5x and
        # reads softer than the (downscaled) props; an unsharp mask restores edge contrast.
        ground = raw.resize(size, Image.Resampling.LANCZOS).filter(
            ImageFilter.UnsharpMask(radius=1.5, percent=110, threshold=2))
    ground.save(SRC / 'ground.webp', quality=92, method=6)
    print(f'  ground {size[0]}x{size[1]}')


if __name__ == '__main__':
    PROP_OUT.mkdir(parents=True, exist_ok=True)
    build_ground()
    cut_props(SRC / 'houses-raw-v1.png', HOUSE_ORDER)
    cut_props(SRC / 'props-raw-v1.png', PROP_ORDER)

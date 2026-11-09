"""Astral V7 + ultimate T9, uniform anatomy and growing staves, build copy only."""
import argparse
import importlib.util
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('wizard_variant_export', ROOT / 'Tools/install-wizard-northwind-v7.py')
export = importlib.util.module_from_spec(spec)
spec.loader.exec_module(export)
export.VARIANT = 'astral'
export.ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/astral-v7'
export.BUILD = ROOT / 'Build/wizard-astral'
export.PARTS = export.BUILD / 'extracted'
export.STAFF_HEIGHTS = [160 + 9*tier for tier in range(10)]

original_split = export.split
def split_v8():
    original_split()
    source = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/astral-v8/t9-separated.png'
    parts = export.pieces(Image.open(source).convert('RGBA'), 3)
    staff = max(parts, key=lambda im: im.height/im.width)
    book = min(parts, key=lambda im: im.height/im.width)
    body = next(im for im in parts if im is not staff and im is not book)
    for name, im in (('staff', staff), ('book', book), ('body', body)):
        im.save(export.PARTS / f't9-{name}.png')
export.split = split_v8

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('command', choices=('split', 'install'))
    parser.add_argument('--target', type=Path)
    args = parser.parse_args()
    if args.command == 'split':
        export.split()
    else:
        assert args.target is not None
        export.install(args.target)

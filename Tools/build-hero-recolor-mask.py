"""Layered hero recolour: source art drawn by the spec colour rules -> game sheet (outfit turned grey) + mask (R hair, G outfit).

  python Tools/build-hero-recolor-mask.py convert SRC.png OUT.png OUT-mask.png [--hair]
      SRC on flat #00FF00; outfit in magenta (#FF8CFF / #FF00FF / #A000A0, hue 285-320, sat > 0.45); --hair = a grey hair layer
      (#242424 / #555555 / #999999 / #E0E0E0; grey pixels -> mask R, coloured accents stay). Prints how many pixels went to each mask.
  python Tools/build-hero-recolor-mask.py selftest
      synthetic figure (Tools/hero_layered_test_figure.py) -> docs/reference/hero-recolor-test/: source / grey sheet / mask, and
      8 hair x 6 outfit colours (Data/hero-layered/palettes.json) through the same ramp as HeroRecolor.shader.
  python Tools/build-hero-recolor-mask.py test-sheets
      the synthetic figure as game data for `--layered-test` (Sprites/hero-layered/test/, Data/hero-layered/test/: body + mask + fist
      sheets, hair, hat, sword, shield, head anchors, grips). Spec: docs/character-appearance-layered-spec.md.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hero_recolor_core as core  # noqa: E402
import hero_layered_test_figure as fig  # noqa: E402
import hero_layered_test_sheets as sheets  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PALETTES = os.path.join(ROOT, 'Assets/Resources/Data/hero-layered/palettes.json')
PREVIEW = os.path.join(ROOT, 'docs/reference/hero-recolor-test')


def convert(src, out, out_mask, hair):
    rgba = core.key_green(Image.open(src))
    sheet, mask = core.to_game(rgba, hair_layer=hair)
    Image.fromarray(sheet).save(out)
    Image.fromarray(mask).save(out_mask)
    print('%s: %d outfit px, %d hair px -> %s, %s' % (src, (mask[..., 1] > 0).sum(), (mask[..., 0] > 0).sum(), out, out_mask))


def composite(base, layer, at):
    """Pastes a keyed layer image (PIL RGBA) onto base at top-left `at`."""
    base.alpha_composite(layer, (int(at[0]), int(at[1])))


def figure_rgba(d=1):
    """Keyed body + hair (+ None) of painted dir d, idle frame, as numpy RGBA arrays, plus the hair paste position."""
    body, _, head, _ = fig.body_frame(d, 'idle', 0)
    hair = fig.hair_cell(d)
    hx, hy = head['x'] - fig.HAIR_AX, head['y'] - fig.HAIR_AY
    return core.key_green(body), core.key_green(hair), (hx, hy)


def selftest():
    os.makedirs(PREVIEW, exist_ok=True)
    pal = json.load(open(PALETTES, encoding='utf-8'))
    body, hair, at = figure_rgba(1)
    gbody, gmask = core.to_game(body)
    ghair, hmask = core.to_game(hair, hair_layer=True)
    # 1) source | game grey | mask, at 2x
    trio = Image.new('RGBA', (fig.CW * 3, fig.CH), (40, 44, 52, 255))
    for i, (b, h) in enumerate(((body, hair), (gbody, ghair), (gmask, hmask))):
        cell = Image.new('RGBA', (fig.CW, fig.CH), (40, 44, 52, 255) if i < 2 else (0, 0, 0, 255))
        composite(cell, Image.fromarray(b), (0, 0))
        composite(cell, Image.fromarray(h), at)
        trio.alpha_composite(cell, (fig.CW * i, 0))
    trio.resize((trio.width * 2, trio.height * 2), Image.NEAREST).save(os.path.join(PREVIEW, 'source-grey-mask.png'))
    # 2) 8 hair x 6 outfit grid, cropped to the figure
    crop = (80, 40, 240, 250)
    cw, chh = crop[2] - crop[0], crop[3] - crop[1]
    grid = Image.new('RGBA', (cw * 8 + 90, chh * 6 + 30), (58, 64, 74, 255))
    gd = ImageDraw.Draw(grid)
    for c, name in enumerate(pal['hairNames']):
        gd.text((90 + c * cw + 4, 8), name[:20], fill=(255, 255, 255, 255))
    for r, oc in enumerate(pal['outfit']):
        gd.text((4, 30 + r * chh + chh // 2), pal['outfitNames'][r][:13], fill=(255, 255, 255, 255))
        for c, hc in enumerate(pal['hair']):
            cell = Image.new('RGBA', (fig.CW, fig.CH), (0, 0, 0, 0))
            composite(cell, Image.fromarray(core.recolour(gbody, gmask, None, oc)), (0, 0))
            composite(cell, Image.fromarray(core.recolour(ghair, hmask, hc, None)), at)
            grid.alpha_composite(cell.crop(crop), (90 + c * cw, 30 + r * chh))
    grid.save(os.path.join(PREVIEW, 'hair8-x-outfit6.png'))
    # 3) a 3x close-up of four picks, to judge the shading steps
    picks = [(0, 0), (3, 1), (5, 2), (7, 5)]
    close = Image.new('RGBA', (cw * 4, chh), (58, 64, 74, 255))
    for i, (h, o) in enumerate(picks):
        cell = Image.new('RGBA', (fig.CW, fig.CH), (0, 0, 0, 0))
        composite(cell, Image.fromarray(core.recolour(gbody, gmask, None, pal['outfit'][o])), (0, 0))
        composite(cell, Image.fromarray(core.recolour(ghair, hmask, pal['hair'][h], None)), at)
        close.alpha_composite(cell.crop(crop), (i * cw, 0))
    close.resize((close.width * 3, close.height * 3), Image.NEAREST).save(os.path.join(PREVIEW, 'closeup-4-picks-3x.png'))
    leak = int(((gmask[..., 1] > 0) & ~core.outfit_mask(body)).sum())
    print('outfit px %d, hair px %d, leak %d -> %s' % ((gmask[..., 1] > 0).sum(), (hmask[..., 0] > 0).sum(), leak, PREVIEW))


def main(argv):
    if len(argv) >= 4 and argv[0] == 'convert':
        convert(argv[1], argv[2], argv[3], '--hair' in argv)
    elif argv[:1] == ['selftest']:
        selftest()
    elif argv[:1] == ['test-sheets']:
        sheets.write(ROOT)
    else:
        print(__doc__)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

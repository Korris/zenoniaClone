"""Wizard V3 (Asteria Mage, female, no hat, 26/11/2026) -> game, through the shared RO Fantasy pipeline without editing it.

  python Tools/build-wizard-v3-game-assets.py write    body sheets + 3 hair styles + body.json / class.json (wizard)
  python Tools/build-wizard-v3-game-assets.py staff    V3 staff sprite + grip (Sprites/hero-layered/weapons/wizard-staff.png, weapons.json)
  python Tools/build-wizard-v3-game-assets.py fists    fists json + marked 4x sheets (ArtLibrary/ro-fantasy-heroes/sheets/wizard-v3/)
  python Tools/build-wizard-v3-game-assets.py grips    hand layer + staff / book grips (class.json)
  python Tools/build-wizard-v3-game-assets.py check    offline sheets composed like the game (docs/reference/character-v3/wizard-v3-test/check/)
Run write, staff, fists, grips, check in that order after new art lands.

How: sets RO_FANTASY_VARIANT=wizard-v3 (sources from Assets/ArtLibrary/ro-fantasy-heroes/src/wizard-v3/, the newest -chatgpt-N of
body-wizard-idle-5dir / hair-wizard-style<k>-5dir / body-wizard-walk-<dir>-8f / body-wizard-attack-<dir>-8f; bald-head ref
ref/wizard-v3-test/), then patches the shared modules in memory only:
  - the gold hair flower keeps its colour (accessory pixels, like the Assassin headband), 3 hair styles;
  - the old Wizard fist tables (19/11 art) are replaced by Tools/wizard_v3_fists.py;
  - the 8-direction attack gets staff grips per painted row (Tools/wizard_v3_attack8_grips.py).
Game outputs are the normal Wizard paths (Sprites/hero-layered/ro-fantasy/wizard/, Data/hero-layered/ro-fantasy/wizard/).
Revert: copy D:/temp/wizard-v2-backup/ back (see docs/reference/character-v3/wizard-v3/HOW-TO-REVERT.txt).
"""
import os
import sys
from importlib import import_module
from pathlib import Path

os.environ["RO_FANTASY_VARIANT"] = "wizard-v3"
sys.path.insert(0, str(Path(__file__).resolve().parent))

import ro_fantasy_hair_cells as hc  # noqa: E402

hc.ACC_CLASSES = tuple(hc.ACC_CLASSES) + ("wizard",)  # gold flower = accessory, keeps its colour in the hair strip
_hair_layer = hc.ac.hair_layer


def _gold_hair_layer(rgba):
    """Wizard V3: accessory = the GOLD hair ornaments only (flower, pins; any size, touching the hair) + their gems and dark outline.
    The generic rule (saturated pieces > 3000 px touching hair) kept the navy collar and shaded ear skin and lost the small flowers
    of the back views (their pale petals then read as stray grey hair)."""
    import numpy as np
    from scipy import ndimage
    keep, _ = _hair_layer(rgba)
    r, g, b, a = [rgba[..., i] for i in range(4)]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    gold = (a > 0) & (r > 0.45) & (r >= g) & (g > b) & (r - b > 0.28) & (s > 0.32) & (g - b > 0.42 * (r - b)) & ~hc.ac.skin_mask(rgba)  # yellow-orange, not shaded skin
    lab, n = ndimage.label(gold)
    if n:
        sizes = ndimage.sum(gold, lab, range(1, n + 1))
        touch = ndimage.maximum(ndimage.binary_dilation(keep, iterations=8), lab, range(1, n + 1))
        gold = np.isin(lab, 1 + np.nonzero((sizes >= 40) & (touch > 0))[0])
    region = ndimage.binary_fill_holes(ndimage.binary_closing(gold, iterations=3))  # gems inside the flower
    pale = ndimage.binary_dilation(region, iterations=3) & (a > 0) & (r >= g) & (g >= b) & (r - b > 0.12) & (g - b > 0.42 * (r - b))
    region |= pale  # pale petal highlights
    lum = rgba[..., :3].mean(-1)
    rim = ndimage.binary_dilation(region, iterations=3) & (a > 0) & (lum < 0.38) & ~hc.ac.skin_mask(rgba)  # its brown outline
    acc = (region | rim) & (a > 0)
    return keep & ~acc, acc


hc.ac.hair_layer = _gold_hair_layer
game = import_module("build-ro-fantasy-hero-game-assets")
game.HAIR_STYLES_OF["wizard"] = 3


def patch_grips():
    g = import_module("ro_fantasy_grips")
    wf = import_module("wizard_v3_fists")
    for k in [k for k in g.OVERRIDES if k[0] == "wizard"]:
        del g.OVERRIDES[k]  # 19/11 art
    g.OVERRIDES.update(wf.overrides())
    a8 = import_module("ro_fantasy_attack8_grips")
    a8.FISTS.update({("wizard", d8, f): v for (d8, f), v in wf.ATTACK.items()})
    old = g.steady_staff

    def steady(cj, *a, **k):
        old(cj, *a, **k)
        import_module("wizard_v3_attack8_grips").apply(cj)
    g.steady_staff = steady
    return g


def main(cmd):
    if cmd == "write":
        game.write_class("wizard")
    elif cmd == "staff":
        import_module("wizard_v3_staff").write()
    elif cmd == "fists":
        patch_grips().cmd_fists("wizard")
    elif cmd == "grips":
        patch_grips().cmd_grips("wizard")
    elif cmd == "check":
        import_module("ro_fantasy_ingame_check").main(["wizard"])
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "")

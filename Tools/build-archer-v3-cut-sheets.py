"""Archer V3 (Asteria Ranger, male, 26/11/2026): cut every ChatGPT sheet and INSTALL it into the game (HeroClass.Archer, since 26/11)
with the RO Fantasy pipeline of Knight V3 (Tools/build-ro-fantasy-hero-game-assets.py + ro_fantasy_* helpers).

  python Tools/build-archer-v3-cut-sheets.py key      magenta sources (src/archer-v3/) -> #00FF00 copies in src/archer-v3/keyed/
  python Tools/build-archer-v3-cut-sheets.py heads    bald-head strip x3 (hair is painted on it) -> ref/archer-v3/bald-head-archer-5dir.png
  python Tools/build-archer-v3-cut-sheets.py write    body sheets (idle / walk / attack 8 dirs) + 3 hair styles + body.json / class.json
  python Tools/build-archer-v3-cut-sheets.py fists    fist table (R = right hand = arrow / string hand, L = left hand = BOW hand)
  python Tools/build-archer-v3-cut-sheets.py weapons  bow / quiver / arrow cut from the weapons sheet + bow grips (Tools/archer_v3_weapons.py)
  python Tools/build-archer-v3-cut-sheets.py check    offline sheets composed like the game (review images)
  python Tools/build-archer-v3-cut-sheets.py all      key, heads, write, fists, weapons, check

Why a wrapper: the archer art is painted on #FF00FF (green cloak / eyes), the pipeline keys #00FF00, so `key` converts the magenta
background (alpha from the distance to pure magenta, colour un-premultiplied) onto pure green; the green key of the pipeline then leaves the
cloak (g - max(r, b) ~ 0.13-0.16 < 0.30) and the enclosed green irises (hole refill) intact. Then the pipeline runs with its module paths
pointed at the archer sources (same trick as the RO_FANTASY_VARIANT test look):
  game output        Assets/Resources/{Sprites,Data}/hero-layered/ro-fantasy/archer/ (like every class), bow / quiver / arrow art in
                     Sprites/hero-layered/weapons/archer-*.png (grip in Data/hero-layered/weapons.json, written under the shared lock
                     D:/temp/hero-install.lock)
  fists json         Assets/ArtLibrary/classes/archer/ro-fantasy-v3/sheets/archer-fists.json
  review images      docs/reference/character-v3/archer-v3/check/
Shared tools are not edited: only module attributes are set at run time in this process.
"""
import shutil
import sys
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))

ART = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes"
SRC_ARCHER = ART / "src/archer-v3"
KEYED = SRC_ARCHER / "keyed"
OUT = ROOT / "Assets/ArtLibrary/classes/archer/ro-fantasy-v3"
GAME = ROOT / "Assets/Resources"  # installed like the other classes (26/11; was a game-ready copy under ArtLibrary)
DOCS = ROOT / "docs/reference/character-v3/archer-v3"

# keyed name <- magenta source (the approved idle is -chatgpt-3 with the 3/4 Back cell mirrored; newest() reads the N of the name)
IDLE_SRC = "body-archer-idle-5dir-v3-chatgpt-3-flip34back.png"
IDLE_SRC_OVERRIDE = SRC_ARCHER / "body-archer-idle-5dir-v3-chatgpt-4.png"  # a proper 3/4 Back redraw wins when it exists


def setup_paths():
    """Point the shared RO Fantasy modules at the archer folders (this process only)."""
    import ro_fantasy_variant as v
    v.VARIANT = "archer-v3/keyed"          # art_check.newest(): src/archer-v3/keyed/ first
    v.SRC = KEYED
    v.REF = ART / "ref/archer-v3"          # bald-head strip the archer hair is painted on
    v.SHEETS = OUT / "sheets"
    v.DOCS = DOCS
    v.review_dir = lambda default: DOCS / "check"
    for p in (v.REF, v.SHEETS, DOCS / "check"):
        p.mkdir(parents=True, exist_ok=True)
    bc = import_module("ro_fantasy_body_cut")
    bc.SRC = KEYED
    # Archer V3 = Knight V3 proportions (sprite-rules 26/11: skull-chin 34.7 % of the height) -> same 216 px skull top to feet
    bc.BALD_PX_OF["archer"] = 216
    hc = import_module("ro_fantasy_hair_cells")
    hc.PAINTED_ON_RAMP = tuple(hc.PAINTED_ON_RAMP) + ("archer",)  # hair painted as masses on the Ramp5 knots, like Knight V3
    hc.ACC_CLASSES = tuple(hc.ACC_CLASSES) + ("archer",)  # style 2: feather + cord + gold bead keep their colour (in the hair strip)
    ga = import_module("build-ro-fantasy-hero-game-assets")  # default outputs: Assets/Resources (Sprites|Data)/hero-layered/ro-fantasy
    ga.HAIR_STYLES_OF["archer"] = 3
    _boost_style3(hc)
    g = import_module("ro_fantasy_grips")
    g.SPR, g.DATA, g.FISTS, g.REF = ga.SPR, ga.DATA, v.SHEETS, DOCS / "check"
    return bc, ga, g


# Hair style 3 (pending-tasks 26/11 11:20: "highlight yeu: tang sang khi cat"): its painted shine strands sit low, so after the Ramp5 map
# most of the hair reads as Base and the gloss is lost. Lift the neutral greys toward the Light knot (0.86), like the Golden Paladin lift:
# shadow 0.41 -> 0.46, base 0.65 -> 0.70, the top ~20 % (>= 0.70) onto Light. Outline (0.12) and deep (0.28) stay.
STYLE3_LIFT = ([0.0, 0.12, 0.28, 0.41, 0.62, 0.70, 0.94, 1.0], [0.0, 0.12, 0.30, 0.46, 0.70, 0.86, 0.95, 1.0])


def _boost_style3(hc):
    old = hc._strips_from

    def strips(cls, k, idle_cells, path, body, hr, ref):
        strip, acc, name = old(cls, k, idle_cells, path, body, hr, ref)
        if cls == "archer" and k == 3:
            h = strip.copy()
            m = h[..., 3] > 0
            neutral = m & (np.abs(h[..., 0].astype(int) - h[..., 2].astype(int)) < 6)
            g2 = np.interp(h[..., 0].astype(np.float32) / 255.0, *STYLE3_LIFT)
            for c in range(3):
                h[..., c] = np.where(neutral, (g2 * 255).round().astype(np.uint8), h[..., c])
            print("archer style3: highlight lift", STYLE3_LIFT[1])
            strip = h
        return strip, acc, name
    hc._strips_from = strips


def magenta_to_green(path, dst):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum.reduce([g, 255 - r, 255 - b]) - 40) / 80, 0, 1)
    M, G = np.array([255, 0, 255], np.float32), np.array([0, 255, 0], np.float32)
    al = alpha[..., None]
    fg = np.where(al > 0.02, (a - (1 - al) * M) / np.maximum(al, 0.02), 0).clip(0, 255)
    # magenta spill on the outline band (seen 26/11: purple fringe on the grey hair, kept as a coloured "accessory" by the hair tool):
    # pixels near the background whose red AND blue both exceed green get red / blue pulled down to green + their own min excess
    from scipy import ndimage
    near = ndimage.binary_dilation(alpha < 0.5, iterations=4)
    fr, fgc, fb = fg[..., 0], fg[..., 1], fg[..., 2]
    spill = near & (fr > fgc + 6) & (fb > fgc + 6)
    ex = np.minimum(fr - fgc, fb - fgc)
    fg[..., 0] = np.where(spill, fr - ex, fr)
    fg[..., 2] = np.where(spill, fb - ex, fb)
    out = al * fg + (1 - al) * G
    Image.fromarray(out.round().astype(np.uint8), "RGB").save(dst)


def cmd_key():
    KEYED.mkdir(parents=True, exist_ok=True)
    idle = IDLE_SRC_OVERRIDE if IDLE_SRC_OVERRIDE.exists() else SRC_ARCHER / IDLE_SRC
    jobs = [(idle, KEYED / "body-archer-idle-5dir-chatgpt-3.png")]
    # walk / attack / hair sources: same names as the pipeline expects (body-archer-walk-<dir>-8f-chatgpt-N.png, ...)
    for p in sorted(SRC_ARCHER.glob("*.png")):
        if p.name.startswith(("body-archer-walk-", "body-archer-attack-", "hair-archer-", "weapons-archer-")):
            jobs.append((p, KEYED / p.name))
    for s, d in jobs:
        if s.name.startswith("weapons-archer-") or _is_green(s):
            shutil.copyfile(s, d)  # already on #00FF00
        else:
            magenta_to_green(s, d)
        print("keyed", s.name, "->", d.relative_to(ROOT))


def _is_green(p):
    a = np.asarray(Image.open(p).convert("RGB"))[:8, :8].reshape(-1, 3).mean(0)
    return a[1] > 200 and a[0] < 80 and a[2] < 80


def main(cmd):
    if cmd in ("key", "all"):
        cmd_key()
    bc, ga, g = setup_paths()
    if cmd in ("heads", "all"):
        bc.art_check.cmd_heads("archer")
    if cmd in ("write", "all"):
        ga.write_class("archer")
    if cmd in ("fists", "all"):
        import_module("archer_v3_fists").run(bc, ga, g, OUT / "sheets", DOCS / "check")
    if cmd in ("weapons", "all"):
        import_module("archer_v3_weapons").main(bc, ga, g, OUT, GAME, DOCS)
    if cmd in ("check", "all"):
        import_module("archer_v3_check").main(bc, ga, GAME, DOCS)
    if cmd not in ("key", "heads", "write", "fists", "weapons", "check", "all"):
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "")

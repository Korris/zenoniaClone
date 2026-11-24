"""RO Fantasy heroes: optional TEST look variant (2026-11-24, Golden Paladin test for the Warrior / Starward set).

Set the environment variable RO_FANTASY_VARIANT=golden-paladin before running the RO Fantasy tools
(build-ro-fantasy-hero-game-assets.py, build-starward-knight-weapons.py, build-ro-fantasy-hero-art-check.py) and they:
  - read the ChatGPT sources from Assets/ArtLibrary/ro-fantasy-heroes/src/<variant>/ first (same file names as src/),
    falling back to src/ for anything the variant does not have (e.g. the older hairstyles);
  - use the variant fist tables (Tools/ro_fantasy_golden_paladin_fists.py) instead of the Knight V3 ones;
  - write their review / reference images under the variant folders, never over the Knight V3 ones.
The game outputs (Sprites/hero-layered/ro-fantasy/warrior, Data/hero-layered/ro-fantasy/warrior, weapons) are the SAME paths:
revert = copy the backup back (docs/reference/character-v3/golden-paladin-test/HOW-TO-REVERT.txt).
Unset (default) = the normal Knight V3 pipeline, unchanged.
"""
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
VARIANT = os.environ.get("RO_FANTASY_VARIANT", "").strip()
ART = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes"
SRC_MAIN = ART / "src"
SRC = SRC_MAIN / VARIANT if VARIANT else SRC_MAIN
# bald-head reference strip the hair of the variant was painted on (x3, #00FF00)
REF = ART / "ref" / f"{VARIANT}-test" if VARIANT else ART / "ref"
# fists json + review images of the variant
SHEETS = ART / "sheets" / VARIANT if VARIANT else ART / "sheets"
DOCS = ROOT / "docs/reference/character-v3" / f"{VARIANT}-test" if VARIANT else None


def bald_head_ref(cls, hair_path=None):
    """The bald-head strip a hair source was painted on: the variant one for a variant source, else the main one."""
    if VARIANT and (hair_path is None or Path(hair_path).parent == SRC):
        return REF / f"bald-head-{cls}-5dir.png"
    return ART / "ref" / f"bald-head-{cls}-5dir.png"


def review_dir(default):
    """Where review images go: the variant docs folder (check/) when a variant is set, else `default`."""
    return DOCS / "check" if VARIANT else default

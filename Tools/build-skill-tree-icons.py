"""Skill tree icons: Assets/Resources/Sprites/skills/<class>/<skill-id>/icon.png (128 px, Lanczos).

  py -3 -E Tools/build-skill-tree-icons.py --from-preview [--class wizard]
      crops the icon at the bottom-left of each panel of Assets/ArtLibrary/custom/skill-vfx-preview/<class>.png (4x2 panels, order = tree index)
  py -3 -E Tools/build-skill-tree-icons.py --sheet <class> <sheet.png>
      8-icon sheet (2 rows x 4, transparent) from the art pipeline -> same output files

Skill ids / order come from the SkillTable.<Class>.cs partials (Def(C, index, name, ...)), id = "<class>.<name lower, spaces -> '-'>".
"""
import argparse, re, sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
PREVIEW = ROOT / "Assets/ArtLibrary/custom/skill-vfx-preview"
OUT = ROOT / "Assets/Resources/Sprites/skills"
TABLES = ROOT / "Assets/Scripts/Rpg/Skills"
CLASSES = ["warrior", "wizard", "mechanic", "cleric", "assassin"]
SIZE = 128
# Preview panel origins (1774 x 887 sheet, 8 px gutters) and the icon art rect inside a panel (inside the gold trim, px from its top-left).
PANEL_X, PANEL_Y = (0, 446, 891, 1336), (0, 428)
ICON_X, ICON_Y, ICON_W = 42, 349, 62


def skill_ids(cls):
    src = (TABLES / ("SkillTable." + cls.capitalize() + ".cs")).read_text(encoding="utf-8")
    found = re.findall(r'Def\(C,\s*(\d+),\s*"([^"]+)"', src)  # every partial declares const string C = "<class>"
    ids = {int(i): cls + "." + n.lower().replace(" ", "-") for i, n in found}
    if sorted(ids) != list(range(8)):
        sys.exit("cannot read 8 skills of " + cls + ": " + str(ids))
    return [ids[i] for i in range(8)]


def save(icon, cls, sid):
    d = OUT / cls / sid.split(".", 1)[1]  # folder = skill slug, same as the VFX slicer
    d.mkdir(parents=True, exist_ok=True)
    icon.convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS).save(d / "icon.png")
    print("wrote", d / "icon.png")


def from_preview(cls):
    im = Image.open(PREVIEW / (cls + ".png")).convert("RGB")
    for i, sid in enumerate(skill_ids(cls)):
        x0, y0 = PANEL_X[i % 4] + ICON_X, PANEL_Y[i // 4] + ICON_Y
        save(im.crop((x0, y0, x0 + ICON_W, y0 + ICON_W)), cls, sid)


def from_sheet(cls, path):
    im = Image.open(path).convert("RGBA")
    cw, ch = im.width // 4, im.height // 2
    for i, sid in enumerate(skill_ids(cls)):
        cell = im.crop(((i % 4) * cw, (i // 4) * ch, (i % 4 + 1) * cw, (i // 4 + 1) * ch))
        box = cell.getbbox()
        if box:
            cell = cell.crop(box)
        side = max(cell.size)
        sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        sq.paste(cell, ((side - cell.width) // 2, (side - cell.height) // 2))
        save(sq, cls, sid)


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--from-preview", action="store_true")
    ap.add_argument("--class", dest="cls", choices=CLASSES)
    ap.add_argument("--sheet", nargs=2, metavar=("CLASS", "PNG"))
    a = ap.parse_args()
    if a.sheet:
        from_sheet(a.sheet[0], a.sheet[1])
    elif a.from_preview:
        for c in ([a.cls] if a.cls else CLASSES):
            from_preview(c)
    else:
        ap.print_help()

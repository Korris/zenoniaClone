"""RO Fantasy heroes -> game (2026-11-19): one body per class, 3 dyeable hairstyles, weapons on the fists.

  python Tools/build-ro-fantasy-hero-game-assets.py write [class]   body sheets + empty outfit mask + hair styles 0-2 + body.json / class.json
  python Tools/build-ro-fantasy-hero-game-assets.py fists [class]   detect the fists (Tools/ro_fantasy_grips.py), marked sheets for review
  python Tools/build-ro-fantasy-hero-game-assets.py grips [class]   hand layer + weapon grips (8 dirs) into class.json
  python Tools/build-ro-fantasy-hero-game-assets.py check [class]   offline sheets composed like the game (Tools/ro_fantasy_ingame_check.py)
Run all four in that order after new art lands (walk / attack files: just run them again; missing anims draw the idle frame with a
stand-in motion in code). Sources (read only): Assets/ArtLibrary/ro-fantasy-heroes/src/.

Outputs: Sprites/hero-layered/ro-fantasy/<class>/body/<anim>.png, <anim>-mask.png (empty: the outfit keeps its real colours),
body/hand/<anim>.png, hair/<style>.png (+ <style>-acc.png Mechanic goggles); Data/hero-layered/ro-fantasy/<class>/body.json, class.json
(`fixedOutfit`: the creation screen hides the outfit colours). The renderer prefers this set over the common gender body
(HeroRenderer.LayeredSetFor; `--hero-style layered-common` = the common bodies, `classic` = paper doll).
"""
import json
import sys
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import ro_fantasy_body_cut as bc  # noqa: E402
import ro_fantasy_hair_cells as hc  # noqa: E402

game_assets = import_module("build-hero-layered-game-assets")  # meta(), mkdirs()
ROOT = bc.ROOT
SPR = ROOT / "Assets/Resources/Sprites/hero-layered/ro-fantasy"
DATA = ROOT / "Assets/Resources/Data/hero-layered/ro-fantasy"


def res(p):
    return str(p.relative_to(ROOT / "Assets/Resources")).replace("\\", "/")


def write_class(cls):
    idle, src_name = bc.idle(cls)
    idle_cells = [c for c, _ in idle]
    frames = {"idle": [[c] for c in idle_cells]}
    frames.update(bc.motion_rows(cls, [c.crop(c.getbbox()) for c in idle_cells]))
    bc.recentre_walk(frames, idle_cells)
    body_dir = SPR / cls / "body"
    game_assets.mkdirs(body_dir)
    for anim, rows in frames.items():
        sheet = bc.sheet_of(rows)
        sheet.save(body_dir / f"{anim}.png")
        mask = np.zeros((sheet.height, sheet.width, 4), np.uint8)
        mask[..., 3] = 255  # no hair / outfit region: the body keeps every painted colour
        Image.fromarray(mask).save(body_dir / f"{anim}-mask.png")
        game_assets.meta(body_dir / f"{anim}.png", "idle")
        game_assets.meta(body_dir / f"{anim}-mask.png", "idle-mask")
    head = [{"anim": a, "dir": d, "f": f, "x": x, "y": int(y), "w": w, "angle": 0} for a, d, f, x, y, w in bc.head_anchors(frames)]
    game_assets.mkdirs(DATA / cls)
    body = {"note": f"RO Fantasy {cls} body (Tools/build-ro-fantasy-hero-game-assets.py, from {src_name}). Rows: down, down-right, right, "
                    "up-right, up. Anims not listed draw the idle frame with a stand-in motion (HeroRenderer.Layered.cs).",
            "cellW": bc.CW, "cellH": bc.CH, "feetY": bc.FEET, "bodyPx": 200, "sheets": res(body_dir) + "/",
            "anims": [{"name": a, "frames": len(rows[0])} for a, rows in frames.items()], "head": head}
    (DATA / cls / "body.json").write_text(json.dumps(body), encoding="utf-8")
    hair_dir = SPR / cls / "hair"
    game_assets.mkdirs(hair_dir)
    entries, names = [], []
    for k in (1, 2, 3):
        got = hc.style_strips(cls, k, idle)
        if got is None:
            continue
        strip, acc, name = got
        style = len(entries)
        Image.fromarray(strip).save(hair_dir / f"{style}.png")
        game_assets.meta(hair_dir / f"{style}.png", "idle")
        e = {"style": style, "sheet": res(hair_dir / str(style)), "cellW": hc.CELL_W, "cellH": hc.CELL_H, "ax": hc.AX, "ay": hc.AY, "refW": hc.REF_W}
        if acc is not None:
            Image.fromarray(acc).save(hair_dir / f"{style}-acc.png")
            game_assets.meta(hair_dir / f"{style}-acc.png", "idle")
            e["accSheet"] = res(hair_dir / f"{style}-acc")
        entries.append(e)
        names.append(name)
    path = DATA / cls / "class.json"
    old = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}
    cj = {"note": f"RO Fantasy {cls}: body per class, outfit fixed, hair {', '.join(names)} (Tools/build-ro-fantasy-hero-game-assets.py); "
                  "weapons: `grips`; hats: Data/hero-layered/headgear.json.",
          "body": res(DATA / cls / "body"), "fixedOutfit": True, "hair": entries, "headgear": [],
          "weapons": old.get("weapons", []), "grips": old.get("grips", [])}
    path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
    print(cls, "written:", [a for a in frames], len(entries), "hair styles")


def main(cmd, which):
    classes = bc.CLASSES if which in (None, "all") else [which]
    if cmd == "write":
        for c in classes:
            write_class(c)
    elif cmd in ("fists", "grips"):
        g = import_module("ro_fantasy_grips")
        for c in classes:
            (g.cmd_fists if cmd == "fists" else g.cmd_grips)(c)
    elif cmd == "check":
        import_module("ro_fantasy_ingame_check").main(classes)
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "", sys.argv[2] if len(sys.argv) > 2 else None)

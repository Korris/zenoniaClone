"""Wizard basic-attack VFX v4, after the combo v2 board (2026-11-10, Assets/ArtLibrary/classes/wizard/attack-board/northwind-combo-v2/): one ChatGPT
sheet 1024x1536 on flat #00FF00, 7 rows x 4 frames (cells 256 x ~219 px), Assets/ArtLibrary/classes/wizard/vfx/attack-v4/wizard-attack-fx-v4-chatgpt-1.png
(chat https://chatgpt.com/c/6af289b8-8a08-83ec-9c0d-4d140f9a957e, prompt-01-sheet.txt). Same pipeline as Tools/build-wizard-attack-fx-v3.py (loaded
with its constants replaced), set "wizard-v4" (VfxSprites.IdFor prefers it): Resources/Sprites/classes/wizard/fx/attack-v4/<row>_<n>.png and
Resources/Data/classes/wizard/vfx-attack-v4.json. Sheet rows -> set rows:
  1 Crystal Flash   -> cast    (every hit's windup at the staff crystal, HeroFx.WizardCastTip)
  2 Ice Bolt        -> proj    (hit 1, flying right, pivot on the crystal head)
  3 Frost Crescent  -> shard   (hit 2, BoltDef sprite "frostShard", pivot on the leading tips)
  4 Glacier Spikes  -> spikes  (hit 3, BoltDef sprite "glacierSpikes": slides along the ground upright, pivot 85 % down)
  5 Ice Orb         -> bolt    (hit 4's orb, pivot at its centre)
  6 Blizzard Nova   -> blast   (hit 4's burst, VfxAnim.Blast sizes it to the blast radius)
  7 Ice Impact      -> impact  (hits 1-3; pivot 70 % down)

  python Tools/build-wizard-attack-fx-v4.py cut       frames, json, metas
  python Tools/build-wizard-attack-fx-v4.py preview   OUT.png: every frame at game size on snow and on dark, next to the hero
"""
import importlib.util
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
os.chdir(ROOT)
sys.path.insert(0, f"{ROOT}/Tools")


def _load_v3():
    """The v3 tool with its root, paths and set name replaced (its code otherwise unchanged)."""
    path = f"{ROOT}/Tools/build-wizard-attack-fx-v3.py"
    src = open(path, encoding="utf-8").read()
    for old, new in (('ROOT = "D:/zenoniaClone"', f'ROOT = "{ROOT}"'),
                     ('vfx/attack-v3/wizard-attack-fx-v3-chatgpt-1.png', 'vfx/attack-v4/wizard-attack-fx-v4-chatgpt-1.png'),
                     ('OUT = "Assets/Resources/Sprites/classes/wizard/fx/attack-v3"', 'OUT = "Assets/Resources/Sprites/classes/wizard/fx/attack-v4"'),
                     ('LIKE = "Assets/Resources/Sprites/classes/wizard/fx/attack-v2"', 'LIKE = "Assets/Resources/Sprites/classes/wizard/fx/attack-v3"'),
                     ('SHEET_ROWS = ["cast", "proj", "shard", "bolt", "blast", "impact"]', 'SHEET_ROWS = ["cast", "proj", "shard", "spikes", "bolt", "blast", "impact"]'),
                     ('ROW_SCALE = {"cast": 1.15, "proj": 1.3, "shard": 1.3, "bolt": 1.1, "blast": 1.0, "impact": 1.25}',
                      'ROW_SCALE = {"cast": 0.85, "proj": 1.3, "shard": 1.3, "spikes": 1.25, "bolt": 1.1, "blast": 1.0, "impact": 1.25}'),  # cast 0.85: the v4 star is drawn bigger than v3's; at 1.15 (x1.35 finisher) it covered the hero
                     ("vfx-attack-v3.json", "vfx-attack-v4.json"), ("vfx-attack-v2.json.meta", "vfx-attack-v3.json.meta"),
                     ("""        like = f"{LIKE}/{'cast' if name not in ('cast', 'proj', 'impact') else name}_{n}.png.meta\"""",
                      """        like = f"{LIKE}/{name if os.path.exists(f'{LIKE}/{name}_{n}.png.meta') else 'cast'}_{n}.png.meta\""""),
                     ('''    if name == "impact":
        return im.width / 2.0, im.height * 0.7''', '''    if name == "impact":
        return im.width / 2.0, im.height * 0.7
    if name == "spikes":                                  # standing on the ground, sliding toward the target
        return im.width / 2.0, im.height * 0.85''')):
        assert old in src, old[:50]
        src = src.replace(old, new)
    m = importlib.util.module_from_spec(importlib.util.spec_from_file_location("fx3", path))
    exec(compile(src, path, "exec"), m.__dict__)
    return m


def _load_compose():
    """Tools/wizard_attack_compose.py (the preview's hero) with its hard-coded root replaced, registered for the v3 tool's `import`."""
    path = f"{ROOT}/Tools/wizard_attack_compose.py"
    src = open(path, encoding="utf-8").read().replace('ROOT = "D:/zenoniaClone"', f'ROOT = "{ROOT}"')
    m = importlib.util.module_from_spec(importlib.util.spec_from_file_location("wizard_attack_compose", path))
    exec(compile(src, path, "exec"), m.__dict__)
    sys.modules["wizard_attack_compose"] = m


_load_compose()
fx = _load_v3()

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cut": fx.cut()
    elif cmd == "preview": fx.preview(sys.argv[2])
    else: print(__doc__)

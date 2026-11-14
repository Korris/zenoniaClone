"""Cuts the Cliff Harpy feather projectile into Assets/Resources/Sprites/fx/monster-shots/cliff-harpy-feather.png (loaded by MonsterProjectile).

Usage: python Tools/cut-cliff-harpy-feather.py
Source: Assets/ArtLibrary/monsters/sunbreak/cliff-harpy-feather-chatgpt-1.png (1536x1024 on #FF00FF): the feather with its wind streaks in the top half (tip pointing
right), a hit spark in the bottom half (not used yet). Keying is build-meadow-monsters.key_magenta. MonsterProjectile shows the sprite at GameConfig.FramePpu
(8 px per world unit) and rotates it to the flight direction, so the art must point right; the old code quill was 10 units long, the feather + streaks get 96 px (12 units).
"""
import importlib.util, os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("mm", os.path.join(HERE, "build-meadow-monsters.py"))
mm = importlib.util.module_from_spec(spec); spec.loader.exec_module(mm)
SRC = os.path.join(ROOT, "Assets/ArtLibrary/monsters/sunbreak/cliff-harpy-feather-chatgpt-1.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/fx/monster-shots/cliff-harpy-feather.png")
WIDTH = 96


def main():
    rgba = mm.key_magenta(SRC)[:512]                      # top half: feather + streaks only
    ys, xs = np.where(rgba[..., 3] > 0.5)
    crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    im = Image.fromarray((np.clip(crop, 0, 1) * 255).astype(np.uint8), "RGBA")
    # premultiply before resizing so the keyed-out magenta does not bleed into the rim
    arr = np.asarray(im).astype(np.float32); a = arr[..., 3:] / 255.0
    h = max(1, round(im.height * WIDTH / im.width))
    pm = np.asarray(Image.fromarray(np.dstack([arr[..., :3] * a, arr[..., 3:]]).astype(np.uint8), "RGBA").resize((WIDTH, h), Image.LANCZOS)).astype(np.float32)
    pa = pm[..., 3:] / 255.0
    rgb = np.where(pa > 0, pm[..., :3] / np.maximum(pa, 1e-6), 0)
    out = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), pm[..., 3:]]).astype(np.uint8), "RGBA")
    canvas = Image.new("RGBA", (WIDTH + 4, h + 4), (0, 0, 0, 0)); canvas.alpha_composite(out, (2, 2))   # 2 px clear border for point filtering
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    canvas.save(OUT, optimize=True)
    print("wrote", OUT, canvas.size)


if __name__ == "__main__":
    main()

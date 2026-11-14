"""Cuts the Sand Wraith's sand orb projectile out of its attack sheet into Assets/Resources/Sprites/fx/monster-shots/sand-wraith-orb.png (MonsterProjectile).

Usage: python Tools/cut-sand-wraith-orb.py
Source: Assets/ArtLibrary/monsters/scorpion-gorge/sand-wraith-attack-chatgpt.png (1254x1254, 4x4 on #FF00FF): row 4 (facing right), frame 3 shows the orb
flying right with its sand trail, clear of the wraith's body. Every alpha blob in that box except the body (the blob touching the box's left edge) is kept.
MonsterProjectile draws the art at 8 px per world unit, pointing right and rotated to the flight direction; the orb gets 44 px (5.5 units).
"""
import importlib.util, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("mm", os.path.join(HERE, "build-meadow-monsters.py"))
mm = importlib.util.module_from_spec(spec); spec.loader.exec_module(mm)
SRC = os.path.join(ROOT, "Assets/ArtLibrary/monsters/scorpion-gorge/sand-wraith-attack-chatgpt.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/fx/monster-shots/sand-wraith-orb.png")
BOX = (828, 1058, 944, 1158)   # x0, y0, x1, y1 on the sheet: orb + trail of row 4 frame 3
WIDTH = 44


def main():
    a = mm.key_magenta(SRC)[BOX[1]:BOX[3], BOX[0]:BOX[2]].copy()
    lab, n = ndi.label(a[..., 3] > 0.05)
    body = set(np.unique(lab[:, 0])) - {0}   # the wraith's hand reaches the left edge of the box
    a[..., 3] *= ~np.isin(lab, list(body))
    ys, xs = np.where(a[..., 3] > 0.3)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    # despill: the trail's soft edges were painted over the magenta key and keep a pink rim (R and B above G); sand has none
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = np.minimum(r, b) - g > 0.03
    a[..., 0] = np.where(pink, np.minimum(r, g + 0.15), r)
    a[..., 2] = np.where(pink, g * 0.7, b)
    arr = (np.clip(a, 0, 1) * 255).astype(np.float32); al = arr[..., 3:] / 255.0
    h = max(1, round(a.shape[0] * WIDTH / a.shape[1]))
    pm = np.asarray(Image.fromarray(np.dstack([arr[..., :3] * al, arr[..., 3:]]).astype(np.uint8), "RGBA").resize((WIDTH, h), Image.LANCZOS)).astype(np.float32)
    pa = pm[..., 3:] / 255.0
    rgb = np.where(pa > 0, pm[..., :3] / np.maximum(pa, 1e-6), 0)
    img = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), pm[..., 3:]]).astype(np.uint8), "RGBA")
    canvas = Image.new("RGBA", (WIDTH + 4, h + 4), (0, 0, 0, 0)); canvas.alpha_composite(img, (2, 2))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    canvas.save(OUT, optimize=True)
    print("wrote", OUT, canvas.size)


if __name__ == "__main__":
    main()

"""Grades the Willowbrook props that were cut out of the FIRST painting (wb*, not wbx*) to the redrawn art (13/11): that painting was sunset yellow-olive,
the redrawn ground and the redrawn prop sheet (wbx*) are green and more contrasty, so the old oaks, rocks and wood looked faded next to them.

Usage: python Tools/grade-willowbrook-old-section-props-to-redraw.py
 Source: Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-src/wb*.png (the sprites as build-willowbrook-props.py cut them; never overwritten,
 so this can be re-run). Output: Assets/Resources/Sprites/village/props/wb*.png, same canvas, same alpha.
 Oaks: canopy (opaque upper 60%) mean and contrast moved to the average of the redrawn oaks wbxOakBig/Mid/Small (the reverse of grade_to_old_oaks in
 cut-willowbrook-extra-props.py). Everything else (rocks, cliffs, fences, shed, hay, logs, signpost, palisade): a little less yellow, a little more contrast,
 the same curve as the bridge (cut-willowbrook-bridge-prop.py).
"""
import glob, os
import numpy as np
from PIL import Image
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-src")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
NEW_OAKS = ["wbxOakBig", "wbxOakMid", "wbxOakSmall"]


def canopy(a):
    m = a[..., 3] > 200; m[int(a.shape[0] * 0.6):] = False
    return a[m][:, :3].mean(0), a[m][:, :3].std(0)


def main():
    st = [canopy(np.asarray(Image.open(os.path.join(OUT, n + ".png")).convert("RGBA")).astype(np.float32)) for n in NEW_OAKS]
    mu_new, sd_new = np.mean([s[0] for s in st], 0), np.mean([s[1] for s in st], 0)
    for p in sorted(glob.glob(os.path.join(SRC, "wb*.png"))):
        name = os.path.basename(p)[:-4]
        a = np.asarray(Image.open(p).convert("RGBA")).astype(np.float32)
        if "Oak" in name:
            mu, sd = canopy(a)
            k = np.clip(sd_new / np.maximum(sd, 1e-3), 1.0, 1.5)
            a[..., :3] = (a[..., :3] - mu) * k + mu + (mu_new - mu) * 0.8     # 0.8: trunks and the grass at the roots ride along, keep them believable
        else:
            a[..., :3] = (a[..., :3] - 128) * 1.08 + 128 + np.array([-10, -6, 2], np.float32)
        a[..., :3] = np.clip(a[..., :3], 0, 255)
        Image.fromarray(a.astype(np.uint8), "RGBA").save(os.path.join(OUT, name + ".png"), optimize=True)
        print("graded", name)


if __name__ == "__main__":
    main()

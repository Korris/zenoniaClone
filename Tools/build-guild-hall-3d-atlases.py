"""Install the ChatGPT Guild Hall atlases for the 3D map (Map3DGuildHallBuilder; RO3 look loop round 2, 24/11/2026).

usage: python Tools/build-guild-hall-3d-atlases.py a <chatgpt-atlas-a.png>
       python Tools/build-guild-hall-3d-atlases.py b <chatgpt-atlas-b.png>

a: the 2x2 structure atlas (plaster / stone / timber / slate roof). Each quadrant is cut on the image's own seam line (ChatGPT draws ~1254 px,
   quadrants not exactly half), resized to 512 and packed as drawn into Assets/Resources/Map3D/guild-atlas-a.png (1024, Map3DPainted _Mode 2
   tiles inside each cell). ChatGPT's cells already tile; a roll-blend to force it ghosted the bricks / slates (tried 24/11), so none.
b: the detail atlas on #FF00FF: magenta keyed to alpha (soft edge, de-spilled), resized to 1024 -> Assets/Resources/Map3D/guild-atlas-b.png;
   prints the bounding boxes of the separate pieces in uv (v up) so Map3DGuildHallBuilder.B* rects can be set.
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

OUT = 'Assets/Resources/Map3D/'


def seam_split(a, axis):
    """Index of the strongest straight seam near the middle (the quadrant border ChatGPT painted)."""
    n = a.shape[axis]
    g = np.abs(np.diff(a.astype(np.float32), axis=axis)).mean(axis=(1 - axis, 2))
    lo, hi = int(n * 0.4), int(n * 0.6)
    return lo + int(np.argmax(g[lo:hi])) + 1


def atlas_a(src):
    a = np.asarray(Image.open(src).convert('RGB'))
    sy, sx = seam_split(a, 0), seam_split(a, 1)
    m = 6  # drop the painted border pixels around each seam
    quads = [a[:sy - m, :sx - m], a[:sy - m, sx + m:], a[sy + m:, :sx - m], a[sy + m:, sx + m:]]
    out = Image.new('RGB', (1024, 1024))
    for i, q in enumerate(quads):
        q = np.asarray(Image.fromarray(q).resize((512, 512), Image.LANCZOS))
        out.paste(Image.fromarray(q), ((i % 2) * 512, (i // 2) * 512))
    out.save(OUT + 'guild-atlas-a.png')
    print('atlas a: seams at', sx, sy, '->', OUT + 'guild-atlas-a.png')


def atlas_b(src):
    im = Image.open(src).convert('RGB').resize((1024, 1024), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mag = np.clip(((r + b) * 0.5 - g - 60) / 80, 0, 1)        # magenta-ness: high red + blue, low green
    alpha = 1 - mag
    spill = np.minimum(r, b) - g
    fix = (spill > 20) & (alpha > 0)
    a[..., 0] = np.where(fix, np.minimum(r, g + 20 + (r - b).clip(0)), r)
    a[..., 2] = np.where(fix, np.minimum(b, g + 20 + (b - r).clip(0)), b)
    out = np.dstack([a.clip(0, 255), (alpha * 255)]).astype(np.uint8)
    Image.fromarray(out, 'RGBA').save(OUT + 'guild-atlas-b.png')
    lab, n = ndimage.label(alpha > 0.5)
    print('atlas b ->', OUT + 'guild-atlas-b.png', n, 'pieces')
    for i, sl in enumerate(ndimage.find_objects(lab)):
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        if (y1 - y0) * (x1 - x0) < 900:
            continue
        print('piece %2d px x %4d-%4d y %4d-%4d  uv (%.3f, %.3f, %.3f, %.3f)' % (i, x0, x1, y0, y1, x0 / 1024, 1 - y1 / 1024, x1 / 1024, 1 - y0 / 1024))


if __name__ == '__main__':
    {'a': atlas_a, 'b': atlas_b}[sys.argv[1]](sys.argv[2])

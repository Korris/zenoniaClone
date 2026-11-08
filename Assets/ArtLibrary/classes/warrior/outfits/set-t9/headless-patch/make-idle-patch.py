# Usage: move headless-patch/idle.png away, run build-head-layer.py warrior outfit-headless set-t9 (unpatched cut), then
#   python make-idle-patch.py all 2 headless-patch/idle.png  and run outfit-headless again (2026-11-08, T9 redraw).
# headless-patch for set-t9 idle: outfit pixels the head cut removed that are not the painted head (gold horns, dragon heads, wing edges),
# kept only where joined to what is left of the body. Drawn UNDER the cut sheet by outfit_headless, the head layer draws over it.
import sys, numpy as np
EYE_ROW = 88
from PIL import Image
from scipy import ndimage
sys.path.insert(0, 'D:/zenoniaClone/Tools')
from head_layer_classes import CLASSES
B = 'D:/zenoniaClone/Assets/Resources/Sprites/classes/warrior/body'
ob = np.array(Image.open(f'{B}/set-t9/bare/idle.png').convert('RGBA')).astype(np.float64)
hl = np.array(Image.open(f'{B}/set-t9/headless/idle.png').convert('RGBA')).astype(np.float64)
lv1hl = np.array(Image.open(f'{B}/headless/idle.png').convert('RGBA')).astype(np.float64)
lv1 = np.array(Image.open(f'{B}/bare/idle.png').convert('RGBA')).astype(np.float64)
r, g, b = ob[..., 0], ob[..., 1], ob[..., 2]
hair = (r > 150) & (g < 115) & (r > g + 95) & (ob[..., 3] > 8)   # painted red hair only (the Warrior hair test r > g + 60 also takes gold)
skin = (r > 180) & (g > 120) & (b > 90) & (r - b < 120) & (g - b > 10) & (abs(r - g) < 90) & (b > 0.55 * g)
lv1head = (lv1[..., 3] > 8) & (lv1hl[..., 3] <= 8)          # Lv1 head silhouette (what the Lv1 cut erased)
cut = (ob[..., 3] > 8) & (hl[..., 3] <= 8)
hs = hair | skin
lab0, _ = ndimage.label(hs)
ids = [i for i in np.unique(lab0[lv1head]) if i > 0]
sz0 = ndimage.sum(hs, lab0, ids)
ids = [i for i, z in zip(ids, sz0) if z > 30]
painted_head = ndimage.binary_dilation(np.isin(lab0, ids), iterations=int(sys.argv[2]) if len(sys.argv) > 2 else 2)
mode = sys.argv[1]
lum = ob[..., :3] @ [0.3, 0.59, 0.11]
ring = ndimage.binary_dilation(painted_head, iterations=4) & (lum < 80)   # the painted hair's dark outline
cand = cut & ~painted_head & ~ring
if mode == 'outside':   # only outside the Lv1 head silhouette (grown 1 px)
    cand &= ~ndimage.binary_dilation(lv1head, iterations=1)
patch = np.zeros_like(ob); patch[cand] = ob[cand]
# keep pieces joined to the cut body
lab, n = ndimage.label((hl[..., 3] > 8) | cand, np.ones((3, 3)))
body_ids = np.unique(lab[hl[..., 3] > 8]); keep = np.isin(lab, body_ids[body_ids > 0]) & cand
patch[~keep] = 0
patch[192:] = 0   # only row 0 (idle down) is the outfit; other rows are Lv1
Image.fromarray(patch.clip(0, 255).astype(np.uint8)).save(sys.argv[3])
print('patch px', int(keep.sum()), 'cut px row0', int(cut[:192].sum()))
# gaps beside the ears/neck that only the Lv1 hair covered: below the eye row, inside the Lv1 head silhouette (grown 2 px), not covered by
# every hairstyle, within 4 px of the body: filled with the nearest body colour so short hairstyles do not show background there
import json
sys.path.insert(0, 'D:/zenoniaClone/Tools'); import head_layer_core as core
H = 'D:/zenoniaClone/Assets/Resources/Sprites/classes/warrior/heads'
an = [a for a in json.load(open('D:/zenoniaClone/Assets/Resources/Data/classes/warrior/head-anchors.json'))['anims'] if a['anim'] == 'idle' and a['dir'] == 'down'][0]['frames']
body = core.over(patch, hl)
solid = body[..., 3] > 128
fill = np.zeros(ob.shape[:2], bool)
for f in range(4):
    sl = (slice(0, 192), slice(f * 192, f * 192 + 192))
    cover = np.ones((192, 192), bool)
    for st in range(5):
        h = core.place_head(np.array(Image.open(f'{H}/{st}/down.png').convert('RGBA')).astype(np.float64), an[f])
        cover &= h[..., 3] > 128
    zone = ndimage.binary_dilation(lv1head[sl], iterations=2) & ~cover & ~solid[sl]
    zone[:EYE_ROW] = False
    zone &= ndimage.binary_dilation(solid[sl], iterations=4)
    fill[sl] = zone
_, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
patch[fill] = body[iy[fill], ix[fill]]; patch[fill, 3] = 255
Image.fromarray(patch.clip(0, 255).astype(np.uint8)).save(sys.argv[3])
print('gap fill px', int(fill.sum()))

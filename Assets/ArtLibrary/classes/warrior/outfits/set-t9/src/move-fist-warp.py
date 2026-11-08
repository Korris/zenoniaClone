# Used 2026-11-08: python move-fist-warp.py src/idle-down.png 65,128 55,132 18  (painted fist -> Lv1 fist spot, cell px)
# Moves the painted right fist (viewer's left) of set-t9 idle-down onto the Lv1 fist spot with a smooth local warp (forearm stretches).
import sys, importlib, numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, 'D:/zenoniaClone/Tools'); m = importlib.import_module('build-warrior-outfit-sheets')
SRC = 'D:/zenoniaClone/Assets/ArtLibrary/classes/warrior/outfits/set-t9/src/idle-down-redraw-chatgpt-3.png'
DST = sys.argv[1]
cur = tuple(map(float, sys.argv[2].split(',')))   # painted fist centre, cell px
tgt = tuple(map(float, sys.argv[3].split(',')))   # Lv1 fist centre, cell px
R = float(sys.argv[4]) if len(sys.argv) > 4 else 14   # warp radius, cell px
src = np.array(Image.open(SRC).convert('RGB')).astype(np.float32)
a = src.astype(int); r, g, b = a[..., 0], a[..., 1], a[..., 2]
fg = ~((g > 1.3 * np.maximum(r, b)) & (g > 110)); fg[:4] = fg[-4:] = False; fg[:, :4] = fg[:, -4:] = False
lab, n = ndimage.label(ndimage.binary_closing(fg, iterations=2)); sz = ndimage.sum(fg, lab, range(1, n + 1))
keep = [i + 1 for i in range(n) if sz[i] > 0.15 * sz.max()]
ys, xs = np.where(np.isin(lab, keep)); y0, x0 = ys.min(), xs.min()
fig = m.figures(SRC)[0]; fig = fig if isinstance(fig, Image.Image) else Image.fromarray(fig.astype(np.uint8))
old = m.cell(Image.open(m.BODY + '/bare/idle.png').convert('RGBA'), 0, 0)
top, bot, hx = m.box(old)
fa = np.asarray(fig)[..., 3] > 20; fy, fx = np.where(fa)
k = (bot - top + 1) / (fy.max() - fy.min() + 1)
fs = fig.resize((max(1, round(fig.width * k)), max(1, round(fig.height * k))), Image.LANCZOS)
sa = np.asarray(fs)[..., 3] > 20; sy, sx = np.where(sa)
head = sx[sy < sy.min() + 0.2 * (sy.max() - sy.min())]; fhx = (head.min() + head.max()) / 2
ox, oy = round(hx - fhx), round(bot - sy.max())
to_src = lambda p: ((p[0] - ox) / k + x0, (p[1] - oy) / k + y0)
c, t = np.array(to_src(cur)), np.array(to_src(tgt)); d = t - c; Rs = R / k
print('k', k, 'src cur', c, 'tgt', t, 'R', Rs)
H, W = src.shape[:2]; YY, XX = np.mgrid[0:H, 0:W].astype(np.float32)
# backward map: output pixel p samples p - d*w(|p - t|), w smooth 1 at the target, 0 at radius
dist = np.hypot(XX - t[0], YY - t[1]); RIN = 58; w = np.clip(1 - (dist - RIN) / Rs, 0, 1); w = w * w * (3 - 2 * w)
sxm, sym = XX - d[0] * w, YY - d[1] * w
out = np.stack([ndimage.map_coordinates(src[..., ch], [sym, sxm], order=1, mode='nearest') for ch in range(3)], -1)
Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(DST)

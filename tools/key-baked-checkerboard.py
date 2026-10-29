# Remove a baked-in light checkerboard background (ChatGPT often paints a fake one instead of real alpha):
# usage: python tools/key-baked-checkerboard.py <src.png> <dst.png>
# plus large enclosed light pockets (gaps between arms/legs).
import sys, numpy as np, cv2
from PIL import Image
src, dst = sys.argv[1], sys.argv[2]
rgb = np.array(Image.open(src).convert('RGB')).astype(int)
mx, mn = rgb.max(2), rgb.min(2)
bgish = ((mn > 170) & (mx - mn < 22)).astype(np.uint8)
n, lab, stats, _ = cv2.connectedComponentsWithStats(bgish, connectivity=4)
border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
keep_bg = border | {i for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] > 300}
bg = np.isin(lab, list(keep_bg))
alpha = np.where(bg, 0, 255).astype(np.uint8)
# soften the cut edge by 1px so the light fringe does not halo
alpha = cv2.erode(alpha, np.ones((2, 2), np.uint8))
Image.fromarray(np.dstack([rgb.astype(np.uint8), alpha]), 'RGBA').save(dst)
print('bg %', round(bg.mean() * 100, 1))

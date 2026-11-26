"""Measure figures in a 5-direction idle sheet on flat background.
Usage: python measure_idle.py IMG [IMG...]
Prints per-figure bbox height, width, head height (top to chin estimate = row where width shrinks to neck),
background colour detected from corner."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

def measure(path):
    im = np.asarray(Image.open(path).convert('RGB')).astype(int)
    bg = np.median(np.concatenate([im[:5,:5].reshape(-1,3), im[-5:,-5:].reshape(-1,3)]),axis=0)
    d = np.abs(im - bg).sum(2)
    fg = d > 90
    fg = ndimage.binary_opening(fg, iterations=2)
    lab, n = ndimage.label(fg)
    sizes = ndimage.sum(fg, lab, range(1, n+1))
    keep = [i+1 for i,s in enumerate(sizes) if s > fg.size*0.004]
    objs = ndimage.find_objects(lab)
    figs = []
    for k in keep:
        sl = objs[k-1]
        figs.append((sl[1].start, sl[1].stop, sl[0].start, sl[0].stop, k))
    figs.sort()
    H, W = fg.shape
    print(f"{path}  size={W}x{H} bg={bg.astype(int).tolist()} figs={len(figs)}")
    res = []
    for x0,x1,y0,y1,k in figs:
        m = lab[y0:y1, x0:x1] == k
        h = y1 - y0
        widths = m.sum(1)
        # head: from top, find neck = min width in band 25%-50% of height
        a, b = int(h*0.22), int(h*0.5)
        neck = a + int(np.argmin(widths[a:b]))
        headw = widths[:neck].max()
        shoulder = widths[neck:neck+int(h*0.15)].max()
        res.append((h, neck/h, headw/h, shoulder/headw))
        print(f"  x{x0}-{x1} y{y0}-{y1} h={h} w={x1-x0} neck@{neck} head%={neck/h*100:.1f} headW%={headw/h*100:.1f} shoulder/headW={shoulder/headw:.2f} heads={h/neck:.2f} feetY={y1} imgH%={h/H*100:.1f}")
    return res

for p in sys.argv[1:]:
    measure(p)

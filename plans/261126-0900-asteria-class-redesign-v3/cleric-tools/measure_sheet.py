"""Measure figures on a flat #00FF00 sheet: per figure top/bottom/height, head width (max width in top 30%)."""
import sys
import numpy as np
from PIL import Image
def fg_mask(path):
    a = np.asarray(Image.open(path).convert('RGB')).astype(int)
    r,g,b = a[...,0],a[...,1],a[...,2]
    return ~((g > 150) & (g - r > 70) & (g - b > 70))
def figures(m, mincol=8):
    cols = m.sum(0) > 2
    segs=[];s=None
    for x,v in enumerate(cols):
        if v and s is None: s=x
        if not v and s is not None:
            if x-s>mincol: segs.append((s,x))
            s=None
    if s is not None: segs.append((s,len(cols)))
    # merge segments separated by small gaps (<12px)
    out=[]
    for sg in segs:
        if out and sg[0]-out[-1][1]<12: out[-1]=(out[-1][0],sg[1])
        else: out.append(sg)
    return [o for o in out if o[1]-o[0]>40]
for p in sys.argv[1:]:
    m=fg_mask(p); H=m.shape[0]
    print(p, m.shape)
    for i,(x0,x1) in enumerate(figures(m)):
        sub=m[:,x0:x1]; rows=np.where(sub.sum(1)>1)[0]
        t,b=rows[0],rows[-1]; h=b-t+1
        widths=[ (np.where(sub[y])[0][-1]-np.where(sub[y])[0][0]+1) if sub[y].any() else 0 for y in range(t,b+1)]
        hw=max(widths[:int(h*0.30)])
        print(f"  fig{i} x{x0}-{x1} top {t} ({t/H:.1%}) feet {b} ({b/H:.1%}) h {h} headW {hw} ({hw/h:.1%})")

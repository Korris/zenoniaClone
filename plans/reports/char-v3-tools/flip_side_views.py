"""Mirror figures 2-4 (3/4 Front, Side, 3/4 Back) of a 5-dir idle sheet in place so they face screen-right
like the Knight sheet. Usage: python flip_side_views.py IN OUT"""
import sys
from PIL import Image, ImageOps
sys.path.insert(0,'plans/reports/char-v3-tools')
from measure_v3 import figs
im,F=figs(sys.argv[1]); src=Image.open(sys.argv[1]).convert('RGB'); out=src.copy()
bg=tuple(int(v) for v in im[2,2])
for sl,m in F[1:4]:
    y0,y1,x0,x1=sl[0].start,sl[0].stop,sl[1].start,sl[1].stop
    pad=6; box=(max(0,x0-pad),max(0,y0-pad),min(src.width,x1+pad),min(src.height,y1+pad))
    c=src.crop(box); out.paste(Image.new('RGB',c.size,bg),box[:2]); out.paste(ImageOps.mirror(c),box[:2])
out.save(sys.argv[2]); print('saved',sys.argv[2])

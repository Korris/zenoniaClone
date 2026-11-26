"""Side-by-side review image: Knight idle row (top) vs new class idle row (bottom), scaled so both
figure heights are equal (420 px), with guide lines at skull top, Knight chin and feet.
Usage: python side_by_side.py NEW_SHEET OUT_PNG"""
import sys
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, 'plans/reports/char-v3-tools')
from measure_v3 import figs, KNIGHT, metrics
FH=420; PAD=40
def row(path):
    im,F=figs(path); src=Image.open(path).convert('RGB'); cells=[]
    for sl,m in F:
        y0,y1,x0,x1=sl[0].start,sl[0].stop,sl[1].start,sl[1].stop
        c=src.crop((x0,y0,x1,y1)); s=FH/(y1-y0)
        cells.append(c.resize((max(1,int((x1-x0)*s)),FH),Image.LANCZOS))
    return cells
k=row(KNIGHT); n=row(sys.argv[1]); kc=metrics(KNIGHT)['chin_pct']/100
cw=max(c.width for c in k+n)+PAD
W=cw*5+PAD; H=(FH+PAD*2)*2+30
out=Image.new('RGB',(W,H),(236,232,224)); d=ImageDraw.Draw(out)
for r,cells in enumerate([k,n]):
    top=PAD+30+r*(FH+PAD*2)
    for i,c in enumerate(cells[:5]):
        out.paste(c,(PAD+i*cw+(cw-PAD-c.width)//2,top))
    for y,col in [(top,(200,0,0)),(top+int(FH*kc),(0,0,220)),(top+FH,(200,0,0))]:
        d.line([(0,y),(W,y)],fill=col,width=2)
d.text((10,8),'TOP: Knight (current Warrior) idle | BOTTOM: new class idle | same figure height; red = skull top / feet, blue = Knight chin',fill=(0,0,0))
out.save(sys.argv[2]); print('saved',sys.argv[2])

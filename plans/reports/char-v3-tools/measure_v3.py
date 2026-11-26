"""Robust proportion metrics for 5-dir idle sheets (bald body) vs Knight.
front skull height = top of figure to chin (last skin row in centre column of the front figure),
skull width = widest row in top 30 % of figure (incl. ears) for front + back figures.
Usage: python measure_v3.py SHEET [SHEET...]  (Knight measured first automatically)"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
KNIGHT='Assets/ArtLibrary/ro-fantasy-heroes/src/body-warrior-idle-5dir-chatgpt-6.png'

def figs(path):
    im=np.asarray(Image.open(path).convert('RGB')).astype(int)
    bg=np.median(np.concatenate([im[:5,:5].reshape(-1,3),im[-5:,-5:].reshape(-1,3)]),axis=0)
    fg=ndimage.binary_opening(np.abs(im-bg).sum(2)>90,iterations=2)
    lab,n=ndimage.label(fg); sizes=ndimage.sum(fg,lab,range(1,n+1))
    out=[]
    for k,sl in enumerate(ndimage.find_objects(lab)):
        if sizes[k]>fg.size*0.004: out.append((sl[1].start,sl,lab[sl]==k+1))
    out.sort(key=lambda t:t[0])
    return im,[(sl,m) for _,sl,m in out]

def metrics(path):
    im,F=figs(path)
    res={}
    hs=[];ws=[]
    for i,(sl,m) in enumerate(F):
        h=m.shape[0]; hs.append(h)
        ws.append(m[:int(h*0.30)].sum(1).max()/h)
    sl,m=F[0]; h=m.shape[0]; y0,x0=sl[0].start,sl[1].start
    cols=np.where(m[int(h*0.1)])[0]; cx=x0+int(cols.mean())
    skin=im[y0+int(h*0.22), cx]   # cheek/nose area
    col=im[y0:y0+int(h*0.6), cx-3:cx+4].mean(1)
    isskin=np.abs(col-skin).sum(1)<60
    chin=None
    for y in range(int(h*0.2),int(h*0.55)):
        if isskin[y-1] and not isskin[y:y+8].any(): chin=y; break
    res=dict(height=int(np.median(hs)), heights=hs, skullW_pct=[round(w*100,1) for w in ws],
             skullW_front_back=round((ws[0]+ws[-1])/2*100,1), chin_pct=round(chin/h*100,1) if chin else None,
             heads=round(h/chin,2) if chin else None, img_h_pct=round(np.median(hs)/im.shape[0]*100,1))
    return res

if __name__=='__main__':
  k=metrics(KNIGHT); print('KNIGHT',k)
  for p in sys.argv[1:]:
    r=metrics(p)
    dW=(r['skullW_front_back']/k['skullW_front_back']-1)*100
    dC=(r['chin_pct']/k['chin_pct']-1)*100 if r['chin_pct'] else float('nan')
    dH=(r['img_h_pct']/k['img_h_pct']-1)*100
    print(p.split('/')[-1], r, f"| vs Knight: skullW {dW:+.1f}%  skull-chin {dC:+.1f}%  height-in-image {dH:+.1f}%")

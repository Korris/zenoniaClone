"""Skull width above the ears (max width in top 20 % of figure height) per figure, vs Knight."""
import sys
sys.path.insert(0,'plans/reports/char-v3-tools')
from measure_v3 import figs, KNIGHT
def w(p):
    _,F=figs(p); out=[]
    for sl,m in F:
        h=m.shape[0]; out.append(round(m[:int(h*0.20)].sum(1).max()/h*100,1))
    return out
k=w(KNIGHT); print('knight',k)
for p in sys.argv[1:]:
    n=w(p); print(p.split('/')[-1],n,[f"{(b/a-1)*100:+.1f}%" for a,b in zip(k,n)])

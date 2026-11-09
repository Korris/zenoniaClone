"""Install T9 side idle/combo sheets and outfit-specific layered anchors."""
from pathlib import Path
import argparse,json,re,uuid
import numpy as np
from scipy import ndimage
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--target',type=Path,required=True);args=p.parse_args()
target=args.target.resolve(); assert (target/'ProjectSettings/ProjectVersion.txt').exists()
res=target/'Assets/Resources'; base=res/'Sprites/classes/wizard/body'; outfit=base/'set-t9'
art=ROOT/'Assets/ArtLibrary/classes/wizard/northwind-t9-combos'
heads=json.loads((outfit/'head-anchors.json').read_text())
held=json.loads((outfit/'held-anchors.json').read_text())
original=json.loads((res/'Data/classes/wizard/held-wizard-melee.json').read_text())['anchors']
reference={(a['part'],a['anim'],a['dir']):a['frames'] for a in original}
default_heads={(a['anim'],a['dir']):a['frames'] for a in json.loads((ROOT/'Assets/Resources/Data/classes/wizard/head-anchors.json').read_text())['anims']}
def put(table,key,entry):
    table[:]=[v for v in table if any(v.get(k)!=entry.get(k) for k in key)];table.append(entry)
def save(im,path,template):
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        content=Path(str(template)+'.meta').read_text()
        meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,content,count=1))
for anim in ['idle','attack1','attack2','attack3','attack4']:
    count=6 if anim=='attack4' else 4
    sheets={kind:Image.open((outfit if anim=='idle' else base)/kind/f'{anim}.png').convert('RGBA') for kind in ['bare','headless','hand']}
    for direction,row in [('left',2),('right',3)]:
        folder=ROOT/'Assets/ArtLibrary/classes/wizard/northwind-t9-sides' if anim=='idle' else art
        strip=Image.open(folder/f'{anim}-{direction}-headless.png').convert('RGBA')
        headframes=[]; gear={part:[] for part in ['staff','book']}
        for f in range(count):
            source_frame=1 if anim=='attack3' and f==0 else f
            body=strip.crop((source_frame*192,0,(source_frame+1)*192,192)); a=np.array(body)
            ha=dict(default_heads[(anim,direction)][f])
            # Match the default animation's neck, soles and both fist joints.
            # The T9 artwork supplies clothing; the default supplies motion.
            k=82/95
            ky=(82-ha['y'])/95
            def motion(x,y):
                return (96+(x-96)*k+ha['x']*(189-y)/95,189-(189-y)*ky)
            rgb=a[:,:,:3].astype(float);r,g,b=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
            skin=(a[:,:,3]>140)&(r>150)&(g>85)&(r>g*1.09)&(g>b*1.02)&(b>g*.62)
            labels,n=ndimage.label(skin); candidates=[]
            for lab in range(1,n+1):
                yy,xx=np.where(labels==lab)
                if len(xx)>=4 and yy.mean()<160:
                    candidates.append((float(xx.mean()),float(yy.mean()),lab))
            used=set(); mask=np.zeros((192,192),bool)
            for part in ['staff','book']:
                if (part,anim,direction) not in reference:
                    gear[part].append(dict(x=96,y=120,s=.25,angle=0,hidden=True))
                    continue
                anchor=dict(reference[(part,anim,direction)][f])
                ex=96+(anchor['x']-96)*95/82; ey=94+(anchor['y']-107)*95/82
                choices=[v for v in candidates if v[2] not in used]
                if not choices:raise ValueError(f'No fist {anim}/{direction}/{f}/{part}')
                x,y,lab=min(choices,key=lambda v:(v[0]-ex)**2+(v[1]-ey)**2)
                used.add(lab);mask|=ndimage.binary_dilation(labels==lab,iterations=2)
                gx,gy=motion(x,y)
                anchor.update(x=gx,y=gy,fallback=False,hidden=False)
                anchor.pop('art',None)
                gear[part].append(anchor)
            hand=a.copy();hand[:,:,3]=np.where(mask,a[:,:,3],0)
            yy,xx=np.indices((192,192))
            source_y=189-(189-yy)/ky
            source_x=96+(xx-96-ha['x']*(189-source_y)/95)/k
            coords=np.column_stack([source_x.ravel(),source_y.ravel()])
            def warp(pixels):
                # Interpolate premultiplied colors to retain clean alpha edges.
                rgba=pixels.astype(float);rgba[:,:,:3]*=rgba[:,:,3:4]/255
                fitted=np.stack([ndimage.map_coordinates(rgba[:,:,ch],[coords[:,1],coords[:,0]],order=1,mode='constant').reshape(192,192) for ch in range(4)],axis=2)
                fitted[:,:,:3]=np.divide(fitted[:,:,:3]*255,fitted[:,:,3:4],out=np.zeros_like(fitted[:,:,:3]),where=fitted[:,:,3:4]>0)
                return Image.fromarray(np.clip(fitted,0,255).astype('uint8'))
            body=warp(a);hand=warp(hand)
            for kind,im in [('bare',body),('headless',body),('hand',hand)]:
                sheets[kind].paste(im,(f*192,row*192))
            headframes.append(ha)
        put(heads['anims'],['anim','dir'],dict(anim=anim,dir=direction,frames=headframes))
        for part,frames in gear.items():put(held['anchors'],['part','anim','dir'],dict(part=part,anim=anim,dir=direction,frames=frames))
    for kind,im in sheets.items(): save(im,outfit/kind/f'{anim}.png',base/kind/f'{anim}.png')
(outfit/'head-anchors.json').write_text(json.dumps(heads,indent=2))
(outfit/'held-anchors.json').write_text(json.dumps(held,indent=2))
print('Installed T9 side idle + attack1-4, head and grip anchors, fist overlays')

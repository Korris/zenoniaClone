"""Register authored hair-only parts and bald anatomy to existing 192px head cells."""
from pathlib import Path
import argparse,json,math
import numpy as np
from scipy.optimize import minimize
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--classes',default='wizard,mechanic,warrior,assassin,cleric');args=p.parse_args()
def move(im,v):
    dx,dy,s=v;cx,cy=96,72
    return im.transform((192,192),Image.Transform.AFFINE,(1/s,0,cx-(cx+dx)/s,0,1/s,cy-(cy+dy)/s),Image.Resampling.BILINEAR)
def face_points(im,cls,direction):
    a=np.array(im);r,g,b=[a[:,:,i].astype(float) for i in range(3)];yy,xx=np.indices(a.shape[:2]);box=im.getbbox()
    skin=(a[:,:,3]>100)&(r>150)&(g>80)&(r>=g)&(g>b+5)&(r>b+18)&(r<b+120)&(yy>box[1]+(box[3]-box[1])*.52)
    from scipy import ndimage
    labels,n=ndimage.label(skin);sizes=np.bincount(labels.ravel());sizes[0]=0
    if not n:return None
    skin=labels==sizes.argmax()
    sy,sx=np.where(skin);chin=float(sy.max())
    if cls in ('wizard','mechanic'):color=(b>r*1.2)&((g>r*1.1)|(b>g*1.2))&(r<185)
    elif cls in ('warrior','assassin'):color=(r>g*1.5)&(r>b*1.5)
    else:color=(g>r*1.2)&(g>b*1.05)
    eye=color&(a[:,:,3]>100)&(yy>=sy.min())&(yy<chin-4)&(xx>=sx.min()-1)&(xx<=sx.max()+1)
    labels,n=ndimage.label(eye);components=[]
    for lab in range(1,n+1):
        ey,ex=np.where(labels==lab)
        if len(ex)>=4:components.append((len(ex),float(ex.mean()),float(ey.mean())))
    if not components:return None
    components.sort(reverse=True)
    # Irises are compact components; selecting the lowest colored pixel picked
    # up blue/purple outline specks at the temples instead of the actual eyes.
    chosen=components[:2] if direction=='down' and len(components)>1 and components[1][0]>components[0][0]*.35 else components[:1]
    return (float(np.mean([v[1] for v in chosen])),float(np.mean([v[2] for v in chosen])),chin)
for cls in args.classes.split(','):
    folder=ROOT/f'Assets/ArtLibrary/avatar-hair-parts/{cls}'
    data=json.loads((folder/'layout.json').read_text());crop=data['crop'];size=crop[2]-crop[0]
    sheet=Image.open(folder/'parts-generated.png').convert('RGBA');records=[];alignment={}
    def unpack(col,row):
        cell=sheet.crop((col*sheet.width//6,row*sheet.height//4,(col+1)*sheet.width//6,(row+1)*sheet.height//4)).resize((256,256),Image.Resampling.LANCZOS)
        a=np.array(cell);a[:,:,3]=np.where(a[:,:,3]<8,0,a[:,:,3]);cell=Image.fromarray(a)
        from scipy import ndimage
        labels,n=ndimage.label(a[:,:,3]>150)
        if not n:raise ValueError(f'Missing opaque asset: {cls}/{row}/{col}')
        sizes=np.bincount(labels.ravel());sizes[0]=0
        core=np.isin(labels,np.where(sizes>=max(8,sizes.max()*.001))[0])
        a[:,:,3]=np.where(ndimage.binary_dilation(core,iterations=2),a[:,:,3],0)
        cell=Image.fromarray(a)
        art=cell.crop((8,8,248,248)).resize((size,size),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(192,192));canvas.alpha_composite(art,(crop[0],crop[1]));return canvas
    for row,direction in enumerate(['down','up','right']):
        for style in data['styles']:
            part=unpack(style,row)
            ref=Image.open(ROOT/f'Assets/Resources/Sprites/classes/{cls}/heads/{style}/{direction}.png').convert('RGBA')
            box=ref.getbbox();limit=round(box[1]+(box[3]-box[1])*.50)
            target=np.array(ref)[:,:,3].astype(float)/255
            region=np.indices(target.shape)[0]<limit
            def cost(v):
                alpha=np.array(move(part,v))[:,:,3].astype(float)/255
                return float(np.mean((target[region]-alpha[region])**2))
            result=minimize(cost,[0,0,1],method='Powell',bounds=[(-8,8),(-8,8),(.86,1.14)],options=dict(maxiter=30,xtol=.05,ftol=.00001))
            fitted=move(part,result.x)
            dest=folder/f'hair/{style}';dest.mkdir(parents=True,exist_ok=True);fitted.save(dest/f'{direction}.png')
            if style==0:alignment[direction]=result.x
            records.append(dict(style=style,dir=direction,transform=result.x.tolist(),cost=cost(result.x)))
    dest=folder/'anatomy';dest.mkdir(exist_ok=True)
    for col,direction in enumerate(['down','up','right']):
        base=move(unpack(col,3),alignment[direction])
        if direction!='up':
            ref=Image.open(ROOT/f'Assets/Resources/Sprites/classes/{cls}/heads/0/{direction}.png').convert('RGBA')
            source_points=face_points(base,cls,direction);target_points=face_points(ref,cls,direction)
            if source_points and target_points:
                sx,sy,sc=source_points;tx,ty,tc=target_points
                k=(tc-ty)/(sc-sy)
                yy,xx=np.indices((192,192));source_y=np.where(yy<=ty,yy-(ty-sy),sy+(yy-ty)/k)
                source_x=xx-(tx-sx)
                from scipy import ndimage
                raw=np.array(base).astype(float);raw[:,:,:3]*=raw[:,:,3:4]/255
                fitted=np.stack([ndimage.map_coordinates(raw[:,:,ch],[source_y,source_x],order=1,mode='constant') for ch in range(4)],axis=2)
                fitted[:,:,:3]=np.divide(fitted[:,:,:3]*255,fitted[:,:,3:4],out=np.zeros_like(fitted[:,:,:3]),where=fitted[:,:,3:4]>0)
                base=Image.fromarray(np.clip(fitted,0,255).astype('uint8'))
                records.append(dict(anatomy=direction,source=source_points,target=target_points))
        base.save(dest/f'{direction}.png')
    (folder/'registration.json').write_text(json.dumps(records,indent=2))
    print(f'Imported {cls}: {len(data["styles"])*3} hair parts + 3 bald base heads')

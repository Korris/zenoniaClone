"""Make Northwind Wizard share one head/neck model across idle, walk and combos."""
from pathlib import Path
import argparse,json,math
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--target',type=Path,required=True);args=p.parse_args()
RES=args.target/'Assets/Resources';BASE=RES/'Sprites/classes/wizard/body';HEADS=RES/'Sprites/classes/wizard/heads'
ART=ROOT/'Assets/ArtLibrary/classes/wizard/unified-northwind-model';OUT=ROOT/'Build/wizard-unified-review';OUT.mkdir(parents=True,exist_ok=True)
DIRS=['down','up','left','right'];JOINT={'down':(98,107),'up':(96,105),'left':(92,103),'right':(100,103)}
heads=json.loads((RES/'Data/classes/wizard/head-anchors.json').read_text());ha={(e['anim'],e['dir']):e for e in heads['anims']}
held=json.loads((RES/'Data/classes/wizard/held-wizard-melee.json').read_text());ga={(e['part'],e['anim'],e['dir']):e for e in held['anchors']}
local=json.loads((BASE/'held-anchors.json').read_text())
for e in local['anchors']:
    ga.setdefault((e['part'],e['anim'],e['dir']),e)
def skin(a):
    r,g,b=[a[:,:,i].astype(float) for i in range(3)]
    return (a[:,:,3]>160)&(r>175)&(g>130)&(b>100)&(r>=g)&(g>b+5)&(r>b+18)&(r<b+120)
def read(anim,row,f):
    im=Image.open(ART/f'{anim}-source.png').convert('RGBA')
    transpose=anim in ['idle','attack1','attack2','attack3']
    c,r=(row,f) if transpose else (f,row)
    cols=6 if anim=='attack4' else 4
    cell=im.crop((c*im.width//cols,r*im.height//4,(c+1)*im.width//cols,(r+1)*im.height//4));a=np.array(cell)
    labs,n=ndimage.label(a[:,:,3]>150);sizes=np.bincount(labs.ravel());sizes[0]=0
    a[:,:,3]=np.where(ndimage.binary_dilation(labs==sizes.argmax(),iterations=2),a[:,:,3],0)
    cell=Image.fromarray(a);cell=cell.crop(cell.getbbox());a=np.array(cell);yy,xx=np.indices(a.shape[:2])
    if row==1:
        ny=round(cell.height*.44);nx=cell.width/2
    else:
        mask=skin(a)&(yy<cell.height*.53);labs,n=ndimage.label(mask);sizes=np.bincount(labs.ravel());sizes[0]=0
        fy,fx=np.where(labs==sizes.argmax());ny=int(fy.max())+2;nx=float(fx[fy>=fy.max()-2].mean())
    fy,fx=np.where((a[:,:,3]>150)&(yy>cell.height*.91));foot=(float(fx.min())+float(fx.max()))/2
    return cell,nx,ny,foot
def put(table,keys,entry):
    table[:]=[e for e in table if any(e[k]!=entry[k] for k in keys)];table.append(entry)
def transform(im,a):
    s=a.get('s',1);sy=a.get('sy',1);t=math.radians(a.get('angle',0));c=math.cos(t);n=math.sin(t)
    A=c/s;B=n/s;D=-n/(s*sy);E=c/(s*sy)
    return im.transform((192,192),Image.Transform.AFFINE,(A,B,96-A*(96+a['x'])-B*(192+a['y']),D,E,192-D*(96+a['x'])-E*(192+a['y'])),Image.Resampling.BICUBIC)
def model_head(direction):
    d='right' if direction=='left' else direction;im=Image.open(HEADS/f'0/{d}.png').convert('RGBA')
    return im.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if direction=='left' else im
def head_anchor(nx,ny,direction):
    native=Image.open(ROOT/f'Assets/Resources/Sprites/classes/wizard/heads/0/{"right" if direction=="left" else direction}.png').getbbox()
    hs=65/(native[3]-native[1]);jx,jy=JOINT[direction]
    return dict(x=nx-96-(jx-96)*hs,y=ny-192-(jy-192)*hs,s=hs,sy=1,angle=0)
def fists(body,anim,direction,f,neck_y):
    a=np.array(body);m=skin(a)&(np.indices(a.shape[:2])[0]>neck_y+7);labs,n=ndimage.label(m);choices=[]
    for lab in range(1,n+1):
        y,x=np.where(labs==lab)
        if len(x)>=4:choices.append((float(x.mean()),float(y.mean()),lab))
    mask=np.zeros((192,192),bool);entries={};used=set()
    for part in ['staff','book']:
        key=(part,anim,direction)
        if key not in ga:continue
        old=ga[key]['frames'][f];available=[v for v in choices if v[2] not in used] or choices
        if not available:continue
        x,y,lab=min(available,key=lambda v:(v[0]-old['x'])**2+(v[1]-old['y'])**2);used.add(lab)
        entry=dict(old,x=x,y=y,fallback=False);entry.pop('art',None);entries[part]=entry
        mask|=ndimage.binary_dilation(labs==lab,iterations=2)
    a[:,:,3]=np.where(mask,a[:,:,3],0);return Image.fromarray(a),entries
records=[]
# Import natural nape silhouettes; never clip the hair horizontally at the chin.
source=Image.open(ART/'head-parts-source.png').convert('RGBA')
canonical=ART/'canonical'
for col,direction in enumerate(['down','up','right']):
    pieces=[]
    for row in range(3):
        cell=source.crop((col*source.width//3,row*source.height//3,(col+1)*source.width//3,(row+1)*source.height//3))
        a=np.array(cell);labs,n=ndimage.label(a[:,:,3]>150);sizes=np.bincount(labs.ravel());sizes[0]=0
        keep=ndimage.binary_dilation(labs==sizes.argmax(),iterations=2)
        a[:,:,3]=np.where(keep,a[:,:,3],0);pieces.append(Image.fromarray(a))
    head=pieces[0];box=head.getbbox();a=np.array(head);y,x=np.where(skin(a))
    neck_y=int(y.max());neck_x=float(x[y>=neck_y-3].mean())
    # Generated rows keep the same design but can shift within their cells.
    # Register the hair crown and the bald face before applying the native map.
    hair=pieces[1];hb=hair.getbbox();aligned=Image.new('RGBA',head.size)
    aligned.alpha_composite(hair,(round((box[0]+box[2]-hb[0]-hb[2])/2),box[1]-hb[1]));pieces[1]=aligned
    def eyes(im):
        a=np.array(im);r,g,b=[a[:,:,i].astype(float) for i in range(3)]
        yy=np.indices(a.shape[:2])[0];mask=(a[:,:,3]>150)&(b>r+35)&(g>r+20)&(yy>im.height*.4)
        labs,n=ndimage.label(mask);sizes=np.bincount(labs.ravel());sizes[0]=0
        labs_used=np.argsort(sizes)[-2:] if direction=='down' else [int(sizes.argmax())]
        y,x=np.where(np.isin(labs,labs_used)&(labs>0));return float(x.mean()),float(y.mean())
    bald=pieces[2];ba=np.array(bald);by,bx=np.where(skin(ba));bottom=int(by.max())
    if direction!='up':
        ex,ey=eyes(head);bxeye,byeye=eyes(bald)
        sy=(neck_y-ey)/(bottom-byeye);sx=sy
        bald=bald.resize((round(bald.width*sx),round(bald.height*sy)),Image.Resampling.LANCZOS)
        aligned=Image.new('RGBA',head.size);aligned.alpha_composite(bald,(round(ex-bxeye*sx),round(ey-byeye*sy)));pieces[2]=aligned
    else:
        bb=bald.getbbox();aligned=Image.new('RGBA',head.size)
        aligned.alpha_composite(bald,(round(neck_x-(bb[0]+bb[2])/2),neck_y-bottom));pieces[2]=aligned
    old=Image.open(ROOT/f'Assets/Resources/Sprites/classes/wizard/heads/0/{direction}.png');native=old.getbbox()[3]-old.getbbox()[1]
    scale=native/(box[3]-box[1]);jx,jy=JOINT[direction]
    for row,kind in enumerate(['head0','hair0','anatomy']):
        # Hair-only and bald rows share the complete head's cell coordinates.
        part=pieces[row];part=part.resize((round(part.width*scale),round(part.height*scale)),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(192,192));canvas.alpha_composite(part,(round(jx-neck_x*scale),round(jy-neck_y*scale)))
        dest=canonical/kind;dest.mkdir(parents=True,exist_ok=True);canvas.save(dest/f'{direction}.png')
        if row==0:canvas.save(HEADS/f'0/{direction}.png')
for anim in ['idle','walk','attack1','attack2','attack3','attack4']:
    count=6 if anim=='attack4' else 4
    sheets={kind:Image.new('RGBA',(count*192,768)) for kind in ['bare','headless','hand']}
    for row,direction in enumerate(DIRS):
        cells=[read(anim,row,f) for f in range(count)]
        body_scale=95/float(np.median([c.height-ny for c,nx,ny,foot in cells]))
        for f,(cell,nx,ny,foot) in enumerate(cells):
            # Resample the connected source, keeping every collar and nape pixel.
            # Upper and lower proportions meet continuously at the authored neck;
            # no independently pasted face or horizontal head cut is involved.
            neck_y=189-(cell.height-ny)*body_scale
            neck_x=96+(nx-foot)*body_scale
            head_scale=65/ny
            yy,xx=np.indices((192,192),dtype=float)
            source_y=np.where(yy<neck_y,ny+(yy-neck_y)/head_scale,ny+(yy-neck_y)/body_scale)
            blend=np.clip((source_y-(ny-12))/24,0,1)
            sx=head_scale+(body_scale-head_scale)*blend
            source_x=nx+(xx-neck_x)/sx
            a=np.array(cell)
            sampled=np.stack([ndimage.map_coordinates(a[:,:,ch].astype(float),[source_y,source_x],order=1,mode='constant',cval=0) for ch in range(4)],axis=2).clip(0,255).astype('uint8')
            full=Image.fromarray(sampled)
            torso=sampled.copy();torso[yy<neck_y]=0;frame=Image.fromarray(torso)
            anchor=head_anchor(neck_x,neck_y,direction)
            hand,entries=fists(frame,anim,direction,f,neck_y);full.alpha_composite(hand)
            for kind,im in [('bare',full),('headless',frame),('hand',hand)]:sheets[kind].paste(im,(f*192,row*192))
            ha[(anim,direction)]['frames'][f]=anchor
            for part,entry in entries.items():
                ga[(part,anim,direction)]['frames'][f]=entry
            records.append(dict(anim=anim,dir=direction,frame=f,headSize=65,neck=[neck_x,neck_y]))
        for part in ['staff','book']:
            if (part,anim,direction) in ga:put(local['anchors'],['part','anim','dir'],dict(ga[(part,anim,direction)]))
    for kind,im in sheets.items():im.save(BASE/kind/f'{anim}.png')
# T9 side art was previously shrunk to the legacy body82/head70 skeleton.
# Keep its authored 95px body and use the same 65px model head as default T0.
outfit=BASE/'set-t9';oh=json.loads((outfit/'head-anchors.json').read_text());og=json.loads((outfit/'held-anchors.json').read_text())
for anim in ['idle','attack1','attack2','attack3','attack4']:
    sheets={kind:Image.open(outfit/kind/f'{anim}.png').convert('RGBA') for kind in ['bare','headless','hand']}
    count=6 if anim=='attack4' else 4
    for direction,row in [('left',2),('right',3)]:
        folder=ROOT/'Assets/ArtLibrary/classes/wizard/northwind-t9-sides' if anim=='idle' else ROOT/'Assets/ArtLibrary/classes/wizard/northwind-t9-combos'
        strip=Image.open(folder/f'{anim}-{direction}-headless.png').convert('RGBA');frames=[];parts={}
        for f in range(count):
            frame=strip.crop((f*192,0,(f+1)*192,192));anchor=head_anchor(96,94,direction);frames.append(anchor)
            full=frame.copy();full.alpha_composite(transform(model_head(direction),anchor));hand,entries=fists(frame,anim,direction,f,94);full.alpha_composite(hand)
            for kind,im in [('bare',full),('headless',frame),('hand',hand)]:sheets[kind].paste(im,(f*192,row*192))
            for part,entry in entries.items():parts.setdefault(part,[]).append(entry)
        put(oh['anims'],['anim','dir'],dict(anim=anim,dir=direction,frames=frames))
        for part,values in parts.items():put(og['anchors'],['part','anim','dir'],dict(part=part,anim=anim,dir=direction,frames=values))
    for kind,im in sheets.items():im.save(outfit/kind/f'{anim}.png')
(RES/'Data/classes/wizard/head-anchors.json').write_text(json.dumps(heads,indent=2))
(RES/'Data/classes/wizard/held-wizard-melee.json').write_text(json.dumps(held,indent=2))
(BASE/'held-anchors.json').write_text(json.dumps(local,indent=2))
(outfit/'head-anchors.json').write_text(json.dumps(oh,indent=2));(outfit/'held-anchors.json').write_text(json.dumps(og,indent=2))
(OUT/'registration.json').write_text(json.dumps(records,indent=2))
print('Unified Northwind Wizard: idle/walk/attack model + T9 sides; head65/body95 in every supported state')

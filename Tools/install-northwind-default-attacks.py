"""Install real Northwind T0 default attacks, with consistent idle/head scale."""
from pathlib import Path
import argparse,json,re,uuid,math
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--target',type=Path,required=True);args=p.parse_args()
RES=args.target/'Assets/Resources';BODY=RES/'Sprites/classes/wizard/body'
ART=ROOT/'Assets/ArtLibrary/classes/wizard/northwind-default-attacks'
OUT=ROOT/'Build/Northwind-T9-Combo/prebuild-default';OUT.mkdir(parents=True,exist_ok=True)
heads=json.loads((ROOT/'Assets/Resources/Data/classes/wizard/head-anchors.json').read_text())
anchors={(e['anim'],e['dir']):e for e in heads['anims']}
held=json.loads((RES/'Data/classes/wizard/held-wizard-melee.json').read_text())
gear={(e['part'],e['anim'],e['dir']):e for e in held['anchors']}
records=[]
idle_seeds={}
landmarks=json.loads((ART/'collar-landmarks.json').read_text())
def extract_cell(source, count, row, f, anim):
    cell=source.crop((f*source.width//count,row*source.height//4,(f+1)*source.width//count,(row+1)*source.height//4))
    pixels=np.array(cell);labs,n=ndimage.label(pixels[:,:,3]>150);sizes=np.bincount(labs.ravel());sizes[0]=0
    if sizes.max()<200:raise ValueError('Missing body')
    pixels[:,:,3]=np.where(ndimage.binary_dilation(labs==sizes.argmax(),iterations=2),pixels[:,:,3],0)
    cell=Image.fromarray(pixels);box=cell.getbbox();cell=cell.crop(box);pixels=np.array(cell)
    yy,xx=np.indices(pixels.shape[:2])
    point=landmarks[anim][['down','up','left','right'][row]][f]
    cx=point[0]+f*(source.width//count)-f*source.width//count-box[0]
    cy=point[1]+row*(source.height//4)-row*source.height//4-box[1]
    # Root registration comes from the feet, not the moving collar.
    fy,fx=np.where((pixels[:,:,3]>150)&(yy>cell.height*.91))
    foot_x=(float(fx.min())+float(fx.max()))/2
    return cell,float(cx),float(cy),foot_x
def save(im,path,template):
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
    meta=Path(str(path)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,Path(str(template)+'.meta').read_text(),count=1))
def place_head(body,style,direction,a):
    h=Image.open(RES/f'Sprites/classes/wizard/heads/{style}/{"right" if direction=="left" else direction}.png').convert('RGBA')
    if direction=='left':h=h.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    s=a['s'];sy=a.get('sy',1);t=math.radians(a.get('angle',0));c=math.cos(t);n=math.sin(t)
    A=c/s;B=n/s;D=-n/(s*sy);E=c/(s*sy)
    h=h.transform((192,192),Image.Transform.AFFINE,(A,B,96-A*(96+a['x'])-B*(192+a['y']),D,E,192-D*(96+a['x'])-E*(192+a['y'])),Image.Resampling.BICUBIC)
    result=body.copy();result.alpha_composite(h);return result
for num in range(1,5):
    anim=f'attack{num}';count=6 if num==4 else 4
    assert (ART/f'{anim}-source.png').exists(), f'Missing {anim} source'
    source=Image.open(ART/f'{anim}-source.png').convert('RGBA')
    scales={}
    for row in range(4):
        spans=[]
        for f in range(count):
            cell,cx,cy,foot=extract_cell(source,count,row,f,anim)
            spans.append(cell.height-cy)
        scales[row]=95/float(np.median(spans))
    sheets={kind:Image.new('RGBA',(count*192,768)) for kind in ['bare','headless','hand']}
    for row,direction in enumerate(['down','up','left','right']):
        h0=Image.open(RES/f'Sprites/classes/wizard/heads/0/{"right" if direction=="left" else direction}.png').convert('RGBA')
        native=h0.getchannel('A').point(lambda a:255 if a>100 else 0).getbbox();hs=65/(native[3]-native[1])
        frames=[]
        for f in range(count):
            cell,neck_x,neck_y,foot_x=extract_cell(source,count,row,f,anim)
            old=anchors[(anim,direction)]['frames'][f]
            scale=scales[row]
            fitted=cell.resize((round(cell.width*scale),round(cell.height*scale)),Image.Resampling.LANCZOS)
            dx=round(96-foot_x*scale);dy=189-fitted.height
            nx=dx+neck_x*scale;ny=dy+neck_y*scale
            body=Image.new('RGBA',(192,192));body.alpha_composite(fitted,(dx,dy))
            joint={'down':(98,107),'up':(96,105),'left':(92,103),'right':(100,103)}[direction]
            a=dict(x=nx-96-(joint[0]-96)*hs,y=ny-192-(joint[1]-192)*hs,s=hs,sy=1,angle=0)
            assert abs(96+a['x']+(joint[0]-96)*hs-nx)<.001
            assert abs(192+a['y']+(joint[1]-192)*hs-ny)<.001
            frames.append(a)
            pixels=np.array(body);r,g,b=[pixels[:,:,i].astype(float) for i in range(3)]
            skin=(pixels[:,:,3]>140)&(r>150)&(g>85)&(r>g*1.09)&(g>b*1.02)&(b>g*.62)
            labels,n=ndimage.label(skin);candidates=[]
            for lab in range(1,n+1):
                cy,cx=np.where(labels==lab)
                if len(cx)>=4 and cy.mean()<165:candidates.append((float(cx.mean()),float(cy.mean()),lab))
            used=set();handmask=np.zeros((192,192),bool)
            for part in ['staff','book']:
                entry=gear.get((part,anim,direction))
                if entry is None:continue
                anchor=entry['frames'][f]
                ex=96+(anchor['x']-96)*95/82;ey=94+(anchor['y']-107)*95/82
                choices=[c for c in candidates if c[2] not in used] or candidates
                if not choices:raise ValueError(f'Missing fist {anim}/{direction}/{f}')
                x,y,lab=min(choices,key=lambda v:(v[0]-ex)**2+(v[1]-ey)**2);used.add(lab)
                anchor.update(x=x,y=y,fallback=False);anchor.pop('art',None)
                handmask|=ndimage.binary_dilation(labels==lab,iterations=2)
            hand=pixels.copy();hand[:,:,3]=np.where(handmask,pixels[:,:,3],0);hand=Image.fromarray(hand)
            # Unlike the old high-collar costume, Northwind has an open neck.
            # This is body anatomy beneath the interchangeable head, registered
            # to the same measured joint; do not leave a transparent neck gap.
            neck=Image.new('RGBA',(192,192))
            nd=ImageDraw.Draw(neck)
            nd.polygon([(nx-4,ny-8),(nx+4,ny-8),(nx+4,ny+3),(nx-4,ny+3)],fill=(55,38,44,255))
            nd.polygon([(nx-3,ny-8),(nx+3,ny-8),(nx+3,ny+2),(nx-3,ny+2)],fill=(255,211,174,255))
            neck.alpha_composite(body);body=neck
            sheets['headless'].paste(body,(f*192,row*192));sheets['hand'].paste(hand,(f*192,row*192))
            if num==1 and f==0:
                idle_seeds[direction]=(body.copy(),hand.copy(),dict(a),{part:dict(gear[(part,anim,direction)]['frames'][f]) for part in ['staff','book'] if (part,anim,direction) in gear})
            baked=place_head(body,0,direction,a);baked.alpha_composite(hand);sheets['bare'].paste(baked,(f*192,row*192))
            dest=OUT/f'{anim}-{direction}';dest.mkdir(exist_ok=True)
            for style in range(6):
                preview=place_head(body,style,direction,a);preview.alpha_composite(hand)
                bg=Image.new('RGBA',(192,192),'#263443');bg.alpha_composite(preview)
                bg.convert('RGB').resize((576,576),Image.Resampling.NEAREST).save(dest/f'f{f}-hair{style}.png')
            records.append(dict(anim=anim,dir=direction,frame=f,scale=scale,neck=[nx,ny],headScale=hs,headJoint=joint))
        anchors[(anim,direction)]['frames']=frames
    for kind,im in sheets.items():save(im,BODY/kind/f'{anim}.png',ROOT/f'Assets/Resources/Sprites/classes/wizard/body/{kind}/{anim}.png')
idle={kind:Image.open(BODY/kind/'idle.png').convert('RGBA') for kind in ['bare','headless','hand']}
outfit_held=json.loads((BODY/'held-anchors.json').read_text()) if (BODY/'held-anchors.json').exists() else dict(anchors=[])
for direction,row in [('up',1),('left',2),('right',3)]:
    body,hand,a,parts=idle_seeds[direction];frames=[]
    for f,sy in enumerate([1,.9947,.9894,.9947]):
        b=body.transform((192,192),Image.Transform.AFFINE,(1,0,0,0,1/sy,189-189/sy),Image.Resampling.BICUBIC)
        h=hand.transform((192,192),Image.Transform.AFFINE,(1,0,0,0,1/sy,189-189/sy),Image.Resampling.BICUBIC)
        anchor=dict(a,y=-3+(a['y']+3)*sy,sy=sy);frames.append(anchor)
        full=place_head(b,0,direction,anchor);full.alpha_composite(h)
        for kind,im in [('bare',full),('headless',b),('hand',h)]:idle[kind].paste(im,(f*192,row*192))
    anchors[('idle',direction)]['frames']=frames
    for part,seed in parts.items():
        outfit_held['anchors']=[e for e in outfit_held['anchors'] if (e['part'],e['anim'],e['dir'])!=(part,'idle',direction)]
        outfit_held['anchors'].append(dict(part=part,anim='idle',dir=direction,frames=[dict(seed,y=189+(seed['y']-189)*sy) for sy in [1,.9947,.9894,.9947]]))
for kind,im in idle.items():im.save(BODY/kind/'idle.png')
(BODY/'held-anchors.json').write_text(json.dumps(outfit_held,indent=2))
(RES/'Data/classes/wizard/head-anchors.json').write_text(json.dumps(heads,indent=2))
(RES/'Data/classes/wizard/held-wizard-melee.json').write_text(json.dumps(held,indent=2))
(OUT/'registration.json').write_text(json.dumps(records,indent=2))
print(f'Installed {len(records)} Northwind T0 attack frames / 4 directions; exported {len(records)*6} magnified hairstyle reviews')

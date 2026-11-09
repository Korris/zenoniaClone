"""Compile hairstyle variants around the authored face/neck in each animation frame.

The runtime selects a complete frame, never a separately moving head. Original
sprite sheets and head art remain inputs; compiled variants are disposable output.
"""
from pathlib import Path
import argparse,json,math,re,uuid
import numpy as np
from scipy import ndimage
from scipy.spatial import ConvexHull
from PIL import Image,ImageDraw
from head_layer_classes import CLASSES

ROOT=Path(__file__).resolve().parents[1]
P=argparse.ArgumentParser();P.add_argument('--target',type=Path,required=True)
P.add_argument('--classes',default='mechanic,wizard,warrior,assassin,cleric')
P.add_argument('--base-only',action='store_true')
P.add_argument('--review',type=Path,default=ROOT/'Build/avatar-hair-frames/review')
ARGS=P.parse_args();RES=ARGS.target/'Assets/Resources';ARGS.review.mkdir(parents=True,exist_ok=True)
DIRS=['down','up','left','right'];S=192

def rgba(path):return Image.open(path).convert('RGBA')
def skin(a):
    r,g,b=[a[:,:,i].astype(float) for i in range(3)]
    return (a[:,:,3]>100)&(r>175)&(g>130)&(b>100)&(r>=g)&(g>b+5)&(r>b+18)&(r<b+120)
def face_mask(im,direction,cls):
    a=np.array(im);mask=skin(a);box=im.getbbox()
    if not box or direction=='up':return np.zeros(mask.shape,bool)
    yy,xx=np.indices(mask.shape)
    mask&=(yy>box[1]+(box[3]-box[1])*.54)
    labels,n=ndimage.label(mask)
    if not n:raise ValueError('No facial skin found')
    sizes=np.bincount(labels.ravel());sizes[0]=0
    core=labels==sizes.argmax()
    # Ears can be separated from the cheek by the hair outline.
    y,x=np.where(core)
    allowed=(abs(xx-float(x.mean()))<max(18,float(x.max()-x.min())*.85))&(yy>=y.min()-2)&(yy<=y.max()+3)
    points=np.column_stack(np.where(mask&allowed))[:,::-1]
    if len(points)<3:raise ValueError('Incomplete face')
    hull=ConvexHull(points);result=Image.new('1',im.size)
    ImageDraw.Draw(result).polygon([tuple(v) for v in points[hull.vertices]],fill=1)
    face=ndimage.binary_dilation(np.array(result),iterations=1)
    if cls=='assassin':
        # The covered mouth is black fabric, not peach skin.
        y,x=np.where(face)
        face|=(xx>=x.min())&(xx<=x.max())&(yy>=y.min())&(yy<=y.max()+20)&(a[:,:,3]>50)&~CLASSES[cls]['hair'](a)
    return face
def transform(im,a):
    s=a.get('s',1) or 1;sy=a.get('sy',1) or 1;t=math.radians(a.get('angle',0));c=math.cos(t);n=math.sin(t)
    A=c/s;B=n/s;D=-n/(s*sy);E=c/(s*sy)
    return im.transform((S,S),Image.Transform.AFFINE,(A,B,96-A*(96+a['x'])-B*(192+a['y']),D,E,192-D*(96+a['x'])-E*(192+a['y'])),Image.Resampling.BICUBIC)
def head_art(folder,style,direction):
    d='right' if direction=='left' else direction
    im=rgba(folder/str(style)/f'{d}.png')
    return im.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if direction=='left' else im
def save(im,path,template):
    path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,Path(str(template)+'.meta').read_text(),count=1))

report=[]
for cls in ARGS.classes.split(','):
    root=RES/f'Sprites/classes/{cls}';body=root/'body';head_dir=root/'heads'
    data=json.loads((RES/f'Data/classes/{cls}/head-anchors.json').read_text())
    base_anchors={(e['anim'],e['dir']):e['frames'] for e in data['anims']}
    styles=[]
    for style in range(10):
        if not (head_dir/str(style)/'down.png').exists():break
        styles.append(style)
    hair={};heads={};anatomy={}
    parts_dir=ROOT/f'Assets/ArtLibrary/avatar-hair-parts/{cls}'
    unified=ROOT/'Assets/ArtLibrary/classes/wizard/unified-northwind-model/canonical'
    unified_wizard=(cls=='wizard' and (unified/'head0/right.png').exists() and rgba(unified/'head0/right.png').tobytes()==rgba(head_dir/'0/right.png').tobytes())
    assert (parts_dir/'anatomy/down.png').exists(), f'Missing authored hair parts: {cls}'
    for style in styles:
        for direction in DIRS:
            im=head_art(head_dir,style,direction);heads[style,direction]=im
            d='right' if direction=='left' else direction
            part=rgba(parts_dir/f'hair/{style}/{d}.png')
            base_head=rgba(parts_dir/f'anatomy/{d}.png')
            if unified_wizard:
                if style==0:part=rgba(unified/f'hair0/{d}.png')
                base_head=rgba(unified/f'anatomy/{d}.png')
            if direction=='left':
                part=part.transpose(Image.Transpose.FLIP_LEFT_RIGHT);base_head=base_head.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            hair[style,direction]=part;anatomy[direction]=base_head
    for outfit in [body]+([] if ARGS.base_only else sorted(p for p in body.glob('set-t*') if p.is_dir())):
        anchors=dict(base_anchors)
        anchor_path=outfit/'head-anchors.json'
        if outfit!=body and anchor_path.exists():
            anchors.update({(e['anim'],e['dir']):e['frames'] for e in json.loads(anchor_path.read_text())['anims']})
        for path in sorted((outfit/'bare').glob('*.png')):
            anim=path.stem
            if not (outfit/'headless'/path.name).exists():continue
            master=rgba(path);headless=rgba(outfit/'headless'/path.name)
            sheets={s:master.copy() for s in styles};count=master.width//S
            for row,direction in enumerate(DIRS):
                frames=anchors.get((anim,direction))
                if not frames:continue
                for f,a in enumerate(frames[:count]):
                    box=(f*S,row*S,(f+1)*S,(row+1)*S)
                    original=master.crop(box);torso=headless.crop(box)
                    placed=transform(heads[0,direction],a)
                    roi=np.array(placed)[:,:,3]>20
                    # Some outfit bare sheets are headless fallback art. Materialize
                    # their face once before generating independent hair variants.
                    if outfit!=body and (unified_wizard or original.tobytes()==torso.tobytes() or (direction!='up' and (skin(np.array(original))&roi).sum()<25)):
                        original=torso.copy();original.alpha_composite(placed)
                    original_array=np.array(original)
                    old_hair=transform(hair[0,direction],a)
                    hm=np.array(old_hair)[:,:,3]>10
                    removed=(original_array[:,:,3].astype(int)-np.array(torso)[:,:,3].astype(int))>20
                    # Keep the authored face, including eye blinks and expressions.
                    actual_face=face_mask(Image.fromarray(np.where(ndimage.binary_dilation(roi,iterations=8)[:,:,None],original_array,0).astype('uint8')),direction,cls)
                    face_outline=actual_face.copy()
                    # The authored matte distinguishes fringe from eyes even
                    # when their colors match (blue eyes / cyan forelock).
                    actual_face&=~ndimage.binary_dilation(hm,iterations=1)
                    # Preserve the authored collar/neck exactly. Only pixels
                    # removed by the original head split can be replaced here.
                    erase=removed&~actual_face
                    clean=original_array.copy();clean[erase]=0
                    bald=transform(anatomy[direction],a)
                    if face_outline.any():
                        fy,fx=np.where(face_outline)
                        lower=np.indices(face_outline.shape)[0]>=fy.min()
                        ba=np.array(bald)
                        ba[:,:,3]=np.where(lower&~ndimage.binary_dilation(face_outline,iterations=1),0,ba[:,:,3])
                        bald=Image.fromarray(ba)
                    bald.alpha_composite(Image.fromarray(clean))
                    for style in styles:
                        if style==0:
                            frame=original.copy()  # default retains the authored connected anatomy, pixel for pixel
                        else:
                            if unified_wizard:
                                # Wizard's new connected master has a different
                                # face registration from the legacy bald model.
                                # Bake each complete authored alternate head at
                                # this frame's measured neck; keep it in the same
                                # final sprite as the moving coat.
                                frame=torso.copy()
                                frame.alpha_composite(transform(heads[style,direction],a))
                                sheets[style].paste(frame,(f*S,row*S))
                                if outfit==body and (anim.startswith(('attack','melee')) or anim in ('idle','walk')):
                                    dest=ARGS.review/cls/f'style{style}'/anim;dest.mkdir(parents=True,exist_ok=True)
                                    bg=Image.new('RGBA',(S,S),'#263443');bg.alpha_composite(frame)
                                    bg.convert('RGB').resize((S*3,S*3),Image.Resampling.NEAREST).save(dest/f'{direction}-{f}.png')
                                continue
                            part=np.array(hair[style,direction]);yy=np.indices(part.shape[:2])[0]
                            ref_skin=skin(np.array(heads[0,direction]));ys,_=np.where(ref_skin)
                            chin=int(ys.max()) if len(ys) else 105
                            back=part.copy();front=part.copy()
                            back[:,:,3]=np.where(yy>chin-2,part[:,:,3],0)
                            front[:,:,3]=np.where(yy<=chin-2,part[:,:,3],0)
                            frame=transform(Image.fromarray(back),a)
                            frame.alpha_composite(bald)
                            frame.alpha_composite(transform(Image.fromarray(front),a))
                        sheets[style].paste(frame,(f*S,row*S))
                        if outfit==body and (anim.startswith(('attack','melee')) or anim in ('idle','walk')):
                            dest=ARGS.review/cls/f'style{style}'/anim;dest.mkdir(parents=True,exist_ok=True)
                            bg=Image.new('RGBA',(S,S),'#263443');bg.alpha_composite(frame)
                            bg.convert('RGB').resize((S*3,S*3),Image.Resampling.NEAREST).save(dest/f'{direction}-{f}.png')
            if outfit==body:
                assert sheets[0].tobytes()==master.tobytes(), f'Default master changed: {cls}/{anim}'
            for style,sheet in sheets.items():
                save(sheet,outfit/f'appearance/{style}/{anim}.png',path)
            report.append(dict(cls=cls,outfit=outfit.name,anim=anim,styles=len(styles),frames=count*4))
        (outfit/'appearance').mkdir(exist_ok=True)
        (outfit/'appearance/manifest.json').write_text(json.dumps(dict(version=1,styles=len(styles),anatomy='authored-frame',hair='compiled')))
    print(f'Compiled complete hairstyle frames: {cls}, {len(styles)} styles',flush=True)
(ARGS.review/'report.json').write_text(json.dumps(report,indent=2))

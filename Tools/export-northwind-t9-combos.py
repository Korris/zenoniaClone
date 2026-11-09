"""Export Northwind T9 combo art while retaining independent hairstyle layers."""
from pathlib import Path
import json
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ArtLibrary/classes/wizard/northwind-t9-combos'
records=[]
board=Image.new('RGB',(2304,8*404),'#263443')
for attack in range(1,5):
    if not (OUT/f'attack{attack}-source.png').exists(): continue
    count=6 if attack==4 else 4
    source=Image.open(OUT/f'attack{attack}-source.png').convert('RGBA')
    for row,direction in enumerate(('left','right')):
        previews=[]
        sheet=Image.new('RGBA',(count*192,192))
        for frame in range(count):
            cell=source.crop((frame*source.width//count,row*source.height//2,(frame+1)*source.width//count,(row+1)*source.height//2))
            a=np.array(cell); labels,n=ndimage.label(a[:,:,3]>150)
            sizes=np.bincount(labels.ravel());sizes[0]=0
            keep=ndimage.binary_dilation(labels==sizes.argmax(),iterations=2)
            a[:,:,3]=np.where(keep,a[:,:,3],0)
            cell=Image.fromarray(a);cell=cell.crop(cell.getbbox());a=np.array(cell)
            yy,xx=np.indices(a.shape[:2]); top=(yy<cell.height*.19)&(abs(xx-cell.width/2)<cell.width*.23)&(a[:,:,3]>180)
            dark=top&(a[:,:,:3].max(axis=2)<110)
            cy,cx=np.where(dark)
            neck_y=float(np.median(cy)) if len(cx) else cell.height*.055
            neck_x=float(np.median(cx)) if len(cx) else cell.width/2
            scale=95/(cell.height-neck_y)
            fitted=cell.resize((round(cell.width*scale),round(cell.height*scale)),Image.Resampling.LANCZOS)
            body=Image.new('RGBA',(192,192));dx=round(96-neck_x*scale);dy=189-fitted.height
            body.alpha_composite(fitted,(dx,dy))
            dest=OUT/'frames'/direction/f'attack{attack}';dest.mkdir(parents=True,exist_ok=True)
            body.save(dest/f'{frame}.png');sheet.alpha_composite(body,(192*frame,0))
            head=Image.open(ROOT/'Assets/Resources/Sprites/classes/wizard/heads/0/right.png').convert('RGBA')
            if direction=='left':head=head.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            hs=65/70; head=head.resize((round(192*hs),round(192*hs)),Image.Resampling.LANCZOS)
            preview=body.copy();preview.alpha_composite(head,(round(96-96*hs+(3 if direction=='left' else -3)),round(97-103*hs)))
            bg=Image.new('RGBA',(192,192),'#263443');bg.alpha_composite(preview)
            previews.append(bg.convert('RGB').resize((384,384),Image.Resampling.NEAREST))
            board.paste(previews[-1],(frame*384,((attack-1)*2+row)*404+20))
            records.append(dict(anim=f'attack{attack}',direction=direction,frame=frame,uniformScale=scale,neck=94,sole=189))
        sheet.save(OUT/f'attack{attack}-{direction}-headless.png')
        previews[0].save(OUT/f'attack{attack}-{direction}.gif',save_all=True,append_images=previews[1:],duration=90,loop=0)
        ImageDraw.Draw(board).text((8,((attack-1)*2+row)*404+4),f'attack{attack} / {direction} / {count} frames',fill='white')
board.save(OUT/'review.png')
(OUT/'frames.json').write_text(json.dumps(dict(cellSize=192,headSeparate=True,frames=records),indent=2))
print(f'Exported {len(records)} combo frames')
